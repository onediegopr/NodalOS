using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("NamingCleanup")]
[TestCategory("PauseClosure")]
public sealed class NodalOsNamingCleanupM464LiteTests
{
    [TestMethod]
    public void NamingCleanupReportExists() =>
        Assert.IsTrue(File.Exists(ReportPath()));

    [TestMethod]
    public void NamingCleanupArtifactExists() =>
        Assert.IsTrue(File.Exists(ArtifactPath()));

    [TestMethod]
    public void ArtifactOperationalProjectNameIsNodalOs() =>
        AssertContains(ArtifactText(), "\"operationalProjectName\": \"NODAL OS\"");

    [TestMethod]
    public void RoadmapDoesNotUseExternalNameAsOperationalProjectName() =>
        AssertNoExternalName(RoadmapText(), "roadmap");

    [TestMethod]
    public void RecentAgentOperationsArtifactsDoNotUseExternalNameAsOperationalProjectName()
    {
        foreach (var path in RecentArtifactPaths())
            AssertNoExternalName(File.ReadAllText(path), path);
    }

    [TestMethod]
    public void RecentReportsDoNotUseExternalNameAsOperationalProjectName()
    {
        foreach (var path in RecentReportPaths())
            AssertNoExternalName(File.ReadAllText(path), path);
    }

    [TestMethod]
    public void AutomationLayerAdrUsesNodalOsOperationalName()
    {
        var adrPath = Path.Combine(RepoRoot(), "docs", "architecture", "nodal-os-automation-layer-decision-record.md");
        var oldPath = Path.Combine(RepoRoot(), "docs", "architecture", $"no{"drix"}-automation-layer-decision-record.md");
        var adr = File.ReadAllText(adrPath);

        Assert.IsTrue(File.Exists(adrPath), adrPath);
        Assert.IsFalse(File.Exists(oldPath), oldPath);
        AssertContains(adr, "# NODAL OS Automation Layer Decision Record");
        AssertNoExternalName(adr, adrPath);
    }

    [TestMethod]
    public void NoNexaOperationalReferencesInRecentDocs()
    {
        foreach (var path in RecentReportPaths().Append(Path.Combine(RepoRoot(), "docs", "architecture", "nodal-os-automation-layer-decision-record.md")))
        {
            var text = File.ReadAllText(path);
            Assert.IsFalse(text.Contains("NEXA Automation", StringComparison.OrdinalIgnoreCase), path);
            Assert.IsFalse(text.Contains("NEXA Mission Control", StringComparison.OrdinalIgnoreCase), path);
            Assert.IsFalse(text.Contains("NEXA core", StringComparison.OrdinalIgnoreCase), path);
        }
    }

    [TestMethod]
    public void ArtifactMarksNoRuntimeBehaviorChange() =>
        AssertContains(ArtifactText(), "\"noRuntimeBehaviorChange\": true");

    [TestMethod]
    public void ArtifactMarksNoArchitectureChange() =>
        AssertContains(ArtifactText(), "\"noArchitectureChange\": true");

    private static IEnumerable<string> RecentArtifactPaths()
    {
        var root = Path.Combine(RepoRoot(), "artifacts", "agent-operations");
        foreach (var milestone in new[] { "m448", "m451", "m454", "m457", "m462", "m463" })
        {
            var dir = Path.Combine(root, milestone);
            if (!Directory.Exists(dir))
                continue;

            foreach (var path in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
                yield return path;
        }
    }

    private static IEnumerable<string> RecentReportPaths()
    {
        var names = new[]
        {
            "automation-layer-rpa-references-discovery-m446.md",
            "automation-event-evidence-schema-contracts-v1-m451.md",
            "selector-safety-human-handoff-contracts-v1-m454.md",
            "recipe-risk-classifier-dsl-decision-m457.md",
            "automation-layer-integration-no-divergence-m462.md",
            "pause-closure-core-roadmap-resync-m463.md"
        };

        foreach (var name in names)
            yield return Path.Combine(RepoRoot(), "docs", "reports", name);
    }

    private static string ReportPath() =>
        Path.Combine(RepoRoot(), "docs", "reports", "nodal-os-naming-cleanup-m464-lite.md");

    private static string ArtifactPath() =>
        Path.Combine(RepoRoot(), "artifacts", "agent-operations", "m464-lite", "nodal-os-naming-cleanup-summary.json");

    private static string ArtifactText() =>
        File.ReadAllText(ArtifactPath());

    private static string RoadmapText() =>
        File.ReadAllText(Path.Combine(RepoRoot(), "docs", "roadmap", "nodal-os-roadmap-vnext.md"));

    private static void AssertNoExternalName(string text, string source) =>
        Assert.IsFalse(text.Contains($"NO{"DRIX"} Automation", StringComparison.OrdinalIgnoreCase), source);

    private static void AssertContains(string text, string expected) =>
        Assert.IsTrue(text.Contains(expected, StringComparison.Ordinal), expected);

    private static string RepoRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "OneBrain.slnx")))
                return current;

            current = Directory.GetParent(current)?.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
