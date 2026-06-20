namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsWorkspaceMetadataIndexStatus
{
    Empty,
    Draft,
    MockIndexed,
    RequiresRealScanLater,
    Blocked,
    Archived
}

public enum NodalOsWorkspaceMetadataSourceType
{
    Fixture,
    UserProvidedMetadata,
    ImportWizardPreview,
    Mock
}

public enum NodalOsProjectSummaryConfidence
{
    Mock,
    UserProvided,
    Low,
    Unknown
}

public enum NodalOsWorkspaceHealthStatus
{
    HealthyMock,
    NeedsWorkspaceValidation,
    NeedsPathJailValidation,
    NeedsMetadata,
    BlockedByGuardrail,
    BlockedByRuntimeGate,
    BlockedByCloudQuarantine,
    Unknown
}

public sealed record NodalOsWorkspaceMetadataIndexMock
{
    public required string IndexId { get; init; }

    public required string WorkspaceId { get; init; }

    public required NodalOsWorkspaceMetadataIndexStatus Status { get; init; }

    public IReadOnlyList<string> IndexedItemRefsMock { get; init; } = [];

    public IReadOnlyList<string> ProjectTypeHintsMock { get; init; } = [];

    public IReadOnlyList<string> ItemCategorySummariesMock { get; init; } = [];

    public IReadOnlyList<string> TechnologyHintsMock { get; init; } = [];

    public IReadOnlyList<string> DocumentationHintsMock { get; init; } = [];

    public IReadOnlyList<string> RiskHintsMock { get; init; } = [];

    public IReadOnlyList<string> KnownMissionRefs { get; init; } = [];

    public IReadOnlyList<string> KnownWorkspaceRefs { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public required NodalOsWorkspaceMetadataSourceType SourceType { get; init; }

    public IReadOnlyList<string> RedactionSummaryRedacted { get; init; } = [];

    public IReadOnlyList<string> GuardrailSummaryRedacted { get; init; } = [];

    public IReadOnlyList<string> DisabledCapabilitiesRedacted { get; init; } = [];

    public required bool MockOnly { get; init; }

    public required bool IsSourceOfTruthForExecution { get; init; }

    public required bool RealFilesystemScanAllowed { get; init; }

    public required bool DirectoryEnumerationAllowed { get; init; }

    public required bool FileContentAccessAllowed { get; init; }

    public required bool FileFingerprintingAllowed { get; init; }

    public required bool ShellCommandAllowed { get; init; }

    public required bool LlmProviderCallsAllowed { get; init; }

    public required bool CloudSyncAllowed { get; init; }

    public required bool VectorIndexAllowed { get; init; }

    public required bool ProductivePersistenceAllowed { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }
}

public sealed record NodalOsSafeProjectSummaryContract
{
    public required string ProjectSummaryId { get; init; }

    public required string WorkspaceId { get; init; }

    public IReadOnlyList<string> MissionIds { get; init; } = [];

    public required string TitleRedacted { get; init; }

    public required string ShortSummaryRedacted { get; init; }

    public required string StatusRedacted { get; init; }

    public IReadOnlyList<string> ProjectTypeHintsRedacted { get; init; } = [];

    public IReadOnlyList<string> RiskSummaryRedacted { get; init; } = [];

    public IReadOnlyList<string> ReadinessSummaryRedacted { get; init; } = [];

    public IReadOnlyList<string> MissingInformationRedacted { get; init; } = [];

    public IReadOnlyList<string> DisabledCapabilitiesRedacted { get; init; } = [];

    public IReadOnlyList<string> NextSafeStepsRedacted { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> ObservabilityRefs { get; init; } = [];

    public string? WorkspaceHealthRef { get; init; }

    public IReadOnlyList<string> RedactionSummaryRedacted { get; init; } = [];

    public required NodalOsProjectSummaryConfidence Confidence { get; init; }

    public required string BasisDisclosureRedacted { get; init; }

    public required string ProjectKnowledgeDisclosureRedacted { get; init; }

    public required string NoContentAccessDisclosureRedacted { get; init; }

    public required bool SafeToDisplay { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RealFilesystemScanAllowed { get; init; }

    public required bool LlmProviderCallsAllowed { get; init; }

    public required bool CloudSyncAllowed { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsWorkspaceHealthReport
{
    public required string HealthReportId { get; init; }

    public required string WorkspaceId { get; init; }

    public required NodalOsWorkspaceHealthStatus HealthStatus { get; init; }

    public required string PathJailStatusRedacted { get; init; }

    public required string MetadataIndexStatusRedacted { get; init; }

    public required string MissionBindingStatusRedacted { get; init; }

    public required string UiStateStatusRedacted { get; init; }

    public required string EvidenceTimelineStatusRedacted { get; init; }

    public IReadOnlyList<string> DisabledCapabilitiesRedacted { get; init; } = [];

    public IReadOnlyList<string> BlockersRedacted { get; init; } = [];

    public IReadOnlyList<string> WarningsRedacted { get; init; } = [];

    public IReadOnlyList<string> NextSafeStepsRedacted { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public IReadOnlyList<string> RedactionSummaryRedacted { get; init; } = [];

    public required bool RequiresAction { get; init; }

    public required bool RequiresHumanAttention { get; init; }

    public required bool ReadOnlyReport { get; init; }

    public required bool MutatesState { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RealFilesystemScanAllowed { get; init; }

    public required bool LlmProviderCallsAllowed { get; init; }

    public required bool CloudSyncAllowed { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
