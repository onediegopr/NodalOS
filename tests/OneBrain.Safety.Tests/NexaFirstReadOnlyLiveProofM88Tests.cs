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
        Assert.IsTrue(result.EvidencePack.LogRefs.Any(log => log.Contains("project:lab", StringComparison.Ordinal)));
        Assert.IsTrue(result.EvidencePack.LogRefs.Any(log => log.Contains("domain:lab.nodalos.com.ar", StringComparison.Ordinal)));
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
        var result = await new NexaFirstReadOnlyLiveProofRunner(new NexaHttpsOwnershipVerificationM87Tests.FakeProbe(404, 200, "NODAL OS")).RunAsync(optIn: true, executeNetwork: true);

        Assert.AreEqual(NexaFirstReadOnlyLiveProofStatus.BlockedVerificationFailed, result.Status);
        Assert.AreNotEqual(NexaExternalReadOnlyEvidencePackStatus.PassedReadOnlyProof, result.EvidencePack.Status);
    }

    [TestMethod]
    public async Task FirstReadOnlyLiveProofOptInCanExecuteAgainstRealTargetWhenEnabled()
    {
        var optIn = NodalOsExternalLiveProofOptIn.IsEnabled();
        using var temp = new TempDirectory();
        var ledger = TestLedger(temp.Path);
        var result = await new NexaFirstReadOnlyLiveProofRunner().RunAsync(optIn, executeNetwork: optIn, optIn ? ledger : null);

        if (!optIn)
        {
            Assert.AreEqual(NexaFirstReadOnlyLiveProofStatus.SkippedNoOptIn, result.Status);
            Assert.IsFalse(result.ExecutedNetwork);
            return;
        }

        Assert.AreEqual(NexaFirstReadOnlyLiveProofStatus.PassedReadOnlyProof, result.Status);
        Assert.IsTrue(result.ExecutedNetwork);
        Assert.AreEqual(NexaExternalReadOnlyEvidencePackStatus.PassedReadOnlyProof, result.EvidencePack.Status);
        Assert.AreEqual(NexaExternalEvidencePersistenceStatus.PersistedRedactedLedger, result.EvidencePack.PersistenceStatus);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.EvidencePack.LedgerRef));
    }

    [TestMethod]
    public void FirstReadOnlyLiveProofNodalOsOptInEnvVarIsAccepted()
    {
        var enabled = NodalOsExternalLiveProofOptIn.IsEnabled(name =>
            name == NodalOsExternalLiveProofOptIn.CurrentEnvironmentVariable ? "true" : null);

        Assert.IsTrue(enabled);
    }

    [TestMethod]
    public void FirstReadOnlyLiveProofLegacyNexaOptInEnvVarIsAcceptedTemporarily()
    {
        var enabled = NodalOsExternalLiveProofOptIn.IsEnabled(name =>
            name == NodalOsExternalLiveProofOptIn.LegacyEnvironmentVariable ? "true" : null);

        Assert.IsTrue(enabled);
    }

    [TestMethod]
    public void FirstReadOnlyLiveProofWithoutOptInEnvVarsIsDisabled()
    {
        var enabled = NodalOsExternalLiveProofOptIn.IsEnabled(_ => null);

        Assert.IsFalse(enabled);
    }

    private static NexaFirstReadOnlyLiveProofRunner Runner() =>
        new(new NexaHttpsOwnershipVerificationM87Tests.FakeProbe(200, 200, "NODAL OS test-owned read-only no-real-users no-real-credentials no-real-payments no-submit"));

    private static BrowserPersistentAuditLedger TestLedger(string path) =>
        new(
            new BrowserAuditLedgerPolicy(path, AllowFilePersistence: true, RedactBeforePersist: true, new BrowserAuditLedgerRetentionPolicy(null, null, DeleteOnCleanup: true)),
            BrowserAuditLedgerHmacIntegrityProvider.CreateDevFixtureProvider("nodal-m90-explicit-test-fixture-hmac-key"));

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"nodal-m90-live-{Guid.NewGuid():N}");

        public TempDirectory() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
