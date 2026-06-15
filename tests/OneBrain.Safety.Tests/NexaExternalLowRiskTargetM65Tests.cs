using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaExternalLowRiskTargetM65Tests
{
    [TestMethod]
    public void NexaExternalLowRiskTargetSetupRequiresBaseUrl()
    {
        var setup = Evaluate(null);

        Assert.AreEqual(NexaExternalLowRiskTargetDecisionKind.BlockedNoTestOwnedExternalTarget, setup.Decision.Decision);
    }

    [TestMethod]
    public void NexaExternalLowRiskTargetSetupRejectsUnknownHost()
    {
        var setup = Evaluate(Config() with { HostAllowlisted = false });

        Assert.AreEqual(NexaExternalLowRiskTargetDecisionKind.Blocked, setup.Decision.Decision);
        CollectionAssert.Contains(setup.Decision.ReasonCodes.ToList(), "host not allowlisted");
    }

    [TestMethod]
    public void NexaExternalLowRiskTargetSetupRejectsSensitiveCategory()
    {
        var setup = Evaluate(Config() with { Host = "bank.example.test", SensitiveCategory = true });

        Assert.AreEqual(NexaExternalLowRiskTargetDecisionKind.Blocked, setup.Decision.Decision);
        CollectionAssert.Contains(setup.Decision.ReasonCodes.ToList(), "sensitive host/category blocked");
    }

    [TestMethod]
    public void NexaExternalLowRiskTargetSetupRequiresTestOwnedProof()
    {
        var setup = new NexaExternalLowRiskTargetSetupEvaluator().Evaluate(Config() with { TestOwned = false }, Proof() with { TestOwned = false });

        Assert.AreEqual(NexaExternalLowRiskTargetDecisionKind.Blocked, setup.Decision.Decision);
        CollectionAssert.Contains(setup.Decision.ReasonCodes.ToList(), "test-owned proof required");
    }

    [TestMethod]
    public void NexaExternalLowRiskTargetSetupBlocksPaymentOrIrreversibleActions()
    {
        var setup = Evaluate(Config() with { HasPayment = true, HasIrreversibleActions = true });

        Assert.AreEqual(NexaExternalLowRiskTargetDecisionKind.Blocked, setup.Decision.Decision);
        CollectionAssert.Contains(setup.Decision.ReasonCodes.ToList(), "payment or irreversible actions blocked");
    }

    [TestMethod]
    public void NexaExternalLowRiskTargetSetupReportsBlockedWhenMissing()
    {
        var setup = new NexaExternalLowRiskTargetSetupEvaluator().Evaluate(NexaExternalLowRiskTargetSetupEvaluator.FromEnvironment(), null);

        if (Environment.GetEnvironmentVariable("ONEBRAIN_EXTERNAL_LOW_RISK_TARGET_BASE_URL") is null)
            Assert.AreEqual(NexaExternalLowRiskTargetDecisionKind.BlockedNoTestOwnedExternalTarget, setup.Decision.Decision);
    }

    [TestMethod]
    public void NexaExternalLowRiskTargetSetupAllowsConfiguredTestOwnedTarget()
    {
        var setup = Evaluate(Config());

        Assert.AreEqual(NexaExternalLowRiskTargetDecisionKind.Allowed, setup.Decision.Decision);
        Assert.IsTrue(setup.Decision.Readiness.SemanticProofVerified);
    }

    [TestMethod]
    public void NexaExternalLowRiskTargetLiveVerifiesSemanticProof()
    {
        var setup = LiveSetupOrSkip();

        Assert.IsTrue(setup.Decision.Readiness.SemanticProofVerified);
    }

    [TestMethod]
    public void NexaExternalLowRiskTargetLiveCapturesMetadataOnly()
    {
        var setup = LiveSetupOrSkip();

        Assert.IsTrue(setup.Decision.Readiness.MetadataOnly);
        Assert.IsFalse(setup.Decision.Readiness.SensitiveHeaderValuesCaptured);
    }

    [TestMethod]
    public void NexaExternalLowRiskTargetLiveCleansBrowser()
    {
        var setup = LiveSetupOrSkip();

        Assert.IsTrue(setup.Decision.Readiness.BrowserCleanupConfirmed);
    }

    private static NexaExternalLowRiskTargetSetup LiveSetupOrSkip()
    {
        if (Environment.GetEnvironmentVariable("ONEBRAIN_RUN_EXTERNAL_LOW_RISK_TARGET_TESTS") != "1")
            Assert.Inconclusive("External low-risk target live tests are opt-in.");

        var config = NexaExternalLowRiskTargetSetupEvaluator.FromEnvironment();
        if (config is null)
            Assert.Inconclusive("ONEBRAIN_EXTERNAL_LOW_RISK_TARGET_BASE_URL is not configured.");

        return Evaluate(config!);
    }

    private static NexaExternalLowRiskTargetSetup Evaluate(NexaExternalLowRiskTargetConfig? config) =>
        new NexaExternalLowRiskTargetSetupEvaluator().Evaluate(config, config is null ? null : Proof());

    private static NexaExternalLowRiskTargetConfig Config() =>
        new("target-test-owned", "https://readonly.test-owned.example", "readonly.test-owned.example", HostAllowlisted: true, TestOwned: true, SensitiveCategory: false, ContainsRealCustomerData: false, HasPayment: false, HasIrreversibleActions: false, RequiresTwoFactorOrCaptcha: false, SemanticProofAvailable: true, ReadOnlyPaths: ["/", "/status", "/readonly"]);

    private static NexaExternalLowRiskTargetOwnershipProof Proof() =>
        new("ownership-proof", TestOwned: true, "ownership-proof-ref", Redacted: true);
}
