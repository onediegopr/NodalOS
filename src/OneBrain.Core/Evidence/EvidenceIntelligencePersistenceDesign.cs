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

public interface IEvidenceIntelligenceReadStore
{
    EvidenceIntelligencePersistenceCapabilityStatus CapabilityStatus { get; }
}

public interface IEvidenceIntelligenceWriteStore
{
    EvidenceIntelligencePersistenceCapabilityStatus CapabilityStatus { get; }
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
