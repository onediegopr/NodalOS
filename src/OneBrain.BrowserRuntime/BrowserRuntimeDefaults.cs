namespace OneBrain.BrowserRuntime;

public static class BrowserRuntimeDefaults
{
    public const string RuntimeEnvironmentVariable = "NODAL_BROWSER_RUNTIME";
    public const string DefaultRuntime = "cloakbrowser";
    public const bool ExtensionDefaultRuntime = false;
    public const bool ExtensionLegacyUi = true;
    public const bool CdpDirectRuntimeDefault = true;
    public const string RuntimeArtifactRequiredReason = "CloakBrowser runtime artifact required.";
}
