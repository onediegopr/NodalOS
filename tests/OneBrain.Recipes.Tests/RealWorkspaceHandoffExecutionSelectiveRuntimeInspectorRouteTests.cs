using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Core.Workspace;
using OneBrain.Core.Models;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("WorkspaceHandoffExecution")]
[TestCategory("MvpVerticalSlice")]
[TestCategory("AiriSelectiveRuntime")]
public sealed class RealWorkspaceHandoffExecutionSelectiveRuntimeInspectorRouteTests
{
    [TestMethod]
    public async Task ExecutionRouteApprovesRunsVerifiesProjectsAndRollsBackWithoutLeaks()
    {
        RequireWindows();
        using var fixture = WorkspaceFixture.Create();
        var services = fixture.CreateServices();
        await PrepareMissionAsync(services);
        await using var app = BuildApp(services);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);
        var address = ServerAddress(app);
        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(address) };

        using var approvalPageResponse = await client.GetAsync(
            RealWorkspaceHandoffExecutionEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var approvalPage = await approvalPageResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        var approvalToken = ExtractToken(approvalPage);

        Assert.AreEqual(HttpStatusCode.OK, approvalPageResponse.StatusCode);
        StringAssert.Contains(approvalPage, "data-nodal-os=\"real-workspace-handoff-execution\"");
        StringAssert.Contains(approvalPage, "data-state=\"ReadyForApproval\"");
        StringAssert.Contains(approvalPage, "data-executed=\"false\"");
        StringAssert.Contains(approvalPage, "Aprobar alcance y ejecutar");
        StringAssert.Contains(approvalPage, "NODAL_HANDOFF.md");
        StringAssert.Contains(approvalPageResponse.Headers.GetValues("Content-Security-Policy").Single(), "form-action 'self'");
        AssertNoLeak(approvalPage, fixture);
        Assert.IsFalse(File.Exists(fixture.TargetPath));

        using var executeRequest = CreatePost(
            new Uri(address),
            RealWorkspaceHandoffExecutionEndpointMapper.HtmlRoute,
            approvalToken);
        using var executeResponse = await client.SendAsync(
            executeRequest,
            TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(HttpStatusCode.Redirect, executeResponse.StatusCode);
        Assert.AreEqual(RealWorkspaceHandoffExecutionEndpointMapper.HtmlRoute, executeResponse.Headers.Location?.OriginalString);

        using var executionJsonResponse = await client.GetAsync(
            RealWorkspaceHandoffExecutionEndpointMapper.JsonRoute,
            TestContext.CancellationTokenSource.Token);
        var executionJson = await executionJsonResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var executionDocument = JsonDocument.Parse(executionJson);
        var execution = executionDocument.RootElement;

        Assert.AreEqual(HttpStatusCode.OK, executionJsonResponse.StatusCode);
        Assert.IsTrue(execution.GetProperty("accepted").GetBoolean());
        Assert.AreEqual((int)NodalOsWorkspaceHandoffExecutionState.Completed, execution.GetProperty("state").GetInt32());
        Assert.IsTrue(execution.GetProperty("persisted").GetBoolean());
        Assert.IsTrue(execution.GetProperty("rehydrated").GetBoolean());
        Assert.IsTrue(execution.GetProperty("executed").GetBoolean());
        Assert.IsTrue(execution.GetProperty("verified").GetBoolean());
        Assert.IsTrue(execution.GetProperty("rollbackAvailable").GetBoolean());
        Assert.IsFalse(execution.GetProperty("networkUsed").GetBoolean());
        Assert.IsFalse(execution.GetProperty("externalProcessUsed").GetBoolean());
        Assert.IsFalse(execution.GetProperty("productAuthorityGranted").GetBoolean());
        Assert.AreEqual("CreateTextFile", execution.GetProperty("actionKind").GetString());
        Assert.AreEqual("NODAL_HANDOFF.md", execution.GetProperty("relativeTargetPath").GetString());
        Assert.AreEqual(64, execution.GetProperty("resultSha256").GetString()?.Length);
        Assert.IsTrue(execution.GetProperty("evidenceRefs").GetArrayLength() >= 2);
        Assert.IsTrue(execution.GetProperty("timeline").GetArrayLength() >= 5);
        Assert.IsTrue(File.Exists(fixture.TargetPath));
        Assert.AreEqual(execution.GetProperty("resultSha256").GetString(), fixture.TargetHash());
        AssertNoLeak(executionJson, fixture);

        using var executionPageResponse = await client.GetAsync(
            RealWorkspaceHandoffExecutionEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var executionPage = await executionPageResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        var rollbackToken = ExtractToken(executionPage);
        StringAssert.Contains(executionPage, "data-state=\"Completed\"");
        StringAssert.Contains(executionPage, "data-executed=\"true\"");
        StringAssert.Contains(executionPage, "data-verified=\"true\"");
        StringAssert.Contains(executionPage, "data-rollback-available=\"true\"");
        StringAssert.Contains(executionPage, "Restaurar estado anterior");
        AssertNoLeak(executionPage, fixture);

        using var controlJsonResponse = await client.GetAsync(
            MissionControlProductShellEndpointMapper.JsonRoute,
            TestContext.CancellationTokenSource.Token);
        var controlJson = await controlJsonResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var controlDocument = JsonDocument.Parse(controlJson);
        var control = controlDocument.RootElement;
        Assert.IsTrue(control.GetProperty("realMissionDraft").GetBoolean());
        Assert.IsFalse(control.GetProperty("actionApprovalAvailable").GetBoolean());
        Assert.AreEqual("Completed", control.GetProperty("actionExecutionState").GetString());
        Assert.IsTrue(control.GetProperty("actionExecuted").GetBoolean());
        Assert.IsTrue(control.GetProperty("actionVerified").GetBoolean());
        Assert.IsTrue(control.GetProperty("actionRollbackAvailable").GetBoolean());
        Assert.IsFalse(control.GetProperty("productAuthorityGranted").GetBoolean());
        Assert.AreEqual("Completed", control.GetProperty("missionStatus").GetString());
        Assert.AreEqual(100, control.GetProperty("progressPercent").GetInt32());
        AssertNoLeak(controlJson, fixture);

        using var controlPageResponse = await client.GetAsync(
            MissionControlProductShellEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var controlPage = await controlPageResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        StringAssert.Contains(controlPage, "data-action-execution-state=\"Completed\"");
        StringAssert.Contains(controlPage, "data-action-executed=\"true\"");
        StringAssert.Contains(controlPage, "data-action-verified=\"true\"");
        StringAssert.Contains(controlPage, "data-rollback-available=\"true\"");
        StringAssert.Contains(controlPage, "/mission/execution");
        AssertNoLeak(controlPage, fixture);

        using var rollbackRequest = CreatePost(
            new Uri(address),
            RealWorkspaceHandoffExecutionEndpointMapper.RollbackRoute,
            rollbackToken);
        using var rollbackResponse = await client.SendAsync(
            rollbackRequest,
            TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(HttpStatusCode.Redirect, rollbackResponse.StatusCode);
        Assert.AreEqual(RealWorkspaceHandoffExecutionEndpointMapper.HtmlRoute, rollbackResponse.Headers.Location?.OriginalString);
        Assert.IsFalse(File.Exists(fixture.TargetPath));

        using var rolledBackJsonResponse = await client.GetAsync(
            RealWorkspaceHandoffExecutionEndpointMapper.JsonRoute,
            TestContext.CancellationTokenSource.Token);
        var rolledBackJson = await rolledBackJsonResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var rolledBackDocument = JsonDocument.Parse(rolledBackJson);
        var rolledBack = rolledBackDocument.RootElement;
        Assert.IsTrue(rolledBack.GetProperty("accepted").GetBoolean());
        Assert.AreEqual((int)NodalOsWorkspaceHandoffExecutionState.RolledBack, rolledBack.GetProperty("state").GetInt32());
        Assert.IsTrue(rolledBack.GetProperty("rolledBack").GetBoolean());
        Assert.IsFalse(rolledBack.GetProperty("rollbackAvailable").GetBoolean());
        Assert.IsFalse(rolledBack.GetProperty("productAuthorityGranted").GetBoolean());
        AssertNoLeak(rolledBackJson, fixture);
    }

    [TestMethod]
    public async Task ExecutionPostRejectsMismatchedTokenAndOriginWithoutMutation()
    {
        RequireWindows();
        using var fixture = WorkspaceFixture.Create();
        var services = fixture.CreateServices();
        await PrepareMissionAsync(services);
        await using var app = BuildApp(services);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);
        var address = ServerAddress(app);
        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(address) };

        using var formResponse = await client.GetAsync(
            RealWorkspaceHandoffExecutionEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var token = ExtractToken(await formResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token));
        using var wrongTokenRequest = CreatePost(
            new Uri(address),
            RealWorkspaceHandoffExecutionEndpointMapper.HtmlRoute,
            new string('0', token.Length));
        using var wrongTokenResponse = await client.SendAsync(
            wrongTokenRequest,
            TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(HttpStatusCode.Forbidden, wrongTokenResponse.StatusCode);
        Assert.IsFalse(File.Exists(fixture.TargetPath));
        Assert.IsFalse(File.Exists(fixture.ExecutionMetadataPath));

        using var refreshed = await client.GetAsync(
            RealWorkspaceHandoffExecutionEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        token = ExtractToken(await refreshed.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token));
        using var wrongOriginRequest = CreatePost(
            new Uri(address),
            RealWorkspaceHandoffExecutionEndpointMapper.HtmlRoute,
            token,
            origin: "http://example.invalid");
        using var wrongOriginResponse = await client.SendAsync(
            wrongOriginRequest,
            TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(HttpStatusCode.Forbidden, wrongOriginResponse.StatusCode);
        Assert.IsFalse(File.Exists(fixture.TargetPath));
        Assert.IsFalse(File.Exists(fixture.ExecutionMetadataPath));
    }

    [TestMethod]
    public void ExecutionBoundaryIsLoopbackOnly()
    {
        Assert.IsFalse(RealWorkspaceHandoffExecutionEndpointMapper.IsRequestAllowed(null));
        Assert.IsFalse(RealWorkspaceHandoffExecutionEndpointMapper.IsRequestAllowed(IPAddress.Parse("192.0.2.88")));
        Assert.IsTrue(RealWorkspaceHandoffExecutionEndpointMapper.IsRequestAllowed(IPAddress.Loopback));
    }

    public TestContext TestContext { get; set; } = null!;

    private async Task PrepareMissionAsync(ServiceSet services)
    {
        var selected = await services.Selection.SelectAsync(
            services.Fixture.WorkspaceRoot,
            "Route Handoff Workspace",
            TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(selected.Accepted, string.Join(" | ", selected.ReviewBlockers));
        var mission = await services.Mission.CreateAsync(
            "Prepare a verified handoff and execute only the reviewed reversible document action.",
            TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(mission.Accepted, string.Join(" | ", mission.ReviewBlockers));
    }

    private static WebApplication BuildApp(ServiceSet services)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        LocalWorkspaceSelectionEndpointMapper.MapLocalWorkspaceSelection(app, app.Environment, () => services.Selection);
        RealWorkspaceMissionDraftEndpointMapper.MapRealWorkspaceMissionDraft(app, app.Environment, () => services.Mission);
        RealWorkspaceHandoffExecutionEndpointMapper.MapRealWorkspaceHandoffExecution(app, app.Environment, () => services.Execution);
        MissionControlProductShellEndpointMapper.MapMissionControlProductShell(
            app,
            app.Environment,
            () => services.Selection,
            () => services.Mission,
            () => services.Execution);
        return app;
    }

    private static HttpRequestMessage CreatePost(
        Uri baseUri,
        string route,
        string token,
        string? origin = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, route)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                [RealWorkspaceHandoffExecutionEndpointMapper.TokenField] = token
            })
        };
        request.Headers.TryAddWithoutValidation(
            "Origin",
            origin ?? baseUri.GetLeftPart(UriPartial.Authority));
        return request;
    }

    private static string ExtractToken(string html)
    {
        var match = Regex.Match(
            html,
            $"name=\"{RealWorkspaceHandoffExecutionEndpointMapper.TokenField}\" value=\"(?<token>[0-9a-f]+)\"",
            RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));
        Assert.IsTrue(match.Success, "Handoff execution request token was not rendered.");
        return match.Groups["token"].Value;
    }

    private static string ServerAddress(WebApplication app)
    {
        var feature = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        var address = feature?.Addresses.SingleOrDefault();
        Assert.IsFalse(string.IsNullOrWhiteSpace(address), "Server address was not assigned.");
        return address!;
    }

    private static void RequireWindows()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Inconclusive("Protected workspace execution uses the Windows DPAPI root reference.");
    }

    private static void AssertNoLeak(string text, WorkspaceFixture fixture)
    {
        Assert.IsFalse(text.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains(fixture.SensitiveFixtureValue, StringComparison.Ordinal));
    }

    private sealed record ServiceSet(
        WorkspaceFixture Fixture,
        NodalOsWorkspaceSelectionService Selection,
        NodalOsWorkspaceMissionDraftService Mission,
        NodalOsWorkspaceHandoffExecutionService Execution);

    private sealed class WorkspaceFixture : IDisposable
    {
        private WorkspaceFixture(string root)
        {
            Root = root;
            WorkspaceRoot = Path.Combine(root, "workspace");
            SelectionMetadataPath = Path.Combine(root, "config", "selection.v1.json");
            MissionMetadataPath = Path.Combine(root, "config", "mission.v1.json");
            ExecutionMetadataPath = Path.Combine(root, "config", "execution.v1.json");
            RestoreRoot = Path.Combine(root, "restore");
            SecretRoot = Path.Combine(root, "secrets");
            TargetPath = Path.Combine(WorkspaceRoot, NodalOsWorkspaceMissionDraftService.RelativeTargetPath);
            SensitiveFixtureValue = "route-handoff-sensitive-fixture-value";
            Directory.CreateDirectory(Path.Combine(WorkspaceRoot, "src"));
            File.WriteAllText(Path.Combine(WorkspaceRoot, "README.md"), "# Route handoff workspace fixture");
            var sensitiveName = string.Concat("api", "_key");
            File.WriteAllText(
                Path.Combine(WorkspaceRoot, "src", "Program.cs"),
                $"var {sensitiveName} = \"{SensitiveFixtureValue}\";{Environment.NewLine}Console.WriteLine(\"fixture\");");
        }

        public string Root { get; }
        public string WorkspaceRoot { get; }
        public string SelectionMetadataPath { get; }
        public string MissionMetadataPath { get; }
        public string ExecutionMetadataPath { get; }
        public string RestoreRoot { get; }
        public string SecretRoot { get; }
        public string TargetPath { get; }
        public string SensitiveFixtureValue { get; }

        public static WorkspaceFixture Create() => new(
            Path.Combine(Path.GetTempPath(), "nodal-os-workspace-handoff-route-tests", Guid.NewGuid().ToString("N")));

        public ServiceSet CreateServices()
        {
            var store = new WindowsDpapiSecretReferenceStore(SecretRoot);
            var selection = new NodalOsWorkspaceSelectionService(SelectionMetadataPath, store);
            var mission = new NodalOsWorkspaceMissionDraftService(MissionMetadataPath, selection, store);
            var execution = new NodalOsWorkspaceHandoffExecutionService(
                ExecutionMetadataPath,
                RestoreRoot,
                selection,
                mission,
                store);
            return new ServiceSet(this, selection, mission, execution);
        }

        public string TargetHash() =>
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(TargetPath))).ToLowerInvariant();

        public void Dispose()
        {
            try
            {
                if (!Directory.Exists(Root))
                    return;
                foreach (var path in Directory.GetFiles(Root, "*", SearchOption.AllDirectories))
                    File.SetAttributes(path, FileAttributes.Normal);
                Directory.Delete(Root, recursive: true);
            }
            catch
            {
            }
        }
    }
}