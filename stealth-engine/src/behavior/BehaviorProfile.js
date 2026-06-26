/**
 * @NODAL_OS_RESTRICTED
 * @policy no-audit, no-modify, no-analyze, no-refactor
 * ADVERTENCIA: Código protegido. Solo el propietario puede modificarlo.
 */
export class BehaviorProfile {
  static PRESETS = {
    casual: { typoRate: 0.03, baseDelay: 80, clickPauseMin: 80, clickPauseMax: 230, scrollPauseMs: 1500, overShootRate: 0.6 },
    pro:     { typoRate: 0.01, baseDelay: 40, clickPauseMin: 50, clickPauseMax: 150, scrollPauseMs: 600,  overShootRate: 0.3 },
    elderly: { typoRate: 0.05, baseDelay: 150, clickPauseMin: 150, clickPauseMax: 400, scrollPauseMs: 3000, overShootRate: 0.8 },
  };

  constructor(name = 'casual') {
    const resolved = BehaviorProfile.PRESETS[name] ? name : 'casual';
    const preset = BehaviorProfile.PRESETS[resolved];
    Object.assign(this, preset);
    this.name = resolved;
  }
}
