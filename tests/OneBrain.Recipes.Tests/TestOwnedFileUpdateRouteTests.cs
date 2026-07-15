using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Core.Runtime;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("MvpVerticalSlice")]
[TestCategory("ControlledFileOperation")]
public sealed class TestOwnedFileUpdateRouteTests
{
    [TestMethod]
    public async Task DevelopmentLoopbackRoutesReturnVerifiedExactHashUpdateWithoutExecutableSurfaceOrResidue()
    {
        EnsureFixtureBaseClean();
        await using var app = BuildApp(Environments.Development);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);
        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };

        using var jsonResponse = await client.GetAsync(
            TestOwnedFileUpdateEndpointMapper.JsonRoute,
            TestContext.CancellationTokenSource.Token);
        var json = await jsonResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var htmlResponse = await client.GetAsync(
            TestOwnedFileUpdateEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var html = await htmlResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(HttpStatusCode.OK, jsonResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, htmlResponse.StatusCode);
        Assert.AreEqual("no-store", jsonResponse.Headers.CacheControl?.ToString());
        Assert.AreEqual("no-store", htmlResponse.Headers.CacheControl?.ToString());
        Assert.AreEqual("text/html", htmlResponse.Content.Headers.ContentType?.MediaType);
        StringAssert.Contains(htmlResponse.Content.Headers.ContentType?.CharSet ?? string.Empty, "utf-8");
        StringAssert.Contains(htmlResponse.Headers.GetValues("Content-Security-Policy").Single(), "default-src 'none'");

        using var document = JsonDocument.Parse(json);
        var payload = document.RootElement;
        Assert.IsTrue(payload.GetProperty("localDevOnly").GetBoolean());
        Assert.IsTrue(payload.GetProperty("readOnlySurface").GetBoolean());
        Assert.IsTrue(payload.GetProperty("secretsExcluded").GetBoolean());
        Assert.AreEqual("Completed", payload.GetProperty("missionStatus").GetString());
        Assert.AreEqual("Completed", payload.GetProperty("registryState").GetString());
        Assert.AreEqual("Approve", payload.GetProperty("approvalStatus").GetString());
        Assert.AreEqual("UpdatedAndVerified", payload.GetProperty("fileUpdateState").GetString());
        Assert.IsTrue(payload.GetProperty("fileUpdateVerified").GetBoolean());
        Assert.IsTrue(payload.GetProperty("preconditionMatched").GetBoolean());
        Assert.IsTrue(payload.GetProperty("snapshotCreated").GetBoolean());
        Assert.IsTrue(payload.GetProperty("atomicReplaceUsed").GetBoolean());
        Assert.IsTrue(payload.GetProperty("rollbackPlanCreated").GetBoolean());
        Assert.IsTrue(payload.GetProperty("fixtureCleanupRemovedRollbackSnapshot").GetBoolean());
        Assert.IsTrue(payload.GetProperty("testOwnedFilesystemTouched").GetBoolean());
        Assert.IsTrue(payload.GetProperty("testOwnedFixtureCleaned").GetBoolean());
        Assert.IsFalse(payload.GetProperty("userWorkspaceFilesystemTouched").GetBoolean());
        Assert.IsFalse(payload.GetProperty("networkUsed").GetBoolean());
        Assert.IsFalse(payload.GetProperty("productAuthorityGranted").GetBoolean());
        Assert.AreEqual(64, payload.GetProperty("originalSha256").GetString()?.Length);
        Assert.AreEqual(64, payload.GetProperty("updatedSha256").GetString()?.Length);
        Assert.AreNotEqual(
            payload.GetProperty("originalSha256").GetString(),
            payload.GetProperty("updatedSha256").GetString());

        StringAssert.Contains(html, "data-nodal-os=\"test-owned-file-update\"");
        StringAssert.Contains(html, "data-section-id=\"file-action\"");
        StringAssert.Contains(html, "data-section-id=\"verification\"");
        StringAssert.Contains(html, "data-section-id=\"rollback\"");
        StringAssert.Contains(html, "data-section-id=\"timeline\"");
        StringAssert.Contains(html, "data-section-id=\"evidence\"");
        StringAssert.Contains(html, "UpdatedAndVerified");
        StringAssert.Contains(html, "exact-hash update");
        StringAssert.Contains(html, "General patch");
        StringAssert.Contains(html, "disabled");
        Assert.IsFalse(json.Contains(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(Environment.UserName) && Environment.UserName.Length > 2)
        {
            Assert.IsFalse(json.Contains(Environment.UserName, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(html.Contains(Environment.UserName, StringComparison.OrdinalIgnoreCase));
        }
        Assert.IsFalse(json.Contains(".nodal-restore", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains(".nodal-restore", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("onclick=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("http://", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("https://", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(0, FixtureRuns().Count);
    }

    [TestMethod]
    public void UpdateFixtureRejectsNonLoopbackAndProduction()
    {
        var development = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        var production = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });

        Assert.IsFalse(SelectiveRuntimeInspectorEndpointMapper.IsRequestAllowed(
            development.Environment,
            null));
        Assert.IsFalse(SelectiveRuntimeInspectorEndpointMapper.IsRequestAllowed(
            development.Environment,
            IPAddress.Parse("192.0.2.60")));
        Assert.IsTrue(SelectiveRuntimeInspectorEndpointMapper.IsRequestAllowed(
            development.Environment,
            IPAddress.Loopback));
        Assert.IsFalse(SelectiveRuntimeInspectorEndpointMapper.IsRequestAllowed(
            production.Environment,
            IPAddress.Loopback));
    }

    public TestContext TestContext { get; set; } = null!;

    private static WebApplication BuildApp(string environmentName)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        app.MapTestOwnedFileUpdateFixture(app.Environment);
        return app;
    }

    private static string ServerAddress(WebApplication app)
    {
        var feature = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        var address = feature?.Addresses.SingleOrDefault();
        Assert.IsFalse(string.IsNullOrWhiteSpace(address), "Server address was not assigned.");
        return address!;
    }

    private static IReadOnlyList<string> FixtureRuns()
    {
        var root = NodalOsTestOwnedFileCreateAction.AllowedBaseRoot;
        if (!Directory.Exists(root))
            return Array.Empty<string>();
        return Directory.GetDirectories(root, "run-*", SearchOption.TopDirectoryOnly);
    }

    private static void EnsureFixtureBaseClean()
    {
        foreach (var run in FixtureRuns())
        {
            var fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(run))))
                .ToLowerInvariant();
            new NodalOsTestOwnedFileCreateAction().CleanupOwnedRoot(run, fingerprint);
        }
        Assert.AreEqual(0, FixtureRuns().Count);
    }
}
