using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("VisualFoundationPhase1")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsVisualFoundationPhase1M585M587Tests
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
    public void VisualTokensDocAndArtifact_Exist()
    {
        AssertExists("docs", "design", "nodal-os-visual-tokens-phase-1.md");
        AssertExists("artifacts", "agent-operations", "m587", "visual-foundation-tokens.json");
    }

    [TestMethod]
    public void VisualTokens_IncludePaletteTypographyAndLayoutStrategy()
    {
        var doc = Read("docs", "design", "nodal-os-visual-tokens-phase-1.md");
        var artifact = Read("artifacts", "agent-operations", "m587", "visual-foundation-tokens.json");
        var combined = doc + artifact;

        AssertContains(combined, "#F5F3EE");
        AssertContains(combined, "#F2F0EA");
        AssertContains(combined, "#EFECE6");
        AssertContains(combined, "#5B6CFF");
        AssertContains(combined, "#7265FF");
        AssertContains(combined, "Georgia");
        AssertContains(combined, "Inter");
        AssertContains(combined, "--nos-sidebar-width");
        AssertContains(combined, "--nos-focus-ring");
        AssertSafeOutput(combined);
    }

    [TestMethod]
    public void ShellPreview_ExistsAndIncludesMissionControlCurrentMissionAndSidebar()
    {
        var html = Preview();

        AssertContains(html, "Mission Control");
        AssertContains(html, "Current Mission");
        AssertContains(html, "Build Local AI Workspace");
        AssertContains(html, "Timeline");
        AssertContains(html, "Missions");
        AssertContains(html, "Workspace");
        AssertContains(html, "Consent");
        AssertContains(html, "Evidence");
        AssertContains(html, "Decisions");
        AssertContains(html, "Advisor");
        AssertContains(html, "Models");
        AssertContains(html, "Agents");
        AssertContains(html, "Runtime");
        AssertContains(html, "Settings");
    }

    [TestMethod]
    public void ShellPreview_IncludesMandatoryCardsAndActivityFeed()
    {
        var html = Preview();

        AssertContains(html, "Consent / Capability");
        AssertContains(html, "Evidence");
        AssertContains(html, "Decision");
        AssertContains(html, "Advisor Note");
        AssertContains(html, "Activity Feed");
        AssertContains(html, "Runtime Local-First Status");
        AssertDoesNotContain(html, "chatbot bubble");
    }

    [TestMethod]
    public void ShellPreview_IncludesBlockedStateAnatomy()
    {
        var html = Preview();

        AssertContains(html, "Why blocked");
        AssertContains(html, "Missing");
        AssertContains(html, "Evidence");
        AssertContains(html, "Action");
        AssertContains(html, "Disabled");
    }

    [TestMethod]
    public void StaticVisualQaPack_ExistsAndConfirmsPhaseOneBoundaries()
    {
        var qa = Read("artifacts", "agent-operations", "m587", "static-visual-qa-pack.json");
        var summary = Read("artifacts", "agent-operations", "m587", "visual-foundation-phase-1-summary.json");

        AssertContains(qa, "not generic SaaS dashboard");
        AssertContains(qa, "mission centered");
        AssertContains(qa, "no runtime changes");
        AssertContains(qa, "no operational access");
        AssertContains(qa, "no LLM activity");
        AssertContains(qa, "no cloud activity");
        AssertContains(qa, "no productive consent");
        AssertContains(summary, "\"canChangeRuntime\": false");
        AssertContains(summary, "\"canAccessFilesystem\": false");
        AssertContains(summary, "\"canUseCloud\": false");
        AssertSafeOutput(qa + summary);
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
    public void Boundary_NewVisualFoundationFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
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

    private static string Preview() => Read("artifacts", "agent-operations", "m587", "research-os-shell-preview.html");
    private static void AssertExists(params string[] segments) => Assert.IsTrue(TextStore.Exists(PathFor(segments)), $"Missing {PathFor(segments)}");
    private static string Read(params string[] segments) => TextStore.ReadAllText(PathFor(segments));
    private static void AssertContains(string value, string expected) => StringAssert.Contains(value, expected);

    private static void AssertSafeOutput(string value)
    {
        foreach (var marker in SensitiveMarkers)
            AssertDoesNotContain(value, marker);

        AssertDoesNotContain(value, "NEXA");
        AssertDoesNotContain(value, "NODRIX");
        AssertDoesNotContain(value, "HOTEP");
    }

    private static void AssertDoesNotContain(string value, string forbidden) =>
        Assert.IsFalse(value.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Unexpected content: {forbidden}");

    private static IEnumerable<string> NewFiles()
    {
        var root = FindRepoRoot();
        var relative = new[]
        {
            Path.Combine("docs", "design", "nodal-os-visual-tokens-phase-1.md"),
            Path.Combine("docs", "reports", "visual-foundation-phase-1-m585-m587.md"),
            Path.Combine("artifacts", "agent-operations", "m587", "visual-foundation-tokens.json"),
            Path.Combine("artifacts", "agent-operations", "m587", "research-os-shell-preview.html"),
            Path.Combine("artifacts", "agent-operations", "m587", "static-visual-qa-pack.json"),
            Path.Combine("artifacts", "agent-operations", "m587", "visual-foundation-phase-1-summary.json")
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
