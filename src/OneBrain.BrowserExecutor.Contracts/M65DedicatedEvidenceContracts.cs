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
