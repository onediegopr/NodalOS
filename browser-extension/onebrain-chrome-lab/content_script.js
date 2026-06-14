(() => {
  const protocolVersion = 'chrome-lab-v1';
  let highlightNode = null;
  let stopped = false;

  chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
    if (!message || message.protocolVersion !== protocolVersion) {
      return false;
    }

    if (message.type === 'local.stop') {
      stopped = true;
      clearHighlight();
      sendResponse({ success: true, result: { stopped: true } });
      return true;
    }

    if (message.type !== 'tool.execute') {
      return false;
    }

    executeTool(message.tool, message.args || {})
      .then((result) => sendResponse({ success: true, result }))
      .catch((error) => sendResponse({ success: false, error: String(error && error.message ? error.message : error) }));
    return true;
  });

  async function executeTool(tool, args) {
    if (stopped && tool !== 'observePage' && tool !== 'clearHighlight') {
      throw new Error('Stopped');
    }

    if (isRestrictedLocation(window.location.href)) {
      throw new Error('Restricted page');
    }

    switch (tool) {
      case 'observePage':
        return observePage();
      case 'getCurrentTab':
        return getCurrentPage();
      case 'query':
        return query(args.selector);
      case 'read':
        return read(args.selector);
      case 'click':
        return click(args.selector, Boolean(args.explicitAllowSensitiveSubmit));
      case 'setValue':
        return setValue(args.selector, String(args.value || ''));
      case 'selectOption':
        return selectOption(args.selector, String(args.value || ''));
      case 'scrollIntoView':
        return scrollIntoView(args.selector);
      case 'waitForSelector':
        return waitForSelector(args.selector, Number(args.timeoutMs || 5000));
      case 'highlight':
        return highlight(args.selector);
      case 'clearHighlight':
        clearHighlight();
        return { cleared: true };
      default:
        throw new Error(`Tool not allowed: ${tool}`);
    }
  }

  function observePage() {
    const inputs = Array.from(document.querySelectorAll('input, textarea, select')).slice(0, 80).map(describeInput);
    const buttons = Array.from(document.querySelectorAll('button, input[type="button"], input[type="submit"], [role="button"]')).slice(0, 80).map(describeElement);
    const links = Array.from(document.querySelectorAll('a[href]')).slice(0, 80).map((link) => ({
      selector: selectorFor(link),
      text: safeText(link.innerText || link.textContent || ''),
      href: link.href
    }));
    const forms = Array.from(document.forms).slice(0, 20).map((form) => ({
      selector: selectorFor(form),
      inputCount: form.querySelectorAll('input, textarea, select').length,
      hasPassword: Boolean(form.querySelector('input[type="password"]')),
      hasCredentialInput: Boolean(form.querySelector('input[type="password"], input[name*="user" i], input[name*="cuit" i], input[name*="login" i], input[name*="clave" i], input[autocomplete*="username" i], input[autocomplete*="current-password" i]'))
    }));
    const text = safeText(document.body ? document.body.innerText || '' : '');
    const sensitiveHints = detectSensitiveHints(text, inputs);
    const hasCredentialForm = forms.some((form) => form.hasPassword || form.hasCredentialInput);
    const hasCredentialInput = inputs.some((input) => input.isPassword || input.isCredentialLike);
    const hasCredentialEntry = hasCredentialForm || hasCredentialInput;

    return {
      url: window.location.href,
      title: document.title,
      readyState: document.readyState,
      visibleTextSummary: text.slice(0, 1800),
      forms,
      inputs,
      buttons,
      links,
      hasPasswordField: inputs.some((input) => input.isPassword),
      hasCaptchaLike: sensitiveHints.includes('captcha'),
      hasTwoFactorLike: sensitiveHints.includes('twoFactor'),
      hasCredentialLike: hasCredentialEntry || sensitiveHints.includes('credential'),
      hasCredentialEntry,
      sensitiveHints,
      viewport: {
        width: window.innerWidth,
        height: window.innerHeight,
        scrollX: window.scrollX,
        scrollY: window.scrollY
      },
      timestamp: new Date().toISOString()
    };
  }

  function getCurrentPage() {
    return {
      url: window.location.href,
      title: document.title,
      readyState: document.readyState,
      timestamp: new Date().toISOString()
    };
  }

  function query(selector) {
    const element = requireElement(selector);
    return describeElement(element);
  }

  function read(selector) {
    const element = requireElement(selector);
    if (isPasswordLike(element)) {
      return {
        selector,
        value: '',
        redacted: true,
        reason: 'password value redacted'
      };
    }

    return {
      selector,
      text: safeText(element.innerText || element.textContent || ''),
      value: isReadableValueElement(element) ? String(element.value || '') : '',
      tagName: element.tagName.toLowerCase()
    };
  }

  function click(selector, explicitAllowSensitiveSubmit) {
    const element = requireElement(selector);
    if (isSensitiveSubmit(element) && !explicitAllowSensitiveSubmit) {
      throw new Error('Sensitive submit blocked');
    }
    element.scrollIntoView({ block: 'center', inline: 'center', behavior: 'smooth' });
    temporaryHighlight(element);
    element.click();
    return { clicked: true, selector };
  }

  function setValue(selector, value) {
    const element = requireElement(selector);
    if (isPasswordLike(element) || isOtpLike(element) || pageHasCaptchaOrTwoFactor()) {
      throw new Error('Credential or token field blocked');
    }
    if (!('value' in element)) {
      throw new Error('Target does not expose value');
    }
    element.focus();
    element.value = value;
    element.dispatchEvent(new Event('input', { bubbles: true }));
    element.dispatchEvent(new Event('change', { bubbles: true }));
    return { selector, valueAfter: String(element.value || '') };
  }

  function selectOption(selector, value) {
    const element = requireElement(selector);
    if (!(element instanceof HTMLSelectElement)) {
      throw new Error('Target is not a select');
    }
    element.value = value;
    element.dispatchEvent(new Event('input', { bubbles: true }));
    element.dispatchEvent(new Event('change', { bubbles: true }));
    return { selector, valueAfter: element.value };
  }

  function scrollIntoView(selector) {
    const element = requireElement(selector);
    element.scrollIntoView({ block: 'center', inline: 'center', behavior: 'smooth' });
    return { scrolled: true, selector };
  }

  function waitForSelector(selector, timeoutMs) {
    return new Promise((resolve, reject) => {
      const existing = document.querySelector(selector);
      if (existing) {
        resolve({ found: true, selector });
        return;
      }

      const started = Date.now();
      const timer = window.setInterval(() => {
        const element = document.querySelector(selector);
        if (element) {
          window.clearInterval(timer);
          resolve({ found: true, selector });
          return;
        }
        if (Date.now() - started >= timeoutMs) {
          window.clearInterval(timer);
          reject(new Error('waitForSelector timeout'));
        }
      }, 150);
    });
  }

  function highlight(selector) {
    const element = requireElement(selector);
    temporaryHighlight(element, 0);
    return { highlighted: true, selector };
  }

  function clearHighlight() {
    if (highlightNode && highlightNode.parentNode) {
      highlightNode.parentNode.removeChild(highlightNode);
    }
    highlightNode = null;
  }

  function temporaryHighlight(element, timeoutMs = 1200) {
    clearHighlight();
    const rect = element.getBoundingClientRect();
    const node = document.createElement('div');
    node.style.position = 'fixed';
    node.style.left = `${rect.left}px`;
    node.style.top = `${rect.top}px`;
    node.style.width = `${rect.width}px`;
    node.style.height = `${rect.height}px`;
    node.style.border = '3px solid #1dd3b0';
    node.style.boxShadow = '0 0 0 4px rgba(29, 211, 176, 0.18)';
    node.style.zIndex = '2147483647';
    node.style.pointerEvents = 'none';
    node.style.borderRadius = '8px';
    document.documentElement.appendChild(node);
    highlightNode = node;
    if (timeoutMs > 0) {
      window.setTimeout(clearHighlight, timeoutMs);
    }
  }

  function describeInput(element) {
    const password = isPasswordLike(element);
    return {
      selector: selectorFor(element),
      tagName: element.tagName.toLowerCase(),
      type: String(element.getAttribute('type') || '').toLowerCase(),
      name: element.getAttribute('name') || '',
      id: element.id || '',
      label: labelFor(element),
      placeholder: element.getAttribute('placeholder') || '',
      value: password || isOtpLike(element) ? '' : String(element.value || '').slice(0, 200),
      redacted: password || isOtpLike(element),
      isPassword: password,
      isCredentialLike: password || isOtpLike(element) || credentialLikeText(attributeText(element))
    };
  }

  function describeElement(element) {
    return {
      selector: selectorFor(element),
      tagName: element.tagName.toLowerCase(),
      role: element.getAttribute('role') || '',
      type: element.getAttribute('type') || '',
      id: element.id || '',
      name: element.getAttribute('name') || '',
      text: safeText(element.innerText || element.textContent || element.getAttribute('aria-label') || '').slice(0, 300),
      visible: isVisible(element)
    };
  }

  function detectSensitiveHints(text, inputs) {
    const hints = new Set();
    const normalized = normalize(text);
    if (normalized.includes('captcha') || normalized.includes('recaptcha') || normalized.includes('hcaptcha') || normalized.includes('cf-turnstile') || normalized.includes('no soy un robot')) {
      hints.add('captcha');
    }
    if (normalized.includes('2fa') || normalized.includes('two factor') || normalized.includes('otp') || normalized.includes('token') || normalized.includes('verificacion') || normalized.includes('codigo')) {
      hints.add('twoFactor');
    }
    if (normalized.includes('password') || normalized.includes('contrasena') || normalized.includes('clave fiscal') || normalized.includes('usuario') || normalized.includes('cuit') || normalized.includes('login') || normalized.includes('iniciar sesion')) {
      hints.add('credential');
    }
    if (inputs.some((input) => input.isPassword || input.isCredentialLike)) {
      hints.add('credential');
    }
    return Array.from(hints);
  }

  function requireElement(selector) {
    if (!selector || typeof selector !== 'string') {
      throw new Error('selector required');
    }
    const element = document.querySelector(selector);
    if (!element) {
      throw new Error(`selector not found: ${selector}`);
    }
    return element;
  }

  function selectorFor(element) {
    if (element.id) {
      return `#${cssEscape(element.id)}`;
    }
    const name = element.getAttribute('name');
    if (name) {
      return `${element.tagName.toLowerCase()}[name="${cssEscape(name)}"]`;
    }
    const aria = element.getAttribute('aria-label');
    if (aria) {
      return `${element.tagName.toLowerCase()}[aria-label="${cssEscape(aria)}"]`;
    }
    return element.tagName.toLowerCase();
  }

  function cssEscape(value) {
    if (window.CSS && typeof window.CSS.escape === 'function') {
      return window.CSS.escape(value);
    }
    return String(value).replace(/["\\]/g, '\\$&');
  }

  function isPasswordLike(element) {
    const text = attributeText(element);
    return String(element.getAttribute('type') || '').toLowerCase() === 'password' ||
      String(element.getAttribute('autocomplete') || '').toLowerCase().includes('password') ||
      normalize(text).includes('password') ||
      normalize(text).includes('contrasena') ||
      normalize(text).includes('clave');
  }

  function isOtpLike(element) {
    const text = normalize(attributeText(element));
    return text.includes('one-time-code') || text.includes('otp') || text.includes('token') || text.includes('codigo') || text.includes('2fa');
  }

  function isSensitiveSubmit(element) {
    const text = normalize(attributeText(element) + ' ' + (element.innerText || element.textContent || ''));
    return text.includes('login') || text.includes('iniciar sesion') || text.includes('clave') || text.includes('password') || text.includes('pagar') || text.includes('submit');
  }

  function pageHasCaptchaOrTwoFactor() {
    const hints = detectSensitiveHints(document.body ? document.body.innerText || '' : '', []);
    return hints.includes('captcha') || hints.includes('twoFactor');
  }

  function attributeText(element) {
    return [
      element.id,
      element.getAttribute('name'),
      element.getAttribute('type'),
      element.getAttribute('autocomplete'),
      element.getAttribute('aria-label'),
      element.getAttribute('placeholder'),
      labelFor(element)
    ].filter(Boolean).join(' ');
  }

  function labelFor(element) {
    if (element.id) {
      const label = document.querySelector(`label[for="${cssEscape(element.id)}"]`);
      if (label) {
        return safeText(label.innerText || label.textContent || '');
      }
    }
    const parentLabel = element.closest('label');
    return parentLabel ? safeText(parentLabel.innerText || parentLabel.textContent || '') : '';
  }

  function credentialLikeText(value) {
    const text = normalize(value);
    return text.includes('password') || text.includes('contrasena') || text.includes('clave') || text.includes('token') || text.includes('otp') || text.includes('captcha') || text.includes('cuit');
  }

  function isReadableValueElement(element) {
    return element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement || element instanceof HTMLSelectElement;
  }

  function isVisible(element) {
    const rect = element.getBoundingClientRect();
    const style = window.getComputedStyle(element);
    return rect.width > 0 && rect.height > 0 && style.visibility !== 'hidden' && style.display !== 'none';
  }

  function safeText(value) {
    return String(value || '').replace(/\s+/g, ' ').trim();
  }

  function normalize(value) {
    return safeText(value).toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
  }

  function isRestrictedLocation(url) {
    return /^(chrome|edge|extension):\/\//i.test(url);
  }
})();
