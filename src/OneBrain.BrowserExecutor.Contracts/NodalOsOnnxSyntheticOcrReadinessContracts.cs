namespace OneBrain.BrowserExecutor.Contracts;

// M205 — ONNX synthetic OCR readiness review contracts.
// Honest gate: only advance to shadow mode if all safety and quality requirements pass.

public enum NodalOsOnnxSyntheticOcrReadinessDecision
{
    ReadyForRedactedCropShadow,
    ReadyForMoreSyntheticFixtures,
    BlockedByModelRuntime,
    BlockedByPreProcessing,
    BlockedByPostProcessing,
    BlockedByLowConfidence,
    BlockedByNoTextDetected,
    BlockedByEvidenceRisk,
    NotReady
}

public sealed record NodalOsOnnxSyntheticOcrRequirement(
    string RequirementId,
    string Name,
    bool Satisfied,
    string Evidence,
    string MissingReason,
    bool BlocksReadiness);

public sealed record NodalOsOnnxSyntheticOcrReport(
    string ReportId,
    NodalOsOnnxSyntheticOcrReadinessDecision Decision,
    bool CanAttemptRedactedCropShadow,
    bool CanContinueWithMoreFixtures,
    IReadOnlyList<NodalOsOnnxSyntheticOcrRequirement> Requirements,
    bool ModelsVerified,
    bool SessionsLoaded,
    bool SyntheticCropPrepared,
    bool DetectionInferenceAttempted,
    bool RecognitionInferenceAttempted,
    bool ResultNormalized,
    bool NoRawPersistence,
    bool NoFullScreen,
    bool NoSensitive,
    bool NoSaas,
    bool NoAuthority,
    bool ProductionPublicOcrBlocked,
    IReadOnlyList<string> Warnings,
    DateTimeOffset CreatedAtUtc);
