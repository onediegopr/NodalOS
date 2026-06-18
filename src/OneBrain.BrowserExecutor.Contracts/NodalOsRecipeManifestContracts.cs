namespace OneBrain.BrowserExecutor.Contracts;

public sealed record NodalOsRecipeManifest
{
    public required string RecipeId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Version { get; init; }
    public required NodalOsRecipeStatus Status { get; init; }
    public required string GoalTemplate { get; init; }
    public IReadOnlyList<string> AllowedDomains { get; init; } = [];
    public required IReadOnlyList<NodalOsRecipeStepManifest> Steps { get; init; }
    public required NodalOsRecipePolicyManifest Policy { get; init; }
    public IReadOnlyList<string> SuccessCriteria { get; init; } = [];
    public IReadOnlyList<string> FailureSignals { get; init; } = [];
    public IReadOnlyList<string> EvidenceRequirements { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public enum NodalOsRecipeStatus
{
    Draft,
    Shadow,
    Supervised,
    Approved,
    Deprecated,
    Blocked
}

public sealed record NodalOsRecipeStepManifest
{
    public required string StepId { get; init; }
    public required int Index { get; init; }
    public required string Label { get; init; }
    public required NodalOsRecipeActionKind ActionKind { get; init; }
    public string? UrlTemplate { get; init; }
    public IReadOnlyList<string> SelectorHints { get; init; } = [];
    public IReadOnlyList<string> FallbackHints { get; init; } = [];
    public bool RequiresApproval { get; init; }
    public string? ExpectedOutcome { get; init; }
    public bool TargetsSensitiveField { get; init; }
}

public enum NodalOsRecipeActionKind
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

public sealed record NodalOsRecipePolicyManifest
{
    public bool RequiresHumanApprovalByDefault { get; init; }
    public bool SensitiveActionsBlocked { get; init; }
    public IReadOnlyList<NodalOsRecipeActionKind> AllowedActionKinds { get; init; } = [];
    public IReadOnlyList<NodalOsRecipeActionKind> DisallowedActionKinds { get; init; } = [];
    public int? MaxRuntimeSteps { get; init; }
}

public sealed record NodalOsRecipeManifestValidationResult
{
    public required bool IsValid { get; init; }
    public required bool CanExecute { get; init; }
    public required bool RequiresApproval { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
