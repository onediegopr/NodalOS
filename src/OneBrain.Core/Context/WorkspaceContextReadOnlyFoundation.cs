using OneBrain.Core.Evidence;

namespace OneBrain.Core.Context;

public enum WorkspaceContextItemKind
{
    WorkspaceIdentity,
    WorkspaceBoundary,
    ContextPacketSummary,
    SelectedContext,
    LockedContext,
    ExcludedContext,
    EvidenceLinkedContextReference,
    AuthorityPolicy,
    FreshnessSignal,
    ContradictionMemoryPreview,
    RiskMemoryPreview,
    DecisionMemoryPreview,
    ClaimMemoryPreview,
    ActionMemoryPreview,
    MissingContextWarning,
    StaleContextWarning,
    SensitiveUnsafeContextBlocker,
    ProviderCloudDisabledNotice,
    SemanticVectorDisabledNotice,
    SafeNextStep,
    NoSideEffectProof,
    DeferredCapability
}

public enum WorkspaceContextItemStatus
{
    Ready,
    Selected,
    Locked,
    Excluded,
    Warning,
    Blocked,
    Disabled,
    Deferred
}

public enum WorkspaceContextSourceKind
{
    Fixture,
    EilReadOnlyEvidence,
    EilTimelineExportPreview,
    HumanProvidedFixture,
    CapabilityNotice,
    NoSideEffectProof,
    DocumentedDebt
}

public enum WorkspaceContextAuthorityLevel
{
    Informational,
    EvidenceLinked,
    HumanReviewRequired,
    LockedBySafety,
    ExcludedBySafety
}

public enum WorkspaceContextFreshnessStatus
{
    FixtureCurrent,
    Fresh,
    Stale,
    Missing,
    NotApplicable
}

public sealed record WorkspaceContextNoSideEffectProof(
    bool ReadOnly,
    bool Deterministic,
    bool FixtureSafe,
    bool WorkspaceFilesystemReadAttempted,
    bool FilesystemWriteAttempted,
    bool DatabaseTouched,
    bool DurablePersistenceActive,
    bool DurableMemoryActive,
    bool VectorSemanticBackendTouched,
    bool LlmProviderTouched,
    bool ProviderCloudTouched,
    bool MigrationRunnerStarted,
    bool MigrationExecuted,
    bool RuntimeTouched,
    bool BrowserCdpTouched,
    bool WcuTouched,
    bool OcrTouched,
    bool ProductActionExposed,
    bool ProductServiceRegistered)
{
    public bool Passes =>
        ReadOnly
        && Deterministic
        && FixtureSafe
        && !WorkspaceFilesystemReadAttempted
        && !FilesystemWriteAttempted
        && !DatabaseTouched
        && !DurablePersistenceActive
        && !DurableMemoryActive
        && !VectorSemanticBackendTouched
        && !LlmProviderTouched
        && !ProviderCloudTouched
        && !MigrationRunnerStarted
        && !MigrationExecuted
        && !RuntimeTouched
        && !BrowserCdpTouched
        && !WcuTouched
        && !OcrTouched
        && !ProductActionExposed
        && !ProductServiceRegistered;

    public static WorkspaceContextNoSideEffectProof FixtureReadOnly() =>
        new(
            ReadOnly: true,
            Deterministic: true,
            FixtureSafe: true,
            WorkspaceFilesystemReadAttempted: false,
            FilesystemWriteAttempted: false,
            DatabaseTouched: false,
            DurablePersistenceActive: false,
            DurableMemoryActive: false,
            VectorSemanticBackendTouched: false,
            LlmProviderTouched: false,
            ProviderCloudTouched: false,
            MigrationRunnerStarted: false,
            MigrationExecuted: false,
            RuntimeTouched: false,
            BrowserCdpTouched: false,
            WcuTouched: false,
            OcrTouched: false,
            ProductActionExposed: false,
            ProductServiceRegistered: false);
}

public sealed record WorkspaceContextSourceDescriptor(
    string SourceId,
    string Label,
    WorkspaceContextSourceKind SourceKind,
    WorkspaceContextAuthorityLevel Authority,
    WorkspaceContextFreshnessStatus Freshness,
    bool FixtureOnly,
    bool ReadsWorkspaceFilesystem,
    bool UsesProviderCloud,
    bool UsesVectorSemanticBackend,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    WorkspaceContextNoSideEffectProof NoSideEffectProof);

public sealed record WorkspaceContextItem(
    string ItemId,
    WorkspaceContextItemKind Kind,
    WorkspaceContextItemStatus Status,
    string Title,
    string Summary,
    WorkspaceContextSourceKind Source,
    WorkspaceContextAuthorityLevel Authority,
    WorkspaceContextFreshnessStatus Freshness,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    bool Selected,
    bool Locked,
    bool Excluded,
    WorkspaceContextNoSideEffectProof NoSideEffectProof);

public sealed record WorkspaceContextMemoryCandidate(
    string CandidateId,
    WorkspaceContextItemKind Kind,
    WorkspaceContextItemStatus Status,
    string Title,
    string Preview,
    WorkspaceContextAuthorityLevel Authority,
    WorkspaceContextFreshnessStatus Freshness,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    bool DurableMemoryEnabled,
    bool Selected,
    bool Locked,
    bool Excluded,
    WorkspaceContextNoSideEffectProof NoSideEffectProof);

public sealed record WorkspaceContextPacketReadOnly(
    string PacketId,
    string WorkspaceId,
    string Title,
    string Mode,
    string SourceLabel,
    string Summary,
    IReadOnlyList<WorkspaceContextSourceDescriptor> Sources,
    IReadOnlyList<WorkspaceContextItem> Items,
    IReadOnlyList<WorkspaceContextMemoryCandidate> MemoryCandidates,
    IReadOnlyList<WorkspaceContextItem> SelectedContext,
    IReadOnlyList<WorkspaceContextItem> LockedContext,
    IReadOnlyList<WorkspaceContextItem> ExcludedContext,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    IReadOnlyList<string> DocumentedDebt,
    string ProviderCloudNotice,
    string SemanticVectorNotice,
    string SafeNextStep,
    string ReadOnlySummary,
    WorkspaceContextNoSideEffectProof NoSideEffectProof)
{
    public bool ReadOnly => NoSideEffectProof.ReadOnly;
    public bool Deterministic => NoSideEffectProof.Deterministic;
    public bool FixtureSafe => NoSideEffectProof.FixtureSafe;
    public bool HasDurableMemory => MemoryCandidates.Any(candidate => candidate.DurableMemoryEnabled)
        || NoSideEffectProof.DurableMemoryActive;
    public bool HasProductActions => NoSideEffectProof.ProductActionExposed;
}

public static class WorkspaceContextReadOnlyPresenter
{
    public static WorkspaceContextPacketReadOnly CreateFixture()
    {
        var auditDashboard = EvidenceIntelligenceAuditDashboardReadOnlyPresenter.CreateFixture();
        var proof = WorkspaceContextNoSideEffectProof.FixtureReadOnly();
        var evidenceRefs = auditDashboard.TimelineExport.Manifest.IncludedEvidenceRefs.Take(8).ToList();
        var warnings = Warnings();
        var blockers = Blockers();
        var debt = DocumentedDebt();
        var sources = Sources(evidenceRefs, proof);
        var items = Items(evidenceRefs, warnings, blockers, proof);
        var memoryCandidates = MemoryCandidates(evidenceRefs, proof);
        var selected = items.Where(item => item.Selected).ToList();
        var locked = items.Where(item => item.Locked).ToList();
        var excluded = items.Where(item => item.Excluded).ToList();
        var summary = Summary(items, memoryCandidates);

        return new WorkspaceContextPacketReadOnly(
            PacketId: "phase-d.workspace-context.packet.read-only.fixture.v1",
            WorkspaceId: "workspace.fixture.nodal-os.phase-d",
            Title: "Workspace Context and Memory Read-Only Foundation",
            Mode: "READ_ONLY_FIXTURE_SAFE_NO_MEMORY_RUNTIME",
            SourceLabel: "EvidenceIntelligenceAuditDashboardReadOnlyPresenter.CreateFixture",
            Summary: "Deterministic fixture context packet for Phase D foundation; no workspace scan, durable memory, provider, vector backend or runtime is enabled.",
            Sources: sources,
            Items: items,
            MemoryCandidates: memoryCandidates,
            SelectedContext: selected,
            LockedContext: locked,
            ExcludedContext: excluded,
            Warnings: warnings,
            Blockers: blockers,
            DocumentedDebt: debt,
            ProviderCloudNotice: "Provider/cloud calls are disabled for context and memory foundation.",
            SemanticVectorNotice: "Semantic/vector backend is disabled; memory candidates are lexical/read-only previews only.",
            SafeNextStep: "Harden context authority and freshness guards before expanding memory candidates or mounting a UI surface.",
            ReadOnlySummary: summary,
            NoSideEffectProof: proof);
    }

    private static IReadOnlyList<WorkspaceContextSourceDescriptor> Sources(
        IReadOnlyList<string> evidenceRefs,
        WorkspaceContextNoSideEffectProof proof) =>
    [
        new(
            "source.workspace-identity.fixture",
            "Workspace identity fixture",
            WorkspaceContextSourceKind.Fixture,
            WorkspaceContextAuthorityLevel.Informational,
            WorkspaceContextFreshnessStatus.FixtureCurrent,
            FixtureOnly: true,
            ReadsWorkspaceFilesystem: false,
            UsesProviderCloud: false,
            UsesVectorSemanticBackend: false,
            EvidenceRefs: [],
            Warnings: [],
            Blockers: [],
            NoSideEffectProof: proof),
        new(
            "source.eil.audit-dashboard.fixture",
            "EIL read-only audit dashboard fixture",
            WorkspaceContextSourceKind.EilReadOnlyEvidence,
            WorkspaceContextAuthorityLevel.EvidenceLinked,
            WorkspaceContextFreshnessStatus.FixtureCurrent,
            FixtureOnly: true,
            ReadsWorkspaceFilesystem: false,
            UsesProviderCloud: false,
            UsesVectorSemanticBackend: false,
            EvidenceRefs: evidenceRefs,
            Warnings: ["EIL is referenced as read-only evidence only."],
            Blockers: [],
            NoSideEffectProof: proof),
        new(
            "source.capability-notices.fixture",
            "Capability notices fixture",
            WorkspaceContextSourceKind.CapabilityNotice,
            WorkspaceContextAuthorityLevel.LockedBySafety,
            WorkspaceContextFreshnessStatus.NotApplicable,
            FixtureOnly: true,
            ReadsWorkspaceFilesystem: false,
            UsesProviderCloud: false,
            UsesVectorSemanticBackend: false,
            EvidenceRefs: [],
            Warnings: [],
            Blockers: ["Filesystem scan, durable memory, provider/cloud and vector backend remain disabled."],
            NoSideEffectProof: proof)
    ];

    private static IReadOnlyList<WorkspaceContextItem> Items(
        IReadOnlyList<string> evidenceRefs,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> blockers,
        WorkspaceContextNoSideEffectProof proof) =>
    [
        Item("workspace-identity", WorkspaceContextItemKind.WorkspaceIdentity, WorkspaceContextItemStatus.Ready, "Workspace identity fixture", "Synthetic workspace identity for Phase D foundation.", WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.Informational, WorkspaceContextFreshnessStatus.FixtureCurrent, [], [], [], selected: true, locked: false, excluded: false, proof),
        Item("workspace-boundary", WorkspaceContextItemKind.WorkspaceBoundary, WorkspaceContextItemStatus.Locked, "Workspace boundary descriptor", "Boundary is declared from fixture metadata; no workspace filesystem is scanned.", WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.LockedBySafety, WorkspaceContextFreshnessStatus.FixtureCurrent, [], [], ["Real workspace scan is disabled."], selected: true, locked: true, excluded: false, proof),
        Item("context-packet-summary", WorkspaceContextItemKind.ContextPacketSummary, WorkspaceContextItemStatus.Ready, "Context packet summary", "Read-only packet combines fixture context, EIL evidence refs and disabled capability notices.", WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.FixtureCurrent, evidenceRefs, [], [], selected: true, locked: false, excluded: false, proof),
        Item("selected-context-list", WorkspaceContextItemKind.SelectedContext, WorkspaceContextItemStatus.Selected, "Selected context list", "Selected context contains only fixture-safe identity, boundary and EIL evidence references.", WorkspaceContextSourceKind.HumanProvidedFixture, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.FixtureCurrent, evidenceRefs, [], [], selected: true, locked: false, excluded: false, proof),
        Item("locked-context-list", WorkspaceContextItemKind.LockedContext, WorkspaceContextItemStatus.Locked, "Locked context list", "Runtime, provider/cloud, vector backend and filesystem scanning are locked out.", WorkspaceContextSourceKind.CapabilityNotice, WorkspaceContextAuthorityLevel.LockedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, [], [], blockers, selected: false, locked: true, excluded: false, proof),
        Item("excluded-context-list", WorkspaceContextItemKind.ExcludedContext, WorkspaceContextItemStatus.Excluded, "Excluded context list", "Raw workspace files, secrets, provider output and durable memory records are excluded.", WorkspaceContextSourceKind.CapabilityNotice, WorkspaceContextAuthorityLevel.ExcludedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, [], warnings, ["Sensitive/raw context remains excluded."], selected: false, locked: false, excluded: true, proof),
        Item("evidence-linked-context-refs", WorkspaceContextItemKind.EvidenceLinkedContextReference, WorkspaceContextItemStatus.Ready, "Evidence-linked context refs", "EIL refs are included as labels only; no EIL persistence store is read.", WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.FixtureCurrent, evidenceRefs, [], [], selected: true, locked: false, excluded: false, proof),
        Item("authority-levels", WorkspaceContextItemKind.AuthorityPolicy, WorkspaceContextItemStatus.Ready, "Authority levels", "Informational, evidence-linked, human-review, locked and excluded authority levels are modeled.", WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.LockedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, [], [], [], selected: true, locked: true, excluded: false, proof),
        Item("freshness-staleness", WorkspaceContextItemKind.FreshnessSignal, WorkspaceContextItemStatus.Warning, "Freshness/staleness status", "Fixture context is current for tests, while real workspace freshness remains unavailable.", WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.HumanReviewRequired, WorkspaceContextFreshnessStatus.Stale, [], ["Real workspace freshness is not evaluated."], [], selected: false, locked: false, excluded: false, proof),
        Item("missing-context-warning", WorkspaceContextItemKind.MissingContextWarning, WorkspaceContextItemStatus.Warning, "Missing context warnings", "Installed-extension QA and real workspace inventory remain out of scope.", WorkspaceContextSourceKind.DocumentedDebt, WorkspaceContextAuthorityLevel.HumanReviewRequired, WorkspaceContextFreshnessStatus.Missing, [], ["Manual context confirmation remains required."], [], selected: false, locked: false, excluded: false, proof),
        Item("stale-context-warning", WorkspaceContextItemKind.StaleContextWarning, WorkspaceContextItemStatus.Warning, "Stale context warnings", "Stale signals block any future memory authority escalation.", WorkspaceContextSourceKind.DocumentedDebt, WorkspaceContextAuthorityLevel.HumanReviewRequired, WorkspaceContextFreshnessStatus.Stale, [], ["Stale context cannot authorize actions."], [], selected: false, locked: false, excluded: false, proof),
        Item("sensitive-context-blocker", WorkspaceContextItemKind.SensitiveUnsafeContextBlocker, WorkspaceContextItemStatus.Blocked, "Sensitive/unsafe context blockers", "Secrets, raw payloads and unknown-sensitivity context remain blocked.", WorkspaceContextSourceKind.CapabilityNotice, WorkspaceContextAuthorityLevel.ExcludedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, [], [], ["Sensitive context must be redacted and approved before any future use."], selected: false, locked: true, excluded: true, proof),
        Item("provider-cloud-disabled", WorkspaceContextItemKind.ProviderCloudDisabledNotice, WorkspaceContextItemStatus.Disabled, "Provider/cloud disabled notice", "No provider/cloud/network calls are enabled.", WorkspaceContextSourceKind.CapabilityNotice, WorkspaceContextAuthorityLevel.LockedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, [], [], ["Provider/cloud disabled."], selected: false, locked: true, excluded: false, proof),
        Item("semantic-vector-disabled", WorkspaceContextItemKind.SemanticVectorDisabledNotice, WorkspaceContextItemStatus.Disabled, "Semantic/vector disabled notice", "No embeddings, vector store or semantic backend are enabled.", WorkspaceContextSourceKind.CapabilityNotice, WorkspaceContextAuthorityLevel.LockedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, [], [], ["Semantic/vector backend disabled."], selected: false, locked: true, excluded: false, proof),
        Item("safe-next-step", WorkspaceContextItemKind.SafeNextStep, WorkspaceContextItemStatus.Ready, "Safe next step", "Harden authority/freshness guards before expanding candidates.", WorkspaceContextSourceKind.NoSideEffectProof, WorkspaceContextAuthorityLevel.HumanReviewRequired, WorkspaceContextFreshnessStatus.NotApplicable, [], [], [], selected: true, locked: false, excluded: false, proof),
        Item("no-side-effect-proof", WorkspaceContextItemKind.NoSideEffectProof, WorkspaceContextItemStatus.Ready, "No-side-effect proof", "Proof asserts no filesystem, DB, provider, vector, memory, persistence or runtime side effects.", WorkspaceContextSourceKind.NoSideEffectProof, WorkspaceContextAuthorityLevel.LockedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, [], [], [], selected: true, locked: true, excluded: false, proof),
        Item("deferred-capabilities", WorkspaceContextItemKind.DeferredCapability, WorkspaceContextItemStatus.Deferred, "Deferred capabilities / debt", "Durable memory, real workspace scan, provider/cloud and semantic/vector backend remain future explicit milestones.", WorkspaceContextSourceKind.DocumentedDebt, WorkspaceContextAuthorityLevel.HumanReviewRequired, WorkspaceContextFreshnessStatus.NotApplicable, [], warnings, [], selected: false, locked: false, excluded: false, proof)
    ];

    private static IReadOnlyList<WorkspaceContextMemoryCandidate> MemoryCandidates(
        IReadOnlyList<string> evidenceRefs,
        WorkspaceContextNoSideEffectProof proof) =>
    [
        Candidate("memory.contradiction-preview", WorkspaceContextItemKind.ContradictionMemoryPreview, "Contradiction memory preview", "Contradiction patterns are previewed from EIL fixture refs only.", WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.FixtureCurrent, evidenceRefs.Take(2).ToList(), [], [], selected: true, locked: false, excluded: false, proof),
        Candidate("memory.risk-preview", WorkspaceContextItemKind.RiskMemoryPreview, "Risk memory preview", "Risk notes remain non-durable and require human review.", WorkspaceContextAuthorityLevel.HumanReviewRequired, WorkspaceContextFreshnessStatus.FixtureCurrent, evidenceRefs.Take(2).ToList(), ["Risk memory is preview-only."], [], selected: true, locked: false, excluded: false, proof),
        Candidate("memory.decision-preview", WorkspaceContextItemKind.DecisionMemoryPreview, "Decision memory preview", "Decision context references Fase C NO-GO runtime/release gates.", WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.FixtureCurrent, evidenceRefs.Take(1).ToList(), [], [], selected: true, locked: false, excluded: false, proof),
        Candidate("memory.claim-preview", WorkspaceContextItemKind.ClaimMemoryPreview, "Claim memory preview", "Claim memory is a deterministic fixture label, not semantic memory.", WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.FixtureCurrent, evidenceRefs.Take(2).ToList(), [], [], selected: true, locked: false, excluded: false, proof),
        Candidate("memory.action-preview", WorkspaceContextItemKind.ActionMemoryPreview, "Action memory preview", "Action memory preserves no-runtime boundaries and cannot dispatch work.", WorkspaceContextAuthorityLevel.LockedBySafety, WorkspaceContextFreshnessStatus.FixtureCurrent, evidenceRefs.Take(2).ToList(), [], ["Runtime action dispatch is blocked."], selected: false, locked: true, excluded: false, proof)
    ];

    private static WorkspaceContextItem Item(
        string itemId,
        WorkspaceContextItemKind kind,
        WorkspaceContextItemStatus status,
        string title,
        string summary,
        WorkspaceContextSourceKind source,
        WorkspaceContextAuthorityLevel authority,
        WorkspaceContextFreshnessStatus freshness,
        IReadOnlyList<string> evidenceRefs,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> blockers,
        bool selected,
        bool locked,
        bool excluded,
        WorkspaceContextNoSideEffectProof proof) =>
        new(itemId, kind, status, title, Sanitize(summary), source, authority, freshness, evidenceRefs, Sanitize(warnings), Sanitize(blockers), selected, locked, excluded, proof);

    private static WorkspaceContextMemoryCandidate Candidate(
        string candidateId,
        WorkspaceContextItemKind kind,
        string title,
        string preview,
        WorkspaceContextAuthorityLevel authority,
        WorkspaceContextFreshnessStatus freshness,
        IReadOnlyList<string> evidenceRefs,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> blockers,
        bool selected,
        bool locked,
        bool excluded,
        WorkspaceContextNoSideEffectProof proof) =>
        new(candidateId, kind, WorkspaceContextItemStatus.Ready, title, Sanitize(preview), authority, freshness, evidenceRefs, Sanitize(warnings), Sanitize(blockers), DurableMemoryEnabled: false, selected, locked, excluded, proof);

    private static IReadOnlyList<string> Warnings() =>
    [
        "Workspace context is fixture-only and does not read the real workspace.",
        "Memory candidates are non-durable previews.",
        "Semantic/vector and provider/cloud capabilities remain disabled."
    ];

    private static IReadOnlyList<string> Blockers() =>
    [
        "Real workspace filesystem reads are blocked.",
        "Durable memory and persistence are blocked.",
        "Provider/cloud/network and LLM live calls are blocked.",
        "Semantic/vector backend is blocked.",
        "Runtime/live/browser/CDP/WCU/OCR remain blocked."
    ];

    private static IReadOnlyList<string> DocumentedDebt() =>
    [
        "Context authority/freshness guards.",
        "Memory candidate contradiction/risk hardening.",
        "Workspace context packet visible surface.",
        "Manual QA before any real workspace scan or durable memory."
    ];

    private static string Summary(
        IReadOnlyList<WorkspaceContextItem> items,
        IReadOnlyList<WorkspaceContextMemoryCandidate> candidates) =>
        string.Join(
            "\n",
            new[]
            {
                "# Workspace Context and Memory Read-Only Foundation",
                "Mode: READ_ONLY_FIXTURE_SAFE_NO_MEMORY_RUNTIME",
                $"Context items: {items.Count}.",
                $"Memory candidates: {candidates.Count}.",
                $"Selected context: {items.Count(item => item.Selected)}.",
                $"Locked context: {items.Count(item => item.Locked)}.",
                $"Excluded context: {items.Count(item => item.Excluded)}.",
                "No workspace filesystem read, durable memory, provider/cloud, semantic/vector backend or runtime is enabled."
            });

    private static IReadOnlyList<string> Sanitize(IReadOnlyList<string> lines) =>
        lines.Select(Sanitize).ToList();

    private static string Sanitize(string value) =>
        value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("raw payload", "excluded payload", StringComparison.OrdinalIgnoreCase)
            .Replace("secret", "sensitive value", StringComparison.OrdinalIgnoreCase);
}
