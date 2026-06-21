using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SidepanelTokenDryRun")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsSidepanelTokenDryRunM612M614Tests
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
    public void SidepanelTokenDryRunArtifacts_Exist()
    {
        AssertExists("docs", "reports", "sidepanel-token-dry-run-m612-m614.md");
        AssertExists("artifacts", "agent-operations", "m614", "sidepanel-token-integration-dry-run.json");
        AssertExists("artifacts", "agent-operations", "m614", "sidepanel-diff-patch-plan.json");
        AssertExists("artifacts", "agent-operations", "m614", "no-runtime-coupling-test-plan.json");
        AssertExists("artifacts", "agent-operations", "m614", "sidepanel-token-dry-run-readiness-summary.json");
        AssertExists("artifacts", "agent-operations", "m614", "sidepanel-token-dry-run-preview.html");
        AssertExists("artifacts", "agent-operations", "m614", "sidepanel-diff-patch-plan-preview.html");
    }

    [TestMethod]
    public void DryRun_MapsResearchOsTokensToSidepanelCandidatesAndCurrentStyles()
    {
        var dryRun = DryRun();

        foreach (var token in new[]
        {
            "--nos-color-bg",
            "--nos-color-bg-muted",
            "--nos-color-bg-soft",
            "--nos-color-text",
            "--nos-color-text-muted",
            "--nos-color-border",
            "--nos-color-accent",
            "--nos-color-success",
            "--nos-color-warning",
            "--nos-color-danger",
            "--nos-focus-ring",
            "--nos-radius-panel",
            "--nos-space-panel",
            "--nos-card-padding",
            "--nos-sidebar-width",
            "--nos-heading-font",
            "--nos-ui-font"
        })
        {
            AssertContains(dryRun, token);
        }

        AssertContains(dryRun, "currentStyleMapping");
        AssertContains(dryRun, "--bg");
        AssertContains(dryRun, ".surface");
        AssertContains(dryRun, ".tabs");
        AssertContains(dryRun, ".timeline-card");
        AssertContains(dryRun, ".log-pane");
        AssertContains(dryRun, "\"sidepanelProductFilesModified\": false");
        AssertSafeOutput(dryRun);
    }

    [TestMethod]
    public void PatchPlan_IncludesPatchOneThroughEightAndBlocksProductCoupling()
    {
        var patchPlan = PatchPlan();

        foreach (var patch in new[]
        {
            "Patch 1",
            "Patch 2",
            "Patch 3",
            "Patch 4",
            "Patch 5",
            "Patch 6",
            "Patch 7",
            "Patch 8"
        })
        {
            AssertContains(patchPlan, patch);
        }

        AssertContains(patchPlan, "\"CanConnectRuntime\": false");
        AssertContains(patchPlan, "\"CanEnableCapabilities\": false");
        AssertContains(patchPlan, "\"CanCallProvider\": false");
        AssertContains(patchPlan, "\"CanUseFilesystem\": false");
        AssertContains(patchPlan, "\"CanTouchManifest\": false");
        AssertContains(patchPlan, "noRuntimeCouplingAssertion");
        AssertSafeOutput(patchPlan);
    }

    [TestMethod]
    public void NoRuntimeCouplingPlan_IncludesRequiredChecksAndGoNoGo()
    {
        var plan = NoRuntimePlan();

        AssertContains(plan, "new runtime message paths");
        AssertContains(plan, "new Provider/cloud paths");
        AssertContains(plan, "new network paths");
        AssertContains(plan, "filesystem APIs");
        AssertContains(plan, "capability enablement");
        AssertContains(plan, "productive consent persistence");
        AssertContains(plan, "source-of-truth promotion");
        AssertContains(plan, "no broad rewrite");
        AssertContains(plan, "patch modifies manifest permissions");
        AssertContains(plan, "generic SaaS dashboard return");
        AssertContains(plan, "runtime appearing active");
        AssertContains(plan, "\"ReadyForRuntimeConnection\": false");
        AssertContains(plan, "\"ReadyForRealCapabilityConnection\": false");
        AssertSafeOutput(plan);
    }

    [TestMethod]
    public void ReadinessSummary_IsDryRunOnlyAndAllowsOnlyFutureSmallTokenPatch()
    {
        var summary = Summary();

        AssertContains(summary, "\"isDryRunOnly\": true");
        AssertContains(summary, "\"canModifySidepanelProductUi\": false");
        AssertContains(summary, "\"canModifySidepanelHtml\": false");
        AssertContains(summary, "\"canModifySidepanelCss\": false");
        AssertContains(summary, "\"canModifySidepanelJs\": false");
        AssertContains(summary, "\"canModifyManifest\": false");
        AssertContains(summary, "\"canConnectRuntime\": false");
        AssertContains(summary, "\"canEnableCapabilities\": false");
        AssertContains(summary, "\"sidepanelProductFilesModified\": false");
        AssertContains(summary, "\"readyForFutureSmallTokenPatch\": true");
        AssertSafeOutput(summary);
    }

    [TestMethod]
    public void StaticPreviews_HaveRequiredContentAndNoRemoteOrScriptSurfaces()
    {
        var html = TokenPreview() + PatchPreview();

        AssertContains(html, "Sidepanel Visual Token Dry Run");
        AssertContains(html, "Product files");
        AssertContains(html, "Unchanged in this block");
        AssertContains(html, "Eight Small Patches");
        AssertContains(html, "Patch 1");
        AssertContains(html, "Patch 8");
        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "c" + "dn");
        AssertDoesNotContain(html, "tele" + "metry");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void NewSidepanelTokenDryRunFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
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

    private static string DryRun() => Read("artifacts", "agent-operations", "m614", "sidepanel-token-integration-dry-run.json");
    private static string PatchPlan() => Read("artifacts", "agent-operations", "m614", "sidepanel-diff-patch-plan.json");
    private static string NoRuntimePlan() => Read("artifacts", "agent-operations", "m614", "no-runtime-coupling-test-plan.json");
    private static string Summary() => Read("artifacts", "agent-operations", "m614", "sidepanel-token-dry-run-readiness-summary.json");
    private static string TokenPreview() => Read("artifacts", "agent-operations", "m614", "sidepanel-token-dry-run-preview.html");
    private static string PatchPreview() => Read("artifacts", "agent-operations", "m614", "sidepanel-diff-patch-plan-preview.html");
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
            Path.Combine("docs", "reports", "sidepanel-token-dry-run-m612-m614.md"),
            Path.Combine("artifacts", "agent-operations", "m614", "sidepanel-token-integration-dry-run.json"),
            Path.Combine("artifacts", "agent-operations", "m614", "sidepanel-diff-patch-plan.json"),
            Path.Combine("artifacts", "agent-operations", "m614", "no-runtime-coupling-test-plan.json"),
            Path.Combine("artifacts", "agent-operations", "m614", "sidepanel-token-dry-run-readiness-summary.json"),
            Path.Combine("artifacts", "agent-operations", "m614", "sidepanel-token-dry-run-preview.html"),
            Path.Combine("artifacts", "agent-operations", "m614", "sidepanel-diff-patch-plan-preview.html")
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
