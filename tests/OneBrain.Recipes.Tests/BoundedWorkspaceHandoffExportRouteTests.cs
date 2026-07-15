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
public sealed class BoundedWorkspaceHandoffExportRouteTests
{
    [TestMethod]
    public async Task VerifiedWorkspaceExportsSanitizedDeterministicMarkdownWithoutMutation()
    {
        var root = CreateRoot();
        var fakeSecret = "s" + "k-export-fixture-secret-value-123456789";
        var readme = Path.Combine(root, "README.md");
        var source = Path.Combine(root, "src", "Program.cs");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(source)!);
            await File.WriteAllTextAsync(readme, "# Export fixture");
            await File.WriteAllTextAsync(
                source,
                $"var api_key = \"{fakeSecret}\";{Environment.NewLine}Console.WriteLine(\"fixture\");");
            var readmeBefore = await File.ReadAllTextAsync(readme);
            var sourceBefore = await File.ReadAllTextAsync(source);
            var filesBefore = Directory.GetFiles(root, "*", SearchOption.AllDirectories).OrderBy(value => value).ToArray();

            await using var app = BuildApp(Environments.Development, () => root);
            await app.StartAsync(TestContext.CancellationTokenSource.Token);
            using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };

            using var jsonResponse = await client.GetAsync(
                BoundedWorkspaceHandoffExportEndpointMapper.JsonRoute,
                TestContext.CancellationTokenSource.Token);
            var json = await jsonResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
            using var markdownResponse = await client.GetAsync(
                BoundedWorkspaceHandoffExportEndpointMapper.MarkdownRoute,
                TestContext.CancellationTokenSource.Token);
            var markdown = await markdownResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

            Assert.AreEqual(HttpStatusCode.OK, jsonResponse.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, markdownResponse.StatusCode);
            Assert.AreEqual("no-store", jsonResponse.Headers.CacheControl?.ToString());
            Assert.AreEqual("no-store", markdownResponse.Headers.CacheControl?.ToString());
            Assert.AreEqual("text/markdown", markdownResponse.Content.Headers.ContentType?.MediaType);
            StringAssert.Contains(markdownResponse.Content.Headers.ContentType?.CharSet ?? string.Empty, "utf-8");

            var disposition = markdownResponse.Content.Headers.ContentDisposition?.ToString();
            if (string.IsNullOrWhiteSpace(disposition) &&
                markdownResponse.Headers.TryGetValues("Content-Disposition", out var dispositionValues))
            {
                disposition = dispositionValues.Single();
            }
            Assert.IsFalse(string.IsNullOrWhiteSpace(disposition));
            StringAssert.Contains(disposition!, "attachment");
            StringAssert.Contains(disposition!, "nodal-os-workspace-handoff-");
            StringAssert.Contains(disposition!, ".md");

            using var document = JsonDocument.Parse(json);
            var payload = document.RootElement;
            Assert.IsTrue(payload.GetProperty("accepted").GetBoolean());
            Assert.IsTrue(payload.GetProperty("localDevOnly").GetBoolean());
            Assert.IsTrue(payload.GetProperty("readOnly").GetBoolean());
            Assert.IsTrue(payload.GetProperty("secretsExcluded").GetBoolean());
            Assert.IsTrue(payload.GetProperty("rootConfigured").GetBoolean());
            Assert.AreEqual("Completed", payload.GetProperty("missionStatus").GetString());
            Assert.AreEqual("GO_BOUNDED_WORKSPACE_HANDOFF_EXPORT_READY", payload.GetProperty("decision").GetString());
            Assert.AreEqual(64, payload.GetProperty("evidenceDigest").GetString()?.Length);
            Assert.IsTrue(payload.GetProperty("deterministic").GetBoolean());
            Assert.IsFalse(payload.GetProperty("containsRawPayload").GetBoolean());
            Assert.IsFalse(payload.GetProperty("containsExternalResource").GetBoolean());
            Assert.IsFalse(payload.GetProperty("isAuthoritative").GetBoolean());
            Assert.IsFalse(payload.GetProperty("executable").GetBoolean());
            Assert.IsTrue(payload.GetProperty("realFilesystemRead").GetBoolean());
            Assert.IsFalse(payload.GetProperty("filesystemMutationAllowed").GetBoolean());
            Assert.IsFalse(payload.GetProperty("networkUsed").GetBoolean());
            Assert.IsFalse(payload.GetProperty("productAuthorityGranted").GetBoolean());
            Assert.IsTrue(payload.GetProperty("evidenceRefs").GetArrayLength() > 0);
            StringAssert.Contains(payload.GetProperty("markdownRedacted").GetString() ?? string.Empty, "NODAL OS Bounded Workspace Handoff");

            StringAssert.Contains(markdown, "# NODAL OS Bounded Workspace Handoff");
            StringAssert.Contains(markdown, "## Evidence");
            StringAssert.Contains(markdown, "## Next safe step");
            Assert.IsFalse(json.Contains(root, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(markdown.Contains(root, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(json.Contains(fakeSecret, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(markdown.Contains(fakeSecret, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(markdown.Contains("<script", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(markdown.Contains("http://", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(markdown.Contains("https://", StringComparison.OrdinalIgnoreCase));

            CollectionAssert.AreEqual(filesBefore, Directory.GetFiles(root, "*", SearchOption.AllDirectories).OrderBy(value => value).ToArray());
            Assert.AreEqual(readmeBefore, await File.ReadAllTextAsync(readme));
            Assert.AreEqual(sourceBefore, await File.ReadAllTextAsync(source));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task MissingRootReturnsConflictWithoutAttachmentOrFilesystemRead()
    {
        await using var app = BuildApp(Environments.Development, () => null);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);
        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };

        using var jsonResponse = await client.GetAsync(
            BoundedWorkspaceHandoffExportEndpointMapper.JsonRoute,
            TestContext.CancellationTokenSource.Token);
        var json = await jsonResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var markdownResponse = await client.GetAsync(
            BoundedWorkspaceHandoffExportEndpointMapper.MarkdownRoute,
            TestContext.CancellationTokenSource.Token);
        var markdown = await markdownResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(HttpStatusCode.Conflict, jsonResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.Conflict, markdownResponse.StatusCode);
        Assert.IsNull(markdownResponse.Content.Headers.ContentDisposition);
        using var document = JsonDocument.Parse(json);
        Assert.IsFalse(document.RootElement.GetProperty("accepted").GetBoolean());
        Assert.IsFalse(document.RootElement.GetProperty("rootConfigured").GetBoolean());
        Assert.IsFalse(document.RootElement.GetProperty("realFilesystemRead").GetBoolean());
        Assert.AreEqual(
            "BLOCKED_BOUNDED_WORKSPACE_ROOT_NOT_CONFIGURED",
            document.RootElement.GetProperty("decision").GetString());
        StringAssert.Contains(markdown, "Export unavailable");
        StringAssert.Contains(markdown, "BLOCKED_BOUNDED_WORKSPACE_ROOT_NOT_CONFIGURED");
    }

    [TestMethod]
    public void ExportRejectsNonLoopbackAndProductionWithoutExplicitFlag()
    {
        var development = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        var production = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });

        Assert.IsFalse(BoundedWorkspaceHandoffExportEndpointMapper.IsRequestAllowed(development.Environment, null));
        Assert.IsFalse(BoundedWorkspaceHandoffExportEndpointMapper.IsRequestAllowed(
            development.Environment,
            IPAddress.Parse("192.0.2.40")));
        Assert.IsTrue(BoundedWorkspaceHandoffExportEndpointMapper.IsRequestAllowed(
            development.Environment,
            IPAddress.Loopback));
        Assert.IsFalse(BoundedWorkspaceHandoffExportEndpointMapper.IsRequestAllowed(
            production.Environment,
            IPAddress.Loopback));
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
        app.MapBoundedWorkspaceHandoffExport(app.Environment, rootProvider);
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
        var root = Path.Combine(Path.GetTempPath(), "nodal-workspace-export-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
