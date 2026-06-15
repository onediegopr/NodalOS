using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaLicenseM37Tests
{
    [TestMethod]
    public void NexaFreePlanExpiresAfterSevenDays()
    {
        var now = DateTimeOffset.UtcNow;
        var license = new NexaFreeLicenseRequest("free@example.test", now, null, TimeSpan.FromDays(30)).Generate("account-person");

        Assert.AreEqual(now.AddDays(7), license.ExpiresAtUtc);
        Assert.AreEqual(NexaPlanKind.Free, license.Plan.Kind);
    }

    [TestMethod]
    public void NexaFreePlanAllowsOnlyOnePerEmailWindow()
    {
        var now = DateTimeOffset.UtcNow;
        var request = new NexaFreeLicenseRequest("free@example.test", now, now.AddDays(-2), TimeSpan.FromDays(30));

        Assert.IsFalse(request.CanGenerate);
    }

    [TestMethod]
    public void NexaFreePlanDoesNotEnableSensitiveRealPilot()
    {
        var plan = NexaPlan.Free();

        Assert.IsFalse(plan.Enables(NexaFeatureFlag.SensitiveRealPilot));
        Assert.IsTrue(plan.Validate().IsValid);
    }

    [TestMethod]
    public void NexaTrialPlanSupportsCustomFeatureLimits()
    {
        var plan = NexaPlan.Trial(
            new HashSet<NexaFeatureFlag> { NexaFeatureFlag.BrowserRuntime, NexaFeatureFlag.SafeDownload },
            [new NexaUsageLimit("limit-trial-custom", 5, TimeSpan.FromDays(3))],
            TimeSpan.FromDays(3));

        Assert.IsTrue(plan.Enables(NexaFeatureFlag.SafeDownload));
        Assert.AreEqual(5, plan.Limits.Single().MaxCount);
    }

    [TestMethod]
    public void NexaProPlanDoesNotEnableSensitiveSitesByDefault()
    {
        var plan = NexaPlan.Pro();

        Assert.IsFalse(plan.Enables(NexaFeatureFlag.SensitiveRealPilot));
        Assert.IsFalse(plan.Enables(NexaFeatureFlag.ProductiveVault));
        Assert.IsTrue(plan.Validate().IsValid);
    }

    [TestMethod]
    public void NexaEnterprisePlanCanDefineOrganizationPolicies()
    {
        var plan = NexaPlan.Enterprise(new HashSet<NexaFeatureFlag> { NexaFeatureFlag.BrowserRuntime, NexaFeatureFlag.SensitiveSimulation, NexaFeatureFlag.AdminConsole }, maxWorkers: 50);

        Assert.AreEqual(NexaPlanKind.Enterprise, plan.Kind);
        Assert.IsTrue(plan.Enables(NexaFeatureFlag.SensitiveSimulation));
        Assert.AreEqual(50, plan.MaxWorkers);
    }

    [TestMethod]
    public void NexaLicenseDecisionFailsWhenExpired()
    {
        var now = DateTimeOffset.UtcNow;
        var license = License(NexaPlan.Pro(), now.AddDays(-2), now.AddDays(-1));
        var decision = Evaluate(license, NexaFeatureFlag.SafeDownload, now);

        Assert.AreEqual(NexaLicenseDecisionKind.Denied, decision.Decision);
    }

    [TestMethod]
    public void NexaLicenseDecisionFailsWhenFeatureDisabled()
    {
        var decision = Evaluate(License(NexaPlan.Free()), NexaFeatureFlag.SafeDownload);

        Assert.AreEqual(NexaLicenseDecisionKind.Denied, decision.Decision);
    }

    [TestMethod]
    public void NexaLicenseDecisionFailsWhenUsageLimitExceeded()
    {
        var plan = NexaPlan.Pro();
        var counter = new NexaUsageCounter("limit-pro-browser-runs", plan.Limits.Single().MaxCount, DateTimeOffset.UtcNow);
        var decision = Evaluate(License(plan), NexaFeatureFlag.SafeDownload, usageCounter: counter);

        Assert.AreEqual(NexaLicenseDecisionKind.Denied, decision.Decision);
    }

    [TestMethod]
    public void NexaLicenseDecisionFailsWhenWorkerUnauthorized()
    {
        var worker = NexaProductAdminM36Tests.Worker("worker-disabled", "workspace-main") with { Active = false };
        var decision = Evaluate(License(NexaPlan.Pro()), NexaFeatureFlag.SafeDownload, worker: worker);

        Assert.AreEqual(NexaLicenseDecisionKind.Denied, decision.Decision);
    }

    [TestMethod]
    public void NexaLicenseAuditDoesNotContainSecrets()
    {
        var decision = Evaluate(License(NexaPlan.Pro()), NexaFeatureFlag.SafeDownload);
        var text = decision.AuditEvent.ToString()!;

        Assert.IsTrue(decision.AuditEvent.Validate().IsValid);
        Assert.IsFalse(text.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("cookie", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("card", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenSensitiveRealPilotFeatureEnabledWithoutDecision()
    {
        var report = PhaseReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with
        {
            ProductAdminFoundationDefined = true,
            LicensingFoundationDefined = true,
            FeatureFlagSensitiveRealPilotEnabled = true,
            SensitiveRealPilotDecisionApproved = false
        });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "product governance feature flags safe");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenProductiveVaultFeatureEnabled()
    {
        var report = PhaseReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with
        {
            ProductAdminFoundationDefined = true,
            LicensingFoundationDefined = true,
            FeatureFlagProductiveVaultEnabled = true
        });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenReplayProductiveFeatureEnabled()
    {
        var report = PhaseReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with
        {
            ProductAdminFoundationDefined = true,
            LicensingFoundationDefined = true,
            FeatureFlagReplayProductiveEnabled = true
        });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenExpiredLicenseAttemptsExecution()
    {
        var report = PhaseReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with
        {
            ProductAdminFoundationDefined = true,
            LicensingFoundationDefined = true,
            ExpiredLicenseAttemptsExecution = true
        });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
    }

    private static NexaLicensePolicyDecision Evaluate(NexaLicense license, NexaFeatureFlag feature, DateTimeOffset? now = null, NexaWorker? worker = null, NexaUsageCounter? usageCounter = null)
    {
        var account = NexaProductAdminM36Tests.CompanyAccount();
        return new NexaLicensePolicyEvaluator().Evaluate(new NexaLicensePolicyRequest(account, license, feature, worker ?? account.Workers.First(), usageCounter, now ?? DateTimeOffset.UtcNow, SensitiveCompliancePolicyApproved: false));
    }

    private static NexaLicense License(NexaPlan plan, DateTimeOffset? issued = null, DateTimeOffset? expires = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new NexaLicense(
            $"license-{Guid.NewGuid():N}",
            "account-company",
            plan,
            NexaLicenseStatus.Active,
            issued ?? now.AddMinutes(-1),
            expires ?? now.AddDays(30),
            [],
            ManualAdminOverride: false);
    }

    private static BrowserRuntimePhaseCloseReport PhaseReport(BrowserRuntimeObservedState state)
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        return BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, state);
    }
}
