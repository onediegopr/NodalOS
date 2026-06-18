namespace OneBrain.BrowserExecutor.Contracts;

// M322-M324 — verification/isolation/confidence contracts for low-risk OCR observation.

public enum NodalOsWindowIsolationState
{
    NotEvaluated,
    ForegroundVerified,
    VisibleButNotForeground,
    WrongWindowDetected,
    BoundsMismatch,
    CaptureVerificationFailed
}

public enum NodalOsOcrObservationAcceptanceState
{
    AcceptedEvidence,
    RejectedWrongWindow,
    RejectedNotForeground,
    RejectedBoundsMismatch,
    RejectedFullScreen,
    RejectedSensitive,
    RejectedLowConfidence,
    UncertainRequiresExpansion
}

public sealed record NodalOsRegionCaptureFingerprint(
    string Sha256,
    int Width,
    int Height,
    double AverageRed,
    double AverageGreen,
    double AverageBlue,
    double DarkPixelRatio,
    string SampleSignature);

public sealed record NodalOsOcrRegionVerificationResult(
    string ExpectedWindowTitle,
    string ObservedWindowTitle,
    string ExpectedProcess,
    string ObservedProcess,
    NodalOsScreenRegionBounds ExpectedWindowBounds,
    NodalOsScreenRegionBounds ObservedWindowBounds,
    NodalOsScreenRegionBounds ExpectedRegionBounds,
    NodalOsScreenRegionBounds ObservedRegionBounds,
    bool WindowVisible,
    bool WindowForegroundOrActivated,
    bool RegionInsideWindow,
    bool FullScreen,
    string CaptureTechnique,
    NodalOsRegionCaptureFingerprint? ExpectedFingerprint,
    NodalOsRegionCaptureFingerprint? ObservedFingerprint,
    double? DiffScore,
    bool RegionVerified,
    NodalOsWindowIsolationState IsolationState,
    IReadOnlyList<string> VerificationWarnings,
    string FailureReason);

public sealed record NodalOsOcrObservationConfidenceGateResult(
    bool Attempted,
    bool Passed,
    double ConfidenceThreshold,
    double? ConfidenceScore,
    int EditDistanceThreshold,
    int? EditDistance,
    bool ExactOrNormalizedMatch,
    string Reason);
