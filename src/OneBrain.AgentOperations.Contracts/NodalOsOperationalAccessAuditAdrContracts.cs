namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsOperationalAccessAuditDecisionStatus
{
    OperationalFilesystemAccessNotReadyAuditRequired,
    UnknownRequiresReview
}

public sealed record NodalOsOperationalAccessAuditAdrSummary
{
    public required string AdrId { get; init; }
    public required string DecisionRecordPath { get; init; }
    public required NodalOsOperationalAccessAuditDecisionStatus DecisionStatus { get; init; }
    public required bool OperationalFilesystemAccessReady { get; init; }
    public required bool DisabledPathJailGateIsPreconditionOnly { get; init; }
    public required bool RequiresExplicitFutureMilestone { get; init; }
    public required bool RequiresDisabledByDefaultGate { get; init; }
    public required bool RequiresUserConsentEnforcement { get; init; }
    public required bool RequiresPathJailImplementationAudit { get; init; }
    public required bool RequiresCanonicalizationImplementationAudit { get; init; }
    public required bool RequiresNoMutationRuntimeProof { get; init; }
    public required bool RequiresCancellationSemantics { get; init; }
    public required bool RequiresLocalOnlyGuarantee { get; init; }
    public required bool RequiresRedactionAndSensitiveDataEnforcement { get; init; }
    public required bool RequiresExclusionPolicyEnforcement { get; init; }
    public required bool RequiresEvidenceTimelineEmission { get; init; }
    public required bool RequiresKillSwitchRollbackDisableStrategy { get; init; }
    public required bool RequiresFullSuiteAndAdversarialTests { get; init; }
    public required bool CanonicalizationMustBeExplicitlyAudited { get; init; }
    public required bool FolderEnumerationRequiresSeparateGate { get; init; }
    public required bool ContentAccessRequiresSeparateGate { get; init; }
    public required bool ContentFingerprintingRequiresSeparateGate { get; init; }
    public required bool IndexingRepresentationAndLlmContextBlocked { get; init; }
    public required bool CloudProviderRuntimeBlocked { get; init; }
    public required bool SyntheticRegressionNecessaryNotSufficient { get; init; }
    public required bool FuturePrototypeMustFailClosed { get; init; }
}

