using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaDomainBindingReadinessM84Tests
{
    [TestMethod]
    public void DomainBindingReadinessDefaultUsesNexaLabDomain()
    {
        var config = new NexaTargetBindingReadinessEvaluator().CreateDefault();

        Assert.IsTrue(string.Equals(config.ExpectedDomain, "nexa-lab.nodalos.com.ar", StringComparison.Ordinal));
        Assert.IsTrue(string.Equals(config.ExpectedBaseUrl, "https://nexa-lab.nodalos.com.ar", StringComparison.Ordinal));
    }

    [TestMethod]
    public void DomainBindingReadinessAllowedHostsOnlyIncludeExpectedSubdomain()
    {
        var config = ReadyConfig();

        Assert.AreEqual(1, config.AllowedHosts.Count);
        CollectionAssert.Contains(config.AllowedHosts.ToList(), "nexa-lab.nodalos.com.ar");
        CollectionAssert.DoesNotContain(config.AllowedHosts.ToList(), "nodalos.com.ar");
    }

    [TestMethod]
    public void DomainBindingReadinessRequiresHealthAndOwnershipPaths()
    {
        var config = ReadyConfig();

        CollectionAssert.Contains(config.AllowedPaths.ToList(), "/health");
        CollectionAssert.Contains(config.AllowedPaths.ToList(), "/ownership");
    }

    [TestMethod]
    public void DomainBindingReadinessUnknownDnsDoesNotAllowLiveProof()
    {
        var decision = new NexaTargetBindingReadinessEvaluator().Evaluate(new NexaTargetBindingReadinessEvaluator().CreateDefault());

        Assert.IsFalse(decision.CandidateLiveProofAllowed);
        Assert.IsFalse(decision.ExecutesNetwork);
    }

    [TestMethod]
    public void DomainBindingReadinessHttpsAndOwnershipVerifiedAllowsCandidateButDoesNotExecute()
    {
        var decision = new NexaTargetBindingReadinessEvaluator().Evaluate(ReadyConfig());

        Assert.IsTrue(decision.CandidateLiveProofAllowed);
        Assert.IsFalse(decision.ExecutesNetwork);
    }

    [TestMethod]
    public void DomainBindingReadinessDoesNotAssumeRootDomain()
    {
        var config = ReadyConfig() with
        {
            AllowedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "nexa-lab.nodalos.com.ar", "nodalos.com.ar" }
        };
        var decision = new NexaTargetBindingReadinessEvaluator().Evaluate(config);

        Assert.IsFalse(decision.CandidateLiveProofAllowed);
        Assert.IsTrue(decision.ReasonCodes.Any(reason => reason.Contains("subdomain", StringComparison.OrdinalIgnoreCase)));
    }

    internal static NexaTargetBindingConfig ReadyConfig() =>
        new NexaTargetBindingReadinessEvaluator().CreateDefault(
            NexaTargetBindingDnsMode.CnameOnly,
            NexaTargetBindingVerificationStatus.OwnershipVerified);
}
