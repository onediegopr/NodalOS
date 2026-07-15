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
    public async Task DevelopmentLoopbackRoutesReturnControlledVerifiedSliceWithoutExecutableSurface()
    {
        await using var app = BuildInspectorApp(Environments.Development);
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
        var root = document.RootElement;
        Assert.IsTrue(root.GetProperty("localDevOnly").GetBoolean());
        Assert.IsTrue(root.GetProperty("readOnly").GetBoolean());
        Assert.IsTrue(root.GetProperty("secretsExcluded").GetBoolean());
        Assert.AreEqual("Completed", root.GetProperty("missionStatus").GetString());
        Assert.AreEqual("Completed", root.GetProperty("registryState").GetString());
        Assert.AreEqual("Approve", root.GetProperty("approvalStatus").GetString());
        Assert.AreEqual("Succeeded", root.GetProperty("controlledActionState").GetString());
        Assert.IsTrue(root.GetProperty("controlledActionVerified").GetBoolean());
        Assert.IsTrue(root.GetProperty("missionAuthorizationReused").GetBoolean());
        Assert.IsFalse(root.GetProperty("additionalStepApprovalRequested").GetBoolean());
        Assert.IsFalse(root.GetProperty("realFilesystemTouched").GetBoolean());
        Assert.IsFalse(root.GetProperty("networkUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("productAuthorityGranted").GetBoolean());
        Assert.IsTrue(root.GetProperty("timelineCount").GetInt32() >= 4);
        Assert.IsTrue(root.GetProperty("evidenceCount").GetInt32() >= 1);
        StringAssert.Contains(root.GetProperty("handoffPackId").GetString() ?? string.Empty, "verified-handoff-");
        Assert.AreEqual(
            "BLOCKED_EXTERNAL_CLOAKBROWSER_BINARY",
            root.GetProperty("runtime").GetProperty("browser").GetProperty("state").GetString());

        StringAssert.Contains(html, "data-nodal-os=\"runtime-inspector\"");
        StringAssert.Contains(html, "data-local-dev-only=\"true\"");
        StringAssert.Contains(html, "data-read-only=\"true\"");
        StringAssert.Contains(html, "data-product-authority=\"false\"");
        StringAssert.Contains(html, "data-section-id=\"approval\"");
        StringAssert.Contains(html, "data-section-id=\"controlled-action\"");
        StringAssert.Contains(html, "data-section-id=\"handoff\"");
        StringAssert.Contains(html, "data-section-id=\"timeline\"");
        StringAssert.Contains(html, "Mission-level approval required");
        StringAssert.Contains(html, "Mission-level approval granted");
        StringAssert.Contains(html, "Primary fixture model was rate-limited");
        StringAssert.Contains(html, "No per-step prompt");
        StringAssert.Contains(html, "SafeExecutionFsm");
        StringAssert.Contains(html, "verified report");
        AssertNoExecutableOrExternalSurface(html);
        Assert.IsFalse(json.Contains("API_KEY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task IntegratedPilotRoutesExposeVerifiedTeachNodalReviewWithoutLiveCaptureAuthority()
    {
        await using var app = BuildIntegratedApp(Environments.Development);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);
        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };

        using var jsonResponse = await client.GetAsync(
            TeachNodalLocalDevSurface.JsonRoute,
            TestContext.CancellationTokenSource.Token);
        var json = await jsonResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var htmlResponse = await client.GetAsync(
            TeachNodalLocalDevSurface.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var html = await htmlResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(HttpStatusCode.OK, jsonResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, htmlResponse.StatusCode);
        Assert.AreEqual("no-store", jsonResponse.Headers.CacheControl?.ToString());
        Assert.AreEqual("no-store", htmlResponse.Headers.CacheControl?.ToString());
        StringAssert.Contains(htmlResponse.Headers.GetValues("Content-Security-Policy").Single(), "default-src 'none'");
        Assert.AreEqual("text/html", htmlResponse.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.AreEqual("GO_TEACH_NODAL_LOCAL_DEV_SURFACE_READY", root.GetProperty("decision").GetString());
        Assert.IsTrue(root.GetProperty("accepted").GetBoolean());
        Assert.IsTrue(root.GetProperty("localDevOnly").GetBoolean());
        Assert.IsTrue(root.GetProperty("readOnly").GetBoolean());
        Assert.IsTrue(root.GetProperty("fixtureOnly").GetBoolean());
        Assert.IsTrue(root.GetProperty("secretsExcluded").GetBoolean());
        StringAssert.StartsWith(root.GetProperty("compilationDecision").GetString() ?? string.Empty, "CompiledVerifiedSkill");
        Assert.IsFalse(root.GetProperty("recipeLiveRuntimeEnabled").GetBoolean());
        Assert.AreEqual("Verified", root.GetProperty("skillState").GetString());
        Assert.AreEqual(2, root.GetProperty("transitionCount").GetInt32());
        Assert.AreEqual(64, root.GetProperty("skillFingerprint").GetString()?.Length);
        Assert.AreEqual("stable", root.GetProperty("processMemoryStatus").GetString());
        Assert.AreEqual(2, root.GetProperty("steps").GetArrayLength());
        foreach (var step in root.GetProperty("steps").EnumerateArray())
        {
            Assert.IsTrue(step.GetProperty("verified").GetBoolean());
            Assert.AreEqual(64, step.GetProperty("beforeFingerprint").GetString()?.Length);
            Assert.AreEqual(64, step.GetProperty("afterFingerprint").GetString()?.Length);
            Assert.AreNotEqual(
                step.GetProperty("beforeFingerprint").GetString(),
                step.GetProperty("afterFingerprint").GetString());
        }
        Assert.IsTrue(root.GetProperty("promptInjectionObserved").GetBoolean());
        Assert.IsFalse(root.GetProperty("promptInjectionModifiedGoal").GetBoolean());
        Assert.IsFalse(root.GetProperty("promptInjectionExpandedScope").GetBoolean());
        Assert.IsFalse(root.GetProperty("promptInjectionPublishedExternally").GetBoolean());
        Assert.AreEqual(TeachNodalLocalDevSurface.DisabledActionId, root.GetProperty("disabledActionId").GetString());
        Assert.AreEqual("DISABLED_LOCAL_DEV_FIXTURE_ONLY", root.GetProperty("disabledActionState").GetString());
        Assert.AreEqual(TeachNodalLocalDevSurface.RequiredOperatorSignal, root.GetProperty("requiredOperatorSignal").GetString());
        Assert.IsFalse(root.GetProperty("liveRecorderUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("mouseOrKeyboardHooksUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("rawScreenshotStored").GetBoolean());
        Assert.IsFalse(root.GetProperty("rawDomStored").GetBoolean());
        Assert.IsFalse(root.GetProperty("networkUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("productAuthorityGranted").GetBoolean());

        StringAssert.Contains(html, "data-nodal-os=\"teach-nodal\"");
        StringAssert.Contains(html, "data-local-dev-only=\"true\"");
        StringAssert.Contains(html, "data-read-only=\"true\"");
        StringAssert.Contains(html, "data-fixture-only=\"true\"");
        StringAssert.Contains(html, "data-product-authority=\"false\"");
        StringAssert.Contains(html, "data-section-id=\"teaching-timeline\"");
        StringAssert.Contains(html, "data-section-id=\"compiled-outputs\"");
        StringAssert.Contains(html, "data-section-id=\"trusted-control\"");
        StringAssert.Contains(html, "data-section-id=\"limits\"");
        StringAssert.Contains(html, "data-section-id=\"disabled-action\"");
        StringAssert.Contains(html, $"data-action-id=\"{TeachNodalLocalDevSurface.DisabledActionId}\"");
        StringAssert.Contains(html, "Observed prompt-injection text remained evidence");
        StringAssert.Contains(html, TeachNodalLocalDevSurface.RequiredOperatorSignal);
        StringAssert.Contains(html, "Live capture remains closed");
        AssertNoExecutableOrExternalSurface(html);
        Assert.IsFalse(json.Contains("sk-", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("sk-", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void TeachNodalReviewSnapshotIsDeterministicAndCannotOpenLiveAuthority()
    {
        var first = TeachNodalLocalDevSurface.BuildSnapshot();
        var second = TeachNodalLocalDevSurface.BuildSnapshot();

        Assert.IsTrue(first.Accepted);
        Assert.AreEqual(first.Decision, second.Decision);
        Assert.AreEqual(first.SkillFingerprint, second.SkillFingerprint);
        Assert.AreEqual(first.TransitionCount, second.TransitionCount);
        Assert.AreEqual(first.RecipeId, second.RecipeId);
        Assert.AreEqual(first.ProcessMemoryId, second.ProcessMemoryId);
        CollectionAssert.AreEqual(
            first.Steps.Select(value => value.BeforeFingerprint).ToArray(),
            second.Steps.Select(value => value.BeforeFingerprint).ToArray());
        CollectionAssert.AreEqual(
            first.Steps.Select(value => value.AfterFingerprint).ToArray(),
            second.Steps.Select(value => value.AfterFingerprint).ToArray());
        Assert.AreEqual("DISABLED_LOCAL_DEV_FIXTURE_ONLY", first.DisabledActionState);
        Assert.IsFalse(first.LiveRecorderUsed);
        Assert.IsFalse(first.MouseOrKeyboardHooksUsed);
        Assert.IsFalse(first.NetworkUsed);
        Assert.IsFalse(first.ProductAuthorityGranted);
    }

    [TestMethod]
    public void InspectorAndTeachNodalRejectNonLoopbackRequestsEvenInDevelopment()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });

        Assert.IsFalse(SelectiveRuntimeInspectorEndpointMapper.IsRequestAllowed(builder.Environment, null));
        Assert.IsFalse(SelectiveRuntimeInspectorEndpointMapper.IsRequestAllowed(builder.Environment, IPAddress.Parse("192.0.2.10")));
        Assert.IsTrue(SelectiveRuntimeInspectorEndpointMapper.IsRequestAllowed(builder.Environment, IPAddress.Loopback));
        Assert.IsFalse(TeachNodalLocalDevSurface.IsRequestAllowed(builder.Environment, null));
        Assert.IsFalse(TeachNodalLocalDevSurface.IsRequestAllowed(builder.Environment, IPAddress.Parse("192.0.2.10")));
        Assert.IsTrue(TeachNodalLocalDevSurface.IsRequestAllowed(builder.Environment, IPAddress.Loopback));
    }

    public TestContext TestContext { get; set; } = null!;

    private static WebApplication BuildInspectorApp(string environmentName)
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
