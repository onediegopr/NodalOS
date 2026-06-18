using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("LowRiskScreenOcrObservation")]
[TestCategory("OcrObservation")]
[TestCategory("OcrEvidenceEnvelope")]
[TestCategory("QaWindowCaptureHardening")]
[TestCategory("QaWindowHost")]
[TestCategory("RealQaWindowRegion")]
[TestCategory("QaWindowRegion")]
[TestCategory("WindowRegionCapture")]
[TestCategory("InternalControlledScreenRegion")]
[TestCategory("RegionProvenance")]
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
public sealed class NodalOsLowRiskScreenOcrObservationM319M321Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void ObservationRequest_RejectsFullScreen()
    {
        var evaluator = new NodalOsLowRiskOcrObservationEvaluator();
        var request = CreateRequest(fullScreen: true);

        var result = evaluator.Evaluate(request, "ROMA", "ROMA", 1, 0, exactMatch: true, normalizedMatch: false, confidence: 0.99d);

        Assert.AreEqual(NodalOsOcrObservationDecision.RejectedFullScreen, result.PolicyDecision);
        Assert.IsFalse(result.Accepted);
        Assert.IsFalse(result.ActionAllowed);
        Assert.IsTrue(result.NoAuthority);
        Assert.IsTrue(result.EvidenceOnly);
    }

    [TestMethod]
    public void ObservationRequest_RejectsSensitiveDocumentAndCredentialRegions()
    {
        var evaluator = new NodalOsLowRiskOcrObservationEvaluator();

        var sensitive = evaluator.Evaluate(CreateRequest(containsSensitiveData: true), "ROMA", "ROMA", 1, 0, true, false, 0.99d);
        var document = evaluator.Evaluate(CreateRequest(containsDocumentData: true), "ROMA", "ROMA", 1, 0, true, false, 0.99d);
        var credentials = evaluator.Evaluate(CreateRequest(containsCredentials: true), "ROMA", "ROMA", 1, 0, true, false, 0.99d);

        Assert.AreEqual(NodalOsOcrObservationDecision.RejectedSensitiveData, sensitive.PolicyDecision);
        Assert.AreEqual(NodalOsOcrObservationDecision.RejectedDocumentData, document.PolicyDecision);
        Assert.AreEqual(NodalOsOcrObservationDecision.RejectedCredentialData, credentials.PolicyDecision);
    }

    [TestMethod]
    public void ObservationRequest_HasActionsBlocked_AndResultHasNoAuthorityEvidenceOnly()
    {
        var evaluator = new NodalOsLowRiskOcrObservationEvaluator();

        var result = evaluator.Evaluate(CreateRequest(), "12 34", "1234", 1, 0, exactMatch: true, normalizedMatch: false, confidence: 0.81d);

        Assert.AreEqual(NodalOsOcrObservationDecision.AcceptedEvidenceOnly, result.PolicyDecision);
        Assert.IsTrue(result.Accepted);
        Assert.IsFalse(result.ActionAllowed);
        Assert.IsTrue(result.NoAuthority);
        Assert.IsTrue(result.EvidenceOnly);
        Assert.IsTrue(result.OfficialSpacePolicy);
        Assert.IsFalse(result.SoftmaxReapplied);
    }

    [TestMethod]
    public void Envelope_RecordsRegionCaptureProvenance_AndOfficialSpacePolicy()
    {
        var result = new NodalOsLowRiskOcrObservationEvaluator().Evaluate(
            CreateRequest(expectedText: "ROMA"),
            "ROMA",
            "ROMA",
            detectorBoxesCount: 1,
            editDistance: 0,
            exactMatch: true,
            normalizedMatch: false,
            confidence: 0.99d);
        var envelope = new NodalOsOcrEvidenceEnvelope(
            "obs-qa-real-window-roma",
            DateTimeOffset.UtcNow,
            "real-qa-window-region",
            NodalOsOcrObservationSource.RealQaWindowRegion,
            "NODAL OS OCR QA Window",
            "OneBrain.Tools.QaWindowHost",
            new NodalOsScreenRegionBounds(70, 54, 660, 180),
            1,
            result);

        Assert.AreEqual("real-qa-window-region", envelope.CaptureMode);
        Assert.AreEqual("NODAL OS OCR QA Window", envelope.WindowTitleOrSource);
        Assert.AreEqual(660, envelope.RegionBounds.Width);
        Assert.AreEqual("OfficialSpaceToken", envelope.Result.DictionaryPolicy);
        Assert.IsFalse(envelope.Result.SoftmaxReapplied);
    }

    [TestMethod]
    public void Artifact_ValidatesFlags_AndDocumentsObservationResult()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m321",
            "paddleocr-low-risk-screen-ocr-observation-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M319-M321", root.GetProperty("milestone").GetString());
        Assert.AreEqual("READY_FOR_LOW_RISK_SCREEN_OCR_OBSERVATION_EXPANSION", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("lowRiskObservationOnly").GetBoolean());
        Assert.IsFalse(root.GetProperty("actionsAllowed").GetBoolean());
        Assert.IsTrue(root.GetProperty("noAuthority").GetBoolean());
        Assert.IsTrue(root.GetProperty("evidenceOnly").GetBoolean());
        Assert.AreEqual("real-qa-window-region", root.GetProperty("captureMode").GetString());
        Assert.AreEqual("RatioPreservingRightPad", root.GetProperty("recognizerResizeMode").GetString());
        Assert.IsFalse(root.GetProperty("recognizerSoftmaxReapplied").GetBoolean());
        Assert.IsTrue(root.GetProperty("officialSpacePolicy").GetBoolean());
        Assert.IsTrue(root.GetProperty("hostProcessCleanedUp").GetBoolean());
        Assert.AreEqual(3, root.GetProperty("observationsTotal").GetInt32());
        Assert.AreEqual(3, root.GetProperty("evidenceEnvelopesCreated").GetInt32());
        Assert.AreEqual(3, root.GetProperty("results").GetArrayLength());
        Assert.AreEqual(3, root.GetProperty("observationEnvelopes").GetArrayLength());
    }

    [TestMethod]
    public void Runner_ExposesLowRiskObservationProbeMode()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot, "tools", "onnx-ocr-probe-runner", "Program.cs"));

        StringAssert.Contains(source, "low-risk-screen-ocr-observation-probe");
        StringAssert.Contains(source, "NodalOsOcrEvidenceEnvelope");
        StringAssert.Contains(source, "ActionAllowed = false");
        StringAssert.Contains(source, "EvidenceOnly = true");
    }

    [TestMethod]
    public void Report_Artifact_Audit_Adr_Exist_ForM321()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "paddleocr-internal-low-risk-screen-ocr-observation-m321.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "paddleocr-low-risk-ocr-observation-policy-m319-m321.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "paddleocr-low-risk-screen-ocr-observation-audit-m321.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m321", "paddleocr-low-risk-screen-ocr-observation-summary.json")));
    }

    [TestMethod]
    public void Git_DoesNotTrackOnnxModels_OrGitignoredDictionaries_ForM321()
    {
        Assert.AreEqual(string.Empty, RunGit("ls-files", "*.onnx").Trim());

        var trackedDictionaries = RunGit("ls-files", "tools/ocr-worker/models/onnx/dictionaries/*").Trim();
        Assert.AreEqual(string.Empty, trackedDictionaries);
    }

    private static NodalOsOcrObservationRequest CreateRequest(
        bool fullScreen = false,
        bool containsSensitiveData = false,
        bool containsDocumentData = false,
        bool containsCredentials = false,
        string? expectedText = "ROMA")
        => new(
            "obs-test",
            NodalOsOcrObservationSource.RealQaWindowRegion,
            "real-qa-window-region",
            "NODAL OS OCR QA Window",
            "OneBrain.Tools.QaWindowHost",
            new NodalOsScreenRegionBounds(70, 54, 660, 180),
            expectedText,
            NodalOsOcrObservationRiskLevel.LowRiskOnly,
            LowRiskOnly: true,
            AllowActions: false,
            AllowAuthority: false,
            ContainsSensitiveData: containsSensitiveData,
            ContainsDocumentData: containsDocumentData,
            ContainsCredentials: containsCredentials,
            FullScreen: fullScreen,
            "bounded internal QA observation only");

    private static string RunGit(params string[] args)
    {
        var psi = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = RepoRoot
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi);
        Assert.IsNotNull(process);
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.IsTrue(process.WaitForExit(10000), stderr);
        Assert.AreEqual(0, process.ExitCode, stderr);
        return stdout;
    }
}
