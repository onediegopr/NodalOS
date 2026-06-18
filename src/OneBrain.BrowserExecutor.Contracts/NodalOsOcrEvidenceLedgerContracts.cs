namespace OneBrain.BrowserExecutor.Contracts;

public enum NodalOsOcrEvidenceSource
{
    Ocr
}

public enum NodalOsOcrEvidenceAuthority
{
    NoAuthority
}

public enum NodalOsOcrEvidenceUse
{
    AuxiliaryOnly,
    DiagnosticOnly
}

public enum NodalOsOcrEvidenceLedgerStatus
{
    AcceptedAuxiliary,
    RecordedDiagnosticRejected,
    RecordedDiagnosticUncertain,
    RejectedPolicyViolation
}

public enum NodalOsOcrEvidenceKind
{
    OcrObservationEvidence
}

public enum NodalOsOcrEvidenceConfidenceBand
{
    Unknown,
    Rejected,
    Low,
    Medium,
    High
}

public sealed record NodalOsOcrEvidenceLedgerEntry(
    string ObservationId,
    string EvidenceId,
    DateTimeOffset Timestamp,
    NodalOsOcrEvidenceSource Source,
    NodalOsOcrEvidenceKind Kind,
    NodalOsOcrEvidenceUse EvidenceUse,
    NodalOsOcrEvidenceAuthority Authority,
    NodalOsOcrObservationSource SourceCategory,
    string CaptureMode,
    string WindowTitleOrSource,
    string ProcessOrSource,
    NodalOsScreenRegionBounds RegionBounds,
    string RecognizedText,
    string NormalizedText,
    string? ExpectedText,
    bool ExactMatch,
    bool NormalizedMatch,
    int? EditDistance,
    double? Confidence,
    bool RegionVerified,
    bool ConfidenceGatePassed,
    NodalOsOcrObservationAcceptanceState AcceptanceState,
    string RejectionReason,
    bool NoAuthority,
    bool EvidenceOnly,
    bool ActionAllowed,
    bool SoftmaxReapplied,
    bool OfficialSpacePolicy,
    string ModelFamily,
    string DictionaryPolicy,
    bool RawImagePersisted,
    string RedactionStatus,
    IReadOnlyList<string> Warnings,
    string? ArtifactRef,
    NodalOsOcrRegionVerificationResult? RegionVerification,
    NodalOsOcrObservationConfidenceGateResult? ConfidenceGate,
    NodalOsRegionCaptureFingerprint? CaptureFingerprint)
{
    public bool FingerprintHashMatch { get; init; }

    public double? DiffScore { get; init; }

    public double? DarkPixelRatio { get; init; }

    public (double Red, double Green, double Blue)? MeanRgb { get; init; }

    public string? SampleSignature { get; init; }

    public NodalOsOcrEvidenceConfidenceBand ConfidenceBand { get; init; } = NodalOsOcrEvidenceConfidenceBand.Unknown;
}

public sealed record NodalOsOcrEvidencePolicyEvaluation(
    NodalOsOcrEvidenceLedgerStatus LedgerStatus,
    bool CanRegister,
    bool CanBeAcceptedEvidence,
    bool CanAttachAsAuxiliaryEvidence,
    bool CanAttachAsDiagnosticEvidence,
    bool CanAuthorizeAction,
    bool CanApproveClick,
    bool CanApproveSubmit,
    bool CanApproveSend,
    bool CanApproveDelete,
    bool CanApprovePay,
    bool CanApproveSign,
    string Reason,
    NodalOsOcrEvidenceLedgerEntry? Entry);
