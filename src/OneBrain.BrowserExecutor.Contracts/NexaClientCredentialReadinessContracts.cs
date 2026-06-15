namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaClientCredentialReadinessStatus
{
    BlockedForRealClientCredentials,
    ReadyForSyntheticOnly,
    ReadyForControlledInternalCredentialPilot,
    RequiresExternalSecurityAudit
}

public sealed record NexaClientCredentialReadinessCheck(
    string CheckId,
    bool Passed,
    string Reason,
    bool Redacted);

public sealed record NexaClientCredentialBlocker(
    string BlockerId,
    string Description,
    string RequiredAction,
    bool Redacted);

public sealed record NexaClientCredentialRiskRegister(
    IReadOnlyList<NexaClientCredentialBlocker> Blockers,
    bool Redacted);

public sealed record NexaClientCredentialRecommendation(
    NexaClientCredentialReadinessStatus Status,
    string NextAction,
    bool RealClientCredentialsAllowed,
    bool Redacted);

public sealed record NexaClientCredentialReadinessReport(
    NexaClientCredentialReadinessStatus Status,
    IReadOnlyList<NexaClientCredentialReadinessCheck> Checks,
    NexaClientCredentialRiskRegister RiskRegister,
    NexaClientCredentialRecommendation Recommendation,
    bool Redacted)
{
    public bool BlocksRealCredentials =>
        !Recommendation.RealClientCredentialsAllowed &&
        Status is NexaClientCredentialReadinessStatus.BlockedForRealClientCredentials or NexaClientCredentialReadinessStatus.ReadyForSyntheticOnly or NexaClientCredentialReadinessStatus.RequiresExternalSecurityAudit;
}

public sealed record NexaClientCredentialReadinessInput(
    bool AuditKeyCustodyOk,
    bool VaultOsBackedOk,
    bool VaultThreatTestsPassed,
    bool VaultRotationRecoveryExportPolicyOk,
    bool LeakHardeningOk,
    bool DiagnosticsSupportRedacted,
    bool TenantGovernanceOk,
    bool LicenseGatingOk,
    bool CoreOnlyBoundaryOk,
    bool CompanionNonAuthoritative,
    bool ProfileRawBlocked,
    bool PublicApiNotExposed,
    bool M51ExternalProofValidated,
    bool ExternalSecurityAuditCompleted,
    bool RealCustomerCredentialPolicyApproved,
    bool RealCredentialIncidentSupportProcessApproved);
