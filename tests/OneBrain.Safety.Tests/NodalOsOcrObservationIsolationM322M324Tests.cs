using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OcrObservationIsolation")]
[TestCategory("RegionVerification")]
[TestCategory("ConfidenceGate")]
[TestCategory("LowRiskScreenOcrObservation")]
[TestCategory("OcrObservation")]
[TestCategory("OcrEvidenceEnvelope")]
[TestCategory("QaWindowHost")]
[TestCategory("RealQaWindowRegion")]
[TestCategory("WindowRegionCapture")]
[TestCategory("OnnxOutOfProcessGuard")]
[TestCategory("OnnxOcrProbeRunner")]
[TestCategory("OfficialSpaceToken")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
public sealed class NodalOsOcrObservationIsolationM322M324Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void WrongWindow_IsRejected()
    {
        var result = Evaluate(
            verification: Verification(
                observedWindowTitle: "Other Window",
                isolationState: NodalOsWindowIsolationState.WrongWindowDetected,
                regionVerified: false,
                failureReason: "wrong-window"),
            gate: Gate(passed: false, reason: "region-not-verified"),
            acceptance: NodalOsOcrObservationAcceptanceState.RejectedWrongWindow);

        Assert.IsFalse(result.Accepted);
        Assert.AreEqual(NodalOsOcrObservationAcceptanceState.RejectedWrongWindow, result.AcceptanceState);
        Assert.AreEqual("wrong-window", result.RejectionReason);
    }

    [TestMethod]
    public void NotForeground_UntrustedCapture_IsRejectedOrUncertain()
    {
        var result = Evaluate(
            verification: Verification(
                windowForegroundOrActivated: false,
                captureTechnique: "screen-physical-from-client-pointtoscreen",
                isolationState: NodalOsWindowIsolationState.VisibleButNotForeground,
                regionVerified: false,
                failureReason: "window-not-foreground"),
            gate: Gate(passed: false, reason: "region-not-verified"),
            acceptance: NodalOsOcrObservationAcceptanceState.RejectedNotForeground);

        Assert.IsFalse(result.Accepted);
        Assert.AreEqual(NodalOsOcrObservationAcceptanceState.RejectedNotForeground, result.AcceptanceState);
    }

    [TestMethod]
    public void BoundsMismatch_IsRejected()
    {
        var result = Evaluate(
            verification: Verification(
                observedRegionBounds: new NodalOsScreenRegionBounds(71, 54, 660, 180),
                isolationState: NodalOsWindowIsolationState.BoundsMismatch,
                regionVerified: false,
                failureReason: "region-bounds-mismatch"),
            gate: Gate(passed: false, reason: "region-not-verified"),
            acceptance: NodalOsOcrObservationAcceptanceState.RejectedBoundsMismatch);

        Assert.IsFalse(result.Accepted);
        Assert.AreEqual(NodalOsOcrObservationAcceptanceState.RejectedBoundsMismatch, result.AcceptanceState);
    }

    [TestMethod]
    public void FullScreen_AndSensitive_AreRejected()
    {
        var evaluator = new NodalOsLowRiskOcrObservationEvaluator();

        var fullScreen = evaluator.Evaluate(CreateRequest(fullScreen: true), "", "", 0, 4, false, false, null);
        var sensitive = evaluator.Evaluate(CreateRequest(containsSensitiveData: true), "", "", 0, 4, false, false, null);

        Assert.AreEqual(NodalOsOcrObservationDecision.RejectedFullScreen, fullScreen.PolicyDecision);
        Assert.AreEqual(NodalOsOcrObservationDecision.RejectedSensitiveData, sensitive.PolicyDecision);
        Assert.IsFalse(fullScreen.Accepted);
        Assert.IsFalse(sensitive.Accepted);
    }

    [TestMethod]
    public void LowConfidence_IsRejected()
    {
        var result = Evaluate(
            verification: Verification(regionVerified: true),
            gate: Gate(passed: false, confidenceScore: 0.22d, reason: "confidence-below-threshold"),
            acceptance: NodalOsOcrObservationAcceptanceState.RejectedLowConfidence,
            recognizedText: "PVC WALI",
            exactMatch: false,
            normalizedMatch: false,
            editDistance: 1,
            confidence: 0.22d);

        Assert.IsFalse(result.Accepted);
        Assert.AreEqual(NodalOsOcrObservationAcceptanceState.RejectedLowConfidence, result.AcceptanceState);
        Assert.IsFalse(result.ConfidenceGatePassed);
    }

    [TestMethod]
    public void AcceptedEvidence_RequiresVerifiedRegion_Gate_NoAuthority_AndNoActions()
    {
        var result = Evaluate(
            verification: Verification(regionVerified: true),
            gate: Gate(passed: true, reason: "exact-or-normalized-match"),
            acceptance: NodalOsOcrObservationAcceptanceState.AcceptedEvidence,
            recognizedText: "ROMA",
            exactMatch: true,
            normalizedMatch: false,
            editDistance: 0,
            confidence: 0.99d);

        Assert.IsTrue(result.Accepted);
        Assert.IsTrue(result.RegionVerified);
        Assert.IsTrue(result.ConfidenceGatePassed);
        Assert.IsFalse(result.ActionAllowed);
        Assert.IsTrue(result.NoAuthority);
        Assert.IsTrue(result.EvidenceOnly);
        Assert.AreEqual(NodalOsOcrObservationAcceptanceState.AcceptedEvidence, result.AcceptanceState);
    }

    [TestMethod]
    public void Envelope_RecordsVerificationFingerprint_AndRejectionReason()
    {
        var result = Evaluate(
            verification: Verification(
                regionVerified: false,
                isolationState: NodalOsWindowIsolationState.CaptureVerificationFailed,
                failureReason: "fingerprint-diff-too-high"),
            gate: Gate(passed: false, reason: "region-not-verified"),
            acceptance: NodalOsOcrObservationAcceptanceState.UncertainRequiresExpansion);
        var envelope = new NodalOsOcrEvidenceEnvelope(
            "obs-1",
            DateTimeOffset.UtcNow,
            "real-qa-window-region",
            NodalOsOcrObservationSource.RealQaWindowRegion,
            "NODAL OS OCR QA Window",
            "OneBrain.Tools.QaWindowHost",
            new NodalOsScreenRegionBounds(70, 54, 660, 180),
            0,
            result);

        Assert.IsNotNull(envelope.CaptureFingerprint);
        Assert.IsNotNull(envelope.Result.RegionVerification);
        Assert.IsNotNull(envelope.Result.ConfidenceGate);
        Assert.AreEqual("fingerprint-diff-too-high", envelope.RejectionReason);
    }

    [TestMethod]
    public void Artifact_ValidatesM324Flags()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m324",
            "paddleocr-low-risk-ocr-observation-isolation-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M322-M324", root.GetProperty("Milestone").GetString());
        Assert.AreEqual("READY_FOR_INTERNAL_OCR_EVIDENCE_INTEGRATION", root.GetProperty("ReadinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("LowRiskObservationOnly").GetBoolean());
        Assert.IsFalse(root.GetProperty("ActionsAllowed").GetBoolean());
        Assert.IsTrue(root.GetProperty("NoAuthority").GetBoolean());
        Assert.IsTrue(root.GetProperty("EvidenceOnly").GetBoolean());
        Assert.IsTrue(root.GetProperty("IsolationGateAttempted").GetBoolean());
        Assert.IsTrue(root.GetProperty("RegionVerificationAttempted").GetBoolean());
        Assert.IsTrue(root.GetProperty("ConfidenceGateAttempted").GetBoolean());
        Assert.AreEqual(3, root.GetProperty("AcceptedEvidenceCount").GetInt32());
        Assert.AreEqual(3, root.GetProperty("RegionVerifiedCount").GetInt32());
        Assert.AreEqual(3, root.GetProperty("ConfidenceGatePassedCount").GetInt32());
        Assert.IsTrue(root.GetProperty("SuccessCriteriaMet").GetBoolean());
    }

    [TestMethod]
    public void Git_DoesNotTrackOnnxModels_OrGitignoredDictionaries_ForM324()
    {
        Assert.AreEqual(string.Empty, RunGit("ls-files", "*.onnx").Trim());
        Assert.AreEqual(string.Empty, RunGit("ls-files", "tools/ocr-worker/models/onnx/dictionaries/*").Trim());
    }

    [TestMethod]
    public void Runner_RecordsIsolationVerification_AndConfidenceGate()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot, "tools", "onnx-ocr-probe-runner", "Program.cs"));

        StringAssert.Contains(source, "IsolationGateAttempted = true");
        StringAssert.Contains(source, "RegionVerificationAttempted = true");
        StringAssert.Contains(source, "ConfidenceGateAttempted = true");
        StringAssert.Contains(source, "NodalOsOcrRegionVerificationResult");
        StringAssert.Contains(source, "NodalOsOcrObservationConfidenceGateResult");
    }

    private static NodalOsOcrObservationResult Evaluate(
        NodalOsOcrRegionVerificationResult verification,
        NodalOsOcrObservationConfidenceGateResult gate,
        NodalOsOcrObservationAcceptanceState acceptance,
        string recognizedText = "",
        bool exactMatch = false,
        bool normalizedMatch = false,
        int? editDistance = 4,
        double? confidence = null)
    {
        return new NodalOsLowRiskOcrObservationEvaluator().Evaluate(
            CreateRequest(),
            recognizedText,
            recognizedText.Replace(" ", string.Empty, StringComparison.Ordinal),
            detectorBoxesCount: 1,
            editDistance,
            exactMatch,
            normalizedMatch,
            confidence,
            warnings: [],
            verificationResult: verification,
            confidenceGateResult: gate,
            acceptanceState: acceptance,
            rejectionReason: verification.FailureReason);
    }

    private static NodalOsOcrObservationRequest CreateRequest(
        bool fullScreen = false,
        bool containsSensitiveData = false)
        => new(
            "obs-test",
            NodalOsOcrObservationSource.RealQaWindowRegion,
            "real-qa-window-region",
            "NODAL OS OCR QA Window",
            "OneBrain.Tools.QaWindowHost",
            new NodalOsScreenRegionBounds(70, 54, 660, 180),
            "ROMA",
            NodalOsOcrObservationRiskLevel.LowRiskOnly,
            LowRiskOnly: true,
            AllowActions: false,
            AllowAuthority: false,
            ContainsSensitiveData: containsSensitiveData,
            ContainsDocumentData: false,
            ContainsCredentials: false,
            FullScreen: fullScreen,
            "bounded internal QA observation only");

    private static NodalOsOcrRegionVerificationResult Verification(
        string observedWindowTitle = "NODAL OS OCR QA Window",
        bool windowForegroundOrActivated = true,
        string captureTechnique = "window-client-bitmap-region",
        NodalOsWindowIsolationState isolationState = NodalOsWindowIsolationState.ForegroundVerified,
        bool regionVerified = true,
        string failureReason = "",
        NodalOsScreenRegionBounds? observedRegionBounds = null)
    {
        var fingerprint = new NodalOsRegionCaptureFingerprint("ABC", 660, 180, 200, 200, 200, 0.1, "sig");
        return new NodalOsOcrRegionVerificationResult(
            "NODAL OS OCR QA Window",
            observedWindowTitle,
            "OneBrain.Tools.QaWindowHost",
            "OneBrain.Tools.QaWindowHost",
            new NodalOsScreenRegionBounds(131, 165, 800, 320),
            new NodalOsScreenRegionBounds(131, 165, 800, 320),
            new NodalOsScreenRegionBounds(70, 54, 660, 180),
            observedRegionBounds ?? new NodalOsScreenRegionBounds(70, 54, 660, 180),
            WindowVisible: true,
            WindowForegroundOrActivated: windowForegroundOrActivated,
            RegionInsideWindow: true,
            FullScreen: false,
            CaptureTechnique: captureTechnique,
            ExpectedFingerprint: fingerprint,
            ObservedFingerprint: fingerprint,
            DiffScore: 0d,
            RegionVerified: regionVerified,
            IsolationState: isolationState,
            VerificationWarnings: [],
            FailureReason: failureReason);
    }

    private static NodalOsOcrObservationConfidenceGateResult Gate(
        bool passed,
        string reason,
        double? confidenceScore = 0.95d)
        => new(
            Attempted: true,
            Passed: passed,
            ConfidenceThreshold: 0.75d,
            ConfidenceScore: confidenceScore,
            EditDistanceThreshold: 1,
            EditDistance: passed ? 0 : 2,
            ExactOrNormalizedMatch: passed,
            Reason: reason);

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
