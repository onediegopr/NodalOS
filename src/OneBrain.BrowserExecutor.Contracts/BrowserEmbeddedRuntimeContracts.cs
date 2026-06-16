namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserEmbeddedRuntimeKind
{
    ChromeCdpExternal,
    WebView2Embedded,
    CefEmbedded,
    Disabled,
    Future
}

public enum BrowserRuntimeArchitectureRecommendation
{
    KeepChromeCdpPrimary,
    PrototypeEmbeddedSandboxOnly,
    DoNotUse,
    FutureReviewRequired
}

public enum BrowserEmbeddedRuntimeSafetyDecisionKind
{
    Allowed,
    Blocked,
    SandboxOnly,
    DesignOnly
}

public enum BrowserRuntimeProviderKind
{
    ChromeCdpExternal,
    WebView2EmbeddedSandbox,
    CefEmbeddedSandbox,
    FakeTestRuntime,
    Disabled
}

public enum BrowserRuntimeCapabilityKind
{
    NavigationReadOnly,
    DomReadOnly,
    NetworkMetadataOnly,
    SafeDownload,
    SafeUpload,
    ProfileControlled,
    VaultBoundary,
    RecorderReadOnly,
    ReplaySafeMode,
    SensitiveSimulation,
    ExternalReadOnly
}

public enum BrowserRuntimeProviderCapabilityStatus
{
    Supported,
    Unsupported,
    DesignOnly,
    SandboxOnly,
    RequiresGate,
    RequiresApproval
}

public enum BrowserRuntimeProviderDecisionKind
{
    Compatible,
    SandboxOnly,
    Blocked
}

public sealed record BrowserEmbeddedRuntimeOption(
    BrowserEmbeddedRuntimeKind Kind,
    bool PrimarySupported,
    bool SandboxPrototypeAllowed,
    bool ProductionAllowed,
    string Summary);

public sealed record BrowserRuntimeArchitectureTradeoff(
    string Criterion,
    BrowserEmbeddedRuntimeKind RuntimeKind,
    string Assessment,
    int Score);

public sealed record BrowserRuntimeArchitectureRisk(
    string RiskId,
    BrowserEmbeddedRuntimeKind RuntimeKind,
    string Severity,
    string Description,
    string Mitigation);

public sealed record BrowserRuntimeArchitectureDecision(
    string DecisionId,
    IReadOnlyList<BrowserEmbeddedRuntimeOption> Options,
    IReadOnlyList<BrowserRuntimeArchitectureTradeoff> Tradeoffs,
    IReadOnlyList<BrowserRuntimeArchitectureRisk> Risks,
    BrowserRuntimeArchitectureRecommendation Recommendation,
    BrowserEmbeddedRuntimeKind PrimaryRuntime,
    bool ChromeCdpRemainsPrimary,
    bool EmbeddedRuntimeProductionDisabled,
    bool Redacted);

public sealed record BrowserEmbeddedRuntimeCapability(
    BrowserRuntimeCapabilityKind Capability,
    BrowserRuntimeProviderCapabilityStatus Status,
    bool RequiresGate,
    bool RequiresApproval);

public sealed record BrowserEmbeddedRuntimeEvidence(
    IReadOnlyList<string> EvidenceRefs,
    bool SemanticProofVerified,
    bool CookiesExposed,
    bool BodiesCaptured,
    bool SensitiveHeaderValuesCaptured,
    bool Redacted);

public sealed record BrowserEmbeddedRuntimeSafetyDecision(
    BrowserEmbeddedRuntimeSafetyDecisionKind Decision,
    IReadOnlyList<string> ReasonCodes,
    bool Redacted);

public sealed record BrowserEmbeddedRuntimeSandboxRequest(
    string RequestId,
    BrowserEmbeddedRuntimeKind RuntimeKind,
    string FixturePath,
    bool EnableSandbox,
    bool AllowExternalSites,
    bool AllowDownloads,
    bool AllowUploads,
    bool RuntimeAuthoritative);

public sealed record BrowserEmbeddedRuntimeSandboxResult(
    BrowserEmbeddedRuntimeSafetyDecision Decision,
    BrowserEmbeddedRuntimeEvidence Evidence,
    IReadOnlyList<BrowserEmbeddedRuntimeCapability> Capabilities,
    bool NavigatedLocalFixture,
    bool ReadDom,
    bool ReplacedChromeCdp,
    bool ProductionActivated,
    string SemanticProof,
    bool Redacted);

public sealed record BrowserEmbeddedRuntimeSandbox(
    BrowserEmbeddedRuntimeKind RuntimeKind,
    bool EnabledByDefault,
    bool LocalFixtureOnly,
    bool ProductionDisabled,
    IReadOnlyList<string> FixtureRoutes,
    string ExpectedSemanticProof);

public sealed record BrowserRuntimeProviderCapability(
    BrowserRuntimeCapabilityKind Capability,
    BrowserRuntimeProviderCapabilityStatus Status);

public sealed record BrowserRuntimeProviderCapabilitySet(
    IReadOnlyList<BrowserRuntimeProviderCapability> Capabilities)
{
    public BrowserRuntimeProviderCapabilityStatus StatusFor(BrowserRuntimeCapabilityKind capability) =>
        Capabilities.FirstOrDefault(item => item.Capability == capability)?.Status ?? BrowserRuntimeProviderCapabilityStatus.Unsupported;
}

public sealed record BrowserRuntimeProviderSafetyProfile(
    bool CoreAuthorityRequired,
    bool RuntimeAuthoritative,
    bool ExposesCookiesOrSession,
    bool CapturesBodies,
    bool CapturesSensitiveHeaderValues,
    bool AllowsUnsafeDownloadUpload,
    bool AllowsIrreversibleActions,
    bool ProducesEvidenceRefs,
    bool RespectsCoreFsmSafety,
    bool ProductionActive,
    bool ReplacesChromeCdpPrimary);

public sealed record BrowserRuntimeProvider(
    BrowserRuntimeProviderKind Kind,
    bool Primary,
    bool SandboxOnly,
    bool DesignOnly,
    BrowserRuntimeProviderCapabilitySet CapabilitySet,
    BrowserRuntimeProviderSafetyProfile SafetyProfile);

public sealed record BrowserRuntimeProviderDecision(
    BrowserRuntimeProviderDecisionKind Decision,
    IReadOnlyList<string> ReasonCodes,
    bool Redacted);

public sealed record BrowserRuntimeProviderCompatibilityReport(
    BrowserRuntimeProvider Provider,
    BrowserRuntimeProviderDecision Decision,
    bool ChromeCdpRemainsPrimary,
    bool EmbeddedRuntimeProductionDisabled,
    bool Redacted)
{
    public bool Compatible => Decision.Decision is BrowserRuntimeProviderDecisionKind.Compatible or BrowserRuntimeProviderDecisionKind.SandboxOnly;
}
