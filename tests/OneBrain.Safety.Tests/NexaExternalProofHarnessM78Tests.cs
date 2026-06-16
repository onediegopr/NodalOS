using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaExternalProofHarnessM78Tests
{
    [TestMethod]
    public void ExternalProofHarnessIsDisabledByDefault()
    {
        var decision = Harness().Evaluate(Request(optIn: false, NexaExternalTestOwnedTargetM77Tests.ApprovedTarget()), DateTimeOffset.UtcNow);

        Assert.AreEqual(NexaExternalProofHarnessDecisionKind.SkippedNoOptIn, decision.Decision);
        Assert.IsFalse(decision.CanExecuteReadOnlyNavigation);
        Assert.IsTrue(decision.Explanation.OperatorReadableMessage.Contains("Skipped external/live tests", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ExternalProofHarnessNoTargetRemainsBlockedDeferred()
    {
        var decision = Harness().Evaluate(Request(optIn: true, target: null), DateTimeOffset.UtcNow);

        Assert.AreEqual(NexaExternalProofHarnessDecisionKind.BlockedNoTarget, decision.Decision);
        Assert.AreEqual(NexaExternalTestOwnedTargetStatus.MissingTarget, decision.TargetDecision.Status);
    }

    [TestMethod]
    public void ExternalProofHarnessUnapprovedTargetIsBlocked()
    {
        var target = NexaExternalTestOwnedTargetM77Tests.ApprovedTarget() with { ExplicitlyTestOwned = false };
        var decision = Harness().Evaluate(Request(optIn: true, target), DateTimeOffset.UtcNow);

        Assert.AreEqual(NexaExternalProofHarnessDecisionKind.BlockedPolicyViolation, decision.Decision);
        Assert.IsFalse(decision.CanExecuteReadOnlyNavigation);
    }

    [TestMethod]
    public void ExternalProofHarnessApprovedTargetWithOptInAllowsReadOnlyProof()
    {
        var decision = Harness().Evaluate(Request(optIn: true, NexaExternalTestOwnedTargetM77Tests.ApprovedTarget()), DateTimeOffset.UtcNow);

        Assert.AreEqual(NexaExternalProofHarnessDecisionKind.AllowedReadOnlyProof, decision.Decision);
        Assert.IsTrue(decision.CanExecuteReadOnlyNavigation);
    }

    [TestMethod]
    public void ExternalProofHarnessRejectsMutatingMethodBeforeExecution()
    {
        var decision = Harness().Evaluate(Request(optIn: true, NexaExternalTestOwnedTargetM77Tests.ApprovedTarget(), method: "POST"), DateTimeOffset.UtcNow);

        Assert.AreEqual(NexaExternalProofHarnessDecisionKind.BlockedPolicyViolation, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), "requested method rejected before execution");
    }

    [TestMethod]
    public void ExternalProofHarnessRejectsSensitiveHostBeforeExecution()
    {
        var target = NexaExternalTestOwnedTargetM77Tests.ApprovedTarget("https://afip.gob.ar/status") with
        {
            AllowedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "afip.gob.ar" }
        };
        var decision = Harness().Evaluate(Request(optIn: true, target, host: "afip.gob.ar"), DateTimeOffset.UtcNow);

        Assert.AreEqual(NexaExternalProofHarnessDecisionKind.BlockedPolicyViolation, decision.Decision);
        Assert.AreEqual(NexaExternalTestOwnedTargetStatus.BlockedSensitiveSurface, decision.TargetDecision.Status);
    }

    [TestMethod]
    public void ExternalProofHarnessAppliesEvidenceRedaction()
    {
        var decision = Harness().Evaluate(Request(optIn: true, NexaExternalTestOwnedTargetM77Tests.ApprovedTarget(), path: "/status?token=opaque-token-value-123456789"), DateTimeOffset.UtcNow);
        var serialized = System.Text.Json.JsonSerializer.Serialize(decision);

        Assert.IsTrue(decision.Redacted);
        Assert.IsFalse(serialized.Contains("opaque-token-value-123456789", StringComparison.Ordinal));
    }

    private static NexaExternalProofHarness Harness() => new();

    internal static NexaExternalProofHarnessRequest Request(
        bool optIn,
        NexaExternalTestOwnedTarget? target,
        string host = "nodal-os-test-owned.example.invalid",
        string path = "/status",
        string method = "GET") =>
        new(
            optIn,
            target,
            host,
            path,
            method,
            WouldCaptureBodies: false,
            WouldCaptureSensitiveHeaderValues: false,
            WouldPersistCookies: false,
            WouldSubmit: false,
            "operator-local");
}
