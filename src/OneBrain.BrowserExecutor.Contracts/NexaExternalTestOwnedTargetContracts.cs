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

public enum NexaSyntheticExternalScenarioKind
{
    LandingReadOnly,
    ProductListReadOnly,
    DocumentReadOnly,
    TableReportReadOnly,
    DisabledFormBlocked,
    LoginBlocked,
    CheckoutPaymentBlocked,
    DestructiveActionBlocked
}

public enum NexaSyntheticExternalScenarioSensitivity
{
    SyntheticOnly,
    CredentialSurfaceBlocked,
    PaymentSurfaceBlocked,
    DestructiveSurfaceBlocked
}

public sealed record NexaSyntheticExternalScenario(
    string ScenarioId,
    NexaSyntheticExternalScenarioKind Kind,
    string Path,
    IReadOnlyList<string> ExpectedAllowedActions,
    IReadOnlyList<string> ExpectedDeniedActions,
    NexaSyntheticExternalScenarioSensitivity Sensitivity,
    string ExpectedEvidenceBehavior,
    NexaOperatorBlockerScenario? ExpectedBlockerExplanation,
    bool UsesRealContent);

public sealed record NexaSyntheticExternalScenarioCatalog(
    IReadOnlyList<NexaSyntheticExternalScenario> Scenarios,
    bool UsesInternet,
    bool UsesRealCustomerData,
    bool Redacted);

public enum NexaProofDryRunStatus
{
    DryRunPrepared,
    DryRunAllowedReadOnly,
    DryRunBlockedByPolicy,
    DryRunEvidenceGenerated,
    DryRunDoesNotCloseM51M65
}

public sealed record NexaProofDryRunResult(
    string DryRunId,
    NexaProofDryRunStatus Status,
    NexaSyntheticExternalScenario Scenario,
    NexaExternalProofHarnessDecision HarnessDecision,
    NexaExternalReadOnlyEvidencePack EvidencePack,
    bool ExecutedNetwork,
    bool ClosesM51M65,
    bool Redacted);

public enum NexaTargetBindingDeploymentProvider
{
    VercelHobbyLab,
    Unknown
}

public enum NexaTargetBindingDnsMode
{
    DelegatedToVercel,
    CnameOnly,
    Unknown
}

public enum NexaTargetBindingVerificationStatus
{
    NotConfigured,
    DnsPending,
    VercelPending,
    HttpsReady,
    OwnershipVerified
}

public sealed record NexaTargetBindingConfig(
    string ExpectedDomain,
    string ExpectedBaseUrl,
    string ExpectedOwnershipPath,
    string ExpectedHealthPath,
    IReadOnlySet<string> AllowedHosts,
    IReadOnlySet<string> AllowedPaths,
    IReadOnlySet<string> DeniedPaths,
    NexaTargetBindingDeploymentProvider DeploymentProvider,
    NexaTargetBindingDnsMode DnsMode,
    NexaTargetBindingVerificationStatus VerificationStatus);

public sealed record NexaTargetBindingDecision(
    NexaTargetBindingConfig Config,
    IReadOnlyList<string> ReasonCodes,
    bool CandidateLiveProofAllowed,
    bool ExecutesNetwork,
    bool Redacted);

public enum NexaLiveProofSafetyGateStatus
{
    LiveProofNotConfigured,
    DnsPending,
    HttpsPending,
    OwnershipPending,
    TargetPolicyRejected,
    HarnessOptInMissing,
    OperatorApprovalMissing,
    ReadyForReadOnlyLiveProof,
    BlockedSensitiveSurface
}

public sealed record NexaLiveProofSafetyGateRequest(
    NexaTargetBindingConfig? Binding,
    NexaExternalTestOwnedTarget? Target,
    bool HarnessOptInEnabled,
    string RequestedHost,
    string RequestedPath,
    string RequestedMethod,
    bool WouldUseCredentials,
    bool WouldPersistPersonalCookies,
    bool WouldCaptureSensitiveHeaderValues,
    bool WouldCaptureBodies,
    bool WouldSubmit,
    bool WouldMutate,
    bool WouldUsePaymentOrCheckoutOrRealLogin,
    bool EvidencePackReady,
    bool OperatorApprovalRequired,
    string? OperatorApprovalRef);

public sealed record NexaLiveProofSafetyGateDecision(
    NexaLiveProofSafetyGateStatus Status,
    IReadOnlyList<string> ReasonCodes,
    NexaOperatorBlockerExplanation Explanation,
    bool ReadyForReadOnlyLiveProof,
    bool ClosesM51M65,
    bool ExecutesNetwork,
    bool Redacted);
