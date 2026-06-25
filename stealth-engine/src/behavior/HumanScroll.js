export class HumanScroll {
  constructor(profile) {
    this.profile = profile;
  }

  async scroll(page, opts = {}) {
    const max = opts.maxScrolls || 2 + Math.floor(Math.random() * 5);
    const direction = opts.direction || 'down';
    const sign = direction === 'down' ? 1 : -1;
    const subMin = this.profile.scrollSubStepsMin || 5;
    const subMax = this.profile.scrollSubStepsMax || 15;

    for (let i = 0; i < max; i++) {
      const textDensity = await this._estimateTextDensity(page);
      const densityFactor = 0.5 + textDensity * 1.5;

      const delta = sign * (200 + Math.random() * 400);
      const subSteps = subMin + Math.floor(Math.random() * (subMax - subMin));
      const sd = Math.round(delta / subSteps);

      for (let j = 0; j < subSteps; j++) {
        await page.mouse.wheel(0, sd);
        await new Promise(r => setTimeout(r, (8 + Math.random() * 15) * densityFactor));
      }

      const readTime = this.profile.scrollPauseMs * (0.5 + Math.random()) * densityFactor;
      await new Promise(r => setTimeout(r, readTime));

      if (Math.random() < 0.4) {
        await page.mouse.wheel(0, (Math.random() - 0.5) * 40);
      }
    }
  }

  async scrollToElement(page, selector) {
    const el = await page.$(selector);
    if (!el) throw new Error('Element not found: ' + selector);
    await el.scrollIntoViewIfNeeded();
    await new Promise(r => setTimeout(r, 300 + Math.random() * 500));
    const box = await el.boundingBox();
    if (box) {
      const ty = box.y - 100 + Math.random() * 200;
      await page.evaluate((y) => window.scrollTo({ top: Math.max(0, y), behavior: 'smooth' }), ty);
      await new Promise(r => setTimeout(r, 400 + Math.random() * 600));
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
