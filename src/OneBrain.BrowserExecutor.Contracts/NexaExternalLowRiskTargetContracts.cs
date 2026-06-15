namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaExternalLowRiskTargetDecisionKind
{
    Allowed,
    Blocked,
    BlockedNoTestOwnedExternalTarget
}

public sealed record NexaExternalLowRiskTargetConfig(
    string TargetId,
    string? BaseUrl,
    string Host,
    bool HostAllowlisted,
    bool TestOwned,
    bool SensitiveCategory,
    bool ContainsRealCustomerData,
    bool HasPayment,
    bool HasIrreversibleActions,
    bool RequiresTwoFactorOrCaptcha,
    bool SemanticProofAvailable,
    IReadOnlyList<string> ReadOnlyPaths);

public sealed record NexaExternalLowRiskTargetOwnershipProof(
    string ProofId,
    bool TestOwned,
    string EvidenceRef,
    bool Redacted);

public sealed record NexaExternalLowRiskTargetReadiness(
    bool Configured,
    bool Reachable,
    bool SemanticProofVerified,
    bool MetadataOnly,
    bool CookiesPersisted,
    bool SensitiveHeaderValuesCaptured,
    bool BrowserCleanupConfirmed,
    bool Redacted);

public sealed record NexaExternalLowRiskTargetDecision(
    NexaExternalLowRiskTargetDecisionKind Decision,
    NexaExternalLowRiskTargetConfig? Config,
    NexaExternalLowRiskTargetReadiness Readiness,
    IReadOnlyList<string> ReasonCodes,
    bool Redacted);

public sealed record NexaExternalLowRiskTargetSetup(
    NexaExternalLowRiskTargetConfig? Config,
    NexaExternalLowRiskTargetOwnershipProof? OwnershipProof,
    NexaExternalLowRiskTargetDecision Decision);
