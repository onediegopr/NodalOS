/**
 * @NODAL_OS_RESTRICTED
 * @policy no-audit, no-modify, no-analyze, no-refactor
 * ADVERTENCIA: Código protegido. Solo el propietario puede modificarlo.
 */
import { cryptoRandom } from '../behavior/AdaptiveBehaviorEngine.js';

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

function buildSelector(el, index) {
  const tag = el.tagName.toLowerCase();
  const type = el.getAttribute('type');
  const href = el.getAttribute('href');
  const placeholder = el.getAttribute('placeholder');
  const ariaLabel = el.getAttribute('aria-label');
  const name = el.getAttribute('name');
  const id = el.id;
  const classes = Array.from(el.classList).filter(c => c.length > 1 && c.length < 30);

  if (id && /^[a-zA-Z][a-zA-Z0-9_-]*$/.test(id)) {
    return `#${CSS.escape(id)}`;
  }

  if (name && /^[a-zA-Z][a-zA-Z0-9_-]*$/.test(name)) {
    return `${tag}[name="${CSS.escape(name)}"]`;
  }

  if (type && tag === 'input') {
    return `input[type="${CSS.escape(type)}"]`;
  }

  if (placeholder) {
    const sel = `${tag}[placeholder="${CSS.escape(placeholder)}"]`;
    const all = document.querySelectorAll(sel);
    if (all.length === 1) return sel;
  }

  if (href && tag === 'a') {
    let h = href;
    if (h.length > 60) h = h.substring(0, 60);
    const sel = `a[href^="${CSS.escape(h)}"]`;
    const all = document.querySelectorAll(sel);
    if (all.length === 1) return sel;
  }

  if (ariaLabel) {
    const sel = `${tag}[aria-label="${CSS.escape(ariaLabel)}"]`;
    const all = document.querySelectorAll(sel);
    if (all.length === 1) return sel;
  }

  if (classes.length > 0) {
    const sel = `${tag}.${classes.map(c => CSS.escape(c)).join('.')}`;
    const all = document.querySelectorAll(sel);
    if (all.length === 1) return sel;
  }

  const sameTag = document.querySelectorAll(tag);
  let count = 0;
  for (const s of sameTag) {
    count++;
    if (s === el)   return `${tag}:nth-of-type(${count})`;
  }
}

function buildText(text) {
  return (text || '').trim().substring(0, 120);
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
    const CSS = {
      escape: (s) => String(s).replace(/([^\w-])/g, '\\$1'),
    };

    const results = [];
    const candidates = document.querySelectorAll(
      'button, input, textarea, a[href], select');

    let idx = 0;
    for (const el of candidates) {
      const rect = el.getBoundingClientRect();
      if (rect.width === 0 || rect.height === 0) continue;
      if (results.length >= 100) break;
      const tag = el.tagName.toLowerCase();
      const selector = buildSelector(el, idx);
      results.push({
        elementIndex: idx++,
        selector,
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
    viewport: page.viewportSize() || { width: 1280, height: 720 },
    timestamp: new Date().toISOString(),
  };
}

async function navigatePage(page, url) {
  if (!url) throw new Error('URL required');
  await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 30000 });
  return { navigated: true, url };
}

function resolveSelector(args) {
  if (args.selector) return args.selector;
  if (typeof args.elementIndex === 'number') {
    return `(function(){var els=document.querySelectorAll('button,input,textarea,a[href],select');return els[${args.elementIndex}];})()`;
  }
  if (args.elementId) {
    return `[id="${args.elementId}"]`;
  }
  return null;
}

async function clickElement(session, args) {
  const selector = resolveSelector(args);
  if (!selector) throw new Error('selector, elementIndex, or elementId required');

  const useEval = typeof args.elementIndex === 'number' && !args.selector;

  if (session.behavior) {
    if (useEval) {
      await session.page.evaluate((idx) => {
        const els = document.querySelectorAll('button,input,textarea,a[href],select');
        const el = els[idx];
        if (el) el.click();
      }, args.elementIndex);
    } else {
      await session.behavior.mouse.humanClick(session.page, selector);
    }
  } else {
    if (useEval) {
      await session.page.evaluate((idx) => {
        const els = document.querySelectorAll('button,input,textarea,a[href],select');
        const el = els[idx];
        if (!el) throw new Error('Element not found');
        el.scrollIntoViewIfNeeded?.();
        el.click();
      }, args.elementIndex);
    } else {
      const el = await session.page.$(selector);
      if (!el) throw new Error('Element not found');
      const box = await el.boundingBox();
      if (!box) throw new Error('Element not visible');
      await session.page.mouse.click(box.x + box.width / 2, box.y + box.height / 2);
    }
  }

  return { clicked: true, selector: selector.substring(0, 120) };
}

async function setElementValue(session, args) {
  const selector = resolveSelector(args);
  if (!selector) throw new Error('selector, elementIndex, or elementId required');

  const useEval = typeof args.elementIndex === 'number' && !args.selector;
  const valueStr = String(args.value || '');

  if (session.behavior) {
    if (useEval) {
      await session.page.evaluate(({ idx, val }) => {
        const els = document.querySelectorAll('button,input,textarea,a[href],select');
        const el = els[idx];
        if (!el) throw new Error('Element not found');
        el.focus();
        el.value = val;
        el.dispatchEvent(new Event('input', { bubbles: true }));
        el.dispatchEvent(new Event('change', { bubbles: true }));
      }, { idx: args.elementIndex, val: valueStr });
    } else {
      await session.behavior.keyboard.type(session.page, selector, valueStr);
    }
  } else {
    if (useEval) {
      await session.page.evaluate(({ idx, val }) => {
        const els = document.querySelectorAll('button,input,textarea,a[href],select');
        const el = els[idx];
        if (!el) throw new Error('Element not found');
        el.focus();
        el.value = val;
        el.dispatchEvent(new Event('input', { bubbles: true }));
        el.dispatchEvent(new Event('change', { bubbles: true }));
      }, { idx: args.elementIndex, val: valueStr });
    } else {
      const el = await session.page.$(selector);
      if (!el) throw new Error('Element not found');
      await el.click();
      await el.fill('');
      await el.type(valueStr, { delay: 30 + cryptoRandom() * 50 });
    }
  }

  return { set: true, selector: selector.substring(0, 120), value: valueStr };
}

async function scrollToElement(session, args) {
  const selector = resolveSelector(args);
  if (!selector) throw new Error('selector, elementIndex, or elementId required');

  const useEval = typeof args.elementIndex === 'number' && !args.selector;

  if (session.behavior) {
    if (useEval) {
      await session.page.evaluate((idx) => {
        const els = document.querySelectorAll('button,input,textarea,a[href],select');
        const el = els[idx];
        if (el) el.scrollIntoViewIfNeeded?.();
      }, args.elementIndex);
    } else {
      await session.behavior.scroll.scrollToElement(session.page, selector);
    }
  } else {
    if (useEval) {
      await session.page.evaluate((idx) => {
        const els = document.querySelectorAll('button,input,textarea,a[href],select');
        const el = els[idx];
        if (el) el.scrollIntoViewIfNeeded?.();
      }, args.elementIndex);
    } else {
      const el = await session.page.$(selector);
      if (el) await el.scrollIntoViewIfNeeded();
    }
  }

  return { scrolled: true, selector: selector.substring(0, 120) };
}
