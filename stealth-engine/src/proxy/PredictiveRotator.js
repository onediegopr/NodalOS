/**
 * @NODAL_OS_RESTRICTED
 * @policy no-audit, no-modify, no-analyze, no-refactor
 * ADVERTENCIA: Código protegido. Solo el propietario puede modificarlo.
 */
export class PredictiveRotator {
  constructor(config = {}) {
    this.enabled = config.predictiveRotation !== false;
    this.minRiskScore = config.minRiskScore || 0.5;
    this.metrics = {
      checks: 0,
      rotationsTriggered: 0,
      successfulPredictions: 0,
    };
  }

  shouldRotate(proxyId, domain, reputationEngine) {
    if (!this.enabled || !proxyId || !reputationEngine) return false;
    this.metrics.checks++;
    const atRisk = reputationEngine.isProxyAtRisk(proxyId, domain);
    if (atRisk) this.metrics.rotationsTriggered++;
    return atRisk;
  }

  selectNewProxy(taskId, currentProxyId, domain, proxyManager) {
    if (!proxyManager || proxyManager.pool.length === 0) return null;
    // Elegir un proxy distinto al actual, preferiblemente de distinto país y mejor successRate
    const candidates = proxyManager.pool
      .filter(p => p.id !== currentProxyId)
      .filter(p => p.status !== 'banned' && !ProxyRotatorHelper.isOnCooldown(p))
      .sort((a, b) => (b.successRate || 0) - (a.successRate || 0));

    if (candidates.length === 0) return null;
    const chosen = candidates[0];
    return {
      id: chosen.id,
      server: chosen.url,
      url: chosen.url,
      username: chosen.username,
      password: chosen.password,
      type: chosen.type,
      country: chosen.country,
      provider: chosen.provider,
    };
  }

  recordPredictionSuccess() {
    this.metrics.successfulPredictions++;
  }

  getMetrics() {
    return { ...this.metrics };
  }
}

class ProxyRotatorHelper {
  static isOnCooldown(p) {
    if (p.status === 'cooldown' && p.cooldownUntil && Date.now() < p.cooldownUntil) return true;
    return false;
  }
}
