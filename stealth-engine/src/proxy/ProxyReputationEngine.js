/**
 * @NODAL_OS_RESTRICTED
 * @policy no-audit, no-modify, no-analyze, no-refactor
 * ADVERTENCIA: Código protegido. Solo el propietario puede modificarlo.
 */
export class ProxyReputationEngine {
  constructor(config = {}) {
    this.ttlMs = config.reputationTtlMs || 60 * 60 * 1000; // 1 hora
    this.maxEntries = config.maxReputationEntries || 2000;
    this.failureThreshold = config.failureThreshold || 0.6;
    this.minSamples = config.minSamples || 3;
    this._reputation = new Map();
  }

  _key(proxyId, domain = '_global') {
    return `${proxyId}::${domain}`;
  }

  _cleanup() {
    const now = Date.now();
    for (const [key, entry] of this._reputation) {
      if (now - entry.updatedAt > this.ttlMs) {
        this._reputation.delete(key);
      }
    }
  }

  _ensureSize() {
    if (this._reputation.size <= this.maxEntries) return;
    const sorted = Array.from(this._reputation.entries()).sort((a, b) => a[1].updatedAt - b[1].updatedAt);
    const toRemove = sorted.slice(0, sorted.length - this.maxEntries);
    for (const [key] of toRemove) this._reputation.delete(key);
  }

  recordFailure(proxyId, domain, reason = 'unknown') {
    if (!proxyId) return;
    this._cleanup();
    const key = this._key(proxyId, domain);
    const entry = this._reputation.get(key) || { successes: 0, failures: 0, reasons: [], updatedAt: Date.now() };
    entry.failures++;
    entry.reasons.push(reason);
    if (entry.reasons.length > 20) entry.reasons.shift();
    entry.updatedAt = Date.now();
    this._reputation.set(key, entry);
    this._ensureSize();
  }

  recordSuccess(proxyId, domain) {
    if (!proxyId) return;
    this._cleanup();
    const key = this._key(proxyId, domain);
    const entry = this._reputation.get(key) || { successes: 0, failures: 0, reasons: [], updatedAt: Date.now() };
    entry.successes++;
    entry.updatedAt = Date.now();
    this._reputation.set(key, entry);
    this._ensureSize();
  }

  getReputation(proxyId, domain = '_global') {
    this._cleanup();
    const key = this._key(proxyId, domain);
    return this._reputation.get(key) || { successes: 0, failures: 0, reasons: [] };
  }

  isProxyAtRisk(proxyId, domain = '_global') {
    if (!proxyId) return false;
    const rep = this.getReputation(proxyId, domain);
    const total = rep.successes + rep.failures;
    if (total < this.minSamples) return false;
    const failureRate = rep.failures / total;
    return failureRate >= this.failureThreshold;
  }

  getStats() {
    this._cleanup();
    return {
      entries: this._reputation.size,
      maxEntries: this.maxEntries,
      ttlMs: this.ttlMs,
    };
  }

  shutdown() {
    this._reputation.clear();
  }
}
