namespace OneBrain.BrowserRuntime;

public sealed class CdpInjectionManager
{
    public const string MarkerName = "__NODAL_OS_CDP_INJECTED__";
    public const string BootstrapVersion = "cdp-bootstrap-v1";

    public string BuildBootstrapScript()
    {
        return $$"""
(() => {
  if (window.{{MarkerName}} === true) {
    return {
      injected: true,
      alreadyInjected: true,
      version: window.__NODAL_OS_CDP_BOOTSTRAP_VERSION__ || '{{BootstrapVersion}}'
    };
  }

  Object.defineProperty(window, '{{MarkerName}}', {
    value: true,
    enumerable: false,
    configurable: false,
    writable: false
  });

  Object.defineProperty(window, '__NODAL_OS_CDP_BOOTSTRAP_VERSION__', {
    value: '{{BootstrapVersion}}',
    enumerable: false,
    configurable: false,
    writable: false
  });

  window.__NODAL_OS_CDP_PAGE_METADATA__ = () => ({
    url: String(window.location.href || ''),
    title: String(document.title || ''),
    readyState: String(document.readyState || 'unknown'),
    timestamp: new Date().toISOString()
  });

  return {
    injected: true,
    alreadyInjected: false,
    version: '{{BootstrapVersion}}',
    readyState: String(document.readyState || 'unknown')
  };
})();
""";
    }

    public bool ContainsDoubleInjectionGuard(string script) =>
        script.Contains($"window.{MarkerName} === true", StringComparison.Ordinal)
        && script.Contains("alreadyInjected: true", StringComparison.Ordinal);
}
