using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M194 — OCR runtime strategy pivot service.
// Primary: ONNX Runtime .NET + PaddleOCR ONNX models.
// Secondary: C++ local worker.
// Fallback: Python PaddleOCR legacy/experimental only.
// SaaS: disabled-by-default.
public sealed class NodalOsOcrRuntimeStrategyService
{
    public NodalOsOcrRuntimeStrategy CurrentStrategy() =>
        new(
            $"ocr-strategy-{Guid.NewGuid():N}",
            NodalOsOcrRuntimeKind.OnnxRuntimeDotNet,
            NodalOsOcrRuntimeKind.CppLocalWorker,
            NodalOsOcrRuntimeKind.PythonPaddleOcrLegacy,
            NodalOsOcrRuntimeKind.HumanReview,
            SaasDisabledByDefault: true,
            ProductionPublicOcrBlocked: true,
            NoAuthority: true,
            NoRawPersistence: true,
            CropOnly: true,
            NoFullScreen: true,
            NoSensitive: true,
            [
                NodalOsOcrRuntimeCapability.TextDetection,
                NodalOsOcrRuntimeCapability.TextRecognition,
                NodalOsOcrRuntimeCapability.LocalInference,
                NodalOsOcrRuntimeCapability.NoPythonDependency,
                NodalOsOcrRuntimeCapability.NoSaasDependency,
                NodalOsOcrRuntimeCapability.NoApiKey,
                NodalOsOcrRuntimeCapability.PackagedForWindows
            ],
            [
                NodalOsOcrRuntimeRisk.ModelSize,
                NodalOsOcrRuntimeRisk.PrePostProcessingComplexity,
                NodalOsOcrRuntimeRisk.PerformanceUnknown,
                NodalOsOcrRuntimeRisk.MemoryUsage
            ],
            [
                NodalOsOcrRuntimeRisk.PythonRuntimeFragility,
                NodalOsOcrRuntimeRisk.SaasDataLeak
            ],
            "ONNX Runtime .NET is the primary local OCR path because it integrates with NODAL OS .NET stack, avoids Python runtime fragility, and supports Windows packaging. Python PaddleOCR is demoted to legacy/experimental fallback.");

    public NodalOsOcrRuntimeDecision Decide(
        bool onnxRuntimePackageAvailable,
        bool onnxModelAvailable,
        bool modelVerified)
    {
        if (!onnxRuntimePackageAvailable)
            return NodalOsOcrRuntimeDecision.BlockedByMissingDependency;

        if (!onnxModelAvailable)
            return NodalOsOcrRuntimeDecision.BlockedByMissingModel;

        if (!modelVerified)
            return NodalOsOcrRuntimeDecision.RequiresHumanReview;

        return NodalOsOcrRuntimeDecision.PrimaryReady;
    }

    public bool RequiresPython(NodalOsOcrRuntimeKind kind) =>
        kind == NodalOsOcrRuntimeKind.PythonPaddleOcrLegacy;

    public bool RequiresSaas(NodalOsOcrRuntimeKind kind) =>
        kind == NodalOsOcrRuntimeKind.CloudSaasDisabled;

    public bool RequiresApiKey(NodalOsOcrRuntimeKind kind) => false;
}
