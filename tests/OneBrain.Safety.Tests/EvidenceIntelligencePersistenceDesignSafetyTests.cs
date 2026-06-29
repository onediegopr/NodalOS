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
    private const string RecipesPersistenceDesignTestsPath = "tests/OneBrain.Recipes.Tests/EvidenceIntelligencePersistenceDesignTests.cs";

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
    public void WriteStoreScaffold_DoesNotReadWriteDatabaseOrMigrate()
    {
        var store = new DisabledEvidenceIntelligenceWriteStore();
        var result = store.Write(EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("evidence.fixture.001"));

        Assert.IsFalse(store.ScaffoldStatus.FilesystemWriteEnabled);
        Assert.IsFalse(store.ScaffoldStatus.DatabaseWriteEnabled);
        Assert.IsFalse(store.ScaffoldStatus.MigrationEnabled);
        Assert.IsFalse(store.ScaffoldStatus.DurableWriteEnabled);
        Assert.IsFalse(result.WritesFilesystem);
        Assert.IsFalse(result.ReadsFilesystem);
        Assert.IsFalse(result.WritesDatabase);
        Assert.IsFalse(result.RunsMigration);
        Assert.IsTrue(result.FailClosed);
    }

    [TestMethod]
    public void WriteStoreScaffold_DoesNotEnableProviderCloudSemanticRuntimeOrServiceRegistration()
    {
        var store = new DisabledEvidenceIntelligenceWriteStore();
        var result = store.Write(EvidenceIntelligenceWriteCommand.AppendSafeNextStep("safe-next-step.fixture.001"));

        Assert.IsFalse(store.ScaffoldStatus.ProviderCloudEnabled);
        Assert.IsFalse(store.ScaffoldStatus.SemanticVectorBackendEnabled);
        Assert.IsFalse(store.ScaffoldStatus.RuntimeEnabled);
        Assert.IsFalse(store.ScaffoldStatus.ServiceRegistrationEnabled);
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
    public void WriteStoreScaffold_RedactionGateIsRequiredButNotExecutable()
    {
        var store = new DisabledEvidenceIntelligenceWriteStore();
        var requirement = store.ScaffoldStatus.RedactionRequirement;
        var rawCommand = EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("raw.fixture.001", containsRawPayload: true);
        var rawResult = store.Write(rawCommand);

        Assert.IsTrue(requirement.RedactionRequired);
        Assert.IsTrue(requirement.RawPayloadNeverPersist);
        Assert.IsTrue(requirement.SecretFieldsRejected);
        Assert.IsTrue(requirement.UnknownSensitivityRejected);
        Assert.IsTrue(requirement.IntegrityHashAfterCanonicalRedaction);
        Assert.IsFalse(requirement.ExecutablePipelineEnabled);
        Assert.AreEqual(EvidenceIntelligenceWriteResultStatus.Rejected, rawResult.Status);
        Assert.IsFalse(rawResult.RedactionPipelineExecuted);
        Assert.IsFalse(rawResult.WritesFilesystem);
        Assert.IsFalse(rawResult.WritesDatabase);
    }

    [TestMethod]
    public void RedactionAtWriteHostileCommands_DoNotWriteReadMigrateOrFallback()
    {
        var store = new DisabledEvidenceIntelligenceWriteStore();
        var commands = new[]
        {
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("hostile.api-key.synthetic", EvidenceIntelligencePersistenceFieldClassification.SafeToDisplay, containsSecretField: true),
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("hostile.raw-browser-cdp", EvidenceIntelligencePersistenceFieldClassification.RedactedBeforeWrite, containsRawPayload: true),
            EvidenceIntelligenceWriteCommand.AppendEvidenceRecord("hostile.unknown-sensitivity", sensitivityKnown: false),
            EvidenceIntelligenceWriteCommand.AppendIntegrityHashEnvelope("hostile.integrity-before-redaction", integrityHashBeforeCanonicalRedaction: true),
            EvidenceIntelligenceWriteCommand.AppendGraphNode("hostile.graph-node", containsSecretField: true),
            EvidenceIntelligenceWriteCommand.AppendSafeNextStep("hostile.safe-next-step", containsSecretField: true)
        };

        foreach (var command in commands)
        {
            var result = store.Write(command);

            Assert.AreEqual(EvidenceIntelligenceWriteResultStatus.Rejected, result.Status, command.TargetId);
            Assert.IsTrue(result.FailClosed, command.TargetId);
            Assert.IsFalse(result.WritesFilesystem, command.TargetId);
            Assert.IsFalse(result.ReadsFilesystem, command.TargetId);
            Assert.IsFalse(result.WritesDatabase, command.TargetId);
            Assert.IsFalse(result.RunsMigration, command.TargetId);
            Assert.IsFalse(result.CallsProviderCloud, command.TargetId);
            Assert.IsFalse(result.UsesSemanticVectorBackend, command.TargetId);
            Assert.IsFalse(result.UsesRuntime, command.TargetId);
            Assert.IsFalse(result.RedactionPipelineExecuted, command.TargetId);
            Assert.IsFalse(result.FallbackUsed, command.TargetId);
        }
    }

    [TestMethod]
    public void RedactionAtWriteHostileFixtureSource_UsesSyntheticSecretLikeValuesOnly()
    {
        var source = ReadRepoText(RecipesPersistenceDesignTestsPath);

        StringAssert.Contains(source, "sk-fixture-not-real");
        StringAssert.Contains(source, "Bearer SYNTHETIC-fixture-token-not-real");
        StringAssert.Contains(source, "ghp_fixture SYNTHETIC not-real");
        StringAssert.Contains(source, "AKIA-FIXTURE-NOT-REAL-SYNTHETIC");
        StringAssert.Contains(source, "BEGIN SYNTHETIC PRIVATE KEY");
        Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(source, @"(?<![A-Za-z0-9_-])sk-[A-Za-z0-9]{16,}"));
        Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(source, @"ghp_[A-Za-z0-9_]{20,}"));
        Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(source, @"AKIA[0-9A-Z]{16}"));
    }

    [TestMethod]
    public void RedactionAtWriteHostileFixtureSource_HasNoRedactionOrPersistenceOverclaim()
    {
        var source = ReadRepoText(PersistenceDesignPath) + ReadRepoText(RecipesPersistenceDesignTestsPath);
        var forbidden = new[]
        {
            "ExecutablePipelineEnabled: true",
            "RedactionPipelineExecuted: true",
            "WritesFilesystem: true",
            "WritesDatabase: true",
            "DurableWritesEnabled: true",
            "FilesystemProductWritesEnabled: true",
            "production-ready",
            "semantic search enabled"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(source.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
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
        var writeStore = new DisabledEvidenceIntelligenceWriteStore();

        Assert.IsTrue(mount.UsesDeterministicFixture);
        Assert.IsFalse(mount.DurablePersistenceEnabled);
        Assert.IsFalse(mount.FilesystemWritesEnabled);
        Assert.IsFalse(mount.ProviderCloudEnabled);
        Assert.IsFalse(mount.RuntimeEnabled);
        Assert.IsFalse(store.ScaffoldStatus.DurableReadEnabled);
        Assert.IsFalse(writeStore.ScaffoldStatus.DurableWriteEnabled);
        StringAssert.Contains(source, "UsesDeterministicFixture: true");
        StringAssert.Contains(source, "DurablePersistenceEnabled: false");
        StringAssert.Contains(source, "FilesystemWritesEnabled: false");
    }

    [TestMethod]
    public void DryRunMigrationPlan_DoesNotExecuteTouchFilesystemDatabaseOrRunner()
    {
        var step = new EvidenceIntelligenceDryRunMigrationStep(
            StepId: "safety.dangerous-step",
            Kind: EvidenceIntelligenceDryRunMigrationStepKind.FutureWritePlanCheck,
            RequiresFilesystemRead: true,
            RequiresFilesystemWrite: true,
            RequiresDatabase: true,
            RequiresMigrationRunner: true,
            RequiresDurableWrite: true,
            RequiresRedactionAtWrite: true,
            RedactionGateSatisfied: false,
            RequiresHumanApproval: true,
            HumanApprovalPresent: false,
            CanRollback: false,
            HasRawPayloadRisk: true,
            HasIncompatibleGraphShape: true,
            HasStaleEvidenceVersion: true,
            HasUnsafeIntegrityHashPlan: true,
            ExpectedEvidenceKind: "safety-fixture-only");
        var plan = new EvidenceIntelligenceDryRunMigrationPlan(
            PlanId: "safety.dangerous-dry-run-plan",
            CurrentSchemaVersion: EvidenceIntelligenceSchemaVersionDescriptor.V1(),
            TargetSchemaVersion: EvidenceIntelligenceSchemaVersionDescriptor.V1(),
            DryRunOnly: true,
            RequiresHumanApproval: true,
            HumanApprovalPresent: false,
            Steps: [step],
            ExpectedAuditEvidence: ["No-side-effect proof."]);

        var result = EvidenceIntelligenceDryRunMigrationPlanner.Evaluate(plan);

        Assert.AreEqual(EvidenceIntelligenceDryRunMigrationDecision.Blocked, result.Decision);
        Assert.IsTrue(result.FailClosed);
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
        Assert.IsTrue(result.Blockers.Any(blocker => blocker.Code == EvidenceIntelligenceDryRunMigrationBlockerCode.RequiresFilesystemRead));
        Assert.IsTrue(result.Blockers.Any(blocker => blocker.Code == EvidenceIntelligenceDryRunMigrationBlockerCode.RequiresFilesystemWrite));
        Assert.IsTrue(result.Blockers.Any(blocker => blocker.Code == EvidenceIntelligenceDryRunMigrationBlockerCode.RequiresDatabase));
        Assert.IsTrue(result.Blockers.Any(blocker => blocker.Code == EvidenceIntelligenceDryRunMigrationBlockerCode.RequiresMigrationRunner));
    }

    [TestMethod]
    public void DryRunMigrationPlan_BlocksRawPayloadUnknownSchemaRedactionAndUnsafeIntegrity()
    {
        var plan = new EvidenceIntelligenceDryRunMigrationPlan(
            PlanId: "safety.redaction-schema-integrity-blockers",
            CurrentSchemaVersion: EvidenceIntelligenceSchemaVersionDescriptor.Unknown(),
            TargetSchemaVersion: EvidenceIntelligenceSchemaVersionDescriptor.FutureUnsupported(2, 0),
            DryRunOnly: true,
            RequiresHumanApproval: false,
            HumanApprovalPresent: false,
            Steps:
            [
                new EvidenceIntelligenceDryRunMigrationStep(
                    StepId: "safety.raw-redaction-integrity",
                    Kind: EvidenceIntelligenceDryRunMigrationStepKind.IntegrityHashCheck,
                    RequiresFilesystemRead: false,
                    RequiresFilesystemWrite: false,
                    RequiresDatabase: false,
                    RequiresMigrationRunner: false,
                    RequiresDurableWrite: false,
                    RequiresRedactionAtWrite: true,
                    RedactionGateSatisfied: false,
                    RequiresHumanApproval: false,
                    HumanApprovalPresent: false,
                    CanRollback: true,
                    HasRawPayloadRisk: true,
                    HasIncompatibleGraphShape: false,
                    HasStaleEvidenceVersion: false,
                    HasUnsafeIntegrityHashPlan: true,
                    ExpectedEvidenceKind: "safety-fixture-only")
            ],
            ExpectedAuditEvidence: ["No-side-effect proof."]);

        var result = EvidenceIntelligenceDryRunMigrationPlanner.Evaluate(plan);
        var blockerCodes = result.Blockers.Select(blocker => blocker.Code).ToHashSet();

        Assert.AreEqual(EvidenceIntelligenceDryRunMigrationDecision.Blocked, result.Decision);
        CollectionAssert.IsSubsetOf(
            new[]
            {
                EvidenceIntelligenceDryRunMigrationBlockerCode.UnknownSchemaVersion,
                EvidenceIntelligenceDryRunMigrationBlockerCode.FutureSchemaUnsupported,
                EvidenceIntelligenceDryRunMigrationBlockerCode.RedactionGateMissing,
                EvidenceIntelligenceDryRunMigrationBlockerCode.RawPayloadRisk,
                EvidenceIntelligenceDryRunMigrationBlockerCode.UnsafeIntegrityHashPlan
            }.ToList(),
            blockerCodes.ToList());
        Assert.IsTrue(result.FailClosed);
        Assert.IsFalse(result.MigrationExecuted);
        Assert.IsFalse(result.DurablePersistenceActive);
    }

    [TestMethod]
    public void DryRunMigrationPlan_SourceHasNoExecutionPersistenceOrMigrationOverclaim()
    {
        var source = ReadRepoText(PersistenceDesignPath) + ReadRepoText(RecipesPersistenceDesignTestsPath);
        var forbidden = new[]
        {
            "MigrationExecutionEnabled: true",
            "MigrationRunnerEnabled: true",
            "DurableStoreEnabled: true",
            "FilesystemReadEnabled: true",
            "FilesystemWriteEnabled: true",
            "DatabaseEnabled: true",
            "ExecutionAttempted: true",
            "MigrationExecuted: true",
            "DurablePersistenceActive: true",
            "FilesystemReadAttempted: true",
            "FilesystemWriteAttempted: true",
            "DatabaseTouched: true",
            "MigrationRunnerStarted: true",
            "ProviderCloudTouched: true",
            "SemanticVectorBackendTouched: true",
            "RuntimeTouched: true",
            "ProductWriteFallbackUsed: true",
            "production-ready",
            "migration executed",
            "dry-run migration completed",
            "durable persistence active"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(source.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
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
