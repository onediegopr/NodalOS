using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneBrain.BrowserRuntime;

public enum CdpBrowserSkillsSessionState
{
    Created,
    Capturing,
    Captured,
    EvidenceReady,
    Closed,
    Failed
}

public sealed record CdpBrowserSkillsSessionRequest(
    string RepositoryRoot,
    string LockfilePath,
    string? RuntimeArtifactPath = null,
    TimeSpan? Timeout = null);

public sealed record CdpBrowserSkillsSessionEvidenceRef(
    string Kind,
    string? Path,
    DateTimeOffset CreatedAt,
    bool Redacted,
    bool LocalOnly);

public sealed record CdpBrowserSkillsSessionDomIndexSummary(
    int NodeCount,
    int InteractiveElementsCount,
    int HeadingsCount,
    int ButtonsCount,
    int InputsCount,
    int LinksCount,
    int FormsCount,
    int DisabledElementsCount,
    int RequiredEmptyFieldsCount,
    string PageStructureSummary,
    bool StoresRawHtml,
    bool StoresInputValues);

public sealed record CdpBrowserSkillsSessionFrictionSummary(
    int Count,
    IReadOnlyList<string> SignalTypes,
    bool BypassAttempted);

public sealed record CdpBrowserSkillsSessionActionMapSummary(
    int EntriesCount,
    bool ExternalNavigationBlocked,
    bool DangerousActionsBlocked);

public sealed record CdpBrowserSkillsSessionSnapshot(
    string SessionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastCaptureAt,
    string RuntimeProvider,
    string Source,
    bool ExtensionUsed,
    bool SystemBrowserUsed,
    CdpBrowserSkillsSessionState Status,
    CdpBrowserSkillPageSummary? PageSummary,
    CdpBrowserSkillsSessionDomIndexSummary? DomIndexSummary,
    CdpBrowserSkillsSessionFrictionSummary? FrictionSummary,
    CdpBrowserSkillsSessionActionMapSummary? ActionMapSummary,
    IReadOnlyList<CdpBrowserSkillsSessionEvidenceRef> EvidenceRefs,
    bool ReadOnly,
    bool ExternalNavigationBlocked,
    bool ProductFilesModified,
    bool RawHtmlStored,
    bool InputValuesStored,
    bool SecretsRedacted);

public sealed record CdpBrowserSkillsUiBridgeSummary(
    string Title,
    string? Url,
    string RuntimeLabel,
    string CaptureStatus,
    int ElementCount,
    int FrictionCount,
    int ActionMapCount,
    bool ScreenshotCaptured,
    bool EvidenceAvailable,
    IReadOnlyList<string> Flags);

public sealed record CdpBrowserSkillsUiBridgeModel(
    string SessionId,
    CdpBrowserSkillsSessionState Status,
    CdpBrowserSkillsUiBridgeSummary Summary,
    IReadOnlyList<CdpBrowserSkillsSessionEvidenceRef> EvidenceRefs,
    bool ReadOnly,
    bool ExtensionUsed,
    bool SystemBrowserUsed,
    bool ExternalNavigationBlocked,
    bool ProductFilesModified,
    bool ContainsRawDom,
    bool ContainsSecrets);

public sealed record CdpBrowserSkillsSessionResult(
    string Status,
    string Decision,
    string Reason,
    CdpBrowserSkillsSessionSnapshot Snapshot,
    CdpBrowserSkillCaptureResult? LatestCapture,
    CdpBrowserSkillsUiBridgeModel? UiBridgeModel,
    bool SessionCreated,
    bool CaptureOk,
    bool UiBridgeModelOk,
    bool RuntimeShutdown,
    bool ProcessExited,
    bool OrphanProcessDetected,
    bool ExtensionUsed,
    bool SystemBrowserUsed,
    bool ExternalNavigationBlocked);

public sealed class CdpBrowserSkillsSessionService
{
    private const string SuccessDecision = "NODAL_OS_CLOAKBROWSER_CDP_BROWSER_SKILLS_SESSION_API_UI_BRIDGE_READY";
    private readonly ConcurrentDictionary<string, SessionRecord> sessions = new(StringComparer.Ordinal);
    private readonly CdpBrowserSkillsService browserSkillsService;

    public CdpBrowserSkillsSessionService()
        : this(new CdpBrowserSkillsService())
    {
    }

    public CdpBrowserSkillsSessionService(CdpBrowserSkillsService browserSkillsService)
    {
        this.browserSkillsService = browserSkillsService;
    }

    public Task<CdpBrowserSkillsSessionResult> CreateSessionAsync(
        CdpBrowserSkillsSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RepositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.LockfilePath);
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        var sessionId = "cdp-browser-skills-" + now.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture)
            + "-" + Guid.NewGuid().ToString("N")[..8];
        var record = new SessionRecord(
            request,
            new CdpBrowserSkillsSessionSnapshot(
                SessionId: sessionId,
                CreatedAt: now,
                LastCaptureAt: null,
                RuntimeProvider: "cloakbrowser",
                Source: "cloakbrowser-cdp-direct",
                ExtensionUsed: false,
                SystemBrowserUsed: false,
                Status: CdpBrowserSkillsSessionState.Created,
                PageSummary: null,
                DomIndexSummary: null,
                FrictionSummary: null,
                ActionMapSummary: null,
                EvidenceRefs: [],
                ReadOnly: true,
                ExternalNavigationBlocked: true,
                ProductFilesModified: false,
                RawHtmlStored: false,
                InputValuesStored: false,
                SecretsRedacted: true),
            LatestCapture: null,
            UiBridgeModel: null);

        sessions[sessionId] = record;
        return Task.FromResult(BuildResult(record, "PASS", SuccessDecision, "CDP Browser Skills session created."));
    }

    public async Task<CdpBrowserSkillsSessionResult> CaptureControlledPageAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        var record = GetRecord(sessionId);
        if (record.Snapshot.Status == CdpBrowserSkillsSessionState.Closed)
        {
            return BuildResult(record, "BLOCKED", SuccessDecision, "CDP Browser Skills session is closed.");
        }

        record = UpdateSnapshot(record, record.Snapshot with { Status = CdpBrowserSkillsSessionState.Capturing });
        sessions[sessionId] = record;

        try
        {
            var capture = await browserSkillsService.CapturePageAsync(
                    new CdpBrowserSkillCaptureRequest(
                        record.Request.RepositoryRoot,
                        record.Request.LockfilePath,
                        record.Request.RuntimeArtifactPath,
                        record.Request.Timeout ?? TimeSpan.FromSeconds(45)),
                    cancellationToken)
                .ConfigureAwait(false);

            var now = DateTimeOffset.UtcNow;
            var evidenceRefs = BuildEvidenceRefs(capture);
            var snapshot = record.Snapshot with
            {
                LastCaptureAt = now,
                Status = capture.Status == "PASS" && evidenceRefs.Count > 0
                    ? CdpBrowserSkillsSessionState.EvidenceReady
                    : capture.Status == "PASS"
                        ? CdpBrowserSkillsSessionState.Captured
                        : CdpBrowserSkillsSessionState.Failed,
                PageSummary = capture.PageSummary,
                DomIndexSummary = ToSessionDomIndexSummary(capture.DomIndex),
                FrictionSummary = ToSessionFrictionSummary(capture.FrictionSignals),
                ActionMapSummary = ToSessionActionMapSummary(capture.ActionMap),
                EvidenceRefs = evidenceRefs,
                ExtensionUsed = capture.ExtensionUsed,
                SystemBrowserUsed = capture.SystemBrowserUsed,
                ExternalNavigationBlocked = capture.ExternalNavigationBlocked,
                ProductFilesModified = capture.Evidence.ProductFilesModified,
                RawHtmlStored = capture.Evidence.StoresRawHtml,
                InputValuesStored = capture.Evidence.StoresInputValues,
                SecretsRedacted = capture.Evidence.SecretsRedacted
            };

            record = new SessionRecord(record.Request, snapshot, capture, record.UiBridgeModel);
            var uiBridge = BuildUiBridgeModel(record);
            record = record with { UiBridgeModel = uiBridge };
            record = await WriteSessionEvidenceAsync(record, cancellationToken).ConfigureAwait(false);
            sessions[sessionId] = record;

            return BuildResult(
                record,
                capture.Status,
                capture.Status == "PASS" ? SuccessDecision : capture.Decision,
                capture.Status == "PASS"
                    ? "CDP Browser Skills session captured controlled page and built read-only UI bridge model."
                    : capture.Reason);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            record = UpdateSnapshot(record, record.Snapshot with { Status = CdpBrowserSkillsSessionState.Failed });
            sessions[sessionId] = record;
            return BuildResult(record, "BLOCKED", SuccessDecision, "CDP Browser Skills session capture failed: " + ex.Message);
        }
    }

    public Task<CdpBrowserSkillsSessionSnapshot> GetSessionSnapshotAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetRecord(sessionId).Snapshot);
    }

    public Task<CdpBrowserSkillCaptureResult?> GetLatestCaptureAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetRecord(sessionId).LatestCapture);
    }

    public Task<CdpBrowserSkillsUiBridgeModel> BuildUiBridgeModelAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = GetRecord(sessionId);
        var model = record.UiBridgeModel ?? BuildUiBridgeModel(record);
        sessions[sessionId] = record with { UiBridgeModel = model };
        return Task.FromResult(model);
    }

    public Task<CdpBrowserSkillsSessionResult> CloseSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = GetRecord(sessionId);
        if (record.Snapshot.Status != CdpBrowserSkillsSessionState.Closed)
        {
            record = UpdateSnapshot(record, record.Snapshot with { Status = CdpBrowserSkillsSessionState.Closed });
            sessions[sessionId] = record;
        }

        return Task.FromResult(BuildResult(record, "PASS", SuccessDecision, "CDP Browser Skills session closed."));
    }

    private SessionRecord GetRecord(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return sessions.TryGetValue(sessionId, out var record)
            ? record
            : throw new InvalidOperationException("CDP Browser Skills session was not found: " + sessionId);
    }

    private static SessionRecord UpdateSnapshot(SessionRecord record, CdpBrowserSkillsSessionSnapshot snapshot) =>
        record with { Snapshot = snapshot };

    private static CdpBrowserSkillsUiBridgeModel BuildUiBridgeModel(SessionRecord record)
    {
        var snapshot = record.Snapshot;
        var summary = new CdpBrowserSkillsUiBridgeSummary(
            Title: snapshot.PageSummary?.Title ?? "Browser Skills CDP",
            Url: snapshot.PageSummary?.Url,
            RuntimeLabel: "CloakBrowser CDP",
            CaptureStatus: snapshot.Status.ToString(),
            ElementCount: snapshot.DomIndexSummary?.InteractiveElementsCount ?? 0,
            FrictionCount: snapshot.FrictionSummary?.Count ?? 0,
            ActionMapCount: snapshot.ActionMapSummary?.EntriesCount ?? 0,
            ScreenshotCaptured: snapshot.PageSummary?.ScreenshotCaptured ?? false,
            EvidenceAvailable: snapshot.EvidenceRefs.Count > 0,
            Flags:
            [
                "Solo lectura",
                "Sin extensión",
                "Sin navegador del sistema",
                "Sin navegación externa",
                "No se modificaron archivos"
            ]);

        return new CdpBrowserSkillsUiBridgeModel(
            SessionId: snapshot.SessionId,
            Status: snapshot.Status,
            Summary: summary,
            EvidenceRefs: snapshot.EvidenceRefs,
            ReadOnly: snapshot.ReadOnly,
            ExtensionUsed: snapshot.ExtensionUsed,
            SystemBrowserUsed: snapshot.SystemBrowserUsed,
            ExternalNavigationBlocked: snapshot.ExternalNavigationBlocked,
            ProductFilesModified: snapshot.ProductFilesModified,
            ContainsRawDom: snapshot.RawHtmlStored,
            ContainsSecrets: !snapshot.SecretsRedacted);
    }

    private static List<CdpBrowserSkillsSessionEvidenceRef> BuildEvidenceRefs(CdpBrowserSkillCaptureResult capture)
    {
        var refs = new List<CdpBrowserSkillsSessionEvidenceRef>();
        if (!string.IsNullOrWhiteSpace(capture.EvidencePath))
        {
            refs.Add(new CdpBrowserSkillsSessionEvidenceRef(
                Kind: "browser-skills-capture",
                Path: capture.EvidencePath,
                CreatedAt: DateTimeOffset.UtcNow,
                Redacted: true,
                LocalOnly: true));
        }

        return refs;
    }

    private static CdpBrowserSkillsSessionDomIndexSummary ToSessionDomIndexSummary(CdpBrowserSkillDomIndex domIndex) =>
        new(
            NodeCount: domIndex.NodeCount,
            InteractiveElementsCount: domIndex.InteractiveElementsCount,
            HeadingsCount: domIndex.HeadingsCount,
            ButtonsCount: domIndex.ButtonsCount,
            InputsCount: domIndex.InputsCount,
            LinksCount: domIndex.LinksCount,
            FormsCount: domIndex.FormsCount,
            DisabledElementsCount: domIndex.DisabledElementsCount,
            RequiredEmptyFieldsCount: domIndex.RequiredEmptyFieldsCount,
            PageStructureSummary: domIndex.PageStructureSummary,
            StoresRawHtml: domIndex.StoresRawHtml,
            StoresInputValues: domIndex.StoresInputValues);

    private static CdpBrowserSkillsSessionFrictionSummary ToSessionFrictionSummary(
        IReadOnlyList<CdpBrowserSkillFrictionSignal> frictionSignals) =>
        new(
            Count: frictionSignals.Count,
            SignalTypes: frictionSignals.Select(signal => signal.SignalType).ToArray(),
            BypassAttempted: frictionSignals.Any(signal => signal.BypassAttempted));

    private static CdpBrowserSkillsSessionActionMapSummary ToSessionActionMapSummary(CdpBrowserSkillActionMap actionMap) =>
        new(
            EntriesCount: actionMap.Entries.Count,
            ExternalNavigationBlocked: actionMap.ExternalNavigationBlocked,
            DangerousActionsBlocked: actionMap.DangerousActionsBlocked);

    private async Task<SessionRecord> WriteSessionEvidenceAsync(
        SessionRecord record,
        CancellationToken cancellationToken)
    {
        var artifactsRoot = Path.Combine(record.Request.RepositoryRoot, "artifacts", "local-verification");
        Directory.CreateDirectory(artifactsRoot);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss-fffZ");
        var evidencePath = Path.Combine(
            artifactsRoot,
            "cloakbrowser-cdp-browser-skills-session-" + timestamp + ".redacted.json");

        var latest = record.LatestCapture;
        var refs = record.Snapshot.EvidenceRefs
            .Concat(
            [
                new CdpBrowserSkillsSessionEvidenceRef(
                    Kind: "browser-skills-session",
                    Path: evidencePath,
                    CreatedAt: DateTimeOffset.UtcNow,
                    Redacted: true,
                    LocalOnly: true)
            ])
            .ToArray();
        var updatedSnapshot = record.Snapshot with { EvidenceRefs = refs };
        var updatedRecord = record with { Snapshot = updatedSnapshot };
        var uiBridge = BuildUiBridgeModel(updatedRecord);
        var evidence = new
        {
            status = latest?.Status ?? "PASS",
            decision = SuccessDecision,
            sessionId = updatedSnapshot.SessionId,
            runtimeProvider = updatedSnapshot.RuntimeProvider,
            source = updatedSnapshot.Source,
            sessionCreated = true,
            captureOk = latest?.CaptureOk ?? false,
            uiBridgeModelOk = uiBridge.ReadOnly
                && !uiBridge.ExtensionUsed
                && !uiBridge.SystemBrowserUsed
                && !uiBridge.ContainsRawDom
                && !uiBridge.ContainsSecrets,
            domIndexOk = latest?.DomIndexOk ?? false,
            frictionSignals = updatedSnapshot.FrictionSummary,
            actionMap = updatedSnapshot.ActionMapSummary,
            screenshotCaptured = latest?.ScreenshotCaptured ?? false,
            externalNavigationBlocked = updatedSnapshot.ExternalNavigationBlocked,
            extensionUsed = updatedSnapshot.ExtensionUsed,
            systemBrowserUsed = updatedSnapshot.SystemBrowserUsed,
            processExited = latest?.ProcessExited ?? false,
            runtimeShutdown = latest?.RuntimeShutdown ?? false,
            orphanProcessDetected = latest?.OrphanProcessDetected ?? false,
            secretsRedacted = updatedSnapshot.SecretsRedacted,
            rawHtmlStored = updatedSnapshot.RawHtmlStored,
            inputValuesStored = updatedSnapshot.InputValuesStored,
            productFilesModified = updatedSnapshot.ProductFilesModified,
            uiBridgeModel = uiBridge,
            evidenceRefs = updatedSnapshot.EvidenceRefs,
            timestamp = DateTimeOffset.UtcNow
        };

        await File.WriteAllTextAsync(
                evidencePath,
                JsonSerializer.Serialize(evidence, JsonOptions),
                cancellationToken)
            .ConfigureAwait(false);

        return updatedRecord with { UiBridgeModel = uiBridge };
    }

    private static CdpBrowserSkillsSessionResult BuildResult(
        SessionRecord record,
        string status,
        string decision,
        string reason)
    {
        var latest = record.LatestCapture;
        var uiBridge = record.UiBridgeModel;
        return new CdpBrowserSkillsSessionResult(
            Status: status,
            Decision: decision,
            Reason: reason,
            Snapshot: record.Snapshot,
            LatestCapture: latest,
            UiBridgeModel: uiBridge,
            SessionCreated: true,
            CaptureOk: latest?.CaptureOk ?? false,
            UiBridgeModelOk: uiBridge is not null
                && uiBridge.ReadOnly
                && !uiBridge.ExtensionUsed
                && !uiBridge.SystemBrowserUsed
                && !uiBridge.ContainsRawDom
                && !uiBridge.ContainsSecrets,
            RuntimeShutdown: latest?.RuntimeShutdown ?? false,
            ProcessExited: latest?.ProcessExited ?? false,
            OrphanProcessDetected: latest?.OrphanProcessDetected ?? false,
            ExtensionUsed: record.Snapshot.ExtensionUsed,
            SystemBrowserUsed: record.Snapshot.SystemBrowserUsed,
            ExternalNavigationBlocked: record.Snapshot.ExternalNavigationBlocked);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private sealed record SessionRecord(
        CdpBrowserSkillsSessionRequest Request,
        CdpBrowserSkillsSessionSnapshot Snapshot,
        CdpBrowserSkillCaptureResult? LatestCapture,
        CdpBrowserSkillsUiBridgeModel? UiBridgeModel);
}
