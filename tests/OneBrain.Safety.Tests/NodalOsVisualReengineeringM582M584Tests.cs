using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("VisualReengineering")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsVisualReengineeringM582M584Tests
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
    public void VisualFoundationDocuments_Exist()
    {
        AssertExists("docs", "reports", "visual-reengineering-audit-m582-m584.md");
        AssertExists("docs", "design", "nodal-os-research-mission-design-system.md");
        AssertExists("docs", "design", "nodal-os-research-mission-screen-map.md");
        AssertExists("docs", "design", "nodal-os-visual-migration-plan.md");
    }

    [TestMethod]
    public void VisualFoundationArtifacts_Exist()
    {
        AssertExists("artifacts", "agent-operations", "m584", "visual-reengineering-audit.json");
        AssertExists("artifacts", "agent-operations", "m584", "research-os-design-system-foundation.json");
        AssertExists("artifacts", "agent-operations", "m584", "visual-reengineering-migration-plan.json");
        AssertExists("artifacts", "agent-operations", "m584", "research-mission-control-concept-preview.html");
    }

    [TestMethod]
    public void DesignSystem_IncludesWarmEditorialPaletteAndTypography()
    {
        var design = Read("docs", "design", "nodal-os-research-mission-design-system.md");

        AssertContains(design, "#F5F3EE");
        AssertContains(design, "#F2F0EA");
        AssertContains(design, "#EFECE6");
        AssertContains(design, "#171717");
        AssertContains(design, "#5C5C5C");
        AssertContains(design, "#DDD8D0");
        AssertContains(design, "#5B6CFF");
        AssertContains(design, "#7265FF");
        AssertContains(design, "serif headings");
        AssertContains(design, "sans UI");
        AssertSafeOutput(design);
    }

    [TestMethod]
    public void DesignSystem_IncludesMissionCenteredUxAndRequiredComponents()
    {
        var design = Read("docs", "design", "nodal-os-research-mission-design-system.md");

        AssertContains(design, "Local Mission Control");
        AssertContains(design, "active mission is the center");
        AssertContains(design, "Permission / Capability Card");
        AssertContains(design, "Evidence Card");
        AssertContains(design, "Advisor Note");
        AssertContains(design, "Do not render as chatbot bubble");
        AssertContains(design, "Blocked State Panel");
        AssertContains(design, "why blocked");
    }

    [TestMethod]
    public void ScreenMap_IncludesAllMandatoryScreens()
    {
        var screenMap = Read("docs", "design", "nodal-os-research-mission-screen-map.md");
        var screens = new[]
        {
            "Mission Control",
            "Timeline",
            "Mission Detail",
            "Consent",
            "Evidence",
            "Decisions",
            "Advisor",
            "Models",
            "Agents",
            "Runtime",
            "Settings",
            "Activity Feed",
            "Readiness Gates",
            "Error / Blocked State",
            "Empty States"
        };

        foreach (var screen in screens)
            AssertContains(screenMap, screen);
    }

    [TestMethod]
    public void MigrationPlan_IncludesFivePhasesAndAcceptanceCriteria()
    {
        var plan = Read("docs", "design", "nodal-os-visual-migration-plan.md");

        AssertContains(plan, "Phase 1 - Visual Foundation");
        AssertContains(plan, "Phase 2 - Mission Control");
        AssertContains(plan, "Phase 3 - Core Internal Screens");
        AssertContains(plan, "Phase 4 - Advisor And Activity Feed");
        AssertContains(plan, "Phase 5 - Polish And Consistency");
        AssertContains(plan, "not generic SaaS dashboard");
        AssertContains(plan, "mission centered");
        AssertContains(plan, "Future files");
        AssertContains(plan, "Future validations");
    }

    [TestMethod]
    public void StaticPreview_HasNoExternalScriptsOrNetworkResources()
    {
        var html = Read("artifacts", "agent-operations", "m584", "research-mission-control-concept-preview.html");

        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "cdn");
        AssertDoesNotContain(html, "tele" + "metry");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Artifacts_DeclareFoundationAndNoRuntimeAuthority()
    {
        var audit = Read("artifacts", "agent-operations", "m584", "visual-reengineering-audit.json");
        var design = Read("artifacts", "agent-operations", "m584", "research-os-design-system-foundation.json");
        var migration = Read("artifacts", "agent-operations", "m584", "visual-reengineering-migration-plan.json");

        AssertContains(audit, "\"canChangeRuntime\": false");
        AssertContains(audit, "\"canAccessFilesystem\": false");
        AssertContains(design, "\"Research OS\"");
        AssertContains(design, "\"permission capability card\"");
        AssertContains(migration, "\"not generic SaaS dashboard\"");
        AssertContains(migration, "\"canImplementMassiveRedesign\": false");
        AssertSafeOutput(audit + design + migration);
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
            Path.Combine("docs", "reports", "visual-reengineering-audit-m582-m584.md"),
            Path.Combine("docs", "design", "nodal-os-research-mission-design-system.md"),
            Path.Combine("docs", "design", "nodal-os-research-mission-screen-map.md"),
            Path.Combine("docs", "design", "nodal-os-visual-migration-plan.md"),
            Path.Combine("artifacts", "agent-operations", "m584", "visual-reengineering-audit.json"),
            Path.Combine("artifacts", "agent-operations", "m584", "research-os-design-system-foundation.json"),
            Path.Combine("artifacts", "agent-operations", "m584", "visual-reengineering-migration-plan.json"),
            Path.Combine("artifacts", "agent-operations", "m584", "research-mission-control-concept-preview.html")
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
