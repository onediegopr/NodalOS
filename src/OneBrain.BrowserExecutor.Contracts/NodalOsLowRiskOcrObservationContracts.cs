namespace OneBrain.BrowserExecutor.Contracts;

// M319-M321 — internal low-risk OCR observation contracts.
// These requests/results are explicitly evidence-only and never authorize actions.

public enum NodalOsOcrObservationRiskLevel
{
    LowRiskOnly
}

public enum NodalOsOcrObservationSource
{
    RealQaWindowRegion,
    InternalControlledScreenRegion,
    InternalControlledRealImage
}

public enum NodalOsOcrObservationDecision
{
    AcceptedEvidenceOnly,
    RejectedNonLowRiskRequest,
    RejectedActionsRequested,
    RejectedAuthorityRequested,
    RejectedSensitiveData,
    RejectedDocumentData,
    RejectedCredentialData,
    RejectedFullScreen,
    RejectedInvalidRegion,
    RejectedUnknownSource
}

public sealed record NodalOsOcrObservationRequest(
    string ObservationId,
    NodalOsOcrObservationSource SourceCategory,
    string CaptureMode,
    string WindowTitleOrSource,
    string ProcessOrSource,
    NodalOsScreenRegionBounds RegionBounds,
    string? ExpectedText,
    NodalOsOcrObservationRiskLevel RiskLevel,
    bool LowRiskOnly,
    bool AllowActions,
    bool AllowAuthority,
    bool ContainsSensitiveData,
    bool ContainsDocumentData,
    bool ContainsCredentials,
    bool FullScreen,
    string Reason);

public sealed record NodalOsOcrObservationResult(
    string ObservationId,
    NodalOsOcrObservationDecision PolicyDecision,
    bool Accepted,
    string RecognizedText,
    string NormalizedText,
    string? ExpectedText,
    bool ExactMatch,
    bool NormalizedMatch,
    int? EditDistance,
    double? Confidence,
    IReadOnlyList<string> Warnings,
    bool NoAuthority,
    bool ActionAllowed,
    bool EvidenceOnly,
    string RedactionStatus,
    bool RawImagePersisted,
    string ModelFamily,
    string DictionaryPolicy,
    bool OfficialSpacePolicy,
    bool SoftmaxReapplied);

public sealed record NodalOsOcrEvidenceEnvelope(
    string ObservationId,
    DateTimeOffset Timestamp,
    string CaptureMode,
    NodalOsOcrObservationSource SourceCategory,
    string WindowTitleOrSource,
    string ProcessOrSource,
    NodalOsScreenRegionBounds RegionBounds,
    int DetectorBoxesCount,
    NodalOsOcrObservationResult Result);
