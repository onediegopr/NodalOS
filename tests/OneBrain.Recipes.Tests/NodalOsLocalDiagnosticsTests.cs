using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
[DoNotParallelize]
[TestCategory("LocalDiagnostics")]
[TestCategory("MvpVerticalSlice")]
[TestCategory("AiriSelectiveRuntime")]
public sealed class NodalOsLocalDiagnosticsTests
{
    [TestMethod]
    public void DiagnosticsWriteNothingUntilOptInAndNeverPersistExceptionContent()
    {
        using var fixture = DiagnosticsFixture.Create();
        var diagnostics = fixture.Diagnostics;

        diagnostics.RecordStartup(packaged: true);
        Assert.IsFalse(diagnostics.Enabled);
        Assert.IsFalse(File.Exists(fixture.EventsPath));

        diagnostics.Enable(packaged: true);
        diagnostics.RecordStartup(packaged: true);
        diagnostics.RecordRequestError(
            new InvalidOperationException("private-payload-marker should never be persisted"),
            packaged: true);

        Assert.IsTrue(diagnostics.Enabled);
        Assert.IsTrue(File.Exists(fixture.OptInPath));
        var events = File.ReadAllText(fixture.EventsPath);
        StringAssert.Contains(events, "\"kind\":\"diagnostics\"");
        StringAssert.Contains(events, "\"kind\":\"startup\"");
        StringAssert.Contains(events, "\"kind\":\"request-error\"");
        StringAssert.Contains(events, "InvalidOperationException");
        Assert.IsFalse(events.Contains("private-payload-marker", StringComparison.Ordinal));
        Assert.IsFalse(events.Contains("message", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(events.Contains("stack", StringComparison.OrdinalIgnoreCase));

        diagnostics.Disable(packaged: true);
        var lengthAfterDisable = new FileInfo(fixture.EventsPath).Length;
        diagnostics.RecordRequestError(new Exception("ignored-after-opt-out"), packaged: true);

        Assert.IsFalse(diagnostics.Enabled);
        Assert.IsFalse(File.Exists(fixture.OptInPath));
        Assert.AreEqual(lengthAfterDisable, new FileInfo(fixture.EventsPath).Length);
    }

    [TestMethod]
    public void LocalEventRetentionStaysInsideThePublishedBounds()
    {
        using var fixture = DiagnosticsFixture.Create();
        fixture.Diagnostics.Enable(packaged: true);

        for (var index = 0; index < 250; index++)
            fixture.Diagnostics.RecordRequestError(new InvalidOperationException($"ignored-{index}"), packaged: true);

        Assert.AreEqual(200, File.ReadLines(fixture.EventsPath).Count());
        Assert.IsLessThanOrEqualTo(128 * 1024, new FileInfo(fixture.EventsPath).Length);
        Assert.IsFalse(File.ReadAllText(fixture.EventsPath).Contains("ignored-", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ProductActivationMetricsAreLocalRedactedAndDeduplicated()
    {
        using var fixture = DiagnosticsFixture.Create();
        var diagnostics = fixture.Diagnostics;
        diagnostics.Enable(packaged: true);
        diagnostics.RecordStartup(packaged: true);
        diagnostics.RecordFirstValue(packaged: true);
        diagnostics.RecordFirstValue(packaged: true);

        var executedAt = DateTimeOffset.UtcNow;
        var createdAt = executedAt.AddSeconds(-12);
        const string missionId = "mission-private-marker-must-not-persist";
        diagnostics.RecordMissionCompletion(missionId, createdAt, executedAt, packaged: true);
        diagnostics.RecordMissionCompletion(missionId, createdAt, executedAt, packaged: true);

        var metrics = diagnostics.ReadMetrics();
        Assert.IsNotNull(metrics.StartupMilliseconds);
        Assert.IsNotNull(metrics.FirstValueMilliseconds);
        Assert.IsNotNull(metrics.MissionCompletionMilliseconds);
        Assert.IsTrue(metrics.StartupMilliseconds >= 0);
        Assert.IsTrue(metrics.FirstValueMilliseconds >= 0);
        Assert.IsTrue(metrics.MissionCompletionMilliseconds is >= 11_900 and <= 12_100);
        Assert.AreEqual(1, metrics.MissionCompletionCount);

        var events = File.ReadAllText(fixture.EventsPath);
        StringAssert.Contains(events, NodalOsLocalDiagnostics.StartupReadyMetricCode);
        StringAssert.Contains(events, NodalOsLocalDiagnostics.FirstValueMetricCode);
        StringAssert.Contains(events, NodalOsLocalDiagnostics.MissionCompletionMetricCode);
        StringAssert.Contains(events, "\"durationMilliseconds\":12000");
        Assert.AreEqual(
            1,
            File.ReadLines(fixture.EventsPath)
                .Count(line => line.Contains(NodalOsLocalDiagnostics.FirstValueMetricCode, StringComparison.Ordinal)));
        Assert.AreEqual(
            1,
            File.ReadLines(fixture.EventsPath)
                .Count(line => line.Contains(NodalOsLocalDiagnostics.MissionCompletionMetricCode, StringComparison.Ordinal)));
        Assert.IsFalse(events.Contains(missionId, StringComparison.Ordinal));
    }

    [TestMethod]
    public void ClearingEventsDoesNotSilentlyChangeTheOptInDecision()
    {
        using var fixture = DiagnosticsFixture.Create();
        var diagnostics = fixture.Diagnostics;
        diagnostics.Enable(packaged: false);
        diagnostics.RecordStartup(packaged: false);
        Assert.IsTrue(File.Exists(fixture.EventsPath));

        diagnostics.Clear();

        Assert.IsTrue(diagnostics.Enabled);
        Assert.IsTrue(File.Exists(fixture.OptInPath));
        Assert.IsFalse(File.Exists(fixture.EventsPath));
    }

    [TestMethod]
    public async Task SuccessfulCanonicalHandoffRecordsFirstValueOnlyOnce()
    {
        using var fixture = DiagnosticsFixture.Create();
        fixture.Diagnostics.Enable(packaged: false);
        await using var app = BuildFirstValueApp(fixture.Diagnostics);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);
        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };

        using var unavailable = await client.GetAsync(
            $"{MissionControlProductHandoffExportEndpointMapper.MarkdownRoute}?ready=false",
            TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(HttpStatusCode.Conflict, unavailable.StatusCode);
        Assert.IsNull(fixture.Diagnostics.ReadMetrics().FirstValueMilliseconds);

        using var first = await client.GetAsync(
            $"{MissionControlProductHandoffExportEndpointMapper.MarkdownRoute}?ready=true",
            TestContext.CancellationTokenSource.Token);
        using var repeated = await client.GetAsync(
            $"{MissionControlProductHandoffExportEndpointMapper.MarkdownRoute}?ready=true",
            TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(HttpStatusCode.OK, first.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, repeated.StatusCode);
        Assert.IsNotNull(fixture.Diagnostics.ReadMetrics().FirstValueMilliseconds);
        Assert.AreEqual(
            1,
            File.ReadLines(fixture.EventsPath)
                .Count(line => line.Contains(NodalOsLocalDiagnostics.FirstValueMetricCode, StringComparison.Ordinal)));
        Assert.IsFalse(File.ReadAllText(fixture.EventsPath).Contains("ready=", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task SettingsRouteRequiresSameOriginAndSupportsEnableClearAndDisable()
    {
        using var fixture = DiagnosticsFixture.Create();
        await using var app = BuildSettingsApp(fixture.Diagnostics);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);
        var address = ServerAddress(app);
        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(address) };

        using var initialResponse = await client.GetAsync(
            NodalOsLocalDiagnosticsEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var initialPage = await initialResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(HttpStatusCode.OK, initialResponse.StatusCode);
        StringAssert.Contains(initialPage, "data-diagnostics-enabled=\"false\"");
        StringAssert.Contains(initialPage, "data-local-metrics=\"true\"");
        StringAssert.Contains(initialPage, "Desactivados");
        StringAssert.Contains(initialPage, "no envía datos");
        StringAssert.Contains(initialPage, "Time to first value");
        StringAssert.Contains(initialPage, "Pendiente");
        StringAssert.Contains(initialResponse.Headers.GetValues("Content-Security-Policy").Single(), "form-action 'self'");
        Assert.IsFalse(initialPage.Contains("<script", StringComparison.OrdinalIgnoreCase));

        using var crossOrigin = Post(
            NodalOsLocalDiagnosticsEndpointMapper.HtmlRoute,
            "enable",
            origin: "http://example.invalid");
        using var crossOriginResponse = await client.SendAsync(
            crossOrigin,
            TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(HttpStatusCode.Forbidden, crossOriginResponse.StatusCode);
        Assert.IsFalse(fixture.Diagnostics.Enabled);

        using var enable = Post(
            NodalOsLocalDiagnosticsEndpointMapper.HtmlRoute,
            "enable",
            new Uri(address).GetLeftPart(UriPartial.Authority));
        using var enableResponse = await client.SendAsync(enable, TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(HttpStatusCode.Redirect, enableResponse.StatusCode);
        Assert.AreEqual(NodalOsLocalDiagnosticsEndpointMapper.HtmlRoute, enableResponse.Headers.Location?.OriginalString);
        Assert.IsTrue(fixture.Diagnostics.Enabled);

        fixture.Diagnostics.RecordRequestError(new IOException("route-private-marker"), packaged: false);
        fixture.Diagnostics.RecordFirstValue(packaged: false);
        var completedAt = DateTimeOffset.UtcNow;
        fixture.Diagnostics.RecordMissionCompletion(
            "settings-mission-private-marker",
            completedAt.AddMilliseconds(-2_500),
            completedAt,
            packaged: false);
        using var enabledResponse = await client.GetAsync(
            NodalOsLocalDiagnosticsEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var enabledPage = await enabledResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        StringAssert.Contains(enabledPage, "data-diagnostics-enabled=\"true\"");
        StringAssert.Contains(enabledPage, "IOException");
        StringAssert.Contains(enabledPage, "Misión → completada");
        StringAssert.Contains(enabledPage, "2.5 s");
        StringAssert.Contains(enabledPage, NodalOsLocalDiagnostics.FirstValueMetricCode);
        Assert.IsFalse(enabledPage.Contains("route-private-marker", StringComparison.Ordinal));
        Assert.IsFalse(enabledPage.Contains("settings-mission-private-marker", StringComparison.Ordinal));
        StringAssert.Contains(enabledPage, "data-network-used=\"false\"");
        StringAssert.Contains(enabledPage, "data-product-authority=\"false\"");

        using var clear = Post(
            NodalOsLocalDiagnosticsEndpointMapper.HtmlRoute,
            "clear",
            new Uri(address).GetLeftPart(UriPartial.Authority));
        using var clearResponse = await client.SendAsync(clear, TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(HttpStatusCode.Redirect, clearResponse.StatusCode);
        Assert.IsTrue(fixture.Diagnostics.Enabled);
        Assert.IsFalse(File.Exists(fixture.EventsPath));

        using var disable = Post(
            NodalOsLocalDiagnosticsEndpointMapper.HtmlRoute,
            "disable",
            new Uri(address).GetLeftPart(UriPartial.Authority));
        using var disableResponse = await client.SendAsync(disable, TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(HttpStatusCode.Redirect, disableResponse.StatusCode);
        Assert.IsFalse(fixture.Diagnostics.Enabled);
    }

    [TestMethod]
    public async Task AttachedRuntimeRecordsStartupRequestFailureAndShutdownWithoutPayloads()
    {
        using var fixture = DiagnosticsFixture.Create();
        fixture.Diagnostics.Enable(packaged: false);
        await using var app = BuildAttachedApp(fixture.Diagnostics);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);
        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };

        using var response = await client.GetAsync("/diagnostics-test-failure", TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        await app.StopAsync(TestContext.CancellationTokenSource.Token);

        var events = File.ReadAllText(fixture.EventsPath);
        StringAssert.Contains(events, "\"kind\":\"startup\"");
        StringAssert.Contains(events, NodalOsLocalDiagnostics.StartupReadyMetricCode);
        StringAssert.Contains(events, "\"kind\":\"request-error\"");
        StringAssert.Contains(events, "\"kind\":\"shutdown\"");
        StringAssert.Contains(events, "InvalidOperationException");
        Assert.IsFalse(events.Contains("runtime-private-payload-marker", StringComparison.Ordinal));
        Assert.IsFalse(events.Contains("diagnostics-test-failure", StringComparison.Ordinal));
    }

    [TestMethod]
    public void DiagnosticsBoundaryAndPackagedSurfaceRemainNarrow()
    {
        Assert.IsFalse(NodalOsLocalDiagnosticsEndpointMapper.IsRequestAllowed(null));
        Assert.IsFalse(NodalOsLocalDiagnosticsEndpointMapper.IsRequestAllowed(IPAddress.Parse("192.0.2.88")));
        Assert.IsTrue(NodalOsLocalDiagnosticsEndpointMapper.IsRequestAllowed(IPAddress.Loopback));
        Assert.IsTrue(NodalOsDesktopLaunchRuntime.IsPackagedProductPath(
            NodalOsLocalDiagnosticsEndpointMapper.HtmlRoute));
        Assert.IsFalse(NodalOsDesktopLaunchRuntime.IsPackagedProductPath("/api/diagnostics/upload"));
        Assert.IsFalse(NodalOsDesktopLaunchRuntime.IsPackagedProductPath("/settings/diagnostics/export"));
    }

    public TestContext TestContext { get; set; } = null!;

    private static WebApplication BuildSettingsApp(NodalOsLocalDiagnostics diagnostics)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        NodalOsLocalDiagnosticsEndpointMapper.MapNodalOsLocalDiagnostics(
            app,
            () => diagnostics,
            () => false);
        return app;
    }

    private static WebApplication BuildFirstValueApp(NodalOsLocalDiagnostics diagnostics)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        diagnostics.Attach(app, packaged: false);
        app.MapGet(
            MissionControlProductHandoffExportEndpointMapper.MarkdownRoute,
            (HttpContext context) =>
            {
                if (string.Equals(context.Request.Query["ready"].ToString(), "true", StringComparison.Ordinal))
                    return (IResult)Results.Content("# handoff\n", "text/markdown; charset=utf-8");
                return Results.StatusCode(StatusCodes.Status409Conflict);
            });
        return app;
    }

    private static WebApplication BuildAttachedApp(NodalOsLocalDiagnostics diagnostics)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        diagnostics.Attach(app, packaged: false);
        app.MapGet(
            "/diagnostics-test-failure",
            static () => Task.FromException<IResult>(
                new InvalidOperationException("runtime-private-payload-marker")));
        return app;
    }

    private static HttpRequestMessage Post(string route, string action, string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, route)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["action"] = action
            })
        };
        request.Headers.TryAddWithoutValidation("Origin", origin);
        return request;
    }

    private static string ServerAddress(WebApplication app)
    {
        var feature = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        var address = feature?.Addresses.SingleOrDefault();
        Assert.IsFalse(string.IsNullOrWhiteSpace(address), "Server address was not assigned.");
        return address!;
    }

    private sealed class DiagnosticsFixture : IDisposable
    {
        private DiagnosticsFixture(string root)
        {
            Root = root;
            Diagnostics = new NodalOsLocalDiagnostics(root);
        }

        public string Root { get; }
        public NodalOsLocalDiagnostics Diagnostics { get; }
        public string OptInPath => Path.Combine(Root, NodalOsLocalDiagnostics.OptInFileName);
        public string EventsPath => Path.Combine(Root, NodalOsLocalDiagnostics.EventsFileName);

        public static DiagnosticsFixture Create()
        {
            var root = Path.Combine(Path.GetTempPath(), "nodal-os-local-diagnostics-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new DiagnosticsFixture(root);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
    }
}
