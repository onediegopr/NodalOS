using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OnnxOcrPreProcessing")]
[TestCategory("OnnxOcrPostProcessing")]
[TestCategory("OnnxOcrDetectorPostProcessor")]
[TestCategory("OnnxOcrRecognizerPostProcessor")]
[TestCategory("OnnxOcrNoAuthority")]
[TestCategory("OcrVisionActivationGate")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("BrowserGroundingSnapshot")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsOnnxOcrPrePostProcessingM198Tests
{
    private static NodalOsPixelRedactionResult ValidRedaction(int width, int height) =>
        new(
            $"redact-{Guid.NewGuid():N}",
            NodalOsPixelRedactionDecision.RedactedPixels,
            OriginalImageHash: "hash-original",
            RedactedImageHash: "hash-redacted",
            MaskRegionsApplied: new List<NodalOsPixelRedactionMask>(),
            VerificationStatus: NodalOsPixelRedactionVerificationStatus.Verified,
            SafeForOcr: true,
            SafeForPersistence: true,
            OriginalRawPersisted: false,
            NoAuthority: true,
            Findings: new List<NodalOsPixelRedactionFinding>(),
            EvidenceRefs: new List<string>(),
            Evidence: new NodalOsPixelRedactionEvidence(
                $"ev-{Guid.NewGuid():N}",
                "hash-original",
                "hash-redacted",
                OriginalRawPersisted: false,
                new List<string>(),
                "synthetic redacted crop",
                Redacted: true),
            Redacted: true);

    [TestMethod]
    public void ImagePreProcessor_RejectsMissingRedaction()
    {
        var preprocessor = new NodalOsOnnxOcrImagePreProcessor();
        var result = preprocessor.Prepare(
            new byte[4],
            1,
            1,
            pixelRedactionResult: null,
            NodalOsOcrVisionSensitivity.Low,
            allowFullScreen: false,
            allowRawPersistence: false);

        Assert.AreEqual(NodalOsOnnxOcrPreProcessingStatus.MissingRedaction, result.Status);
    }

    [TestMethod]
    public void ImagePreProcessor_RejectsRawPersistence()
    {
        var preprocessor = new NodalOsOnnxOcrImagePreProcessor();
        var redaction = ValidRedaction(2, 2);
        var result = preprocessor.Prepare(
            new byte[16],
            2,
            2,
            redaction,
            NodalOsOcrVisionSensitivity.Low,
            allowFullScreen: false,
            allowRawPersistence: true);

        Assert.AreEqual(NodalOsOnnxOcrPreProcessingStatus.BlockedRawPersistence, result.Status);
    }

    [TestMethod]
    public void ImagePreProcessor_RejectsFullScreen()
    {
        var preprocessor = new NodalOsOnnxOcrImagePreProcessor();
        var redaction = ValidRedaction(2, 2);
        var result = preprocessor.Prepare(
            new byte[16],
            2,
            2,
            redaction,
            NodalOsOcrVisionSensitivity.Low,
            allowFullScreen: true,
            allowRawPersistence: false);

        Assert.AreEqual(NodalOsOnnxOcrPreProcessingStatus.BlockedFullScreen, result.Status);
    }

    [TestMethod]
    public void ImagePreProcessor_RejectsSensitive()
    {
        var preprocessor = new NodalOsOnnxOcrImagePreProcessor();
        var redaction = ValidRedaction(2, 2);
        var result = preprocessor.Prepare(
            new byte[16],
            2,
            2,
            redaction,
            NodalOsOcrVisionSensitivity.SensitiveSurface,
            allowFullScreen: false,
            allowRawPersistence: false);

        Assert.AreEqual(NodalOsOnnxOcrPreProcessingStatus.BlockedSensitive, result.Status);
    }

    [TestMethod]
    public void ImagePreProcessor_ReturnsNormalizedRgba_ForSyntheticCrop()
    {
        var preprocessor = new NodalOsOnnxOcrImagePreProcessor();
        var redaction = ValidRedaction(2, 2);
        var image = new byte[16] { 255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255 };

        var result = preprocessor.Prepare(
            image,
            2,
            2,
            redaction,
            NodalOsOcrVisionSensitivity.Low,
            allowFullScreen: false,
            allowRawPersistence: false);

        Assert.AreEqual(NodalOsOnnxOcrPreProcessingStatus.Success, result.Status);
        Assert.AreEqual(4, result.Channels);
        Assert.AreEqual(2, result.Width);
        Assert.AreEqual(2, result.Height);
        Assert.AreEqual(16, result.NormalizedData.Length);
        Assert.AreEqual(1.0f, result.NormalizedData[0], 0.01f);
    }

    [TestMethod]
    public void DetectorPreProcessor_ProducesExpectedTensorMetadata()
    {
        var img = new NodalOsOnnxOcrImagePreProcessingResult(
            $"img-{Guid.NewGuid():N}",
            NodalOsOnnxOcrPreProcessingStatus.Success,
            new byte[4 * 4 * 4].Select(b => 0.5f).ToArray(),
            1,
            4,
            2,
            2,
            ScaleX: 1,
            ScaleY: 1,
            PadLeft: 0,
            PadTop: 0,
            NodalOsOnnxOcrImageFormat.RawRgba32,
            2,
            2,
            true,
            true,
            "ok");

        var preprocessor = new NodalOsOnnxOcrDetectorPreProcessor();
        var result = preprocessor.Prepare(img, targetWidth: 32, targetHeight: 32);

        Assert.AreEqual(NodalOsOnnxOcrPreProcessingStatus.Success, result.Status);
        Assert.AreEqual(4, result.InputShape.Length);
        Assert.AreEqual(1, result.InputShape[0]);
        Assert.AreEqual(3, result.InputShape[1]);
        Assert.AreEqual(32, result.InputShape[2]);
        Assert.AreEqual(32, result.InputShape[3]);
        Assert.AreEqual(1 * 3 * 32 * 32, result.InputTensor.Length);
        Assert.IsTrue(result.Scale > 0);
    }

    [TestMethod]
    public void DetectorPostProcessor_RejectsMalformedOutput()
    {
        var postprocessor = new NodalOsOnnxOcrDetectorPostProcessor();
        var result = postprocessor.Decode(
            new float[0],
            new int[0],
            cropWidth: 64,
            cropHeight: 64);

        Assert.AreEqual(NodalOsOnnxOcrPostProcessingStatus.InvalidOutput, result.Status);
    }

    [TestMethod]
    public void DetectorPostProcessor_MapsFixtureBoxes()
    {
        var postprocessor = new NodalOsOnnxOcrDetectorPostProcessor();
        var h = 32;
        var w = 32;
        var output = new float[h * w];
        // Create a 4x4 high-probability region at center.
        for (var y = 10; y < 14; y++)
        {
            for (var x = 10; x < 14; x++)
            {
                output[y * w + x] = 0.9f;
            }
        }

        var result = postprocessor.Decode(output, new[] { 1, 1, h, w }, cropWidth: 320, cropHeight: 320, threshold: 0.5f);

        Assert.AreEqual(NodalOsOnnxOcrPostProcessingStatus.Success, result.Status);
        Assert.IsTrue(result.TextBoxes.Count > 0);
        var box = result.TextBoxes[0];
        Assert.AreEqual(8, box.Polygon.Count);
        Assert.IsTrue(box.CropX >= 0);
        Assert.IsTrue(box.CropY >= 0);
        Assert.IsTrue(box.Valid);
    }

    [TestMethod]
    public void DetectorPostProcessor_UnsupportedShape_ReturnsUnsupportedModelShape()
    {
        var postprocessor = new NodalOsOnnxOcrDetectorPostProcessor();
        var result = postprocessor.Decode(
            new float[8],
            new[] { 2, 2, 2 },
            cropWidth: 64,
            cropHeight: 64);

        Assert.AreEqual(NodalOsOnnxOcrPostProcessingStatus.UnsupportedModelShape, result.Status);
    }

    [TestMethod]
    public void CharacterDictionary_Loads_And_DecodesCtc()
    {
        var dict = new NodalOsOnnxOcrCharacterDictionary().Load("digits", "digits");
        Assert.AreEqual(10, dict.Characters.Count);
        Assert.AreEqual(10, dict.BlankIndex);

        var sequence = new[] { 1, 1, 2, 2, 10, 3, 3, 3, 4 };
        var text = dict.DecodeCtc(sequence);
        Assert.AreEqual("1234", text);
    }

    [TestMethod]
    public void RecognizerPostProcessor_DecodesFixtureCtc()
    {
        var dictionary = new NodalOsOnnxOcrCharacterDictionary().Load("digits", "digits");
        var postprocessor = new NodalOsOnnxOcrRecognizerPostProcessor(dictionary);

        var timeSteps = 10;
        var vocabSize = 11; // 0-9 digits + blank
        var output = new float[timeSteps * vocabSize];
        // Emit class 1 for first 3 steps, blank, class 2 for next 3, blank, class 3...
        for (var t = 0; t < 3; t++) output[t * vocabSize + 1] = 0.9f;
        for (var t = 3; t < 4; t++) output[t * vocabSize + 10] = 0.9f;
        for (var t = 4; t < 7; t++) output[t * vocabSize + 2] = 0.9f;
        for (var t = 7; t < 8; t++) output[t * vocabSize + 10] = 0.9f;
        for (var t = 8; t < 10; t++) output[t * vocabSize + 3] = 0.9f;

        var result = postprocessor.Decode(output, new[] { timeSteps, vocabSize }, confidenceThreshold: 0.5);

        Assert.AreEqual(NodalOsOnnxOcrPostProcessingStatus.Success, result.Status);
        Assert.AreEqual("123", result.Candidates[0].Text);
    }

    [TestMethod]
    public void RecognizerPostProcessor_LowConfidence_RequiresHumanReview()
    {
        var dictionary = new NodalOsOnnxOcrCharacterDictionary().Load("digits", "digits");
        var postprocessor = new NodalOsOnnxOcrRecognizerPostProcessor(dictionary);

        var timeSteps = 5;
        var vocabSize = 11;
        var output = new float[timeSteps * vocabSize];
        for (var t = 0; t < timeSteps; t++) output[t * vocabSize + 1] = 0.3f; // low confidence

        var result = postprocessor.Decode(output, new[] { timeSteps, vocabSize }, confidenceThreshold: 0.8);

        Assert.AreEqual(NodalOsOnnxOcrPostProcessingStatus.RequiresHumanReview, result.Status);
        Assert.IsTrue(result.Candidates[0].RequiresHumanReview);
    }

    [TestMethod]
    public void RecognizerPostProcessor_UnsupportedShape_ReturnsUnsupportedModelShape()
    {
        var postprocessor = new NodalOsOnnxOcrRecognizerPostProcessor();
        var result = postprocessor.Decode(
            new float[8],
            new[] { 2, 2, 2 },
            confidenceThreshold: 0.5);

        Assert.AreEqual(NodalOsOnnxOcrPostProcessingStatus.UnsupportedModelShape, result.Status);
    }

    [TestMethod]
    public void PrePostProcessingReport_Exists()
    {
        var root = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug")
            ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
            : AppDomain.CurrentDomain.BaseDirectory;

        Assert.IsTrue(File.Exists(Path.Combine(root, "docs", "reports", "onnx-ocr-pre-post-processing-readiness-m198.md")));
    }
}
