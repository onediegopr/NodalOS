using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaAdminRuntimeM40Tests
{
    [TestMethod]
    public void NexaAdminRuntimeCreatesPersonAccount()
    {
        var service = new NexaAdminConsoleService();
        var result = service.Execute(new NexaAdminCreateAccountCommand("admin-runtime-create-person", "actor-owner", NexaRole.Owner, "account-runtime-person", "org-runtime", NexaProductAccountKind.Person));

        Assert.IsTrue(result.Succeeded, result.Reason);
        Assert.IsTrue(service.State.TryGetAccount("account-runtime-person", out var account));
        Assert.IsTrue(account.IsPerson);
    }

    [TestMethod]
    public void NexaAdminRuntimeCreatesCompanyWithWorkspace()
    {
        var service = new NexaAdminConsoleService();
        service.Execute(new NexaAdminCreateAccountCommand("admin-runtime-create-company", "actor-owner", NexaRole.Owner, "account-runtime-company", "org-runtime", NexaProductAccountKind.Company));

        Assert.IsTrue(service.State.TryGetAccount("account-runtime-company", out var account));
        Assert.IsTrue(account.IsCompany);
        Assert.AreEqual(1, account.Workspaces.Count);
    }

    [TestMethod]
    public void NexaAdminRuntimeCreatesWorkerSeat()
    {
        var service = SeededService();
        var result = service.Execute(new NexaAdminCreateWorkerCommand("admin-runtime-create-worker", "actor-owner", NexaRole.Owner, "account-company", "org-main", "workspace-main", NexaRole.Operator));

        Assert.IsTrue(result.Succeeded, result.Reason);
        Assert.IsTrue(service.State.TryGetAccount("account-company", out var account));
        Assert.IsTrue(account.Workers.Count > 2);
        Assert.AreEqual(account.Workers.Count, account.Seats.Count);
    }

    [TestMethod]
    public void NexaAdminRuntimeChangesWorkerRoleWithOwner()
    {
        var service = SeededService();
        var workerId = NexaProductAdminM36Tests.CompanyAccount().Workers.Last().WorkerId;
        var result = service.Execute(new NexaAdminUpdateWorkerRoleCommand("admin-runtime-role", "actor-owner", NexaRole.Owner, "account-company", "org-main", workerId, NexaRole.Admin));

        Assert.IsTrue(result.Succeeded, result.Reason);
        Assert.IsTrue(service.State.TryGetAccount("account-company", out var account));
        Assert.AreEqual(NexaRole.Admin, account.Workers.Single(w => w.WorkerId == workerId).Role);
    }

    [TestMethod]
    public void NexaAdminRuntimeBlocksViewerMutation()
    {
        var result = SeededService().Execute(new NexaAdminCreateWorkerCommand("admin-runtime-viewer", "actor-viewer", NexaRole.Viewer, "account-company", "org-main", "workspace-main", NexaRole.Operator));

        Assert.AreEqual(NexaAdminDecisionKind.Denied, result.Decision);
    }

    [TestMethod]
    public void NexaAdminRuntimeBlocksSupportSecretAccess()
    {
        var decision = new NexaAdminPolicyService().Evaluate(new NexaAdminAuditQuery("admin-runtime-query", "actor-support", NexaRole.Support, "account-company", "org-main", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow), NexaProductAdminM36Tests.CompanyAccount());
        var secretDecision = new NexaAdminPolicyEvaluator().Evaluate(NexaProductAdminM36Tests.CompanyAccount(), "actor-support", NexaRole.Support, NexaAdminAction.ViewSecret);

        Assert.IsTrue(decision.Allowed);
        Assert.AreEqual(NexaAdminDecisionKind.Denied, secretDecision.Decision);
    }

    [TestMethod]
    public void NexaAdminRuntimeAssignsLicense()
    {
        var service = SeededService(includeLicense: false);
        var license = License("license-runtime-pro", "unassigned", NexaPlan.Pro());
        service.State.UpsertLicense(license);

        var result = service.Execute(new NexaAdminAssignLicenseCommand("admin-runtime-license", "actor-owner", NexaRole.Owner, "account-company", "org-main", license.LicenseId));

        Assert.IsTrue(result.Succeeded, result.Reason);
        Assert.AreEqual("account-company", service.State.LicenseForAccount("account-company")!.AccountId);
    }

    [TestMethod]
    public void NexaAdminRuntimeSetsFeatureFlagWhenAllowed()
    {
        var service = SeededService();
        var result = service.Execute(new NexaAdminSetFeatureFlagCommand("admin-runtime-feature", "actor-owner", NexaRole.Owner, "account-company", "org-main", NexaFeatureFlag.SafeDownload, Enabled: true, ComplianceApproved: false));

        Assert.IsTrue(result.Succeeded, result.Reason);
    }

    [TestMethod]
    public void NexaAdminRuntimeBlocksSensitiveRealPilotWithoutCompliance()
    {
        var service = SeededService();
        var result = service.Execute(new NexaAdminSetFeatureFlagCommand("admin-runtime-sensitive", "actor-owner", NexaRole.Owner, "account-company", "org-main", NexaFeatureFlag.SensitiveRealPilot, Enabled: true, ComplianceApproved: false));

        Assert.AreEqual(NexaAdminDecisionKind.Denied, result.Decision);
    }

    [TestMethod]
    public void NexaAdminRuntimeBlocksProductiveVaultWithoutEntitlement()
    {
        var service = SeededService();
        var result = service.Execute(new NexaAdminSetFeatureFlagCommand("admin-runtime-vault", "actor-owner", NexaRole.Owner, "account-company", "org-main", NexaFeatureFlag.ProductiveVault, Enabled: true, ComplianceApproved: false));

        Assert.AreEqual(NexaAdminDecisionKind.Denied, result.Decision);
    }

    [TestMethod]
    public void NexaAdminRuntimeBlocksRecorderReplayProductive()
    {
        var service = SeededService();
        var recorder = service.Execute(new NexaAdminSetFeatureFlagCommand("admin-runtime-recorder", "actor-owner", NexaRole.Owner, "account-company", "org-main", NexaFeatureFlag.RecorderProductive, Enabled: true, ComplianceApproved: true));
        var replay = service.Execute(new NexaAdminSetFeatureFlagCommand("admin-runtime-replay", "actor-owner", NexaRole.Owner, "account-company", "org-main", NexaFeatureFlag.ReplayProductive, Enabled: true, ComplianceApproved: true));

        Assert.AreEqual(NexaAdminDecisionKind.Denied, recorder.Decision);
        Assert.AreEqual(NexaAdminDecisionKind.Denied, replay.Decision);
    }

    [TestMethod]
    public void NexaAdminRuntimeSuspendsAccount()
    {
        var service = SeededService();
        var result = service.Execute(new NexaAdminSuspendAccountCommand("admin-runtime-suspend", "actor-owner", NexaRole.Owner, "account-company", "org-main"));

        Assert.IsTrue(result.Succeeded, result.Reason);
        Assert.AreEqual(NexaAccountStatus.Suspended, service.State.Accounts.Single(a => a.AccountId == "account-company").Status);
    }

    [TestMethod]
    public void NexaAdminRuntimeDashboardIsRedacted()
    {
        var dashboard = SeededService().Queries.GetDashboard("account-company");

        Assert.IsTrue(dashboard.Validate().IsValid, string.Join("; ", dashboard.Validate().Errors));
        Assert.IsFalse(dashboard.ToString()!.Contains("cookie", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NexaAdminRuntimeWritesAuditEventForMutation()
    {
        var service = SeededService();
        service.Execute(new NexaAdminSuspendAccountCommand("admin-runtime-audit", "actor-owner", NexaRole.Owner, "account-company", "org-main"));

        Assert.IsTrue(service.Audit.Events.Any(e => e.Action == NexaAdminAction.UpdateAccount));
        Assert.IsTrue(service.Audit.Events.All(e => e.Validate().IsValid));
    }

    internal static NexaAdminConsoleService SeededService(bool includeLicense = true)
    {
        var service = new NexaAdminConsoleService();
        service.State.UpsertAccount(NexaProductAdminM36Tests.CompanyAccount());
        if (includeLicense)
            service.State.UpsertLicense(License("license-runtime-pro", "account-company", NexaPlan.Pro()));
        return service;
    }

    internal static NexaLicense License(string licenseId, string accountId, NexaPlan plan) =>
        new(licenseId, accountId, plan, NexaLicenseStatus.Active, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(30), [], ManualAdminOverride: false);
}
