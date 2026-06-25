export class ProxyHealthChecker {
  constructor(proxyManager, intervalMs = 60000) {
    this.manager = proxyManager;
    this.intervalMs = intervalMs;
    this.timer = null;
    this.running = false;
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

    for (const p of this.manager.pool) {
      if (p.status === 'banned' || p.status === 'cooldown') continue;
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
    }
  }
}
