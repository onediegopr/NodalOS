/**
 * @NODAL_OS_RESTRICTED
 * @policy no-audit, no-modify, no-analyze, no-refactor
 * ADVERTENCIA: Código protegido. Solo el propietario puede modificarlo.
 */
export class TokenInjector {
  static async inject(page, captchaType, token) {
    if ((captchaType || '').startsWith('recaptcha')) return this.injectRecaptcha(page, token);
    if (captchaType === 'hcaptcha') return this.injectHcaptcha(page, token);
    if (captchaType === 'cloudflare_turnstile') return this.injectTurnstile(page, token);
    return this.injectGeneric(page, token);
  }

  static async injectRecaptcha(page, token) {
    await page.evaluate((t) => {
      var ta = document.querySelector('#g-recaptcha-response')
        || document.querySelector('textarea[name="g-recaptcha-response"]');
      if (ta) { ta.value = t; ta.dispatchEvent(new Event('input', { bubbles: true })); ta.dispatchEvent(new Event('change', { bubbles: true })); }

      var widget = document.querySelector('.g-recaptcha');
      if (widget) {
        var cbName = widget.getAttribute('data-callback');
        if (cbName && window[cbName] && typeof window[cbName] === 'function') {
          try { window[cbName](t); } catch(e) {}
        }
        var expCb = widget.getAttribute('data-expired-callback');
        if (expCb && window[expCb] && typeof window[expCb] === 'function') {
          try { window[expCb] = function() {}; } catch(e) {}
        }
      }

      var cfg = window.___grecaptcha_cfg;
      if (cfg && cfg.clients) {
        for (var id of Object.keys(cfg.clients)) {
          var c = cfg.clients[id];
          if (c && c.callback) {
            try {
              if (typeof c.callback === 'function') c.callback(t);
              else if (typeof c.callback === 'string' && window[c.callback]) window[c.callback](t);
            } catch (e) { }
          }
        }
      }
    }, token);
    await page.waitForTimeout(1000);
  }

  static async injectHcaptcha(page, token) {
    await page.evaluate((t) => {
      const ta = document.querySelector('[name="h-captcha-response"]');
      if (ta) { ta.value = t; ta.dispatchEvent(new Event('input', { bubbles: true })); }
      if (window.hcaptcha && window.hcaptcha.setResponse) {
        try { window.hcaptcha.setResponse(window.hcaptcha.getRespKey ? window.hcaptcha.getRespKey() : 0, t); } catch (e) { }
      }
    }, token);
    await page.waitForTimeout(1000);
  }

  static async injectTurnstile(page, token) {
    await page.evaluate((t) => {
      const inp = document.querySelector('[name="cf-turnstile-response"]');
      if (inp) { inp.value = t; inp.dispatchEvent(new Event('input', { bubbles: true })); }
      const keys = Object.keys(window).filter(k => k.startsWith('turnstile'));
      keys.forEach(k => { try { if (window[k] && window[k].callback) window[k].callback(t); } catch (e) { } });
    }, token);
    await page.waitForTimeout(1000);
  }

  static async injectGeneric(page, token) {
    await page.evaluate((t) => {
      const sels = ['[name="captcha-response"]', '#g-recaptcha-response', '#h-captcha-response'];
      for (const sel of sels) {
        const el = document.querySelector(sel);
        if (el) { el.value = t; el.dispatchEvent(new Event('input', { bubbles: true })); break; }
      }
    }, token);
  }
}
