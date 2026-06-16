namespace OneBrain.BrowserExecutor.Contracts;

public enum M65EvidenceScope
{
    HttpReadOnlyExpansion,
    ChromeCdpDomReadOnly,
    PolicyBlockedUnsafeRoutes,
    DedicatedLowRiskWorkflow
}

public enum M65EvidenceRequirementStatus
{
    Required,
    Optional,
    NotSufficientAlone
}

public enum M65ClosureReadinessStatus
{
    DeferredNeedsDedicatedEvidence,
    ScopeDefined,
    ScenarioPlanReady,
    EvidenceCollected,
    CandidateCloseM65,
    DoNotClose,
    BlockedByPolicy,
    RequiresChromeCdpDomProof,
    ChromeCdpDomProofSkippedNoOptIn,
    ChromeCdpDomProofUnavailable,
    ChromeCdpDomProofFailed,
    ChromeCdpDomProofPassed,
    ReviewRequired
}

public sealed record M65EvidenceRequirement(
    M65EvidenceScope Scope,
    M65EvidenceRequirementStatus Status,
    NexaExternalProofProbeKind RequiredProbeKind,
    string RequiredTooling,
    bool RequiresLedger,
    bool RequiresTargetVerified,
    bool RequiresNoSecretsCookiesTokens,
    bool RequiresNoSubmitMutationPaymentLogin,
    string Reason);

public sealed record M65DedicatedEvidencePlan(
    string PlanId,
    string TargetBaseUrl,
    IReadOnlyList<M65EvidenceRequirement> Requirements,
    M65ClosureReadinessStatus Status,
    bool M51EvidenceSufficient,
    bool RequiresChromeCdpDomProof,
    bool PublicSaasStillDisabled,
    bool RealBillingStillDisabled,
    bool RealEmailStillDisabled,
    bool RealCredentialsStillBlocked,
    bool SensitiveSurfacesStillBlocked,
    bool Redacted);

public sealed record M65ClosureReadiness(
    M65ClosureReadinessStatus Status,
    string Summary,
    IReadOnlyList<string> MissingEvidence,
    bool CanClose,
    bool CandidateOnly,
    bool Redacted);

public enum M65LowRiskScenarioKind
{
    LandingReadOnlyVerification,
    DocumentReadOnlyVerification,
    StructuredTableReportReadOnly,
    DisabledFormPolicyVerification,
    BlockedLoginPolicyVerification,
    BlockedCheckoutPaymentPolicyVerification,
    BlockedDestructiveActionPolicyVerification,
    SyntheticMultiPageReadOnlyWorkflow,
    SafeSearchFilterReadOnly,
    SyntheticDownloadMetadataOnly
}

public enum M65ScenarioEvidenceMode
{
    HttpReadOnlyScenario,
    BrowserCdpDomScenarioPending,
    PolicyOnlyScenario,
    NotEnoughForClosure
}

public sealed record M65LowRiskExternalScenario(
    string ScenarioId,
    string Path,
    M65LowRiskScenarioKind Kind,
    IReadOnlyList<string> AllowedActions,
    IReadOnlyList<string> DeniedActions,
    NexaExternalProofProbeKind RequiredProbeKind,
    string RequiredTooling,
    string RequiredEvidence,
    string RiskLevel,
    string PolicyExplanation,
    bool RequiresLedger,
    bool RelevantForM65Closure,
    M65ScenarioEvidenceMode EvidenceMode,
    bool Redacted);

public sealed record M65LowRiskExternalScenarioCatalog(
    string TargetBaseUrl,
    IReadOnlyList<M65LowRiskExternalScenario> Scenarios,
    bool UsesRealCustomerData,
    bool UsesCredentials,
    bool AllowsSubmitMutationPaymentLogin,
    bool PlanReady,
    bool Redacted);

public sealed record M65DedicatedEvidenceReviewInput(
    bool M51Closed,
    bool ScenarioPlanReady,
    NexaExternalProofProbeKind ProofKind,
    string Tooling,
    bool LedgerRefPresent,
    bool TargetVerified,
    bool ReadOnlyProofPassed,
    bool ChromeCdpDomProofPassed,
    bool SecretsCookiesTokensDetected,
    bool SubmitMutationPaymentLoginDetected,
    bool PolicyViolationDetected,
    bool ScopeRequiresChromeCdpDomProof,
    bool PublicSaasEnabled,
    bool RealBillingEnabled,
    bool RealEmailEnabled,
    bool RealCredentialsEnabled,
    bool SensitiveSurfaceEnabled);

public sealed record M65DedicatedEvidenceReview(
    M65ClosureReadinessStatus Status,
    IReadOnlyList<string> ReasonCodes,
    string Recommendation,
    bool CandidateCloseM65,
    bool PublicSaasStillDisabled,
    bool RealBillingStillDisabled,
    bool RealEmailStillDisabled,
    bool RealCredentialsStillBlocked,
    bool SensitiveSurfacesStillBlocked,
    bool Redacted);

public enum M65FormalClosureDecision
{
    CandidateOnly,
    FormallyClosedTargetOwnedReadOnlyCdp,
    DoNotClose,
    NeedsAdditionalEvidence,
    ScopeInflationBlocked
}

public enum M65ClosureScope
{
    TargetOwnedExternalLowRiskChromeCdpDomReadOnly,
    GeneralExternalCdp,
    ProductionExternalCdp,
    SensitiveExternalCdp,
    ThirdPartyExternalCdp
}

public sealed record M65FormalClosureReviewInput(
    M65DedicatedEvidenceReview CandidateReview,
    string TargetBaseUrl,
    NexaExternalProofProbeKind ProofKind,
    string Tooling,
    IReadOnlyList<string> Capabilities,
    bool LedgerPersisted,
    string? LedgerRef,
    string? LedgerHash,
    bool IsolatedProfile,
    bool NoSecretsCookiesTokens,
    bool NoFullDomOrBodyPersisted,
    bool NoSubmitMutationPaymentLogin,
    bool BlockedRoutesPolicyVerified,
    bool PublicSaasEnabled,
    bool PublicApiEnabled,
    bool RealBillingEnabled,
    bool RealEmailEnabled,
    bool RealCredentialsEnabled,
    bool SensitiveSitesEnabled,
    bool GeneralExternalCdpRequested);

public sealed record M65FormalClosureReview(
    M65FormalClosureDecision Decision,
    M65ClosureScope Scope,
    string TargetBaseUrl,
    string Summary,
    IReadOnlyList<string> ReasonCodes,
    string? LedgerRef,
    string? LedgerHash,
    bool ExternalCdpGeneralReady,
    bool PublicSaasStillDisabled,
    bool PublicApiStillDisabled,
    bool RealBillingStillDisabled,
    bool RealEmailStillDisabled,
    bool RealCredentialsStillBlocked,
    bool SensitiveSitesStillBlocked,
    bool Redacted);

public enum ExternalCdpScopeLockStatus
{
    TargetOwnedProofOnly,
    GeneralExternalCdpBlocked,
    SensitiveExternalCdpBlocked,
    ProductionExternalCdpBlocked,
    ThirdPartyExternalCdpBlocked,
    RequiresDedicatedApproval,
    RequiresNewEvidence
}

public sealed record ExternalCdpScopeLockRequest(
    bool M65FormallyClosed,
    string RequestedTargetHost,
    bool IsTargetOwnedLabHost,
    bool IsThirdPartyTarget,
    bool IsSensitiveTarget,
    bool ProductionModeRequested,
    bool CredentialsRequested,
    bool SubmitPaySignDeleteRequested,
    bool GeneralExternalCdpRequested,
    bool HasDedicatedApproval,
    bool HasNewEvidence);

public sealed record ExternalCdpScopeLockDecision(
    ExternalCdpScopeLockStatus Status,
    bool Allowed,
    bool BrowserRuntimeExternalGeneralReady,
    IReadOnlyList<string> ReasonCodes,
    string OperatorMessage,
    bool Redacted);
