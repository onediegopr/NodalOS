/**
 * @NODAL_OS_RESTRICTED
 * @policy no-audit, no-modify, no-analyze, no-refactor
 * ADVERTENCIA: Código protegido. Solo el propietario puede modificarlo.
 */
import pLimit from 'p-limit';

export class ProxyHealthChecker {
  constructor(proxyManager, intervalMs = 60000, maxConcurrency = 10) {
    this.manager = proxyManager;
    this.intervalMs = intervalMs;
    this.maxConcurrency = maxConcurrency;
    this.timer = null;
    this.running = false;
    this.limiter = pLimit(maxConcurrency);
  }

  start() {
    if (this.running) return;
    this.running = true;
    this.check();
    this.timer = setInterval(() => this.check(), this.intervalMs);
    console.log('[ProxyHealthChecker] Started (interval: ' + this.intervalMs + 'ms)');
  }

  stop() {
    this.running = false;
    if (this.timer) { clearInterval(this.timer); this.timer = null; }
  }

  async check() {
    this.manager.checkCooldowns();
    if (this.manager.pool.length === 0) return;

    const checkOne = async (p) => {
      if (p.status === 'banned' || p.status === 'cooldown') return;
      try {
        const controller = new AbortController();
        const timeout = setTimeout(() => controller.abort(), 10000);
        const resp = await fetch('http://httpbin.org/ip', { signal: controller.signal });
        clearTimeout(timeout);

        p.lastHealthCheck = new Date().toISOString();
        if (resp.ok) {
          p.healthStatus = 'healthy';
          p.successRate = Math.min(1, (p.successRate || 0.7) + 0.05);
        } else {
          p.healthStatus = 'degraded';
          p.successRate = Math.max(0, (p.successRate || 0.5) - 0.1);
        }
      } catch (e) {
        p.healthStatus = 'dead';
        p.successRate = Math.max(0, (p.successRate || 0.5) - 0.2);
        p.lastHealthCheck = new Date().toISOString();
      }
    };

    await Promise.all(this.manager.pool.map(p => this.limiter(() => checkOne(p))));
  }
}
