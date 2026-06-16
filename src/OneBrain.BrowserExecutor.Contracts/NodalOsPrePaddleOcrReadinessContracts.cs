namespace OneBrain.BrowserExecutor.Contracts;

// M190 — Pre-PaddleOCR readiness review.
// Consolidates M188 pixel redaction and M189 process/IPC isolation.
// Decision is honest: does NOT declare ready for real OCR until all gates are proven.

public enum NodalOsPrePaddleOcrReadinessDecision
{
    NotReadyForPaddleOcr,
    ReadyForPaddleOcrDesignOnly,
    ReadyForPaddleOcrSyntheticInstallPlan,
    ReadyForLocalEchoOnly,
    BlockedByRedaction,
    BlockedByIsolation,
    BlockedByIpcSecurity,
    BlockedByRawPersistenceRisk,
    BlockedByNetworkRisk,
    BlockedByAuthorityRisk
}

public sealed record NodalOsPrePaddleOcrRequirement(
    string RequirementId,
    string Name,
    bool Passed,
    bool IsModeledOnly,
    string EvidenceRef);

public sealed record NodalOsPrePaddleOcrReadinessReview(
    string ReviewId,
    NodalOsPixelRedactionResult? PixelRedactionResult,
    NodalOsLocalWorkerProcessHealth IpcHealth,
    NodalOsLocalWorkerProcessIsolationEvidence IsolationEvidence,
    bool FullScreenBlocked,
    bool ActivationGateStillBlocksRealOcr,
    bool SaaSStillDisabled,
    bool HumanEscalationConfigured,
    bool RollbackPauseConfigured,
    bool EvaluationHarnessPassing,
    bool MessageSizeLimitConfigured,
    bool TimeoutKillBehaviorVerified,
    bool ProcessBoundaryEvaluated,
    bool NoAuthority,
    bool Redacted);

public sealed record NodalOsPrePaddleOcrReadinessReport(
    string ReportId,
    NodalOsPrePaddleOcrReadinessDecision Decision,
    string Reason,
    bool PixelRedactionV2Verified,
    bool RawNotPersisted,
    bool FullScreenBlocked,
    bool IpcAuthEnforcedOrModeled,
    bool MessageSizeLimitEnforced,
    bool TimeoutKillBehaviorVerified,
    bool ProcessBoundaryEvaluated,
    bool ActivationGateStillBlocksRealOcr,
    bool SaaSStillDisabled,
    bool HumanEscalationConfigured,
    bool RollbackPauseConfigured,
    bool EvaluationHarnessPassing,
    NodalOsLocalWorkerIsolationEnforcementLevel NetworkIsolationStatus,
    NodalOsLocalWorkerIsolationEnforcementLevel FilesystemIsolationStatus,
    NodalOsLocalWorkerIsolationEnforcementLevel ProcessIsolationStatus,
    IReadOnlyList<NodalOsPrePaddleOcrRequirement> Requirements,
    IReadOnlyList<string> Warnings,
    DateTimeOffset CreatedAtUtc,
    bool NoAuthority,
    bool Redacted);
