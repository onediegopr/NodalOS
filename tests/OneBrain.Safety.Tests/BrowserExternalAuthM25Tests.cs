using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserExternalAuthM25Tests
{
    [TestMethod]
    public void BrowserExternalAuthRejectsNonAllowlistedDomain()
    {
        var result = Evaluate(Target("https://example.test/login"), Policy(Set("allowed.example.test")));

        Assert.AreEqual(BrowserExternalAuthDecisionKind.Blocked, result.Decision);
    }

    [TestMethod]
    public void BrowserExternalAuthRejectsFinancialDomain() =>
        Assert.AreEqual(BrowserExternalAuthDecisionKind.Blocked, Evaluate(Target("https://demo-bank.test/login") with { RiskProfile = BrowserExternalAuthRiskProfile.Financial, NonFinancial = false }, Policy(Set("demo-bank.test"))).Decision);

    [TestMethod]
    public void BrowserExternalAuthRejectsFiscalDomain() =>
        Assert.AreEqual(BrowserExternalAuthDecisionKind.Blocked, Evaluate(Target("https://fiscal.test/login") with { RiskProfile = BrowserExternalAuthRiskProfile.Fiscal, NonFiscal = false }, Policy(Set("fiscal.test"))).Decision);

    [TestMethod]
    public void BrowserExternalAuthRejectsErpDomain() =>
        Assert.AreEqual(BrowserExternalAuthDecisionKind.Blocked, Evaluate(Target("https://erp.test/login") with { RiskProfile = BrowserExternalAuthRiskProfile.Erp, NonErp = false }, Policy(Set("erp.test"))).Decision);

    [TestMethod]
    public void BrowserExternalAuthRequiresConsentPolicyGate()
    {
        var target = Target("https://demo.example.test/login");
        var noConsent = Evaluate(target, Policy(Set("demo.example.test")), useDefaultConsent: false);
        var gateFailed = Evaluate(target, Policy(Set("demo.example.test")), gate: BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { CompanionAuthoritative = true }));

        Assert.AreEqual(BrowserExternalAuthDecisionKind.RequiresConsent, noConsent.Decision);
        Assert.AreEqual(BrowserExternalAuthDecisionKind.RequiresGate, gateFailed.Decision);
    }

    [TestMethod]
    public void BrowserExternalAuthRequiresVaultTestCredential()
    {
        var result = Evaluate(Target("https://demo.example.test/login"), Policy(Set("demo.example.test")), hasVault: false);

        Assert.AreEqual(BrowserExternalAuthDecisionKind.RequiresVault, result.Decision);
    }

    [TestMethod]
    public void BrowserExternalAuthDoesNotExposeCredentialValues()
    {
        var result = Evaluate(Target("https://demo.example.test/login"), Policy(Set("demo.example.test")));

        Assert.IsFalse(result.CredentialValuesExposed);
        Assert.IsFalse(result.ToString()!.Contains("synthetic-local-passphrase", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BrowserExternalAuthDoesNotExposeCookies()
    {
        var result = Evaluate(Target("https://demo.example.test/login"), Policy(Set("demo.example.test")));

        Assert.IsFalse(result.CookiesExposed);
    }

    [TestMethod]
    public void BrowserExternalAuthStepDoneRequiresSemanticProof()
    {
        var result = Evaluate(Target("https://demo.example.test/login"), Policy(Set("demo.example.test")));

        Assert.IsFalse(result.AllowsStepDone);
    }

    [TestMethod]
    public void BrowserExternalAuthBlocksPostLoginSubmit()
    {
        var guard = new BrowserExternalAuthReadOnlyGuard();

        Assert.IsTrue(guard.Blocks("submit"));
    }

    [TestMethod]
    public void BrowserExternalAuthBlocksPostLoginMutation()
    {
        var guard = new BrowserExternalAuthReadOnlyGuard();

        Assert.IsTrue(guard.Blocks("mutate"));
    }

    [TestMethod]
    public void BrowserExternalAuthAllowsReadOnlyDashboard()
    {
        var guard = new BrowserExternalAuthReadOnlyGuard();

        Assert.IsTrue(guard.Allows("read"));
        Assert.IsTrue(guard.Allows("navigate-readonly"));
    }

    [TestMethod]
    public void BrowserExternalLowRiskAuthLiveIsBlockedWithoutSafeTarget()
    {
        var result = Evaluate(Target("https://demo.example.test/login"), Policy(Set()));

        Assert.AreEqual(BrowserExternalAuthDecisionKind.BlockedNoSafeExternalTarget, result.Decision);
    }

    private static BrowserExternalAuthResult Evaluate(BrowserExternalAuthTarget target, BrowserExternalAuthPolicy policy, BrowserConsentGrant? consent = null, BrowserRuntimePhaseCloseReport? gate = null, bool hasVault = true, bool useDefaultConsent = true) =>
        new BrowserExternalAuthPolicyEvaluator().Evaluate(
            new BrowserExternalAuthAttempt(
                "run-external-auth",
                "action-external-auth",
                "corr-external-auth",
                target,
                consent ?? (useDefaultConsent ? BrowserVaultMinimalM23Tests.Consent() : null),
                gate ?? BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true)),
                hasVault,
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
        return new BrowserExternalAuthTarget(
            $"external-target-{Guid.NewGuid():N}",
            login,
            new Uri(login, "/dashboard"),
            "Low risk demo",
            BrowserExternalAuthRiskProfile.LowRisk,
            NonFinancial: true,
            NonFiscal: true,
            NonErp: true,
            NoIrreversibleActions: true,
            NoSensitivePersonalData: true,
            ReadOnlyAfterLogin: true,
            RequiresTwoFactorOrCaptcha: false);
    }
}
