using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OcrVisionControlledActivation")]
[TestCategory("OcrVisionActivationGate")]
[TestCategory("LocalOcrWorkerBoundary")]
[TestCategory("LocalOcrWorkerContract")]
[TestCategory("OcrRealActivationReadiness")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("OcrVisionBudget")]
[TestCategory("OcrVisionProviderRegistry")]
[TestCategory("OcrVisionEvaluation")]
[TestCategory("BrowserGroundingSnapshot")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsOcrVisionControlledActivationM178M180Tests
{
    private readonly NodalOsLocalOcrWorkerBoundaryService worker = new();
    private readonly NodalOsOcrRealActivationGate gate = new();
    private readonly NodalOsImageCropRedactor redactor = new();

    [TestMethod]
    public void ControlledActivationAdrModelsStatesAndKeepsRealOcrDisabled()
    {
        var adr = File.ReadAllText(SourcePath("docs", "adr", "ocr-vision-controlled-activation-m178-m180.md"));

        StringAssert.Contains(adr, "ModelOnly");
        StringAssert.Contains(adr, "ShadowEvaluation");
        StringAssert.Contains(adr, "LocalWorkerEnabledForSynthetic");
        StringAssert.Contains(adr, "SaaS OCR remains disabled-by-default");
        StringAssert.Contains(adr, "OCR/Vision remains no-authority");
        StringAssert.Contains(adr, "No real OCR is activated");
    }

    [TestMethod]
    public void LocalWorkerNotInstalledIsUnavailableAndDoesNotInvokeProcess()
    {
        var contract = worker.CreateModelOnlyContract();
        var response = worker.InvokeModelOnly(contract, Request());

        Assert.AreEqual(NodalOsLocalOcrWorkerHealthStatus.NotInstalled, contract.Health.Status);
        Assert.IsFalse(contract.Health.Installed);
        Assert.IsFalse(contract.Health.Available);
        Assert.IsFalse(contract.RuntimeProfile.InvokesExternalProcess);
        Assert.AreEqual(NodalOsLocalOcrWorkerError.WorkerNotInstalled, response.Error);
        Assert.IsFalse(response.InvokedExternalProcess);
        Assert.IsFalse(response.CallsRealOcr);
        Assert.IsTrue(response.NoAuthority);
    }

    [TestMethod]
    public void LocalWorkerDisabledCannotRun()
    {
        var contract = worker.CreateModelOnlyContract(NodalOsLocalOcrWorkerHealthStatus.Available, enabled: false);
        var response = worker.InvokeModelOnly(contract, Request());

        Assert.AreEqual(NodalOsLocalOcrWorkerHealthStatus.InstalledButDisabled, contract.Health.Status);
        Assert.AreEqual(NodalOsLocalOcrWorkerError.WorkerDisabled, response.Error);
        Assert.IsFalse(response.CallsRealOcr);
    }

    [TestMethod]
    public void LocalWorkerBlocksRedactionFailedFullScreenAndSensitiveSurface()
    {
        var contract = worker.CreateModelOnlyContract(NodalOsLocalOcrWorkerHealthStatus.Available, enabled: true);

        Assert.AreEqual(NodalOsLocalOcrWorkerError.RedactionFailed, worker.InvokeModelOnly(contract, Request(redaction: NodalOsGroundingRedactionStatus.RedactionFailed)).Error);
        Assert.AreEqual(NodalOsLocalOcrWorkerError.FullScreenBlocked, worker.InvokeModelOnly(contract, Request(fullScreen: true)).Error);
        Assert.AreEqual(NodalOsLocalOcrWorkerError.SensitiveSurfaceBlocked, worker.InvokeModelOnly(contract, Request(sensitivity: NodalOsOcrVisionSensitivity.SensitiveSurface)).Error);
    }

    [TestMethod]
    public void LocalWorkerAllowsRedactedSyntheticCropInModelOnlyAndLowConfidenceRequiresHuman()
    {
        var contract = worker.CreateModelOnlyContract(NodalOsLocalOcrWorkerHealthStatus.Available, enabled: true);
        var paddle = worker.InvokeModelOnly(contract, Request(engine: NodalOsOcrEngineHint.PaddleOcr));
        var tesseract = worker.InvokeModelOnly(contract, Request(engine: NodalOsOcrEngineHint.Tesseract));

        Assert.AreEqual(NodalOsLocalOcrWorkerError.None, paddle.Error);
        Assert.AreEqual(NodalOsLocalOcrWorkerHealthStatus.Available, paddle.WorkerStatus);
        Assert.IsFalse(paddle.CallsRealOcr);
        Assert.IsFalse(paddle.CallsRealSaas);
        Assert.IsFalse(paddle.CanApproveAction);
        Assert.IsFalse(paddle.CanClick);
        Assert.IsFalse(paddle.CanSubmit);
        Assert.IsTrue(paddle.NoAuthority);
        Assert.AreEqual(NodalOsLocalOcrWorkerHealthStatus.Degraded, tesseract.WorkerStatus);
        Assert.IsTrue(tesseract.RequiresHumanReview);
    }

    [TestMethod]
    public void ActivationGateDefaultCurrentPhaseBlocksRealOcr()
    {
        var decision = gate.Evaluate(gate.CurrentPhaseReadiness());

        Assert.AreEqual(NodalOsOcrActivationDecisionKind.BlockedByDefault, decision.Decision);
        Assert.AreEqual(NodalOsOcrVisionActivationState.BlockedByPolicy, decision.ActivationState);
        Assert.IsFalse(decision.RealOcrEnabled);
        Assert.IsFalse(decision.RealSaasEnabled);
        Assert.IsTrue(decision.NoAuthority);
    }

    [TestMethod]
    public void ActivationGateBlocksMissingOptInWorkerRedactionSensitiveBudgetPrivacyAuditAndNoAuthority()
    {
        Assert.AreEqual(NodalOsOcrActivationDecisionKind.BlockedByMissingOptIn, gate.Evaluate(Readiness(optIn: false)).Decision);
        Assert.AreEqual(NodalOsOcrActivationDecisionKind.BlockedByMissingWorker, gate.Evaluate(Readiness(workerAvailable: false)).Decision);
        Assert.AreEqual(NodalOsOcrActivationDecisionKind.BlockedByRedaction, gate.Evaluate(Readiness(redaction: false)).Decision);
        Assert.AreEqual(NodalOsOcrActivationDecisionKind.BlockedBySensitivePolicy, gate.Evaluate(Readiness(sensitivePolicy: false)).Decision);
        Assert.AreEqual(NodalOsOcrActivationDecisionKind.BlockedByBudget, gate.Evaluate(Readiness(budget: false)).Decision);
        Assert.AreEqual(NodalOsOcrActivationDecisionKind.BlockedByPrivacy, gate.Evaluate(Readiness(privacy: false)).Decision);
        Assert.AreEqual(NodalOsOcrActivationDecisionKind.BlockedByMissingAudit, gate.Evaluate(Readiness(audit: false)).Decision);
        Assert.AreEqual(NodalOsOcrActivationDecisionKind.BlockedByNoAuthorityViolation, gate.Evaluate(Readiness(noAuthority: false)).Decision);
    }

    [TestMethod]
    public void ActivationGateCanModelSyntheticReadinessWithoutEnablingRealOcr()
    {
        var decision = gate.Evaluate(Readiness());

        Assert.AreEqual(NodalOsOcrActivationDecisionKind.ReadyForSyntheticOnly, decision.Decision);
        Assert.AreEqual(NodalOsOcrVisionActivationState.LocalWorkerEnabledForSynthetic, decision.ActivationState);
        Assert.IsFalse(decision.RealOcrEnabled);
        Assert.IsFalse(decision.RealSaasEnabled);
        Assert.IsTrue(decision.NoAuthority);
        Assert.IsTrue(decision.Requirements.All(requirement => requirement.Satisfied));
    }

    [TestMethod]
    public void SaasRealActivationRemainsBlockedInCurrentPhase()
    {
        var readiness = Readiness(kind: NodalOsOcrVisionProviderKind.CloudOpenAiVision, allowsSaas: true, currentPhaseAllowsSaas: false);
        var decision = gate.Evaluate(readiness);

        Assert.AreEqual(NodalOsOcrActivationDecisionKind.BlockedByDefault, decision.Decision);
        Assert.IsFalse(decision.RealSaasEnabled);
        Assert.IsFalse(decision.RealOcrEnabled);
    }

    [TestMethod]
    public void ActivationReadinessReportAndRunbookExist()
    {
        var report = File.ReadAllText(SourcePath("docs", "reports", "ocr-vision-controlled-activation-readiness-m180.md"));
        var runbook = File.ReadAllText(SourcePath("docs", "runbooks", "nodal-os-internal-preview-operator-ux-guide-m148-m150.md"));

        StringAssert.Contains(report, "real OCR remains disabled");
        StringAssert.Contains(report, "PaddleOCR");
        StringAssert.Contains(report, "SaaS OCR remains disabled");
        StringAssert.Contains(runbook, "Reading OCR/Vision Activation States");
        StringAssert.Contains(runbook, "ReadyForSyntheticOnly");
        StringAssert.Contains(runbook, "OCR/Vision remains no-authority");
    }

    private static NodalOsLocalOcrWorkerRequest Request(
        NodalOsOcrEngineHint engine = NodalOsOcrEngineHint.PaddleOcr,
        NodalOsGroundingRedactionStatus redaction = NodalOsGroundingRedactionStatus.RedactedSafe,
        NodalOsOcrVisionSensitivity sensitivity = NodalOsOcrVisionSensitivity.Low,
        bool fullScreen = false) =>
        new NodalOsLocalOcrWorkerRequest(
            "worker-request-1",
            new NodalOsGroundingSnapshotId("snapshot-worker-1"),
            "crop:redacted",
            "screenshot:redacted",
            new NodalOsOcrBoundingBox(1, 2, 120, 40),
            engine,
            sensitivity,
            redaction,
            Synthetic: true,
            fullScreen,
            CropRedacted: true,
            ImageWidth: 800,
            ImageHeight: 600,
            Pages: 1,
            MaxLatencyMs: 1000,
            PersistRawImage: false,
            Redacted: true)
        {
            RedactionResult = redaction == NodalOsGroundingRedactionStatus.RedactedSafe
                ? ValidRedactionResult()
                : null
        };

    private static NodalOsImageCropRedactionResult ValidRedactionResult()
    {
        var localRedactor = new NodalOsImageCropRedactor();
        return localRedactor.Redact(new NodalOsImageCropRedactionRequest(
            "worker-redaction-test",
            new NodalOsGroundingSnapshotId("snapshot-worker-1"),
            "crop:redacted",
            System.Text.Encoding.UTF8.GetBytes("clean local worker crop"),
            new NodalOsOcrBoundingBox(1, 2, 120, 40),
            "test-fixture",
            NodalOsOcrVisionSensitivity.Low,
            NodalOsOcrPurpose.EvidenceDebug,
            AllowPersistence: false,
            AllowFullScreen: false,
            localRedactor.DefaultPolicy()));
    }

    private static NodalOsOcrActivationReadiness Readiness(
        bool optIn = true,
        bool workerAvailable = true,
        bool redaction = true,
        bool sensitivePolicy = true,
        bool budget = true,
        bool privacy = true,
        bool audit = true,
        bool noAuthority = true,
        NodalOsOcrVisionProviderKind kind = NodalOsOcrVisionProviderKind.LocalPaddleOcr,
        bool allowsSaas = false,
        bool currentPhaseAllowsSaas = false) =>
        new NodalOsOcrActivationReadiness(
            new NodalOsOcrVisionProviderId(kind == NodalOsOcrVisionProviderKind.LocalPaddleOcr ? "local-paddleocr-stub" : "cloud-openai-vision-disabled"),
            kind,
            new NodalOsOcrActivationScope(NodalOsOcrActivationScopeKind.SyntheticOnly, LocalOnly: !allowsSaas, allowsSaas, AllowsFullScreen: false, AllowsSensitive: false, "synthetic-only activation model"),
            ProviderExplicitlyEnabled: true,
            LocalWorkerInstalled: workerAvailable,
            LocalWorkerAvailable: workerAvailable,
            optIn,
            RedactionGatePassed: redaction,
            SensitivePolicyPassed: sensitivePolicy,
            FullScreenDisabledOrApproved: true,
            BudgetConfigured: budget,
            PrivacyProfileAccepted: privacy,
            new NodalOsOcrActivationAuditEvidence(audit, [new NodalOsGroundingEvidenceRef("ocr-activation:audit:redacted", "activation audit fixture", Redacted: true)], audit ? "audit present" : "audit missing", Redacted: true),
            NoAuthorityConfirmed: noAuthority,
            HumanEscalationPolicyConfigured: true,
            EvaluationHarnessPassed: true,
            RollbackPauseConfigured: true,
            currentPhaseAllowsSaas,
            Redacted: true)
        {
            RequiresExternalDataTransfer = allowsSaas
        };

    private static string SourcePath(params string[] relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "OneBrain.slnx")))
                return Path.Combine(new[] { current.FullName }.Concat(relativePath).ToArray());
            current = current.Parent;
        }

        Assert.Fail("Could not locate repository root.");
        return "";
    }
}
