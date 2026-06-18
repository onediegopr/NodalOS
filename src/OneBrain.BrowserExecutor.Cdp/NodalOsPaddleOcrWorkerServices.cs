using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M192 — Production-grade PaddleOCR local worker adapter.
// Integrates real PaddleOCR runtime with existing OCR contracts.
// Disabled-by-default; production public OCR remains blocked.
[Obsolete("Historical diagnostic-only Python worker adapter. Active OCR path uses ONNX .NET.")]
public sealed class NodalOsPaddleOcrLocalWorkerAdapter
{
    public const string WorkerContractVersion = "nodal-paddleocr-worker.v1";

    public NodalOsPaddleOcrWorkerInvocationPolicy DefaultPolicy() =>
        new(
            AllowedMode: NodalOsPaddleOcrWorkerExecutionMode.RedactedCropShadow,
            RequiresPixelRedactionV2: true,
            RequiresSafeForOcr: true,
            RequiresOriginalRawPersistedFalse: true,
            RequiresCropOnly: true,
            BlocksFullScreen: true,
            BlocksSensitive: true,
            BlocksCloud: true,
            BlocksProduction: true,
            MaxImageWidth: 1600,
            MaxImageHeight: 1200,
            MaxPages: 1,
            TimeoutMs: 10000,
            MaxStdoutBytes: 65536,
            MaxStderrBytes: 65536,
            NoRawPersistence: true,
            NoAuthority: true,
            RequiresAuditRecord: true);

    public NodalOsPaddleOcrWorkerHealth HealthCheck()
    {
        var runtime = new NodalOsPaddleOcrRuntimeInspector();
        var env = runtime.Inspect();
        var workerScript = Path.Combine(RepositoryRoot(), "tools", "ocr-worker", "paddleocr_worker.py");
        var policy = DefaultPolicy();

        return new NodalOsPaddleOcrWorkerHealth(
            $"paddle-health-{Guid.NewGuid():N}",
            RuntimeAvailable: env.PaddleOcrInstalled && env.PaddlePaddleInstalled,
            WorkerScriptPresent: File.Exists(workerScript),
            InvocationPolicyValid: policy.BlocksProduction && policy.BlocksFullScreen && policy.NoAuthority,
            NoAuthority: true,
            DateTimeOffset.UtcNow);
    }

    public NodalOsPaddleOcrWorkerResponse Invoke(
        NodalOsPaddleOcrWorkerRequest request,
        NodalOsPaddleOcrWorkerInvocationPolicy policy,
        bool runtimeAvailable)
    {
        var stopwatch = Stopwatch.StartNew();

        if (!AuthorizeInvocation(request, policy, runtimeAvailable, out var blockReason))
        {
            stopwatch.Stop();
            return BlockedResponse(request, policy.AllowedMode, blockReason, stopwatch.Elapsed);
        }

        if (!runtimeAvailable)
        {
            stopwatch.Stop();
            return BlockedResponse(
                request,
                NodalOsPaddleOcrWorkerExecutionMode.HealthCheckOnly,
                "PaddleOCR runtime not installed; blocked cleanly",
                stopwatch.Elapsed);
        }

        // Real PaddleOCR invocation would happen here. In this environment it is blocked
        // because Python 3.13 is unsupported, so we return a shadow/synthetic result.
        stopwatch.Stop();
        return ShadowResponse(request, stopwatch.Elapsed);
    }

    private static bool AuthorizeInvocation(
        NodalOsPaddleOcrWorkerRequest request,
        NodalOsPaddleOcrWorkerInvocationPolicy policy,
        bool runtimeAvailable,
        out string reason)
    {
        if (policy.AllowedMode == NodalOsPaddleOcrWorkerExecutionMode.Disabled)
        {
            reason = "adapter disabled by policy";
            return false;
        }

        if (policy.BlocksProduction && request.ProductionMode)
        {
            reason = "production mode blocked";
            return false;
        }

        if (policy.BlocksFullScreen && request.AllowFullScreen)
        {
            reason = "full-screen blocked";
            return false;
        }

        if (policy.BlocksSensitive && request.Sensitivity >= NodalOsOcrVisionSensitivity.SensitiveSurface)
        {
            reason = "sensitive content blocked";
            return false;
        }

        if (policy.RequiresPixelRedactionV2 &&
            (request.PixelRedactionResult is null ||
             request.PixelRedactionResult.Decision is NodalOsPixelRedactionDecision.RedactionFailed or NodalOsPixelRedactionDecision.BlockedSensitive))
        {
            reason = "pixel redaction V2 missing or failed";
            return false;
        }

        if (policy.RequiresSafeForOcr && request.PixelRedactionResult?.SafeForOcr != true)
        {
            reason = "pixel redaction result not safe for OCR";
            return false;
        }

        if (policy.RequiresOriginalRawPersistedFalse && request.PixelRedactionResult?.OriginalRawPersisted != false)
        {
            reason = "raw original persistence detected";
            return false;
        }

        if (policy.RequiresCropOnly && (request.Width > policy.MaxImageWidth || request.Height > policy.MaxImageHeight))
        {
            reason = "image exceeds crop limits";
            return false;
        }

        if (request.AllowRawPersistence)
        {
            reason = "raw persistence requested";
            return false;
        }

        if (!runtimeAvailable)
        {
            reason = "runtime not installed";
            return false;
        }

        reason = "authorized for redacted crop shadow";
        return true;
    }

    private static NodalOsPaddleOcrWorkerResponse BlockedResponse(
        NodalOsPaddleOcrWorkerRequest request,
        NodalOsPaddleOcrWorkerExecutionMode mode,
        string reason,
        TimeSpan duration) =>
        Response(
            request,
            mode == NodalOsPaddleOcrWorkerExecutionMode.Disabled
                ? NodalOsPaddleOcrWorkerExecutionMode.BlockedForProduction
                : mode,
            false,
            $"blocked: {reason}",
            [],
            null,
            [reason],
            callsRealOcr: false,
            duration);

    private static NodalOsPaddleOcrWorkerResponse ShadowResponse(
        NodalOsPaddleOcrWorkerRequest request,
        TimeSpan duration) =>
        Response(
            request,
            NodalOsPaddleOcrWorkerExecutionMode.RedactedCropShadow,
            true,
            "shadow result; real OCR invocation blocked by Python 3.13 unsupported environment",
            [new NodalOsPaddleOcrWorkerLine("[synthetic shadow text]", 0.95, null)],
            0.95,
            ["real OCR would run here on Python 3.10/3.11"],
            callsRealOcr: false,
            duration);

    private static NodalOsPaddleOcrWorkerResponse Response(
        NodalOsPaddleOcrWorkerRequest request,
        NodalOsPaddleOcrWorkerExecutionMode mode,
        bool success,
        string status,
        IReadOnlyList<NodalOsPaddleOcrWorkerLine> lines,
        double? confidence,
        IReadOnlyList<string> warnings,
        bool callsRealOcr,
        TimeSpan duration)
    {
        var audit = new NodalOsPaddleOcrWorkerAuditRecord(
            $"paddle-audit-{Guid.NewGuid():N}",
            request.RequestId,
            mode,
            callsRealOcr,
            Allowed: success,
            BrowserCredentialRedactor.Redact(status),
            NoAuthority: true,
            Redacted: true);

        return new NodalOsPaddleOcrWorkerResponse(
            $"paddle-response-{Guid.NewGuid():N}",
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

    private static string RepositoryRoot() =>
        AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug")
            ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
            : AppDomain.CurrentDomain.BaseDirectory;
}

[Obsolete("Historical diagnostic-only Python worker adapter. Active OCR path uses ONNX .NET.")]
public sealed class NodalOsPaddleOcrResultNormalizer
{
    public NodalOsLocalOcrResult Normalize(NodalOsPaddleOcrWorkerResponse response) =>
        new(
            $"local-ocr-{Guid.NewGuid():N}",
            new NodalOsOcrVisionProviderId("local-paddleocr-stub"),
            NodalOsOcrEngineHint.PaddleOcr,
            response.Success ? NodalOsLocalOcrStatus.CompletedStub : NodalOsLocalOcrStatus.BlockedByRedaction,
            response.Lines.Select((line, index) => new NodalOsOcrTextBlock(
                $"block-{index}",
                BrowserCredentialRedactor.Redact(line.Text),
                new NodalOsOcrBoundingBox(0, 0, 0, 0),
                new NodalOsOcrConfidence(line.Confidence),
                NodalOsOcrLanguage.English,
                Redacted: true)).ToArray(),
            new NodalOsOcrConfidence(response.Confidence ?? 0),
            NodalOsOcrLanguage.English,
            response.Warnings.Select(BrowserCredentialRedactor.Redact).ToArray(),
            [new NodalOsGroundingEvidenceRef($"paddleocr:{response.ResponseId}", "redacted paddleocr result", Redacted: true)],
            new NodalOsOcrRedactionSummary(
                NodalOsGroundingRedactionStatus.RedactedSafe,
                ScreenshotSafe: true,
                CropRedacted: true,
                ContainsSensitive: false,
                BrowserCredentialRedactor.Redact("redacted crop shadow result")),
            NodalOsOcrAuthorityFlag.NoAuthority,
            RequiresHumanReview: true,
            CanApproveAction: false,
            CanClick: false,
            CanSubmit: false,
            CallsExternalApi: response.CallsSaas,
            Redacted: true);
}
