namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaExternalTestOwnedTargetStatus
{
    MissingTarget,
    CandidateTarget,
    OwnershipUnverified,
    PolicyRejected,
    ApprovedReadOnlyTestOwned,
    Expired,
    BlockedSensitiveSurface
}

public enum NexaExternalTargetOwnershipProofMode
{
    None,
    DnsTxt,
    HostedWellKnownFile,
    RepositoryControlledDeployment,
    OperatorAttestation
}

public enum NexaExternalTargetCredentialPolicy
{
    NoCredentials,
    SyntheticOnly,
    RealCredentialsBlocked
}

public enum NexaExternalTargetSubmitPolicy
{
    ReadOnlyNoSubmit,
    MutationsBlocked,
    SubmitBlocked
}

public enum NexaExternalTargetDataSensitivityProfile
{
    SyntheticPublic,
    LowRiskSynthetic,
    PersonalDataBlocked,
    FinancialFiscalGovernmentBlocked,
    SensitiveBlocked
}

public enum NexaExternalTargetEvidencePolicy
{
    MetadataOnlyRedacted,
    BodiesBlocked,
    SensitiveHeadersBlocked
}

public sealed record NexaExternalTestOwnedTarget(
    string TargetId,
    string? BaseUrl,
    NexaExternalTargetOwnershipProofMode OwnershipProofMode,
    IReadOnlySet<string> AllowedHosts,
    IReadOnlySet<string> DeniedHosts,
    IReadOnlySet<string> AllowedPaths,
    IReadOnlySet<string> DeniedPaths,
    IReadOnlySet<string> AllowedMethods,
    IReadOnlySet<string> DeniedMethods,
    NexaExternalTargetCredentialPolicy CredentialPolicy,
    NexaExternalTargetSubmitPolicy SubmitPolicy,
    NexaExternalTargetDataSensitivityProfile DataSensitivityProfile,
    NexaExternalTargetEvidencePolicy EvidencePolicy,
    string ComplianceNotes,
    DateTimeOffset? ValidUntilUtc,
    string? OperatorOwner,
    string? ApprovalRef,
    bool ExplicitlyTestOwned);

public sealed record NexaExternalTestOwnedTargetDecision(
    NexaExternalTestOwnedTargetStatus Status,
    NexaExternalTestOwnedTarget? Target,
    IReadOnlyList<string> ReasonCodes,
    bool AllowsReadOnlyProof,
    bool Redacted);

public enum NexaExternalProofHarnessDecisionKind
{
    SkippedNoOptIn,
    BlockedNoTarget,
    BlockedPolicyViolation,
    AllowedReadOnlyProof
}

public sealed record NexaExternalProofHarnessRequest(
    bool OptInEnabled,
    NexaExternalTestOwnedTarget? Target,
    string RequestedHost,
    string RequestedPath,
    string RequestedMethod,
    bool WouldCaptureBodies,
    bool WouldCaptureSensitiveHeaderValues,
    bool WouldPersistCookies,
    bool WouldSubmit,
    string OperatorId);

public sealed record NexaExternalProofHarnessDecision(
    NexaExternalProofHarnessDecisionKind Decision,
    NexaExternalTestOwnedTargetDecision TargetDecision,
    IReadOnlyList<string> ReasonCodes,
    NexaOperatorBlockerExplanation Explanation,
    bool CanExecuteReadOnlyNavigation,
    bool Redacted);

public enum NexaExternalReadOnlyEvidencePackStatus
{
    PreparedButNotExecuted,
    SkippedNoOptIn,
    BlockedNoTarget,
    BlockedPolicyViolation,
    ExecutedReadOnlyAgainstApprovedTestOwnedTarget,
    FailedRuntime,
    PassedReadOnlyProof
}

public sealed record NexaExternalReadOnlyEvidencePack(
    string ProofId,
    string? TargetId,
    NexaExternalTestOwnedTargetStatus TargetApprovalStatus,
    DateTimeOffset TimestampUtc,
    string RuntimeProvider,
    IReadOnlyList<string> RuntimeCapabilities,
    string RequestedAction,
    string AllowedAction,
    IReadOnlyList<string> DeniedActions,
    string? VisitedHost,
    string? VisitedPath,
    string Method,
    string RedactionSummary,
    IReadOnlyList<string> ScreenshotRefs,
    IReadOnlyList<string> LogRefs,
    IReadOnlyList<string> PolicyDecisions,
    IReadOnlyList<NexaOperatorBlockerExplanation> BlockerExplanations,
    string FinalGoNoGo,
    NexaExternalReadOnlyEvidencePackStatus Status,
    bool CandidateForM51M65Closure,
    bool Redacted);
