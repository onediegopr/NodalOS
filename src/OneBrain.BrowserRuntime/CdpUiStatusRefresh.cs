namespace OneBrain.BrowserRuntime;

public enum CdpUiStatusRefreshSource
{
    LocalRedactedEvidence,
    LockfileRuntimeMetadata,
    LiveStatusAdapterCacheModel,
    ExtensionFallback,
    SystemBrowserFallback,
    RuntimeLaunchFromUi,
    BridgeWebSocketProtectedChannel
}

public enum CdpUiStatusRefreshStatus
{
    Refreshed,
    MissingEvidence,
    Error
}

public sealed record CdpUiStatusRefreshRequest(
    string RepositoryRoot,
    string LockfilePath,
    CdpUiLiveStatusSnapshot? StatusBefore = null,
    DateTimeOffset? RequestedAt = null,
    DateTimeOffset? Now = null,
    TimeSpan? FreshWithin = null);

public sealed record CdpUiStatusRefreshError(
    string Code,
    string Message);

public sealed record CdpUiStatusRefreshResult(
    DateTimeOffset RequestedAt,
    DateTimeOffset CompletedAt,
    CdpUiStatusRefreshStatus Status,
    CdpUiLiveStatusSnapshot? StatusBefore,
    CdpUiLiveStatusSnapshot StatusAfter,
    IReadOnlyList<CdpUiStatusRefreshSource> Sources,
    bool EvidenceRead,
    bool RuntimeLaunched,
    bool CdpLiveExecuted,
    bool ExtensionUsed,
    bool SystemBrowserUsed,
    bool ProductFilesModified,
    bool BoundaryReadOnly,
    bool ExternalNavigationBlocked,
    bool DangerousActionBlocked,
    CdpUiStatusRefreshError? Error);

public sealed class CdpUiStatusRefreshCommand
{
    private static readonly CdpUiStatusRefreshSource[] AllowedRefreshSources =
    [
        CdpUiStatusRefreshSource.LocalRedactedEvidence,
        CdpUiStatusRefreshSource.LockfileRuntimeMetadata,
        CdpUiStatusRefreshSource.LiveStatusAdapterCacheModel
    ];

    private readonly CdpUiLiveStatusAdapter liveStatusAdapter;

    public CdpUiStatusRefreshCommand()
        : this(new CdpUiLiveStatusAdapter())
    {
    }

    public CdpUiStatusRefreshCommand(CdpUiLiveStatusAdapter liveStatusAdapter)
    {
        this.liveStatusAdapter = liveStatusAdapter;
    }

    public CdpUiStatusRefreshResult Refresh(CdpUiStatusRefreshRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RepositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.LockfilePath);

        var requestedAt = request.RequestedAt ?? DateTimeOffset.UtcNow;
        var refreshed = liveStatusAdapter.Build(new CdpUiLiveStatusRequest(
            request.RepositoryRoot,
            request.LockfilePath,
            request.Now ?? requestedAt,
            request.FreshWithin));
        var snapshot = refreshed.Snapshot;
        var status = snapshot.Freshness switch
        {
            CdpUiLiveStatusFreshness.Missing => CdpUiStatusRefreshStatus.MissingEvidence,
            CdpUiLiveStatusFreshness.Error => CdpUiStatusRefreshStatus.Error,
            _ => CdpUiStatusRefreshStatus.Refreshed
        };
        var error = status == CdpUiStatusRefreshStatus.Error
            ? new CdpUiStatusRefreshError(
                "EVIDENCE_REFRESH_ERROR",
                "No se pudo actualizar el estado CDP desde evidencia redactada local.")
            : null;

        return new CdpUiStatusRefreshResult(
            RequestedAt: requestedAt,
            CompletedAt: request.Now ?? DateTimeOffset.UtcNow,
            Status: status,
            StatusBefore: request.StatusBefore,
            StatusAfter: snapshot,
            Sources: AllowedRefreshSources,
            EvidenceRead: snapshot.EvidenceAvailable && snapshot.Freshness != CdpUiLiveStatusFreshness.Error,
            RuntimeLaunched: false,
            CdpLiveExecuted: false,
            ExtensionUsed: false,
            SystemBrowserUsed: false,
            ProductFilesModified: false,
            BoundaryReadOnly: true,
            ExternalNavigationBlocked: true,
            DangerousActionBlocked: false,
            Error: error);
    }

    public static bool IsAllowedSource(CdpUiStatusRefreshSource source) =>
        source is CdpUiStatusRefreshSource.LocalRedactedEvidence
            or CdpUiStatusRefreshSource.LockfileRuntimeMetadata
            or CdpUiStatusRefreshSource.LiveStatusAdapterCacheModel;

    public static string BuildCopySummary(CdpUiStatusRefreshResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return string.Join('\n',
            "NODAL OS - Browser Skills CDP status refresh",
            $"lastRefreshAt: {result.CompletedAt:O}",
            "refreshSource: local-redacted-evidence",
            $"status: {result.StatusAfter.Status}",
            $"freshness: {result.StatusAfter.Freshness}",
            $"evidenceRead: {result.EvidenceRead}",
            $"evidenceAvailable: {result.StatusAfter.EvidenceAvailable}",
            $"runtimeLaunched: {result.RuntimeLaunched}",
            $"cdpLiveExecuted: {result.CdpLiveExecuted}",
            $"extensionUsed: {result.ExtensionUsed}",
            $"systemBrowserUsed: {result.SystemBrowserUsed}",
            $"boundaryReadOnly: {result.BoundaryReadOnly}",
            $"externalNavigationBlocked: {result.ExternalNavigationBlocked}",
            $"productFilesModified: {result.ProductFilesModified}",
            $"runtimeShutdown: {result.StatusAfter.RuntimeShutdown}",
            $"processExited: {result.StatusAfter.ProcessExited}",
            $"orphanProcessDetected: {result.StatusAfter.OrphanProcessDetected}");
    }
}
