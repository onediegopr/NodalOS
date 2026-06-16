using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("LocalOcrWorkerSkeleton")]
[TestCategory("LocalOcrWorkerBoundary")]
[TestCategory("OcrSyntheticWorkerActivation")]
[TestCategory("OcrSyntheticActivation")]
[TestCategory("OcrWorkerHealth")]
[TestCategory("OcrWorkerIsolation")]
[TestCategory("OcrWorkerFailureMode")]
[TestCategory("OcrImageRedaction")]
[TestCategory("OcrRedactionPrecondition")]
[TestCategory("OcrVisionRouter")]
[TestCategory("OcrVisionControlledActivation")]
[TestCategory("OcrVisionActivationGate")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("BrowserGroundingSnapshot")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsLocalOcrWorkerM182M184Tests
{
    private readonly NodalOsLocalOcrWorkerSkeletonService skeletonService = new();
    private readonly NodalOsOcrSyntheticWorkerActivationService activation = new();
    private readonly NodalOsLocalOcrWorkerHealthChecker health = new();
    private readonly NodalOsImageCropRedactor redactor = new();

    [TestMethod]
    public void WorkerSkeletonAcceptsValidRedactedSyntheticCrop()
    {
        var skeleton = skeletonService.CreateSyntheticOnlySkeleton();
        var response = skeletonService.RunSynthetic(skeleton, Request(Redact("clean synthetic crop")));

        Assert.AreEqual(NodalOsLocalOcrWorkerSkeletonState.SyntheticOnly, response.Status);
        Assert.IsTrue(response.TextBlocks.Count > 0);
        Assert.IsTrue(response.NoAuthority);
        Assert.IsFalse(response.RawPersisted);
        Assert.IsFalse(response.CallsRealOcr);
        Assert.IsFalse(response.CallsExternalProcess);
        Assert.IsFalse(response.CallsRealSaas);
    }

    [TestMethod]
    public void WorkerSkeletonRejectsMissingFailedSensitiveFullScreenAndExternalTransfer()
    {
        var skeleton = skeletonService.CreateSyntheticOnlySkeleton();

        Assert.AreEqual(NodalOsLocalOcrWorkerSkeletonState.FailedHealthCheck, skeletonService.RunSynthetic(skeleton, Request(null)).Status);
        Assert.AreEqual(NodalOsLocalOcrWorkerSkeletonState.FailedHealthCheck, skeletonService.RunSynthetic(skeleton, Request(Redact("uncertain unknown_sensitive"))).Status);
        Assert.AreEqual(NodalOsLocalOcrWorkerSkeletonState.FailedHealthCheck, skeletonService.RunSynthetic(skeleton, Request(Redact("token=abc"))).Status);
        Assert.AreEqual(NodalOsLocalOcrWorkerSkeletonState.FailedHealthCheck, skeletonService.RunSynthetic(skeleton, Request(Redact("clean"), fullScreen: true)).Status);
        Assert.AreEqual(NodalOsLocalOcrWorkerSkeletonState.FailedHealthCheck, skeletonService.RunSynthetic(skeleton, Request(Redact("clean"), external: true)).Status);
    }

    [TestMethod]
    public void SyntheticActivationBlocksByDefaultAndMissingOptIn()
    {
        var defaultDecision = activation.Evaluate(Readiness(syntheticOptIn: false, worker: false));
        var missingOptIn = activation.Evaluate(Readiness(syntheticOptIn: false));

        Assert.AreEqual(NodalOsOcrSyntheticActivationDecisionKind.BlockedByMissingSyntheticOptIn, defaultDecision.Decision);
        Assert.AreEqual(NodalOsOcrSyntheticActivationDecisionKind.BlockedByMissingSyntheticOptIn, missingOptIn.Decision);
        Assert.IsFalse(defaultDecision.RealOcrEnabled);
        Assert.IsFalse(defaultDecision.RealSaasEnabled);
    }

    [TestMethod]
    public void ValidSyntheticPrerequisitesAreReadyForSyntheticOnly()
    {
        var decision = activation.Evaluate(Readiness());

        Assert.AreEqual(NodalOsOcrSyntheticActivationDecisionKind.ReadyForSyntheticOnly, decision.Decision);
        Assert.IsFalse(decision.RealOcrEnabled);
        Assert.IsFalse(decision.RealSaasEnabled);
        Assert.IsTrue(decision.NoAuthority);
    }

    [TestMethod]
    public void SyntheticRunGeneratesAuditRecordAndArtifacts()
    {
        var record = activation.RunSynthetic(skeletonService.CreateSyntheticOnlySkeleton());
        var json = SourcePath("artifacts", "ocr-vision-worker", "m183", "synthetic-worker-run-summary.json");
        var markdown = SourcePath("docs", "reports", "ocr-vision-synthetic-worker-run-m183.md");
        activation.WriteRunRecord(record, json, markdown);

        Assert.AreEqual(4, record.TotalFixtures);
        Assert.AreEqual(4, record.AuditRecords.Count);
        Assert.IsTrue(record.BlockedFixtures >= 1);
        Assert.IsFalse(record.CallsRealOcr);
        Assert.IsFalse(record.CallsRealSaas);
        Assert.IsFalse(record.CallsExternalProcess);
        Assert.IsFalse(record.RawPersisted);
        Assert.IsTrue(File.Exists(json));
        Assert.IsTrue(File.Exists(markdown));
        StringAssert.Contains(File.ReadAllText(markdown), "No real OCR executed: true");
    }

    [TestMethod]
    public void HealthCheckerReportsDisabledDefaultAndPassesSyntheticSafeState()
    {
        var audit = health.Audit(skeletonService.CreateSyntheticOnlySkeleton());

        Assert.IsTrue(audit.WorkerDisabledByDefault);
        Assert.IsTrue(audit.SyntheticOnlyAllowed);
        Assert.IsTrue(audit.ResourceLimitsConfigured);
        Assert.IsTrue(audit.TimeoutConfigured);
        Assert.IsTrue(audit.NoExternalNetwork);
        Assert.IsTrue(audit.NoRawPersistence);
        Assert.IsTrue(audit.NoRealOcr);
        Assert.IsTrue(audit.NoProcessInvocation);
        Assert.IsTrue(audit.NoAuthority);
        Assert.IsTrue(audit.Passed);
    }

    [TestMethod]
    public void HealthCheckerBlocksVersionTimeoutRawExternalAndAuthorityFailures()
    {
        var version = health.Audit(skeletonService.CreateSyntheticOnlySkeleton() with { WorkerContractVersion = "wrong" });
        var timeout = health.Audit(skeletonService.CreateSyntheticOnlySkeleton() with { TimeoutPolicy = new NodalOsLocalOcrWorkerTimeoutPolicy(0, 0, 0, StopWithEvidenceOnTimeout: false) });
        var raw = health.Audit(skeletonService.CreateSyntheticOnlySkeleton() with { RawPersistence = true });
        var external = health.Audit(skeletonService.CreateSyntheticOnlySkeleton() with { CallsExternalProcess = true });
        var authority = health.Audit(skeletonService.CreateSyntheticOnlySkeleton() with { NoAuthority = false });

        Assert.IsTrue(version.FailureModes.Contains(NodalOsLocalOcrWorkerFailureMode.VersionMismatch));
        Assert.IsTrue(timeout.FailureModes.Contains(NodalOsLocalOcrWorkerFailureMode.Timeout));
        Assert.IsTrue(raw.FailureModes.Contains(NodalOsLocalOcrWorkerFailureMode.RawPersistenceAttempted));
        Assert.IsTrue(external.FailureModes.Contains(NodalOsLocalOcrWorkerFailureMode.ExternalProcessAttempted));
        Assert.IsTrue(authority.FailureModes.Contains(NodalOsLocalOcrWorkerFailureMode.AuthorityViolation));
    }

    [TestMethod]
    public void FailureDecisionsBlockOrAskHumanAndPauseProviderAvailable()
    {
        Assert.AreEqual(NodalOsLocalOcrWorkerFailureDecision.StopWithEvidence, health.Decide(NodalOsLocalOcrWorkerFailureMode.Timeout).Decision);
        Assert.AreEqual(NodalOsLocalOcrWorkerFailureDecision.Blocked, health.Decide(NodalOsLocalOcrWorkerFailureMode.RawPersistenceAttempted).Decision);
        Assert.AreEqual(NodalOsLocalOcrWorkerFailureDecision.Blocked, health.Decide(NodalOsLocalOcrWorkerFailureMode.ExternalProcessAttempted).Decision);
        Assert.AreEqual(NodalOsLocalOcrWorkerFailureDecision.Blocked, health.Decide(NodalOsLocalOcrWorkerFailureMode.AuthorityViolation).Decision);
        Assert.AreEqual(NodalOsLocalOcrWorkerFailureDecision.AskHuman, health.Decide(NodalOsLocalOcrWorkerFailureMode.LowConfidence).Decision);
        Assert.AreEqual(NodalOsLocalOcrWorkerRecoveryAction.PauseProvider, health.Decide(NodalOsLocalOcrWorkerFailureMode.WorkerUnavailable).RecoveryAction);
    }

    [TestMethod]
    public void AdrAndRunbookExist()
    {
        var adr = File.ReadAllText(SourcePath("docs", "adr", "local-ocr-worker-synthetic-activation-m182-m184.md"));
        var runbook = File.ReadAllText(SourcePath("docs", "runbooks", "nodal-os-internal-preview-operator-ux-guide-m148-m150.md"));

        StringAssert.Contains(adr, "worker skeleton");
        StringAssert.Contains(adr, "synthetic-only activation");
        StringAssert.Contains(adr, "no real OCR");
        StringAssert.Contains(runbook, "Reading Local OCR Synthetic Worker Status");
        StringAssert.Contains(runbook, "ReadyForSyntheticOnly");
    }

    private NodalOsLocalOcrWorkerRequestEnvelope Request(
        NodalOsImageCropRedactionResult? redaction,
        bool fullScreen = false,
        bool external = false,
        NodalOsOcrVisionSensitivity sensitivity = NodalOsOcrVisionSensitivity.Low) =>
        skeletonService.CreateRequestEnvelope(
            $"worker-request-{Guid.NewGuid():N}",
            new NodalOsOcrVisionProviderId("local-paddleocr-stub"),
            NodalOsOcrEngineHint.PaddleOcr,
            new NodalOsGroundingSnapshotId("snapshot-worker"),
            "crop:redacted",
            redaction,
            fullScreen,
            sensitivity,
            external);

    private NodalOsImageCropRedactionResult Redact(string marker) =>
        redactor.Redact(new NodalOsImageCropRedactionRequest(
            $"redaction-{Guid.NewGuid():N}",
            new NodalOsGroundingSnapshotId("snapshot-worker"),
            "crop:redacted",
            System.Text.Encoding.UTF8.GetBytes(marker),
            new NodalOsOcrBoundingBox(1, 2, 320, 120),
            "m182-test-fixture",
            NodalOsOcrVisionSensitivity.Low,
            NodalOsOcrPurpose.EvidenceDebug,
            AllowPersistence: false,
            AllowFullScreen: false,
            redactor.DefaultPolicy()));

    private static NodalOsOcrSyntheticActivationReadiness Readiness(
        bool syntheticOptIn = true,
        bool worker = true,
        bool redactorPassed = true,
        bool noAuthority = true) =>
        new(
            WorkerSkeletonAvailable: worker,
            ProviderLocalStub: true,
            EvaluationHarnessPassed: true,
            RedactorPassed: redactorPassed,
            NoRawPersistence: true,
            NoExternalProcess: true,
            NoRealOcr: true,
            NoSaas: true,
            SyntheticOptIn: syntheticOptIn,
            RollbackPauseModeled: true,
            AuditRecordGenerated: true,
            NoAuthorityConfirmed: noAuthority);

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
