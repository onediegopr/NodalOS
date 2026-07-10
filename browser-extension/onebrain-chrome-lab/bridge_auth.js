(() => {
  if (typeof globalThis.fetch !== 'function' ||
      typeof chrome === 'undefined' ||
      !chrome.storage ||
      !chrome.storage.local) {
    return;
  }

  const nativeFetch = globalThis.fetch.bind(globalThis);
  const TOKEN_HEADER = 'X-Nodal-Bridge-Token';
  const DEFAULTS = { host: '127.0.0.1', port: '8787', token: '' };

  function normalizeHost(value) {
    const host = String(value || '').trim().toLowerCase();
    return host === 'localhost' ? 'localhost' : host;
  }

  function effectivePort(url) {
    if (url.port) {
      return url.port;
    }
    return url.protocol === 'https:' ? '443' : '80';
  }

  function requestUrl(input) {
    if (typeof input === 'string' || input instanceof URL) {
      return new URL(input.toString());
    }
    if (typeof Request !== 'undefined' && input instanceof Request) {
      return new URL(input.url);
    }
    return null;
  }

  async function authenticatedBridgeFetch(input, init = {}) {
    let url;
    try {
      url = requestUrl(input);
    } catch {
      return nativeFetch(input, init);
    }

    if (!url || (url.protocol !== 'http:' && url.protocol !== 'https:')) {
      return nativeFetch(input, init);
    }

    const config = await chrome.storage.local.get(DEFAULTS);
    const configuredHost = normalizeHost(config.host || DEFAULTS.host);
    const configuredPort = String(config.port || DEFAULTS.port);
    if (normalizeHost(url.hostname) !== configuredHost || effectivePort(url) !== configuredPort) {
      return nativeFetch(input, init);
    }

    const token = String(config.token || '').trim();
    if (!token) {
      return nativeFetch(input, init);
    }

    const sourceHeaders = init.headers ||
      (typeof Request !== 'undefined' && input instanceof Request ? input.headers : undefined);
    const headers = new Headers(sourceHeaders || {});
    headers.set(TOKEN_HEADER, token);

    if (typeof Request !== 'undefined' && input instanceof Request) {
      return nativeFetch(new Request(input, { ...init, headers }));
    }

    return nativeFetch(input, { ...init, headers });
  }

  globalThis.fetch = authenticatedBridgeFetch;
})();
