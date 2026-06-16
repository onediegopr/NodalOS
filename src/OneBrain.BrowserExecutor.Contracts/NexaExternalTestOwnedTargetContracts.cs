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

public enum NexaExternalProofProbeKind
{
    ModeledFake,
    RealHttpClient,
    RealChromeCdp
}

public enum NexaExternalEvidencePersistenceStatus
{
    NotPersisted,
    NotPersistedModeled,
    PersistedRedactedLedger,
    PersistenceFailed
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
    bool Redacted,
    NexaExternalProofProbeKind ProbeKind = NexaExternalProofProbeKind.ModeledFake,
    NexaExternalEvidencePersistenceStatus PersistenceStatus = NexaExternalEvidencePersistenceStatus.NotPersisted,
    string Tooling = "ModeledFake",
    string? LedgerRef = null,
    long? LedgerSequence = null,
    string? LedgerHash = null,
    DateTimeOffset? PersistedAtUtc = null);

public sealed record ChromeCdpDomReadOnlySnapshot(
    string Url,
    string Title,
    IReadOnlyList<string> MetadataKeys,
    int ElementCount,
    bool FullDomPersisted,
    bool ContainsSensitiveMaterial,
    bool Redacted);

public sealed record ChromeCdpReadOnlyEvidencePolicy(
    bool RequireRealChromeCdpSession,
    bool RequireNavigationEvidence,
    bool RequireDomPageMetadata,
    bool BlockFullDomPersistence,
    bool BlockSubmitMutationLoginPayment,
    bool BlockCookiesTokensSecrets,
    IReadOnlySet<string> AllowedHosts,
    IReadOnlySet<string> AllowedPaths);

public enum ChromeCdpExternalProbeStatus
{
    SkippedNoOptIn,
    ChromeCdpUnavailable,
    TargetPolicyBlocked,
    NavigationFailed,
    DomReadOnlyFailed,
    SafetyRejected,
    PassedReadOnlyDomProof
}

public sealed record ChromeCdpExternalProbeResult(
    ChromeCdpExternalProbeStatus Status,
    bool IsRealChromeCdpSession,
    bool NavigatedToAllowedTarget,
    bool DomOrPageMetadataCaptured,
    string BrowserVersion,
    string TargetUrl,
    IReadOnlyList<string> RoutesVisited,
    IReadOnlyList<string> PolicyBlockedRoutes,
    ChromeCdpDomReadOnlySnapshot? Snapshot,
    bool SubmittedOrMutated,
    bool UsedCredentials,
    bool UsedLoginOrPayment,
    bool PersistedCookies,
    bool CapturedSensitiveHeaderValues,
    bool PersistedFullDomOrBody,
    bool SecretsCookiesTokensDetected,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ReasonCodes,
    bool Redacted);

public enum ChromeCdpExternalProofStatus
{
    SkippedNoOptIn,
    ChromeCdpUnavailable,
    BlockedPolicyViolation,
    FailedRuntime,
    PassedReadOnlyProof
}

public sealed record ChromeCdpExternalProofReadiness(
    bool OptInEnabled,
    bool TargetVerified,
    bool LiveProofSafetyGatePassed,
    bool ChromeCdpAvailable,
    bool ReadyForReadOnlyDomProof,
    IReadOnlyList<string> ReasonCodes,
    bool Redacted);

public sealed record ChromeCdpExternalProofResult(
    ChromeCdpExternalProofStatus Status,
    ChromeCdpExternalProofReadiness Readiness,
    ChromeCdpExternalProbeResult? ProbeResult,
    NexaExternalReadOnlyEvidencePack EvidencePack,
    M65DedicatedEvidenceReview M65Review,
    bool ExecutedLiveCdp,
    bool Redacted);

public enum ChromeCdpExternalPreflightStatus
{
    ChromeCdpUnavailable,
    ChromeCdpAvailable,
    IsolatedProfileReady,
    UnsafeProfileBlocked,
    CdpSessionReady,
    CdpPreflightFailed,
    ReadyForExternalCdpReadOnlyProof
}

public sealed record ChromeCdpExternalPreflightRequest(
    bool OptInEnabled,
    string? BrowserExecutablePath,
    string ProfileDirectory,
    bool UsesPersonalProfile,
    bool UsesDefaultUserDataDir,
    bool CookiesPersisted,
    bool CredentialsAvailable,
    bool PersonalExtensionsEnabled,
    bool SavedPasswordsAvailable,
    bool CdpSessionControlled,
    string TargetHost,
    bool ReadOnlyOnly);

public sealed record ChromeCdpExternalPreflightResult(
    ChromeCdpExternalPreflightStatus Status,
    IReadOnlyList<string> ReasonCodes,
    bool CanAttemptLiveProof,
    bool LaunchesBrowser,
    bool Redacted);

public interface INexaChromeCdpExternalProbe
{
    bool IsAvailable { get; }

    Task<ChromeCdpExternalProbeResult> ProbeReadOnlyAsync(
        ChromeCdpReadOnlyEvidencePolicy policy,
        NexaExternalTestOwnedTarget target,
        IReadOnlyList<string> allowedRoutes,
        IReadOnlyList<string> blockedRoutes,
        CancellationToken cancellationToken);
}

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

public enum NexaHttpsOwnershipVerificationStatus
{
    NotChecked,
    DnsResolved,
    HttpsReady,
    HealthOk,
    OwnershipOk,
    MetadataMismatch,
    VerificationFailed,
    VerifiedTestOwnedReadOnlyTarget
}

public sealed record NexaHttpsOwnershipVerificationRequest(
    string ExpectedBaseUrl,
    string RequiredHealthPath,
    string RequiredOwnershipPath,
    IReadOnlyList<string> ExpectedProjectMetadata,
    string DeploymentProvider,
    string DeploymentScope,
    string DeploymentProject,
    bool OptInLiveNetwork);

public sealed record NexaHttpsOwnershipVerificationResult(
    NexaHttpsOwnershipVerificationStatus Status,
    string BaseUrl,
    string HealthUrl,
    string OwnershipUrl,
    int? HealthStatusCode,
    int? OwnershipStatusCode,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ReasonCodes,
    bool RestrictionsConfirmed,
    bool EnablesCandidateLiveProof,
    bool ClosesM51M65,
    bool ExecutedNetwork,
    bool Redacted);

public interface INexaReadOnlyHttpProbe
{
    Task<NexaReadOnlyHttpProbeResult> GetAsync(Uri uri, CancellationToken cancellationToken);
}

public sealed record NexaReadOnlyHttpProbeResult(
    int StatusCode,
    string RedactedText,
    IReadOnlyList<string> HeaderNames,
    bool CapturedCookies,
    bool CapturedBodies,
    bool CapturedSensitiveHeaderValues);

public enum NexaFirstReadOnlyLiveProofStatus
{
    SkippedNoOptIn,
    BlockedVerificationFailed,
    BlockedPolicyViolation,
    CandidateRunnerAllowed,
    PassedReadOnlyProof,
    FailedRuntime
}

public sealed record NexaFirstReadOnlyLiveProofResult(
    NexaFirstReadOnlyLiveProofStatus Status,
    NexaHttpsOwnershipVerificationResult Verification,
    NexaLiveProofSafetyGateDecision SafetyGate,
    NexaExternalReadOnlyEvidencePack EvidencePack,
    IReadOnlyList<string> RoutesTested,
    IReadOnlyList<string> DeniedRoutesTested,
    IReadOnlyList<NexaOperatorBlockerExplanation> BlockerExplanations,
    bool ExecutedNetwork,
    bool Redacted);

public enum NexaM51M65ClosureCandidateReviewDecision
{
    NoLiveProofExecuted,
    LiveProofSkippedNoOptIn,
    LiveProofFailed,
    LiveProofPassedButReviewRequired,
    CandidateCloseM51Only,
    CandidateCloseM65Only,
    CandidateCloseM51AndM65,
    DoNotClose
}

public sealed record NexaM51M65ClosureCandidateReview(
    string ProofId,
    string TargetId,
    string Domain,
    string Provider,
    string ScopeProject,
    NexaHttpsOwnershipVerificationStatus TargetVerificationStatus,
    NexaFirstReadOnlyLiveProofStatus LiveProofStatus,
    IReadOnlyList<string> RoutesTested,
    IReadOnlyList<string> DeniedRoutesTested,
    NexaExternalReadOnlyEvidencePackStatus EvidencePackStatus,
    bool RedactionOk,
    IReadOnlyList<string> PolicyViolations,
    IReadOnlyList<NexaOperatorBlockerExplanation> OperatorExplanations,
    string M51Recommendation,
    string M65Recommendation,
    NexaM51M65ClosureCandidateReviewDecision FinalDecision,
    bool PublicSaasStillDisabled,
    bool RealBillingStillDisabled,
    bool RealEmailStillDisabled,
    bool RealCredentialsStillBlocked,
    bool SensitiveSurfacesStillBlocked,
    bool Redacted);
