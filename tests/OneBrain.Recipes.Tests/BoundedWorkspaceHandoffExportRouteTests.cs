using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Core.Workspace;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("MvpVerticalSlice")]
[TestCategory("WorkspaceUnderstanding")]
[TestCategory("ExpertAdvisor")]
public sealed class BoundedWorkspaceHandoffExportRouteTests
{
    [TestMethod]
    public async Task VerifiedWorkspaceExportsSanitizedAdvisorHandoffWithoutMutation()
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
            Assert.AreEqual("GO_BOUNDED_WORKSPACE_ADVISOR_HANDOFF_EXPORT_READY", payload.GetProperty("decision").GetString());
            Assert.AreEqual(64, payload.GetProperty("evidenceDigest").GetString()?.Length);
            Assert.IsTrue(payload.GetProperty("deterministic").GetBoolean());
            Assert.IsFalse(payload.GetProperty("containsRawPayload").GetBoolean());
            Assert.IsFalse(payload.GetProperty("containsExternalResource").GetBoolean());
            Assert.IsFalse(payload.GetProperty("isAuthoritative").GetBoolean());
            Assert.IsFalse(payload.GetProperty("executable").GetBoolean());
            Assert.AreEqual("GO_EXPERT_ADVISOR_SUGGESTIONS_READY", payload.GetProperty("advisorDecision").GetString());
            Assert.AreEqual("Balanced", payload.GetProperty("advisorProfile").GetString());
            Assert.AreEqual(50, payload.GetProperty("advisorInterventionLevel").GetInt32());
            Assert.IsTrue(payload.GetProperty("advisorSuggestionCount").GetInt32() > 0);
            Assert.IsTrue(payload.GetProperty("advisorFindingsIncluded").GetBoolean());
            Assert.IsTrue(payload.GetProperty("advisorNonExecutor").GetBoolean());
            var findings = payload.GetProperty("advisorFindings");
            Assert.IsTrue(findings.GetArrayLength() > 0);
            foreach (var finding in findings.EnumerateArray())
            {
                Assert.IsTrue(finding.GetProperty("nonExecutable").GetBoolean());
                Assert.IsFalse(finding.GetProperty("canAuthorizeExecution").GetBoolean());
            }
            Assert.IsTrue(payload.GetProperty("realFilesystemRead").GetBoolean());
            Assert.IsFalse(payload.GetProperty("filesystemMutationAllowed").GetBoolean());
            Assert.IsFalse(payload.GetProperty("networkUsed").GetBoolean());
            Assert.IsFalse(payload.GetProperty("productAuthorityGranted").GetBoolean());
            Assert.IsTrue(payload.GetProperty("evidenceRefs").GetArrayLength() > 0);
            StringAssert.Contains(payload.GetProperty("markdownRedacted").GetString() ?? string.Empty, "NODAL OS Bounded Workspace Handoff");

            StringAssert.Contains(markdown, "# NODAL OS Bounded Workspace Handoff");
            StringAssert.Contains(markdown, "## Evidence");
            StringAssert.Contains(markdown, "## Expert Advisor findings");
            StringAssert.Contains(markdown, "Profile: Balanced (50/100)");
            StringAssert.Contains(markdown, "The advisor is read-only, non-executing and cannot authorize work.");
            StringAssert.Contains(markdown, "Secret-like values were redacted");
            StringAssert.Contains(markdown, "## Next safe step");
            Assert.IsTrue(
                markdown.IndexOf("## Expert Advisor findings", StringComparison.Ordinal) <
                markdown.IndexOf("## Next safe step", StringComparison.Ordinal));
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
    public async Task SilentAdvisorExportRetainsOnlyHighSeverityFinding()
    {
        var root = CreateRoot();
        var fakeSecret = "s" + "k-export-silent-secret-value-123456789";
        try
        {
            await WriteAsync(root, "src/Program.cs", $"var secret = \"{fakeSecret}\";");
            await using var app = BuildApp(
                Environments.Development,
                () => root,
                () => new BoundedWorkspaceAdvisorSettings(BoundedWorkspaceAdvisorProfile.Silent, 100));
            await app.StartAsync(TestContext.CancellationTokenSource.Token);
            using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };

            using var response = await client.GetAsync(
                BoundedWorkspaceHandoffExportEndpointMapper.JsonRoute,
                TestContext.CancellationTokenSource.Token);
            var json = await response.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            using var document = JsonDocument.Parse(json);
            var payload = document.RootElement;
            Assert.AreEqual("Silent", payload.GetProperty("advisorProfile").GetString());
            Assert.AreEqual(100, payload.GetProperty("advisorInterventionLevel").GetInt32());
            Assert.AreEqual(1, payload.GetProperty("advisorSuggestionCount").GetInt32());
            var finding = payload.GetProperty("advisorFindings")[0];
            Assert.AreEqual("Audit", finding.GetProperty("category").GetString());
            Assert.AreEqual("High", finding.GetProperty("severity").GetString());
            StringAssert.Contains(finding.GetProperty("title").GetString() ?? string.Empty, "Secret-like values were redacted");
            Assert.IsFalse(json.Contains(fakeSecret, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(json.Contains(root, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task MissingRootReturnsConflictWithoutAdvisorAttachmentOrFilesystemRead()
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
        Assert.IsTrue(document.RootElement.GetProperty("advisorNonExecutor").GetBoolean());
        Assert.IsFalse(document.RootElement.GetProperty("advisorFindingsIncluded").GetBoolean());
        Assert.AreEqual(0, document.RootElement.GetProperty("advisorSuggestionCount").GetInt32());
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

    private static WebApplication BuildApp(
        string environmentName,
        Func<string?> rootProvider,
        Func<BoundedWorkspaceAdvisorSettings>? advisorSettingsProvider = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        app.MapBoundedWorkspaceHandoffExport(
            app.Environment,
            rootProvider,
            advisorSettingsProvider);
        return app;
    }

    private static string ServerAddress(WebApplication app)
    {
        var feature = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        var address = feature?.Addresses.SingleOrDefault();
        Assert.IsFalse(string.IsNullOrWhiteSpace(address), "Server address was not assigned.");
        return address!;
    }

    private static async Task WriteAsync(string root, string relativePath, string content)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, content);
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "nodal-workspace-export-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
