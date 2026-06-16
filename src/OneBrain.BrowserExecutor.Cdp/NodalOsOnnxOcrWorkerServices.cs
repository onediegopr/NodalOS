using System.Diagnostics;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M196 — ONNX Runtime .NET OCR worker skeleton + readiness gate.
// Uses Microsoft.ML.OnnxRuntime package but only instantiates sessions when verified models exist.
// If models are missing, returns ModelMissing cleanly. No real OCR is executed without models.
public sealed class NodalOsOnnxOcrWorker
{
    public const string WorkerContractVersion = "nodal-onnx-ocr-worker.v1";

    public NodalOsOnnxOcrWorkerHealth HealthCheck(NodalOsOnnxOcrModelSet modelSet)
    {
        var onnxRuntimeAvailable = IsOnnxRuntimePackageAvailable();
        var invocationPolicyValid =
            modelSet.NoAuthority;

        return new NodalOsOnnxOcrWorkerHealth(
            $"onnx-health-{Guid.NewGuid():N}",
            onnxRuntimeAvailable,
            modelSet.AllRequiredPresent,
            modelSet.AllVerified,
            invocationPolicyValid,
            NoAuthority: true,
            DateTimeOffset.UtcNow);
    }

    public NodalOsOnnxOcrWorkerResponse Invoke(
        NodalOsOnnxOcrWorkerRequest request,
        NodalOsOnnxOcrModelSet modelSet,
        bool productionEnabled)
    {
        var stopwatch = Stopwatch.StartNew();

        if (!AuthorizeInvocation(request, productionEnabled, out var blockReason))
        {
            stopwatch.Stop();
            return BlockedResponse(request, blockReason, stopwatch.Elapsed);
        }

        if (!IsOnnxRuntimePackageAvailable())
        {
            stopwatch.Stop();
            return BlockedResponse(request, "ONNX Runtime package not available", stopwatch.Elapsed);
        }

        if (!modelSet.AllRequiredPresent)
        {
            stopwatch.Stop();
            return ModelMissingResponse(request, "required ONNX models missing", stopwatch.Elapsed);
        }

        if (!modelSet.AllVerified)
        {
            stopwatch.Stop();
            return ModelMissingResponse(request, "ONNX models present but not verified", stopwatch.Elapsed);
        }

        // At this point models are verified and ONNX Runtime is available.
        // Real inference would happen here; for now we return a shadow result to avoid
        // executing OCR without explicit authorization and real model validation.
        stopwatch.Stop();
        return ShadowResponse(request, stopwatch.Elapsed);
    }

    public NodalOsOnnxOcrReadinessReport ReadinessReport(NodalOsOnnxOcrModelSet modelSet) =>
        new(
            $"onnx-readiness-{Guid.NewGuid():N}",
            Ready: IsOnnxRuntimePackageAvailable() && modelSet.AllRequiredPresent && modelSet.AllVerified,
            Mode: NodalOsOnnxOcrWorkerExecutionMode.SyntheticRedactedCropOnly,
            OnnxRuntimePackageAvailable: IsOnnxRuntimePackageAvailable(),
            ModelSetPresent: modelSet.AllRequiredPresent,
            ModelSetVerified: modelSet.AllVerified,
            PixelRedactionV2Required: true,
            CropOnly: true,
            NoFullScreen: true,
            NoSensitive: true,
            NoRawPersistence: true,
            NoSaas: true,
            NoPython: true,
            NoAuthority: true,
            ProductionPublicOcrBlocked: true,
            [
                IsOnnxRuntimePackageAvailable()
                    ? "ONNX Runtime .NET package available"
                    : "ONNX Runtime .NET package missing",
                modelSet.AllRequiredPresent
                    ? "required ONNX models present"
                    : "required ONNX models missing; see paddleocr-onnx-model-acquisition-plan-m195.md",
                modelSet.AllVerified
                    ? "ONNX models verified"
                    : "ONNX models not verified; checksum/license review required"
            ],
            DateTimeOffset.UtcNow);

    private static bool AuthorizeInvocation(
        NodalOsOnnxOcrWorkerRequest request,
        bool productionEnabled,
        out string reason)
    {
        if (productionEnabled || request.ProductionMode)
        {
            reason = "production OCR blocked";
            return false;
        }

        if (request.AllowFullScreen)
        {
            reason = "full-screen OCR blocked";
            return false;
        }

        if (request.Sensitivity >= NodalOsOcrVisionSensitivity.SensitiveSurface)
        {
            reason = "sensitive content blocked";
            return false;
        }

        if (request.PixelRedactionResult is null ||
            request.PixelRedactionResult.Decision is NodalOsPixelRedactionDecision.RedactionFailed or NodalOsPixelRedactionDecision.BlockedSensitive ||
            !request.PixelRedactionResult.SafeForOcr ||
            request.PixelRedactionResult.OriginalRawPersisted)
        {
            reason = "pixel redaction V2 precondition not met";
            return false;
        }

        if (request.AllowRawPersistence)
        {
            reason = "raw persistence requested";
            return false;
        }

        reason = "authorized for synthetic redacted crop";
        return true;
    }

    private static NodalOsOnnxOcrWorkerResponse BlockedResponse(
        NodalOsOnnxOcrWorkerRequest request,
        string reason,
        TimeSpan duration) =>
        Response(
            request,
            NodalOsOnnxOcrWorkerExecutionMode.BlockedForProduction,
            false,
            $"blocked: {reason}",
            [],
            null,
            [reason],
            callsRealOcr: false,
            duration);

    private static NodalOsOnnxOcrWorkerResponse ModelMissingResponse(
        NodalOsOnnxOcrWorkerRequest request,
        string reason,
        TimeSpan duration) =>
        Response(
            request,
            NodalOsOnnxOcrWorkerExecutionMode.ModelMissing,
            false,
            $"model missing: {reason}",
            [],
            null,
            [reason],
            callsRealOcr: false,
            duration);

    private static NodalOsOnnxOcrWorkerResponse ShadowResponse(
        NodalOsOnnxOcrWorkerRequest request,
        TimeSpan duration) =>
        Response(
            request,
            NodalOsOnnxOcrWorkerExecutionMode.ShadowRedactedCropOnly,
            true,
            "shadow result; real ONNX inference requires explicit authorization and model validation",
            [new NodalOsOnnxOcrLine("[synthetic shadow text]", 0.95, null)],
            0.95,
            ["verified ONNX models available; shadow mode until explicit OCR enable"],
            callsRealOcr: false,
            duration);

    private static NodalOsOnnxOcrWorkerResponse Response(
        NodalOsOnnxOcrWorkerRequest request,
        NodalOsOnnxOcrWorkerExecutionMode mode,
        bool success,
        string status,
        IReadOnlyList<NodalOsOnnxOcrLine> lines,
        double? confidence,
        IReadOnlyList<string> warnings,
        bool callsRealOcr,
        TimeSpan duration)
    {
        var audit = new NodalOsOnnxOcrWorkerAuditRecord(
            $"onnx-audit-{Guid.NewGuid():N}",
            request.RequestId,
            mode,
            callsRealOcr,
            Allowed: success,
            BrowserCredentialRedactor.Redact(status),
            NoAuthority: true,
            Redacted: true);

        return new NodalOsOnnxOcrWorkerResponse(
            $"onnx-response-{Guid.NewGuid():N}",
            mode,
            success,
            status,
            lines,
            confidence,
            warnings,
            callsRealOcr,
            CallsSaas: false,
            RawPersisted: false,
            NoAuthority: true,
            Redacted: true,
            duration,
            audit);
    }

    private static bool IsOnnxRuntimePackageAvailable()
    {
        try
        {
            // This will succeed only if Microsoft.ML.OnnxRuntime is referenced and loadable.
            _ = typeof(Microsoft.ML.OnnxRuntime.SessionOptions);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

// Placeholder pre/post processing services. Real implementations require verified models.
public sealed class NodalOsOnnxOcrPreProcessor
{
    public byte[] ResizeAndNormalize(byte[] rgba, int sourceWidth, int sourceHeight, int targetWidth, int targetHeight)
    {
        // Production implementation: bilinear resize, mean/std normalization, NCHW layout.
        // Placeholder returns a zeroed tensor-sized buffer for contract validation.
        return new byte[targetWidth * targetHeight * 3];
    }
}

public sealed class NodalOsOnnxOcrPostProcessor
{
    public IReadOnlyList<NodalOsOnnxOcrLine> DecodeDetection(float[] output)
    {
        // Production implementation: DBNet/PP-OCR post-processing, box decoding, NMS.
        return [];
    }

    public IReadOnlyList<NodalOsOnnxOcrLine> DecodeRecognition(float[] output)
    {
        // Production implementation: CTC decoding, dictionary lookup.
        return [];
    }
}
