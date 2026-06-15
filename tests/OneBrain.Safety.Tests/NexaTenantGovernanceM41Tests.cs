using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaTenantGovernanceM41Tests
{
    [TestMethod]
    public void NexaTenantGovernanceBlocksCrossTenantAccess()
    {
        var decision = Evaluate(Tenant("tenant-a", "account-a", "org-a", "workspace-a", "worker-a"), Tenant("tenant-b", "account-b", "org-b", "workspace-b", "worker-b"), NexaRole.Owner);

        Assert.AreEqual(NexaTenantIsolationDecisionKind.Denied, decision.Decision);
    }

    [TestMethod]
    public void NexaTenantGovernanceAllowsOwnerWithinTenant()
    {
        var tenant = Tenant("tenant-a", "account-a", "org-a", "workspace-a", "worker-a");
        var decision = Evaluate(tenant, tenant, NexaRole.Owner);

        Assert.IsTrue(decision.Allowed, decision.Reason);
    }

    [TestMethod]
    public void NexaTenantGovernanceBlocksWorkerOtherWorkspace()
    {
        var actor = Tenant("tenant-a", "account-a", "org-a", "workspace-a", "worker-a");
        var target = Tenant("tenant-a", "account-a", "org-a", "workspace-b", "worker-b");
        var decision = Evaluate(actor, target, NexaRole.Worker);

        Assert.AreEqual(NexaTenantIsolationDecisionKind.Denied, decision.Decision);
    }

    [TestMethod]
    public void NexaTenantGovernanceBlocksSupportSecretAccess()
    {
        var tenant = Tenant("tenant-a", "account-a", "org-a", "workspace-a", "worker-a");
        var decision = new NexaTenantGovernanceEvaluator().Evaluate(new NexaTenantGovernanceRequest(tenant, tenant, NexaRole.Support, NexaAdminAction.SupportInspect, NexaTenantDataClassification.Secret, NexaTenantPolicy.Strict()));

        Assert.AreEqual(NexaTenantIsolationDecisionKind.Denied, decision.Decision);
    }

    [TestMethod]
    public void NexaTenantGovernanceFailsClosedForUnknownTenant()
    {
        var tenant = Tenant("", "account-a", "org-a", "workspace-a", "worker-a");
        var decision = Evaluate(tenant, tenant, NexaRole.Owner);

        Assert.AreEqual(NexaTenantIsolationDecisionKind.FailClosed, decision.Decision);
    }

    [TestMethod]
    public void NexaAuditExportRespectsTenantScope()
    {
        var export = Export(AuditEvents());

        Assert.IsTrue(export.Succeeded, export.Reason);
        Assert.AreEqual(1, export.Manifest!.EventCount);
        Assert.IsFalse(export.Payload.Contains("account-other", StringComparison.Ordinal));
    }

    [TestMethod]
    public void NexaAuditExportViewerRequiresPermission()
    {
        var request = ExportRequest(role: NexaRole.Viewer);
        var tenant = Tenant("tenant-a", "account-company", "org-main", "workspace-main", "worker-main");
        var export = new NexaAuditExportService().Export(request, AuditEvents(), tenant, tenant);

        Assert.AreEqual(NexaTenantIsolationDecisionKind.Denied, export.Decision);
    }

    [TestMethod]
    public void NexaAuditExportDoesNotContainSecrets()
    {
        var export = Export([AuditEvent("admin-audit-one", "account-company", "org-main", "safe reason", "before password=abc", "after summary")]);

        Assert.IsFalse(export.Payload.Contains("password=abc", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(export.ContainsSecretLikeContent());
    }

    [TestMethod]
    public void NexaAuditExportDoesNotContainCookies()
    {
        var export = Export([AuditEvent("admin-audit-one", "account-company", "org-main", "safe reason", "before cookie=session", "after summary")]);

        Assert.IsFalse(export.Payload.Contains("cookie=session", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NexaAuditExportDoesNotContainBodies()
    {
        var export = Export([AuditEvent("admin-audit-one", "account-company", "org-main", "request body redacted", "before summary", "after summary")]);

        Assert.IsTrue(export.Succeeded, export.Reason);
        Assert.IsFalse(export.Payload.Contains("request body:", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NexaAuditExportProducesManifest()
    {
        var export = Export(AuditEvents());

        Assert.IsNotNull(export.Manifest);
        Assert.IsTrue(export.Manifest.Validate().IsValid);
    }

    [TestMethod]
    public void NexaAuditExportManifestIncludesEventCountAndHash()
    {
        var export = Export(AuditEvents());

        Assert.AreEqual(1, export.Manifest!.EventCount);
        Assert.IsFalse(string.IsNullOrWhiteSpace(export.Manifest.Hash));
    }

    [TestMethod]
    public void NexaAuditExportBlocksSensitiveDataWithoutPolicy()
    {
        var request = ExportRequest();
        var tenant = Tenant("tenant-a", "account-company", "org-main", "workspace-main", "worker-main");
        var export = new NexaAuditExportService().Export(request, [AuditEvent("admin-audit-sensitive", "account-company", "org-main", "safe", "before", "after") with { Action = NexaAdminAction.ViewSecret }], tenant, tenant);

        Assert.AreEqual(NexaTenantIsolationDecisionKind.Denied, export.Decision);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenAuditExportLeaksSecrets()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with
        {
            ProductAdminFoundationDefined = true,
            LicensingFoundationDefined = true,
            AdminRuntimeServiceDefined = true,
            TenantGovernanceDefined = true,
            AuditExportDefined = true,
            AuditExportLeaksSecrets = true
        });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "tenant governance and audit export safe");
    }

    private static NexaTenantGovernanceDecision Evaluate(NexaTenant actor, NexaTenant target, NexaRole role) =>
        new NexaTenantGovernanceEvaluator().Evaluate(new NexaTenantGovernanceRequest(actor, target, role, NexaAdminAction.ViewAudit, NexaTenantDataClassification.InternalMetadata, NexaTenantPolicy.Strict()));

    private static NexaAuditExportResult Export(IReadOnlyList<NexaAdminAuditEvent> events)
    {
        var tenant = Tenant("tenant-a", "account-company", "org-main", "workspace-main", "worker-main");
        return new NexaAuditExportService().Export(ExportRequest(), events, tenant, tenant);
    }

    private static NexaAuditExportRequest ExportRequest(NexaRole role = NexaRole.Owner) =>
        new("audit-export-one", "actor-owner", role, new NexaTenantAuditScope("account-company", "org-main", "workspace-main", NexaTenantScope.Organization), DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), NexaAuditExportFormat.Json, NexaAuditExportRedactionPolicy.Strict(), SensitiveDataPolicyApproved: false);

    private static IReadOnlyList<NexaAdminAuditEvent> AuditEvents() =>
    [
        AuditEvent("admin-audit-one", "account-company", "org-main", "safe reason", "before summary", "after summary"),
        AuditEvent("admin-audit-two", "account-other", "org-other", "safe reason", "before summary", "after summary")
    ];

    private static NexaAdminAuditEvent AuditEvent(string eventId, string accountId, string orgId, string reason, string before, string after) =>
        new(eventId, "actor-owner", NexaRole.Owner, accountId, orgId, NexaAdminAction.ViewAudit, NexaAdminDecisionKind.Allowed, reason, DateTimeOffset.UtcNow, before, after, Redacted: true);

    private static NexaTenant Tenant(string tenantId, string accountId, string orgId, string workspaceId, string workerId) =>
        new(tenantId, accountId, orgId, workspaceId, workerId);
}
