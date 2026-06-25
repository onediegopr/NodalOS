namespace OneBrain.BrowserRuntime;

public sealed record CloakBrowserRuntimeStatus(
    bool RuntimeConfigured,
    bool RuntimeArtifactPresent,
    bool LaunchAllowed,
    bool CdpDirectMode,
    bool SystemBrowserAllowed,
    string State,
    string Reason);

public sealed class CloakBrowserRuntimeProvider
{
    public CloakBrowserRuntimeStatus GetStatus(BrowserRuntimeLock runtimeLock, string? runtimeArtifactPath = null)
    {
        ArgumentNullException.ThrowIfNull(runtimeLock);

        var validation = runtimeLock.Validate();
        if (!validation.IsValid)
        {
            return new CloakBrowserRuntimeStatus(
                RuntimeConfigured: false,
                RuntimeArtifactPresent: false,
                LaunchAllowed: false,
                CdpDirectMode: runtimeLock.Mode.Equals("cdp-direct", StringComparison.OrdinalIgnoreCase),
                SystemBrowserAllowed: runtimeLock.SystemBrowserAllowed,
                State: "BLOCKED_LOCK_INVALID",
                Reason: string.Join(",", validation.Errors));
        }

        var artifactPresent = IsRuntimeArtifactPresent(runtimeLock, runtimeArtifactPath);
        if (!artifactPresent)
        {
            return new CloakBrowserRuntimeStatus(
                RuntimeConfigured: true,
                RuntimeArtifactPresent: false,
                LaunchAllowed: false,
                CdpDirectMode: true,
                SystemBrowserAllowed: false,
                State: "BLOCKED_RUNTIME_ARTIFACT_REQUIRED",
                Reason: BrowserRuntimeDefaults.RuntimeArtifactRequiredReason);
        }

        var guard = BrowserRuntimeGuard.ValidateLaunch(
            new BrowserRuntimeLaunchRequest(ExecutablePath: runtimeArtifactPath),
            runtimeLock);

        return new CloakBrowserRuntimeStatus(
            RuntimeConfigured: true,
            RuntimeArtifactPresent: true,
            LaunchAllowed: guard.IsAllowed,
            CdpDirectMode: true,
            SystemBrowserAllowed: false,
            State: guard.IsAllowed ? "READY_CDP_DIRECT" : "BLOCKED_BY_RUNTIME_GUARD",
            Reason: guard.Reason);
    }

    private static bool IsRuntimeArtifactPresent(BrowserRuntimeLock runtimeLock, string? runtimeArtifactPath)
    {
        if (!runtimeLock.HasPinnedRuntimeArtifact || string.IsNullOrWhiteSpace(runtimeArtifactPath))
        {
            return false;
        }

        return File.Exists(runtimeArtifactPath);
    }
}
