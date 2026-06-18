using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OcrEvidenceLedgerConsumer")]
[TestCategory("OcrEvidenceAuditConsumer")]
[TestCategory("OcrConfidenceDiff")]
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
public sealed class NodalOsOcrEvidenceLedgerConsumerM328M330Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void AcceptedOcrEvidence_WritesAuxiliaryLedgerAuditEntry()
    {
        using var temp = new TempDirectory();
        var ledger = CreateLedger(temp.Path);
        var consumer = new NodalOsOcrEvidenceAuditConsumer();

        var evaluation = consumer.Consume(ledger, CreateEnvelope());

        Assert.AreEqual(NodalOsOcrEvidenceLedgerStatus.AcceptedAuxiliary, evaluation.LedgerStatus);
        Assert.AreEqual(1, ledger.Events.Count);
        Assert.AreEqual(BrowserAuditLedgerEventKind.OcrEvidenceAuxiliaryRecorded, ledger.Events[0].Kind);
        Assert.AreEqual("AcceptedAuxiliary", ledger.Events[0].Decision);
    }

    [TestMethod]
    public void RejectedOcrEvidence_WritesDiagnosticRejectedAuditEntry()
    {
        using var temp = new TempDirectory();
        var ledger = CreateLedger(temp.Path);
        var consumer = new NodalOsOcrEvidenceAuditConsumer();

        var envelope = CreateEnvelope(
            acceptanceState: NodalOsOcrObservationAcceptanceState.RejectedWrongWindow,
            accepted: false,
            regionVerified: false,
            confidenceGatePassed: false,
            rejectionReason: "wrong-window");
        var evaluation = consumer.Consume(ledger, envelope);

        Assert.AreEqual(NodalOsOcrEvidenceLedgerStatus.RecordedDiagnosticRejected, evaluation.LedgerStatus);
        Assert.AreEqual(1, ledger.Events.Count);
        Assert.AreEqual(BrowserAuditLedgerEventKind.OcrEvidenceDiagnosticRejectedRecorded, ledger.Events[0].Kind);
    }

    [TestMethod]
    public void UncertainOcrEvidence_WritesDiagnosticUncertainAuditEntry()
    {
        using var temp = new TempDirectory();
        var ledger = CreateLedger(temp.Path);
        var consumer = new NodalOsOcrEvidenceAuditConsumer();

        var envelope = CreateEnvelope(
            acceptanceState: NodalOsOcrObservationAcceptanceState.UncertainRequiresExpansion,
            accepted: false,
            regionVerified: false,
            confidenceGatePassed: false,
            rejectionReason: "needs-expansion");
        var evaluation = consumer.Consume(ledger, envelope);

        Assert.AreEqual(NodalOsOcrEvidenceLedgerStatus.RecordedDiagnosticUncertain, evaluation.LedgerStatus);
        Assert.AreEqual(1, ledger.Events.Count);
        Assert.AreEqual(BrowserAuditLedgerEventKind.OcrEvidenceDiagnosticUncertainRecorded, ledger.Events[0].Kind);
    }

    [TestMethod]
    public void PolicyViolation_DoesNotBecomeAcceptedOrAuxiliary_AndWritesPolicyRejectionAudit()
    {
        using var temp = new TempDirectory();
        var ledger = CreateLedger(temp.Path);
        var consumer = new NodalOsOcrEvidenceAuditConsumer();

        var envelope = CreateEnvelope(actionAllowed: true);
        var evaluation = consumer.Consume(ledger, envelope);

        Assert.AreEqual(NodalOsOcrEvidenceLedgerStatus.RejectedPolicyViolation, evaluation.LedgerStatus);
        Assert.IsFalse(evaluation.CanBeAcceptedEvidence);
        Assert.IsFalse(evaluation.CanAttachAsAuxiliaryEvidence);
        Assert.AreEqual(1, ledger.Events.Count);
        Assert.AreEqual(BrowserAuditLedgerEventKind.OcrEvidencePolicyViolationRejected, ledger.Events[0].Kind);
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
    public void HighConfidenceAcceptedOcr_StillCannotAuthorizeAction()
    {
        var evaluation = new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(CreateEnvelope(confidence: 0.99d));

        Assert.AreEqual(NodalOsOcrEvidenceConfidenceBand.High, evaluation.Entry?.ConfidenceBand);
        Assert.IsFalse(evaluation.CanAuthorizeAction);
    }

    [TestMethod]
    public void FingerprintMismatch_BlocksAcceptedAuxiliary()
    {
        var evaluation = new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(
            CreateEnvelope(expectedFingerprintHash: "AAA", observedFingerprintHash: "BBB"));

        Assert.AreEqual(NodalOsOcrEvidenceLedgerStatus.RejectedPolicyViolation, evaluation.LedgerStatus);
        Assert.IsFalse(evaluation.CanAttachAsAuxiliaryEvidence);
    }

    [TestMethod]
    public void DiffThreshold_BlocksAcceptedAuxiliary()
    {
        var evaluation = new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(
            CreateEnvelope(diffScore: 0.25d));

        Assert.AreEqual(NodalOsOcrEvidenceLedgerStatus.RejectedPolicyViolation, evaluation.LedgerStatus);
        Assert.IsFalse(evaluation.CanAttachAsAuxiliaryEvidence);
    }

    [TestMethod]
    public void ExactMatch_DoesNotOverrideFailedRegionVerification()
    {
        var evaluation = new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(
            CreateEnvelope(regionVerified: false, confidenceGatePassed: true, accepted: true, exactMatch: true));

        Assert.AreEqual(NodalOsOcrEvidenceLedgerStatus.RejectedPolicyViolation, evaluation.LedgerStatus);
    }

    [TestMethod]
    public void AcceptedEvidence_PreservesConfidenceMetrics()
    {
        var evaluation = new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(CreateEnvelope(confidence: 0.96d));

        Assert.IsNotNull(evaluation.Entry);
        Assert.IsTrue(evaluation.Entry.FingerprintHashMatch);
        Assert.AreEqual(0d, evaluation.Entry.DiffScore);
        Assert.AreEqual(0.1d, evaluation.Entry.DarkPixelRatio);
        Assert.AreEqual("sig", evaluation.Entry.SampleSignature);
        Assert.AreEqual(NodalOsOcrEvidenceConfidenceBand.High, evaluation.Entry.ConfidenceBand);
    }

    [TestMethod]
    public void DiagnosticEvidence_PreservesConfidenceMetrics()
    {
        var evaluation = new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(
            CreateEnvelope(
                acceptanceState: NodalOsOcrObservationAcceptanceState.UncertainRequiresExpansion,
                accepted: false,
                regionVerified: true,
                confidenceGatePassed: false,
                confidence: 0.51d,
                rejectionReason: "low-confidence"));

        Assert.IsNotNull(evaluation.Entry);
        Assert.AreEqual(NodalOsOcrEvidenceUse.DiagnosticOnly, evaluation.Entry.EvidenceUse);
        Assert.AreEqual(NodalOsOcrEvidenceConfidenceBand.Low, evaluation.Entry.ConfidenceBand);
        Assert.IsTrue(evaluation.Entry.FingerprintHashMatch);
    }

    [TestMethod]
    public void Provenance_IsPreserved_InAuditMetadata()
    {
        using var temp = new TempDirectory();
        var ledger = CreateLedger(temp.Path);
        var consumer = new NodalOsOcrEvidenceAuditConsumer();

        consumer.Consume(ledger, CreateEnvelope());

        var metadata = ledger.Events[0].Metadata;
        Assert.AreEqual("Ocr", metadata["source"]);
        Assert.AreEqual("RealQaWindowRegion", metadata["sourceCategory"]);
        Assert.AreEqual("real-qa-window-region", metadata["captureMode"]);
        Assert.AreEqual("True", metadata["regionVerified"]);
        Assert.AreEqual("True", metadata["confidenceGatePassed"]);
        Assert.AreEqual("True", metadata["fingerprintHashMatch"]);
    }

    [TestMethod]
    public void Artifact_ValidatesM330Flags()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m330",
            "paddleocr-ocr-evidence-ledger-consumer-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M328-M330", root.GetProperty("milestone").GetString());
        Assert.AreEqual("READY_FOR_OCR_EVIDENCE_TO_FSM_OBSERVATION_WIRING", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("ocrEvidenceLedgerExpansionAttempted").GetBoolean());
        Assert.IsTrue(root.GetProperty("ledgerConsumerCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("auditConsumerCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("acceptedAuxiliaryCanBeWritten").GetBoolean());
        Assert.IsTrue(root.GetProperty("diagnosticRejectedCanBeWritten").GetBoolean());
        Assert.IsTrue(root.GetProperty("diagnosticUncertainCanBeWritten").GetBoolean());
        Assert.IsFalse(root.GetProperty("policyViolationCanBecomeAccepted").GetBoolean());
        Assert.IsTrue(root.GetProperty("confidenceDiffExpansionCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("fingerprintMismatchBlocksAccepted").GetBoolean());
        Assert.IsTrue(root.GetProperty("diffThresholdBlocksAccepted").GetBoolean());
        Assert.IsTrue(root.GetProperty("exactMatchDoesNotOverrideFailedVerification").GetBoolean());
    }

    [TestMethod]
    public void Git_DoesNotTrackOnnxModels_OrGitignoredDictionaries_ForM330()
    {
        Assert.AreEqual(string.Empty, RunGit("ls-files", "*.onnx").Trim());
        Assert.AreEqual(string.Empty, RunGit("ls-files", "tools/ocr-worker/models/onnx/dictionaries/*").Trim());
    }

    private static BrowserPersistentAuditLedger CreateLedger(string path) =>
        new(
            new BrowserAuditLedgerPolicy(
                path,
                AllowFilePersistence: true,
                RedactBeforePersist: true,
                new BrowserAuditLedgerRetentionPolicy(null, null, DeleteOnCleanup: true)),
            BrowserAuditLedgerHmacIntegrityProvider.CreateDevFixtureProvider("onebrain-ocr-m330-explicit-test-fixture-hmac-key"));

    private static NodalOsOcrEvidenceEnvelope CreateEnvelope(
        NodalOsOcrObservationAcceptanceState acceptanceState = NodalOsOcrObservationAcceptanceState.AcceptedEvidence,
        bool accepted = true,
        bool regionVerified = true,
        bool confidenceGatePassed = true,
        bool actionAllowed = false,
        bool noAuthority = true,
        bool evidenceOnly = true,
        bool exactMatch = true,
        double confidence = 0.99d,
        string rejectionReason = "",
        double diffScore = 0d,
        string expectedFingerprintHash = "ABC",
        string observedFingerprintHash = "ABC")
    {
        var fingerprint = new NodalOsRegionCaptureFingerprint(observedFingerprintHash, 660, 180, 200, 200, 200, 0.1, "sig");
        var expectedFingerprint = new NodalOsRegionCaptureFingerprint(expectedFingerprintHash, 660, 180, 200, 200, 200, 0.1, "sig");
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
            ExpectedFingerprint: expectedFingerprint,
            ObservedFingerprint: fingerprint,
            DiffScore: diffScore,
            RegionVerified: regionVerified,
            IsolationState: regionVerified ? NodalOsWindowIsolationState.ForegroundVerified : NodalOsWindowIsolationState.CaptureVerificationFailed,
            VerificationWarnings: [],
            FailureReason: rejectionReason);
        var gate = new NodalOsOcrObservationConfidenceGateResult(
            Attempted: true,
            Passed: confidenceGatePassed,
            ConfidenceThreshold: 0.75d,
            ConfidenceScore: confidence,
            EditDistanceThreshold: 1,
            EditDistance: acceptanceState == NodalOsOcrObservationAcceptanceState.AcceptedEvidence ? 0 : 2,
            ExactOrNormalizedMatch: exactMatch,
            Reason: confidenceGatePassed ? "passed" : "failed");
        var result = new NodalOsOcrObservationResult(
            "obs-qa-real-window-roma",
            NodalOsOcrObservationDecision.AcceptedEvidenceOnly,
            accepted,
            "ROMA",
            "ROMA",
            "ROMA",
            ExactMatch: exactMatch,
            NormalizedMatch: false,
            EditDistance: exactMatch ? 0 : 2,
            Confidence: confidence,
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
            DiffScore = diffScore,
            CaptureFingerprint = fingerprint,
            RegionVerification = verification,
            ConfidenceGate = gate
        };

        return new NodalOsOcrEvidenceEnvelope(
            "obs-qa-real-window-roma",
            DateTimeOffset.UtcNow,
            "real-qa-window-region",
            NodalOsOcrObservationSource.RealQaWindowRegion,
            "NODAL OS OCR QA Window",
            "OneBrain.Tools.QaWindowHost",
            new NodalOsScreenRegionBounds(70, 54, 660, 180),
            1,
            result);
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

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"onebrain-ocr-audit-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
