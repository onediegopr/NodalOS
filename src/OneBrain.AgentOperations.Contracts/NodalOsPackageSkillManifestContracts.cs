namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsPackageStatus
{
    Draft,
    InternalPreview,
    ApprovedForCatalog,
    Deprecated,
    Blocked
}

public enum NodalOsSkillStatus
{
    Draft,
    InternalPreview,
    ApprovedForCatalog,
    Deprecated,
    Blocked
}

public enum NodalOsSkillCapabilityKind
{
    ReadOnly,
    Navigation,
    Extraction,
    Interaction,
    DataEntry,
    FileTransfer,
    HumanInput,
    ControlFlow,
    EvidenceProcessing,
    Reporting,
    Unknown
}

public enum NodalOsSkillRiskLevel
{
    None,
    Low,
    Medium,
    High,
    Critical
}

public sealed record NodalOsPackageManifest
{
    public required string PackageId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Version { get; init; }
    public required NodalOsPackageStatus Status { get; init; }
    public required string Publisher { get; init; }
    public required string Provenance { get; init; }
    public IReadOnlyList<NodalOsSkillManifest> Skills { get; init; } = [];
    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyList<string> EvidenceRequirements { get; init; } = [];
    public required bool InternalOnly { get; init; }
    public required bool RuntimeExecutionAllowed { get; init; }
    public required bool RuntimeExecutionDeferred { get; init; }
    public required bool RequiresGlobalPolicyEvaluation { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record NodalOsSkillManifest
{
    public required string SkillId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Version { get; init; }
    public required NodalOsSkillStatus Status { get; init; }
    public IReadOnlyList<NodalOsSkillCapabilityKind> Capabilities { get; init; } = [];
    public required NodalOsSkillRiskLevel RiskLevel { get; init; }
    public IReadOnlyList<string> AllowedDomains { get; init; } = [];
    public IReadOnlyList<string> RequiredApprovals { get; init; } = [];
    public IReadOnlyList<string> EvidenceRequirements { get; init; } = [];
    public IReadOnlyList<string> RelatedRecipeIds { get; init; } = [];
    public IReadOnlyList<string> RelatedStepKinds { get; init; } = [];
    public required bool InternalOnly { get; init; }
    public required bool RuntimeExecutionAllowed { get; init; }
    public required bool RuntimeExecutionDeferred { get; init; }
    public required bool RequiresGlobalPolicyEvaluation { get; init; }
}

public sealed record NodalOsPackageSkillManifestValidationResult
{
    public required bool IsValid { get; init; }
    public required bool CanPassCatalogPolicy { get; init; }
    public required bool RuntimeExecutionAllowed { get; init; }
    public required bool RuntimeExecutionDeferred { get; init; }
    public required bool RequiresGlobalPolicyEvaluation { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
