/**
 * @NODAL_OS_RESTRICTED
 * @policy no-audit, no-modify, no-analyze, no-refactor
 * ADVERTENCIA: Código protegido. Solo el propietario puede modificarlo.
 */
export class DomainRateLimiter {
  constructor(config = {}) {
    this.baseDelayMs = config.baseDelayMs || 1000;
    this.minDelayMs = config.minDelayMs || 200;
    this.maxDelayMs = config.maxDelayMs || 60000;
    this.backoffMultiplier = config.backoffMultiplier || 2.0;
    this.recoveryFactor = config.recoveryFactor || 0.9;
    this.ttlMs = config.ttlMs || 60 * 60 * 1000; // 1 hora
    this._domains = new Map();
  }

  _entry(domain) {
    const now = Date.now();
    let e = this._domains.get(domain);
    if (!e || now - e.updatedAt > this.ttlMs) {
      e = { delayMs: this.baseDelayMs, consecutive429: 0, consecutiveSuccess: 0, updatedAt: now };
      this._domains.set(domain, e);
    }
    return e;
  }

  recordRequest(domain) {
    this._entry(domain);
  }

  recordResponse(domain, statusCode) {
    const e = this._entry(domain);
    e.updatedAt = Date.now();
    const code = statusCode || 429;
    if (code === 429 || code === 403 || code === 503) {
      e.consecutive429++;
      e.consecutiveSuccess = 0;
      e.delayMs = Math.min(this.maxDelayMs, e.delayMs * this.backoffMultiplier);
    } else if (statusCode >= 200 && statusCode < 300) {
      e.consecutiveSuccess++;
      if (e.consecutiveSuccess >= 3) {
        e.consecutive429 = 0;
        e.delayMs = Math.max(this.minDelayMs, e.delayMs * this.recoveryFactor);
      }
    }
  }

  getDelay(domain) {
    return this._entry(domain).delayMs;
  }

  async wait(domain) {
    const delay = this.getDelay(domain);
    if (delay > 0) await new Promise(r => setTimeout(r, delay));
    return delay;
  }

  getStats() {
    return {
      trackedDomains: this._domains.size,
      delays: Object.fromEntries(Array.from(this._domains).map(([d, e]) => [d, e.delayMs])),
    };
  }

  shutdown() {
    this._domains.clear();
  }
}
