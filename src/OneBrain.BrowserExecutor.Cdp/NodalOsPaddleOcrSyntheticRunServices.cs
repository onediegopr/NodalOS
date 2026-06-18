using System.Diagnostics;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M193 — PaddleOCR synthetic redacted crop real OCR run.
// Orchestrates a controlled crop, redaction, and (if runtime available) local OCR invocation.
// If runtime is unavailable, returns a clean BLOCKED_BY_ENVIRONMENT result.
[Obsolete("Historical diagnostic-only synthetic Python worker run. Active OCR path uses ONNX .NET.")]
public sealed class NodalOsPaddleOcrSyntheticRunService
{
    private readonly NodalOsPixelImageRedactor _redactor = new();
    private readonly NodalOsPaddleOcrLocalWorkerAdapter _adapter = new();
    private readonly NodalOsPaddleOcrRuntimeInspector _runtime = new();

    public NodalOsPaddleOcrSyntheticRunResult Run()
    {
        var stopwatch = Stopwatch.StartNew();

        // 1. Generate a small synthetic crop with simple non-sensitive text pixels.
        var (imageBytes, width, height) = GenerateSyntheticCrop();

        // 2. Pixel redaction V2: clean image => CleanNoRedactionRequired.
        var redactionRequest = new NodalOsPixelRedactionRequest(
            $"redact-{Guid.NewGuid():N}",
            imageBytes,
            NodalOsPixelRedactionImageFormat.RawRgba32,
            width,
            height,
            new NodalOsOcrBoundingBox(0, 0, width, height),
            default,
            NodalOsOcrVisionSensitivity.Low,
            [],
            AllowRawPersistence: false,
            AllowFullScreen: false);

        var redaction = _redactor.Redact(redactionRequest);

        // 3. Check runtime availability.
        var env = _runtime.Inspect();
        var runtimeAvailable = env.PaddleOcrInstalled && env.PaddlePaddleInstalled;

        // 4. Build worker request.
        var workerRequest = new NodalOsPaddleOcrWorkerRequest(
            $"paddle-req-{Guid.NewGuid():N}",
            NodalOsPaddleOcrLocalWorkerAdapter.WorkerContractVersion,
            "synthetic-run-token",
            redaction.RedactedImageBytesForOcrHandoff ?? imageBytes,
            width,
            height,
            "en",
            redaction,
            NodalOsOcrVisionSensitivity.Low,
            AllowFullScreen: false,
            AllowRawPersistence: false,
            ProductionMode: false,
            DateTimeOffset.UtcNow);

        // 5. Invoke adapter (blocked if runtime unavailable).
        var adapterResponse = _adapter.Invoke(workerRequest, _adapter.DefaultPolicy(), runtimeAvailable);
        stopwatch.Stop();

        var decision = runtimeAvailable && adapterResponse.Success
            ? NodalOsPaddleOcrSyntheticRunDecision.RealOcrExecuted
            : NodalOsPaddleOcrSyntheticRunDecision.BlockedByEnvironment;

        return new NodalOsPaddleOcrSyntheticRunResult(
            $"run-{Guid.NewGuid():N}",
            decision,
            redaction.Decision,
            adapterResponse.Mode,
            runtimeAvailable,
            adapterResponse.Success,
            adapterResponse.CallsRealOcr,
            adapterResponse.CallsSaas,
            adapterResponse.RawPersisted,
            redaction.OriginalRawPersisted,
            adapterResponse.NoAuthority,
            adapterResponse.Warnings.ToArray(),
            stopwatch.Elapsed,
            DateTimeOffset.UtcNow);
    }

    private static (byte[] Bytes, int Width, int Height) GenerateSyntheticCrop()
    {
        const int w = 128;
        const int h = 64;
        var bytes = new byte[w * h * 4];

        // White background.
        for (var i = 0; i < w * h; i++)
        {
            bytes[(i * 4) + 0] = 0xFF;
            bytes[(i * 4) + 1] = 0xFF;
            bytes[(i * 4) + 2] = 0xFF;
            bytes[(i * 4) + 3] = 0xFF;
        }

        // Draw a simple horizontal bar pattern representing non-sensitive text.
        for (var y = 20; y < 30; y++)
            for (var x = 20; x < 108; x++)
                if (x % 4 != 0)
                {
                    var o = ((y * w) + x) * 4;
                    bytes[o] = 0x00;
                    bytes[o + 1] = 0x00;
                    bytes[o + 2] = 0x00;
                    bytes[o + 3] = 0xFF;
                }

        return (bytes, w, h);
    }
}

[Obsolete("Historical diagnostic-only synthetic Python worker run. Active OCR path uses ONNX .NET.")]
public enum NodalOsPaddleOcrSyntheticRunDecision
{
    RealOcrExecuted,
    ShadowResult,
    BlockedByEnvironment,
    BlockedByRedaction
}

[Obsolete("Historical diagnostic-only synthetic Python worker run. Active OCR path uses ONNX .NET.")]
public sealed record NodalOsPaddleOcrSyntheticRunResult(
    string RunId,
    NodalOsPaddleOcrSyntheticRunDecision Decision,
    NodalOsPixelRedactionDecision RedactionDecision,
    NodalOsPaddleOcrWorkerExecutionMode WorkerMode,
    bool RuntimeAvailable,
    bool AdapterSuccess,
    bool CallsRealOcr,
    bool CallsSaas,
    bool RawPersisted,
    bool OriginalRawPersisted,
    bool NoAuthority,
    IReadOnlyList<string> Warnings,
    TimeSpan Duration,
    DateTimeOffset CreatedAtUtc);
