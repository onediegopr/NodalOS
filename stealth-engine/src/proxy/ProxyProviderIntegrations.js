export class ProxyProviderIntegrations {
  static async fetchFromBrightData(cfg) {
    if (!cfg.enabled || !cfg.apiKey) return [];
    try {
      const resp = await fetch(cfg.endpoint || 'https://api.brightdata.com/zone/get_proxy_ips?zone=res', {
        headers: { 'Authorization': 'Bearer ' + cfg.apiKey, 'Content-Type': 'application/json' },
      });
      const data = await resp.json();
      if (!Array.isArray(data)) return [];
      return data.map(ip => ({
        url: `http://${ip}:${cfg.port || 22225}`,
        type: 'residential',
        country: 'US',
        provider: 'brightdata',
        username: cfg.username || '',
        password: cfg.password || '',
      }));
    } catch (e) {
      console.warn('[ProxyProvider] BrightData fetch failed:', e.message);
      return [];
    }
  }

  static async fetchFromOxylabs(cfg) {
    if (!cfg.enabled || !cfg.username) return [];
    try {
      const endpoint = cfg.endpoint || 'https://realtime.oxylabs.io/v1/queries';
      const resp = await fetch(endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Authorization': 'Basic ' + Buffer.from(cfg.username + ':' + cfg.password).toString('base64') },
        body: JSON.stringify({ source: 'universal', url: 'http://httpbin.org/ip', geo_location: 'United States' }),
      });
      return [];
    } catch (e) {
      console.warn('[ProxyProvider] Oxylabs fetch failed:', e.message);
      return [];
    }
  }

  static async fetchFromIPRoyal(cfg) {
    if (!cfg.enabled || !cfg.apiKey) return [];
    return [];
  }

  static async fetchAll(providerCfgs = {}) {
    const results = await Promise.allSettled([
      ProxyProviderIntegrations.fetchFromBrightData(providerCfgs.brightdata || {}),
      ProxyProviderIntegrations.fetchFromOxylabs(providerCfgs.oxylabs || {}),
      ProxyProviderIntegrations.fetchFromIPRoyal(providerCfgs.iproyal || {}),
    ]);
    const proxies = [];
    for (const r of results) {
      if (r.status === 'fulfilled' && Array.isArray(r.value)) proxies.push(...r.value);
    }
    return proxies;
  }
}
