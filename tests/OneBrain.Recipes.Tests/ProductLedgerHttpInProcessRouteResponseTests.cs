using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerHttpInProcessRouteResponseTests
{
    [TestMethod]
    public async Task ProductLedgerRouteResponse_DevelopmentHostReturnsCanonicalLocalOnlyHtml()
    {
        await using var app = BuildLocalOnlyApp(Environments.Development);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);

        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
        using var response = await client.GetAsync(
            ProductLedgerLocalDevRoutePreview.RouteTemplatePreview,
            TestContext.CancellationTokenSource.Token);
        var html = await response.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/html", response.Content.Headers.ContentType?.MediaType);
        StringAssert.Contains(response.Content.Headers.ContentType?.CharSet ?? string.Empty, "utf-8");
        StringAssert.Contains(html, "data-testid=\"local-dev-route-preview\"");
        StringAssert.Contains(html, "data-testid=\"canonical-surface-model\"");
        StringAssert.Contains(html, "data-testid=\"product-ledger-approval-preview\"");
        StringAssert.Contains(html, "data-route-path=\"/internal/product-ledger/operator-surface\"");
        StringAssert.Contains(html, "read-only");
        StringAssert.Contains(html, "preview-only");
        StringAssert.Contains(html, "no product command execution");
        StringAssert.Contains(html, "no write/export");
        StringAssert.Contains(html, "no release/commercial");
        Assert.IsFalse(html.Contains("data-executable=\"true\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("onclick=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("formaction=", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task ProductLedgerRouteResponse_NonDevelopmentHostDoesNotMapRoute()
    {
        await using var app = BuildLocalOnlyApp(Environments.Production);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);

        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
        using var response = await client.GetAsync(
            ProductLedgerLocalDevRoutePreview.RouteTemplatePreview,
            TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    public TestContext TestContext { get; set; } = null!;

    private static WebApplication BuildLocalOnlyApp(string environmentName)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName
        });

        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        app.MapProductLedgerLocalDevRoutePreview(app.Environment);
        return app;
    }

    private static string ServerAddress(WebApplication app)
    {
        var feature = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        var address = feature?.Addresses.SingleOrDefault();
        Assert.IsFalse(string.IsNullOrWhiteSpace(address), "Server address was not assigned.");
        return address!;
    }
}
