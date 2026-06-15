using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaClientCredentialReadinessM58Tests
{
    [TestMethod]
    public void NexaClientCredentialReadinessBlocksRealCredentialsWhenM51Deferred()
    {
        var report = Report();

        Assert.IsTrue(report.BlocksRealCredentials);
        Assert.IsTrue(report.RiskRegister.Blockers.Any(blocker => blocker.BlockerId == "m51-deferred"));
    }

    [TestMethod]
    public void NexaClientCredentialReadinessRequiresAuditKeyCustody() =>
        AssertFailedCheck(NexaClientCredentialReadinessEvaluator.SafeSyntheticInput() with { AuditKeyCustodyOk = false }, "audit-key-custody");

    [TestMethod]
    public void NexaClientCredentialReadinessRequiresVaultThreatTests() =>
        AssertFailedCheck(NexaClientCredentialReadinessEvaluator.SafeSyntheticInput() with { VaultThreatTestsPassed = false }, "vault-threat-tests");

    [TestMethod]
    public void NexaClientCredentialReadinessRequiresLeakHardening() =>
        AssertFailedCheck(NexaClientCredentialReadinessEvaluator.SafeSyntheticInput() with { LeakHardeningOk = false }, "leak-hardening");

    [TestMethod]
    public void NexaClientCredentialReadinessRequiresTenantGovernance() =>
        AssertFailedCheck(NexaClientCredentialReadinessEvaluator.SafeSyntheticInput() with { TenantGovernanceOk = false }, "tenant-governance");

    [TestMethod]
    public void NexaClientCredentialReadinessRequiresCoreOnlyBoundary() =>
        AssertFailedCheck(NexaClientCredentialReadinessEvaluator.SafeSyntheticInput() with { CoreOnlyBoundaryOk = false }, "core-only-boundary");

    [TestMethod]
    public void NexaClientCredentialReadinessBlocksSupportSecretAccess() =>
        AssertFailedCheck(NexaClientCredentialReadinessEvaluator.SafeSyntheticInput() with { CompanionNonAuthoritative = false }, "companion-no-authority");

    [TestMethod]
    public void NexaClientCredentialReadinessRecommendsExternalAuditBeforeRealCredentials()
    {
        var report = Report();

        Assert.AreEqual(NexaClientCredentialReadinessStatus.BlockedForRealClientCredentials, report.Status);
        Assert.IsTrue(report.RiskRegister.Blockers.Any(blocker => blocker.BlockerId == "external-security-audit"));
        Assert.IsFalse(report.Recommendation.RealClientCredentialsAllowed);
    }

    private static NexaClientCredentialReadinessReport Report() =>
        new NexaClientCredentialReadinessEvaluator().Evaluate(NexaClientCredentialReadinessEvaluator.SafeSyntheticInput());

    private static void AssertFailedCheck(NexaClientCredentialReadinessInput input, string checkId)
    {
        var report = new NexaClientCredentialReadinessEvaluator().Evaluate(input);

        Assert.IsFalse(report.Checks.Single(check => check.CheckId == checkId).Passed);
        Assert.IsTrue(report.BlocksRealCredentials);
    }
}
