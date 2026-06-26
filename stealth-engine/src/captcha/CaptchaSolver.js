/**
 * @NODAL_OS_RESTRICTED
 * @policy no-audit, no-modify, no-analyze, no-refactor
 * ADVERTENCIA: Código protegido. Solo el propietario puede modificarlo.
 */
export class CaptchaSolver {
  constructor(config = {}) {
    this.twoCaptchaApiKey = config.twoCaptchaApiKey || '';
    this.capSolverApiKey = config.capSolverApiKey || '';
    this.visualSolver = config.visualSolver || null;
    this.proxy = config.proxy || null;
  }

  async solve(page, detection, taskId, proxy = null) {
    const sitekey = detection.sitekey || await this.findSitekey(page);
    const url = page.url();
    const viewport = page.viewportSize() || { width: 1280, height: 720 };

    if (['geetest', 'funcaptcha', 'kasada'].includes(detection.type) && this.capSolverApiKey) {
      try {
        console.log(`[${taskId}] Attempting CAPTCHA solve via CapSolver for ${detection.type}`);
        const result = await this._solveCapSolver(page, sitekey, url, taskId, detection.type, proxy);
        if (result.success) return result;
        console.warn(`[${taskId}] CapSolver failed: ${result.error}`);
      } catch (e) {
        console.warn(`[${taskId}] CapSolver error: ${e.message}`);
      }
    }

    if (sitekey && this.twoCaptchaApiKey) {
      try {
        console.log(`[${taskId}] Attempting CAPTCHA solve via 2captcha for ${detection.type}`);
        const result = await this._solve2captcha(sitekey, url, taskId, detection.type, proxy);
        if (result.success) return result;
        console.warn(`[${taskId}] 2captcha failed: ${result.error}, trying visual solver`);
      } catch (e) {
        console.warn(`[${taskId}] 2captcha error: ${e.message}`);
      }
    }

    if (this.visualSolver && this.visualSolver.enabled) {
      try {
        console.log(`[${taskId}] Attempting visual CAPTCHA solve`);
        const screenshot = await page.screenshot({ clip: { x: 0, y: 0, width: Math.min(viewport.width, 600), height: Math.min(viewport.height, 400) } });
        const captchaType = await this.visualSolver.classify(screenshot);
        console.log(`[${taskId}] Visual CAPTCHA classified as: ${captchaType}`);
        const result = await this.visualSolver.solve(screenshot, captchaType, {}, proxy);
        if (result.success) return { ...result, provider: 'visual', durationMs: 0, cost: 0 };
      } catch (e) {
        console.warn(`[${taskId}] Visual solver error: ${e.message}`);
      }
    }

    if (!sitekey && !this.visualSolver?.enabled) {
      return { success: false, error: 'No sitekey found and visual solver not enabled' };
    }

    return { success: false, error: 'All solvers failed' };
  }

  async _solve2captcha(sitekey, url, taskId, captchaType, proxy = null) {
    const startTime = Date.now();

    var safeUrl;
    try { const u = new URL(url); safeUrl = u.origin + u.pathname; } catch { safeUrl = url; }

    var taskType;
    var taskConfig;
    switch ((captchaType || '').split('_')[0]) {
      case 'hcaptcha':
        taskType = 'HCaptchaTaskProxyless';
        taskConfig = { type: taskType, websiteURL: safeUrl, websiteKey: sitekey };
        break;
      case 'cloudflare':
        taskType = 'TurnstileTaskProxyless';
        taskConfig = { type: taskType, websiteURL: safeUrl, websiteKey: sitekey };
        break;
      default:
        taskType = 'NoCaptchaTaskProxyless';
        taskConfig = { type: taskType, websiteURL: safeUrl, websiteKey: sitekey };
    }

    const fetchOpts = { headers: { 'Content-Type': 'application/json' } };
    if (proxy) {
      try {
        const { ProxyAgent } = await import('undici');
        const parsed = new URL(proxy.server);
        const auth = (proxy.username || proxy.password)
          ? `${encodeURIComponent(proxy.username || '')}:${encodeURIComponent(proxy.password || '')}@`
          : '';
        const proxyUrl = `http://${auth}${parsed.hostname}:${parsed.port || 8080}`;
        fetchOpts.dispatcher = new ProxyAgent(proxyUrl);
      } catch (e) {
        console.warn(`[${taskId}] Could not create proxy dispatcher: ${e.message}`);
      }
    }

    const createResp = await fetch('https://api.2captcha.com/createTask', {
      ...fetchOpts,
      method: 'POST',
      body: JSON.stringify({ clientKey: this.twoCaptchaApiKey, task: taskConfig }),
    });
    const createResult = await createResp.json();
    if (createResult.errorId !== 0) return { success: false, error: createResult.errorDescription || 'create task failed' };

    const taskId2 = createResult.taskId;
    const maxWait = 120000;
    const startWait = Date.now();
    let token = null;

    while (!token && Date.now() - startWait < maxWait) {
      await new Promise(r => setTimeout(r, 3000));
      const checkResp = await fetch('https://api.2captcha.com/getTaskResult', {
        ...fetchOpts,
        method: 'POST',
        body: JSON.stringify({ clientKey: this.twoCaptchaApiKey, taskId: taskId2 }),
      });
      const checkResult = await checkResp.json();
      if (checkResult.status === 'ready') token = checkResult.solution?.gRecaptchaResponse || null;
      else if (checkResult.errorId !== 0) return { success: false, error: checkResult.errorDescription || 'get result failed' };
    }

    if (!token) return { success: false, error: 'Timed out after 120s' };

    const durationMs = Date.now() - startTime;
    console.log(`[${taskId}] 2captcha solved in ${durationMs}ms`);
    return { success: true, token, provider: '2captcha', durationMs, cost: 0.002 };
  }

  async _solveCapSolver(page, sitekey, url, taskId, captchaType, proxy = null) {
    const startTime = Date.now();

    var safeUrl;
    try { const u = new URL(url); safeUrl = u.origin + u.pathname; } catch { safeUrl = url; }

    var taskType;
    var taskConfig;
    switch (captchaType) {
      case 'geetest': {
        taskType = 'GeeTestTaskProxyless';
        const gt = sitekey || await this._extractGeeTestGt(page);
        const challenge = await this._extractGeeTestChallenge(page, gt);
        taskConfig = { type: taskType, websiteURL: safeUrl, gt: gt || '', challenge: challenge || '' };
        if (!gt || !challenge) {
          return { success: false, error: 'Could not extract GeeTest challenge data' };
        }
        break;
      }
      case 'funcaptcha':
        taskType = 'FunCaptchaTaskProxyless';
        taskConfig = { type: taskType, websiteURL: safeUrl, websitePublicKey: sitekey || '' };
        break;
      case 'kasada': {
        taskType = 'AntiKasadaTaskProxyless';
        const kasadaData = await this._extractKasadaData(page);
        if (!kasadaData) {
          return { success: false, error: 'Could not extract Kasada challenge data' };
        }
        taskConfig = { type: taskType, websiteURL: safeUrl, ...kasadaData };
        break;
      }
      default:
        return { success: false, error: 'Unsupported CapSolver type: ' + captchaType };
    }

    const fetchOpts = { headers: { 'Content-Type': 'application/json' } };
    if (proxy) {
      try {
        const { ProxyAgent } = await import('undici');
        const parsed = new URL(proxy.server);
        const auth = (proxy.username || proxy.password)
          ? `${encodeURIComponent(proxy.username || '')}:${encodeURIComponent(proxy.password || '')}@`
          : '';
        const proxyUrl = `http://${auth}${parsed.hostname}:${parsed.port || 8080}`;
        fetchOpts.dispatcher = new ProxyAgent(proxyUrl);
      } catch (e) {
        console.warn(`[${taskId}] Could not create proxy dispatcher: ${e.message}`);
      }
    }

    const createResp = await fetch('https://api.capsolver.com/createTask', {
      ...fetchOpts,
      method: 'POST',
      body: JSON.stringify({ clientKey: this.capSolverApiKey, task: taskConfig }),
    });
    const createResult = await createResp.json();
    if (createResult.errorId !== 0) return { success: false, error: createResult.errorDescription || 'create task failed' };

    const taskId2 = createResult.taskId;
    const maxWait = 120000;
    const startWait = Date.now();
    let token = null;

    while (!token && Date.now() - startWait < maxWait) {
      await new Promise(r => setTimeout(r, 3000));
      const checkResp = await fetch('https://api.capsolver.com/getTaskResult', {
        ...fetchOpts,
        method: 'POST',
        body: JSON.stringify({ clientKey: this.capSolverApiKey, taskId: taskId2 }),
      });
      const checkResult = await checkResp.json();
      if (checkResult.status === 'ready') {
        token = checkResult.solution?.token || checkResult.solution?.gRecaptchaResponse || JSON.stringify(checkResult.solution);
      } else if (checkResult.errorId !== 0) {
        return { success: false, error: checkResult.errorDescription || 'get result failed' };
      }
    }

    if (!token) return { success: false, error: 'Timed out after 120s' };

    const durationMs = Date.now() - startTime;
    console.log(`[${taskId}] CapSolver solved in ${durationMs}ms`);
    return { success: true, token, provider: 'capsolver', durationMs, cost: 0.003 };
  }

  async findSitekey(page) {
    return page.evaluate(() => {
      const el = document.querySelector('[data-sitekey]');
      if (el) return el.getAttribute('data-sitekey');
      for (const s of document.querySelectorAll('script[src*="recaptcha"]')) {
        const m = s.src.match(/[?&]k=([^&]+)/);
        if (m) return m[1];
      }
      return null;
    });
  }

  async _extractGeeTestGt(page) {
    try {
      return await page.evaluate(() => {
        if (window.geetest?.gt) return window.geetest.gt;
        const el = document.querySelector('[data-gt]');
        if (el) return el.getAttribute('data-gt');
        const match = document.body.innerHTML.match(/"gt"\s*:\s*"([^"]+)"/);
        if (match) return match[1];
        return null;
      });
    } catch { return null; }
  }

  async _extractGeeTestChallenge(page, gt) {
    try {
      return await page.evaluate((g) => {
        if (window.geetest?.challenge) return window.geetest.challenge;
        const el = document.querySelector('[data-challenge]');
        if (el) return el.getAttribute('data-challenge');
        const match = document.body.innerHTML.match(/"challenge"\s*:\s*"([^"]+)"/);
        if (match) return match[1];
        return null;
      }, gt);
    } catch { return null; }
  }

  async _extractKasadaData(page) {
    try {
      return await page.evaluate(() => {
        const el = document.querySelector('[id^="kpsdk-"]');
        if (!el) return null;
        const kpsdk = el.dataset?.kpsdk || el.getAttribute('data-kpsdk') || '';
        const challenge = el.dataset?.kpsdkChallenge || el.getAttribute('data-kpsdk-challenge') || '';
        if (!kpsdk && !challenge) return null;
        return { kpsdk, challenge };
      });
    } catch { return null; }
  }
}
