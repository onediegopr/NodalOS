using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaPrivateLocalApiRoleEnforcementM67Tests
{
    [TestMethod]
    public void NexaPrivateLocalApiBlocksWorkerFromAuditExport() =>
        AssertBlocked(Request("/audit/export", NexaPrivateLocalApiMethod.Post, "worker-test-token", WorkerTenant()), "role Worker does not satisfy minimum role Admin");

    [TestMethod]
    public void NexaPrivateLocalApiBlocksSupportFromAuditExport() =>
        AssertBlocked(Request("/audit/export", NexaPrivateLocalApiMethod.Post, "support-test-token", SupportTenant()), "role Support does not satisfy minimum role Admin");

    [TestMethod]
    public void NexaPrivateLocalApiBlocksWorkerFromOnboardingFreeMock() =>
        AssertBlocked(Request("/onboarding/free/mock", NexaPrivateLocalApiMethod.Post, "worker-test-token", WorkerTenant()), "role Worker does not satisfy minimum role Admin");

    [TestMethod]
    public void NexaPrivateLocalApiBlocksSupportFromOnboardingFreeMock() =>
        AssertBlocked(Request("/onboarding/free/mock", NexaPrivateLocalApiMethod.Post, "support-test-token", SupportTenant()), "role Support does not satisfy minimum role Admin");

    [TestMethod]
    public void NexaPrivateLocalApiBlocksOperatorFromAdminOnlyRoute()
    {
        var service = new NexaPrivateLocalApiService(auth: new NexaPrivateLocalApiAuthService(new Dictionary<string, NexaPrivateLocalApiAuthToken>
        {
            ["operator-test-token"] = new("operator-test-token", NexaRole.Operator, "operator-local", "tenant-local", "workspace-local", "worker-operator")
        }));

        var response = service.Handle(Request("/audit/export", NexaPrivateLocalApiMethod.Post, "operator-test-token", OperatorTenant())).Response;

        Assert.AreEqual(403, response.StatusCode);
        CollectionAssert.Contains(response.ReasonCodes.ToList(), "role Operator does not satisfy minimum role Admin");
    }

    [TestMethod]
    public void NexaPrivateLocalApiAllowsAdminForAdminRoute()
    {
        var response = Handle(Request("/audit/export", NexaPrivateLocalApiMethod.Post, "admin-test-token", AdminTenant())).Response;

        Assert.AreEqual(200, response.StatusCode);
    }

    [TestMethod]
    public void NexaPrivateLocalApiAllowsOwnerForAdminRoute()
    {
        var response = Handle(Request("/audit/export", NexaPrivateLocalApiMethod.Post, "owner-test-token", OwnerTenant())).Response;

        Assert.AreEqual(200, response.StatusCode);
    }

    [TestMethod]
    public void NexaPrivateLocalApiSupportOnlyMetadataRoutes()
    {
        var support = Handle(Request("/support/bundle/mock", NexaPrivateLocalApiMethod.Post, "support-test-token", SupportTenant())).Response;
        var admin = Handle(Request("/admin/dashboard", NexaPrivateLocalApiMethod.Get, "support-test-token", SupportTenant())).Response;

        Assert.AreEqual(200, support.StatusCode);
        Assert.AreEqual(403, admin.StatusCode);
        CollectionAssert.Contains(admin.ReasonCodes.ToList(), "support can access metadata-only routes only");
    }

    [TestMethod]
    public void NexaPrivateLocalApiUnknownRoleDenied()
    {
        var service = new NexaPrivateLocalApiService(auth: new NexaPrivateLocalApiAuthService(new Dictionary<string, NexaPrivateLocalApiAuthToken>
        {
            ["unknown-role-token"] = new("unknown-role-token", NexaRole.Unknown, "unknown-local", "tenant-local", "workspace-local", "worker-unknown")
        }));

        var response = service.Handle(Request("/runtime/status", token: "unknown-role-token", target: UnknownTenant())).Response;

        Assert.AreEqual(403, response.StatusCode);
        CollectionAssert.Contains(response.ReasonCodes.ToList(), "role Unknown does not satisfy minimum role Viewer");
    }

    [TestMethod]
    public void NexaPrivateLocalApiMinimumRoleAppliedBeforeMutation()
    {
        var response = Handle(Request("/audit/export", NexaPrivateLocalApiMethod.Post, "worker-test-token", WorkerTenant())).Response;
        var reasons = response.ReasonCodes.ToList();

        Assert.AreEqual(403, response.StatusCode);
        Assert.IsTrue(reasons.IndexOf("role Worker does not satisfy minimum role Admin") < reasons.IndexOf("Worker mutation blocked"));
    }

    private static void AssertBlocked(NexaPrivateLocalApiRequest request, string expectedReason)
    {
        var response = Handle(request).Response;

        Assert.AreEqual(403, response.StatusCode);
        CollectionAssert.Contains(response.ReasonCodes.ToList(), expectedReason);
    }

    private static (NexaPrivateLocalApiResponse Response, NexaPrivateLocalApiAuditEvent Audit) Handle(NexaPrivateLocalApiRequest request) =>
        new NexaPrivateLocalApiService().Handle(request);

    private static NexaPrivateLocalApiRequest Request(
        string path,
        NexaPrivateLocalApiMethod method = NexaPrivateLocalApiMethod.Get,
        string token = "owner-test-token",
        NexaPublicApiTenantContext? target = null) =>
        new(method, path, token, target ?? OwnerTenant(), LicenseAllowsFeature: true, RequestContainsSecret: false, RequestContainsCookie: false, RequestContainsBody: false);

    private static NexaPublicApiTenantContext OwnerTenant() => new("tenant-local", "account-local", "org-local", "workspace-local", "worker-owner");
    private static NexaPublicApiTenantContext AdminTenant() => OwnerTenant() with { WorkerId = "worker-admin" };
    private static NexaPublicApiTenantContext WorkerTenant() => OwnerTenant() with { WorkerId = "worker-local" };
    private static NexaPublicApiTenantContext SupportTenant() => OwnerTenant() with { WorkerId = "worker-support" };
    private static NexaPublicApiTenantContext OperatorTenant() => OwnerTenant() with { WorkerId = "worker-operator" };
    private static NexaPublicApiTenantContext UnknownTenant() => OwnerTenant() with { WorkerId = "worker-unknown" };
}
