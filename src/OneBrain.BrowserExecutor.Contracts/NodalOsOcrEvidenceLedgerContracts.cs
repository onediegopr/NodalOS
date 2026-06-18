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
    NodalOsRegionCaptureFingerprint? CaptureFingerprint);

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
