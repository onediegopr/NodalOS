using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Evidence;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("EvidenceIntelligence")]
[TestCategory("EvidenceIntelligencePersistenceDesign")]
public sealed class EvidenceIntelligencePersistenceDesignSafetyTests
{
    private const string PersistenceDesignPath = "src/OneBrain.Core/Evidence/EvidenceIntelligencePersistenceDesign.cs";
    private const string UiMountPath = "src/OneBrain.Core/Evidence/EvidenceIntelligenceReadOnlyUiMount.cs";

    [TestMethod]
    public void PersistenceDesign_FailsClosedAndDoesNotEnableWrites()
    {
        var plan = EvidenceIntelligencePersistencePlan.CreateDisabledLocalFirstDesign();

        Assert.IsTrue(plan.CapabilityStatus.FailClosed);
        Assert.IsFalse(plan.CapabilityStatus.DurableStoreEnabled);
        Assert.IsFalse(plan.CapabilityStatus.DurableWritesEnabled);
        Assert.IsFalse(plan.CapabilityStatus.FilesystemProductWritesEnabled);
        Assert.IsFalse(plan.CapabilityStatus.RegistersProductService);
        StringAssert.Contains(plan.WriteModelBoundaries.Single(boundary => boundary.Contains("No write model", StringComparison.Ordinal)), "No write model is active");
    }

    [TestMethod]
    public void PersistenceDesign_SourceHasNoFilesystemDatabaseNetworkOrCloudImplementation()
    {
        var source = ReadRepoText(PersistenceDesignPath);
        var forbidden = new[]
        {
            "File.Read",
            "File.Write",
            "FileStream",
            "Directory.",
            "Directory.CreateDirectory",
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
            "VectorStore"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(source.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }

    [TestMethod]
    public void ReadStoreScaffold_DoesNotReadFilesystemDatabaseWriteOrMigrate()
    {
        var store = new DisabledEvidenceIntelligenceReadStore();
        var result = store.Query(EvidenceIntelligenceReadStoreQuery.ByWorkspaceId("workspace.fixture"));

        Assert.IsFalse(store.ScaffoldStatus.FilesystemReadEnabled);
        Assert.IsFalse(store.ScaffoldStatus.DatabaseReadEnabled);
        Assert.IsFalse(store.ScaffoldStatus.WriteEnabled);
        Assert.IsFalse(store.ScaffoldStatus.MigrationEnabled);
        Assert.IsFalse(result.ReadsFilesystem);
        Assert.IsFalse(result.ReadsDatabase);
        Assert.IsFalse(result.WritesFilesystem);
        Assert.IsFalse(result.RunsMigration);
        Assert.IsTrue(result.FailClosed);
    }

    [TestMethod]
    public void ReadStoreScaffold_DoesNotEnableProviderCloudSemanticRuntimeOrServiceRegistration()
    {
        var store = new DisabledEvidenceIntelligenceReadStore();
        var result = store.Query(EvidenceIntelligenceReadStoreQuery.SafeNextStepSnapshot());

        Assert.IsFalse(store.ScaffoldStatus.ProviderCloudEnabled);
        Assert.IsFalse(store.ScaffoldStatus.SemanticVectorBackendEnabled);
        Assert.IsFalse(store.ScaffoldStatus.RuntimeEnabled);
        Assert.IsFalse(store.ScaffoldStatus.RegistersProductService);
        Assert.IsFalse(store.CapabilityStatus.ProviderCloudEnabled);
        Assert.IsFalse(store.CapabilityStatus.SemanticVectorBackendEnabled);
        Assert.IsFalse(store.CapabilityStatus.RuntimeActionsEnabled);
        Assert.IsFalse(store.CapabilityStatus.RegistersProductService);
        Assert.IsFalse(result.CallsProviderCloud);
        Assert.IsFalse(result.UsesSemanticVectorBackend);
        Assert.IsFalse(result.UsesRuntime);
        Assert.IsFalse(result.FallbackUsed);
    }

    [TestMethod]
    public void PersistenceDesign_DoesNotEnableMigrationSemanticProviderOrRuntime()
    {
        var plan = EvidenceIntelligencePersistencePlan.CreateDisabledLocalFirstDesign();
        var status = plan.CapabilityStatus;

        Assert.IsFalse(plan.Migration.MigrationRunnerEnabled);
        Assert.IsFalse(status.SemanticVectorBackendEnabled);
        Assert.IsFalse(status.ProviderCloudEnabled);
        Assert.IsFalse(status.RuntimeActionsEnabled);
        Assert.IsFalse(status.BrowserCdpAutomationEnabled);
        Assert.IsFalse(status.WcuLiveEnabled);
        Assert.IsFalse(status.OcrLiveEnabled);
    }

    [TestMethod]
    public void PersistenceDesign_RedactionAndSensitiveFieldsAreMandatoryBeforeFutureWrites()
    {
        var schema = EvidenceIntelligencePersistenceSchemaCatalog.CreateV1Descriptor();
        var evidenceRecord = schema.Entities.Single(entity => entity.EntityName == "EvidenceRecord");
        var redaction = schema.Entities.Single(entity => entity.EntityName == "RedactionMetadata");

        Assert.IsTrue(schema.RedactionMetadataRequired);
        Assert.IsTrue(schema.IntegrityHashRequired);
        Assert.IsTrue(evidenceRecord.Fields.Any(field => field.Name == "DisplayText" && field.Classification == EvidenceIntelligencePersistenceFieldClassification.RedactedBeforeWrite));
        Assert.IsTrue(evidenceRecord.Fields.Any(field => field.Name == "RawPayload" && field.Classification == EvidenceIntelligencePersistenceFieldClassification.SensitiveNeverPersist));
        Assert.IsTrue(redaction.Fields.Any(field => field.Name == "RawSecret" && field.Classification == EvidenceIntelligencePersistenceFieldClassification.SensitiveNeverPersist));
    }

    [TestMethod]
    public void EilUiMount_RemainsFixtureReadOnlyAndPersistenceDisabled()
    {
        var source = ReadRepoText(UiMountPath);
        var mount = EvidenceIntelligenceReadOnlyUiMount.CreateFixture();
        var store = new DisabledEvidenceIntelligenceReadStore();

        Assert.IsTrue(mount.UsesDeterministicFixture);
        Assert.IsFalse(mount.DurablePersistenceEnabled);
        Assert.IsFalse(mount.FilesystemWritesEnabled);
        Assert.IsFalse(mount.ProviderCloudEnabled);
        Assert.IsFalse(mount.RuntimeEnabled);
        Assert.IsFalse(store.ScaffoldStatus.DurableReadEnabled);
        StringAssert.Contains(source, "UsesDeterministicFixture: true");
        StringAssert.Contains(source, "DurablePersistenceEnabled: false");
        StringAssert.Contains(source, "FilesystemWritesEnabled: false");
    }

    private static string ReadRepoText(string relativePath) =>
        File.ReadAllText(Path.Combine(RepoRoot(), relativePath));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? Environment.CurrentDirectory;
    }
}
