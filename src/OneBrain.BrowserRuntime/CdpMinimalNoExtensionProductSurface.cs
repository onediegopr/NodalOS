namespace OneBrain.BrowserRuntime;

public enum CdpMinimalNoExtensionProductSurfaceStatus
{
    Ready,
    MissingSnapshot,
    MissingEvidence,
    InvalidSnapshot,
    Error
}

public sealed record CdpMinimalNoExtensionProductSurfaceRequest(
    string SnapshotPath,
    DateTimeOffset? RequestedAt = null);

public sealed record CdpMinimalNoExtensionRuntimeSummary(
    string RuntimeLabel,
    string RuntimeProvider,
    string Source,
    string RuntimeStatus,
    bool ArtifactHashVerified,
    bool RuntimeShutdown,
    bool ProcessExited,
    bool OrphanProcessDetected);

public sealed record CdpMinimalNoExtensionBrowserSkillsSummary(
    bool CaptureOk,
    bool ScreenshotCaptured,
    int InteractiveElements,
    int FrictionSignals,
    int ActionMapEntries,
    bool MetadataOnly);

public sealed record CdpMinimalNoExtensionEvidenceSummary(
    bool EvidenceAvailable,
    string SnapshotName,
    string LastEvidenceName,
    string SnapshotChannel,
    string Freshness,
    DateTimeOffset? SnapshotGeneratedAt,
    DateTimeOffset? LastHealthcheckAt,
    DateTimeOffset? LastSessionAt);

public sealed record CdpMinimalNoExtensionProductSurfaceModel(
    string Title,
    string Surface,
    CdpMinimalNoExtensionRuntimeSummary Runtime,
    CdpMinimalNoExtensionBrowserSkillsSummary BrowserSkills,
    CdpMinimalNoExtensionEvidenceSummary Evidence,
    bool ReadOnly,
    bool ExtensionRequired,
    bool ExtensionOpened,
    bool InstalledSidepanelHarnessUsed,
    bool ExtensionUsed,
    bool SystemBrowserUsed,
    bool RuntimeLaunchedFromSurface,
    bool CdpLiveExecutedFromSurface,
    bool BridgeWebSocketUsed,
    bool ExternalNavigationBlocked,
    bool ProductFilesModified,
    bool FallbackUsed,
    bool RawDomStored,
    bool RawHtmlStored,
    bool InputValuesStored,
    bool CookiesOrStorageStored,
    bool SecretsStored);

public sealed record CdpMinimalNoExtensionProductSurfaceResult(
    CdpMinimalNoExtensionProductSurfaceStatus Status,
    CdpMinimalNoExtensionProductSurfaceModel Model,
    bool SnapshotRead,
    bool MetadataOnly,
    CdpSafeLocalStatusChannelError? Error);

public sealed class CdpMinimalNoExtensionProductSurfaceBridge
{
    private readonly CdpSafeLocalStatusSnapshotReader snapshotReader;

    public CdpMinimalNoExtensionProductSurfaceBridge()
        : this(new CdpSafeLocalStatusSnapshotReader())
    {
    }

    public CdpMinimalNoExtensionProductSurfaceBridge(CdpSafeLocalStatusSnapshotReader snapshotReader)
    {
        this.snapshotReader = snapshotReader;
    }

    public CdpMinimalNoExtensionProductSurfaceResult Build(CdpMinimalNoExtensionProductSurfaceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SnapshotPath);

        var read = snapshotReader.ReadSnapshot(request.SnapshotPath);
        var status = MapStatus(read.Status);
        var model = read.Snapshot is null
            ? BuildEmptyModel(
                request,
                Path.GetFileName(request.SnapshotPath),
                status == CdpMinimalNoExtensionProductSurfaceStatus.MissingSnapshot
                    ? "sin snapshot local"
                    : "snapshot no disponible")
            : BuildModel(read.Snapshot, read.SnapshotName);

        return new CdpMinimalNoExtensionProductSurfaceResult(
            Status: status,
            Model: model,
            SnapshotRead: read.SnapshotRead,
            MetadataOnly: true,
            Error: read.Error);
    }

    public static string BuildCopySummary(CdpMinimalNoExtensionProductSurfaceResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var model = result.Model;

        return string.Join('\n',
            "NODAL OS - Browser Skills CDP minimal no-extension surface",
            $"surface: {model.Surface}",
            $"status: {result.Status}",
            $"runtime: {model.Runtime.RuntimeLabel}",
            $"source: {model.Runtime.Source}",
            $"runtimeStatus: {model.Runtime.RuntimeStatus}",
            $"snapshotChannel: {model.Evidence.SnapshotChannel}",
            $"snapshotFreshness: {model.Evidence.Freshness}",
            $"evidenceAvailable: {model.Evidence.EvidenceAvailable}",
            $"interactiveElements: {model.BrowserSkills.InteractiveElements}",
            $"frictionSignals: {model.BrowserSkills.FrictionSignals}",
            $"actionMapEntries: {model.BrowserSkills.ActionMapEntries}",
            $"readOnly: {model.ReadOnly}",
            $"metadataOnly: {result.MetadataOnly}",
            $"extensionRequired: {model.ExtensionRequired}",
            $"extensionOpened: {model.ExtensionOpened}",
            $"installedSidepanelHarnessUsed: {model.InstalledSidepanelHarnessUsed}",
            $"extensionUsed: {model.ExtensionUsed}",
            $"systemBrowserUsed: {model.SystemBrowserUsed}",
            $"runtimeLaunchedFromSurface: {model.RuntimeLaunchedFromSurface}",
            $"cdpLiveExecutedFromSurface: {model.CdpLiveExecutedFromSurface}",
            $"bridgeWebSocketUsed: {model.BridgeWebSocketUsed}",
            $"externalNavigationBlocked: {model.ExternalNavigationBlocked}",
            $"productFilesModified: {model.ProductFilesModified}",
            $"fallbackUsed: {model.FallbackUsed}",
            $"rawDomStored: {model.RawDomStored}",
            $"rawHtmlStored: {model.RawHtmlStored}",
            $"inputValuesStored: {model.InputValuesStored}",
            $"cookiesOrStorageStored: {model.CookiesOrStorageStored}",
            $"secretsStored: {model.SecretsStored}");
    }

    private static CdpMinimalNoExtensionProductSurfaceModel BuildModel(
        CdpSafeLocalStatusSnapshot snapshot,
        string snapshotName) =>
        new(
            Title: "Browser Skills CDP",
            Surface: "minimal-no-extension-runtime-bridge",
            Runtime: new CdpMinimalNoExtensionRuntimeSummary(
                RuntimeLabel: "CloakBrowser CDP",
                RuntimeProvider: snapshot.RuntimeProvider,
                Source: snapshot.Source,
                RuntimeStatus: snapshot.RuntimeStatus,
                ArtifactHashVerified: snapshot.ArtifactHashVerified,
                RuntimeShutdown: snapshot.RuntimeShutdown,
                ProcessExited: snapshot.ProcessExited,
                OrphanProcessDetected: snapshot.OrphanProcessDetected),
            BrowserSkills: new CdpMinimalNoExtensionBrowserSkillsSummary(
                CaptureOk: snapshot.CaptureOk,
                ScreenshotCaptured: snapshot.ScreenshotCaptured,
                InteractiveElements: snapshot.InteractiveElements,
                FrictionSignals: snapshot.FrictionSignals,
                ActionMapEntries: snapshot.ActionMapEntries,
                MetadataOnly: true),
            Evidence: new CdpMinimalNoExtensionEvidenceSummary(
                EvidenceAvailable: snapshot.EvidenceAvailable,
                SnapshotName: Path.GetFileName(snapshotName),
                LastEvidenceName: Path.GetFileName(snapshot.LastEvidenceName),
                SnapshotChannel: snapshot.Channel,
                Freshness: snapshot.Freshness,
                SnapshotGeneratedAt: snapshot.GeneratedAt,
                LastHealthcheckAt: snapshot.LastHealthcheckAt,
                LastSessionAt: snapshot.LastSessionAt),
            ReadOnly: true,
            ExtensionRequired: false,
            ExtensionOpened: false,
            InstalledSidepanelHarnessUsed: false,
            ExtensionUsed: false,
            SystemBrowserUsed: false,
            RuntimeLaunchedFromSurface: false,
            CdpLiveExecutedFromSurface: false,
            BridgeWebSocketUsed: false,
            ExternalNavigationBlocked: true,
            ProductFilesModified: false,
            FallbackUsed: false,
            RawDomStored: false,
            RawHtmlStored: false,
            InputValuesStored: false,
            CookiesOrStorageStored: false,
            SecretsStored: false);

    private static CdpMinimalNoExtensionProductSurfaceModel BuildEmptyModel(
        CdpMinimalNoExtensionProductSurfaceRequest request,
        string snapshotName,
        string runtimeStatus) =>
        new(
            Title: "Browser Skills CDP",
            Surface: "minimal-no-extension-runtime-bridge",
            Runtime: new CdpMinimalNoExtensionRuntimeSummary(
                RuntimeLabel: "CloakBrowser CDP",
                RuntimeProvider: "cloakbrowser",
                Source: "cloakbrowser-cdp-direct",
                RuntimeStatus: runtimeStatus,
                ArtifactHashVerified: false,
                RuntimeShutdown: false,
                ProcessExited: false,
                OrphanProcessDetected: false),
            BrowserSkills: new CdpMinimalNoExtensionBrowserSkillsSummary(
                CaptureOk: false,
                ScreenshotCaptured: false,
                InteractiveElements: 0,
                FrictionSignals: 0,
                ActionMapEntries: 0,
                MetadataOnly: true),
            Evidence: new CdpMinimalNoExtensionEvidenceSummary(
                EvidenceAvailable: false,
                SnapshotName: Path.GetFileName(snapshotName),
                LastEvidenceName: "sin-evidencia-cdp",
                SnapshotChannel: CdpSafeLocalStatusSnapshotWriter.Channel,
                Freshness: "Missing",
                SnapshotGeneratedAt: request.RequestedAt,
                LastHealthcheckAt: null,
                LastSessionAt: null),
            ReadOnly: true,
            ExtensionRequired: false,
            ExtensionOpened: false,
            InstalledSidepanelHarnessUsed: false,
            ExtensionUsed: false,
            SystemBrowserUsed: false,
            RuntimeLaunchedFromSurface: false,
            CdpLiveExecutedFromSurface: false,
            BridgeWebSocketUsed: false,
            ExternalNavigationBlocked: true,
            ProductFilesModified: false,
            FallbackUsed: false,
            RawDomStored: false,
            RawHtmlStored: false,
            InputValuesStored: false,
            CookiesOrStorageStored: false,
            SecretsStored: false);

    private static CdpMinimalNoExtensionProductSurfaceStatus MapStatus(CdpSafeLocalStatusChannelStatus status) =>
        status switch
        {
            CdpSafeLocalStatusChannelStatus.Ready => CdpMinimalNoExtensionProductSurfaceStatus.Ready,
            CdpSafeLocalStatusChannelStatus.MissingEvidence => CdpMinimalNoExtensionProductSurfaceStatus.MissingEvidence,
            CdpSafeLocalStatusChannelStatus.MissingSnapshot => CdpMinimalNoExtensionProductSurfaceStatus.MissingSnapshot,
            CdpSafeLocalStatusChannelStatus.InvalidSnapshot => CdpMinimalNoExtensionProductSurfaceStatus.InvalidSnapshot,
            _ => CdpMinimalNoExtensionProductSurfaceStatus.Error
        };
}
