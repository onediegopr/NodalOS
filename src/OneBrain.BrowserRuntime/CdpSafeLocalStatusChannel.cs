using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneBrain.BrowserRuntime;

public enum CdpSafeLocalStatusChannelStatus
{
    Ready,
    MissingEvidence,
    MissingSnapshot,
    InvalidSnapshot,
    Error
}

public sealed record CdpSafeLocalStatusSnapshot(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    string Channel,
    string Source,
    string RuntimeProvider,
    string RuntimeStatus,
    bool ArtifactHashVerified,
    string Freshness,
    DateTimeOffset? LastHealthcheckAt,
    DateTimeOffset? LastSessionAt,
    string LastEvidenceName,
    bool EvidenceAvailable,
    bool CaptureOk,
    bool ScreenshotCaptured,
    int InteractiveElements,
    int FrictionSignals,
    int ActionMapEntries,
    bool RuntimeShutdown,
    bool ProcessExited,
    bool OrphanProcessDetected,
    bool ExtensionUsed,
    bool SystemBrowserUsed,
    bool BoundaryReadOnly,
    bool RuntimeLaunchedFromUi,
    bool CdpLiveExecutedFromUi,
    bool ExternalNavigationBlocked,
    bool ProductFilesModified);

public sealed record CdpSafeLocalStatusChannelError(
    string Code,
    string Message);

public sealed record CdpSafeLocalStatusChannelResult(
    CdpSafeLocalStatusChannelStatus Status,
    CdpSafeLocalStatusSnapshot? Snapshot,
    bool SnapshotWritten,
    bool SnapshotRead,
    string SnapshotName,
    bool RuntimeLaunched,
    bool CdpLiveExecuted,
    bool ExtensionUsed,
    bool SystemBrowserUsed,
    bool ProductFilesModified,
    bool BoundaryReadOnly,
    CdpSafeLocalStatusChannelError? Error);

public sealed class CdpSafeLocalStatusSnapshotWriter
{
    public const string SchemaVersion = "1.0";
    public const string Channel = "safe-local-status-snapshot";
    public const string FileName = "cdp-status.snapshot.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly CdpUiStatusRefreshCommand refreshCommand;

    public CdpSafeLocalStatusSnapshotWriter()
        : this(new CdpUiStatusRefreshCommand())
    {
    }

    public CdpSafeLocalStatusSnapshotWriter(CdpUiStatusRefreshCommand refreshCommand)
    {
        this.refreshCommand = refreshCommand;
    }

    public CdpSafeLocalStatusChannelResult Export(
        CdpUiStatusRefreshRequest request,
        string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var refresh = refreshCommand.Refresh(request);
        var snapshot = FromRefresh(refresh);
        return WriteSnapshot(snapshot, outputPath, refresh.Status);
    }

    public CdpSafeLocalStatusChannelResult WriteSnapshot(
        CdpSafeLocalStatusSnapshot snapshot,
        string outputPath,
        CdpUiStatusRefreshStatus refreshStatus = CdpUiStatusRefreshStatus.Refreshed)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        File.WriteAllText(outputPath, JsonSerializer.Serialize(snapshot, JsonOptions));

        return BuildResult(
            refreshStatus switch
            {
                CdpUiStatusRefreshStatus.MissingEvidence => CdpSafeLocalStatusChannelStatus.MissingEvidence,
                CdpUiStatusRefreshStatus.Error => CdpSafeLocalStatusChannelStatus.Error,
                _ => CdpSafeLocalStatusChannelStatus.Ready
            },
            snapshot,
            snapshotWritten: true,
            snapshotRead: false,
            snapshotName: Path.GetFileName(outputPath),
            error: refreshStatus == CdpUiStatusRefreshStatus.Error
                ? new CdpSafeLocalStatusChannelError("SNAPSHOT_SOURCE_ERROR", "Snapshot was written from an error status refresh.")
                : null);
    }

    public static CdpSafeLocalStatusSnapshot FromRefresh(CdpUiStatusRefreshResult refresh)
    {
        ArgumentNullException.ThrowIfNull(refresh);

        var status = refresh.StatusAfter;
        var evidence = status.BrowserSkillsSession ?? status.BrowserSkills ?? status.Healthcheck;
        return new CdpSafeLocalStatusSnapshot(
            SchemaVersion: SchemaVersion,
            GeneratedAt: refresh.CompletedAt,
            Channel: Channel,
            Source: "cloakbrowser-cdp-direct",
            RuntimeProvider: "cloakbrowser",
            RuntimeStatus: status.Status,
            ArtifactHashVerified: string.Equals(status.HashVerifiedStatus, "verificado", StringComparison.OrdinalIgnoreCase),
            Freshness: status.Freshness.ToString(),
            LastHealthcheckAt: status.LastHealthcheckAt,
            LastSessionAt: status.LastBrowserSkillsSessionAt,
            LastEvidenceName: status.LastEvidenceName,
            EvidenceAvailable: status.EvidenceAvailable,
            CaptureOk: evidence?.CaptureOk ?? false,
            ScreenshotCaptured: evidence?.ScreenshotCaptured ?? false,
            InteractiveElements: evidence?.InteractiveElementCount ?? 0,
            FrictionSignals: evidence?.FrictionSignalCount ?? 0,
            ActionMapEntries: evidence?.ActionMapCount ?? 0,
            RuntimeShutdown: status.RuntimeShutdown,
            ProcessExited: status.ProcessExited,
            OrphanProcessDetected: status.OrphanProcessDetected,
            ExtensionUsed: false,
            SystemBrowserUsed: false,
            BoundaryReadOnly: true,
            RuntimeLaunchedFromUi: false,
            CdpLiveExecutedFromUi: false,
            ExternalNavigationBlocked: true,
            ProductFilesModified: false);
    }

    internal static CdpSafeLocalStatusChannelResult BuildResult(
        CdpSafeLocalStatusChannelStatus status,
        CdpSafeLocalStatusSnapshot? snapshot,
        bool snapshotWritten,
        bool snapshotRead,
        string snapshotName,
        CdpSafeLocalStatusChannelError? error) =>
        new(
            Status: status,
            Snapshot: snapshot,
            SnapshotWritten: snapshotWritten,
            SnapshotRead: snapshotRead,
            SnapshotName: snapshotName,
            RuntimeLaunched: false,
            CdpLiveExecuted: false,
            ExtensionUsed: false,
            SystemBrowserUsed: false,
            ProductFilesModified: false,
            BoundaryReadOnly: true,
            Error: error);
}

public sealed class CdpSafeLocalStatusSnapshotReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CdpSafeLocalStatusChannelResult ReadSnapshot(string snapshotPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotPath);

        if (!File.Exists(snapshotPath))
        {
            return CdpSafeLocalStatusSnapshotWriter.BuildResult(
                CdpSafeLocalStatusChannelStatus.MissingSnapshot,
                snapshot: null,
                snapshotWritten: false,
                snapshotRead: false,
                snapshotName: Path.GetFileName(snapshotPath),
                error: new CdpSafeLocalStatusChannelError("SNAPSHOT_MISSING", "Safe local CDP status snapshot was not found."));
        }

        try
        {
            var snapshot = JsonSerializer.Deserialize<CdpSafeLocalStatusSnapshot>(
                File.ReadAllText(snapshotPath),
                JsonOptions);
            var validationError = Validate(snapshot);
            if (validationError is not null)
            {
                return CdpSafeLocalStatusSnapshotWriter.BuildResult(
                    CdpSafeLocalStatusChannelStatus.InvalidSnapshot,
                    snapshot: null,
                    snapshotWritten: false,
                    snapshotRead: true,
                    snapshotName: Path.GetFileName(snapshotPath),
                    error: validationError);
            }

            return CdpSafeLocalStatusSnapshotWriter.BuildResult(
                snapshot!.EvidenceAvailable ? CdpSafeLocalStatusChannelStatus.Ready : CdpSafeLocalStatusChannelStatus.MissingEvidence,
                snapshot,
                snapshotWritten: false,
                snapshotRead: true,
                snapshotName: Path.GetFileName(snapshotPath),
                error: null);
        }
        catch (JsonException exception)
        {
            return CdpSafeLocalStatusSnapshotWriter.BuildResult(
                CdpSafeLocalStatusChannelStatus.InvalidSnapshot,
                snapshot: null,
                snapshotWritten: false,
                snapshotRead: true,
                snapshotName: Path.GetFileName(snapshotPath),
                error: new CdpSafeLocalStatusChannelError("SNAPSHOT_INVALID_JSON", exception.Message));
        }
        catch (IOException exception)
        {
            return CdpSafeLocalStatusSnapshotWriter.BuildResult(
                CdpSafeLocalStatusChannelStatus.Error,
                snapshot: null,
                snapshotWritten: false,
                snapshotRead: false,
                snapshotName: Path.GetFileName(snapshotPath),
                error: new CdpSafeLocalStatusChannelError("SNAPSHOT_READ_ERROR", exception.Message));
        }
    }

    private static CdpSafeLocalStatusChannelError? Validate(CdpSafeLocalStatusSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return new CdpSafeLocalStatusChannelError("SNAPSHOT_EMPTY", "Safe local CDP status snapshot was empty.");
        }

        if (!string.Equals(snapshot.SchemaVersion, CdpSafeLocalStatusSnapshotWriter.SchemaVersion, StringComparison.Ordinal))
        {
            return new CdpSafeLocalStatusChannelError("SNAPSHOT_SCHEMA_VERSION_INVALID", "Safe local CDP status snapshot schema version is not supported.");
        }

        if (!string.Equals(snapshot.Channel, CdpSafeLocalStatusSnapshotWriter.Channel, StringComparison.Ordinal))
        {
            return new CdpSafeLocalStatusChannelError("SNAPSHOT_CHANNEL_INVALID", "Safe local CDP status snapshot channel is not supported.");
        }

        if (!string.Equals(snapshot.Source, "cloakbrowser-cdp-direct", StringComparison.Ordinal))
        {
            return new CdpSafeLocalStatusChannelError("SNAPSHOT_SOURCE_INVALID", "Safe local CDP status snapshot source is not supported.");
        }

        if (!string.Equals(snapshot.RuntimeProvider, "cloakbrowser", StringComparison.Ordinal))
        {
            return new CdpSafeLocalStatusChannelError("SNAPSHOT_PROVIDER_INVALID", "Safe local CDP status snapshot provider is not supported.");
        }

        if (snapshot.ExtensionUsed || snapshot.SystemBrowserUsed || snapshot.RuntimeLaunchedFromUi || snapshot.CdpLiveExecutedFromUi || snapshot.ProductFilesModified)
        {
            return new CdpSafeLocalStatusChannelError("SNAPSHOT_UNSAFE_FLAGS", "Safe local CDP status snapshot contains unsafe runtime flags.");
        }

        return null;
    }
}
