using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneBrain.BrowserRuntime;

public sealed record CdpBrowserSkillCaptureRequest(
    string RepositoryRoot,
    string LockfilePath,
    string? RuntimeArtifactPath = null,
    TimeSpan? Timeout = null);

public sealed record CdpBrowserSkillPageSummary(
    string Source,
    string RuntimeProvider,
    bool ExtensionUsed,
    bool SystemBrowserUsed,
    string? Url,
    string? Title,
    string ReadyState,
    bool ScreenshotCaptured);

public sealed record CdpBrowserSkillInteractiveElement(
    string StableId,
    string ElementKind,
    string Tag,
    string Role,
    string Label,
    string? InputType,
    string? Href,
    bool Enabled,
    bool Visible,
    bool Required,
    string SelectorHint);

public sealed record CdpBrowserSkillDomIndex(
    int NodeCount,
    int HeadingsCount,
    int ButtonsCount,
    int InputsCount,
    int LinksCount,
    int FormsCount,
    int DisabledElementsCount,
    int RequiredEmptyFieldsCount,
    int IndexedElementsCount,
    int InteractiveElementsCount,
    string KeyTextPreview,
    string PageStructureSummary,
    IReadOnlyList<CdpBrowserSkillInteractiveElement> InteractiveElements,
    bool StoresRawHtml,
    bool StoresInputValues);

public sealed record CdpBrowserSkillFrictionSignal(
    string SignalType,
    string Severity,
    string Summary,
    bool RequiresReview,
    bool BypassAttempted);

public sealed record CdpBrowserSkillActionMapEntry(
    string StableId,
    string Label,
    string ElementKind,
    IReadOnlyList<string> AllowedActions,
    IReadOnlyList<string> BlockedActions,
    string Reason);

public sealed record CdpBrowserSkillActionMap(
    IReadOnlyList<CdpBrowserSkillActionMapEntry> Entries,
    bool ExternalNavigationBlocked,
    bool DangerousActionsBlocked);

public sealed record CdpBrowserSkillEvidence(
    DateTimeOffset Timestamp,
    string RuntimeProvider,
    string Source,
    bool ExtensionUsed,
    bool SystemBrowserUsed,
    string? Url,
    string? Title,
    bool ScreenshotCaptured,
    CdpBrowserSkillDomIndex DomIndexSummary,
    int InteractiveElementCount,
    IReadOnlyList<CdpBrowserSkillFrictionSignal> FrictionSignals,
    CdpBrowserSkillActionMap ActionMap,
    bool ExternalNavigationBlocked,
    bool ProductFilesModified,
    bool SecretsRedacted,
    bool CdpCommandsExecuted,
    bool StoresRawHtml,
    bool StoresInputValues);

public sealed record CdpBrowserSkillCaptureResult(
    string Status,
    string Decision,
    string Reason,
    CdpBrowserSkillPageSummary PageSummary,
    CdpBrowserSkillDomIndex DomIndex,
    IReadOnlyList<CdpBrowserSkillFrictionSignal> FrictionSignals,
    CdpBrowserSkillActionMap ActionMap,
    CdpBrowserSkillEvidence Evidence,
    string? EvidencePath,
    bool CaptureOk,
    bool DomIndexOk,
    bool FrictionSignalsDetected,
    bool ActionMapOk,
    bool ScreenshotCaptured,
    bool ExtensionUsed,
    bool SystemBrowserUsed,
    bool ExternalNavigationBlocked,
    bool RuntimeShutdown,
    bool ProcessExited,
    bool OrphanProcessDetected);

public static class CdpBrowserSkillsControlledPage
{
    public const string Title = "NODAL OS Browser Skills CDP Parity";

    public static string BuildHtml() =>
        """
<!doctype html>
<html>
  <head><meta charset="utf-8"><title>NODAL OS Browser Skills CDP Parity</title></head>
  <body>
    <main>
      <h1>NODAL OS Browser Skills CDP Parity</h1>
      <section aria-label="Visible browser skill context">
        <p>Browser Skills parity uses a controlled local page.</p>
        <p>captcha demo marker - requires review, no bypass.</p>
        <div role="status" aria-label="requires review">requires review</div>
      </section>
      <button id="primary-action" type="button" onclick="document.body.setAttribute('data-browser-skill-clicked', 'true')">Capture controlled state</button>
      <button id="disabled-action" type="button" disabled>Disabled review action</button>
      <label for="controlled-text">Controlled text input</label>
      <input id="controlled-text" name="controlledText" type="text" required placeholder="safe required text" />
      <a id="external-dummy" href="https://example.invalid/browser-skills?token=redacted">External dummy link</a>
      <form id="blocked-form" action="https://example.invalid/submit" onsubmit="return false;">
        <button type="submit">Submit stays blocked</button>
      </form>
      <div role="button" tabindex="0" aria-label="Role button sample">Role button sample</div>
    </main>
  </body>
</html>
""";

    public static string BuildDataUrl() => CloakBrowserCdpHealthcheckRunner.BuildDataUrl(BuildHtml());
}

public sealed class CdpBrowserSkillsService
{
    private const string SuccessDecision = "NODAL_OS_CLOAKBROWSER_CDP_BROWSER_SKILLS_CORE_PARITY_READY";

    public async Task<CdpBrowserSkillCaptureResult> CapturePageAsync(
        CdpBrowserSkillCaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var healthcheck = await new CloakBrowserRuntimeProvider()
            .RunLiveHealthcheckAsync(
                new CloakBrowserCdpHealthcheckOptions(
                    request.RepositoryRoot,
                    request.LockfilePath,
                    request.RuntimeArtifactPath,
                    request.Timeout ?? TimeSpan.FromSeconds(45),
                    ControlledPageTitle: CdpBrowserSkillsControlledPage.Title,
                    ControlledPageHtml: CdpBrowserSkillsControlledPage.BuildHtml(),
                    SuccessDecisionOverride: SuccessDecision),
                cancellationToken)
            .ConfigureAwait(false);

        var snapshot = healthcheck.DomSnapshot ?? CreateEmptySnapshot(healthcheck);
        var pageSummary = new CdpBrowserSkillPageSummary(
            Source: "cloakbrowser-cdp-direct",
            RuntimeProvider: "cloakbrowser",
            ExtensionUsed: healthcheck.ExtensionUsed,
            SystemBrowserUsed: healthcheck.SystemBrowserUsed,
            Url: healthcheck.Url,
            Title: healthcheck.Title,
            ReadyState: snapshot.PageMetadata.ReadyState,
            ScreenshotCaptured: healthcheck.ScreenshotCaptured);
        var domIndex = BuildDomIndex(snapshot);
        var frictionSignals = DetectFrictionSignals(snapshot, healthcheck.ExternalNavigationBlocked);
        var actionMap = BuildActionMap(domIndex.InteractiveElements, healthcheck.ExternalNavigationBlocked);
        var evidence = new CdpBrowserSkillEvidence(
            Timestamp: DateTimeOffset.UtcNow,
            RuntimeProvider: "cloakbrowser",
            Source: "cloakbrowser-cdp-direct",
            ExtensionUsed: healthcheck.ExtensionUsed,
            SystemBrowserUsed: healthcheck.SystemBrowserUsed,
            Url: healthcheck.Url,
            Title: healthcheck.Title,
            ScreenshotCaptured: healthcheck.ScreenshotCaptured,
            DomIndexSummary: domIndex,
            InteractiveElementCount: domIndex.InteractiveElementsCount,
            FrictionSignals: frictionSignals,
            ActionMap: actionMap,
            ExternalNavigationBlocked: healthcheck.ExternalNavigationBlocked,
            ProductFilesModified: healthcheck.FilesModified,
            SecretsRedacted: healthcheck.SecretsRedacted,
            CdpCommandsExecuted: healthcheck.CdpCommandsExecuted,
            StoresRawHtml: snapshot.StoresRawHtml,
            StoresInputValues: snapshot.StoresInputValues);
        var captureOk = healthcheck.Status == "PASS" && pageSummary.Title == CdpBrowserSkillsControlledPage.Title;
        var result = new CdpBrowserSkillCaptureResult(
            Status: captureOk ? "PASS" : healthcheck.Status,
            Decision: captureOk ? SuccessDecision : healthcheck.Decision,
            Reason: captureOk ? "CloakBrowser CDP Browser Skills core parity completed." : healthcheck.Reason,
            PageSummary: pageSummary,
            DomIndex: domIndex,
            FrictionSignals: frictionSignals,
            ActionMap: actionMap,
            Evidence: evidence,
            EvidencePath: null,
            CaptureOk: captureOk,
            DomIndexOk: domIndex.IndexedElementsCount > 0 && !domIndex.StoresRawHtml && !domIndex.StoresInputValues,
            FrictionSignalsDetected: frictionSignals.Count > 0,
            ActionMapOk: actionMap.Entries.Count > 0 && actionMap.DangerousActionsBlocked,
            ScreenshotCaptured: healthcheck.ScreenshotCaptured,
            ExtensionUsed: healthcheck.ExtensionUsed,
            SystemBrowserUsed: healthcheck.SystemBrowserUsed,
            ExternalNavigationBlocked: healthcheck.ExternalNavigationBlocked,
            RuntimeShutdown: healthcheck.RuntimeShutdown,
            ProcessExited: healthcheck.ProcessExited,
            OrphanProcessDetected: healthcheck.OrphanProcessDetected);

        return await WriteEvidenceAsync(request.RepositoryRoot, result, cancellationToken).ConfigureAwait(false);
    }

    public CdpBrowserSkillDomIndex BuildDomIndex(CdpDomSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var interactive = snapshot.InteractiveElements
            .Select(ToBrowserSkillElement)
            .ToArray();
        return new CdpBrowserSkillDomIndex(
            NodeCount: snapshot.NodeCount,
            HeadingsCount: snapshot.HeadingsCount,
            ButtonsCount: snapshot.ButtonsCount,
            InputsCount: snapshot.InputsCount,
            LinksCount: snapshot.LinksCount,
            FormsCount: snapshot.FormsCount,
            DisabledElementsCount: snapshot.DisabledElementsCount,
            RequiredEmptyFieldsCount: snapshot.RequiredEmptyFieldsCount,
            IndexedElementsCount: snapshot.NodeCount,
            InteractiveElementsCount: interactive.Length,
            KeyTextPreview: snapshot.TextPreview,
            PageStructureSummary: snapshot.PageStructureSummary,
            InteractiveElements: interactive,
            StoresRawHtml: snapshot.StoresRawHtml,
            StoresInputValues: snapshot.StoresInputValues);
    }

    public IReadOnlyList<CdpBrowserSkillFrictionSignal> DetectFrictionSignals(
        CdpDomSnapshot snapshot,
        bool externalNavigationBlocked)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var signals = new List<CdpBrowserSkillFrictionSignal>();
        if (snapshot.DisabledElementsCount > 0)
        {
            signals.Add(new CdpBrowserSkillFrictionSignal(
                "disabled-controls",
                "review",
                "Disabled controls are visible on the controlled page.",
                RequiresReview: true,
                BypassAttempted: false));
        }

        if (snapshot.RequiredEmptyFieldsCount > 0)
        {
            signals.Add(new CdpBrowserSkillFrictionSignal(
                "required-empty-fields",
                "review",
                "Required empty fields are present.",
                RequiresReview: true,
                BypassAttempted: false));
        }

        if (snapshot.TextPreview.Contains("captcha", StringComparison.OrdinalIgnoreCase)
            || snapshot.TextPreview.Contains("requires review", StringComparison.OrdinalIgnoreCase))
        {
            signals.Add(new CdpBrowserSkillFrictionSignal(
                "controlled-review-marker",
                "review",
                "Local controlled marker requests human review; no challenge bypass was attempted.",
                RequiresReview: true,
                BypassAttempted: false));
        }

        if (snapshot.FormsCount > 0)
        {
            signals.Add(new CdpBrowserSkillFrictionSignal(
                "form-submit-blocked",
                "blocked",
                "Form submission remains blocked in Browser Skills CDP parity V1.",
                RequiresReview: true,
                BypassAttempted: false));
        }

        if (externalNavigationBlocked)
        {
            signals.Add(new CdpBrowserSkillFrictionSignal(
                "external-navigation-blocked",
                "blocked",
                "External navigation is blocked for the controlled parity flow.",
                RequiresReview: false,
                BypassAttempted: false));
        }

        return signals;
    }

    public CdpBrowserSkillActionMap BuildActionMap(
        IReadOnlyList<CdpBrowserSkillInteractiveElement> elements,
        bool externalNavigationBlocked)
    {
        ArgumentNullException.ThrowIfNull(elements);

        var entries = elements.Select(element =>
        {
            var allowed = new List<string> { "read metadata", "screenshot" };
            if (element.ElementKind == "button" && element.Enabled && element.Visible)
            {
                allowed.Add("click controlled button");
            }

            if (element.ElementKind == "input" && element.Enabled && element.Visible && element.InputType is "text" or null)
            {
                allowed.Add("type controlled input");
                allowed.Add("clear controlled input");
            }

            var blocked = new List<string>
            {
                "submit form",
                "upload file",
                "download file",
                "credentials",
                "captcha/challenge",
                "arbitrary JS",
                "file system write",
                "shell"
            };

            if (element.ElementKind == "link" || externalNavigationBlocked)
            {
                blocked.Add("navigate external");
            }

            return new CdpBrowserSkillActionMapEntry(
                element.StableId,
                element.Label,
                element.ElementKind,
                allowed,
                blocked,
                "CDP Browser Skills parity V1 only permits controlled local actions.");
        }).ToArray();

        return new CdpBrowserSkillActionMap(
            entries,
            ExternalNavigationBlocked: externalNavigationBlocked,
            DangerousActionsBlocked: entries.All(entry =>
                entry.BlockedActions.Contains("credentials", StringComparer.Ordinal)
                && entry.BlockedActions.Contains("captcha/challenge", StringComparer.Ordinal)
                && entry.BlockedActions.Contains("file system write", StringComparer.Ordinal)
                && entry.BlockedActions.Contains("shell", StringComparer.Ordinal)));
    }

    private static CdpBrowserSkillInteractiveElement ToBrowserSkillElement(CdpInteractiveElementSummary element) =>
        new(
            StableId: element.StableId,
            ElementKind: ToElementKind(element),
            Tag: element.Tag,
            Role: element.Role,
            Label: element.Label,
            InputType: element.InputType,
            Href: element.Href,
            Enabled: element.Enabled,
            Visible: element.Visible,
            Required: element.Required,
            SelectorHint: element.SelectorHint);

    private static string ToElementKind(CdpInteractiveElementSummary element)
    {
        if (element.Tag == "a")
        {
            return "link";
        }

        if (element.Tag is "input" or "textarea" or "select" || element.Role == "input")
        {
            return "input";
        }

        if (element.Tag == "button" || element.Role == "button")
        {
            return "button";
        }

        return "interactive";
    }

    private static CdpDomSnapshot CreateEmptySnapshot(CloakBrowserCdpHealthcheckResult result) =>
        new(
            PageMetadata: new CdpPageMetadata(result.Url ?? string.Empty, result.Title ?? string.Empty, "unknown"),
            NodeCount: 0,
            TextPreview: string.Empty,
            InteractiveElements: [],
            FormsCount: 0,
            LinksCount: 0,
            ButtonsCount: 0,
            InputsCount: 0,
            ScreenshotsAvailable: result.ScreenshotCaptured,
            Source: "cloakbrowser-cdp-direct",
            ExtensionUsed: result.ExtensionUsed,
            SystemBrowserUsed: result.SystemBrowserUsed);

    private static async Task<CdpBrowserSkillCaptureResult> WriteEvidenceAsync(
        string repositoryRoot,
        CdpBrowserSkillCaptureResult result,
        CancellationToken cancellationToken)
    {
        var artifactsRoot = Path.Combine(repositoryRoot, "artifacts", "local-verification");
        Directory.CreateDirectory(artifactsRoot);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss-fffZ");
        var evidencePath = Path.Combine(artifactsRoot, "cloakbrowser-cdp-browser-skills-" + timestamp + ".redacted.json");
        var evidence = new
        {
            status = result.Status,
            decision = result.Decision,
            reason = result.Reason,
            captureOk = result.CaptureOk,
            domIndexOk = result.DomIndexOk,
            interactiveElementsDetected = result.DomIndex.InteractiveElementsCount > 0,
            frictionSignalsDetected = result.FrictionSignalsDetected,
            actionMapOk = result.ActionMapOk,
            screenshotCaptured = result.ScreenshotCaptured,
            extensionUsed = result.ExtensionUsed,
            systemBrowserUsed = result.SystemBrowserUsed,
            externalNavigationBlocked = result.ExternalNavigationBlocked,
            shutdownOk = result.RuntimeShutdown && result.ProcessExited && !result.OrphanProcessDetected,
            processExited = result.ProcessExited,
            orphanProcessDetected = result.OrphanProcessDetected,
            pageSummary = result.PageSummary,
            domIndexSummary = new
            {
                result.DomIndex.NodeCount,
                result.DomIndex.HeadingsCount,
                result.DomIndex.ButtonsCount,
                result.DomIndex.InputsCount,
                result.DomIndex.LinksCount,
                result.DomIndex.FormsCount,
                result.DomIndex.DisabledElementsCount,
                result.DomIndex.RequiredEmptyFieldsCount,
                result.DomIndex.IndexedElementsCount,
                result.DomIndex.InteractiveElementsCount,
                result.DomIndex.PageStructureSummary,
                result.DomIndex.StoresRawHtml,
                result.DomIndex.StoresInputValues
            },
            frictionSignals = result.FrictionSignals,
            actionMapSummary = new
            {
                entries = result.ActionMap.Entries.Count,
                result.ActionMap.ExternalNavigationBlocked,
                result.ActionMap.DangerousActionsBlocked
            },
            evidence = result.Evidence,
            timestamp = DateTimeOffset.UtcNow
        };

        await File.WriteAllTextAsync(
                evidencePath,
                JsonSerializer.Serialize(evidence, JsonOptions),
                cancellationToken)
            .ConfigureAwait(false);

        return result with { EvidencePath = evidencePath };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
