namespace OneBrain.Core.Recipes;

public sealed record ReliableRecipeOperatorReviewPack(
    string PackId,
    string SubjectId,
    ReliableRecipeOperatorReviewSubjectKind SubjectKind,
    ReliableRecipeOperatorReviewDecision OverallDecision,
    string ExecutiveSummary,
    IReadOnlyList<ReliableRecipeOperatorReviewRow> ReviewRows,
    ReliableRecipeOperatorApprovalLanguage ApprovalLanguage,
    ReliableRecipeOperatorHandoffSummary HandoffSummary,
    ReliableRecipeOperatorAuditSummary AuditSummary,
    string AdapterGateSummary,
    IReadOnlyList<string> ProtectedScopeSummary,
    IReadOnlyList<ReliableRecipeOperatorRecommendedAction> RecommendedOperatorActions,
    string NoRuntimeNotice)
{
    public bool ReadOnly => true;
    public bool FixtureOnly => true;
    public bool RuntimeEnabled => false;
    public bool AdapterRuntimeEnabled => false;
    public bool RuntimeActionExposed => false;
}

public enum ReliableRecipeOperatorReviewSubjectKind
{
    Recipe,
    RecorderDraft,
    EvalScenario,
    SandboxReadinessScenario,
    PerceptionScenario,
    StructuredPrerequisiteAuthoringReport,
    DryRunAdapterReadinessReport
}

public enum ReliableRecipeOperatorReviewDecision
{
    ReadyForHumanReview,
    NeedsOperatorDecision,
    BlockedByCriticalGaps,
    BlockedByRejectedCriticalRequirement,
    DesignOnlyAcceptedForFixture,
    RuntimeStillBlocked
}

public sealed record ReliableRecipeOperatorReviewRow(
    string RowId,
    ReliableRecipeOperatorReviewCategory Category,
    string Title,
    ReliableRecipeOperatorReviewStatus Status,
    ReliableRecipeQualitySeverity Severity,
    string Message,
    string RequiredOperatorAction,
    string? RelatedProposalId,
    string? RelatedRequirementId,
    string AdapterGateImpact);

public enum ReliableRecipeOperatorReviewCategory
{
    Evidence,
    Validation,
    Perception,
    RecorderDraft,
    EvalHarness,
    SandboxReadiness,
    AdapterGate,
    HumanApproval,
    ProtectedScope,
    RuntimeBoundary
}

public enum ReliableRecipeOperatorReviewStatus
{
    AcceptedForFixture,
    PendingReview,
    RejectedUnsafe,
    Deferred,
    MissingCritical,
    MappedLegacyWarning,
    Blocked,
    Info
}

public sealed record ReliableRecipeOperatorApprovalLanguage(
    string Title,
    string ShortSummary,
    string WhatWillChange,
    string WhatWillNotChange,
    string RiskStatement,
    IReadOnlyList<string> AllowedDecisionLabels,
    IReadOnlyList<string> ForbiddenDecisionLabels,
    string NoRuntimeNotice);

public sealed record ReliableRecipeOperatorHandoffSummary(
    string Title,
    string Summary,
    IReadOnlyList<string> CriticalBlockers,
    IReadOnlyList<string> PendingHumanDecisions,
    IReadOnlyList<string> EvidenceGaps,
    IReadOnlyList<string> ValidationGaps,
    IReadOnlyList<string> NextAllowedActions,
    IReadOnlyList<string> ForbiddenActions,
    bool ExternalAuditRequired,
    string NoRuntimeNotice);

public sealed record ReliableRecipeOperatorAuditSummary(
    string AuditDecision,
    IReadOnlyList<string> PassedChecks,
    IReadOnlyList<string> BlockedChecks,
    IReadOnlyList<string> ProtectedScopesUntouched,
    IReadOnlyList<string> RuntimeCapabilitiesAbsent,
    IReadOnlyList<string> RequiredBeforeRuntime);

public sealed record ReliableRecipeOperatorRecommendedAction(
    string Label,
    ReliableRecipeOperatorActionKind ActionKind,
    string Description,
    bool IsRuntimeAction,
    bool RequiresExternalAuditBeforeRuntime);

public enum ReliableRecipeOperatorActionKind
{
    Review,
    AcceptForFixtureOnly,
    RejectUnsafe,
    Defer,
    CopySummary,
    ExportReviewPack,
    RequestExternalAudit,
    NoRuntimeAction
}

public sealed record ReliableRecipeOperatorReviewPackScenario(
    string ScenarioId,
    StructuredPrerequisiteAuthoringReport AuthoringReport,
    ReliableRecipeDryRunAdapterReadinessReport? AdapterReadinessReport,
    ReliableRecipeOperatorReviewDecision ExpectedDecision,
    string Summary)
{
    public bool FixtureOnly => true;
    public bool RuntimeEnabled => false;
    public bool UsesLiveBrowser => false;
    public bool UsesLiveDesktop => false;
    public bool UsesNetwork => false;
    public bool StoresSecrets => false;
}

public sealed record ReliableRecipeLabOperatorReviewPackPanel(
    string DecisionLabel,
    string ExecutiveSummary,
    IReadOnlyList<string> TopReviewRows,
    int PendingDecisionCount,
    int BlockedCriticalCount,
    IReadOnlyList<string> RecommendedActions,
    string ApprovalLanguageSummary,
    string HandoffSummary,
    string AuditSummary,
    string NoRuntimeNotice,
    IReadOnlyList<string> ReadOnlyActionLabels)
{
    public bool ReadOnly => true;
    public bool FixtureOnly => true;
    public bool RuntimeActionExposed => false;
    public bool CanRun => false;
    public bool CanExecute => false;
    public bool CanEnableAdapter => false;
    public bool CanLaunchBrowser => false;
    public bool CanConnectCdp => false;
    public bool CanReplay => false;
    public bool CanRecordLive => false;
}

public static class ReliableRecipeOperatorReviewPackScenarioCatalog
{
    public static IReadOnlyList<ReliableRecipeOperatorReviewPackScenario> All()
    {
        var complete = Authoring("complete_explicit_no_changes_needed");
        var missing = Authoring("missing_post_submit_validation_proposal");

        return
        [
            Scenario("complete_explicit_review_pack", complete, null, ReliableRecipeOperatorReviewDecision.ReadyForHumanReview, "Complete explicit prerequisites are ready for audit review."),
            Scenario("pending_inferred_requirement_review_pack", Authoring("inferred_download_evidence_proposal"), null, ReliableRecipeOperatorReviewDecision.NeedsOperatorDecision, "Inferred requirement needs operator decision."),
            Scenario("missing_critical_requirement_review_pack", missing, null, ReliableRecipeOperatorReviewDecision.BlockedByCriticalGaps, "Missing critical requirement blocks adapter readiness."),
            Scenario("rejected_critical_requirement_review_pack", Authoring("rejected_critical_requirement_blocks"), null, ReliableRecipeOperatorReviewDecision.BlockedByRejectedCriticalRequirement, "Rejected critical requirement blocks adapter readiness."),
            Scenario("accepted_fixture_only_review_pack", Authoring("accepted_fixture_requirements_still_runtime_blocked"), null, ReliableRecipeOperatorReviewDecision.DesignOnlyAcceptedForFixture, "Accepted fixture proposals remain design-only."),
            Scenario("mapped_legacy_warning_review_pack", Authoring("mapped_legacy_accepted_with_warning"), null, ReliableRecipeOperatorReviewDecision.DesignOnlyAcceptedForFixture, "Mapped legacy compatibility warning remains visible."),
            Scenario("external_side_effect_approval_review_pack", Authoring("external_side_effect_approval_evidence_proposal"), null, ReliableRecipeOperatorReviewDecision.BlockedByCriticalGaps, "External side effect requires approval language."),
            Scenario("ocr_supporting_signal_review_pack", Authoring("ocr_only_sensitive_perception_validation_proposal"), null, ReliableRecipeOperatorReviewDecision.BlockedByCriticalGaps, "OCR is supporting evidence only."),
            Scenario("captcha_handoff_review_pack", Authoring("captcha_handoff_evidence_proposal"), null, ReliableRecipeOperatorReviewDecision.BlockedByCriticalGaps, "Challenge handoff needs human review."),
            Scenario("sandbox_readiness_review_pack", Authoring("sandbox_readiness_assertion_proposal"), null, ReliableRecipeOperatorReviewDecision.BlockedByCriticalGaps, "Sandbox readiness assertion is missing."),
            Scenario("adapter_gate_blocked_review_pack", missing, Adapter(missing), ReliableRecipeOperatorReviewDecision.BlockedByCriticalGaps, "Adapter gate remains blocked by missing prerequisite."),
            Scenario("external_audit_required_review_pack", complete, ReliableRecipeDryRunAdapterReadinessEvaluator.Evaluate(ReliableRecipeDryRunAdapterReadinessScenarioCatalog.Get("complete_fixture_stack_design_only_ready")), ReliableRecipeOperatorReviewDecision.ReadyForHumanReview, "External audit remains required before runtime.")
        ];
    }

    public static ReliableRecipeOperatorReviewPackScenario Get(string scenarioId) =>
        All().Single(s => s.ScenarioId == scenarioId);

    private static ReliableRecipeOperatorReviewPackScenario Scenario(
        string scenarioId,
        StructuredPrerequisiteAuthoringReport authoringReport,
        ReliableRecipeDryRunAdapterReadinessReport? adapterReport,
        ReliableRecipeOperatorReviewDecision expected,
        string summary) =>
        new(scenarioId, authoringReport, adapterReport, expected, summary);

    private static StructuredPrerequisiteAuthoringReport Authoring(string scenarioId) =>
        StructuredPrerequisiteAuthoringEvaluator.Evaluate(StructuredPrerequisiteAuthoringScenarioCatalog.Get(scenarioId));

    private static ReliableRecipeDryRunAdapterReadinessReport Adapter(StructuredPrerequisiteAuthoringReport authoring)
    {
        var recipe = ReliableRecipeStructuredPrerequisiteScenarioCatalog.Get("submit_without_explicit_post_validation_blocks").Recipe;
        var preflight = ReliableRecipePreflightComposer.Compose(recipe, ReliableRecipeRunMode.DryRun);
        var profile = ReliableRecipeStructuredPrerequisiteEvaluator.Evaluate(recipe, preflight);
        return ReliableRecipeDryRunAdapterReadinessEvaluator.Evaluate(recipe, preflight, structuredPrerequisiteProfile: profile, authoringReport: authoring);
    }
}

public static class ReliableRecipeOperatorReviewPackGenerator
{
    private static readonly string NoRuntimeNotice = "Operator review pack only. Runtime not enabled; review decisions cannot start automation or enable an adapter.";

    public static ReliableRecipeOperatorReviewPack Generate(ReliableRecipeOperatorReviewPackScenario scenario) =>
        Generate(scenario.AuthoringReport, scenario.AdapterReadinessReport);

    public static ReliableRecipeOperatorReviewPack Generate(
        StructuredPrerequisiteAuthoringReport authoringReport,
        ReliableRecipeDryRunAdapterReadinessReport? adapterReadinessReport = null,
        ReliableRecipeFixtureEvalRun? evalRun = null,
        ComputerUseSandboxReadinessReport? sandboxReport = null,
        ReliableRecipePerceptionIntegrationReport? perceptionReport = null)
    {
        var rows = Rows(authoringReport, adapterReadinessReport, evalRun, sandboxReport, perceptionReport).ToArray();
        var decision = DecisionFor(authoringReport);

        return new ReliableRecipeOperatorReviewPack(
            $"operator-review-pack.{authoringReport.SubjectId}",
            authoringReport.SubjectId,
            ReliableRecipeOperatorReviewSubjectKind.StructuredPrerequisiteAuthoringReport,
            decision,
            ExecutiveSummary(decision, authoringReport),
            rows,
            Approval(authoringReport),
            Handoff(authoringReport, rows),
            Audit(authoringReport, adapterReadinessReport),
            AdapterGateSummary(authoringReport, adapterReadinessReport),
            ProtectedScopes(),
            RecommendedActions(authoringReport, decision),
            NoRuntimeNotice);
    }

    private static IEnumerable<ReliableRecipeOperatorReviewRow> Rows(
        StructuredPrerequisiteAuthoringReport authoringReport,
        ReliableRecipeDryRunAdapterReadinessReport? adapterReadinessReport,
        ReliableRecipeFixtureEvalRun? evalRun,
        ComputerUseSandboxReadinessReport? sandboxReport,
        ReliableRecipePerceptionIntegrationReport? perceptionReport)
    {
        foreach (var proposal in authoringReport.Proposals)
        {
            var accepted = authoringReport.AcceptedRequirements.Contains(proposal.ProposalId);
            var rejected = authoringReport.RejectedRequirements.Contains(proposal.ProposalId);
            var status = rejected
                ? ReliableRecipeOperatorReviewStatus.RejectedUnsafe
                : accepted
                    ? (proposal.ProposedSource == ReliableRecipeRequirementSource.MappedFromLegacyContract ? ReliableRecipeOperatorReviewStatus.MappedLegacyWarning : ReliableRecipeOperatorReviewStatus.AcceptedForFixture)
                    : ReliableRecipeOperatorReviewStatus.PendingReview;

            yield return new ReliableRecipeOperatorReviewRow(
                proposal.ProposalId,
                CategoryFor(proposal),
                $"Operator review: {proposal.RequirementType} {proposal.RequirementKind}",
                status,
                proposal.WouldBlockAdapterIfRejected && !accepted ? ReliableRecipeQualitySeverity.Blocking : ReliableRecipeQualitySeverity.Warning,
                MessageFor(proposal, accepted, rejected),
                accepted ? "No runtime action. Keep accepted for fixture review only." : rejected ? "Reject unsafe and redesign explicitly." : "Review proposal and accept for fixture only, reject unsafe or defer.",
                proposal.ProposalId,
                $"{proposal.RequirementType}:{proposal.TargetBlockId}:{proposal.RequirementKind}",
                proposal.WouldBlockAdapterIfRejected && !accepted ? "Adapter gate blocked" : "Fixture review only");
        }

        foreach (var missing in authoringReport.StillMissingCriticalRequirements)
        {
            yield return new ReliableRecipeOperatorReviewRow(
                $"missing.{missing}",
                CategoryFromText(missing),
                "Critical prerequisite gap",
                ReliableRecipeOperatorReviewStatus.MissingCritical,
                ReliableRecipeQualitySeverity.Blocking,
                missing,
                "Author explicit fixture prerequisite or defer; runtime remains blocked.",
                null,
                missing,
                "Adapter gate blocked");
        }

        if (adapterReadinessReport is not null)
        {
            foreach (var missing in adapterReadinessReport.MissingPrerequisites.Take(5))
            {
                yield return new ReliableRecipeOperatorReviewRow(
                    $"adapter.{missing}",
                    ReliableRecipeOperatorReviewCategory.AdapterGate,
                    "Adapter gate review",
                    ReliableRecipeOperatorReviewStatus.Blocked,
                    ReliableRecipeQualitySeverity.Blocking,
                    missing,
                    "Review prerequisite gate; external audit required before runtime.",
                    null,
                    null,
                    "Adapter gate blocked");
            }
        }

        if (perceptionReport is not null)
        {
            yield return new ReliableRecipeOperatorReviewRow(
                "perception.summary",
                ReliableRecipeOperatorReviewCategory.Perception,
                "Perception review",
                perceptionReport.OverallDecision.ToString().Contains("Blocked", StringComparison.OrdinalIgnoreCase) ? ReliableRecipeOperatorReviewStatus.Blocked : ReliableRecipeOperatorReviewStatus.Info,
                perceptionReport.OverallDecision.ToString().Contains("Blocked", StringComparison.OrdinalIgnoreCase) ? ReliableRecipeQualitySeverity.Blocking : ReliableRecipeQualitySeverity.Info,
                "Perception is fixture-only; OCR remains supporting evidence and not action authority.",
                "Review signal confidence; do not authorize live action.",
                null,
                null,
                "Runtime not enabled");
        }

        if (sandboxReport is not null)
        {
            yield return new ReliableRecipeOperatorReviewRow(
                "sandbox.summary",
                ReliableRecipeOperatorReviewCategory.SandboxReadiness,
                "Sandbox readiness review",
                sandboxReport.OverallDecision.ToString().Contains("Blocked", StringComparison.OrdinalIgnoreCase) ? ReliableRecipeOperatorReviewStatus.Blocked : ReliableRecipeOperatorReviewStatus.Info,
                sandboxReport.OverallDecision.ToString().Contains("Blocked", StringComparison.OrdinalIgnoreCase) ? ReliableRecipeQualitySeverity.Blocking : ReliableRecipeQualitySeverity.Info,
                "Sandbox readiness is design-only; no VM, container or browser/desktop launch is present.",
                "Review missing requirements; keep runtime disabled.",
                null,
                null,
                "Runtime not enabled");
        }

        if (evalRun is not null)
        {
            yield return new ReliableRecipeOperatorReviewRow(
                "eval.summary",
                ReliableRecipeOperatorReviewCategory.EvalHarness,
                "Eval harness review",
                ReliableRecipeOperatorReviewStatus.Info,
                ReliableRecipeQualitySeverity.Info,
                "Eval result is fixture-only and does not prove live runtime behavior.",
                "Review fixture outcome; do not treat as live validation.",
                null,
                null,
                "Runtime not enabled");
        }

        yield return new ReliableRecipeOperatorReviewRow(
            "runtime.boundary",
            ReliableRecipeOperatorReviewCategory.RuntimeBoundary,
            "Runtime boundary",
            ReliableRecipeOperatorReviewStatus.Blocked,
            ReliableRecipeQualitySeverity.Blocking,
            "Runtime not enabled. External audit required before any runtime work.",
            "Review, copy or export summary only.",
            null,
            null,
            "Runtime blocked");
    }

    private static ReliableRecipeOperatorReviewDecision DecisionFor(StructuredPrerequisiteAuthoringReport report) =>
        report.OverallDecision switch
        {
            StructuredPrerequisiteAuthoringDecision.BlockedRejectedCriticalRequirements => ReliableRecipeOperatorReviewDecision.BlockedByRejectedCriticalRequirement,
            StructuredPrerequisiteAuthoringDecision.BlockedMissingCriticalRequirements => ReliableRecipeOperatorReviewDecision.BlockedByCriticalGaps,
            StructuredPrerequisiteAuthoringDecision.ProposalsNeedReview => ReliableRecipeOperatorReviewDecision.NeedsOperatorDecision,
            StructuredPrerequisiteAuthoringDecision.DesignOnlyReadyWithReviewedRequirements => ReliableRecipeOperatorReviewDecision.DesignOnlyAcceptedForFixture,
            StructuredPrerequisiteAuthoringDecision.RuntimeStillBlocked => ReliableRecipeOperatorReviewDecision.RuntimeStillBlocked,
            _ => ReliableRecipeOperatorReviewDecision.ReadyForHumanReview
        };

    private static string ExecutiveSummary(ReliableRecipeOperatorReviewDecision decision, StructuredPrerequisiteAuthoringReport report) =>
        decision switch
        {
            ReliableRecipeOperatorReviewDecision.BlockedByRejectedCriticalRequirement => "Operator review pack found rejected critical requirements. Adapter gate remains blocked and runtime is not enabled.",
            ReliableRecipeOperatorReviewDecision.BlockedByCriticalGaps => "Operator review pack found critical prerequisite gaps. Adapter readiness is blocked until explicit fixture requirements are authored.",
            ReliableRecipeOperatorReviewDecision.NeedsOperatorDecision => "Operator review pack contains proposals that require human review before fixture acceptance.",
            ReliableRecipeOperatorReviewDecision.DesignOnlyAcceptedForFixture => "Operator review pack has fixture-accepted requirements. Runtime remains blocked and external audit is required before runtime.",
            ReliableRecipeOperatorReviewDecision.RuntimeStillBlocked => "Operator review pack confirms runtime remains blocked.",
            _ => report.Proposals.Count == 0 ? "Operator review pack is audit-ready with no migration proposals." : "Operator review pack is ready for human review."
        };

    private static ReliableRecipeOperatorApprovalLanguage Approval(StructuredPrerequisiteAuthoringReport report) =>
        new(
            "Structured prerequisite review",
            "Review proposed structured requirements for fixture-only acceptance.",
            "Accepted proposals become fixture review language for evidence and validation prerequisites.",
            "This will not enable runtime, adapter execution, browser launch, desktop control, OCR live capture, recorder runtime or sandbox runtime.",
            report.StillMissingCriticalRequirements.Count > 0 ? "Critical gaps block adapter readiness." : "Fixture acceptance does not reduce policy or runtime risk.",
            ["Review", "Accept for fixture only", "Reject unsafe", "Defer", "Copy summary", "Export review pack", "Request external audit"],
            ["Run now", "Execute", "Enable adapter", "Launch browser", "Connect CDP", "Replay", "Record live", "Approved to run"],
            NoRuntimeNotice);

    private static ReliableRecipeOperatorHandoffSummary Handoff(StructuredPrerequisiteAuthoringReport report, IReadOnlyList<ReliableRecipeOperatorReviewRow> rows) =>
        new(
            "Operator handoff summary",
            "Review structured prerequisite proposals, critical blockers and adapter gate impact. This handoff is fixture-only.",
            rows.Where(r => r.Severity == ReliableRecipeQualitySeverity.Blocking).Select(r => r.Message).Distinct().ToArray(),
            rows.Where(r => r.Status == ReliableRecipeOperatorReviewStatus.PendingReview).Select(r => r.Message).Distinct().ToArray(),
            rows.Where(r => r.Category == ReliableRecipeOperatorReviewCategory.Evidence && r.Status != ReliableRecipeOperatorReviewStatus.AcceptedForFixture).Select(r => r.Message).Distinct().ToArray(),
            rows.Where(r => r.Category == ReliableRecipeOperatorReviewCategory.Validation && r.Status != ReliableRecipeOperatorReviewStatus.AcceptedForFixture).Select(r => r.Message).Distinct().ToArray(),
            ["Review", "Accept for fixture only", "Reject unsafe", "Defer", "Copy summary", "Export review pack", "Request external audit"],
            ["Run now", "Execute", "Enable adapter", "Launch browser", "Connect CDP", "Replay", "Record live"],
            true,
            NoRuntimeNotice);

    private static ReliableRecipeOperatorAuditSummary Audit(
        StructuredPrerequisiteAuthoringReport report,
        ReliableRecipeDryRunAdapterReadinessReport? adapterReadinessReport) =>
        new(
            report.RejectedRequirements.Count > 0 ? "BLOCKED_REJECTED_CRITICAL_REQUIREMENT" : report.StillMissingCriticalRequirements.Count > 0 ? "BLOCKED_CRITICAL_GAPS" : "READY_FOR_HUMAN_REVIEW_NO_RUNTIME",
            ["No executable adapter present", "No runtime action exposed", "Review pack generated deterministically", "External audit required before runtime"],
            report.StillMissingCriticalRequirements.Concat(adapterReadinessReport?.MissingPrerequisites ?? []).Distinct().ToArray(),
            ["OCR/WCU internals untouched", "Perception live capture absent", "Recorder runtime absent", "Sandbox runtime absent", "Browser/CDP/live execution scope untouched"],
            ["Browser live", "CDP live", "browser driver frameworks", "Desktop live", "OCR live", "Screenshot capture", "Recorder runtime", "Sandbox runtime", "Provider/LLM call", "Network/shell/process runner"],
            ["External audit", "Protected scope review", "Explicit fixture requirements for critical gaps", "Human approval policy before runtime"]);

    private static string AdapterGateSummary(StructuredPrerequisiteAuthoringReport authoringReport, ReliableRecipeDryRunAdapterReadinessReport? adapterReadinessReport)
    {
        var adapter = adapterReadinessReport is null ? "No adapter readiness report attached." : $"{adapterReadinessReport.OverallDecision}; missing gates: {adapterReadinessReport.MissingPrerequisites.Count}.";
        return $"{authoringReport.AdapterGateImpact}. {adapter} External audit required before runtime.";
    }

    private static IReadOnlyList<string> ProtectedScopes() =>
    [
        "OCR/WCU protected: supporting evidence references only.",
        "Perception protected: no live capture.",
        "Recorder protected: no mouse, keyboard, clipboard or screen capture.",
        "Sandbox protected: no VM, container, Docker, browser or desktop launch.",
        "Runtime protected: no executable adapter, command, network, shell or provider call."
    ];

    private static IReadOnlyList<ReliableRecipeOperatorRecommendedAction> RecommendedActions(StructuredPrerequisiteAuthoringReport report, ReliableRecipeOperatorReviewDecision decision)
    {
        var actions = new List<ReliableRecipeOperatorRecommendedAction>
        {
            new("Review", ReliableRecipeOperatorActionKind.Review, "Review proposal, checklist, blockers and no-runtime notice.", false, false),
            new("Copy summary", ReliableRecipeOperatorActionKind.CopySummary, "Copy the read-only review summary.", false, false),
            new("Export review pack", ReliableRecipeOperatorActionKind.ExportReviewPack, "Export review pack metadata only; no runtime artifacts.", false, false),
            new("Request external audit", ReliableRecipeOperatorActionKind.RequestExternalAudit, "Request external audit before any future runtime work.", false, true)
        };

        if (report.Proposals.Count > 0)
        {
            actions.Insert(1, new ReliableRecipeOperatorRecommendedAction("Accept for fixture only", ReliableRecipeOperatorActionKind.AcceptForFixtureOnly, "Accept selected proposal for fixture review only.", false, false));
            actions.Insert(2, new ReliableRecipeOperatorRecommendedAction("Reject unsafe", ReliableRecipeOperatorActionKind.RejectUnsafe, "Reject unsafe proposal and keep adapter gate blocked.", false, false));
            actions.Insert(3, new ReliableRecipeOperatorRecommendedAction("Defer", ReliableRecipeOperatorActionKind.Defer, "Defer proposal until explicit prerequisite review.", false, false));
        }

        if (decision is ReliableRecipeOperatorReviewDecision.BlockedByCriticalGaps or ReliableRecipeOperatorReviewDecision.BlockedByRejectedCriticalRequirement)
            actions.Add(new ReliableRecipeOperatorRecommendedAction("No runtime action", ReliableRecipeOperatorActionKind.NoRuntimeAction, "Runtime remains blocked by review pack decision.", false, true));

        return actions;
    }

    private static ReliableRecipeOperatorReviewCategory CategoryFor(StructuredPrerequisiteProposal proposal)
    {
        if (proposal.RequirementKind.Contains("Perception", StringComparison.OrdinalIgnoreCase) || proposal.RequirementKind.Contains("Ocr", StringComparison.OrdinalIgnoreCase))
            return ReliableRecipeOperatorReviewCategory.Perception;
        if (proposal.RequirementKind.Contains("Human", StringComparison.OrdinalIgnoreCase) || proposal.RequirementKind.Contains("Approval", StringComparison.OrdinalIgnoreCase))
            return ReliableRecipeOperatorReviewCategory.HumanApproval;
        if (proposal.RequirementKind.Contains("Sandbox", StringComparison.OrdinalIgnoreCase))
            return ReliableRecipeOperatorReviewCategory.SandboxReadiness;
        if (proposal.RequirementKind.Contains("Eval", StringComparison.OrdinalIgnoreCase))
            return ReliableRecipeOperatorReviewCategory.EvalHarness;
        return proposal.RequirementType == StructuredPrerequisiteRequirementType.Evidence
            ? ReliableRecipeOperatorReviewCategory.Evidence
            : ReliableRecipeOperatorReviewCategory.Validation;
    }

    private static ReliableRecipeOperatorReviewCategory CategoryFromText(string text)
    {
        if (text.Contains("Perception", StringComparison.OrdinalIgnoreCase) || text.Contains("Ocr", StringComparison.OrdinalIgnoreCase))
            return ReliableRecipeOperatorReviewCategory.Perception;
        if (text.Contains("Human", StringComparison.OrdinalIgnoreCase) || text.Contains("Approval", StringComparison.OrdinalIgnoreCase))
            return ReliableRecipeOperatorReviewCategory.HumanApproval;
        if (text.Contains("Sandbox", StringComparison.OrdinalIgnoreCase))
            return ReliableRecipeOperatorReviewCategory.SandboxReadiness;
        if (text.Contains("Eval", StringComparison.OrdinalIgnoreCase))
            return ReliableRecipeOperatorReviewCategory.EvalHarness;
        if (text.StartsWith("evidence", StringComparison.OrdinalIgnoreCase))
            return ReliableRecipeOperatorReviewCategory.Evidence;
        if (text.StartsWith("validation", StringComparison.OrdinalIgnoreCase))
            return ReliableRecipeOperatorReviewCategory.Validation;
        return ReliableRecipeOperatorReviewCategory.AdapterGate;
    }

    private static string MessageFor(StructuredPrerequisiteProposal proposal, bool accepted, bool rejected)
    {
        if (accepted)
            return proposal.ProposedSource == ReliableRecipeRequirementSource.MappedFromLegacyContract
                ? "Mapped legacy requirement accepted for fixture compatibility; explicit proof is still preferred."
                : "Requirement accepted for fixture review only. Runtime not enabled.";
        if (rejected)
            return "Rejected unsafe requirement; adapter gate remains blocked until redesign.";
        return proposal.Reason;
    }
}

public static class ReliableRecipeOperatorReviewPackMapper
{
    public static ReliableRecipeLabOperatorReviewPackPanel ToLabPanel(ReliableRecipeOperatorReviewPack pack) =>
        new(
            pack.OverallDecision.ToString(),
            pack.ExecutiveSummary,
            pack.ReviewRows.Take(5).Select(r => $"{r.Title}: {r.Message}").ToArray(),
            pack.ReviewRows.Count(r => r.Status == ReliableRecipeOperatorReviewStatus.PendingReview),
            pack.ReviewRows.Count(r => r.Severity == ReliableRecipeQualitySeverity.Blocking),
            pack.RecommendedOperatorActions.Select(a => a.Label).ToArray(),
            pack.ApprovalLanguage.ShortSummary,
            pack.HandoffSummary.Summary,
            pack.AuditSummary.AuditDecision,
            pack.NoRuntimeNotice,
            ["Review", "Accept for fixture only", "Reject unsafe", "Defer", "Copy summary", "Export review pack", "Request external audit"]);
}
