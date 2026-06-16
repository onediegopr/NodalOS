using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsTimelineAdapter
{
    private const string ProductName = "NODAL OS";
    private const string DefaultScope = "internal-local ReadyWithRestrictions";
    private static readonly string[] DefaultBlockedOptions =
    [
        "production/SaaS public",
        "public API",
        "real billing/email",
        "real credentials",
        "sensitive sites",
        "submit/pay/sign/delete",
        "productive recorder/replay",
        "external CDP general-ready"
    ];

    public NodalOsTimeline UserTaskToTimeline(string userIntent)
    {
        var safeIntent = Safe(userIntent);
        return Timeline(
            "timeline-task-structuring",
            "Structured user task",
            safeIntent,
            [
                Step(
                    "understand-request",
                    "Entender pedido",
                    "Resumir objetivo, restricciones y superficies bloqueadas antes de ejecutar.",
                    NodalOsTimelineStepStatus.Done,
                    1,
                    NodalOsTimelineNodeType.UserRequest,
                    [
                        Sub("Identificar objetivo", NodalOsTimelineStepStatus.Done, 1),
                        Sub("Detectar constraints y bloqueos", NodalOsTimelineStepStatus.Done, 2)
                    ],
                    safeNextAction: "Proceed only to local planning under Core policy."),
                Step(
                    "structure-recipe",
                    "Estructurar receta",
                    "Convertir el pedido en pasos operator-facing sin habilitar acciones sensibles.",
                    NodalOsTimelineStepStatus.Planned,
                    2,
                    NodalOsTimelineNodeType.StructuredTask,
                    [
                        Sub("Ordenar pasos", NodalOsTimelineStepStatus.Planned, 1),
                        Sub("Marcar subtareas", NodalOsTimelineStepStatus.Planned, 2)
                    ],
                    safeNextAction: "Present recipe preview for operator review."),
                Step(
                    "validate-safety",
                    "Validar seguridad",
                    "Core debe bloquear credenciales, sitios sensibles, submit, pago, firma y delete.",
                    NodalOsTimelineStepStatus.Blocked,
                    3,
                    NodalOsTimelineNodeType.BlockerStep,
                    [Sub("Bloqueos activos visibles", NodalOsTimelineStepStatus.Blocked, 1)],
                    blockers: [Blocker("Sensitive or mutating actions remain blocked.", "Use only local/read-only safe next action.")],
                    risk: NodalOsTimelineRiskLevel.Prohibited,
                    safeNextAction: "Do not run blocked actions."),
                Step(
                    "next-safe-action",
                    "Próxima acción segura",
                    "Mostrar únicamente la acción local/read-only permitida y sus límites.",
                    NodalOsTimelineStepStatus.Ready,
                    4,
                    NodalOsTimelineNodeType.SafeAction,
                    [],
                    safeNextAction: "Continue with local private preview action if Core approves.")
            ]);
    }

    public NodalOsTimeline RecipeToTimeline(NodalOsRecipeTimelineInput recipe)
    {
        var steps = recipe.Steps.Count == 0 ? ["No recipe steps provided."] : recipe.Steps;
        return Timeline(
            $"timeline-recipe-{SafeId(recipe.RecipeId)}",
            $"Recipe preview: {Safe(recipe.RecipeName)}",
            "Recipe timeline is a presentation view only.",
            steps.Select((label, index) => Step(
                $"recipe-step-{index + 1}",
                Safe(label),
                "Recipe step rendered as timeline; Core still decides execution.",
                NodalOsTimelineStepStatus.Planned,
                index + 1,
                NodalOsTimelineNodeType.RecipeStep,
                [Sub("Preconditions checked by Core", NodalOsTimelineStepStatus.Planned, 1)],
                safeNextAction: "Wait for Core-approved execution.")).ToArray());
    }

    public NodalOsTimeline ExecutionToTimeline(NodalOsExecutionTimelineInput execution) =>
        Timeline(
            $"timeline-execution-{SafeId(execution.RunId)}",
            "Execution progress",
            "Execution timeline uses redacted status only.",
            execution.Steps.Select((input, index) =>
            {
                var status = MapStatus(input.Status);
                var blockers = status is NodalOsTimelineStepStatus.Failed or NodalOsTimelineStepStatus.Blocked
                    ? [Blocker(Safe(input.Error ?? input.Status), "Review blocker before retrying.")]
                    : Array.Empty<NodalOsTimelineBlocker>();
                return Step(
                    $"execution-step-{index + 1}",
                    Safe(input.Label),
                    Safe(input.Error ?? "Execution status is redacted."),
                    status,
                    index + 1,
                    status is NodalOsTimelineStepStatus.Blocked ? NodalOsTimelineNodeType.BlockerStep : NodalOsTimelineNodeType.ExecutionStep,
                    [],
                    blockers: blockers,
                    risk: status is NodalOsTimelineStepStatus.Blocked or NodalOsTimelineStepStatus.Failed ? NodalOsTimelineRiskLevel.High : NodalOsTimelineRiskLevel.Low,
                    safeNextAction: status is NodalOsTimelineStepStatus.Blocked or NodalOsTimelineStepStatus.Failed ? "Stop and review evidence." : "Continue observing.");
            }).ToArray());

    public NodalOsTimeline EvidenceToTimeline(NodalOsEvidenceTimelineInput evidence) =>
        Timeline(
            $"timeline-evidence-{SafeId(evidence.SummaryId)}",
            "Evidence summary",
            "Evidence references are redacted metadata only.",
            evidence.EvidenceRefs.Select((refId, index) => Step(
                $"evidence-step-{index + 1}",
                "Evidence ready",
                "Redacted evidence reference available; no body, DOM, cookies, tokens, or secrets.",
                NodalOsTimelineStepStatus.EvidenceReady,
                index + 1,
                NodalOsTimelineNodeType.EvidenceStep,
                [],
                evidenceRefs: [Evidence(refId, "redacted evidence ref")],
                safeNextAction: "Review redacted reference only.")).ToArray());

    public NodalOsTimeline BlockerToTimeline(NodalOsBlockerTimelineInput blocker) =>
        Timeline(
            "timeline-blocker",
            "Blocker explanation",
            "Blocked state is explicit and cannot be hidden by the timeline.",
            [
                Step(
                    "blocker-step-1",
                    "Bloqueo activo",
                    Safe(blocker.Reason),
                    blocker.NeedsHuman ? NodalOsTimelineStepStatus.NeedsHuman : NodalOsTimelineStepStatus.Blocked,
                    1,
                    blocker.NeedsHuman ? NodalOsTimelineNodeType.HumanIntervention : NodalOsTimelineNodeType.BlockerStep,
                    [],
                    blockers: [Blocker(blocker.Reason, blocker.ExpectedOperatorAction, blocker.BlockedOptions, blocker.NeedsHuman)],
                    risk: NodalOsTimelineRiskLevel.High,
                    safeNextAction: Safe(blocker.ExpectedOperatorAction),
                    human: blocker.NeedsHuman)
            ]);

    public NodalOsTimeline OperatorSummaryToTimeline(NodalOsOperatorSummaryTimelineInput summary) =>
        Timeline(
            "timeline-operator-summary",
            "Operator summary",
            Safe(summary.CurrentDecision),
            [
                Step(
                    "operator-status",
                    "Estado actual",
                    Safe(summary.CurrentDecision),
                    NodalOsTimelineStepStatus.Ready,
                    1,
                    NodalOsTimelineNodeType.StatusSummary,
                    [],
                    evidenceRefs: summary.EvidenceRefs.Select(e => Evidence(e, "operator evidence")).ToArray(),
                    safeNextAction: Safe(summary.SafeNextAction)),
                Step(
                    "operator-blockers",
                    "Bloqueos activos",
                    "Los bloqueos permanecen visibles para el operador.",
                    summary.ActiveBlockers.Count > 0 ? NodalOsTimelineStepStatus.Blocked : NodalOsTimelineStepStatus.Done,
                    2,
                    NodalOsTimelineNodeType.BlockerStep,
                    [],
                    blockers: summary.ActiveBlockers.Select(b => Blocker(b, "Do not bypass; ask Core/human if needed.")).ToArray(),
                    risk: summary.ActiveBlockers.Count > 0 ? NodalOsTimelineRiskLevel.High : NodalOsTimelineRiskLevel.Low,
                    safeNextAction: "Continue only within ReadyWithRestrictions.")
            ]);

    public NodalOsTimeline PrivatePreviewRunToTimeline(NodalOsPrivatePreviewRunTimelineInput run) =>
        Timeline(
            $"timeline-private-preview-run-{SafeId(run.RunId)}",
            "Private preview run summary",
            Safe(run.Decision),
            [
                Step(
                    "preview-allowed",
                    "Allowed local flows",
                    "Internal local/private preview flows only.",
                    NodalOsTimelineStepStatus.Done,
                    1,
                    NodalOsTimelineNodeType.StatusSummary,
                    run.AllowedFlows.Select((flow, index) => Sub(Safe(flow), NodalOsTimelineStepStatus.Done, index + 1)).ToArray(),
                    evidenceRefs: run.EvidenceRefs.Select(e => Evidence(e, "preview evidence")).ToArray(),
                    safeNextAction: "Continue internal local preview only."),
                Step(
                    "preview-denied",
                    "Denied scope",
                    "Production/SaaS/public/sensitive scope remains denied.",
                    NodalOsTimelineStepStatus.Blocked,
                    2,
                    NodalOsTimelineNodeType.BlockerStep,
                    [],
                    blockers: run.BlockedFlows.Select(flow => Blocker(flow, "Keep denied.")).ToArray(),
                    risk: NodalOsTimelineRiskLevel.Prohibited,
                    safeNextAction: "Do not expand scope.")
            ]);

    public NodalOsTimeline IssueTriageToTimeline(NodalOsIssueTriageTimelineInput issue) =>
        Timeline(
            $"timeline-issue-{SafeId(issue.IssueId)}",
            "Issue triage",
            $"{Safe(issue.Severity)} / {Safe(issue.Category)}",
            [
                Step(
                    $"issue-{SafeId(issue.IssueId)}",
                    Safe(issue.IssueId),
                    Safe(issue.Decision),
                    issue.BlocksRun ? NodalOsTimelineStepStatus.Blocked : NodalOsTimelineStepStatus.Warning,
                    1,
                    issue.BlocksRun ? NodalOsTimelineNodeType.BlockerStep : NodalOsTimelineNodeType.OperatorAction,
                    [],
                    blockers: issue.BlocksRun ? [Blocker("Issue blocks run.", "Fix or audit before continuing.")] : [],
                    risk: issue.BlocksRun ? NodalOsTimelineRiskLevel.High : NodalOsTimelineRiskLevel.Low,
                    safeNextAction: issue.BlocksRun ? "Stop preview run." : "Track as minor/internal-only.")
            ]);

    private static NodalOsTimeline Timeline(
        string timelineId,
        string title,
        string originalIntent,
        IReadOnlyList<NodalOsTimelineStep> steps)
    {
        var status = new NodalOsTimelineStatusCard(
            DefaultScope,
            NodalOsTimelineRiskLevel.Low,
            "ReadyWithRestrictions",
            "NODAL OS timeline is presentation only; Core remains authoritative.",
            ReadyWithRestrictions: true,
            ProductionReady: false,
            GrantsAuthority: false,
            Redacted: true);

        return new NodalOsTimeline(
            SafeId(timelineId),
            ProductName,
            Safe(title),
            Safe(originalIntent),
            steps,
            status,
            ReadyWithRestrictions: true,
            ProductionReady: false,
            GrantsAuthority: false,
            Redacted: true);
    }

    private static NodalOsTimelineStep Step(
        string stepId,
        string title,
        string description,
        NodalOsTimelineStepStatus status,
        int order,
        NodalOsTimelineNodeType nodeType,
        IReadOnlyList<NodalOsTimelineSubStep> subSteps,
        IReadOnlyList<NodalOsTimelineEvidenceRef>? evidenceRefs = null,
        IReadOnlyList<NodalOsTimelineBlocker>? blockers = null,
        NodalOsTimelineRiskLevel risk = NodalOsTimelineRiskLevel.Low,
        string safeNextAction = "Observe only until Core decides.",
        bool human = false)
    {
        var normalizedBlockers = blockers ?? [];
        var statusCard = new NodalOsTimelineStatusCard(
            DefaultScope,
            risk,
            status.ToString(),
            "Timeline card is redacted and non-authoritative.",
            ReadyWithRestrictions: true,
            ProductionReady: false,
            GrantsAuthority: false,
            Redacted: true);
        var decision = new NodalOsTimelineDecision(
            Safe(safeNextAction),
            normalizedBlockers.SelectMany(b => b.BlockedOptions).DefaultIfEmpty().Where(o => !string.IsNullOrWhiteSpace(o)).Concat(DefaultBlockedOptions).Distinct(StringComparer.OrdinalIgnoreCase).Select(Safe).ToArray(),
            CoreAuthorityRequired: true,
            HumanInterventionRequired: human,
            GrantsAuthority: false);

        return new NodalOsTimelineStep(
            SafeId(stepId),
            Safe(title),
            Safe(description),
            status,
            order,
            new NodalOsTimelineNode(SafeId($"{stepId}-node"), nodeType, IconId: null),
            subSteps,
            statusCard,
            evidenceRefs ?? [],
            normalizedBlockers,
            decision,
            risk,
            DefaultScope,
            "redacted timeline metadata only; no credentials, cookies, tokens, DOM/body, or raw sensitive data",
            CoreAuthorityRequired: true,
            HumanInterventionRequired: human,
            GrantsAuthority: false,
            Redacted: true);
    }

    private static NodalOsTimelineSubStep Sub(string title, NodalOsTimelineStepStatus status, int order) =>
        new(Safe(title), "", status, order, Redacted: true);

    private static NodalOsTimelineEvidenceRef Evidence(string refId, string label) =>
        new(Safe(refId), Safe(label), Redacted: true);

    private static NodalOsTimelineBlocker Blocker(
        string reason,
        string expectedOperatorAction,
        IReadOnlyList<string>? blockedOptions = null,
        bool needsHuman = false) =>
        new(
            Safe(reason),
            Safe(expectedOperatorAction),
            (blockedOptions is { Count: > 0 } ? blockedOptions : DefaultBlockedOptions).Select(Safe).ToArray(),
            needsHuman,
            Redacted: true);

    private static NodalOsTimelineStepStatus MapStatus(string status)
    {
        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            "passed" or "done" or "completed" or "success" => NodalOsTimelineStepStatus.Done,
            "running" or "inprogress" or "in-progress" => NodalOsTimelineStepStatus.Running,
            "blocked" or "policyblocked" => NodalOsTimelineStepStatus.Blocked,
            "failed" or "error" => NodalOsTimelineStepStatus.Failed,
            "skipped" => NodalOsTimelineStepStatus.Skipped,
            "warning" => NodalOsTimelineStepStatus.Warning,
            "notallowed" or "not-allowed" => NodalOsTimelineStepStatus.NotAllowed,
            _ => NodalOsTimelineStepStatus.Planned
        };
    }

    private static string Safe(string? value) => BrowserCredentialRedactor.Redact(value);

    private static string SafeId(string? value)
    {
        var safe = BrowserCredentialRedactor.Redact(value);
        var chars = safe.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' or ':' ? char.ToLowerInvariant(c) : '-').ToArray();
        var normalized = new string(chars).Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "timeline" : normalized;
    }
}

public sealed class NodalOsTimelineStabilizationReviewer
{
    public NodalOsTimelineStabilizationReview Review(
        IReadOnlyList<NodalOsTimelineUxIssue> issues,
        bool blockersVisible = true,
        bool evidenceRedacted = true,
        bool needsHumanClear = true,
        bool coreAuthorityVisible = true,
        bool uiAuthorizesActions = false,
        bool scopeExpanded = false)
    {
        var redactedIssues = issues.Select(RedactIssue).ToArray();
        var decision = Resolve(redactedIssues, blockersVisible, evidenceRedacted, needsHumanClear, coreAuthorityVisible, uiAuthorizesActions, scopeExpanded);
        return new NodalOsTimelineStabilizationReview(
            "timeline-stabilization-review-m160-m162",
            decision,
            redactedIssues,
            blockersVisible,
            evidenceRedacted,
            needsHumanClear,
            coreAuthorityVisible,
            uiAuthorizesActions,
            scopeExpanded,
            Redacted: true);
    }

    public NodalOsTimelineInternalPreviewRun CreateDefaultRun(string commit)
    {
        var issues = new[]
        {
            new NodalOsTimelineUxIssue(
                "tl-readability-001",
                NodalOsTimelineUxIssueCategory.TimelineReadability,
                NodalOsTimelineUxIssueSeverity.Info,
                NodalOsTimelineUxIssueDecision.AcceptForInternalOnly,
                "Timeline is readable in the narrow side panel; monitor spacing as recipes grow.",
                BlocksTimelineStabilization: false,
                Redacted: true)
        };

        return new NodalOsTimelineInternalPreviewRun(
            "m160-m162",
            BrowserCredentialRedactor.Redact(commit),
            "internal-local ReadyWithRestrictions; timeline presentation only",
            [
                "task structuring",
                "recipe preview",
                "recipe execution summary",
                "evidence/log summary",
                "blocker explanation",
                "needs-human/human intervention",
                "operator summary",
                "private preview run summary",
                "issue triage summary"
            ],
            [
                "local task structuring",
                "local recipe preview",
                "redacted evidence review",
                "local issue triage",
                "operator blocker review"
            ],
            [
                "production/SaaS public",
                "public API real",
                "billing/email real",
                "real credentials",
                "sensitive sites",
                "submit/pay/sign/delete",
                "productive recorder/replay",
                "external CDP general-ready"
            ],
            [
                "vertical nodes visible",
                "vertical connector visible",
                "subtasks indented",
                "status cards and badges visible",
                "blocker cards visible",
                "evidence refs redacted",
                "needs-human and Core authority visible"
            ],
            [
                "timeline-ui:sidepanel-renderer:m157-m159",
                "timeline-adr:m157-m159",
                "release-candidate:verified-internal-local-use",
                "ledger:m51:verified:redacted",
                "ledger:m65:verified:redacted"
            ],
            issues,
            "TimelineStableForInternalPreview",
            ScopeExpanded: false,
            Redacted: true);
    }

    private static NodalOsTimelineStabilizationDecision Resolve(
        IReadOnlyList<NodalOsTimelineUxIssue> issues,
        bool blockersVisible,
        bool evidenceRedacted,
        bool needsHumanClear,
        bool coreAuthorityVisible,
        bool uiAuthorizesActions,
        bool scopeExpanded)
    {
        if (scopeExpanded || uiAuthorizesActions ||
            issues.Any(issue => issue.Category == NodalOsTimelineUxIssueCategory.TimelineScopeInflationRisk && issue.BlocksTimelineStabilization))
            return NodalOsTimelineStabilizationDecision.TimelineBlockedByScopeInflation;

        if (!evidenceRedacted ||
            issues.Any(issue => issue.Category == NodalOsTimelineUxIssueCategory.TimelineSecurityLeakRisk && issue.BlocksTimelineStabilization))
            return NodalOsTimelineStabilizationDecision.TimelineBlockedBySecurityLeak;

        if (!coreAuthorityVisible || !blockersVisible || !needsHumanClear)
            return NodalOsTimelineStabilizationDecision.TimelineNeedsAccessibilityFixes;

        if (issues.Any(issue =>
            issue.Category is NodalOsTimelineUxIssueCategory.TimelineLayout or NodalOsTimelineUxIssueCategory.TimelineReadability or NodalOsTimelineUxIssueCategory.TimelineAccessibility &&
            issue.Severity is NodalOsTimelineUxIssueSeverity.Low or NodalOsTimelineUxIssueSeverity.Medium))
            return NodalOsTimelineStabilizationDecision.TimelineContinueWithMinorFixes;

        return NodalOsTimelineStabilizationDecision.TimelineStableForInternalPreview;
    }

    private static NodalOsTimelineUxIssue RedactIssue(NodalOsTimelineUxIssue issue) =>
        issue with
        {
            Summary = BrowserCredentialRedactor.Redact(issue.Summary),
            BlocksTimelineStabilization = issue.BlocksTimelineStabilization ||
                issue.Severity is NodalOsTimelineUxIssueSeverity.Critical or NodalOsTimelineUxIssueSeverity.High &&
                issue.Category is NodalOsTimelineUxIssueCategory.TimelineSecurityLeakRisk or NodalOsTimelineUxIssueCategory.TimelineScopeInflationRisk,
            Redacted = true
        };
}

public sealed class NodalOsExecutionPlanPreviewService
{
    private static readonly NodalOsPlanSensitiveAction[] BlockingSensitiveActions =
    [
        NodalOsPlanSensitiveAction.CredentialEntry,
        NodalOsPlanSensitiveAction.Login,
        NodalOsPlanSensitiveAction.Captcha,
        NodalOsPlanSensitiveAction.TwoFactor,
        NodalOsPlanSensitiveAction.Submit,
        NodalOsPlanSensitiveAction.Payment,
        NodalOsPlanSensitiveAction.Sign,
        NodalOsPlanSensitiveAction.Delete,
        NodalOsPlanSensitiveAction.SensitiveSite,
        NodalOsPlanSensitiveAction.ProductiveRecorderReplay
    ];

    public NodalOsExecutionPlanPreview Draft(
        string planId,
        string goal,
        IReadOnlyList<string> stepTitles,
        IReadOnlyList<string>? allowedDomains = null,
        IReadOnlyList<string>? deniedDomains = null,
        IReadOnlyList<NodalOsPlanSensitiveAction>? sensitiveActions = null)
    {
        var detected = (sensitiveActions ?? []).Where(action => action != NodalOsPlanSensitiveAction.None).Distinct().ToArray();
        var blocksByPolicy = detected.Intersect(BlockingSensitiveActions).Any();
        var status = blocksByPolicy ? NodalOsPlanPreviewStatus.ExecutionBlockedByPolicy : NodalOsPlanPreviewStatus.PlanDrafted;
        return Create(planId, goal, stepTitles, status, allowedDomains, deniedDomains, detected);
    }

    public NodalOsExecutionPlanPreview MarkPreviewReady(NodalOsExecutionPlanPreview preview)
    {
        if (preview.Status == NodalOsPlanPreviewStatus.ExecutionBlockedByPolicy)
            return preview;
        return preview with { Status = preview.HumanApprovalRequired ? NodalOsPlanPreviewStatus.PlanAwaitingApproval : NodalOsPlanPreviewStatus.PlanPreviewReady };
    }

    public NodalOsExecutionPlanPreview AwaitingApproval(NodalOsExecutionPlanPreview preview) =>
        preview with { Status = NodalOsPlanPreviewStatus.PlanAwaitingApproval };

    public NodalOsExecutionPlanPreview Reject(NodalOsExecutionPlanPreview preview) =>
        preview with { Status = NodalOsPlanPreviewStatus.PlanRejected, ExecutesAutomatically = false };

    private static NodalOsExecutionPlanPreview Create(
        string planId,
        string goal,
        IReadOnlyList<string> stepTitles,
        NodalOsPlanPreviewStatus status,
        IReadOnlyList<string>? allowedDomains,
        IReadOnlyList<string>? deniedDomains,
        IReadOnlyList<NodalOsPlanSensitiveAction> sensitiveActions)
    {
        var humanApprovalRequired = sensitiveActions.Count > 0;
        var steps = (stepTitles.Count == 0 ? ["Review current local state"] : stepTitles)
            .Select((title, index) => Step(title, index + 1, sensitiveActions))
            .ToArray();
        var risks = steps.Select(step => step.Risk).Distinct().ToArray();
        var approvals = steps.Select(step => step.ApprovalRequirement).Distinct().ToArray();
        var evidence = steps.SelectMany(step => step.EvidenceRequirements).Distinct().ToArray();
        var blockedOptions = new[]
        {
            "credentials",
            "login/captcha/2FA automation",
            "submit/pay/sign/delete",
            "sensitive sites",
            "production/SaaS public",
            "public API real",
            "external CDP general-ready",
            "productive recorder/replay"
        };

        return new NodalOsExecutionPlanPreview(
            BrowserCredentialRedactor.Redact(planId),
            BrowserCredentialRedactor.Redact(goal),
            status,
            DateTimeOffset.UtcNow,
            steps,
            (allowedDomains ?? ["local-private-preview"]).Select(BrowserCredentialRedactor.Redact).ToArray(),
            (deniedDomains ?? ["production", "public-saas", "sensitive-sites", "external-general"]).Select(BrowserCredentialRedactor.Redact).ToArray(),
            risks,
            approvals,
            evidence,
            new NodalOsPlanPolicySummary(
                CoreAuthorityRequired: true,
                UiAuthorityBlocked: true,
                AutoExecutionBlocked: true,
                SensitiveActionsBlocked: true,
                ProductionBlocked: true,
                ExternalGeneralBlocked: true,
                blockedOptions,
                Redacted: true),
            sensitiveActions,
            humanApprovalRequired,
            CoreAuthorityRequired: true,
            UiAuthorityBlocked: true,
            ExecutesAutomatically: false,
            TimelineCompatibilityMapping: "plan steps map to existing NodalOsTimelineStep objects and render through renderTimeline",
            RedactionSummary: "plan preview metadata only; no secrets, cookies, tokens, raw DOM/body, credentials, or sensitive payloads",
            Redacted: true);
    }

    private static NodalOsExecutionPlanStep Step(string title, int order, IReadOnlyList<NodalOsPlanSensitiveAction> sensitiveActions)
    {
        var hasSensitive = sensitiveActions.Count > 0;
        return new NodalOsExecutionPlanStep(
            $"plan-step-{order}",
            order,
            BrowserCredentialRedactor.Redact(title),
            hasSensitive ? "Sensitive action detected; execution is blocked or requires human/Core policy review." : "Visible plan step; preview only.",
            hasSensitive ? NodalOsPlanRisk.Prohibited : NodalOsPlanRisk.Low,
            hasSensitive ? NodalOsPlanApprovalRequirement.AlwaysBlocked : NodalOsPlanApprovalRequirement.CoreApprovalRequired,
            hasSensitive ? [NodalOsPlanEvidenceRequirement.PolicyDecisionRef, NodalOsPlanEvidenceRequirement.HumanApprovalRef] : [NodalOsPlanEvidenceRequirement.RedactedEvidenceRef],
            sensitiveActions,
            HumanApprovalRequired: hasSensitive,
            CoreAuthorityRequired: true,
            ExecutesAutomatically: false,
            TimelineNodeType: hasSensitive ? NodalOsTimelineNodeType.BlockerStep.ToString() : NodalOsTimelineNodeType.StructuredTask.ToString(),
            RedactionSummary: "redacted plan step only");
    }
}

public sealed class NodalOsPlanPreviewToTimelineAdapter
{
    public NodalOsTimeline Map(NodalOsExecutionPlanPreview preview)
    {
        var status = preview.Status == NodalOsPlanPreviewStatus.ExecutionBlockedByPolicy
            ? NodalOsTimelineStepStatus.Blocked
            : preview.Status == NodalOsPlanPreviewStatus.PlanAwaitingApproval
                ? NodalOsTimelineStepStatus.NeedsHuman
                : NodalOsTimelineStepStatus.Planned;
        var steps = preview.Steps.Select(step =>
        {
            var stepStatus = step.SensitiveActionsDetected.Count > 0 ? NodalOsTimelineStepStatus.Blocked : status;
            var blockers = step.SensitiveActionsDetected.Count > 0
                ? [new NodalOsTimelineBlocker(
                    "Sensitive action detected in plan preview.",
                    "Do not execute; Core policy and human review required.",
                    preview.PolicySummary.BlockedOptions,
                    NeedsHuman: true,
                    Redacted: true)]
                : Array.Empty<NodalOsTimelineBlocker>();
            return new NodalOsTimelineStep(
                step.StepId,
                step.Title,
                step.Description,
                stepStatus,
                step.Order,
                new NodalOsTimelineNode($"{step.StepId}-node", Enum.Parse<NodalOsTimelineNodeType>(step.TimelineNodeType), IconId: null),
                [],
                new NodalOsTimelineStatusCard(
                    "plan-preview internal-local",
                    MapRisk(step.Risk),
                    preview.Status.ToString(),
                    "Plan preview is Core-owned and UI-rendered.",
                    ReadyWithRestrictions: true,
                    ProductionReady: false,
                    GrantsAuthority: false,
                    Redacted: true),
                step.EvidenceRequirements.Select(req => new NodalOsTimelineEvidenceRef($"plan-evidence:{req}", req.ToString(), Redacted: true)).ToArray(),
                blockers,
                new NodalOsTimelineDecision(
                    step.SensitiveActionsDetected.Count > 0 ? "Stop; do not execute sensitive action." : "Render plan preview and wait for Core decision.",
                    preview.PolicySummary.BlockedOptions,
                    CoreAuthorityRequired: true,
                    HumanInterventionRequired: step.HumanApprovalRequired,
                    GrantsAuthority: false),
                MapRisk(step.Risk),
                "plan-preview internal-local",
                step.RedactionSummary,
                CoreAuthorityRequired: true,
                HumanInterventionRequired: step.HumanApprovalRequired,
                GrantsAuthority: false,
                Redacted: true);
        }).ToArray();

        return new NodalOsTimeline(
            $"timeline-plan-preview-{preview.PlanId}",
            "NODAL OS",
            "Plan preview",
            preview.Goal,
            steps,
            new NodalOsTimelineStatusCard(
                "plan-preview internal-local",
                steps.Any(step => step.RiskLevel == NodalOsTimelineRiskLevel.Prohibited) ? NodalOsTimelineRiskLevel.Prohibited : NodalOsTimelineRiskLevel.Low,
                preview.Status.ToString(),
                "Plan preview feeds existing timeline; it does not execute automatically.",
                ReadyWithRestrictions: true,
                ProductionReady: false,
                GrantsAuthority: false,
                Redacted: true),
            ReadyWithRestrictions: true,
            ProductionReady: false,
            GrantsAuthority: false,
            Redacted: true);
    }

    private static NodalOsTimelineRiskLevel MapRisk(NodalOsPlanRisk risk) =>
        risk switch
        {
            NodalOsPlanRisk.None => NodalOsTimelineRiskLevel.None,
            NodalOsPlanRisk.Low => NodalOsTimelineRiskLevel.Low,
            NodalOsPlanRisk.Medium => NodalOsTimelineRiskLevel.Medium,
            NodalOsPlanRisk.High => NodalOsTimelineRiskLevel.High,
            NodalOsPlanRisk.Critical => NodalOsTimelineRiskLevel.Critical,
            NodalOsPlanRisk.Prohibited => NodalOsTimelineRiskLevel.Prohibited,
            _ => NodalOsTimelineRiskLevel.Low
        };
}

public sealed class NodalOsRuntimeStagnationDetector
{
    public IReadOnlyList<NodalOsRuntimeStagnationSignal> Detect(
        IReadOnlyList<NodalOsRuntimeProgressSnapshot> snapshots,
        int threshold = 3)
    {
        var safeSnapshots = snapshots.Select(RedactSnapshot).ToArray();
        if (safeSnapshots.Length == 0)
            return [];

        var signals = new List<NodalOsRuntimeStagnationSignal>();
        AddRepeated(signals, safeSnapshots, threshold, snapshot => snapshot.Url, NodalOsStagnationKind.RepeatedUrl);
        AddRepeated(signals, safeSnapshots, threshold, snapshot => snapshot.DomHash, NodalOsStagnationKind.RepeatedDomHash);
        AddRepeated(signals, safeSnapshots, threshold, snapshot => snapshot.ScreenshotHash, NodalOsStagnationKind.RepeatedScreenshotHash);
        AddRepeated(signals, safeSnapshots, threshold, snapshot => snapshot.Action, NodalOsStagnationKind.RepeatedAction);
        AddRepeated(signals, safeSnapshots, threshold, snapshot => $"{snapshot.Selector}:{snapshot.Error}", NodalOsStagnationKind.SelectorRepeatedFailure);
        AddRepeated(signals, safeSnapshots, threshold, snapshot => $"{snapshot.Action}:{snapshot.Selector}", NodalOsStagnationKind.SameTargetRepeatedAction);

        var clickNoVisual = safeSnapshots.Count(snapshot =>
            snapshot.Action.Contains("click", StringComparison.OrdinalIgnoreCase) && !snapshot.VisualChanged);
        if (clickNoVisual >= threshold)
            signals.Add(Signal(safeSnapshots[^1], NodalOsStagnationKind.ClickNoVisualChange, clickNoVisual, threshold, NodalOsRecoveryRecommendation.Replan));

        var pageNotLoaded = safeSnapshots.Count(snapshot => !snapshot.PageLoaded);
        if (pageNotLoaded >= threshold)
            signals.Add(Signal(safeSnapshots[^1], NodalOsStagnationKind.PageNotLoaded, pageNotLoaded, threshold, NodalOsRecoveryRecommendation.Retry));

        if (safeSnapshots.Any(snapshot => snapshot.CaptchaLoginTwoFactorDetected))
            signals.Add(Signal(safeSnapshots[^1], NodalOsStagnationKind.CaptchaLoginTwoFactorDetected, 1, 1, NodalOsRecoveryRecommendation.AskHuman, NodalOsStagnationSeverity.Blocked));

        return signals.DistinctBy(signal => signal.Kind).ToArray();
    }

    private static void AddRepeated(
        List<NodalOsRuntimeStagnationSignal> signals,
        IReadOnlyList<NodalOsRuntimeProgressSnapshot> snapshots,
        int threshold,
        Func<NodalOsRuntimeProgressSnapshot, string> selector,
        NodalOsStagnationKind kind)
    {
        var grouped = snapshots.Select(selector)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .FirstOrDefault();
        if (grouped is null || grouped.Count() < threshold)
            return;

        var recommendation = kind switch
        {
            NodalOsStagnationKind.RepeatedAction or NodalOsStagnationKind.SameTargetRepeatedAction => NodalOsRecoveryRecommendation.Replan,
            NodalOsStagnationKind.SelectorRepeatedFailure => NodalOsRecoveryRecommendation.AskHuman,
            _ => NodalOsRecoveryRecommendation.Retry
        };
        var severity = kind is NodalOsStagnationKind.RepeatedAction or NodalOsStagnationKind.SameTargetRepeatedAction or NodalOsStagnationKind.SelectorRepeatedFailure
            ? NodalOsStagnationSeverity.Blocked
            : NodalOsStagnationSeverity.Warning;
        signals.Add(Signal(snapshots[^1], kind, grouped.Count(), threshold, recommendation, severity));
    }

    private static NodalOsRuntimeStagnationSignal Signal(
        NodalOsRuntimeProgressSnapshot snapshot,
        NodalOsStagnationKind kind,
        int observed,
        int threshold,
        NodalOsRecoveryRecommendation recommendation,
        NodalOsStagnationSeverity? severity = null) =>
        new(
            $"stagnation-{kind.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}",
            snapshot.RuntimeId,
            snapshot.TabId,
            snapshot.StepId,
            kind,
            severity ?? (observed >= threshold ? NodalOsStagnationSeverity.Warning : NodalOsStagnationSeverity.Info),
            observed,
            threshold,
            recommendation,
            [$"stagnation:{kind}:redacted"],
            DateTimeOffset.UtcNow,
            "redacted runtime metadata only; no cookies, tokens, credentials, DOM/body, or screenshot content",
            GrantsAuthority: false,
            Redacted: true);

    private static NodalOsRuntimeProgressSnapshot RedactSnapshot(NodalOsRuntimeProgressSnapshot snapshot) =>
        snapshot with
        {
            Url = BrowserCredentialRedactor.Redact(snapshot.Url),
            DomHash = BrowserCredentialRedactor.Redact(snapshot.DomHash),
            ScreenshotHash = BrowserCredentialRedactor.Redact(snapshot.ScreenshotHash),
            Action = BrowserCredentialRedactor.Redact(snapshot.Action),
            Selector = BrowserCredentialRedactor.Redact(snapshot.Selector),
            Error = BrowserCredentialRedactor.Redact(snapshot.Error),
            Redacted = true
        };
}

public sealed class NodalOsRecoveryUxService
{
    public NodalOsRecoveryDecision CreateDecision(NodalOsRuntimeStagnationSignal signal)
    {
        var state = signal.Kind switch
        {
            NodalOsStagnationKind.CaptchaLoginTwoFactorDetected => NodalOsRecoveryState.BlockedByCaptchaLoginTwoFactor,
            NodalOsStagnationKind.SelectorRepeatedFailure => NodalOsRecoveryState.WaitingForHumanInput,
            NodalOsStagnationKind.RepeatedAction or NodalOsStagnationKind.SameTargetRepeatedAction => NodalOsRecoveryState.ReplanSuggested,
            NodalOsStagnationKind.ClickNoVisualChange => NodalOsRecoveryState.ReplanSuggested,
            NodalOsStagnationKind.PageNotLoaded => NodalOsRecoveryState.RetrySuggested,
            _ => NodalOsRecoveryState.RecoveryRequired
        };
        var explanation = new NodalOsRecoveryExplanation(
            $"Detected {signal.Kind} after {signal.ObservedCount}/{signal.Threshold} observations.",
            MessageFor(state),
            HumanActionFor(state),
            signal.EvidenceRefs,
            "redacted recovery explanation only",
            Redacted: true);
        return new NodalOsRecoveryDecision(
            $"recovery-{Guid.NewGuid():N}",
            state,
            signal,
            explanation,
            OptionsFor(state),
            NextSafeActionFor(state),
            CoreAuthorityRequired: true,
            UiAuthorityBlocked: true,
            GrantsAuthority: false,
            Redacted: true);
    }

    private static IReadOnlyList<NodalOsRecoveryOption> OptionsFor(NodalOsRecoveryState state)
    {
        var baseOptions = new List<NodalOsRecoveryOption>
        {
            Option("retry", "Reintentar", safe: state is NodalOsRecoveryState.RetrySuggested or NodalOsRecoveryState.RecoveryRequired, human: false),
            Option("replan", "Replanificar", safe: true, human: false),
            Option("ask-human", "Pedir ayuda humana", safe: true, human: true),
            Option("partial-evidence", "Continuar con evidencia parcial", safe: true, human: false),
            Option("finish", "Finalizar", safe: true, human: false),
            Option("copy-log", "Copiar LOG", safe: true, human: false),
            Option("view-evidence", "Ver evidencia", safe: true, human: false),
            Option("report-issue", "Reportar issue", safe: true, human: false)
        };
        return baseOptions;
    }

    private static NodalOsRecoveryOption Option(string id, string label, bool safe, bool human) =>
        new(id, label, safe, RequiresCoreAuthority: true, RequiresHumanInput: human, ExecutesSensitiveWorkaround: false);

    private static string MessageFor(NodalOsRecoveryState state) =>
        state switch
        {
            NodalOsRecoveryState.BlockedByCaptchaLoginTwoFactor => "CAPTCHA/login/2FA detected. NODAL OS will not bypass it.",
            NodalOsRecoveryState.WaitingForHumanInput => "Repeated selector failure requires human review.",
            NodalOsRecoveryState.ReplanSuggested => "No progress detected. Replan before retrying.",
            NodalOsRecoveryState.RetrySuggested => "Page did not load consistently. Retry can be considered by Core.",
            _ => "Runtime stagnation detected. Review recovery options."
        };

    private static string HumanActionFor(NodalOsRecoveryState state) =>
        state == NodalOsRecoveryState.BlockedByCaptchaLoginTwoFactor
            ? "Complete or cancel the sensitive step manually; do not share credentials, OTP, tokens, or cookies."
            : "Review the cause and choose a safe option; Core remains authoritative.";

    private static string NextSafeActionFor(NodalOsRecoveryState state) =>
        state switch
        {
            NodalOsRecoveryState.BlockedByCaptchaLoginTwoFactor => "Ask human; no bypass.",
            NodalOsRecoveryState.ReplanSuggested => "Replan with Core approval.",
            NodalOsRecoveryState.RetrySuggested => "Retry only if Core approves and no sensitive action is involved.",
            _ => "Stop with redacted evidence or ask human."
        };
}

public sealed class NodalOsRecoveryTimelineAdapter
{
    public NodalOsTimeline Map(NodalOsRecoveryDecision recovery)
    {
        var step = new NodalOsTimelineStep(
            $"recovery-step-{recovery.RecoveryId}",
            "Recovery required",
            recovery.Explanation.OperatorMessage,
            recovery.State is NodalOsRecoveryState.WaitingForHumanInput or NodalOsRecoveryState.BlockedByCaptchaLoginTwoFactor
                ? NodalOsTimelineStepStatus.NeedsHuman
                : NodalOsTimelineStepStatus.Blocked,
            1,
            new NodalOsTimelineNode("recovery-node", NodalOsTimelineNodeType.HumanIntervention, IconId: null),
            recovery.Options.Select((option, index) => new NodalOsTimelineSubStep(option.Label, option.Safe ? "Safe option" : "Blocked option", NodalOsTimelineStepStatus.Planned, index + 1, Redacted: true)).ToArray(),
            new NodalOsTimelineStatusCard(
                "recovery internal-local",
                NodalOsTimelineRiskLevel.High,
                recovery.State.ToString(),
                "Recovery UX is visible only and does not execute workarounds.",
                ReadyWithRestrictions: true,
                ProductionReady: false,
                GrantsAuthority: false,
                Redacted: true),
            recovery.Explanation.EvidenceRefs.Select(refId => new NodalOsTimelineEvidenceRef(refId, "recovery evidence", Redacted: true)).ToArray(),
            [new NodalOsTimelineBlocker(recovery.Explanation.Cause, recovery.Explanation.RequiredHumanAction, ["credentials", "captcha/2FA bypass", "submit/pay/sign/delete", "sensitive workaround"], NeedsHuman: true, Redacted: true)],
            new NodalOsTimelineDecision(recovery.NextSafeAction, ["credentials", "captcha/2FA bypass", "submit/pay/sign/delete", "sensitive workaround"], CoreAuthorityRequired: true, HumanInterventionRequired: true, GrantsAuthority: false),
            NodalOsTimelineRiskLevel.High,
            "recovery internal-local",
            recovery.Explanation.RedactionSummary,
            CoreAuthorityRequired: true,
            HumanInterventionRequired: true,
            GrantsAuthority: false,
            Redacted: true);

        return new NodalOsTimeline(
            $"timeline-recovery-{recovery.RecoveryId}",
            "NODAL OS",
            "Recovery UX",
            recovery.Explanation.Cause,
            [step],
            step.StatusCard,
            ReadyWithRestrictions: true,
            ProductionReady: false,
            GrantsAuthority: false,
            Redacted: true);
    }
}
