export class HumanMouse {
  constructor(profile) {
    this.profile = profile;
  }

  async move(page, tx, ty, opts = {}) {
    const sx = opts.startX ?? Math.floor(Math.random() * 400);
    const sy = opts.startY ?? Math.floor(Math.random() * 300);
    const dist = Math.sqrt((tx - sx) ** 2 + (ty - sy) ** 2);
    const threshold = this.profile.ballisticsThreshold || 600;

    if (dist > threshold) {
      await this._ballisticMove(page, sx, sy, tx, ty, opts);
    } else {
      await this._bezierMove(page, sx, sy, tx, ty, opts);
    }
  }

  async _ballisticMove(page, sx, sy, tx, ty, opts = {}) {
    var steps = opts.steps || 80 + Math.floor(Math.random() * 60);
    var jitter = opts.jitterAmount || 3;
    var baseDelay = this.profile.baseDelay || 80;
    var dist = Math.sqrt((tx - sx) ** 2 + (ty - sy) ** 2);

    for (var i = 0; i <= steps; i++) {
      var t = i / steps;
      var curveT = 1 - Math.pow(1 - t, 3);
      var speedT = Math.sin(t * Math.PI);
      var x = sx + (tx - sx) * curveT;
      var y = sy + (ty - sy) * curveT;
      var delay = (2 + speedT * 12) * (baseDelay / 80);

      var noiseScale = dist > 500 ? 1.5 : 1;
      var jx = (Math.random() - 0.5) * jitter * noiseScale;
      var jy = (Math.random() - 0.5) * jitter * noiseScale;

      await page.mouse.move(Math.round(x + jx), Math.round(y + jy));
      await new Promise(r => setTimeout(r, delay));
    }

    await page.mouse.move(tx, ty);
  }

  async _bezierMove(page, sx, sy, tx, ty, opts = {}) {
    const steps = opts.steps || 60 + Math.floor(Math.random() * 40);
    const jitter = opts.jitterAmount || 3;
    const baseDelay = this.profile.baseDelay || 80;
    const variance = this.profile.bezierVariance || 120;

    const c1x = sx + (tx - sx) * 0.25 + (Math.random() - 0.5) * variance;
    const c1y = sy + (ty - sy) * 0.25 + (Math.random() - 0.5) * variance * 0.7;
    const c2x = sx + (tx - sx) * 0.75 + (Math.random() - 0.5) * variance * 0.8;
    const c2y = sy + (ty - sy) * 0.75 + (Math.random() - 0.5) * variance * 0.5;

    const over = opts.overshoot !== false && Math.random() < (this.profile.overShootRate || 0.6);
    const ex = over ? tx + (tx - c2x) * 0.25 : tx;
    const ey = over ? ty + (ty - c2y) * 0.25 : ty;

    const pts = [];
    for (let i = 0; i <= steps; i++) {
      const t = i / steps;
      pts.push({
        x: Math.round((1 - t) ** 3 * sx + 3 * (1 - t) ** 2 * t * c1x + 3 * (1 - t) * t ** 2 * c2x + t ** 3 * ex),
        y: Math.round((1 - t) ** 3 * sy + 3 * (1 - t) ** 2 * t * c1y + 3 * (1 - t) * t ** 2 * c2y + t ** 3 * ey),
      });
    }

    for (let i = 0; i < pts.length; i++) {
      const t = i / pts.length;
      const easing = t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
      const delay = 2 + easing * 8 * (baseDelay / 80);
      await page.mouse.move(pts[i].x + (Math.random() - 0.5) * jitter, pts[i].y + (Math.random() - 0.5) * jitter);
      await new Promise(r => setTimeout(r, delay));
    }

    if (over) {
      await new Promise(r => setTimeout(r, 30 + Math.random() * 50));
      for (let i = 0; i <= 8; i++) {
        const t = i / 8;
        await page.mouse.move(ex + (tx - ex) * t + (Math.random() - 0.5) * 2, ey + (ty - ey) * t + (Math.random() - 0.5) * 2);
        await new Promise(r => setTimeout(r, 5 + Math.random() * 5));
      }
    }
  }

  async click(page, x, y, opts = {}) {
    await this.move(page, x, y, opts);
    const pause = this.profile.clickPauseMin + Math.random() * (this.profile.clickPauseMax - this.profile.clickPauseMin);
    await new Promise(r => setTimeout(r, pause));
    await page.mouse.click(x, y, { ...opts });
    await new Promise(r => setTimeout(r, 50 + Math.random() * 100));
    if (Math.random() > 0.7) {
      await page.mouse.move(x + (Math.random() - 0.5) * 3, y + (Math.random() - 0.5) * 3);
    }
  }

  async humanClick(page, selector) {
    const el = await page.$(selector);
    if (!el) throw new Error('Element not found: ' + selector);
    const box = await el.boundingBox();
    if (!box) throw new Error('Element not visible: ' + selector);
    const x = box.x + box.width * (0.2 + Math.random() * 0.6);
    const y = box.y + box.height * (0.2 + Math.random() * 0.6);
    await this.click(page, x, y);
  }
}
