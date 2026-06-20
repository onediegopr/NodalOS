namespace OneBrain.AgentOperations.Contracts;

public sealed record NodalOsDryRunUiPreviewSection
{
    public required string SectionId { get; init; }

    public required string TitleRedacted { get; init; }

    public required string SummaryRedacted { get; init; }

    public IReadOnlyList<string> RefIds { get; init; } = [];
}

public sealed record NodalOsProjectUnderstandingDryRunUiPreview
{
    public required string PreviewId { get; init; }

    public required string WorkspaceRef { get; init; }

    public required string MissionRef { get; init; }

    public required string DryRunContractRef { get; init; }

    public required string PathJailPreconditionsRef { get; init; }

    public required string ConsentScopePreviewRef { get; init; }

    public required string SecretDetectionPolicyPreviewRef { get; init; }

    public required string ExclusionPolicyPackRef { get; init; }

    public required string RealScanAuditGateRef { get; init; }

    public required bool IsStaticPreview { get; init; }

    public required bool IsReadOnly { get; init; }

    public required bool IsNoOp { get; init; }

    public required bool UsesRealFilesystem { get; init; }

    public required bool PerformsDirectoryListing { get; init; }

    public required bool PerformsFileRead { get; init; }

    public required bool PerformsFileHash { get; init; }

    public required bool PerformsIndexing { get; init; }

    public required bool PerformsVectorization { get; init; }

    public required bool BuildsLlmContext { get; init; }

    public required bool CallsProvider { get; init; }

    public required bool UsesCloud { get; init; }

    public IReadOnlyList<NodalOsDryRunUiPreviewSection> Sections { get; init; } = [];

    public IReadOnlyList<string> RequiredDisclosuresRedacted { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];
}
