namespace OneBrain.BrowserExecutor.Contracts;

public enum NodalOsAssistedVerificationSignalKind
{
    OcrAuxiliaryEvidence,
    DomSignal,
    UiaSignal,
    Win32Signal,
    FsmStateSignal,
    KnownQaFixtureSignal,
    ManualExpectedValueSignal,
    DiagnosticOnly,
    Rejected
}

public enum NodalOsAssistedVerificationDecision
{
    VerifiedLowRisk,
    NotVerified,
    NeedsMoreEvidence,
    RejectedPolicyViolation,
    RejectedOcrOnly,
    RejectedRiskTooHigh,
    RejectedActionRequest,
    RejectedSensitive,
    RejectedFullScreen,
    RejectedDocument
}

public enum NodalOsAssistedVerificationRiskLevel
{
    Low,
    Medium,
    High
}

public sealed record NodalOsAssistedVerificationSignal(
    string SignalId,
    NodalOsAssistedVerificationSignalKind Kind,
    bool SupportsVerification,
    bool DiagnosticOnly,
    bool Rejected,
    string Source,
    string? ExpectedText,
    string? ObservedText,
    string? NormalizedText,
    bool ExactMatch,
    bool NormalizedMatch,
    int? EditDistance,
    NodalOsOcrEvidenceConfidenceBand ConfidenceBand,
    bool RegionVerified,
    bool ConfidenceGatePassed,
    bool FingerprintHashMatch,
    double? DiffScore,
    bool NoAuthority,
    bool EvidenceOnly,
    bool ActionAllowed,
    string Reason,
    NodalOsOcrObservationSource? SourceCategory,
    string? CaptureMode,
    string? WindowTitleOrSource,
    string? ProcessOrSource,
    NodalOsScreenRegionBounds? RegionBounds);

public sealed record NodalOsAssistedVerificationRequest(
    string RequestId,
    NodalOsAssistedVerificationRiskLevel RiskLevel,
    bool LowRiskOnly,
    bool ActionRequested,
    bool ApprovalRequested,
    bool ContainsSensitiveData,
    bool ContainsDocumentData,
    bool ContainsCredentials,
    bool FullScreen,
    IReadOnlyList<NodalOsAssistedVerificationSignal> Signals,
    string Reason);

public sealed record NodalOsAssistedVerificationResult(
    string ResultId,
    NodalOsAssistedVerificationDecision Decision,
    IReadOnlyList<NodalOsAssistedVerificationSignal> SupportingSignals,
    IReadOnlyList<NodalOsAssistedVerificationSignal> DiagnosticSignals,
    bool NonOcrCorroborationRequired,
    bool NonOcrCorroborationSatisfied,
    bool ReadOnlyObservationOnly,
    bool ActionsAllowed,
    bool CanProduceActionPlan,
    bool CanProduceSafeAction,
    bool CanApproveAction,
    bool CanApproveClick,
    bool CanApproveSubmit,
    bool CanApproveSend,
    bool CanApproveDelete,
    bool CanApprovePay,
    bool CanApproveSign,
    bool NoAuthority,
    bool EvidenceOnly,
    bool ProvenancePreserved,
    string Reason);
