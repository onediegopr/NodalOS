using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("RealQaWindowRegion")]
[TestCategory("QaWindowRegion")]
[TestCategory("WindowRegionCapture")]
[TestCategory("InternalControlledScreenRegion")]
[TestCategory("ScreenRegionFixture")]
[TestCategory("RegionProvenance")]
[TestCategory("ScreenRegionCapture")]
[TestCategory("InternalControlledRealImage")]
[TestCategory("RealImageFixture")]
[TestCategory("FixtureProvenance")]
[TestCategory("PreprocessingAlignment")]
[TestCategory("RatioPreserving")]
[TestCategory("PipelineCalibrationAudit")]
[TestCategory("CropCalibration")]
[TestCategory("DetectorRecognizerCalibration")]
[TestCategory("DetectorModelRecovery")]
[TestCategory("SyntheticDetectorToRecognizer")]
[TestCategory("DetectorToRecognizer")]
[TestCategory("SyntheticImageRecognizer")]
[TestCategory("SyntheticCrop")]
[TestCategory("OnnxSyntheticRecognizer")]
[TestCategory("PaddleOcrSynthetic")]
[TestCategory("OfficialSpaceToken")]
[TestCategory("PaddleOcrSpaceToken")]
[TestCategory("PaddleOcrExtraClass")]
[TestCategory("ExtraClassArgmax")]
[TestCategory("DecodePolicy")]
[TestCategory("CtcDecodePolicy")]
[TestCategory("RecognizerDictionaryPair")]
[TestCategory("RecognizerClassSemantics")]
[TestCategory("RecognizerTokenPolicy")]
[TestCategory("OcrDictionary")]
[TestCategory("CtcDecoder")]
[TestCategory("DictionaryCompatibility")]
[TestCategory("RecognizerRuntimeCompatibility")]
[TestCategory("OnnxOutOfProcessGuard")]
[TestCategory("OnnxOcrProbeRunner")]
[TestCategory("OnnxModelInventory")]
[TestCategory("OnnxModelVerification")]
[TestCategory("OnnxModelReadiness")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("PixelRedaction")]
[TestCategory("OcrRedactionPrecondition")]
public sealed class NodalOsRealQaWindowRegionM310M312Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void RealQaWindowRegionProvenance_AcceptsLiveVisibleBoundedWindow()
    {
        var result = new NodalOsRealQaWindowRegionCaptureProvenanceEvaluator().Evaluate(BaseRealQaWindow());

        Assert.AreEqual(NodalOsRealQaWindowRegionCaptureDecision.AcceptedForRealQaWindowCapture, result.Decision);
        Assert.IsTrue(result.AllowedForRealQaWindowCapture);
        Assert.IsTrue(result.RealQaWindowRegionUsed);
        Assert.IsTrue(result.WindowLiveAndVisible);
        Assert.IsTrue(result.RegionInsideWindow);
    }

    [TestMethod]
    public void RealQaWindowRegionProvenance_RejectsSimulationAndUnsafeCases()
    {
        var evaluator = new NodalOsRealQaWindowRegionCaptureProvenanceEvaluator();

        Assert.AreEqual(
            NodalOsRealQaWindowRegionCaptureDecision.RejectedSimulatedWindow,
            evaluator.Evaluate(BaseRealQaWindow() with { CaptureMode = NodalOsRealQaWindowRegionCaptureMode.SimulatedWindowRegion }).Decision);

        Assert.AreEqual(
            NodalOsRealQaWindowRegionCaptureDecision.RejectedUnknownWindow,
            evaluator.Evaluate(BaseRealQaWindow() with { ObservedWindowTitle = "Other" }).Decision);

        Assert.AreEqual(
            NodalOsRealQaWindowRegionCaptureDecision.RejectedProcessMismatch,
            evaluator.Evaluate(BaseRealQaWindow() with { ObservedProcessOrSource = "other-process" }).Decision);

        Assert.AreEqual(
            NodalOsRealQaWindowRegionCaptureDecision.RejectedFullScreen,
            evaluator.Evaluate(BaseRealQaWindow() with { ContainsFullScreen = true }).Decision);

        Assert.AreEqual(
            NodalOsRealQaWindowRegionCaptureDecision.RejectedRegionOutsideWindow,
            evaluator.Evaluate(BaseRealQaWindow() with { RegionBounds = new NodalOsScreenRegionBounds(700, 64, 200, 160) }).Decision);

        Assert.AreEqual(
            NodalOsRealQaWindowRegionCaptureDecision.RejectedInvalidBounds,
            evaluator.Evaluate(BaseRealQaWindow() with { RegionBounds = new NodalOsScreenRegionBounds(80, 64, 0, 160) }).Decision);

        Assert.AreEqual(
            NodalOsRealQaWindowRegionCaptureDecision.RejectedLivenessOrVisibility,
            evaluator.Evaluate(BaseRealQaWindow() with { LivenessConfirmed = false }).Decision);

        Assert.AreEqual(
            NodalOsRealQaWindowRegionCaptureDecision.RejectedSensitiveRegion,
            evaluator.Evaluate(BaseRealQaWindow() with { Sensitive = true }).Decision);

        Assert.AreEqual(
            NodalOsRealQaWindowRegionCaptureDecision.RejectedDocumentRegion,
            evaluator.Evaluate(BaseRealQaWindow() with { ContainsDocumentData = true }).Decision);

        Assert.AreEqual(
            NodalOsRealQaWindowRegionCaptureDecision.RejectedCredentialData,
            evaluator.Evaluate(BaseRealQaWindow() with { ContainsCredentialOrPasswordData = true }).Decision);
    }

    [TestMethod]
    public void Artifact_CapturesRealQaWindowBlockedBeforeCapture()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m312",
            "paddleocr-real-qa-window-region-capture-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M310-M312", root.GetProperty("milestone").GetString());
        Assert.AreEqual("BLOCKED_BY_REAL_QA_WINDOW_CAPTURE_TECHNIQUE", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("realQaWindowRegionAttempted").GetBoolean());
        Assert.IsFalse(root.GetProperty("realQaWindowRegionUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("simulatedWindowRegionUsed").GetBoolean());
        Assert.AreEqual("blocked-before-real-capture", root.GetProperty("captureMode").GetString());
        Assert.IsFalse(root.GetProperty("fullScreenUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("realDocumentUsed").GetBoolean());
        Assert.IsTrue(root.GetProperty("officialSpacePolicy").GetBoolean());
        Assert.IsFalse(root.GetProperty("recognizerSoftmaxReapplied").GetBoolean());
        Assert.IsFalse(root.GetProperty("successCriteriaMet").GetBoolean());
    }

    [TestMethod]
    public void ProbeRunner_ReportsBlockedBeforeRealQaWindowCapture()
    {
        var runner = Path.Combine(RepoRoot, "tools", "onnx-ocr-probe-runner", "bin", "Debug", "net11.0", "OneBrain.Tools.OnnxOcrProbeRunner.dll");
        if (!File.Exists(runner))
            Assert.Inconclusive("Built runner is not available locally.");

        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = RepoRoot
        };
        psi.ArgumentList.Add(runner);
        psi.ArgumentList.Add("--real-qa-window-region-probe");
        psi.ArgumentList.Add("--repo-root");
        psi.ArgumentList.Add(RepoRoot);

        using var process = Process.Start(psi);
        Assert.IsNotNull(process);
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.IsTrue(process.WaitForExit(30000), stderr);
        Assert.AreEqual(0, process.ExitCode, stderr);

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;
        Assert.AreEqual("BLOCKED_BY_REAL_QA_WINDOW_CAPTURE_TECHNIQUE", root.GetProperty("ReadinessDecision").GetString());
        Assert.AreEqual("blocked-before-real-capture", root.GetProperty("CaptureMode").GetString());
        Assert.IsFalse(root.GetProperty("RealQaWindowRegionUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("SimulatedWindowRegionUsed").GetBoolean());
        Assert.IsTrue(root.GetProperty("ParentSurvived").GetBoolean());
    }

    [TestMethod]
    public void Git_DoesNotTrackOnnxModels_ForRealQaWindowGate()
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

    [TestMethod]
    public void Report_Artifact_Audit_Adr_Exist_ForM312()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "paddleocr-real-qa-window-region-capture-fixtures-m312.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m312", "paddleocr-real-qa-window-region-capture-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "paddleocr-real-qa-window-region-capture-audit-m312.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "paddleocr-real-qa-window-region-capture-policy-m310-m312.md")));
    }

    private static NodalOsRealQaWindowRegionCaptureProvenance BaseRealQaWindow() => new(
        "qa-real-window-pvc-wall",
        "PVC WALL",
        NodalOsRealQaWindowRegionCaptureMode.RealQaWindowRegion,
        ExpectedWindowTitle: "NODAL OS OCR QA Window",
        ObservedWindowTitle: "NODAL OS OCR QA Window",
        ExpectedProcessOrSource: "onnx-ocr-probe-runner",
        ObservedProcessOrSource: "onnx-ocr-probe-runner",
        WindowHandleOrSourceId: "qa-window-hwnd-1",
        WindowBounds: new NodalOsScreenRegionBounds(0, 0, 800, 320),
        RegionBounds: new NodalOsScreenRegionBounds(80, 64, 640, 160),
        WindowExists: true,
        WindowVisible: true,
        LivenessConfirmed: true,
        ContainsRealPersonData: false,
        ContainsCustomerData: false,
        ContainsFinancialData: false,
        ContainsDocumentData: false,
        ContainsCredentialOrPasswordData: false,
        ContainsFullScreen: false,
        Sensitive: false,
        Reason: "real QA window liveness fixture");
}
