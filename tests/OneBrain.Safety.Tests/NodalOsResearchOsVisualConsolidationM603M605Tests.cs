using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ResearchOsVisualConsolidation")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsResearchOsVisualConsolidationM603M605Tests
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
    public void ResearchOsVisualConsolidationArtifacts_Exist()
    {
        AssertExists("artifacts", "agent-operations", "m605", "research-os-visual-consolidation.json");
        AssertExists("artifacts", "agent-operations", "m605", "cross-surface-acceptance-pack.json");
        AssertExists("artifacts", "agent-operations", "m605", "research-os-visual-acceptance-summary.json");
        AssertExists("artifacts", "agent-operations", "m605", "product-ui-migration-readiness.json");
        AssertExists("artifacts", "agent-operations", "m605", "research-os-consolidated-preview.html");
        AssertExists("artifacts", "agent-operations", "m605", "product-ui-migration-readiness-preview.html");
        AssertExists("docs", "reports", "research-os-visual-consolidation-m603-m605.md");
    }

    [TestMethod]
    public void Consolidation_IncludesRequiredSurfacesAndSourceOfTruthStatus()
    {
        var consolidation = Consolidation();

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
            "Activity Feed",
            "Blocked States",
            "Readiness Gates",
            "Empty States"
        })
        {
            AssertContains(consolidation, surface);
        }

        AssertContains(consolidation, "\"purpose\"");
        AssertContains(consolidation, "\"sourceOfTruthStatus\"");
        AssertContains(consolidation, "Preview artifact only");
        AssertContains(consolidation, "\"canPromoteSourceOfTruth\": false");
        AssertSafeOutput(consolidation);
    }

    [TestMethod]
    public void AcceptancePack_IncludesRequiredDimensionsAndDecisionBoundaries()
    {
        var acceptance = Acceptance();

        AssertContains(acceptance, "Mission-centered clarity");
        AssertContains(acceptance, "Governance clarity");
        AssertContains(acceptance, "Evidence traceability");
        AssertContains(acceptance, "Advisor non-chatbot pattern");
        AssertContains(acceptance, "Agents non-autonomous pattern");
        AssertContains(acceptance, "Settings governance config");
        AssertContains(acceptance, "Blocked-state explanation");
        AssertContains(acceptance, "\"ResearchOsVisualBaselineReady\": true");
        AssertContains(acceptance, "\"ReadyForProductiveRuntime\": false");
        AssertContains(acceptance, "\"ReadyForFilesystemAccess\": false");
        AssertContains(acceptance, "\"ReadyForProductiveConsent\": false");
        AssertContains(acceptance, "\"ReadyForLlmProviderCalls\": false");
        AssertSafeOutput(acceptance);
    }

    [TestMethod]
    public void MigrationReadiness_IsReadinessOnlyAndBlocksProductConnections()
    {
        var readiness = Readiness();

        AssertContains(readiness, "\"isReadinessOnly\": true");
        AssertContains(readiness, "\"canModifyProductUi\": false");
        AssertContains(readiness, "\"canConnectRuntime\": false");
        AssertContains(readiness, "\"canConnectFilesystem\": false");
        AssertContains(readiness, "\"canConnectLlmProvider\": false");
        AssertContains(readiness, "\"canPersistProductiveState\": false");
        AssertContains(readiness, "\"canEnableCapabilities\": false");
        AssertContains(readiness, "doesNotRecommendDirectBroadUiRewrite");
        AssertContains(readiness, "Product UI Entry Point Audit");
        AssertSafeOutput(readiness);
    }

    [TestMethod]
    public void StaticHtmlPreviews_HaveRequiredContentAndNoNetworkOrScriptSurfaces()
    {
        var html = ConsolidatedPreview() + MigrationPreview();

        AssertContains(html, "Mission Control");
        AssertContains(html, "Timeline");
        AssertContains(html, "Evidence");
        AssertContains(html, "Decisions");
        AssertContains(html, "Advisor");
        AssertContains(html, "Consent");
        AssertContains(html, "Runtime");
        AssertContains(html, "Models");
        AssertContains(html, "Agents");
        AssertContains(html, "Settings");
        AssertContains(html, "Activity Feed");
        AssertContains(html, "Product UI Entry Point Audit");
        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "c" + "dn");
        AssertDoesNotContain(html, "tele" + "metry");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Boundary_NewResearchOsConsolidationFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
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

    private static string Consolidation() => Read("artifacts", "agent-operations", "m605", "research-os-visual-consolidation.json");
    private static string Acceptance() => Read("artifacts", "agent-operations", "m605", "cross-surface-acceptance-pack.json");
    private static string Readiness() => Read("artifacts", "agent-operations", "m605", "product-ui-migration-readiness.json");
    private static string ConsolidatedPreview() => Read("artifacts", "agent-operations", "m605", "research-os-consolidated-preview.html");
    private static string MigrationPreview() => Read("artifacts", "agent-operations", "m605", "product-ui-migration-readiness-preview.html");
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
            Path.Combine("docs", "reports", "research-os-visual-consolidation-m603-m605.md"),
            Path.Combine("artifacts", "agent-operations", "m605", "research-os-visual-consolidation.json"),
            Path.Combine("artifacts", "agent-operations", "m605", "cross-surface-acceptance-pack.json"),
            Path.Combine("artifacts", "agent-operations", "m605", "research-os-visual-acceptance-summary.json"),
            Path.Combine("artifacts", "agent-operations", "m605", "product-ui-migration-readiness.json"),
            Path.Combine("artifacts", "agent-operations", "m605", "research-os-consolidated-preview.html"),
            Path.Combine("artifacts", "agent-operations", "m605", "product-ui-migration-readiness-preview.html")
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
