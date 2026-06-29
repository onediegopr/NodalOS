namespace OneBrain.Core.Evidence;

public enum EvidenceIntelligencePersistenceBackendRecommendation
{
    InMemoryOnlyNow,
    FutureAppendOnlyLogWithReadModel,
    FutureSqliteReadModel
}

public enum EvidenceIntelligencePersistenceFieldClassification
{
    SafeToDisplay,
    RedactedBeforeWrite,
    SensitiveNeverPersist,
    Derived,
    FixtureOnly,
    FuturePersisted
}

public enum EvidenceIntelligenceRetentionDisposition
{
    KeepUntilWorkspaceReset,
    KeepForAuditWindow,
    PurgeOnSourceRevocation,
    PurgeBeforePersistence
}

public sealed record EvidenceIntelligencePersistenceCapabilityStatus(
    bool DesignExists,
    bool DurableStoreEnabled,
    bool DurableReadsEnabled,
    bool DurableWritesEnabled,
    bool MigrationEnabled,
    bool RuntimeActionsEnabled,
    bool BrowserCdpAutomationEnabled,
    bool WcuLiveEnabled,
    bool OcrLiveEnabled,
    bool SemanticVectorBackendEnabled,
    bool ProviderCloudEnabled,
    bool FilesystemProductWritesEnabled,
    bool RegistersProductService,
    string ImplementationStatus,
    IReadOnlyList<string> DisabledReasons)
{
    public bool FailClosed =>
        DesignExists
        && !DurableStoreEnabled
        && !DurableReadsEnabled
        && !DurableWritesEnabled
        && !MigrationEnabled
        && !RuntimeActionsEnabled
        && !BrowserCdpAutomationEnabled
        && !WcuLiveEnabled
        && !OcrLiveEnabled
        && !SemanticVectorBackendEnabled
        && !ProviderCloudEnabled
        && !FilesystemProductWritesEnabled
        && !RegistersProductService;

    public static EvidenceIntelligencePersistenceCapabilityStatus DisabledDesign() =>
        new(
            DesignExists: true,
            DurableStoreEnabled: false,
            DurableReadsEnabled: false,
            DurableWritesEnabled: false,
            MigrationEnabled: false,
            RuntimeActionsEnabled: false,
            BrowserCdpAutomationEnabled: false,
            WcuLiveEnabled: false,
            OcrLiveEnabled: false,
            SemanticVectorBackendEnabled: false,
            ProviderCloudEnabled: false,
            FilesystemProductWritesEnabled: false,
            RegistersProductService: false,
            ImplementationStatus: "DESIGN_ONLY_DISABLED_FAIL_CLOSED",
            DisabledReasons:
            [
                "Durable store implementation is a future hito.",
                "Redaction-at-write audit must pass before any write path exists.",
                "Migration runner is not implemented.",
                "EIL UI remains fixture/local read-only.",
                "Runtime, live automation, provider/cloud and semantic/vector backends remain disabled."
            ]);
}

public sealed record EvidenceIntelligenceSchemaDescriptor(
    string SchemaId,
    int MajorVersion,
    int MinorVersion,
    bool SchemaVersionRequired,
    bool RedactionMetadataRequired,
    bool IntegrityHashRequired,
    bool WorkspaceBoundaryRequired,
    bool AllowsRawSecrets,
    bool AllowsRawDom,
    bool AllowsRawScreenshot,
    IReadOnlyList<EvidenceIntelligenceEntityDescriptor> Entities);

public sealed record EvidenceIntelligenceEntityDescriptor(
    string EntityName,
    string Purpose,
    EvidenceIntelligenceRetentionDisposition RetentionDisposition,
    IReadOnlyList<EvidenceIntelligenceFieldDescriptor> Fields);

public sealed record EvidenceIntelligenceFieldDescriptor(
    string Name,
    EvidenceIntelligencePersistenceFieldClassification Classification,
    bool Required,
    string Notes);

public sealed record EvidenceIntelligenceRetentionPolicyDesign(
    string PolicyId,
    bool WorkspaceBounded,
    bool SessionBounded,
    bool UserRevocationRequired,
    bool RawPayloadRetentionAllowed,
    bool DeletionTombstonesRequired,
    IReadOnlyList<string> RetentionWindows);

public sealed record EvidenceIntelligenceMigrationPlanDesign(
    string PlanId,
    bool MigrationRunnerEnabled,
    bool DestructiveMigrationAllowed,
    bool RequiresDryRunReport,
    bool RequiresBackupBeforeFutureMigration,
    IReadOnlyList<string> FutureSteps);

public sealed record EvidenceIntelligenceThreatModelDesign(
    string ModelId,
    IReadOnlyList<string> Assets,
    IReadOnlyList<string> Threats,
    IReadOnlyList<string> Mitigations,
    IReadOnlyList<string> ExplicitNonGoals);

public sealed record EvidenceIntelligenceUnlockCriteria(
    string CriteriaId,
    bool RequiresFutureExplicitHito,
    bool RequiresRedactionAudit,
    bool RequiresFilesystemWriteAudit,
    bool RequiresMigrationDryRunAudit,
    bool RequiresManualQa,
    bool AllowsRuntimeUnlock,
    IReadOnlyList<string> RequiredEvidence);

public sealed record EvidenceIntelligencePersistencePlan(
    string PlanId,
    EvidenceIntelligencePersistenceBackendRecommendation Recommendation,
    EvidenceIntelligencePersistenceCapabilityStatus CapabilityStatus,
    EvidenceIntelligenceSchemaDescriptor Schema,
    EvidenceIntelligenceRetentionPolicyDesign Retention,
    EvidenceIntelligenceMigrationPlanDesign Migration,
    EvidenceIntelligenceThreatModelDesign ThreatModel,
    EvidenceIntelligenceUnlockCriteria UnlockCriteria,
    IReadOnlyList<string> ReadModelBoundaries,
    IReadOnlyList<string> WriteModelBoundaries,
    IReadOnlyList<string> CompatibilityNotes)
{
    public static EvidenceIntelligencePersistencePlan CreateDisabledLocalFirstDesign() =>
        new(
            PlanId: "eil.local-persistence.design.read-only.v1",
            Recommendation: EvidenceIntelligencePersistenceBackendRecommendation.FutureAppendOnlyLogWithReadModel,
            CapabilityStatus: EvidenceIntelligencePersistenceCapabilityStatus.DisabledDesign(),
            Schema: EvidenceIntelligencePersistenceSchemaCatalog.CreateV1Descriptor(),
            Retention: new EvidenceIntelligenceRetentionPolicyDesign(
                PolicyId: "eil.retention.local-first.design.v1",
                WorkspaceBounded: true,
                SessionBounded: true,
                UserRevocationRequired: true,
                RawPayloadRetentionAllowed: false,
                DeletionTombstonesRequired: true,
                RetentionWindows:
                [
                    "Fixture snapshots: in-memory only for this hito.",
                    "Future redacted evidence records: workspace-bounded audit window.",
                    "Future contradiction/readiness snapshots: expire with source evidence.",
                    "Future deleted evidence refs: tombstone metadata only."
                ]),
            Migration: new EvidenceIntelligenceMigrationPlanDesign(
                PlanId: "eil.migration.design.disabled.v1",
                MigrationRunnerEnabled: false,
                DestructiveMigrationAllowed: false,
                RequiresDryRunReport: true,
                RequiresBackupBeforeFutureMigration: true,
                FutureSteps:
                [
                    "Create disabled read-store scaffold.",
                    "Add schema hash snapshot tests.",
                    "Add redaction-at-write dry-run validator.",
                    "Add explicit migration dry-run hito before any real migration."
                ]),
            ThreatModel: new EvidenceIntelligenceThreatModelDesign(
                ModelId: "eil.local-persistence.threat-model.v1",
                Assets:
                [
                    "Redacted evidence records.",
                    "Evidence references and source refs.",
                    "Claim/action scan snapshots.",
                    "Contradiction and readiness decisions.",
                    "Workspace/session boundaries."
                ],
                Threats:
                [
                    "Raw secret or credential persisted before redaction.",
                    "Cross-workspace evidence leakage.",
                    "Stale or corrupted evidence treated as current.",
                    "Future migration silently broadens retained fields.",
                    "Semantic/vector index implies capability that is still disabled."
                ],
                Mitigations:
                [
                    "Redaction-at-write is mandatory before any durable write exists.",
                    "WorkspaceId and SessionId are required schema fields.",
                    "IntegrityHash and schema version are required on future persisted records.",
                    "Migration remains disabled until a separate dry-run/audit hito.",
                    "Semantic/vector backend remains explicitly disabled."
                ],
                ExplicitNonGoals:
                [
                    "No durable store implementation in this hito.",
                    "No product filesystem write path.",
                    "No runtime/live/browser/CDP/WCU/OCR activation.",
                    "No provider/cloud or semantic/vector backend."
                ]),
            UnlockCriteria: new EvidenceIntelligenceUnlockCriteria(
                CriteriaId: "eil.persistence.unlock.future-explicit-hito.v1",
                RequiresFutureExplicitHito: true,
                RequiresRedactionAudit: true,
                RequiresFilesystemWriteAudit: true,
                RequiresMigrationDryRunAudit: true,
                RequiresManualQa: true,
                AllowsRuntimeUnlock: false,
                RequiredEvidence:
                [
                    "Approved ADR update.",
                    "Redaction-at-write tests with hostile fixtures.",
                    "No product-write proof for disabled scaffold.",
                    "Schema compatibility report.",
                    "Manual QA packet for any visible persistence status."
                ]),
            ReadModelBoundaries:
            [
                "Current EIL UI reads deterministic fixture snapshots only.",
                "Future read-store must expose redacted records and derived snapshots only.",
                "Read model cannot imply action authority or runtime readiness."
            ],
            WriteModelBoundaries:
            [
                "No write model is active in this hito.",
                "Future write model must run redaction before durable write.",
                "Future write model must reject sensitive-never-persist fields.",
                "Future write model must produce integrity hashes after redaction."
            ],
            CompatibilityNotes:
            [
                "EvidenceIntelligenceReadOnlyPresenter.CreateFixture remains the UI source.",
                "Recipe Lab can reference future evidence refs but cannot execute recipes.",
                "Fixture/local snapshots remain valid while durable store is disabled."
            ]);
}

public enum EvidenceIntelligenceReadStoreQueryKind
{
    ByEvidenceId,
    ByWorkspaceId,
    ByClaimId,
    ByActionScanId,
    ByGraphNodeId,
    ByGraphEdgeId,
    LatestReadinessSnapshot,
    SafeNextStepSnapshot
}

public enum EvidenceIntelligenceReadStoreResultStatus
{
    Disabled,
    Unavailable,
    NotConfigured,
    DesignOnly,
    FailClosed
}

public enum EvidenceIntelligenceWriteCommandKind
{
    AppendEvidenceRecord,
    AppendClaimScanSnapshot,
    AppendActionScanSnapshot,
    AppendContradictionRecord,
    AppendGraphNode,
    AppendGraphEdge,
    AppendReadinessSnapshot,
    AppendSafeNextStep,
    AppendHumanActionRequirement,
    AppendRedactionMetadata,
    AppendIntegrityHashEnvelope
}

public enum EvidenceIntelligenceWriteResultStatus
{
    Disabled,
    Rejected,
    FailClosed,
    DesignOnly,
    RequiresFutureUnlock
}

public enum EvidenceIntelligenceDryRunMigrationStepKind
{
    NoOp,
    SchemaVersionCheck,
    RedactionGateCheck,
    EvidenceRecordShapeCheck,
    GraphShapeCheck,
    ReadinessSnapshotCheck,
    IntegrityHashCheck,
    RollbackPlanCheck,
    HumanApprovalCheck,
    FutureWritePlanCheck
}

public enum EvidenceIntelligenceDryRunMigrationDecision
{
    NoOp,
    Blocked,
    DesignOnly
}

public enum EvidenceIntelligenceDryRunMigrationBlockerSeverity
{
    Info,
    P3,
    P2,
    P1
}

public enum EvidenceIntelligenceDryRunMigrationBlockerCode
{
    UnknownSchemaVersion,
    FutureSchemaUnsupported,
    RequiresFilesystemRead,
    RequiresFilesystemWrite,
    RequiresDatabase,
    RequiresMigrationRunner,
    RequiresDurableWrite,
    RedactionGateMissing,
    RawPayloadRisk,
    RollbackUnavailable,
    HumanApprovalRequired,
    SchemaDowngrade,
    IncompatibleGraphEdgeShape,
    StaleEvidenceVersion,
    UnsafeIntegrityHashPlan,
    DryRunOnlyRequired
}

public enum EvidenceIntelligenceSchemaCompatibilityArtifactKind
{
    EvidenceRecord,
    EvidenceSource,
    EvidenceReference,
    ClaimScanSnapshot,
    ActionScanSnapshot,
    EvidenceGraphNode,
    EvidenceGraphEdge,
    ReadinessMatrixSnapshot,
    SafeNextStep,
    RedactionMetadata,
    IntegrityHashEnvelope,
    MigrationPlan
}

public enum EvidenceIntelligenceSchemaCompatibilityIssueKind
{
    None,
    UnknownSchemaVersion,
    FutureUnsupportedSchemaVersion,
    SchemaDowngradeAttempt,
    IncompatibleFieldShape,
    MissingRequiredField,
    DeprecatedFieldPresent,
    ExtraUnknownFieldPolicy,
    UnknownEnumValue,
    GraphNodeMissingId,
    GraphEdgeUnknownRelation,
    EvidenceItemMissingSource,
    ClaimScanMissingConfidence,
    ActionScanMissingRequiredAction,
    ReadinessMatrixUnknownState,
    SafeNextStepMissingGuard,
    RedactionMetadataMissing,
    RedactionMetadataUnknownSensitivity,
    IntegrityHashMissing,
    IntegrityHashBeforeRedaction,
    MigrationPlanTargetIncompatible
}

public enum EvidenceIntelligenceSchemaCompatibilityDecision
{
    CompatibleDesignOnly,
    Blocked
}

public sealed record EvidenceIntelligenceReadStoreQuery(
    EvidenceIntelligenceReadStoreQueryKind Kind,
    string WorkspaceId,
    string TargetId,
    int MaxResults,
    IReadOnlyDictionary<string, string> Metadata)
{
    public static EvidenceIntelligenceReadStoreQuery ByEvidenceId(
        string evidenceId,
        string workspaceId = EvidenceItem.DefaultWorkspaceId) =>
        Create(EvidenceIntelligenceReadStoreQueryKind.ByEvidenceId, workspaceId, evidenceId);

    public static EvidenceIntelligenceReadStoreQuery ByWorkspaceId(string workspaceId, int maxResults = 100) =>
        Create(EvidenceIntelligenceReadStoreQueryKind.ByWorkspaceId, workspaceId, workspaceId, maxResults);

    public static EvidenceIntelligenceReadStoreQuery ByClaimId(
        string claimId,
        string workspaceId = EvidenceItem.DefaultWorkspaceId) =>
        Create(EvidenceIntelligenceReadStoreQueryKind.ByClaimId, workspaceId, claimId);

    public static EvidenceIntelligenceReadStoreQuery ByActionScanId(
        string actionScanId,
        string workspaceId = EvidenceItem.DefaultWorkspaceId) =>
        Create(EvidenceIntelligenceReadStoreQueryKind.ByActionScanId, workspaceId, actionScanId);

    public static EvidenceIntelligenceReadStoreQuery ByGraphNodeId(
        string nodeId,
        string workspaceId = EvidenceItem.DefaultWorkspaceId) =>
        Create(EvidenceIntelligenceReadStoreQueryKind.ByGraphNodeId, workspaceId, nodeId);

    public static EvidenceIntelligenceReadStoreQuery ByGraphEdgeId(
        string edgeId,
        string workspaceId = EvidenceItem.DefaultWorkspaceId) =>
        Create(EvidenceIntelligenceReadStoreQueryKind.ByGraphEdgeId, workspaceId, edgeId);

    public static EvidenceIntelligenceReadStoreQuery LatestReadinessSnapshot(
        string workspaceId = EvidenceItem.DefaultWorkspaceId) =>
        Create(EvidenceIntelligenceReadStoreQueryKind.LatestReadinessSnapshot, workspaceId, "latest-readiness");

    public static EvidenceIntelligenceReadStoreQuery SafeNextStepSnapshot(
        string workspaceId = EvidenceItem.DefaultWorkspaceId) =>
        Create(EvidenceIntelligenceReadStoreQueryKind.SafeNextStepSnapshot, workspaceId, "safe-next-step");

    private static EvidenceIntelligenceReadStoreQuery Create(
        EvidenceIntelligenceReadStoreQueryKind kind,
        string workspaceId,
        string targetId,
        int maxResults = 1) =>
        new(
            kind,
            string.IsNullOrWhiteSpace(workspaceId) ? EvidenceItem.DefaultWorkspaceId : workspaceId,
            string.IsNullOrWhiteSpace(targetId) ? "not-configured" : targetId,
            Math.Max(0, maxResults),
            new Dictionary<string, string>
            {
                ["mode"] = "design-only",
                ["fallback"] = "disabled"
            });
}

public sealed record EvidenceIntelligenceReadStoreScaffoldStatus(
    bool IsEnabled,
    bool DurableReadEnabled,
    bool FilesystemReadEnabled,
    bool DatabaseReadEnabled,
    bool MigrationEnabled,
    bool WriteEnabled,
    bool RuntimeEnabled,
    bool ProviderCloudEnabled,
    bool SemanticVectorBackendEnabled,
    bool RegistersProductService,
    string Mode,
    IReadOnlyList<string> DisabledReasons,
    IReadOnlyList<string> UnlockRequirements)
{
    public bool FailClosed =>
        !IsEnabled
        && !DurableReadEnabled
        && !FilesystemReadEnabled
        && !DatabaseReadEnabled
        && !MigrationEnabled
        && !WriteEnabled
        && !RuntimeEnabled
        && !ProviderCloudEnabled
        && !SemanticVectorBackendEnabled
        && !RegistersProductService;

    public static EvidenceIntelligenceReadStoreScaffoldStatus Disabled(EvidenceIntelligenceUnlockCriteria unlockCriteria) =>
        new(
            IsEnabled: false,
            DurableReadEnabled: false,
            FilesystemReadEnabled: false,
            DatabaseReadEnabled: false,
            MigrationEnabled: false,
            WriteEnabled: false,
            RuntimeEnabled: false,
            ProviderCloudEnabled: false,
            SemanticVectorBackendEnabled: false,
            RegistersProductService: false,
            Mode: "DISABLED_DESIGN_ONLY_FAIL_CLOSED",
            DisabledReasons:
            [
                "Read store scaffold is present for contract coverage only.",
                "Durable reads are disabled until a future explicit hito.",
                "Queries return fail-closed results instead of falling back to a local store.",
                "EIL UI remains on deterministic fixture snapshots."
            ],
            UnlockRequirements: unlockCriteria.RequiredEvidence);
}

public sealed record EvidenceIntelligenceReadStoreResult(
    EvidenceIntelligenceReadStoreQuery Query,
    EvidenceIntelligenceReadStoreResultStatus Status,
    string Reason,
    IReadOnlyList<string> EvidenceIds,
    IReadOnlyList<string> Warnings,
    bool FailClosed,
    bool ReadsFilesystem,
    bool ReadsDatabase,
    bool WritesFilesystem,
    bool RunsMigration,
    bool CallsProviderCloud,
    bool UsesSemanticVectorBackend,
    bool UsesRuntime,
    bool FallbackUsed)
{
    public static EvidenceIntelligenceReadStoreResult Disabled(
        EvidenceIntelligenceReadStoreQuery query,
        EvidenceIntelligenceReadStoreScaffoldStatus status) =>
        new(
            query,
            EvidenceIntelligenceReadStoreResultStatus.FailClosed,
            "Local persistence read store is disabled, design-only and fail-closed.",
            EvidenceIds: [],
            Warnings:
            [
                status.Mode,
                "No durable read source is configured.",
                "No fallback source is used."
            ],
            FailClosed: true,
            ReadsFilesystem: false,
            ReadsDatabase: false,
            WritesFilesystem: false,
            RunsMigration: false,
            CallsProviderCloud: false,
            UsesSemanticVectorBackend: false,
            UsesRuntime: false,
            FallbackUsed: false);
}

public sealed record EvidenceIntelligenceRedactionAtWriteRequirement(
    bool RedactionRequired,
    bool RawPayloadNeverPersist,
    bool SecretFieldsRejected,
    bool UnknownSensitivityRejected,
    bool IntegrityHashAfterCanonicalRedaction,
    bool ExecutablePipelineEnabled,
    IReadOnlyList<string> RequiredBeforeUnlock)
{
    public bool FailClosed =>
        RedactionRequired
        && RawPayloadNeverPersist
        && SecretFieldsRejected
        && UnknownSensitivityRejected
        && IntegrityHashAfterCanonicalRedaction
        && !ExecutablePipelineEnabled;

    public static EvidenceIntelligenceRedactionAtWriteRequirement DesignOnly() =>
        new(
            RedactionRequired: true,
            RawPayloadNeverPersist: true,
            SecretFieldsRejected: true,
            UnknownSensitivityRejected: true,
            IntegrityHashAfterCanonicalRedaction: true,
            ExecutablePipelineEnabled: false,
            RequiredBeforeUnlock:
            [
                "Hostile redaction-at-write fixtures must pass.",
                "Unknown sensitivity must fail closed.",
                "Sensitive-never-persist fields must be rejected.",
                "Integrity hash must be computed only after canonical redaction."
            ]);
}

public sealed record EvidenceIntelligenceWriteCommand(
    EvidenceIntelligenceWriteCommandKind Kind,
    string WorkspaceId,
    string TargetId,
    EvidenceIntelligencePersistenceFieldClassification PayloadClassification,
    bool SensitivityKnown,
    bool ContainsRawPayload,
    bool ContainsSecretField,
    bool RedactionMetadataPresent,
    bool IntegrityHashPresent,
    bool IntegrityHashBeforeCanonicalRedaction,
    IReadOnlyDictionary<string, string> Metadata)
{
    public bool RejectedByDesign =>
        !SensitivityKnown
        || ContainsRawPayload
        || ContainsSecretField
        || PayloadClassification == EvidenceIntelligencePersistenceFieldClassification.SensitiveNeverPersist
        || PayloadClassification == EvidenceIntelligencePersistenceFieldClassification.FixtureOnly
        || IntegrityHashBeforeCanonicalRedaction;

    public static EvidenceIntelligenceWriteCommand AppendEvidenceRecord(
        string evidenceId,
        EvidenceIntelligencePersistenceFieldClassification classification = EvidenceIntelligencePersistenceFieldClassification.RedactedBeforeWrite,
        bool sensitivityKnown = true,
        bool containsRawPayload = false,
        bool containsSecretField = false,
        bool integrityHashBeforeCanonicalRedaction = false,
        string workspaceId = EvidenceItem.DefaultWorkspaceId) =>
        Create(
            EvidenceIntelligenceWriteCommandKind.AppendEvidenceRecord,
            workspaceId,
            evidenceId,
            classification,
            sensitivityKnown,
            containsRawPayload,
            containsSecretField,
            integrityHashBeforeCanonicalRedaction: integrityHashBeforeCanonicalRedaction);

    public static EvidenceIntelligenceWriteCommand AppendClaimScanSnapshot(
        string claimScanId,
        string workspaceId = EvidenceItem.DefaultWorkspaceId) =>
        Create(EvidenceIntelligenceWriteCommandKind.AppendClaimScanSnapshot, workspaceId, claimScanId, EvidenceIntelligencePersistenceFieldClassification.Derived);

    public static EvidenceIntelligenceWriteCommand AppendActionScanSnapshot(
        string actionScanId,
        string workspaceId = EvidenceItem.DefaultWorkspaceId) =>
        Create(EvidenceIntelligenceWriteCommandKind.AppendActionScanSnapshot, workspaceId, actionScanId, EvidenceIntelligencePersistenceFieldClassification.Derived);

    public static EvidenceIntelligenceWriteCommand AppendContradictionRecord(
        string contradictionId,
        string workspaceId = EvidenceItem.DefaultWorkspaceId) =>
        Create(EvidenceIntelligenceWriteCommandKind.AppendContradictionRecord, workspaceId, contradictionId, EvidenceIntelligencePersistenceFieldClassification.RedactedBeforeWrite);

    public static EvidenceIntelligenceWriteCommand AppendGraphNode(
        string nodeId,
        EvidenceIntelligencePersistenceFieldClassification classification = EvidenceIntelligencePersistenceFieldClassification.FuturePersisted,
        bool containsRawPayload = false,
        bool containsSecretField = false,
        string workspaceId = EvidenceItem.DefaultWorkspaceId) =>
        Create(EvidenceIntelligenceWriteCommandKind.AppendGraphNode, workspaceId, nodeId, classification, containsRawPayload: containsRawPayload, containsSecretField: containsSecretField);

    public static EvidenceIntelligenceWriteCommand AppendGraphEdge(
        string edgeId,
        EvidenceIntelligencePersistenceFieldClassification classification = EvidenceIntelligencePersistenceFieldClassification.FuturePersisted,
        bool containsRawPayload = false,
        bool containsSecretField = false,
        string workspaceId = EvidenceItem.DefaultWorkspaceId) =>
        Create(EvidenceIntelligenceWriteCommandKind.AppendGraphEdge, workspaceId, edgeId, classification, containsRawPayload: containsRawPayload, containsSecretField: containsSecretField);

    public static EvidenceIntelligenceWriteCommand AppendReadinessSnapshot(
        string readinessId,
        EvidenceIntelligencePersistenceFieldClassification classification = EvidenceIntelligencePersistenceFieldClassification.Derived,
        bool containsRawPayload = false,
        bool containsSecretField = false,
        string workspaceId = EvidenceItem.DefaultWorkspaceId) =>
        Create(EvidenceIntelligenceWriteCommandKind.AppendReadinessSnapshot, workspaceId, readinessId, classification, containsRawPayload: containsRawPayload, containsSecretField: containsSecretField);

    public static EvidenceIntelligenceWriteCommand AppendSafeNextStep(
        string stepId,
        EvidenceIntelligencePersistenceFieldClassification classification = EvidenceIntelligencePersistenceFieldClassification.SafeToDisplay,
        bool containsRawPayload = false,
        bool containsSecretField = false,
        string workspaceId = EvidenceItem.DefaultWorkspaceId) =>
        Create(EvidenceIntelligenceWriteCommandKind.AppendSafeNextStep, workspaceId, stepId, classification, containsRawPayload: containsRawPayload, containsSecretField: containsSecretField);

    public static EvidenceIntelligenceWriteCommand AppendHumanActionRequirement(
        string requirementId,
        string workspaceId = EvidenceItem.DefaultWorkspaceId) =>
        Create(EvidenceIntelligenceWriteCommandKind.AppendHumanActionRequirement, workspaceId, requirementId, EvidenceIntelligencePersistenceFieldClassification.SafeToDisplay);

    public static EvidenceIntelligenceWriteCommand AppendRedactionMetadata(
        string redactionMetadataId,
        string workspaceId = EvidenceItem.DefaultWorkspaceId) =>
        Create(EvidenceIntelligenceWriteCommandKind.AppendRedactionMetadata, workspaceId, redactionMetadataId, EvidenceIntelligencePersistenceFieldClassification.FuturePersisted, redactionMetadataPresent: true);

    public static EvidenceIntelligenceWriteCommand AppendIntegrityHashEnvelope(
        string integrityHashId,
        bool integrityHashBeforeCanonicalRedaction = false,
        string workspaceId = EvidenceItem.DefaultWorkspaceId) =>
        Create(
            EvidenceIntelligenceWriteCommandKind.AppendIntegrityHashEnvelope,
            workspaceId,
            integrityHashId,
            EvidenceIntelligencePersistenceFieldClassification.Derived,
            integrityHashPresent: true,
            integrityHashBeforeCanonicalRedaction: integrityHashBeforeCanonicalRedaction);

    private static EvidenceIntelligenceWriteCommand Create(
        EvidenceIntelligenceWriteCommandKind kind,
        string workspaceId,
        string targetId,
        EvidenceIntelligencePersistenceFieldClassification classification,
        bool sensitivityKnown = true,
        bool containsRawPayload = false,
        bool containsSecretField = false,
        bool redactionMetadataPresent = false,
        bool integrityHashPresent = false,
        bool integrityHashBeforeCanonicalRedaction = false) =>
        new(
            kind,
            string.IsNullOrWhiteSpace(workspaceId) ? EvidenceItem.DefaultWorkspaceId : workspaceId,
            string.IsNullOrWhiteSpace(targetId) ? "not-configured" : targetId,
            classification,
            sensitivityKnown,
            containsRawPayload,
            containsSecretField,
            redactionMetadataPresent,
            integrityHashPresent,
            integrityHashBeforeCanonicalRedaction,
            new Dictionary<string, string>
            {
                ["mode"] = "design-only",
                ["write"] = "disabled",
                ["fallback"] = "disabled"
            });
}

public sealed record EvidenceIntelligenceWriteStoreScaffoldStatus(
    bool IsEnabled,
    bool DurableWriteEnabled,
    bool FilesystemWriteEnabled,
    bool DatabaseWriteEnabled,
    bool MigrationEnabled,
    bool RuntimeEnabled,
    bool ProviderCloudEnabled,
    bool SemanticVectorBackendEnabled,
    bool RedactionAtWriteExecutable,
    bool ServiceRegistrationEnabled,
    string Mode,
    EvidenceIntelligenceRedactionAtWriteRequirement RedactionRequirement,
    IReadOnlyList<string> DisabledReasons,
    IReadOnlyList<string> UnlockRequirements)
{
    public bool FailClosed =>
        !IsEnabled
        && !DurableWriteEnabled
        && !FilesystemWriteEnabled
        && !DatabaseWriteEnabled
        && !MigrationEnabled
        && !RuntimeEnabled
        && !ProviderCloudEnabled
        && !SemanticVectorBackendEnabled
        && !RedactionAtWriteExecutable
        && !ServiceRegistrationEnabled
        && RedactionRequirement.FailClosed;

    public static EvidenceIntelligenceWriteStoreScaffoldStatus Disabled(EvidenceIntelligenceUnlockCriteria unlockCriteria) =>
        new(
            IsEnabled: false,
            DurableWriteEnabled: false,
            FilesystemWriteEnabled: false,
            DatabaseWriteEnabled: false,
            MigrationEnabled: false,
            RuntimeEnabled: false,
            ProviderCloudEnabled: false,
            SemanticVectorBackendEnabled: false,
            RedactionAtWriteExecutable: false,
            ServiceRegistrationEnabled: false,
            Mode: "DISABLED_DESIGN_ONLY_FAIL_CLOSED",
            RedactionRequirement: EvidenceIntelligenceRedactionAtWriteRequirement.DesignOnly(),
            DisabledReasons:
            [
                "Write store scaffold is present for contract coverage only.",
                "Durable writes are disabled until a future explicit hito.",
                "Write commands return fail-closed results instead of writing evidence.",
                "Redaction-at-write is required before future unlock but is not executable in this hito.",
                "EIL UI remains on deterministic fixture snapshots."
            ],
            UnlockRequirements: unlockCriteria.RequiredEvidence);
}

public sealed record EvidenceIntelligenceWriteResult(
    EvidenceIntelligenceWriteCommand Command,
    EvidenceIntelligenceWriteResultStatus Status,
    string Reason,
    IReadOnlyList<string> Warnings,
    bool FailClosed,
    bool WritesFilesystem,
    bool ReadsFilesystem,
    bool WritesDatabase,
    bool RunsMigration,
    bool CallsProviderCloud,
    bool UsesSemanticVectorBackend,
    bool UsesRuntime,
    bool RedactionPipelineExecuted,
    bool FallbackUsed)
{
    public static EvidenceIntelligenceWriteResult Disabled(
        EvidenceIntelligenceWriteCommand command,
        EvidenceIntelligenceWriteStoreScaffoldStatus status) =>
        new(
            command,
            command.RejectedByDesign
                ? EvidenceIntelligenceWriteResultStatus.Rejected
                : EvidenceIntelligenceWriteResultStatus.FailClosed,
            command.RejectedByDesign
                ? "Write command is rejected by design because it contains raw, secret, sensitive-never-persist or fixture-only data."
                : "Local persistence write store is disabled, design-only and fail-closed.",
            Warnings:
            [
                status.Mode,
                "No durable write target is configured.",
                "No fallback target is used.",
                "Redaction-at-write pipeline is required before future unlock but is not executable."
            ],
            FailClosed: true,
            WritesFilesystem: false,
            ReadsFilesystem: false,
            WritesDatabase: false,
            RunsMigration: false,
            CallsProviderCloud: false,
            UsesSemanticVectorBackend: false,
            UsesRuntime: false,
            RedactionPipelineExecuted: false,
            FallbackUsed: false);
}

public sealed record EvidenceIntelligenceSchemaVersionDescriptor(
    int Major,
    int Minor,
    bool IsKnown,
    bool IsSupported,
    string Label)
{
    public bool IsV1 => Major == 1 && Minor == 0 && IsKnown && IsSupported;

    public static EvidenceIntelligenceSchemaVersionDescriptor V1() =>
        new(1, 0, IsKnown: true, IsSupported: true, Label: "eil.local-evidence.schema.v1.design-only");

    public static EvidenceIntelligenceSchemaVersionDescriptor Unknown(string label = "unknown-schema") =>
        new(0, 0, IsKnown: false, IsSupported: false, Label: label);

    public static EvidenceIntelligenceSchemaVersionDescriptor FutureUnsupported(int major, int minor) =>
        new(major, minor, IsKnown: true, IsSupported: false, Label: $"eil.local-evidence.schema.v{major}.{minor}.future-unsupported");
}

public sealed record EvidenceIntelligenceMigrationCapabilityStatus(
    bool DesignExists,
    bool DryRunPlanExists,
    bool DryRunOnly,
    bool MigrationRunnerEnabled,
    bool MigrationExecutionEnabled,
    bool DurableStoreEnabled,
    bool FilesystemReadEnabled,
    bool FilesystemWriteEnabled,
    bool DatabaseEnabled,
    bool ProviderCloudEnabled,
    bool SemanticVectorBackendEnabled,
    bool RuntimeEnabled,
    bool ServiceRegistrationEnabled,
    string Mode)
{
    public bool FailClosed =>
        DesignExists
        && DryRunPlanExists
        && DryRunOnly
        && !MigrationRunnerEnabled
        && !MigrationExecutionEnabled
        && !DurableStoreEnabled
        && !FilesystemReadEnabled
        && !FilesystemWriteEnabled
        && !DatabaseEnabled
        && !ProviderCloudEnabled
        && !SemanticVectorBackendEnabled
        && !RuntimeEnabled
        && !ServiceRegistrationEnabled;

    public static EvidenceIntelligenceMigrationCapabilityStatus DisabledDryRunPlan() =>
        new(
            DesignExists: true,
            DryRunPlanExists: true,
            DryRunOnly: true,
            MigrationRunnerEnabled: false,
            MigrationExecutionEnabled: false,
            DurableStoreEnabled: false,
            FilesystemReadEnabled: false,
            FilesystemWriteEnabled: false,
            DatabaseEnabled: false,
            ProviderCloudEnabled: false,
            SemanticVectorBackendEnabled: false,
            RuntimeEnabled: false,
            ServiceRegistrationEnabled: false,
            Mode: "DRY_RUN_PLAN_ONLY_DISABLED_FAIL_CLOSED");
}

public sealed record EvidenceIntelligenceDryRunMigrationStep(
    string StepId,
    EvidenceIntelligenceDryRunMigrationStepKind Kind,
    bool RequiresFilesystemRead,
    bool RequiresFilesystemWrite,
    bool RequiresDatabase,
    bool RequiresMigrationRunner,
    bool RequiresDurableWrite,
    bool RequiresRedactionAtWrite,
    bool RedactionGateSatisfied,
    bool RequiresHumanApproval,
    bool HumanApprovalPresent,
    bool CanRollback,
    bool HasRawPayloadRisk,
    bool HasIncompatibleGraphShape,
    bool HasStaleEvidenceVersion,
    bool HasUnsafeIntegrityHashPlan,
    string ExpectedEvidenceKind)
{
    public static EvidenceIntelligenceDryRunMigrationStep NoOp(string stepId = "dry-run.no-op.same-schema") =>
        new(
            stepId,
            EvidenceIntelligenceDryRunMigrationStepKind.NoOp,
            RequiresFilesystemRead: false,
            RequiresFilesystemWrite: false,
            RequiresDatabase: false,
            RequiresMigrationRunner: false,
            RequiresDurableWrite: false,
            RequiresRedactionAtWrite: true,
            RedactionGateSatisfied: true,
            RequiresHumanApproval: false,
            HumanApprovalPresent: false,
            CanRollback: true,
            HasRawPayloadRisk: false,
            HasIncompatibleGraphShape: false,
            HasStaleEvidenceVersion: false,
            HasUnsafeIntegrityHashPlan: false,
            ExpectedEvidenceKind: "schema-version-check");
}

public sealed record EvidenceIntelligenceDryRunMigrationPlan(
    string PlanId,
    EvidenceIntelligenceSchemaVersionDescriptor CurrentSchemaVersion,
    EvidenceIntelligenceSchemaVersionDescriptor TargetSchemaVersion,
    bool DryRunOnly,
    bool RequiresHumanApproval,
    bool HumanApprovalPresent,
    IReadOnlyList<EvidenceIntelligenceDryRunMigrationStep> Steps,
    IReadOnlyList<string> ExpectedAuditEvidence)
{
    public static EvidenceIntelligenceDryRunMigrationPlan SameSchemaNoOp() =>
        new(
            PlanId: "eil.migration.dry-run.same-schema.no-op.v1",
            CurrentSchemaVersion: EvidenceIntelligenceSchemaVersionDescriptor.V1(),
            TargetSchemaVersion: EvidenceIntelligenceSchemaVersionDescriptor.V1(),
            DryRunOnly: true,
            RequiresHumanApproval: false,
            HumanApprovalPresent: false,
            Steps: [EvidenceIntelligenceDryRunMigrationStep.NoOp()],
            ExpectedAuditEvidence:
            [
                "Schema version comparison.",
                "No-side-effect flag proof.",
                "Human review of blockers before future activation."
            ]);
}

public sealed record EvidenceIntelligenceDryRunMigrationBlocker(
    EvidenceIntelligenceDryRunMigrationBlockerCode Code,
    EvidenceIntelligenceDryRunMigrationBlockerSeverity Severity,
    string StepId,
    string Reason);

public sealed record EvidenceIntelligenceDryRunMigrationResult(
    EvidenceIntelligenceDryRunMigrationPlan Plan,
    EvidenceIntelligenceDryRunMigrationDecision Decision,
    IReadOnlyList<EvidenceIntelligenceDryRunMigrationBlocker> Blockers,
    EvidenceIntelligenceMigrationCapabilityStatus CapabilityStatus,
    bool DryRunOnly,
    bool ExecutionAttempted,
    bool MigrationExecuted,
    bool DurablePersistenceActive,
    bool FilesystemReadAttempted,
    bool FilesystemWriteAttempted,
    bool DatabaseTouched,
    bool MigrationRunnerStarted,
    bool ProviderCloudTouched,
    bool SemanticVectorBackendTouched,
    bool RuntimeTouched,
    bool ProductWriteFallbackUsed)
{
    public bool FailClosed =>
        CapabilityStatus.FailClosed
        && DryRunOnly
        && !ExecutionAttempted
        && !MigrationExecuted
        && !DurablePersistenceActive
        && !FilesystemReadAttempted
        && !FilesystemWriteAttempted
        && !DatabaseTouched
        && !MigrationRunnerStarted
        && !ProviderCloudTouched
        && !SemanticVectorBackendTouched
        && !RuntimeTouched
        && !ProductWriteFallbackUsed;
}

public static class EvidenceIntelligenceDryRunMigrationPlanner
{
    public static EvidenceIntelligenceDryRunMigrationResult Evaluate(EvidenceIntelligenceDryRunMigrationPlan plan)
    {
        var blockers = new List<EvidenceIntelligenceDryRunMigrationBlocker>();

        AddSchemaBlockers(plan, blockers);
        AddPlanBlockers(plan, blockers);

        foreach (var step in plan.Steps)
        {
            AddStepBlockers(step, blockers);
        }

        var decision = blockers.Count == 0
            ? EvidenceIntelligenceDryRunMigrationDecision.NoOp
            : EvidenceIntelligenceDryRunMigrationDecision.Blocked;

        return new(
            plan,
            decision,
            blockers,
            EvidenceIntelligenceMigrationCapabilityStatus.DisabledDryRunPlan(),
            DryRunOnly: true,
            ExecutionAttempted: false,
            MigrationExecuted: false,
            DurablePersistenceActive: false,
            FilesystemReadAttempted: false,
            FilesystemWriteAttempted: false,
            DatabaseTouched: false,
            MigrationRunnerStarted: false,
            ProviderCloudTouched: false,
            SemanticVectorBackendTouched: false,
            RuntimeTouched: false,
            ProductWriteFallbackUsed: false);
    }

    private static void AddSchemaBlockers(
        EvidenceIntelligenceDryRunMigrationPlan plan,
        List<EvidenceIntelligenceDryRunMigrationBlocker> blockers)
    {
        if (!plan.CurrentSchemaVersion.IsKnown || !plan.TargetSchemaVersion.IsKnown)
        {
            blockers.Add(Blocker(EvidenceIntelligenceDryRunMigrationBlockerCode.UnknownSchemaVersion, "schema", "Unknown schema version blocks migration planning."));
        }

        if (!plan.TargetSchemaVersion.IsSupported)
        {
            blockers.Add(Blocker(EvidenceIntelligenceDryRunMigrationBlockerCode.FutureSchemaUnsupported, "schema", "Target schema version is not supported by this design-only plan."));
        }

        if (plan.TargetSchemaVersion.Major < plan.CurrentSchemaVersion.Major
            || (plan.TargetSchemaVersion.Major == plan.CurrentSchemaVersion.Major && plan.TargetSchemaVersion.Minor < plan.CurrentSchemaVersion.Minor))
        {
            blockers.Add(Blocker(EvidenceIntelligenceDryRunMigrationBlockerCode.SchemaDowngrade, "schema", "Schema downgrade is blocked."));
        }
    }

    private static void AddPlanBlockers(
        EvidenceIntelligenceDryRunMigrationPlan plan,
        List<EvidenceIntelligenceDryRunMigrationBlocker> blockers)
    {
        if (!plan.DryRunOnly)
        {
            blockers.Add(Blocker(EvidenceIntelligenceDryRunMigrationBlockerCode.DryRunOnlyRequired, plan.PlanId, "Migration planning must remain dry-run only."));
        }

        if (plan.RequiresHumanApproval && !plan.HumanApprovalPresent)
        {
            blockers.Add(Blocker(EvidenceIntelligenceDryRunMigrationBlockerCode.HumanApprovalRequired, plan.PlanId, "Human approval is required before future migration planning can proceed."));
        }
    }

    private static void AddStepBlockers(
        EvidenceIntelligenceDryRunMigrationStep step,
        List<EvidenceIntelligenceDryRunMigrationBlocker> blockers)
    {
        if (step.RequiresFilesystemRead)
        {
            blockers.Add(Blocker(EvidenceIntelligenceDryRunMigrationBlockerCode.RequiresFilesystemRead, step.StepId, "Step requires filesystem read capability."));
        }

        if (step.RequiresFilesystemWrite)
        {
            blockers.Add(Blocker(EvidenceIntelligenceDryRunMigrationBlockerCode.RequiresFilesystemWrite, step.StepId, "Step requires filesystem write capability."));
        }

        if (step.RequiresDatabase)
        {
            blockers.Add(Blocker(EvidenceIntelligenceDryRunMigrationBlockerCode.RequiresDatabase, step.StepId, "Step requires database capability."));
        }

        if (step.RequiresMigrationRunner)
        {
            blockers.Add(Blocker(EvidenceIntelligenceDryRunMigrationBlockerCode.RequiresMigrationRunner, step.StepId, "Step requires migration runner capability."));
        }

        if (step.RequiresDurableWrite)
        {
            blockers.Add(Blocker(EvidenceIntelligenceDryRunMigrationBlockerCode.RequiresDurableWrite, step.StepId, "Step requires durable write capability."));
        }

        if (step.RequiresRedactionAtWrite && !step.RedactionGateSatisfied)
        {
            blockers.Add(Blocker(EvidenceIntelligenceDryRunMigrationBlockerCode.RedactionGateMissing, step.StepId, "Redaction-at-write gate is missing."));
        }

        if (step.HasRawPayloadRisk)
        {
            blockers.Add(Blocker(EvidenceIntelligenceDryRunMigrationBlockerCode.RawPayloadRisk, step.StepId, "Raw payload risk blocks migration planning."));
        }

        if (!step.CanRollback)
        {
            blockers.Add(Blocker(EvidenceIntelligenceDryRunMigrationBlockerCode.RollbackUnavailable, step.StepId, "Rollback or restore strategy is unavailable."));
        }

        if (step.RequiresHumanApproval && !step.HumanApprovalPresent)
        {
            blockers.Add(Blocker(EvidenceIntelligenceDryRunMigrationBlockerCode.HumanApprovalRequired, step.StepId, "Human approval is required for this step."));
        }

        if (step.HasIncompatibleGraphShape)
        {
            blockers.Add(Blocker(EvidenceIntelligenceDryRunMigrationBlockerCode.IncompatibleGraphEdgeShape, step.StepId, "Graph edge shape is incompatible."));
        }

        if (step.HasStaleEvidenceVersion)
        {
            blockers.Add(Blocker(EvidenceIntelligenceDryRunMigrationBlockerCode.StaleEvidenceVersion, step.StepId, "Evidence version is stale."));
        }

        if (step.HasUnsafeIntegrityHashPlan)
        {
            blockers.Add(Blocker(EvidenceIntelligenceDryRunMigrationBlockerCode.UnsafeIntegrityHashPlan, step.StepId, "Integrity hash plan is unsafe."));
        }
    }

    private static EvidenceIntelligenceDryRunMigrationBlocker Blocker(
        EvidenceIntelligenceDryRunMigrationBlockerCode code,
        string stepId,
        string reason) =>
        new(code, EvidenceIntelligenceDryRunMigrationBlockerSeverity.P2, stepId, reason);
}

public sealed record EvidenceIntelligenceSchemaCompatibilityStatus(
    bool DesignExists,
    bool GuardExists,
    bool DesignOnly,
    bool DurablePersistenceEnabled,
    bool FilesystemReadEnabled,
    bool FilesystemWriteEnabled,
    bool DatabaseEnabled,
    bool MigrationRunnerEnabled,
    bool MigrationExecutionEnabled,
    bool ProviderCloudEnabled,
    bool SemanticVectorBackendEnabled,
    bool RuntimeEnabled,
    bool ServiceRegistrationEnabled,
    string Mode)
{
    public bool FailClosed =>
        DesignExists
        && GuardExists
        && DesignOnly
        && !DurablePersistenceEnabled
        && !FilesystemReadEnabled
        && !FilesystemWriteEnabled
        && !DatabaseEnabled
        && !MigrationRunnerEnabled
        && !MigrationExecutionEnabled
        && !ProviderCloudEnabled
        && !SemanticVectorBackendEnabled
        && !RuntimeEnabled
        && !ServiceRegistrationEnabled;

    public static EvidenceIntelligenceSchemaCompatibilityStatus DisabledDesignOnlyGuard() =>
        new(
            DesignExists: true,
            GuardExists: true,
            DesignOnly: true,
            DurablePersistenceEnabled: false,
            FilesystemReadEnabled: false,
            FilesystemWriteEnabled: false,
            DatabaseEnabled: false,
            MigrationRunnerEnabled: false,
            MigrationExecutionEnabled: false,
            ProviderCloudEnabled: false,
            SemanticVectorBackendEnabled: false,
            RuntimeEnabled: false,
            ServiceRegistrationEnabled: false,
            Mode: "SCHEMA_COMPATIBILITY_DESIGN_ONLY_FAIL_CLOSED");
}

public sealed record EvidenceIntelligenceSchemaCompatibilityCheck(
    string CheckId,
    EvidenceIntelligenceSchemaVersionDescriptor CurrentSchemaVersion,
    EvidenceIntelligenceSchemaVersionDescriptor TargetSchemaVersion,
    EvidenceIntelligenceSchemaCompatibilityArtifactKind ArtifactKind,
    EvidenceIntelligenceSchemaCompatibilityIssueKind IssueKind,
    bool RequiredFieldsPresent,
    bool EnumValuesKnown,
    bool DeprecatedFieldPresent,
    bool ExtraUnknownFieldPresent,
    bool RedactionMetadataPresent,
    bool RedactionSensitivityKnown,
    bool IntegrityHashPresent,
    bool IntegrityHashAfterRedaction,
    bool MigrationTargetCompatible,
    bool DurablePersistenceAllowed)
{
    public static EvidenceIntelligenceSchemaCompatibilityCheck V1KnownCompatible(
        string checkId = "schema-compatibility.v1-known-compatible") =>
        new(
            CheckId: checkId,
            CurrentSchemaVersion: EvidenceIntelligenceSchemaVersionDescriptor.V1(),
            TargetSchemaVersion: EvidenceIntelligenceSchemaVersionDescriptor.V1(),
            ArtifactKind: EvidenceIntelligenceSchemaCompatibilityArtifactKind.EvidenceRecord,
            IssueKind: EvidenceIntelligenceSchemaCompatibilityIssueKind.None,
            RequiredFieldsPresent: true,
            EnumValuesKnown: true,
            DeprecatedFieldPresent: false,
            ExtraUnknownFieldPresent: false,
            RedactionMetadataPresent: true,
            RedactionSensitivityKnown: true,
            IntegrityHashPresent: true,
            IntegrityHashAfterRedaction: true,
            MigrationTargetCompatible: true,
            DurablePersistenceAllowed: false);
}

public sealed record EvidenceIntelligenceSchemaCompatibilityIssue(
    EvidenceIntelligenceSchemaCompatibilityIssueKind Kind,
    EvidenceIntelligenceDryRunMigrationBlockerSeverity Severity,
    string Artifact,
    string Reason);

public sealed record EvidenceIntelligenceSchemaCompatibilityResult(
    EvidenceIntelligenceSchemaCompatibilityCheck Check,
    EvidenceIntelligenceSchemaCompatibilityDecision Decision,
    IReadOnlyList<EvidenceIntelligenceSchemaCompatibilityIssue> Issues,
    EvidenceIntelligenceSchemaCompatibilityStatus Status,
    bool MigrationCouldBePlannedDesignOnly,
    bool DurablePersistenceAllowed,
    bool FilesystemReadAttempted,
    bool FilesystemWriteAttempted,
    bool DatabaseTouched,
    bool MigrationRunnerStarted,
    bool MigrationExecuted,
    bool ProviderCloudTouched,
    bool SemanticVectorBackendTouched,
    bool RuntimeTouched,
    bool ProductWriteFallbackUsed)
{
    public bool FailClosed =>
        Status.FailClosed
        && !DurablePersistenceAllowed
        && !FilesystemReadAttempted
        && !FilesystemWriteAttempted
        && !DatabaseTouched
        && !MigrationRunnerStarted
        && !MigrationExecuted
        && !ProviderCloudTouched
        && !SemanticVectorBackendTouched
        && !RuntimeTouched
        && !ProductWriteFallbackUsed;
}

public static class EvidenceIntelligenceSchemaCompatibilityGuard
{
    public static EvidenceIntelligenceSchemaCompatibilityResult Evaluate(EvidenceIntelligenceSchemaCompatibilityCheck check)
    {
        var issues = new List<EvidenceIntelligenceSchemaCompatibilityIssue>();

        AddSchemaVersionIssues(check, issues);
        AddArtifactIssues(check, issues);
        AddRequiredPolicyIssues(check, issues);

        var decision = issues.Count == 0
            ? EvidenceIntelligenceSchemaCompatibilityDecision.CompatibleDesignOnly
            : EvidenceIntelligenceSchemaCompatibilityDecision.Blocked;

        return new(
            Check: check,
            Decision: decision,
            Issues: issues,
            Status: EvidenceIntelligenceSchemaCompatibilityStatus.DisabledDesignOnlyGuard(),
            MigrationCouldBePlannedDesignOnly: decision == EvidenceIntelligenceSchemaCompatibilityDecision.CompatibleDesignOnly,
            DurablePersistenceAllowed: false,
            FilesystemReadAttempted: false,
            FilesystemWriteAttempted: false,
            DatabaseTouched: false,
            MigrationRunnerStarted: false,
            MigrationExecuted: false,
            ProviderCloudTouched: false,
            SemanticVectorBackendTouched: false,
            RuntimeTouched: false,
            ProductWriteFallbackUsed: false);
    }

    private static void AddSchemaVersionIssues(
        EvidenceIntelligenceSchemaCompatibilityCheck check,
        List<EvidenceIntelligenceSchemaCompatibilityIssue> issues)
    {
        if (!check.CurrentSchemaVersion.IsKnown || !check.TargetSchemaVersion.IsKnown)
        {
            issues.Add(Issue(EvidenceIntelligenceSchemaCompatibilityIssueKind.UnknownSchemaVersion, check.ArtifactKind, "Unknown schema version blocks compatibility."));
        }

        if (!check.TargetSchemaVersion.IsSupported)
        {
            issues.Add(Issue(EvidenceIntelligenceSchemaCompatibilityIssueKind.FutureUnsupportedSchemaVersion, check.ArtifactKind, "Future schema version is unsupported."));
        }

        if (check.TargetSchemaVersion.Major < check.CurrentSchemaVersion.Major
            || (check.TargetSchemaVersion.Major == check.CurrentSchemaVersion.Major && check.TargetSchemaVersion.Minor < check.CurrentSchemaVersion.Minor))
        {
            issues.Add(Issue(EvidenceIntelligenceSchemaCompatibilityIssueKind.SchemaDowngradeAttempt, check.ArtifactKind, "Schema downgrade is blocked."));
        }
    }

    private static void AddArtifactIssues(
        EvidenceIntelligenceSchemaCompatibilityCheck check,
        List<EvidenceIntelligenceSchemaCompatibilityIssue> issues)
    {
        if (check.IssueKind != EvidenceIntelligenceSchemaCompatibilityIssueKind.None)
        {
            issues.Add(Issue(check.IssueKind, check.ArtifactKind, $"Artifact issue '{check.IssueKind}' blocks schema compatibility."));
        }

        if (!check.RequiredFieldsPresent)
        {
            issues.Add(Issue(EvidenceIntelligenceSchemaCompatibilityIssueKind.MissingRequiredField, check.ArtifactKind, "Required schema field is missing."));
        }

        if (!check.EnumValuesKnown)
        {
            issues.Add(Issue(EvidenceIntelligenceSchemaCompatibilityIssueKind.UnknownEnumValue, check.ArtifactKind, "Unknown enum value blocks compatibility."));
        }

        if (check.DeprecatedFieldPresent)
        {
            issues.Add(Issue(EvidenceIntelligenceSchemaCompatibilityIssueKind.DeprecatedFieldPresent, check.ArtifactKind, "Deprecated field is present and requires future migration policy."));
        }

        if (check.ExtraUnknownFieldPresent)
        {
            issues.Add(Issue(EvidenceIntelligenceSchemaCompatibilityIssueKind.ExtraUnknownFieldPolicy, check.ArtifactKind, "Extra unknown field policy is not unlocked."));
        }
    }

    private static void AddRequiredPolicyIssues(
        EvidenceIntelligenceSchemaCompatibilityCheck check,
        List<EvidenceIntelligenceSchemaCompatibilityIssue> issues)
    {
        if (!check.RedactionMetadataPresent)
        {
            issues.Add(Issue(EvidenceIntelligenceSchemaCompatibilityIssueKind.RedactionMetadataMissing, check.ArtifactKind, "Redaction metadata is required."));
        }

        if (!check.RedactionSensitivityKnown)
        {
            issues.Add(Issue(EvidenceIntelligenceSchemaCompatibilityIssueKind.RedactionMetadataUnknownSensitivity, check.ArtifactKind, "Unknown redaction sensitivity blocks compatibility."));
        }

        if (!check.IntegrityHashPresent)
        {
            issues.Add(Issue(EvidenceIntelligenceSchemaCompatibilityIssueKind.IntegrityHashMissing, check.ArtifactKind, "Integrity hash is required."));
        }

        if (!check.IntegrityHashAfterRedaction)
        {
            issues.Add(Issue(EvidenceIntelligenceSchemaCompatibilityIssueKind.IntegrityHashBeforeRedaction, check.ArtifactKind, "Integrity hash must be after canonical redaction."));
        }

        if (!check.MigrationTargetCompatible)
        {
            issues.Add(Issue(EvidenceIntelligenceSchemaCompatibilityIssueKind.MigrationPlanTargetIncompatible, check.ArtifactKind, "Migration target is incompatible."));
        }

        if (check.DurablePersistenceAllowed)
        {
            issues.Add(Issue(EvidenceIntelligenceSchemaCompatibilityIssueKind.MigrationPlanTargetIncompatible, check.ArtifactKind, "Durable persistence remains disabled for schema compatibility guards."));
        }
    }

    private static EvidenceIntelligenceSchemaCompatibilityIssue Issue(
        EvidenceIntelligenceSchemaCompatibilityIssueKind kind,
        EvidenceIntelligenceSchemaCompatibilityArtifactKind artifactKind,
        string reason) =>
        new(kind, EvidenceIntelligenceDryRunMigrationBlockerSeverity.P2, artifactKind.ToString(), reason);
}

public interface IEvidenceIntelligenceReadStore
{
    EvidenceIntelligencePersistenceCapabilityStatus CapabilityStatus { get; }

    EvidenceIntelligenceReadStoreScaffoldStatus ScaffoldStatus { get; }

    EvidenceIntelligenceReadStoreResult Query(EvidenceIntelligenceReadStoreQuery query);
}

public interface IEvidenceIntelligenceWriteStore
{
    EvidenceIntelligencePersistenceCapabilityStatus CapabilityStatus { get; }

    EvidenceIntelligenceWriteStoreScaffoldStatus ScaffoldStatus { get; }

    EvidenceIntelligenceWriteResult Write(EvidenceIntelligenceWriteCommand command);
}

public sealed class DisabledEvidenceIntelligenceReadStore : IEvidenceIntelligenceReadStore
{
    public DisabledEvidenceIntelligenceReadStore()
        : this(EvidenceIntelligencePersistencePlan.CreateDisabledLocalFirstDesign())
    {
    }

    public DisabledEvidenceIntelligenceReadStore(EvidenceIntelligencePersistencePlan plan)
    {
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        CapabilityStatus = Plan.CapabilityStatus;
        ScaffoldStatus = EvidenceIntelligenceReadStoreScaffoldStatus.Disabled(Plan.UnlockCriteria);
    }

    public EvidenceIntelligencePersistencePlan Plan { get; }

    public EvidenceIntelligencePersistenceCapabilityStatus CapabilityStatus { get; }

    public EvidenceIntelligenceReadStoreScaffoldStatus ScaffoldStatus { get; }

    public EvidenceIntelligenceReadStoreResult Query(EvidenceIntelligenceReadStoreQuery query) =>
        EvidenceIntelligenceReadStoreResult.Disabled(query, ScaffoldStatus);
}

public sealed class DisabledEvidenceIntelligenceWriteStore : IEvidenceIntelligenceWriteStore
{
    public DisabledEvidenceIntelligenceWriteStore()
        : this(EvidenceIntelligencePersistencePlan.CreateDisabledLocalFirstDesign())
    {
    }

    public DisabledEvidenceIntelligenceWriteStore(EvidenceIntelligencePersistencePlan plan)
    {
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        CapabilityStatus = Plan.CapabilityStatus;
        ScaffoldStatus = EvidenceIntelligenceWriteStoreScaffoldStatus.Disabled(Plan.UnlockCriteria);
    }

    public EvidenceIntelligencePersistencePlan Plan { get; }

    public EvidenceIntelligencePersistenceCapabilityStatus CapabilityStatus { get; }

    public EvidenceIntelligenceWriteStoreScaffoldStatus ScaffoldStatus { get; }

    public EvidenceIntelligenceWriteResult Write(EvidenceIntelligenceWriteCommand command) =>
        EvidenceIntelligenceWriteResult.Disabled(command, ScaffoldStatus);
}

public static class EvidenceIntelligencePersistenceSchemaCatalog
{
    public static EvidenceIntelligenceSchemaDescriptor CreateV1Descriptor() =>
        new(
            SchemaId: "eil.local-evidence.schema.v1.design-only",
            MajorVersion: 1,
            MinorVersion: 0,
            SchemaVersionRequired: true,
            RedactionMetadataRequired: true,
            IntegrityHashRequired: true,
            WorkspaceBoundaryRequired: true,
            AllowsRawSecrets: false,
            AllowsRawDom: false,
            AllowsRawScreenshot: false,
            Entities:
            [
                Entity("EvidenceRecord", "Redacted local evidence metadata and display-safe summary.", EvidenceIntelligenceRetentionDisposition.KeepForAuditWindow,
                [
                    Field("EvidenceId", EvidenceIntelligencePersistenceFieldClassification.FuturePersisted, true, "Stable local id."),
                    Field("WorkspaceId", EvidenceIntelligencePersistenceFieldClassification.FuturePersisted, true, "Required workspace boundary."),
                    Field("SessionId", EvidenceIntelligencePersistenceFieldClassification.FuturePersisted, true, "Required session boundary."),
                    Field("DisplayText", EvidenceIntelligencePersistenceFieldClassification.RedactedBeforeWrite, true, "Only redacted summary text."),
                    Field("RawPayload", EvidenceIntelligencePersistenceFieldClassification.SensitiveNeverPersist, false, "Raw DOM, screenshot, logs, cookies and secrets are prohibited."),
                    Field("IntegrityHash", EvidenceIntelligencePersistenceFieldClassification.Derived, true, "Hash of canonical redacted payload.")
                ]),
                Entity("EvidenceSource", "Source metadata without raw payload.", EvidenceIntelligenceRetentionDisposition.PurgeOnSourceRevocation,
                [
                    Field("SourceRef", EvidenceIntelligencePersistenceFieldClassification.FuturePersisted, true, "Reference only."),
                    Field("SourceType", EvidenceIntelligencePersistenceFieldClassification.FuturePersisted, true, "Typed evidence source."),
                    Field("CaptureMode", EvidenceIntelligencePersistenceFieldClassification.SafeToDisplay, true, "Must remain read-only/local in current roadmap.")
                ]),
                Entity("EvidenceReference", "Reference token used by UI and Recipe Lab.", EvidenceIntelligenceRetentionDisposition.KeepForAuditWindow,
                [
                    Field("EvidenceId", EvidenceIntelligencePersistenceFieldClassification.FuturePersisted, true, "Reference target."),
                    Field("Label", EvidenceIntelligencePersistenceFieldClassification.SafeToDisplay, true, "Display-safe label."),
                    Field("Relation", EvidenceIntelligencePersistenceFieldClassification.FuturePersisted, true, "Typed relation.")
                ]),
                Entity("ClaimScanSnapshot", "Derived claim scan result.", EvidenceIntelligenceRetentionDisposition.KeepForAuditWindow,
                [
                    Field("Verdict", EvidenceIntelligencePersistenceFieldClassification.Derived, true, "Derived from evidence refs."),
                    Field("SupportingEvidenceIds", EvidenceIntelligencePersistenceFieldClassification.FuturePersisted, true, "Refs only."),
                    Field("ContradictingEvidenceIds", EvidenceIntelligencePersistenceFieldClassification.FuturePersisted, true, "Refs only.")
                ]),
                Entity("ActionScanSnapshot", "Derived no-runtime action readiness result.", EvidenceIntelligenceRetentionDisposition.KeepForAuditWindow,
                [
                    Field("Verdict", EvidenceIntelligencePersistenceFieldClassification.Derived, true, "Must not authorize execution."),
                    Field("RequiredHumanActions", EvidenceIntelligencePersistenceFieldClassification.SafeToDisplay, true, "Display-safe checklist."),
                    Field("RuntimeEnabled", EvidenceIntelligencePersistenceFieldClassification.Derived, true, "Must remain false.")
                ]),
                Entity("ContradictionRecord", "Contradiction-first safety record.", EvidenceIntelligenceRetentionDisposition.KeepForAuditWindow,
                [
                    Field("FromEvidenceId", EvidenceIntelligencePersistenceFieldClassification.FuturePersisted, true, "Ref only."),
                    Field("ToTargetId", EvidenceIntelligencePersistenceFieldClassification.FuturePersisted, true, "Claim/action target ref."),
                    Field("Rationale", EvidenceIntelligencePersistenceFieldClassification.RedactedBeforeWrite, true, "Redacted rationale.")
                ]),
                Entity("EvidenceGraphNode", "Typed graph node.", EvidenceIntelligenceRetentionDisposition.KeepForAuditWindow,
                [
                    Field("NodeId", EvidenceIntelligencePersistenceFieldClassification.FuturePersisted, true, "Stable graph id."),
                    Field("NodeType", EvidenceIntelligencePersistenceFieldClassification.SafeToDisplay, true, "Evidence, claim, action or policy.")
                ]),
                Entity("EvidenceGraphEdge", "Typed graph edge.", EvidenceIntelligenceRetentionDisposition.KeepForAuditWindow,
                [
                    Field("FromId", EvidenceIntelligencePersistenceFieldClassification.FuturePersisted, true, "Ref only."),
                    Field("ToId", EvidenceIntelligencePersistenceFieldClassification.FuturePersisted, true, "Ref only."),
                    Field("RelationType", EvidenceIntelligencePersistenceFieldClassification.SafeToDisplay, true, "Typed relation.")
                ]),
                Entity("ReadinessMatrixSnapshot", "Derived readiness matrix.", EvidenceIntelligenceRetentionDisposition.KeepForAuditWindow,
                [
                    Field("FinalVerdict", EvidenceIntelligencePersistenceFieldClassification.Derived, true, "Read-only readiness."),
                    Field("BlockingReasons", EvidenceIntelligencePersistenceFieldClassification.RedactedBeforeWrite, true, "Redacted display-safe reasons."),
                    Field("SafeNextStep", EvidenceIntelligencePersistenceFieldClassification.SafeToDisplay, true, "No-runtime next step.")
                ]),
                Entity("HumanActionRequirement", "Human review requirement.", EvidenceIntelligenceRetentionDisposition.KeepForAuditWindow,
                [
                    Field("RequirementId", EvidenceIntelligencePersistenceFieldClassification.FuturePersisted, true, "Stable local id."),
                    Field("Copy", EvidenceIntelligencePersistenceFieldClassification.SafeToDisplay, true, "Human-readable no-runtime requirement.")
                ]),
                Entity("SafeNextStep", "No-runtime next step.", EvidenceIntelligenceRetentionDisposition.KeepForAuditWindow,
                [
                    Field("StepText", EvidenceIntelligencePersistenceFieldClassification.SafeToDisplay, true, "Must not be execute/apply/run."),
                    Field("RequiresHumanApproval", EvidenceIntelligencePersistenceFieldClassification.Derived, true, "Future real action remains gated.")
                ]),
                Entity("RedactionMetadata", "Proof that persisted display fields were redacted.", EvidenceIntelligenceRetentionDisposition.KeepForAuditWindow,
                [
                    Field("RedactionStatus", EvidenceIntelligencePersistenceFieldClassification.FuturePersisted, true, "Applied/required/rejected."),
                    Field("RedactedFields", EvidenceIntelligencePersistenceFieldClassification.Derived, true, "Field names only."),
                    Field("RawSecret", EvidenceIntelligencePersistenceFieldClassification.SensitiveNeverPersist, false, "Never persisted.")
                ]),
                Entity("SchemaVersion", "Schema compatibility marker.", EvidenceIntelligenceRetentionDisposition.KeepUntilWorkspaceReset,
                [
                    Field("Major", EvidenceIntelligencePersistenceFieldClassification.FuturePersisted, true, "Major schema version."),
                    Field("Minor", EvidenceIntelligencePersistenceFieldClassification.FuturePersisted, true, "Minor schema version.")
                ]),
                Entity("RetentionPolicy", "Applied retention policy marker.", EvidenceIntelligenceRetentionDisposition.KeepUntilWorkspaceReset,
                [
                    Field("PolicyId", EvidenceIntelligencePersistenceFieldClassification.FuturePersisted, true, "Retention policy id."),
                    Field("WorkspaceBounded", EvidenceIntelligencePersistenceFieldClassification.Derived, true, "Must remain true.")
                ]),
                Entity("IntegrityHash", "Canonical hash after redaction.", EvidenceIntelligenceRetentionDisposition.KeepForAuditWindow,
                [
                    Field("Algorithm", EvidenceIntelligencePersistenceFieldClassification.SafeToDisplay, true, "sha256 recommended."),
                    Field("Value", EvidenceIntelligencePersistenceFieldClassification.Derived, true, "Hash of redacted canonical record.")
                ])
            ]);

    private static EvidenceIntelligenceEntityDescriptor Entity(
        string name,
        string purpose,
        EvidenceIntelligenceRetentionDisposition retention,
        IReadOnlyList<EvidenceIntelligenceFieldDescriptor> fields) =>
        new(name, purpose, retention, fields);

    private static EvidenceIntelligenceFieldDescriptor Field(
        string name,
        EvidenceIntelligencePersistenceFieldClassification classification,
        bool required,
        string notes) =>
        new(name, classification, required, notes);
}
