export class CaptchaSolver {
  constructor(config = {}) {
    this.twoCaptchaApiKey = config.twoCaptchaApiKey || '';
    this.visualSolver = config.visualSolver || null;
    this.proxy = config.proxy || null;
  }

  async solve(page, detection, taskId, proxy = null) {
    const sitekey = detection.sitekey || await this.findSitekey(page);
    const url = page.url();

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
        const screenshot = await page.screenshot({ clip: { x: 0, y: 0, width: Math.min(page.viewportSize().width, 600), height: Math.min(page.viewportSize().height, 400) } });
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
        const { Agent } = await import('undici');
        const parsedProxy = new URL(proxy.server);
        const dispatcher = new Agent({
          keepAliveTimeout: 30000,
          keepAliveMaxTimeout: 120000,
          connect: {
            host: parsedProxy.hostname,
            port: parsedProxy.port || 8080,
          },
        });
        fetchOpts.dispatcher = dispatcher;
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
      if (checkResult.status === 'ready') token = checkResult.solution.gRecaptchaResponse;
      else if (checkResult.errorId !== 0) return { success: false, error: checkResult.errorDescription || 'get result failed' };
    }

    if (!token) return { success: false, error: 'Timed out after 120s' };

    const durationMs = Date.now() - startTime;
    console.log(`[${taskId}] 2captcha solved in ${durationMs}ms`);
    return { success: true, token, provider: '2captcha', durationMs, cost: 0.002 };
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
}
