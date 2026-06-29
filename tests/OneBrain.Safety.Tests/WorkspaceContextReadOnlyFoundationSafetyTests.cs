using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Context;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("WorkspaceContextMemory")]
[TestCategory("PhaseDContextWorkspaceMemory")]
public sealed class WorkspaceContextReadOnlyFoundationSafetyTests
{
    private const string FoundationPath = "src/OneBrain.Core/Context/WorkspaceContextReadOnlyFoundation.cs";

    [TestMethod]
    public void FoundationSource_HasNoFilesystemDatabaseProviderVectorRuntimeOrServiceImplementation()
    {
        var source = ReadRepoText(FoundationPath);
        var forbidden = new[]
        {
            "File.Read",
            "File.Write",
            "FileStream",
            "Directory.",
            "Directory.CreateDirectory",
            "Path.",
            "Microsoft.Data.Sqlite",
            "SQLiteConnection",
            "SqlConnection",
            "DbContext",
            "IDbConnection",
            "HttpClient",
            "WebSocket",
            "Process.Start",
            "ServiceCollection",
            "AddSingleton",
            "AddScoped",
            "AddTransient",
            "OpenAI",
            "EmbeddingClient",
            "VectorStore",
            "KernelMemory"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(source.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }

    [TestMethod]
    public void FoundationProof_DisablesAllSideEffectsAndRuntimeCapabilities()
    {
        var packet = WorkspaceContextReadOnlyPresenter.CreateFixture();
        var proof = packet.NoSideEffectProof;

        Assert.IsTrue(proof.Passes);
        Assert.IsFalse(proof.WorkspaceFilesystemReadAttempted);
        Assert.IsFalse(proof.FilesystemWriteAttempted);
        Assert.IsFalse(proof.DatabaseTouched);
        Assert.IsFalse(proof.DurablePersistenceActive);
        Assert.IsFalse(proof.DurableMemoryActive);
        Assert.IsFalse(proof.VectorSemanticBackendTouched);
        Assert.IsFalse(proof.LlmProviderTouched);
        Assert.IsFalse(proof.ProviderCloudTouched);
        Assert.IsFalse(proof.MigrationRunnerStarted);
        Assert.IsFalse(proof.MigrationExecuted);
        Assert.IsFalse(proof.RuntimeTouched);
        Assert.IsFalse(proof.BrowserCdpTouched);
        Assert.IsFalse(proof.WcuTouched);
        Assert.IsFalse(proof.OcrTouched);
        Assert.IsFalse(proof.ProductActionExposed);
        Assert.IsFalse(proof.ProductServiceRegistered);
    }

    [TestMethod]
    public void FoundationSources_DoNotReadWorkspaceOrUseProviderCloudOrVectorBackend()
    {
        var packet = WorkspaceContextReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(packet.Sources.All(source => source.FixtureOnly));
        Assert.IsTrue(packet.Sources.All(source => !source.ReadsWorkspaceFilesystem));
        Assert.IsTrue(packet.Sources.All(source => !source.UsesProviderCloud));
        Assert.IsTrue(packet.Sources.All(source => !source.UsesVectorSemanticBackend));
        Assert.IsTrue(packet.Sources.All(source => source.NoSideEffectProof.Passes));
    }

    [TestMethod]
    public void FoundationMemoryCandidates_ArePreviewOnlyAndNonDurable()
    {
        var packet = WorkspaceContextReadOnlyPresenter.CreateFixture();

        Assert.IsFalse(packet.HasDurableMemory);
        Assert.IsTrue(packet.MemoryCandidates.Count >= 5);
        Assert.IsTrue(packet.MemoryCandidates.All(candidate => !candidate.DurableMemoryEnabled));
        Assert.IsTrue(packet.MemoryCandidates.All(candidate => candidate.NoSideEffectProof.Passes));
        Assert.IsTrue(packet.MemoryCandidates.Any(candidate => candidate.Kind == WorkspaceContextItemKind.ContradictionMemoryPreview));
        Assert.IsTrue(packet.MemoryCandidates.Any(candidate => candidate.Kind == WorkspaceContextItemKind.RiskMemoryPreview));
        Assert.IsTrue(packet.MemoryCandidates.Any(candidate => candidate.Kind == WorkspaceContextItemKind.DecisionMemoryPreview));
    }

    [TestMethod]
    public void FoundationDoesNotReuseEilReadWriteStoresAsImplementation()
    {
        var source = ReadRepoText(FoundationPath);

        Assert.IsFalse(source.Contains("DisabledEvidenceIntelligenceReadStore", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("DisabledEvidenceIntelligenceWriteStore", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("ProcessMemoryStore", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("WorkflowRetrievalService", StringComparison.Ordinal));
        StringAssert.Contains(source, "EvidenceIntelligenceAuditDashboardReadOnlyPresenter.CreateFixture");
    }

    [TestMethod]
    public void FoundationText_HasNoContextMemoryOrProductionOverclaim()
    {
        var packet = WorkspaceContextReadOnlyPresenter.CreateFixture();
        var text = string.Join(
            "\n",
            packet.ReadOnlySummary,
            packet.Summary,
            packet.ProviderCloudNotice,
            packet.SemanticVectorNotice,
            packet.SafeNextStep,
            string.Join("\n", packet.Items.Select(item => item.Summary)),
            string.Join("\n", packet.MemoryCandidates.Select(candidate => candidate.Preview)),
            string.Join("\n", packet.Warnings),
            string.Join("\n", packet.Blockers));

        var forbidden = new[]
        {
            "production" + "-ready",
            "memory persisted",
            "durable memory active",
            "semantic search enabled",
            "vector backend enabled",
            "provider call enabled",
            "workspace scan completed",
            "runtime action enabled",
            "live automation enabled",
            "filesystem indexed"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(text.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }

    [TestMethod]
    public void FoundationText_HasNoSyntheticSecretLeakage()
    {
        var packet = WorkspaceContextReadOnlyPresenter.CreateFixture();
        var text = string.Join(
            "\n",
            packet.ReadOnlySummary,
            packet.Summary,
            string.Join("\n", packet.Items.Select(item => item.Summary)),
            string.Join("\n", packet.MemoryCandidates.Select(candidate => candidate.Preview)));

        Assert.IsFalse(text.Contains("sk-", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("Bearer" + " ", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("AKIA", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("ghp_", StringComparison.OrdinalIgnoreCase));
    }

    private static string ReadRepoText(string relativePath)
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            var candidate = System.IO.Path.Combine(current, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(candidate))
                return System.IO.File.ReadAllText(candidate);

            current = System.IO.Directory.GetParent(current)?.FullName;
        }

        Assert.Fail($"Could not locate {relativePath}");
        return "";
    }
}
