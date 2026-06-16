using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaHttpsOwnershipVerificationM87Tests
{
    [TestMethod]
    public async Task HttpsOwnershipVerificationNotCheckedBlocksLiveProof()
    {
        var result = await VerifyAsync(optIn: false);

        Assert.AreEqual(NexaHttpsOwnershipVerificationStatus.NotChecked, result.Status);
        Assert.IsFalse(result.EnablesCandidateLiveProof);
        Assert.IsFalse(result.ClosesM51M65);
    }

    [TestMethod]
    public async Task HttpsOwnershipVerificationHttpsPendingBlocks()
    {
        var request = NexaHttpsOwnershipVerifier.DefaultRequest(optInLiveNetwork: true) with { ExpectedBaseUrl = "http://lab.nodalos.com.ar" };
        var result = await new NexaHttpsOwnershipVerifier(HealthyProbe()).VerifyAsync(request);

        Assert.AreNotEqual(NexaHttpsOwnershipVerificationStatus.VerifiedTestOwnedReadOnlyTarget, result.Status);
        Assert.IsFalse(result.EnablesCandidateLiveProof);
    }

    [TestMethod]
    public async Task HttpsOwnershipVerificationHealthMissingBlocks()
    {
        var result = await VerifyAsync(optIn: true, healthStatus: 404);

        Assert.AreEqual(NexaHttpsOwnershipVerificationStatus.VerificationFailed, result.Status);
        Assert.IsFalse(result.EnablesCandidateLiveProof);
    }

    [TestMethod]
    public async Task HttpsOwnershipVerificationOwnershipMissingBlocks()
    {
        var result = await VerifyAsync(optIn: true, ownershipStatus: 404);

        Assert.AreEqual(NexaHttpsOwnershipVerificationStatus.VerificationFailed, result.Status);
        Assert.IsFalse(result.EnablesCandidateLiveProof);
    }

    [TestMethod]
    public async Task HttpsOwnershipVerificationMetadataMismatchBlocks()
    {
        var result = await new NexaHttpsOwnershipVerifier(new FakeProbe(200, 200, "NEXA without expected restrictions")).VerifyAsync(NexaHttpsOwnershipVerifier.DefaultRequest(optInLiveNetwork: true));

        Assert.AreEqual(NexaHttpsOwnershipVerificationStatus.MetadataMismatch, result.Status);
        Assert.IsFalse(result.EnablesCandidateLiveProof);
    }

    [TestMethod]
    public async Task HttpsOwnershipVerificationVerifiedTargetEnablesCandidateOnly()
    {
        var result = await VerifyAsync(optIn: true);

        Assert.AreEqual(NexaHttpsOwnershipVerificationStatus.VerifiedTestOwnedReadOnlyTarget, result.Status);
        Assert.IsTrue(result.EnablesCandidateLiveProof);
        Assert.IsFalse(result.ClosesM51M65);
        CollectionAssert.Contains(result.EvidenceRefs.ToList(), "provider:Vercel");
    }

    [TestMethod]
    public async Task HttpsOwnershipVerificationEvidenceIsRedacted()
    {
        var result = await VerifyAsync(optIn: true);
        var serialized = System.Text.Json.JsonSerializer.Serialize(result);

        Assert.IsTrue(result.Redacted);
        Assert.IsFalse(serialized.Contains("opaque-token-value-123456789", StringComparison.Ordinal));
    }

    private static Task<NexaHttpsOwnershipVerificationResult> VerifyAsync(bool optIn, int healthStatus = 200, int ownershipStatus = 200) =>
        new NexaHttpsOwnershipVerifier(HealthyProbe(healthStatus, ownershipStatus)).VerifyAsync(NexaHttpsOwnershipVerifier.DefaultRequest(optIn));

    private static FakeProbe HealthyProbe(int healthStatus = 200, int ownershipStatus = 200) =>
        new(healthStatus, ownershipStatus, "NEXA test-owned read-only no-real-users no-real-credentials no-real-payments no-submit");

    internal sealed class FakeProbe(int healthStatus, int ownershipStatus, string text) : INexaReadOnlyHttpProbe
    {
        public Task<NexaReadOnlyHttpProbeResult> GetAsync(Uri uri, CancellationToken cancellationToken)
        {
            var status = uri.AbsolutePath.Contains("health", StringComparison.OrdinalIgnoreCase) ? healthStatus : ownershipStatus;
            return Task.FromResult(new NexaReadOnlyHttpProbeResult(status, text, ["Content-Type"], false, false, false));
        }
    }
}
