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
        IEvidenceIntelligenceReadStore? readStore = null;
        IEvidenceIntelligenceWriteStore? writeStore = null;

        Assert.IsNull(readStore);
        Assert.IsNull(writeStore);
        Assert.IsTrue(status.FailClosed);
        Assert.IsFalse(status.DurableReadsEnabled);
        Assert.IsFalse(status.DurableWritesEnabled);
    }
}
