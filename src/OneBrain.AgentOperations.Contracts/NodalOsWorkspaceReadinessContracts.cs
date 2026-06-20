namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsWorkspaceReadinessState
{
    NotReady,
    ReadyForReadOnlyPreview,
    ReadyForMockMetadata,
    ReadyForUserProvidedContextIntake,
    BlockedByPathJail,
    BlockedByMissingWorkspace,
    BlockedByRuntimeGate,
    BlockedByCloudQuarantine,
    BlockedByLegacySensitiveSubsystem,
    BlockedByRecipeRiskHardening,
    Unknown
}

public enum NodalOsProjectUnderstandingIntakeSource
{
    UserProvidedSummary,
    UserProvidedFileList,
    UserProvidedTechStack,
    ImportWizardMetadata,
    WorkspaceMetadataMock,
    SafeProjectSummary,
    FutureRealScanPlaceholder
}

public enum NodalOsProjectUnderstandingIntakeItemType
{
    ProjectSummary,
    TechStackHint,
    FolderStructureHint,
    ImportantFileHint,
    RiskHint,
    BusinessContextHint,
    ConstraintHint,
    Unknown,
    FutureRealScanPlaceholder
}

public enum NodalOsContextSensitivityLevel
{
    PublicSafe,
    UserProvidedSafe,
    WorkspaceMetadataSafe,
    EvidenceRefOnly,
    RedactedOnly,
    SensitiveBlocked,
    SecretBlocked,
    RawPayloadBlocked,
    UnknownRequiresReview
}

public enum NodalOsSafeContextUsageTarget
{
    Display,
    Export,
    FutureLlmPrompt,
    FutureAdvisor,
    FutureAssignmentEngine,
    FutureEvidenceReport
}

public sealed record NodalOsWorkspaceReadinessGateResult
{
    public required string GateId { get; init; }

    public string? WorkspaceId { get; init; }

    public required NodalOsWorkspaceReadinessState Status { get; init; }

    public IReadOnlyList<string> ReasonsRedacted { get; init; } = [];

    public IReadOnlyList<string> BlockersRedacted { get; init; } = [];

    public IReadOnlyList<string> WarningsRedacted { get; init; } = [];

    public IReadOnlyList<string> AllowedNextSafeCapabilitiesRedacted { get; init; } = [];

    public IReadOnlyList<string> DisabledCapabilitiesRedacted { get; init; } = [];

    public required string HumanSummaryRedacted { get; init; }

    public required string TechnicalSummaryRedacted { get; init; }

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public required bool ReadOnlyGate { get; init; }

    public required bool FilesystemScanAllowed { get; init; }

    public required bool LlmProviderCallsAllowed { get; init; }

    public required bool CloudSyncAllowed { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool PositiveExecutionGateImplemented { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsProjectUnderstandingIntakeItem
{
    public required string IntakeItemId { get; init; }

    public required NodalOsProjectUnderstandingIntakeItemType ItemType { get; init; }

    public required string TextRedacted { get; init; }

    public required string MetadataRedacted { get; init; }

    public required NodalOsProjectSummaryConfidence Confidence { get; init; }

    public required string FreshnessRedacted { get; init; }

    public required string ProvenanceRedacted { get; init; }

    public required bool UserProvidedOrMockSafe { get; init; }

    public required bool ValidatesRealExistence { get; init; }

    public required bool ReadsWorkspaceContent { get; init; }
}

public sealed record NodalOsProjectUnderstandingIntakeContract
{
    public required string IntakeId { get; init; }

    public required string WorkspaceId { get; init; }

    public string? MissionId { get; init; }

    public required NodalOsProjectUnderstandingIntakeSource Source { get; init; }

    public IReadOnlyList<NodalOsProjectUnderstandingIntakeItem> Items { get; init; } = [];

    public required string ContextDisclosureRedacted { get; init; }

    public required string NoContentAccessDisclosureRedacted { get; init; }

    public required string StructureNotVerifiedDisclosureRedacted { get; init; }

    public required string NoRealUnderstandingDisclosureRedacted { get; init; }

    public required NodalOsProjectSummaryConfidence DeclaredConfidence { get; init; }

    public required string DeclaredFreshnessRedacted { get; init; }

    public required string DeclaredProvenanceRedacted { get; init; }

    public IReadOnlyList<string> AllowedUsageRedacted { get; init; } = [];

    public IReadOnlyList<string> DisallowedUsageRedacted { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public IReadOnlyList<string> MissingInformationRedacted { get; init; } = [];

    public IReadOnlyList<string> QuestionsForUserRedacted { get; init; } = [];

    public IReadOnlyList<string> NextSafeStepsRedacted { get; init; } = [];

    public required bool ReadsFiles { get; init; }

    public required bool ValidatesRealStructure { get; init; }

    public required bool UsesGit { get; init; }

    public required bool CreatesVectorIndex { get; init; }

    public required bool CallsLlmProvider { get; init; }

    public required bool CreatesRealProjectUnderstanding { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool ChangesWorkspaceProductively { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsSafeContextBoundaryDecision
{
    public required string BoundaryId { get; init; }

    public required string WorkspaceId { get; init; }

    public IReadOnlyList<string> AllowedContextRefs { get; init; } = [];

    public IReadOnlyList<string> DeniedContextRefs { get; init; } = [];

    public required string RedactionStatusRedacted { get; init; }

    public required NodalOsContextSensitivityLevel SensitivityLevel { get; init; }

    public required string ProvenanceRedacted { get; init; }

    public required NodalOsSafeContextUsageTarget UsageTarget { get; init; }

    public IReadOnlyList<string> AllowedFieldsRedacted { get; init; } = [];

    public IReadOnlyList<string> DeniedFieldsRedacted { get; init; } = [];

    public IReadOnlyList<string> ReasonCodesRedacted { get; init; } = [];

    public IReadOnlyList<string> MissingApprovalRequirementsRedacted { get; init; } = [];

    public required string UserConsentPlaceholderRedacted { get; init; }

    public required string PolicySummaryRedacted { get; init; }

    public IReadOnlyList<string> GuardrailSummaryRedacted { get; init; } = [];

    public required bool SafeForDisplay { get; init; }

    public required bool SafeForExport { get; init; }

    public required bool FutureLlmPolicyRequired { get; init; }

    public required bool ByokRequiredForFutureLlm { get; init; }

    public required bool EvidenceRefOnly { get; init; }

    public required bool RawPathRedactedOrFingerprinted { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool CanBypassApproval { get; init; }

    public required bool CallsLlmProvider { get; init; }

    public required bool CallsCloud { get; init; }

    public required bool ScansWorkspace { get; init; }

    public required bool MutatesWorkspace { get; init; }

    public required bool CreatesVectorIndex { get; init; }

    public required bool CreatesPrompt { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
