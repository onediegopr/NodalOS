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
        StringAssert.Contains(mapper, "endpoints.MapGet(ProductLedgerLocalDevRoutePreview.RouteTemplatePreview");
        StringAssert.Contains(mapper, "Results.Content(result.HtmlSnapshot, result.ContentType)");
        StringAssert.Contains(mapper, "Results.NotFound()");
        StringAssert.Contains(mapper, "LOCAL_ONLY_DEVELOPMENT_ONLY_HTTP_RESPONSE_PREVIEW_NO_EXECUTION");
    }

    [TestMethod]
    public void ProductLedgerHttpRouteMapper_SourceHasNoWriteExportCommandHandlerNetworkDbPilotRunOrPost()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.Pilot",
            "ProductLedgerLocalDevRouteEndpointMapper.cs"));
        var forbiddenFragments = new[]
        {
            "MapPost",
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
}
