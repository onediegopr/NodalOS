using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OnnxSyntheticOcrReadiness")]
[TestCategory("OnnxOcrNoAuthority")]
[TestCategory("OcrVisionActivationGate")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("BrowserGroundingSnapshot")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsOnnxSyntheticOcrReadinessM208Tests
{
    private static NodalOsPixelRedactionResult CleanRedaction()
    {
        return new NodalOsPixelRedactionResult(
            $"redact-{Guid.NewGuid():N}",
            NodalOsPixelRedactionDecision.CleanNoRedactionRequired,
            "hash-original",
            "hash-redacted",
            [],
            NodalOsPixelRedactionVerificationStatus.Verified,
            SafeForOcr: true,
            SafeForPersistence: false,
            OriginalRawPersisted: false,
            NoAuthority: true,
            [],
            [],
            new NodalOsPixelRedactionEvidence(
                $"ev-{Guid.NewGuid():N}",
                "hash-original",
                "hash-redacted",
                OriginalRawPersisted: false,
                [],
                "clean synthetic crop",
                Redacted: true),
            Redacted: true);
    }

    private static NodalOsOnnxOcrSyntheticInferenceResult BlockedByModelRuntimeResult()
    {
        var detection = new NodalOsOnnxOcrDetectionInferenceResult(
            $"det-{Guid.NewGuid():N}",
            NodalOsOnnxOcrInferenceStatus.BlockedByModelRuntime,
            Array.Empty<NodalOsOnnxOcrTextBox>(),
            0,
            "paddleocr-det-onnx",
            "v4",
            "native onnx runtime crash on synthetic text fixture");

        var evidence = new NodalOsOnnxOcrEndToEndEvidence(
            $"ev-{Guid.NewGuid():N}",
            "hash-redacted",
            detection.ResultId,
            null,
            new[] { "paddleocr-det-onnx", "paddleocr-rec-onnx" },
            new[] { "redaction-evidence-1" },
            0,
            0,
            0,
            OriginalRawPersisted: false,
            CallsSaas: false,
            ProductionEnabled: false,
            NoAuthority: true,
            "blocked by model runtime");

        return new NodalOsOnnxOcrSyntheticInferenceResult(
            $"inf-{Guid.NewGuid():N}",
            NodalOsOnnxOcrInferenceStatus.BlockedByModelRuntime,
            false,
            detection,
            Array.Empty<NodalOsOnnxOcrRecognitionInferenceResult>(),
            evidence,
            CallsRealOcr: true,
            CallsSaas: false,
            RawPersisted: false,
            NoAuthority: true,
            RequiresHumanReview: false,
            TimeSpan.FromMilliseconds(0),
            Array.Empty<string>());
    }

    private static NodalOsOnnxOcrSyntheticInferenceResult NoTextSafeShapeResult()
    {
        var detection = new NodalOsOnnxOcrDetectionInferenceResult(
            $"det-{Guid.NewGuid():N}",
            NodalOsOnnxOcrInferenceStatus.NoTextDetected,
            Array.Empty<NodalOsOnnxOcrTextBox>(),
            100,
            "paddleocr-det-onnx",
            "v4",
            "no text detected on safe shape");

        var evidence = new NodalOsOnnxOcrEndToEndEvidence(
            $"ev-{Guid.NewGuid():N}",
            "hash-redacted",
            detection.ResultId,
            null,
            new[] { "paddleocr-det-onnx", "paddleocr-rec-onnx" },
            new[] { "redaction-evidence-1" },
            100,
            0,
            0,
            OriginalRawPersisted: false,
            CallsSaas: false,
            ProductionEnabled: false,
            NoAuthority: true,
            "safe shape diagnostic");

        return new NodalOsOnnxOcrSyntheticInferenceResult(
            $"inf-{Guid.NewGuid():N}",
            NodalOsOnnxOcrInferenceStatus.NoTextDetected,
            true,
            detection,
            Array.Empty<NodalOsOnnxOcrRecognitionInferenceResult>(),
            evidence,
            CallsRealOcr: true,
            CallsSaas: false,
            RawPersisted: false,
            NoAuthority: true,
            RequiresHumanReview: false,
            TimeSpan.FromMilliseconds(100),
            Array.Empty<string>());
    }

    private static NodalOsOnnxOcrNormalizedResult Normalized(NodalOsOnnxOcrSyntheticInferenceResult inference)
    {
        return new NodalOsOnnxOcrResultNormalizer().Normalize(inference, CleanRedaction());
    }

    [TestMethod]
    public void Readiness_BlockedByModelRuntime_WhenInferenceReportsIt()
    {
        var review = new NodalOsOnnxSyntheticOcrReadinessReview();
        var inference = BlockedByModelRuntimeResult();
        var normalized = Normalized(inference);

        var report = review.Evaluate(inference, normalized, modelsVerified: true, sessionsLoaded: true);

        Assert.AreEqual(NodalOsOnnxSyntheticOcrReadinessDecision.BlockedByModelRuntime, report.Decision);
        Assert.IsFalse(report.CanAttemptRedactedCropShadow);
        Assert.IsFalse(report.CanContinueWithMoreFixtures);
    }

    [TestMethod]
    public void Readiness_DoesNotAdvanceToShadow_WithoutTextDetection()
    {
        var review = new NodalOsOnnxSyntheticOcrReadinessReview();
        var inference = NoTextSafeShapeResult();
        var normalized = Normalized(inference);

        var report = review.Evaluate(inference, normalized, modelsVerified: true, sessionsLoaded: true);

        Assert.AreEqual(NodalOsOnnxSyntheticOcrReadinessDecision.ReadyForMoreSyntheticFixtures, report.Decision);
        Assert.IsFalse(report.CanAttemptRedactedCropShadow);
        Assert.IsTrue(report.CanContinueWithMoreFixtures);
    }

    [TestMethod]
    public void Readiness_Preserves_NoAuthority_NoSaas_NoProduction_WhenBlocked()
    {
        var review = new NodalOsOnnxSyntheticOcrReadinessReview();
        var inference = BlockedByModelRuntimeResult();
        var normalized = Normalized(inference);

        var report = review.Evaluate(inference, normalized, modelsVerified: true, sessionsLoaded: true);

        Assert.IsTrue(report.NoAuthority);
        Assert.IsTrue(report.NoSaas);
        Assert.IsTrue(report.ProductionPublicOcrBlocked);
        Assert.IsTrue(report.NoRawPersistence);
    }

    [TestMethod]
    public void Report_And_Artifact_Exist_ForM208()
    {
        var repoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug")
            ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
            : AppDomain.CurrentDomain.BaseDirectory;

        Assert.IsTrue(File.Exists(Path.Combine(repoRoot, "docs", "reports", "onnx-synthetic-text-fixture-run-m208.md")));
        Assert.IsTrue(File.Exists(Path.Combine(repoRoot, "docs", "audits", "claude-onnx-synthetic-text-fixture-run-audit-m208.md")));
        Assert.IsTrue(File.Exists(Path.Combine(repoRoot, "artifacts", "ocr-vision-onnx", "m208", "onnx-synthetic-text-fixture-run-summary.json")));
    }
}
