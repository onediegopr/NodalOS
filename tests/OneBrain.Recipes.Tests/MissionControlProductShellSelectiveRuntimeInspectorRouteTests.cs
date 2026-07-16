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
[TestCategory("AiriSelectiveRuntime")]
[TestCategory("MvpVerticalSlice")]
[TestCategory("MissionControlProductShell")]
public sealed class MissionControlProductShellSelectiveRuntimeInspectorRouteTests
{
    [TestMethod]
    public async Task CanonicalRootRendersDarkMissionControlAndKeepsLegacyPilotOffRoot()
    {
        await using var app = BuildIntegratedApp(Environments.Development);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);
        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };

        using var jsonResponse = await client.GetAsync(
            MissionControlProductShellEndpointMapper.JsonRoute,
            TestContext.CancellationTokenSource.Token);
        var json = await jsonResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var htmlResponse = await client.GetAsync(
            MissionControlProductShellEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var html = await htmlResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var legacyResponse = await client.GetAsync(
            MissionControlProductShellEndpointMapper.LegacyPilotRoute,
            TestContext.CancellationTokenSource.Token);
        var legacy = await legacyResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(HttpStatusCode.OK, jsonResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, htmlResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, legacyResponse.StatusCode);
        Assert.AreEqual("no-store", jsonResponse.Headers.CacheControl?.ToString());
        Assert.AreEqual("no-store", htmlResponse.Headers.CacheControl?.ToString());
        StringAssert.Contains(htmlResponse.Headers.GetValues("Content-Security-Policy").Single(), "default-src 'none'");
        Assert.AreEqual("text/html", htmlResponse.Content.Headers.ContentType?.MediaType);
        StringAssert.Contains(htmlResponse.Content.Headers.ContentType?.CharSet ?? string.Empty, "utf-8");

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.AreEqual(MissionControlProductShellEndpointMapper.Decision, root.GetProperty("decision").GetString());
        Assert.IsTrue(root.GetProperty("accepted").GetBoolean());
        Assert.IsTrue(root.GetProperty("localOnly").GetBoolean());
        Assert.IsTrue(root.GetProperty("readOnly").GetBoolean());
        Assert.IsTrue(root.GetProperty("fixtureBacked").GetBoolean());
        Assert.IsTrue(root.GetProperty("secretsExcluded").GetBoolean());
        Assert.IsFalse(root.GetProperty("externalIoUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("networkUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("productAuthorityGranted").GetBoolean());
        Assert.AreEqual("Completed", root.GetProperty("missionStatus").GetString());
        Assert.AreEqual(100, root.GetProperty("progressPercent").GetInt32());
        Assert.AreEqual("fixture-fallback-chat", root.GetProperty("activeModel").GetString());
        Assert.IsTrue(root.GetProperty("timeline").GetArrayLength() >= 8);
        Assert.IsTrue(root.GetProperty("context").GetArrayLength() >= 6);
        Assert.IsTrue(root.GetProperty("evidenceRefs").GetArrayLength() >= 2);
        StringAssert.Contains(root.GetProperty("recentFallback").GetString() ?? string.Empty, "fallback");

        StringAssert.Contains(html, "data-nodal-os=\"mission-control-product-shell\"");
        StringAssert.Contains(html, "data-local-only=\"true\"");
        StringAssert.Contains(html, "data-read-only=\"true\"");
        StringAssert.Contains(html, "data-fixture-backed=\"true\"");
        StringAssert.Contains(html, "data-product-authority=\"false\"");
        StringAssert.Contains(html, "data-section-id=\"topbar\"");
        StringAssert.Contains(html, "data-section-id=\"mission\"");
        StringAssert.Contains(html, "data-section-id=\"timeline\"");
        StringAssert.Contains(html, "data-section-id=\"context\"");
        StringAssert.Contains(html, "data-section-id=\"evidence\"");
        StringAssert.Contains(html, "data-section-id=\"diagnostics\"");
        StringAssert.Contains(html, "#0D1117");
        StringAssert.Contains(html, "#161B22");
        StringAssert.Contains(html, "#1C2128");
        StringAssert.Contains(html, "#4F7CFF");
        StringAssert.Contains(html, "Mission Control");
        StringAssert.Contains(html, "Timeline de misión");
        StringAssert.Contains(html, "Fallback applied automatically");
        StringAssert.Contains(html, "Primary fixture model was rate-limited");
        StringAssert.Contains(html, "Mission-level scope avoids per-step approval prompts");
        StringAssert.Contains(html, "Seleccionar y persistir un workspace local real");
        Assert.IsFalse(html.Contains("Probar ahora", StringComparison.OrdinalIgnoreCase));
        AssertNoExecutableOrExternalSurface(html);

        StringAssert.Contains(legacy, "ONE BRAIN Pilot");
        StringAssert.Contains(legacy, "Probar ahora");
    }

    [TestMethod]
    public async Task ProductShellProjectionReusesCanonicalRuntimeWithoutCreatingAuthority()
    {
        var snapshot = await MissionControlProductShellEndpointMapper.BuildSnapshotAsync(
            TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(MissionControlProductShellEndpointMapper.Decision, snapshot.Decision);
        Assert.IsTrue(snapshot.Accepted);
        Assert.IsTrue(snapshot.LocalOnly);
        Assert.IsTrue(snapshot.ReadOnly);
        Assert.IsTrue(snapshot.FixtureBacked);
        Assert.IsTrue(snapshot.SecretsExcluded);
        Assert.IsFalse(snapshot.ExternalIoUsed);
        Assert.IsFalse(snapshot.NetworkUsed);
        Assert.IsFalse(snapshot.ProductAuthorityGranted);
        Assert.AreEqual("Completed", snapshot.MissionStatus);
        Assert.AreEqual(100, snapshot.ProgressPercent);
        Assert.IsTrue(snapshot.Timeline.Any(value => value.State == "fallback"));
        Assert.IsTrue(snapshot.Timeline.Any(value => value.Title == "Verification and evidence"));
        Assert.IsTrue(snapshot.Context.Any(value => value.Id == "workspace" && value.State == "attention"));
        Assert.IsTrue(snapshot.Context.Any(value => value.Id == "advisor" && value.Value.Contains("non-executor", StringComparison.Ordinal)));
        Assert.IsTrue(snapshot.Diagnostics.Any(value => value == "external-io:false"));
        Assert.IsTrue(snapshot.Diagnostics.Any(value => value == "network:false"));
        Assert.IsFalse(snapshot.Diagnostics.Any(value => value.Contains("API_KEY", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ProductShellRejectsNonLoopbackRequests()
    {
        Assert.IsFalse(MissionControlProductShellEndpointMapper.IsRequestAllowed(null));
        Assert.IsFalse(MissionControlProductShellEndpointMapper.IsRequestAllowed(IPAddress.Parse("192.0.2.10")));
        Assert.IsTrue(MissionControlProductShellEndpointMapper.IsRequestAllowed(IPAddress.Loopback));
    }

    public TestContext TestContext { get; set; } = null!;

    private static WebApplication BuildIntegratedApp(string environmentName)
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

    private static void AssertNoExecutableOrExternalSurface(string html)
    {
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("onclick=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("http://", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("https://", StringComparison.OrdinalIgnoreCase));
    }

    private static string ServerAddress(WebApplication app)
    {
        var feature = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        var address = feature?.Addresses.SingleOrDefault();
        Assert.IsFalse(string.IsNullOrWhiteSpace(address), "Server address was not assigned.");
        return address!;
    }
}
