using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsMissionControlShellValidator
{
    private readonly NodalOsRedactionService redaction;
    private readonly NodalOsApprovalUxHandoffObservabilityValidator uxValidator;
    private readonly NodalOsApprovalTimelineEvidenceValidator timelineValidator;

    public NodalOsMissionControlShellValidator()
        : this(new NodalOsRedactionService())
    {
    }

    public NodalOsMissionControlShellValidator(NodalOsRedactionService redaction)
        : this(
            redaction,
            new NodalOsApprovalUxHandoffObservabilityValidator(redaction),
            new NodalOsApprovalTimelineEvidenceValidator(redaction))
    {
    }

    public NodalOsMissionControlShellValidator(
        NodalOsRedactionService redaction,
        NodalOsApprovalUxHandoffObservabilityValidator uxValidator,
        NodalOsApprovalTimelineEvidenceValidator timelineValidator)
    {
        this.redaction = redaction;
        this.uxValidator = uxValidator;
        this.timelineValidator = timelineValidator;
    }

    public NodalOsCoreRuntimeValidationResult ValidateShell(NodalOsMissionControlShellPreview shell)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, shell.ShellId, "ShellId is required.");
        if (!string.Equals(shell.ProjectOperationalName, "NODAL OS", StringComparison.Ordinal))
            errors.Add("Mission Control shell operational project name must be NODAL OS.");
        if (shell.CreatedAt == default)
            errors.Add("CreatedAt is required.");

        ValidateReadOnlyFlags(
            shell.ReadOnlyUi,
            shell.CanAuthorizeExecution,
            shell.RuntimeExecutionAllowed,
            shell.RuntimeExecutionDeferred,
            shell.BrowserAutomationAllowed,
            shell.CloudSyncAllowed,
            shell.LlmProviderCallsAllowed,
            errors);

        ValidateTopBar(shell.TopBar, errors);
        ValidateWorkspace(shell.Workspace, errors);
        ValidateApprovalDisplay(shell.ApprovalDisplay, errors, warnings);
        ValidateTimeline(shell.Timeline, errors, warnings);
        ValidateEvidence(shell.Evidence, errors);
        ValidateObservability(shell.Observability, errors);

        foreach (var navigation in shell.Navigation)
        {
            ValidateSafeText(errors, "Navigation.LabelRedacted", navigation.LabelRedacted);
            ValidateSafeText(errors, "Navigation.DisabledReasonRedacted", navigation.DisabledReasonRedacted);
        }

        foreach (var guardrail in shell.GuardrailsSummaryRedacted)
            ValidateSafeText(errors, "GuardrailsSummaryRedacted", guardrail);

        if (!shell.GuardrailsSummaryRedacted.Any(value => value.Contains("Read-only preview", StringComparison.OrdinalIgnoreCase)))
            errors.Add("Mission Control shell must expose read-only preview guardrail.");
        if (!shell.GuardrailsSummaryRedacted.Any(value => value.Contains("No runtime execution", StringComparison.OrdinalIgnoreCase)))
            errors.Add("Mission Control shell must expose no-runtime guardrail.");

        return Result(errors, warnings);
    }

    public string Redact(string? value) => redaction.RedactValue(value).Value;

    public IReadOnlyDictionary<string, string> RedactMetadata(IReadOnlyDictionary<string, string> metadata) =>
        redaction.RedactDictionary(metadata).Values;

    private void ValidateTopBar(NodalOsMissionControlTopBar topBar, List<string> errors)
    {
        AddRequired(errors, topBar.MissionTitleRedacted, "Mission title is required.");
        AddRequired(errors, topBar.OverallStatusRedacted, "Overall status is required.");
        ValidatePercent(topBar.ProgressPercent, "TopBar.ProgressPercent", errors);
        ValidateReadOnlyFlags(
            topBar.ReadOnlyUi,
            canAuthorizeExecution: false,
            topBar.RuntimeExecutionAllowed,
            topBar.RuntimeExecutionDeferred,
            topBar.BrowserAutomationAllowed,
            topBar.CloudSyncAllowed,
            topBar.LlmProviderCallsAllowed,
            errors);
        ValidateSafeText(errors, "TopBar.MissionTitleRedacted", topBar.MissionTitleRedacted);
        ValidateSafeText(errors, "TopBar.OverallStatusRedacted", topBar.OverallStatusRedacted);
    }

    private void ValidateWorkspace(NodalOsMissionControlWorkspace workspace, List<string> errors)
    {
        AddRequired(errors, workspace.ActiveMissionRedacted, "Workspace active mission is required.");
        AddRequired(errors, workspace.SummaryRedacted, "Workspace summary is required.");
        ValidatePercent(workspace.ProgressPercent, "Workspace.ProgressPercent", errors);
        if (!workspace.ReadOnlyUi)
            errors.Add("Workspace must be read-only.");
        ValidateSafeText(errors, "Workspace.ActiveMissionRedacted", workspace.ActiveMissionRedacted);
        ValidateSafeText(errors, "Workspace.SummaryRedacted", workspace.SummaryRedacted);
        foreach (var nextStep in workspace.NextStepsRedacted)
            ValidateSafeText(errors, "Workspace.NextStepsRedacted", nextStep);
        foreach (var guardrail in workspace.GuardrailBadgesRedacted)
            ValidateSafeText(errors, "Workspace.GuardrailBadgesRedacted", guardrail);
    }

    private void ValidateApprovalDisplay(
        NodalOsApprovalDisplayView approvalDisplay,
        List<string> errors,
        List<string> warnings)
    {
        AddRequired(errors, approvalDisplay.ViewId, "Approval display ViewId is required.");
        if (!approvalDisplay.ReadOnlyUi)
            errors.Add("Approval display must be read-only.");
        if (approvalDisplay.CanAuthorizeExecution)
            errors.Add("Approval display cannot authorize execution.");
        if (approvalDisplay.Cards.Count == 0)
            errors.Add("Approval display requires at least one card.");
        if (approvalDisplay.ActionOptions.Count == 0)
            errors.Add("Approval display requires action options.");

        foreach (var card in approvalDisplay.Cards)
            Merge(uxValidator.ValidateApprovalCardPreview(card), errors, warnings);
        foreach (var option in approvalDisplay.ActionOptions)
        {
            if (!option.Disabled)
                errors.Add("Approval display options must be disabled in read-only preview.");
            if (option.CanAuthorizeExecution)
                errors.Add("Approval display options cannot authorize execution.");
            AddRequired(errors, option.LabelRedacted, "Approval option label is required.");
            AddRequired(errors, option.DisabledReasonRedacted, "Approval option disabled reason is required.");
            ValidateSafeText(errors, "ApprovalOption.LabelRedacted", option.LabelRedacted);
            ValidateSafeText(errors, "ApprovalOption.DisabledReasonRedacted", option.DisabledReasonRedacted);
        }
    }

    private void ValidateTimeline(
        NodalOsTimelineDisplayView timeline,
        List<string> errors,
        List<string> warnings)
    {
        AddRequired(errors, timeline.ViewId, "Timeline ViewId is required.");
        if (!timeline.ReadOnlyUi)
            errors.Add("Timeline view must be read-only.");
        if (timeline.Entries.Count == 0)
            errors.Add("Timeline view requires entries.");
        if (!timeline.Entries.SequenceEqual(timeline.Entries.OrderBy(entry => entry.CreatedAt)))
            errors.Add("Timeline entries must be ordered by CreatedAt.");
        foreach (var entry in timeline.Entries)
            Merge(timelineValidator.ValidateTimelineEntry(entry), errors, warnings);
    }

    private void ValidateEvidence(NodalOsEvidenceDisplayView evidence, List<string> errors)
    {
        AddRequired(errors, evidence.ViewId, "Evidence ViewId is required.");
        if (!evidence.ReadOnlyUi)
            errors.Add("Evidence view must be read-only.");
        if (!evidence.RefOnly)
            errors.Add("Evidence view must be ref-only.");
        if (evidence.EvidenceRefs.Count == 0)
            errors.Add("Evidence view requires evidence refs.");
        foreach (var item in evidence.EvidenceRefs)
        {
            AddRequired(errors, item.EvidenceId, "EvidenceId is required.");
            AddRequired(errors, item.KindRedacted, "Evidence kind is required.");
            AddRequired(errors, item.RefRedacted, "Evidence ref is required.");
            if (!item.RefOnly)
                errors.Add("Evidence item must be ref-only.");
            if (item.RawPayloadInline)
                errors.Add("Evidence item cannot expose raw payload inline.");
            ValidateSafeText(errors, "Evidence.KindRedacted", item.KindRedacted);
            ValidateSafeText(errors, "Evidence.RefRedacted", item.RefRedacted);
            foreach (var (key, value) in item.SafeMetadataRedacted)
                ValidateSafeField(errors, key, value);
        }
    }

    private void ValidateObservability(NodalOsObservabilityLogPreview observability, List<string> errors)
    {
        AddRequired(errors, observability.PreviewId, "Observability PreviewId is required.");
        AddRequired(errors, observability.TitleRedacted, "Observability title is required.");
        AddRequired(errors, observability.RuntimeSummaryRedacted, "Observability runtime summary is required.");
        if (!observability.ReadOnlyUi)
            errors.Add("Observability log preview must be read-only.");
        if (observability.CopyReportAvailable)
            errors.Add("Copy report must remain disabled/mock-safe in M480-M482.");
        AddRequired(errors, observability.CopyReportDisabledReasonRedacted, "Copy report disabled reason is required.");
        ValidateSafeText(errors, "Observability.TitleRedacted", observability.TitleRedacted);
        ValidateSafeText(errors, "Observability.RuntimeSummaryRedacted", observability.RuntimeSummaryRedacted);
        ValidateSafeText(errors, "Observability.CopyReportDisabledReasonRedacted", observability.CopyReportDisabledReasonRedacted);
        foreach (var line in observability.LinesRedacted)
            ValidateSafeText(errors, "Observability.LinesRedacted", line);
    }

    private static void ValidateReadOnlyFlags(
        bool readOnlyUi,
        bool canAuthorizeExecution,
        bool runtimeExecutionAllowed,
        bool runtimeExecutionDeferred,
        bool browserAutomationAllowed,
        bool cloudSyncAllowed,
        bool llmProviderCallsAllowed,
        List<string> errors)
    {
        if (!readOnlyUi)
            errors.Add("Mission Control shell must be read-only.");
        if (canAuthorizeExecution)
            errors.Add("Mission Control shell cannot authorize execution.");
        if (runtimeExecutionAllowed)
            errors.Add("Mission Control shell cannot allow runtime execution.");
        if (!runtimeExecutionDeferred)
            errors.Add("Mission Control shell must keep runtime execution deferred.");
        if (browserAutomationAllowed)
            errors.Add("Mission Control shell cannot allow browser automation.");
        if (cloudSyncAllowed)
            errors.Add("Mission Control shell cannot allow cloud sync.");
        if (llmProviderCallsAllowed)
            errors.Add("Mission Control shell cannot allow LLM provider calls.");
    }

    private static void ValidatePercent(int percent, string fieldName, List<string> errors)
    {
        if (percent is < 0 or > 100)
            errors.Add($"{fieldName} must be between 0 and 100.");
    }

    private void ValidateSafeText(List<string> errors, string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (redaction.ContainsSensitiveField(fieldName, value) || redaction.ContainsSensitiveContent(value))
            errors.Add($"{fieldName} contains sensitive content and must be redacted before Mission Control display.");
    }

    private void ValidateSafeField(List<string> errors, string fieldName, string? value)
    {
        if (redaction.ContainsSensitiveField(fieldName, value) || redaction.ContainsSensitiveContent(value))
            errors.Add($"Evidence metadata field {fieldName} contains sensitive content and must be redacted before Mission Control display.");
    }

    private static void AddRequired(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(message);
    }

    private static void Merge(NodalOsCoreRuntimeValidationResult result, List<string> errors, List<string> warnings)
    {
        errors.AddRange(result.Errors);
        warnings.AddRange(result.Warnings);
    }

    private static NodalOsCoreRuntimeValidationResult Result(List<string> errors, List<string> warnings) =>
        new()
        {
            IsValid = errors.Count == 0,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            Errors = errors.Distinct(StringComparer.Ordinal).ToArray(),
            Warnings = warnings.Distinct(StringComparer.Ordinal).ToArray()
        };
}

public sealed class NodalOsMissionControlShellService
{
    private readonly NodalOsMissionControlShellValidator validator;

    public NodalOsMissionControlShellService()
        : this(new NodalOsMissionControlShellValidator())
    {
    }

    public NodalOsMissionControlShellService(NodalOsMissionControlShellValidator validator) =>
        this.validator = validator;

    public NodalOsMissionControlShellPreview CreateShellPreview(
        NodalOsApprovalUxPreview approvalUxPreview,
        NodalOsHandoffDataPack handoffDataPack,
        NodalOsRuntimeObservabilityReport observabilityReport)
    {
        var timelineEntries = approvalUxPreview.TimelineEntries
            .Concat(handoffDataPack.TimelineEntries)
            .GroupBy(entry => entry.TimelineEntryId, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(entry => entry.CreatedAt)
            .ToArray();

        var evidenceRefs = handoffDataPack.EvidenceRefs
            .Concat(approvalUxPreview.Cards.SelectMany(card => card.EvidenceRefs))
            .GroupBy(evidence => evidence.EvidenceId, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();

        return new()
        {
            ShellId = $"mission-control-shell-{Guid.NewGuid():N}",
            ProjectOperationalName = "NODAL OS",
            TopBar = new NodalOsMissionControlTopBar
            {
                MissionTitleRedacted = "NODAL OS Mission Control",
                OverallStatusRedacted = "Read-only preview / runtime blocked",
                ProgressPercent = 64,
                ReadOnlyUi = true,
                RuntimeExecutionAllowed = false,
                RuntimeExecutionDeferred = true,
                BrowserAutomationAllowed = false,
                CloudSyncAllowed = false,
                LlmProviderCallsAllowed = false
            },
            Navigation = CreateNavigation(),
            Workspace = new NodalOsMissionControlWorkspace
            {
                ActiveMissionRedacted = "Mission: Approval and evidence review",
                SummaryRedacted = "Observe core state, approvals, timeline and evidence refs without execution authority.",
                ProgressPercent = 64,
                NextStepsRedacted =
                [
                    "Review approval card details.",
                    "Inspect timeline and evidence refs.",
                    "Copy technical log remains disabled in read-only preview."
                ],
                GuardrailBadgesRedacted = Guardrails(),
                ReadOnlyUi = true
            },
            ApprovalDisplay = new NodalOsApprovalDisplayView
            {
                ViewId = $"approval-display-{Guid.NewGuid():N}",
                Cards = approvalUxPreview.Cards,
                ActionOptions = CreateActionOptions(approvalUxPreview.Cards.SelectMany(card => card.UserOptions).Distinct().ToArray()),
                ReadOnlyUi = true,
                CanAuthorizeExecution = false
            },
            Timeline = new NodalOsTimelineDisplayView
            {
                ViewId = $"timeline-view-{Guid.NewGuid():N}",
                Entries = timelineEntries,
                ReadOnlyUi = true
            },
            Evidence = new NodalOsEvidenceDisplayView
            {
                ViewId = $"evidence-view-{Guid.NewGuid():N}",
                EvidenceRefs = evidenceRefs.Select(ToEvidenceDisplayItem).ToArray(),
                RefOnly = true,
                ReadOnlyUi = true
            },
            Observability = new NodalOsObservabilityLogPreview
            {
                PreviewId = $"observability-preview-{Guid.NewGuid():N}",
                TitleRedacted = "LOG preview",
                RuntimeSummaryRedacted = observabilityReport.ExecutionRegistrySummaryRedacted,
                LinesRedacted =
                [
                    observabilityReport.EventBusSummaryRedacted,
                    observabilityReport.TimelineSummaryRedacted,
                    observabilityReport.ApprovalSummaryRedacted,
                    observabilityReport.EvidenceSummaryRedacted,
                    observabilityReport.GuardrailsSummaryRedacted,
                    observabilityReport.NextRecommendedActionRedacted
                ],
                CopyReportAvailable = false,
                CopyReportDisabledReasonRedacted = "Disabled in read-only preview; copy-report wiring is not implemented.",
                ReadOnlyUi = true
            },
            GuardrailsSummaryRedacted = Guardrails(),
            AttentionFlags =
            [
                NodalOsMissionControlAttentionKind.ApprovalRequired,
                NodalOsMissionControlAttentionKind.RuntimeBlocked
            ],
            ReadOnlyUi = true,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            BrowserAutomationAllowed = false,
            CloudSyncAllowed = false,
            LlmProviderCallsAllowed = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public string RenderReadOnlyHtml(NodalOsMissionControlShellPreview shell)
    {
        var safe = Sanitize(shell);
        var validation = validator.ValidateShell(safe);
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join(" | ", validation.Errors));

        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\">");
        builder.AppendLine("<head>");
        builder.AppendLine("<meta charset=\"utf-8\">");
        builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        builder.AppendLine("<title>NODAL OS Mission Control - Read-only Preview</title>");
        builder.AppendLine("<style>");
        builder.AppendLine(":root{--bg:#0D1117;--panel:#161B22;--card:#1C2128;--border:#30363D;--text:#F5F7FA;--muted:#AAB4C0;--accent:#4F7CFF;--violet:#7C5CFF;--warn:#F2C94C;--danger:#FF667A;}");
        builder.AppendLine("*{box-sizing:border-box}body{margin:0;background:radial-gradient(circle at 20% 0%,rgba(79,124,255,.24),transparent 32%),linear-gradient(135deg,#0D1117,#0A0D12 72%);color:var(--text);font-family:'Aptos Display','Segoe UI',sans-serif;}");
        builder.AppendLine(".shell{min-height:100vh;display:grid;grid-template-columns:220px minmax(0,1fr) 340px;grid-template-rows:76px minmax(0,1fr) 210px;gap:14px;padding:18px}.top,.side,.main,.right,.log{border:1px solid var(--border);background:rgba(22,27,34,.88);backdrop-filter:blur(14px);border-radius:24px;box-shadow:0 20px 70px rgba(0,0,0,.28)}");
        builder.AppendLine(".top{grid-column:1/4;display:flex;align-items:center;justify-content:space-between;padding:0 22px}.brand{font-size:13px;letter-spacing:.18em;text-transform:uppercase;color:var(--muted)}.mission{font-size:24px;font-weight:700}.pills{display:flex;gap:8px;flex-wrap:wrap}.pill{border:1px solid var(--border);border-radius:999px;padding:7px 10px;color:var(--muted);font-size:12px}.pill.hot{color:#fff;background:linear-gradient(135deg,var(--accent),var(--violet));border:0}");
        builder.AppendLine(".side{padding:18px}.nav{display:grid;gap:10px}.nav div{padding:11px 12px;border-radius:14px;background:rgba(28,33,40,.72);color:var(--muted)}.nav div:first-child{color:#fff;border:1px solid rgba(79,124,255,.5)}.main{padding:24px;overflow:hidden}.right{padding:18px;overflow:auto}.log{grid-column:1/4;padding:18px;overflow:auto}.grid{display:grid;grid-template-columns:1.2fr .8fr;gap:14px}.card{background:var(--card);border:1px solid var(--border);border-radius:20px;padding:16px}.headline{font-size:42px;line-height:1;margin:12px 0}.muted{color:var(--muted)}.progress{height:10px;background:#0A0D12;border-radius:999px;overflow:hidden}.progress span{display:block;height:100%;background:linear-gradient(90deg,var(--accent),var(--violet));width:" + safe.TopBar.ProgressPercent + "%}.timeline{display:grid;gap:12px}.event{border-left:2px solid var(--accent);padding-left:12px}.button{display:inline-flex;margin:4px 6px 4px 0;padding:9px 11px;border-radius:12px;border:1px solid var(--border);color:var(--muted);background:#11161D}.disabled{opacity:.62}.evidence{font-family:'Cascadia Mono','Consolas',monospace;font-size:12px;color:#C9D7E8}.warn{color:var(--warn)}@media(max-width:980px){.shell{grid-template-columns:1fr;grid-template-rows:auto}.top,.side,.main,.right,.log{grid-column:1}.grid{grid-template-columns:1fr}}");
        builder.AppendLine("</style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("<main class=\"shell\" data-read-only=\"true\" data-can-authorize-execution=\"false\">");
        builder.AppendLine($"<section class=\"top\"><div><div class=\"brand\">{Html(safe.ProjectOperationalName)} / Mission Control</div><div class=\"mission\">{Html(safe.TopBar.MissionTitleRedacted)}</div></div><div class=\"pills\"><span class=\"pill hot\">Read-only preview</span><span class=\"pill\">No runtime execution</span><span class=\"pill\">No browser automation</span><span class=\"pill\">No cloud sync</span><span class=\"pill\">No LLM provider calls</span></div></section>");
        builder.AppendLine("<aside class=\"side\"><div class=\"brand\">Navigation</div><div class=\"nav\">");
        foreach (var item in safe.Navigation)
            builder.AppendLine($"<div class=\"{(item.Disabled ? "disabled" : string.Empty)}\">{Html(item.LabelRedacted)}</div>");
        builder.AppendLine("</div></aside>");
        builder.AppendLine("<section class=\"main\"><div class=\"headline\">Read-only command surface.</div>");
        builder.AppendLine($"<p class=\"muted\">{Html(safe.Workspace.SummaryRedacted)}</p><div class=\"progress\"><span></span></div>");
        builder.AppendLine("<div class=\"grid\"><div class=\"card\"><h2>Timeline</h2><div class=\"timeline\">");
        foreach (var entry in safe.Timeline.Entries)
            builder.AppendLine($"<div class=\"event\"><strong>{Html(entry.TitleRedacted)}</strong><div class=\"muted\">{Html(entry.MessageRedacted)}</div><div class=\"evidence\">registry: {Html(entry.ExecutionRegistryEntryId ?? "unlinked")}</div></div>");
        builder.AppendLine("</div></div><div class=\"card\"><h2>Evidence refs</h2>");
        foreach (var evidence in safe.Evidence.EvidenceRefs)
            builder.AppendLine($"<div class=\"evidence\">{Html(evidence.KindRedacted)} / {Html(evidence.RefRedacted)}</div>");
        builder.AppendLine("</div></div></section>");
        builder.AppendLine("<aside class=\"right\"><h2>Approval Display</h2>");
        foreach (var card in safe.ApprovalDisplay.Cards)
        {
            builder.AppendLine($"<div class=\"card\"><div class=\"warn\">{Html(card.Severity.ToString())}</div><h3>{Html(card.TitleRedacted)}</h3><p class=\"muted\">{Html(card.FullExplanationRedacted)}</p><div class=\"evidence\">approval: {Html(card.ApprovalCardId)}</div><div class=\"evidence\">evidence refs: {card.EvidenceRefs.Count}</div></div>");
        }
        builder.AppendLine("<h3>Options disabled</h3>");
        foreach (var option in safe.ApprovalDisplay.ActionOptions)
            builder.AppendLine($"<span class=\"button disabled\" aria-disabled=\"true\">{Html(option.LabelRedacted)}</span>");
        builder.AppendLine("</aside>");
        builder.AppendLine("<section class=\"log\"><h2>Observability / LOG Preview</h2>");
        builder.AppendLine($"<p class=\"muted\">{Html(safe.Observability.RuntimeSummaryRedacted)}</p>");
        foreach (var line in safe.Observability.LinesRedacted)
            builder.AppendLine($"<div class=\"evidence\">{Html(line)}</div>");
        builder.AppendLine($"<p class=\"muted\">{Html(safe.Observability.CopyReportDisabledReasonRedacted)}</p>");
        builder.AppendLine("</section></main></body></html>");
        return builder.ToString();
    }

    private NodalOsMissionControlShellPreview Sanitize(NodalOsMissionControlShellPreview shell) =>
        shell with
        {
            ProjectOperationalName = "NODAL OS",
            TopBar = shell.TopBar with
            {
                MissionTitleRedacted = validator.Redact(shell.TopBar.MissionTitleRedacted),
                OverallStatusRedacted = validator.Redact(shell.TopBar.OverallStatusRedacted),
                ReadOnlyUi = true,
                RuntimeExecutionAllowed = false,
                RuntimeExecutionDeferred = true,
                BrowserAutomationAllowed = false,
                CloudSyncAllowed = false,
                LlmProviderCallsAllowed = false
            },
            Workspace = shell.Workspace with
            {
                ActiveMissionRedacted = validator.Redact(shell.Workspace.ActiveMissionRedacted),
                SummaryRedacted = validator.Redact(shell.Workspace.SummaryRedacted),
                NextStepsRedacted = shell.Workspace.NextStepsRedacted.Select(validator.Redact).ToArray(),
                GuardrailBadgesRedacted = shell.Workspace.GuardrailBadgesRedacted.Select(validator.Redact).ToArray(),
                ReadOnlyUi = true
            },
            ApprovalDisplay = shell.ApprovalDisplay with
            {
                ReadOnlyUi = true,
                CanAuthorizeExecution = false,
                ActionOptions = shell.ApprovalDisplay.ActionOptions.Select(option => option with
                {
                    LabelRedacted = validator.Redact(option.LabelRedacted),
                    DisabledReasonRedacted = validator.Redact(option.DisabledReasonRedacted),
                    Disabled = true,
                    CanAuthorizeExecution = false
                }).ToArray()
            },
            Evidence = shell.Evidence with
            {
                RefOnly = true,
                ReadOnlyUi = true,
                EvidenceRefs = shell.Evidence.EvidenceRefs.Select(evidence => evidence with
                {
                    KindRedacted = validator.Redact(evidence.KindRedacted),
                    RefRedacted = validator.Redact(evidence.RefRedacted),
                    SafeMetadataRedacted = validator.RedactMetadata(evidence.SafeMetadataRedacted),
                    RefOnly = true,
                    RawPayloadInline = false
                }).ToArray()
            },
            Observability = shell.Observability with
            {
                TitleRedacted = validator.Redact(shell.Observability.TitleRedacted),
                RuntimeSummaryRedacted = validator.Redact(shell.Observability.RuntimeSummaryRedacted),
                LinesRedacted = shell.Observability.LinesRedacted.Select(validator.Redact).ToArray(),
                CopyReportAvailable = false,
                CopyReportDisabledReasonRedacted = validator.Redact(shell.Observability.CopyReportDisabledReasonRedacted),
                ReadOnlyUi = true
            },
            GuardrailsSummaryRedacted = shell.GuardrailsSummaryRedacted.Select(validator.Redact).ToArray(),
            ReadOnlyUi = true,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            BrowserAutomationAllowed = false,
            CloudSyncAllowed = false,
            LlmProviderCallsAllowed = false
        };

    private static IReadOnlyList<NodalOsMissionControlNavigationItem> CreateNavigation() =>
    [
        new() { PanelKind = NodalOsMissionControlPanelKind.MissionControl, LabelRedacted = "Mission Control", Disabled = false },
        new() { PanelKind = NodalOsMissionControlPanelKind.Timeline, LabelRedacted = "Timeline", Disabled = false },
        new() { PanelKind = NodalOsMissionControlPanelKind.Approvals, LabelRedacted = "Approvals", Disabled = false },
        new() { PanelKind = NodalOsMissionControlPanelKind.Evidence, LabelRedacted = "Evidence", Disabled = false },
        new() { PanelKind = NodalOsMissionControlPanelKind.LogsObservability, LabelRedacted = "Logs / Observability", Disabled = false },
        new() { PanelKind = NodalOsMissionControlPanelKind.SettingsDisabled, LabelRedacted = "Settings", Disabled = true, DisabledReasonRedacted = "Disabled in read-only preview." }
    ];

    private static IReadOnlyList<string> Guardrails() =>
    [
        "Read-only preview",
        "No runtime execution",
        "No browser automation",
        "No cloud sync",
        "No LLM provider calls",
        "Evidence is ref-only"
    ];

    private static IReadOnlyList<NodalOsApprovalDisplayActionOption> CreateActionOptions(
        IReadOnlyList<NodalOsApprovalUserOptionKind> optionKinds) =>
        optionKinds.Select(option => new NodalOsApprovalDisplayActionOption
        {
            OptionKind = option,
            LabelRedacted = LabelFor(option),
            Disabled = true,
            DisabledReasonRedacted = "Disabled in read-only preview. Positive execution gate is not implemented.",
            CanAuthorizeExecution = false
        }).ToArray();

    private NodalOsEvidenceRefDisplayItem ToEvidenceDisplayItem(NodalOsEvidenceBridgeRef evidenceRef) =>
        new()
        {
            EvidenceId = evidenceRef.EvidenceId,
            KindRedacted = validator.Redact(evidenceRef.Kind),
            RefRedacted = validator.Redact(evidenceRef.Ref),
            SourceEventId = null,
            SafeMetadataRedacted = new Dictionary<string, string>
            {
                ["sourceKind"] = evidenceRef.SourceKind.ToString(),
                ["useKind"] = evidenceRef.UseKind.ToString(),
                ["authority"] = evidenceRef.Authority.ToString(),
                ["redactionState"] = evidenceRef.RedactionState.ToString()
            },
            RefOnly = true,
            RawPayloadInline = false
        };

    private static string LabelFor(NodalOsApprovalUserOptionKind option) =>
        option switch
        {
            NodalOsApprovalUserOptionKind.Approve => "Approve",
            NodalOsApprovalUserOptionKind.Reject => "Reject",
            NodalOsApprovalUserOptionKind.RequestChanges => "Request changes",
            NodalOsApprovalUserOptionKind.RequestExplanation => "Request explanation",
            NodalOsApprovalUserOptionKind.Defer => "Defer",
            NodalOsApprovalUserOptionKind.Pause => "Pause",
            NodalOsApprovalUserOptionKind.CopyTechnicalLog => "Copy technical log",
            NodalOsApprovalUserOptionKind.HumanHandoffRequired => "Human handoff required",
            _ => option.ToString()
        };

    private static string Html(string? value) =>
        WebUtility.HtmlEncode(value ?? string.Empty);
}

public sealed class NodalOsMissionControlShellJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    private readonly NodalOsMissionControlShellValidator validator;

    public NodalOsMissionControlShellJsonSerializer()
        : this(new NodalOsMissionControlShellValidator())
    {
    }

    public NodalOsMissionControlShellJsonSerializer(NodalOsMissionControlShellValidator validator) =>
        this.validator = validator;

    public string SerializeShell(NodalOsMissionControlShellPreview shell) =>
        JsonSerializer.Serialize(Sanitize(shell), Options);

    public NodalOsMissionControlShellPreview? DeserializeShell(string json) =>
        JsonSerializer.Deserialize<NodalOsMissionControlShellPreview>(json, Options);

    private NodalOsMissionControlShellPreview Sanitize(NodalOsMissionControlShellPreview shell) =>
        shell with
        {
            ProjectOperationalName = "NODAL OS",
            TopBar = shell.TopBar with
            {
                MissionTitleRedacted = validator.Redact(shell.TopBar.MissionTitleRedacted),
                OverallStatusRedacted = validator.Redact(shell.TopBar.OverallStatusRedacted),
                ReadOnlyUi = true,
                RuntimeExecutionAllowed = false,
                RuntimeExecutionDeferred = true,
                BrowserAutomationAllowed = false,
                CloudSyncAllowed = false,
                LlmProviderCallsAllowed = false
            },
            Navigation = shell.Navigation.Select(item => item with
            {
                LabelRedacted = validator.Redact(item.LabelRedacted),
                DisabledReasonRedacted = validator.Redact(item.DisabledReasonRedacted)
            }).ToArray(),
            Workspace = shell.Workspace with
            {
                ActiveMissionRedacted = validator.Redact(shell.Workspace.ActiveMissionRedacted),
                SummaryRedacted = validator.Redact(shell.Workspace.SummaryRedacted),
                NextStepsRedacted = shell.Workspace.NextStepsRedacted.Select(validator.Redact).ToArray(),
                GuardrailBadgesRedacted = shell.Workspace.GuardrailBadgesRedacted.Select(validator.Redact).ToArray(),
                ReadOnlyUi = true
            },
            ApprovalDisplay = shell.ApprovalDisplay with
            {
                Cards = shell.ApprovalDisplay.Cards.Select(SanitizeApprovalCard).ToArray(),
                ActionOptions = shell.ApprovalDisplay.ActionOptions.Select(option => option with
                {
                    LabelRedacted = validator.Redact(option.LabelRedacted),
                    DisabledReasonRedacted = validator.Redact(option.DisabledReasonRedacted),
                    Disabled = true,
                    CanAuthorizeExecution = false
                }).ToArray(),
                ReadOnlyUi = true,
                CanAuthorizeExecution = false
            },
            Timeline = shell.Timeline with
            {
                Entries = shell.Timeline.Entries.Select(SanitizeTimelineEntry).ToArray(),
                ReadOnlyUi = true
            },
            Evidence = shell.Evidence with
            {
                EvidenceRefs = shell.Evidence.EvidenceRefs.Select(evidence => evidence with
                {
                    KindRedacted = validator.Redact(evidence.KindRedacted),
                    RefRedacted = validator.Redact(evidence.RefRedacted),
                    SafeMetadataRedacted = validator.RedactMetadata(evidence.SafeMetadataRedacted),
                    RefOnly = true,
                    RawPayloadInline = false
                }).ToArray(),
                RefOnly = true,
                ReadOnlyUi = true
            },
            Observability = shell.Observability with
            {
                TitleRedacted = validator.Redact(shell.Observability.TitleRedacted),
                RuntimeSummaryRedacted = validator.Redact(shell.Observability.RuntimeSummaryRedacted),
                LinesRedacted = shell.Observability.LinesRedacted.Select(validator.Redact).ToArray(),
                CopyReportAvailable = false,
                CopyReportDisabledReasonRedacted = validator.Redact(shell.Observability.CopyReportDisabledReasonRedacted),
                ReadOnlyUi = true
            },
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            CanAuthorizeExecution = false,
            BrowserAutomationAllowed = false,
            CloudSyncAllowed = false,
            LlmProviderCallsAllowed = false,
            GuardrailsSummaryRedacted = shell.GuardrailsSummaryRedacted.Select(validator.Redact).ToArray()
        };

    private NodalOsApprovalCardPreview SanitizeApprovalCard(NodalOsApprovalCardPreview card) =>
        card with
        {
            TitleRedacted = validator.Redact(card.TitleRedacted),
            ShortSummaryRedacted = validator.Redact(card.ShortSummaryRedacted),
            FullExplanationRedacted = validator.Redact(card.FullExplanationRedacted),
            AffectedResourcesRedacted = card.AffectedResourcesRedacted.Select(validator.Redact).ToArray(),
            PolicyGateReasonRedacted = validator.Redact(card.PolicyGateReasonRedacted),
            NoRollbackReasonRedacted = validator.Redact(card.NoRollbackReasonRedacted),
            ExpectedEvidenceRedacted = validator.Redact(card.ExpectedEvidenceRedacted),
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            CanAuthorizeExecution = false
        };

    private NodalOsTimelineEntry SanitizeTimelineEntry(NodalOsTimelineEntry entry) =>
        entry with
        {
            TitleRedacted = validator.Redact(entry.TitleRedacted),
            MessageRedacted = validator.Redact(entry.MessageRedacted),
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true
        };
}

public static class NodalOsMissionControlShellFixtures
{
    public static NodalOsMissionControlShellPreview ShellPreview()
    {
        var approvalPreview = NodalOsApprovalUxHandoffObservabilityFixtures.ApprovalUxPreview();
        var handoffPack = NodalOsApprovalUxHandoffObservabilityFixtures.HandoffDataPack();
        var observabilityReport = NodalOsApprovalUxHandoffObservabilityFixtures.RuntimeObservabilityReport();

        return new NodalOsMissionControlShellService().CreateShellPreview(
            approvalPreview,
            handoffPack,
            observabilityReport);
    }
}
