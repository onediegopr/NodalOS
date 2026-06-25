export async function executeTool(session, tool, args = {}) {
  const page = session.page;
  switch (tool) {
    case 'observePage':
      return observePage(page);
    case 'navigate':
      return navigatePage(page, args.url);
    case 'clickElement':
    case 'click':
      return clickElement(session, args);
    case 'setElementValue':
    case 'setValue':
      return setElementValue(session, args);
    case 'scrollElementIntoView':
    case 'scrollIntoView':
      return scrollToElement(session, args);
    default:
      throw new Error('Unknown tool: ' + tool);
  }
}

async function observePage(page) {
  const url = page.url();
  const title = await page.title().catch(() => '');

  const bodyText = await page.evaluate(() => {
    const body = document.body;
    if (!body) return '';
    const items = [];
    const walk = (node) => {
      if (node.nodeType === Node.TEXT_NODE) {
        const t = node.textContent.trim();
        if (t) items.push(t);
        return;
      }
      for (const child of node.childNodes) walk(child);
    };
    walk(body);
    return items.slice(0, 500).join(' ');
  });

  const elements = await page.evaluate(() => {
    const results = [];
    const candidates = document.querySelectorAll(
      'a[href], button, input, textarea, select, [role="button"], [role="link"], [role="menuitem"]');
    let idx = 1;
    for (const el of candidates) {
      const rect = el.getBoundingClientRect();
      if (rect.width === 0 || rect.height === 0) continue;
      if (results.length >= 100) break;
      const tag = el.tagName.toLowerCase();
      results.push({
        elementId: 'el-' + (idx++),
        tagName: tag,
        visibleText: (el.innerText || el.textContent || '').trim().substring(0, 120),
        type: el.getAttribute('type') || '',
        href: el.getAttribute('href') || '',
        placeholder: el.getAttribute('placeholder') || '',
        ariaLabel: el.getAttribute('aria-label') || '',
      });
    }
    return results;
  });

  const lower = bodyText.toLowerCase();
  return {
    url, title,
    visibleTextSummary: bodyText.substring(0, 2400),
    elements: elements.slice(0, 120),
    hasPasswordField: !!(await page.evaluate(() => !!document.querySelector('input[type="password"]'))),
    hasCaptchaLike: ['captcha', 'recaptcha', 'hcaptcha', 'turnstile'].some(k => lower.includes(k)),
    hasTwoFactorLike: ['2fa', 'two factor', 'otp', 'verification code'].some(k => lower.includes(k)),
    viewport: { width: page.viewportSize().width, height: page.viewportSize().height },
    timestamp: new Date().toISOString(),
  };
}

async function navigatePage(page, url) {
  if (!url) throw new Error('URL required');
  await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 30000 });
  return { navigated: true, url };
}

async function clickElement(session, args) {
  const selector = args.selector
    || (args.elementId ? '[data-element-id="' + args.elementId + '"]' : null);
  if (!selector) throw new Error('selector or elementId required');

  if (session.behavior) {
    await session.behavior.mouse.humanClick(session.page, selector);
  } else {
    const el = await session.page.$(selector);
    if (!el) throw new Error('Element not found');
    const box = await el.boundingBox();
    if (!box) throw new Error('Element not visible');
    await session.page.mouse.click(box.x + box.width / 2, box.y + box.height / 2);
  }

  return { clicked: true, selector };
}

async function setElementValue(session, args) {
  const selector = args.selector
    || (args.elementId ? '[data-element-id="' + args.elementId + '"]' : null);
  if (!selector) throw new Error('selector or elementId required');

  if (session.behavior) {
    await session.behavior.keyboard.type(session.page, selector, String(args.value || ''));
  } else {
    const el = await session.page.$(selector);
    if (!el) throw new Error('Element not found');
    await el.click();
    await el.fill('');
    await el.type(String(args.value || ''), { delay: 30 + Math.random() * 50 });
  }

  return { set: true, selector, value: String(args.value || '') };
}

async function scrollToElement(session, args) {
  const selector = args.selector
    || (args.elementId ? '[data-element-id="' + args.elementId + '"]' : null);
  if (!selector) throw new Error('selector required');

  if (session.behavior) {
    await session.behavior.scroll.scrollToElement(session.page, selector);
  } else {
    const el = await session.page.$(selector);
    if (el) await el.scrollIntoViewIfNeeded();
  }

  return { scrolled: true, selector };
}
