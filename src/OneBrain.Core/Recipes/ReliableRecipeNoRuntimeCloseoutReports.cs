namespace OneBrain.Core.Recipes;

public sealed record ReliableRecipeNoRuntimeCloseoutReport(
    string ReportId,
    string LineName,
    ReliableRecipeNoRuntimeCloseoutDecision OverallDecision,
    IReadOnlyList<ReliableRecipeCloseoutBlockSummary> BlockSummaries,
    ReliableRecipeInvariantMatrix InvariantMatrix,
    ReliableRecipeProtectedScopeProof ProtectedScopeProof,
    ReliableRecipeNoRuntimeProof NoRuntimeProof,
    IReadOnlyList<ReliableRecipeOperatorSignoffFixture> OperatorSignoffFixtures,
    ReliableRecipeExternalAuditHandoff ExternalAuditHandoff,
    ReliableRecipeRecommendedNextPhase RecommendedNextPhase,
    string NoRuntimeNotice)
{
    public bool ReadOnly => true;
    public bool FixtureOnly => true;
    public bool RuntimeEnabled => false;
    public bool AdapterRuntimeEnabled => false;
    public bool RuntimeActionExposed => false;
}

public enum ReliableRecipeNoRuntimeCloseoutDecision
{
    ReadyForReadOnlyUi,
    ReadyForExternalAudit,
    NeedsHardening,
    BlockedByRuntimeLeak,
    BlockedByProtectedScopeRisk
}

public sealed record ReliableRecipeCloseoutBlockSummary(
    string BlockId,
    string Decision,
    string Commit,
    string Purpose,
    IReadOnlyList<string> PrimaryFiles,
    string FocusedTestCategory,
    IReadOnlyList<string> KnownRisks,
    int ReadinessContribution,
    string NoRuntimeInvariant);

public sealed record ReliableRecipeInvariantMatrix(
    IReadOnlyList<ReliableRecipeCloseoutInvariant> Invariants,
    int PassedCount,
    int FailedCount,
    int WarningCount,
    string OverallStatus);

public sealed record ReliableRecipeCloseoutInvariant(
    string Code,
    string Title,
    ReliableRecipeCloseoutInvariantStatus Status,
    ReliableRecipeQualitySeverity Severity,
    string Evidence,
    string FailureImpact);

public enum ReliableRecipeCloseoutInvariantStatus
{
    Passed,
    Warning,
    Failed
}

public sealed record ReliableRecipeProtectedScopeProof(
    IReadOnlyList<string> ProtectedScopes,
    IReadOnlyList<string> TouchedScopes,
    IReadOnlyList<string> UntouchedScopes,
    IReadOnlyList<string> Violations,
    string OverallStatus);

public sealed record ReliableRecipeNoRuntimeProof(
    IReadOnlyList<string> RuntimeCapabilitiesAbsent,
    IReadOnlyList<string> BlockedCapabilities,
    IReadOnlyList<string> ForbiddenLabelsAbsent,
    IReadOnlyList<string> ExternalEffectsAbsent,
    string OverallStatus);

public sealed record ReliableRecipeOperatorSignoffFixture(
    string SignoffId,
    string SubjectId,
    string Title,
    ReliableRecipeOperatorSignoffDecision Decision,
    string RequiredReviewerRole,
    IReadOnlyList<string> ReviewStatements,
    bool CannotApproveRuntime,
    bool ExternalAuditRequiredBeforeRuntime)
{
    public bool ReadOnly => true;
    public bool FixtureOnly => true;
    public bool RuntimeApprovalGranted => false;
}

public enum ReliableRecipeOperatorSignoffDecision
{
    ReadyForReadOnlyReview,
    ExternalAuditRequired,
    RuntimeProhibited,
    ProtectedScopesUntouched,
    FixtureOnlyAccepted,
    AdapterGateBlockedUntilAudit,
    NoRuntimeRegressionGuard
}

public sealed record ReliableRecipeExternalAuditHandoff(
    string Title,
    string Scope,
    IReadOnlyList<string> IncludedBlocks,
    IReadOnlyList<string> AuditQuestions,
    IReadOnlyList<string> KnownRisks,
    IReadOnlyList<string> EvidenceReferences,
    string RuntimeProhibitedStatement,
    IReadOnlyList<string> RecommendedAuditDecisionLabels);

public sealed record ReliableRecipeRecommendedNextPhase(
    string NextBlockId,
    string Title,
    string AllowedScope,
    string ForbiddenScope,
    string Reason);

public sealed record ReliableRecipeNoRuntimeCloseoutScenario(
    string ScenarioId,
    IReadOnlyList<ReliableRecipeCloseoutBlockSummary> BlockSummaries,
    bool RuntimeLeakDetected,
    bool ProtectedScopeViolationDetected,
    bool ExternalAuditHandoffPresent,
    ReliableRecipeNoRuntimeCloseoutDecision ExpectedDecision)
{
    public bool FixtureOnly => true;
    public bool RuntimeEnabled => false;
    public bool UsesLiveBrowser => false;
    public bool UsesLiveDesktop => false;
    public bool UsesNetwork => false;
    public bool StoresSecrets => false;
}

public sealed record ReliableRecipeLabCloseoutPanel(
    string DecisionLabel,
    string ReadinessSummary,
    string InvariantSummary,
    string ProtectedScopeSummary,
    string NoRuntimeSummary,
    string OperatorSignoffSummary,
    string ExternalAuditSummary,
    string RecommendedNextPhase,
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
    public bool CanApproveRuntime => false;
}

public static class ReliableRecipeNoRuntimeCloseoutScenarioCatalog
{
    public static IReadOnlyList<ReliableRecipeNoRuntimeCloseoutScenario> All() =>
    [
        new("m1_m11_no_runtime_closeout", ReliableRecipeNoRuntimeCloseoutReportGenerator.DefaultBlockSummaries(), false, false, true, ReliableRecipeNoRuntimeCloseoutDecision.ReadyForExternalAudit),
        new("runtime_leak_blocks_closeout", ReliableRecipeNoRuntimeCloseoutReportGenerator.DefaultBlockSummaries(), true, false, true, ReliableRecipeNoRuntimeCloseoutDecision.BlockedByRuntimeLeak),
        new("protected_scope_violation_blocks_closeout", ReliableRecipeNoRuntimeCloseoutReportGenerator.DefaultBlockSummaries(), false, true, true, ReliableRecipeNoRuntimeCloseoutDecision.BlockedByProtectedScopeRisk),
        new("missing_external_audit_handoff_needs_hardening", ReliableRecipeNoRuntimeCloseoutReportGenerator.DefaultBlockSummaries(), false, false, false, ReliableRecipeNoRuntimeCloseoutDecision.NeedsHardening)
    ];

    public static ReliableRecipeNoRuntimeCloseoutScenario Get(string scenarioId) =>
        All().Single(s => s.ScenarioId == scenarioId);
}

public static class ReliableRecipeOperatorSignoffFixtureCatalog
{
    public static IReadOnlyList<ReliableRecipeOperatorSignoffFixture> All() =>
    [
        Signoff("read_only_ui_signoff", "foundation.closeout", "Ready for read-only UI", ReliableRecipeOperatorSignoffDecision.ReadyForReadOnlyReview, ["Review product copy, fixture panels and no-runtime notices."]),
        Signoff("external_audit_required_signoff", "foundation.closeout", "External audit required", ReliableRecipeOperatorSignoffDecision.ExternalAuditRequired, ["External audit is required before any future runtime work."]),
        Signoff("runtime_prohibited_signoff", "foundation.closeout", "Runtime prohibited", ReliableRecipeOperatorSignoffDecision.RuntimeProhibited, ["Runtime is not enabled and cannot be approved by this fixture."]),
        Signoff("protected_scopes_untouched_signoff", "foundation.closeout", "Protected scopes untouched", ReliableRecipeOperatorSignoffDecision.ProtectedScopesUntouched, ["OCR, perception, recorder, sandbox and browser/live execution scopes remain untouched."]),
        Signoff("structured_prerequisites_reviewed_signoff", "foundation.closeout", "Structured prerequisites reviewed", ReliableRecipeOperatorSignoffDecision.FixtureOnlyAccepted, ["Structured prerequisite review is fixture-only and does not approve execution."]),
        Signoff("operator_review_pack_accepted_for_fixture_only_signoff", "foundation.closeout", "Operator review accepted for fixture only", ReliableRecipeOperatorSignoffDecision.FixtureOnlyAccepted, ["Accepted review pack language remains fixture-only."]),
        Signoff("adapter_gate_blocked_until_audit_signoff", "foundation.closeout", "Adapter gate blocked until audit", ReliableRecipeOperatorSignoffDecision.AdapterGateBlockedUntilAudit, ["Adapter gates remain blocked until external audit and explicit operator decision."]),
        Signoff("no_runtime_regression_guard_signoff", "foundation.closeout", "No-runtime regression guard", ReliableRecipeOperatorSignoffDecision.NoRuntimeRegressionGuard, ["No-runtime invariant must be preserved by future work."])
    ];

    public static ReliableRecipeOperatorSignoffFixture Get(string signoffId) =>
        All().Single(s => s.SignoffId == signoffId);

    private static ReliableRecipeOperatorSignoffFixture Signoff(
        string signoffId,
        string subjectId,
        string title,
        ReliableRecipeOperatorSignoffDecision decision,
        IReadOnlyList<string> statements) =>
        new(
            signoffId,
            subjectId,
            title,
            decision,
            "Operator or external auditor",
            statements.Concat(["Operator signoff fixture cannot approve runtime.", "Runtime not enabled."]).ToArray(),
            CannotApproveRuntime: true,
            ExternalAuditRequiredBeforeRuntime: true);
}

public static class ReliableRecipeNoRuntimeCloseoutReportGenerator
{
    private const string LineName = "Reliable Recipe / Recorder / Eval / Sandbox / Perception / Adapter Readiness / Structured Prerequisite / Operator Review Pack Foundation";
    private const string NoRuntimeNotice = "No-runtime closeout only. Runtime not enabled; external audit is required before any future runtime or adapter work.";

    public static ReliableRecipeNoRuntimeCloseoutReport Generate(ReliableRecipeNoRuntimeCloseoutScenario scenario) =>
        Generate(
            scenario.BlockSummaries,
            scenario.RuntimeLeakDetected,
            scenario.ProtectedScopeViolationDetected,
            scenario.ExternalAuditHandoffPresent);

    public static ReliableRecipeNoRuntimeCloseoutReport Generate(
        IReadOnlyList<ReliableRecipeCloseoutBlockSummary>? blockSummaries = null,
        bool runtimeLeakDetected = false,
        bool protectedScopeViolationDetected = false,
        bool externalAuditHandoffPresent = true)
    {
        var blocks = blockSummaries ?? DefaultBlockSummaries();
        var invariantMatrix = BuildInvariantMatrix(blocks, runtimeLeakDetected, protectedScopeViolationDetected, externalAuditHandoffPresent);
        var protectedScopeProof = BuildProtectedScopeProof(protectedScopeViolationDetected);
        var noRuntimeProof = BuildNoRuntimeProof(runtimeLeakDetected);
        var externalAuditHandoff = externalAuditHandoffPresent
            ? BuildExternalAuditHandoff(blocks)
            : BuildMissingExternalAuditHandoff(blocks);

        return new ReliableRecipeNoRuntimeCloseoutReport(
            "no-runtime-closeout.m1-m11",
            LineName,
            Decision(invariantMatrix, protectedScopeProof, noRuntimeProof, externalAuditHandoffPresent),
            blocks,
            invariantMatrix,
            protectedScopeProof,
            noRuntimeProof,
            ReliableRecipeOperatorSignoffFixtureCatalog.All(),
            externalAuditHandoff,
            NextPhase(),
            NoRuntimeNotice);
    }

    public static IReadOnlyList<ReliableRecipeCloseoutBlockSummary> DefaultBlockSummaries() =>
    [
        Block("M1", "GO_M1_RELIABLE_RECIPE_RECORDER_EVAL_SANDBOX_FOUNDATION_READY", "634cfc0b797532fb3becca78476a5a8a9372c0f1", "Reliable Recipe contracts, recorder draft, eval, sandbox and perception foundations.", ["ReliableRecipeFoundationContracts.cs"], "ReliableRecipeFoundation", 97),
        Block("M2", "GO_M2_RELIABLE_RECIPE_PREFLIGHT_QUALITY_SCORE_READY", "e183e40250e5842396e869ba38f51444ea4b1f78", "Quality score and deterministic preflight composition.", ["ReliableRecipeQualityPreflightContracts.cs"], "ReliableRecipeQualityScore", 96),
        Block("M3", "GO_M3_RELIABLE_RECIPE_LAB_READ_ONLY_QUALITY_SURFACE_READY", "7690fb3e4fb845b3680cb2e059a202d7468b2eeb", "Read-only Recipe Lab quality surface.", ["ReliableRecipeLabViewModels.cs"], "ReliableRecipeLabReadOnlySurface", 96),
        Block("M4", "GO_M4_RECORDER_TO_RECIPE_FIXTURE_DRAFT_EXPANSION_READY", "004ab7ad998e118d569604f234cf48e31f156a92", "Recorder-to-recipe fixture draft review model.", ["ReliableRecipeRecorderDraftContracts.cs"], "RecorderToRecipeFixtureDraft", 90),
        Block("M5", "GO_M5_RELIABLE_RECIPE_EVAL_HARNESS_FIXTURE_SCENARIOS_READY", "4db6894b67b235a930e3b0e639edb8348ab593dc", "Fixture eval harness scenarios and reports.", ["ReliableRecipeEvalHarnessFixtureContracts.cs"], "ReliableRecipeEvalHarnessFixtureScenarios", 92),
        Block("M6", "GO_M6_COMPUTER_USE_SANDBOX_READINESS_REPORTS_READY", "c857f3a4a5338842fdf4bfa5f69b44286a903250", "Computer-use sandbox readiness reports.", ["ReliableRecipeSandboxReadinessReports.cs"], "ComputerUseSandboxReadinessReports", 95),
        Block("M7", "GO_M7_PERCEPTION_STACK_FORMAL_INTEGRATION_REPORTS_READY", "acff92079fb443583faf0de0f3c17789b0813777", "Perception integration reports and action authority boundaries.", ["ReliableRecipePerceptionIntegrationReports.cs"], "PerceptionStackFormalIntegrationReports", 96),
        Block("M8", "GO_M8_PROTECTED_DRY_RUN_ADAPTER_READINESS_DESIGN_AUDIT_READY", "52bfb825ffa8d439acbcca27b08edba6a7a34c35", "Protected dry-run adapter readiness design.", ["ReliableRecipeDryRunAdapterReadiness.cs"], "ProtectedDryRunAdapterReadinessDesignAudit", 89),
        Block("M9", "GO_M9_STRUCTURED_EVIDENCE_VALIDATION_PREREQUISITE_HARDENING_READY", "5b1c4086fcd78082a74075d8e06ff7fc360ff9ce", "Structured evidence and validation prerequisites.", ["ReliableRecipeStructuredPrerequisites.cs"], "StructuredEvidenceValidationPrerequisites", 96),
        Block("M10", "GO_M10_STRUCTURED_PREREQUISITE_AUTHORING_REVIEW_MIGRATION_REPORTS_READY", "cd49b6582eaca03ccf38e1c022b293a3e406b10b", "Structured prerequisite authoring and migration reports.", ["ReliableRecipeStructuredPrerequisiteAuthoring.cs"], "StructuredPrerequisiteAuthoringReviewMigration", 97),
        Block("M11", "GO_M11_NO_RUNTIME_OPERATOR_REVIEW_PACKS_READY", "7df800d3c0a5ec70069fcb5ee05205414bdd2ce2", "No-runtime operator review packs.", ["ReliableRecipeOperatorReviewPacks.cs"], "NoRuntimeOperatorReviewPacks", 97)
    ];

    private static ReliableRecipeCloseoutBlockSummary Block(
        string id,
        string decision,
        string commit,
        string purpose,
        IReadOnlyList<string> files,
        string testCategory,
        int readiness) =>
        new(
            id,
            decision,
            commit,
            purpose,
            files,
            testCategory,
            ["Runtime remains intentionally blocked.", "External audit required before runtime."],
            readiness,
            "No executable runtime, adapter, live capture or external side effect is introduced.");

    private static ReliableRecipeInvariantMatrix BuildInvariantMatrix(
        IReadOnlyList<ReliableRecipeCloseoutBlockSummary> blocks,
        bool runtimeLeakDetected,
        bool protectedScopeViolationDetected,
        bool externalAuditHandoffPresent)
    {
        var invariants = new List<ReliableRecipeCloseoutInvariant>
        {
            Invariant("no-runtime", "Runtime remains disabled", runtimeLeakDetected ? ReliableRecipeCloseoutInvariantStatus.Failed : ReliableRecipeCloseoutInvariantStatus.Passed, ReliableRecipeQualitySeverity.Blocking, "No executable adapter or runtime command is present.", "Runtime leak blocks closeout."),
            Invariant("protected-scope", "Protected scopes remain untouched", protectedScopeViolationDetected ? ReliableRecipeCloseoutInvariantStatus.Failed : ReliableRecipeCloseoutInvariantStatus.Passed, ReliableRecipeQualitySeverity.Blocking, "OCR, perception, recorder, sandbox and browser/live scopes are proof-only in this line.", "Protected-scope violation blocks closeout."),
            Invariant("external-audit-before-runtime", "External audit required before runtime", externalAuditHandoffPresent ? ReliableRecipeCloseoutInvariantStatus.Passed : ReliableRecipeCloseoutInvariantStatus.Warning, ReliableRecipeQualitySeverity.Warning, "External audit handoff is part of closeout.", "Missing handoff requires hardening before close."),
            Invariant("m1-m11-coverage", "M1-M11 summaries present", blocks.Count == 11 ? ReliableRecipeCloseoutInvariantStatus.Passed : ReliableRecipeCloseoutInvariantStatus.Failed, ReliableRecipeQualitySeverity.Blocking, $"{blocks.Count} block summaries included.", "Missing block summary weakens audit readiness."),
            Invariant("operator-signoff-fixture-only", "Operator signoff is fixture-only", ReliableRecipeCloseoutInvariantStatus.Passed, ReliableRecipeQualitySeverity.Blocking, "Signoff fixtures cannot approve runtime.", "Runtime approval through signoff would block closeout.")
        };

        return new ReliableRecipeInvariantMatrix(
            invariants,
            invariants.Count(i => i.Status == ReliableRecipeCloseoutInvariantStatus.Passed),
            invariants.Count(i => i.Status == ReliableRecipeCloseoutInvariantStatus.Failed),
            invariants.Count(i => i.Status == ReliableRecipeCloseoutInvariantStatus.Warning),
            invariants.Any(i => i.Status == ReliableRecipeCloseoutInvariantStatus.Failed) ? "failed" : invariants.Any(i => i.Status == ReliableRecipeCloseoutInvariantStatus.Warning) ? "warning" : "passed");
    }

    private static ReliableRecipeCloseoutInvariant Invariant(
        string code,
        string title,
        ReliableRecipeCloseoutInvariantStatus status,
        ReliableRecipeQualitySeverity severity,
        string evidence,
        string failureImpact) =>
        new(code, title, status, severity, evidence, failureImpact);

    private static ReliableRecipeProtectedScopeProof BuildProtectedScopeProof(bool violationDetected)
    {
        var scopes = new[]
        {
            "OCR/WCU protected scope",
            "Perception live capture protected scope",
            "Recorder/live capture protected scope",
            "Sandbox/VM/container protected scope",
            "Browser/CDP/live execution protected scope",
            "Runtime adapter protected scope"
        };

        return new ReliableRecipeProtectedScopeProof(
            scopes,
            violationDetected ? ["Runtime adapter protected scope"] : [],
            violationDetected ? scopes.Where(s => s != "Runtime adapter protected scope").ToArray() : scopes,
            violationDetected ? ["Protected runtime adapter scope was touched by fixture violation."] : [],
            violationDetected ? "failed" : "passed");
    }

    private static ReliableRecipeNoRuntimeProof BuildNoRuntimeProof(bool runtimeLeakDetected) =>
        new(
            runtimeLeakDetected ? [] : ["Browser live", "CDP live", "Browser driver frameworks", "Desktop live", "OCR live", "Screenshot capture", "Recorder runtime", "Sandbox runtime", "Provider/LLM call", "Network/shell/process runner", "Executable adapter"],
            ["Browser live", "CDP live", "Desktop live", "OCR live", "Recorder runtime", "Sandbox runtime", "Provider/LLM call", "Network/shell/process runner", "Payment/publish/send/delete action"],
            ["Runtime-ready", "Run now", "Adapter enabled", "Automation ready", "Validated live", "Production-ready", "Approved to run"],
            ["Network call", "Shell/process runner", "Provider call", "Productive filesystem write", "Live capture"],
            runtimeLeakDetected ? "failed" : "passed");

    private static ReliableRecipeExternalAuditHandoff BuildExternalAuditHandoff(IReadOnlyList<ReliableRecipeCloseoutBlockSummary> blocks) =>
        new(
            "External audit handoff for no-runtime Reliable Recipe foundation",
            "Audit the M1-M11 no-runtime foundation for read-only UI readiness and runtime boundary integrity.",
            blocks.Select(b => b.BlockId).ToArray(),
            [
                "Do all product-facing reports preserve no-runtime language?",
                "Are OCR, perception, recorder and sandbox scopes proof-only?",
                "Do operator signoff fixtures avoid runtime approval?",
                "Are adapter gates blocked until external audit and explicit future scope?",
                "Are protected browser/live execution files untouched?"
            ],
            ["Runtime remains intentionally unimplemented.", "Fixture reports do not persist real operator approvals.", "Future runtime requires separate protected-scope audit."],
            blocks.Select(b => $"{b.BlockId}:{b.Commit}:{b.FocusedTestCategory}").ToArray(),
            "Runtime is prohibited by this closeout. No operator fixture can approve runtime.",
            ["FINAL_AUDIT_GO_NO_RUNTIME_FOUNDATION_READ_ONLY_UI_READY", "FINAL_AUDIT_GO_WITH_P2_P3_FINDINGS", "FINAL_AUDIT_NO_GO_WITH_P0_P1_FINDINGS"]);

    private static ReliableRecipeExternalAuditHandoff BuildMissingExternalAuditHandoff(IReadOnlyList<ReliableRecipeCloseoutBlockSummary> blocks) =>
        BuildExternalAuditHandoff(blocks) with
        {
            KnownRisks = ["External audit handoff missing from closeout fixture.", "Closeout needs hardening before final audit readiness."]
        };

    private static ReliableRecipeRecommendedNextPhase NextPhase() =>
        new(
            "M13",
            "Read-only Recipe Lab UI audit integration",
            "Read-only UI and external audit handoff only.",
            "Runtime, adapters, browser/desktop launch, live OCR/capture, recorder runtime, sandbox runtime, provider/network/shell actions.",
            "The foundation is audit-ready as no-runtime; next work should expose review artifacts without changing runtime boundaries.");

    private static ReliableRecipeNoRuntimeCloseoutDecision Decision(
        ReliableRecipeInvariantMatrix invariantMatrix,
        ReliableRecipeProtectedScopeProof protectedScopeProof,
        ReliableRecipeNoRuntimeProof noRuntimeProof,
        bool externalAuditHandoffPresent)
    {
        if (noRuntimeProof.OverallStatus == "failed")
            return ReliableRecipeNoRuntimeCloseoutDecision.BlockedByRuntimeLeak;
        if (protectedScopeProof.OverallStatus == "failed")
            return ReliableRecipeNoRuntimeCloseoutDecision.BlockedByProtectedScopeRisk;
        if (!externalAuditHandoffPresent || invariantMatrix.WarningCount > 0)
            return ReliableRecipeNoRuntimeCloseoutDecision.NeedsHardening;
        return ReliableRecipeNoRuntimeCloseoutDecision.ReadyForExternalAudit;
    }
}

public static class ReliableRecipeNoRuntimeCloseoutReportMapper
{
    public static ReliableRecipeLabCloseoutPanel ToLabPanel(ReliableRecipeNoRuntimeCloseoutReport report) =>
        new(
            report.OverallDecision.ToString(),
            $"{report.LineName}: {report.BlockSummaries.Count} blocks closed as no-runtime.",
            $"{report.InvariantMatrix.PassedCount} passed; {report.InvariantMatrix.WarningCount} warning; {report.InvariantMatrix.FailedCount} failed.",
            $"{report.ProtectedScopeProof.UntouchedScopes.Count} protected scopes untouched; {report.ProtectedScopeProof.Violations.Count} violations.",
            $"{report.NoRuntimeProof.RuntimeCapabilitiesAbsent.Count} runtime capabilities absent; runtime not enabled.",
            $"{report.OperatorSignoffFixtures.Count} operator signoff fixtures; none can approve runtime.",
            report.ExternalAuditHandoff.RuntimeProhibitedStatement,
            $"{report.RecommendedNextPhase.NextBlockId}: {report.RecommendedNextPhase.Title}",
            report.NoRuntimeNotice,
            ["Review closeout", "Copy summary", "Export closeout report", "Request external audit"]);
}
