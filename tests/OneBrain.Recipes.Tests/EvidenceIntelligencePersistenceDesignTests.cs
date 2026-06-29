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
        IEvidenceIntelligenceWriteStore writeStore = new DisabledEvidenceIntelligenceWriteStore();

        Assert.IsTrue(readStore.CapabilityStatus.FailClosed);
        Assert.IsTrue(readStore.ScaffoldStatus.FailClosed);
        Assert.IsTrue(writeStore.CapabilityStatus.FailClosed);
        Assert.IsTrue(writeStore.ScaffoldStatus.FailClosed);
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

    [TestMethod]
    public void WriteStoreScaffold_IsDisabledByDefault()
    {
        var store = new DisabledEvidenceIntelligenceWriteStore();
        var status = store.ScaffoldStatus;

        Assert.IsFalse(status.IsEnabled);
        Assert.IsTrue(status.FailClosed);
        Assert.AreEqual("DISABLED_DESIGN_ONLY_FAIL_CLOSED", status.Mode);
        Assert.IsFalse(status.DurableWriteEnabled);
        Assert.IsFalse(status.FilesystemWriteEnabled);
        Assert.IsFalse(status.DatabaseWriteEnabled);
        Assert.IsFalse(status.MigrationEnabled);
        Assert.IsFalse(status.RuntimeEnabled);
        Assert.IsFalse(status.ProviderCloudEnabled);
        Assert.IsFalse(status.SemanticVectorBackendEnabled);
        Assert.IsFalse(status.RedactionAtWriteExecutable);
        Assert.IsFalse(status.ServiceRegistrationEnabled);
        Assert.IsTrue(store.CapabilityStatus.FailClosed);
    }

    [TestMethod]
    public void WriteStoreScaffold_ReturnsFailClosedForAllCommands()
    {
        var store = new DisabledEvidenceIntelligenceWriteStore();
        var commands = CreateWriteStoreCommands();

        foreach (var command in commands)
        {
            var result = store.Write(command);

            Assert.AreEqual(EvidenceIntelligenceWriteResultStatus.FailClosed, result.Status);
            Assert.IsTrue(result.FailClosed);
            Assert.AreEqual(command, result.Command);
            Assert.IsFalse(result.WritesFilesystem);
            Assert.IsFalse(result.ReadsFilesystem);
            Assert.IsFalse(result.WritesDatabase);
            Assert.IsFalse(result.RunsMigration);
            Assert.IsFalse(result.CallsProviderCloud);
            Assert.IsFalse(result.UsesSemanticVectorBackend);
            Assert.IsFalse(result.UsesRuntime);
            Assert.IsFalse(result.RedactionPipelineExecuted);
            Assert.IsFalse(result.FallbackUsed);
            StringAssert.Contains(result.Reason, "disabled");
        }
    }

    [TestMethod]
    public void WriteStoreScaffold_CommandModelCoversFutureWriteShapes()
    {
        var commands = CreateWriteStoreCommands();
        var kinds = commands.Select(command => command.Kind).ToHashSet();

        CollectionAssert.AreEquivalent(
            Enum.GetValues<EvidenceIntelligenceWriteCommandKind>().ToList(),
            kinds.ToList());
        Assert.IsTrue(commands.All(command => command.Metadata["mode"] == "design-only"));
        Assert.IsTrue(commands.All(command => command.Metadata["write"] == "disabled"));
        Assert.IsTrue(commands.All(command => command.Metadata["fallback"] == "disabled"));
    }

    [TestMethod]
    public void WriteStoreScaffold_RequiresRedactionBeforeFutureUnlock()
    {
        var store = new DisabledEvidenceIntelligenceWriteStore();
        var requirement = store.ScaffoldStatus.RedactionRequirement;

        Assert.IsTrue(requirement.RedactionRequired);
        Assert.IsTrue(requirement.RawPayloadNeverPersist);
        Assert.IsTrue(requirement.SecretFieldsRejected);
        Assert.IsTrue(requirement.UnknownSensitivityRejected);
        Assert.IsTrue(requirement.IntegrityHashAfterCanonicalRedaction);
        Assert.IsFalse(requirement.ExecutablePipelineEnabled);
        Assert.IsTrue(requirement.FailClosed);
        CollectionAssert.Contains(requirement.RequiredBeforeUnlock.ToList(), "Hostile redaction-at-write fixtures must pass.");
    }

    [TestMethod]
    public void WriteStoreScaffold_RejectsRawPayloadPersistenceByDesign()
    {
        var store = new DisabledEvidenceIntelligenceWriteStore();
        var commands = new[]
        {
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord(
                "raw.fixture.001",
                containsRawPayload: true),
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord(
                "secret.fixture.001",
                containsSecretField: true),
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord(
                "sensitive.fixture.001",
                EvidenceIntelligencePersistenceFieldClassification.SensitiveNeverPersist),
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord(
                "fixture-only.fixture.001",
                EvidenceIntelligencePersistenceFieldClassification.FixtureOnly)
        };

        foreach (var command in commands)
        {
            var result = store.Write(command);

            Assert.IsTrue(command.RejectedByDesign);
            Assert.AreEqual(EvidenceIntelligenceWriteResultStatus.Rejected, result.Status);
            Assert.IsTrue(result.FailClosed);
            Assert.IsFalse(result.WritesFilesystem);
            Assert.IsFalse(result.WritesDatabase);
            Assert.IsFalse(result.RedactionPipelineExecuted);
            StringAssert.Contains(result.Reason, "rejected by design");
        }
    }

    [TestMethod]
    public void RedactionAtWriteHostileFixtures_AllRejectFailClosed()
    {
        var store = new DisabledEvidenceIntelligenceWriteStore();
        var fixtures = CreateHostileRedactionFixtures();

        Assert.AreEqual(20, fixtures.Count);

        foreach (var fixture in fixtures)
        {
            var result = store.Write(fixture.Command);

            Assert.AreEqual(EvidenceIntelligenceWriteResultStatus.Rejected, result.Status, fixture.FixtureId);
            Assert.IsTrue(result.FailClosed, fixture.FixtureId);
            Assert.IsTrue(result.Command.RejectedByDesign, fixture.FixtureId);
            Assert.IsFalse(result.WritesFilesystem, fixture.FixtureId);
            Assert.IsFalse(result.ReadsFilesystem, fixture.FixtureId);
            Assert.IsFalse(result.WritesDatabase, fixture.FixtureId);
            Assert.IsFalse(result.RunsMigration, fixture.FixtureId);
            Assert.IsFalse(result.CallsProviderCloud, fixture.FixtureId);
            Assert.IsFalse(result.UsesSemanticVectorBackend, fixture.FixtureId);
            Assert.IsFalse(result.UsesRuntime, fixture.FixtureId);
            Assert.IsFalse(result.RedactionPipelineExecuted, fixture.FixtureId);
            Assert.IsFalse(result.FallbackUsed, fixture.FixtureId);
            Assert.IsFalse(fixture.PersistAllowed, fixture.FixtureId);
            StringAssert.Contains(result.Reason, fixture.ExpectedRejectionReason);
        }
    }

    [TestMethod]
    public void RedactionAtWriteHostileFixtures_CoverExpectedCategories()
    {
        var fixtures = CreateHostileRedactionFixtures();
        var categories = fixtures.Select(fixture => fixture.Category).ToHashSet(StringComparer.Ordinal);

        CollectionAssert.IsSubsetOf(
            new[]
            {
                "api-key",
                "bearer-token",
                "jwt",
                "github-token",
                "aws-access-key",
                "private-key",
                "credential-pair",
                "cookie-session",
                "synthetic-pii",
                "synthetic-fiscal-id",
                "raw-ocr",
                "raw-browser-cdp",
                "raw-wcu",
                "unknown-sensitivity",
                "sensitive-never-persist",
                "mixed-safe-secret",
                "redacted-looking-raw",
                "integrity-before-redaction",
                "graph-sensitive",
                "safe-next-step-secret"
            }.ToList(),
            categories.ToList());
    }

    [TestMethod]
    public void RedactionAtWriteHostileFixtures_UseSyntheticSecretLikeValuesOnly()
    {
        var fixtures = CreateHostileRedactionFixtures();

        Assert.IsTrue(fixtures.All(fixture => fixture.SyntheticPayload.Contains("SYNTHETIC", StringComparison.OrdinalIgnoreCase)
            || fixture.SyntheticPayload.Contains("fixture", StringComparison.OrdinalIgnoreCase)
            || fixture.SyntheticPayload.Contains("not-real", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(fixtures.Any(fixture => fixture.SyntheticPayload.Contains("sk-", StringComparison.Ordinal)));
        Assert.IsTrue(fixtures.Any(fixture => fixture.SyntheticPayload.Contains("Bearer", StringComparison.Ordinal)));
        Assert.IsTrue(fixtures.Any(fixture => fixture.SyntheticPayload.Contains("ghp_", StringComparison.Ordinal)));
        Assert.IsTrue(fixtures.Any(fixture => fixture.SyntheticPayload.Contains("AKIA-", StringComparison.Ordinal)));
        Assert.IsTrue(fixtures.Any(fixture => fixture.SyntheticPayload.Contains("PRIVATE KEY", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void RedactionAtWriteHostileFixtures_NeverProducePersistedOrSuccessState()
    {
        var store = new DisabledEvidenceIntelligenceWriteStore();

        foreach (var fixture in CreateHostileRedactionFixtures())
        {
            var result = store.Write(fixture.Command);

            Assert.AreNotEqual(EvidenceIntelligenceWriteResultStatus.DesignOnly, result.Status, fixture.FixtureId);
            Assert.AreNotEqual(EvidenceIntelligenceWriteResultStatus.RequiresFutureUnlock, result.Status, fixture.FixtureId);
            Assert.IsFalse(result.Warnings.Any(warning => warning.Contains("persisted", StringComparison.OrdinalIgnoreCase)), fixture.FixtureId);
            Assert.IsFalse(result.Warnings.Any(warning => warning.Contains("success", StringComparison.OrdinalIgnoreCase)), fixture.FixtureId);
        }
    }

    [TestMethod]
    public void WriteStoreScaffold_RequiresFutureExplicitUnlock()
    {
        var store = new DisabledEvidenceIntelligenceWriteStore();

        Assert.IsTrue(store.Plan.UnlockCriteria.RequiresFutureExplicitHito);
        Assert.IsTrue(store.Plan.UnlockCriteria.RequiresRedactionAudit);
        Assert.IsTrue(store.Plan.UnlockCriteria.RequiresFilesystemWriteAudit);
        Assert.IsTrue(store.Plan.UnlockCriteria.RequiresMigrationDryRunAudit);
        Assert.IsFalse(store.Plan.UnlockCriteria.AllowsRuntimeUnlock);
        CollectionAssert.Contains(store.ScaffoldStatus.UnlockRequirements.ToList(), "Redaction-at-write tests with hostile fixtures.");
        CollectionAssert.Contains(store.ScaffoldStatus.DisabledReasons.ToList(), "Durable writes are disabled until a future explicit hito.");
    }

    [TestMethod]
    public void DryRunMigrationPlanContract_SameSchemaNoOpDoesNotExecute()
    {
        var plan = EvidenceIntelligenceDryRunMigrationPlan.SameSchemaNoOp();
        var result = EvidenceIntelligenceDryRunMigrationPlanner.Evaluate(plan);

        Assert.AreEqual(EvidenceIntelligenceDryRunMigrationDecision.NoOp, result.Decision);
        Assert.AreEqual(0, result.Blockers.Count);
        Assert.IsTrue(result.FailClosed);
        Assert.IsTrue(result.DryRunOnly);
        Assert.IsFalse(result.ExecutionAttempted);
        Assert.IsFalse(result.MigrationExecuted);
        Assert.IsFalse(result.DurablePersistenceActive);
        Assert.IsFalse(result.FilesystemReadAttempted);
        Assert.IsFalse(result.FilesystemWriteAttempted);
        Assert.IsFalse(result.DatabaseTouched);
        Assert.IsFalse(result.MigrationRunnerStarted);
        Assert.IsFalse(result.ProviderCloudTouched);
        Assert.IsFalse(result.SemanticVectorBackendTouched);
        Assert.IsFalse(result.RuntimeTouched);
        Assert.IsFalse(result.ProductWriteFallbackUsed);
        Assert.IsTrue(result.CapabilityStatus.FailClosed);
        Assert.IsFalse(result.CapabilityStatus.MigrationRunnerEnabled);
        Assert.IsFalse(result.CapabilityStatus.MigrationExecutionEnabled);
        Assert.IsFalse(result.CapabilityStatus.DurableStoreEnabled);
    }

    [TestMethod]
    public void DryRunMigrationPlanFixtures_BlockDangerousPlansFailClosed()
    {
        var fixtures = CreateDryRunMigrationPlanFixtures();

        Assert.AreEqual(15, fixtures.Count);

        foreach (var fixture in fixtures.Where(fixture => fixture.ExpectedDecision == EvidenceIntelligenceDryRunMigrationDecision.Blocked))
        {
            var result = EvidenceIntelligenceDryRunMigrationPlanner.Evaluate(fixture.Plan);

            Assert.AreEqual(EvidenceIntelligenceDryRunMigrationDecision.Blocked, result.Decision, fixture.FixtureId);
            Assert.IsTrue(result.FailClosed, fixture.FixtureId);
            Assert.IsTrue(result.Blockers.Any(blocker => blocker.Code == fixture.ExpectedBlocker), fixture.FixtureId);
            Assert.IsFalse(result.ExecutionAttempted, fixture.FixtureId);
            Assert.IsFalse(result.MigrationExecuted, fixture.FixtureId);
            Assert.IsFalse(result.DurablePersistenceActive, fixture.FixtureId);
            Assert.IsFalse(result.FilesystemReadAttempted, fixture.FixtureId);
            Assert.IsFalse(result.FilesystemWriteAttempted, fixture.FixtureId);
            Assert.IsFalse(result.DatabaseTouched, fixture.FixtureId);
            Assert.IsFalse(result.MigrationRunnerStarted, fixture.FixtureId);
            Assert.IsFalse(result.ProviderCloudTouched, fixture.FixtureId);
            Assert.IsFalse(result.SemanticVectorBackendTouched, fixture.FixtureId);
            Assert.IsFalse(result.RuntimeTouched, fixture.FixtureId);
            Assert.IsFalse(result.ProductWriteFallbackUsed, fixture.FixtureId);
        }
    }

    [TestMethod]
    public void DryRunMigrationPlanFixtures_CoverExpectedBlockers()
    {
        var blockers = CreateDryRunMigrationPlanFixtures()
            .Where(fixture => fixture.ExpectedDecision == EvidenceIntelligenceDryRunMigrationDecision.Blocked)
            .Select(fixture => fixture.ExpectedBlocker)
            .ToHashSet();

        CollectionAssert.IsSubsetOf(
            new[]
            {
                EvidenceIntelligenceDryRunMigrationBlockerCode.UnknownSchemaVersion,
                EvidenceIntelligenceDryRunMigrationBlockerCode.FutureSchemaUnsupported,
                EvidenceIntelligenceDryRunMigrationBlockerCode.RequiresFilesystemWrite,
                EvidenceIntelligenceDryRunMigrationBlockerCode.RequiresFilesystemRead,
                EvidenceIntelligenceDryRunMigrationBlockerCode.RequiresDatabase,
                EvidenceIntelligenceDryRunMigrationBlockerCode.RequiresMigrationRunner,
                EvidenceIntelligenceDryRunMigrationBlockerCode.RedactionGateMissing,
                EvidenceIntelligenceDryRunMigrationBlockerCode.RawPayloadRisk,
                EvidenceIntelligenceDryRunMigrationBlockerCode.RollbackUnavailable,
                EvidenceIntelligenceDryRunMigrationBlockerCode.HumanApprovalRequired,
                EvidenceIntelligenceDryRunMigrationBlockerCode.SchemaDowngrade,
                EvidenceIntelligenceDryRunMigrationBlockerCode.IncompatibleGraphEdgeShape,
                EvidenceIntelligenceDryRunMigrationBlockerCode.StaleEvidenceVersion,
                EvidenceIntelligenceDryRunMigrationBlockerCode.UnsafeIntegrityHashPlan
            }.ToList(),
            blockers.ToList());
    }

    [TestMethod]
    public void DryRunMigrationPlanResults_NeverDeclareMigrationExecutedOrDurablePersistence()
    {
        foreach (var fixture in CreateDryRunMigrationPlanFixtures())
        {
            var result = EvidenceIntelligenceDryRunMigrationPlanner.Evaluate(fixture.Plan);

            Assert.IsFalse(result.ExecutionAttempted, fixture.FixtureId);
            Assert.IsFalse(result.MigrationExecuted, fixture.FixtureId);
            Assert.IsFalse(result.DurablePersistenceActive, fixture.FixtureId);
            Assert.IsFalse(result.FilesystemReadAttempted, fixture.FixtureId);
            Assert.IsFalse(result.FilesystemWriteAttempted, fixture.FixtureId);
            Assert.IsFalse(result.DatabaseTouched, fixture.FixtureId);
            Assert.IsFalse(result.MigrationRunnerStarted, fixture.FixtureId);
            Assert.IsFalse(result.ProviderCloudTouched, fixture.FixtureId);
            Assert.IsFalse(result.SemanticVectorBackendTouched, fixture.FixtureId);
            Assert.IsFalse(result.RuntimeTouched, fixture.FixtureId);
            Assert.IsFalse(result.ProductWriteFallbackUsed, fixture.FixtureId);
        }
    }

    [TestMethod]
    public void DryRunMigrationPlanCapabilityStatus_IsDisabledFailClosed()
    {
        var status = EvidenceIntelligenceMigrationCapabilityStatus.DisabledDryRunPlan();

        Assert.IsTrue(status.DesignExists);
        Assert.IsTrue(status.DryRunPlanExists);
        Assert.IsTrue(status.DryRunOnly);
        Assert.IsTrue(status.FailClosed);
        Assert.AreEqual("DRY_RUN_PLAN_ONLY_DISABLED_FAIL_CLOSED", status.Mode);
        Assert.IsFalse(status.MigrationRunnerEnabled);
        Assert.IsFalse(status.MigrationExecutionEnabled);
        Assert.IsFalse(status.DurableStoreEnabled);
        Assert.IsFalse(status.FilesystemReadEnabled);
        Assert.IsFalse(status.FilesystemWriteEnabled);
        Assert.IsFalse(status.DatabaseEnabled);
        Assert.IsFalse(status.ProviderCloudEnabled);
        Assert.IsFalse(status.SemanticVectorBackendEnabled);
        Assert.IsFalse(status.RuntimeEnabled);
        Assert.IsFalse(status.ServiceRegistrationEnabled);
    }

    [TestMethod]
    public void SchemaCompatibilityGuard_V1KnownCompatibleReturnsDesignOnlyWithoutSideEffects()
    {
        var check = EvidenceIntelligenceSchemaCompatibilityCheck.V1KnownCompatible();
        var result = EvidenceIntelligenceSchemaCompatibilityGuard.Evaluate(check);

        Assert.AreEqual(EvidenceIntelligenceSchemaCompatibilityDecision.CompatibleDesignOnly, result.Decision);
        Assert.AreEqual(0, result.Issues.Count);
        Assert.IsTrue(result.FailClosed);
        Assert.IsTrue(result.MigrationCouldBePlannedDesignOnly);
        Assert.IsFalse(result.DurablePersistenceAllowed);
        Assert.IsFalse(result.FilesystemReadAttempted);
        Assert.IsFalse(result.FilesystemWriteAttempted);
        Assert.IsFalse(result.DatabaseTouched);
        Assert.IsFalse(result.MigrationRunnerStarted);
        Assert.IsFalse(result.MigrationExecuted);
        Assert.IsFalse(result.ProviderCloudTouched);
        Assert.IsFalse(result.SemanticVectorBackendTouched);
        Assert.IsFalse(result.RuntimeTouched);
        Assert.IsFalse(result.ProductWriteFallbackUsed);
        Assert.IsTrue(result.Status.FailClosed);
    }

    [TestMethod]
    public void SchemaCompatibilityFixtures_IncompatibleArtifactsBlockFailClosed()
    {
        var fixtures = CreateSchemaCompatibilityFixtures();

        Assert.AreEqual(20, fixtures.Count);

        foreach (var fixture in fixtures.Where(fixture => fixture.ExpectedDecision != EvidenceIntelligenceSchemaCompatibilityDecision.CompatibleDesignOnly))
        {
            var result = EvidenceIntelligenceSchemaCompatibilityGuard.Evaluate(fixture.Check);

            Assert.AreEqual(fixture.ExpectedDecision, result.Decision, fixture.FixtureId);
            Assert.IsTrue(result.FailClosed, fixture.FixtureId);
            Assert.IsTrue(result.Issues.Any(issue => issue.Kind == fixture.ExpectedIssue), fixture.FixtureId);
            Assert.AreEqual(fixture.ExpectedMigrationCouldBePlanned, result.MigrationCouldBePlannedDesignOnly, fixture.FixtureId);
            Assert.IsFalse(result.DurablePersistenceAllowed, fixture.FixtureId);
            Assert.IsFalse(result.FilesystemReadAttempted, fixture.FixtureId);
            Assert.IsFalse(result.FilesystemWriteAttempted, fixture.FixtureId);
            Assert.IsFalse(result.DatabaseTouched, fixture.FixtureId);
            Assert.IsFalse(result.MigrationRunnerStarted, fixture.FixtureId);
            Assert.IsFalse(result.MigrationExecuted, fixture.FixtureId);
            Assert.IsFalse(result.ProviderCloudTouched, fixture.FixtureId);
            Assert.IsFalse(result.SemanticVectorBackendTouched, fixture.FixtureId);
            Assert.IsFalse(result.RuntimeTouched, fixture.FixtureId);
            Assert.IsFalse(result.ProductWriteFallbackUsed, fixture.FixtureId);
        }
    }

    [TestMethod]
    public void SchemaCompatibilityFixtures_CoverExpectedArtifactKindsAndIssues()
    {
        var fixtures = CreateSchemaCompatibilityFixtures();
        var artifactKinds = fixtures.Select(fixture => fixture.Check.ArtifactKind).ToHashSet();
        var issueKinds = fixtures.Select(fixture => fixture.Check.IssueKind).ToHashSet();

        CollectionAssert.IsSubsetOf(
            new[]
            {
                EvidenceIntelligenceSchemaCompatibilityArtifactKind.EvidenceRecord,
                EvidenceIntelligenceSchemaCompatibilityArtifactKind.EvidenceSource,
                EvidenceIntelligenceSchemaCompatibilityArtifactKind.ClaimScanSnapshot,
                EvidenceIntelligenceSchemaCompatibilityArtifactKind.ActionScanSnapshot,
                EvidenceIntelligenceSchemaCompatibilityArtifactKind.EvidenceGraphNode,
                EvidenceIntelligenceSchemaCompatibilityArtifactKind.EvidenceGraphEdge,
                EvidenceIntelligenceSchemaCompatibilityArtifactKind.ReadinessMatrixSnapshot,
                EvidenceIntelligenceSchemaCompatibilityArtifactKind.SafeNextStep,
                EvidenceIntelligenceSchemaCompatibilityArtifactKind.RedactionMetadata,
                EvidenceIntelligenceSchemaCompatibilityArtifactKind.IntegrityHashEnvelope,
                EvidenceIntelligenceSchemaCompatibilityArtifactKind.MigrationPlan
            }.ToList(),
            artifactKinds.ToList());
        CollectionAssert.IsSubsetOf(
            new[]
            {
                EvidenceIntelligenceSchemaCompatibilityIssueKind.None,
                EvidenceIntelligenceSchemaCompatibilityIssueKind.MissingRequiredField,
                EvidenceIntelligenceSchemaCompatibilityIssueKind.UnknownEnumValue,
                EvidenceIntelligenceSchemaCompatibilityIssueKind.DeprecatedFieldPresent,
                EvidenceIntelligenceSchemaCompatibilityIssueKind.ExtraUnknownFieldPolicy,
                EvidenceIntelligenceSchemaCompatibilityIssueKind.UnknownSchemaVersion,
                EvidenceIntelligenceSchemaCompatibilityIssueKind.FutureUnsupportedSchemaVersion,
                EvidenceIntelligenceSchemaCompatibilityIssueKind.SchemaDowngradeAttempt,
                EvidenceIntelligenceSchemaCompatibilityIssueKind.GraphNodeMissingId,
                EvidenceIntelligenceSchemaCompatibilityIssueKind.GraphEdgeUnknownRelation,
                EvidenceIntelligenceSchemaCompatibilityIssueKind.EvidenceItemMissingSource,
                EvidenceIntelligenceSchemaCompatibilityIssueKind.ClaimScanMissingConfidence,
                EvidenceIntelligenceSchemaCompatibilityIssueKind.ActionScanMissingRequiredAction,
                EvidenceIntelligenceSchemaCompatibilityIssueKind.ReadinessMatrixUnknownState,
                EvidenceIntelligenceSchemaCompatibilityIssueKind.SafeNextStepMissingGuard,
                EvidenceIntelligenceSchemaCompatibilityIssueKind.RedactionMetadataMissing,
                EvidenceIntelligenceSchemaCompatibilityIssueKind.RedactionMetadataUnknownSensitivity,
                EvidenceIntelligenceSchemaCompatibilityIssueKind.IntegrityHashMissing,
                EvidenceIntelligenceSchemaCompatibilityIssueKind.IntegrityHashBeforeRedaction,
                EvidenceIntelligenceSchemaCompatibilityIssueKind.MigrationPlanTargetIncompatible
            }.ToList(),
            issueKinds.ToList());
    }

    [TestMethod]
    public void SchemaCompatibilityStatus_RemainsDisabledAndFailClosed()
    {
        var status = EvidenceIntelligenceSchemaCompatibilityStatus.DisabledDesignOnlyGuard();

        Assert.IsTrue(status.DesignExists);
        Assert.IsTrue(status.GuardExists);
        Assert.IsTrue(status.DesignOnly);
        Assert.IsTrue(status.FailClosed);
        Assert.AreEqual("SCHEMA_COMPATIBILITY_DESIGN_ONLY_FAIL_CLOSED", status.Mode);
        Assert.IsFalse(status.DurablePersistenceEnabled);
        Assert.IsFalse(status.FilesystemReadEnabled);
        Assert.IsFalse(status.FilesystemWriteEnabled);
        Assert.IsFalse(status.DatabaseEnabled);
        Assert.IsFalse(status.MigrationRunnerEnabled);
        Assert.IsFalse(status.MigrationExecutionEnabled);
        Assert.IsFalse(status.ProviderCloudEnabled);
        Assert.IsFalse(status.SemanticVectorBackendEnabled);
        Assert.IsFalse(status.RuntimeEnabled);
        Assert.IsFalse(status.ServiceRegistrationEnabled);
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

    private static IReadOnlyList<EvidenceIntelligenceWriteCommand> CreateWriteStoreCommands() =>
    [
        EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("evidence.fixture.001"),
        EvidenceIntelligenceWriteCommand.AppendClaimScanSnapshot("claim-scan.fixture.001"),
        EvidenceIntelligenceWriteCommand.AppendActionScanSnapshot("action-scan.fixture.001"),
        EvidenceIntelligenceWriteCommand.AppendContradictionRecord("contradiction.fixture.001"),
        EvidenceIntelligenceWriteCommand.AppendGraphNode("graph-node.fixture.001"),
        EvidenceIntelligenceWriteCommand.AppendGraphEdge("graph-edge.fixture.001"),
        EvidenceIntelligenceWriteCommand.AppendReadinessSnapshot("readiness.fixture.001"),
        EvidenceIntelligenceWriteCommand.AppendSafeNextStep("safe-next-step.fixture.001"),
        EvidenceIntelligenceWriteCommand.AppendHumanActionRequirement("human-action.fixture.001"),
        EvidenceIntelligenceWriteCommand.AppendRedactionMetadata("redaction.fixture.001"),
        EvidenceIntelligenceWriteCommand.AppendIntegrityHashEnvelope("integrity-hash.fixture.001")
    ];

    private static IReadOnlyList<HostileRedactionFixture> CreateHostileRedactionFixtures() =>
    [
        Fixture(
            "hostile.api-key.synthetic",
            "api-key",
            "SYNTHETIC sk-fixture-not-real appears in display text",
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("hostile.api-key.synthetic", EvidenceIntelligencePersistenceFieldClassification.SafeToDisplay, containsSecretField: true),
            containsSecretLikeContent: true),
        Fixture(
            "hostile.bearer-token.synthetic",
            "bearer-token",
            "Bearer SYNTHETIC-fixture-token-not-real",
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("hostile.bearer-token.synthetic", EvidenceIntelligencePersistenceFieldClassification.RedactedBeforeWrite, containsSecretField: true),
            containsSecretLikeContent: true),
        Fixture(
            "hostile.jwt.synthetic",
            "jwt",
            "eyJ.fixture.not-real.SYNTHETIC",
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("hostile.jwt.synthetic", EvidenceIntelligencePersistenceFieldClassification.RedactedBeforeWrite, containsSecretField: true),
            containsSecretLikeContent: true),
        Fixture(
            "hostile.github-token.synthetic",
            "github-token",
            "ghp_fixture SYNTHETIC not-real",
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("hostile.github-token.synthetic", EvidenceIntelligencePersistenceFieldClassification.RedactedBeforeWrite, containsSecretField: true),
            containsSecretLikeContent: true),
        Fixture(
            "hostile.aws-access-key.synthetic",
            "aws-access-key",
            "AKIA-FIXTURE-NOT-REAL-SYNTHETIC",
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("hostile.aws-access-key.synthetic", EvidenceIntelligencePersistenceFieldClassification.RedactedBeforeWrite, containsSecretField: true),
            containsSecretLikeContent: true),
        Fixture(
            "hostile.private-key.synthetic",
            "private-key",
            "-----BEGIN SYNTHETIC PRIVATE KEY----- fixture-not-real",
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("hostile.private-key.synthetic", EvidenceIntelligencePersistenceFieldClassification.SensitiveNeverPersist, containsRawPayload: true, containsSecretField: true),
            containsRawPayload: true,
            containsSecretLikeContent: true),
        Fixture(
            "hostile.credential-pair.synthetic",
            "credential-pair",
            "username=fixture-user password=SYNTHETIC-not-real",
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("hostile.credential-pair.synthetic", EvidenceIntelligencePersistenceFieldClassification.SensitiveNeverPersist, containsSecretField: true),
            containsSecretLikeContent: true),
        Fixture(
            "hostile.cookie-session.synthetic",
            "cookie-session",
            "Cookie: session=SYNTHETIC-fixture-not-real",
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("hostile.cookie-session.synthetic", EvidenceIntelligencePersistenceFieldClassification.SensitiveNeverPersist, containsRawPayload: true, containsSecretField: true),
            containsRawPayload: true,
            containsSecretLikeContent: true),
        Fixture(
            "hostile.synthetic-pii.contact",
            "synthetic-pii",
            "SYNTHETIC person fixture@example.invalid +1-555-0100",
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("hostile.synthetic-pii.contact", EvidenceIntelligencePersistenceFieldClassification.SensitiveNeverPersist),
            containsSecretLikeContent: true),
        Fixture(
            "hostile.synthetic-fiscal-id",
            "synthetic-fiscal-id",
            "SYNTHETIC fiscal id CUIT-00-00000000-0 fixture",
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("hostile.synthetic-fiscal-id", EvidenceIntelligencePersistenceFieldClassification.SensitiveNeverPersist),
            containsSecretLikeContent: true),
        Fixture(
            "hostile.raw-ocr",
            "raw-ocr",
            "SYNTHETIC raw OCR payload with coordinates fixture",
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("hostile.raw-ocr", EvidenceIntelligencePersistenceFieldClassification.RedactedBeforeWrite, containsRawPayload: true),
            containsRawPayload: true),
        Fixture(
            "hostile.raw-browser-cdp",
            "raw-browser-cdp",
            "SYNTHETIC raw browser CDP fixture payload",
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("hostile.raw-browser-cdp", EvidenceIntelligencePersistenceFieldClassification.RedactedBeforeWrite, containsRawPayload: true),
            containsRawPayload: true),
        Fixture(
            "hostile.raw-wcu",
            "raw-wcu",
            "SYNTHETIC raw WCU fixture payload",
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("hostile.raw-wcu", EvidenceIntelligencePersistenceFieldClassification.RedactedBeforeWrite, containsRawPayload: true),
            containsRawPayload: true),
        Fixture(
            "hostile.unknown-sensitivity",
            "unknown-sensitivity",
            "SYNTHETIC unknown sensitivity fixture",
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("hostile.unknown-sensitivity", sensitivityKnown: false),
            sensitivityKnown: false),
        Fixture(
            "hostile.sensitive-never-persist",
            "sensitive-never-persist",
            "SYNTHETIC sensitive never persist fixture",
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("hostile.sensitive-never-persist", EvidenceIntelligencePersistenceFieldClassification.SensitiveNeverPersist)),
        Fixture(
            "hostile.mixed-safe-secret",
            "mixed-safe-secret",
            "safe summary plus sk-fixture-not-real SYNTHETIC token",
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("hostile.mixed-safe-secret", EvidenceIntelligencePersistenceFieldClassification.SafeToDisplay, containsSecretField: true),
            containsSecretLikeContent: true),
        Fixture(
            "hostile.redacted-looking-raw",
            "redacted-looking-raw",
            "[REDACTED] SYNTHETIC but raw payload still present fixture",
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("hostile.redacted-looking-raw", EvidenceIntelligencePersistenceFieldClassification.RedactedBeforeWrite, containsRawPayload: true),
            containsRawPayload: true),
        Fixture(
            "hostile.integrity-before-redaction",
            "integrity-before-redaction",
            "SYNTHETIC integrity hash envelope before redaction fixture",
            EvidenceIntelligenceWriteCommand.AppendIntegrityHashEnvelope("hostile.integrity-before-redaction", integrityHashBeforeCanonicalRedaction: true),
            integrityHashBeforeRedaction: true),
        Fixture(
            "hostile.graph-sensitive",
            "graph-sensitive",
            "SYNTHETIC graph edge/node contains secret-like fixture",
            EvidenceIntelligenceWriteCommand.AppendGraphEdge("hostile.graph-sensitive", EvidenceIntelligencePersistenceFieldClassification.FuturePersisted, containsSecretField: true),
            containsSecretLikeContent: true),
        Fixture(
            "hostile.safe-next-step-secret",
            "safe-next-step-secret",
            "Safe next step accidentally embeds Bearer SYNTHETIC-fixture-not-real",
            EvidenceIntelligenceWriteCommand.AppendSafeNextStep("hostile.safe-next-step-secret", containsSecretField: true),
            containsSecretLikeContent: true)
    ];

    private static HostileRedactionFixture Fixture(
        string fixtureId,
        string category,
        string payload,
        EvidenceIntelligenceWriteCommand command,
        bool sensitivityKnown = true,
        bool containsRawPayload = false,
        bool containsSecretLikeContent = false,
        bool integrityHashBeforeRedaction = false) =>
        new(
            fixtureId,
            category,
            payload,
            command,
            ExpectedDecision: EvidenceIntelligenceWriteResultStatus.Rejected,
            ExpectedRejectionReason: "rejected by design",
            SensitivityKnown: sensitivityKnown,
            ContainsRawPayload: containsRawPayload,
            ContainsSecretLikeContent: containsSecretLikeContent,
            IntegrityHashBeforeRedaction: integrityHashBeforeRedaction,
            PersistAllowed: false);

    private static IReadOnlyList<DryRunMigrationPlanFixture> CreateDryRunMigrationPlanFixtures() =>
    [
        DryRunFixture(
            "dry-run.same-schema.no-op",
            EvidenceIntelligenceDryRunMigrationPlan.SameSchemaNoOp(),
            EvidenceIntelligenceDryRunMigrationDecision.NoOp,
            null),
        DryRunFixture(
            "dry-run.future-schema-unsupported",
            Plan("dry-run.future-schema-unsupported", EvidenceIntelligenceSchemaVersionDescriptor.V1(), EvidenceIntelligenceSchemaVersionDescriptor.FutureUnsupported(2, 0), Step("future-schema-check")),
            EvidenceIntelligenceDryRunMigrationDecision.Blocked,
            EvidenceIntelligenceDryRunMigrationBlockerCode.FutureSchemaUnsupported),
        DryRunFixture(
            "dry-run.unknown-schema",
            Plan("dry-run.unknown-schema", EvidenceIntelligenceSchemaVersionDescriptor.Unknown(), EvidenceIntelligenceSchemaVersionDescriptor.V1(), Step("unknown-schema-check")),
            EvidenceIntelligenceDryRunMigrationDecision.Blocked,
            EvidenceIntelligenceDryRunMigrationBlockerCode.UnknownSchemaVersion),
        DryRunFixture(
            "dry-run.write-required",
            Plan("dry-run.write-required", EvidenceIntelligenceSchemaVersionDescriptor.V1(), EvidenceIntelligenceSchemaVersionDescriptor.V1(), Step("write-required", requiresFilesystemWrite: true, requiresDurableWrite: true)),
            EvidenceIntelligenceDryRunMigrationDecision.Blocked,
            EvidenceIntelligenceDryRunMigrationBlockerCode.RequiresFilesystemWrite),
        DryRunFixture(
            "dry-run.read-required",
            Plan("dry-run.read-required", EvidenceIntelligenceSchemaVersionDescriptor.V1(), EvidenceIntelligenceSchemaVersionDescriptor.V1(), Step("read-required", requiresFilesystemRead: true)),
            EvidenceIntelligenceDryRunMigrationDecision.Blocked,
            EvidenceIntelligenceDryRunMigrationBlockerCode.RequiresFilesystemRead),
        DryRunFixture(
            "dry-run.database-required",
            Plan("dry-run.database-required", EvidenceIntelligenceSchemaVersionDescriptor.V1(), EvidenceIntelligenceSchemaVersionDescriptor.V1(), Step("database-required", requiresDatabase: true)),
            EvidenceIntelligenceDryRunMigrationDecision.Blocked,
            EvidenceIntelligenceDryRunMigrationBlockerCode.RequiresDatabase),
        DryRunFixture(
            "dry-run.runner-required",
            Plan("dry-run.runner-required", EvidenceIntelligenceSchemaVersionDescriptor.V1(), EvidenceIntelligenceSchemaVersionDescriptor.V1(), Step("runner-required", requiresMigrationRunner: true)),
            EvidenceIntelligenceDryRunMigrationDecision.Blocked,
            EvidenceIntelligenceDryRunMigrationBlockerCode.RequiresMigrationRunner),
        DryRunFixture(
            "dry-run.redaction-gate-missing",
            Plan("dry-run.redaction-gate-missing", EvidenceIntelligenceSchemaVersionDescriptor.V1(), EvidenceIntelligenceSchemaVersionDescriptor.V1(), Step("redaction-gate-missing", redactionGateSatisfied: false)),
            EvidenceIntelligenceDryRunMigrationDecision.Blocked,
            EvidenceIntelligenceDryRunMigrationBlockerCode.RedactionGateMissing),
        DryRunFixture(
            "dry-run.raw-payload-risk",
            Plan("dry-run.raw-payload-risk", EvidenceIntelligenceSchemaVersionDescriptor.V1(), EvidenceIntelligenceSchemaVersionDescriptor.V1(), Step("raw-payload-risk", hasRawPayloadRisk: true)),
            EvidenceIntelligenceDryRunMigrationDecision.Blocked,
            EvidenceIntelligenceDryRunMigrationBlockerCode.RawPayloadRisk),
        DryRunFixture(
            "dry-run.rollback-unavailable",
            Plan("dry-run.rollback-unavailable", EvidenceIntelligenceSchemaVersionDescriptor.V1(), EvidenceIntelligenceSchemaVersionDescriptor.V1(), Step("rollback-unavailable", canRollback: false)),
            EvidenceIntelligenceDryRunMigrationDecision.Blocked,
            EvidenceIntelligenceDryRunMigrationBlockerCode.RollbackUnavailable),
        DryRunFixture(
            "dry-run.human-approval-required",
            Plan("dry-run.human-approval-required", EvidenceIntelligenceSchemaVersionDescriptor.V1(), EvidenceIntelligenceSchemaVersionDescriptor.V1(), Step("human-approval-required", requiresHumanApproval: true, humanApprovalPresent: false)),
            EvidenceIntelligenceDryRunMigrationDecision.Blocked,
            EvidenceIntelligenceDryRunMigrationBlockerCode.HumanApprovalRequired),
        DryRunFixture(
            "dry-run.schema-downgrade",
            Plan("dry-run.schema-downgrade", EvidenceIntelligenceSchemaVersionDescriptor.V1(), new EvidenceIntelligenceSchemaVersionDescriptor(0, 9, IsKnown: true, IsSupported: true, Label: "eil.local-evidence.schema.v0.9.downgrade-blocked"), Step("schema-downgrade")),
            EvidenceIntelligenceDryRunMigrationDecision.Blocked,
            EvidenceIntelligenceDryRunMigrationBlockerCode.SchemaDowngrade),
        DryRunFixture(
            "dry-run.incompatible-graph-edge",
            Plan("dry-run.incompatible-graph-edge", EvidenceIntelligenceSchemaVersionDescriptor.V1(), EvidenceIntelligenceSchemaVersionDescriptor.V1(), Step("incompatible-graph-edge", hasIncompatibleGraphShape: true)),
            EvidenceIntelligenceDryRunMigrationDecision.Blocked,
            EvidenceIntelligenceDryRunMigrationBlockerCode.IncompatibleGraphEdgeShape),
        DryRunFixture(
            "dry-run.stale-evidence-version",
            Plan("dry-run.stale-evidence-version", EvidenceIntelligenceSchemaVersionDescriptor.V1(), EvidenceIntelligenceSchemaVersionDescriptor.V1(), Step("stale-evidence-version", hasStaleEvidenceVersion: true)),
            EvidenceIntelligenceDryRunMigrationDecision.Blocked,
            EvidenceIntelligenceDryRunMigrationBlockerCode.StaleEvidenceVersion),
        DryRunFixture(
            "dry-run.unsafe-integrity-hash",
            Plan("dry-run.unsafe-integrity-hash", EvidenceIntelligenceSchemaVersionDescriptor.V1(), EvidenceIntelligenceSchemaVersionDescriptor.V1(), Step("unsafe-integrity-hash", hasUnsafeIntegrityHashPlan: true)),
            EvidenceIntelligenceDryRunMigrationDecision.Blocked,
            EvidenceIntelligenceDryRunMigrationBlockerCode.UnsafeIntegrityHashPlan)
    ];

    private static DryRunMigrationPlanFixture DryRunFixture(
        string fixtureId,
        EvidenceIntelligenceDryRunMigrationPlan plan,
        EvidenceIntelligenceDryRunMigrationDecision expectedDecision,
        EvidenceIntelligenceDryRunMigrationBlockerCode? expectedBlocker) =>
        new(fixtureId, plan, expectedDecision, expectedBlocker);

    private static EvidenceIntelligenceDryRunMigrationPlan Plan(
        string planId,
        EvidenceIntelligenceSchemaVersionDescriptor current,
        EvidenceIntelligenceSchemaVersionDescriptor target,
        EvidenceIntelligenceDryRunMigrationStep step) =>
        new(
            PlanId: planId,
            CurrentSchemaVersion: current,
            TargetSchemaVersion: target,
            DryRunOnly: true,
            RequiresHumanApproval: false,
            HumanApprovalPresent: false,
            Steps: [step],
            ExpectedAuditEvidence:
            [
                "Schema compatibility comparison.",
                "No-side-effect flag proof.",
                "Future human approval before activation."
            ]);

    private static EvidenceIntelligenceDryRunMigrationStep Step(
        string stepId,
        bool requiresFilesystemRead = false,
        bool requiresFilesystemWrite = false,
        bool requiresDatabase = false,
        bool requiresMigrationRunner = false,
        bool requiresDurableWrite = false,
        bool redactionGateSatisfied = true,
        bool requiresHumanApproval = false,
        bool humanApprovalPresent = true,
        bool canRollback = true,
        bool hasRawPayloadRisk = false,
        bool hasIncompatibleGraphShape = false,
        bool hasStaleEvidenceVersion = false,
        bool hasUnsafeIntegrityHashPlan = false) =>
        new(
            StepId: stepId,
            Kind: EvidenceIntelligenceDryRunMigrationStepKind.SchemaVersionCheck,
            RequiresFilesystemRead: requiresFilesystemRead,
            RequiresFilesystemWrite: requiresFilesystemWrite,
            RequiresDatabase: requiresDatabase,
            RequiresMigrationRunner: requiresMigrationRunner,
            RequiresDurableWrite: requiresDurableWrite,
            RequiresRedactionAtWrite: true,
            RedactionGateSatisfied: redactionGateSatisfied,
            RequiresHumanApproval: requiresHumanApproval,
            HumanApprovalPresent: humanApprovalPresent,
            CanRollback: canRollback,
            HasRawPayloadRisk: hasRawPayloadRisk,
            HasIncompatibleGraphShape: hasIncompatibleGraphShape,
            HasStaleEvidenceVersion: hasStaleEvidenceVersion,
            HasUnsafeIntegrityHashPlan: hasUnsafeIntegrityHashPlan,
            ExpectedEvidenceKind: "dry-run-migration-plan-fixture");

    private static IReadOnlyList<SchemaCompatibilityFixture> CreateSchemaCompatibilityFixtures() =>
    [
        SchemaFixture(
            "v1_known_compatible",
            EvidenceIntelligenceSchemaCompatibilityCheck.V1KnownCompatible(),
            EvidenceIntelligenceSchemaCompatibilityDecision.CompatibleDesignOnly,
            EvidenceIntelligenceSchemaCompatibilityIssueKind.None,
            migrationCouldBePlanned: true),
        SchemaFixture(
            "v1_missing_required_field",
            Check("v1_missing_required_field", EvidenceIntelligenceSchemaCompatibilityArtifactKind.EvidenceRecord, EvidenceIntelligenceSchemaCompatibilityIssueKind.MissingRequiredField, requiredFieldsPresent: false),
            EvidenceIntelligenceSchemaCompatibilityDecision.Blocked,
            EvidenceIntelligenceSchemaCompatibilityIssueKind.MissingRequiredField),
        SchemaFixture(
            "v1_unknown_enum_value",
            Check("v1_unknown_enum_value", EvidenceIntelligenceSchemaCompatibilityArtifactKind.EvidenceSource, EvidenceIntelligenceSchemaCompatibilityIssueKind.UnknownEnumValue, enumValuesKnown: false),
            EvidenceIntelligenceSchemaCompatibilityDecision.Blocked,
            EvidenceIntelligenceSchemaCompatibilityIssueKind.UnknownEnumValue),
        SchemaFixture(
            "v1_deprecated_field_present",
            Check("v1_deprecated_field_present", EvidenceIntelligenceSchemaCompatibilityArtifactKind.EvidenceRecord, EvidenceIntelligenceSchemaCompatibilityIssueKind.DeprecatedFieldPresent, deprecatedFieldPresent: true),
            EvidenceIntelligenceSchemaCompatibilityDecision.Blocked,
            EvidenceIntelligenceSchemaCompatibilityIssueKind.DeprecatedFieldPresent),
        SchemaFixture(
            "v1_extra_unknown_field_policy",
            Check("v1_extra_unknown_field_policy", EvidenceIntelligenceSchemaCompatibilityArtifactKind.EvidenceRecord, EvidenceIntelligenceSchemaCompatibilityIssueKind.ExtraUnknownFieldPolicy, extraUnknownFieldPresent: true),
            EvidenceIntelligenceSchemaCompatibilityDecision.Blocked,
            EvidenceIntelligenceSchemaCompatibilityIssueKind.ExtraUnknownFieldPolicy),
        SchemaFixture(
            "unknown_schema_version",
            Check("unknown_schema_version", EvidenceIntelligenceSchemaCompatibilityArtifactKind.EvidenceRecord, EvidenceIntelligenceSchemaCompatibilityIssueKind.UnknownSchemaVersion, current: EvidenceIntelligenceSchemaVersionDescriptor.Unknown()),
            EvidenceIntelligenceSchemaCompatibilityDecision.Blocked,
            EvidenceIntelligenceSchemaCompatibilityIssueKind.UnknownSchemaVersion),
        SchemaFixture(
            "future_unsupported_schema_version",
            Check("future_unsupported_schema_version", EvidenceIntelligenceSchemaCompatibilityArtifactKind.EvidenceRecord, EvidenceIntelligenceSchemaCompatibilityIssueKind.FutureUnsupportedSchemaVersion, target: EvidenceIntelligenceSchemaVersionDescriptor.FutureUnsupported(2, 0)),
            EvidenceIntelligenceSchemaCompatibilityDecision.Blocked,
            EvidenceIntelligenceSchemaCompatibilityIssueKind.FutureUnsupportedSchemaVersion),
        SchemaFixture(
            "schema_downgrade_attempt",
            Check("schema_downgrade_attempt", EvidenceIntelligenceSchemaCompatibilityArtifactKind.MigrationPlan, EvidenceIntelligenceSchemaCompatibilityIssueKind.SchemaDowngradeAttempt, target: new EvidenceIntelligenceSchemaVersionDescriptor(0, 9, IsKnown: true, IsSupported: true, Label: "eil.local-evidence.schema.v0.9.downgrade-blocked")),
            EvidenceIntelligenceSchemaCompatibilityDecision.Blocked,
            EvidenceIntelligenceSchemaCompatibilityIssueKind.SchemaDowngradeAttempt),
        SchemaFixture(
            "graph_node_missing_id",
            Check("graph_node_missing_id", EvidenceIntelligenceSchemaCompatibilityArtifactKind.EvidenceGraphNode, EvidenceIntelligenceSchemaCompatibilityIssueKind.GraphNodeMissingId, requiredFieldsPresent: false),
            EvidenceIntelligenceSchemaCompatibilityDecision.Blocked,
            EvidenceIntelligenceSchemaCompatibilityIssueKind.GraphNodeMissingId),
        SchemaFixture(
            "graph_edge_unknown_relation",
            Check("graph_edge_unknown_relation", EvidenceIntelligenceSchemaCompatibilityArtifactKind.EvidenceGraphEdge, EvidenceIntelligenceSchemaCompatibilityIssueKind.GraphEdgeUnknownRelation, enumValuesKnown: false),
            EvidenceIntelligenceSchemaCompatibilityDecision.Blocked,
            EvidenceIntelligenceSchemaCompatibilityIssueKind.GraphEdgeUnknownRelation),
        SchemaFixture(
            "evidence_item_missing_source",
            Check("evidence_item_missing_source", EvidenceIntelligenceSchemaCompatibilityArtifactKind.EvidenceSource, EvidenceIntelligenceSchemaCompatibilityIssueKind.EvidenceItemMissingSource, requiredFieldsPresent: false),
            EvidenceIntelligenceSchemaCompatibilityDecision.Blocked,
            EvidenceIntelligenceSchemaCompatibilityIssueKind.EvidenceItemMissingSource),
        SchemaFixture(
            "claim_scan_missing_confidence",
            Check("claim_scan_missing_confidence", EvidenceIntelligenceSchemaCompatibilityArtifactKind.ClaimScanSnapshot, EvidenceIntelligenceSchemaCompatibilityIssueKind.ClaimScanMissingConfidence, requiredFieldsPresent: false),
            EvidenceIntelligenceSchemaCompatibilityDecision.Blocked,
            EvidenceIntelligenceSchemaCompatibilityIssueKind.ClaimScanMissingConfidence),
        SchemaFixture(
            "action_scan_missing_required_action",
            Check("action_scan_missing_required_action", EvidenceIntelligenceSchemaCompatibilityArtifactKind.ActionScanSnapshot, EvidenceIntelligenceSchemaCompatibilityIssueKind.ActionScanMissingRequiredAction, requiredFieldsPresent: false),
            EvidenceIntelligenceSchemaCompatibilityDecision.Blocked,
            EvidenceIntelligenceSchemaCompatibilityIssueKind.ActionScanMissingRequiredAction),
        SchemaFixture(
            "readiness_matrix_unknown_state",
            Check("readiness_matrix_unknown_state", EvidenceIntelligenceSchemaCompatibilityArtifactKind.ReadinessMatrixSnapshot, EvidenceIntelligenceSchemaCompatibilityIssueKind.ReadinessMatrixUnknownState, enumValuesKnown: false),
            EvidenceIntelligenceSchemaCompatibilityDecision.Blocked,
            EvidenceIntelligenceSchemaCompatibilityIssueKind.ReadinessMatrixUnknownState),
        SchemaFixture(
            "safe_next_step_missing_guard",
            Check("safe_next_step_missing_guard", EvidenceIntelligenceSchemaCompatibilityArtifactKind.SafeNextStep, EvidenceIntelligenceSchemaCompatibilityIssueKind.SafeNextStepMissingGuard, requiredFieldsPresent: false),
            EvidenceIntelligenceSchemaCompatibilityDecision.Blocked,
            EvidenceIntelligenceSchemaCompatibilityIssueKind.SafeNextStepMissingGuard),
        SchemaFixture(
            "redaction_metadata_missing",
            Check("redaction_metadata_missing", EvidenceIntelligenceSchemaCompatibilityArtifactKind.RedactionMetadata, EvidenceIntelligenceSchemaCompatibilityIssueKind.RedactionMetadataMissing, redactionMetadataPresent: false),
            EvidenceIntelligenceSchemaCompatibilityDecision.Blocked,
            EvidenceIntelligenceSchemaCompatibilityIssueKind.RedactionMetadataMissing),
        SchemaFixture(
            "redaction_metadata_unknown_sensitivity",
            Check("redaction_metadata_unknown_sensitivity", EvidenceIntelligenceSchemaCompatibilityArtifactKind.RedactionMetadata, EvidenceIntelligenceSchemaCompatibilityIssueKind.RedactionMetadataUnknownSensitivity, redactionSensitivityKnown: false),
            EvidenceIntelligenceSchemaCompatibilityDecision.Blocked,
            EvidenceIntelligenceSchemaCompatibilityIssueKind.RedactionMetadataUnknownSensitivity),
        SchemaFixture(
            "integrity_hash_missing",
            Check("integrity_hash_missing", EvidenceIntelligenceSchemaCompatibilityArtifactKind.IntegrityHashEnvelope, EvidenceIntelligenceSchemaCompatibilityIssueKind.IntegrityHashMissing, integrityHashPresent: false),
            EvidenceIntelligenceSchemaCompatibilityDecision.Blocked,
            EvidenceIntelligenceSchemaCompatibilityIssueKind.IntegrityHashMissing),
        SchemaFixture(
            "integrity_hash_before_redaction",
            Check("integrity_hash_before_redaction", EvidenceIntelligenceSchemaCompatibilityArtifactKind.IntegrityHashEnvelope, EvidenceIntelligenceSchemaCompatibilityIssueKind.IntegrityHashBeforeRedaction, integrityHashAfterRedaction: false),
            EvidenceIntelligenceSchemaCompatibilityDecision.Blocked,
            EvidenceIntelligenceSchemaCompatibilityIssueKind.IntegrityHashBeforeRedaction),
        SchemaFixture(
            "migration_plan_target_incompatible",
            Check("migration_plan_target_incompatible", EvidenceIntelligenceSchemaCompatibilityArtifactKind.MigrationPlan, EvidenceIntelligenceSchemaCompatibilityIssueKind.MigrationPlanTargetIncompatible, migrationTargetCompatible: false),
            EvidenceIntelligenceSchemaCompatibilityDecision.Blocked,
            EvidenceIntelligenceSchemaCompatibilityIssueKind.MigrationPlanTargetIncompatible)
    ];

    private static SchemaCompatibilityFixture SchemaFixture(
        string fixtureId,
        EvidenceIntelligenceSchemaCompatibilityCheck check,
        EvidenceIntelligenceSchemaCompatibilityDecision expectedDecision,
        EvidenceIntelligenceSchemaCompatibilityIssueKind expectedIssue,
        bool migrationCouldBePlanned = false) =>
        new(fixtureId, check, expectedDecision, expectedIssue, migrationCouldBePlanned, DurablePersistenceAllowed: false);

    private static EvidenceIntelligenceSchemaCompatibilityCheck Check(
        string checkId,
        EvidenceIntelligenceSchemaCompatibilityArtifactKind artifactKind,
        EvidenceIntelligenceSchemaCompatibilityIssueKind issueKind,
        EvidenceIntelligenceSchemaVersionDescriptor? current = null,
        EvidenceIntelligenceSchemaVersionDescriptor? target = null,
        bool requiredFieldsPresent = true,
        bool enumValuesKnown = true,
        bool deprecatedFieldPresent = false,
        bool extraUnknownFieldPresent = false,
        bool redactionMetadataPresent = true,
        bool redactionSensitivityKnown = true,
        bool integrityHashPresent = true,
        bool integrityHashAfterRedaction = true,
        bool migrationTargetCompatible = true) =>
        new(
            CheckId: checkId,
            CurrentSchemaVersion: current ?? EvidenceIntelligenceSchemaVersionDescriptor.V1(),
            TargetSchemaVersion: target ?? EvidenceIntelligenceSchemaVersionDescriptor.V1(),
            ArtifactKind: artifactKind,
            IssueKind: issueKind,
            RequiredFieldsPresent: requiredFieldsPresent,
            EnumValuesKnown: enumValuesKnown,
            DeprecatedFieldPresent: deprecatedFieldPresent,
            ExtraUnknownFieldPresent: extraUnknownFieldPresent,
            RedactionMetadataPresent: redactionMetadataPresent,
            RedactionSensitivityKnown: redactionSensitivityKnown,
            IntegrityHashPresent: integrityHashPresent,
            IntegrityHashAfterRedaction: integrityHashAfterRedaction,
            MigrationTargetCompatible: migrationTargetCompatible,
            DurablePersistenceAllowed: false);

    private sealed record HostileRedactionFixture(
        string FixtureId,
        string Category,
        string SyntheticPayload,
        EvidenceIntelligenceWriteCommand Command,
        EvidenceIntelligenceWriteResultStatus ExpectedDecision,
        string ExpectedRejectionReason,
        bool SensitivityKnown,
        bool ContainsRawPayload,
        bool ContainsSecretLikeContent,
        bool IntegrityHashBeforeRedaction,
        bool PersistAllowed);

    private sealed record DryRunMigrationPlanFixture(
        string FixtureId,
        EvidenceIntelligenceDryRunMigrationPlan Plan,
        EvidenceIntelligenceDryRunMigrationDecision ExpectedDecision,
        EvidenceIntelligenceDryRunMigrationBlockerCode? ExpectedBlocker);

    private sealed record SchemaCompatibilityFixture(
        string FixtureId,
        EvidenceIntelligenceSchemaCompatibilityCheck Check,
        EvidenceIntelligenceSchemaCompatibilityDecision ExpectedDecision,
        EvidenceIntelligenceSchemaCompatibilityIssueKind ExpectedIssue,
        bool ExpectedMigrationCouldBePlanned,
        bool DurablePersistenceAllowed);
}
