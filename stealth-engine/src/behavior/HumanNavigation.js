/**
 * @NODAL_OS_RESTRICTED
 * @policy no-audit, no-modify, no-analyze, no-refactor
 * ADVERTENCIA: Código protegido. Solo el propietario puede modificarlo.
 */
import { cryptoRandom } from './AdaptiveBehaviorEngine.js';

export class HumanNavigation {
  constructor(mouse) {
    this.mouse = mouse;
  }

  async simulateReading(page, opts = {}) {
    const text = await page.evaluate(() => {
      const b = document.body;
      return b ? (b.innerText || b.textContent || '').length : 0;
    });

    const words = text / 5;
    const readingTimeMs = Math.min(words * 50, 12000);

    const viewport = page.viewportSize();
    const w = viewport.width, h = viewport.height;

    const moveCount = Math.floor(readingTimeMs / 2000);
    for (let i = 0; i < moveCount; i++) {
      await this.mouse.move(
        page,
        cryptoRandom() * w * 0.8,
        cryptoRandom() * h * 0.8,
        { steps: 20 }
      );
      await new Promise(r => setTimeout(r, 1500 + cryptoRandom() * 1500));
    }

    await new Promise(r => setTimeout(r, readingTimeMs));
  }

  async randomPause(min, max) {
    await new Promise(r => setTimeout(r, min + cryptoRandom() * (max - min)));
  }
}
