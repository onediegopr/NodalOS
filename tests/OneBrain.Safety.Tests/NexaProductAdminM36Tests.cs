using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaProductAdminM36Tests
{
    [TestMethod]
    public void NexaProductAccountSupportsPersonAndCompany()
    {
        var person = PersonAccount();
        var company = CompanyAccount(workerCount: 2);

        Assert.IsTrue(person.IsPerson);
        Assert.IsTrue(company.IsCompany);
        Assert.IsTrue(person.Validate().IsValid, string.Join("; ", person.Validate().Errors));
        Assert.IsTrue(company.Validate().IsValid, string.Join("; ", company.Validate().Errors));
    }

    [TestMethod]
    public void NexaCompanyAccountSupportsMultipleWorkers()
    {
        var account = CompanyAccount(workerCount: 3);

        Assert.IsTrue(account.Company!.SupportsMultipleWorkers);
        Assert.AreEqual(3, account.Workers.Count);
    }

    [TestMethod]
    public void NexaWorkerRequiresWorkspace()
    {
        var worker = Worker("worker-no-workspace", "");

        Assert.IsFalse(worker.Validate().IsValid);
    }

    [TestMethod]
    public void NexaAdminRoleOwnerCanManageWorkers()
    {
        var decision = new NexaAdminPolicyEvaluator().Evaluate(CompanyAccount(), "actor-owner", NexaRole.Owner, NexaAdminAction.AddWorker);

        Assert.IsTrue(decision.Allowed, decision.Reason);
        Assert.AreEqual(NexaAdminCapability.ManageWorkers, decision.RequiredCapability);
    }

    [TestMethod]
    public void NexaAdminRoleViewerCannotMutate()
    {
        var decision = new NexaAdminPolicyEvaluator().Evaluate(CompanyAccount(), "actor-viewer", NexaRole.Viewer, NexaAdminAction.AddWorker);

        Assert.AreEqual(NexaAdminDecisionKind.Denied, decision.Decision);
        Assert.IsFalse(decision.Allowed);
    }

    [TestMethod]
    public void NexaSupportRoleCannotSeeSecrets()
    {
        var decision = new NexaAdminPolicyEvaluator().Evaluate(CompanyAccount(), "actor-support", NexaRole.Support, NexaAdminAction.ViewSecret);

        Assert.AreEqual(NexaAdminDecisionKind.Denied, decision.Decision);
    }

    [TestMethod]
    public void NexaAdminAuditDoesNotContainSecrets()
    {
        var decision = new NexaAdminPolicyEvaluator().Evaluate(CompanyAccount(), "actor-owner", NexaRole.Owner, NexaAdminAction.ManagePlan);
        var text = decision.AuditEvent.ToString()!;

        Assert.IsTrue(decision.AuditEvent.Validate().IsValid);
        Assert.IsFalse(text.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("cookie", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("card", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NexaAdminDecisionFailsClosedForUnknownRole()
    {
        var decision = new NexaAdminPolicyEvaluator().Evaluate(CompanyAccount(), "actor-unknown", NexaRole.Unknown, NexaAdminAction.AddWorker);

        Assert.AreEqual(NexaAdminDecisionKind.FailClosed, decision.Decision);
    }

    internal static NexaProductAccount PersonAccount() =>
        new(
            "account-person",
            NexaProductAccountKind.Person,
            NexaAccountStatus.Free,
            new NexaPersonAccount("person-one", "person@example.test", NexaAccountStatus.Free),
            null,
            Organization(),
            [Workspace()],
            [Worker("worker-one", "workspace-main")],
            [Seat("seat-one", "account-person", NexaRole.Owner)]);

    internal static NexaProductAccount CompanyAccount(int workerCount = 2)
    {
        var workers = Enumerable.Range(1, workerCount).Select(i => Worker($"worker-{i}", "workspace-main")).ToArray();
        var seats = Enumerable.Range(1, workerCount).Select(i => Seat($"seat-{i}", "account-company", i == 1 ? NexaRole.Owner : NexaRole.Worker)).ToArray();
        return new NexaProductAccount(
            "account-company",
            NexaProductAccountKind.Company,
            NexaAccountStatus.Trial,
            null,
            new NexaCompanyAccount("company-main", "org-main", ["person-owner", "person-admin"], workers.Select(w => w.WorkerId).ToArray(), NexaAccountStatus.Trial),
            Organization(),
            [Workspace()],
            workers,
            seats);
    }

    internal static NexaOrganization Organization() =>
        new("org-main", "NODAL OS Test Org", NexaAccountStatus.Trial);

    internal static NexaWorkspace Workspace() =>
        new("workspace-main", "org-main", "Main Workspace", NexaAccountStatus.Trial);

    internal static NexaWorker Worker(string workerId, string workspaceId) =>
        new(workerId, workspaceId, $"seat-{workerId}", NexaRole.Worker, new HashSet<NexaAdminCapability> { NexaAdminCapability.ExecuteAllowedFlows }, Active: true);

    internal static NexaSeat Seat(string seatId, string accountId, NexaRole role) =>
        new(seatId, accountId, role, Active: true);
}
