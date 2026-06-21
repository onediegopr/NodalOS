using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ProductUiMigrationAudit")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsProductUiMigrationAuditM606M608Tests
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
    public void ProductUiMigrationAuditArtifacts_Exist()
    {
        AssertExists("docs", "reports", "product-ui-entry-point-audit-m606-m608.md");
        AssertExists("artifacts", "agent-operations", "m608", "product-ui-entry-point-audit.json");
        AssertExists("artifacts", "agent-operations", "m608", "sidepanel-mission-control-migration-boundary.json");
        AssertExists("artifacts", "agent-operations", "m608", "visual-regression-qa-plan.json");
        AssertExists("artifacts", "agent-operations", "m608", "product-ui-migration-audit-summary.json");
        AssertExists("artifacts", "agent-operations", "m608", "product-ui-migration-boundary-preview.html");
    }

    [TestMethod]
    public void EntryPointAudit_ClassifiesEntrypointsRiskAndHandling()
    {
        var audit = Audit();

        foreach (var classification in new[]
        {
            "Productive UI entrypoint",
            "Static preview only",
            "Legacy/Quarantine",
            "Test fixture",
            "Artifact preview",
            "Documentation only",
            "Unknown"
        })
        {
            AssertContains(audit, classification);
        }

        foreach (var riskLevel in new[] { "Low", "Medium", "High", "Blocked" })
            AssertContains(audit, riskLevel);

        foreach (var handling in new[]
        {
            "update later",
            "do not touch",
            "static preview only",
            "requires audit",
            "requires visual regression",
            "requires security review",
            "requires no-runtime-coupling test"
        })
        {
            AssertContains(audit, handling);
        }

        AssertContains(audit, "browser-extension/onebrain-chrome-lab/sidepanel.html");
        AssertContains(audit, "browser-extension/onebrain-chrome-lab/sidepanel.css");
        AssertContains(audit, "browser-extension/onebrain-chrome-lab/sidepanel.js");
        AssertSafeOutput(audit);
    }

    [TestMethod]
    public void MigrationBoundary_IncludesSurfacesAllowedForbiddenChangesAndDecision()
    {
        var boundary = Boundary();

        foreach (var surface in new[]
        {
            "Mission Control",
            "Sidebar",
            "Current Mission Hero",
            "Timeline Summary",
            "Evidence Summary",
            "Decision Preview",
            "Advisor Note",
            "Consent/Capability Preview",
            "Runtime Status",
            "Activity Feed"
        })
        {
            AssertContains(boundary, surface);
        }

        AssertContains(boundary, "visual tokens");
        AssertContains(boundary, "static layout");
        AssertContains(boundary, "blocked-state anatomy");
        AssertContains(boundary, "no-op actions only");
        AssertContains(boundary, "Runtime calls");
        AssertContains(boundary, "Filesystem calls");
        AssertContains(boundary, "Provider Calls");
        AssertContains(boundary, "Capability enablement");
        AssertContains(boundary, "Source-of-truth promotion");
        AssertContains(boundary, "\"ReadyForProductUiMigrationPlanning\": true");
        AssertContains(boundary, "\"ReadyForDirectProductUiRewrite\": false");
        AssertContains(boundary, "\"ReadyForRuntimeCoupling\": false");
        AssertContains(boundary, "\"ReadyForFilesystemCoupling\": false");
        AssertContains(boundary, "\"ReadyForLlmProviderCoupling\": false");
        AssertSafeOutput(boundary);
    }

    [TestMethod]
    public void VisualRegressionQaPlan_IncludesResearchOsVerificationAndFailureCases()
    {
        var qa = QaPlan();

        AssertContains(qa, "Research OS style verification");
        AssertContains(qa, "blocked-state anatomy verification");
        AssertContains(qa, "no runtime coupling");
        AssertContains(qa, "no filesystem coupling");
        AssertContains(qa, "no provider/cloud coupling");
        AssertContains(qa, "no productive consent coupling");
        AssertContains(qa, "generic SaaS dashboard returns");
        AssertContains(qa, "runtime appears active");
        AssertContains(qa, "file access appears available");
        AssertContains(qa, "\"VisualRegressionPlanReady\": true");
        AssertContains(qa, "\"ReadyForBroadRewrite\": false");
        AssertContains(qa, "\"ReadyForRuntimeConnection\": false");
        AssertContains(qa, "\"ReadyForRealCapabilityConnection\": false");
        AssertSafeOutput(qa);
    }

    [TestMethod]
    public void Summary_ConfirmsAuditReadyAndNoProductMigration()
    {
        var summary = Summary();

        AssertContains(summary, "\"productUiEntrypointsIdentified\": true");
        AssertContains(summary, "\"sidepanelEntrypointsIdentified\": true");
        AssertContains(summary, "\"visualRegressionPlanReady\": true");
        AssertContains(summary, "\"readyForProductUiMigrationPlanning\": true");
        AssertContains(summary, "\"readyForDirectProductUiRewrite\": false");
        AssertContains(summary, "\"readyForRuntimeConnection\": false");
        AssertContains(summary, "\"readyForFilesystemFeature\": false");
        AssertContains(summary, "\"readyForProductiveConsent\": false");
        AssertContains(summary, "\"ProductUiMigrationImplemented\": false");
        AssertSafeOutput(summary);
    }

    [TestMethod]
    public void BoundaryPreview_IsStandaloneAndContainsNoRemoteOrScriptSurfaces()
    {
        var html = Preview();

        AssertContains(html, "Product UI Entry Point Audit");
        AssertContains(html, "Sidepanel candidates");
        AssertContains(html, "Allowed visual changes");
        AssertContains(html, "Migration boundary");
        AssertContains(html, "Why:");
        AssertContains(html, "Missing:");
        AssertContains(html, "Evidence required:");
        AssertContains(html, "User action:");
        AssertContains(html, "Disabled:");
        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "c" + "dn");
        AssertDoesNotContain(html, "tele" + "metry");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void NewProductUiMigrationAuditFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
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

    private static string Audit() => Read("artifacts", "agent-operations", "m608", "product-ui-entry-point-audit.json");
    private static string Boundary() => Read("artifacts", "agent-operations", "m608", "sidepanel-mission-control-migration-boundary.json");
    private static string QaPlan() => Read("artifacts", "agent-operations", "m608", "visual-regression-qa-plan.json");
    private static string Summary() => Read("artifacts", "agent-operations", "m608", "product-ui-migration-audit-summary.json");
    private static string Preview() => Read("artifacts", "agent-operations", "m608", "product-ui-migration-boundary-preview.html");
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
            Path.Combine("docs", "reports", "product-ui-entry-point-audit-m606-m608.md"),
            Path.Combine("artifacts", "agent-operations", "m608", "product-ui-entry-point-audit.json"),
            Path.Combine("artifacts", "agent-operations", "m608", "sidepanel-mission-control-migration-boundary.json"),
            Path.Combine("artifacts", "agent-operations", "m608", "visual-regression-qa-plan.json"),
            Path.Combine("artifacts", "agent-operations", "m608", "product-ui-migration-audit-summary.json"),
            Path.Combine("artifacts", "agent-operations", "m608", "product-ui-migration-boundary-preview.html")
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
