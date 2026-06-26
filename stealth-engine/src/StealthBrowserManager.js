import pLimit from 'p-limit';
import { StealthSession } from './StealthSession.js';
import { FingerprintGenerator, FingerprintProfile } from './fingerprint/FingerprintProfile.js';
import { ProxyManager } from './proxy/ProxyManager.js';

export class StealthBrowserManager {
  constructor(options = {}) {
    this.maxConcurrent = options.maxConcurrentSessions || 5;
    this.queueTimeoutMs = options.sessionQueueTimeoutMs || 120000;
    this.activeSessions = new Map();
    this.queue = [];
    this.limiter = pLimit(this.maxConcurrent);
    this.stats = {
      totalCreated: 0,
      totalDestroyed: 0,
      avgSessionDurationMs: 0,
      sessionDurations: [],
    };
    this.SessionClass = StealthSession;
    this.fingerprintGenerator = FingerprintGenerator;
    this.proxyManager = options.proxyManager || new ProxyManager([], {});
    this.config = options.config || {};
  }

  async createSession(taskId, instruction, options = {}) {
    const now = Date.now();

    const limitedCreate = () => this._doCreateSession(taskId, instruction, options);

    if (this.activeSessions.size >= this.maxConcurrent) {
      return new Promise((resolve, reject) => {
        const queued = { taskId, instruction, options, resolve, reject, queuedAt: now };
        this.queue.push(queued);
        console.log(`[SessionManager] Task ${taskId} queued (${this.queue.length} ahead)`);
        const timeout = setTimeout(() => {
          const idx = this.queue.indexOf(queued);
          if (idx >= 0) { this.queue.splice(idx, 1); reject(new Error('Queue timeout')); }
        }, this.queueTimeoutMs);
        queued._timeout = timeout;
      });
    }

    return this.limiter(limitedCreate);
  }

  async _doCreateSession(taskId, instruction, options) {
    const startTime = Date.now();
    let proxy = null;

    if (this.proxyManager && this.config.proxy?.enabled) {
      proxy = this.proxyManager.acquire(taskId, { sticky: this.config.proxy?.rotationMode !== 'random' });
    }

    let profile;
    if (options.fingerprintProfile) {
      profile = FingerprintProfile.ensureCoherence(options.fingerprintProfile, proxy?.country);
    } else {
      profile = this.fingerprintGenerator.generate({
        preset: this.config.fingerprint?.defaultPreset,
        country: proxy?.country,
      });
    }

    const session = new this.SessionClass({
      taskId, instruction, profile,
      proxy: proxy ? { server: proxy.url || proxy.server, username: proxy.username, password: proxy.password } : null,
      proxyId: proxy?.id || null,
      behaviorProfile: this.config.behavior?.defaultProfile || 'casual',
      tlsFingerprint: this.config.tlsFingerprint || { enabled: false },
    });

    await session.initialize();
    this.activeSessions.set(taskId, { session, startTime });
    this.stats.totalCreated++;

    console.log(`[SessionManager] Task ${taskId} session created (active: ${this.activeSessions.size}, max: ${this.maxConcurrent})`);

    this._processQueue();
    return session;
  }

  async destroySession(taskId) {
    const entry = this.activeSessions.get(taskId);
    if (entry) {
      this.activeSessions.delete(taskId);
      const durationMs = Date.now() - entry.startTime;
      this.stats.totalDestroyed++;
      this.stats.sessionDurations.push(durationMs);
      if (this.stats.sessionDurations.length > 200) this.stats.sessionDurations.shift();
      this.stats.avgSessionDurationMs = Math.round(
        this.stats.sessionDurations.reduce((a, b) => a + b, 0) / this.stats.sessionDurations.length
      );

      try { await entry.session.dispose(); } catch (e) { }
      this.proxyManager.release(taskId);
      console.log(`[SessionManager] Task ${taskId} session destroyed (${durationMs}ms)`);
    }
    this._processQueue();
  }

  _processQueue() {
    if (this.queue.length === 0) return;
    if (this.activeSessions.size >= this.maxConcurrent) return;
    const next = this.queue.shift();
    clearTimeout(next._timeout);
    this.limiter(() => this._doCreateSession(next.taskId, next.instruction, next.options))
      .then(session => next.resolve(session))
      .catch(err => next.reject(err));
  }

  getSession(taskId) {
    return this.activeSessions.get(taskId)?.session || null;
  }

  getStats() {
    return {
      active: this.activeSessions.size,
      queued: this.queue.length,
      max: this.maxConcurrent,
      totalCreated: this.stats.totalCreated,
      totalDestroyed: this.stats.totalDestroyed,
      avgSessionDurationMs: this.stats.avgSessionDurationMs,
      proxyPool: this.proxyManager?.getStats?.() || {},
    };
  }

  async shutdown() {
    for (const [taskId, entry] of this.activeSessions) {
      try { await entry.session.dispose(); } catch (e) { }
    }
    this.activeSessions.clear();
    this.queue.forEach(q => { clearTimeout(q._timeout); q.reject(new Error('Shutdown')); });
    this.queue = [];
  }
}
