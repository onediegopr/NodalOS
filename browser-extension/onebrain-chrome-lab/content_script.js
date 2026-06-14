(() => {
  const protocolVersion = 'chrome-lab-v1';
  const maxCatalogElements = 250;
  const maxVisibleTextLength = 2400;
  let stopped = false;
  let nextElementId = 1;
  let highlightNode = null;
  const nodeToElementId = new WeakMap();
  const elementIdToNode = new Map();

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
      .catch((error) => sendResponse({
        success: false,
        error: String(error && error.message ? error.message : error)
      }));
    return true;
  });

  async function executeTool(tool, args) {
    if (stopped && tool !== 'observePage' && tool !== 'getElementCatalog' && tool !== 'clearHighlight') {
      throw new Error('Stopped');
    }

    if (isRestrictedLocation(window.location.href)) {
      throw new Error('Restricted page');
    }

    switch (tool) {
      case 'observePage':
        return observePage();
      case 'getElementCatalog':
        return getElementCatalog(args);
      case 'resolveTarget':
        return resolveTarget(args);
      case 'getCurrentTab':
        return getCurrentPage();
      case 'query':
        return query(args);
      case 'read':
        return read(args);
      case 'readElement':
        return readElement(args);
      case 'click':
        return click(args);
      case 'clickElement':
        return clickElement(args);
      case 'setValue':
        return setValue(args, String(args.value || ''));
      case 'setElementValue':
        return setElementValue(args, String(args.value || ''));
      case 'focusElement':
        return focusElement(args);
      case 'selectOption':
        return selectOption(args, String(args.value || ''));
      case 'scrollIntoView':
        return scrollElementIntoView(args);
      case 'scrollElementIntoView':
        return scrollElementIntoView(args);
      case 'waitForSelector':
        return waitForSelector(String(args.selector || ''), Number(args.timeoutMs || 5000));
      case 'highlight':
        return highlightElement(args);
      case 'highlightElement':
        return highlightElement(args);
      case 'clearHighlight':
        clearHighlight();
        return { cleared: true };
      default:
        throw new Error(`Tool not allowed: ${tool}`);
    }
  }

  function observePage() {
    const observation = buildObservation({ includeElements: true, limit: 120 });
    return observation;
  }

  function getElementCatalog(args) {
    const offset = clamp(Number(args.offset || 0), 0, 1000);
    const limit = clamp(Number(args.limit || 50), 1, 150);
    const requestedKinds = Array.isArray(args.elementKinds)
      ? args.elementKinds.map((item) => String(item).toLowerCase())
      : [];
    const requestedRisks = Array.isArray(args.riskFlags)
      ? args.riskFlags.map((item) => String(item).toLowerCase())
      : [];

    let catalog = buildInteractiveCatalog();
    if (requestedKinds.length > 0) {
      catalog = catalog.filter((element) =>
        requestedKinds.includes(element.elementKind) ||
        requestedKinds.includes(element.tagName) ||
        requestedKinds.includes(element.inputKind));
    }

    if (requestedRisks.length > 0) {
      catalog = catalog.filter((element) =>
        element.riskFlags.some((risk) => requestedRisks.includes(String(risk).toLowerCase())));
    }

    const paged = catalog.slice(offset, offset + limit);
    return {
      totalElements: catalog.length,
      offset,
      limit,
      returned: paged.length,
      elements: paged
    };
  }

  function resolveTarget(args) {
    const request = {
      intent: String(args.intent || 'click').toLowerCase(),
      targetText: safeText(args.targetText || ''),
      context: safeText(args.context || ''),
      elementKinds: Array.isArray(args.elementKinds)
        ? args.elementKinds.map((item) => String(item).toLowerCase())
        : [],
      maxCandidates: clamp(Number(args.maxCandidates || 5), 1, 10)
    };

    if (!request.targetText) {
      throw new Error('targetText required');
    }

    const catalog = buildInteractiveCatalog();
    const scored = catalog
      .map((element) => scoreTargetCandidate(element, request, catalog))
      .filter((candidate) => candidate.score > 0.08)
      .sort((left, right) => right.score - left.score)
      .slice(0, request.maxCandidates);

    applyAmbiguityPenalty(scored);
    scored.sort((left, right) => right.score - left.score);

    const candidates = scored.map((candidate) => buildResolveCandidate(candidate));
    const bestCandidate = candidates.length > 0 ? candidates[0] : null;

    return {
      ok: true,
      intent: request.intent,
      targetText: request.targetText,
      context: request.context,
      candidateCount: candidates.length,
      bestCandidate,
      candidates
    };
  }

  function getCurrentPage() {
    return {
      url: window.location.href,
      title: document.title,
      readyState: document.readyState,
      focusedElement: compactElementDescriptor(document.activeElement),
      timestamp: new Date().toISOString()
    };
  }

  function query(args) {
    const resolved = resolveLiveElement(args);
    return describeInteractiveElement(resolved.element);
  }

  function read(args) {
    return readElement(args);
  }

  function readElement(args) {
    const resolved = resolveLiveElement(args);
    const descriptor = describeInteractiveElement(resolved.element);
    if (descriptor.isPassword) {
      return {
        success: true,
        elementId: descriptor.elementId,
        redacted: true,
        reason: 'password value redacted',
        element: descriptor
      };
    }

    return {
      success: true,
      elementId: descriptor.elementId,
      text: descriptor.visibleText,
      value: isReadableValueElement(resolved.element) ? redactValueIfSensitive(String(resolved.element.value || ''), descriptor) : '',
      redacted: false,
      rehydrated: resolved.rehydrated,
      element: descriptor
    };
  }

  function click(args) {
    return clickElement(args);
  }

  async function clickElement(args) {
    const resolved = resolveLiveElement(args);
    const descriptor = describeInteractiveElement(resolved.element);
    if (!descriptor.isVisible) {
      throw new Error('Target not visible');
    }
    if (!descriptor.isEnabled) {
      throw new Error('Target disabled');
    }
    if (isSensitiveSubmit(resolved.element) && !Boolean(args.explicitAllowSensitiveSubmit)) {
      throw new Error('Sensitive submit blocked');
    }

    const before = capturePageSnapshot();
    const verificationRequest = normalizeVerificationRequest(args.verify);
    resolved.element.scrollIntoView({ block: 'center', inline: 'center', behavior: 'instant' });
    if (args.highlight !== false) {
      temporaryHighlight(resolved.element, 0);
    }
    tryFocus(resolved.element);
    resolved.element.click();
    const verification = await verifyAfterAction(before, verificationRequest);

    return {
      success: true,
      action: 'clickElement',
      clicked: true,
      elementId: descriptor.elementId,
      rehydrated: resolved.rehydrated,
      element: descriptor,
      bestSelector: selectBestStableSelector(descriptor.stableSelectors),
      stableSelectors: descriptor.stableSelectors,
      beforeUrl: before.url,
      afterUrl: verification.afterUrl,
      beforeTitle: before.title,
      afterTitle: verification.afterTitle,
      urlChanged: verification.urlChanged,
      titleChanged: verification.titleChanged,
      domChanged: verification.domChanged,
      expectedConditionMet: verification.expectedConditionMet,
      verificationStatus: verification.verificationStatus,
      reason: verification.reason
    };
  }

  function setValue(args, value) {
    return setElementValue(args, value);
  }

  function setElementValue(args, value) {
    const resolved = resolveLiveElement(args);
    const descriptor = describeInteractiveElement(resolved.element);
    if (descriptor.isPassword || descriptor.isCredentialLike || pageHasCaptchaOrTwoFactor()) {
      throw new Error('Credential or token field blocked');
    }
    if (!('value' in resolved.element)) {
      throw new Error('Target does not expose value');
    }

    const before = capturePageSnapshot();
    const valueBefore = String(resolved.element.value || '');
    tryFocus(resolved.element);
    resolved.element.value = value;
    resolved.element.dispatchEvent(new Event('input', { bubbles: true }));
    resolved.element.dispatchEvent(new Event('change', { bubbles: true }));
    const valueAfter = String(resolved.element.value || '');
    const after = capturePageSnapshot();

    return {
      success: valueAfter === value,
      action: 'setElementValue',
      elementId: descriptor.elementId,
      rehydrated: resolved.rehydrated,
      element: descriptor,
      valueBefore: redactValueIfSensitive(valueBefore, descriptor),
      valueAfter: redactValueIfSensitive(valueAfter, descriptor),
      verificationStatus: valueAfter === value ? 'passed' : 'failed',
      reason: valueAfter === value ? 'Value applied and read back.' : 'Readback mismatch after setElementValue.',
      beforeUrl: before.url,
      afterUrl: after.url,
      beforeTitle: before.title,
      afterTitle: after.title,
      urlChanged: before.url !== after.url,
      titleChanged: before.title !== after.title,
      domChanged: before.domSignature !== after.domSignature
    };
  }

  function focusElement(args) {
    const resolved = resolveLiveElement(args);
    const descriptor = describeInteractiveElement(resolved.element);
    resolved.element.scrollIntoView({ block: 'center', inline: 'center', behavior: 'instant' });
    tryFocus(resolved.element);
    return {
      success: true,
      action: 'focusElement',
      elementId: descriptor.elementId,
      rehydrated: resolved.rehydrated,
      element: descriptor
    };
  }

  function selectOption(args, value) {
    const resolved = resolveLiveElement(args);
    if (!(resolved.element instanceof HTMLSelectElement)) {
      throw new Error('Target is not a select');
    }

    const descriptor = describeInteractiveElement(resolved.element);
    const valueBefore = String(resolved.element.value || '');
    resolved.element.value = value;
    resolved.element.dispatchEvent(new Event('input', { bubbles: true }));
    resolved.element.dispatchEvent(new Event('change', { bubbles: true }));

    return {
      success: true,
      action: 'selectOption',
      elementId: descriptor.elementId,
      rehydrated: resolved.rehydrated,
      element: descriptor,
      valueBefore,
      valueAfter: String(resolved.element.value || '')
    };
  }

  function scrollElementIntoView(args) {
    const resolved = resolveLiveElement(args);
    resolved.element.scrollIntoView({ block: 'center', inline: 'center', behavior: 'instant' });
    const descriptor = describeInteractiveElement(resolved.element);
    return {
      success: true,
      action: 'scrollElementIntoView',
      elementId: descriptor.elementId,
      rehydrated: resolved.rehydrated,
      element: descriptor
    };
  }

  function highlightElement(args) {
    const resolved = resolveLiveElement(args);
    temporaryHighlight(resolved.element, 0);
    const descriptor = describeInteractiveElement(resolved.element);
    return {
      success: true,
      action: 'highlightElement',
      elementId: descriptor.elementId,
      rehydrated: resolved.rehydrated,
      element: descriptor
    };
  }

  function waitForSelector(selector, timeoutMs) {
    return new Promise((resolve, reject) => {
      if (!selector) {
        reject(new Error('selector required'));
        return;
      }

      const existing = document.querySelector(selector);
      if (existing) {
        resolve({ found: true, selector, elementId: getOrCreateElementId(existing) });
        return;
      }

      const started = Date.now();
      const timer = window.setInterval(() => {
        const element = document.querySelector(selector);
        if (element) {
          window.clearInterval(timer);
          resolve({ found: true, selector, elementId: getOrCreateElementId(element) });
          return;
        }

        if (Date.now() - started >= timeoutMs) {
          window.clearInterval(timer);
          reject(new Error('waitForSelector timeout'));
        }
      }, 150);
    });
  }

  function buildObservation(options) {
    const catalog = buildInteractiveCatalog();
    const limit = clamp(Number(options.limit || 120), 1, 150);
    const elements = catalog.slice(0, limit);
    const inputs = elements.filter((item) => ['input', 'textarea', 'select', 'contenteditable'].includes(item.inputKind));
    const buttons = elements.filter((item) => item.elementKind === 'button');
    const links = elements.filter((item) => item.elementKind === 'link');
    const forms = summarizeForms();
    const visibleText = safeText(document.body ? document.body.innerText || '' : '').slice(0, maxVisibleTextLength);
    const sensitiveHints = detectSensitiveHints(visibleText, inputs);
    const hasCredentialForm = forms.some((form) => form.hasPassword || form.hasCredentialInput);
    const hasCredentialInput = inputs.some((input) => input.isPassword || input.isCredentialLike);
    const focused = compactElementDescriptor(document.activeElement);
    const topInteractiveElements = elements.slice(0, 20).map(compactElementDescriptor);

    return {
      url: window.location.href,
      title: document.title,
      readyState: document.readyState,
      visibleTextSummary: visibleText,
      elementCatalogSummary: {
        totalElements: catalog.length,
        visibleElements: catalog.filter((item) => item.isVisible).length,
        enabledElements: catalog.filter((item) => item.isEnabled).length,
        clickableElements: catalog.filter((item) => item.elementKind === 'button' || item.elementKind === 'link').length,
        credentialLikeElements: catalog.filter((item) => item.isCredentialLike).length,
        passwordElements: catalog.filter((item) => item.isPassword).length
      },
      topInteractiveElements,
      forms,
      inputs,
      buttons,
      links,
      elements: options.includeElements ? elements : undefined,
      focusedElement: focused,
      hasPasswordField: catalog.some((item) => item.isPassword),
      hasCaptchaLike: sensitiveHints.includes('captcha'),
      hasTwoFactorLike: sensitiveHints.includes('twoFactor'),
      hasCredentialLike: hasCredentialForm || hasCredentialInput || sensitiveHints.includes('credential'),
      hasCredentialEntry: hasCredentialForm || hasCredentialInput,
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

  function summarizeForms() {
    return Array.from(document.forms).slice(0, 40).map((form) => ({
      selector: bestSelector(form),
      inputCount: form.querySelectorAll('input, textarea, select').length,
      hasPassword: Boolean(form.querySelector('input[type="password"]')),
      hasCredentialInput: Boolean(form.querySelector('input[type="password"], input[name*="user" i], input[name*="cuit" i], input[name*="login" i], input[name*="clave" i], input[autocomplete*="username" i], input[autocomplete*="current-password" i]')),
      formContext: safeText(form.innerText || form.textContent || '').slice(0, 160)
    }));
  }

  function buildInteractiveCatalog() {
    const candidates = new Set();
    const selectors = [
      'button',
      'a[href]',
      'input',
      'textarea',
      'select',
      'option',
      '[role="button"]',
      '[role="link"]',
      '[role="menuitem"]',
      '[role="tab"]',
      '[role="checkbox"]',
      '[role="radio"]',
      '[role="switch"]',
      '[tabindex]:not([tabindex="-1"])',
      '[onclick]',
      '[contenteditable="true"]',
      '[contenteditable=""]',
      'input[type="submit"]',
      'input[type="reset"]',
      'input[type="button"]',
      'label[for]'
    ];

    for (const selector of selectors) {
      for (const element of document.querySelectorAll(selector)) {
        candidates.add(element);
      }
    }

    collectVisualClickableCandidates(candidates);

    return Array.from(candidates)
      .map((element) => describeInteractiveElement(element))
      .filter((item) => item.isVisible || item.elementKind === 'option')
      .sort(compareCatalogElements)
      .slice(0, maxCatalogElements);
  }

  function collectVisualClickableCandidates(candidates) {
    const all = document.body ? Array.from(document.body.querySelectorAll('*')).slice(0, 2000) : [];
    for (const element of all) {
      if (candidates.has(element)) {
        continue;
      }
      if (isSemanticInteractive(element)) {
        continue;
      }
      if (!isVisible(element)) {
        continue;
      }

      const style = window.getComputedStyle(element);
      const hasPointerCursor = style && style.cursor === 'pointer';
      const hasText = safeText(element.innerText || element.textContent || '').length > 0;
      if (hasPointerCursor && hasText) {
        candidates.add(element);
      }
    }
  }

  function describeInteractiveElement(element) {
    const tagName = element.tagName.toLowerCase();
    const role = inferRole(element);
    const descriptor = {
      elementId: getOrCreateElementId(element),
      tagName,
      role,
      elementKind: inferElementKind(element),
      type: String(element.getAttribute('type') || '').toLowerCase(),
      visibleText: safeText(element.innerText || element.textContent || ''),
      accessibleName: accessibleNameFor(element),
      ariaLabel: element.getAttribute('aria-label') || '',
      title: element.getAttribute('title') || '',
      placeholder: element.getAttribute('placeholder') || '',
      name: element.getAttribute('name') || '',
      id: element.id || '',
      href: element instanceof HTMLAnchorElement ? element.href : '',
      value: '',
      inputKind: inferInputKind(element),
      isVisible: isVisible(element),
      isEnabled: isEnabled(element),
      isPassword: isPasswordLike(element),
      isCredentialLike: isCredentialLikeElement(element),
      formContext: formContextFor(element),
      nearbyText: nearbyTextFor(element),
      bounds: boundsFor(element),
      viewportPosition: viewportPositionFor(element),
      stableSelectors: [],
      riskFlags: [],
      scoreHints: []
    };

    descriptor.value = describeValue(element, descriptor);
    descriptor.riskFlags = buildRiskFlags(descriptor);
    descriptor.scoreHints = buildScoreHints(descriptor);
    descriptor.stableSelectors = buildStableSelectors(element, descriptor);
    return descriptor;
  }

  function buildStableSelectors(element, descriptor) {
    const selectors = [];
    addSelector(selectors, uniqueIdSelector(element), 'id', 1.0, 'unique id');
    addSelector(selectors, uniqueDataSelector(element, 'data-testid'), 'data-testid', 0.98, 'unique data-testid');
    addSelector(selectors, uniqueDataSelector(element, 'data-test'), 'data-test', 0.97, 'unique data-test');
    addSelector(selectors, uniqueDataSelector(element, 'data-cy'), 'data-cy', 0.97, 'unique data-cy');
    addSelector(selectors, uniqueDataSelector(element, 'data-automation-id'), 'automation-id', 0.96, 'unique automation id');
    addSelector(selectors, ariaLabelSelector(element), 'aria-label', 0.93, 'aria-label');
    addSelector(selectors, nameSelector(element), 'name', 0.9, 'tag + name');
    addSelector(selectors, xpathByRoleAndName(element, descriptor), 'role-accessible-name', 0.88, 'role + accessible name');
    addSelector(selectors, hrefSelector(element), 'href', 0.84, 'href');
    addSelector(selectors, formInputSelector(element), 'form-input', 0.82, 'form + input');
    addSelector(selectors, cssPathSelector(element), 'css-path', 0.62, 'css path');
    addSelector(selectors, xpathSelector(element), 'xpath', 0.56, 'xpath fallback');
    addSelector(selectors, nthChildSelector(element), 'nth-child', 0.32, 'nth-child fallback');
    return selectors;
  }

  function addSelector(selectors, selector, type, confidence, reason) {
    if (!selector || selectors.some((item) => item.selector === selector)) {
      return;
    }

    selectors.push({ selector, type, confidence, reason });
  }

  function uniqueIdSelector(element) {
    if (!element.id) {
      return '';
    }
    const selector = `#${escapeIdentifier(element.id)}`;
    return isUniqueSelector(selector) ? selector : '';
  }

  function uniqueDataSelector(element, attributeName) {
    const value = element.getAttribute(attributeName);
    if (!value) {
      return '';
    }
    const selector = `${element.tagName.toLowerCase()}[${attributeName}="${escapeAttributeValue(value)}"]`;
    return isUniqueSelector(selector) ? selector : '';
  }

  function ariaLabelSelector(element) {
    const value = element.getAttribute('aria-label');
    if (!value) {
      return '';
    }
    const selector = `${element.tagName.toLowerCase()}[aria-label="${escapeAttributeValue(value)}"]`;
    return isUniqueSelector(selector) ? selector : '';
  }

  function nameSelector(element) {
    const value = element.getAttribute('name');
    if (!value) {
      return '';
    }
    const selector = `${element.tagName.toLowerCase()}[name="${escapeAttributeValue(value)}"]`;
    return isUniqueSelector(selector) ? selector : '';
  }

  function xpathByRoleAndName(element, descriptor) {
    const role = descriptor.role || '';
    const accessibleName = descriptor.accessibleName || descriptor.visibleText || '';
    if (!role || !accessibleName || accessibleName.length > 80) {
      return '';
    }

    const value = normalizeSpaceForXpath(accessibleName.slice(0, 80));
    return `//*[@role="${escapeXpathLiteral(role)}" and contains(normalize-space(.), ${escapeXpathLiteral(value)})]`;
  }

  function hrefSelector(element) {
    if (!(element instanceof HTMLAnchorElement)) {
      return '';
    }

    const href = element.getAttribute('href');
    if (!href || href.length > 240) {
      return '';
    }

    const selector = `a[href="${escapeAttributeValue(href)}"]`;
    return isUniqueSelector(selector) ? selector : '';
  }

  function formInputSelector(element) {
    if (!(element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement || element instanceof HTMLSelectElement)) {
      return '';
    }

    const form = element.form;
    const name = element.getAttribute('name');
    if (!form || !name) {
      return '';
    }

    const formSelector = form.id ? `#${escapeIdentifier(form.id)}` : '';
    if (!formSelector) {
      return '';
    }

    const selector = `${formSelector} ${element.tagName.toLowerCase()}[name="${escapeAttributeValue(name)}"]`;
    return isUniqueSelector(selector) ? selector : '';
  }

  function cssPathSelector(element) {
    const segments = [];
    let current = element;
    while (current && current.nodeType === Node.ELEMENT_NODE && current !== document.body && segments.length < 6) {
      const part = cssSegment(current);
      if (!part) {
        break;
      }
      segments.unshift(part);
      const selector = segments.join(' > ');
      if (isUniqueSelector(selector)) {
        return selector;
      }
      current = current.parentElement;
    }

    return segments.join(' > ');
  }

  function xpathSelector(element) {
    const segments = [];
    let current = element;
    while (current && current.nodeType === Node.ELEMENT_NODE && segments.length < 8) {
      const siblings = current.parentElement
        ? Array.from(current.parentElement.children).filter((child) => child.tagName === current.tagName)
        : [current];
      const index = siblings.indexOf(current) + 1;
      segments.unshift(`${current.tagName.toLowerCase()}[${index}]`);
      current = current.parentElement;
    }

    return `/${segments.join('/')}`;
  }

  function nthChildSelector(element) {
    if (!element.parentElement) {
      return '';
    }

    const siblings = Array.from(element.parentElement.children);
    const index = siblings.indexOf(element) + 1;
    if (index <= 0) {
      return '';
    }

    return `${element.parentElement.tagName.toLowerCase()} > ${element.tagName.toLowerCase()}:nth-child(${index})`;
  }

  function cssSegment(element) {
    if (element.id) {
      return `#${escapeIdentifier(element.id)}`;
    }

    let segment = element.tagName.toLowerCase();
    const classNames = Array.from(element.classList || []).filter(Boolean).slice(0, 2);
    if (classNames.length > 0) {
      segment += classNames.map((className) => `.${escapeIdentifier(className)}`).join('');
    }
    if (element.parentElement) {
      const siblings = Array.from(element.parentElement.children).filter((child) => child.tagName === element.tagName);
      if (siblings.length > 1) {
        segment += `:nth-of-type(${siblings.indexOf(element) + 1})`;
      }
    }
    return segment;
  }

  function scoreTargetCandidate(element, request, catalog) {
    const rawTexts = [
      element.visibleText,
      element.accessibleName,
      element.ariaLabel,
      element.title,
      element.placeholder,
      element.name,
      element.id,
      element.href,
      element.formContext,
      element.nearbyText
    ];
    const textByField = {
      visibleText: normalize(element.visibleText),
      accessibleName: normalize(element.accessibleName),
      ariaLabel: normalize(element.ariaLabel),
      title: normalize(element.title),
      placeholder: normalize(element.placeholder),
      name: normalize(element.name),
      id: normalize(element.id),
      href: normalize(element.href),
      formContext: normalize(element.formContext),
      nearbyText: normalize(element.nearbyText)
    };
    const normalizedTarget = normalize(request.targetText);
    const normalizedContext = normalize(request.context);
    const targetTokens = tokenize(normalizedTarget);
    const contextTokens = tokenize(normalizedContext);
    let score = 0;
    const reasons = [];

    if (textByField.visibleText === normalizedTarget && normalizedTarget) {
      score += 0.38;
      reasons.push(`Exact visible text match "${request.targetText}"`);
    } else if (textByField.visibleText.includes(normalizedTarget) && normalizedTarget) {
      score += 0.24;
      reasons.push('Visible text contains target text');
    }

    if (textByField.accessibleName === normalizedTarget && normalizedTarget) {
      score += 0.28;
      reasons.push('Accessible name exact match');
    } else if (textByField.accessibleName.includes(normalizedTarget) && normalizedTarget) {
      score += 0.18;
      reasons.push('Accessible name contains target text');
    }

    if (textByField.ariaLabel.includes(normalizedTarget) && normalizedTarget) {
      score += 0.16;
      reasons.push('Aria label contains target text');
    }

    if (textByField.placeholder.includes(normalizedTarget) && normalizedTarget) {
      score += 0.12;
      reasons.push('Placeholder contains target text');
    }

    if (textByField.title.includes(normalizedTarget) && normalizedTarget) {
      score += 0.1;
      reasons.push('Title contains target text');
    }

    if (textByField.href.includes(normalizedTarget.replace(/\s+/g, '-')) && normalizedTarget) {
      score += 0.08;
      reasons.push('Href matches target text');
    }

    let tokenMatches = 0;
    for (const token of targetTokens) {
      if (token.length < 3) {
        continue;
      }

      if (rawTexts.some((value) => normalize(value).includes(token))) {
        tokenMatches += 1;
      }
    }
    if (tokenMatches > 0) {
      const tokenScore = Math.min(0.18, tokenMatches * 0.05);
      score += tokenScore;
      reasons.push(`Target token matches: ${tokenMatches}`);
    }

    let contextMatches = 0;
    for (const token of contextTokens) {
      if (token.length < 4) {
        continue;
      }

      if (rawTexts.some((value) => normalize(value).includes(token))) {
        contextMatches += 1;
      }
    }
    if (contextMatches > 0) {
      score += Math.min(0.12, contextMatches * 0.03);
      reasons.push(`Context token matches: ${contextMatches}`);
    }

    if (request.elementKinds.length > 0) {
      const kindMatch = request.elementKinds.includes(element.elementKind) ||
        request.elementKinds.includes(element.tagName) ||
        request.elementKinds.includes(element.inputKind);
      if (kindMatch) {
        score += 0.12;
        reasons.push('Element kind matches intent filter');
      } else {
        score -= 0.1;
      }
    }

    score += intentBonus(element, request.intent, reasons);

    if (element.isVisible) {
      score += 0.06;
      reasons.push('Element is visible');
    } else {
      score -= 0.3;
    }

    if (element.isEnabled) {
      score += 0.05;
      reasons.push('Element is enabled');
    } else {
      score -= 0.2;
    }

    if (element.viewportPosition.inViewport) {
      score += 0.04;
      reasons.push('Element is in viewport');
    } else if (!element.viewportPosition.onScreen) {
      score -= 0.08;
    }

    if (element.formContext && normalizedContext && normalize(element.formContext).includes(normalizedContext)) {
      score += 0.06;
      reasons.push('Form context matches instruction context');
    }

    if (element.stableSelectors.length > 0) {
      score += Math.min(0.08, Number(element.stableSelectors[0].confidence || 0) * 0.08);
      reasons.push(`Best selector confidence ${Number(element.stableSelectors[0].confidence || 0).toFixed(2)}`);
    }

    if (element.riskFlags.includes('credentialLike') || element.riskFlags.includes('password')) {
      score -= 0.14;
      reasons.push('Credential-like risk');
    }

    const similar = catalog.filter((candidate) =>
      candidate !== element &&
      normalize(candidate.visibleText || candidate.accessibleName || '') === normalize(element.visibleText || element.accessibleName || ''));
    if (similar.length > 0) {
      score -= Math.min(0.12, similar.length * 0.04);
      reasons.push(`Ambiguous text across ${similar.length + 1} elements`);
    }

    return {
      element,
      score: clamp(score, 0, 0.99),
      reasons
    };
  }

  function intentBonus(element, intent, reasons) {
    switch (intent) {
      case 'click':
        if (element.elementKind === 'button' || element.elementKind === 'link' || element.elementKind === 'label') {
          reasons.push('Clickable semantic kind');
          return 0.1;
        }
        return 0;
      case 'input':
        if (['input', 'textarea', 'contenteditable'].includes(element.inputKind)) {
          reasons.push('Input-compatible surface');
          return 0.12;
        }
        return 0;
      case 'read':
        reasons.push('Readable surface');
        return 0.06;
      case 'select':
        if (element.elementKind === 'select' || element.elementKind === 'option') {
          reasons.push('Selectable surface');
          return 0.12;
        }
        return 0;
      default:
        return 0;
    }
  }

  function applyAmbiguityPenalty(scored) {
    if (scored.length < 2) {
      return;
    }

    const best = scored[0];
    const second = scored[1];
    if (Math.abs(best.score - second.score) < 0.06) {
      best.score = clamp(best.score - 0.06, 0, 0.99);
      best.reasons.push('Top candidates are close in score');
    }
  }

  function buildResolveCandidate(candidate) {
    const bestSelector = selectBestStableSelector(candidate.element.stableSelectors);
    return {
      elementId: candidate.element.elementId,
      score: Number(candidate.score.toFixed(4)),
      reason: candidate.reasons[0] || 'Ranked candidate',
      reasons: candidate.reasons,
      element: candidate.element,
      bestSelector
    };
  }

  function resolveLiveElement(args) {
    const elementId = args && args.elementId ? String(args.elementId) : '';
    if (elementId) {
      const direct = getElementById(elementId);
      if (direct) {
        return { element: direct, rehydrated: false };
      }

      const selectors = normalizeStableSelectors(args.stableSelectors || args.selectors || []);
      const rehydrated = rehydrateElement(selectors);
      if (rehydrated) {
        nodeToElementId.set(rehydrated, elementId);
        elementIdToNode.set(elementId, rehydrated);
        return { element: rehydrated, rehydrated: true };
      }

      throw new Error(`elementId not found: ${elementId}`);
    }

    const selector = String((args && args.selector) || '');
    if (!selector) {
      throw new Error('selector or elementId required');
    }

    const element = document.querySelector(selector);
    if (!element) {
      throw new Error(`selector not found: ${selector}`);
    }

    return { element, rehydrated: false };
  }

  function normalizeStableSelectors(value) {
    if (!Array.isArray(value)) {
      return [];
    }

    return value
      .map((item) => {
        if (!item) {
          return null;
        }
        if (typeof item === 'string') {
          return { selector: item, type: 'css-path', confidence: 0.3, reason: 'legacy selector' };
        }
        if (typeof item.selector === 'string') {
          return {
            selector: item.selector,
            type: String(item.type || 'css-path'),
            confidence: Number(item.confidence || 0),
            reason: String(item.reason || '')
          };
        }
        return null;
      })
      .filter(Boolean);
  }

  function rehydrateElement(selectors) {
    for (const candidate of selectors) {
      const selector = candidate.selector || '';
      if (!selector) {
        continue;
      }

      const element = candidate.type === 'xpath'
        || candidate.type === 'role-accessible-name'
        ? queryByXpath(selector)
        : safeQuerySelector(selector);
      if (element) {
        return element;
      }
    }

    return null;
  }

  function safeQuerySelector(selector) {
    try {
      return document.querySelector(selector);
    } catch {
      return null;
    }
  }

  function queryByXpath(selector) {
    try {
      const result = document.evaluate(selector, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);
      return result.singleNodeValue instanceof Element ? result.singleNodeValue : null;
    } catch {
      return null;
    }
  }

  function getOrCreateElementId(element) {
    const existing = nodeToElementId.get(element);
    if (existing) {
      return existing;
    }

    const elementId = `nexa-el-${nextElementId++}`;
    nodeToElementId.set(element, elementId);
    elementIdToNode.set(elementId, element);
    return elementId;
  }

  function getElementById(elementId) {
    const element = elementIdToNode.get(elementId);
    if (!element || !element.isConnected) {
      elementIdToNode.delete(elementId);
      return null;
    }
    return element;
  }

  function capturePageSnapshot() {
    const bodyText = safeText(document.body ? document.body.innerText || '' : '').slice(0, 1200);
    return {
      url: window.location.href,
      title: document.title,
      domSignature: `${document.title}|${bodyText}|${document.readyState}|${document.body ? document.body.childElementCount : 0}`
    };
  }

  function normalizeVerificationRequest(verify) {
    if (!verify || typeof verify !== 'object') {
      return {
        expectUrlChange: false,
        expectDomChange: true,
        expectTextAppears: '',
        expectSelectorAppears: '',
        timeoutMs: 1500
      };
    }

    return {
      expectUrlChange: Boolean(verify.expectUrlChange),
      expectDomChange: verify.expectDomChange !== false,
      expectTextAppears: safeText(verify.expectTextAppears || ''),
      expectSelectorAppears: String(verify.expectSelectorAppears || ''),
      timeoutMs: clamp(Number(verify.timeoutMs || 5000), 200, 15000)
    };
  }

  async function verifyAfterAction(before, verify) {
    const started = Date.now();
    let latest = evaluateVerification(before, verify);
    while (!latest.expectedConditionMet && Date.now() - started < verify.timeoutMs) {
      await delay(150);
      latest = evaluateVerification(before, verify);
      if ((latest.urlChanged || latest.domChanged || latest.titleChanged) && !verify.expectTextAppears && !verify.expectSelectorAppears && !verify.expectUrlChange) {
        break;
      }
    }

    const changedAnything = latest.urlChanged || latest.domChanged || latest.titleChanged;
    let verificationStatus = 'uncertain';
    let reason = 'click posiblemente no efectivo';

    if (latest.expectedConditionMet) {
      verificationStatus = 'passed';
      reason = latest.reason;
    } else if (verify.expectUrlChange || verify.expectSelectorAppears || verify.expectTextAppears) {
      verificationStatus = 'failed';
      reason = latest.reason;
    } else if (changedAnything) {
      verificationStatus = 'passed';
      reason = latest.reason;
    }

    return {
      beforeUrl: before.url,
      afterUrl: latest.afterUrl,
      beforeTitle: before.title,
      afterTitle: latest.afterTitle,
      urlChanged: latest.urlChanged,
      titleChanged: latest.titleChanged,
      domChanged: latest.domChanged,
      expectedConditionMet: latest.expectedConditionMet,
      verificationStatus,
      reason
    };
  }

  function evaluateVerification(before, verify) {
    const after = capturePageSnapshot();
    const urlChanged = before.url !== after.url;
    const titleChanged = before.title !== after.title;
    const domChanged = before.domSignature !== after.domSignature;
    const textAppears = verify.expectTextAppears
      ? normalize(document.body ? document.body.innerText || '' : '').includes(normalize(verify.expectTextAppears))
      : false;
    const selectorAppears = verify.expectSelectorAppears
      ? Boolean(safeQuerySelector(verify.expectSelectorAppears))
      : false;

    const expectedConditionMet =
      (!verify.expectUrlChange || urlChanged) &&
      (!verify.expectDomChange || domChanged) &&
      (!verify.expectTextAppears || textAppears) &&
      (!verify.expectSelectorAppears || selectorAppears);

    let reason = 'click posiblemente no efectivo';
    if (expectedConditionMet) {
      reason = 'Post-action verification passed.';
    } else if (verify.expectUrlChange && !urlChanged) {
      reason = 'Expected URL change not observed.';
    } else if (verify.expectSelectorAppears && !selectorAppears) {
      reason = 'Expected selector did not appear.';
    } else if (verify.expectTextAppears && !textAppears) {
      reason = 'Expected text did not appear.';
    } else if (verify.expectDomChange && !domChanged) {
      reason = 'Expected DOM change not observed.';
    }

    return {
      afterUrl: after.url,
      afterTitle: after.title,
      urlChanged,
      titleChanged,
      domChanged,
      expectedConditionMet,
      reason
    };
  }

  function selectBestStableSelector(selectors) {
    if (!Array.isArray(selectors) || selectors.length === 0) {
      return null;
    }

    return selectors
      .slice()
      .sort((left, right) => Number(right.confidence || 0) - Number(left.confidence || 0))[0];
  }

  function compactElementDescriptor(element) {
    if (!element || !(element instanceof Element)) {
      return null;
    }

    const descriptor = describeInteractiveElement(element);
    return compactDescriptorFromDescriptor(descriptor);
  }

  function compactDescriptorFromDescriptor(descriptor) {
    if (!descriptor) {
      return null;
    }

    return {
      elementId: descriptor.elementId,
      elementKind: descriptor.elementKind,
      tagName: descriptor.tagName,
      role: descriptor.role,
      visibleText: descriptor.visibleText,
      accessibleName: descriptor.accessibleName,
      bestSelector: selectBestStableSelector(descriptor.stableSelectors),
      riskFlags: descriptor.riskFlags,
      isVisible: descriptor.isVisible,
      isEnabled: descriptor.isEnabled
    };
  }

  function compareCatalogElements(left, right) {
    const leftPriority = catalogPriority(left);
    const rightPriority = catalogPriority(right);
    if (leftPriority !== rightPriority) {
      return rightPriority - leftPriority;
    }

    return (right.visibleText || right.accessibleName || '').length - (left.visibleText || left.accessibleName || '').length;
  }

  function catalogPriority(descriptor) {
    let priority = 0;
    if (descriptor.isVisible) {
      priority += 4;
    }
    if (descriptor.isEnabled) {
      priority += 3;
    }
    if (descriptor.elementKind === 'button' || descriptor.elementKind === 'link') {
      priority += 4;
    }
    if (descriptor.inputKind === 'input' || descriptor.inputKind === 'textarea' || descriptor.inputKind === 'select') {
      priority += 3;
    }
    if (descriptor.accessibleName || descriptor.visibleText) {
      priority += 2;
    }
    if (descriptor.stableSelectors.length > 0) {
      priority += 1;
    }
    return priority;
  }

  function inferElementKind(element) {
    const tagName = element.tagName.toLowerCase();
    const role = inferRole(element);
    if (tagName === 'a') {
      return 'link';
    }
    if (tagName === 'button' || role === 'button' || tagName === 'label') {
      return 'button';
    }
    if (tagName === 'select') {
      return 'select';
    }
    if (tagName === 'option') {
      return 'option';
    }
    if (tagName === 'textarea') {
      return 'textarea';
    }
    if (tagName === 'input') {
      return 'input';
    }
    if (element.isContentEditable) {
      return 'contenteditable';
    }
    return 'interactive';
  }

  function inferInputKind(element) {
    const tagName = element.tagName.toLowerCase();
    if (tagName === 'textarea') {
      return 'textarea';
    }
    if (tagName === 'select') {
      return 'select';
    }
    if (tagName === 'option') {
      return 'option';
    }
    if (tagName === 'input') {
      return 'input';
    }
    if (element.isContentEditable) {
      return 'contenteditable';
    }
    return tagName;
  }

  function inferRole(element) {
    const explicitRole = element.getAttribute('role');
    if (explicitRole) {
      return explicitRole.toLowerCase();
    }

    const tagName = element.tagName.toLowerCase();
    if (tagName === 'a') {
      return 'link';
    }
    if (tagName === 'button') {
      return 'button';
    }
    if (tagName === 'select') {
      return 'combobox';
    }
    if (tagName === 'textarea' || element.isContentEditable) {
      return 'textbox';
    }
    if (tagName === 'input') {
      const type = String(element.getAttribute('type') || 'text').toLowerCase();
      if (type === 'checkbox') {
        return 'checkbox';
      }
      if (type === 'radio') {
        return 'radio';
      }
      return 'textbox';
    }
    return '';
  }

  function accessibleNameFor(element) {
    const ariaLabel = element.getAttribute('aria-label');
    if (ariaLabel) {
      return safeText(ariaLabel);
    }

    const labelledBy = element.getAttribute('aria-labelledby');
    if (labelledBy) {
      const names = labelledBy
        .split(/\s+/)
        .map((id) => document.getElementById(id))
        .filter(Boolean)
        .map((node) => safeText(node.innerText || node.textContent || ''))
        .filter(Boolean);
      if (names.length > 0) {
        return names.join(' ');
      }
    }

    if (element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement || element instanceof HTMLSelectElement) {
      if (element.labels && element.labels.length > 0) {
        const labelText = Array.from(element.labels)
          .map((label) => safeText(label.innerText || label.textContent || ''))
          .filter(Boolean)
          .join(' ');
        if (labelText) {
          return labelText;
        }
      }
    }

    return safeText(element.innerText || element.textContent || '');
  }

  function formContextFor(element) {
    const form = element.form || element.closest('form');
    if (!form) {
      return '';
    }
    return safeText(form.innerText || form.textContent || '').slice(0, 220);
  }

  function nearbyTextFor(element) {
    const parent = element.closest('label, form, section, article, div, nav, aside') || element.parentElement;
    if (!parent) {
      return '';
    }

    return safeText(parent.innerText || parent.textContent || '').slice(0, 180);
  }

  function boundsFor(element) {
    const rect = element.getBoundingClientRect();
    return {
      x: Math.round(rect.x),
      y: Math.round(rect.y),
      width: Math.round(rect.width),
      height: Math.round(rect.height)
    };
  }

  function viewportPositionFor(element) {
    const rect = element.getBoundingClientRect();
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;
    const inViewport = rect.bottom >= 0 &&
      rect.right >= 0 &&
      rect.top <= viewportHeight &&
      rect.left <= viewportWidth;
    return {
      x: Math.round(rect.x),
      y: Math.round(rect.y),
      centerX: Math.round(rect.left + rect.width / 2),
      centerY: Math.round(rect.top + rect.height / 2),
      inViewport,
      onScreen: inViewport
    };
  }

  function describeValue(element, descriptor) {
    if (descriptor.isPassword) {
      return '';
    }

    if ('value' in element) {
      return redactValueIfSensitive(String(element.value || ''), descriptor);
    }

    return '';
  }

  function redactValueIfSensitive(value, descriptor) {
    if (!value) {
      return '';
    }

    if (descriptor.isPassword || descriptor.isCredentialLike || looksSensitiveValue(value)) {
      return '[redacted]';
    }

    return value.length > 200 ? `${value.slice(0, 200)}...` : value;
  }

  function buildRiskFlags(descriptor) {
    const flags = [];
    if (descriptor.isPassword) {
      flags.push('password');
    }
    if (descriptor.isCredentialLike) {
      flags.push('credentialLike');
    }
    if (!descriptor.isVisible) {
      flags.push('hidden');
    }
    if (!descriptor.isEnabled) {
      flags.push('disabled');
    }
    if (!descriptor.viewportPosition.inViewport) {
      flags.push('offscreen');
    }
    return flags;
  }

  function buildScoreHints(descriptor) {
    const hints = [];
    if (descriptor.visibleText) {
      hints.push(`text:${descriptor.visibleText.slice(0, 80)}`);
    }
    if (descriptor.accessibleName) {
      hints.push(`name:${descriptor.accessibleName.slice(0, 80)}`);
    }
    if (descriptor.formContext) {
      hints.push('formContext');
    }
    if (descriptor.stableSelectors.length > 0) {
      hints.push(`selector:${descriptor.stableSelectors[0].type}`);
    }
    return hints;
  }

  function isVisible(element) {
    if (!(element instanceof Element)) {
      return false;
    }

    const style = window.getComputedStyle(element);
    if (!style || style.visibility === 'hidden' || style.display === 'none' || Number(style.opacity) === 0) {
      return false;
    }

    const rect = element.getBoundingClientRect();
    return rect.width > 0 && rect.height > 0;
  }

  function isEnabled(element) {
    if ('disabled' in element && element.disabled) {
      return false;
    }
    if (element.getAttribute('aria-disabled') === 'true') {
      return false;
    }
    return true;
  }

  function isPasswordLike(element) {
    return element instanceof HTMLInputElement && String(element.type || '').toLowerCase() === 'password';
  }

  function isCredentialLikeElement(element) {
    if (!(element instanceof Element)) {
      return false;
    }

    const values = [
      element.getAttribute('name') || '',
      element.getAttribute('id') || '',
      element.getAttribute('placeholder') || '',
      element.getAttribute('autocomplete') || '',
      accessibleNameFor(element),
      safeText(element.innerText || element.textContent || '')
    ].join(' ');
    const normalized = normalize(values);
    return normalized.includes('password') ||
      normalized.includes('contrasena') ||
      normalized.includes('clave') ||
      normalized.includes('clave fiscal') ||
      normalized.includes('token') ||
      normalized.includes('otp') ||
      normalized.includes('codigo') ||
      normalized.includes('usuario') ||
      normalized.includes('login') ||
      normalized.includes('cuit') ||
      normalized.includes('username') ||
      normalized.includes('current-password') ||
      normalized.includes('one-time-code');
  }

  function isReadableValueElement(element) {
    return element instanceof HTMLInputElement ||
      element instanceof HTMLTextAreaElement ||
      element instanceof HTMLSelectElement;
  }

  function pageHasCaptchaOrTwoFactor() {
    const text = safeText(document.body ? document.body.innerText || '' : '');
    const normalized = normalize(text);
    return normalized.includes('captcha') ||
      normalized.includes('recaptcha') ||
      normalized.includes('turnstile') ||
      normalized.includes('hcaptcha') ||
      normalized.includes('2fa') ||
      normalized.includes('two factor') ||
      normalized.includes('otp') ||
      normalized.includes('codigo de verificacion');
  }

  function detectSensitiveHints(text, inputs) {
    const normalized = normalize(text);
    const hints = [];
    if (normalized.includes('captcha') || normalized.includes('recaptcha') || normalized.includes('hcaptcha') || normalized.includes('turnstile')) {
      hints.push('captcha');
    }
    if (normalized.includes('2fa') || normalized.includes('two factor') || normalized.includes('otp') || normalized.includes('token') || normalized.includes('verificacion')) {
      hints.push('twoFactor');
    }
    if (normalized.includes('login') ||
        normalized.includes('iniciar sesion') ||
        normalized.includes('clave fiscal') ||
        normalized.includes('cuit') ||
        normalized.includes('usuario') ||
        normalized.includes('contrasena') ||
        inputs.some((input) => input.isCredentialLike)) {
      hints.push('credential');
    }
    return hints;
  }

  function isSensitiveSubmit(element) {
    if (!(element instanceof Element)) {
      return false;
    }

    const normalized = normalize([
      element.innerText || element.textContent || '',
      element.getAttribute('aria-label') || '',
      element.getAttribute('title') || '',
      element.getAttribute('name') || '',
      element.getAttribute('id') || ''
    ].join(' '));
    return normalized.includes('iniciar sesion') ||
      normalized.includes('login') ||
      normalized.includes('clave fiscal') ||
      normalized.includes('submit') ||
      normalized.includes('confirmar pago') ||
      normalized.includes('pagar');
  }

  function looksSensitiveValue(value) {
    if (!value) {
      return false;
    }

    const normalized = normalize(value);
    return normalized.includes('token') ||
      normalized.includes('password') ||
      normalized.includes('contrasena') ||
      normalized.includes('clave');
  }

  function isSemanticInteractive(element) {
    const tagName = element.tagName.toLowerCase();
    return tagName === 'button' ||
      tagName === 'a' ||
      tagName === 'input' ||
      tagName === 'textarea' ||
      tagName === 'select' ||
      tagName === 'option';
  }

  function isRestrictedLocation(url) {
    return /^(chrome|edge|extension):\/\//i.test(url || '');
  }

  function normalize(value) {
    return String(value || '')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase()
      .replace(/\s+/g, ' ')
      .trim();
  }

  function tokenize(value) {
    return normalize(value)
      .split(/[^a-z0-9]+/i)
      .filter(Boolean);
  }

  function safeText(value) {
    return String(value || '').replace(/\s+/g, ' ').trim();
  }

  function clamp(value, min, max) {
    return Math.max(min, Math.min(max, value));
  }

  function isUniqueSelector(selector) {
    try {
      return document.querySelectorAll(selector).length === 1;
    } catch {
      return false;
    }
  }

  function escapeIdentifier(value) {
    if (window.CSS && typeof window.CSS.escape === 'function') {
      return window.CSS.escape(value);
    }
    return String(value).replace(/[^a-zA-Z0-9_-]/g, '\\$&');
  }

  function escapeAttributeValue(value) {
    return String(value).replace(/\\/g, '\\\\').replace(/"/g, '\\"');
  }

  function escapeXpathLiteral(value) {
    if (!value.includes('"')) {
      return `"${value}"`;
    }
    if (!value.includes('\'')) {
      return `'${value}'`;
    }

    const parts = value.split('"');
    return `concat(${parts.map((part, index) => `${index > 0 ? ', \'"\', ' : ''}"${part}"`).join('')})`;
  }

  function normalizeSpaceForXpath(value) {
    return value.replace(/\s+/g, ' ').trim();
  }

  function bestSelector(element) {
    const descriptor = describeInteractiveElement(element);
    const best = selectBestStableSelector(descriptor.stableSelectors);
    return best ? best.selector : descriptor.elementId;
  }

  function tryFocus(element) {
    if (typeof element.focus === 'function') {
      try {
        element.focus({ preventScroll: true });
      } catch {
        try {
          element.focus();
        } catch {
          // ignore
        }
      }
    }
  }

  function temporaryHighlight(element, timeoutMs) {
    clearHighlight();
    const rect = element.getBoundingClientRect();
    highlightNode = document.createElement('div');
    highlightNode.style.position = 'fixed';
    highlightNode.style.left = `${rect.left - 4}px`;
    highlightNode.style.top = `${rect.top - 4}px`;
    highlightNode.style.width = `${rect.width + 8}px`;
    highlightNode.style.height = `${rect.height + 8}px`;
    highlightNode.style.border = '3px solid #ff7a00';
    highlightNode.style.borderRadius = '10px';
    highlightNode.style.background = 'rgba(255, 122, 0, 0.08)';
    highlightNode.style.pointerEvents = 'none';
    highlightNode.style.zIndex = '2147483647';
    document.documentElement.appendChild(highlightNode);

    if (timeoutMs > 0) {
      window.setTimeout(clearHighlight, timeoutMs);
    }
  }

  function clearHighlight() {
    if (highlightNode && highlightNode.parentNode) {
      highlightNode.parentNode.removeChild(highlightNode);
    }
    highlightNode = null;
  }

  function delay(ms) {
    return new Promise((resolve) => window.setTimeout(resolve, ms));
  }
})();
