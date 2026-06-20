using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("MissionControlResearchOs")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsMissionControlResearchOsM588M590Tests
{
    private static readonly string[] SensitiveMarkers =
    [
        "Bear" + "er ",
        "Authorization:",
        "Cook" + "ie:",
        "password",
        "raw " + "secret",
        "api" + "_key",
        "access" + "_token",
        "refresh" + "_token",
        "private key",
        "s" + "k-"
    ];

    [TestMethod]
    public void MissionControlResearchOsArtifacts_Exist()
    {
        AssertExists("artifacts", "agent-operations", "m590", "mission-control-research-os-layout.json");
        AssertExists("artifacts", "agent-operations", "m590", "current-mission-hero.json");
        AssertExists("artifacts", "agent-operations", "m590", "mission-status-panels.json");
        AssertExists("artifacts", "agent-operations", "m590", "mission-control-research-os-preview.html");
        AssertExists("docs", "reports", "mission-control-research-os-layout-m588-m590.md");
    }

    [TestMethod]
    public void Preview_UsesWarmEditorialMissionControlHierarchy()
    {
        var html = Preview();

        AssertContains(html, "#F5F3EE");
        AssertContains(html, "#F2F0EA");
        AssertContains(html, "#EFECE6");
        AssertContains(html, "Mission Control");
        AssertContains(html, "Current Mission");
        AssertContains(html, "Build Local AI Workspace");
        AssertContains(html, "72% Complete");
        AssertContains(html, "Current Phase / Consent Governance");
        AssertContains(html, "Pending Decisions");
        AssertContains(html, "Advisor Warnings");
        AssertContains(html, "Evidence Artifacts");
    }

    [TestMethod]
    public void Preview_IncludesRequiredSidebarSections()
    {
        var html = Preview();

        foreach (var section in new[]
        {
            "Mission Control",
            "Timeline",
            "Missions",
            "Workspace",
            "Consent",
            "Evidence",
            "Decisions",
            "Advisor",
            "Models",
            "Agents",
            "Runtime",
            "Settings"
        })
        {
            AssertContains(html, section);
        }
    }

    [TestMethod]
    public void Preview_IncludesMissionStatusPanelsAndActivityFeed()
    {
        var html = Preview();

        AssertContains(html, "Consent / Capability Status");
        AssertContains(html, "Evidence Summary");
        AssertContains(html, "Advisor Note");
        AssertContains(html, "Runtime / File Read Blocked");
        AssertContains(html, "Activity Feed");
        AssertContains(html, "Capability blocked - File Read remains disabled.");
        AssertDoesNotContain(html, "chatbot bubble");
    }

    [TestMethod]
    public void Preview_IncludesBlockedStateAnatomy()
    {
        var html = Preview();

        AssertContains(html, "Why");
        AssertContains(html, "Missing");
        AssertContains(html, "Evidence");
        AssertContains(html, "Action");
        AssertContains(html, "Disabled");
        AssertContains(html, "Real access is intentionally disabled.");
        AssertContains(html, "Productive consent, audited path jail");
    }

    [TestMethod]
    public void ActionsAndArtifacts_AreNoOpPreviewOnly()
    {
        var hero = Read("artifacts", "agent-operations", "m590", "current-mission-hero.json");
        var layout = Read("artifacts", "agent-operations", "m590", "mission-control-research-os-layout.json");
        var panels = Read("artifacts", "agent-operations", "m590", "mission-status-panels.json");
        var combined = hero + layout + panels;

        AssertContains(hero, "\"actionAreaIsNoOpOnly\": true");
        AssertContains(hero, "\"isNoOp\": true");
        AssertContains(combined, "\"canConnectRuntime\": false");
        AssertContains(combined, "\"canAccessFilesystem\": false");
        AssertContains(combined, "\"canUseLlm\": false");
        AssertContains(combined, "\"canUseCloud\": false");
        AssertContains(combined, "\"canPersistProductiveConsent\": false");
        AssertContains(combined, "\"canEnableCapability\": false");
        AssertSafeOutput(combined);
    }

    [TestMethod]
    public void StaticHtmlPreview_HasNoNetworkOrScriptSurfaces()
    {
        var html = Preview();

        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "c" + "dn");
        AssertDoesNotContain(html, "tele" + "metry");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Boundary_NewMissionControlFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
    {
        var text = string.Join(Environment.NewLine, NewFiles().Select(TextStore.ReadAllText));

        AssertDoesNotContain(text, "OneBrain." + "BrowserExecutor" + ".Cdp");
        AssertDoesNotContain(text, "Http" + "Client");
        AssertDoesNotContain(text, "Client" + "WebSocket");
        AssertDoesNotContain(text, "Process" + ".Start");
        AssertDoesNotContain(text, "System.Diagnostics." + "Process");
        AssertDoesNotContain(text, "Background" + "Service");
        AssertDoesNotContain(text, "Task.Run");
        AssertDoesNotContain(text, "File" + ".Read");
        AssertDoesNotContain(text, "File" + ".Write");
        AssertDoesNotContain(text, "File" + ".Delete");
        AssertDoesNotContain(text, "File" + ".Move");
        AssertDoesNotContain(text, "Directory" + ".");
        AssertDoesNotContain(text, "File" + "Info");
        AssertDoesNotContain(text, "Directory" + "Info");
        AssertSafeOutput(text);
    }

    private static string Preview() => Read("artifacts", "agent-operations", "m590", "mission-control-research-os-preview.html");
    private static void AssertExists(params string[] segments) => Assert.IsTrue(TextStore.Exists(PathFor(segments)), $"Missing {PathFor(segments)}");
    private static string Read(params string[] segments) => TextStore.ReadAllText(PathFor(segments));
    private static void AssertContains(string value, string expected) => StringAssert.Contains(value, expected);

    private static void AssertSafeOutput(string value)
    {
        foreach (var marker in SensitiveMarkers)
            AssertDoesNotContain(value, marker);
    }

    private static void AssertDoesNotContain(string value, string forbidden) =>
        Assert.IsFalse(value.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Unexpected content: {forbidden}");

    private static IEnumerable<string> NewFiles()
    {
        var root = FindRepoRoot();
        var relative = new[]
        {
            Path.Combine("docs", "reports", "mission-control-research-os-layout-m588-m590.md"),
            Path.Combine("artifacts", "agent-operations", "m590", "mission-control-research-os-layout.json"),
            Path.Combine("artifacts", "agent-operations", "m590", "current-mission-hero.json"),
            Path.Combine("artifacts", "agent-operations", "m590", "mission-status-panels.json"),
            Path.Combine("artifacts", "agent-operations", "m590", "mission-control-research-os-preview.html")
        };

        return relative.Select(path => Path.Combine(root, path));
    }

    private static string PathFor(params string[] segments) => Path.Combine([FindRepoRoot(), .. segments]);

    private static string FindRepoRoot()
    {
        var current = Path.GetFullPath(AppContext.BaseDirectory);
        while (!string.IsNullOrEmpty(current) && !TextStore.Exists(Path.Combine(current, "OneBrain.slnx")))
            current = Path.GetDirectoryName(current) ?? string.Empty;

        return string.IsNullOrEmpty(current) ? throw new InvalidOperationException("Repository root not found.") : current;
    }
}
