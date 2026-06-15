using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserExternalAuthM25BTests
{
    [TestMethod]
    public void BrowserExternalLowRiskAuthLiveBlockedWithoutTargetConfig()
    {
        var result = Evaluate(Target("https://demo.example.test/login"), Policy(Set()));

        Assert.AreEqual(BrowserExternalAuthDecisionKind.BlockedNoSafeExternalTarget, result.Decision);
    }

    [TestMethod]
    public void BrowserExternalLowRiskAuthLiveRejectsNonAllowlistedHost()
    {
        var result = Evaluate(Target("https://demo.example.test/login"), Policy(Set("other.example.test")));

        Assert.AreEqual(BrowserExternalAuthDecisionKind.Blocked, result.Decision);
    }

    [TestMethod]
    public void BrowserExternalLowRiskAuthLiveRejectsSensitiveTarget()
    {
        var target = Target("https://demo.example.test/login") with { RiskProfile = BrowserExternalAuthRiskProfile.SensitivePersonalData, NoSensitivePersonalData = false };

        Assert.AreEqual(BrowserExternalAuthDecisionKind.Blocked, Evaluate(target, Policy(Set("demo.example.test"))).Decision);
    }

    [TestMethod]
    public void BrowserExternalLowRiskAuthLiveRequiresTestOwnedCredential()
    {
        var reference = BrowserVaultMinimalM23Tests.Reference(BrowserSecretKind.Password);
        var binding = new BrowserExternalLowRiskCredentialBinding("target-demo", reference, TestOwned: false, ContainsRealPersonalCredential: false);

        Assert.IsFalse(binding.IsUsable);
    }

    [TestMethod]
    public void BrowserExternalLowRiskAuthLiveRequiresConsentPolicyGate()
    {
        var noConsent = Evaluate(Target("https://demo.example.test/login"), Policy(Set("demo.example.test")), useDefaultConsent: false);
        var gateFailed = Evaluate(Target("https://demo.example.test/login"), Policy(Set("demo.example.test")), gate: BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { CompanionAuthoritative = true }));

        Assert.AreEqual(BrowserExternalAuthDecisionKind.RequiresConsent, noConsent.Decision);
        Assert.AreEqual(BrowserExternalAuthDecisionKind.RequiresGate, gateFailed.Decision);
    }

    [TestMethod]
    public void BrowserExternalLowRiskAuthLiveDoesNotExposeCookies()
    {
        var result = Evaluate(Target("https://demo.example.test/login"), Policy(Set("demo.example.test")));

        Assert.IsFalse(result.CookiesExposed);
        Assert.IsFalse(result.ToString()!.Contains("session=", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserExternalLowRiskAuthLiveBlocksPostLoginMutation()
    {
        var guard = new BrowserExternalAuthReadOnlyGuard();

        Assert.IsTrue(guard.Blocks("mutate"));
        Assert.IsTrue(guard.Blocks("submit"));
    }

    [TestMethod]
    public void BrowserExternalLowRiskAuthLiveRequiresSemanticProof()
    {
        var result = Evaluate(Target("https://demo.example.test/login"), Policy(Set("demo.example.test")));

        Assert.IsFalse(result.AllowsStepDone);
    }

    [TestMethod]
    [TestCategory("BrowserExternalLowRiskAuthLive")]
    public void BrowserExternalLowRiskAuthLiveTargetValidationIsBlockedWithoutConfiguredTarget()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ONEBRAIN_RUN_EXTERNAL_LOW_RISK_AUTH_TESTS"), "1", StringComparison.Ordinal))
            Assert.Inconclusive("External low-risk auth live tests are opt-in.");

        var result = Evaluate(Target("https://demo.example.test/login"), Policy(Set()));

        Assert.AreEqual(BrowserExternalAuthDecisionKind.BlockedNoSafeExternalTarget, result.Decision);
    }

    private static BrowserExternalAuthResult Evaluate(BrowserExternalAuthTarget target, BrowserExternalAuthPolicy policy, BrowserConsentGrant? consent = null, BrowserRuntimePhaseCloseReport? gate = null, bool useDefaultConsent = true) =>
        new BrowserExternalAuthPolicyEvaluator().Evaluate(
            new BrowserExternalAuthAttempt(
                "run-external-auth-live",
                "action-external-auth-live",
                "corr-external-auth-live",
                target,
                consent ?? (useDefaultConsent ? BrowserVaultMinimalM23Tests.Consent() : null),
                gate ?? BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true)),
                HasVaultTestCredential: true,
                HasControlledProfile: true),
            policy,
            DateTimeOffset.UtcNow);

    private static BrowserExternalAuthPolicy Policy(IReadOnlySet<string> hosts) =>
        new(hosts, RequireConsent: true, RequireVaultTestCredential: true, RequireControlledProfile: true, RequireGate: true, EnforceReadOnlyAfterLogin: true);

    private static IReadOnlySet<string> Set(params string[] hosts) =>
        new HashSet<string>(hosts, StringComparer.OrdinalIgnoreCase);

    private static BrowserExternalAuthTarget Target(string loginUrl)
    {
        var login = new Uri(loginUrl);
        return new BrowserExternalAuthTarget($"external-target-{Guid.NewGuid():N}", login, new Uri(login, "/dashboard"), "External low-risk test target", BrowserExternalAuthRiskProfile.LowRisk, true, true, true, true, true, true, false);
    }
}
