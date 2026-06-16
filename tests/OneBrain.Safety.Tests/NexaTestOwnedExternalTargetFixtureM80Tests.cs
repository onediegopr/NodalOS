using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaTestOwnedExternalTargetFixtureM80Tests
{
    [TestMethod]
    public void TestOwnedExternalTargetFixtureApprovesAsReadOnlyTestOwnedFixture()
    {
        var decision = Evaluate(NexaTestOwnedExternalTargetFixtureFactory.Create());

        Assert.AreEqual(NexaExternalTestOwnedTargetStatus.ApprovedReadOnlyTestOwned, decision.Status);
        Assert.IsTrue(decision.AllowsReadOnlyProof);
        Assert.IsTrue(decision.Target!.ComplianceNotes.Contains("fixture-approved", StringComparison.Ordinal));
    }

    [TestMethod]
    public void TestOwnedExternalTargetFixtureWithPostIsBlocked()
    {
        var target = NexaTestOwnedExternalTargetFixtureFactory.Create() with
        {
            AllowedMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GET", "POST" }
        };

        Assert.AreEqual(NexaExternalTestOwnedTargetStatus.PolicyRejected, Evaluate(target).Status);
    }

    [TestMethod]
    public void TestOwnedExternalTargetFixtureWithLoginIsBlocked()
    {
        var target = NexaTestOwnedExternalTargetFixtureFactory.Create() with
        {
            CredentialPolicy = NexaExternalTargetCredentialPolicy.SyntheticOnly
        };

        Assert.AreEqual(NexaExternalTestOwnedTargetStatus.PolicyRejected, Evaluate(target).Status);
    }

    [TestMethod]
    public void TestOwnedExternalTargetFixtureWithRealPiiSimulatedIsBlocked()
    {
        var target = NexaTestOwnedExternalTargetFixtureFactory.Create() with
        {
            DataSensitivityProfile = NexaExternalTargetDataSensitivityProfile.PersonalDataBlocked
        };

        Assert.AreEqual(NexaExternalTestOwnedTargetStatus.PolicyRejected, Evaluate(target).Status);
    }

    [TestMethod]
    public void TestOwnedExternalTargetFixtureWithSensitiveDomainIsBlocked()
    {
        var target = NexaTestOwnedExternalTargetFixtureFactory.Create() with
        {
            BaseUrl = "https://afip.gob.ar/landing",
            AllowedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "afip.gob.ar" }
        };

        Assert.AreEqual(NexaExternalTestOwnedTargetStatus.BlockedSensitiveSurface, Evaluate(target).Status);
    }

    [TestMethod]
    public void TestOwnedExternalTargetFixtureApprovalDoesNotCloseM51M65()
    {
        var scenario = new NexaSyntheticExternalScenarioCatalogService().CreateDefault().Scenarios.First(s => s.Kind == NexaSyntheticExternalScenarioKind.LandingReadOnly);
        var dryRun = new NexaProofDryRunBinding().Run(NexaTestOwnedExternalTargetFixtureFactory.Create(), scenario);

        Assert.AreEqual(NexaProofDryRunStatus.DryRunEvidenceGenerated, dryRun.Status);
        Assert.IsFalse(dryRun.ClosesM51M65);
        Assert.IsFalse(dryRun.EvidencePack.CandidateForM51M65Closure);
    }

    private static NexaExternalTestOwnedTargetDecision Evaluate(NexaExternalTestOwnedTarget target) =>
        new NexaExternalTestOwnedTargetEvaluator().Evaluate(target, DateTimeOffset.UtcNow);
}
