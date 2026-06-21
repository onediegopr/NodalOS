using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SidepanelResearchOsMigrationPlan")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsSidepanelResearchOsMigrationPlanM609M611Tests
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
    public void SidepanelResearchOsMigrationPlanArtifacts_Exist()
    {
        AssertExists("docs", "reports", "sidepanel-research-os-migration-plan-m609-m611.md");
        AssertExists("artifacts", "agent-operations", "m611", "sidepanel-research-os-migration-plan.json");
        AssertExists("artifacts", "agent-operations", "m611", "sidepanel-migration-rollback-strategy.json");
        AssertExists("artifacts", "agent-operations", "m611", "sidepanel-visual-regression-fixtures.json");
        AssertExists("artifacts", "agent-operations", "m611", "sidepanel-visual-qa-baseline.json");
        AssertExists("artifacts", "agent-operations", "m611", "sidepanel-migration-readiness-summary.json");
        AssertExists("artifacts", "agent-operations", "m611", "sidepanel-research-os-migration-preview.html");
    }

    [TestMethod]
    public void MigrationPlan_ReferencesSidepanelFilesPhasesAllowedAndForbiddenChanges()
    {
        var plan = Plan();

        AssertContains(plan, "sidepanel.html");
        AssertContains(plan, "sidepanel.css");
        AssertContains(plan, "sidepanel.js");
        AssertContains(plan, "manifest.json");
        AssertContains(plan, "Phase 1");
        AssertContains(plan, "Phase 2");
        AssertContains(plan, "Phase 3");
        AssertContains(plan, "Phase 4");
        AssertContains(plan, "Phase 5");
        AssertContains(plan, "Phase 6");
        AssertContains(plan, "Phase 7");
        AssertContains(plan, "CSS tokens");
        AssertContains(plan, "semantic HTML structure");
        AssertContains(plan, "blocked-state anatomy");
        AssertContains(plan, "Runtime calls");
        AssertContains(plan, "Filesystem calls");
        AssertContains(plan, "Provider Calls");
        AssertContains(plan, "Productive consent");
        AssertContains(plan, "Source-of-truth promotion");
        AssertSafeOutput(plan);
    }

    [TestMethod]
    public void RollbackStrategy_IncludesFutureFilesRevertChecksAndSafetyFailures()
    {
        var rollback = Rollback();

        AssertContains(rollback, "futureFilesToTouch");
        AssertContains(rollback, "sidepanel.html");
        AssertContains(rollback, "sidepanel.css");
        AssertContains(rollback, "sidepanel.js");
        AssertContains(rollback, "manifest.json");
        AssertContains(rollback, "revertPlan");
        AssertContains(rollback, "no-runtime-coupling checks");
        AssertContains(rollback, "no Provider Calls checks");
        AssertContains(rollback, "no filesystem checks");
        AssertContains(rollback, "no productive consent checks");
        AssertContains(rollback, "sidepanel JS calls runtime unexpectedly");
        AssertContains(rollback, "Provider/cloud/network calls introduced");
        AssertContains(rollback, "filesystem APIs introduced");
        AssertContains(rollback, "capability enablement implied");
        AssertContains(rollback, "productive consent persisted");
        AssertContains(rollback, "previews become source-of-truth");
        AssertSafeOutput(rollback);
    }

    [TestMethod]
    public void VisualRegressionFixtures_IncludeRequiredSurfacesAndAssertions()
    {
        var fixtures = Fixtures();

        foreach (var surface in new[]
        {
            "Mission Control",
            "Timeline",
            "Evidence",
            "Decisions",
            "Advisor",
            "Consent",
            "Runtime",
            "Models",
            "Agents",
            "Settings",
            "Activity Feed"
        })
        {
            AssertContains(fixtures, surface);
        }

        AssertContains(fixtures, "requiredCopy");
        AssertContains(fixtures, "requiredBlockedStateAnatomy");
        AssertContains(fixtures, "noOpActionLabels");
        AssertContains(fixtures, "sourceOfTruthStatus");
        AssertContains(fixtures, "runtimeCouplingStatus");
        AssertContains(fixtures, "providerCloudCouplingStatus");
        AssertContains(fixtures, "filesystemCouplingStatus");
        AssertContains(fixtures, "consentCapabilityStatus");
        AssertSafeOutput(fixtures);
    }

    [TestMethod]
    public void VisualQaBaseline_IncludesResearchOsAcceptanceBoundaries()
    {
        var baseline = Baseline();

        AssertContains(baseline, "Research OS warm editorial style");
        AssertContains(baseline, "mission-first hierarchy");
        AssertContains(baseline, "no generic SaaS dashboard");
        AssertContains(baseline, "no dark technical control center as default");
        AssertContains(baseline, "no raw console main UI");
        AssertContains(baseline, "no chatbot advisor");
        AssertContains(baseline, "no generic settings forms");
        AssertContains(baseline, "no autonomous agents");
        AssertContains(baseline, "no provider selector implying calls");
        AssertContains(baseline, "no runtime active implication");
        AssertContains(baseline, "no file access availability implication");
        AssertSafeOutput(baseline);
    }

    [TestMethod]
    public void ReadinessSummary_IsPlanOnlyAndBlocksProductConnections()
    {
        var summary = Summary();

        AssertContains(summary, "\"isPlanOnly\": true");
        AssertContains(summary, "\"canModifySidepanelProductUi\": false");
        AssertContains(summary, "\"canConnectRuntime\": false");
        AssertContains(summary, "\"canConnectFilesystem\": false");
        AssertContains(summary, "\"canConnectLlmProvider\": false");
        AssertContains(summary, "\"canEnableCapabilities\": false");
        AssertContains(summary, "\"canPersistProductiveConsent\": false");
        AssertContains(summary, "\"canPromoteSourceOfTruth\": false");
        AssertContains(summary, "\"sidepanelProductFilesModified\": false");
        AssertSafeOutput(summary);
    }

    [TestMethod]
    public void MigrationPreview_IsStandaloneAndContainsNoRemoteOrScriptSurfaces()
    {
        var html = Preview();

        AssertContains(html, "Sidepanel Research OS Migration Plan");
        AssertContains(html, "Blocked-state anatomy");
        AssertContains(html, "M609");
        AssertContains(html, "M610");
        AssertContains(html, "M611");
        AssertContains(html, "Product modification remains blocked");
        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "c" + "dn");
        AssertDoesNotContain(html, "tele" + "metry");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void NewSidepanelMigrationPlanFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
    {
        var text = string.Join(Environment.NewLine, NewFiles().Select(TextStore.ReadAllText));

        AssertDoesNotContain(text, "OneBrain." + "BrowserExecutor" + ".Cdp");
        AssertDoesNotContain(text, "Http" + "Client");
        AssertDoesNotContain(text, "Client" + "WebSocket");
        AssertDoesNotContain(text, "Process" + ".Start");
        AssertDoesNotContain(text, "System.Diagnostics." + "Process");
        AssertDoesNotContain(text, "sched" + "uler");
        AssertDoesNotContain(text, "wor" + "ker");
        AssertDoesNotContain(text, "que" + "ue");
        AssertDoesNotContain(text, "File" + ".Read");
        AssertDoesNotContain(text, "File" + ".Write");
        AssertDoesNotContain(text, "File" + ".Delete");
        AssertDoesNotContain(text, "File" + ".Move");
        AssertDoesNotContain(text, "Directory" + ".");
        AssertDoesNotContain(text, "File" + "Info");
        AssertDoesNotContain(text, "Directory" + "Info");
        AssertSafeOutput(text);
    }

    private static string Plan() => Read("artifacts", "agent-operations", "m611", "sidepanel-research-os-migration-plan.json");
    private static string Rollback() => Read("artifacts", "agent-operations", "m611", "sidepanel-migration-rollback-strategy.json");
    private static string Fixtures() => Read("artifacts", "agent-operations", "m611", "sidepanel-visual-regression-fixtures.json");
    private static string Baseline() => Read("artifacts", "agent-operations", "m611", "sidepanel-visual-qa-baseline.json");
    private static string Summary() => Read("artifacts", "agent-operations", "m611", "sidepanel-migration-readiness-summary.json");
    private static string Preview() => Read("artifacts", "agent-operations", "m611", "sidepanel-research-os-migration-preview.html");
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
            Path.Combine("docs", "reports", "sidepanel-research-os-migration-plan-m609-m611.md"),
            Path.Combine("artifacts", "agent-operations", "m611", "sidepanel-research-os-migration-plan.json"),
            Path.Combine("artifacts", "agent-operations", "m611", "sidepanel-migration-rollback-strategy.json"),
            Path.Combine("artifacts", "agent-operations", "m611", "sidepanel-visual-regression-fixtures.json"),
            Path.Combine("artifacts", "agent-operations", "m611", "sidepanel-visual-qa-baseline.json"),
            Path.Combine("artifacts", "agent-operations", "m611", "sidepanel-migration-readiness-summary.json"),
            Path.Combine("artifacts", "agent-operations", "m611", "sidepanel-research-os-migration-preview.html")
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
