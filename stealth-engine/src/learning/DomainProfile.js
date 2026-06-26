/**
 * @NODAL_OS_RESTRICTED
 * @policy no-audit, no-modify, no-analyze, no-refactor
 * ADVERTENCIA: Código protegido. Solo el propietario puede modificarlo.
 */
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { existsSync } from 'node:fs';
import { dirname } from 'node:path';

export class DomainProfile {
  constructor(config = {}) {
    this.filePath = config.filePath || './src/data/domain-profiles.json';
    this.maxDomains = config.maxDomains || 500;
    this.ttlMs = config.ttlMs || 30 * 24 * 60 * 60 * 1000; // 30 días
    this.profiles = new Map();
    this.loaded = false;
  }

  async load() {
    if (this.loaded) return;
    try {
      if (existsSync(this.filePath)) {
        const data = await readFile(this.filePath, 'utf-8');
        const parsed = JSON.parse(data);
        if (parsed.profiles && Array.isArray(parsed.profiles)) {
          for (const p of parsed.profiles) {
            if (p.domain) this.profiles.set(p.domain, p);
          }
        }
      }
    } catch (e) {
      console.warn('[DomainProfile] Could not load profiles:', e.message);
    }
    this.loaded = true;
  }

  async save() {
    try {
      await mkdir(dirname(this.filePath), { recursive: true });
      const payload = {
        updatedAt: new Date().toISOString(),
        profiles: Array.from(this.profiles.values()),
      };
      await writeFile(this.filePath, JSON.stringify(payload, null, 2));
    } catch (e) {
      console.warn('[DomainProfile] Could not save profiles:', e.message);
    }
  }

  _cleanup() {
    const now = Date.now();
    for (const [domain, profile] of this.profiles) {
      if (profile.lastUsed && now - new Date(profile.lastUsed).getTime() > this.ttlMs) {
        this.profiles.delete(domain);
      }
    }
  }

  _ensureSize() {
    if (this.profiles.size <= this.maxDomains) return;
    const sorted = Array.from(this.profiles.entries()).sort((a, b) => {
      return new Date(a[1].lastUsed || 0).getTime() - new Date(b[1].lastUsed || 0).getTime();
    });
    const toRemove = sorted.slice(0, sorted.length - this.maxDomains);
    for (const [domain] of toRemove) this.profiles.delete(domain);
  }

  get(domain) {
    this._cleanup();
    return this.profiles.get(domain) || null;
  }

  update(domain, result = {}) {
    this._cleanup();
    const now = new Date().toISOString();
    let profile = this.profiles.get(domain) || {
      domain,
      captchaType: null,
      preferredProxyType: null,
      preferredBehaviorProfile: null,
      successRate: 0.5,
      uses: 0,
      successes: 0,
      failures: 0,
      lastUsed: now,
      firstSeen: now,
    };

    profile.uses++;
    profile.lastUsed = now;
    if (result.success) profile.successes++;
    else profile.failures++;

    if (result.captchaType) profile.captchaType = result.captchaType;
    if (result.proxyType) profile.preferredProxyType = result.proxyType;
    if (result.behaviorProfile) profile.preferredBehaviorProfile = result.behaviorProfile;

    profile.successRate = profile.uses > 0 ? profile.successes / profile.uses : 0.5;

    this.profiles.set(domain, profile);
    this._ensureSize();
    this.save().catch(() => {});
    return profile;
  }

  suggest(domain, defaults = {}) {
    const profile = this.get(domain);
    if (!profile) return defaults;
    return {
      proxyType: profile.preferredProxyType || defaults.proxyType,
      behaviorProfile: profile.preferredBehaviorProfile || defaults.behaviorProfile,
      captchaType: profile.captchaType || defaults.captchaType,
    };
  }
}
