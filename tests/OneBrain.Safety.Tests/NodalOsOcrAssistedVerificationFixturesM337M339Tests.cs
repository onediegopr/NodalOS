using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OcrAssistedVerificationFixtures")]
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
public sealed class NodalOsOcrAssistedVerificationFixturesM337M339Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void OcrPlusKnownQaFixtureSignal_PassesVerifiedLowRisk()
    {
        var result = ExecuteFixture("assisted-qa-pvc-wall-known-fixture-pass");

        Assert.AreEqual(NodalOsAssistedVerificationDecision.VerifiedLowRisk, result.Decision);
        Assert.AreEqual(NodalOsAssistedVerificationSignalKind.KnownQaFixtureSignal, result.NonOcrSignalKind);
        Assert.IsTrue(result.CorroborationSatisfied);
    }

    [TestMethod]
    public void OcrPlusManualExpectedValueSignal_PassesVerifiedLowRisk()
    {
        var result = ExecuteFixture("assisted-qa-roma-manual-pass");

        Assert.AreEqual(NodalOsAssistedVerificationDecision.VerifiedLowRisk, result.Decision);
        Assert.AreEqual(NodalOsAssistedVerificationSignalKind.ManualExpectedValueSignal, result.NonOcrSignalKind);
    }

    [TestMethod]
    public void OcrOnlyExact_FailsRejectedOcrOnly()
    {
        var result = ExecuteFixture("assisted-qa-roma-ocr-only-rejected");

        Assert.AreEqual(NodalOsAssistedVerificationDecision.RejectedOcrOnly, result.Decision);
        Assert.IsFalse(result.CorroborationSatisfied);
    }

    [TestMethod]
    public void OcrOnlyHighConfidence_Fails()
    {
        var result = ExecuteFixture("assisted-qa-roma-ocr-only-rejected");

        Assert.AreEqual(NodalOsAssistedVerificationDecision.RejectedOcrOnly, result.Decision);
    }

    [TestMethod]
    public void OcrPlusNonOcrMismatch_ReturnsNeedsMoreEvidence()
    {
        var result = ExecuteFixture("assisted-qa-roma-mismatch-needs-more-evidence");

        Assert.AreEqual(NodalOsAssistedVerificationDecision.NeedsMoreEvidence, result.Decision);
    }

    [TestMethod]
    public void RejectedOcr_Fails()
    {
        var result = ExecuteFixture("assisted-qa-rejected-ocr-not-verified");

        Assert.AreEqual(NodalOsAssistedVerificationDecision.NotVerified, result.Decision);
        Assert.AreEqual("Rejected", result.OcrSignalState);
    }

    [TestMethod]
    public void UncertainOcr_Fails()
    {
        var result = ExecuteFixture("assisted-qa-uncertain-ocr-needs-more-evidence");

        Assert.AreEqual(NodalOsAssistedVerificationDecision.NeedsMoreEvidence, result.Decision);
        Assert.AreEqual("Uncertain", result.OcrSignalState);
    }

    [TestMethod]
    public void PolicyViolationOcr_Fails()
    {
        var policy = new NodalOsAssistedVerificationPolicy();
        var request = new NodalOsAssistedVerificationRequest(
            "policy-violation",
            NodalOsAssistedVerificationRiskLevel.Low,
            LowRiskOnly: true,
            ActionRequested: false,
            ApprovalRequested: false,
            ContainsSensitiveData: false,
            ContainsDocumentData: false,
            ContainsCredentials: false,
            FullScreen: false,
            Signals:
            [
                new NodalOsAssistedVerificationSignal(
                    "policy-violation:ocr",
                    NodalOsAssistedVerificationSignalKind.Rejected,
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
                    RegionBounds: new NodalOsScreenRegionBounds(70, 54, 660, 180))
            ],
            Reason: "policy-violation");

        var result = policy.Evaluate(request);

        Assert.AreEqual(NodalOsAssistedVerificationDecision.RejectedPolicyViolation, result.Decision);
    }

    [TestMethod]
    public void SensitiveRequest_Rejected()
    {
        Assert.AreEqual(
            NodalOsAssistedVerificationDecision.RejectedSensitive,
            ExecuteFixture("assisted-qa-sensitive-rejected").Decision);
    }

    [TestMethod]
    public void FullScreenRequest_Rejected()
    {
        Assert.AreEqual(
            NodalOsAssistedVerificationDecision.RejectedFullScreen,
            ExecuteFixture("assisted-qa-fullscreen-rejected").Decision);
    }

    [TestMethod]
    public void DocumentRequest_Rejected()
    {
        var policy = new NodalOsAssistedVerificationPolicy();
        var request = new NodalOsAssistedVerificationRequest(
            "document-rejected",
            NodalOsAssistedVerificationRiskLevel.Low,
            LowRiskOnly: true,
            ActionRequested: false,
            ApprovalRequested: false,
            ContainsSensitiveData: false,
            ContainsDocumentData: true,
            ContainsCredentials: false,
            FullScreen: false,
            Signals:
            [
                new NodalOsAssistedVerificationSignal(
                    "document-rejected:ocr",
                    NodalOsAssistedVerificationSignalKind.OcrAuxiliaryEvidence,
                    SupportsVerification: true,
                    DiagnosticOnly: false,
                    Rejected: false,
                    Source: "FixtureOcrAcceptedAuxiliary",
                    ExpectedText: "ROMA",
                    ObservedText: "ROMA",
                    NormalizedText: "ROMA",
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
                    Reason: "fixture accepted OCR auxiliary evidence",
                    SourceCategory: NodalOsOcrObservationSource.RealQaWindowRegion,
                    CaptureMode: "real-qa-window-region",
                    WindowTitleOrSource: "NODAL OS OCR QA Window",
                    ProcessOrSource: "OneBrain.Tools.QaWindowHost",
                    RegionBounds: new NodalOsScreenRegionBounds(70, 54, 660, 180))
            ],
            Reason: "document-rejected");

        var result = policy.Evaluate(request);

        Assert.AreEqual(NodalOsAssistedVerificationDecision.RejectedDocument, result.Decision);
    }

    [TestMethod]
    public void ActionRequest_Rejected()
    {
        Assert.AreEqual(
            NodalOsAssistedVerificationDecision.RejectedActionRequest,
            ExecuteFixture("assisted-qa-action-request-rejected").Decision);
    }

    [TestMethod]
    public void VerifiedLowRisk_CannotProduceActionPlan_OrSafeAction()
    {
        var result = ExecuteFixture("assisted-qa-pvc-wall-known-fixture-pass");

        Assert.IsFalse(result.ActionsAllowed);
        Assert.IsFalse(result.CanProduceActionPlan);
        Assert.IsFalse(result.CanProduceSafeAction);
    }

    [TestMethod]
    public void VerifiedLowRisk_CannotApproveClickSubmitSendDeletePaySign()
    {
        var result = ExecuteFixture("assisted-qa-roma-manual-pass");

        Assert.IsFalse(result.CanApproveClick);
        Assert.IsFalse(result.CanApproveSubmit);
        Assert.IsFalse(result.CanApproveSend);
        Assert.IsFalse(result.CanApproveDelete);
        Assert.IsFalse(result.CanApprovePay);
        Assert.IsFalse(result.CanApproveSign);
    }

    [TestMethod]
    public void ResidualOcrMismatch_CurrentPolicyReturnsNeedsMoreEvidence()
    {
        var result = ExecuteFixture("assisted-qa-pvc-wali-residual-needs-more-evidence");

        Assert.AreEqual(NodalOsAssistedVerificationDecision.NeedsMoreEvidence, result.Decision);
        CollectionAssert.Contains(result.Warnings.ToArray(), "OCR and non-OCR values did not corroborate");
    }

    [TestMethod]
    public void FixtureSummary_RecordsExpectedOutcomes()
    {
        var summary = ExecuteSummary();

        Assert.AreEqual(10, summary.FixturesTotal);
        Assert.AreEqual(2, summary.PassingFixturesPassed);
        Assert.AreEqual(8, summary.FailingFixturesRejected);
        Assert.AreEqual(0, summary.UnexpectedPasses);
        Assert.AreEqual(0, summary.UnexpectedFailures);
    }

    [TestMethod]
    public void Artifact_ValidatesM339Flags()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m339",
            "paddleocr-ocr-assisted-verification-fixtures-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M337-M339", root.GetProperty("milestone").GetString());
        Assert.AreEqual("READY_FOR_OCR_ASSISTED_VERIFICATION_AUDIT", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("assistedVerificationFixturesCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("fixturesExecuted").GetBoolean());
        Assert.IsFalse(root.GetProperty("ocrOnlyVerificationAllowed").GetBoolean());
        Assert.IsTrue(root.GetProperty("nonOcrCorroborationRequired").GetBoolean());
        Assert.AreEqual(10, root.GetProperty("fixturesTotal").GetInt32());
        Assert.AreEqual(2, root.GetProperty("passingFixturesPassed").GetInt32());
        Assert.AreEqual(8, root.GetProperty("failingFixturesRejected").GetInt32());
        Assert.AreEqual(0, root.GetProperty("unexpectedPasses").GetInt32());
        Assert.AreEqual(0, root.GetProperty("unexpectedFailures").GetInt32());
    }

    [TestMethod]
    public void Git_DoesNotTrackOnnxModels_OrGitignoredDictionaries_ForM339()
    {
        Assert.AreEqual(string.Empty, RunGit("ls-files", "*.onnx").Trim());
        Assert.AreEqual(string.Empty, RunGit("ls-files", "tools/ocr-worker/models/onnx/dictionaries/*").Trim());
    }

    private static NodalOsAssistedVerificationFixtureExecutionResult ExecuteFixture(string fixtureId) =>
        ExecuteSummary().Fixtures.Single(f => f.FixtureId == fixtureId);

    private static NodalOsAssistedVerificationFixtureExecutionSummary ExecuteSummary()
    {
        var fixtureSet = new NodalOsAssistedVerificationFixtureSet();
        return fixtureSet.Execute(fixtureSet.CreateDefaultFixtureCases());
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
