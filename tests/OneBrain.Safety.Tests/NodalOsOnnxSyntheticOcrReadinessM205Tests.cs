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
public sealed class NodalOsOnnxSyntheticOcrReadinessM205Tests
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

    private static NodalOsOnnxOcrSyntheticInferenceResult SuccessResult()
    {
        var detection = new NodalOsOnnxOcrDetectionInferenceResult(
            $"det-{Guid.NewGuid():N}",
            NodalOsOnnxOcrInferenceStatus.Success,
            new List<NodalOsOnnxOcrTextBox>
            {
                new("box-0", new List<float> { 0, 0, 10, 0, 10, 10, 0, 10 }, 0.85, 0, 0, 10, 10, true)
            },
            100,
            "paddleocr-det-onnx",
            "v4",
            "detection completed");

        var recognition = new NodalOsOnnxOcrRecognitionInferenceResult(
            $"rec-{Guid.NewGuid():N}",
            NodalOsOnnxOcrInferenceStatus.Success,
            new List<NodalOsOnnxOcrRecognitionCandidate>
            {
                new($"cand-{Guid.NewGuid():N}", "hello", 0.82, new List<int> { 1, 2, 3, 4 }, false, false)
            },
            80,
            "paddleocr-rec-onnx",
            "v4",
            "recognition completed");

        var evidence = new NodalOsOnnxOcrEndToEndEvidence(
            $"ev-{Guid.NewGuid():N}",
            "hash-redacted",
            detection.ResultId,
            recognition.ResultId,
            new[] { "paddleocr-det-onnx", "paddleocr-rec-onnx" },
            new[] { "redaction-evidence-1" },
            200,
            1,
            1,
            OriginalRawPersisted: false,
            CallsSaas: false,
            ProductionEnabled: false,
            NoAuthority: true,
            "synthetic run");

        return new NodalOsOnnxOcrSyntheticInferenceResult(
            $"inf-{Guid.NewGuid():N}",
            NodalOsOnnxOcrInferenceStatus.Success,
            true,
            detection,
            new[] { recognition },
            evidence,
            CallsRealOcr: true,
            CallsSaas: false,
            RawPersisted: false,
            NoAuthority: true,
            RequiresHumanReview: false,
            TimeSpan.FromMilliseconds(200),
            Array.Empty<string>());
    }

    private static NodalOsOnnxOcrSyntheticInferenceResult NoTextResult()
    {
        var detection = new NodalOsOnnxOcrDetectionInferenceResult(
            $"det-{Guid.NewGuid():N}",
            NodalOsOnnxOcrInferenceStatus.NoTextDetected,
            Array.Empty<NodalOsOnnxOcrTextBox>(),
            100,
            "paddleocr-det-onnx",
            "v4",
            "no text detected");

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
            "no text run");

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

    private static NodalOsOnnxOcrSyntheticInferenceResult RawPersistedResult()
    {
        var result = SuccessResult();
        return result with { RawPersisted = true };
    }

    private static NodalOsOnnxOcrNormalizedResult Normalized(NodalOsOnnxOcrSyntheticInferenceResult inference)
    {
        return new NodalOsOnnxOcrResultNormalizer().Normalize(inference, CleanRedaction());
    }

    [TestMethod]
    public void Readiness_Blocks_IfModelsMissing()
    {
        var review = new NodalOsOnnxSyntheticOcrReadinessReview();
        var inference = SuccessResult();
        var normalized = Normalized(inference);

        var report = review.Evaluate(inference, normalized, modelsVerified: false, sessionsLoaded: true);

        Assert.AreEqual(NodalOsOnnxSyntheticOcrReadinessDecision.BlockedByModelRuntime, report.Decision);
        Assert.IsFalse(report.CanAttemptRedactedCropShadow);
    }

    [TestMethod]
    public void Readiness_Blocks_IfSessionsFail()
    {
        var review = new NodalOsOnnxSyntheticOcrReadinessReview();
        var inference = SuccessResult();
        var normalized = Normalized(inference);

        var report = review.Evaluate(inference, normalized, modelsVerified: true, sessionsLoaded: false);

        Assert.AreEqual(NodalOsOnnxSyntheticOcrReadinessDecision.BlockedByModelRuntime, report.Decision);
    }

    [TestMethod]
    public void Readiness_HandlesNoTextDetected()
    {
        var review = new NodalOsOnnxSyntheticOcrReadinessReview();
        var inference = NoTextResult();
        var normalized = Normalized(inference);

        var report = review.Evaluate(inference, normalized, modelsVerified: true, sessionsLoaded: true);

        Assert.AreEqual(NodalOsOnnxSyntheticOcrReadinessDecision.ReadyForMoreSyntheticFixtures, report.Decision);
        Assert.IsFalse(report.CanAttemptRedactedCropShadow);
        Assert.IsTrue(report.CanContinueWithMoreFixtures);
    }

    [TestMethod]
    public void Readiness_Blocks_IfRawPersistence()
    {
        var review = new NodalOsOnnxSyntheticOcrReadinessReview();
        var inference = RawPersistedResult();
        var normalized = Normalized(inference);

        var report = review.Evaluate(inference, normalized, modelsVerified: true, sessionsLoaded: true);

        Assert.AreEqual(NodalOsOnnxSyntheticOcrReadinessDecision.BlockedByEvidenceRisk, report.Decision);
    }

    [TestMethod]
    public void Readiness_CanDecide_ReadyForRedactedCropShadow()
    {
        var review = new NodalOsOnnxSyntheticOcrReadinessReview();
        var inference = SuccessResult();
        var normalized = Normalized(inference);

        var report = review.Evaluate(inference, normalized, modelsVerified: true, sessionsLoaded: true);

        Assert.AreEqual(NodalOsOnnxSyntheticOcrReadinessDecision.ReadyForRedactedCropShadow, report.Decision);
        Assert.IsTrue(report.CanAttemptRedactedCropShadow);
    }

    [TestMethod]
    public void Readiness_Preserves_NoAuthority_NoSaas_NoProduction()
    {
        var review = new NodalOsOnnxSyntheticOcrReadinessReview();
        var inference = SuccessResult();
        var normalized = Normalized(inference);

        var report = review.Evaluate(inference, normalized, modelsVerified: true, sessionsLoaded: true);

        Assert.IsTrue(report.NoAuthority);
        Assert.IsTrue(report.NoSaas);
        Assert.IsTrue(report.ProductionPublicOcrBlocked);
        Assert.IsTrue(report.NoRawPersistence);
    }

    [TestMethod]
    public void Report_And_Artifact_Exist()
    {
        var repoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug")
            ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
            : AppDomain.CurrentDomain.BaseDirectory;

        Assert.IsTrue(File.Exists(Path.Combine(repoRoot, "docs", "reports", "onnx-synthetic-redacted-crop-ocr-run-m205.md")));
        Assert.IsTrue(File.Exists(Path.Combine(repoRoot, "docs", "audits", "claude-onnx-synthetic-ocr-run-audit-m205.md")));
        Assert.IsTrue(File.Exists(Path.Combine(repoRoot, "artifacts", "ocr-vision-onnx", "m205", "onnx-synthetic-ocr-run-summary.json")));
    }
}
