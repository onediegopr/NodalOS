namespace OneBrain.Core.Recipes;

public sealed record ReliableRecipeDefinition(
    string Id,
    string Name,
    string Version,
    string WorkspaceScope,
    IReadOnlyList<ReliableRecipeVariable> Variables,
    IReadOnlyList<ReliableRecipeBlock> Blocks,
    ReliableRecipeRunLimits? Limits,
    ReliableRecipeRiskProfile RiskProfile,
    ReliableRecipeReadiness Readiness,
    ReliableRecipeCreatedFrom CreatedFrom)
{
    public bool LiveRuntimeEnabled => false;
    public bool BrowserAutomationEnabled => false;
    public bool DesktopAutomationEnabled => false;
    public bool RecorderEnabled => false;
    public bool SandboxEnabled => false;
}

public sealed record ReliableRecipeVariable(
    string Name,
    string ValueRef,
    bool SecretByReference = false,
    bool RawValuePresent = false);

public sealed record ReliableRecipeBlock(
    string Id,
    ReliableRecipeBlockKind Kind,
    string Label,
    IReadOnlyDictionary<string, string> Config,
    IReadOnlyList<string> Inputs,
    IReadOnlyList<string> Outputs,
    ReliableRecipeRiskProfile Risk,
    IReadOnlyList<string> EvidenceExpectations,
    IReadOnlyList<string> ValidationRequirements);

public enum ReliableRecipeBlockKind
{
    BrowserGoal,
    BrowserAction,
    Extract,
    Validate,
    Wait,
    Conditional,
    Loop,
    HumanIntervention,
    FileDownloadEvidence,
    CaptureArtifact,
    ConnectorDraft,
    DesktopFuture,
    SandboxFuture
}

public sealed record ReliableRecipeRunLimits(
    int? MaxSteps,
    int? MaxRetries,
    int? MaxLoopIterations,
    int? MaxRuntimeSeconds,
    IReadOnlyList<string> AllowedDomains,
    IReadOnlyList<ReliableRecipeBlockKind> AllowedActions,
    ReliableCompleteCriteria? CompleteCriteria,
    ReliableTerminateCriteria? TerminateCriteria);

public enum ReliableRecipeRunMode
{
    DraftOnly,
    DryRun,
    AssistedRun,
    SupervisedRun,
    LimitedAutonomy
}

public enum ReliableRecipeReadiness
{
    DesignOnly,
    RunnableDryRun,
    RunnableAssisted,
    RunnableSupervised,
    BlockedNeedsReview
}

public enum ReliableRecipeCreatedFrom
{
    ManualDesign,
    Template,
    RecorderDraft,
    SopDraft,
    ImportedDefinition
}

[Flags]
public enum ReliableRecipeRiskProfile
{
    None = 0,
    ReadOnly = 1,
    LocalWrite = 2,
    ExternalSideEffect = 4,
    Credentialed = 8,
    Financial = 16,
    Destructive = 32,
    Irreversible = 64,
    SensitiveData = 128
}

public enum ReliableRecipePolicyDecision
{
    AllowReadOnly,
    AllowDryRunOnly,
    NeedsApproval,
    NeedsHumanIntervention,
    Reject
}

public sealed record ReliableRecipePreflightIssue(
    string IssueId,
    ReliableRecipePolicyDecision Decision,
    string Message,
    string? BlockId = null);

public sealed record ReliableRecipePreflightResult(
    bool IsReady,
    ReliableRecipeReadiness Readiness,
    ReliableRecipePolicyDecision Decision,
    IReadOnlyList<ReliableRecipePreflightIssue> BlockingIssues,
    IReadOnlyList<ReliableRecipePreflightIssue> Warnings)
{
    public bool LiveRuntimeEnabled => false;
    public bool ActionAuthorityGranted => false;
}

public sealed record ReliableActionResolutionPolicy(
    IReadOnlyList<ReliableActionResolutionMode> AllowedResolutionModes,
    ReliableActionResolutionMode PreferredMode,
    IReadOnlyList<ReliableActionResolutionMode> FallbackModes,
    bool FallbackAllowed,
    bool FallbackRequiresApproval,
    int MaxAttempts,
    double MinimumConfidence,
    bool EvidenceRequired,
    double HumanHandoffThreshold);

public enum ReliableActionResolutionMode
{
    KnownTarget,
    StableSelector,
    DomOrCdpSnapshot,
    AccessibilityTree,
    VisibleTextExact,
    VisibleTextApproximate,
    OcrRegion,
    VisualBoundingBox,
    SetOfMarks,
    AiSemanticFallback,
    HumanIntervention,
    Abort
}

public sealed record ReliableTargetResolutionConfidence(
    double Score,
    IReadOnlyList<ReliableActionResolutionMode> SignalsUsed,
    string? AmbiguityReason,
    ReliableRecipePolicyDecision RiskAdjustedDecision);

public sealed record ReliableTargetDescriptor(
    string? Text,
    string? Role,
    string? Selector,
    ReliableBoundingBox? BoundingBox,
    string? RelativeCoordinates,
    string? ElementIndex,
    string? WindowOrTabRef,
    ReliableActionResolutionMode FallbackStrategy,
    ReliableTargetResolutionConfidence Confidence);

public sealed record ReliableBoundingBox(int X, int Y, int Width, int Height);

public sealed record ReliableActionResolutionDecision(
    ReliableActionResolutionMode SelectedMode,
    ReliableRecipePolicyDecision PolicyDecision,
    bool RequiresHumanHandoff,
    bool EvidenceRequired,
    IReadOnlyList<string> Reasons);

public sealed record ReliableValidationBlock(
    string Id,
    string ExpectedState,
    IReadOnlyList<ReliableValidationCheck> Checks,
    bool EvidenceRequired,
    ReliableValidationFailurePolicy FailurePolicy,
    string? OptionalReviewer = null);

public sealed record ReliableValidationCheck(
    string CheckId,
    ReliableValidationCheckKind Kind,
    string ExpectedRef,
    bool Passed = false,
    bool RawSecretPresent = false);

public enum ReliableValidationCheckKind
{
    VisibleTextExists,
    VisibleTextNotExists,
    UrlChanged,
    WindowTitleChanged,
    ElementExists,
    ElementNotExists,
    FieldValueEquals,
    FileDownloaded,
    StateChanged,
    ModalOpened,
    ModalClosed,
    TimelineEventCreated,
    ManualConfirmationRequired
}

public enum ReliableValidationFailurePolicy
{
    Block,
    RequestHumanIntervention,
    RequestApproval,
    RetryIfPolicyAllows
}

public sealed record ReliableValidationBlockResult(
    IReadOnlyList<ReliableValidationCheck> Checks,
    bool Passed,
    string? FailureReason,
    IReadOnlyList<string> EvidenceRefs,
    double Confidence,
    string NextRecommendedAction);

public sealed record ReliableCompleteCriteria(IReadOnlyList<ReliableValidationCheck> Checks);

public sealed record ReliableTerminateCriteria(IReadOnlyList<ReliableValidationCheck> AbortConditions);

public sealed record ReliableRecipeStepEvidencePack(
    string StepId,
    string? BeforeStateRef,
    string? AfterStateRef,
    string ActionProposal,
    string ActionResult,
    string? ValidationReportRef,
    string? FailureReason,
    IReadOnlyList<string> EvidenceRefs,
    bool RedactionApplied,
    string? TimelineEventRef)
{
    public bool RawEvidenceInline => false;
    public bool SecretsInline => false;
}

public sealed record ReliableBrowserExecutionEvidencePack(
    string? ScreenshotBeforeRef,
    string? ScreenshotAfterRef,
    string? VisibleElementsTreeRef,
    string? DomSnapshotRef,
    string? AccessibilitySnapshotRef,
    IReadOnlyList<string> OcrRegionRefs,
    string? NetworkSummaryRedactedRef,
    IReadOnlyList<string> DownloadedFileRefs,
    string? LogsRedactedRef,
    string? ValidationReportRef)
{
    public bool UsesReferenceOnlyEvidence => true;
    public bool RealScreenshotCaptureEnabled => false;
}

public sealed record ReliableTimelineProjection(
    string RecipeRunId,
    string BlockId,
    string AttemptId,
    ReliableTimelineStatus Status,
    ReliableRecipeRiskProfile Risk,
    ReliableRecipePolicyDecision PolicyDecision,
    IReadOnlyList<string> EvidenceRefs,
    string UserVisibleSummary,
    bool TechnicalDetailsAvailable);

public enum ReliableTimelineStatus
{
    Planned,
    Previewed,
    Blocked,
    NeedsHuman,
    NeedsApproval,
    FailedValidation,
    EvidenceCapturedRef,
    CompletedFixture
}

public sealed record ReliableHumanInterventionRequest(
    ReliableHumanInterventionReason Reason,
    string WhatHappened,
    string WhatWasTried,
    string WhatUserMustDo,
    IReadOnlyList<string> Options,
    string TimeoutPolicy,
    bool CanContinueAfterUserAction,
    IReadOnlyList<string> EvidenceRefs)
{
    public bool BypassAttempted => false;
}

public enum ReliableHumanInterventionReason
{
    CredentialRequired,
    CaptchaDetected,
    TwoFactorRequired,
    OsPermissionRequired,
    AmbiguousTarget,
    VerificationFailedRepeatedly,
    HighRiskApprovalRequired,
    OutsideMissionScope,
    BlockedByPolicy,
    SensitiveDataRisk
}

public sealed record ReliableApprovalRequiredAction(
    string ActionSummary,
    ReliableRecipeRiskProfile RiskClassification,
    string PolicyReason,
    IReadOnlyList<string> EvidenceRefs,
    string Reversibility,
    IReadOnlyList<string> UserChoices)
{
    public bool MutatesProductiveState => false;
}

public sealed record ReliableRecordedInteraction(
    DateTimeOffset Timestamp,
    string ObservationRef,
    ReliableRecordedInputEventKind InputEventKind,
    ReliableTargetDescriptor TargetDescriptor,
    string? TextInputRedacted,
    string? WindowOrTabRef,
    bool UserCorrectionMarker,
    bool SensitiveInputDetected);

public enum ReliableRecordedInputEventKind
{
    Click,
    Type,
    Select,
    Navigate,
    Wait,
    DownloadObserved,
    HumanCorrection,
    Unknown
}

public sealed record ReliableRecordedObservation(
    string? VisibleStateRef,
    string? DomSnapshotRef,
    string? AccessibilitySnapshotRef,
    IReadOnlyList<string> OcrTextRefs,
    string? ScreenshotRef,
    bool RedactionApplied)
{
    public bool RawScreenshotInline => false;
}

public sealed record ReliableRecorderTrajectory(
    string Id,
    string WorkspaceScope,
    IReadOnlyList<ReliableRecordedInteraction> Interactions,
    string SensitiveDataSummary,
    IReadOnlyList<string> DetectedVariables,
    ReliableRecipeRiskProfile RiskProfile)
{
    public bool RealRecorderEnabled => false;
}

public sealed record ReliableRecipeDraftFromRecording(
    ReliableRecipeDefinition DraftRecipe,
    double Confidence,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> MissingValidations,
    bool RequiredHumanReview)
{
    public bool RunReady => false;
}

public sealed record ReliableRecipeEvalScenario(
    string Id,
    string RecipeId,
    string InitialFixtureState,
    string ExpectedOutcome,
    IReadOnlyList<ReliableRecipeEvalFailureKind> AllowedFailures,
    ReliableRecipeRiskProfile RiskConstraints);

public sealed record ReliableRecipeEvalRun(
    string ScenarioId,
    ReliableRecipeEvalMode Mode,
    int Attempts,
    ReliableRecipeEvalResult Result,
    ReliableRecipeEvalMetric Metrics,
    IReadOnlyList<ReliableRecipeEvalFailureKind> FailureTaxonomy)
{
    public bool UsesLiveBrowser => false;
    public bool UsesNetwork => false;
}

public enum ReliableRecipeEvalMode
{
    FixtureOnly,
    StaticPolicyCheck,
    DeterministicReplayRefOnly
}

public enum ReliableRecipeEvalResult
{
    Passed,
    Failed,
    BlockedByPolicy,
    NeedsHuman,
    FixtureMismatch
}

public sealed record ReliableRecipeEvalMetric(
    double SuccessRate,
    double VerificationFailureRate,
    double TargetConfidenceAverage,
    double RetryCountAverage,
    double HumanInterventionRate,
    double EvidenceCompletenessScore,
    double FlakinessScore);

public enum ReliableRecipeEvalFailureKind
{
    TargetNotFound,
    TargetAmbiguous,
    ValidationFailed,
    PolicyBlocked,
    SecretExposureBlocked,
    LoopLimitReached,
    HandoffRequired,
    FixtureMismatch
}

public sealed record ReliableRecipeEvalReport(
    string ReportId,
    IReadOnlyList<ReliableRecipeEvalRun> Runs,
    ReliableRecipeEvalMetric AggregateMetrics,
    bool Deterministic,
    bool FixtureOnly);

public sealed record ComputerUseSandboxProfile(
    ReliableSandboxIsolationMode IsolationMode,
    IReadOnlyList<ReliableComputerUseSurface> AllowedSurfaces,
    string NetworkPolicy,
    string FilesystemPolicy,
    string CredentialPolicy,
    string RollbackPolicy,
    string EvidencePolicy,
    int MaxRuntimeSeconds);

public enum ReliableSandboxIsolationMode
{
    None,
    DesignOnly,
    DryRunFixture,
    LocalProfileFuture,
    VmFuture,
    RemoteSandboxFuture
}

public sealed record ReliableSandboxReadinessReport(
    bool Ready,
    IReadOnlyList<string> MissingRequirements,
    IReadOnlyList<string> RiskReasons,
    IReadOnlyList<ReliableRecipeRunMode> AllowedRunModes,
    IReadOnlyList<string> BlockedCapabilities);

public enum ReliableComputerUseSurface
{
    Browser,
    Desktop,
    FileDialog,
    RemoteSession,
    MobileFuture
}

public sealed record ReliablePerceptionStackSnapshot(
    string SnapshotId,
    ReliablePerceptionSourceSurface SourceSurface,
    IReadOnlyList<ReliablePerceptionSignal> DomSignals,
    IReadOnlyList<ReliablePerceptionSignal> AccessibilitySignals,
    IReadOnlyList<ReliablePerceptionSignal> OcrSignals,
    IReadOnlyList<ReliablePerceptionSignal> VisualSignals,
    IReadOnlyList<ReliablePerceptionSignal> SetOfMarksSignals,
    bool RedactionApplied,
    ReliablePerceptionConfidence Confidence);

public enum ReliablePerceptionSourceSurface
{
    FixtureBrowser,
    FixtureDesktop,
    FixtureDocument,
    FutureBrowserBlocked,
    FutureDesktopBlocked
}

public sealed record ReliablePerceptionSignal(
    ReliablePerceptionSignalKind Kind,
    string RefId,
    string Summary,
    double Confidence,
    bool SensitiveDataRisk = false);

public enum ReliablePerceptionSignalKind
{
    DomElement,
    AccessibilityNode,
    OcrText,
    VisualBoundingBox,
    SetOfMarksIndex,
    ClickableRegion,
    StateClassifier
}

public sealed record ReliablePerceptionConfidence(
    double OverallScore,
    double SignalAgreement,
    IReadOnlyList<string> Contradictions,
    bool SensitiveDataRisk);

public sealed record ReliablePerceptionFallbackPolicy(
    IReadOnlyList<ReliableActionResolutionMode> OrderedFallbacks,
    bool AiSemanticFallbackGated,
    bool HumanHandoffOnHighRiskAmbiguity);

public static class ReliableRecipePreflightValidator
{
    public static ReliableRecipePreflightResult Validate(ReliableRecipeDefinition recipe, ReliableRecipeRunMode requestedMode = ReliableRecipeRunMode.DraftOnly)
    {
        var blocking = new List<ReliableRecipePreflightIssue>();
        var warnings = new List<ReliableRecipePreflightIssue>();

        if (recipe.Limits is null)
        {
            blocking.Add(Issue("missing-limits", "Reliable recipe requires limits before any non-design readiness."));
        }
        else
        {
            if (recipe.Limits.MaxSteps is null or <= 0)
                blocking.Add(Issue("missing-max-steps", "Reliable recipe requires MaxSteps."));
            if (recipe.Blocks.Any(b => b.Kind == ReliableRecipeBlockKind.Loop) && recipe.Limits.MaxLoopIterations is null)
                blocking.Add(Issue("missing-loop-limit", "Loop blocks require MaxLoopIterations."));
            if (recipe.Blocks.Any(b => b.Kind == ReliableRecipeBlockKind.Loop) && (recipe.Limits.TerminateCriteria is null || recipe.Limits.TerminateCriteria.AbortConditions.Count == 0))
                blocking.Add(Issue("missing-terminate-criteria", "Loop recipes require terminate criteria."));
            if (requestedMode != ReliableRecipeRunMode.DraftOnly && (recipe.Limits.CompleteCriteria is null || recipe.Limits.CompleteCriteria.Checks.Count == 0))
                blocking.Add(Issue("missing-complete-criteria", "Runnable recipe modes require complete criteria."));
        }

        if (requestedMode == ReliableRecipeRunMode.LimitedAutonomy)
            blocking.Add(Issue("limited-autonomy-blocked", "LimitedAutonomy is blocked by default in M1."));

        if (requestedMode == ReliableRecipeRunMode.SupervisedRun && !HasEvidenceValidationApproval(recipe))
            blocking.Add(Issue("supervised-missing-gates", "SupervisedRun requires evidence, validation, and approval or human gates."));

        if (recipe.CreatedFrom == ReliableRecipeCreatedFrom.RecorderDraft && requestedMode != ReliableRecipeRunMode.DraftOnly)
            blocking.Add(Issue("recorder-draft-not-run-ready", "Recorder drafts cannot be run-ready by default."));

        foreach (var variable in recipe.Variables)
        {
            if (variable.RawValuePresent || LooksSecretLike(variable.ValueRef))
                blocking.Add(Issue("raw-secret-variable", "Variables must be redacted or by reference only."));
        }

        foreach (var block in recipe.Blocks)
        {
            if (block.Risk == ReliableRecipeRiskProfile.None && IsSensitiveBlock(block))
                blocking.Add(Issue("sensitive-block-missing-risk", "Sensitive blocks require an explicit risk profile.", block.Id));

            if (IsSideEffectBlock(block) && block.ValidationRequirements.Count == 0)
                blocking.Add(Issue("side-effect-missing-validation", "Submit, download, external side-effect, desktop future, and connector draft blocks require validation.", block.Id));

            if (IsCriticalEvidenceBlock(block) && block.EvidenceExpectations.Count == 0)
                blocking.Add(Issue("critical-step-missing-evidence", "Critical blocks require evidence expectations.", block.Id));

            foreach (var pair in block.Config)
            {
                if (LooksSecretLike(pair.Key) || LooksSecretLike(pair.Value))
                    blocking.Add(Issue("raw-secret-config", "Block config cannot include secret-like raw values.", block.Id));
            }
        }

        var ready = blocking.Count == 0;
        return new ReliableRecipePreflightResult(
            ready,
            ready ? recipe.Readiness : ReliableRecipeReadiness.BlockedNeedsReview,
            ready ? ReliableRecipePolicyDecision.AllowDryRunOnly : ReliableRecipePolicyDecision.Reject,
            blocking,
            warnings);
    }

    private static bool HasEvidenceValidationApproval(ReliableRecipeDefinition recipe) =>
        recipe.Blocks.Any(b => b.EvidenceExpectations.Count > 0) &&
        recipe.Blocks.Any(b => b.ValidationRequirements.Count > 0) &&
        recipe.Blocks.Any(b => b.Kind == ReliableRecipeBlockKind.HumanIntervention || b.Risk.HasFlag(ReliableRecipeRiskProfile.Credentialed) || b.Risk.HasFlag(ReliableRecipeRiskProfile.Financial));

    private static bool IsSensitiveBlock(ReliableRecipeBlock block) =>
        block.Kind is ReliableRecipeBlockKind.BrowserAction or ReliableRecipeBlockKind.ConnectorDraft or ReliableRecipeBlockKind.DesktopFuture or ReliableRecipeBlockKind.SandboxFuture ||
        block.Risk.HasFlag(ReliableRecipeRiskProfile.Credentialed) ||
        block.Risk.HasFlag(ReliableRecipeRiskProfile.Financial) ||
        block.Risk.HasFlag(ReliableRecipeRiskProfile.Destructive) ||
        block.Risk.HasFlag(ReliableRecipeRiskProfile.Irreversible) ||
        block.Risk.HasFlag(ReliableRecipeRiskProfile.SensitiveData);

    private static bool IsSideEffectBlock(ReliableRecipeBlock block) =>
        block.Kind is ReliableRecipeBlockKind.BrowserAction or ReliableRecipeBlockKind.FileDownloadEvidence or ReliableRecipeBlockKind.CaptureArtifact or ReliableRecipeBlockKind.ConnectorDraft or ReliableRecipeBlockKind.DesktopFuture or ReliableRecipeBlockKind.SandboxFuture ||
        block.Risk.HasFlag(ReliableRecipeRiskProfile.ExternalSideEffect) ||
        block.Risk.HasFlag(ReliableRecipeRiskProfile.LocalWrite);

    private static bool IsCriticalEvidenceBlock(ReliableRecipeBlock block) =>
        IsSideEffectBlock(block) || block.Kind is ReliableRecipeBlockKind.HumanIntervention or ReliableRecipeBlockKind.Validate;

    private static bool LooksSecretLike(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var lowered = value.ToLowerInvariant();
        return lowered.Contains("password", StringComparison.Ordinal) ||
               lowered.Contains("token", StringComparison.Ordinal) ||
               lowered.Contains("secret", StringComparison.Ordinal) ||
               lowered.Contains("authorization", StringComparison.Ordinal) ||
               lowered.Contains("api_key", StringComparison.Ordinal) ||
               lowered.Contains("apikey", StringComparison.Ordinal);
    }

    private static ReliableRecipePreflightIssue Issue(string id, string message, string? blockId = null) =>
        new(id, ReliableRecipePolicyDecision.Reject, message, blockId);
}

public static class ReliableActionResolutionPolicyEvaluator
{
    public static ReliableActionResolutionDecision Decide(
        ReliableActionResolutionPolicy policy,
        ReliableTargetDescriptor target,
        ReliableRecipeRiskProfile risk)
    {
        if (target.Confidence.Score < policy.HumanHandoffThreshold)
            return new ReliableActionResolutionDecision(ReliableActionResolutionMode.HumanIntervention, ReliableRecipePolicyDecision.NeedsHumanIntervention, true, policy.EvidenceRequired, ["Low confidence target requires human handoff."]);

        if (target.FallbackStrategy == ReliableActionResolutionMode.AiSemanticFallback && IsSensitive(risk))
            return new ReliableActionResolutionDecision(ReliableActionResolutionMode.HumanIntervention, ReliableRecipePolicyDecision.NeedsApproval, true, true, ["AI semantic fallback is blocked for sensitive actions."]);

        if (target.RelativeCoordinates is not null && target.FallbackStrategy == ReliableActionResolutionMode.VisualBoundingBox && risk != ReliableRecipeRiskProfile.ReadOnly)
            return new ReliableActionResolutionDecision(ReliableActionResolutionMode.HumanIntervention, ReliableRecipePolicyDecision.NeedsHumanIntervention, true, true, ["Relative coordinate targeting is last-resort and blocked outside low-risk dry-run fixtures."]);

        var deterministic = policy.AllowedResolutionModes
            .Where(m => m is ReliableActionResolutionMode.KnownTarget or ReliableActionResolutionMode.StableSelector or ReliableActionResolutionMode.DomOrCdpSnapshot or ReliableActionResolutionMode.AccessibilityTree)
            .Cast<ReliableActionResolutionMode?>()
            .FirstOrDefault();
        if (deterministic is not null)
            return new ReliableActionResolutionDecision(deterministic.Value, ReliableRecipePolicyDecision.AllowDryRunOnly, false, policy.EvidenceRequired, ["Deterministic target signal selected before OCR or semantic fallback."]);

        return new ReliableActionResolutionDecision(target.FallbackStrategy, ReliableRecipePolicyDecision.AllowDryRunOnly, false, policy.EvidenceRequired, ["Fallback is fixture-only and requires validation."]);
    }

    private static bool IsSensitive(ReliableRecipeRiskProfile risk) =>
        risk.HasFlag(ReliableRecipeRiskProfile.Credentialed) ||
        risk.HasFlag(ReliableRecipeRiskProfile.Financial) ||
        risk.HasFlag(ReliableRecipeRiskProfile.Destructive) ||
        risk.HasFlag(ReliableRecipeRiskProfile.Irreversible) ||
        risk.HasFlag(ReliableRecipeRiskProfile.ExternalSideEffect) ||
        risk.HasFlag(ReliableRecipeRiskProfile.SensitiveData);
}

public static class ReliableValidationEvaluator
{
    public static ReliableValidationBlockResult Evaluate(ReliableValidationBlock block)
    {
        if (block.Checks.Any(c => c.RawSecretPresent))
            return new ReliableValidationBlockResult(block.Checks, false, "Validation contains raw secret-like data.", [], 0, "Redact validation inputs and request review.");

        var passed = block.Checks.All(c => c.Passed);
        if (block.EvidenceRequired && block.Checks.Count == 0)
            return new ReliableValidationBlockResult(block.Checks, false, "Validation evidence is required but no checks exist.", [], 0, "Add fixture evidence refs.");

        return new ReliableValidationBlockResult(
            block.Checks,
            passed,
            passed ? null : "One or more validation checks failed.",
            passed ? block.Checks.Select(c => $"evidence.{c.CheckId}").ToArray() : [],
            passed ? 1 : 0.25,
            passed ? "Continue fixture review." : "Stop and request human review.");
    }
}

public static class ReliableHumanInterventionFactory
{
    public static ReliableHumanInterventionRequest ForChallenge(ReliableHumanInterventionReason reason, IReadOnlyList<string> evidenceRefs) =>
        new(
            reason,
            reason switch
            {
                ReliableHumanInterventionReason.CaptchaDetected => "A CAPTCHA or challenge was detected.",
                ReliableHumanInterventionReason.TwoFactorRequired => "A two-factor step was detected.",
                ReliableHumanInterventionReason.CredentialRequired => "A credential field is required.",
                _ => "A policy-controlled human intervention is required."
            },
            "NODAL OS stopped at the fixture-safe handoff boundary.",
            "Complete the sensitive step manually or keep the recipe blocked.",
            ["Keep blocked", "Mark manual step completed by reference", "Request approval narrative"],
            "No automatic retry in M1.",
            false,
            evidenceRefs);
}

public static class ReliableRecorderDraftPolicy
{
    public static ReliableRecipeDraftFromRecording CreateDraft(ReliableRecorderTrajectory trajectory, ReliableRecipeDefinition draft)
    {
        var warnings = new List<string>();
        if (trajectory.Interactions.Any(i => i.SensitiveInputDetected))
            warnings.Add("Sensitive input was detected and must remain redacted/by-reference.");
        if (!draft.Blocks.Any(b => b.Kind == ReliableRecipeBlockKind.Validate))
            warnings.Add("Draft is missing validation blocks.");

        var missingValidation = draft.Blocks
            .Where(b => b.ValidationRequirements.Count == 0 && b.Kind != ReliableRecipeBlockKind.Validate)
            .Select(b => b.Id)
            .ToArray();

        return new ReliableRecipeDraftFromRecording(
            draft with { Readiness = ReliableRecipeReadiness.DesignOnly, CreatedFrom = ReliableRecipeCreatedFrom.RecorderDraft },
            trajectory.Interactions.Count == 0 ? 0 : 0.5,
            warnings,
            missingValidation,
            trajectory.Interactions.Any(i => i.SensitiveInputDetected) || missingValidation.Length > 0);
    }
}

public static class ReliableEvalHarness
{
    public static ReliableRecipeEvalRun EvaluateFixture(ReliableRecipeEvalScenario scenario, ReliableRecipePreflightResult preflight, ReliableRecipeEvalMetric metrics)
    {
        var failures = new List<ReliableRecipeEvalFailureKind>();
        if (!preflight.IsReady)
            failures.Add(ReliableRecipeEvalFailureKind.PolicyBlocked);
        if (metrics.EvidenceCompletenessScore < 0.75)
            failures.Add(ReliableRecipeEvalFailureKind.ValidationFailed);
        if (metrics.FlakinessScore > 0.25)
            failures.Add(ReliableRecipeEvalFailureKind.FixtureMismatch);

        return new ReliableRecipeEvalRun(
            scenario.Id,
            ReliableRecipeEvalMode.FixtureOnly,
            Attempts: 1,
            failures.Count == 0 ? ReliableRecipeEvalResult.Passed : ReliableRecipeEvalResult.BlockedByPolicy,
            metrics,
            failures);
    }
}

public static class ReliableSandboxReadinessEvaluator
{
    public static ReliableSandboxReadinessReport Evaluate(ComputerUseSandboxProfile profile)
    {
        var missing = new List<string>();
        var risk = new List<string>();
        var blocked = new List<string>();

        if (profile.IsolationMode is ReliableSandboxIsolationMode.None or ReliableSandboxIsolationMode.LocalProfileFuture or ReliableSandboxIsolationMode.VmFuture or ReliableSandboxIsolationMode.RemoteSandboxFuture)
        {
            risk.Add("Only DesignOnly or DryRunFixture sandbox modes are allowed in M1.");
            blocked.Add("live-sandbox");
        }

        if (profile.RollbackPolicy.Contains("none", StringComparison.OrdinalIgnoreCase))
            missing.Add("rollback-policy");
        if (profile.NetworkPolicy.Contains("unrestricted", StringComparison.OrdinalIgnoreCase))
            risk.Add("Unrestricted network is not ready for computer-use sandbox.");
        if (profile.CredentialPolicy.Contains("raw", StringComparison.OrdinalIgnoreCase))
            risk.Add("Raw credentials are rejected.");
        if (profile.AllowedSurfaces.Any(s => s is ReliableComputerUseSurface.Desktop or ReliableComputerUseSurface.RemoteSession) && profile.IsolationMode != ReliableSandboxIsolationMode.DryRunFixture)
            blocked.Add("desktop-live-surface");

        var ready = missing.Count == 0 && risk.Count == 0;
        return new ReliableSandboxReadinessReport(
            ready,
            missing,
            risk,
            ready ? [ReliableRecipeRunMode.DryRun] : [ReliableRecipeRunMode.DraftOnly],
            blocked);
    }
}

public static class ReliablePerceptionConfidenceEvaluator
{
    public static ReliablePerceptionConfidence Evaluate(ReliablePerceptionStackSnapshot snapshot, ReliableRecipeRiskProfile intendedRisk)
    {
        var allSignals = snapshot.DomSignals.Concat(snapshot.AccessibilitySignals).Concat(snapshot.OcrSignals).Concat(snapshot.VisualSignals).Concat(snapshot.SetOfMarksSignals).ToArray();
        var contradictions = new List<string>();
        if (snapshot.DomSignals.Count > 0 && snapshot.OcrSignals.Count > 0)
        {
            var domSummary = string.Join(" ", snapshot.DomSignals.Select(s => s.Summary));
            var ocrSummary = string.Join(" ", snapshot.OcrSignals.Select(s => s.Summary));
            if (!string.IsNullOrWhiteSpace(domSummary) && !string.IsNullOrWhiteSpace(ocrSummary) && !domSummary.Contains(ocrSummary, StringComparison.OrdinalIgnoreCase) && !ocrSummary.Contains(domSummary, StringComparison.OrdinalIgnoreCase))
                contradictions.Add("dom-ocr-disagreement");
        }

        var signalAgreement = contradictions.Count == 0 ? 1.0 : 0.35;
        var sensitive = allSignals.Any(s => s.SensitiveDataRisk) || intendedRisk.HasFlag(ReliableRecipeRiskProfile.SensitiveData);
        var score = allSignals.Length == 0 ? 0 : Math.Round(allSignals.Average(s => s.Confidence) * signalAgreement, 2);
        if (snapshot.OcrSignals.Count > 0 && snapshot.DomSignals.Count == 0 && intendedRisk != ReliableRecipeRiskProfile.ReadOnly)
            score = Math.Min(score, 0.45);

        return new ReliablePerceptionConfidence(score, signalAgreement, contradictions, sensitive);
    }
}

public static class ReliableRecipeFixtureFactory
{
    public static ReliableRecipeDefinition Create(string fixtureName) =>
        fixtureName switch
        {
            "safe_invoice_download_dry_run" => SafeInvoiceDownload(),
            "government_form_submit_high_risk" => GovernmentFormSubmitHighRisk(),
            "captcha_two_factor_handoff" => CaptchaTwoFactorHandoff(),
            "ocr_only_canvas_low_confidence" => OcrOnlyCanvasLowConfidence(),
            "desktop_future_sandbox_blocked" => DesktopFutureSandboxBlocked(),
            "ambiguous_button_target" => AmbiguousButtonTarget(),
            "secret_in_visible_state" => SecretInVisibleState(),
            _ => throw new ArgumentOutOfRangeException(nameof(fixtureName), fixtureName, "Unknown reliable recipe fixture.")
        };

    public static ReliableRecipeDefinition SafeInvoiceDownload() =>
        Base("safe_invoice_download_dry_run", ReliableRecipeRiskProfile.ReadOnly, [
            Block("goal", ReliableRecipeBlockKind.BrowserGoal, ReliableRecipeRiskProfile.ReadOnly, ["evidence.goal"], []),
            Block("download", ReliableRecipeBlockKind.FileDownloadEvidence, ReliableRecipeRiskProfile.LocalWrite, ["evidence.download"], ["validation.file_downloaded"]),
            Block("validate", ReliableRecipeBlockKind.Validate, ReliableRecipeRiskProfile.ReadOnly, ["evidence.validation"], ["validation.file_downloaded"])
        ]) with { Readiness = ReliableRecipeReadiness.RunnableDryRun };

    public static ReliableRecipeDefinition GovernmentFormSubmitHighRisk() =>
        Base("government_form_submit_high_risk", ReliableRecipeRiskProfile.ExternalSideEffect | ReliableRecipeRiskProfile.SensitiveData | ReliableRecipeRiskProfile.Irreversible, [
            Block("submit", ReliableRecipeBlockKind.BrowserAction, ReliableRecipeRiskProfile.ExternalSideEffect | ReliableRecipeRiskProfile.SensitiveData | ReliableRecipeRiskProfile.Irreversible, ["evidence.preview"], ["validation.manual-confirmation"]),
            Block("human", ReliableRecipeBlockKind.HumanIntervention, ReliableRecipeRiskProfile.SensitiveData, ["evidence.handoff"], ["validation.manual-confirmation"])
        ]) with { Readiness = ReliableRecipeReadiness.BlockedNeedsReview };

    public static ReliableRecipeDefinition CaptchaTwoFactorHandoff() =>
        Base("captcha_two_factor_handoff", ReliableRecipeRiskProfile.Credentialed, [
            Block("handoff", ReliableRecipeBlockKind.HumanIntervention, ReliableRecipeRiskProfile.Credentialed, ["evidence.challenge"], ["validation.handoff-created"])
        ]) with { Readiness = ReliableRecipeReadiness.BlockedNeedsReview };

    public static ReliableRecipeDefinition OcrOnlyCanvasLowConfidence() =>
        Base("ocr_only_canvas_low_confidence", ReliableRecipeRiskProfile.ReadOnly, [
            Block("extract", ReliableRecipeBlockKind.Extract, ReliableRecipeRiskProfile.ReadOnly, ["evidence.ocr"], ["validation.visible-text"])
        ]);

    public static ReliableRecipeDefinition DesktopFutureSandboxBlocked() =>
        Base("desktop_future_sandbox_blocked", ReliableRecipeRiskProfile.ExternalSideEffect, [
            Block("desktop", ReliableRecipeBlockKind.DesktopFuture, ReliableRecipeRiskProfile.ExternalSideEffect, ["evidence.desktop-intent"], ["validation.sandbox-readiness"])
        ]) with { Readiness = ReliableRecipeReadiness.BlockedNeedsReview };

    public static ReliableRecipeDefinition AmbiguousButtonTarget() =>
        Base("ambiguous_button_target", ReliableRecipeRiskProfile.ReadOnly, [
            Block("ambiguous", ReliableRecipeBlockKind.BrowserAction, ReliableRecipeRiskProfile.ReadOnly, ["evidence.ambiguous"], ["validation.human-target-confirmed"])
        ]) with { Readiness = ReliableRecipeReadiness.BlockedNeedsReview };

    public static ReliableRecipeDefinition SecretInVisibleState() =>
        Base("secret_in_visible_state", ReliableRecipeRiskProfile.SensitiveData, [
            Block("redact", ReliableRecipeBlockKind.CaptureArtifact, ReliableRecipeRiskProfile.SensitiveData, ["evidence.redacted"], ["validation.no-secret-exposure"])
        ]) with { Variables = [new ReliableRecipeVariable("apiToken", "secret.ref.api-token", SecretByReference: true)] };

    private static ReliableRecipeDefinition Base(string id, ReliableRecipeRiskProfile risk, IReadOnlyList<ReliableRecipeBlock> blocks) =>
        new(
            id,
            id.Replace('_', ' '),
            "1.0.0",
            "workspace.fixture",
            [],
            blocks,
            new ReliableRecipeRunLimits(
                MaxSteps: 12,
                MaxRetries: 1,
                MaxLoopIterations: 3,
                MaxRuntimeSeconds: 60,
                AllowedDomains: ["fixture.local"],
                AllowedActions: blocks.Select(b => b.Kind).Distinct().ToArray(),
                CompleteCriteria: new ReliableCompleteCriteria([new ReliableValidationCheck("complete", ReliableValidationCheckKind.TimelineEventCreated, "timeline.complete", Passed: true)]),
                TerminateCriteria: new ReliableTerminateCriteria([new ReliableValidationCheck("terminate", ReliableValidationCheckKind.ManualConfirmationRequired, "terminate.human", Passed: true)])),
            risk,
            ReliableRecipeReadiness.DesignOnly,
            ReliableRecipeCreatedFrom.ManualDesign);

    private static ReliableRecipeBlock Block(string id, ReliableRecipeBlockKind kind, ReliableRecipeRiskProfile risk, IReadOnlyList<string> evidence, IReadOnlyList<string> validation) =>
        new(id, kind, id.Replace('-', ' '), new Dictionary<string, string>(), [], [], risk, evidence, validation);
}
