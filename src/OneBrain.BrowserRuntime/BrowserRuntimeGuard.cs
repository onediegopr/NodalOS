namespace OneBrain.BrowserRuntime;

public sealed record BrowserRuntimeLaunchRequest(
    string? ExecutablePath = null,
    string? PlaywrightChannel = null,
    bool UsesPlaywrightDefaultChromium = false,
    string? RequestedInstallerCommand = null);

public sealed record BrowserRuntimeGuardResult(bool IsAllowed, string Reason)
{
    public static BrowserRuntimeGuardResult Allowed { get; } = new(true, "Allowed");

    public static BrowserRuntimeGuardResult Rejected(string reason) => new(false, reason);
}

public static class BrowserRuntimeGuard
{
    private static readonly string[] RejectedExecutableNames =
    [
        "chrome.exe",
        "msedge.exe",
        "chromium.exe",
        "google-chrome",
        "microsoft-edge",
        "chromium"
    ];

    private static readonly string[] RejectedPathFragments =
    [
        "\\Google\\Chrome\\",
        "/google/chrome/",
        "\\Microsoft\\Edge\\",
        "/microsoft/edge/",
        "\\Chromium\\Application\\",
        "/chromium/"
    ];

    public static BrowserRuntimeGuardResult ValidateLaunch(BrowserRuntimeLaunchRequest request, BrowserRuntimeLock runtimeLock)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(runtimeLock);

        var lockValidation = runtimeLock.Validate();
        if (!lockValidation.IsValid)
        {
            return BrowserRuntimeGuardResult.Rejected(string.Join(",", lockValidation.Errors));
        }

        if (request.UsesPlaywrightDefaultChromium)
        {
            return BrowserRuntimeGuardResult.Rejected("PlaywrightDefaultChromiumRejected");
        }

        if (IsRejectedChannel(request.PlaywrightChannel))
        {
            return BrowserRuntimeGuardResult.Rejected("PlaywrightSystemBrowserChannelRejected");
        }

        if (IsPlaywrightChromiumInstall(request.RequestedInstallerCommand))
        {
            return BrowserRuntimeGuardResult.Rejected("PlaywrightChromiumInstallRejected");
        }

        if (IsRejectedExecutablePath(request.ExecutablePath, runtimeLock))
        {
            return BrowserRuntimeGuardResult.Rejected("SystemBrowserExecutableRejected");
        }

        return BrowserRuntimeGuardResult.Allowed;
    }

    public static bool IsRejectedExecutablePath(string? executablePath, BrowserRuntimeLock runtimeLock)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return false;
        }

        var normalized = executablePath.Replace('/', Path.DirectorySeparatorChar);
        var fileName = Path.GetFileName(normalized);
        var hasRejectedName = RejectedExecutableNames.Any(name => fileName.Equals(name, StringComparison.OrdinalIgnoreCase));
        var hasRejectedPath = RejectedPathFragments.Any(fragment =>
            normalized.Contains(fragment.Replace('/', Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase));

        if (!hasRejectedName && !hasRejectedPath)
        {
            return false;
        }

        return !IsPinnedCloakBrowserRuntime(normalized, runtimeLock);
    }

    public static bool IsRejectedChannel(string? channel) =>
        channel is not null
        && (channel.Equals("chrome", StringComparison.OrdinalIgnoreCase)
            || channel.Equals("msedge", StringComparison.OrdinalIgnoreCase)
            || channel.Equals("edge", StringComparison.OrdinalIgnoreCase));

    public static bool IsPlaywrightChromiumInstall(string? command) =>
        command is not null
        && command.Contains("playwright", StringComparison.OrdinalIgnoreCase)
        && command.Contains("install", StringComparison.OrdinalIgnoreCase)
        && command.Contains("chromium", StringComparison.OrdinalIgnoreCase);

    private static bool IsPinnedCloakBrowserRuntime(string executablePath, BrowserRuntimeLock runtimeLock)
    {
        if (!runtimeLock.HasPinnedRuntimeArtifact)
        {
            return false;
        }

        return executablePath.Contains("cloakbrowser", StringComparison.OrdinalIgnoreCase)
            || executablePath.Contains(runtimeLock.RuntimeRepo, StringComparison.OrdinalIgnoreCase);
    }
}
