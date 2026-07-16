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
[TestCategory("WorkspaceMissionDraft")]
[TestCategory("MvpVerticalSlice")]
[TestCategory("AiriSelectiveRuntime")]
public sealed class RealWorkspaceMissionDraftSelectiveRuntimeInspectorRouteTests
{
    [TestMethod]
    public async Task MissionDraftRoutePersistsReviewedCandidateAndMissionControlProjectsItWithoutMutationOrPathLeak()
    {
        RequireWindows();
        using var fixture = WorkspaceFixture.Create();
        var before = fixture.FileHashes();
        var selection = fixture.CreateSelectionService();
        var selected = await selection.SelectAsync(
            fixture.WorkspaceRoot,
            "CI Mission Workspace",
            TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(selected.Accepted, string.Join(" | ", selected.ReviewBlockers));
        var mission = fixture.CreateMissionService(selection);

        await using var app = BuildApp(selection, mission);
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
            RealWorkspaceMissionDraftEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var formHtml = await formResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        var token = ExtractToken(formHtml);

        Assert.AreEqual(HttpStatusCode.OK, formResponse.StatusCode);
        StringAssert.Contains(formHtml, "data-nodal-os=\"real-workspace-mission-draft\"");
        StringAssert.Contains(formHtml, "data-state=\"NotConfigured\"");
        StringAssert.Contains(formResponse.Headers.GetValues("Content-Security-Policy").Single(), "form-action 'self'");
        Assert.IsFalse(formHtml.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(formHtml.Contains("<script", StringComparison.OrdinalIgnoreCase));

        const string goal = "Prepare a verified product handoff for the selected workspace and make the next safe action explicit.";
        using var request = CreatePost(
            new Uri(address),
            RealWorkspaceMissionDraftEndpointMapper.HtmlRoute,
            token,
            goal);
        using var postResponse = await client.SendAsync(request, TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.AreEqual(RealWorkspaceMissionDraftEndpointMapper.HtmlRoute, postResponse.Headers.Location?.OriginalString);

        using var jsonResponse = await client.GetAsync(
            RealWorkspaceMissionDraftEndpointMapper.JsonRoute,
            TestContext.CancellationTokenSource.Token);
        var json = await jsonResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.AreEqual(HttpStatusCode.OK, jsonResponse.StatusCode);
        Assert.IsTrue(root.GetProperty("accepted").GetBoolean());
        Assert.AreEqual("ReadyForReview", root.GetProperty("state").GetString());
        Assert.IsTrue(root.GetProperty("persisted").GetBoolean());
        Assert.IsTrue(root.GetProperty("rehydrated").GetBoolean());
        Assert.IsTrue(root.GetProperty("realFilesystemRead").GetBoolean());
        Assert.IsFalse(root.GetProperty("workspaceFilesystemMutated").GetBoolean());
        Assert.IsFalse(root.GetProperty("networkUsed").GetBoolean());
        Assert.IsTrue(root.GetProperty("secretsExcluded").GetBoolean());
        Assert.IsFalse(root.GetProperty("productAuthorityGranted").GetBoolean());
        Assert.AreEqual(goal, root.GetProperty("goalRedacted").GetString());
        var candidate = root.GetProperty("candidate");
        Assert.AreEqual("CreateTextFile", candidate.GetProperty("kind").GetString());
        Assert.AreEqual("ReadyForReview", candidate.GetProperty("state").GetString());
        Assert.AreEqual("NODAL_HANDOFF.md", candidate.GetProperty("relativeTargetPath").GetString());
        Assert.IsTrue(candidate.GetProperty("approvalRequired").GetBoolean());
        Assert.IsFalse(candidate.GetProperty("executionEnabled").GetBoolean());
        Assert.AreEqual(64, candidate.GetProperty("proposedSha256").GetString()?.Length);
        Assert.AreEqual(6, root.GetProperty("plan").GetProperty("steps").GetArrayLength());
        Assert.IsFalse(json.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains(fixture.SensitiveFixtureValue, StringComparison.Ordinal));

        using var missionPageResponse = await client.GetAsync(
            RealWorkspaceMissionDraftEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var missionPage = await missionPageResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        StringAssert.Contains(missionPage, "data-state=\"ReadyForReview\"");
        StringAssert.Contains(missionPage, "data-persisted=\"true\"");
        StringAssert.Contains(missionPage, "data-rehydrated=\"true\"");
        StringAssert.Contains(missionPage, "data-candidate-state=\"ReadyForReview\"");
        StringAssert.Contains(missionPage, "data-execution-enabled=\"false\"");
        StringAssert.Contains(missionPage, goal);
        StringAssert.Contains(missionPage, "NODAL_HANDOFF.md");
        StringAssert.Contains(missionPage, "Aprobar y ejecutar — próximo bloque");
        Assert.IsFalse(missionPage.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(missionPage.Contains(fixture.SensitiveFixtureValue, StringComparison.Ordinal));

        using var controlJsonResponse = await client.GetAsync(
            MissionControlProductShellEndpointMapper.JsonRoute,
            TestContext.CancellationTokenSource.Token);
        var controlJson = await controlJsonResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var controlDocument = JsonDocument.Parse(controlJson);
        var control = controlDocument.RootElement;
        Assert.IsTrue(control.GetProperty("realMissionDraft").GetBoolean());
        Assert.IsTrue(control.GetProperty("missionDraftPersisted").GetBoolean());
        Assert.AreEqual(goal, control.GetProperty("goal").GetString());
        Assert.AreEqual("NODAL_HANDOFF.md", control.GetProperty("actionCandidateTarget").GetString());
        Assert.AreEqual("CreateTextFile", control.GetProperty("actionCandidateKind").GetString());
        Assert.IsFalse(control.GetProperty("actionExecutionEnabled").GetBoolean());
        Assert.AreEqual("AwaitingMissionScopeApproval", control.GetProperty("missionStatus").GetString());
        Assert.IsFalse(controlJson.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(controlJson.Contains(fixture.SensitiveFixtureValue, StringComparison.Ordinal));

        using var controlPageResponse = await client.GetAsync(
            MissionControlProductShellEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var controlPage = await controlPageResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        StringAssert.Contains(controlPage, "data-real-mission-draft=\"true\"");
        StringAssert.Contains(controlPage, "data-mission-draft-persisted=\"true\"");
        StringAssert.Contains(controlPage, "data-action-execution-enabled=\"false\"");
        StringAssert.Contains(controlPage, goal);
        StringAssert.Contains(controlPage, "NODAL_HANDOFF.md");
        StringAssert.Contains(controlPage, "/mission/new");
        Assert.IsFalse(controlPage.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(controlPage.Contains(fixture.SensitiveFixtureValue, StringComparison.Ordinal));

        Assert.IsTrue(File.Exists(fixture.MissionMetadataPath));
        Assert.IsFalse(File.Exists(fixture.TargetPath));
        var metadata = await File.ReadAllTextAsync(
            fixture.MissionMetadataPath,
            TestContext.CancellationTokenSource.Token);
        Assert.IsFalse(metadata.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(metadata.Contains(fixture.SensitiveFixtureValue, StringComparison.Ordinal));
        AssertWorkspaceUnchanged(before, fixture.FileHashes());
    }

    [TestMethod]
    public async Task MissionDraftPostRejectsMismatchedTokenAndOriginWithoutPersistence()
    {
        RequireWindows();
        using var fixture = WorkspaceFixture.Create();
        var selection = fixture.CreateSelectionService();
        var selected = await selection.SelectAsync(
            fixture.WorkspaceRoot,
            cancellationToken: TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(selected.Accepted);
        var mission = fixture.CreateMissionService(selection);
        await using var app = BuildApp(selection, mission);
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
            RealWorkspaceMissionDraftEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var token = ExtractToken(await formResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token));
        using var wrongTokenRequest = CreatePost(
            new Uri(address),
            RealWorkspaceMissionDraftEndpointMapper.HtmlRoute,
            new string('0', token.Length),
            "Prepare a safe mission draft for review only.");
        using var wrongTokenResponse = await client.SendAsync(
            wrongTokenRequest,
            TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(HttpStatusCode.Forbidden, wrongTokenResponse.StatusCode);
        Assert.IsFalse(File.Exists(fixture.MissionMetadataPath));

        using var refreshed = await client.GetAsync(
            RealWorkspaceMissionDraftEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        token = ExtractToken(await refreshed.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token));
        using var wrongOriginRequest = CreatePost(
            new Uri(address),
            RealWorkspaceMissionDraftEndpointMapper.HtmlRoute,
            token,
            "Prepare a safe mission draft for review only.",
            origin: "http://example.invalid");
        using var wrongOriginResponse = await client.SendAsync(
            wrongOriginRequest,
            TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(HttpStatusCode.Forbidden, wrongOriginResponse.StatusCode);
        Assert.IsFalse(File.Exists(fixture.MissionMetadataPath));
        Assert.IsFalse(File.Exists(fixture.TargetPath));
    }

    [TestMethod]
    public void MissionDraftBoundaryIsLoopbackOnly()
    {
        Assert.IsFalse(RealWorkspaceMissionDraftEndpointMapper.IsRequestAllowed(null));
        Assert.IsFalse(RealWorkspaceMissionDraftEndpointMapper.IsRequestAllowed(IPAddress.Parse("192.0.2.44")));
        Assert.IsTrue(RealWorkspaceMissionDraftEndpointMapper.IsRequestAllowed(IPAddress.Loopback));
    }

    public TestContext TestContext { get; set; } = null!;

    private static WebApplication BuildApp(
        NodalOsWorkspaceSelectionService selection,
        NodalOsWorkspaceMissionDraftService mission)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        LocalWorkspaceSelectionEndpointMapper.MapLocalWorkspaceSelection(app, app.Environment, () => selection);
        RealWorkspaceMissionDraftEndpointMapper.MapRealWorkspaceMissionDraft(app, app.Environment, () => mission);
        MissionControlProductShellEndpointMapper.MapMissionControlProductShell(
            app,
            app.Environment,
            () => selection,
            () => mission);
        return app;
    }

    private static HttpRequestMessage CreatePost(
        Uri baseUri,
        string route,
        string token,
        string goal,
        string? origin = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, route)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                [RealWorkspaceMissionDraftEndpointMapper.TokenField] = token,
                ["goal"] = goal
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
            $"name=\"{RealWorkspaceMissionDraftEndpointMapper.TokenField}\" value=\"(?<token>[0-9a-f]+)\"",
            RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));
        Assert.IsTrue(match.Success, "Mission draft request token was not rendered.");
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
            Assert.Inconclusive("Protected real workspace mission drafts depend on the Windows DPAPI workspace root reference.");
    }

    private static void AssertWorkspaceUnchanged(
        IReadOnlyDictionary<string, string> before,
        IReadOnlyDictionary<string, string> after)
    {
        CollectionAssert.AreEquivalent(before.Keys.ToArray(), after.Keys.ToArray());
        foreach (var pair in before)
            Assert.AreEqual(pair.Value, after[pair.Key], pair.Key);
    }

    private sealed class WorkspaceFixture : IDisposable
    {
        private WorkspaceFixture(string root)
        {
            Root = root;
            WorkspaceRoot = Path.Combine(root, "workspace");
            SelectionMetadataPath = Path.Combine(root, "config", "selection.v1.json");
            MissionMetadataPath = Path.Combine(root, "config", "mission.v1.json");
            SecretRoot = Path.Combine(root, "secrets");
            TargetPath = Path.Combine(WorkspaceRoot, NodalOsWorkspaceMissionDraftService.RelativeTargetPath);
            SensitiveFixtureValue = "route-mission-sensitive-fixture-value";
            Directory.CreateDirectory(Path.Combine(WorkspaceRoot, "src"));
            File.WriteAllText(Path.Combine(WorkspaceRoot, "README.md"), "# Real mission route fixture");
            File.WriteAllText(
                Path.Combine(WorkspaceRoot, "src", "Program.cs"),
                string.Concat("var ", "api", "_key", " = \\"", SensitiveFixtureValue, "\\\";"));
        }

        public string Root { get; }
        public string WorkspaceRoot { get; }
        public string SelectionMetadataPath { get; }
        public string MissionMetadataPath { get; }
        public string SecretRoot { get; }
        public string TargetPath { get; }
        public string SensitiveFixtureValue { get; }

        public static WorkspaceFixture Create() => new(
            Path.Combine(Path.GetTempPath(), "nodal-os-workspace-mission-route-tests", Guid.NewGuid().ToString("N")));

        public NodalOsWorkspaceSelectionService CreateSelectionService() => new(
            SelectionMetadataPath,
            new WindowsDpapiSecretReferenceStore(SecretRoot));

        public NodalOsWorkspaceMissionDraftService CreateMissionService(
            NodalOsWorkspaceSelectionService selection) => new(
            MissionMetadataPath,
            selection,
            new WindowsDpapiSecretReferenceStore(SecretRoot));

        public Dictionary<string, string> FileHashes() =>
            Directory.GetFiles(WorkspaceRoot, "*", SearchOption.AllDirectories)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    value => Path.GetRelativePath(WorkspaceRoot, value),
                    value => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(value))).ToLowerInvariant(),
                    StringComparer.OrdinalIgnoreCase);

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