using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OnnxOcrResultNormalizer")]
[TestCategory("OnnxOcrEvidence")]
[TestCategory("OnnxOcrTimelineAdapter")]
[TestCategory("OnnxOcrNoAuthority")]
[TestCategory("OcrVisionActivationGate")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("BrowserGroundingSnapshot")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsOnnxOcrResultNormalizationM204Tests
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

    private static NodalOsOnnxOcrSyntheticInferenceResult LowConfidenceResult()
    {
        var detection = new NodalOsOnnxOcrDetectionInferenceResult(
            $"det-{Guid.NewGuid():N}",
            NodalOsOnnxOcrInferenceStatus.Success,
            new List<NodalOsOnnxOcrTextBox>
            {
                new("box-0", new List<float> { 0, 0, 10, 0, 10, 10, 0, 10 }, 0.55, 0, 0, 10, 10, true)
            },
            100,
            "paddleocr-det-onnx",
            "v4",
            "detection completed");

        var recognition = new NodalOsOnnxOcrRecognitionInferenceResult(
            $"rec-{Guid.NewGuid():N}",
            NodalOsOnnxOcrInferenceStatus.LowConfidence,
            new List<NodalOsOnnxOcrRecognitionCandidate>
            {
                new($"cand-{Guid.NewGuid():N}", "x", 0.3, new List<int> { 1 }, true, true)
            },
            80,
            "paddleocr-rec-onnx",
            "v4",
            "low confidence");

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
            "low confidence run");

        return new NodalOsOnnxOcrSyntheticInferenceResult(
            $"inf-{Guid.NewGuid():N}",
            NodalOsOnnxOcrInferenceStatus.LowConfidence,
            true,
            detection,
            new[] { recognition },
            evidence,
            CallsRealOcr: true,
            CallsSaas: false,
            RawPersisted: false,
            NoAuthority: true,
            RequiresHumanReview: true,
            TimeSpan.FromMilliseconds(200),
            Array.Empty<string>());
    }

    [TestMethod]
    public void Normalizer_MapsDetectionRecognition_ToTextBlocks()
    {
        var normalizer = new NodalOsOnnxOcrResultNormalizer();
        var inference = SuccessResult();
        var redaction = CleanRedaction();

        var normalized = normalizer.Normalize(inference, redaction);

        Assert.AreEqual(NodalOsOnnxOcrNormalizedStatus.Completed, normalized.Status);
        Assert.AreEqual(1, normalized.TextBlocks.Count);
        Assert.AreEqual("box-0", normalized.TextBlocks[0].BlockId);
        Assert.IsTrue(normalized.TextBlocks[0].Redacted);
        Assert.IsFalse(normalized.CanApproveAction);
        Assert.IsFalse(normalized.CanClick);
        Assert.IsFalse(normalized.CanSubmit);
    }

    [TestMethod]
    public void Normalizer_NoTextDetected_Honestly()
    {
        var normalizer = new NodalOsOnnxOcrResultNormalizer();
        var inference = SuccessResult();
        inference = inference with
        {
            Status = NodalOsOnnxOcrInferenceStatus.NoTextDetected,
            DetectionResult = inference.DetectionResult with { Status = NodalOsOnnxOcrInferenceStatus.NoTextDetected, TextBoxes = Array.Empty<NodalOsOnnxOcrTextBox>() },
            RecognitionResults = Array.Empty<NodalOsOnnxOcrRecognitionInferenceResult>()
        };

        var normalized = normalizer.Normalize(inference, CleanRedaction());

        Assert.AreEqual(NodalOsOnnxOcrNormalizedStatus.NoTextDetected, normalized.Status);
        Assert.AreEqual(0, normalized.TextBlocks.Count);
    }

    [TestMethod]
    public void Normalizer_LowConfidence_RequiresHumanReview()
    {
        var normalizer = new NodalOsOnnxOcrResultNormalizer();
        var inference = LowConfidenceResult();

        var normalized = normalizer.Normalize(inference, CleanRedaction());

        Assert.AreEqual(NodalOsOnnxOcrNormalizedStatus.RequiresHumanReview, normalized.Status);
        Assert.IsTrue(normalized.RequiresHumanReview);
        Assert.IsTrue(normalized.TextBlocks[0].LowConfidence);
    }

    [TestMethod]
    public void EvidenceSummary_DoesNotContainRawImage()
    {
        var normalizer = new NodalOsOnnxOcrResultNormalizer();
        var inference = SuccessResult();
        var redaction = CleanRedaction();

        var summary = normalizer.BuildEvidenceSummary(inference, redaction);

        Assert.IsFalse(summary.OriginalRawPersisted);
        Assert.IsFalse(summary.CallsSaas);
        Assert.IsFalse(summary.ProductionEnabled);
        Assert.IsTrue(summary.NoAuthority);
    }

    [TestMethod]
    public void TimelineAdapter_CreatesCard_WithNoAuthority()
    {
        var normalizer = new NodalOsOnnxOcrResultNormalizer();
        var inference = SuccessResult();
        var normalized = normalizer.Normalize(inference, CleanRedaction());

        var card = normalizer.AdaptToTimeline(normalized);

        Assert.IsNotNull(card);
        Assert.AreEqual(normalized.ResultId, card.SourceResultId);
        Assert.IsTrue(card.NoAuthority);
        Assert.IsFalse(card.OriginalRawPersisted);
        Assert.IsFalse(card.CallsSaas);
    }

    [TestMethod]
    public void Normalizer_Preserves_NoAuthority_NoSaas_NoProduction()
    {
        var normalizer = new NodalOsOnnxOcrResultNormalizer();
        var inference = SuccessResult();

        var normalized = normalizer.Normalize(inference, CleanRedaction());

        Assert.IsTrue(normalized.NoAuthority);
        Assert.AreEqual(NodalOsOcrAuthorityFlag.NoAuthority, normalized.AuthorityFlag);
        Assert.IsFalse(normalized.CallsExternalApi);
        Assert.IsFalse(normalized.CallsSaas);
        Assert.IsFalse(normalized.ProductionEnabled);
    }
}
