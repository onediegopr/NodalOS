/**
 * @NODAL_OS_RESTRICTED
 * @policy no-audit, no-modify, no-analyze, no-refactor
 * ADVERTENCIA: Código protegido. Solo el propietario puede modificarlo.
 */
export class CaptchaDetector {
  static async detect(page) {
    const results = await Promise.all([
      this.detectByDom(page),
      this.detectByFrames(page),
      this.detectByText(page),
    ]);
    return results.find(r => r !== null) || null;
  }

  static async detectByDom(page) {
    const map = {
      'recaptcha_v2': ['.g-recaptcha', 'iframe[src*="recaptcha/api2/anchor"]', 'div[data-sitekey]'],
      'recaptcha_v3': ['.grecaptcha-badge', 'script[src*="recaptcha/api.js?render="]'],
      'hcaptcha': ['.h-captcha', 'iframe[src*="hcaptcha.com/captcha"]', 'div[data-hcaptcha-widget-id]'],
      'cloudflare_turnstile': ['.cf-turnstile', 'iframe[src*="challenges.cloudflare.com"]', '#challenge-stage'],
      'datadome': ['iframe[src*="datadome.co"]', '#datadome-captcha'],
    };
    for (const [type, sels] of Object.entries(map)) {
      for (const sel of sels) {
        try {
          const el = await page.$(sel);
          if (el) {
            const sitekey = await this.extractSitekey(page, type);
            return { type, sitekey, selector: sel, detectedBy: 'dom', frameId: 'main' };
          }
        } catch (e) { /* selector invalido */ }
      }
    }
    return null;
  }

  static async detectByFrames(page) {
    for (const frame of page.frames()) {
      const url = frame.url();
      if (url.includes('recaptcha/api2/anchor'))
        return { type: 'recaptcha_v2', sitekey: this.extractFromUrl(url), detectedBy: 'frame', frameId: 'captcha-frame' };
      if (url.includes('hcaptcha.com/captcha'))
        return { type: 'hcaptcha', sitekey: this.extractFromUrl(url), detectedBy: 'frame', frameId: 'hcaptcha-frame' };
      if (url.includes('challenges.cloudflare.com'))
        return { type: 'cloudflare_turnstile', sitekey: this.extractFromUrl(url), detectedBy: 'frame', frameId: 'turnstile-frame' };
    }
    return null;
  }

  static async detectByText(page) {
    try {
      const txt = (await page.evaluate(() => document.body ? document.body.innerText : '')).toLowerCase();
      if (txt.includes("i'm not a robot") || txt.includes('recaptcha'))
        return { type: 'recaptcha_v2', sitekey: null, detectedBy: 'text', frameId: 'main' };
      if (txt.includes('hcaptcha') || txt.includes('i am human'))
        return { type: 'hcaptcha', sitekey: null, detectedBy: 'text', frameId: 'main' };
      if ((txt.includes('verify you are human') || txt.includes('checking your browser')) && txt.includes('cloudflare'))
        return { type: 'cloudflare_turnstile', sitekey: null, detectedBy: 'text', frameId: 'main' };
      if (txt.includes('datadome') || txt.includes('are you a bot'))
        return { type: 'datadome', sitekey: null, detectedBy: 'text', frameId: 'main' };
    } catch (e) { }
    return null;
  }

  static async extractSitekey(page) {
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

  static extractFromUrl(url) {
    try { const u = new URL(url); return u.searchParams.get('k') || u.searchParams.get('sitekey') || null; }
    catch { return null; }
  }

  static isAutoSolvable(type) {
    return ['recaptcha_v2', 'hcaptcha', 'cloudflare_turnstile'].some(t => (type || '').startsWith(t));
  }

  static recommendSolver(type) {
    if ((type || '').startsWith('recaptcha')) return '2captcha';
    if (type === 'hcaptcha') return '2captcha';
    if (type === 'cloudflare_turnstile') return 'capsolver';
    return 'none';
  }

  static mapToFrictionKind(type) {
    if ((type || '').startsWith('recaptcha') || type === 'hcaptcha' || type === 'cloudflare_turnstile')
      return 'CaptchaDetected';
    if (type === 'datadome') return 'BotBlockDetected';
    return 'UnknownFriction';
  }
}
