namespace OneBrain.Core.Recipes;

public enum RecipeHumanInterventionReason
{
    SensitiveAction,
    AuthRequired,
    ChallengeDetected,
    RiskGateBlocked,
    ValidationFailed,
    LocatorAmbiguous,
    PerceptionAmbiguous,
    PolicyBlocked,
    UnknownUnsafe,
    ManualCheckpoint,
    OperatorChoiceRequired
}

public enum RecipeHumanInterventionKind
{
    LoginRequired,
    CredentialRequired,
    TwoFactorRequired,
    CaptchaOrChallengeDetected,
    PaymentConfirmationRequired,
    FiscalOrLegalSubmissionReview,
    EmailOrMessageSendReview,
    PublicPostingReview,
    MarketplaceListingChangeReview,
    PriceOrStockChangeReview,
    DataDeletionReview,
    DataMutationReview,
    FileWriteReview,
    ExternalSystemMutationReview,
    PersonalDataReview,
    SecretHandlingReview,
    LocatorAmbiguity,
    PerceptionAmbiguity,
    ValidationFailureReview,
    PolicyBlockedReview,
    UnknownUnsafeState,
    ManualCheckpoint,
    OperatorChoiceRequired
}

public enum RecipeHumanInterventionStatus
{
    Requested,
    AwaitingOperator,
    ApprovedForDryRunOnly,
    ApprovedForFixtureOnly,
    ManuallyResolved,
    Rejected,
    Cancelled,
    Expired,
    BlockedByPolicy,
    BlockedLiveRuntimeDisabled,
    BlockedByProtectedScope
}

public enum RecipeHumanInterventionTrigger
{
    RecipeBlock,
    RiskGate,
    ValidationFailure,
    AuthChallenge,
    EvidenceMissing,
    ActionResolution,
    Policy,
    OperatorCheckpoint,
    UnknownUnsafe
}

public enum RecipeSafeNextActionKind
{
    KeepBlocked,
    RequestMoreEvidence,
    RequestApproval,
    RequestManualResolution,
    ContinuePreviewOnly,
    ContinueDryRunOnly,
    ContinueFixtureOnly,
    CancelRun,
    Escalate,
    AbortUnsafe
}

public enum RecipeManualResolutionOutcome
{
    ResolvedByOperator,
    RejectedByOperator,
    MoreEvidenceRequired,
    Escalated,
    Cancelled,
    KeptBlocked
}

public enum RecipeApprovalDecisionOption
{
    ApprovePreviewOnly,
    ApproveDryRunOnly,
    ApproveFixtureRunOnly,
    ApproveManualContinuation,
    Reject,
    CancelRun,
    RequestMoreEvidence,
    MarkManuallyResolved,
    Escalate,
    KeepBlocked
}

public enum RecipeApprovalDecisionStatus
{
    Pending,
    ApprovedPreviewOnly,
    ApprovedDryRunOnly,
    ApprovedFixtureRunOnly,
    ApprovedManualContinuation,
    Rejected,
    Cancelled,
    MoreEvidenceRequested,
    ManuallyResolved,
    Escalated,
    KeptBlocked,
    BlockedLiveRuntimeDisabled,
    BlockedByPolicy
}

public enum RecipeApprovalConsequenceKind
{
    PreviewOnly,
    DryRunOnly,
    FixtureOnly,
    ManualContinuation,
    MoreEvidenceRequired,
    RunCancelled,
    RemainsBlocked,
    Escalated,
    NoExternalMutation,
    NoLiveRuntime
}

public enum RecipeApprovalRollbackBoundaryKind
{
    NoneRequiredPreviewOnly,
    FixtureOnlyNoExternalState,
    ManualOperatorResponsibility,
    RestorePointRefOnly,
    NotAvailable,
    Unknown
}

public sealed record RecipeHumanInterventionRef(string InterventionId);

public sealed record RecipeManualResolutionRef(string ManualResolutionId);

public sealed record RecipeApprovalNarrativeRef(string NarrativeId);

public sealed record RecipeApprovalDecisionRef(string DecisionId);

public sealed record RecipeSafeNextAction(
    RecipeSafeNextActionKind Kind,
    string Summary,
    bool AllowsLiveRuntime = false,
    bool AllowsExternalMutation = false)
{
    public bool ActionAuthorityGranted => false;
}

public sealed record RecipeManualResolution(
    string ManualResolutionId,
    RecipeManualResolutionOutcome Outcome,
    string OperatorNoteRef,
    string RedactedSummary,
    IReadOnlyList<string> EvidenceRefs,
    DateTimeOffset? ResolvedAt)
{
    public bool ContainsRawSecretValue => false;
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeHumanInterventionRequest(
    string InterventionId,
    string RecipeId,
    string RecipeVersion,
    string RunId,
    string? StepId,
    string? BlockId,
    string? WorkitemId,
    RecipeHumanInterventionReason Reason,
    RecipeHumanInterventionKind Kind,
    RecipeHumanInterventionStatus Status,
    RecipeHumanInterventionTrigger SourceTrigger,
    string RequiredOperatorActionSummary,
    string BlockedActionSummary,
    string? RiskProfileRef,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ValidationRefs,
    IReadOnlyList<string> PolicyDecisionRefs,
    IReadOnlyList<string> TimelineRefs,
    string? RedactionSummaryRef,
    string? ApprovalNarrativeRef,
    RecipeSafeNextAction SafeNextAction,
    IReadOnlyList<RecipeManualResolutionOutcome> AllowedManualOutcomes,
    IReadOnlyList<string> DisallowedOutcomes,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? ResolvedAt,
    string? OperatorNoteRef)
{
    public bool LiveRuntimeEnabled => false;
    public bool ActionAuthorityGranted => false;
}

public sealed record RecipeApprovalRiskExplanation(
    RecipeRiskLevel RiskLevel,
    IReadOnlySet<SensitiveActionCategory> SensitiveCategories,
    string WhyHumanReviewIsRequired,
    IReadOnlyList<string> BlockedActions);

public sealed record RecipeApprovalEvidenceSummary(
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ValidationRefs,
    IReadOnlyList<string> PolicyDecisionRefs,
    string RedactedSummary,
    bool MissingRequiredEvidence = false,
    bool RawDataOmitted = true);

public sealed record RecipeApprovalConsequence(
    RecipeApprovalConsequenceKind Kind,
    string Summary,
    bool AllowsExternalMutation = false,
    bool AllowsLiveRuntime = false);

public sealed record RecipeApprovalRollbackBoundary(
    RecipeApprovalRollbackBoundaryKind Kind,
    string Summary,
    string? RestoreBoundaryRef = null,
    bool Verified = false);

public sealed record RecipeApprovalLimitSummary(
    string? LimitsRef,
    int? MaxSteps,
    int? MaxRetries,
    int? MaxLoopIterations,
    bool LiveRuntimeAllowed);

public sealed record RecipeApprovalNarrative(
    string NarrativeId,
    string RecipeId,
    string RecipeVersion,
    string RunId,
    string RequestedAction,
    string TargetSystem,
    string? TargetEntitySummary,
    string ReasonForApproval,
    RecipeApprovalRiskExplanation RiskExplanation,
    RecipeApprovalEvidenceSummary EvidenceSummary,
    string ValidationSummary,
    string RedactionSummary,
    RecipeApprovalLimitSummary LimitsSummary,
    string CompleteCriteriaSummary,
    string TerminateCriteriaSummary,
    string DeterministicActionResolutionSummary,
    IReadOnlyList<RecipeApprovalConsequence> IfApprovedConsequences,
    IReadOnlyList<RecipeApprovalConsequence> IfRejectedConsequences,
    IReadOnlyList<string> RemainsBlocked,
    RecipeSafeNextAction SafeNextAction,
    RecipeApprovalRollbackBoundary RollbackBoundary,
    IReadOnlyList<string> RawDataOmitted,
    string NoSecretGuaranteeSummary,
    string OperatorVisibleExplanation,
    IReadOnlyList<RecipeApprovalDecisionOption> DecisionOptions)
{
    public bool LiveRuntimeEnabled => false;
    public bool ActionAuthorityGranted => false;
    public bool HasRequiredNarrativeParts =>
        !string.IsNullOrWhiteSpace(RequestedAction) &&
        !string.IsNullOrWhiteSpace(TargetSystem) &&
        !string.IsNullOrWhiteSpace(ReasonForApproval) &&
        EvidenceSummary.EvidenceRefs.Count > 0 &&
        !string.IsNullOrWhiteSpace(ValidationSummary) &&
        !string.IsNullOrWhiteSpace(RedactionSummary) &&
        !string.IsNullOrWhiteSpace(CompleteCriteriaSummary) &&
        !string.IsNullOrWhiteSpace(TerminateCriteriaSummary) &&
        !string.IsNullOrWhiteSpace(DeterministicActionResolutionSummary) &&
        IfApprovedConsequences.Count > 0 &&
        IfRejectedConsequences.Count > 0 &&
        !string.IsNullOrWhiteSpace(SafeNextAction.Summary);
}

public sealed record RecipeApprovalDecision(
    string DecisionId,
    string NarrativeId,
    RecipeApprovalDecisionOption Option,
    RecipeApprovalDecisionStatus Status,
    string DecidedByRef,
    string ReasonSummary,
    DateTimeOffset? DecidedAt,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> TimelineRefs,
    bool SafeForTimeline = true,
    bool SafeForHandoff = true)
{
    public bool LiveRuntimeEnabled => false;
    public bool ActionAuthorityGranted => false;
    public bool AllowsExternalMutation => false;
}

public sealed record RecipeApprovalReadinessResult(
    bool IsReady,
    RecipeReadinessStatus Status,
    IReadOnlyList<RecipeReadinessIssue> BlockingIssues,
    IReadOnlyList<RecipeReadinessIssue> Warnings,
    bool LiveRuntimeEnabled,
    bool ActionAuthorityGranted);

public sealed record RecipeHumanBlockingScenario(
    RecipeHumanInterventionKind Kind,
    RecipeHumanInterventionStatus DefaultStatus,
    IReadOnlyList<RecipeApprovalDecisionOption> DefaultDecisionOptions,
    IReadOnlyList<RecipeRunMode> AllowedRunModes,
    IReadOnlyList<RecipeEvidenceSourceKind> EvidenceRequirements,
    RecipeTimelineEventKind TimelineEventKind,
    RecipeSafeNextAction SafeNextAction,
    IReadOnlyList<string> BlockedActions,
    bool ApprovalCanResolveInThisPhase);

public sealed record RecipeApprovalHandoffSummary(
    string RecipeId,
    string RecipeVersion,
    string RunId,
    string ApprovalNarrativeRef,
    string OperatorVisibleSummary,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> RawDataOmitted,
    RecipeSafeNextAction SafeNextAction,
    RecipeEvidenceRedactionSummary RedactionSummary)
{
    public bool IncludesRawPayloads => false;
    public bool IncludesSecretValues => false;
    public bool LiveRuntimeEnabled => false;
}

public static class RecipeHumanBlockingScenarioCatalog
{
    public static IReadOnlyList<RecipeHumanBlockingScenario> All { get; } =
    [
        Scenario(RecipeHumanInterventionKind.LoginRequired, "login is required", [RecipeApprovalDecisionOption.ApproveManualContinuation, RecipeApprovalDecisionOption.KeepBlocked]),
        Scenario(RecipeHumanInterventionKind.CredentialRequired, "credential use requires operator action", [RecipeApprovalDecisionOption.ApproveManualContinuation, RecipeApprovalDecisionOption.KeepBlocked]),
        Scenario(RecipeHumanInterventionKind.TwoFactorRequired, "2FA requires operator action", [RecipeApprovalDecisionOption.MarkManuallyResolved, RecipeApprovalDecisionOption.KeepBlocked], canResolve: false),
        Scenario(RecipeHumanInterventionKind.CaptchaOrChallengeDetected, "challenge requires operator action", [RecipeApprovalDecisionOption.MarkManuallyResolved, RecipeApprovalDecisionOption.KeepBlocked], canResolve: false),
        Scenario(RecipeHumanInterventionKind.PaymentConfirmationRequired, "payment confirmation requires review", ReviewOptions()),
        Scenario(RecipeHumanInterventionKind.FiscalOrLegalSubmissionReview, "fiscal/legal submission requires review", ReviewOptions()),
        Scenario(RecipeHumanInterventionKind.EmailOrMessageSendReview, "message send requires review", ReviewOptions()),
        Scenario(RecipeHumanInterventionKind.PublicPostingReview, "public posting requires review", ReviewOptions()),
        Scenario(RecipeHumanInterventionKind.MarketplaceListingChangeReview, "marketplace listing change requires review", ReviewOptions()),
        Scenario(RecipeHumanInterventionKind.PriceOrStockChangeReview, "price or stock change requires review", ReviewOptions()),
        Scenario(RecipeHumanInterventionKind.DataDeletionReview, "data deletion requires review", ReviewOptions()),
        Scenario(RecipeHumanInterventionKind.DataMutationReview, "data mutation requires review", ReviewOptions()),
        Scenario(RecipeHumanInterventionKind.FileWriteReview, "file write requires review", ReviewOptions()),
        Scenario(RecipeHumanInterventionKind.ExternalSystemMutationReview, "external mutation requires review", ReviewOptions()),
        Scenario(RecipeHumanInterventionKind.PersonalDataReview, "personal data handling requires review", ReviewOptions()),
        Scenario(RecipeHumanInterventionKind.SecretHandlingReview, "secret handling requires review", [RecipeApprovalDecisionOption.ApproveManualContinuation, RecipeApprovalDecisionOption.KeepBlocked]),
        Scenario(RecipeHumanInterventionKind.LocatorAmbiguity, "locator ambiguity requires review", [RecipeApprovalDecisionOption.RequestMoreEvidence, RecipeApprovalDecisionOption.KeepBlocked]),
        Scenario(RecipeHumanInterventionKind.PerceptionAmbiguity, "perception ambiguity requires review", [RecipeApprovalDecisionOption.RequestMoreEvidence, RecipeApprovalDecisionOption.KeepBlocked]),
        Scenario(RecipeHumanInterventionKind.ValidationFailureReview, "validation failure requires review", [RecipeApprovalDecisionOption.RequestMoreEvidence, RecipeApprovalDecisionOption.KeepBlocked]),
        Scenario(RecipeHumanInterventionKind.PolicyBlockedReview, "policy block requires review", [RecipeApprovalDecisionOption.KeepBlocked, RecipeApprovalDecisionOption.Escalate], canResolve: false),
        Scenario(RecipeHumanInterventionKind.UnknownUnsafeState, "unknown unsafe state requires block", [RecipeApprovalDecisionOption.KeepBlocked, RecipeApprovalDecisionOption.Escalate], status: RecipeHumanInterventionStatus.BlockedByPolicy, canResolve: false),
        Scenario(RecipeHumanInterventionKind.ManualCheckpoint, "manual checkpoint", [RecipeApprovalDecisionOption.ApproveDryRunOnly, RecipeApprovalDecisionOption.ApproveFixtureRunOnly, RecipeApprovalDecisionOption.KeepBlocked]),
        Scenario(RecipeHumanInterventionKind.OperatorChoiceRequired, "operator choice required", [RecipeApprovalDecisionOption.ApprovePreviewOnly, RecipeApprovalDecisionOption.KeepBlocked])
    ];

    public static RecipeHumanBlockingScenario For(RecipeHumanInterventionKind kind) =>
        All.First(s => s.Kind == kind);

    public static RecipeHumanInterventionKind FromSensitiveCategory(SensitiveActionCategory category) =>
        category switch
        {
            SensitiveActionCategory.Login => RecipeHumanInterventionKind.LoginRequired,
            SensitiveActionCategory.CredentialUse => RecipeHumanInterventionKind.CredentialRequired,
            SensitiveActionCategory.TwoFactor => RecipeHumanInterventionKind.TwoFactorRequired,
            SensitiveActionCategory.CaptchaOrChallenge => RecipeHumanInterventionKind.CaptchaOrChallengeDetected,
            SensitiveActionCategory.Payment => RecipeHumanInterventionKind.PaymentConfirmationRequired,
            SensitiveActionCategory.FiscalOrLegalSubmission => RecipeHumanInterventionKind.FiscalOrLegalSubmissionReview,
            SensitiveActionCategory.EmailOrMessageSend => RecipeHumanInterventionKind.EmailOrMessageSendReview,
            SensitiveActionCategory.PublicPosting => RecipeHumanInterventionKind.PublicPostingReview,
            SensitiveActionCategory.MarketplaceListingChange => RecipeHumanInterventionKind.MarketplaceListingChangeReview,
            SensitiveActionCategory.PriceOrStockChange => RecipeHumanInterventionKind.PriceOrStockChangeReview,
            SensitiveActionCategory.DataDeletion => RecipeHumanInterventionKind.DataDeletionReview,
            SensitiveActionCategory.DataMutation => RecipeHumanInterventionKind.DataMutationReview,
            SensitiveActionCategory.FileWrite => RecipeHumanInterventionKind.FileWriteReview,
            SensitiveActionCategory.ExternalSystemMutation => RecipeHumanInterventionKind.ExternalSystemMutationReview,
            SensitiveActionCategory.PersonalDataHandling => RecipeHumanInterventionKind.PersonalDataReview,
            SensitiveActionCategory.SecretHandling => RecipeHumanInterventionKind.SecretHandlingReview,
            SensitiveActionCategory.UnknownSensitiveAction => RecipeHumanInterventionKind.UnknownUnsafeState,
            _ => RecipeHumanInterventionKind.PolicyBlockedReview
        };

    private static RecipeHumanBlockingScenario Scenario(
        RecipeHumanInterventionKind kind,
        string summary,
        IReadOnlyList<RecipeApprovalDecisionOption> options,
        RecipeHumanInterventionStatus status = RecipeHumanInterventionStatus.AwaitingOperator,
        bool canResolve = true) =>
        new(
            kind,
            status,
            options,
            [RecipeRunMode.CatalogPreview, RecipeRunMode.DryRun, RecipeRunMode.FixtureRun],
            [RecipeEvidenceSourceKind.ValidationResultRef, RecipeEvidenceSourceKind.PolicyDecisionRef, RecipeEvidenceSourceKind.HumanNoteRef],
            RecipeTimelineEventKind.HumanInterventionRequested,
            new RecipeSafeNextAction(canResolve ? RecipeSafeNextActionKind.RequestApproval : RecipeSafeNextActionKind.KeepBlocked, summary),
            ["live browser runtime", "live desktop runtime", "external mutation execution", "payment/fiscal/message/delete/publication execution"],
            canResolve);

    private static IReadOnlyList<RecipeApprovalDecisionOption> ReviewOptions() =>
        [RecipeApprovalDecisionOption.ApprovePreviewOnly, RecipeApprovalDecisionOption.ApproveDryRunOnly, RecipeApprovalDecisionOption.ApproveFixtureRunOnly, RecipeApprovalDecisionOption.ApproveManualContinuation, RecipeApprovalDecisionOption.Reject, RecipeApprovalDecisionOption.RequestMoreEvidence, RecipeApprovalDecisionOption.KeepBlocked];
}

public static class RecipeHumanInterventionFactory
{
    public static RecipeHumanInterventionRequest Create(
        string interventionId,
        string recipeId,
        string recipeVersion,
        string runId,
        RecipeHumanInterventionKind kind,
        string? blockId = null,
        string? stepId = null,
        string? workitemId = null,
        string? narrativeRef = null)
    {
        var scenario = RecipeHumanBlockingScenarioCatalog.For(kind);
        return new RecipeHumanInterventionRequest(
            interventionId,
            recipeId,
            recipeVersion,
            runId,
            stepId,
            blockId,
            workitemId,
            ReasonFor(kind),
            kind,
            scenario.DefaultStatus,
            TriggerFor(kind),
            scenario.SafeNextAction.Summary,
            string.Join(", ", scenario.BlockedActions),
            RiskProfileRef: "risk.profile.ref",
            EvidenceRefs: ["evidence.ref"],
            ValidationRefs: ["validation.ref"],
            PolicyDecisionRefs: ["policy.ref"],
            TimelineRefs: ["timeline.ref"],
            RedactionSummaryRef: "redaction.ref",
            ApprovalNarrativeRef: narrativeRef,
            scenario.SafeNextAction,
            AllowedManualOutcomes: [RecipeManualResolutionOutcome.ResolvedByOperator, RecipeManualResolutionOutcome.KeptBlocked],
            DisallowedOutcomes: scenario.BlockedActions,
            CreatedAt: DateTimeOffset.Parse("2026-06-27T00:00:00Z"),
            ResolvedAt: null,
            OperatorNoteRef: "operator.note.ref");
    }

    private static RecipeHumanInterventionReason ReasonFor(RecipeHumanInterventionKind kind) =>
        kind switch
        {
            RecipeHumanInterventionKind.LoginRequired or RecipeHumanInterventionKind.CredentialRequired => RecipeHumanInterventionReason.AuthRequired,
            RecipeHumanInterventionKind.TwoFactorRequired or RecipeHumanInterventionKind.CaptchaOrChallengeDetected => RecipeHumanInterventionReason.ChallengeDetected,
            RecipeHumanInterventionKind.ValidationFailureReview => RecipeHumanInterventionReason.ValidationFailed,
            RecipeHumanInterventionKind.LocatorAmbiguity => RecipeHumanInterventionReason.LocatorAmbiguous,
            RecipeHumanInterventionKind.PerceptionAmbiguity => RecipeHumanInterventionReason.PerceptionAmbiguous,
            RecipeHumanInterventionKind.PolicyBlockedReview => RecipeHumanInterventionReason.PolicyBlocked,
            RecipeHumanInterventionKind.UnknownUnsafeState => RecipeHumanInterventionReason.UnknownUnsafe,
            RecipeHumanInterventionKind.ManualCheckpoint => RecipeHumanInterventionReason.ManualCheckpoint,
            RecipeHumanInterventionKind.OperatorChoiceRequired => RecipeHumanInterventionReason.OperatorChoiceRequired,
            _ => RecipeHumanInterventionReason.SensitiveAction
        };

    private static RecipeHumanInterventionTrigger TriggerFor(RecipeHumanInterventionKind kind) =>
        kind switch
        {
            RecipeHumanInterventionKind.TwoFactorRequired or RecipeHumanInterventionKind.CaptchaOrChallengeDetected => RecipeHumanInterventionTrigger.AuthChallenge,
            RecipeHumanInterventionKind.ValidationFailureReview => RecipeHumanInterventionTrigger.ValidationFailure,
            RecipeHumanInterventionKind.LocatorAmbiguity or RecipeHumanInterventionKind.PerceptionAmbiguity => RecipeHumanInterventionTrigger.ActionResolution,
            RecipeHumanInterventionKind.UnknownUnsafeState => RecipeHumanInterventionTrigger.UnknownUnsafe,
            _ => RecipeHumanInterventionTrigger.RiskGate
        };
}

public static class RecipeApprovalNarrativeFactory
{
    public static RecipeApprovalNarrative Create(
        string narrativeId,
        string recipeId,
        string recipeVersion,
        string runId,
        RecipeHumanInterventionKind kind,
        RecipeRiskLevel riskLevel = RecipeRiskLevel.High,
        bool missingEvidence = false,
        bool includeSafeNextAction = true,
        bool includeRollbackBoundary = true)
    {
        var scenario = RecipeHumanBlockingScenarioCatalog.For(kind);
        var evidenceRefs = missingEvidence ? Array.Empty<string>() : ["evidence.ref"];
        var safeNext = includeSafeNextAction
            ? scenario.SafeNextAction
            : new RecipeSafeNextAction(RecipeSafeNextActionKind.KeepBlocked, "");

        return new RecipeApprovalNarrative(
            narrativeId,
            recipeId,
            recipeVersion,
            runId,
            RequestedAction: $"Review {kind}",
            TargetSystem: "fixture system",
            TargetEntitySummary: "redacted target entity",
            ReasonForApproval: scenario.SafeNextAction.Summary,
            new RecipeApprovalRiskExplanation(
                riskLevel,
                SensitiveCategoriesFor(kind),
                "Human review is required because this action is sensitive, blocked, ambiguous, or unsafe.",
                scenario.BlockedActions),
            new RecipeApprovalEvidenceSummary(
                evidenceRefs,
                missingEvidence ? [] : ["validation.ref"],
                ["policy.ref"],
                "redacted evidence summary",
                MissingRequiredEvidence: missingEvidence),
            ValidationSummary: missingEvidence ? "missing validation evidence" : "validation refs present",
            RedactionSummary: "redaction applied; raw data omitted",
            new RecipeApprovalLimitSummary("limits.ref", MaxSteps: 25, MaxRetries: 2, MaxLoopIterations: 5, LiveRuntimeAllowed: false),
            CompleteCriteriaSummary: "complete criteria refs present",
            TerminateCriteriaSummary: "terminate criteria refs present",
            DeterministicActionResolutionSummary: "deterministic target refs declared before fallback",
            IfApprovedConsequences: [new RecipeApprovalConsequence(RecipeApprovalConsequenceKind.FixtureOnly, "Approval allows fixture/manual continuation only.")],
            IfRejectedConsequences: [new RecipeApprovalConsequence(RecipeApprovalConsequenceKind.RemainsBlocked, "Recipe remains blocked.")],
            RemainsBlocked: scenario.BlockedActions,
            safeNext,
            includeRollbackBoundary
                ? new RecipeApprovalRollbackBoundary(RecipeApprovalRollbackBoundaryKind.FixtureOnlyNoExternalState, "Fixture-only approval has no external rollback requirement.", Verified: true)
                : new RecipeApprovalRollbackBoundary(RecipeApprovalRollbackBoundaryKind.Unknown, ""),
            RawDataOmitted: ["raw screenshots", "raw DOM", "HAR/network logs", "secret values", "raw payloads"],
            NoSecretGuaranteeSummary: "Secret values are omitted; only secret refs are allowed.",
            OperatorVisibleExplanation: "Review required; no live runtime or external mutation is enabled.",
            DecisionOptions: scenario.DefaultDecisionOptions);
    }

    private static IReadOnlySet<SensitiveActionCategory> SensitiveCategoriesFor(RecipeHumanInterventionKind kind)
    {
        var category = kind switch
        {
            RecipeHumanInterventionKind.LoginRequired => SensitiveActionCategory.Login,
            RecipeHumanInterventionKind.CredentialRequired => SensitiveActionCategory.CredentialUse,
            RecipeHumanInterventionKind.TwoFactorRequired => SensitiveActionCategory.TwoFactor,
            RecipeHumanInterventionKind.CaptchaOrChallengeDetected => SensitiveActionCategory.CaptchaOrChallenge,
            RecipeHumanInterventionKind.PaymentConfirmationRequired => SensitiveActionCategory.Payment,
            RecipeHumanInterventionKind.FiscalOrLegalSubmissionReview => SensitiveActionCategory.FiscalOrLegalSubmission,
            RecipeHumanInterventionKind.EmailOrMessageSendReview => SensitiveActionCategory.EmailOrMessageSend,
            RecipeHumanInterventionKind.PublicPostingReview => SensitiveActionCategory.PublicPosting,
            RecipeHumanInterventionKind.DataDeletionReview => SensitiveActionCategory.DataDeletion,
            RecipeHumanInterventionKind.DataMutationReview => SensitiveActionCategory.DataMutation,
            RecipeHumanInterventionKind.FileWriteReview => SensitiveActionCategory.FileWrite,
            RecipeHumanInterventionKind.ExternalSystemMutationReview => SensitiveActionCategory.ExternalSystemMutation,
            RecipeHumanInterventionKind.PersonalDataReview => SensitiveActionCategory.PersonalDataHandling,
            RecipeHumanInterventionKind.SecretHandlingReview => SensitiveActionCategory.SecretHandling,
            RecipeHumanInterventionKind.UnknownUnsafeState => SensitiveActionCategory.UnknownSensitiveAction,
            _ => SensitiveActionCategory.UnknownSensitiveAction
        };

        return new HashSet<SensitiveActionCategory> { category };
    }
}

public static class RecipeApprovalDecisionPolicy
{
    public static RecipeApprovalDecision Decide(
        string decisionId,
        RecipeApprovalNarrative narrative,
        RecipeApprovalDecisionOption option,
        string decidedByRef = "operator.ref")
    {
        if (!narrative.DecisionOptions.Contains(option))
        {
            var fallbackOption = SafeFallbackOption(narrative.DecisionOptions);
            return CreateDecision(
                decisionId,
                narrative,
                fallbackOption,
                fallbackOption == RecipeApprovalDecisionOption.KeepBlocked
                    ? RecipeApprovalDecisionStatus.KeptBlocked
                    : RecipeApprovalDecisionStatus.BlockedByPolicy,
                decidedByRef,
                $"Decision option {option} was not offered by narrative {narrative.NarrativeId}; blocked by policy.");
        }

        var status = option switch
        {
            RecipeApprovalDecisionOption.ApprovePreviewOnly => RecipeApprovalDecisionStatus.ApprovedPreviewOnly,
            RecipeApprovalDecisionOption.ApproveDryRunOnly => RecipeApprovalDecisionStatus.ApprovedDryRunOnly,
            RecipeApprovalDecisionOption.ApproveFixtureRunOnly => RecipeApprovalDecisionStatus.ApprovedFixtureRunOnly,
            RecipeApprovalDecisionOption.ApproveManualContinuation => RecipeApprovalDecisionStatus.ApprovedManualContinuation,
            RecipeApprovalDecisionOption.Reject => RecipeApprovalDecisionStatus.Rejected,
            RecipeApprovalDecisionOption.CancelRun => RecipeApprovalDecisionStatus.Cancelled,
            RecipeApprovalDecisionOption.RequestMoreEvidence => RecipeApprovalDecisionStatus.MoreEvidenceRequested,
            RecipeApprovalDecisionOption.MarkManuallyResolved => RecipeApprovalDecisionStatus.ManuallyResolved,
            RecipeApprovalDecisionOption.Escalate => RecipeApprovalDecisionStatus.Escalated,
            RecipeApprovalDecisionOption.KeepBlocked => RecipeApprovalDecisionStatus.KeptBlocked,
            _ => RecipeApprovalDecisionStatus.BlockedByPolicy
        };

        if (narrative.EvidenceSummary.MissingRequiredEvidence &&
            option is RecipeApprovalDecisionOption.ApprovePreviewOnly or RecipeApprovalDecisionOption.ApproveDryRunOnly or RecipeApprovalDecisionOption.ApproveFixtureRunOnly or RecipeApprovalDecisionOption.ApproveManualContinuation)
        {
            status = RecipeApprovalDecisionStatus.MoreEvidenceRequested;
            option = RecipeApprovalDecisionOption.RequestMoreEvidence;
        }

        return CreateDecision(
            decisionId,
            narrative,
            option,
            status,
            decidedByRef,
            status == RecipeApprovalDecisionStatus.MoreEvidenceRequested ? "More evidence required." : "Decision recorded as reference-only.");
    }

    private static RecipeApprovalDecisionOption SafeFallbackOption(IReadOnlyList<RecipeApprovalDecisionOption> offeredOptions)
    {
        if (offeredOptions.Contains(RecipeApprovalDecisionOption.KeepBlocked))
            return RecipeApprovalDecisionOption.KeepBlocked;

        if (offeredOptions.Contains(RecipeApprovalDecisionOption.Reject))
            return RecipeApprovalDecisionOption.Reject;

        if (offeredOptions.Contains(RecipeApprovalDecisionOption.RequestMoreEvidence))
            return RecipeApprovalDecisionOption.RequestMoreEvidence;

        if (offeredOptions.Contains(RecipeApprovalDecisionOption.CancelRun))
            return RecipeApprovalDecisionOption.CancelRun;

        return RecipeApprovalDecisionOption.KeepBlocked;
    }

    private static RecipeApprovalDecision CreateDecision(
        string decisionId,
        RecipeApprovalNarrative narrative,
        RecipeApprovalDecisionOption option,
        RecipeApprovalDecisionStatus status,
        string decidedByRef,
        string reasonSummary)
    {
        return new RecipeApprovalDecision(
            decisionId,
            narrative.NarrativeId,
            option,
            status,
            decidedByRef,
            reasonSummary,
            DateTimeOffset.Parse("2026-06-27T00:00:00Z"),
            narrative.EvidenceSummary.EvidenceRefs,
            ["timeline.approval"]);
    }
}

public static class RecipeApprovalReadinessEvaluator
{
    public static RecipeApprovalReadinessResult Evaluate(
        RecipeRiskProfile risk,
        RecipeApprovalNarrative? narrative,
        bool humanInterventionPathPresent,
        ActionResolutionPolicy? actionResolutionPolicy = null)
    {
        var blocking = new List<RecipeReadinessIssue>();
        var warnings = new List<RecipeReadinessIssue>();

        if (risk.OverallRisk is RecipeRiskLevel.High or RecipeRiskLevel.Critical or RecipeRiskLevel.Blocked && narrative is null)
            blocking.Add(Issue("approval-narrative-required", RecipeReadinessStatus.BlockedMissingApprovalPolicy, "High/Critical risk requires approval narrative."));

        if (risk.SensitiveCategories.Count > 0 && !humanInterventionPathPresent)
            blocking.Add(Issue("human-path-required", RecipeReadinessStatus.BlockedMissingApprovalPolicy, "Sensitive action requires human intervention path."));

        foreach (var category in risk.SensitiveCategories)
        {
            if (category is SensitiveActionCategory.TwoFactor or SensitiveActionCategory.CaptchaOrChallenge or SensitiveActionCategory.UnknownSensitiveAction)
                blocking.Add(Issue("human-or-block-required", RecipeReadinessStatus.BlockedRiskGate, $"{category} requires human intervention or blocked state."));

            if (category is SensitiveActionCategory.BrowserLiveAction or SensitiveActionCategory.DesktopLiveAction)
                blocking.Add(Issue("live-runtime-remains-blocked", RecipeReadinessStatus.BlockedLiveRuntimeDisabled, $"{category} remains blocked regardless of approval."));
        }

        if (actionResolutionPolicy?.Attempts.FirstOrDefault()?.Strategy == ActionResolutionStrategy.AIFallback &&
            risk.SensitiveCategories.Count > 0 &&
            !humanInterventionPathPresent)
        {
            blocking.Add(Issue("sensitive-ai-without-human-path", RecipeReadinessStatus.BlockedActionResolutionPolicy, "Sensitive AI fallback requires human path."));
        }

        if (risk.OverallRisk == RecipeRiskLevel.Critical && narrative is not null)
        {
            if (string.IsNullOrWhiteSpace(narrative.SafeNextAction.Summary) || narrative.SafeNextAction.Kind == RecipeSafeNextActionKind.KeepBlocked && string.IsNullOrWhiteSpace(narrative.SafeNextAction.Summary))
                blocking.Add(Issue("critical-missing-safe-next-action", RecipeReadinessStatus.BlockedMissingApprovalPolicy, "Critical narrative requires safe next action."));

            if (narrative.RollbackBoundary.Kind is RecipeApprovalRollbackBoundaryKind.Unknown or RecipeApprovalRollbackBoundaryKind.NotAvailable && string.IsNullOrWhiteSpace(narrative.RollbackBoundary.Summary))
                warnings.Add(Issue("critical-missing-rollback-boundary", RecipeReadinessStatus.BlockedMissingApprovalPolicy, "Critical narrative should describe rollback/restore boundary.", RecipeReadinessIssueSeverity.Warning));
        }

        if (blocking.Count > 0)
            return new(false, blocking[0].Status, blocking, warnings, LiveRuntimeEnabled: false, ActionAuthorityGranted: false);

        return new(true, RecipeReadinessStatus.ReadyForFixtureRun, [], warnings, LiveRuntimeEnabled: false, ActionAuthorityGranted: false);
    }

    private static RecipeReadinessIssue Issue(
        string id,
        RecipeReadinessStatus status,
        string message,
        RecipeReadinessIssueSeverity severity = RecipeReadinessIssueSeverity.Blocking) =>
        new(id, status, severity, message);
}

public static class RecipeApprovalTimelineProjector
{
    public static RecipeTimelineEvent FromHumanIntervention(RecipeHumanInterventionRequest request) =>
        new(
            $"timeline.{request.InterventionId}",
            request.RunId,
            request.RecipeId,
            RecipeTimelineEventKind.HumanInterventionRequested,
            RecipeTimelineProjectionStatus.Projected,
            request.RequiredOperatorActionSummary,
            request.EvidenceRefs,
            request.ValidationRefs,
            request.ApprovalNarrativeRef is null ? [] : [request.ApprovalNarrativeRef],
            request.PolicyDecisionRefs,
            request.RedactionSummaryRef is null ? [] : [request.RedactionSummaryRef],
            request.CreatedAt,
            RecipeTimelineEventSeverity.Blocking,
            RecipeTimelineEventSource.Policy,
            request.BlockId,
            request.StepId,
            request.WorkitemId);

    public static RecipeTimelineEvent ApprovalRequired(RecipeApprovalNarrative narrative) =>
        new(
            $"timeline.approval.required.{narrative.NarrativeId}",
            narrative.RunId,
            narrative.RecipeId,
            RecipeTimelineEventKind.ApprovalRequired,
            RecipeTimelineProjectionStatus.Projected,
            narrative.OperatorVisibleExplanation,
            narrative.EvidenceSummary.EvidenceRefs,
            narrative.EvidenceSummary.ValidationRefs,
            [narrative.NarrativeId],
            narrative.EvidenceSummary.PolicyDecisionRefs,
            RedactionRefs: [],
            Timestamp: null,
            RecipeTimelineEventSeverity.Blocking,
            RecipeTimelineEventSource.Policy);

    public static RecipeTimelineEvent FromApprovalDecision(RecipeApprovalDecision decision, string runId, string recipeId) =>
        new(
            $"timeline.approval.decision.{decision.DecisionId}",
            runId,
            recipeId,
            decision.Status switch
            {
                RecipeApprovalDecisionStatus.Rejected or RecipeApprovalDecisionStatus.KeptBlocked => RecipeTimelineEventKind.RecipeStepBlocked,
                RecipeApprovalDecisionStatus.Cancelled => RecipeTimelineEventKind.RecipeRunCancelled,
                RecipeApprovalDecisionStatus.MoreEvidenceRequested => RecipeTimelineEventKind.EvidenceMissing,
                RecipeApprovalDecisionStatus.ManuallyResolved => RecipeTimelineEventKind.HandoffCreated,
                _ => RecipeTimelineEventKind.ApprovalRecordedRef
            },
            RecipeTimelineProjectionStatus.Projected,
            decision.ReasonSummary,
            decision.EvidenceRefs,
            ValidationRefs: [],
            ApprovalRefs: [decision.DecisionId],
            RiskGateRefs: [],
            RedactionRefs: [],
            decision.DecidedAt,
            decision.Status is RecipeApprovalDecisionStatus.Rejected or RecipeApprovalDecisionStatus.KeptBlocked or RecipeApprovalDecisionStatus.MoreEvidenceRequested
                ? RecipeTimelineEventSeverity.Blocking
                : RecipeTimelineEventSeverity.Info,
            RecipeTimelineEventSource.Operator);

    public static RecipeApprovalHandoffSummary CreateHandoffSummary(
        RecipeApprovalNarrative narrative,
        RecipeApprovalDecision? decision,
        RecipeEvidenceRedactionSummary redactionSummary) =>
        new(
            narrative.RecipeId,
            narrative.RecipeVersion,
            narrative.RunId,
            narrative.NarrativeId,
            decision is null ? narrative.OperatorVisibleExplanation : $"{narrative.OperatorVisibleExplanation} Decision: {decision.Status}.",
            narrative.EvidenceSummary.EvidenceRefs,
            narrative.RawDataOmitted,
            narrative.SafeNextAction,
            redactionSummary);
}
