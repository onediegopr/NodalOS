/**
 * @NODAL_OS_RESTRICTED
 * @policy no-audit, no-modify, no-analyze, no-refactor
 * ADVERTENCIA: Código protegido. Solo el propietario puede modificarlo.
 */
import { cryptoRandom } from './AdaptiveBehaviorEngine.js';

export class HumanScroll {
  constructor(profile) {
    this.profile = profile;
  }

  async scroll(page, opts = {}) {
    const max = opts.maxScrolls || 2 + Math.floor(cryptoRandom() * 5);
    const direction = opts.direction || 'down';
    const sign = direction === 'down' ? 1 : -1;
    const subMin = this.profile.scrollSubStepsMin || 5;
    const subMax = this.profile.scrollSubStepsMax || 15;

    const textDensity = await this._estimateTextDensity(page);
    const densityFactor = 0.5 + textDensity * 1.5;

    for (let i = 0; i < max; i++) {
      const delta = sign * (200 + cryptoRandom() * 400);
      const subSteps = subMin + Math.floor(cryptoRandom() * (subMax - subMin));
      const sd = Math.round(delta / subSteps);

      for (let j = 0; j < subSteps; j++) {
        await page.mouse.wheel(0, sd);
        await new Promise(r => setTimeout(r, (8 + cryptoRandom() * 15) * densityFactor));
      }

      const readTime = this.profile.scrollPauseMs * (0.5 + cryptoRandom()) * densityFactor;
      await new Promise(r => setTimeout(r, readTime));

      if (cryptoRandom() < 0.4) {
        await page.mouse.wheel(0, (cryptoRandom() - 0.5) * 40);
      }
    }
  }

  async scrollToElement(page, selector) {
    const el = await page.$(selector);
    if (!el) throw new Error('Element not found: ' + selector);
    await el.scrollIntoViewIfNeeded();
    await new Promise(r => setTimeout(r, 300 + cryptoRandom() * 500));
    const box = await el.boundingBox();
    if (box) {
      const ty = box.y - 100 + cryptoRandom() * 200;
      await page.evaluate((y) => window.scrollTo({ top: Math.max(0, y), behavior: 'smooth' }), ty);
      await new Promise(r => setTimeout(r, 400 + cryptoRandom() * 600));
    }
  }

  async _estimateTextDensity(page) {
    try {
      return await page.evaluate(() => {
        const b = document.body;
        if (!b) return 0.5;
        const textLen = (b.innerText || '').length;
        const area = b.clientWidth * b.clientHeight || 1;
        const density = Math.min(textLen / Math.max(area / 10, 1), 2);
        return density;
      });
    } catch { return 0.5; }
  }
}
