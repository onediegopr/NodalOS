namespace OneBrain.Core.Approval;

public enum HumanReviewPacketExportPreviewKind
{
    MarkdownLikeText,
    JsonLikeText
}

public enum HumanReviewPacketExportPreviewStatus
{
    Included,
    Warning,
    Blocked,
    Disabled,
    Deferred
}

public sealed record HumanReviewPacketExportManifest(
    string PreviewId,
    HumanReviewPacketExportPreviewKind FormatPreviewKind,
    string SourceFixture,
    bool PhysicalFileCreated,
    bool ClipboardUsed,
    bool DownloadStarted,
    int ProductActionsCount,
    int StateMutationCount,
    int ExportActionsCount,
    bool ApprovalExecutionOccurred,
    bool ApprovalStateMutationOccurred,
    bool ContainsRawPayload,
    bool ContainsSecretLikeContent,
    bool ContainsDurableMemory,
    ApprovalReviewNoSideEffectProof NoSideEffectProof);

public sealed record HumanReviewPacketExportSection(
    string SectionId,
    string Title,
    HumanReviewPacketExportPreviewStatus Status,
    ApprovalHumanReviewSeverity Severity,
    ApprovalHumanReviewSourceKind Source,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ContextRefs,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    bool IncludedInPreview,
    bool PhysicalExportOccurred,
    ApprovalReviewNoSideEffectProof NoSideEffectProof);

public sealed record HumanReviewPacketExportReadOnlyPreview(
    string PreviewId,
    string Title,
    string Mode,
    HumanReviewPacketExportManifest Manifest,
    ApprovalPacketReadOnlySurface SourceSurface,
    IReadOnlyList<HumanReviewPacketExportSection> Sections,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    IReadOnlyList<string> Exclusions,
    IReadOnlyList<string> DisabledNotices,
    string PreviewText,
    string NextSafeStep,
    ApprovalReviewNoSideEffectProof NoSideEffectProof)
{
    public bool ReadOnly => NoSideEffectProof.ReadOnly;
    public bool Deterministic => NoSideEffectProof.Deterministic;
    public bool FixtureSafe => NoSideEffectProof.FixtureSafe;
    public bool HasRealExport => Manifest.PhysicalFileCreated || Manifest.ClipboardUsed || Manifest.DownloadStarted || Sections.Any(section => section.PhysicalExportOccurred);
    public bool HasApprovalExecution => Manifest.ApprovalExecutionOccurred || SourceSurface.HasApprovalExecution || NoSideEffectProof.ApprovalExecutionStarted;
    public bool HasApprovalStateMutation => Manifest.ApprovalStateMutationOccurred || SourceSurface.HasApprovalStateMutation || NoSideEffectProof.ApprovalStateMutationAttempted;
    public bool HasProductActions => Manifest.ProductActionsCount > 0 || SourceSurface.HasProductActions || NoSideEffectProof.ProductActionExposed;
    public bool HasStateMutations => Manifest.StateMutationCount > 0 || SourceSurface.StateMutationsCount > 0;
    public bool HasExportActions => Manifest.ExportActionsCount > 0 || SourceSurface.HasExportActions;
    public bool HasDurableMemory => Manifest.ContainsDurableMemory || SourceSurface.HasDurableMemory || NoSideEffectProof.DurableMemoryActive;
}

public static class HumanReviewPacketExportReadOnlyPresenter
{
    public static HumanReviewPacketExportReadOnlyPreview CreateFixture()
    {
        var surface = ApprovalPacketReadOnlySurfacePresenter.CreateFixture();
        var proof = ApprovalReviewNoSideEffectProof.FixtureReadOnly();
        var manifest = new HumanReviewPacketExportManifest(
            PreviewId: "phase-e.human-review.packet.export-preview.read-only.fixture.v1",
            FormatPreviewKind: HumanReviewPacketExportPreviewKind.MarkdownLikeText,
            SourceFixture: "ApprovalPacketReadOnlySurfacePresenter.CreateFixture",
            PhysicalFileCreated: false,
            ClipboardUsed: false,
            DownloadStarted: false,
            ProductActionsCount: 0,
            StateMutationCount: 0,
            ExportActionsCount: 0,
            ApprovalExecutionOccurred: false,
            ApprovalStateMutationOccurred: false,
            ContainsRawPayload: false,
            ContainsSecretLikeContent: false,
            ContainsDurableMemory: false,
            NoSideEffectProof: proof);
        var sections = Sections(surface, proof);
        var warnings = Clean(sections.SelectMany(section => section.Warnings).Concat(surface.Warnings).Distinct().ToList());
        var blockers = Clean(sections.SelectMany(section => section.Blockers).Concat(surface.Blockers).Distinct().ToList());
        var exclusions = Exclusions();
        var disabled = Clean(
            surface.DisabledNotices
                .Concat(
                [
                    "Physical export disabled.",
                    "Clipboard disabled.",
                    "Browser download disabled.",
                    "Approval execution disabled.",
                    "Approval state mutation disabled."
                ])
                .Distinct()
                .ToList());
        var previewText = PreviewText(surface, manifest, sections, warnings, blockers, exclusions, disabled);

        return new HumanReviewPacketExportReadOnlyPreview(
            PreviewId: manifest.PreviewId,
            Title: "Human Review Packet Export Preview Read-Only",
            Mode: "READ_ONLY_IN_MEMORY_EXPORT_PREVIEW_NO_FILE_NO_CLIPBOARD_NO_DOWNLOAD_NO_APPROVAL_EXECUTION",
            Manifest: manifest,
            SourceSurface: surface,
            Sections: sections,
            Warnings: warnings,
            Blockers: blockers,
            Exclusions: exclusions,
            DisabledNotices: disabled,
            PreviewText: previewText,
            NextSafeStep: "PHASE_E_APPROVAL_CLOSEOUT_AUDIT_PREP",
            NoSideEffectProof: proof);
    }

    private static IReadOnlyList<HumanReviewPacketExportSection> Sections(
        ApprovalPacketReadOnlySurface surface,
        ApprovalReviewNoSideEffectProof proof) =>
    [
        Section("export.manifest", "Export manifest", HumanReviewPacketExportPreviewStatus.Included, ApprovalHumanReviewSeverity.Info, ApprovalHumanReviewSourceKind.NoSideEffectProof, [], [], ["Preview is in-memory only."], ["Physical export, clipboard and browser download remain disabled."], proof),
        Section("executive.summary", "Executive summary", HumanReviewPacketExportPreviewStatus.Included, ApprovalHumanReviewSeverity.Info, ApprovalHumanReviewSourceKind.Fixture, [], [], surface.Warnings.Take(4).ToList(), surface.Blockers.Take(4).ToList(), proof),
        FromSurface(surface, "human.review.packet.identity", "Human review packet identity", HumanReviewPacketExportPreviewStatus.Included, proof),
        FromSurface(surface, "approval.packet.executive-summary", "Approval packet summary", HumanReviewPacketExportPreviewStatus.Included, proof, outputSectionId: "approval.packet.summary"),
        FromSurface(surface, "candidate.action.previews", "Candidate action previews", HumanReviewPacketExportPreviewStatus.Warning, proof),
        FromSurface(surface, "candidate.action.risk.summary", "Candidate action risk summary", HumanReviewPacketExportPreviewStatus.Warning, proof),
        FromSurface(surface, "risk.decision.guard.summary", "Risk/decision guard summary", HumanReviewPacketExportPreviewStatus.Warning, proof),
        FromSurface(surface, "evidence.context.link.guard.summary", "Evidence/context link guard summary", HumanReviewPacketExportPreviewStatus.Warning, proof),
        FromSurface(surface, "evidence.links", "Evidence links", HumanReviewPacketExportPreviewStatus.Warning, proof),
        FromSurface(surface, "context.links", "Context links", HumanReviewPacketExportPreviewStatus.Warning, proof),
        FromSurface(surface, "missing.evidence.blockers", "Missing evidence blockers", HumanReviewPacketExportPreviewStatus.Blocked, proof),
        FromSurface(surface, "missing.stale.excluded.context.blockers", "Missing/stale/excluded context blockers", HumanReviewPacketExportPreviewStatus.Blocked, proof),
        FromSurface(surface, "unresolved.contradiction.blockers", "Unresolved contradiction blockers", HumanReviewPacketExportPreviewStatus.Blocked, proof),
        FromSurface(surface, "critical.risk.blockers", "Critical risk blockers", HumanReviewPacketExportPreviewStatus.Blocked, proof),
        FromSurface(surface, "decision.options.preview", "Decision options preview", HumanReviewPacketExportPreviewStatus.Included, proof),
        FromSurface(surface, "approve.preview.label", "Approve preview label", HumanReviewPacketExportPreviewStatus.Blocked, proof),
        FromSurface(surface, "reject.preview.label", "Reject preview label", HumanReviewPacketExportPreviewStatus.Included, proof),
        FromSurface(surface, "request.evidence.preview.label", "Request evidence preview label", HumanReviewPacketExportPreviewStatus.Included, proof),
        FromSurface(surface, "request.context.refresh.preview.label", "Request context refresh preview label", HumanReviewPacketExportPreviewStatus.Included, proof),
        FromSurface(surface, "defer.decision.preview.label", "Defer decision preview label", HumanReviewPacketExportPreviewStatus.Included, proof),
        FromSurface(surface, "human.review.requirements", "Human review requirements", HumanReviewPacketExportPreviewStatus.Warning, proof),
        FromSurface(surface, "runtime.live.disabled", "Runtime/live disabled notice", HumanReviewPacketExportPreviewStatus.Disabled, proof),
        FromSurface(surface, "filesystem.db.disabled", "Filesystem/DB disabled notice", HumanReviewPacketExportPreviewStatus.Disabled, proof),
        FromSurface(surface, "provider.cloud.disabled", "Provider/cloud disabled notice", HumanReviewPacketExportPreviewStatus.Disabled, proof),
        FromSurface(surface, "semantic.vector.disabled", "Semantic/vector disabled notice", HumanReviewPacketExportPreviewStatus.Disabled, proof),
        FromSurface(surface, "llm.live.disabled", "LLM live disabled notice", HumanReviewPacketExportPreviewStatus.Disabled, proof),
        FromSurface(surface, "durable.memory.disabled", "Durable memory disabled notice", HumanReviewPacketExportPreviewStatus.Disabled, proof),
        FromSurface(surface, "approval.execution.disabled", "Approval execution disabled notice", HumanReviewPacketExportPreviewStatus.Disabled, proof),
        FromSurface(surface, "approval.state.mutation.disabled", "Approval state mutation disabled notice", HumanReviewPacketExportPreviewStatus.Disabled, proof),
        FromSurface(surface, "no.side.effect.proof", "No-side-effect proof", HumanReviewPacketExportPreviewStatus.Included, proof),
        FromSurface(surface, "documented.debt", "Documented debt", HumanReviewPacketExportPreviewStatus.Deferred, proof),
        Section("next.recommended.block", "Next recommended block", HumanReviewPacketExportPreviewStatus.Deferred, ApprovalHumanReviewSeverity.Deferred, ApprovalHumanReviewSourceKind.DocumentedDebt, [], [], [surface.NextRecommendedBlock, "PHASE_E_APPROVAL_CLOSEOUT_AUDIT_PREP"], [], proof)
    ];

    private static HumanReviewPacketExportSection FromSurface(
        ApprovalPacketReadOnlySurface surface,
        string sourceSectionId,
        string title,
        HumanReviewPacketExportPreviewStatus status,
        ApprovalReviewNoSideEffectProof proof,
        string? outputSectionId = null)
    {
        var source = surface.Sections.Single(section => section.SectionId == sourceSectionId);

        return Section(outputSectionId ?? sourceSectionId, title, status, source.Severity, source.Source, source.EvidenceRefs, source.ContextRefs, source.Warnings, source.Blockers, proof);
    }

    private static HumanReviewPacketExportSection Section(
        string id,
        string title,
        HumanReviewPacketExportPreviewStatus status,
        ApprovalHumanReviewSeverity severity,
        ApprovalHumanReviewSourceKind source,
        IReadOnlyList<string> evidenceRefs,
        IReadOnlyList<string> contextRefs,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> blockers,
        ApprovalReviewNoSideEffectProof proof) =>
        new(id, title, status, severity, source, Clean(evidenceRefs), Clean(contextRefs), Clean(warnings), Clean(blockers), IncludedInPreview: true, PhysicalExportOccurred: false, proof);

    private static IReadOnlyList<string> Exclusions() =>
    [
        "Excluded payload values are omitted from preview content.",
        "Sensitive-value-like content is omitted from preview content.",
        "Durable memory records are omitted because durable memory capability is disabled.",
        "Approval execution output is omitted because approval execution capability is disabled.",
        "Physical export artifacts are omitted because this is an in-memory preview."
    ];

    private static string PreviewText(
        ApprovalPacketReadOnlySurface surface,
        HumanReviewPacketExportManifest manifest,
        IReadOnlyList<HumanReviewPacketExportSection> sections,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> blockers,
        IReadOnlyList<string> exclusions,
        IReadOnlyList<string> disabledNotices) =>
        Clean(
            string.Join(
                "\n",
                new[]
                {
                    "# Human Review Packet Export Preview Read-Only",
                    $"PreviewId: {manifest.PreviewId}",
                    $"FormatPreviewKind: {manifest.FormatPreviewKind}",
                    $"SourceFixture: {manifest.SourceFixture}",
                    $"PhysicalFileCreated: {manifest.PhysicalFileCreated}",
                    $"ClipboardUsed: {manifest.ClipboardUsed}",
                    $"DownloadStarted: {manifest.DownloadStarted}",
                    $"ProductActionsCount: {manifest.ProductActionsCount}",
                    $"StateMutationCount: {manifest.StateMutationCount}",
                    $"ExportActionsCount: {manifest.ExportActionsCount}",
                    $"ApprovalExecutionOccurred: {manifest.ApprovalExecutionOccurred}",
                    $"ApprovalStateMutationOccurred: {manifest.ApprovalStateMutationOccurred}",
                    $"ContainsRawPayload: {manifest.ContainsRawPayload}",
                    $"ContainsSecretLikeContent: {manifest.ContainsSecretLikeContent}",
                    $"ContainsDurableMemory: {manifest.ContainsDurableMemory}",
                    $"Surface: {surface.SurfaceId}",
                    $"Packet: {surface.Packet.PacketId}",
                    $"Sections: {sections.Count}",
                    $"CandidateActionPreviews: {surface.CandidateActionPreviews.Count}",
                    $"RiskDecisionSummary: {string.Join(" | ", surface.RiskDecisionSummaries)}",
                    $"EvidenceContextLinkSummary: {string.Join(" | ", surface.EvidenceContextLinkSummaries)}",
                    $"DecisionOptionPreviews: {string.Join(" | ", surface.DecisionOptionPreviews)}",
                    $"HumanReviewRequirements: {surface.HumanReviewRequirements.Count}",
                    $"DisabledNotices: {string.Join(" | ", disabledNotices)}",
                    $"Warnings: {warnings.Count}",
                    $"Blockers: {blockers.Count}",
                    $"Exclusions: {string.Join(" | ", exclusions)}",
                    "Decision labels are preview-only and expose zero product actions.",
                    "Human review packet export preview is not physical export.",
                    "Human review packet export preview is not approval execution.",
                    "Human review packet export preview is not approval state mutation.",
                    "No filesystem product IO, DB, provider/cloud, semantic/vector, LLM live, durable memory, runtime/live, service registration or approval execution is enabled."
                }));

    private static IReadOnlyList<string> Clean(IReadOnlyList<string> values) =>
        values.Select(Clean).Where(value => value.Length > 0).Distinct().ToList();

    private static string Clean(string value) =>
        value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("raw payload", "excluded payload", StringComparison.OrdinalIgnoreCase)
            .Replace("secret", "sensitive value", StringComparison.OrdinalIgnoreCase);
}
