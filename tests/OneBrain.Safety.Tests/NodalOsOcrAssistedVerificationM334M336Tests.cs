using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OcrAssistedVerification")]
[TestCategory("AssistedVerificationPolicy")]
[TestCategory("SignalFusion")]
[TestCategory("OcrFsmObservation")]
[TestCategory("OcrEvidenceRanking")]
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
public sealed class NodalOsOcrAssistedVerificationM334M336Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void OcrOnly_HighConfidence_FailsWithRejectedOcrOnly()
    {
        var result = Evaluate(CreateRequest(CreateAcceptedOcrSignals()));

        Assert.AreEqual(NodalOsAssistedVerificationDecision.RejectedOcrOnly, result.Decision);
        Assert.IsFalse(result.NonOcrCorroborationSatisfied);
    }

    [TestMethod]
    public void OcrExactMatchAlone_Fails()
    {
        var result = Evaluate(CreateRequest(CreateAcceptedOcrSignals()));

        Assert.AreEqual(NodalOsAssistedVerificationDecision.RejectedOcrOnly, result.Decision);
    }

    [TestMethod]
    public void OcrPlusKnownQaFixtureCorroboration_PassesVerifiedLowRisk()
    {
        var signals = CreateAcceptedOcrSignals()
            .Concat(new[]
            {
                CreateNonOcrSignal(
                    "qa-fixture-roma",
                    NodalOsAssistedVerificationSignalKind.KnownQaFixtureSignal,
                    expectedText: "ROMA")
            })
            .ToArray();

        var result = Evaluate(CreateRequest(signals));

        Assert.AreEqual(NodalOsAssistedVerificationDecision.VerifiedLowRisk, result.Decision);
        Assert.IsTrue(result.NonOcrCorroborationSatisfied);
        Assert.IsFalse(result.CanProduceActionPlan);
        Assert.IsFalse(result.CanProduceSafeAction);
    }

    [TestMethod]
    public void OcrPlusNonOcrMismatch_ReturnsNeedsMoreEvidence()
    {
        var signals = CreateAcceptedOcrSignals()
            .Concat(new[]
            {
                CreateNonOcrSignal(
                    "qa-fixture-mismatch",
                    NodalOsAssistedVerificationSignalKind.KnownQaFixtureSignal,
                    expectedText: "PVC WALL")
            })
            .ToArray();

        var result = Evaluate(CreateRequest(signals));

        Assert.AreEqual(NodalOsAssistedVerificationDecision.NeedsMoreEvidence, result.Decision);
    }

    [TestMethod]
    public void RejectedOcr_CannotSupportVerification()
    {
        var result = Evaluate(CreateRequest(CreateDiagnosticOnlySignals(rejected: true)));

        Assert.AreEqual(NodalOsAssistedVerificationDecision.NotVerified, result.Decision);
    }

    [TestMethod]
    public void UncertainOcr_CannotSupportVerification()
    {
        var result = Evaluate(CreateRequest(CreateDiagnosticOnlySignals(rejected: false)));

        Assert.AreEqual(NodalOsAssistedVerificationDecision.NeedsMoreEvidence, result.Decision);
    }

    [TestMethod]
    public void PolicyViolationOcr_CannotSupportVerification()
    {
        var result = Evaluate(CreateRequest(new[]
        {
            CreateRejectedPolicySignal()
        }));

        Assert.AreEqual(NodalOsAssistedVerificationDecision.RejectedPolicyViolation, result.Decision);
    }

    [TestMethod]
    public void SensitiveRequest_IsRejected()
    {
        var result = Evaluate(CreateRequest(CreateAcceptedOcrSignals(), containsSensitiveData: true));

        Assert.AreEqual(NodalOsAssistedVerificationDecision.RejectedSensitive, result.Decision);
    }

    [TestMethod]
    public void FullScreenRequest_IsRejected()
    {
        var result = Evaluate(CreateRequest(CreateAcceptedOcrSignals(), fullScreen: true));

        Assert.AreEqual(NodalOsAssistedVerificationDecision.RejectedFullScreen, result.Decision);
    }

    [TestMethod]
    public void DocumentRequest_IsRejected()
    {
        var result = Evaluate(CreateRequest(CreateAcceptedOcrSignals(), containsDocumentData: true));

        Assert.AreEqual(NodalOsAssistedVerificationDecision.RejectedDocument, result.Decision);
    }

    [TestMethod]
    public void HighRiskRequest_IsRejected()
    {
        var result = Evaluate(CreateRequest(CreateAcceptedOcrSignals(), riskLevel: NodalOsAssistedVerificationRiskLevel.High));

        Assert.AreEqual(NodalOsAssistedVerificationDecision.RejectedRiskTooHigh, result.Decision);
    }

    [TestMethod]
    public void ActionRequest_IsRejected()
    {
        var result = Evaluate(CreateRequest(CreateAcceptedOcrSignals(), actionRequested: true));

        Assert.AreEqual(NodalOsAssistedVerificationDecision.RejectedActionRequest, result.Decision);
    }

    [TestMethod]
    public void VerifiedLowRisk_CannotProduceActionPlan_OrSafeAction()
    {
        var signals = CreateAcceptedOcrSignals()
            .Concat(new[]
            {
                CreateNonOcrSignal("qa-fixture-roma", NodalOsAssistedVerificationSignalKind.KnownQaFixtureSignal, expectedText: "ROMA")
            })
            .ToArray();

        var result = Evaluate(CreateRequest(signals));

        Assert.AreEqual(NodalOsAssistedVerificationDecision.VerifiedLowRisk, result.Decision);
        Assert.IsTrue(result.ReadOnlyObservationOnly);
        Assert.IsFalse(result.ActionsAllowed);
        Assert.IsFalse(result.CanProduceActionPlan);
        Assert.IsFalse(result.CanProduceSafeAction);
    }

    [TestMethod]
    public void VerifiedLowRisk_CannotApproveClickSubmitSendDeletePaySign()
    {
        var signals = CreateAcceptedOcrSignals()
            .Concat(new[]
            {
                CreateNonOcrSignal("qa-fixture-roma", NodalOsAssistedVerificationSignalKind.ManualExpectedValueSignal, expectedText: "ROMA")
            })
            .ToArray();

        var result = Evaluate(CreateRequest(signals));

        Assert.IsFalse(result.CanApproveAction);
        Assert.IsFalse(result.CanApproveClick);
        Assert.IsFalse(result.CanApproveSubmit);
        Assert.IsFalse(result.CanApproveSend);
        Assert.IsFalse(result.CanApproveDelete);
        Assert.IsFalse(result.CanApprovePay);
        Assert.IsFalse(result.CanApproveSign);
    }

    [TestMethod]
    public void Artifact_ValidatesM336Flags()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m336",
            "paddleocr-ocr-assisted-verification-policy-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M334-M336", root.GetProperty("milestone").GetString());
        Assert.AreEqual("READY_FOR_INTERNAL_LOW_RISK_OCR_ASSISTED_VERIFICATION_FIXTURES", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("assistedVerificationPolicyCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("signalFusionCreated").GetBoolean());
        Assert.IsFalse(root.GetProperty("ocrOnlyVerificationAllowed").GetBoolean());
        Assert.IsTrue(root.GetProperty("nonOcrCorroborationRequired").GetBoolean());
        Assert.IsFalse(root.GetProperty("verifiedLowRiskCanProduceActionPlan").GetBoolean());
        Assert.IsFalse(root.GetProperty("verifiedLowRiskCanProduceSafeAction").GetBoolean());
        Assert.IsFalse(root.GetProperty("highConfidenceOcrAloneCanVerify").GetBoolean());
        Assert.IsFalse(root.GetProperty("exactOcrMatchAloneCanVerify").GetBoolean());
        Assert.IsFalse(root.GetProperty("rejectedOcrCanSupportVerification").GetBoolean());
        Assert.IsFalse(root.GetProperty("uncertainOcrCanSupportVerification").GetBoolean());
        Assert.IsFalse(root.GetProperty("policyViolationOcrCanSupportVerification").GetBoolean());
    }

    [TestMethod]
    public void Git_DoesNotTrackOnnxModels_OrGitignoredDictionaries_ForM336()
    {
        Assert.AreEqual(string.Empty, RunGit("ls-files", "*.onnx").Trim());
        Assert.AreEqual(string.Empty, RunGit("ls-files", "tools/ocr-worker/models/onnx/dictionaries/*").Trim());
    }

    private static NodalOsAssistedVerificationResult Evaluate(NodalOsAssistedVerificationRequest request) =>
        new NodalOsAssistedVerificationPolicy().Evaluate(request);

    private static NodalOsAssistedVerificationRequest CreateRequest(
        IReadOnlyList<NodalOsAssistedVerificationSignal> signals,
        bool containsSensitiveData = false,
        bool containsDocumentData = false,
        bool containsCredentials = false,
        bool fullScreen = false,
        bool actionRequested = false,
        bool approvalRequested = false,
        NodalOsAssistedVerificationRiskLevel riskLevel = NodalOsAssistedVerificationRiskLevel.Low)
    {
        return new NodalOsAssistedVerificationRequest(
            RequestId: "m336-request",
            RiskLevel: riskLevel,
            LowRiskOnly: true,
            ActionRequested: actionRequested,
            ApprovalRequested: approvalRequested,
            ContainsSensitiveData: containsSensitiveData,
            ContainsDocumentData: containsDocumentData,
            ContainsCredentials: containsCredentials,
            FullScreen: fullScreen,
            Signals: signals,
            Reason: "controlled-qa-assisted-verification");
    }

    private static IReadOnlyList<NodalOsAssistedVerificationSignal> CreateAcceptedOcrSignals()
    {
        var evaluation = new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(CreateEnvelope(
            observedText: "ROMA",
            normalizedText: "ROMA",
            expectedText: "ROMA",
            exactMatch: true,
            confidence: 0.99d));
        var result = new NodalOsOcrFsmObservationConsumer().Consume(new NodalOsOcrFsmObservationInput("m336-input", new[] { evaluation }));
        return new NodalOsAssistedVerificationPolicy().CreateSignals(result);
    }

    private static IReadOnlyList<NodalOsAssistedVerificationSignal> CreateDiagnosticOnlySignals(bool rejected)
    {
        var evaluation = new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(CreateEnvelope(
            acceptanceState: rejected ? NodalOsOcrObservationAcceptanceState.RejectedWrongWindow : NodalOsOcrObservationAcceptanceState.UncertainRequiresExpansion,
            accepted: false,
            regionVerified: !rejected,
            confidenceGatePassed: false,
            confidence: 0.60d,
            rejectionReason: rejected ? "wrong-window" : "needs-more-evidence"));
        var result = new NodalOsOcrFsmObservationConsumer().Consume(new NodalOsOcrFsmObservationInput("m336-input", new[] { evaluation }));
        return new NodalOsAssistedVerificationPolicy().CreateSignals(result);
    }

    private static NodalOsAssistedVerificationSignal CreateRejectedPolicySignal() =>
        new(
            SignalId: "policy-violation",
            Kind: NodalOsAssistedVerificationSignalKind.Rejected,
            SupportsVerification: false,
            DiagnosticOnly: false,
            Rejected: true,
            Source: "OcrPolicyViolation",
            ExpectedText: "ROMA",
            ObservedText: "ROMA",
            NormalizedText: "ROMA",
            ExactMatch: true,
            NormalizedMatch: true,
            EditDistance: 0,
            ConfidenceBand: NodalOsOcrEvidenceConfidenceBand.Rejected,
            RegionVerified: false,
            ConfidenceGatePassed: false,
            FingerprintHashMatch: false,
            DiffScore: 1d,
            NoAuthority: true,
            EvidenceOnly: true,
            ActionAllowed: false,
            Reason: "policy-violation",
            SourceCategory: NodalOsOcrObservationSource.RealQaWindowRegion,
            CaptureMode: "real-qa-window-region",
            WindowTitleOrSource: "NODAL OS OCR QA Window",
            ProcessOrSource: "OneBrain.Tools.QaWindowHost",
            RegionBounds: new NodalOsScreenRegionBounds(70, 54, 660, 180));

    private static NodalOsAssistedVerificationSignal CreateNonOcrSignal(
        string signalId,
        NodalOsAssistedVerificationSignalKind kind,
        string expectedText) =>
        new(
            SignalId: signalId,
            Kind: kind,
            SupportsVerification: true,
            DiagnosticOnly: false,
            Rejected: false,
            Source: "ControlledQaNonOcrSignal",
            ExpectedText: expectedText,
            ObservedText: expectedText,
            NormalizedText: expectedText,
            ExactMatch: true,
            NormalizedMatch: true,
            EditDistance: 0,
            ConfidenceBand: NodalOsOcrEvidenceConfidenceBand.High,
            RegionVerified: true,
            ConfidenceGatePassed: true,
            FingerprintHashMatch: true,
            DiffScore: 0d,
            NoAuthority: true,
            EvidenceOnly: true,
            ActionAllowed: false,
            Reason: "controlled corroboration",
            SourceCategory: null,
            CaptureMode: null,
            WindowTitleOrSource: null,
            ProcessOrSource: null,
            RegionBounds: null);

    private static NodalOsOcrEvidenceEnvelope CreateEnvelope(
        NodalOsOcrObservationAcceptanceState acceptanceState = NodalOsOcrObservationAcceptanceState.AcceptedEvidence,
        bool accepted = true,
        bool regionVerified = true,
        bool confidenceGatePassed = true,
        bool exactMatch = true,
        double confidence = 0.99d,
        string rejectionReason = "",
        double diffScore = 0d,
        string observedText = "ROMA",
        string normalizedText = "ROMA",
        string expectedText = "ROMA")
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
            EditDistance: exactMatch ? 0 : 2,
            ExactOrNormalizedMatch: exactMatch,
            Reason: confidenceGatePassed ? "passed" : "failed");
        var result = new NodalOsOcrObservationResult(
            "obs-qa-real-window-roma",
            NodalOsOcrObservationDecision.AcceptedEvidenceOnly,
            accepted,
            observedText,
            normalizedText,
            expectedText,
            ExactMatch: exactMatch,
            NormalizedMatch: false,
            EditDistance: exactMatch ? 0 : 2,
            Confidence: confidence,
            Warnings: [],
            NoAuthority: true,
            ActionAllowed: false,
            EvidenceOnly: true,
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
}
