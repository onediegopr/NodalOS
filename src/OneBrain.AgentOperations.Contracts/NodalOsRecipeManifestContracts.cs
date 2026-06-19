namespace OneBrain.AgentOperations.Contracts;

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
    /// <summary>
    /// Manifest status approved for governance review only. It does not grant runtime execution
    /// and still requires global policy evaluation plus any required human approval.
    /// </summary>
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

    /// <summary>
    /// True when the manifest passes local manifest-policy validation. This is not runtime permission.
    /// </summary>
    public bool CanPassManifestPolicy { get; init; }

    /// <summary>
    /// Compatibility alias for manifest-policy pass. Does not grant runtime execution.
    /// </summary>
    public required bool CanExecute { get; init; }

    /// <summary>
    /// Always false in Recipe Manifest V1. Runtime execution is not implemented by this contract.
    /// </summary>
    public bool RuntimeExecutionAllowed { get; init; }

    /// <summary>
    /// Always true in this phase; recipe execution remains deferred to a future runtime gate.
    /// </summary>
    public bool RuntimeExecutionDeferred { get; init; } = true;

    /// <summary>
    /// Always true; global policy remains authoritative over manifest-local policy.
    /// </summary>
    public bool RequiresGlobalPolicyEvaluation { get; init; } = true;

    public required bool RequiresApproval { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
