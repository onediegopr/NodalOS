using System.Text.Json;

namespace OneBrain.BrowserRuntime;

public enum CdpUiRuntimeCommandKind
{
    GetRuntimeStatus,
    GetLastBrowserSkillsSummary,
    GetLastEvidenceSummary,
    BuildUiBridgeModel,
    RefreshControlledCapture,
    NavigateExternal,
    SubmitForm,
    SolveCaptcha,
    UseCredentials,
    UploadFile,
    DownloadFile,
    ExecuteArbitraryJs,
    WriteFilesystem,
    ShellCommand,
    UseExtensionFallback,
    UseSystemBrowserFallback
}

public enum CdpUiRuntimeCommandStatus
{
    Success,
    Empty,
    Blocked,
    Failed
}

public sealed record CdpUiRuntimeCommandRequest(
    CdpUiRuntimeCommandKind CommandKind,
    string RepositoryRoot,
    string LockfilePath,
    string? RuntimeArtifactPath = null,
    string? SessionId = null,
    TimeSpan? Timeout = null);

public sealed record CdpUiRuntimeStatus(
    bool RuntimeConfigured,
    bool ArtifactPinned,
    string ShaStatus,
    bool CdpCapabilityAvailable,
    string LastHealthcheckStatus,
    string LastBrowserSkillsSessionStatus,
    string ExtensionMode,
    bool SystemBrowserAllowed,
    string RuntimeProvider,
    string Source);

public sealed record CdpUiRuntimeCommandError(
    string Code,
    string Message,
    bool DangerousActionBlocked);

public sealed record CdpUiRuntimeCommandEvidence(
    string Source,
    string RuntimeProvider,
    bool ReadOnly,
    bool ExtensionUsed,
    bool SystemBrowserUsed,
    bool ExternalNavigationBlocked,
    bool ProductFilesModified,
    bool DangerousActionBlocked,
    bool MetadataOnly,
    bool SecretsRedacted);

public sealed record CdpUiRuntimeSummary(
    string Status,
    string RuntimeLabel,
    string Source,
    string LastCaptureStatus,
    int ElementCount,
    int FrictionCount,
    int ActionMapCount,
    bool EvidenceAvailable,
    bool ScreenshotCaptured,
    bool BoundaryReadOnly,
    bool ExtensionUsed,
    bool SystemBrowserUsed,
    bool ExternalNavigationBlocked,
    bool ProductFilesModified);

public sealed record CdpUiRuntimeCommandResult(
    CdpUiRuntimeCommandKind CommandKind,
    CdpUiRuntimeCommandStatus Status,
    string RuntimeProvider,
    string Source,
    bool ReadOnly,
    bool ExtensionUsed,
    bool SystemBrowserUsed,
    bool ExternalNavigationBlocked,
    bool ProductFilesModified,
    bool DangerousActionBlocked,
    CdpUiRuntimeStatus? RuntimeStatus,
    CdpUiRuntimeSummary? Summary,
    CdpBrowserSkillsUiBridgeModel? UiBridgeModel,
    CdpUiRuntimeCommandEvidence Evidence,
    CdpUiRuntimeCommandError? Error);

public sealed class CdpUiRuntimeBoundary
{
    private static readonly HashSet<CdpUiRuntimeCommandKind> AllowedCommands =
    [
        CdpUiRuntimeCommandKind.GetRuntimeStatus,
        CdpUiRuntimeCommandKind.GetLastBrowserSkillsSummary,
        CdpUiRuntimeCommandKind.GetLastEvidenceSummary,
        CdpUiRuntimeCommandKind.BuildUiBridgeModel,
        CdpUiRuntimeCommandKind.RefreshControlledCapture
    ];

    private readonly CdpBrowserSkillsSessionService sessionService;

    public CdpUiRuntimeBoundary()
        : this(new CdpBrowserSkillsSessionService())
    {
    }

    public CdpUiRuntimeBoundary(CdpBrowserSkillsSessionService sessionService)
    {
        this.sessionService = sessionService;
    }

    public async Task<CdpUiRuntimeCommandResult> ExecuteAsync(
        CdpUiRuntimeCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RepositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.LockfilePath);
        cancellationToken.ThrowIfCancellationRequested();

        if (!AllowedCommands.Contains(request.CommandKind))
        {
            return BuildBlocked(request.CommandKind, "DANGEROUS_COMMAND_BLOCKED", "This CDP UI runtime command is not available in read-only V1.");
        }

        return request.CommandKind switch
        {
            CdpUiRuntimeCommandKind.GetRuntimeStatus => BuildRuntimeStatus(request),
            CdpUiRuntimeCommandKind.GetLastBrowserSkillsSummary => BuildLastSummary(request, CdpUiRuntimeCommandKind.GetLastBrowserSkillsSummary),
            CdpUiRuntimeCommandKind.GetLastEvidenceSummary => BuildLastSummary(request, CdpUiRuntimeCommandKind.GetLastEvidenceSummary),
            CdpUiRuntimeCommandKind.BuildUiBridgeModel => BuildLastSummary(request, CdpUiRuntimeCommandKind.BuildUiBridgeModel),
            CdpUiRuntimeCommandKind.RefreshControlledCapture => await RefreshControlledCaptureAsync(request, cancellationToken).ConfigureAwait(false),
            _ => BuildBlocked(request.CommandKind, "DANGEROUS_COMMAND_BLOCKED", "This CDP UI runtime command is not available in read-only V1.")
        };
    }

    private static CdpUiRuntimeCommandResult BuildRuntimeStatus(CdpUiRuntimeCommandRequest request)
    {
        var runtimeLock = BrowserRuntimeLock.Load(request.LockfilePath);
        var validation = runtimeLock.Validate();
        var artifactPinned = runtimeLock.HasPinnedRuntimeArtifact;
        var latestSession = FindLatestSessionEvidence(request.RepositoryRoot);
        var latestHealth = FindLatestHealthcheckEvidence(request.RepositoryRoot);
        var status = new CdpUiRuntimeStatus(
            RuntimeConfigured: validation.IsValid,
            ArtifactPinned: artifactPinned,
            ShaStatus: artifactPinned ? "pinned" : "pending",
            CdpCapabilityAvailable: validation.IsValid && artifactPinned,
            LastHealthcheckStatus: latestHealth?.Status ?? "Sin healthcheck reciente",
            LastBrowserSkillsSessionStatus: latestSession?.Status ?? "Sin captura CDP reciente",
            ExtensionMode: "legacy/no-default",
            SystemBrowserAllowed: runtimeLock.SystemBrowserAllowed,
            RuntimeProvider: "cloakbrowser",
            Source: "cloakbrowser-cdp-direct");

        return BuildResult(
            CdpUiRuntimeCommandKind.GetRuntimeStatus,
            CdpUiRuntimeCommandStatus.Success,
            status,
            Summary: new CdpUiRuntimeSummary(
                Status: validation.IsValid ? "configurado" : "requiere revisión",
                RuntimeLabel: "CloakBrowser CDP",
                Source: "cloakbrowser-cdp-direct",
                LastCaptureStatus: status.LastBrowserSkillsSessionStatus,
                ElementCount: latestSession?.ElementCount ?? 0,
                FrictionCount: latestSession?.FrictionCount ?? 0,
                ActionMapCount: latestSession?.ActionMapCount ?? 0,
                EvidenceAvailable: latestSession is not null,
                ScreenshotCaptured: latestSession?.ScreenshotCaptured ?? false,
                BoundaryReadOnly: true,
                ExtensionUsed: false,
                SystemBrowserUsed: false,
                ExternalNavigationBlocked: true,
                ProductFilesModified: false));
    }

    private static CdpUiRuntimeCommandResult BuildLastSummary(
        CdpUiRuntimeCommandRequest request,
        CdpUiRuntimeCommandKind commandKind)
    {
        var runtimeStatus = BuildRuntimeStatus(request).RuntimeStatus;
        var latest = FindLatestSessionEvidence(request.RepositoryRoot);
        if (latest is null)
        {
            return BuildResult(
                commandKind,
                CdpUiRuntimeCommandStatus.Empty,
                runtimeStatus,
                Summary: new CdpUiRuntimeSummary(
                    Status: "Sin captura CDP reciente",
                    RuntimeLabel: "CloakBrowser CDP",
                    Source: "cloakbrowser-cdp-direct",
                    LastCaptureStatus: "Sin captura CDP reciente",
                    ElementCount: 0,
                    FrictionCount: 0,
                    ActionMapCount: 0,
                    EvidenceAvailable: false,
                    ScreenshotCaptured: false,
                    BoundaryReadOnly: true,
                    ExtensionUsed: false,
                    SystemBrowserUsed: false,
                    ExternalNavigationBlocked: true,
                    ProductFilesModified: false),
                Error: new CdpUiRuntimeCommandError("NO_CDP_CAPTURE_YET", "Sin captura CDP reciente.", DangerousActionBlocked: false));
        }

        var summary = new CdpUiRuntimeSummary(
            Status: latest.Status,
            RuntimeLabel: "CloakBrowser CDP",
            Source: "cloakbrowser-cdp-direct",
            LastCaptureStatus: latest.CaptureOk ? "captura controlada disponible" : latest.Status,
            ElementCount: latest.ElementCount,
            FrictionCount: latest.FrictionCount,
            ActionMapCount: latest.ActionMapCount,
            EvidenceAvailable: true,
            ScreenshotCaptured: latest.ScreenshotCaptured,
            BoundaryReadOnly: true,
            ExtensionUsed: latest.ExtensionUsed,
            SystemBrowserUsed: latest.SystemBrowserUsed,
            ExternalNavigationBlocked: latest.ExternalNavigationBlocked,
            ProductFilesModified: latest.ProductFilesModified);

        return BuildResult(
            commandKind,
            CdpUiRuntimeCommandStatus.Success,
            runtimeStatus,
            Summary: summary);
    }

    private async Task<CdpUiRuntimeCommandResult> RefreshControlledCaptureAsync(
        CdpUiRuntimeCommandRequest request,
        CancellationToken cancellationToken)
    {
        var created = await sessionService.CreateSessionAsync(
                new CdpBrowserSkillsSessionRequest(
                    request.RepositoryRoot,
                    request.LockfilePath,
                    request.RuntimeArtifactPath,
                    request.Timeout ?? TimeSpan.FromSeconds(45)),
                cancellationToken)
            .ConfigureAwait(false);
        var captured = await sessionService.CaptureControlledPageAsync(created.Snapshot.SessionId, cancellationToken)
            .ConfigureAwait(false);
        var uiModel = captured.UiBridgeModel;
        var summary = new CdpUiRuntimeSummary(
            Status: captured.Status,
            RuntimeLabel: "CloakBrowser CDP",
            Source: "cloakbrowser-cdp-direct",
            LastCaptureStatus: captured.CaptureOk ? "captura controlada disponible" : captured.Reason,
            ElementCount: uiModel?.Summary.ElementCount ?? 0,
            FrictionCount: uiModel?.Summary.FrictionCount ?? 0,
            ActionMapCount: uiModel?.Summary.ActionMapCount ?? 0,
            EvidenceAvailable: uiModel?.Summary.EvidenceAvailable ?? false,
            ScreenshotCaptured: uiModel?.Summary.ScreenshotCaptured ?? false,
            BoundaryReadOnly: true,
            ExtensionUsed: captured.ExtensionUsed,
            SystemBrowserUsed: captured.SystemBrowserUsed,
            ExternalNavigationBlocked: captured.ExternalNavigationBlocked,
            ProductFilesModified: captured.Snapshot.ProductFilesModified);

        return BuildResult(
            CdpUiRuntimeCommandKind.RefreshControlledCapture,
            captured.Status == "PASS" ? CdpUiRuntimeCommandStatus.Success : CdpUiRuntimeCommandStatus.Failed,
            BuildRuntimeStatus(request).RuntimeStatus,
            Summary: summary,
            UiBridgeModel: uiModel,
            Error: captured.Status == "PASS"
                ? null
                : new CdpUiRuntimeCommandError("CONTROLLED_CAPTURE_FAILED", captured.Reason, DangerousActionBlocked: false));
    }

    private static CdpUiRuntimeCommandResult BuildBlocked(
        CdpUiRuntimeCommandKind commandKind,
        string code,
        string message) =>
        BuildResult(
            commandKind,
            CdpUiRuntimeCommandStatus.Blocked,
            RuntimeStatus: null,
            Summary: null,
            Error: new CdpUiRuntimeCommandError(code, message, DangerousActionBlocked: true));

    private static CdpUiRuntimeCommandResult BuildResult(
        CdpUiRuntimeCommandKind commandKind,
        CdpUiRuntimeCommandStatus status,
        CdpUiRuntimeStatus? RuntimeStatus,
        CdpUiRuntimeSummary? Summary,
        CdpBrowserSkillsUiBridgeModel? UiBridgeModel = null,
        CdpUiRuntimeCommandError? Error = null) =>
        new(
            CommandKind: commandKind,
            Status: status,
            RuntimeProvider: "cloakbrowser",
            Source: "cloakbrowser-cdp-direct",
            ReadOnly: true,
            ExtensionUsed: false,
            SystemBrowserUsed: false,
            ExternalNavigationBlocked: true,
            ProductFilesModified: false,
            DangerousActionBlocked: Error?.DangerousActionBlocked ?? false,
            RuntimeStatus,
            Summary,
            UiBridgeModel,
            Evidence: new CdpUiRuntimeCommandEvidence(
                Source: "cloakbrowser-cdp-direct",
                RuntimeProvider: "cloakbrowser",
                ReadOnly: true,
                ExtensionUsed: false,
                SystemBrowserUsed: false,
                ExternalNavigationBlocked: true,
                ProductFilesModified: false,
                DangerousActionBlocked: Error?.DangerousActionBlocked ?? false,
                MetadataOnly: true,
                SecretsRedacted: true),
            Error);

    private static EvidenceSummary? FindLatestSessionEvidence(string repositoryRoot) =>
        FindLatestEvidence(repositoryRoot, "cloakbrowser-cdp-browser-skills-session-*.redacted.json");

    private static EvidenceSummary? FindLatestHealthcheckEvidence(string repositoryRoot) =>
        FindLatestEvidence(repositoryRoot, "cloakbrowser-cdp-healthcheck-*.redacted.json");

    private static EvidenceSummary? FindLatestEvidence(string repositoryRoot, string searchPattern)
    {
        var artifactsRoot = Path.Combine(repositoryRoot, "artifacts", "local-verification");
        if (!Directory.Exists(artifactsRoot))
        {
            return null;
        }

        var latest = Directory
            .EnumerateFiles(artifactsRoot, searchPattern, SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(info => info.LastWriteTimeUtc)
            .FirstOrDefault();
        if (latest is null)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(latest.FullName));
            var root = document.RootElement;
            var status = GetString(root, "status") ?? "available";
            return new EvidenceSummary(
                Status: status,
                CaptureOk: GetBool(root, "captureOk"),
                ElementCount: GetInt(root, "interactiveElements", "interactiveElementCount"),
                FrictionCount: GetInt(root, "frictionSignals"),
                ActionMapCount: GetInt(root, "actionMapEntries"),
                ScreenshotCaptured: GetBool(root, "screenshotCaptured"),
                ExtensionUsed: GetBool(root, "extensionUsed"),
                SystemBrowserUsed: GetBool(root, "systemBrowserUsed"),
                ExternalNavigationBlocked: GetBool(root, "externalNavigationBlocked", defaultValue: true),
                ProductFilesModified: GetBool(root, "productFilesModified", "productFilesModified", false),
                FileName: latest.Name);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static string? GetString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static bool GetBool(JsonElement root, string propertyName, bool defaultValue = false) =>
        root.TryGetProperty(propertyName, out var property)
            ? property.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => defaultValue
            }
            : defaultValue;

    private static bool GetBool(JsonElement root, string firstName, string secondName, bool defaultValue) =>
        root.TryGetProperty(firstName, out _)
            ? GetBool(root, firstName, defaultValue)
            : GetBool(root, secondName, defaultValue);

    private static int GetInt(JsonElement root, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (root.TryGetProperty(name, out var property))
            {
                if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value))
                {
                    return value;
                }

                if (property.ValueKind == JsonValueKind.Object)
                {
                    if (property.TryGetProperty("Count", out var count)
                        && count.ValueKind == JsonValueKind.Number
                        && count.TryGetInt32(out var countValue))
                    {
                        return countValue;
                    }

                    if (property.TryGetProperty("EntriesCount", out var entries)
                        && entries.ValueKind == JsonValueKind.Number
                        && entries.TryGetInt32(out var entriesValue))
                    {
                        return entriesValue;
                    }
                }
            }
        }

        return 0;
    }

    private sealed record EvidenceSummary(
        string Status,
        bool CaptureOk,
        int ElementCount,
        int FrictionCount,
        int ActionMapCount,
        bool ScreenshotCaptured,
        bool ExtensionUsed,
        bool SystemBrowserUsed,
        bool ExternalNavigationBlocked,
        bool ProductFilesModified,
        string FileName);
}
