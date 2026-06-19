using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("NamespaceNamingAdr")]
[TestCategory("AgentOperations")]
[TestCategory("RecipeStepRuntimePermission")]
[TestCategory("EvidenceRefLedgerBridge")]
[TestCategory("CommonRedaction")]
public sealed class NodalOsAgentOperationsNamespaceNamingAdrM389M391Tests
{
    [TestMethod]
    public void NamespaceNamingAuditReportExists()
    {
        Assert.IsTrue(File.Exists(SourcePath("docs", "reports", "agent-operations-namespace-naming-audit-m389.md")));
    }

    [TestMethod]
    public void NamespaceNamingAdrExists()
    {
        Assert.IsTrue(File.Exists(AdrPath()));
    }

    [TestMethod]
    public void ArtifactExists()
    {
        Assert.IsTrue(File.Exists(ArtifactPath()));
    }

    [TestMethod]
    public void ArtifactDocumentsNexaCompatibility()
    {
        using var doc = LoadArtifact();
        Assert.IsTrue(doc.RootElement.GetProperty("nexaCompatibilityDocumented").GetBoolean());
    }

    [TestMethod]
    public void ArtifactDocumentsOneBrainHistoricalNamespace()
    {
        using var doc = LoadArtifact();
        Assert.IsTrue(doc.RootElement.GetProperty("oneBrainHistoricalNamespaceDocumented").GetBoolean());
    }

    [TestMethod]
    public void ArtifactDefinesNodalOsForwardNamingRule()
    {
        using var doc = LoadArtifact();
        Assert.IsTrue(doc.RootElement.GetProperty("nodalOsForwardNamingRuleDefined").GetBoolean());
    }

    [TestMethod]
    public void ArtifactDocumentsFutureExtractionBoundary()
    {
        using var doc = LoadArtifact();
        Assert.IsTrue(doc.RootElement.GetProperty("futureExtractionBoundaryDefined").GetBoolean());
    }

    [TestMethod]
    public void ArtifactConfirmsNoNamespaceMove()
    {
        using var doc = LoadArtifact();
        Assert.IsTrue(doc.RootElement.GetProperty("noNamespaceMoveImplemented").GetBoolean());
    }

    [TestMethod]
    public void ArtifactConfirmsNoBroadRename()
    {
        using var doc = LoadArtifact();
        Assert.IsTrue(doc.RootElement.GetProperty("noBroadRenameImplemented").GetBoolean());
    }

    [TestMethod]
    public void AdrMentionsNoNewNexaTypesRule()
    {
        var adr = File.ReadAllText(AdrPath());

        StringAssert.Contains(adr, "No new `Nexa*` Agent Operations types should be introduced.");
    }

    [TestMethod]
    public void AdrMentionsAgentOperationsNotBrowserBoundLongTerm()
    {
        var adr = File.ReadAllText(AdrPath());

        StringAssert.Contains(adr, "Agent Operations is conceptually core/platform-layer functionality.");
        StringAssert.Contains(adr, "should not depend conceptually on Browser/CDP");
    }

    [TestMethod]
    public void AdrMentionsCompatibilityShims()
    {
        var adr = File.ReadAllText(AdrPath());

        StringAssert.Contains(adr, "Compatibility shims");
    }

    [TestMethod]
    public void ReportsUseNodalOsAsProjectName()
    {
        var adr = File.ReadAllText(AdrPath());
        var audit = File.ReadAllText(SourcePath("docs", "reports", "agent-operations-namespace-naming-audit-m389.md"));

        StringAssert.Contains(adr, "The current product name is NODAL OS.");
        Assert.IsFalse(adr.Contains("current product name is NEXA", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(audit.Contains("current product name is NEXA", StringComparison.OrdinalIgnoreCase));
    }

    private static JsonDocument LoadArtifact() =>
        JsonDocument.Parse(File.ReadAllText(ArtifactPath()));

    private static string AdrPath() =>
        SourcePath("docs", "architecture", "agent-operations-namespace-naming-adr.md");

    private static string ArtifactPath() =>
        SourcePath("artifacts", "agent-operations", "m391", "agent-operations-namespace-naming-adr-summary.json");

    private static string SourcePath(params string[] parts) =>
        Path.Combine(new[] { FindRepoRoot() }.Concat(parts).ToArray());

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OneBrain.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
