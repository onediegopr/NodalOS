namespace OneBrain.BrowserExecutor.Contracts;

public enum NodalOsOcrFsmObservationDisposition
{
    ObservationContext,
    DiagnosticOnly,
    ExcludedPolicyViolation
}

public sealed record NodalOsOcrFsmObservationInput(
    string InputId,
    IReadOnlyList<NodalOsOcrEvidencePolicyEvaluation> EvidenceEvaluations);

public sealed record NodalOsOcrFsmObservationRanking(
    string ObservationId,
    string? EvidenceId,
    NodalOsOcrFsmObservationDisposition Disposition,
    NodalOsOcrEvidenceLedgerStatus LedgerStatus,
    NodalOsOcrEvidenceUse? EvidenceUse,
    NodalOsOcrEvidenceConfidenceBand ConfidenceBand,
    int RankOrder,
    double RankScore,
    bool RegionVerified,
    bool ConfidenceGatePassed,
    bool FingerprintHashMatch,
    double? DiffScore,
    bool ExactMatch,
    bool NormalizedMatch,
    int? EditDistance,
    NodalOsOcrObservationSource? SourceCategory,
    string? CaptureMode,
    string? WindowTitleOrSource,
    string? ProcessOrSource,
    NodalOsScreenRegionBounds? RegionBounds,
    string Reason);

public sealed record NodalOsOcrFsmObservationResult(
    string SummaryId,
    IReadOnlyList<NodalOsOcrFsmObservationRanking> ObservationContext,
    IReadOnlyList<NodalOsOcrFsmObservationRanking> Diagnostics,
    IReadOnlyList<NodalOsOcrFsmObservationRanking> Excluded,
    bool ReadOnlyObservationOnly,
    bool CanProduceActionPlan,
    bool CanProduceSafeAction,
    bool CanApproveAction,
    bool CanApproveClick,
    bool CanApproveSubmit,
    bool CanApproveSend,
    bool CanApproveDelete,
    bool CanApprovePay,
    bool CanApproveSign,
    bool ProvenancePreserved);
