/**
 * @NODAL_OS_RESTRICTED
 * @policy no-audit, no-modify, no-analyze, no-refactor
 * ADVERTENCIA: Código protegido. Solo el propietario puede modificarlo.
 */
const LETTER_SPEED = {
  'e': 0.6, 't': 0.6, 'a': 0.65, 'o': 0.65, 'i': 0.7, 'n': 0.7, 's': 0.7, 'h': 0.7, 'r': 0.75,
  'd': 0.75, 'l': 0.75, 'c': 0.8, 'u': 0.8, 'm': 0.8, 'w': 0.85, 'f': 0.85,
  'g': 0.85, 'y': 0.85, 'p': 0.85, 'b': 0.9, 'v': 0.9, 'k': 0.95, 'j': 1.0, 'x': 1.0, 'q': 1.05, 'z': 1.05,
};

export class HumanKeyboard {
  constructor(profile) {
    this.profile = profile;
  }

  async type(page, selector, text, opts = {}) {
    const el = await page.$(selector);
    if (!el) throw new Error('Element not found: ' + selector);

    const box = await el.boundingBox();
    if (box) await page.mouse.click(box.x + box.width / 2, box.y + box.height / 2);
    await new Promise(r => setTimeout(r, 100 + Math.random() * 150));

    const errorRate = opts.errorRate ?? this.profile.typoRate;
    const baseDelay = this.profile.baseDelay || 80;
    const words = text.split(' ');

    for (let w = 0; w < words.length; w++) {
      const word = words[w];
      for (let i = 0; i < word.length; i++) {
        if (Math.random() < errorRate) {
          const wrong = HumanKeyboard.nearbyKey(word[i]);
          await page.keyboard.type(wrong);
          await delay(50, 120);
          await delay(80, 200);
          await page.keyboard.press('Backspace');
          await delay(30, 80);
        }
        await page.keyboard.type(word[i]);

        const speedFactor = LETTER_SPEED[word[i].toLowerCase()] || 0.85;
        const charDelay = baseDelay * speedFactor * (0.6 + Math.random() * 0.8);
        await new Promise(r => setTimeout(r, charDelay));

        if (i < word.length - 1 && Math.random() < 0.1) {
          await new Promise(r => setTimeout(r, 15 + Math.random() * 25));
        }
      }
      if (w < words.length - 1) {
        await page.keyboard.type(' ');
        await new Promise(r => setTimeout(r, baseDelay * 1.2 + Math.random() * baseDelay * 1.5));
      }
    }

    await delay(200, 500);
  }

  async press(page, key) {
    await page.keyboard.press(key);
    await delay(30, 100);
  }

  static nearbyKey(c) {
    const kb = {
      'a': ['q', 'w', 's', 'z'], 'b': ['v', 'g', 'h', 'n'],
      'c': ['x', 'd', 'f', 'v'], 'd': ['s', 'e', 'r', 'f', 'c'],
      'e': ['w', 's', 'd', 'r'], 'f': ['d', 'r', 't', 'g', 'v'],
      'g': ['f', 't', 'y', 'h', 'b'], 'h': ['g', 'y', 'u', 'j', 'n'],
      'i': ['u', 'j', 'k', 'o'], 'j': ['h', 'u', 'i', 'k', 'm'],
      'k': ['j', 'i', 'o', 'l'], 'l': ['k', 'o', 'p'],
      'm': ['n', 'j', 'k'], 'n': ['b', 'h', 'j', 'm'],
      'o': ['i', 'k', 'l', 'p'], 'p': ['o', 'l'],
      'q': ['w', 'a'], 'r': ['e', 'd', 'f', 't'],
      's': ['a', 'w', 'e', 'd', 'x'], 't': ['r', 'f', 'g', 'y'],
      'u': ['y', 'h', 'j', 'i'], 'v': ['c', 'f', 'g', 'b'],
      'w': ['q', 'a', 's', 'e'], 'x': ['z', 's', 'd', 'c'],
      'y': ['t', 'g', 'h', 'u'], 'z': ['a', 's', 'x'],
    };
    const nb = kb[c.toLowerCase()];
    if (!nb) return c;
    const r = nb[Math.floor(Math.random() * nb.length)];
    return c === c.toUpperCase() ? r.toUpperCase() : r;
  }
}

function delay(min, max) {
  return new Promise(r => setTimeout(r, min + Math.random() * (max - min)));
}
