/**
 * @NODAL_OS_RESTRICTED
 * @policy no-audit, no-modify, no-analyze, no-refactor
 * ADVERTENCIA: Código protegido. Solo el propietario puede modificarlo.
 */
export class ProxyProviderIntegrations {
  static async fetchFromBrightData(cfg) {
    if (!cfg.enabled || !cfg.apiKey) return [];
    if (!cfg.zone) {
      console.warn('[ProxyProvider] BrightData zone not configured');
      return [];
    }
    try {
      const resp = await fetch(`https://api.brightdata.com/v1/zone/get_proxy_ips?zone=${encodeURIComponent(cfg.zone)}`, {
        headers: { 'Authorization': 'Bearer ' + cfg.apiKey, 'Content-Type': 'application/json' },
      });
      if (!resp.ok) {
        console.warn('[ProxyProvider] BrightData fetch failed:', resp.status, resp.statusText);
        return [];
      }
      const data = await resp.json();
      if (!Array.isArray(data)) return [];
      return data.map(entry => ({
        url: `http://${entry.ip || entry.host}:${entry.port || cfg.port || 22225}`,
        type: 'residential',
        country: entry.country || entry.country_code || 'US',
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
    if (!cfg.enabled || !cfg.username || !cfg.password) return [];
    try {
      const endpoint = cfg.endpoint || 'https://proxy.oxylabs.io/credentials/' + encodeURIComponent(cfg.username) + '/whitelist';
      const resp = await fetch(endpoint, {
        headers: { 'Authorization': 'Basic ' + Buffer.from(cfg.username + ':' + cfg.password).toString('base64') },
      });
      if (!resp.ok) {
        console.warn('[ProxyProvider] Oxylabs fetch failed:', resp.status, resp.statusText);
        return [];
      }
      const data = await resp.json();
      const proxies = [];
      const seenUrls = new Set();
      if (data.access_whitelist && Array.isArray(data.access_whitelist)) {
        const proxyUrl = `http://${cfg.username}:${cfg.password}@proxy.oxylabs.io:8010`;
        if (!seenUrls.has(proxyUrl)) {
          seenUrls.add(proxyUrl);
          proxies.push({
            url: proxyUrl,
            type: 'residential',
            country: cfg.country || 'US',
            provider: 'oxylabs',
            username: cfg.username,
            password: cfg.password,
          });
        }
        if (data.access_whitelist.length > 1) {
          console.warn(`[ProxyProvider] Oxylabs: ${data.access_whitelist.length} whitelisted IPs share same proxy endpoint, single entry created`);
        }
      }
      return proxies;
    } catch (e) {
      console.warn('[ProxyProvider] Oxylabs fetch failed:', e.message);
      return [];
    }
  }

  static async fetchFromIPRoyal(cfg) {
    if (!cfg.enabled || !cfg.apiKey) return [];
    try {
      const endpoint = cfg.endpoint || 'https://panel.iproyal.com/api/residential/export';
      const resp = await fetch(endpoint, {
        headers: { 'Authorization': 'Bearer ' + cfg.apiKey, 'Content-Type': 'application/json' },
      });
      if (!resp.ok) {
        console.warn('[ProxyProvider] IPRoyal fetch failed:', resp.status, resp.statusText);
        return [];
      }
      const text = await resp.text();
      const lines = text.trim().split('\n').filter(Boolean);
      return lines.map(line => {
        const [host, port, user, pass, country] = line.split(':');
        return {
          url: `http://${user}:${pass}@${host}:${port}`,
          type: 'residential',
          country: country || 'US',
          provider: 'iproyal',
          username: user,
          password: pass,
        };
      }).filter(p => p.url.includes('@'));
    } catch (e) {
      console.warn('[ProxyProvider] IPRoyal fetch failed:', e.message);
      return [];
    }
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
