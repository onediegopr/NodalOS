using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OcrEvidenceIntegration")]
[TestCategory("OcrEvidenceLedger")]
[TestCategory("OcrEvidencePolicy")]
[TestCategory("OcrObservationIsolation")]
[TestCategory("RegionVerification")]
[TestCategory("ConfidenceGate")]
[TestCategory("LowRiskScreenOcrObservation")]
[TestCategory("OcrObservation")]
[TestCategory("OcrEvidenceEnvelope")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("OfficialSpaceToken")]
public sealed class NodalOsOcrEvidenceIntegrationM325M327Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void OcrAcceptedEvidence_MapsToAuxiliaryLedgerEntry()
    {
        var evaluation = new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(CreateEnvelope());

        Assert.AreEqual(NodalOsOcrEvidenceLedgerStatus.AcceptedAuxiliary, evaluation.LedgerStatus);
        Assert.IsTrue(evaluation.CanRegister);
        Assert.IsTrue(evaluation.CanBeAcceptedEvidence);
        Assert.IsTrue(evaluation.CanAttachAsAuxiliaryEvidence);
        Assert.IsFalse(evaluation.CanAttachAsDiagnosticEvidence);
        Assert.IsNotNull(evaluation.Entry);
        Assert.AreEqual(NodalOsOcrEvidenceUse.AuxiliaryOnly, evaluation.Entry.EvidenceUse);
    }

    [TestMethod]
    public void OcrRejectedEvidence_MapsToDiagnosticRejectedEntry_NotAccepted()
    {
        var envelope = CreateEnvelope(NodalOsOcrObservationAcceptanceState.RejectedWrongWindow) with
        {
            Result = CreateResult(
                acceptanceState: NodalOsOcrObservationAcceptanceState.RejectedWrongWindow,
                accepted: false,
                regionVerified: false,
                confidenceGatePassed: false,
                rejectionReason: "wrong-window")
        };
        var evaluation = new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(envelope);

        Assert.AreEqual(NodalOsOcrEvidenceLedgerStatus.RecordedDiagnosticRejected, evaluation.LedgerStatus);
        Assert.IsTrue(evaluation.CanRegister);
        Assert.IsFalse(evaluation.CanBeAcceptedEvidence);
        Assert.IsFalse(evaluation.CanAttachAsAuxiliaryEvidence);
        Assert.IsTrue(evaluation.CanAttachAsDiagnosticEvidence);
        Assert.IsNotNull(evaluation.Entry);
        Assert.AreEqual(NodalOsOcrEvidenceUse.DiagnosticOnly, evaluation.Entry.EvidenceUse);
    }

    [TestMethod]
    public void OcrUncertainEvidence_MapsToDiagnosticUncertainEntry_NotAccepted()
    {
        var envelope = CreateEnvelope(NodalOsOcrObservationAcceptanceState.UncertainRequiresExpansion) with
        {
            Result = CreateResult(
                acceptanceState: NodalOsOcrObservationAcceptanceState.UncertainRequiresExpansion,
                accepted: false,
                regionVerified: false,
                confidenceGatePassed: false,
                rejectionReason: "needs-expansion")
        };
        var evaluation = new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(envelope);

        Assert.AreEqual(NodalOsOcrEvidenceLedgerStatus.RecordedDiagnosticUncertain, evaluation.LedgerStatus);
        Assert.IsTrue(evaluation.CanRegister);
        Assert.IsFalse(evaluation.CanBeAcceptedEvidence);
        Assert.IsTrue(evaluation.CanAttachAsDiagnosticEvidence);
    }

    [TestMethod]
    public void OcrEvidenceWithActionAllowedTrue_IsRejected()
    {
        var envelope = CreateEnvelope() with
        {
            Result = CreateResult(actionAllowed: true)
        };
        var evaluation = new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(envelope);

        Assert.AreEqual(NodalOsOcrEvidenceLedgerStatus.RejectedPolicyViolation, evaluation.LedgerStatus);
        Assert.IsFalse(evaluation.CanRegister);
        StringAssert.Contains(evaluation.Reason, "actionAllowed=true");
    }

    [TestMethod]
    public void OcrEvidenceWithNoAuthorityFalse_IsRejected()
    {
        var envelope = CreateEnvelope() with
        {
            Result = CreateResult(noAuthority: false)
        };
        var evaluation = new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(envelope);

        Assert.AreEqual(NodalOsOcrEvidenceLedgerStatus.RejectedPolicyViolation, evaluation.LedgerStatus);
        Assert.IsFalse(evaluation.CanRegister);
        StringAssert.Contains(evaluation.Reason, "no-authority");
    }

    [TestMethod]
    public void OcrEvidenceWithEvidenceOnlyFalse_IsRejected()
    {
        var envelope = CreateEnvelope() with
        {
            Result = CreateResult(evidenceOnly: false)
        };
        var evaluation = new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(envelope);

        Assert.AreEqual(NodalOsOcrEvidenceLedgerStatus.RejectedPolicyViolation, evaluation.LedgerStatus);
        Assert.IsFalse(evaluation.CanRegister);
        StringAssert.Contains(evaluation.Reason, "evidence-only");
    }

    [TestMethod]
    public void OcrAcceptedEvidence_RequiresRegionVerified()
    {
        var envelope = CreateEnvelope() with
        {
            Result = CreateResult(regionVerified: false, confidenceGatePassed: true, accepted: true)
        };
        var evaluation = new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(envelope);

        Assert.AreEqual(NodalOsOcrEvidenceLedgerStatus.RejectedPolicyViolation, evaluation.LedgerStatus);
        Assert.IsFalse(evaluation.CanRegister);
    }

    [TestMethod]
    public void OcrAcceptedEvidence_RequiresConfidenceGatePassed()
    {
        var envelope = CreateEnvelope() with
        {
            Result = CreateResult(regionVerified: true, confidenceGatePassed: false, accepted: true)
        };
        var evaluation = new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(envelope);

        Assert.AreEqual(NodalOsOcrEvidenceLedgerStatus.RejectedPolicyViolation, evaluation.LedgerStatus);
        Assert.IsFalse(evaluation.CanRegister);
    }

    [TestMethod]
    public void OcrEvidence_CannotAuthorizeAction_OrApproveClickSubmitSendDeletePaySign()
    {
        var evaluation = new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(CreateEnvelope());

        Assert.IsFalse(evaluation.CanAuthorizeAction);
        Assert.IsFalse(evaluation.CanApproveClick);
        Assert.IsFalse(evaluation.CanApproveSubmit);
        Assert.IsFalse(evaluation.CanApproveSend);
        Assert.IsFalse(evaluation.CanApproveDelete);
        Assert.IsFalse(evaluation.CanApprovePay);
        Assert.IsFalse(evaluation.CanApproveSign);
    }

    [TestMethod]
    public void OcrEvidence_PreservesProvenance_AndVerification()
    {
        var evaluation = new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(CreateEnvelope(), artifactRef: "artifacts/ocr-vision-onnx/m327/sample.json");

        Assert.IsNotNull(evaluation.Entry);
        Assert.AreEqual("real-qa-window-region", evaluation.Entry.CaptureMode);
        Assert.AreEqual("NODAL OS OCR QA Window", evaluation.Entry.WindowTitleOrSource);
        Assert.AreEqual("OneBrain.Tools.QaWindowHost", evaluation.Entry.ProcessOrSource);
        Assert.IsTrue(evaluation.Entry.RegionVerified);
        Assert.IsTrue(evaluation.Entry.ConfidenceGatePassed);
        Assert.IsNotNull(evaluation.Entry.CaptureFingerprint);
        Assert.IsNotNull(evaluation.Entry.RegionVerification);
        Assert.AreEqual("artifacts/ocr-vision-onnx/m327/sample.json", evaluation.Entry.ArtifactRef);
    }

    [TestMethod]
    public void Artifact_ValidatesM327Flags()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m327",
            "paddleocr-internal-ocr-evidence-integration-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M325-M327", root.GetProperty("milestone").GetString());
        Assert.AreEqual("READY_FOR_OCR_EVIDENCE_LEDGER_EXPANSION", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("ocrEvidenceIntegrationAttempted").GetBoolean());
        Assert.IsTrue(root.GetProperty("evidenceLedgerAdapterCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("fsmPolicyGateCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("acceptedOcrEvidenceCanBeAuxiliary").GetBoolean());
        Assert.IsFalse(root.GetProperty("rejectedOcrEvidenceCanBeAccepted").GetBoolean());
        Assert.IsFalse(root.GetProperty("uncertainOcrEvidenceCanBeAccepted").GetBoolean());
        Assert.IsFalse(root.GetProperty("ocrCanAuthorizeAction").GetBoolean());
        Assert.IsFalse(root.GetProperty("ocrCanApproveClick").GetBoolean());
        Assert.IsFalse(root.GetProperty("ocrCanApproveSubmit").GetBoolean());
        Assert.IsFalse(root.GetProperty("ocrCanApproveSend").GetBoolean());
        Assert.IsFalse(root.GetProperty("ocrCanApproveDelete").GetBoolean());
        Assert.IsFalse(root.GetProperty("ocrCanApprovePay").GetBoolean());
        Assert.IsFalse(root.GetProperty("ocrCanApproveSign").GetBoolean());
        Assert.IsTrue(root.GetProperty("regionVerificationRequired").GetBoolean());
        Assert.IsTrue(root.GetProperty("confidenceGateRequired").GetBoolean());
    }

    [TestMethod]
    public void Git_DoesNotTrackOnnxModels_OrGitignoredDictionaries_ForM327()
    {
        Assert.AreEqual(string.Empty, RunGit("ls-files", "*.onnx").Trim());
        Assert.AreEqual(string.Empty, RunGit("ls-files", "tools/ocr-worker/models/onnx/dictionaries/*").Trim());
    }

    [TestMethod]
    public void Report_Artifact_Audit_Adr_Exist_ForM327()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "paddleocr-internal-ocr-evidence-integration-m327.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "paddleocr-ocr-evidence-ledger-policy-m325-m327.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "paddleocr-ocr-evidence-integration-audit-m327.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m327", "paddleocr-internal-ocr-evidence-integration-summary.json")));
    }

    private static NodalOsOcrEvidenceEnvelope CreateEnvelope(
        NodalOsOcrObservationAcceptanceState acceptanceState = NodalOsOcrObservationAcceptanceState.AcceptedEvidence)
    {
        return new NodalOsOcrEvidenceEnvelope(
            "obs-qa-real-window-roma",
            DateTimeOffset.UtcNow,
            "real-qa-window-region",
            NodalOsOcrObservationSource.RealQaWindowRegion,
            "NODAL OS OCR QA Window",
            "OneBrain.Tools.QaWindowHost",
            new NodalOsScreenRegionBounds(70, 54, 660, 180),
            1,
            CreateResult(acceptanceState: acceptanceState));
    }

    private static NodalOsOcrObservationResult CreateResult(
        NodalOsOcrObservationAcceptanceState acceptanceState = NodalOsOcrObservationAcceptanceState.AcceptedEvidence,
        bool accepted = true,
        bool regionVerified = true,
        bool confidenceGatePassed = true,
        bool actionAllowed = false,
        bool noAuthority = true,
        bool evidenceOnly = true,
        string rejectionReason = "")
    {
        var fingerprint = new NodalOsRegionCaptureFingerprint("ABC", 660, 180, 200, 200, 200, 0.1, "sig");
        var verification = new NodalOsOcrRegionVerificationResult(
            "NODAL OS OCR QA Window",
            "NODAL OS OCR QA Window",
            "OneBrain.Tools.QaWindowHost",
            "OneBrain.Tools.QaWindowHost",
            new NodalOsScreenRegionBounds(131, 165, 800, 320),
            new NodalOsScreenRegionBounds(131, 165, 800, 320),
            new NodalOsScreenRegionBounds(70, 54, 660, 180),
            new NodalOsScreenRegionBounds(70, 54, 660, 180),
            WindowVisible: true,
            WindowForegroundOrActivated: true,
            RegionInsideWindow: true,
            FullScreen: false,
            CaptureTechnique: "window-client-bitmap-region",
            ExpectedFingerprint: fingerprint,
            ObservedFingerprint: fingerprint,
            DiffScore: 0d,
            RegionVerified: regionVerified,
            IsolationState: regionVerified ? NodalOsWindowIsolationState.ForegroundVerified : NodalOsWindowIsolationState.CaptureVerificationFailed,
            VerificationWarnings: [],
            FailureReason: rejectionReason);
        var gate = new NodalOsOcrObservationConfidenceGateResult(
            Attempted: true,
            Passed: confidenceGatePassed,
            ConfidenceThreshold: 0.75d,
            ConfidenceScore: 0.99d,
            EditDistanceThreshold: 1,
            EditDistance: acceptanceState == NodalOsOcrObservationAcceptanceState.AcceptedEvidence ? 0 : 2,
            ExactOrNormalizedMatch: acceptanceState == NodalOsOcrObservationAcceptanceState.AcceptedEvidence,
            Reason: confidenceGatePassed ? "passed" : "failed");

        return new NodalOsOcrObservationResult(
            "obs-qa-real-window-roma",
            NodalOsOcrObservationDecision.AcceptedEvidenceOnly,
            accepted,
            "ROMA",
            "ROMA",
            "ROMA",
            ExactMatch: true,
            NormalizedMatch: false,
            EditDistance: 0,
            Confidence: 0.99d,
            Warnings: [],
            NoAuthority: noAuthority,
            ActionAllowed: actionAllowed,
            EvidenceOnly: evidenceOnly,
            RedactionStatus: "NoRedactionRequiredForControlledQaFixture",
            RawImagePersisted: false,
            ModelFamily: "PaddleOCR-ONNX",
            DictionaryPolicy: "OfficialSpaceToken",
            OfficialSpacePolicy: true,
            SoftmaxReapplied: false)
        {
            RegionVerified = regionVerified,
            IsolationState = verification.IsolationState,
            ConfidenceGatePassed = confidenceGatePassed,
            AcceptanceState = acceptanceState,
            RejectionReason = rejectionReason,
            DiffScore = verification.DiffScore,
            CaptureFingerprint = fingerprint,
            RegionVerification = verification,
            ConfidenceGate = gate
        };
    }

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
