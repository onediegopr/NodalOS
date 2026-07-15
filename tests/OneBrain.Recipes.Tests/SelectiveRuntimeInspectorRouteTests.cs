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
public sealed class SelectiveRuntimeInspectorRouteTests
{
    [TestMethod]
    public async Task DevelopmentLoopbackRoutesReturnReadOnlyInspectorWithoutExecutableSurface()
    {
        await using var app = BuildApp(Environments.Development);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);
        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };

        using var jsonResponse = await client.GetAsync(
            SelectiveRuntimeInspectorEndpointMapper.JsonRoute,
            TestContext.CancellationTokenSource.Token);
        var json = await jsonResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var htmlResponse = await client.GetAsync(
            SelectiveRuntimeInspectorEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var html = await htmlResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(HttpStatusCode.OK, jsonResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, htmlResponse.StatusCode);
        Assert.AreEqual("no-store", jsonResponse.Headers.CacheControl?.ToString());
        Assert.AreEqual("no-store", htmlResponse.Headers.CacheControl?.ToString());
        Assert.AreEqual("text/html", htmlResponse.Content.Headers.ContentType?.MediaType);
        StringAssert.Contains(htmlResponse.Content.Headers.ContentType?.CharSet ?? string.Empty, "utf-8");

        using var document = JsonDocument.Parse(json);
        Assert.IsTrue(document.RootElement.GetProperty("localDevOnly").GetBoolean());
        Assert.IsTrue(document.RootElement.GetProperty("readOnly").GetBoolean());
        Assert.IsTrue(document.RootElement.GetProperty("secretsExcluded").GetBoolean());
        Assert.AreEqual("Completed", document.RootElement.GetProperty("missionStatus").GetString());

        StringAssert.Contains(html, "data-nodal-os=\"runtime-inspector\"");
        StringAssert.Contains(html, "data-local-dev-only=\"true\"");
        StringAssert.Contains(html, "data-read-only=\"true\"");
        StringAssert.Contains(html, "data-section-id=\"timeline\"");
        StringAssert.Contains(html, "Primary fixture model was rate-limited");
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("onclick=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("http://", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("https://", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("API_KEY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void InspectorRejectsNonLoopbackRequestsEvenInDevelopment()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });

        Assert.IsFalse(SelectiveRuntimeInspectorEndpointMapper.IsRequestAllowed(
            builder.Environment,
            null));
        Assert.IsFalse(SelectiveRuntimeInspectorEndpointMapper.IsRequestAllowed(
            builder.Environment,
            IPAddress.Parse("192.0.2.10")));
        Assert.IsTrue(SelectiveRuntimeInspectorEndpointMapper.IsRequestAllowed(
            builder.Environment,
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
        app.MapSelectiveRuntimeInspector(app.Environment);
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
