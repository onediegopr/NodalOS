namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsWorkerStatus
{
    Draft,
    Registered,
    Disabled,
    Deprecated,
    Blocked
}

public enum NodalOsWorkerHealthStatus
{
    Unknown,
    Healthy,
    Degraded,
    Unhealthy,
    Offline
}

public enum NodalOsWorkerCapabilityKind
{
    ReadOnly,
    Navigation,
    Extraction,
    Reporting,
    EvidenceProcessing,
    HumanInput,
    FileTransfer,
    DataEntry,
    Interaction,
    ControlFlow,
    Unknown
}

public enum NodalOsWorkerBoundaryKind
{
    InProcessAdapter,
    ExternalProcess,
    BrowserAdapter,
    DesktopAdapter,
    OcrAdapter,
    ManualHumanAdapter,
    Unknown
}

public sealed record NodalOsWorkerBoundaryManifest
{
    public required string WorkerId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Version { get; init; }
    public required NodalOsWorkerStatus Status { get; init; }
    public required NodalOsWorkerBoundaryKind BoundaryKind { get; init; }
    public IReadOnlyList<NodalOsWorkerCapabilityKind> Capabilities { get; init; } = [];
    public IReadOnlyList<string> SupportedPackageIds { get; init; } = [];
    public IReadOnlyList<string> SupportedSkillIds { get; init; } = [];
    public required string Provenance { get; init; }
    public required bool InternalOnly { get; init; }
    public required bool RuntimeExecutionAllowed { get; init; }
    public required bool RuntimeExecutionDeferred { get; init; }
    public required bool RequiresGlobalPolicyEvaluation { get; init; }
    public required bool CanAuthorizeActions { get; init; }
    public IReadOnlyList<string> EvidenceRequirements { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record NodalOsWorkerHealthReport
{
    public required string WorkerId { get; init; }
    public required NodalOsWorkerHealthStatus HealthStatus { get; init; }
    public string? Summary { get; init; }
    public IReadOnlyList<NodalOsWorkerCapabilityKind> ReportedCapabilities { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsWorkerBoundaryValidationResult
{
    public required bool IsValid { get; init; }
    public required bool CanPassBoundaryPolicy { get; init; }
    public required bool RuntimeExecutionAllowed { get; init; }
    public required bool RuntimeExecutionDeferred { get; init; }
    public required bool RequiresGlobalPolicyEvaluation { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed record NodalOsWorkerRequestEnvelope
{
    public required string RequestId { get; init; }
    public required string WorkerId { get; init; }
    public string? PackageId { get; init; }
    public string? SkillId { get; init; }
    public required bool ExecutionDeferred { get; init; }
    public required bool RequiresGlobalPolicyEvaluation { get; init; }
    public IReadOnlyList<string> EvidenceRequirements { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsWorkerResponseEnvelope
{
    public required string ResponseId { get; init; }
    public required string RequestId { get; init; }
    public required string WorkerId { get; init; }
    public required bool Executed { get; init; }
    public required bool RuntimeExecutionDeferred { get; init; }
    public string? Summary { get; init; }
    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];
    public IReadOnlyList<NexaFailureKind> FailureKinds { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
}
