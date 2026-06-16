using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaExternalTestOwnedTargetM77Tests
{
    [TestMethod]
    public void ExternalTestOwnedTargetMissingBlocksExternalLive()
    {
        var decision = Evaluate(null);

        Assert.AreEqual(NexaExternalTestOwnedTargetStatus.MissingTarget, decision.Status);
        Assert.IsFalse(decision.AllowsReadOnlyProof);
    }

    [TestMethod]
    public void ExternalTestOwnedTargetThirdPartyIsBlocked()
    {
        var target = ApprovedTarget() with { ExplicitlyTestOwned = false, OwnershipProofMode = NexaExternalTargetOwnershipProofMode.None };
        var decision = Evaluate(target);

        Assert.AreEqual(NexaExternalTestOwnedTargetStatus.OwnershipUnverified, decision.Status);
        Assert.IsFalse(decision.AllowsReadOnlyProof);
    }

    [TestMethod]
    public void ExternalTestOwnedTargetFinancialFiscalGovernmentHostIsBlocked()
    {
        var target = ApprovedTarget("https://bank.example.invalid/status") with
        {
            AllowedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bank.example.invalid" }
        };
        var decision = Evaluate(target);

        Assert.AreEqual(NexaExternalTestOwnedTargetStatus.BlockedSensitiveSurface, decision.Status);
    }

    [TestMethod]
    public void ExternalTestOwnedTargetMutatingMethodsAreBlocked()
    {
        var target = ApprovedTarget() with
        {
            AllowedMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GET", "POST" }
        };
        var decision = Evaluate(target);

        Assert.AreEqual(NexaExternalTestOwnedTargetStatus.PolicyRejected, decision.Status);
        Assert.IsFalse(decision.AllowsReadOnlyProof);
    }

    [TestMethod]
    public void ExternalTestOwnedTargetRealCredentialsAreBlocked()
    {
        var target = ApprovedTarget() with { CredentialPolicy = NexaExternalTargetCredentialPolicy.RealCredentialsBlocked };
        var decision = Evaluate(target);

        Assert.AreEqual(NexaExternalTestOwnedTargetStatus.PolicyRejected, decision.Status);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), "real or synthetic login is not allowed for this proof harness");
    }

    [TestMethod]
    public void ExternalTestOwnedTargetReadOnlyTestOwnedIsApproved()
    {
        var decision = Evaluate(ApprovedTarget());

        Assert.AreEqual(NexaExternalTestOwnedTargetStatus.ApprovedReadOnlyTestOwned, decision.Status);
        Assert.IsTrue(decision.AllowsReadOnlyProof);
    }

    [TestMethod]
    public void ExternalTestOwnedTargetExpiredIsBlocked()
    {
        var target = ApprovedTarget() with { ValidUntilUtc = DateTimeOffset.UtcNow.AddMinutes(-1) };
        var decision = Evaluate(target);

        Assert.AreEqual(NexaExternalTestOwnedTargetStatus.Expired, decision.Status);
    }

    internal static NexaExternalTestOwnedTarget ApprovedTarget(string url = "https://nodal-os-test-owned.example.invalid/status") =>
        new(
            "target-test-owned-readonly",
            url,
            NexaExternalTargetOwnershipProofMode.OperatorAttestation,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { new Uri(url).Host },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "afip.gob.ar", "bank.example.invalid" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "/status", "/readonly", "/metadata" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "/checkout", "/submit", "/delete" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GET" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" },
            NexaExternalTargetCredentialPolicy.NoCredentials,
            NexaExternalTargetSubmitPolicy.ReadOnlyNoSubmit,
            NexaExternalTargetDataSensitivityProfile.LowRiskSynthetic,
            NexaExternalTargetEvidencePolicy.MetadataOnlyRedacted,
            "test-owned low-risk target, no personal data, no login",
            DateTimeOffset.UtcNow.AddDays(7),
            "operator-local",
            "approval:test-owned-readonly",
            ExplicitlyTestOwned: true);

    private static NexaExternalTestOwnedTargetDecision Evaluate(NexaExternalTestOwnedTarget? target) =>
        new NexaExternalTestOwnedTargetEvaluator().Evaluate(target, DateTimeOffset.UtcNow);
}
