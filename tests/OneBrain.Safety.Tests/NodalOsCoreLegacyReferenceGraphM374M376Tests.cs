using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("CoreLegacyReferenceGraph")]
[TestCategory("StepLibrary")]
[TestCategory("AgentProgressReporting")]
[TestCategory("VerificationBeforeDone")]
[TestCategory("RunReport")]
[TestCategory("MissionTaskDomain")]
public sealed class NodalOsCoreLegacyReferenceGraphM374M376Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void CoreLegacyReferenceGraphReportExists()
    {
        Assert.IsTrue(File.Exists(SourcePath("docs", "reports", "core-legacy-reference-graph-m376.md")));
    }

    [TestMethod]
    public void DiscoveryReportExists()
    {
        Assert.IsTrue(File.Exists(SourcePath("docs", "reports", "core-legacy-reference-graph-discovery-m374.md")));
    }

    [TestMethod]
    public void ArtifactExists()
    {
        Assert.IsTrue(File.Exists(SourcePath("artifacts", "core", "m376", "core-legacy-reference-graph-summary.json")));
    }

    [TestMethod]
    public void ArtifactMarksActivePathsDocumented()
    {
        using var doc = LoadArtifact();

        Assert.IsTrue(doc.RootElement.GetProperty("activePathsDocumented").GetBoolean());
    }

    [TestMethod]
    public void ArtifactMarksLegacyPathsDocumented()
    {
        using var doc = LoadArtifact();

        Assert.IsTrue(doc.RootElement.GetProperty("legacyPathsDocumented").GetBoolean());
    }

    [TestMethod]
    public void ArtifactMarksDiagnosticOnlyPathsDocumented()
    {
        using var doc = LoadArtifact();

        Assert.IsTrue(doc.RootElement.GetProperty("diagnosticOnlyPathsDocumented").GetBoolean());
    }

    [TestMethod]
    public void ArtifactMarksNoRuntimeBehaviorChange()
    {
        using var doc = LoadArtifact();

        Assert.IsTrue(doc.RootElement.GetProperty("noRuntimeBehaviorChange").GetBoolean());
    }

    [TestMethod]
    public void ArtifactMarksNoUiImplemented()
    {
        using var doc = LoadArtifact();

        Assert.IsTrue(doc.RootElement.GetProperty("noUiImplemented").GetBoolean());
    }

    [TestMethod]
    public void ArtifactMarksNoRecipeExecution()
    {
        using var doc = LoadArtifact();

        Assert.IsTrue(doc.RootElement.GetProperty("noRecipeExecutionImplemented").GetBoolean());
    }

    [TestMethod]
    public void ArtifactMarksNoOrchestrationApi()
    {
        using var doc = LoadArtifact();

        Assert.IsTrue(doc.RootElement.GetProperty("noOrchestrationApiImplemented").GetBoolean());
    }

    [TestMethod]
    public void ArtifactMarksNoNamespaceMove()
    {
        using var doc = LoadArtifact();

        Assert.IsTrue(doc.RootElement.GetProperty("noNamespaceMoveImplemented").GetBoolean());
    }

    [TestMethod]
    public void ArtifactMarksNoLegacyDeleted()
    {
        using var doc = LoadArtifact();

        Assert.IsTrue(doc.RootElement.GetProperty("noLegacyDeleted").GetBoolean());
    }

    [TestMethod]
    public void ArtifactContainsRecommendedNextMilestone()
    {
        using var doc = LoadArtifact();

        Assert.AreEqual(
            "M377-M379 Completion Gate Canonicalization",
            doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
    }

    [TestMethod]
    public void ReportsUseNodalOsName_NotNexaAsProjectName()
    {
        var discovery = File.ReadAllText(SourcePath("docs", "reports", "core-legacy-reference-graph-discovery-m374.md"));
        var graph = File.ReadAllText(SourcePath("docs", "reports", "core-legacy-reference-graph-m376.md"));

        StringAssert.Contains(discovery, "NODAL OS");
        StringAssert.Contains(graph, "NODAL OS");
        Assert.IsFalse(discovery.Contains("Project: NEXA", StringComparison.Ordinal));
        Assert.IsFalse(graph.Contains("Project: NEXA", StringComparison.Ordinal));
    }

    private static JsonDocument LoadArtifact() =>
        JsonDocument.Parse(File.ReadAllText(SourcePath(
            "artifacts",
            "core",
            "m376",
            "core-legacy-reference-graph-summary.json")));

    private static string SourcePath(params string[] parts) => Path.Combine(new[] { RepoRoot }.Concat(parts).ToArray());
}
