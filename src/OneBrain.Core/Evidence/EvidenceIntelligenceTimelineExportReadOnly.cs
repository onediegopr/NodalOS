namespace OneBrain.Core.Evidence;

public enum EvidenceIntelligenceTimelineExportSectionStatus
{
    Included,
    Warning,
    Blocked,
    Deferred
}

public enum EvidenceIntelligenceTimelineExportSourceKind
{
    FixtureSurface,
    PresenterSnapshot,
    PersistenceCapabilityStatus,
    ReadStoreScaffoldStatus,
    WriteStoreScaffoldStatus,
    RedactionHostileCoverage,
    DryRunMigrationPlanStatus,
    SchemaCompatibilityGuardStatus,
    NoSideEffectProof,
    DocumentedDebt
}

public sealed record EvidenceIntelligenceTimelineExportNoSideEffectProof(
    bool ReadOnly,
    bool Deterministic,
    bool FixtureSafe,
    bool FilesystemReadAttempted,
    bool FilesystemWriteAttempted,
    bool ExportFileCreated,
    bool DatabaseTouched,
    bool DurablePersistenceActive,
    bool MigrationRunnerStarted,
    bool MigrationExecuted,
    bool ProviderCloudTouched,
    bool SemanticVectorBackendTouched,
    bool RuntimeTouched,
    bool BrowserCdpTouched,
    bool WcuTouched,
    bool OcrTouched,
    bool ProductWriteFallbackUsed)
{
    public bool Passes =>
        ReadOnly
        && Deterministic
        && FixtureSafe
        && !FilesystemReadAttempted
        && !FilesystemWriteAttempted
        && !ExportFileCreated
        && !DatabaseTouched
        && !DurablePersistenceActive
        && !MigrationRunnerStarted
        && !MigrationExecuted
        && !ProviderCloudTouched
        && !SemanticVectorBackendTouched
        && !RuntimeTouched
        && !BrowserCdpTouched
        && !WcuTouched
        && !OcrTouched
        && !ProductWriteFallbackUsed;

    public static EvidenceIntelligenceTimelineExportNoSideEffectProof FixtureReadOnly() =>
        new(
            ReadOnly: true,
            Deterministic: true,
            FixtureSafe: true,
            FilesystemReadAttempted: false,
            FilesystemWriteAttempted: false,
            ExportFileCreated: false,
            DatabaseTouched: false,
            DurablePersistenceActive: false,
            MigrationRunnerStarted: false,
            MigrationExecuted: false,
            ProviderCloudTouched: false,
            SemanticVectorBackendTouched: false,
            RuntimeTouched: false,
            BrowserCdpTouched: false,
            WcuTouched: false,
            OcrTouched: false,
            ProductWriteFallbackUsed: false);
}

public sealed record EvidenceIntelligenceTimelineEvent(
    string EventId,
    string Label,
    string EventKind,
    string Source,
    IReadOnlyList<string> EvidenceRefs,
    string Summary);

public sealed record EvidenceIntelligenceTimelineExportManifest(
    string ExportId,
    string Title,
    string Mode,
    string SourceLabel,
    string DeterministicSnapshotId,
    string FormatPreview,
    bool ReadOnlyPreview,
    bool CopyReady,
    bool PhysicalExportEnabled,
    IReadOnlyList<string> IncludedEvidenceRefs,
    IReadOnlyList<string> ExcludedContentClasses,
    EvidenceIntelligenceTimelineExportNoSideEffectProof NoSideEffectProof);

public sealed record EvidenceIntelligenceTimelineExportSection(
    string SectionId,
    string Title,
    EvidenceIntelligenceTimelineExportSectionStatus Status,
    EvidenceIntelligenceTimelineExportSourceKind SourceKind,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> Lines,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    bool ExportableInReadOnlyPreview,
    bool RealExportOccurred,
    EvidenceIntelligenceTimelineExportNoSideEffectProof NoSideEffectProof);

public sealed record EvidenceIntelligenceTimelineExportResult(
    EvidenceIntelligenceTimelineExportManifest Manifest,
    IReadOnlyList<EvidenceIntelligenceTimelineEvent> Timeline,
    IReadOnlyList<EvidenceIntelligenceTimelineExportSection> Sections,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    string CopyReadyPreview,
    EvidenceIntelligencePersistenceCapabilityStatus PersistenceStatus,
    EvidenceIntelligenceReadStoreScaffoldStatus ReadStoreStatus,
    EvidenceIntelligenceWriteStoreScaffoldStatus WriteStoreStatus,
    EvidenceIntelligenceMigrationCapabilityStatus DryRunMigrationStatus,
    EvidenceIntelligenceSchemaCompatibilityStatus SchemaCompatibilityStatus,
    EvidenceIntelligenceTimelineExportNoSideEffectProof NoSideEffectProof)
{
    public bool ReadOnly => Manifest.ReadOnlyPreview;
    public bool Deterministic => NoSideEffectProof.Deterministic;
    public bool PhysicalExportEnabled => Manifest.PhysicalExportEnabled;
}

public static class EvidenceIntelligenceTimelineExportReadOnlyPresenter
{
    public static EvidenceIntelligenceTimelineExportResult CreateFixture()
    {
        var mount = EvidenceIntelligenceReadOnlyUiMount.CreateFixture();
        var persistencePlan = EvidenceIntelligencePersistencePlan.CreateDisabledLocalFirstDesign();
        var readStore = new DisabledEvidenceIntelligenceReadStore(persistencePlan);
        var writeStore = new DisabledEvidenceIntelligenceWriteStore(persistencePlan);
        var dryRunStatus = EvidenceIntelligenceMigrationCapabilityStatus.DisabledDryRunPlan();
        var schemaStatus = EvidenceIntelligenceSchemaCompatibilityStatus.DisabledDesignOnlyGuard();
        var proof = EvidenceIntelligenceTimelineExportNoSideEffectProof.FixtureReadOnly();
        var evidenceRefs = EvidenceRefs(mount.Surface);
        var timeline = Timeline(mount.Surface);
        var warnings = Warnings();
        var blockers = Blockers();
        var sections = Sections(mount, persistencePlan, readStore, writeStore, dryRunStatus, schemaStatus, proof, timeline, evidenceRefs);
        var manifest = new EvidenceIntelligenceTimelineExportManifest(
            ExportId: "eil.timeline-export.preview.read-only.v1",
            Title: "Evidence Intelligence Timeline Export Preview",
            Mode: "READ_ONLY_FIXTURE_SAFE_NO_EXPORT_FILE",
            SourceLabel: "EvidenceIntelligenceReadOnlyUiMount.CreateFixture",
            DeterministicSnapshotId: "eil.fixture.timeline-export.snapshot.v1",
            FormatPreview: "copy-ready markdown preview",
            ReadOnlyPreview: true,
            CopyReady: true,
            PhysicalExportEnabled: false,
            IncludedEvidenceRefs: evidenceRefs,
            ExcludedContentClasses:
            [
                "raw payloads",
                "secret-like content",
                "sensitive-never-persist fields",
                "browser/CDP payloads",
                "OCR raw payloads",
                "provider/cloud payloads"
            ],
            NoSideEffectProof: proof);

        return new EvidenceIntelligenceTimelineExportResult(
            manifest,
            timeline,
            sections,
            warnings,
            blockers,
            CopyReadyPreview(manifest, sections),
            persistencePlan.CapabilityStatus,
            readStore.ScaffoldStatus,
            writeStore.ScaffoldStatus,
            dryRunStatus,
            schemaStatus,
            proof);
    }

    private static IReadOnlyList<string> EvidenceRefs(EvidenceIntelligenceSurfaceViewModel surface) =>
        surface.SearchPanel.Results
            .Select(result => result.EvidenceId)
            .Concat(surface.ClaimScanPanel.SupportingEvidenceIds)
            .Concat(surface.ClaimScanPanel.ContradictingEvidenceIds)
            .Concat(surface.ActionScanPanel.SupportingEvidenceIds)
            .Concat(surface.ActionScanPanel.ContradictingEvidenceIds)
            .Concat(surface.ReadinessMatrixPanel.Rows.Select(row => row.EvidenceId))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

    private static IReadOnlyList<EvidenceIntelligenceTimelineEvent> Timeline(EvidenceIntelligenceSurfaceViewModel surface) =>
    [
        Event("timeline.evidence-index", "Evidence index loaded", "index", surface.SurfaceId, EvidenceRefs(surface), surface.IndexSummary.Summary),
        Event("timeline.lexical-search", "Lexical search preview", "search", surface.SearchPanel.Query, surface.SearchPanel.Results.Select(result => result.EvidenceId).ToList(), surface.SearchPanel.FooterNote),
        Event("timeline.claim-scan", "Claim scan evaluated", "claim", surface.ClaimScanPanel.Claim, surface.ClaimScanPanel.SupportingEvidenceIds.Concat(surface.ClaimScanPanel.ContradictingEvidenceIds).ToList(), surface.ClaimScanPanel.Rationale),
        Event("timeline.action-scan", "Action scan evaluated", "action", surface.ActionScanPanel.ActionId, surface.ActionScanPanel.SupportingEvidenceIds.Concat(surface.ActionScanPanel.ContradictingEvidenceIds).ToList(), surface.ActionScanPanel.Rationale),
        Event("timeline.readiness", "Readiness matrix summarized", "readiness", surface.ReadinessMatrixPanel.FinalVerdict, surface.ReadinessMatrixPanel.Rows.Select(row => row.EvidenceId).ToList(), surface.ReadinessMatrixPanel.SafeNextStep)
    ];

    private static EvidenceIntelligenceTimelineEvent Event(
        string eventId,
        string label,
        string eventKind,
        string source,
        IReadOnlyList<string> evidenceRefs,
        string summary) =>
        new(eventId, label, eventKind, source, evidenceRefs, summary);

    private static IReadOnlyList<EvidenceIntelligenceTimelineExportSection> Sections(
        EvidenceIntelligenceReadOnlyUiMountViewModel mount,
        EvidenceIntelligencePersistencePlan persistencePlan,
        DisabledEvidenceIntelligenceReadStore readStore,
        DisabledEvidenceIntelligenceWriteStore writeStore,
        EvidenceIntelligenceMigrationCapabilityStatus dryRunStatus,
        EvidenceIntelligenceSchemaCompatibilityStatus schemaStatus,
        EvidenceIntelligenceTimelineExportNoSideEffectProof proof,
        IReadOnlyList<EvidenceIntelligenceTimelineEvent> timeline,
        IReadOnlyList<string> evidenceRefs) =>
    [
        Section("executive-summary", "Executive summary", EvidenceIntelligenceTimelineExportSourceKind.PresenterSnapshot, evidenceRefs, [mount.Surface.Header.Summary, mount.Surface.NoRuntimeNotice], proof),
        Section("evidence-index-summary", "Evidence index summary", EvidenceIntelligenceTimelineExportSourceKind.PresenterSnapshot, evidenceRefs, [$"Total evidence: {mount.Surface.IndexSummary.TotalEvidenceItems}.", mount.Surface.IndexSummary.Summary], proof),
        Section("timeline-events", "Timeline events", EvidenceIntelligenceTimelineExportSourceKind.FixtureSurface, evidenceRefs, timeline.Select(item => $"{item.EventId}: {item.Label}").ToList(), proof),
        Section("claims-and-evidence-links", "Claims and evidence links", EvidenceIntelligenceTimelineExportSourceKind.PresenterSnapshot, mount.Surface.ClaimScanPanel.SupportingEvidenceIds.Concat(mount.Surface.ClaimScanPanel.ContradictingEvidenceIds).ToList(), [mount.Surface.ClaimScanPanel.VerdictLabel, mount.Surface.ClaimScanPanel.Rationale], proof),
        Section("action-scan-results", "Action scan results", EvidenceIntelligenceTimelineExportSourceKind.PresenterSnapshot, mount.Surface.ActionScanPanel.SupportingEvidenceIds.Concat(mount.Surface.ActionScanPanel.ContradictingEvidenceIds).ToList(), [mount.Surface.ActionScanPanel.VerdictLabel, mount.Surface.ActionScanPanel.Rationale, mount.Surface.ActionScanPanel.SafeNextStep], proof),
        Section("contradictions-and-risks", "Contradictions and risks", EvidenceIntelligenceTimelineExportSourceKind.PresenterSnapshot, mount.Surface.ClaimScanPanel.ContradictingEvidenceIds.Concat(mount.Surface.ActionScanPanel.ContradictingEvidenceIds).ToList(), mount.Surface.ReadinessMatrixPanel.BlockingReasons.DefaultIfEmpty("No contradiction-free runtime authority is granted.").ToList(), proof),
        Section("typed-evidence-graph-summary", "Typed evidence graph summary", EvidenceIntelligenceTimelineExportSourceKind.PresenterSnapshot, evidenceRefs, [$"Edges: {mount.Surface.GraphSummary.EdgeCount}.", mount.Surface.GraphSummary.Summary], proof),
        Section("readiness-matrix", "Readiness matrix", EvidenceIntelligenceTimelineExportSourceKind.PresenterSnapshot, mount.Surface.ReadinessMatrixPanel.Rows.Select(row => row.EvidenceId).ToList(), [$"Final verdict: {mount.Surface.ReadinessMatrixPanel.FinalVerdict}.", $"Blocks runtime: {mount.Surface.ReadinessMatrixPanel.BlocksRuntime}."], proof),
        Section("safe-next-step", "Safe next step", EvidenceIntelligenceTimelineExportSourceKind.PresenterSnapshot, evidenceRefs, [mount.Surface.ReadinessMatrixPanel.SafeNextStep], proof),
        Section("human-actions-required", "Human actions required", EvidenceIntelligenceTimelineExportSourceKind.PresenterSnapshot, evidenceRefs, mount.Surface.ReadinessMatrixPanel.RequiredHumanActions, proof),
        Section("persistence-capability-status", "Persistence capability status", EvidenceIntelligenceTimelineExportSourceKind.PersistenceCapabilityStatus, [], [persistencePlan.CapabilityStatus.ImplementationStatus, $"Durable store enabled: {persistencePlan.CapabilityStatus.DurableStoreEnabled}."], proof),
        Section("read-store-scaffold-status", "Read store scaffold status", EvidenceIntelligenceTimelineExportSourceKind.ReadStoreScaffoldStatus, [], [readStore.ScaffoldStatus.Mode, $"Filesystem read enabled: {readStore.ScaffoldStatus.FilesystemReadEnabled}."], proof),
        Section("write-store-scaffold-status", "Write store scaffold status", EvidenceIntelligenceTimelineExportSourceKind.WriteStoreScaffoldStatus, [], [writeStore.ScaffoldStatus.Mode, $"Filesystem write enabled: {writeStore.ScaffoldStatus.FilesystemWriteEnabled}."], proof),
        Section("redaction-at-write-hostile-coverage", "Redaction-at-write hostile coverage", EvidenceIntelligenceTimelineExportSourceKind.RedactionHostileCoverage, [], ["Hostile fixture coverage exists for synthetic secret-like, raw payload, unknown sensitivity and integrity-before-redaction cases.", "No redaction pipeline is executed by this preview."], proof),
        Section("dry-run-migration-plan-status", "Dry-run migration plan status", EvidenceIntelligenceTimelineExportSourceKind.DryRunMigrationPlanStatus, [], [dryRunStatus.Mode, $"Migration execution enabled: {dryRunStatus.MigrationExecutionEnabled}."], proof),
        Section("schema-compatibility-guard-status", "Schema compatibility guard status", EvidenceIntelligenceTimelineExportSourceKind.SchemaCompatibilityGuardStatus, [], [schemaStatus.Mode, $"Service registration enabled: {schemaStatus.ServiceRegistrationEnabled}."], proof),
        Section("export-blockers", "Export blockers", EvidenceIntelligenceTimelineExportSourceKind.NoSideEffectProof, [], Blockers(), proof, status: EvidenceIntelligenceTimelineExportSectionStatus.Blocked, blockers: Blockers()),
        Section("export-warnings", "Export warnings", EvidenceIntelligenceTimelineExportSourceKind.NoSideEffectProof, [], Warnings(), proof, status: EvidenceIntelligenceTimelineExportSectionStatus.Warning, warnings: Warnings()),
        Section("no-side-effect-proof", "No-side-effect proof", EvidenceIntelligenceTimelineExportSourceKind.NoSideEffectProof, [], [$"Proof passes: {proof.Passes}.", $"Export file created: {proof.ExportFileCreated}.", $"Filesystem write attempted: {proof.FilesystemWriteAttempted}."], proof),
        Section("deferred-capabilities", "Deferred capabilities / documented debt", EvidenceIntelligenceTimelineExportSourceKind.DocumentedDebt, [], ["Durable persistence remains deferred.", "Physical export remains deferred.", "Runtime/live/provider/cloud remain deferred."], proof, status: EvidenceIntelligenceTimelineExportSectionStatus.Deferred)
    ];

    private static EvidenceIntelligenceTimelineExportSection Section(
        string sectionId,
        string title,
        EvidenceIntelligenceTimelineExportSourceKind sourceKind,
        IReadOnlyList<string> evidenceRefs,
        IReadOnlyList<string> lines,
        EvidenceIntelligenceTimelineExportNoSideEffectProof proof,
        EvidenceIntelligenceTimelineExportSectionStatus status = EvidenceIntelligenceTimelineExportSectionStatus.Included,
        IReadOnlyList<string>? warnings = null,
        IReadOnlyList<string>? blockers = null) =>
        new(
            sectionId,
            title,
            status,
            sourceKind,
            evidenceRefs,
            Sanitize(lines),
            warnings ?? [],
            blockers ?? [],
            ExportableInReadOnlyPreview: true,
            RealExportOccurred: false,
            NoSideEffectProof: proof);

    private static IReadOnlyList<string> Warnings() =>
    [
        "Preview is copy-ready text only; no physical export is enabled.",
        "Raw and sensitive-never-persist payload classes are excluded.",
        "Semantic/vector backend remains disabled."
    ];

    private static IReadOnlyList<string> Blockers() =>
    [
        "Physical export to filesystem is disabled.",
        "Durable persistence is disabled.",
        "Runtime/live/provider/cloud actions are disabled."
    ];

    private static string CopyReadyPreview(
        EvidenceIntelligenceTimelineExportManifest manifest,
        IReadOnlyList<EvidenceIntelligenceTimelineExportSection> sections) =>
        string.Join(
            "\n",
            new[] { $"# {manifest.Title}", $"Mode: {manifest.Mode}", $"Source: {manifest.SourceLabel}" }
                .Concat(sections.Select(section => $"## {section.Title}\n{string.Join("\n", section.Lines)}")));

    private static IReadOnlyList<string> Sanitize(IReadOnlyList<string> lines) =>
        lines
            .Select(line => line.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal))
            .Select(line => line.Contains("raw payload", StringComparison.OrdinalIgnoreCase) ? line.Replace("raw payload", "excluded payload", StringComparison.OrdinalIgnoreCase) : line)
            .ToList();
}
