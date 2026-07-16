using System.Net;
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
[TestCategory("WorkspaceSelection")]
[TestCategory("MvpVerticalSlice")]
[TestCategory("AiriSelectiveRuntime")]
public sealed class LocalWorkspaceSelectionSelectiveRuntimeInspectorRouteTests
{
    [TestMethod]
    public async Task LocalSelectionPersistsProtectedWorkspaceAndMissionControlProjectsItWithoutPathLeak()
    {
        RequireWindows();
        using var fixture = WorkspaceFixture.Create();
        var service = fixture.CreateService();
        await using var app = BuildApp(service);
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
            LocalWorkspaceSelectionEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var formHtml = await formResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        var token = ExtractToken(formHtml);

        Assert.AreEqual(HttpStatusCode.OK, formResponse.StatusCode);
        StringAssert.Contains(formHtml, "data-nodal-os=\"workspace-selection\"");
        StringAssert.Contains(formHtml, "data-selection-state=\"NotConfigured\"");
        StringAssert.Contains(formHtml, "form-action 'self'");
        Assert.IsFalse(formHtml.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(formHtml.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(formHtml.Contains("http://", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(formHtml.Contains("https://", StringComparison.OrdinalIgnoreCase));

        using var request = CreatePost(
            new Uri(address),
            LocalWorkspaceSelectionEndpointMapper.HtmlRoute,
            token,
            fixture.WorkspaceRoot,
            "Real Product Workspace");
        using var postResponse = await client.SendAsync(
            request,
            TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.AreEqual(LocalWorkspaceSelectionEndpointMapper.HtmlRoute, postResponse.Headers.Location?.OriginalString);

        using var jsonResponse = await client.GetAsync(
            LocalWorkspaceSelectionEndpointMapper.JsonRoute,
            TestContext.CancellationTokenSource.Token);
        var json = await jsonResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.AreEqual(HttpStatusCode.OK, jsonResponse.StatusCode);
        Assert.IsTrue(root.GetProperty("accepted").GetBoolean());
        Assert.AreEqual("Ready", root.GetProperty("state").GetString());
        Assert.IsTrue(root.GetProperty("persisted").GetBoolean());
        Assert.IsTrue(root.GetProperty("rehydrated").GetBoolean());
        Assert.IsTrue(root.GetProperty("realFilesystemRead").GetBoolean());
        Assert.IsFalse(root.GetProperty("workspaceFilesystemMutated").GetBoolean());
        Assert.IsFalse(root.GetProperty("networkUsed").GetBoolean());
        Assert.IsTrue(root.GetProperty("secretsExcluded").GetBoolean());
        Assert.IsFalse(root.GetProperty("productAuthorityGranted").GetBoolean());
        Assert.AreEqual("Real Product Workspace", root.GetProperty("displayNameRedacted").GetString());
        Assert.AreEqual(64, root.GetProperty("rootPathFingerprint").GetString()?.Length);
        Assert.IsTrue(root.GetProperty("filesRead").GetInt32() >= 2);
        Assert.IsTrue(root.GetProperty("planSteps").GetArrayLength() >= 1);
        Assert.IsFalse(json.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains(fixture.FakeSecret, StringComparison.Ordinal));

        using var selectedPageResponse = await client.GetAsync(
            LocalWorkspaceSelectionEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var selectedPage = await selectedPageResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        StringAssert.Contains(selectedPage, "data-selection-state=\"Ready\"");
        StringAssert.Contains(selectedPage, "data-persisted=\"true\"");
        StringAssert.Contains(selectedPage, "Real Product Workspace");
        StringAssert.Contains(selectedPage, "persisted + revalidated");
        Assert.IsFalse(selectedPage.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(selectedPage.Contains(fixture.FakeSecret, StringComparison.Ordinal));

        using var missionResponse = await client.GetAsync(
            MissionControlProductShellEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var missionHtml = await missionResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var missionJsonResponse = await client.GetAsync(
            MissionControlProductShellEndpointMapper.JsonRoute,
            TestContext.CancellationTokenSource.Token);
        var missionJson = await missionJsonResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var missionDocument = JsonDocument.Parse(missionJson);
        var mission = missionDocument.RootElement;

        Assert.AreEqual(HttpStatusCode.OK, missionResponse.StatusCode);
        StringAssert.Contains(missionHtml, "data-workspace-selected=\"true\"");
        StringAssert.Contains(missionHtml, "data-workspace-persisted=\"true\"");
        StringAssert.Contains(missionHtml, "Real Product Workspace");
        StringAssert.Contains(missionHtml, "/workspace/select");
        Assert.IsFalse(missionHtml.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(missionHtml.Contains(fixture.FakeSecret, StringComparison.Ordinal));
        Assert.IsTrue(mission.GetProperty("workspaceSelected").GetBoolean());
        Assert.IsTrue(mission.GetProperty("workspacePersisted").GetBoolean());
        Assert.AreEqual(64, mission.GetProperty("workspaceFingerprint").GetString()?.Length);
        Assert.IsTrue(mission.GetProperty("workspaceFilesRead").GetInt32() >= 2);
        Assert.IsTrue(mission.GetProperty("evidenceRefs").EnumerateArray()
            .Any(value => (value.GetString() ?? string.Empty).StartsWith("evidence:workspace-selection:", StringComparison.Ordinal)));

        var metadata = await File.ReadAllTextAsync(
            fixture.MetadataPath,
            TestContext.CancellationTokenSource.Token);
        Assert.IsFalse(metadata.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(metadata.Contains(fixture.FakeSecret, StringComparison.Ordinal));
        StringAssert.Contains(metadata, "windows-dpapi");
    }

    [TestMethod]
    public async Task SelectionPostRejectsMissingOrMismatchedCsrfAndOrigin()
    {
        RequireWindows();
        using var fixture = WorkspaceFixture.Create();
        var service = fixture.CreateService();
        await using var app = BuildApp(service);
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
            LocalWorkspaceSelectionEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var token = ExtractToken(await formResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token));

        using var wrongTokenRequest = CreatePost(
            new Uri(address),
            LocalWorkspaceSelectionEndpointMapper.HtmlRoute,
            new string('0', token.Length),
            fixture.WorkspaceRoot,
            "Workspace");
        using var wrongTokenResponse = await client.SendAsync(
            wrongTokenRequest,
            TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(HttpStatusCode.Forbidden, wrongTokenResponse.StatusCode);
        Assert.IsFalse(File.Exists(fixture.MetadataPath));

        using var refreshed = await client.GetAsync(
            LocalWorkspaceSelectionEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        token = ExtractToken(await refreshed.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token));
        using var wrongOriginRequest = CreatePost(
            new Uri(address),
            LocalWorkspaceSelectionEndpointMapper.HtmlRoute,
            token,
            fixture.WorkspaceRoot,
            "Workspace",
            origin: "http://example.invalid");
        using var wrongOriginResponse = await client.SendAsync(
            wrongOriginRequest,
            TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(HttpStatusCode.Forbidden, wrongOriginResponse.StatusCode);
        Assert.IsFalse(File.Exists(fixture.MetadataPath));
    }

    [TestMethod]
    public void SelectionBoundaryIsLoopbackAndOriginBound()
    {
        Assert.IsFalse(LocalWorkspaceSelectionEndpointMapper.IsRequestAllowed(null));
        Assert.IsFalse(LocalWorkspaceSelectionEndpointMapper.IsRequestAllowed(IPAddress.Parse("192.0.2.20")));
        Assert.IsTrue(LocalWorkspaceSelectionEndpointMapper.IsRequestAllowed(IPAddress.Loopback));
    }

    public TestContext TestContext { get; set; } = null!;

    private static WebApplication BuildApp(NodalOsWorkspaceSelectionService service)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        LocalWorkspaceSelectionEndpointMapper.MapLocalWorkspaceSelection(
            app,
            app.Environment,
            () => service);
        MissionControlProductShellEndpointMapper.MapMissionControlProductShell(
            app,
            app.Environment,
            () => service);
        return app;
    }

    private static HttpRequestMessage CreatePost(
        Uri baseUri,
        string route,
        string token,
        string rootPath,
        string displayName,
        string? origin = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, route)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                [LocalWorkspaceSelectionEndpointMapper.TokenField] = token,
                ["rootPath"] = rootPath,
                ["displayName"] = displayName
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
            $"name=\"{LocalWorkspaceSelectionEndpointMapper.TokenField}\" value=\"(?<token>[0-9a-f]+)\"",
            RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));
        Assert.IsTrue(match.Success, "Workspace selection request token was not rendered.");
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
            Assert.Inconclusive("Protected workspace selection uses Windows DPAPI.");
    }

    private sealed class WorkspaceFixture : IDisposable
    {
        private WorkspaceFixture(string root)
        {
            Root = root;
            WorkspaceRoot = Path.Combine(root, "workspace");
            MetadataPath = Path.Combine(root, "config", "selection.v1.json");
            SecretRoot = Path.Combine(root, "secrets");
            FakeSecret = "sk-workspace-route-fixture-secret-value-123456789";
            Directory.CreateDirectory(Path.Combine(WorkspaceRoot, "src"));
            File.WriteAllText(Path.Combine(WorkspaceRoot, "README.md"), "# Real workspace route fixture");
            File.WriteAllText(
                Path.Combine(WorkspaceRoot, "src", "Program.cs"),
                $"var api_key = \"{FakeSecret}\";{Environment.NewLine}Console.WriteLine(\"fixture\");");
        }

        public string Root { get; }
        public string WorkspaceRoot { get; }
        public string MetadataPath { get; }
        public string SecretRoot { get; }
        public string FakeSecret { get; }

        public static WorkspaceFixture Create() => new(
            Path.Combine(Path.GetTempPath(), "nodal-os-workspace-route-tests", Guid.NewGuid().ToString("N")));

        public NodalOsWorkspaceSelectionService CreateService() => new(
            MetadataPath,
            new WindowsDpapiSecretReferenceStore(SecretRoot));

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Root))
                {
                    foreach (var path in Directory.GetFiles(Root, "*", SearchOption.AllDirectories))
                        File.SetAttributes(path, FileAttributes.Normal);
                    Directory.Delete(Root, recursive: true);
                }
            }
            catch
            {
            }
        }
    }
}
