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
