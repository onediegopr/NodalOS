namespace OneBrain.BrowserRuntime;

public sealed record CdpExtensionDeprecationHardeningRequest(
    string RepositoryRoot,
    string LockfilePath,
    string NoExtensionDefaultHarnessPath,
    string MinimalProductSurfaceScriptPath,
    string InstalledSidepanelHarnessPath);

public sealed record CdpExtensionDeprecationHardeningResult(
    bool IsHardened,
    string DefaultRuntime,
    string DefaultHarness,
    string MinimalProductSurface,
    string ExtensionMode,
    bool ExtensionDefaultRuntime,
    bool InstalledSidepanelHarnessDefault,
    bool ExtensionFallbackAllowed,
    bool SystemBrowserFallbackAllowed,
    bool BridgeWebSocketRequired,
    bool RuntimeLaunchFromUiAllowed,
    bool CdpLiveFromUiAllowed,
    bool ProductFilesModified,
    IReadOnlyList<string> Errors);

public sealed class CdpExtensionDeprecationHardening
{
    public const string DefaultRuntime = "cloakbrowser-cdp-direct";
    public const string DefaultHarness = "cloakbrowser-cdp-no-extension";
    public const string MinimalProductSurface = "minimal-no-extension-runtime-bridge";
    public const string ExtensionMode = "legacy/no-default";

    public CdpExtensionDeprecationHardeningResult Evaluate(CdpExtensionDeprecationHardeningRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RepositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.LockfilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.NoExtensionDefaultHarnessPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.MinimalProductSurfaceScriptPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InstalledSidepanelHarnessPath);

        var errors = new List<string>();
        var runtimeLock = BrowserRuntimeLock.Load(request.LockfilePath);
        var lockValidation = runtimeLock.Validate();
        if (!lockValidation.IsValid)
        {
            errors.AddRange(lockValidation.Errors);
        }

        if (runtimeLock.ExtensionEnabled)
        {
            errors.Add("ExtensionEnabledMustRemainFalse");
        }

        if (runtimeLock.SystemBrowserAllowed)
        {
            errors.Add("SystemBrowserAllowedMustRemainFalse");
        }

        var noExtensionHarness = ReadRequiredFile(request.NoExtensionDefaultHarnessPath, "NoExtensionDefaultHarnessMissing", errors);
        var minimalSurface = ReadRequiredFile(request.MinimalProductSurfaceScriptPath, "MinimalProductSurfaceMissing", errors);
        var installedSidepanelHarness = ReadRequiredFile(request.InstalledSidepanelHarnessPath, "InstalledSidepanelHarnessMissing", errors);

        Require(noExtensionHarness, "defaultHarness = \"cloakbrowser-cdp-no-extension\"", "DefaultHarnessMustBeNoExtension", errors);
        Require(noExtensionHarness, "installedSidepanelHarnessUsed = $false", "DefaultHarnessMustNotUseInstalledSidepanel", errors);
        Require(noExtensionHarness, "extensionOpened = $false", "DefaultHarnessMustNotOpenExtension", errors);
        Require(noExtensionHarness, "extensionUsed = $false", "DefaultHarnessMustNotUseExtension", errors);
        Require(noExtensionHarness, "systemBrowserUsed = $false", "DefaultHarnessMustNotUseSystemBrowser", errors);
        Require(noExtensionHarness, "fallbackUsed = $false", "DefaultHarnessMustNotUseFallback", errors);

        Require(minimalSurface, "productSurface = \"minimal-no-extension-runtime-bridge\"", "MinimalProductSurfaceMustBeNoExtension", errors);
        Require(minimalSurface, "extensionRequired = $false", "MinimalProductSurfaceMustNotRequireExtension", errors);
        Require(minimalSurface, "installedSidepanelHarnessUsed = $false", "MinimalProductSurfaceMustNotUseInstalledSidepanel", errors);
        Require(minimalSurface, "runtimeLaunchedFromSurface = $false", "MinimalProductSurfaceMustNotLaunchRuntime", errors);
        Require(minimalSurface, "cdpLiveExecutedFromSurface = $false", "MinimalProductSurfaceMustNotExecuteCdpLive", errors);
        Require(minimalSurface, "bridgeWebSocketUsed = $false", "MinimalProductSurfaceMustNotUseBridgeWebSocket", errors);

        Require(installedSidepanelHarness, "browserRuntimeDefault: 'cloakbrowser-cdp-no-extension'", "InstalledSidepanelMustDeclareCdpDefault", errors);
        Require(installedSidepanelHarness, "defaultRuntimeHarness: 'legacy-installed-sidepanel-compat-only'", "InstalledSidepanelMustBeLegacyCompatOnly", errors);
        Require(installedSidepanelHarness, "installedSidepanelHarnessDefault: false", "InstalledSidepanelMustNotBeDefaultHarness", errors);
        Require(installedSidepanelHarness, "extensionRuntimeDefault: false", "ExtensionMustNotBeRuntimeDefault", errors);

        return new CdpExtensionDeprecationHardeningResult(
            IsHardened: errors.Count == 0,
            DefaultRuntime: DefaultRuntime,
            DefaultHarness: DefaultHarness,
            MinimalProductSurface: MinimalProductSurface,
            ExtensionMode: ExtensionMode,
            ExtensionDefaultRuntime: false,
            InstalledSidepanelHarnessDefault: false,
            ExtensionFallbackAllowed: false,
            SystemBrowserFallbackAllowed: false,
            BridgeWebSocketRequired: false,
            RuntimeLaunchFromUiAllowed: false,
            CdpLiveFromUiAllowed: false,
            ProductFilesModified: false,
            Errors: errors);
    }

    private static string ReadRequiredFile(string path, string errorCode, List<string> errors)
    {
        if (!File.Exists(path))
        {
            errors.Add(errorCode);
            return string.Empty;
        }

        return File.ReadAllText(path);
    }

    private static void Require(string text, string marker, string errorCode, List<string> errors)
    {
        if (!text.Contains(marker, StringComparison.Ordinal))
        {
            errors.Add(errorCode);
        }
    }
}
