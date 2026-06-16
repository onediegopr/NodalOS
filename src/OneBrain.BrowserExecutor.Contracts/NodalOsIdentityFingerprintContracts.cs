namespace OneBrain.BrowserExecutor.Contracts;

public enum NodalOsIdentityConfidence
{
    Unknown,
    Low,
    Medium,
    High,
    VerifiedFixture,
    VerifiedTargetOwned
}

public enum NodalOsIdentityMismatchReason
{
    MissingRequiredSignal,
    HostMismatch,
    WindowTitleMismatch,
    RuntimeProviderMismatch,
    UnsafeSurface,
    SensitiveSurface,
    StaleFingerprint,
    InsufficientEvidence,
    AmbiguousIdentity
}

public enum NodalOsIdentityRecommendedDecision
{
    AllowReadOnlyLocalObservation,
    WarnAndRequireCoreReview,
    BlockUntilIdentityVerified,
    BlockSensitiveSurface
}

public sealed record NodalOsIdentityEvidence(
    string EvidenceRef,
    string Source,
    DateTimeOffset TimestampUtc,
    bool Redacted);

public sealed record NodalOsWindowIdentity(
    string WindowId,
    string TitleNormalized,
    string RuntimeProvider,
    string ProcessAppIdentity,
    IReadOnlyList<string> EvidenceRefs);

public sealed record NodalOsSurfaceIdentity(
    string SurfaceId,
    string RuntimeProvider,
    string WindowTitleNormalized,
    string ProcessAppIdentity,
    string? Host,
    string? RoutePath,
    string? DomPageMetadataRedacted,
    string? UiaMetadataRedacted,
    string SafetyProfile,
    string TargetOwnershipScope,
    bool IsFixture,
    bool IsTargetOwned,
    bool IsSensitiveSurface,
    IReadOnlyList<string> EvidenceRefs);

public sealed record NodalOsRuntimeSurfaceFingerprint(
    string FingerprintId,
    NodalOsSurfaceIdentity Surface,
    IReadOnlyList<string> SignalsUsed,
    IReadOnlyList<string> SignalsMissing,
    NodalOsIdentityConfidence Confidence,
    IReadOnlyList<NodalOsIdentityMismatchReason> MismatchReasons,
    string RedactionSummary,
    bool CoreAuthorityRequired,
    bool GrantsActionAuthority);

public sealed record NodalOsIdentityFingerprintV2(
    string IdentityProofId,
    NodalOsRuntimeSurfaceFingerprint Fingerprint,
    IReadOnlyList<NodalOsIdentityEvidence> Evidence,
    NodalOsIdentityRecommendedDecision RecommendedDecision,
    bool ActionAuthorityGranted,
    bool Redacted);

public sealed record NodalOsIdentityFixture(
    string FixtureId,
    NodalOsSurfaceIdentity Expected,
    NodalOsSurfaceIdentity Observed,
    DateTimeOffset ObservedAtUtc,
    bool ForceStale,
    bool ForceAmbiguous);

public sealed record NodalOsIdentityFixtureHarnessReport(
    string HarnessId,
    IReadOnlyList<NodalOsIdentityFingerprintV2> Proofs,
    bool IdentityFixtureReadiness,
    bool IdentityBlocked,
    bool IdentityWarning,
    bool CoreAuthorityRequired,
    bool ActionAuthorityGranted,
    IReadOnlyList<string> EvidenceRefs,
    bool Redacted);

public sealed record NodalOsIdentityEvidenceSummary(
    string IdentityProofId,
    string FingerprintId,
    NodalOsIdentityConfidence Confidence,
    IReadOnlyList<string> SignalsUsed,
    IReadOnlyList<string> SignalsMissing,
    IReadOnlyList<NodalOsIdentityMismatchReason> MismatchReasons,
    string RedactionSummary,
    string Source,
    string FixtureOrTargetScope,
    NodalOsIdentityRecommendedDecision RecommendedDecision,
    bool CoreAuthorityRequired,
    bool ActionAuthorityGranted,
    bool Redacted);

