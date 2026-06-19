using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("BrowserRuntime")]
[TestCategory("Smoke")]
[TestCategory("Cdp")]
[TestCategory("Cleanup")]
[TestCategory("Diagnostics")]
public sealed class NodalOsBrowserRuntimeFlakeHardeningM359M361Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void RootCauseAndFinalReportsExist()
    {
        Assert.IsTrue(File.Exists(SourcePath("docs", "reports", "browser-runtime-flake-root-cause-m359.md")));
        Assert.IsTrue(File.Exists(SourcePath("docs", "reports", "browser-runtime-flake-hardening-m361.md")));
    }

    [TestMethod]
    public void ArtifactExistsAndValidatesFlags()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(SourcePath(
            "artifacts", "browser-runtime", "m361", "browser-runtime-flake-hardening-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M359-M361", root.GetProperty("milestone").GetString());
        Assert.AreEqual("3021d85", root.GetProperty("baseCommit").GetString());
        Assert.AreEqual("BROWSER_RUNTIME_FLAKE_HARDENED", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("rootCauseDocumented").GetBoolean());
        Assert.IsTrue(root.GetProperty("cleanupHardened").GetBoolean());
        Assert.IsTrue(root.GetProperty("cleanupIdempotent").GetBoolean());
        Assert.IsTrue(root.GetProperty("ownedProcessOnlyCleanup").GetBoolean());
        Assert.IsTrue(root.GetProperty("profileCleanupBoundedRetry").GetBoolean());
        Assert.IsTrue(root.GetProperty("webSocketDisposalHardened").GetBoolean());
        Assert.IsTrue(root.GetProperty("structuredDiagnosticsImproved").GetBoolean());
        Assert.IsTrue(root.GetProperty("uniqueProfilePerRun").GetBoolean());
        Assert.IsTrue(root.GetProperty("noUserBrowserKill").GetBoolean());
        Assert.IsTrue(root.GetProperty("noRuntimeBehaviorChange").GetBoolean());
        Assert.IsTrue(root.GetProperty("noUiImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noRecipeExecutionImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noOrchestrationApiImplemented").GetBoolean());
    }

    [TestMethod]
    public void ArtifactListsAffectedSmokeTests()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(SourcePath(
            "artifacts", "browser-runtime", "m361", "browser-runtime-flake-hardening-summary.json")));
        var affected = doc.RootElement.GetProperty("affectedTests")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        CollectionAssert.Contains(affected, "BrowserRuntimeSmokeRunnerExecutesAllGatesOnFixture");
        CollectionAssert.Contains(affected, "BrowserRuntimeSmokeReportContainsStructuredDiagnostics");
    }

    private static string SourcePath(params string[] parts) => Path.Combine(new[] { RepoRoot }.Concat(parts).ToArray());
}
