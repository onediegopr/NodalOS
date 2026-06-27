namespace OneBrain.Core.Recipes;

public enum RecipeActionCategory
{
    ReadOnlyObservation,
    Extract,
    Validate,
    Wait,
    Submit,
    Download,
    Write,
    WorkitemMutation,
    ExternalSystemCall,
    BrowserDraft,
    DesktopDraft
}

public sealed record RecipeRunLimits(
    int? MaxSteps = null,
    int? MaxRuntimeSeconds = null,
    int? MaxRetries = null,
    int? MaxLoopIterations = null,
    int? MaxNestedLoops = null,
    int? MaxWorkitemsPerRun = null,
    int? MaxDownloadedFiles = null,
    int? MaxCapturedArtifacts = null,
    int? MaxExternalSystemCalls = null,
    IReadOnlyList<string>? AllowedDomains = null,
    IReadOnlyList<string>? AllowedApps = null,
    IReadOnlyList<string>? AllowedFileScopeRefs = null,
    IReadOnlySet<RecipeActionCategory>? AllowedActionCategories = null,
    IReadOnlySet<RecipeActionCategory>? BlockedActionCategories = null,
    bool RequireExplicitApprovalForSensitiveActions = true,
    bool RequireValidationAfterSideEffects = true,
    bool RequireEvidenceAfterDownloadsCaptures = true,
    bool LiveRuntimeAllowed = false,
    bool FutureLiveEligibility = false);

public enum RecipeCompleteCriterionType
{
    ExpectedOutputExists,
    ValidationResultPassed,
    FileArtifactRefExists,
    VisibleStateMatchedRef,
    WorkitemMarkedSucceeded,
    NoUnresolvedHumanIntervention,
    NoBlockingPolicyViolation,
    AllRequiredEvidenceRefsPresent,
    CustomNamedCriterion
}

public sealed record RecipeCompleteCriterion(
    string CriterionId,
    RecipeCompleteCriterionType CriterionType,
    string RefId,
    bool UsesRealWorldProbe = false);

public sealed record RecipeCompleteCriteria(IReadOnlyList<RecipeCompleteCriterion> Criteria);

public enum RecipeTerminateCriterionType
{
    MaxStepsExceeded,
    MaxRuntimeExceeded,
    MaxRetriesExceeded,
    ValidationFailedNonRetryable,
    PolicyBlocked,
    AuthChallengeDetected,
    HumanInterventionRequired,
    LocatorConfidenceBelowThreshold,
    ExternalSystemUnavailable,
    RepeatedSameStateDetected,
    RateLimitDetected,
    UnknownUnsafeState,
    LoopLimitExceeded
}

public sealed record RecipeTerminateCriterion(
    string CriterionId,
    RecipeTerminateCriterionType CriterionType,
    string RefId,
    bool UsesRealTimerOrHook = false);

public sealed record RecipeTerminateCriteria(IReadOnlyList<RecipeTerminateCriterion> Criteria);

public enum RecipeValidationKind
{
    VisibleTextExists,
    VisibleTextNotExists,
    FieldValueEquals,
    UrlDomainMatchedRef,
    DownloadedFileRefExists,
    ArtifactRefExists,
    WorkitemStatusEquals,
    OutputSchemaMatched,
    ApprovalDecisionExists,
    EvidenceRefExists,
    TimelineEventRefExists,
    PolicyDecisionAllowed,
    NoSecretExposure,
    NoUnresolvedHumanIntervention,
    NoBlockingRisk
}

public enum RecipeValidationSeverity
{
    Info,
    Warning,
    Blocking
}

public sealed record RecipeValidationRequirement(
    string RequirementId,
    RecipeValidationKind ValidationKind,
    RecipeValidationSeverity Severity,
    string? AppliesToBlockId = null,
    RecipeBlockType? AppliesToBlockType = null,
    bool PostValidation = false,
    string? RefId = null);

public sealed record RecipeValidationPolicy(IReadOnlyList<RecipeValidationRequirement> Requirements);

public sealed record RecipeValidationResult(
    bool Passed,
    IReadOnlyList<RecipeReadinessIssue> Issues);

public enum RecipeReadinessIssueSeverity
{
    Info,
    Warning,
    Blocking
}

public sealed record RecipeReadinessIssue(
    string IssueId,
    RecipeReadinessStatus Status,
    RecipeReadinessIssueSeverity Severity,
    string Message,
    string? BlockId = null);

public enum SensitiveActionCategory
{
    Login,
    CredentialUse,
    TwoFactor,
    CaptchaOrChallenge,
    Payment,
    FiscalOrLegalSubmission,
    EmailOrMessageSend,
    PublicPosting,
    DataDeletion,
    DataMutation,
    FileWrite,
    ExternalSystemMutation,
    MarketplaceListingChange,
    PriceOrStockChange,
    PersonalDataHandling,
    SecretHandling,
    BrowserLiveAction,
    DesktopLiveAction,
    UnknownSensitiveAction
}

public enum RecipePolicyGateRequirement
{
    None,
    Approval,
    HumanIntervention,
    Blocked,
    EvidenceRequired,
    SecretReferenceOnly
}

public sealed record RecipeRiskGate(
    SensitiveActionCategory Category,
    RecipeRiskLevel RiskLevel,
    RecipePolicyGateRequirement Requirement,
    bool Satisfied,
    string Reason);

public sealed record RecipeRiskProfile(
    string RiskProfileId,
    RecipeRiskLevel OverallRisk,
    IReadOnlySet<SensitiveActionCategory> SensitiveCategories,
    IReadOnlyList<RecipeRiskGate> Gates,
    bool ApprovalPolicyPresent,
    bool HumanInterventionPathPresent,
    IReadOnlyList<string> SecretRefs,
    bool SecretValuesExposed = false);

public enum ActionResolutionStrategy
{
    KnownTarget,
    StableSelector,
    DomOrAccessibility,
    VisibleText,
    SemanticTarget,
    VisualAnchor,
    RelativeCoordinate,
    AIFallback,
    HumanHandoff,
    Abort
}

public sealed record ActionResolutionAttempt(
    int Order,
    ActionResolutionStrategy Strategy,
    string? RefId = null,
    string? EvidenceExpectationRef = null);

public sealed record ActionResolutionPolicy(
    IReadOnlyList<ActionResolutionAttempt> Attempts,
    bool AiFallbackAllowed = false,
    bool SensitiveActionsAllowAiFallback = false);

public sealed record ActionResolutionDecision(
    bool DeterministicStrategyDeclared,
    bool AiFallbackAllowed,
    bool RelativeCoordinateUsed,
    bool RequiresHuman,
    bool BlocksReadiness,
    IReadOnlyList<string> Reasons);

public sealed record RecipePolicyPreflightResult(
    bool IsReady,
    RecipeReadinessStatus Status,
    IReadOnlyList<RecipeReadinessIssue> BlockingIssues,
    IReadOnlyList<RecipeReadinessIssue> Warnings,
    bool LiveRuntimeEnabled,
    bool ActionAuthorityGranted);

public static class RecipePolicyPreflightEvaluator
{
    private static readonly RecipeBlockType[] ActionLikeBlockTypes =
    [
        RecipeBlockType.BrowserAction,
        RecipeBlockType.DesktopActionDraft,
        RecipeBlockType.ConnectorDraft,
        RecipeBlockType.WorkitemUpdate,
        RecipeBlockType.WorkitemCreateNextStage,
        RecipeBlockType.FileDownloadEvidence,
        RecipeBlockType.CaptureArtifact
    ];

    private static readonly SensitiveActionCategory[] ApprovalRequiredCategories =
    [
        SensitiveActionCategory.Payment,
        SensitiveActionCategory.FiscalOrLegalSubmission,
        SensitiveActionCategory.EmailOrMessageSend,
        SensitiveActionCategory.PublicPosting
    ];

    private static readonly SensitiveActionCategory[] HumanOrApprovalRequiredCategories =
    [
        SensitiveActionCategory.Login,
        SensitiveActionCategory.CredentialUse,
        SensitiveActionCategory.DataDeletion,
        SensitiveActionCategory.DataMutation,
        SensitiveActionCategory.FileWrite,
        SensitiveActionCategory.ExternalSystemMutation,
        SensitiveActionCategory.MarketplaceListingChange,
        SensitiveActionCategory.PriceOrStockChange,
        SensitiveActionCategory.PersonalDataHandling,
        SensitiveActionCategory.SecretHandling
    ];

    public static RecipePolicyPreflightResult Evaluate(RecipeDefinition definition, RecipeRunMode mode)
    {
        var blocking = new List<RecipeReadinessIssue>();
        var warnings = new List<RecipeReadinessIssue>();

        if (mode is RecipeRunMode.LiveRunBlocked or RecipeRunMode.LiveRunAllowedFuture || definition.RunLimits?.LiveRuntimeAllowed == true)
        {
            blocking.Add(Issue("live-runtime-disabled", RecipeReadinessStatus.BlockedLiveRuntimeDisabled, "Live runtime remains disabled for Recipe Runtime Phase 2."));
        }

        EvaluateLimits(definition, blocking);
        EvaluateCriteria(definition, blocking);
        EvaluateValidation(definition, blocking);
        EvaluateRisk(definition, blocking);
        EvaluateActionResolution(definition, blocking, warnings);

        if (blocking.Count > 0)
        {
            return new RecipePolicyPreflightResult(
                IsReady: false,
                blocking[0].Status,
                blocking,
                warnings,
                LiveRuntimeEnabled: false,
                ActionAuthorityGranted: false);
        }

        var readyStatus = mode switch
        {
            RecipeRunMode.CatalogPreview => RecipeReadinessStatus.ReadyForCatalogPreview,
            RecipeRunMode.FixtureRun => RecipeReadinessStatus.ReadyForFixtureRun,
            _ => RecipeReadinessStatus.ReadyForDryRun
        };

        return new RecipePolicyPreflightResult(
            IsReady: true,
            readyStatus,
            [],
            warnings,
            LiveRuntimeEnabled: false,
            ActionAuthorityGranted: false);
    }

    private static void EvaluateLimits(RecipeDefinition definition, List<RecipeReadinessIssue> blocking)
    {
        var limits = definition.RunLimits;
        if (limits is null)
        {
            blocking.Add(Issue("missing-limits", RecipeReadinessStatus.BlockedMissingLimits, "Recipe run limits are required."));
            return;
        }

        if (definition.Blocks.Any(IsActionLike) && limits.MaxSteps is null)
            blocking.Add(Issue("missing-max-steps", RecipeReadinessStatus.BlockedMissingLimits, "Action-like recipes require MaxSteps."));

        if (definition.Blocks.Any(b => b.BlockType == RecipeBlockType.Loop) && (limits.MaxLoopIterations is null || limits.MaxNestedLoops is null))
            blocking.Add(Issue("missing-loop-limits", RecipeReadinessStatus.BlockedMissingLimits, "Loop blocks require loop iteration and nested loop limits."));
    }

    private static void EvaluateCriteria(RecipeDefinition definition, List<RecipeReadinessIssue> blocking)
    {
        var complete = definition.CompleteCriteria?.Criteria ?? [];
        var terminate = definition.TerminateCriteria?.Criteria ?? [];

        if (RequiresCompleteCriteria(definition) && complete.Count == 0)
            blocking.Add(Issue("missing-complete-criteria", RecipeReadinessStatus.BlockedMissingCompleteCriteria, "Output-producing recipes require complete criteria."));

        if (definition.Blocks.Any(b => b.BlockType == RecipeBlockType.WorkitemUpdate) &&
            !complete.Any(c => c.CriterionType == RecipeCompleteCriterionType.WorkitemMarkedSucceeded))
            blocking.Add(Issue("missing-workitem-success-criteria", RecipeReadinessStatus.BlockedMissingCompleteCriteria, "Workitem update blocks require success criteria."));

        if (definition.Blocks.Any(b => b.BlockType == RecipeBlockType.FileDownloadEvidence) &&
            !complete.Any(c => c.CriterionType is RecipeCompleteCriterionType.FileArtifactRefExists or RecipeCompleteCriterionType.AllRequiredEvidenceRefsPresent))
            blocking.Add(Issue("missing-file-complete-criteria", RecipeReadinessStatus.BlockedMissingCompleteCriteria, "File download evidence blocks require file/evidence completion criteria."));

        if (terminate.Count == 0)
            blocking.Add(Issue("missing-terminate-criteria", RecipeReadinessStatus.BlockedMissingTerminateCriteria, "Every recipe requires terminate criteria."));

        if (definition.Blocks.Any(b => b.BlockType == RecipeBlockType.Loop) &&
            !terminate.Any(c => c.CriterionType == RecipeTerminateCriterionType.LoopLimitExceeded))
            blocking.Add(Issue("missing-loop-terminate-criteria", RecipeReadinessStatus.BlockedMissingTerminateCriteria, "Loop blocks require loop termination criteria."));

        if (IsSensitiveOrExternal(definition) &&
            !terminate.Any(c => c.CriterionType is RecipeTerminateCriterionType.PolicyBlocked or RecipeTerminateCriterionType.AuthChallengeDetected or RecipeTerminateCriterionType.HumanInterventionRequired))
            blocking.Add(Issue("missing-sensitive-terminate-criteria", RecipeReadinessStatus.BlockedMissingTerminateCriteria, "Sensitive/external-system recipes require policy/auth/challenge terminate criteria."));
    }

    private static void EvaluateValidation(RecipeDefinition definition, List<RecipeReadinessIssue> blocking)
    {
        var requirements = definition.ValidationPolicy?.Requirements ?? [];

        foreach (var block in definition.Blocks)
        {
            if (block.BlockType == RecipeBlockType.BrowserAction && IsSubmitLike(block.Intent) && !HasPostValidation(requirements, block.BlockId))
                blocking.Add(Issue("missing-submit-post-validation", RecipeReadinessStatus.BlockedMissingValidation, "Submit-like BrowserAction blocks require post-validation.", block.BlockId));

            if (block.BlockType == RecipeBlockType.FileDownloadEvidence && !HasValidation(requirements, block.BlockId, RecipeValidationKind.DownloadedFileRefExists, RecipeValidationKind.EvidenceRefExists))
                blocking.Add(Issue("missing-download-validation", RecipeReadinessStatus.BlockedMissingValidation, "FileDownloadEvidence blocks require evidence/file-ref validation.", block.BlockId));

            if (block.BlockType == RecipeBlockType.WorkitemUpdate && !HasValidation(requirements, block.BlockId, RecipeValidationKind.WorkitemStatusEquals))
                blocking.Add(Issue("missing-workitem-validation", RecipeReadinessStatus.BlockedMissingValidation, "WorkitemUpdate blocks require workitem status validation.", block.BlockId));

            if (block.BlockType == RecipeBlockType.HumanIntervention && !HasValidation(requirements, block.BlockId, RecipeValidationKind.ApprovalDecisionExists, RecipeValidationKind.NoUnresolvedHumanIntervention))
                blocking.Add(Issue("missing-human-resolution-validation", RecipeReadinessStatus.BlockedMissingValidation, "HumanIntervention blocks require approval/manual-resolution validation.", block.BlockId));

            if (block.BlockType == RecipeBlockType.ConnectorDraft)
            {
                if (definition.RequiredToolTrustRefs.Count == 0)
                    blocking.Add(Issue("missing-tool-trust", RecipeReadinessStatus.BlockedMissingToolTrust, "ConnectorDraft blocks require tool trust refs.", block.BlockId));

                if (definition.RequiredSecretRefs.Count == 0)
                    blocking.Add(Issue("missing-secret-ref", RecipeReadinessStatus.BlockedMissingSecretReference, "ConnectorDraft blocks require secret refs by id only.", block.BlockId));
            }

            if (block.BlockType == RecipeBlockType.DesktopActionDraft)
                blocking.Add(Issue("desktop-draft-blocked", RecipeReadinessStatus.BlockedLiveRuntimeDisabled, "DesktopActionDraft remains blocked until future desktop runtime authorization.", block.BlockId));
        }
    }

    private static void EvaluateRisk(RecipeDefinition definition, List<RecipeReadinessIssue> blocking)
    {
        var risk = definition.RuntimeRiskProfile;
        if (risk is null)
            return;

        if (risk.SecretValuesExposed)
            blocking.Add(Issue("secret-value-exposed", RecipeReadinessStatus.BlockedRiskGate, "Secret handling must remain reference-only."));

        if (risk.OverallRisk is RecipeRiskLevel.High or RecipeRiskLevel.Critical or RecipeRiskLevel.Blocked)
        {
            if (!risk.ApprovalPolicyPresent && !risk.HumanInterventionPathPresent)
                blocking.Add(Issue("high-critical-missing-approval", RecipeReadinessStatus.BlockedMissingApprovalPolicy, "High/Critical recipes require approval or human intervention."));
        }

        foreach (var category in risk.SensitiveCategories)
        {
            if (ApprovalRequiredCategories.Contains(category) && !risk.ApprovalPolicyPresent)
                blocking.Add(Issue("category-requires-approval", RecipeReadinessStatus.BlockedMissingApprovalPolicy, $"{category} requires approval."));

            if (HumanOrApprovalRequiredCategories.Contains(category) && !risk.ApprovalPolicyPresent && !risk.HumanInterventionPathPresent)
                blocking.Add(Issue("sensitive-requires-human-or-approval", RecipeReadinessStatus.BlockedMissingApprovalPolicy, $"{category} requires approval or human intervention path."));

            if (category is SensitiveActionCategory.CaptchaOrChallenge or SensitiveActionCategory.TwoFactor)
                blocking.Add(Issue("challenge-human-required", RecipeReadinessStatus.BlockedRiskGate, "Captcha/2FA/challenge categories must block to human intervention, never auto-bypass."));

            if (category is SensitiveActionCategory.BrowserLiveAction or SensitiveActionCategory.DesktopLiveAction)
                blocking.Add(Issue("live-action-blocked", RecipeReadinessStatus.BlockedLiveRuntimeDisabled, $"{category} remains blocked in Phase 2."));

            if (category == SensitiveActionCategory.SecretHandling && (risk.SecretRefs.Count == 0 || risk.SecretValuesExposed))
                blocking.Add(Issue("secret-handling-ref-only", RecipeReadinessStatus.BlockedMissingSecretReference, "SecretHandling requires secret refs and no secret values."));

            if (category == SensitiveActionCategory.UnknownSensitiveAction)
                blocking.Add(Issue("unknown-sensitive-blocked", RecipeReadinessStatus.BlockedRiskGate, "Unknown sensitive action defaults to blocked/human required."));
        }

        foreach (var gate in risk.Gates.Where(g => !g.Satisfied || g.Requirement == RecipePolicyGateRequirement.Blocked))
            blocking.Add(Issue("risk-gate-unsatisfied", RecipeReadinessStatus.BlockedRiskGate, gate.Reason));
    }

    private static void EvaluateActionResolution(RecipeDefinition definition, List<RecipeReadinessIssue> blocking, List<RecipeReadinessIssue> warnings)
    {
        if (!definition.Blocks.Any(IsActionLike))
            return;

        var policy = definition.ActionResolutionPolicy;
        if (policy is null || policy.Attempts.Count == 0)
        {
            blocking.Add(Issue("missing-action-resolution-policy", RecipeReadinessStatus.BlockedActionResolutionPolicy, "Action-like blocks require deterministic-first action resolution policy."));
            return;
        }

        var ordered = policy.Attempts.OrderBy(a => a.Order).ToArray();
        var first = ordered[0].Strategy;
        var firstAi = first == ActionResolutionStrategy.AIFallback;
        var hasDeterministic = ordered.Any(a => IsDeterministic(a.Strategy));
        var ai = ordered.FirstOrDefault(a => a.Strategy == ActionResolutionStrategy.AIFallback);

        if (firstAi || (ai is not null && ordered.Any(a => a.Order > ai.Order && IsDeterministic(a.Strategy))))
            blocking.Add(Issue("ai-before-deterministic", RecipeReadinessStatus.BlockedActionResolutionPolicy, "Deterministic strategies must be declared before AI fallback."));

        if (ai is not null && (!policy.AiFallbackAllowed || string.IsNullOrWhiteSpace(ai.EvidenceExpectationRef)))
            blocking.Add(Issue("ai-fallback-missing-policy-evidence", RecipeReadinessStatus.BlockedActionResolutionPolicy, "AI fallback requires explicit policy allowance and evidence expectation."));

        if (IsSensitiveOrExternal(definition) && firstAi)
            blocking.Add(Issue("sensitive-ai-first", RecipeReadinessStatus.BlockedActionResolutionPolicy, "Sensitive actions cannot use AI fallback as first strategy."));

        if (IsSensitiveOrExternal(definition) && !hasDeterministic)
            blocking.Add(Issue("sensitive-missing-deterministic", RecipeReadinessStatus.BlockedActionResolutionPolicy, "Sensitive actions require a deterministic strategy or human handoff."));

        if (ordered.Any(a => a.Strategy == ActionResolutionStrategy.RelativeCoordinate))
            warnings.Add(Issue("relative-coordinate-warning", RecipeReadinessStatus.BlockedActionResolutionPolicy, "RelativeCoordinate is last-resort and lowers readiness confidence.", severity: RecipeReadinessIssueSeverity.Warning));
    }

    private static bool RequiresCompleteCriteria(RecipeDefinition definition) =>
        !string.IsNullOrWhiteSpace(definition.OutputSchemaRef) ||
        definition.Blocks.Any(b => b.BlockType is RecipeBlockType.Extract or RecipeBlockType.CaptureArtifact or RecipeBlockType.FileDownloadEvidence or RecipeBlockType.WorkitemUpdate);

    private static bool IsSensitiveOrExternal(RecipeDefinition definition) =>
        definition.Blocks.Any(b => b.BlockType == RecipeBlockType.ConnectorDraft) ||
        definition.RunLimits?.MaxExternalSystemCalls > 0 ||
        definition.RuntimeRiskProfile?.SensitiveCategories.Count > 0;

    private static bool IsActionLike(RecipeBlock block) => ActionLikeBlockTypes.Contains(block.BlockType);

    private static bool IsSubmitLike(string intent) =>
        new[] { "submit", "send", "post", "payment", "pay", "download", "write", "delete", "update" }
            .Any(term => intent.Contains(term, StringComparison.OrdinalIgnoreCase));

    private static bool HasPostValidation(IReadOnlyList<RecipeValidationRequirement> requirements, string blockId) =>
        requirements.Any(r => AppliesTo(r, blockId) && r.PostValidation && r.Severity == RecipeValidationSeverity.Blocking);

    private static bool HasValidation(IReadOnlyList<RecipeValidationRequirement> requirements, string blockId, params RecipeValidationKind[] kinds) =>
        requirements.Any(r => AppliesTo(r, blockId) && kinds.Contains(r.ValidationKind) && r.Severity == RecipeValidationSeverity.Blocking);

    private static bool AppliesTo(RecipeValidationRequirement requirement, string blockId) =>
        string.Equals(requirement.AppliesToBlockId, blockId, StringComparison.OrdinalIgnoreCase) ||
        requirement.AppliesToBlockId is null;

    private static bool IsDeterministic(ActionResolutionStrategy strategy) =>
        strategy is ActionResolutionStrategy.KnownTarget or
            ActionResolutionStrategy.StableSelector or
            ActionResolutionStrategy.DomOrAccessibility or
            ActionResolutionStrategy.VisibleText or
            ActionResolutionStrategy.SemanticTarget or
            ActionResolutionStrategy.VisualAnchor;

    private static RecipeReadinessIssue Issue(
        string id,
        RecipeReadinessStatus status,
        string message,
        string? blockId = null,
        RecipeReadinessIssueSeverity severity = RecipeReadinessIssueSeverity.Blocking) =>
        new(id, status, severity, message, blockId);
}
