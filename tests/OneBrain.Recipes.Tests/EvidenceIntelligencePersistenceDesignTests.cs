using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Evidence;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("EvidenceIntelligence")]
[TestCategory("EvidenceIntelligencePersistenceDesign")]
public sealed class EvidenceIntelligencePersistenceDesignTests
{
    [TestMethod]
    public void PersistenceCapabilityStatus_IsDisabledByDefault()
    {
        var plan = EvidenceIntelligencePersistencePlan.CreateDisabledLocalFirstDesign();
        var status = plan.CapabilityStatus;

        Assert.IsTrue(status.DesignExists);
        Assert.IsTrue(status.FailClosed);
        Assert.AreEqual("DESIGN_ONLY_DISABLED_FAIL_CLOSED", status.ImplementationStatus);
        Assert.IsFalse(status.DurableStoreEnabled);
        Assert.IsFalse(status.DurableReadsEnabled);
        Assert.IsFalse(status.DurableWritesEnabled);
        Assert.IsFalse(status.FilesystemProductWritesEnabled);
        Assert.IsFalse(status.RegistersProductService);
    }

    [TestMethod]
    public void PersistenceDesign_DoesNotEnableDurableStoreWritesOrMigration()
    {
        var plan = EvidenceIntelligencePersistencePlan.CreateDisabledLocalFirstDesign();

        Assert.AreEqual(EvidenceIntelligencePersistenceBackendRecommendation.FutureAppendOnlyLogWithReadModel, plan.Recommendation);
        Assert.IsFalse(plan.CapabilityStatus.DurableStoreEnabled);
        Assert.IsFalse(plan.CapabilityStatus.DurableWritesEnabled);
        Assert.IsFalse(plan.Migration.MigrationRunnerEnabled);
        Assert.IsFalse(plan.Migration.DestructiveMigrationAllowed);
        Assert.IsTrue(plan.Migration.RequiresDryRunReport);
        Assert.IsTrue(plan.Migration.RequiresBackupBeforeFutureMigration);
    }

    [TestMethod]
    public void PersistenceDesign_DoesNotEnableSemanticProviderCloudOrRuntime()
    {
        var status = EvidenceIntelligencePersistencePlan.CreateDisabledLocalFirstDesign().CapabilityStatus;

        Assert.IsFalse(status.SemanticVectorBackendEnabled);
        Assert.IsFalse(status.ProviderCloudEnabled);
        Assert.IsFalse(status.RuntimeActionsEnabled);
        Assert.IsFalse(status.BrowserCdpAutomationEnabled);
        Assert.IsFalse(status.WcuLiveEnabled);
        Assert.IsFalse(status.OcrLiveEnabled);
    }

    [TestMethod]
    public void SchemaDescriptor_IsVersionedWorkspaceBoundedAndRedactionAware()
    {
        var schema = EvidenceIntelligencePersistenceSchemaCatalog.CreateV1Descriptor();

        Assert.AreEqual("eil.local-evidence.schema.v1.design-only", schema.SchemaId);
        Assert.AreEqual(1, schema.MajorVersion);
        Assert.AreEqual(0, schema.MinorVersion);
        Assert.IsTrue(schema.SchemaVersionRequired);
        Assert.IsTrue(schema.RedactionMetadataRequired);
        Assert.IsTrue(schema.IntegrityHashRequired);
        Assert.IsTrue(schema.WorkspaceBoundaryRequired);
        Assert.IsFalse(schema.AllowsRawSecrets);
        Assert.IsFalse(schema.AllowsRawDom);
        Assert.IsFalse(schema.AllowsRawScreenshot);
        Assert.IsTrue(schema.Entities.Any(entity => entity.EntityName == "EvidenceRecord"));
        Assert.IsTrue(schema.Entities.Any(entity => entity.EntityName == "RedactionMetadata"));
        Assert.IsTrue(schema.Entities.Any(entity => entity.EntityName == "IntegrityHash"));
        Assert.IsTrue(schema.Entities.SelectMany(entity => entity.Fields).Any(field => field.Classification == EvidenceIntelligencePersistenceFieldClassification.SensitiveNeverPersist));
        Assert.IsTrue(schema.Entities.SelectMany(entity => entity.Fields).Any(field => field.Classification == EvidenceIntelligencePersistenceFieldClassification.RedactedBeforeWrite));
    }

    [TestMethod]
    public void UnlockCriteria_RequireFutureExplicitHitoAndDoNotAllowRuntimeUnlock()
    {
        var criteria = EvidenceIntelligencePersistencePlan.CreateDisabledLocalFirstDesign().UnlockCriteria;

        Assert.IsTrue(criteria.RequiresFutureExplicitHito);
        Assert.IsTrue(criteria.RequiresRedactionAudit);
        Assert.IsTrue(criteria.RequiresFilesystemWriteAudit);
        Assert.IsTrue(criteria.RequiresMigrationDryRunAudit);
        Assert.IsTrue(criteria.RequiresManualQa);
        Assert.IsFalse(criteria.AllowsRuntimeUnlock);
        CollectionAssert.Contains(criteria.RequiredEvidence.ToList(), "Redaction-at-write tests with hostile fixtures.");
    }

    [TestMethod]
    public void EilUiMount_StillUsesFixtureSnapshotAndNoDurablePersistence()
    {
        var mount = EvidenceIntelligenceReadOnlyUiMount.CreateFixture();

        Assert.IsTrue(mount.UsesPresenter);
        Assert.IsTrue(mount.UsesDeterministicFixture);
        Assert.IsTrue(mount.ReadOnly);
        Assert.IsTrue(mount.LocalOnly);
        Assert.IsFalse(mount.DurablePersistenceEnabled);
        Assert.IsFalse(mount.FilesystemWritesEnabled);
        Assert.IsFalse(mount.ProviderCloudEnabled);
        Assert.IsFalse(mount.SemanticVectorBackendEnabled);
        Assert.AreEqual("Evidence Intelligence", mount.NavigationLabel);
        Assert.AreEqual("#evidenceIntelligenceSurface", mount.Route);
    }

    [TestMethod]
    public void ReadAndWriteStoreContractsExposeDisabledCapabilityOnly()
    {
        var status = EvidenceIntelligencePersistenceCapabilityStatus.DisabledDesign();
        IEvidenceIntelligenceReadStore readStore = new DisabledEvidenceIntelligenceReadStore();
        IEvidenceIntelligenceWriteStore? writeStore = null;

        Assert.IsTrue(readStore.CapabilityStatus.FailClosed);
        Assert.IsTrue(readStore.ScaffoldStatus.FailClosed);
        Assert.IsNull(writeStore);
        Assert.IsTrue(status.FailClosed);
        Assert.IsFalse(status.DurableReadsEnabled);
        Assert.IsFalse(status.DurableWritesEnabled);
    }

    [TestMethod]
    public void ReadStoreScaffold_IsDisabledByDefault()
    {
        var store = new DisabledEvidenceIntelligenceReadStore();
        var status = store.ScaffoldStatus;

        Assert.IsFalse(status.IsEnabled);
        Assert.IsTrue(status.FailClosed);
        Assert.AreEqual("DISABLED_DESIGN_ONLY_FAIL_CLOSED", status.Mode);
        Assert.IsFalse(status.DurableReadEnabled);
        Assert.IsFalse(status.FilesystemReadEnabled);
        Assert.IsFalse(status.DatabaseReadEnabled);
        Assert.IsFalse(status.MigrationEnabled);
        Assert.IsFalse(status.WriteEnabled);
        Assert.IsFalse(status.RuntimeEnabled);
        Assert.IsFalse(status.ProviderCloudEnabled);
        Assert.IsFalse(status.SemanticVectorBackendEnabled);
        Assert.IsFalse(status.RegistersProductService);
        Assert.IsTrue(store.CapabilityStatus.FailClosed);
    }

    [TestMethod]
    public void ReadStoreScaffold_ReturnsFailClosedForQueries()
    {
        var store = new DisabledEvidenceIntelligenceReadStore();
        var queries = CreateReadStoreQueries();

        foreach (var query in queries)
        {
            var result = store.Query(query);

            Assert.AreEqual(EvidenceIntelligenceReadStoreResultStatus.FailClosed, result.Status);
            Assert.IsTrue(result.FailClosed);
            Assert.AreEqual(query, result.Query);
            Assert.AreEqual(0, result.EvidenceIds.Count);
            Assert.IsFalse(result.ReadsFilesystem);
            Assert.IsFalse(result.ReadsDatabase);
            Assert.IsFalse(result.WritesFilesystem);
            Assert.IsFalse(result.RunsMigration);
            Assert.IsFalse(result.CallsProviderCloud);
            Assert.IsFalse(result.UsesSemanticVectorBackend);
            Assert.IsFalse(result.UsesRuntime);
            Assert.IsFalse(result.FallbackUsed);
            StringAssert.Contains(result.Reason, "disabled");
        }
    }

    [TestMethod]
    public void ReadStoreScaffold_QueryModelCoversFutureReadShapes()
    {
        var queries = CreateReadStoreQueries();
        var kinds = queries.Select(query => query.Kind).ToHashSet();

        CollectionAssert.AreEquivalent(
            Enum.GetValues<EvidenceIntelligenceReadStoreQueryKind>().ToList(),
            kinds.ToList());
        Assert.IsTrue(queries.All(query => query.Metadata["mode"] == "design-only"));
        Assert.IsTrue(queries.All(query => query.Metadata["fallback"] == "disabled"));
    }

    [TestMethod]
    public void ReadStoreScaffold_RequiresFutureExplicitUnlock()
    {
        var store = new DisabledEvidenceIntelligenceReadStore();

        Assert.IsTrue(store.Plan.UnlockCriteria.RequiresFutureExplicitHito);
        Assert.IsTrue(store.Plan.UnlockCriteria.RequiresRedactionAudit);
        Assert.IsTrue(store.Plan.UnlockCriteria.RequiresFilesystemWriteAudit);
        Assert.IsFalse(store.Plan.UnlockCriteria.AllowsRuntimeUnlock);
        CollectionAssert.Contains(store.ScaffoldStatus.UnlockRequirements.ToList(), "Approved ADR update.");
        CollectionAssert.Contains(store.ScaffoldStatus.DisabledReasons.ToList(), "Durable reads are disabled until a future explicit hito.");
    }

    private static IReadOnlyList<EvidenceIntelligenceReadStoreQuery> CreateReadStoreQueries() =>
    [
        EvidenceIntelligenceReadStoreQuery.ByEvidenceId("evidence.fixture.001"),
        EvidenceIntelligenceReadStoreQuery.ByWorkspaceId("workspace.fixture"),
        EvidenceIntelligenceReadStoreQuery.ByClaimId("claim.fixture.001"),
        EvidenceIntelligenceReadStoreQuery.ByActionScanId("action-scan.fixture.001"),
        EvidenceIntelligenceReadStoreQuery.ByGraphNodeId("graph-node.fixture.001"),
        EvidenceIntelligenceReadStoreQuery.ByGraphEdgeId("graph-edge.fixture.001"),
        EvidenceIntelligenceReadStoreQuery.LatestReadinessSnapshot(),
        EvidenceIntelligenceReadStoreQuery.SafeNextStepSnapshot()
    ];
}
