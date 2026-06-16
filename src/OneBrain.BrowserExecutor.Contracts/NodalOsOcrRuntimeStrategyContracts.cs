namespace OneBrain.BrowserExecutor.Contracts;

// M194 — OCR runtime strategy pivot: ONNX Runtime .NET as primary local OCR path.
// Python PaddleOCR becomes legacy/fallback only. SaaS remains disabled-by-default.

public enum NodalOsOcrRuntimeKind
{
    OnnxRuntimeDotNet,
    PaddleOcrOnnxModels,
    CppLocalWorker,
    PythonPaddleOcrLegacy,
    TesseractFallback,
    CloudSaasDisabled,
    HumanReview
}

public enum NodalOsOcrRuntimePriority
{
    Primary,
    Secondary,
    Fallback,
    LastResort,
    Disabled
}

public enum NodalOsOcrRuntimeDecision
{
    PrimaryReady,
    SecondaryReady,
    FallbackReady,
    Disabled,
    BlockedByEnvironment,
    BlockedByPolicy,
    BlockedByMissingDependency,
    BlockedByMissingModel,
    RequiresHumanReview
}

public enum NodalOsOcrRuntimeCapability
{
    TextDetection,
    TextRecognition,
    TextDirectionClassification,
    LayoutAnalysis,
    TableRecognition,
    LocalInference,
    NoPythonDependency,
    NoSaasDependency,
    NoApiKey,
    PackagedForWindows
}

public enum NodalOsOcrRuntimeRisk
{
    PythonRuntimeFragility,
    ModelSize,
    LicenseUncertainty,
    PrePostProcessingComplexity,
    PerformanceUnknown,
    MemoryUsage,
    OperationalOverhead,
    SaasDataLeak
}

public sealed record NodalOsOcrRuntimeStrategy(
    string StrategyId,
    NodalOsOcrRuntimeKind PrimaryRuntime,
    NodalOsOcrRuntimeKind SecondaryRuntime,
    NodalOsOcrRuntimeKind FallbackRuntime,
    NodalOsOcrRuntimeKind LastResortRuntime,
    bool SaasDisabledByDefault,
    bool ProductionPublicOcrBlocked,
    bool NoAuthority,
    bool NoRawPersistence,
    bool CropOnly,
    bool NoFullScreen,
    bool NoSensitive,
    IReadOnlyList<NodalOsOcrRuntimeCapability> RequiredCapabilities,
    IReadOnlyList<NodalOsOcrRuntimeRisk> AcceptedRisks,
    IReadOnlyList<NodalOsOcrRuntimeRisk> BlockedRisks,
    string Reason);
