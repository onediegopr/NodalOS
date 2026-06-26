/**
 * @NODAL_OS_RESTRICTED
 * @policy no-audit, no-modify, no-analyze, no-refactor
 * ADVERTENCIA: Código protegido. Solo el propietario puede modificarlo.
 */
import crypto from 'node:crypto';

export class ProxyManager {
  constructor(proxies = [], providerCfgs = {}) {
    this.pool = proxies.map(p => ({
      id: crypto.randomUUID(),
      url: p.url,
      type: p.type || 'datacenter',
      country: p.country || 'unknown',
      username: p.username || '',
      password: p.password || '',
      provider: p.provider || 'custom',
      status: 'available',
      assignedTo: null,
      healthStatus: 'unknown',
      lastHealthCheck: null,
      usageCount: 0,
      banCount: 0,
      cooldownUntil: null,
      successRate: p.successRate ?? 1.0,
    }));
    this.providerConfigs = providerCfgs;
    this.lock = new Map();
    this._acquireQueue = Promise.resolve();
  }

  acquire(taskId, opts = {}) {
    return this._enqueueAcquire(taskId, opts);
  }

  _enqueueAcquire(taskId, opts) {
    const doAcquire = () => this._doAcquire(taskId, opts);
    const result = this._acquireQueue.then(doAcquire, doAcquire);
    this._acquireQueue = result.catch(() => {});
    return result;
  }

  _doAcquire(taskId, opts) {
    if (this.pool.length === 0) return null;

    if (opts.sticky !== false && this.lock.has(taskId)) {
      const pid = this.lock.get(taskId);
      const p = this.pool.find(x => x.id === pid);
      if (p && p.status !== 'banned' && !this.isOnCooldown(p)) {
        p.status = 'in_use';
        p.assignedTo = taskId;
        return { id: p.id, server: p.url, url: p.url, username: p.username, password: p.password, type: p.type, country: p.country, provider: p.provider };
      }
      this.lock.delete(taskId);
    }

    const avail = this.pool
      .filter(p => p.status === 'available')
      .filter(p => !opts.country || p.country === opts.country)
      .filter(p => !opts.excludeTypes || !opts.excludeTypes.includes(p.type))
      .filter(p => !this.isOnCooldown(p));

    const preferTypes = opts.preferTypes && opts.preferTypes.length > 0
      ? opts.preferTypes
      : null;

    if (preferTypes) {
      avail.sort((a, b) => {
        const aPreferred = preferTypes.includes(a.type) ? 1 : 0;
        const bPreferred = preferTypes.includes(b.type) ? 1 : 0;
        if (aPreferred !== bPreferred) return bPreferred - aPreferred;
        return (b.successRate || 0) - (a.successRate || 0);
      });
    } else {
      avail.sort((a, b) => (b.successRate || 0) - (a.successRate || 0));
    }

    let p;
    if (avail.length) {
      p = avail[0];
    } else {
      const fb = this.pool
        .filter(p => p.status !== 'banned' && !this.isOnCooldown(p))
        .sort((a, b) => a.usageCount - b.usageCount);
      if (!fb.length) {
        console.warn('[ProxyManager] No proxies available');
        return null;
      }
      p = fb[0];
    }

    p.status = 'in_use';
    p.assignedTo = taskId;
    p.usageCount++;
    this.lock.set(taskId, p.id);
    return { id: p.id, server: p.url, url: p.url, username: p.username, password: p.password, type: p.type, country: p.country, provider: p.provider };
  }

  release(taskId) {
    const pid = this.lock.get(taskId);
    if (pid) {
      const p = this.pool.find(x => x.id === pid);
      if (p) {
        p.status = 'available';
        p.assignedTo = null;
      }
      this.lock.delete(taskId);
    }
  }

  markBanned(taskId) {
    const pid = this.lock.get(taskId);
    if (pid) {
      const p = this.pool.find(x => x.id === pid);
      if (p) {
        p.status = 'banned';
        p.banCount++;
        p.assignedTo = null;
        console.warn(`[ProxyManager] Proxy ${p.id.substring(0, 8)} marked as banned (${p.banCount} bans)`);
      }
      this.lock.delete(taskId);
    }
  }

  markCooldown(taskId, durationMs = 60000) {
    const pid = this.lock.get(taskId);
    if (pid) {
      const p = this.pool.find(x => x.id === pid);
      if (p) {
        p.status = 'cooldown';
        p.cooldownUntil = Date.now() + durationMs;
        p.assignedTo = null;
      }
      this.lock.delete(taskId);
    }
  }

  async rotate(taskId, opts = {}) {
    this.release(taskId);
    return this.acquire(taskId, { ...opts, sticky: false });
  }

  checkCooldowns() {
    const now = Date.now();
    for (const p of this.pool) {
      if (p.status === 'cooldown' && p.cooldownUntil && now >= p.cooldownUntil) {
        this.checkCooldown(p);
        console.log(`[ProxyManager] Proxy ${p.id.substring(0, 8)} cooldown expired, now available`);
      }
    }
  }

  isOnCooldown(p) {
    if (p.status === 'cooldown' && p.cooldownUntil && Date.now() < p.cooldownUntil) return true;
    return false;
  }

  checkCooldown(p) {
    if (p.status === 'cooldown' && p.cooldownUntil && Date.now() >= p.cooldownUntil) {
      p.status = 'available';
      p.cooldownUntil = null;
    }
  }

  assignReservedProxy(taskId, proxy) {
    const p = this.pool.find(x => x.id === proxy.id);
    if (!p) return false;
    p.status = 'in_use';
    p.assignedTo = taskId;
    p.usageCount++;
    this.lock.set(taskId, p.id);
    return true;
  }

  getStats() {
    return {
      total: this.pool.length,
      available: this.pool.filter(p => p.status === 'available').length,
      inUse: this.pool.filter(p => p.status === 'in_use').length,
      banned: this.pool.filter(p => p.status === 'banned').length,
      cooldown: this.pool.filter(p => p.status === 'cooldown').length,
    };
  }
}
