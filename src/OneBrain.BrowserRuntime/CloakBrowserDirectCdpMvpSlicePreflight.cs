using System.Security.Cryptography;
using System.Text.Json;

namespace OneBrain.BrowserRuntime;

public sealed record CloakBrowserDirectCdpMvpSlicePreflightResult(
    string Decision,
    string RuntimeClassification,
    string BinaryClassification,
    string RuntimeVersion,
    string ExpectedBinarySha256,
    string ActualBinarySha256Status,
    bool LockValid,
    bool RuntimeArtifactPinned,
    bool RuntimeArtifactConfigured,
    bool RuntimeArtifactPresent,
    bool RuntimeArtifactHashMatches,
    bool LaunchAllowed,
    bool CdpDirectMode,
    bool SystemBrowserAllowed,
    bool ExtensionEnabled,
    bool SystemBrowserFallbackRejected,
    bool PlaywrightDefaultRejected,
    bool ChromeLabExtensionRejectedAsRuntime,
    bool RuntimeSmokeAvailable,
    bool EvidenceSanitized,
    string Blocker,
    string NextStep);

public sealed class CloakBrowserDirectCdpMvpSlicePreflight
{
    public CloakBrowserDirectCdpMvpSlicePreflightResult Evaluate(
        string repositoryRoot,
        string lockfilePath,
        string? runtimeArtifactPath = null,
        IDictionary<string, string?>? environment = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(lockfilePath);

        BrowserRuntimeLock runtimeLock;
        try
        {
            runtimeLock = BrowserRuntimeLock.Load(lockfilePath);
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidOperationException)
        {
            return Blocked(
                decision: "BLOCKED_CLOAKBROWSER_RUNTIME_LOCK_INVALID",
                runtimeClassification: "DESIGN_ONLY",
                binaryClassification: "EXTERNAL_BLOCKER",
                runtimeVersion: "unknown",
                expectedHash: "unknown",
                blocker: "Runtime lock load failed.",
                nextStep: "FIX_BROWSER_RUNTIME_LOCKFILE");
        }

        var lockValidation = runtimeLock.Validate();
        if (!lockValidation.IsValid)
        {
            return Blocked(
                decision: "BLOCKED_CLOAKBROWSER_RUNTIME_LOCK_INVALID",
                runtimeClassification: "DESIGN_ONLY",
                binaryClassification: "EXTERNAL_BLOCKER",
                runtimeVersion: runtimeLock.RuntimeVersion,
                expectedHash: runtimeLock.BinarySha256,
                blocker: string.Join(",", lockValidation.Errors),
                nextStep: "FIX_BROWSER_RUNTIME_LOCKFILE",
                lockValid: false,
                artifactPinned: runtimeLock.HasPinnedRuntimeArtifact,
                cdpDirectMode: runtimeLock.Mode.Equals("cdp-direct", StringComparison.OrdinalIgnoreCase),
                systemBrowserAllowed: runtimeLock.SystemBrowserAllowed,
                extensionEnabled: runtimeLock.ExtensionEnabled);
        }

        var localConfig = BrowserRuntimeLocalConfig.Discover(
            repositoryRoot,
            environment ?? Environment.GetEnvironmentVariables()
                .Cast<System.Collections.DictionaryEntry>()
                .ToDictionary(entry => (string)entry.Key, entry => entry.Value?.ToString()));

        var resolvedArtifactPath = string.IsNullOrWhiteSpace(runtimeArtifactPath)
            ? localConfig.CloakBrowserExecutablePath
            : runtimeArtifactPath;
        var artifactConfigured = !string.IsNullOrWhiteSpace(resolvedArtifactPath);
        var artifactPresent = artifactConfigured && File.Exists(resolvedArtifactPath);
        var actualHashStatus = "NOT_EVALUATED";
        var hashMatches = false;
        if (artifactPresent)
        {
            var actualHash = HashFile(resolvedArtifactPath!);
            hashMatches = actualHash.Equals(runtimeLock.BinarySha256, StringComparison.OrdinalIgnoreCase);
            actualHashStatus = hashMatches ? "MATCH" : "MISMATCH";
        }

        var launchGuard = BrowserRuntimeGuard.ValidateLaunch(
            new BrowserRuntimeLaunchRequest(ExecutablePath: resolvedArtifactPath),
            runtimeLock);
        var systemBrowserRejected = !BrowserRuntimeGuard.ValidateLaunch(
                new BrowserRuntimeLaunchRequest(ExecutablePath: @"C:\Program Files\Google\Chrome\Application\chrome.exe"),
                runtimeLock)
            .IsAllowed;
        var playwrightRejected = !BrowserRuntimeGuard.ValidateLaunch(
                new BrowserRuntimeLaunchRequest(UsesPlaywrightDefaultChromium: true),
                runtimeLock)
            .IsAllowed;
        var chromeLabRejected = IsChromeLabExtensionPath("browser-extension/onebrain-chrome-lab/manifest.json");

        if (!runtimeLock.HasPinnedRuntimeArtifact)
        {
            return Result(
                decision: "BLOCKED_CLOAKBROWSER_RUNTIME_NOT_PINNED",
                runtimeClassification: "DESIGN_ONLY",
                binaryClassification: "EXTERNAL_BLOCKER",
                runtimeLock,
                lockValid: true,
                artifactConfigured,
                artifactPresent,
                hashMatches,
                actualHashStatus,
                launchAllowed: false,
                systemBrowserRejected,
                playwrightRejected,
                chromeLabRejected,
                blocker: "Pinned CloakBrowser runtime metadata is missing.",
                nextStep: "PIN_CLOAKBROWSER_RUNTIME_VERSION_COMMIT_AND_HASH");
        }

        if (!artifactPresent)
        {
            return Result(
                decision: "BLOCKED_EXTERNAL_CLOAKBROWSER_BINARY",
                runtimeClassification: "REAL_PINNED_RUNTIME",
                binaryClassification: artifactConfigured ? "CONFIGURED_BUT_MISSING_BINARY" : "EXTERNAL_BLOCKER",
                runtimeLock,
                lockValid: true,
                artifactConfigured,
                artifactPresent,
                hashMatches,
                actualHashStatus,
                launchAllowed: false,
                systemBrowserRejected,
                playwrightRejected,
                chromeLabRejected,
                blocker: BrowserRuntimeDefaults.RuntimeArtifactRequiredReason,
                nextStep: "PROVIDE_PINNED_CLOAKBROWSER_BINARY_AND_RERUN_LIVE_SMOKE");
        }

        if (!hashMatches)
        {
            return Result(
                decision: "BLOCKED_CLOAKBROWSER_BINARY_HASH_MISMATCH",
                runtimeClassification: "REAL_PINNED_RUNTIME",
                binaryClassification: "CONFIGURED_BUT_HASH_MISMATCH",
                runtimeLock,
                lockValid: true,
                artifactConfigured,
                artifactPresent,
                hashMatches,
                actualHashStatus,
                launchAllowed: false,
                systemBrowserRejected,
                playwrightRejected,
                chromeLabRejected,
                blocker: "Pinned CloakBrowser artifact hash mismatch.",
                nextStep: "REPLACE_BINARY_WITH_PINNED_HASH_OR_UPDATE_LOCK_AFTER_AUDIT");
        }

        return Result(
            decision: launchGuard.IsAllowed
                ? "GO_CLOAKBROWSER_DIRECT_CDP_MVP_SLICE_PREFLIGHT_READY"
                : "BLOCKED_CLOAKBROWSER_RUNTIME_GUARD",
            runtimeClassification: "REAL_PINNED_RUNTIME",
            binaryClassification: launchGuard.IsAllowed ? "AVAILABLE_LOCAL_BINARY" : "CONFIGURED_BUT_GUARD_BLOCKED",
            runtimeLock,
            lockValid: true,
            artifactConfigured,
            artifactPresent,
            hashMatches,
            actualHashStatus,
            launchAllowed: launchGuard.IsAllowed,
            systemBrowserRejected,
            playwrightRejected,
            chromeLabRejected,
            blocker: launchGuard.IsAllowed ? string.Empty : launchGuard.Reason,
            nextStep: launchGuard.IsAllowed ? "RUN_CLOAKBROWSER_CDP_LIVE_SMOKE" : "FIX_RUNTIME_GUARD_BLOCKER");
    }

    public static bool IsChromeLabExtensionPath(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && path.Contains("onebrain-chrome-lab", StringComparison.OrdinalIgnoreCase);

    private static CloakBrowserDirectCdpMvpSlicePreflightResult Result(
        string decision,
        string runtimeClassification,
        string binaryClassification,
        BrowserRuntimeLock runtimeLock,
        bool lockValid,
        bool artifactConfigured,
        bool artifactPresent,
        bool hashMatches,
        string actualHashStatus,
        bool launchAllowed,
        bool systemBrowserRejected,
        bool playwrightRejected,
        bool chromeLabRejected,
        string blocker,
        string nextStep) =>
        new(
            Decision: decision,
            RuntimeClassification: runtimeClassification,
            BinaryClassification: binaryClassification,
            RuntimeVersion: runtimeLock.RuntimeVersion,
            ExpectedBinarySha256: runtimeLock.BinarySha256,
            ActualBinarySha256Status: actualHashStatus,
            LockValid: lockValid,
            RuntimeArtifactPinned: runtimeLock.HasPinnedRuntimeArtifact,
            RuntimeArtifactConfigured: artifactConfigured,
            RuntimeArtifactPresent: artifactPresent,
            RuntimeArtifactHashMatches: hashMatches,
            LaunchAllowed: launchAllowed,
            CdpDirectMode: runtimeLock.Mode.Equals("cdp-direct", StringComparison.OrdinalIgnoreCase),
            SystemBrowserAllowed: runtimeLock.SystemBrowserAllowed,
            ExtensionEnabled: runtimeLock.ExtensionEnabled,
            SystemBrowserFallbackRejected: systemBrowserRejected,
            PlaywrightDefaultRejected: playwrightRejected,
            ChromeLabExtensionRejectedAsRuntime: chromeLabRejected,
            RuntimeSmokeAvailable: launchAllowed,
            EvidenceSanitized: true,
            Blocker: blocker,
            NextStep: nextStep);

    private static CloakBrowserDirectCdpMvpSlicePreflightResult Blocked(
        string decision,
        string runtimeClassification,
        string binaryClassification,
        string runtimeVersion,
        string expectedHash,
        string blocker,
        string nextStep,
        bool lockValid = false,
        bool artifactPinned = false,
        bool cdpDirectMode = false,
        bool systemBrowserAllowed = false,
        bool extensionEnabled = false) =>
        new(
            Decision: decision,
            RuntimeClassification: runtimeClassification,
            BinaryClassification: binaryClassification,
            RuntimeVersion: runtimeVersion,
            ExpectedBinarySha256: expectedHash,
            ActualBinarySha256Status: "NOT_EVALUATED",
            LockValid: lockValid,
            RuntimeArtifactPinned: artifactPinned,
            RuntimeArtifactConfigured: false,
            RuntimeArtifactPresent: false,
            RuntimeArtifactHashMatches: false,
            LaunchAllowed: false,
            CdpDirectMode: cdpDirectMode,
            SystemBrowserAllowed: systemBrowserAllowed,
            ExtensionEnabled: extensionEnabled,
            SystemBrowserFallbackRejected: true,
            PlaywrightDefaultRejected: true,
            ChromeLabExtensionRejectedAsRuntime: true,
            RuntimeSmokeAvailable: false,
            EvidenceSanitized: true,
            Blocker: blocker,
            NextStep: nextStep);

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
