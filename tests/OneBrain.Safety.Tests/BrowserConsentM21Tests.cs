using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserConsentM21Tests
{
    [TestMethod]
    public void BrowserConsentUiShowsScopeTtlAndRisk()
    {
        var service = new BrowserConsentService();
        var request = Request(service);
        var ui = service.CreateUiModel(request);

        Assert.IsTrue(ui.Validate().IsValid);
        Assert.AreEqual("NEXA necesita autorización", ui.Title);
        Assert.AreEqual(BrowserConsentScope.Profile.ToString(), ui.Scope);
        Assert.IsTrue(ui.Ttl > TimeSpan.Zero);
        Assert.IsTrue(ui.Risks.Count > 0);
        StringAssert.Contains(ui.Explanation, "no autoriza acciones sensibles automáticamente");
    }

    [TestMethod]
    public void BrowserConsentGrantExpiresAfterTtl()
    {
        var service = new BrowserConsentService();
        var request = Request(service, ttl: TimeSpan.FromMilliseconds(10));
        var grant = service.Decide(request, BrowserConsentStatus.Granted, "core", "proof-consent", DateTimeOffset.UtcNow).Grant!;

        var decision = service.Evaluate(grant, DateTimeOffset.UtcNow.AddSeconds(1));

        Assert.AreEqual(BrowserConsentStatus.Expired, decision.Status);
        Assert.IsFalse(decision.AllowsPolicyEvaluation(DateTimeOffset.UtcNow.AddSeconds(1)));
    }

    [TestMethod]
    public void BrowserConsentGrantCanBeRevoked()
    {
        var service = new BrowserConsentService();
        var grant = service.Decide(Request(service), BrowserConsentStatus.Granted, "core", "proof-consent", DateTimeOffset.UtcNow).Grant!;

        var revoked = service.Revoke(grant, "operator", DateTimeOffset.UtcNow);

        Assert.AreEqual(BrowserConsentStatus.Revoked, revoked.Status);
        Assert.IsNotNull(revoked.Grant!.RevokedAtUtc);
    }

    [TestMethod]
    public void BrowserConsentRevokedGrantBlocksCapability()
    {
        var service = new BrowserConsentService();
        var grant = service.Decide(Request(service), BrowserConsentStatus.Granted, "core", "proof-consent", DateTimeOffset.UtcNow).Grant!;
        var revoked = service.Revoke(grant, "operator", DateTimeOffset.UtcNow).Grant!;

        Assert.IsFalse(revoked.AllowsCapability(BrowserConsentCapability.ProfileControlledActivation, BrowserConsentScope.Profile, DateTimeOffset.UtcNow));
    }

    [TestMethod]
    public void BrowserConsentExpiredGrantBlocksCapability()
    {
        var service = new BrowserConsentService();
        var request = Request(service, ttl: TimeSpan.FromMilliseconds(1));
        var grant = service.Decide(request, BrowserConsentStatus.Granted, "core", "proof-consent", DateTimeOffset.UtcNow).Grant!;

        Assert.IsFalse(grant.AllowsCapability(BrowserConsentCapability.ProfileControlledActivation, BrowserConsentScope.Profile, DateTimeOffset.UtcNow.AddSeconds(1)));
    }

    [TestMethod]
    public void BrowserConsentDoesNotAuthorizeActionByItself()
    {
        var service = new BrowserConsentService();
        var decision = service.Decide(Request(service), BrowserConsentStatus.Granted, "core", "proof-consent", DateTimeOffset.UtcNow);

        Assert.IsTrue(decision.AllowsPolicyEvaluation(DateTimeOffset.UtcNow));
        Assert.IsFalse(decision.AuthorizesAction);
        Assert.IsFalse(decision.MarksStepDone);
        Assert.IsFalse(decision.Grant!.AuthorizesActionByItself);
    }

    [TestMethod]
    public void BrowserConsentAuditDoesNotContainSecrets()
    {
        var service = new BrowserConsentService();
        var request = service.CreateRequest(
            BrowserConsentCapability.ProfileControlledActivation,
            BrowserConsentScope.Profile,
            "run-consent",
            "action-consent",
            "corr-consent",
            "core",
            BrowserCredentialRedactor.Redact("activate profile with token=synthetic"),
            TimeSpan.FromMinutes(5));

        var decision = service.Decide(request, BrowserConsentStatus.Granted, "core", "proof-consent", DateTimeOffset.UtcNow);
        var serialized = System.Text.Json.JsonSerializer.Serialize(decision);

        Assert.IsTrue(decision.AuditEvent.Validate().IsValid);
        Assert.IsFalse(serialized.Contains("token=synthetic", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserConsentApprovalDoesNotMarkStepDone()
    {
        var service = new BrowserConsentService();
        var decision = service.Decide(Request(service), BrowserConsentStatus.Granted, "core", "proof-consent", DateTimeOffset.UtcNow);

        Assert.IsFalse(decision.MarksStepDone);
    }

    [TestMethod]
    public void BrowserConsentResumeRequiresCorePolicy()
    {
        var service = new BrowserConsentService();
        var decision = service.Decide(Request(service), BrowserConsentStatus.Granted, "core", "proof-consent", DateTimeOffset.UtcNow);

        Assert.IsTrue(decision.AllowsPolicyEvaluation(DateTimeOffset.UtcNow));
        Assert.IsFalse(decision.AuthorizesAction);
    }

    [TestMethod]
    public void BrowserConsentHandoffExplainsUserActionClearly()
    {
        var service = new BrowserConsentService();
        var ui = service.CreateUiModel(Request(service));

        CollectionAssert.Contains(ui.Options.ToList(), "Approve");
        CollectionAssert.Contains(ui.Options.ToList(), "Deny");
        CollectionAssert.Contains(ui.Options.ToList(), "Revoke");
        StringAssert.Contains(ui.RevokeInstructions, "revocar");
    }

    private static BrowserConsentRequest Request(BrowserConsentService service, TimeSpan? ttl = null) =>
        service.CreateRequest(
            BrowserConsentCapability.ProfileControlledActivation,
            BrowserConsentScope.Profile,
            "run-consent",
            "action-consent",
            "corr-consent",
            "core-browser-runtime",
            "activate controlled browser profile",
            ttl ?? TimeSpan.FromMinutes(5));
}
