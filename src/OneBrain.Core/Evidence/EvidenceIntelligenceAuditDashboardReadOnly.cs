namespace OneBrain.Core.Evidence;

public enum EvidenceIntelligenceAuditDashboardCardStatus
{
    Ready,
    Disabled,
    Blocked,
    Warning,
    NoGo,
    Deferred
}

public enum EvidenceIntelligenceAuditDashboardSeverity
{
    Info,
    P3,
    P2,
    P1
}

public enum EvidenceIntelligenceAuditDashboardSourceKind
{
    TimelineExportPreview,
    PersistenceDesign,
    ReadStoreScaffold,
    WriteStoreScaffold,
    RedactionHostileCoverage,
    DryRunMigrationPlan,
    SchemaCompatibilityGuard,
    RuntimeGate,
    ReleaseGate,
    ProviderCloudGate,
    FilesystemDatabaseGate,
    MigrationGate,
    NoSideEffectProof,
    DocumentedDebt
}

public sealed record EvidenceIntelligenceAuditDashboardNoSideEffectProof(
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
    bool ProductWriteFallbackUsed,
    bool ProductActionCommandExposed,
    bool ProductActionButtonExposed)
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
        && !ProductWriteFallbackUsed
        && !ProductActionCommandExposed
        && !ProductActionButtonExposed;

    public static EvidenceIntelligenceAuditDashboardNoSideEffectProof FromTimelineExport(EvidenceIntelligenceTimelineExportNoSideEffectProof proof) =>
        new(
            ReadOnly: proof.ReadOnly,
            Deterministic: proof.Deterministic,
            FixtureSafe: proof.FixtureSafe,
            FilesystemReadAttempted: proof.FilesystemReadAttempted,
            FilesystemWriteAttempted: proof.FilesystemWriteAttempted,
            ExportFileCreated: proof.ExportFileCreated,
            DatabaseTouched: proof.DatabaseTouched,
            DurablePersistenceActive: proof.DurablePersistenceActive,
            MigrationRunnerStarted: proof.MigrationRunnerStarted,
            MigrationExecuted: proof.MigrationExecuted,
            ProviderCloudTouched: proof.ProviderCloudTouched,
            SemanticVectorBackendTouched: proof.SemanticVectorBackendTouched,
            RuntimeTouched: proof.RuntimeTouched,
            BrowserCdpTouched: proof.BrowserCdpTouched,
            WcuTouched: proof.WcuTouched,
            OcrTouched: proof.OcrTouched,
            ProductWriteFallbackUsed: proof.ProductWriteFallbackUsed,
            ProductActionCommandExposed: false,
            ProductActionButtonExposed: false);
}

public sealed record EvidenceIntelligenceAuditDashboardGate(
    string GateId,
    string Title,
    EvidenceIntelligenceAuditDashboardCardStatus Status,
    string Decision,
    bool RuntimeAllowed,
    bool ReleaseAllowed,
    IReadOnlyList<string> Blockers);

public sealed record EvidenceIntelligenceAuditDashboardCard(
    string CardId,
    string Title,
    EvidenceIntelligenceAuditDashboardCardStatus Status,
    string ReadinessRange,
    EvidenceIntelligenceAuditDashboardSeverity Severity,
    EvidenceIntelligenceAuditDashboardSourceKind SourceKind,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> Lines,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    int AllowedActionsCount,
    EvidenceIntelligenceAuditDashboardNoSideEffectProof NoSideEffectProof);

public sealed record EvidenceIntelligenceAuditDashboardResult(
    string DashboardId,
    string Title,
    string Mode,
    string SourceLabel,
    string PhaseCReadinessRange,
    string RuntimeLiveReadiness,
    string ReleaseCommercialDecision,
    IReadOnlyList<EvidenceIntelligenceAuditDashboardCard> Cards,
    IReadOnlyList<EvidenceIntelligenceAuditDashboardGate> Gates,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    IReadOnlyList<string> DocumentedDebt,
    string NextSafeStep,
    string ReadOnlySummary,
    EvidenceIntelligenceTimelineExportResult TimelineExport,
    EvidenceIntelligenceAuditDashboardNoSideEffectProof NoSideEffectProof)
{
    public bool ReadOnly => NoSideEffectProof.ReadOnly;
    public bool Deterministic => NoSideEffectProof.Deterministic;
    public bool FixtureSafe => NoSideEffectProof.FixtureSafe;
    public bool HasProductActions => Cards.Any(card => card.AllowedActionsCount > 0)
        || NoSideEffectProof.ProductActionCommandExposed
        || NoSideEffectProof.ProductActionButtonExposed;
}

public static class EvidenceIntelligenceAuditDashboardReadOnlyPresenter
{
    public static EvidenceIntelligenceAuditDashboardResult CreateFixture()
    {
        var export = EvidenceIntelligenceTimelineExportReadOnlyPresenter.CreateFixture();
        var proof = EvidenceIntelligenceAuditDashboardNoSideEffectProof.FromTimelineExport(export.NoSideEffectProof);
        var blockers = Blockers();
        var warnings = Warnings();
        var debt = DocumentedDebt();
        var gates = Gates();
        var cards = Cards(export, proof, blockers, warnings, debt);
        var summary = ReadOnlySummary(cards, gates);

        return new EvidenceIntelligenceAuditDashboardResult(
            DashboardId: "eil.audit-dashboard.read-only.fixture.v1",
            Title: "Evidence Intelligence Read-Only Audit Dashboard",
            Mode: "READ_ONLY_AUDIT_SAFE_FIXTURE_NO_ACTIONS",
            SourceLabel: "EvidenceIntelligenceTimelineExportReadOnlyPresenter.CreateFixture",
            PhaseCReadinessRange: "72-82%",
            RuntimeLiveReadiness: "0%",
            ReleaseCommercialDecision: "NO-GO",
            Cards: cards,
            Gates: gates,
            Warnings: warnings,
            Blockers: blockers,
            DocumentedDebt: debt,
            NextSafeStep: "Prepare Fase C data/persistence/evidence closeout audit before any durable implementation.",
            ReadOnlySummary: summary,
            TimelineExport: export,
            NoSideEffectProof: proof);
    }

    private static IReadOnlyList<EvidenceIntelligenceAuditDashboardCard> Cards(
        EvidenceIntelligenceTimelineExportResult export,
        EvidenceIntelligenceAuditDashboardNoSideEffectProof proof,
        IReadOnlyList<string> blockers,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> debt) =>
    [
        Card("executive-audit-summary", "Executive audit summary", EvidenceIntelligenceAuditDashboardCardStatus.Ready, "97-99%", EvidenceIntelligenceAuditDashboardSeverity.Info, EvidenceIntelligenceAuditDashboardSourceKind.TimelineExportPreview, export.Manifest.IncludedEvidenceRefs, [export.Manifest.Title, export.Manifest.Mode], [], [], proof),
        Card("phase-c-readiness-summary", "Phase C readiness summary", EvidenceIntelligenceAuditDashboardCardStatus.Warning, "72-82%", EvidenceIntelligenceAuditDashboardSeverity.P3, EvidenceIntelligenceAuditDashboardSourceKind.DocumentedDebt, [], ["Fase C is advanced but still design/scaffold/read-only only.", "Runtime/live readiness remains 0%."], warnings, [], proof),
        Card("eil-persistence-design", "EIL persistence design", EvidenceIntelligenceAuditDashboardCardStatus.Ready, "92-95%", EvidenceIntelligenceAuditDashboardSeverity.Info, EvidenceIntelligenceAuditDashboardSourceKind.PersistenceDesign, [], [export.PersistenceStatus.ImplementationStatus, $"Fail closed: {export.PersistenceStatus.FailClosed}."], [], [], proof),
        Card("read-store-scaffold-disabled", "Read store scaffold disabled", EvidenceIntelligenceAuditDashboardCardStatus.Disabled, "80-90%", EvidenceIntelligenceAuditDashboardSeverity.Info, EvidenceIntelligenceAuditDashboardSourceKind.ReadStoreScaffold, [], [export.ReadStoreStatus.Mode, $"Filesystem read enabled: {export.ReadStoreStatus.FilesystemReadEnabled}."], [], [], proof),
        Card("write-store-scaffold-disabled", "Write store scaffold disabled", EvidenceIntelligenceAuditDashboardCardStatus.Disabled, "75-85%", EvidenceIntelligenceAuditDashboardSeverity.Info, EvidenceIntelligenceAuditDashboardSourceKind.WriteStoreScaffold, [], [export.WriteStoreStatus.Mode, $"Filesystem write enabled: {export.WriteStoreStatus.FilesystemWriteEnabled}."], [], [], proof),
        Card("redaction-at-write-hostile-coverage", "Redaction-at-write hostile fixture coverage", EvidenceIntelligenceAuditDashboardCardStatus.Ready, "70-85%", EvidenceIntelligenceAuditDashboardSeverity.Info, EvidenceIntelligenceAuditDashboardSourceKind.RedactionHostileCoverage, [], ["Hostile coverage is represented for synthetic secret-like, excluded payload, unknown sensitivity and integrity-before-redaction cases.", "The dashboard does not execute a redaction pipeline."], [], [], proof),
        Card("dry-run-migration-plan", "Dry-run migration plan", EvidenceIntelligenceAuditDashboardCardStatus.Disabled, "70-85%", EvidenceIntelligenceAuditDashboardSeverity.Info, EvidenceIntelligenceAuditDashboardSourceKind.DryRunMigrationPlan, [], [export.DryRunMigrationStatus.Mode, $"Migration execution enabled: {export.DryRunMigrationStatus.MigrationExecutionEnabled}."], [], [], proof),
        Card("schema-compatibility-guards", "Schema compatibility guards", EvidenceIntelligenceAuditDashboardCardStatus.Disabled, "70-85%", EvidenceIntelligenceAuditDashboardSeverity.Info, EvidenceIntelligenceAuditDashboardSourceKind.SchemaCompatibilityGuard, [], [export.SchemaCompatibilityStatus.Mode, $"Service registration enabled: {export.SchemaCompatibilityStatus.ServiceRegistrationEnabled}."], [], [], proof),
        Card("evidence-timeline-export-preview", "Evidence timeline export preview", EvidenceIntelligenceAuditDashboardCardStatus.Ready, "70-85%", EvidenceIntelligenceAuditDashboardSeverity.Info, EvidenceIntelligenceAuditDashboardSourceKind.TimelineExportPreview, export.Manifest.IncludedEvidenceRefs, [$"Sections: {export.Sections.Count}.", $"Physical export enabled: {export.PhysicalExportEnabled}."], export.Warnings, export.Blockers, proof),
        Card("runtime-live-gate", "Runtime/live gate", EvidenceIntelligenceAuditDashboardCardStatus.Blocked, "0%", EvidenceIntelligenceAuditDashboardSeverity.P2, EvidenceIntelligenceAuditDashboardSourceKind.RuntimeGate, [], ["Runtime actions are blocked.", "Browser/CDP, WCU and OCR live remain disabled."], [], ["Runtime/live readiness is 0%."], proof),
        Card("release-commercial-no-go", "Release/commercial NO-GO", EvidenceIntelligenceAuditDashboardCardStatus.NoGo, "NO-GO", EvidenceIntelligenceAuditDashboardSeverity.P2, EvidenceIntelligenceAuditDashboardSourceKind.ReleaseGate, [], ["Release/commercial readiness is NO-GO.", "No production claim is made by this dashboard."], [], ["Runtime/live and durable persistence remain blocked."], proof),
        Card("provider-cloud-disabled", "Provider/cloud disabled", EvidenceIntelligenceAuditDashboardCardStatus.Disabled, "0%", EvidenceIntelligenceAuditDashboardSeverity.Info, EvidenceIntelligenceAuditDashboardSourceKind.ProviderCloudGate, [], [$"Provider/cloud touched: {proof.ProviderCloudTouched}."], [], [], proof),
        Card("filesystem-db-durable-disabled", "Filesystem/DB/durable persistence disabled", EvidenceIntelligenceAuditDashboardCardStatus.Disabled, "0%", EvidenceIntelligenceAuditDashboardSeverity.Info, EvidenceIntelligenceAuditDashboardSourceKind.FilesystemDatabaseGate, [], [$"Filesystem read attempted: {proof.FilesystemReadAttempted}.", $"Filesystem write attempted: {proof.FilesystemWriteAttempted}.", $"Database touched: {proof.DatabaseTouched}.", $"Durable persistence active: {proof.DurablePersistenceActive}."], [], [], proof),
        Card("migration-runner-disabled", "Migration runner disabled", EvidenceIntelligenceAuditDashboardCardStatus.Disabled, "0%", EvidenceIntelligenceAuditDashboardSeverity.Info, EvidenceIntelligenceAuditDashboardSourceKind.MigrationGate, [], [$"Migration runner started: {proof.MigrationRunnerStarted}.", $"Migration executed: {proof.MigrationExecuted}."], [], [], proof),
        Card("raw-payload-secret-exclusion", "Excluded payload and secret exclusion", EvidenceIntelligenceAuditDashboardCardStatus.Ready, "70-85%", EvidenceIntelligenceAuditDashboardSeverity.Info, EvidenceIntelligenceAuditDashboardSourceKind.NoSideEffectProof, [], export.Manifest.ExcludedContentClasses.Select(item => $"Excluded: {item}.").ToList(), [], [], proof),
        Card("blockers-list", "Blockers list", EvidenceIntelligenceAuditDashboardCardStatus.Blocked, "Blocked", EvidenceIntelligenceAuditDashboardSeverity.P2, EvidenceIntelligenceAuditDashboardSourceKind.NoSideEffectProof, [], blockers, [], blockers, proof),
        Card("warnings-list", "Warnings list", EvidenceIntelligenceAuditDashboardCardStatus.Warning, "Needs review", EvidenceIntelligenceAuditDashboardSeverity.P3, EvidenceIntelligenceAuditDashboardSourceKind.NoSideEffectProof, [], warnings, warnings, [], proof),
        Card("documented-debt-list", "Documented debt list", EvidenceIntelligenceAuditDashboardCardStatus.Deferred, "Deferred", EvidenceIntelligenceAuditDashboardSeverity.P3, EvidenceIntelligenceAuditDashboardSourceKind.DocumentedDebt, [], debt, [], [], proof),
        Card("no-side-effect-proof", "No-side-effect proof", EvidenceIntelligenceAuditDashboardCardStatus.Ready, "PASS", EvidenceIntelligenceAuditDashboardSeverity.Info, EvidenceIntelligenceAuditDashboardSourceKind.NoSideEffectProof, [], [$"Proof passes: {proof.Passes}.", $"Product action command exposed: {proof.ProductActionCommandExposed}.", $"Product action button exposed: {proof.ProductActionButtonExposed}."], [], [], proof),
        Card("next-safe-step", "Next safe step", EvidenceIntelligenceAuditDashboardCardStatus.Ready, "Safe", EvidenceIntelligenceAuditDashboardSeverity.Info, EvidenceIntelligenceAuditDashboardSourceKind.DocumentedDebt, [], ["Prepare Fase C data/persistence/evidence closeout audit before any durable implementation."], [], [], proof)
    ];

    private static EvidenceIntelligenceAuditDashboardCard Card(
        string cardId,
        string title,
        EvidenceIntelligenceAuditDashboardCardStatus status,
        string readinessRange,
        EvidenceIntelligenceAuditDashboardSeverity severity,
        EvidenceIntelligenceAuditDashboardSourceKind sourceKind,
        IReadOnlyList<string> evidenceRefs,
        IReadOnlyList<string> lines,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> blockers,
        EvidenceIntelligenceAuditDashboardNoSideEffectProof proof) =>
        new(
            cardId,
            title,
            status,
            readinessRange,
            severity,
            sourceKind,
            evidenceRefs,
            Sanitize(lines),
            Sanitize(warnings),
            Sanitize(blockers),
            AllowedActionsCount: 0,
            NoSideEffectProof: proof);

    private static IReadOnlyList<EvidenceIntelligenceAuditDashboardGate> Gates() =>
    [
        new("runtime-live", "Runtime/live gate", EvidenceIntelligenceAuditDashboardCardStatus.Blocked, "BLOCKED_0_PERCENT", RuntimeAllowed: false, ReleaseAllowed: false, ["Runtime/live remains 0%."]),
        new("release-commercial", "Release/commercial gate", EvidenceIntelligenceAuditDashboardCardStatus.NoGo, "NO-GO", RuntimeAllowed: false, ReleaseAllowed: false, ["Release/commercial remains NO-GO."]),
        new("provider-cloud", "Provider/cloud gate", EvidenceIntelligenceAuditDashboardCardStatus.Disabled, "DISABLED", RuntimeAllowed: false, ReleaseAllowed: false, ["Provider/cloud calls remain disabled."]),
        new("filesystem-db-persistence", "Filesystem/DB/durable persistence gate", EvidenceIntelligenceAuditDashboardCardStatus.Disabled, "DISABLED", RuntimeAllowed: false, ReleaseAllowed: false, ["Filesystem, DB and durable persistence remain disabled."])
    ];

    private static IReadOnlyList<string> Warnings() =>
    [
        "Dashboard is fixture-safe and read-only.",
        "Fase C is not a durable persistence implementation.",
        "Manual installed-extension QA remains separate from this dashboard contract."
    ];

    private static IReadOnlyList<string> Blockers() =>
    [
        "Runtime/live/browser/CDP/WCU/OCR remain blocked.",
        "Filesystem export and durable persistence remain blocked.",
        "Provider/cloud/network and semantic/vector backend remain disabled.",
        "Release/commercial decision remains NO-GO."
    ];

    private static IReadOnlyList<string> DocumentedDebt() =>
    [
        "Fase C closeout audit packet.",
        "Future durable persistence design-to-implementation audit.",
        "Manual installed-extension QA before any visible product claim.",
        "Any physical export remains a future explicit hito."
    ];

    private static string ReadOnlySummary(
        IReadOnlyList<EvidenceIntelligenceAuditDashboardCard> cards,
        IReadOnlyList<EvidenceIntelligenceAuditDashboardGate> gates) =>
        string.Join(
            "\n",
            new[] { "# Evidence Intelligence Read-Only Audit Dashboard", "Mode: READ_ONLY_AUDIT_SAFE_FIXTURE_NO_ACTIONS" }
                .Concat(cards.Select(card => $"## {card.Title}\nStatus: {card.Status}\nReadiness: {card.ReadinessRange}\nAllowed product actions: {card.AllowedActionsCount}"))
                .Concat(gates.Select(gate => $"## {gate.Title}\nDecision: {gate.Decision}\nRuntime allowed: {gate.RuntimeAllowed}\nRelease allowed: {gate.ReleaseAllowed}")));

    private static IReadOnlyList<string> Sanitize(IReadOnlyList<string> lines) =>
        lines
            .Select(line => line.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal))
            .Select(line => line.Contains("raw payload", StringComparison.OrdinalIgnoreCase) ? line.Replace("raw payload", "excluded payload", StringComparison.OrdinalIgnoreCase) : line)
            .ToList();
}
