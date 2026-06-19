namespace OneBrain.BrowserExecutor.Contracts;

public enum NodalOsStepKind
{
    Navigate,
    Read,
    Click,
    Type,
    Extract,
    Wait,
    AskHuman,
    Stop,
    DownloadRequest,
    UploadRequest
}

public enum NodalOsStepRiskLevel
{
    None,
    Low,
    Medium,
    High,
    Critical
}

public enum NodalOsStepCapabilityKind
{
    ReadOnly,
    Navigation,
    Interaction,
    DataEntry,
    Extraction,
    FileTransfer,
    HumanInput,
    ControlFlow
}

public sealed record NodalOsStepDefinition
{
    public required NodalOsStepKind StepKind { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required NodalOsStepRiskLevel RiskLevel { get; init; }
    public IReadOnlyList<NodalOsStepCapabilityKind> Capabilities { get; init; } = [];
    public required bool IsReadOnlyCapable { get; init; }
    public required bool RequiresApprovalByDefault { get; init; }
    public required bool IsSensitiveByDefault { get; init; }
    public required bool IsAllowedInV1 { get; init; }
    public string? BlockedReason { get; init; }
    public IReadOnlyList<NexaFailureKind> PossibleFailureKinds { get; init; } = [];
    public IReadOnlyList<string> EvidenceRequirements { get; init; } = [];
}

public sealed record NodalOsStepValidationContext
{
    public required NodalOsStepKind StepKind { get; init; }
    public bool TargetsSensitiveField { get; init; }
    public bool RequiresFileUpload { get; init; }
    public bool RequiresFileDownload { get; init; }
    public bool IsSubmitLike { get; init; }
    public bool IsLoginRelated { get; init; }
    public bool IsCaptchaOrTwoFactorRelated { get; init; }
    public bool GlobalSensitiveActionsBlocked { get; init; }
    public bool HumanApprovalAvailable { get; init; }
}

public sealed record NodalOsStepValidationResult
{
    public required bool IsValid { get; init; }
    public required bool IsAllowed { get; init; }
    public required bool RequiresApproval { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public IReadOnlyList<NexaFailureKind> FailureKinds { get; init; } = [];
}
