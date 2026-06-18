using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
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
public sealed class NodalOsOcrFsmObservationM331M333Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void AcceptedAuxiliary_EntersReadOnlyObservationContext()
    {
        var result = Consume(CreateAcceptedEvaluation(0.98d, NodalOsOcrEvidenceConfidenceBand.High));

        Assert.AreEqual(1, result.ObservationContext.Count);
        Assert.AreEqual(0, result.Diagnostics.Count);
        Assert.AreEqual(NodalOsOcrFsmObservationDisposition.ObservationContext, result.ObservationContext[0].Disposition);
    }

    [TestMethod]
    public void RejectedDiagnostic_DoesNotEnterObservationContext_ButEntersDiagnostics()
    {
        var result = Consume(CreateRejectedEvaluation());

        Assert.AreEqual(0, result.ObservationContext.Count);
        Assert.AreEqual(1, result.Diagnostics.Count);
        Assert.AreEqual(NodalOsOcrFsmObservationDisposition.DiagnosticOnly, result.Diagnostics[0].Disposition);
    }

    [TestMethod]
    public void UncertainDiagnostic_DoesNotEnterObservationContext_ButEntersDiagnostics()
    {
        var result = Consume(CreateUncertainEvaluation());

        Assert.AreEqual(0, result.ObservationContext.Count);
        Assert.AreEqual(1, result.Diagnostics.Count);
        Assert.AreEqual(NodalOsOcrEvidenceLedgerStatus.RecordedDiagnosticUncertain, result.Diagnostics[0].LedgerStatus);
    }

    [TestMethod]
    public void PolicyViolation_ExcludedFromObservation()
    {
        var result = Consume(CreatePolicyViolationEvaluation());

        Assert.AreEqual(0, result.ObservationContext.Count);
        Assert.AreEqual(0, result.Diagnostics.Count);
        Assert.AreEqual(1, result.Excluded.Count);
        Assert.AreEqual(NodalOsOcrFsmObservationDisposition.ExcludedPolicyViolation, result.Excluded[0].Disposition);
    }

    [TestMethod]
    public void HighConfidence_RanksAboveMedium()
    {
        var result = Consume(
            CreateAcceptedEvaluation(0.99d, NodalOsOcrEvidenceConfidenceBand.High),
            CreateAcceptedEvaluation(0.80d, NodalOsOcrEvidenceConfidenceBand.Medium));

        Assert.AreEqual(2, result.ObservationContext.Count);
        Assert.AreEqual(NodalOsOcrEvidenceConfidenceBand.High, result.ObservationContext[0].ConfidenceBand);
        Assert.AreEqual(NodalOsOcrEvidenceConfidenceBand.Medium, result.ObservationContext[1].ConfidenceBand);
    }

    [TestMethod]
    public void Medium_RanksAboveLow()
    {
        var result = Consume(
            CreateAcceptedEvaluation(0.81d, NodalOsOcrEvidenceConfidenceBand.Medium),
            CreateAcceptedEvaluation(0.60d, NodalOsOcrEvidenceConfidenceBand.Low));

        Assert.AreEqual(NodalOsOcrEvidenceConfidenceBand.Medium, result.ObservationContext[0].ConfidenceBand);
        Assert.AreEqual(NodalOsOcrEvidenceConfidenceBand.Low, result.ObservationContext[1].ConfidenceBand);
    }

    [TestMethod]
    public void DiagnosticUncertain_RanksBelowAcceptedAuxiliary_AndAboveRejected()
    {
        var result = Consume(
            CreateAcceptedEvaluation(0.98d, NodalOsOcrEvidenceConfidenceBand.High),
            CreateUncertainEvaluation(),
            CreateRejectedEvaluation());

        Assert.AreEqual(1, result.ObservationContext.Count);
        Assert.AreEqual(2, result.Diagnostics.Count);
        Assert.AreEqual(NodalOsOcrEvidenceLedgerStatus.RecordedDiagnosticUncertain, result.Diagnostics[0].LedgerStatus);
        Assert.AreEqual(NodalOsOcrEvidenceLedgerStatus.RecordedDiagnosticRejected, result.Diagnostics[1].LedgerStatus);
    }

    [TestMethod]
    public void ExactMatch_DoesNotOverrideFailedRegionVerification()
    {
        var result = Consume(CreateExactMatchButFailedVerificationEvaluation());

        Assert.AreEqual(0, result.ObservationContext.Count);
        Assert.AreEqual(1, result.Excluded.Count);
    }

    [TestMethod]
    public void HighConfidence_CannotAuthorizeAction()
    {
        var result = Consume(CreateAcceptedEvaluation(0.99d, NodalOsOcrEvidenceConfidenceBand.High));

        Assert.IsFalse(result.CanApproveAction);
        Assert.IsFalse(result.CanApproveClick);
        Assert.IsFalse(result.CanApproveSubmit);
        Assert.IsFalse(result.CanApproveSend);
        Assert.IsFalse(result.CanApproveDelete);
        Assert.IsFalse(result.CanApprovePay);
        Assert.IsFalse(result.CanApproveSign);
    }

    [TestMethod]
    public void ObservationSummary_CannotProduceActionPlan_OrSafeAction()
    {
        var result = Consume(CreateAcceptedEvaluation(0.99d, NodalOsOcrEvidenceConfidenceBand.High));

        Assert.IsTrue(result.ReadOnlyObservationOnly);
        Assert.IsFalse(result.CanProduceActionPlan);
        Assert.IsFalse(result.CanProduceSafeAction);
    }

    [TestMethod]
    public void Provenance_IsPreserved()
    {
        var result = Consume(CreateAcceptedEvaluation(0.99d, NodalOsOcrEvidenceConfidenceBand.High));
        var ranking = result.ObservationContext.Single();

        Assert.AreEqual(NodalOsOcrObservationSource.RealQaWindowRegion, ranking.SourceCategory);
        Assert.AreEqual("real-qa-window-region", ranking.CaptureMode);
        Assert.AreEqual("NODAL OS OCR QA Window", ranking.WindowTitleOrSource);
        Assert.AreEqual("OneBrain.Tools.QaWindowHost", ranking.ProcessOrSource);
        Assert.IsNotNull(ranking.RegionBounds);
        Assert.IsTrue(result.ProvenancePreserved);
    }

    [TestMethod]
    public void Artifact_ValidatesM333Flags()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m333",
            "paddleocr-ocr-fsm-observation-wiring-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M331-M333", root.GetProperty("milestone").GetString());
        Assert.AreEqual("READY_FOR_OCR_ASSISTED_VERIFICATION_POLICY_DESIGN", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("fsmObservationWiringAttempted").GetBoolean());
        Assert.IsTrue(root.GetProperty("fsmObservationConsumerCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("ocrEvidenceRankingCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("readOnlyObservationOnly").GetBoolean());
        Assert.IsFalse(root.GetProperty("canProduceActionPlan").GetBoolean());
        Assert.IsFalse(root.GetProperty("canProduceSafeAction").GetBoolean());
        Assert.IsFalse(root.GetProperty("canApproveAction").GetBoolean());
        Assert.IsTrue(root.GetProperty("acceptedAuxiliaryCanEnterObservationContext").GetBoolean());
        Assert.IsFalse(root.GetProperty("diagnosticRejectedCanEnterObservationContext").GetBoolean());
        Assert.IsTrue(root.GetProperty("diagnosticRejectedCanEnterDiagnostics").GetBoolean());
        Assert.IsFalse(root.GetProperty("diagnosticUncertainCanEnterObservationContext").GetBoolean());
        Assert.IsTrue(root.GetProperty("diagnosticUncertainCanEnterDiagnostics").GetBoolean());
        Assert.IsTrue(root.GetProperty("policyViolationExcludedFromObservation").GetBoolean());
        Assert.IsTrue(root.GetProperty("highConfidenceCannotAuthorizeAction").GetBoolean());
    }

    [TestMethod]
    public void Git_DoesNotTrackOnnxModels_OrGitignoredDictionaries_ForM333()
    {
        Assert.AreEqual(string.Empty, RunGit("ls-files", "*.onnx").Trim());
        Assert.AreEqual(string.Empty, RunGit("ls-files", "tools/ocr-worker/models/onnx/dictionaries/*").Trim());
    }

    private static NodalOsOcrFsmObservationResult Consume(params NodalOsOcrEvidencePolicyEvaluation[] evaluations)
    {
        var input = new NodalOsOcrFsmObservationInput("m333-input", evaluations);
        return new NodalOsOcrFsmObservationConsumer().Consume(input);
    }

    private static NodalOsOcrEvidencePolicyEvaluation CreateAcceptedEvaluation(double confidence, NodalOsOcrEvidenceConfidenceBand expectedBand)
    {
        var evaluation = new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(CreateEnvelope(confidence: confidence));
        Assert.AreEqual(NodalOsOcrEvidenceLedgerStatus.AcceptedAuxiliary, evaluation.LedgerStatus);
        Assert.IsNotNull(evaluation.Entry);
        Assert.AreEqual(expectedBand, evaluation.Entry.ConfidenceBand);
        return evaluation;
    }

    private static NodalOsOcrEvidencePolicyEvaluation CreateRejectedEvaluation()
    {
        return new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(
            CreateEnvelope(
                acceptanceState: NodalOsOcrObservationAcceptanceState.RejectedWrongWindow,
                accepted: false,
                regionVerified: false,
                confidenceGatePassed: false,
                rejectionReason: "wrong-window"));
    }

    private static NodalOsOcrEvidencePolicyEvaluation CreateUncertainEvaluation()
    {
        return new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(
            CreateEnvelope(
                acceptanceState: NodalOsOcrObservationAcceptanceState.UncertainRequiresExpansion,
                accepted: false,
                regionVerified: true,
                confidenceGatePassed: false,
                confidence: 0.55d,
                rejectionReason: "needs-expansion"));
    }

    private static NodalOsOcrEvidencePolicyEvaluation CreatePolicyViolationEvaluation()
    {
        return new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(CreateEnvelope(actionAllowed: true));
    }

    private static NodalOsOcrEvidencePolicyEvaluation CreateExactMatchButFailedVerificationEvaluation()
    {
        return new NodalOsOcrEvidenceRuntimePolicyGate().Evaluate(
            CreateEnvelope(
                accepted: true,
                regionVerified: false,
                confidenceGatePassed: true,
                exactMatch: true,
                confidence: 0.99d));
    }

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
        string fingerprintHash = "ABC")
    {
        var fingerprint = new NodalOsRegionCaptureFingerprint(fingerprintHash, 660, 180, 200, 200, 200, 0.1, "sig");
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
}
