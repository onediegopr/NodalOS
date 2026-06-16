using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaExternalReadOnlyProofHonestyM90Tests
{
    [TestMethod]
    public async Task ProbeKindFakeProbeProducesModeledFake()
    {
        var proof = await FakeProof();

        Assert.AreEqual(NexaExternalProofProbeKind.ModeledFake, proof.EvidencePack.ProbeKind);
        Assert.AreEqual(NexaExternalEvidencePersistenceStatus.NotPersistedModeled, proof.EvidencePack.PersistenceStatus);
        Assert.IsNull(proof.EvidencePack.LedgerRef);
    }

    [TestMethod]
    public async Task ProbeKindRealHttpClientProducesRealHttpClient()
    {
        var proof = await RealHttpProof(persist: false);

        Assert.AreEqual(NexaExternalProofProbeKind.RealHttpClient, proof.EvidencePack.ProbeKind);
        Assert.AreEqual("HttpReadOnlyExternal", proof.EvidencePack.Tooling);
        Assert.AreEqual("HttpReadOnlyExternal", proof.EvidencePack.RuntimeProvider);
    }

    [TestMethod]
    public async Task ExternalReadOnlyProofHonestyHttpClientDoesNotDeclareChromeCdpOrDom()
    {
        var proof = await RealHttpProof(persist: false);

        CollectionAssert.DoesNotContain(proof.EvidencePack.RuntimeCapabilities.ToList(), "ChromeCdpExternal");
        CollectionAssert.DoesNotContain(proof.EvidencePack.RuntimeCapabilities.ToList(), "DomReadOnly");
        CollectionAssert.DoesNotContain(proof.EvidencePack.RuntimeCapabilities.ToList(), "NavigationReadOnly");
        CollectionAssert.Contains(proof.EvidencePack.RuntimeCapabilities.ToList(), "HttpGetReadOnly");
    }

    [TestMethod]
    public void ProbeKindRealChromeCdpIsModeledButNotUsedByHttpRunner()
    {
        var request = new NexaExternalProofHarnessRequest(true, NexaFirstReadOnlyLiveProofRunner.CreateLiveTarget(), "lab.nodalos.com.ar", "/", "GET", false, false, false, false, "operator");
        var harness = new NexaExternalProofHarness().Evaluate(request, DateTimeOffset.UtcNow);
        var pack = new NexaExternalReadOnlyEvidencePackBuilder().Build(harness, request, true, true, NexaExternalProofProbeKind.RealChromeCdp);

        Assert.AreEqual(NexaExternalProofProbeKind.RealChromeCdp, pack.ProbeKind);
        Assert.AreEqual("ChromeCdpExternal", pack.Tooling);
        CollectionAssert.Contains(pack.RuntimeCapabilities.ToList(), "DomReadOnly");
    }

    [TestMethod]
    public async Task PersistentEvidenceLedgerFakeProbeDoesNotGetLedgerRef()
    {
        var proof = await FakeProof();

        Assert.IsNull(proof.EvidencePack.LedgerRef);
        Assert.AreEqual(NexaExternalEvidencePersistenceStatus.NotPersistedModeled, proof.EvidencePack.PersistenceStatus);
    }

    [TestMethod]
    public async Task PersistentEvidenceLedgerDryRunDoesNotGetLedgerRef()
    {
        using var temp = new TempDirectory();
        var proof = await RealHttpProof(persist: false, temp.Path);

        Assert.IsNull(proof.EvidencePack.LedgerRef);
        Assert.AreEqual(NexaExternalEvidencePersistenceStatus.NotPersisted, proof.EvidencePack.PersistenceStatus);
    }

    [TestMethod]
    public async Task PersistentEvidenceLedgerRunRealGetsLedgerRef()
    {
        using var temp = new TempDirectory();
        var proof = await RealHttpProof(persist: true, temp.Path);

        Assert.AreEqual(NexaExternalEvidencePersistenceStatus.PersistedRedactedLedger, proof.EvidencePack.PersistenceStatus);
        Assert.IsFalse(string.IsNullOrWhiteSpace(proof.EvidencePack.LedgerRef));
        Assert.IsTrue(proof.EvidencePack.LedgerSequence > 0);
        Assert.IsFalse(string.IsNullOrWhiteSpace(proof.EvidencePack.LedgerHash));
    }

    [TestMethod]
    public async Task PersistentEvidenceLedgerEntryDoesNotContainSecretsCookiesOrBody()
    {
        using var temp = new TempDirectory();
        var proof = await RealHttpProof(persist: true, temp.Path);
        var ledgerText = File.ReadAllText(System.IO.Path.Combine(temp.Path, "browser-audit-ledger.jsonl"));

        Assert.IsFalse(ledgerText.Contains("opaque-token-value-123456789", StringComparison.Ordinal));
        Assert.IsFalse(ledgerText.Contains("<html", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(ledgerText.Contains("set-cookie", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(ledgerText.Contains("body transiently scanned and not persisted", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(string.IsNullOrWhiteSpace(proof.EvidencePack.LedgerRef));
    }

    [TestMethod]
    public async Task PersistentEvidenceLedgerFailurePreventsCandidateClose()
    {
        var proof = await RealHttpProof(persist: false);
        var review = new NexaM51M65ClosureCandidateReviewer().Review(proof);

        Assert.AreEqual(NexaM51M65ClosureCandidateReviewDecision.DoNotClose, review.FinalDecision);
    }

    [TestMethod]
    public async Task M51M65ClosureCandidateReviewExecutedNetworkFakeDoesNotClose()
    {
        var proof = await FakeProof();
        var review = new NexaM51M65ClosureCandidateReviewer().Review(proof);

        Assert.AreEqual(NexaM51M65ClosureCandidateReviewDecision.DoNotClose, review.FinalDecision);
    }

    [TestMethod]
    public async Task M51M65ClosureCandidateReviewRealHttpLedgerCandidateClosesOnlyM51()
    {
        using var temp = new TempDirectory();
        var proof = await RealHttpProof(persist: true, temp.Path);
        var review = new NexaM51M65ClosureCandidateReviewer().Review(proof);

        Assert.AreEqual(NexaM51M65ClosureCandidateReviewDecision.CandidateCloseM51Only, review.FinalDecision);
        StringAssert.Contains(review.M51Recommendation, "candidate close M51");
        StringAssert.Contains(review.M65Recommendation, "M65 deferred");
        Assert.IsTrue(review.PublicSaasStillDisabled);
        Assert.IsTrue(review.RealBillingStillDisabled);
        Assert.IsTrue(review.RealEmailStillDisabled);
        Assert.IsTrue(review.RealCredentialsStillBlocked);
        Assert.IsTrue(review.SensitiveSurfacesStillBlocked);
    }

    [TestMethod]
    public async Task ExternalReadOnlyProofHonestyRedactionSummaryStatesBodyTransientNotPersisted()
    {
        var proof = await RealHttpProof(persist: false);

        StringAssert.Contains(proof.EvidencePack.RedactionSummary, "response body fetched transiently for safety scan");
        StringAssert.Contains(proof.EvidencePack.RedactionSummary, "body not persisted");
    }

    [TestMethod]
    public async Task ExternalReadOnlyProofHonestyEvidencePackDoesNotContainBody()
    {
        var proof = await RealHttpProof(persist: false);
        var serialized = System.Text.Json.JsonSerializer.Serialize(proof.EvidencePack);

        Assert.IsFalse(serialized.Contains("<html", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serialized.Contains("NEXA test-owned read-only no-real-users", StringComparison.Ordinal));
    }

    private static Task<NexaFirstReadOnlyLiveProofResult> FakeProof() =>
        new NexaFirstReadOnlyLiveProofRunner(new NexaHttpsOwnershipVerificationM87Tests.FakeProbe(200, 200, "NEXA test-owned read-only no-real-users no-real-credentials no-real-payments no-submit")).RunAsync(optIn: true, executeNetwork: true);

    private static Task<NexaFirstReadOnlyLiveProofResult> RealHttpProof(bool persist, string? ledgerPath = null)
    {
        BrowserPersistentAuditLedger? ledger = persist && ledgerPath is not null ? TestLedger(ledgerPath) : null;
        return new NexaFirstReadOnlyLiveProofRunner(new NexaHttpClientReadOnlyProbe(new HttpClient(new StaticHttpHandler()))).RunAsync(optIn: true, executeNetwork: true, ledger);
    }

    private static BrowserPersistentAuditLedger TestLedger(string path) =>
        new(
            new BrowserAuditLedgerPolicy(path, AllowFilePersistence: true, RedactBeforePersist: true, new BrowserAuditLedgerRetentionPolicy(null, null, DeleteOnCleanup: true)),
            BrowserAuditLedgerHmacIntegrityProvider.CreateDevFixtureProvider("nodal-m90-explicit-test-fixture-hmac-key"));

    private sealed class StaticHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("NEXA test-owned read-only no-real-users no-real-credentials no-real-payments no-submit <html>body-not-persistable</html>")
            });
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"nodal-m90-{Guid.NewGuid():N}");

        public TempDirectory() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
