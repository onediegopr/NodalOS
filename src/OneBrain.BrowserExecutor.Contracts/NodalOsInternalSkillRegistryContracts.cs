namespace OneBrain.BrowserExecutor.Contracts;

public enum NodalOsRegistryEntryStatus
{
    Draft,
    Visible,
    Hidden,
    Deprecated,
    Blocked
}

public enum NodalOsRegistryEntryKind
{
    Package,
    Skill
}

public sealed record NodalOsSkillRegistryEntry
{
    public required string EntryId { get; init; }
    public required NodalOsRegistryEntryKind Kind { get; init; }
    public required string PackageId { get; init; }
    public string? SkillId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Version { get; init; }
    public required NodalOsRegistryEntryStatus Status { get; init; }
    public required string Provenance { get; init; }
    public required bool InternalOnly { get; init; }
    public required bool RuntimeExecutionAllowed { get; init; }
    public required bool RuntimeExecutionDeferred { get; init; }
    public required bool RequiresGlobalPolicyEvaluation { get; init; }
    public IReadOnlyList<NodalOsSkillCapabilityKind> Capabilities { get; init; } = [];
    public NodalOsSkillRiskLevel? RiskLevel { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyList<string> EvidenceRequirements { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record NodalOsInternalSkillRegistrySnapshot
{
    public required string RegistryId { get; init; }
    public required string Version { get; init; }
    public IReadOnlyList<NodalOsSkillRegistryEntry> Entries { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsInternalSkillRegistryValidationResult
{
    public required bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed record NodalOsInternalSkillRegistryBuildResult
{
    public required NodalOsInternalSkillRegistrySnapshot Snapshot { get; init; }
    public required NodalOsInternalSkillRegistryValidationResult Validation { get; init; }
}

public sealed record NodalOsSkillRegistryQuery
{
    public string? PackageId { get; init; }
    public string? SkillId { get; init; }
    public NodalOsRegistryEntryKind? Kind { get; init; }
    public NodalOsRegistryEntryStatus? Status { get; init; }
    public NodalOsSkillRiskLevel? MaxRiskLevel { get; init; }
    public IReadOnlyList<NodalOsSkillCapabilityKind> RequiredCapabilities { get; init; } = [];
    public bool VisibleOnly { get; init; } = true;
}
