namespace OneBrain.BrowserExecutor.Contracts;

// M212-M214 - guarded synthetic text OCR probes and readiness contracts.
// Risky text OCR probes model metadata explicitly and may only execute through the M210 child guard.

public enum NodalOsGuardedSyntheticTextOcrProbeStatus
{
    PositiveDetection,
    PositiveRecognition,
    NoTextDetected,
    LowConfidence,
    RecognitionEmpty,
    DictionaryMismatch,
    NativeRuntimeCrashContained,
    TimedOut,
    InvalidTensorShape,
    BlockedByPreProcessing,
    BlockedByPostProcessing,
    BlockedByModelRuntime,
    BlockedByDictionary,
    BlockedByModelCompatibility,
    BlockedBeforeRuntime
}

public enum NodalOsGuardedSyntheticTextOcrPreProcessingVariant
{
    CurrentMeanStd,
    KeepAspectWhitePadding,
    KeepAspectBlackPadding,
    ThresholdBinarization,
    RgbOrder,
    BgrOrder,
    ChannelLayoutValidation
}

public sealed record NodalOsGuardedSyntheticTextOcrFixtureMetadata(
    string FixtureName,
    string ExpectedText,
    NodalOsOnnxNativeRuntimeCrashFixtureKind FixtureKind,
    NodalOsSyntheticOcrTextRenderMode RenderMode,
    NodalOsSyntheticOcrTextColorScheme ColorScheme,
    int Width,
    int Height,
    int HorizontalPadding,
    int VerticalPadding,
    string Background,
    string Foreground,
    bool UsesSystemFont,
    bool Deterministic,
    bool RiskyTextFixture,
    bool FullScreen,
    bool Sensitive,
    bool RawPersisted,
    bool NoAuthority);

public sealed record NodalOsGuardedSyntheticTextOcrProbeRequest(
    string ProbeId,
    NodalOsGuardedSyntheticTextOcrFixtureMetadata Fixture,
    NodalOsGuardedSyntheticTextOcrPreProcessingVariant PreProcessingVariant,
    NodalOsOnnxNativeRuntimeCrashStage Stage,
    bool RequiresOutOfProcessGuard,
    bool AllowInProcess,
    bool CallsSaas,
    bool ProductionMode);

public sealed record NodalOsGuardedSyntheticTextOcrProbeResult(
    string ResultId,
    string ProbeId,
    NodalOsGuardedSyntheticTextOcrProbeStatus Status,
    bool RanInProcess,
    bool RanOutOfProcess,
    bool ParentSurvived,
    bool ChildLaunched,
    bool TempFilesCleaned,
    bool RawPersisted,
    bool CallsSaas,
    bool NoAuthority,
    int? BoxesDetected,
    int? RecognitionAttempts,
    string Reason);

public sealed record NodalOsGuardedSyntheticTextOcrProbeMatrix(
    string MatrixId,
    IReadOnlyList<NodalOsGuardedSyntheticTextOcrProbeRequest> Requests,
    int FixtureVariantCount,
    int DimensionCount,
    int PreProcessingVariantCount,
    bool RiskyProbesRequireGuard,
    bool RejectsRiskyInProcess,
    bool NoRawPersistence,
    bool NoFullScreen,
    bool NoSensitive,
    bool NoSaas,
    bool NoAuthority,
    DateTimeOffset CreatedAtUtc);

public sealed record NodalOsOnnxTensorStats(
    int[] Shape,
    float Min,
    float Max,
    float Mean,
    bool HasNaN,
    bool HasInfinity,
    string ChannelLayout,
    string ColorOrder);

public sealed record NodalOsDetectorCompatibilityDiagnosis(
    string DiagnosisId,
    bool SessionCreationReached,
    string RuntimeVersion,
    string Provider,
    string ModelPath,
    string ModelOpset,
    int[] InputShape,
    NodalOsOnnxTensorStats InputTensorStats,
    IReadOnlyList<string> OutputNames,
    IReadOnlyList<int[]> OutputShapes,
    bool SessionRunCrashed,
    bool PostProcessingCrashed,
    int BoxesDetected,
    NodalOsGuardedSyntheticTextOcrProbeStatus Status,
    string Reason);

public sealed record NodalOsRecognizerCompatibilityDiagnosis(
    string DiagnosisId,
    bool Reachable,
    int[] RecognizerInputShape,
    bool CropExtractionSucceeded,
    IReadOnlyList<string> OutputNames,
    IReadOnlyList<int[]> OutputShapes,
    bool CtcDecodingAttempted,
    bool DictionaryCompatible,
    string DictionaryId,
    double? Confidence,
    NodalOsGuardedSyntheticTextOcrProbeStatus Status,
    string Reason);

public enum NodalOsGuardedSyntheticTextOcrReadinessDecision
{
    ReadyForGuardedSyntheticTextRun,
    ReadyForSyntheticPositiveRecognition,
    ReadyForDictionaryCompletion,
    ReadyForModelCompatibilityFix,
    ReadyForOnnxRuntimeVersionExperiment,
    ReadyForRendererFix,
    ReadyForMoreSyntheticFixtures,
    BlockedByModelRuntime,
    BlockedByModelCompatibility,
    BlockedByInputTensorShape,
    BlockedByPreProcessing,
    BlockedByPostProcessing,
    BlockedByDictionary,
    BlockedByRenderer,
    NotReady
}

public sealed record NodalOsGuardedSyntheticTextOcrReadinessReport(
    string ReportId,
    NodalOsGuardedSyntheticTextOcrReadinessDecision Decision,
    bool GuardExists,
    bool RiskyTextNeverRanInProcess,
    bool ParentSurvivedCrash,
    bool ChildCleanupWorks,
    bool TempCleanupWorks,
    bool NoRawPersistence,
    bool NoFullScreen,
    bool NoSensitive,
    bool NoSaas,
    bool NoAuthority,
    bool DetectionDiagnosed,
    bool RecognitionDiagnosedOrUnreachable,
    bool DictionaryStatusDocumented,
    bool ModelCompatibilityDocumented,
    bool ShadowModeBlocked,
    bool ProductionPublicOcrBlocked,
    string Reason);
