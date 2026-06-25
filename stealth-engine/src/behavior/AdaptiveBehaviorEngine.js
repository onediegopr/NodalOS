export class AdaptiveBehaviorEngine {
  constructor(baseProfile, config = {}) {
    this.baseProfile = baseProfile;
    this.adaptive = config.adaptive !== false;
    this.profileVariance = config.profileVariance || 0.2;

    this.sessionParams = this.adaptive
      ? this._generateSessionParams()
      : { ...baseProfile };
  }

  _generateSessionParams() {
    const b = this.baseProfile;
    const v = this.profileVariance;

    const vary = (base) => base * (1 + (Math.random() * 2 - 1) * v);

    return {
      typoRate: Math.max(0.005, Math.min(0.15, vary(b.typoRate || 0.03))),
      baseDelay: Math.max(20, Math.min(300, vary(b.baseDelay || 80))),
      clickPauseMin: Math.max(30, Math.min(300, vary(b.clickPauseMin || 80))),
      clickPauseMax: Math.max(b.clickPauseMin + 20, Math.min(600, vary(b.clickPauseMax || 230))),
      scrollPauseMs: Math.max(300, Math.min(5000, vary(b.scrollPauseMs || 1500))),
      overShootRate: Math.max(0.2, Math.min(0.95, b.overShootRate * (1 + (Math.random() - 0.5) * v))),
      bezierVariance: 80 + Math.random() * 160,
      ballisticsThreshold: 500 + Math.random() * 300,
      scrollSubStepsMin: 3 + Math.floor(Math.random() * 5),
      scrollSubStepsMax: 8 + Math.floor(Math.random() * 10),
      name: b.name || 'casual',
    };
  }

  getProfile() {
    return this.sessionParams;
  }
}
