using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("QaWindowCaptureHardening")]
[TestCategory("QaWindowHost")]
[TestCategory("RealQaWindowRegion")]
[TestCategory("QaWindowRegion")]
[TestCategory("WindowRegionCapture")]
[TestCategory("InternalControlledScreenRegion")]
[TestCategory("ScreenRegionFixture")]
[TestCategory("RegionProvenance")]
[TestCategory("ScreenRegionCapture")]
[TestCategory("RatioPreserving")]
[TestCategory("OnnxOutOfProcessGuard")]
[TestCategory("OnnxOcrProbeRunner")]
[TestCategory("OnnxModelInventory")]
[TestCategory("OnnxModelVerification")]
[TestCategory("OnnxModelReadiness")]
[TestCategory("OfficialSpaceToken")]
[TestCategory("PaddleOcrSpaceToken")]
[TestCategory("PaddleOcrExtraClass")]
[TestCategory("DecodePolicy")]
[TestCategory("CtcDecodePolicy")]
[TestCategory("RecognizerDictionaryPair")]
[TestCategory("RecognizerClassSemantics")]
[TestCategory("RecognizerTokenPolicy")]
[TestCategory("OcrDictionary")]
[TestCategory("CtcDecoder")]
[TestCategory("DictionaryCompatibility")]
[TestCategory("RecognizerRuntimeCompatibility")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("PixelRedaction")]
[TestCategory("OcrRedactionPrecondition")]
public sealed class NodalOsQaWindowCaptureHardeningM316M318Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void QaWindowHost_SupportsRenderingConfigurationAndDpiAuditMetadata()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot, "tools", "qa-window-host", "Program.cs"));

        StringAssert.Contains(source, "\"font-family\"");
        StringAssert.Contains(source, "\"font-size\"");
        StringAssert.Contains(source, "\"font-style\"");
        StringAssert.Contains(source, "\"text-rendering-hint\"");
        StringAssert.Contains(source, "\"baseline-shift-y\"");
        StringAssert.Contains(source, "deviceDpi");
        StringAssert.Contains(source, "dpiScaleX");
        StringAssert.Contains(source, "clientBounds");
        StringAssert.Contains(source, "captureCoordinateMode");
    }

    [TestMethod]
    public void Artifact_RecordsDeterministicBestConfig_AndSafetyFlags()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m318",
            "paddleocr-qa-window-capture-hardening-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M316-M318", root.GetProperty("milestone").GetString());
        Assert.AreEqual("READY_FOR_INTERNAL_LOW_RISK_SCREEN_OCR_OBSERVATION", root.GetProperty("readinessDecision").GetString());
        Assert.AreEqual("real-qa-window-region", root.GetProperty("captureMode").GetString());
        Assert.AreEqual("arial-92-bold-antialias-expanded", root.GetProperty("bestRenderingConfiguration").GetString());
        Assert.IsTrue(root.GetProperty("dpiAuditAttempted").GetBoolean());
        Assert.IsTrue(root.GetProperty("windowBoundsValidated").GetBoolean());
        Assert.IsTrue(root.GetProperty("regionBoundsValidated").GetBoolean());
        Assert.IsTrue(root.GetProperty("livenessValidated").GetBoolean());
        Assert.IsTrue(root.GetProperty("hostProcessCleanedUp").GetBoolean());
        Assert.IsTrue(root.GetProperty("realQaWindowRegionUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("simulatedWindowRegionUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("recognizerSoftmaxReapplied").GetBoolean());
        Assert.IsTrue(root.GetProperty("officialSpacePolicy").GetBoolean());
        Assert.AreEqual(1, root.GetProperty("calibratedTotalEditDistance").GetInt32());
        Assert.AreEqual(2, root.GetProperty("calibratedExactMatches").GetInt32());
        Assert.IsTrue(root.GetProperty("successCriteriaMet").GetBoolean());
    }

    [TestMethod]
    public void Artifact_RecordsCaptureMetadata_AndPipelineResult()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m318",
            "paddleocr-qa-window-capture-hardening-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual(144, root.GetProperty("deviceDpi").GetInt32());
        Assert.AreEqual(1.5d, root.GetProperty("dpiScaleX").GetDouble(), 0.0001d);
        Assert.AreEqual(1.5d, root.GetProperty("dpiScaleY").GetDouble(), 0.0001d);
        Assert.AreEqual("[B,T,C]", root.GetProperty("recognizerOutputLayout").GetString());
        Assert.AreEqual("RatioPreservingRightPad", root.GetProperty("recognizerResizeMode").GetString());
        Assert.IsTrue(root.GetProperty("outOfProcessGuardUsed").GetBoolean());
        Assert.IsTrue(root.GetProperty("parentSurvived").GetBoolean());

        var results = root.GetProperty("results");
        Assert.AreEqual(3, results.GetArrayLength());
        Assert.IsTrue(results.EnumerateArray().All(r => string.Equals(r.GetProperty("CaptureMode").GetString(), "real-qa-window-region", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Report_Artifact_Audit_Adr_Exist_ForM318()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "paddleocr-qa-window-capture-hardening-m318.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m318", "paddleocr-qa-window-capture-hardening-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "paddleocr-qa-window-capture-hardening-audit-m318.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "paddleocr-qa-window-capture-hardening-policy-m316-m318.md")));
    }

    [TestMethod]
    public void Git_DoesNotTrackOnnxModels_ForQaWindowCaptureHardening()
    {
        var psi = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = RepoRoot
        };
        psi.ArgumentList.Add("ls-files");
        psi.ArgumentList.Add("*.onnx");

        using var process = Process.Start(psi);
        Assert.IsNotNull(process);
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.IsTrue(process.WaitForExit(10000), stderr);
        Assert.AreEqual(0, process.ExitCode, stderr);
        Assert.AreEqual(string.Empty, stdout.Trim());
    }
}
