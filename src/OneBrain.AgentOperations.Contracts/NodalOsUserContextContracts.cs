namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsUserContextCaptureType
{
    UserSummary,
    UserTechStack,
    UserFolderStructureHint,
    UserImportantFileHint,
    UserConstraint,
    UserRiskNote,
    UserBusinessContext,
    UserArchitectureNote,
    UserTodo,
    UserUnknown
}

public enum NodalOsContextReviewCardStatus
{
    Draft,
    SafeForDisplay,
    SafeForExport,
    RequiresReview,
    BlockedSensitive,
    BlockedSecret,
    BlockedRawPayload,
    DiscardedMock
}

public enum NodalOsContextReviewOptionKind
{
    AcceptForDisplay,
    MarkNeedsClarification,
    EditDraft,
    DiscardDraft,
    RequestExplanation,
    LinkEvidenceRef,
    OpenGuardrails
}

public enum NodalOsContextEvidenceLinkType
{
    SupportsContext,
    UserClaimReference,
    ClarificationNeeded,
    ContradictionSuspected,
    RelatedTimelineEvent,
    RelatedEvidenceRef,
    FutureVerificationNeeded
}

public enum NodalOsContextEvidenceLinkStatus
{
    DraftLink,
    LinkedRefOnly,
    RequiresReview,
    BlockedUnsafeEvidence,
    RemovedMock
}

public sealed record NodalOsUserProvidedContextCapture
{
    public required string ContextCaptureId { get; init; }

    public required string WorkspaceId { get; init; }

    public string? MissionId { get; init; }

    public required string SubmittedByActorRedacted { get; init; }

    public required NodalOsUserContextCaptureType CaptureType { get; init; }

    public required string ContentRedacted { get; init; }

    public required string MetadataRedacted { get; init; }

    public required string DeclaredProvenanceRedacted { get; init; }

    public required NodalOsProjectSummaryConfidence DeclaredConfidence { get; init; }

    public required string DeclaredFreshnessRedacted { get; init; }

    public required NodalOsContextSensitivityLevel SensitivityLevel { get; init; }

    public required NodalOsSafeContextBoundaryDecision BoundaryDecision { get; init; }

    public IReadOnlyList<string> AllowedUsageRedacted { get; init; } = [];

    public IReadOnlyList<string> DisallowedUsageRedacted { get; init; } = [];

    public IReadOnlyList<string> MissingInformationRedacted { get; init; } = [];

    public IReadOnlyList<string> StaticQuestionsRedacted { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public required NodalOsCoreRuntimeValidationResult ValidationResult { get; init; }

    public required bool UserProvidedOnly { get; init; }

    public required bool FilesystemVerificationAllowed { get; init; }

    public required bool ReadsFiles { get; init; }

    public required bool UsesGit { get; init; }

    public required bool CreatesVectorIndex { get; init; }

    public required bool CallsLlmProvider { get; init; }

    public required bool CreatesPrompt { get; init; }

    public required bool CreatesRealProjectUnderstanding { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool ChangesWorkspaceProductively { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsContextReviewCard
{
    public required string ReviewCardId { get; init; }

    public required string ContextCaptureId { get; init; }

    public required string WorkspaceId { get; init; }

    public string? MissionId { get; init; }

    public required string CardTitleRedacted { get; init; }

    public required string SummaryRedacted { get; init; }

    public required string DetailsRedacted { get; init; }

    public required string ProvenanceLabelRedacted { get; init; }

    public required string ConfidenceLabelRedacted { get; init; }

    public required string FreshnessLabelRedacted { get; init; }

    public required string SensitivityLabelRedacted { get; init; }

    public required NodalOsContextReviewCardStatus Status { get; init; }

    public IReadOnlyList<string> AllowedUsageChipsRedacted { get; init; } = [];

    public IReadOnlyList<string> DisallowedUsageChipsRedacted { get; init; } = [];

    public IReadOnlyList<string> MissingInformationRedacted { get; init; } = [];

    public IReadOnlyList<string> QuestionsForUserRedacted { get; init; } = [];

    public IReadOnlyList<string> WarningsRedacted { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<NodalOsContextReviewOptionKind> UserOptions { get; init; } = [];

    public required bool UserOptionsAreNoOp { get; init; }

    public required bool UserProvidedAndNotVerified { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool CallsLlmProvider { get; init; }

    public required bool CreatesPrompt { get; init; }

    public required bool MutatesRuntime { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsContextEvidenceLink
{
    public required string LinkId { get; init; }

    public required string ContextCaptureId { get; init; }

    public string? ReviewCardId { get; init; }

    public required string EvidenceRefId { get; init; }

    public string? TimelineRefId { get; init; }

    public required string WorkspaceId { get; init; }

    public string? MissionId { get; init; }

    public required NodalOsContextEvidenceLinkType LinkType { get; init; }

    public required NodalOsContextEvidenceLinkStatus LinkStatus { get; init; }

    public required string LinkReasonRedacted { get; init; }

    public required string ProvenanceRedacted { get; init; }

    public required NodalOsCoreRuntimeValidationResult ValidationResult { get; init; }

    public required bool RefOnly { get; init; }

    public required bool IncludesRawPayload { get; init; }

    public required bool IncludesScreenshotInline { get; init; }

    public required bool IncludesDomRaw { get; init; }

    public required bool IncludesNetworkRaw { get; init; }

    public required bool ReadsFiles { get; init; }

    public required bool ValidatesRealContent { get; init; }

    public required bool ConvertsClaimToAuthoritativeTruth { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool CallsLlmProvider { get; init; }

    public required bool CallsCloud { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
