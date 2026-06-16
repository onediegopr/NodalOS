using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaPrivatePreviewOperatorM71Tests
{
    [TestMethod]
    public void NexaPrivatePreviewOperatorFlowStartsLocalSession()
    {
        var result = RunSafe();

        Assert.AreEqual(NexaPrivatePreviewOperatorFinalStatus.Completed, result.FinalStatus);
        Assert.IsTrue(result.Flow.Session.LocalOnly);
        Assert.AreEqual("operator-session-local", result.Flow.Session.SessionId);
    }

    [TestMethod]
    public void NexaPrivatePreviewOperatorFlowRequiresSafeConfigProfile() =>
        AssertBlocked(flow => flow with { Session = flow.Session with { ConfigProfile = NexaConfigurationProfileKind.ProductionLocked } }, "configuration profile unsafe");

    [TestMethod]
    public void NexaPrivatePreviewOperatorFlowRequiresValidMockLicense() =>
        AssertBlocked(readiness: NexaPrivatePreviewLocalEvaluator.SafeReadiness() with { LicenseMockValid = false }, expected: "mock license invalid or expired");

    [TestMethod]
    public void NexaPrivatePreviewOperatorFlowRequiresTenantGovernance() =>
        AssertBlocked(readiness: NexaPrivatePreviewLocalEvaluator.SafeReadiness() with { TenantGovernanceAvailable = false }, expected: "tenant governance unavailable");

    [TestMethod]
    public void NexaPrivatePreviewOperatorFlowRequiresDiagnosticsRedaction() =>
        AssertBlocked(readiness: NexaPrivatePreviewLocalEvaluator.SafeReadiness() with { DiagnosticsRedacted = false }, expected: "diagnostics redaction unavailable");

    [TestMethod]
    public void NexaPrivatePreviewOperatorFlowRequiresAuditKeyCustody() =>
        AssertBlocked(readiness: NexaPrivatePreviewLocalEvaluator.SafeReadiness() with { AuditKeyCustodyAvailable = false }, expected: "audit key custody missing");

    [TestMethod]
    public void NexaPrivatePreviewOperatorFlowBlocksPublicSaas() =>
        AssertBlocked(flow => flow with { PublicSaasEnabled = true }, "public SaaS blocked");

    [TestMethod]
    public void NexaPrivatePreviewOperatorFlowBlocksRealBilling() =>
        AssertBlocked(flow => flow with { RealBillingEnabled = true }, "real billing blocked");

    [TestMethod]
    public void NexaPrivatePreviewOperatorFlowBlocksRealEmail() =>
        AssertBlocked(flow => flow with { RealEmailEnabled = true }, "real email blocked");

    [TestMethod]
    public void NexaPrivatePreviewOperatorFlowBlocksSensitiveRealPilot() =>
        AssertBlocked(flow => flow with { SensitiveRealPilotEnabled = true }, "sensitive real pilot blocked");

    [TestMethod]
    public void NexaPrivatePreviewOperatorFlowUsesPrivateLocalApiOnly()
    {
        var result = RunSafe();

        Assert.IsTrue(result.Flow.UsedPrivateLocalApiInProcess);
        Assert.IsFalse(result.Flow.PublicSaasEnabled);
    }

    [TestMethod]
    public void NexaPrivatePreviewOperatorFlowProducesRedactedSummary()
    {
        var result = RunSafe();
        var serialized = System.Text.Json.JsonSerializer.Serialize(result);

        Assert.IsTrue(result.Redacted);
        Assert.IsTrue(result.Flow.Evidence.Redacted);
        Assert.IsFalse(serialized.Contains("synthetic-password-value", StringComparison.Ordinal));
        Assert.IsFalse(serialized.Contains("synthetic-cookie-session-value", StringComparison.Ordinal));
    }

    private static NexaPrivatePreviewOperatorResult RunSafe() =>
        Service().Run(Service().CreateSafeFlow(), NexaPrivatePreviewLocalEvaluator.SafeReadiness());

    private static void AssertBlocked(Func<NexaPrivatePreviewOperatorFlow, NexaPrivatePreviewOperatorFlow>? mutate = null, string? expected = null, NexaPrivatePreviewLocalReadiness? readiness = null)
    {
        var service = Service();
        var flow = mutate?.Invoke(service.CreateSafeFlow()) ?? service.CreateSafeFlow();
        var result = service.Run(flow, readiness ?? NexaPrivatePreviewLocalEvaluator.SafeReadiness());

        Assert.AreEqual(NexaPrivatePreviewOperatorDecisionKind.Blocked, result.Decision.Decision);
        CollectionAssert.Contains(result.Decision.ReasonCodes.ToList(), expected);
    }

    private static NexaPrivatePreviewOperatorFlowService Service() => new();
}
