namespace OneBrain.BrowserExecutor.Contracts;

public enum NodalOsWindowLivenessState
{
    Unknown,
    NotFound,
    Launching,
    Alive,
    Responding,
    Frozen,
    Crashed,
    Minimized,
    Hidden,
    Occluded,
    Stale,
    BlockedByOverlay,
    UnsafeSurface
}

public enum NodalOsSurfaceStabilityState
{
    Unknown,
    Stable,
    Loading,
    Mutating,
    Empty,
    Truncated,
    Ambiguous,
    Blocked,
    Unsafe
}

public enum NodalOsPerceptionReadiness
{
    Unknown,
    UsableForReadOnlyContext,
    WarningRequiresCoreReview,
    Blocked,
    Unsafe
}

public enum NodalOsPerceptionMismatchReason
{
    MissingIdentitySignal,
    WindowNotAlive,
    WindowNotResponding,
    SurfaceUnstable,
    OverlayBlocksPerception,
    UiaTreeEmpty,
    DomMetadataEmpty,
    SurfaceTruncated,
    AmbiguousSurface,
    SensitiveSurface,
    UnsafeSurface,
    SemanticEvidenceInsufficient
}

public enum NodalOsBlockedSurfaceReason
{
    None,
    SystemModalOverlay,
    PermissionOverlay,
    SecurityWarningOverlay,
    LoadingOverlay,
    ConsentOverlayFixture,
    BlockedLoginOverlayFixture,
    EmptyUiaTree,
    EmptyDomMetadata,
    TruncatedSurface,
    AmbiguousInteractiveSurface,
    SensitiveBlockedSurface
}

public enum NodalOsSemanticFallbackDecision
{
    NotNeeded,
    AvailableForReadOnlyContext,
    InsufficientSemanticEvidence,
    BlockedByOverlay,
    BlockedSensitiveSurface,
    RequiresHumanReview,
    RequiresFutureVisionOcr
}

public sealed record NodalOsPerceptionSignal(
    string Name,
    string ValueRedacted,
    string Source,
    bool Required,
    bool Present);

public sealed record NodalOsWindowLivenessEvidence(
    string EvidenceRef,
    DateTimeOffset TimestampUtc,
    NodalOsWindowLivenessState LivenessState,
    NodalOsSurfaceStabilityState StabilityState,
    IReadOnlyList<NodalOsPerceptionSignal> Signals,
    IReadOnlyList<NodalOsPerceptionMismatchReason> MismatchReasons,
    bool CoreAuthorityRequired,
    bool ActionAuthorityGranted,
    bool Redacted);

public sealed record NodalOsPerceptionEvaluation(
    string PerceptionProofId,
    NodalOsIdentityFingerprintV2 IdentityProof,
    NodalOsWindowLivenessEvidence LivenessEvidence,
    NodalOsPerceptionReadiness Readiness,
    IReadOnlyList<string> EvidenceRefs,
    string RedactionSummary,
    bool BlocksSensitiveActions,
    bool CoreAuthorityRequired,
    bool ActionAuthorityGranted);

public sealed record NodalOsOverlayDetectionResult(
    NodalOsBlockedSurfaceReason Reason,
    bool OverlayDetected,
    NodalOsIdentityConfidence Confidence,
    IReadOnlyList<string> EvidenceRefs,
    bool BlocksPerception,
    bool ActionAuthorityGranted,
    bool Redacted);

public sealed record NodalOsEmptySurfaceDetectionResult(
    bool UiaTreeEmpty,
    bool DomMetadataEmpty,
    bool Truncated,
    bool AmbiguousInteractiveSurface,
    NodalOsBlockedSurfaceReason Reason,
    IReadOnlyList<string> EvidenceRefs,
    bool BlocksPerception,
    bool Redacted);

public sealed record NodalOsPerceptionBlockerExplanation(
    string Cause,
    NodalOsIdentityConfidence Confidence,
    string UserExpectedAction,
    IReadOnlyList<string> SafeOptions,
    IReadOnlyList<string> BlockedOptions,
    string RecommendedNextStep,
    IReadOnlyList<string> EvidenceRefs,
    bool ActionAuthorityGranted,
    bool Redacted);

public sealed record NodalOsSemanticAccessSignal(
    string SignalName,
    string DescriptorRedacted,
    string Source,
    bool Present);

public sealed record NodalOsSemanticFallbackEvidence(
    string EvidenceRef,
    IReadOnlyList<NodalOsSemanticAccessSignal> Signals,
    bool UsesOcrVisionProductive,
    bool UsesCredentials,
    bool Redacted);

public sealed record NodalOsSemanticAccessFallback(
    string FallbackId,
    NodalOsSemanticFallbackDecision Decision,
    NodalOsSemanticFallbackEvidence Evidence,
    string Explanation,
    bool CoreAuthorityRequired,
    bool ActionAuthorityGranted);

public sealed record NodalOsRobustPerceptionFixture(
    string FixtureId,
    NodalOsIdentityFixture IdentityFixture,
    NodalOsWindowLivenessState LivenessState,
    NodalOsSurfaceStabilityState StabilityState,
    NodalOsBlockedSurfaceReason BlockedSurfaceReason,
    bool UiaAvailable,
    bool DomMetadataAvailable,
    bool UiaTreeEmpty,
    bool DomMetadataEmpty,
    bool Truncated,
    bool AmbiguousInteractiveSurface,
    bool SensitiveSurface,
    IReadOnlyList<NodalOsSemanticAccessSignal> SemanticSignals,
    bool RequiresFutureVisionOcr);

