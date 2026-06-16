using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaFirstReadOnlyLiveProofM88Tests
{
    [TestMethod]
    public async Task FirstReadOnlyLiveProofNoOptInIsSkippedWithoutFailingSuite()
    {
        var result = await Runner().RunAsync(optIn: false, executeNetwork: false);

        Assert.AreEqual(NexaFirstReadOnlyLiveProofStatus.SkippedNoOptIn, result.Status);
        Assert.IsFalse(result.ExecutedNetwork);
        Assert.AreEqual(NexaExternalReadOnlyEvidencePackStatus.SkippedNoOptIn, result.EvidencePack.Status);
    }

    [TestMethod]
    public async Task FirstReadOnlyLiveProofVerifiedTargetOptInAllowsCandidateRunner()
    {
        var result = await Runner().RunAsync(optIn: true, executeNetwork: false);

        Assert.AreEqual(NexaFirstReadOnlyLiveProofStatus.CandidateRunnerAllowed, result.Status, $"{result.Verification.Status}: {string.Join("; ", result.Verification.ReasonCodes)}");
        Assert.IsFalse(result.ExecutedNetwork);
        Assert.IsFalse(result.EvidencePack.CandidateForM51M65Closure);
    }

    [TestMethod]
    public async Task FirstReadOnlyLiveProofAllowedRoutesAreGetReadOnly()
    {
        var result = await Runner().RunAsync(optIn: true, executeNetwork: false);

        CollectionAssert.AreEquivalent(new[] { "/", "/health/", "/ownership/", "/products/", "/document/", "/report/" }, result.RoutesTested.ToArray());
        Assert.IsTrue(result.EvidencePack.DeniedActions.Contains("POST"));
        Assert.IsTrue(result.EvidencePack.DeniedActions.Contains("DELETE"));
    }

    [TestMethod]
    public async Task FirstReadOnlyLiveProofBlockedRoutesGeneratePolicyExplanations()
    {
        var result = await Runner().RunAsync(optIn: true, executeNetwork: false);

        CollectionAssert.AreEquivalent(new[] { "/disabled-form/", "/blocked-login/", "/blocked-checkout/", "/blocked-destructive-action/" }, result.DeniedRoutesTested.ToArray());
        Assert.IsTrue(result.BlockerExplanations.Count >= 3);
        Assert.IsTrue(result.BlockerExplanations.All(explanation => explanation.Redacted));
    }

    [TestMethod]
    public async Task FirstReadOnlyLiveProofEvidencePackContainsProviderScopeProjectDomainRoutes()
    {
        var result = await Runner().RunAsync(optIn: true, executeNetwork: true);

        Assert.AreEqual(NexaFirstReadOnlyLiveProofStatus.PassedReadOnlyProof, result.Status);
        Assert.IsTrue(result.EvidencePack.LogRefs.Any(log => log.Contains("provider:Vercel", StringComparison.Ordinal)));
        Assert.IsTrue(result.EvidencePack.LogRefs.Any(log => log.Contains("scope:Shift Evidence", StringComparison.Ordinal)));
        Assert.IsTrue(result.EvidencePack.LogRefs.Any(log => log.Contains("project:nexa-test-owned-target", StringComparison.Ordinal)));
        Assert.IsTrue(result.EvidencePack.LogRefs.Any(log => log.Contains("domain:nexalab.nodalos.com.ar", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task FirstReadOnlyLiveProofPassedEvidenceDoesNotExposeSecretsCookiesTokens()
    {
        var result = await Runner().RunAsync(optIn: true, executeNetwork: true);
        var serialized = System.Text.Json.JsonSerializer.Serialize(result);

        Assert.AreEqual(NexaExternalReadOnlyEvidencePackStatus.PassedReadOnlyProof, result.EvidencePack.Status);
        Assert.IsFalse(serialized.Contains("cookie", StringComparison.OrdinalIgnoreCase) && !serialized.Contains("no cookies", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serialized.Contains("opaque-token-value-123456789", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task FirstReadOnlyLiveProofFailedVerificationBlocksProof()
    {
        var result = await new NexaFirstReadOnlyLiveProofRunner(new NexaHttpsOwnershipVerificationM87Tests.FakeProbe(404, 200, "NEXA")).RunAsync(optIn: true, executeNetwork: true);

        Assert.AreEqual(NexaFirstReadOnlyLiveProofStatus.BlockedVerificationFailed, result.Status);
        Assert.AreNotEqual(NexaExternalReadOnlyEvidencePackStatus.PassedReadOnlyProof, result.EvidencePack.Status);
    }

    [TestMethod]
    public async Task FirstReadOnlyLiveProofOptInCanExecuteAgainstRealTargetWhenEnabled()
    {
        var optIn = string.Equals(Environment.GetEnvironmentVariable("NEXA_EXTERNAL_LIVE_PROOF_OPT_IN"), "true", StringComparison.OrdinalIgnoreCase);
        var result = await new NexaFirstReadOnlyLiveProofRunner().RunAsync(optIn, executeNetwork: optIn);

        if (!optIn)
        {
            Assert.AreEqual(NexaFirstReadOnlyLiveProofStatus.SkippedNoOptIn, result.Status);
            Assert.IsFalse(result.ExecutedNetwork);
            return;
        }

        Assert.AreEqual(NexaFirstReadOnlyLiveProofStatus.PassedReadOnlyProof, result.Status);
        Assert.IsTrue(result.ExecutedNetwork);
        Assert.AreEqual(NexaExternalReadOnlyEvidencePackStatus.PassedReadOnlyProof, result.EvidencePack.Status);
    }

    private static NexaFirstReadOnlyLiveProofRunner Runner() =>
        new(new NexaHttpsOwnershipVerificationM87Tests.FakeProbe(200, 200, "NEXA test-owned read-only no-real-users no-real-credentials no-real-payments no-submit"));
}
