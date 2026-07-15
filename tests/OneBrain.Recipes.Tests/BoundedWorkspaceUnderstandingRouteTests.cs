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
[TestCategory("WorkspaceUnderstanding")]
public sealed class BoundedWorkspaceUnderstandingRouteTests
{
    [TestMethod]
    public async Task DevelopmentLoopbackSurfaceReturnsVerifiedWorkspacePlanWithoutPathOrSecretLeak()
    {
        var root = CreateRoot();
        var fakeSecret = "s" + "k-local-fixture-secret-value-123456789";
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "src"));
            await File.WriteAllTextAsync(Path.Combine(root, "README.md"), "# Local fixture");
            await File.WriteAllTextAsync(
                Path.Combine(root, "src", "Program.cs"),
                $"var api_key = \"{fakeSecret}\";{Environment.NewLine}Console.WriteLine(\"fixture\");");
            await using var app = BuildApp(Environments.Development, () => root);
            await app.StartAsync(TestContext.CancellationTokenSource.Token);
            using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };

            using var jsonResponse = await client.GetAsync(
                BoundedWorkspaceUnderstandingEndpointMapper.JsonRoute,
                TestContext.CancellationTokenSource.Token);
            var json = await jsonResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
            using var htmlResponse = await client.GetAsync(
                BoundedWorkspaceUnderstandingEndpointMapper.HtmlRoute,
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
            Assert.IsTrue(payload.GetProperty("accepted").GetBoolean());
            Assert.IsTrue(payload.GetProperty("localDevOnly").GetBoolean());
            Assert.IsTrue(payload.GetProperty("readOnly").GetBoolean());
            Assert.IsTrue(payload.GetProperty("secretsExcluded").GetBoolean());
            Assert.IsTrue(payload.GetProperty("rootConfigured").GetBoolean());
            Assert.AreEqual("Completed", payload.GetProperty("missionStatus").GetString());
            Assert.AreEqual("GO_BOUNDED_WORKSPACE_CONTEXT_READY_FOR_REVIEWED_PLAN", payload.GetProperty("planDecision").GetString());
            Assert.IsFalse(payload.GetProperty("filesystemMutationAllowed").GetBoolean());
            Assert.IsFalse(payload.GetProperty("networkUsed").GetBoolean());
            Assert.IsFalse(payload.GetProperty("productAuthorityGranted").GetBoolean());
            Assert.AreEqual(64, payload.GetProperty("evidenceDigest").GetString()?.Length);
            Assert.IsTrue(payload.GetProperty("planSteps").GetArrayLength() > 0);

            StringAssert.Contains(html, "data-nodal-os=\"bounded-workspace-understanding\"");
            StringAssert.Contains(html, "data-section-id=\"workspace\"");
            StringAssert.Contains(html, "data-section-id=\"plan\"");
            StringAssert.Contains(html, "data-section-id=\"evidence\"");
            StringAssert.Contains(html, "data-section-id=\"export\"");
            StringAssert.Contains(html, "data-action-id=\"download-verified-handoff\"");
            StringAssert.Contains(html, BoundedWorkspaceHandoffExportEndpointMapper.MarkdownRoute);
            StringAssert.Contains(html, "GO_BOUNDED_WORKSPACE_OPERATOR_SURFACE_READY");
            Assert.IsFalse(json.Contains(root, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(html.Contains(root, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(json.Contains(fakeSecret, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(html.Contains(fakeSecret, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(html.Contains("onclick=", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(html.Contains("http://", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(html.Contains("https://", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task MissingConfiguredRootReturnsConflictWithoutStartingScan()
    {
        await using var app = BuildApp(Environments.Development, () => null);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);
        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };

        using var response = await client.GetAsync(
            BoundedWorkspaceUnderstandingEndpointMapper.JsonRoute,
            TestContext.CancellationTokenSource.Token);
        var json = await response.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        using var document = JsonDocument.Parse(json);
        Assert.IsFalse(document.RootElement.GetProperty("accepted").GetBoolean());
        Assert.IsFalse(document.RootElement.GetProperty("rootConfigured").GetBoolean());
        Assert.AreEqual("BLOCKED_BOUNDED_WORKSPACE_ROOT_NOT_CONFIGURED", document.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual(0, document.RootElement.GetProperty("filesRead").GetInt32());
        Assert.IsFalse(document.RootElement.GetProperty("realFilesystemRead").GetBoolean());
    }

    [TestMethod]
    public void SurfaceRejectsNonLoopbackAndProductionWithoutExplicitFlag()
    {
        var development = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        var production = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });

        Assert.IsFalse(BoundedWorkspaceUnderstandingEndpointMapper.IsRequestAllowed(development.Environment, null));
        Assert.IsFalse(BoundedWorkspaceUnderstandingEndpointMapper.IsRequestAllowed(
            development.Environment,
            IPAddress.Parse("192.0.2.20")));
        Assert.IsTrue(BoundedWorkspaceUnderstandingEndpointMapper.IsRequestAllowed(
            development.Environment,
            IPAddress.Loopback));
        Assert.IsFalse(BoundedWorkspaceUnderstandingEndpointMapper.IsEnabled(production.Environment));
    }

    public TestContext TestContext { get; set; } = null!;

    private static WebApplication BuildApp(string environmentName, Func<string?> rootProvider)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        app.MapBoundedWorkspaceUnderstanding(app.Environment, rootProvider);
        return app;
    }

    private static string ServerAddress(WebApplication app)
    {
        var feature = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        var address = feature?.Addresses.SingleOrDefault();
        Assert.IsFalse(string.IsNullOrWhiteSpace(address), "Server address was not assigned.");
        return address!;
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "nodal-workspace-route-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
