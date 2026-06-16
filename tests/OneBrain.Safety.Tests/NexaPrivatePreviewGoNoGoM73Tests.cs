using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaPrivatePreviewGoNoGoM73Tests
{
    [TestMethod]
    public void NexaPrivatePreviewGoNoGoAllowsNextLocalPreviewWhenSafe()
    {
        var report = Evaluate(NexaPrivatePreviewGoNoGoService.SafeCriteria());

        Assert.AreEqual(NexaPrivatePreviewGoNoGoDecisionKind.GoForNextLocalPreview, report.Decision);
        Assert.AreEqual(NexaPrivatePreviewNextStageRecommendation.ContinueLocalPrivatePreview, report.Recommendation);
    }

    [TestMethod]
    public void NexaPrivatePreviewGoNoGoBlocksOnSecurityBlocker()
    {
        var issue = new NexaPrivatePreviewIssueDecision(NexaPrivatePreviewIssueDecisionKind.SecurityBlocker, NexaPrivatePreviewIssueDisposition.Blocked, ["security blocker"], [], Redacted: true);
        var report = Evaluate(NexaPrivatePreviewGoNoGoService.SafeCriteria() with { NoCriticalHighSecurityBlockers = false }, [issue]);

        Assert.AreEqual(NexaPrivatePreviewGoNoGoDecisionKind.NoGoSecurityBlocker, report.Decision);
    }

    [TestMethod]
    public void NexaPrivatePreviewGoNoGoBlocksOnReleaseBlocker()
    {
        var issue = new NexaPrivatePreviewIssueDecision(NexaPrivatePreviewIssueDecisionKind.ReleaseBlocker, NexaPrivatePreviewIssueDisposition.Blocked, ["release blocker"], [], Redacted: true);
        var report = Evaluate(NexaPrivatePreviewGoNoGoService.SafeCriteria() with { NoUnresolvedReleaseBlockers = false }, [issue]);

        Assert.AreEqual(NexaPrivatePreviewGoNoGoDecisionKind.NoGoReleaseBlocker, report.Decision);
    }

    [TestMethod]
    public void NexaPrivatePreviewGoNoGoRequiresAuditKeyCustody() =>
        AssertMissingEvidence(NexaPrivatePreviewGoNoGoService.SafeCriteria() with { AuditKeyCustodyOk = false }, "audit key custody missing");

    [TestMethod]
    public void NexaPrivatePreviewGoNoGoRequiresDiagnosticsRedaction() =>
        AssertMissingEvidence(NexaPrivatePreviewGoNoGoService.SafeCriteria() with { DiagnosticsRedactionOk = false }, "diagnostics redaction missing");

    [TestMethod]
    public void NexaPrivatePreviewGoNoGoRequiresTenantGovernance() =>
        AssertMissingEvidence(NexaPrivatePreviewGoNoGoService.SafeCriteria() with { TenantGovernanceOk = false }, "tenant governance missing");

    [TestMethod]
    public void NexaPrivatePreviewGoNoGoRequiresApiRoleEnforcement() =>
        AssertMissingEvidence(NexaPrivatePreviewGoNoGoService.SafeCriteria() with { PrivateLocalApiRoleEnforcementOk = false }, "private local API role enforcement missing");

    [TestMethod]
    public void NexaPrivatePreviewGoNoGoKeepsPublicSaasDisabled()
    {
        var report = Evaluate(NexaPrivatePreviewGoNoGoService.SafeCriteria());

        Assert.IsTrue(report.PublicSaasStillDisabled);
        AssertMissingEvidence(NexaPrivatePreviewGoNoGoService.SafeCriteria() with { PublicSaasDisabled = false }, "public SaaS/API listener must remain disabled");
    }

    [TestMethod]
    public void NexaPrivatePreviewGoNoGoKeepsRealBillingDisabled()
    {
        var report = Evaluate(NexaPrivatePreviewGoNoGoService.SafeCriteria());

        Assert.IsTrue(report.RealBillingStillDisabled);
        AssertMissingEvidence(NexaPrivatePreviewGoNoGoService.SafeCriteria() with { BillingEmailMockOrSandbox = false }, "billing/email must remain mock or sandbox");
    }

    [TestMethod]
    public void NexaPrivatePreviewGoNoGoReportsM51Deferred()
    {
        var report = Evaluate(NexaPrivatePreviewGoNoGoService.SafeCriteria());

        Assert.IsTrue(report.M51DeferredExplicit);
        Assert.AreEqual(NexaPrivatePreviewGoNoGoDecisionKind.GoForNextLocalPreview, report.Decision);
    }

    [TestMethod]
    public void NexaPrivatePreviewGoNoGoDoesNotExposeSecrets()
    {
        var report = Evaluate(NexaPrivatePreviewGoNoGoService.SafeCriteria() with { ContainsSecretsCookiesBodies = true });
        var serialized = System.Text.Json.JsonSerializer.Serialize(report);

        Assert.IsTrue(report.Redacted);
        Assert.IsFalse(serialized.Contains("synthetic-password-value", StringComparison.Ordinal));
        Assert.IsFalse(serialized.Contains("synthetic-cookie-session-value", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateAllowsPrivatePreviewOperationsWhenSafe()
    {
        var state = BrowserVaultMinimalM23Tests.SafeState() with
        {
            PrivatePreviewOperatorFlowDefined = true,
            PrivatePreviewIssueTriageDefined = true,
            PrivatePreviewGoNoGoDefined = true,
            PrivatePreviewLocalGoAllowed = true,
            M51ExternalProofDeferred = true
        };

        Assert.IsTrue(state.PrivatePreviewOperationsAllowed);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateBlocksGoWithSecurityBlocker()
    {
        var state = BrowserVaultMinimalM23Tests.SafeState() with
        {
            PrivatePreviewOperatorFlowDefined = true,
            PrivatePreviewIssueTriageDefined = true,
            PrivatePreviewGoNoGoDefined = true,
            PrivatePreviewLocalGoAllowed = true,
            M51ExternalProofDeferred = true,
            PrivatePreviewGoAllowedWithSecurityBlocker = true
        };

        Assert.IsFalse(state.PrivatePreviewOperationsAllowed);
    }

    private static void AssertMissingEvidence(NexaPrivatePreviewExitCriteria criteria, string expected)
    {
        var report = Evaluate(criteria);

        CollectionAssert.Contains(report.Blockers.ToList(), expected);
        Assert.AreEqual(NexaPrivatePreviewGoNoGoDecisionKind.NoGoMissingEvidence, report.Decision);
    }

    private static NexaPrivatePreviewGoNoGoReport Evaluate(NexaPrivatePreviewExitCriteria criteria, IReadOnlyList<NexaPrivatePreviewIssueDecision>? issues = null) =>
        new NexaPrivatePreviewGoNoGoService().Evaluate(criteria, issues ?? []);
}
