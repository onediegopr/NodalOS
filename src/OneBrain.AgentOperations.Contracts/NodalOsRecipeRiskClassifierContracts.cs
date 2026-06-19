namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsRecipeStepRiskCategory
{
    ReadOnlyObservation,
    Extraction,
    FormFill,
    Submit,
    PurchaseOrPayment,
    DeleteOrDestructive,
    ExternalPublishOrSend,
    CredentialOrLogin,
    CaptchaOrTwoFactor,
    FileSystemMutation,
    DataExport,
    NetworkOrExternalService,
    BrowserAutomationFuture,
    HumanDecisionRequired,
    Unsupported
}

public enum NodalOsRecipeRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum NodalOsRecipeApprovalRequirement
{
    NotRequiredForObservation,
    RequiredBeforeExecution,
    RequiredBeforeExternalMutation,
    RequiredBeforeCredentialUse,
    RequiredBeforeDestructiveAction,
    RequiredBeforePublishSendPayment,
    RequiredAlwaysInV1
}

public enum NodalOsRecipeDslDecision
{
    RepresentationOnly,
    JsonCanonicalModelRequired,
    ParserDeferred,
    RuntimeDeferred,
    ImportRequiresValidation,
    DirectExecutionForbidden
}

public sealed record NodalOsRecipeStepRiskInput
{
    public required string StepId { get; init; }

    public string? RecipeId { get; init; }

    public string? MissionId { get; init; }

    public string? TaskId { get; init; }

    public required string StepKind { get; init; }

    public string? HumanReadableStepRedacted { get; init; }

    public IReadOnlyList<string> TargetKinds { get; init; } = [];

    public IReadOnlyList<string> DeclaredOperations { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];
}

public sealed record NodalOsRecipeStepRiskClassification
{
    public required string ClassificationId { get; init; }

    public required string StepId { get; init; }

    public required NodalOsRecipeRiskLevel RiskLevel { get; init; }

    public IReadOnlyList<NodalOsRecipeStepRiskCategory> Categories { get; init; } = [];

    public required NodalOsRecipeApprovalRequirement ApprovalRequirement { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool RequiresGlobalPolicyEvaluation { get; init; }

    public required bool RequiresEvidenceRedaction { get; init; }

    public required bool RequiresHumanHandoff { get; init; }

    public required bool CanAuthorizeAction { get; init; }

    public IReadOnlyList<string> Reasons { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsRecipeRiskProfile
{
    public required string ProfileId { get; init; }

    public required string RecipeId { get; init; }

    public required NodalOsRecipeRiskLevel OverallRiskLevel { get; init; }

    public IReadOnlyList<NodalOsRecipeStepRiskClassification> StepClassifications { get; init; } = [];

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool RequiresGlobalPolicyEvaluation { get; init; }

    public required bool RequiresEvidenceRedaction { get; init; }

    public required bool RequiresHumanApproval { get; init; }

    public required bool RequiresHumanHandoff { get; init; }

    public required bool CanAuthorizeAction { get; init; }

    public IReadOnlyList<string> SummaryWarnings { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsRecipeDslDecisionRecord
{
    public required string DecisionId { get; init; }

    public required bool DslIsRuntime { get; init; }

    public required bool ParserImplemented { get; init; }

    public required bool DirectExecutionAllowed { get; init; }

    public required bool ImportRequiresValidation { get; init; }

    public required bool JsonCanonicalModelRequired { get; init; }

    public IReadOnlyList<NodalOsRecipeDslDecision> Decisions { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsRecipeRiskClassifierValidationResult
{
    public required bool IsValid { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool RequiresGlobalPolicyEvaluation { get; init; }

    public required bool CanAuthorizeAction { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];
}
