using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaAdminConsoleM38Tests
{
    [TestMethod]
    public void NexaAdminConsoleDashboardDoesNotExposeSecrets()
    {
        var dashboard = Dashboard();
        var text = dashboard.ToString()!;

        Assert.IsTrue(dashboard.Validate().IsValid, string.Join("; ", dashboard.Validate().Errors));
        Assert.IsFalse(text.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("cookie", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("card", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NexaAdminConsoleShowsPlanAndLicenseStatus()
    {
        var dashboard = Dashboard();

        Assert.AreEqual(NexaPlanKind.Pro, dashboard.License.Plan);
        Assert.AreEqual(NexaLicenseStatus.Active, dashboard.License.Status);
        Assert.IsTrue(dashboard.License.EnabledFeatures.Contains(NexaFeatureFlag.AdminConsole));
    }

    [TestMethod]
    public void NexaAdminConsoleShowsBlockedSensitiveFeatures()
    {
        var dashboard = Dashboard();

        Assert.IsTrue(dashboard.Features.Single(f => f.Feature == NexaFeatureFlag.SensitiveRealPilot).Blocked);
        Assert.IsTrue(dashboard.Features.Single(f => f.Feature == NexaFeatureFlag.ProductiveVault).Blocked);
        CollectionAssert.Contains(dashboard.BlockedCapabilities.ToList(), NexaFeatureFlag.SensitiveRealPilot.ToString());
        CollectionAssert.Contains(dashboard.BlockedCapabilities.ToList(), NexaFeatureFlag.ProductiveVault.ToString());
    }

    [TestMethod]
    public void NexaAdminConsoleShowsUsageLimits()
    {
        var dashboard = Dashboard();
        var usage = dashboard.Usage.Single();

        Assert.AreEqual("limit-pro-browser-runs", usage.LimitId);
        Assert.AreEqual(10, usage.Count);
        Assert.IsFalse(usage.LimitExceeded);
    }

    [TestMethod]
    public void NexaAdminCommandFailsForUnknownRole()
    {
        var result = new NexaAdminCommandHandler().Handle(UpdateCommand(NexaRole.Unknown), NexaProductAdminM36Tests.CompanyAccount());

        Assert.AreEqual(NexaAdminDecisionKind.FailClosed, result.Decision);
    }

    [TestMethod]
    public void NexaAdminCommandViewerCannotMutate()
    {
        var result = new NexaAdminCommandHandler().Handle(UpdateCommand(NexaRole.Viewer), NexaProductAdminM36Tests.CompanyAccount());

        Assert.AreEqual(NexaAdminDecisionKind.Denied, result.Decision);
    }

    [TestMethod]
    public void NexaAdminCommandSupportCannotSeeSecrets()
    {
        var decision = new NexaAdminPolicyEvaluator().Evaluate(NexaProductAdminM36Tests.CompanyAccount(), "actor-support", NexaRole.Support, NexaAdminAction.ViewSecret);

        Assert.AreEqual(NexaAdminDecisionKind.Denied, decision.Decision);
    }

    [TestMethod]
    public void NexaAdminCommandCannotEnableSensitiveRealPilotWithoutCompliance()
    {
        var command = FeatureCommand(NexaFeatureFlag.SensitiveRealPilot, complianceApproved: false);
        var result = new NexaAdminCommandHandler().Handle(command, NexaProductAdminM36Tests.CompanyAccount(), License());

        Assert.AreEqual(NexaAdminDecisionKind.Denied, result.Decision);
    }

    [TestMethod]
    public void NexaAdminCommandCannotEnableReplayProductive()
    {
        var command = FeatureCommand(NexaFeatureFlag.ReplayProductive, complianceApproved: true);
        var result = new NexaAdminCommandHandler().Handle(command, NexaProductAdminM36Tests.CompanyAccount(), License());

        Assert.AreEqual(NexaAdminDecisionKind.Denied, result.Decision);
    }

    [TestMethod]
    public void NexaAdminAuditViewIsRedacted()
    {
        var audit = Dashboard().Audit.Single();

        Assert.IsTrue(audit.Redacted);
        Assert.IsTrue(audit.Validate().IsValid, string.Join("; ", audit.Validate().Errors));
    }

    private static NexaAdminConsoleDashboardModel Dashboard()
    {
        var account = NexaProductAdminM36Tests.CompanyAccount();
        var license = License();
        var audit = new NexaAdminPolicyEvaluator().Evaluate(account, "actor-owner", NexaRole.Owner, NexaAdminAction.ViewAudit).AuditEvent;
        return new NexaAdminConsoleModelBuilder().Build(account, license, [new NexaUsageCounter("limit-pro-browser-runs", 10, DateTimeOffset.UtcNow)], [audit]);
    }

    private static NexaLicense License() =>
        new(
            "license-admin-console",
            "account-company",
            NexaPlan.Pro(),
            NexaLicenseStatus.Active,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddDays(30),
            [],
            ManualAdminOverride: false);

    private static NexaAdminUpdateAccountCommand UpdateCommand(NexaRole role) =>
        new("admin-command-update-account", "actor-admin", role, "account-company", "org-main", NexaAccountStatus.Active);

    private static NexaAdminSetFeatureFlagCommand FeatureCommand(NexaFeatureFlag feature, bool complianceApproved) =>
        new("admin-command-feature", "actor-owner", NexaRole.Owner, "account-company", "org-main", feature, Enabled: true, complianceApproved);
}
