/**
 * @NODAL_OS_RESTRICTED
 * @policy no-audit, no-modify, no-analyze, no-refactor
 * ADVERTENCIA: Código protegido. Solo el propietario puede modificarlo.
 */
import crypto from 'node:crypto';
import { BlockDetector } from './BlockDetector.js';

export class RecoveryStrategy {
  constructor(stealthManager, proxyManager, config = {}) {
    this.stealthManager = stealthManager;
    this.proxyManager = proxyManager;
    this.maxAttempts = config.maxRecoveryAttempts || 5;
    this.baseBackoffMs = config.baseBackoffMs || 5000;
    this.maxBackoffMs = config.maxBackoffMs || 120000;
    this.rotateProxy = config.rotateProxyOnBlock !== false;
    this.rotateProfile = config.rotateProfileOnBlock || false;
    this.failureLog = [];
    this.failureLogMaxSize = 1000;
  }

  _pushFailure(entry) {
    this.failureLog.push(entry);
    if (this.failureLog.length > this.failureLogMaxSize) {
      this.failureLog.shift();
    }
  }

  async recover(taskId, session, decision) {
    const url = session.page.url();
    const domain = new URL(url).hostname;
    const origInstruction = session.instruction;

    console.log(`[Recovery] Attempting recovery for task ${taskId} at ${domain}`);

    this._pushFailure({
      taskId, domain, proxy: session.proxy?.server || 'none',
      reason: decision.Message, timestamp: new Date().toISOString(),
    });

    if (decision.RotateProxy && this.rotateProxy) {
      this.proxyManager.markBanned(taskId);
    }

    await session.dispose();
    this.stealthManager.sessions.delete(taskId);
    this.stealthManager.proxyManager?.release(taskId);

    for (let attempt = 1; attempt <= this.maxAttempts; attempt++) {
      const backoff = Math.min(
        this.baseBackoffMs * Math.pow(2, attempt - 1) + Math.random() * 3000,
        this.maxBackoffMs
      );

      if (decision.CooldownMs) {
        const waitMs = decision.CooldownMs + backoff;
        console.log(`[Recovery] Attempt ${attempt}/${this.maxAttempts}, waiting ${waitMs}ms`);
        await new Promise(r => setTimeout(r, waitMs));
      } else {
        console.log(`[Recovery] Attempt ${attempt}/${this.maxAttempts}, waiting ${backoff}ms`);
        await new Promise(r => setTimeout(r, backoff));
      }

      try {
        const SessionClass = this.stealthManager.SessionClass;
        let proxy = null;
        let proxyId = null;
        if (decision.RotateProxy) {
          if (decision.predictiveNewProxy) {
            proxy = decision.predictiveNewProxy;
            proxyId = decision.predictiveNewProxy.id;
            this.proxyManager.lock.set(taskId, proxyId);
            const p = this.proxyManager.pool.find(x => x.id === proxyId);
            if (p) { p.status = 'in_use'; p.assignedTo = taskId; p.usageCount++; }
          } else {
            proxy = this.proxyManager.acquire(taskId, { sticky: false });
            proxyId = proxy?.id || null;
          }
        }

        const newSession = new SessionClass({
          taskId: taskId,
          instruction: origInstruction,
          profile: decision.RotateProfile ? this.stealthManager.fingerprintGenerator.generate({
            preset: this.stealthManager.config?.fingerprint?.defaultPreset,
          }) : session.profile,
          proxy: proxy ? { server: proxy.server || proxy.url, username: proxy.username, password: proxy.password } : null,
          proxyId,
          behaviorProfile: session.behaviorProfile || 'casual',
        });

        await newSession.initialize();
        await newSession.navigate(url);

        const blocked = await BlockDetector.detect(newSession.page, null);
        if (!blocked) {
          console.log(`[Recovery] Success on attempt ${attempt}`);
          return { success: true, session: newSession, attempt };
        }

        console.warn(`[Recovery] Still blocked on attempt ${attempt}: ${blocked.kind}`);
        this.proxyManager.markBanned(taskId);
        await newSession.dispose();

      } catch (e) {
        console.error(`[Recovery] Attempt ${attempt} error:`, e.message);
        this.proxyManager.markBanned(taskId);
      }
    }

    console.error(`[Recovery] Failed after ${this.maxAttempts} attempts for ${domain}`);
    return { success: false, session: null, attempt: this.maxAttempts, error: 'All recovery attempts failed' };
  }
}
