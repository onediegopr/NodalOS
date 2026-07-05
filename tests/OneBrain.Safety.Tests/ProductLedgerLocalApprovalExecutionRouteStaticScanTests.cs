using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalApprovalExecutionRouteStaticScanTests
{
    [TestMethod]
    public void ProductLedgerApprovalExecutionRoute_StaticScanIsPathSpecificAndIgnoresUnrelatedPilotMapPostRoutes()
    {
        var program = ReadRepoFile("src", "OneBrain.Pilot", "Program.cs");
        var mapper = ReadRepoFile("src", "OneBrain.Pilot", "ProductLedgerLocalDevRouteEndpointMapper.cs");

        StringAssert.Contains(program, "MapPost");
        StringAssert.Contains(mapper, "environment.IsDevelopment()");
        StringAssert.Contains(mapper, "endpoints.MapGet(");
        StringAssert.Contains(mapper, "endpoints.MapPost(");
        StringAssert.Contains(mapper, "ProductLedgerLocalDevRoutePreview.RouteTemplatePreview");
        Assert.AreEqual(2, Count(mapper, "endpoints.MapPost("), "Product Ledger mapper may expose only local approval decision and no-op execution POST routes.");
        StringAssert.Contains(mapper, "LocalApprovalDecisionRoute");
        StringAssert.Contains(mapper, "LocalApprovalExecutionRoute");
        StringAssert.Contains(mapper, "/internal/product-ledger/approval/decision");
        StringAssert.Contains(mapper, "/internal/product-ledger/approval/execute");
        StringAssert.Contains(mapper, "ProductLedgerLocalApprovalDecisionStateStore");
        StringAssert.Contains(mapper, "ProductLedgerLocalApprovedActionNoOpExecutor");
        Assert.IsFalse(mapper.Contains("Request.Query", StringComparison.Ordinal), "Product Ledger mapper must not accept arbitrary path query input.");
        Assert.IsFalse(mapper.Contains("QueryString", StringComparison.Ordinal), "Product Ledger mapper must not inspect query strings.");
    }

    [TestMethod]
    public void ProductLedgerApprovalExecutionRoute_StaticScanHasNoWriteExportPublicUiProductHandlerDbCloudOrReleaseInPath()
    {
        var approvalPathSource = string.Join(
            Environment.NewLine,
            ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalDevRoutePreview.cs"),
            ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerOperatorSurfaceModel.cs"),
            ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalApprovalExecutionCandidate.cs"));
        var forbidden = new[]
        {
            "MapPost",
            "Request.Query",
            "QueryString",
            "HttpContext",
            "File.WriteAllText",
            "File.AppendAllText",
            "File.WriteAllBytes",
            "File.Delete",
            "Directory.GetFiles",
            "Directory.EnumerateFiles",
            "Process.Start",
            "HttpClient",
            "WebSocket",
            "DbContext",
            "MigrationBuilder",
            "KmsClient",
            "WormStore",
            "ProductLedgerLocalReportExportService().Export",
            ".Export(",
            ".Append(",
            "PilotRecipeExecutor",
            "PilotRecipeExecutionGate.Evaluate(",
            "NODAL_OS_ENABLE_PILOT_RECIPE_EXECUTION",
            "PublicUiActionAvailable: true",
            "ProductCommandHandlerAvailable: true",
            "ProductiveServiceRegistrationAvailable: true",
            "ProviderCloudNetworkAvailable: true",
            "DbMigrationAvailable: true",
            "KmsWormExternalTrustAvailable: true",
            "BrowserCdpWcuOcrRecipesLiveAvailable: true",
            "ReleaseCommercialReady: true",
            "PhysicalExportCreated: true",
            "FileWritePerformed: true"
        };

        foreach (var fragment in forbidden)
        {
            Assert.IsFalse(approvalPathSource.Contains(fragment, StringComparison.Ordinal), fragment);
        }

        var mapper = ReadRepoFile("src", "OneBrain.Pilot", "ProductLedgerLocalDevRouteEndpointMapper.cs");
        var mapperForbidden = new[]
        {
            "Process.Start",
            "HttpClient",
            "WebSocket",
            "DbContext",
            "MigrationBuilder",
            "KmsClient",
            "WormStore",
            "ProductLedgerInternalCommandHandler",
            "ProductLedgerLocalReportExportService().Export",
            ".Export(",
            ".Append(",
            "PilotRecipeExecutor",
            "PilotRecipeExecutionGate.Evaluate(",
            "NODAL_OS_ENABLE_PILOT_RECIPE_EXECUTION"
        };

        foreach (var fragment in mapperForbidden)
        {
            Assert.IsFalse(mapper.Contains(fragment, StringComparison.Ordinal), fragment);
        }
    }

    [TestMethod]
    public void ProductLedgerApprovalExecutionRoute_StaticScanRequiresDisabledEvidenceControlAndNoExecutableDom()
    {
        var routePreview = ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalDevRoutePreview.cs");

        StringAssert.Contains(routePreview, "product-ledger-approval-execution-candidate-preview");
        StringAssert.Contains(routePreview, "product-ledger-approval-execution-candidate-control");
        StringAssert.Contains(routePreview, "data-executable=\\\"false\\\"");
        StringAssert.Contains(routePreview, "disabled aria-disabled=\\\"true\\\"");
        Assert.IsFalse(routePreview.Contains("data-executable=\\\"true\\\"", StringComparison.Ordinal));
        Assert.IsFalse(routePreview.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(routePreview.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(routePreview.Contains("onclick=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(routePreview.Contains("formaction=", StringComparison.OrdinalIgnoreCase));
    }

    private static string ReadRepoFile(params string[] segments) =>
        File.ReadAllText(Path.Combine(new[] { RepoRoot() }.Concat(segments).ToArray()));

    private static int Count(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(
                directory.FullName,
                "src",
                "OneBrain.Pilot",
                "ProductLedgerLocalDevRouteEndpointMapper.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
