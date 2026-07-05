using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerHttpInProcessRouteResponseSafetyTests
{
    [TestMethod]
    public void ProductLedgerHttpRouteMapping_IsDevelopmentOnlyReadOnlyAndCentralized()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.Pilot", "Program.cs"));
        var mapper = File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.Pilot", "ProductLedgerLocalDevRouteEndpointMapper.cs"));

        StringAssert.Contains(program, "app.MapProductLedgerLocalDevRoutePreview(app.Environment)");
        Assert.IsFalse(program.Contains("app.MapGet(ProductLedgerLocalDevRoutePreview.RouteTemplatePreview", StringComparison.Ordinal));
        StringAssert.Contains(mapper, "environment.IsDevelopment()");
        StringAssert.Contains(mapper, "endpoints.MapGet(");
        StringAssert.Contains(mapper, "endpoints.MapPost(");
        StringAssert.Contains(mapper, "ProductLedgerLocalDevRoutePreview.RouteTemplatePreview");
        StringAssert.Contains(mapper, "LocalApprovalDecisionRoute");
        StringAssert.Contains(mapper, "LocalApprovalDecisionStateRoute");
        StringAssert.Contains(mapper, "LocalApprovalExecutionRoute");
        StringAssert.Contains(mapper, "LocalApprovalExecutionStateRoute");
        StringAssert.Contains(mapper, "Results.Content(result.HtmlSnapshot, result.ContentType)");
        StringAssert.Contains(mapper, "Results.NotFound()");
        StringAssert.Contains(mapper, "LOCAL_ONLY_DEVELOPMENT_ONLY_HTTP_RESPONSE_PREVIEW_NO_EXECUTION");
    }

    [TestMethod]
    public void ProductLedgerHttpRouteMapper_SourceAllowsOnlyLocalApprovalPostAndHasNoExecutionExportNetworkDbOrPilotRun()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.Pilot",
            "ProductLedgerLocalDevRouteEndpointMapper.cs"));
        var forbiddenFragments = new[]
        {
            "Process.Start",
            "HttpClient",
            "WebSocket",
            "DbContext",
            "MigrationBuilder",
            "File.AppendAllText",
            "File.WriteAllText",
            "File.WriteAllBytes",
            "File.Delete",
            ".Append(",
            ".Export(",
            "ProductLedgerInternalCommandHandler",
            "ProductLedgerLocalReportExportService",
            "PilotRecipeExecutor",
            "PilotRecipeExecutionGate.Evaluate(",
            "NODAL_OS_ENABLE_PILOT_RECIPE_EXECUTION"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.OrdinalIgnoreCase), fragment);
        }

        Assert.AreEqual(2, Count(source, "endpoints.MapPost("));
        StringAssert.Contains(source, "LocalApprovalDecisionRoute");
        StringAssert.Contains(source, "LocalApprovalExecutionRoute");
        StringAssert.Contains(source, "/internal/product-ledger/approval/decision");
        StringAssert.Contains(source, "/internal/product-ledger/approval/execute");
        StringAssert.Contains(source, "environment.IsDevelopment()");
        StringAssert.Contains(source, "ProductLedgerLocalApprovalDecisionStateStore");
        StringAssert.Contains(source, "ProductLedgerLocalApprovedActionNoOpExecutor");
        StringAssert.Contains(source, "RequestsProductCommandExecution: body.RequestsProductCommandExecution == true");
        StringAssert.Contains(source, "RequestsBoundedAction: body.RequestsBoundedAction == true");
    }

    [TestMethod]
    public void ProductLedgerLiveReadModelProvider_SourceHasNoArbitraryPathInputScanWriteExportNetworkDbOrPilotRun()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "ProductLedgerOperatorSurfaceReadModelProvider.cs"));
        var forbiddenFragments = new[]
        {
            "HttpContext",
            "Request.Query",
            "QueryString",
            "Directory.GetFiles",
            "Directory.EnumerateFiles",
            "File.AppendAllText",
            "File.WriteAllText",
            "File.WriteAllBytes",
            "File.Delete",
            "Process.Start",
            "HttpClient",
            "WebSocket",
            "MapPost",
            ".Append(",
            ".Export(",
            "ProductLedgerInternalCommandHandler",
            "ProductLedgerLocalReportExportService().Export",
            "PilotRecipeExecutor",
            "PilotRecipeExecutionGate.Evaluate(",
            "NODAL_OS_ENABLE_PILOT_RECIPE_EXECUTION",
            "DbContext",
            "MigrationBuilder"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.OrdinalIgnoreCase), fragment);
        }

        StringAssert.Contains(source, "ReadVerified");
        StringAssert.Contains(source, "LOCAL_ONLY_BOUNDARY_PATH_REDACTED_NO_ARBITRARY_PATH_INPUT");
        StringAssert.Contains(source, "AllowsArbitraryPathInput: false");
        StringAssert.Contains(source, "AllowsFilesystemScan: false");
        StringAssert.Contains(source, "AllowsWrite: false");
        StringAssert.Contains(source, "AllowsExport: false");
        StringAssert.Contains(source, "AllowsCommandExecution: false");
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "OneBrain.Pilot", "Program.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }

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
}
