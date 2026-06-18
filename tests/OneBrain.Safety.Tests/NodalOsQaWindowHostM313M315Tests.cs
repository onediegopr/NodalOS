using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("QaWindowHost")]
[TestCategory("RealQaWindowRegion")]
[TestCategory("QaWindowRegion")]
[TestCategory("WindowRegionCapture")]
[TestCategory("InternalControlledScreenRegion")]
[TestCategory("ScreenRegionFixture")]
[TestCategory("RegionProvenance")]
[TestCategory("PreprocessingAlignment")]
[TestCategory("RatioPreserving")]
[TestCategory("DetectorToRecognizer")]
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
public sealed class NodalOsQaWindowHostM313M315Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void QaWindowHost_ProjectAndSolutionEntriesExist()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "tools", "qa-window-host", "OneBrain.Tools.QaWindowHost.csproj")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "tools", "qa-window-host", "Program.cs")));

        var solution = File.ReadAllText(Path.Combine(RepoRoot, "OneBrain.slnx"));
        StringAssert.Contains(solution, "tools/qa-window-host/OneBrain.Tools.QaWindowHost.csproj");
    }

    [TestMethod]
    public void RealQaWindowRegionProvenance_StillRejectsSimulatedWindowAsReal()
    {
        var evaluator = new NodalOsRealQaWindowRegionCaptureProvenanceEvaluator();
        var simulated = BaseRealQaWindow() with
        {
            CaptureMode = NodalOsRealQaWindowRegionCaptureMode.SimulatedWindowRegion
        };

        var result = evaluator.Evaluate(simulated);

        Assert.AreEqual(NodalOsRealQaWindowRegionCaptureDecision.RejectedSimulatedWindow, result.Decision);
        Assert.IsFalse(result.RealQaWindowRegionUsed);
        Assert.IsTrue(result.SimulatedWindowRegionUsed);
    }

    [TestMethod]
    public void Artifact_RecordsRealQaWindowHostCaptureAndHardeningDecision()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m315",
            "paddleocr-qa-window-host-real-region-capture-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M313-M315", root.GetProperty("milestone").GetString());
        Assert.AreEqual("READY_FOR_QA_WINDOW_CAPTURE_HARDENING", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("qaWindowHostAttempted").GetBoolean());
        Assert.IsTrue(root.GetProperty("qaWindowHostCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("realQaWindowRegionAttempted").GetBoolean());
        Assert.IsTrue(root.GetProperty("realQaWindowRegionUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("simulatedWindowRegionUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("fullScreenUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("realDocumentUsed").GetBoolean());
        Assert.IsTrue(root.GetProperty("hostProcessCleanedUp").GetBoolean());
        Assert.IsTrue(root.GetProperty("outOfProcessGuardUsed").GetBoolean());
        Assert.IsTrue(root.GetProperty("parentSurvived").GetBoolean());
        Assert.IsTrue(root.GetProperty("officialSpacePolicy").GetBoolean());
        Assert.IsFalse(root.GetProperty("recognizerSoftmaxReapplied").GetBoolean());
        Assert.AreEqual(3, root.GetProperty("fixturesAccepted").GetInt32());
        Assert.AreEqual(1, root.GetProperty("exactMatches").GetInt32());
        Assert.AreEqual(2, root.GetProperty("totalEditDistance").GetInt32());
        Assert.IsFalse(root.GetProperty("successCriteriaMet").GetBoolean());
    }

    [TestMethod]
    public void Report_Artifact_Audit_Adr_Exist_ForM315()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "paddleocr-qa-window-host-real-region-capture-m315.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m315", "paddleocr-qa-window-host-real-region-capture-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "paddleocr-qa-window-host-real-region-capture-audit-m315.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "paddleocr-qa-window-host-real-region-capture-policy-m313-m315.md")));
    }

    [TestMethod]
    public void Git_DoesNotTrackOnnxModels_ForQaWindowHostGate()
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

    private static NodalOsRealQaWindowRegionCaptureProvenance BaseRealQaWindow() => new(
        "qa-real-window-pvc-wall",
        "PVC WALL",
        NodalOsRealQaWindowRegionCaptureMode.RealQaWindowRegion,
        ExpectedWindowTitle: "NODAL OS OCR QA Window",
        ObservedWindowTitle: "NODAL OS OCR QA Window",
        ExpectedProcessOrSource: "OneBrain.Tools.QaWindowHost",
        ObservedProcessOrSource: "OneBrain.Tools.QaWindowHost",
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
