export class DomainBlacklist {
  constructor(maxSize = 100) {
    this.maxSize = maxSize;
    this.list = new Map();
  }

  record(domain, reason) {
    this.list.set(domain, {
      domain,
      reason,
      recordedAt: new Date().toISOString(),
      hitCount: (this.list.get(domain)?.hitCount || 0) + 1,
    });

    if (this.list.size > this.maxSize) {
      const oldest = [...this.list.entries()]
        .sort(([, a], [, b]) => new Date(a.recordedAt) - new Date(b.recordedAt))[0];
      if (oldest) this.list.delete(oldest[0]);
    }
  }

  isBlacklisted(domain) {
    return this.list.has(domain);
  }

  getStats() {
    return {
      size: this.list.size,
      maxSize: this.maxSize,
      domains: [...this.list.values()],
    };
  }
}
