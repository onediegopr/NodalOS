using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PipelineCalibrationAudit")]
[TestCategory("RenderingFidelity")]
[TestCategory("CropCalibration")]
[TestCategory("DetectorRecognizerCalibration")]
[TestCategory("SyntheticDetectorToRecognizer")]
[TestCategory("DetectorToRecognizer")]
[TestCategory("SyntheticImageRecognizer")]
[TestCategory("SyntheticCrop")]
[TestCategory("OnnxSyntheticRecognizer")]
[TestCategory("PaddleOcrSynthetic")]
[TestCategory("OfficialSpaceToken")]
[TestCategory("RecognizerRuntimeCompatibility")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
public sealed class NodalOsPaddleOcrRecognizerCropPreprocessorM295M297Tests
{
    private const int H = 48;
    private const int W = 320;

    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    // RGBA crop, 4 floats/pixel in [0,1]; gray(x,y) sets R=G=B, A=1.
    private static float[] BuildRgbaCrop(int w, int h, Func<int, int, float> gray)
    {
        var data = new float[w * h * 4];
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var v = gray(x, y);
                var idx = (y * w + x) * 4;
                data[idx + 0] = v;
                data[idx + 1] = v;
                data[idx + 2] = v;
                data[idx + 3] = 1f;
            }
        }

        return data;
    }

    private static float At(float[] tensor, int channel, int x, int y) => tensor[channel * H * W + y * W + x];

    [TestMethod]
    public void NarrowCrop_PreservesAspect_AndRightPadsWithZero()
    {
        // 24x48 white crop, aspect 0.5 -> resizedWidth = ceil(48*0.5) = 24, padded 296 columns.
        var crop = BuildRgbaCrop(24, 48, (_, _) => 1f);
        var result = new NodalOsPaddleOcrRecognizerCropPreprocessor().Prepare(crop, 24, 48);

        Assert.AreEqual(24, result.ResizedWidth);
        Assert.AreEqual(296, result.PaddedColumns);
        Assert.IsTrue(result.AspectRatioPreserved);
        Assert.IsFalse(result.WidthCapped);
        Assert.AreEqual(NodalOsPaddleOcrRecognizerResizeMode.RatioPreservingRightPad, result.Mode);

        // White text region normalizes to +1; right padding stays at 0.0 (PaddleOCR pad value).
        Assert.IsTrue(At(result.Tensor, 0, 5, 24) > 0.9f);
        Assert.AreEqual(0f, At(result.Tensor, 0, 200, 24), 1e-6f);
        Assert.AreEqual(0.0d, result.PadValue);
        Assert.IsTrue(result.NoAuthority);
        Assert.IsTrue(result.NoRawPersistence);
    }

    [TestMethod]
    public void WideCrop_CapsAtMaxWidth_NoPadding()
    {
        // 1920x48 crop, aspect 40 -> ideal width 1920 > 320, so capped at 320, no padding, aspect not preserved.
        var crop = BuildRgbaCrop(1920, 48, (_, _) => 1f);
        var result = new NodalOsPaddleOcrRecognizerCropPreprocessor().Prepare(crop, 1920, 48);

        Assert.AreEqual(320, result.ResizedWidth);
        Assert.AreEqual(0, result.PaddedColumns);
        Assert.IsTrue(result.WidthCapped);
        Assert.IsFalse(result.AspectRatioPreserved);
    }

    [TestMethod]
    public void Normalization_MapsWhiteToPlusOne_AndBlackToMinusOne()
    {
        var white = new NodalOsPaddleOcrRecognizerCropPreprocessor().Prepare(BuildRgbaCrop(64, 48, (_, _) => 1f), 64, 48);
        var black = new NodalOsPaddleOcrRecognizerCropPreprocessor().Prepare(BuildRgbaCrop(64, 48, (_, _) => 0f), 64, 48);

        Assert.IsTrue(At(white.Tensor, 0, 0, 24) > 0.99f);
        Assert.IsTrue(At(black.Tensor, 0, 0, 24) < -0.99f);
        Assert.AreEqual("(pixel/255 - 0.5) / 0.5", white.NormalizationFormula);
    }

    [TestMethod]
    public void StretchMode_FillsFullWidth_WithoutPadding()
    {
        var crop = BuildRgbaCrop(24, 48, (_, _) => 1f);
        var result = new NodalOsPaddleOcrRecognizerCropPreprocessor().Prepare(
            crop, 24, 48, mode: NodalOsPaddleOcrRecognizerResizeMode.StretchToFixedWidth);

        Assert.AreEqual(320, result.ResizedWidth);
        Assert.AreEqual(0, result.PaddedColumns);
        Assert.IsFalse(result.AspectRatioPreserved);
    }

    [TestMethod]
    public void RatioPreserving_DoesNotStretch_WhereasStretchModeDistorts()
    {
        // 100x48 crop: black vertical bar over left 20px (20% of width), rest white.
        var crop = BuildRgbaCrop(100, 48, (x, _) => x < 20 ? 0f : 1f);
        var pre = new NodalOsPaddleOcrRecognizerCropPreprocessor();

        var ratio = pre.Prepare(crop, 100, 48, mode: NodalOsPaddleOcrRecognizerResizeMode.RatioPreservingRightPad);
        var stretch = pre.Prepare(crop, 100, 48, mode: NodalOsPaddleOcrRecognizerResizeMode.StretchToFixedWidth);

        // Aspect-preserving: bar stays in ~first 20 of 100 resized columns; column 50 is white.
        Assert.AreEqual(100, ratio.ResizedWidth);
        Assert.IsTrue(At(ratio.Tensor, 0, 10, 24) < 0f, "ratio: column 10 should be inside the dark bar");
        Assert.IsTrue(At(ratio.Tensor, 0, 50, 24) > 0.5f, "ratio: column 50 should be white (bar not stretched)");

        // Stretch: same bar is widened across ~first 64 of 320 columns; column 50 is now dark.
        Assert.AreEqual(320, stretch.ResizedWidth);
        Assert.IsTrue(At(stretch.Tensor, 0, 50, 24) < 0f, "stretch: column 50 is dark because the bar was widened");
    }

    [TestMethod]
    public void DefaultMode_IsRatioPreserving()
    {
        // Calling without an explicit mode must preserve aspect (the corrected default for the pipeline).
        var crop = BuildRgbaCrop(48, 48, (_, _) => 1f);
        var result = new NodalOsPaddleOcrRecognizerCropPreprocessor().Prepare(crop, 48, 48);

        Assert.AreEqual(NodalOsPaddleOcrRecognizerResizeMode.RatioPreservingRightPad, result.Mode);
        Assert.AreEqual(48, result.ResizedWidth); // square crop -> width == height
        Assert.AreEqual(272, result.PaddedColumns);
    }

    [TestMethod]
    public void Report_Artifact_Adr_Audit_Exist_ForM297()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "paddleocr-synthetic-pipeline-calibration-audit-m297.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "paddleocr-synthetic-pipeline-calibration-decision-m295-m297.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "claude-paddleocr-synthetic-pipeline-calibration-audit-m297.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m297", "paddleocr-synthetic-pipeline-calibration-audit-summary.json")));
    }
}
