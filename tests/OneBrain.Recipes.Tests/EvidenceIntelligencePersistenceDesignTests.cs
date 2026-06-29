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
}
