using System.Net;
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
[TestCategory("ProductHandoffExport")]
[TestCategory("MvpVerticalSlice")]
[TestCategory("AiriSelectiveRuntime")]
public sealed class MissionControlProductHandoffExportRouteTests
{
    [TestMethod]
    public void MarkdownRendererProjectsCanonicalStateWithoutAddingAuthority()
    {
        var snapshot = Snapshot();

        var first = MissionControlProductHandoffMarkdownRenderer.Render(snapshot);
        var second = MissionControlProductHandoffMarkdownRenderer.Render(snapshot);

        Assert.AreEqual(first, second);
        StringAssert.Contains(first, "# NODAL OS — Handoff de misión");
        StringAssert.Contains(first, "## Timeline");
        StringAssert.Contains(first, "Verified handoff and evidence");
        StringAssert.Contains(first, "evidence:execution:verified");
        StringAssert.Contains(first, "Autoridad de producto concedida: No");
        Assert.IsFalse(first.Contains("http://", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(first.Contains("https://", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task MarkdownRouteDownloadsRealMissionStateWithNoStoreHeaders()
    {
        var snapshot = Snapshot();
        await using var app = BuildApp(snapshot);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);
        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };

        using var response = await client.GetAsync(
            MissionControlProductHandoffExportEndpointMapper.MarkdownRoute,
            TestContext.CancellationTokenSource.Token);
        var markdown = await response.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(
            MissionControlProductHandoffExportEndpointMapper.MarkdownContentType,
            response.Content.Headers.ContentType?.ToString());
        StringAssert.Contains(
            response.Content.Headers.ContentDisposition?.ToString() ?? string.Empty,
            "nodal-os-handoff-mission-123.md");
        Assert.IsTrue(response.Headers.CacheControl?.NoStore == true);
        StringAssert.Contains(response.Headers.GetValues("Content-Security-Policy").Single(), "default-src 'none'");
        StringAssert.Contains(markdown, "Prepare a human-ready handoff");
        StringAssert.Contains(markdown, "NODAL_HANDOFF.md");
        StringAssert.Contains(markdown, "Conservar el resultado verificado");
    }

    [TestMethod]
    public async Task MarkdownRouteReturnsConflictUntilARealMissionExists()
    {
        var snapshot = Snapshot() with
        {
            MissionId = "mission:not-started",
            RealMissionDraft = false,
            MissionDraftPersisted = false,
            Timeline = [],
            EvidenceRefs = []
        };
        await using var app = BuildApp(snapshot);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);
        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };

        using var response = await client.GetAsync(
            MissionControlProductHandoffExportEndpointMapper.MarkdownRoute,
            TestContext.CancellationTokenSource.Token);
        var markdown = await response.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        Assert.IsNull(response.Content.Headers.ContentDisposition);
        StringAssert.Contains(markdown, "Handoff no disponible");
        StringAssert.Contains(markdown, "Creá una misión real");
    }

    [TestMethod]
    public void ExportBoundaryIsLoopbackOnly()
    {
        Assert.IsFalse(MissionControlProductHandoffExportEndpointMapper.IsRequestAllowed(null));
        Assert.IsFalse(MissionControlProductHandoffExportEndpointMapper.IsRequestAllowed(IPAddress.Parse("192.0.2.88")));
        Assert.IsTrue(MissionControlProductHandoffExportEndpointMapper.IsRequestAllowed(IPAddress.Loopback));
    }

    [TestMethod]
    public void PackagedSurfaceIncludesOnlyTheCanonicalProductHandoffRoute()
    {
        Assert.IsTrue(NodalOsDesktopLaunchRuntime.IsPackagedProductPath(
            MissionControlProductHandoffExportEndpointMapper.MarkdownRoute));
        Assert.IsFalse(NodalOsDesktopLaunchRuntime.IsPackagedProductPath(
            BoundedWorkspaceHandoffExportEndpointMapper.MarkdownRoute));
    }

    public TestContext TestContext { get; set; } = null!;

    private static WebApplication BuildApp(MissionControlProductShellSnapshot snapshot)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        MissionControlProductHandoffExportEndpointMapper.MapMissionControlProductHandoffExport(
            app,
            _ => ValueTask.FromResult(snapshot));
        return app;
    }

    private static string ServerAddress(WebApplication app)
    {
        var feature = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        var address = feature?.Addresses.SingleOrDefault();
        Assert.IsFalse(string.IsNullOrWhiteSpace(address), "Server address was not assigned.");
        return address!;
    }

    private static MissionControlProductShellSnapshot Snapshot() => new(
        Decision: "GO_MISSION_CONTROL_PRODUCT_SHELL_V1_READY",
        Accepted: true,
        ProductMode: "Local private beta",
        MissionId: "mission-123",
        RunId: "execution-123",
        Goal: "Prepare a human-ready handoff",
        MissionStatus: "Completed",
        ProgressPercent: 100,
        CurrentStep: "Verified handoff and evidence",
        ApprovalState: "Mission scope approved · execution verified",
        WorkspaceState: "Selected Local Workspace · protected + revalidated",
        WorkspaceSelected: true,
        WorkspacePersisted: true,
        WorkspaceId: "workspace-redacted",
        WorkspaceFingerprint: new string('a', 64),
        WorkspaceFilesRead: 4,
        RealMissionDraft: true,
        MissionDraftPersisted: true,
        ActionCandidateKind: "CreateTextFile",
        ActionCandidateTarget: "NODAL_HANDOFF.md",
        ActionExecutionEnabled: false,
        ActionApprovalAvailable: false,
        ActionExecutionState: "Completed",
        ActionExecuted: true,
        ActionVerified: true,
        ActionRollbackAvailable: true,
        ActionRolledBack: false,
        ByokConfigured: true,
        ModelConnectionVerified: true,
        ModelFallbackApplied: false,
        LogicalModel: "reasoning-primary",
        ActiveProvider: "local-openai-compatible",
        ActiveModel: "model-a",
        RecentFallback: null,
        BrowserRuntime: "Not configured",
        BrowserState: "Not evaluated",
        Timeline:
        [
            new MissionControlProductTimelineItem(
                Sequence: 1,
                EventId: "timeline:execution:verified",
                Title: "Verified handoff and evidence",
                Detail: "Exact bytes and SHA-256 verified.",
                State: "complete",
                EvidenceRefs: ["evidence:execution:verified"])
        ],
        Context: [],
        EvidenceRefs: ["evidence:workspace", "evidence:execution:verified"],
        Diagnostics: [],
        LocalOnly: true,
        ReadOnly: true,
        FixtureBacked: false,
        SecretsExcluded: true,
        ExternalIoUsed: false,
        NetworkUsed: false,
        ProductAuthorityGranted: false);
}
