using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NodalOsRcRealVerificationM154M156Tests
{
    [TestMethod]
    public void RuntimeStateProbeRealVerificationDetectsDangerousServiceEnabled()
    {
        var services = NodalOsRuntimeStateVerificationService.CurrentLocalPreviewServiceEvidence()
            .Where(service => service.ServiceName != "PublicSaas")
            .Append(NodalOsRuntimeStateVerificationService.EnabledDangerous("PublicSaas"))
            .ToArray();

        var result = new NodalOsRuntimeStateVerificationService().Verify(services);
        var gate = new NodalOsLocalPrivatePreviewReleaseGate().Evaluate(result.Snapshot);

        Assert.IsTrue(result.DangerousServiceEnabled);
        Assert.AreEqual(NodalOsLocalPrivatePreviewReleaseGateStatus.BlockedBySecurity, gate.Status);
    }

    [TestMethod]
    public void RuntimeStateProbeRealVerificationDetectsExternalGeneralEnabled()
    {
        var services = NodalOsRuntimeStateVerificationService.CurrentLocalPreviewServiceEvidence()
            .Where(service => service.ServiceName != "ExternalGeneralCdp")
            .Append(NodalOsRuntimeStateVerificationService.EnabledDangerous("ExternalGeneralCdp"))
            .ToArray();

        var result = new NodalOsRuntimeStateVerificationService().Verify(services);
        var gate = new NodalOsLocalPrivatePreviewReleaseGate().Evaluate(result.Snapshot);

        Assert.IsTrue(result.Snapshot.ExternalGeneralReady);
        Assert.AreEqual(NodalOsLocalPrivatePreviewReleaseGateStatus.BlockedByExternalGeneral, gate.Status);
    }

    [TestMethod]
    public void RuntimeStateProbeReportsMissingServiceAsDisabledByAbsenceNotReady()
    {
        var service = NodalOsRuntimeStateVerificationService.DisabledAbsence("RealBilling");

        Assert.AreEqual(NodalOsRuntimeServiceState.DisabledByAbsence, service.State);
        Assert.IsTrue(service.EvidenceRef.Contains("disabled-by-absence", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RuntimeStateProbeCurrentStateIsVerifiedAndDangerousServicesDisabledByDesign()
    {
        var result = new NodalOsRuntimeStateVerificationService().Verify(NodalOsRuntimeStateVerificationService.CurrentLocalPreviewServiceEvidence());

        Assert.IsTrue(result.VerifiedFromRuntimeState);
        Assert.IsFalse(result.DangerousServiceEnabled);
        Assert.IsTrue(result.Services.Any(service => service.ServiceName == "RealCredentials" && service.State == NodalOsRuntimeServiceState.DisabledByDesign));
    }

    [TestMethod]
    public void ReleaseGateRealVerificationUsesRuntimeProbeAndBlocksDangerousServices()
    {
        var services = NodalOsRuntimeStateVerificationService.CurrentLocalPreviewServiceEvidence()
            .Where(service => service.ServiceName != "RealCredentials")
            .Append(NodalOsRuntimeStateVerificationService.EnabledDangerous("RealCredentials"))
            .ToArray();

        var runtime = new NodalOsRuntimeStateVerificationService().Verify(services);
        var gate = new NodalOsLocalPrivatePreviewReleaseGate().Evaluate(runtime.Snapshot);

        Assert.IsTrue(runtime.DangerousServiceEnabled);
        Assert.AreEqual(NodalOsLocalPrivatePreviewReleaseGateStatus.BlockedBySecurity, gate.Status);
    }

    [TestMethod]
    public void SnapshotHardcodedByItselfDoesNotProduceVerifiedRefreeze()
    {
        var freeze = new NodalOsLocalPreviewReleaseCandidateFreezeService()
            .Freeze(NodalOsLocalPreviewReleaseCandidateFreezeService.DefaultCandidate("test"));

        Assert.AreEqual(NodalOsReleaseCandidateDecision.FrozenReadyForExternalAudit, freeze.Decision);

        var runtime = new NodalOsRuntimeStateVerificationResult(
            new NodalOsReleaseGateStateSnapshot(true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false),
            [],
            ["self-reported snapshot rejected"],
            VerifiedFromRuntimeState: false,
            DangerousServiceEnabled: false,
            Redacted: true);

        var result = new NodalOsVerifiedReleaseCandidateFreezeService().ReFreeze(
            runtime,
            ValidLedgerVerification(),
            ValidSkippedAudit(),
            "test");

        Assert.AreEqual(NodalOsVerifiedReleaseCandidateFreezeState.BlockedByRuntimeState, result.State);
    }

    [TestMethod]
    public void LedgerLiveVerificationMissingLedgerRefBlocksEvidence()
    {
        var result = new NodalOsLedgerEventVerifier().VerifyM51M65([]);

        Assert.IsFalse(result.Verified);
        Assert.AreEqual(NodalOsLedgerLiveVerificationStatus.MissingLedgerRef, result.M51.Status);
    }

    [TestMethod]
    public void LedgerLiveVerificationHashMismatchBlocksEvidence()
    {
        var expected = NodalOsLedgerEventVerifier.DefaultM65ExpectedProof();
        var wrong = expected with { LedgerHash = "wrong-hash" };
        var events = NodalOsLedgerEventVerifier.CurrentVerifiedLedgerEvents();

        var result = new NodalOsLedgerEventVerifier().Verify(wrong, events);

        Assert.AreEqual(NodalOsLedgerLiveVerificationStatus.LedgerHashMismatch, result.Status);
    }

    [TestMethod]
    public void LedgerLiveVerificationWrongProbeKindBlocksEvidence()
    {
        var expected = NodalOsLedgerEventVerifier.DefaultM65ExpectedProof() with { ProbeKind = NexaExternalProofProbeKind.RealHttpClient };

        var result = new NodalOsLedgerEventVerifier().Verify(expected, NodalOsLedgerEventVerifier.CurrentVerifiedLedgerEvents());

        Assert.AreEqual(NodalOsLedgerLiveVerificationStatus.ProbeKindMismatch, result.Status);
    }

    [TestMethod]
    public void LedgerLiveVerificationWrongToolingBlocksEvidence()
    {
        var expected = NodalOsLedgerEventVerifier.DefaultM65ExpectedProof() with { Tooling = "HttpReadOnlyExternal" };

        var result = new NodalOsLedgerEventVerifier().Verify(expected, NodalOsLedgerEventVerifier.CurrentVerifiedLedgerEvents());

        Assert.AreEqual(NodalOsLedgerLiveVerificationStatus.ToolingMismatch, result.Status);
    }

    [TestMethod]
    public void LedgerLiveVerificationSyntheticTokenBlocksEvidence()
    {
        var expected = NodalOsLedgerEventVerifier.DefaultM65ExpectedProof();
        var leaked = NodalOsLedgerEventVerifier.CreateLedgerEvent(expected, new Dictionary<string, string>
        {
            ["token"] = "synthetic-bearer-token"
        });

        var result = new NodalOsLedgerEventVerifier().Verify(expected, [leaked]);

        Assert.AreEqual(NodalOsLedgerLiveVerificationStatus.UnsafeLedgerContent, result.Status);
    }

    [TestMethod]
    public void LedgerLiveVerificationValidM51AndM65EventsAreVerified()
    {
        var result = ValidLedgerVerification();

        Assert.IsTrue(result.Verified);
        Assert.AreEqual(NodalOsLedgerLiveVerificationStatus.Verified, result.M51.Status);
        Assert.AreEqual(NodalOsLedgerLiveVerificationStatus.Verified, result.M65.Status);
    }

    [TestMethod]
    public void FakeChromeCdpFlagWithoutLedgerEventIsNotVerified()
    {
        var result = new NodalOsLedgerEventVerifier().VerifyM51M65([]);

        Assert.IsFalse(result.Verified);
    }

    [TestMethod]
    public void SkippedCategoryRuntimeAuditExpectedCategoriesPass()
    {
        var result = ValidSkippedAudit();

        Assert.AreEqual(NodalOsSkippedCategoryRuntimeAuditStatus.Passed, result.Status);
        Assert.IsTrue(result.Passed);
    }

    [TestMethod]
    public void SkippedCategoryRuntimeAuditWrongCategoriesWithCorrectCountBlocks()
    {
        var report = new NexaSkippedTestsAuditReporter().CreateReport();
        var items = report.Items.ToArray();
        items[0] = items[0] with { Category = NexaSkippedTestCategory.Other };

        var result = new NodalOsSkippedCategoryRuntimeAuditor().Audit(report with { Items = items });

        Assert.AreEqual(NodalOsSkippedCategoryRuntimeAuditStatus.CategoryMismatch, result.Status);
        Assert.IsFalse(result.Passed);
    }

    [TestMethod]
    public void SkippedCategoryRuntimeAuditLocalPreviewSkipBlocks()
    {
        var report = new NexaSkippedTestsAuditReporter().CreateReport();
        var items = report.Items.ToArray();
        items[0] = items[0] with { BlocksLocalPrivatePreview = true };

        var result = new NodalOsSkippedCategoryRuntimeAuditor().Audit(report with { Items = items });

        Assert.AreEqual(NodalOsSkippedCategoryRuntimeAuditStatus.LocalPreviewSkipDetected, result.Status);
    }

    [TestMethod]
    public void RcRefreezeWithRealVerificationIsVerifiedForInternalLocalUse()
    {
        var result = VerifiedRefreeze();

        Assert.AreEqual(NodalOsVerifiedReleaseCandidateFreezeState.FrozenReadyForInternalLocalUseVerified, result.State);
        Assert.IsTrue(result.VerifiedForInternalLocalUse);
        Assert.IsTrue(result.LedgerVerification.Verified);
        Assert.IsTrue(result.SkippedAudit.Passed);
    }

    [TestMethod]
    public void RcRefreezeWithMissingLedgerIsBlockedByMissingEvidence()
    {
        var result = new NodalOsVerifiedReleaseCandidateFreezeService().ReFreeze(
            ValidRuntime(),
            new NodalOsLedgerEventVerifier().VerifyM51M65([]),
            ValidSkippedAudit(),
            "test");

        Assert.AreEqual(NodalOsVerifiedReleaseCandidateFreezeState.BlockedByMissingEvidence, result.State);
    }

    [TestMethod]
    public void RcRefreezeWithWrongSkippedCategoriesIsBlockedByTests()
    {
        var report = new NexaSkippedTestsAuditReporter().CreateReport();
        var items = report.Items.ToArray();
        items[0] = items[0] with { Category = NexaSkippedTestCategory.Other };
        var skipped = new NodalOsSkippedCategoryRuntimeAuditor().Audit(report with { Items = items });

        var result = new NodalOsVerifiedReleaseCandidateFreezeService().ReFreeze(
            ValidRuntime(),
            ValidLedgerVerification(),
            skipped,
            "test");

        Assert.AreEqual(NodalOsVerifiedReleaseCandidateFreezeState.BlockedByTests, result.State);
    }

    [TestMethod]
    public void RealVerificationAdrExistsAndFinalAuditSummaryDistinguishesVerified()
    {
        var adr = ReadDoc("docs", "adr", "local-private-preview-rc-real-verification-m154-m156.md");
        var summary = ReadDoc("artifacts", "local-private-preview-final-audit-m151-m153", "final-audit-summary.json");

        StringAssert.Contains(adr, "auto-certified");
        StringAssert.Contains(adr, "runtime state probe real");
        StringAssert.Contains(adr, "ledger live verification");
        StringAssert.Contains(summary, "FrozenReadyForInternalLocalUseVerified");
        StringAssert.Contains(summary, "verifiedVsSelfReported");
    }

    private static NodalOsRuntimeStateVerificationResult ValidRuntime() =>
        new NodalOsRuntimeStateVerificationService().Verify(NodalOsRuntimeStateVerificationService.CurrentLocalPreviewServiceEvidence());

    private static NodalOsM51M65LedgerVerificationResult ValidLedgerVerification() =>
        new NodalOsLedgerEventVerifier().VerifyM51M65(NodalOsLedgerEventVerifier.CurrentVerifiedLedgerEvents());

    private static NodalOsSkippedCategoryRuntimeAuditResult ValidSkippedAudit() =>
        new NodalOsSkippedCategoryRuntimeAuditor().Audit(new NexaSkippedTestsAuditReporter().CreateReport());

    private static NodalOsVerifiedReleaseCandidateFreezeResult VerifiedRefreeze() =>
        new NodalOsVerifiedReleaseCandidateFreezeService().ReFreeze(
            ValidRuntime(),
            ValidLedgerVerification(),
            ValidSkippedAudit(),
            "dbe87eefdad244c652a972f8c7076cef9a6a8c02");

    private static string ReadDoc(params string[] relativePath)
    {
        var root = FindRepoRoot();
        var path = Path.Combine(new[] { root }.Concat(relativePath).ToArray());
        Assert.IsTrue(File.Exists(path), path);
        return File.ReadAllText(path);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "OneBrain.slnx")))
                return current.FullName;
            current = current.Parent;
        }

        Assert.Fail("Could not locate repository root.");
        return "";
    }
}
