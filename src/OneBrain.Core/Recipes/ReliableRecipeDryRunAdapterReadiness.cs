namespace OneBrain.Core.Recipes;

public sealed record ReliableRecipeDryRunAdapterReadinessReport(
    string ReportId,
    string SubjectId,
    ReliableRecipeDryRunAdapterSubjectKind SubjectKind,
    ReliableRecipeDryRunAdapterReadinessDecision OverallDecision,
    double ReadinessScore,
    IReadOnlyList<ReliableRecipeDryRunAdapterGate> RequiredGates,
    IReadOnlyList<ReliableRecipeDryRunAdapterGate> SatisfiedGates,
    IReadOnlyList<ReliableRecipeDryRunAdapterBlockedCapability> BlockedCapabilities,
    IReadOnlyList<string> MissingPrerequisites,
    IReadOnlyList<ReliableRecipeProtectedScopeReference> ProtectedScopes,
    ReliableRecipeFutureAdapterBoundary FutureAdapterBoundary,
    string NoRuntimeNotice)
{
    public bool ReadOnly => true;
    public bool DesignOnly => true;
    public bool ExecutableAdapterAdded => false;
    public bool RuntimeCommandAdded => false;
    public bool BrowserLiveAdded => false;
    public bool CdpLiveAdded => false;
    public bool CloakLiveAdded => false;
    public bool PlaywrightLiveAdded => false;
    public bool DesktopLiveAdded => false;
    public bool ShellExecutionAdded => false;
    public bool NetworkCallAdded => false;
    public bool ProviderCallAdded => false;
    public bool RuntimeActionExposed => false;
}

public enum ReliableRecipeDryRunAdapterSubjectKind
{
    ReliableRecipe,
    RecorderDraft,
    EvalScenario,
    SandboxReadinessScenario,
    PerceptionScenario,
    PolicyRegressionFixture
}

public enum ReliableRecipeDryRunAdapterReadinessDecision
{
    DesignOnlyReady,
    DesignOnlyReadyWithWarnings,
    BlockedMissingGates,
    BlockedByPolicy,
    BlockedProtectedScope,
    BlockedRuntimeNotAllowed
}

public enum ReliableRecipeDryRunAdapterGate
{
    ReliableRecipePreflightPasses,
    QualityScoreAboveThreshold,
    ValidationExpectationsStructured,
    EvidenceExpectationsStructured,
    RecorderDraftReviewed,
    EvalHarnessPasses,
    SandboxReadinessFixtureOnly,
    PerceptionSignalsSufficient,
    HumanHandoffPolicyPresent,
    ApprovalPolicyPresent,
    SecretRedactionPolicyPresent,
    NoLiveRuntimeCapability,
    ProtectedScopeAuditPassed,
    ExternalAuditRequiredBeforeRuntime
}

public enum ReliableRecipeDryRunAdapterBlockedCapability
{
    BrowserLive,
    CdpLive,
    CloakLive,
    PlaywrightLive,
    DesktopLive,
    RecorderLive,
    OcrLive,
    ScreenshotCapture,
    SandboxRuntime,
    NetworkAccess,
    ShellExecution,
    FilesystemWrite,
    ProviderCall,
    CredentialAutomation,
    CaptchaOrTwoFactorBypass,
    PaymentPublishSendDelete
}

public sealed record ReliableRecipeFutureAdapterBoundary(
    IReadOnlyList<string> AllowedInputs,
    IReadOnlyList<string> AllowedOutputs,
    IReadOnlyList<string> ForbiddenInputs,
    IReadOnlyList<string> ForbiddenOutputs,
    IReadOnlyList<string> AllowedModes,
    IReadOnlyList<string> RequiredApprovals,
    IReadOnlyList<string> EvidenceRequirements,
    IReadOnlyList<string> ValidationRequirements,
    IReadOnlyList<string> PerceptionRequirements,
    IReadOnlyList<string> SandboxRequirements);

public sealed record ReliableRecipeProtectedScopeReference(
    string Name,
    string Reason,
    string AllowedTouch,
    string ForbiddenTouch,
    bool AuditRequiredBeforeChange);

public sealed record ReliableRecipeDryRunAdapterReadinessScenario(
    string ScenarioId,
    string SubjectId,
    ReliableRecipeDryRunAdapterSubjectKind SubjectKind,
    ReliableRecipeDryRunAdapterReadinessDecision ExpectedDecision,
    bool IncludeEval,
    bool IncludeSandbox,
    bool IncludePerception,
    bool ProtectedScopeAuditPassed,
    bool RuntimeCapabilityRequested,
    IReadOnlyList<ReliableRecipeDryRunAdapterBlockedCapability> RequestedBlockedCapabilities,
    string Summary)
{
    public bool FixtureOnly => true;
    public bool DesignOnly => true;
    public bool RuntimeEnabled => false;
    public bool UsesLiveBrowser => false;
    public bool UsesLiveDesktop => false;
    public bool UsesNetwork => false;
    public bool StoresSecrets => false;
    public bool LaunchesAdapter => false;
}

public sealed record ReliableRecipeLabDryRunAdapterReadinessPanel(
    string DecisionLabel,
    double ReadinessScore,
    IReadOnlyList<string> RequiredGates,
    IReadOnlyList<string> MissingGates,
    IReadOnlyList<string> BlockedCapabilities,
    IReadOnlyList<string> ProtectedScopeNotices,
    string FutureAdapterBoundarySummary,
    string NoRuntimeNotice,
    IReadOnlyList<string> ReadOnlyActionLabels)
{
    public bool ReadOnly => true;
    public bool DesignOnly => true;
    public bool RuntimeActionExposed => false;
    public bool CanStartAdapter => false;
    public bool CanExecuteDryRun => false;
    public bool CanConnectBrowser => false;
    public bool CanLaunchCloak => false;
    public bool CanUseCdp => false;
}

public static class ReliableRecipeDryRunAdapterReadinessScenarioCatalog
{
    public static IReadOnlyList<ReliableRecipeDryRunAdapterReadinessScenario> All() =>
    [
        Scenario("complete_fixture_stack_design_only_ready", "safe_invoice_download_dry_run_candidate_eval", ReliableRecipeDryRunAdapterSubjectKind.EvalScenario, ReliableRecipeDryRunAdapterReadinessDecision.DesignOnlyReadyWithWarnings, includeEval: true, includeSandbox: true, includePerception: true, protectedScopeAuditPassed: true, runtimeRequested: false, [], "Complete fixture stack is ready for design-only adapter boundary review."),
        Scenario("missing_structured_evidence_blocked", "external_side_effect_needs_approval", ReliableRecipeDryRunAdapterSubjectKind.ReliableRecipe, ReliableRecipeDryRunAdapterReadinessDecision.BlockedMissingGates, includeEval: true, includeSandbox: true, includePerception: false, protectedScopeAuditPassed: true, runtimeRequested: false, [], "Missing structured evidence blocks adapter readiness."),
        Scenario("missing_structured_validation_blocked", "submit_without_validation_quality_blocked", ReliableRecipeDryRunAdapterSubjectKind.ReliableRecipe, ReliableRecipeDryRunAdapterReadinessDecision.BlockedMissingGates, includeEval: true, includeSandbox: true, includePerception: false, protectedScopeAuditPassed: true, runtimeRequested: false, [], "Missing structured validation blocks adapter readiness."),
        Scenario("recorder_draft_unreviewed_blocked", "invoice_download_demonstration", ReliableRecipeDryRunAdapterSubjectKind.RecorderDraft, ReliableRecipeDryRunAdapterReadinessDecision.BlockedMissingGates, includeEval: true, includeSandbox: true, includePerception: true, protectedScopeAuditPassed: true, runtimeRequested: false, [], "Unreviewed recorder draft cannot be adapter-ready."),
        Scenario("eval_harness_missing_blocked", "safe_invoice_download_quality_pass", ReliableRecipeDryRunAdapterSubjectKind.ReliableRecipe, ReliableRecipeDryRunAdapterReadinessDecision.BlockedMissingGates, includeEval: false, includeSandbox: true, includePerception: true, protectedScopeAuditPassed: true, runtimeRequested: false, [], "Eval harness report is missing."),
        Scenario("sandbox_readiness_missing_blocked", "safe_invoice_download_dry_run_candidate_eval", ReliableRecipeDryRunAdapterSubjectKind.EvalScenario, ReliableRecipeDryRunAdapterReadinessDecision.BlockedMissingGates, includeEval: true, includeSandbox: false, includePerception: true, protectedScopeAuditPassed: true, runtimeRequested: false, [], "Sandbox readiness report is missing."),
        Scenario("perception_signals_weak_blocked", "ocr_only_sensitive_target_blocked", ReliableRecipeDryRunAdapterSubjectKind.PerceptionScenario, ReliableRecipeDryRunAdapterReadinessDecision.BlockedMissingGates, includeEval: true, includeSandbox: true, includePerception: false, protectedScopeAuditPassed: true, runtimeRequested: false, [], "Missing perception signals block adapter readiness."),
        Scenario("high_risk_external_submit_blocked", "government_form_submit_demonstration", ReliableRecipeDryRunAdapterSubjectKind.RecorderDraft, ReliableRecipeDryRunAdapterReadinessDecision.BlockedByPolicy, includeEval: true, includeSandbox: true, includePerception: true, protectedScopeAuditPassed: true, runtimeRequested: false, [], "High-risk external submit remains blocked by policy."),
        Scenario("protected_scope_audit_required", "safe_invoice_download_dry_run_candidate_eval", ReliableRecipeDryRunAdapterSubjectKind.EvalScenario, ReliableRecipeDryRunAdapterReadinessDecision.BlockedProtectedScope, includeEval: true, includeSandbox: true, includePerception: true, protectedScopeAuditPassed: false, runtimeRequested: false, [], "Protected-scope audit is required before any future runtime work."),
        Scenario("runtime_capability_requested_blocked", "safe_invoice_download_dry_run_candidate_eval", ReliableRecipeDryRunAdapterSubjectKind.EvalScenario, ReliableRecipeDryRunAdapterReadinessDecision.BlockedRuntimeNotAllowed, includeEval: true, includeSandbox: true, includePerception: true, protectedScopeAuditPassed: true, runtimeRequested: true, [ReliableRecipeDryRunAdapterBlockedCapability.BrowserLive, ReliableRecipeDryRunAdapterBlockedCapability.CdpLive], "Runtime capability request is blocked in M8."),
        Scenario("captcha_two_factor_bypass_blocked", "captcha_two_factor_challenge_demonstration", ReliableRecipeDryRunAdapterSubjectKind.RecorderDraft, ReliableRecipeDryRunAdapterReadinessDecision.BlockedByPolicy, includeEval: true, includeSandbox: true, includePerception: true, protectedScopeAuditPassed: true, runtimeRequested: false, [ReliableRecipeDryRunAdapterBlockedCapability.CaptchaOrTwoFactorBypass], "CAPTCHA/2FA bypass remains blocked."),
        Scenario("ocr_live_requested_blocked", "ocr_only_sensitive_target_blocked", ReliableRecipeDryRunAdapterSubjectKind.PerceptionScenario, ReliableRecipeDryRunAdapterReadinessDecision.BlockedRuntimeNotAllowed, includeEval: true, includeSandbox: true, includePerception: true, protectedScopeAuditPassed: true, runtimeRequested: true, [ReliableRecipeDryRunAdapterBlockedCapability.OcrLive, ReliableRecipeDryRunAdapterBlockedCapability.ScreenshotCapture], "OCR live request remains blocked.")
    ];

    public static ReliableRecipeDryRunAdapterReadinessScenario Get(string scenarioId) =>
        All().Single(s => s.ScenarioId == scenarioId);

    private static ReliableRecipeDryRunAdapterReadinessScenario Scenario(
        string scenarioId,
        string subjectId,
        ReliableRecipeDryRunAdapterSubjectKind subjectKind,
        ReliableRecipeDryRunAdapterReadinessDecision expected,
        bool includeEval,
        bool includeSandbox,
        bool includePerception,
        bool protectedScopeAuditPassed,
        bool runtimeRequested,
        IReadOnlyList<ReliableRecipeDryRunAdapterBlockedCapability> requestedCapabilities,
        string summary) =>
        new(scenarioId, subjectId, subjectKind, expected, includeEval, includeSandbox, includePerception, protectedScopeAuditPassed, runtimeRequested, requestedCapabilities, summary);
}

public static class ReliableRecipeDryRunAdapterReadinessEvaluator
{
    private static readonly ReliableRecipeDryRunAdapterGate[] RequiredGates =
    [
        ReliableRecipeDryRunAdapterGate.ReliableRecipePreflightPasses,
        ReliableRecipeDryRunAdapterGate.QualityScoreAboveThreshold,
        ReliableRecipeDryRunAdapterGate.ValidationExpectationsStructured,
        ReliableRecipeDryRunAdapterGate.EvidenceExpectationsStructured,
        ReliableRecipeDryRunAdapterGate.RecorderDraftReviewed,
        ReliableRecipeDryRunAdapterGate.EvalHarnessPasses,
        ReliableRecipeDryRunAdapterGate.SandboxReadinessFixtureOnly,
        ReliableRecipeDryRunAdapterGate.PerceptionSignalsSufficient,
        ReliableRecipeDryRunAdapterGate.HumanHandoffPolicyPresent,
        ReliableRecipeDryRunAdapterGate.ApprovalPolicyPresent,
        ReliableRecipeDryRunAdapterGate.SecretRedactionPolicyPresent,
        ReliableRecipeDryRunAdapterGate.NoLiveRuntimeCapability,
        ReliableRecipeDryRunAdapterGate.ProtectedScopeAuditPassed,
        ReliableRecipeDryRunAdapterGate.ExternalAuditRequiredBeforeRuntime
    ];

    private static readonly ReliableRecipeDryRunAdapterBlockedCapability[] AlwaysBlockedCapabilities =
    [
        ReliableRecipeDryRunAdapterBlockedCapability.BrowserLive,
        ReliableRecipeDryRunAdapterBlockedCapability.CdpLive,
        ReliableRecipeDryRunAdapterBlockedCapability.CloakLive,
        ReliableRecipeDryRunAdapterBlockedCapability.PlaywrightLive,
        ReliableRecipeDryRunAdapterBlockedCapability.DesktopLive,
        ReliableRecipeDryRunAdapterBlockedCapability.RecorderLive,
        ReliableRecipeDryRunAdapterBlockedCapability.OcrLive,
        ReliableRecipeDryRunAdapterBlockedCapability.ScreenshotCapture,
        ReliableRecipeDryRunAdapterBlockedCapability.SandboxRuntime,
        ReliableRecipeDryRunAdapterBlockedCapability.NetworkAccess,
        ReliableRecipeDryRunAdapterBlockedCapability.ShellExecution,
        ReliableRecipeDryRunAdapterBlockedCapability.FilesystemWrite,
        ReliableRecipeDryRunAdapterBlockedCapability.ProviderCall,
        ReliableRecipeDryRunAdapterBlockedCapability.CredentialAutomation,
        ReliableRecipeDryRunAdapterBlockedCapability.CaptchaOrTwoFactorBypass,
        ReliableRecipeDryRunAdapterBlockedCapability.PaymentPublishSendDelete
    ];

    public static ReliableRecipeDryRunAdapterReadinessReport Evaluate(ReliableRecipeDryRunAdapterReadinessScenario scenario)
    {
        var source = Resolve(scenario);
        return EvaluateSource(
            $"dryrun-adapter-readiness.{scenario.ScenarioId}",
            scenario.SubjectId,
            scenario.SubjectKind,
            source.Recipe,
            source.PreflightReport,
            source.Draft,
            source.EvalRun,
            source.SandboxReport,
            source.PerceptionReport,
            source.StructuredPrerequisiteProfile,
            null,
            scenario.ProtectedScopeAuditPassed,
            scenario.RuntimeCapabilityRequested,
            scenario.RequestedBlockedCapabilities);
    }

    public static ReliableRecipeDryRunAdapterReadinessReport Evaluate(
        ReliableRecipeDefinition recipe,
        ReliableRecipePreflightReport preflightReport,
        RecorderToRecipeDraft? draft = null,
        ReliableRecipeFixtureEvalRun? evalRun = null,
        ComputerUseSandboxReadinessReport? sandboxReport = null,
        ReliableRecipePerceptionIntegrationReport? perceptionReport = null,
        ReliableRecipeStructuredPrerequisiteProfile? structuredPrerequisiteProfile = null,
        StructuredPrerequisiteAuthoringReport? authoringReport = null) =>
        EvaluateSource(
            $"dryrun-adapter-readiness.{recipe.Id}",
            recipe.Id,
            draft is null ? ReliableRecipeDryRunAdapterSubjectKind.ReliableRecipe : ReliableRecipeDryRunAdapterSubjectKind.RecorderDraft,
            recipe,
            preflightReport,
            draft,
            evalRun,
            sandboxReport,
            perceptionReport,
            structuredPrerequisiteProfile,
            authoringReport,
            protectedScopeAuditPassed: true,
            runtimeCapabilityRequested: false,
            requestedBlockedCapabilities: []);

    private static ReliableRecipeDryRunAdapterReadinessReport EvaluateSource(
        string reportId,
        string subjectId,
        ReliableRecipeDryRunAdapterSubjectKind subjectKind,
        ReliableRecipeDefinition recipe,
        ReliableRecipePreflightReport preflightReport,
        RecorderToRecipeDraft? draft,
        ReliableRecipeFixtureEvalRun? evalRun,
        ComputerUseSandboxReadinessReport? sandboxReport,
        ReliableRecipePerceptionIntegrationReport? perceptionReport,
        ReliableRecipeStructuredPrerequisiteProfile? structuredPrerequisiteProfile,
        StructuredPrerequisiteAuthoringReport? authoringReport,
        bool protectedScopeAuditPassed,
        bool runtimeCapabilityRequested,
        IReadOnlyList<ReliableRecipeDryRunAdapterBlockedCapability> requestedBlockedCapabilities)
    {
        var structured = structuredPrerequisiteProfile ?? ReliableRecipeStructuredPrerequisiteEvaluator.Evaluate(recipe, preflightReport, draft, evalRun, sandboxReport, perceptionReport, ReliableRecipeStructuredPrerequisiteSubjectKind.DryRunAdapterReadinessScenario, subjectId);
        var satisfied = SatisfiedGates(recipe, preflightReport, draft, evalRun, sandboxReport, perceptionReport, structured, authoringReport, protectedScopeAuditPassed, runtimeCapabilityRequested).Distinct().ToArray();
        var missing = RequiredGates.Except(satisfied).Select(g => $"Gate not satisfied: {g}.").ToArray();
        var blocked = AlwaysBlockedCapabilities.Concat(requestedBlockedCapabilities).Distinct().ToArray();
        var policyBlocked = IsPolicyBlocked(recipe, preflightReport, draft, evalRun, sandboxReport, perceptionReport);
        var decision = DecisionFor(satisfied, missing, protectedScopeAuditPassed, runtimeCapabilityRequested, policyBlocked);
        var score = ScoreFor(decision, satisfied.Length, missing.Length);

        return new ReliableRecipeDryRunAdapterReadinessReport(
            reportId,
            subjectId,
            subjectKind,
            decision,
            score,
            RequiredGates,
            satisfied,
            blocked,
            missing,
            ProtectedScopes(),
            Boundary(preflightReport, sandboxReport, perceptionReport),
            "Adapter readiness design only. No runtime enabled; no executable adapter is present.");
    }

    private static IEnumerable<ReliableRecipeDryRunAdapterGate> SatisfiedGates(
        ReliableRecipeDefinition recipe,
        ReliableRecipePreflightReport preflightReport,
        RecorderToRecipeDraft? draft,
        ReliableRecipeFixtureEvalRun? evalRun,
        ComputerUseSandboxReadinessReport? sandboxReport,
        ReliableRecipePerceptionIntegrationReport? perceptionReport,
        ReliableRecipeStructuredPrerequisiteProfile structuredPrerequisiteProfile,
        StructuredPrerequisiteAuthoringReport? authoringReport,
        bool protectedScopeAuditPassed,
        bool runtimeCapabilityRequested)
    {
        if (preflightReport.PolicyDecision != ReliableRecipePolicyDecision.Reject)
            yield return ReliableRecipeDryRunAdapterGate.ReliableRecipePreflightPasses;
        if (preflightReport.QualityReport.OverallScore >= 0.72 && preflightReport.QualityReport.Decision != ReliableRecipeQualityDecision.Blocked)
            yield return ReliableRecipeDryRunAdapterGate.QualityScoreAboveThreshold;
        var authoringAllowsDesignGates = StructuredPrerequisiteAuthoringEvaluator.AllowsAdapterDesignGates(authoringReport);
        var authoringReviewedStructuredRequirements = authoringReport is { OverallDecision: StructuredPrerequisiteAuthoringDecision.DesignOnlyReadyWithReviewedRequirements };
        if (authoringAllowsDesignGates && (structuredPrerequisiteProfile.CompletenessReport.ValidationGateSatisfied || authoringReviewedStructuredRequirements))
            yield return ReliableRecipeDryRunAdapterGate.ValidationExpectationsStructured;
        if (authoringAllowsDesignGates && (structuredPrerequisiteProfile.CompletenessReport.EvidenceGateSatisfied || authoringReviewedStructuredRequirements))
            yield return ReliableRecipeDryRunAdapterGate.EvidenceExpectationsStructured;
        if (draft is null || draft.ReviewState == RecorderDraftReviewState.DryRunCandidate)
            yield return ReliableRecipeDryRunAdapterGate.RecorderDraftReviewed;
        if (evalRun is { FinalDecision: ReliableRecipeFixtureEvalFinalDecision.FixturePass or ReliableRecipeFixtureEvalFinalDecision.FixturePassWithWarnings })
            yield return ReliableRecipeDryRunAdapterGate.EvalHarnessPasses;
        if (sandboxReport is { OverallDecision: ComputerUseSandboxReadinessDecision.FixtureReady or ComputerUseSandboxReadinessDecision.FixtureReadyWithWarnings })
            yield return ReliableRecipeDryRunAdapterGate.SandboxReadinessFixtureOnly;
        if (perceptionReport is { OverallDecision: ReliableRecipePerceptionDecision.FixtureSignalsSufficient or ReliableRecipePerceptionDecision.FixtureSignalsSufficientWithWarnings })
            yield return ReliableRecipeDryRunAdapterGate.PerceptionSignalsSufficient;
        if (preflightReport.RequiredHumanInterventions.Count == 0 || preflightReport.QualityReport.HumanInterventionPlanQuality.Score >= 0.75)
            yield return ReliableRecipeDryRunAdapterGate.HumanHandoffPolicyPresent;
        if (preflightReport.RequiredApprovals.Count == 0)
            yield return ReliableRecipeDryRunAdapterGate.ApprovalPolicyPresent;
        if (draft is null || draft.SensitiveInputSummary.RedactionApplied)
            yield return ReliableRecipeDryRunAdapterGate.SecretRedactionPolicyPresent;
        if (!runtimeCapabilityRequested && !recipe.LiveRuntimeEnabled && !recipe.BrowserAutomationEnabled && !recipe.DesktopAutomationEnabled)
            yield return ReliableRecipeDryRunAdapterGate.NoLiveRuntimeCapability;
        if (protectedScopeAuditPassed)
            yield return ReliableRecipeDryRunAdapterGate.ProtectedScopeAuditPassed;
        yield return ReliableRecipeDryRunAdapterGate.ExternalAuditRequiredBeforeRuntime;
    }

    private static bool IsPolicyBlocked(
        ReliableRecipeDefinition recipe,
        ReliableRecipePreflightReport preflightReport,
        RecorderToRecipeDraft? draft,
        ReliableRecipeFixtureEvalRun? evalRun,
        ComputerUseSandboxReadinessReport? sandboxReport,
        ReliableRecipePerceptionIntegrationReport? perceptionReport) =>
        recipe.RiskProfile.HasFlag(ReliableRecipeRiskProfile.Financial) ||
        recipe.RiskProfile.HasFlag(ReliableRecipeRiskProfile.Irreversible) ||
        draft?.ReviewState is RecorderDraftReviewState.BlockedChallenge or RecorderDraftReviewState.BlockedSensitiveInput ||
        evalRun?.FinalDecision == ReliableRecipeFixtureEvalFinalDecision.RegressionDetected ||
        sandboxReport?.OverallDecision is ComputerUseSandboxReadinessDecision.BlockedByPolicy or ComputerUseSandboxReadinessDecision.BlockedLiveRuntimeNotAllowed ||
        perceptionReport?.OverallDecision is ReliableRecipePerceptionDecision.BlockedSensitiveActionAuthority or ReliableRecipePerceptionDecision.BlockedContradictorySignals;

    private static ReliableRecipeDryRunAdapterReadinessDecision DecisionFor(
        IReadOnlyList<ReliableRecipeDryRunAdapterGate> satisfied,
        IReadOnlyList<string> missing,
        bool protectedScopeAuditPassed,
        bool runtimeCapabilityRequested,
        bool policyBlocked)
    {
        if (runtimeCapabilityRequested)
            return ReliableRecipeDryRunAdapterReadinessDecision.BlockedRuntimeNotAllowed;
        if (!protectedScopeAuditPassed)
            return ReliableRecipeDryRunAdapterReadinessDecision.BlockedProtectedScope;
        if (policyBlocked)
            return ReliableRecipeDryRunAdapterReadinessDecision.BlockedByPolicy;
        if (missing.Count > 0)
            return ReliableRecipeDryRunAdapterReadinessDecision.BlockedMissingGates;
        return satisfied.Contains(ReliableRecipeDryRunAdapterGate.ExternalAuditRequiredBeforeRuntime)
            ? ReliableRecipeDryRunAdapterReadinessDecision.DesignOnlyReadyWithWarnings
            : ReliableRecipeDryRunAdapterReadinessDecision.DesignOnlyReady;
    }

    private static double ScoreFor(ReliableRecipeDryRunAdapterReadinessDecision decision, int satisfied, int missing)
    {
        var baseScore = decision switch
        {
            ReliableRecipeDryRunAdapterReadinessDecision.DesignOnlyReady => 0.78,
            ReliableRecipeDryRunAdapterReadinessDecision.DesignOnlyReadyWithWarnings => 0.72,
            ReliableRecipeDryRunAdapterReadinessDecision.BlockedMissingGates => 0.45,
            ReliableRecipeDryRunAdapterReadinessDecision.BlockedByPolicy => 0.3,
            ReliableRecipeDryRunAdapterReadinessDecision.BlockedProtectedScope => 0.2,
            ReliableRecipeDryRunAdapterReadinessDecision.BlockedRuntimeNotAllowed => 0.1,
            _ => 0
        };

        return Math.Round(Math.Clamp(baseScore + satisfied * 0.005 - missing * 0.01, 0, 1), 2);
    }

    private static IReadOnlyList<ReliableRecipeProtectedScopeReference> ProtectedScopes() =>
    [
        new("post-M1345 browser/live execution", "Browser runtime, CDP and protected live execution paths are out of scope.", "Documentation and audit references only.", "Runtime implementation, launch, connection or mutation.", true),
        new("OCR/WCU interop", "OCR and Windows Computer Use are protected existing capabilities.", "Fixture/supporting signal references only.", "OCR internals, activation gates, screenshot capture or action authority.", true),
        new("recorder/live capture", "Recorder runtime is not allowed in M8.", "Fixture recorder draft references only.", "Keyboard, mouse, clipboard, screen capture or background listener.", true),
        new("sandbox/runtime", "Computer-use sandbox remains design-only.", "Readiness report references only.", "VM/container/Docker/browser/desktop launch or filesystem jail runtime.", true)
    ];

    private static ReliableRecipeFutureAdapterBoundary Boundary(
        ReliableRecipePreflightReport preflightReport,
        ComputerUseSandboxReadinessReport? sandboxReport,
        ReliableRecipePerceptionIntegrationReport? perceptionReport) =>
        new(
            ["ReliableRecipeDefinition", "ReliableRecipePreflightReport", "ReliableRecipeQualityReport", "fixture eval report", "sandbox readiness report", "perception integration report"],
            ["read-only adapter readiness report", "evidence expectation plan", "validation expectation plan", "operator handoff checklist"],
            ["live browser state", "live desktop state", "raw credentials", "screenshots", "network responses", "provider output"],
            ["executed actions", "live artifacts", "filesystem writes", "network calls", "runtime commands"],
            ["Design-only", "Fixture-only prerequisite review"],
            preflightReport.RequiredApprovals.Count == 0 ? ["External audit required before runtime."] : preflightReport.RequiredApprovals,
            preflightReport.RequiredEvidence.Select(e => e.ToString()).DefaultIfEmpty("Structured evidence expectations must remain reference-only.").ToArray(),
            preflightReport.RequiredValidations.Select(v => v.ToString()).DefaultIfEmpty("Structured validation expectations must prove completion before future adapter work.").ToArray(),
            perceptionReport?.MissingSignals.Select(s => s.ToString()).DefaultIfEmpty("Perception signals must be fixture-only and sufficient.").ToArray() ?? ["Perception integration report required."],
            sandboxReport?.MissingRequirements.Select(r => r.ToString()).DefaultIfEmpty("Sandbox readiness must remain fixture-only.").ToArray() ?? ["Sandbox readiness report required."]);

    private static ReliableRecipeDryRunAdapterReadinessSource Resolve(ReliableRecipeDryRunAdapterReadinessScenario scenario)
    {
        var (recipe, preflight, draft) = ResolveSubject(scenario);
        var evalRun = scenario.IncludeEval ? ResolveEval(scenario, recipe, draft) : null;
        var sandbox = scenario.IncludeSandbox ? ResolveSandbox(scenario, recipe, preflight, draft, evalRun) : null;
        var perception = scenario.IncludePerception ? ResolvePerception(scenario, recipe, preflight) : null;
        var structured = ReliableRecipeStructuredPrerequisiteEvaluator.Evaluate(recipe, preflight, draft, evalRun, sandbox, perception, ReliableRecipeStructuredPrerequisiteSubjectKind.DryRunAdapterReadinessScenario, scenario.ScenarioId);
        return new ReliableRecipeDryRunAdapterReadinessSource(recipe, preflight, draft, evalRun, sandbox, perception, structured);
    }

    private static (ReliableRecipeDefinition Recipe, ReliableRecipePreflightReport Preflight, RecorderToRecipeDraft? Draft) ResolveSubject(ReliableRecipeDryRunAdapterReadinessScenario scenario)
    {
        if (scenario.SubjectKind == ReliableRecipeDryRunAdapterSubjectKind.RecorderDraft)
        {
            var draft = ReliableRecipeRecorderFixtureCatalog.Get(scenario.SubjectId).Draft;
            return (draft.Recipe, draft.PreflightReport, draft);
        }

        if (scenario.SubjectKind == ReliableRecipeDryRunAdapterSubjectKind.EvalScenario ||
            scenario.SubjectKind == ReliableRecipeDryRunAdapterSubjectKind.PolicyRegressionFixture)
        {
            var evalScenario = ReliableRecipeEvalScenarioCatalog.Get(scenario.SubjectId);
            if (evalScenario.SourceKind == ReliableRecipeFixtureEvalSourceKind.RecorderDraft)
            {
                var draft = ReliableRecipeRecorderFixtureCatalog.Get(evalScenario.FixtureId).Draft;
                return (draft.Recipe, draft.PreflightReport, draft);
            }

            var fixture = ReliableRecipeLabFixtureCatalog.Get(evalScenario.FixtureId);
            return (fixture.Recipe, fixture.PreflightReport, null);
        }

        if (scenario.SubjectKind == ReliableRecipeDryRunAdapterSubjectKind.PerceptionScenario)
        {
            var perceptionScenario = ReliableRecipePerceptionScenarioCatalog.Get(scenario.SubjectId);
            if (perceptionScenario.SubjectId == "ocr_only_sensitive_submit_blocked")
            {
                var fixture = ReliableRecipeLabFixtureCatalog.Get("ocr_only_sensitive_submit_blocked");
                return (fixture.Recipe, fixture.PreflightReport, null);
            }

            var fallback = ReliableRecipeLabFixtureCatalog.Get("safe_invoice_download_quality_pass");
            return (fallback.Recipe, fallback.PreflightReport, null);
        }

        var lab = ReliableRecipeLabFixtureCatalog.Get(scenario.SubjectId);
        return (lab.Recipe, lab.PreflightReport, null);
    }

    private static ReliableRecipeFixtureEvalRun? ResolveEval(ReliableRecipeDryRunAdapterReadinessScenario scenario, ReliableRecipeDefinition recipe, RecorderToRecipeDraft? draft)
    {
        if (scenario.SubjectKind is ReliableRecipeDryRunAdapterSubjectKind.EvalScenario or ReliableRecipeDryRunAdapterSubjectKind.PolicyRegressionFixture)
            return ReliableRecipeFixtureEvalRunner.Evaluate(ReliableRecipeEvalScenarioCatalog.Get(scenario.SubjectId));
        if (scenario.SubjectId == "external_side_effect_needs_approval")
            return null;
        if (scenario.SubjectId == "submit_without_validation_quality_blocked")
            return null;
        if (draft is not null)
        {
            var evalId = scenario.SubjectId switch
            {
                "invoice_download_demonstration" => "invoice_download_missing_validation_eval",
                "government_form_submit_demonstration" => "government_submit_high_risk_blocked_eval",
                "captcha_two_factor_challenge_demonstration" => "captcha_two_factor_handoff_eval",
                _ => "corrected_user_click_review_eval"
            };
            return ReliableRecipeFixtureEvalRunner.Evaluate(ReliableRecipeEvalScenarioCatalog.Get(evalId));
        }

        return ReliableRecipeFixtureEvalRunner.Evaluate(ReliableRecipeEvalScenarioCatalog.Get("safe_invoice_download_dry_run_candidate_eval"));
    }

    private static ComputerUseSandboxReadinessReport? ResolveSandbox(ReliableRecipeDryRunAdapterReadinessScenario scenario, ReliableRecipeDefinition recipe, ReliableRecipePreflightReport preflight, RecorderToRecipeDraft? draft, ReliableRecipeFixtureEvalRun? evalRun)
    {
        if (evalRun is not null)
        {
            var evalScenario = ReliableRecipeEvalScenarioCatalog.Get(evalRun.ScenarioId);
            return ComputerUseSandboxReadinessEvaluator.Evaluate(evalScenario, evalRun);
        }
        if (draft is not null)
            return ComputerUseSandboxReadinessEvaluator.Evaluate(draft);
        return ComputerUseSandboxReadinessEvaluator.Evaluate(recipe, preflight);
    }

    private static ReliableRecipePerceptionIntegrationReport? ResolvePerception(ReliableRecipeDryRunAdapterReadinessScenario scenario, ReliableRecipeDefinition recipe, ReliableRecipePreflightReport preflight)
    {
        if (scenario.SubjectKind == ReliableRecipeDryRunAdapterSubjectKind.PerceptionScenario)
            return ReliableRecipePerceptionIntegrationEvaluator.Evaluate(ReliableRecipePerceptionScenarioCatalog.Get(scenario.SubjectId));
        if (scenario.SubjectId == "captcha_two_factor_challenge_demonstration")
            return ReliableRecipePerceptionIntegrationEvaluator.Evaluate(ReliableRecipePerceptionScenarioCatalog.Get("captcha_challenge_detected"));
        if (scenario.SubjectId == "government_form_submit_demonstration")
            return ReliableRecipePerceptionIntegrationEvaluator.Evaluate(ReliableRecipePerceptionScenarioCatalog.Get("high_confidence_high_risk_still_blocked"));
        return ReliableRecipePerceptionIntegrationEvaluator.Evaluate(recipe, preflight);
    }

    private sealed record ReliableRecipeDryRunAdapterReadinessSource(
        ReliableRecipeDefinition Recipe,
        ReliableRecipePreflightReport PreflightReport,
        RecorderToRecipeDraft? Draft,
        ReliableRecipeFixtureEvalRun? EvalRun,
        ComputerUseSandboxReadinessReport? SandboxReport,
        ReliableRecipePerceptionIntegrationReport? PerceptionReport,
        ReliableRecipeStructuredPrerequisiteProfile StructuredPrerequisiteProfile);
}

public static class ReliableRecipeDryRunAdapterReadinessReportMapper
{
    public static ReliableRecipeLabDryRunAdapterReadinessPanel ToLabPanel(ReliableRecipeDryRunAdapterReadinessReport report) =>
        new(
            report.OverallDecision.ToString(),
            report.ReadinessScore,
            report.RequiredGates.Select(g => g.ToString()).ToArray(),
            report.MissingPrerequisites,
            report.BlockedCapabilities.Select(c => c.ToString()).ToArray(),
            report.ProtectedScopes.Select(s => $"{s.Name}: protected-scope audit required before change.").ToArray(),
            "Future dry-run adapter boundary is design-only and limited to fixture prerequisite review.",
            report.NoRuntimeNotice,
            ["Review readiness design", "Open audit notes", "Copy summary"]);
}
