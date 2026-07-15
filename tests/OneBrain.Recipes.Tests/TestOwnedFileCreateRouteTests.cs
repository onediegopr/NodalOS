using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("MvpVerticalSlice")]
public sealed class TestOwnedFileCreateRouteTests
{
    [TestMethod]
    public async Task DevelopmentLoopbackRoutesExposeVerifiedCreateOnlyMissionWithoutRawPaths()
    {
        await using var app = BuildApp();
        await app.StartAsync(TestContext.CancellationTokenSource.Token);
        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };

        using var jsonResponse = await client.GetAsync(
            TestOwnedFileCreateEndpointMapper.JsonRoute,
            TestContext.CancellationTokenSource.Token);
        var json = await jsonResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var htmlResponse = await client.GetAsync(
            TestOwnedFileCreateEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var html = await htmlResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(HttpStatusCode.OK, jsonResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, htmlResponse.StatusCode);
        Assert.AreEqual("no-store", jsonResponse.Headers.CacheControl?.ToString());
        Assert.AreEqual("no-store", htmlResponse.Headers.CacheControl?.ToString());
        Assert.AreEqual("text/html", htmlResponse.Content.Headers.ContentType?.MediaType);
        StringAssert.Contains(htmlResponse.Content.Headers.ContentType?.CharSet ?? string.Empty, "utf-8");

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.IsTrue(root.GetProperty("localDevOnly").GetBoolean());
        Assert.IsTrue(root.GetProperty("readOnlySurface").GetBoolean());
        Assert.IsTrue(root.GetProperty("secretsExcluded").GetBoolean());
        Assert.AreEqual("Completed", root.GetProperty("missionStatus").GetString());
        Assert.AreEqual("Completed", root.GetProperty("registryState").GetString());
        Assert.AreEqual("Approve", root.GetProperty("approvalStatus").GetString());
        Assert.AreEqual("CreatedAndVerified", root.GetProperty("fileCreateState").GetString());
        Assert.IsTrue(root.GetProperty("fileCreateVerified").GetBoolean());
        Assert.AreEqual("output/verified-handoff.md", root.GetProperty("relativePath").GetString());
        Assert.AreEqual(64, root.GetProperty("contentSha256").GetString()?.Length);
        Assert.IsTrue(root.GetProperty("missionAuthorizationReused").GetBoolean());
        Assert.IsFalse(root.GetProperty("additionalStepApprovalRequested").GetBoolean());
        Assert.IsTrue(root.GetProperty("testOwnedFilesystemTouched").GetBoolean());
        Assert.IsTrue(root.GetProperty("testOwnedFixtureCleaned").GetBoolean());
        Assert.IsFalse(root.GetProperty("userWorkspaceFilesystemTouched").GetBoolean());
        Assert.IsFalse(root.GetProperty("networkUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("productAuthorityGranted").GetBoolean());

        StringAssert.Contains(html, "data-nodal-os=\"test-owned-file-create\"");
        StringAssert.Contains(html, "data-local-dev-only=\"true\"");
        StringAssert.Contains(html, "data-read-only-surface=\"true\"");
        StringAssert.Contains(html, "data-product-authority=\"false\"");
        StringAssert.Contains(html, "data-section-id=\"file-action\"");
        StringAssert.Contains(html, "data-section-id=\"verification\"");
        StringAssert.Contains(html, "data-section-id=\"cleanup\"");
        StringAssert.Contains(html, "Mission-level approval required");
        StringAssert.Contains(html, "Create-only test-owned file completed and verified");
        StringAssert.Contains(html, "Mission completed after every required step was verified");
        StringAssert.Contains(html, "CreatedAndVerified");
        StringAssert.Contains(html, "output/verified-handoff.md");
        Assert.IsFalse(json.Contains(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains(Environment.UserName, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains(Environment.UserName, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("onclick=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("http://", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("https://", StringComparison.OrdinalIgnoreCase));
    }

    public TestContext TestContext { get; set; } = null!;

    private static WebApplication BuildApp()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        app.MapTestOwnedFileCreateFixture(app.Environment);
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
