namespace OneBrain.Core.Approval;

public sealed record ApprovalPacketSurfaceSection(
    string SectionId,
    string Title,
    ApprovalHumanReviewItemStatus Status,
    ApprovalHumanReviewSeverity Severity,
    ApprovalHumanReviewSourceKind Source,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ContextRefs,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    bool HumanReviewRequired,
    int ProductActionsCount,
    int StateMutationsCount,
    int ExportActionsCount,
    ApprovalReviewNoSideEffectProof NoSideEffectProof);

public sealed record ApprovalPacketReadOnlySurface(
    string SurfaceId,
    string Title,
    string Mode,
    string SourceLabel,
    ApprovalHumanReviewPacketReadOnly Packet,
    IReadOnlyList<ApprovalPacketSurfaceSection> Sections,
    IReadOnlyList<string> CandidateActionPreviews,
    IReadOnlyList<string> RiskDecisionSummaries,
    IReadOnlyList<string> EvidenceContextLinkSummaries,
    IReadOnlyList<string> DecisionOptionPreviews,
    IReadOnlyList<string> HumanReviewRequirements,
    IReadOnlyList<string> DisabledNotices,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    string NextRecommendedBlock,
    string ReadOnlySummary,
    ApprovalReviewNoSideEffectProof NoSideEffectProof)
{
    public bool ReadOnly => NoSideEffectProof.ReadOnly;
    public bool Deterministic => NoSideEffectProof.Deterministic;
    public bool FixtureSafe => NoSideEffectProof.FixtureSafe;
    public int ProductActionsCount => Sections.Sum(section => section.ProductActionsCount) + Packet.ProductActionCount;
    public int StateMutationsCount => Sections.Sum(section => section.StateMutationsCount) + Packet.StateMutationCount;
    public int ExportActionsCount => Sections.Sum(section => section.ExportActionsCount);
    public bool HasApprovalExecution => Packet.HasApprovalExecution || NoSideEffectProof.ApprovalExecutionStarted;
    public bool HasApprovalStateMutation => Packet.HasApprovalStateMutation || NoSideEffectProof.ApprovalStateMutationAttempted;
    public bool HasProductActions => ProductActionsCount > 0 || NoSideEffectProof.ProductActionExposed;
    public bool HasExportActions => ExportActionsCount > 0;
    public bool HasDurableMemory => Packet.HasDurableMemory || NoSideEffectProof.DurableMemoryActive;
}

public static class ApprovalPacketReadOnlySurfacePresenter
{
    public static ApprovalPacketReadOnlySurface CreateFixture()
    {
        var packet = ApprovalHumanReviewReadOnlyPresenter.CreateFixture();
        var proof = ApprovalReviewNoSideEffectProof.FixtureReadOnly();
        var risk = ApprovalRiskDecisionReadOnlyGuard.EvaluateCatalog();
        var links = HumanReviewEvidenceContextLinkReadOnlyGuard.EvaluateCatalog();
        var sections = Sections(packet, risk, links, proof);
        var candidateActions = CandidateActionPreviews(packet);
        var riskDecision = RiskDecisionSummaries(risk);
        var linkSummaries = EvidenceContextLinkSummaries(links);
        var decisionOptions = DecisionOptionPreviews(packet);
        var humanReview = HumanReviewRequirements(packet, risk, links);
        var disabled = DisabledNotices();
        var warnings = Clean(sections.SelectMany(section => section.Warnings).Concat(packet.Warnings).Distinct().ToList());
        var blockers = Clean(sections.SelectMany(section => section.Blockers).Concat(packet.Blockers).Distinct().ToList());

        return new ApprovalPacketReadOnlySurface(
            SurfaceId: "phase-e.approval.packet.surface.read-only.fixture.v1",
            Title: "Approval Packet Read-Only Human Review Surface",
            Mode: "READ_ONLY_FIXTURE_SAFE_NO_APPROVAL_EXECUTION_NO_ACTIONS_NO_EXPORT",
            SourceLabel: "ApprovalHumanReviewReadOnlyPresenter.CreateFixture + ApprovalRiskDecisionReadOnlyGuard + HumanReviewEvidenceContextLinkReadOnlyGuard",
            Packet: packet,
            Sections: sections,
            CandidateActionPreviews: candidateActions,
            RiskDecisionSummaries: riskDecision,
            EvidenceContextLinkSummaries: linkSummaries,
            DecisionOptionPreviews: decisionOptions,
            HumanReviewRequirements: humanReview,
            DisabledNotices: disabled,
            Warnings: warnings,
            Blockers: blockers,
            NextRecommendedBlock: "PHASE_E_HUMAN_REVIEW_PACKET_EXPORT_PREVIEW_READ_ONLY",
            ReadOnlySummary: Summary(packet, sections, riskDecision, linkSummaries, decisionOptions),
            NoSideEffectProof: proof);
    }

    private static IReadOnlyList<ApprovalPacketSurfaceSection> Sections(
        ApprovalHumanReviewPacketReadOnly packet,
        IReadOnlyList<ApprovalRiskDecisionReadOnlyResult> risk,
        IReadOnlyList<HumanReviewEvidenceContextLinkResult> links,
        ApprovalReviewNoSideEffectProof proof) =>
    [
        Section("approval.packet.executive-summary", "Approval packet review summary", ApprovalHumanReviewItemStatus.Ready, ApprovalHumanReviewSeverity.Info, ApprovalHumanReviewSourceKind.Fixture, EvidenceRefs(packet), ContextRefs(packet), packet.Warnings, [], false, proof),
        Section("human.review.packet.identity", "Human review packet fixture identity", ApprovalHumanReviewItemStatus.Ready, ApprovalHumanReviewSeverity.Info, ApprovalHumanReviewSourceKind.Fixture, [], [], [], [], false, proof),
        Section("candidate.action.previews", "Candidate action preview labels", ApprovalHumanReviewItemStatus.PreviewOnly, ApprovalHumanReviewSeverity.Warning, ApprovalHumanReviewSourceKind.Fixture, EvidenceRefs(packet), ContextRefs(packet), packet.CandidateActions.SelectMany(action => action.Warnings).ToList(), packet.CandidateActions.SelectMany(action => action.Blockers).ToList(), true, proof),
        Section("candidate.action.risk.summary", "Candidate action risk preview", ApprovalHumanReviewItemStatus.Warning, ApprovalHumanReviewSeverity.Warning, ApprovalHumanReviewSourceKind.Fixture, EvidenceRefs(packet), ContextRefs(packet), packet.RiskSummaries.SelectMany(item => item.Warnings).ToList(), packet.RiskSummaries.SelectMany(item => item.Blockers).ToList(), true, proof),
        Section("risk.decision.guard.summary", "Risk and decision guard preview", ApprovalHumanReviewItemStatus.Warning, ApprovalHumanReviewSeverity.Warning, ApprovalHumanReviewSourceKind.Fixture, RiskRefs(risk), [], risk.SelectMany(result => result.Warnings).ToList(), risk.SelectMany(result => result.Blockers).ToList(), true, proof),
        Section("evidence.context.link.guard.summary", "Evidence and context link guard preview", ApprovalHumanReviewItemStatus.Warning, ApprovalHumanReviewSeverity.Warning, ApprovalHumanReviewSourceKind.Fixture, LinkRefs(links), [], links.SelectMany(result => result.Warnings).ToList(), links.SelectMany(result => result.Blockers).ToList(), true, proof),
        Section("evidence.links", "Evidence links audit preview", ApprovalHumanReviewItemStatus.PreviewOnly, ApprovalHumanReviewSeverity.Warning, ApprovalHumanReviewSourceKind.PhaseCEvidence, EvidenceRefs(packet), [], packet.EvidenceLinks.SelectMany(link => link.Warnings).ToList(), packet.EvidenceLinks.SelectMany(link => link.Blockers).ToList(), true, proof),
        Section("context.links", "Context links audit preview", ApprovalHumanReviewItemStatus.PreviewOnly, ApprovalHumanReviewSeverity.Warning, ApprovalHumanReviewSourceKind.PhaseDContext, [], ContextRefs(packet), packet.ContextLinks.SelectMany(link => link.Warnings).ToList(), packet.ContextLinks.SelectMany(link => link.Blockers).ToList(), true, proof),
        Section("missing.evidence.blockers", "Missing evidence blockers", ApprovalHumanReviewItemStatus.Blocked, ApprovalHumanReviewSeverity.Blocked, ApprovalHumanReviewSourceKind.PhaseCEvidence, [], ContextRefs(packet).Take(2).ToList(), [], BlockersFor(packet, ApprovalHumanReviewItemKind.MissingEvidenceBlocker).Concat(risk.Where(result => result.HasIssue(ApprovalRiskDecisionReadOnlyIssueKind.MissingEvidence)).SelectMany(result => result.Blockers)).Concat(links.Where(result => result.HasIssue(HumanReviewEvidenceContextLinkIssueKind.MissingEvidenceLink)).SelectMany(result => result.Blockers)).ToList(), true, proof),
        Section("missing.stale.excluded.context.blockers", "Missing/stale/excluded context blockers", ApprovalHumanReviewItemStatus.Blocked, ApprovalHumanReviewSeverity.Blocked, ApprovalHumanReviewSourceKind.PhaseDContext, EvidenceRefs(packet).Take(2).ToList(), ContextRefs(packet), [], ContextBlockers(packet, risk, links), true, proof),
        Section("unresolved.contradiction.blockers", "Unresolved contradiction blockers", ApprovalHumanReviewItemStatus.Blocked, ApprovalHumanReviewSeverity.Blocked, ApprovalHumanReviewSourceKind.PhaseCEvidence, EvidenceRefs(packet), ContextRefs(packet).Take(2).ToList(), [], BlockersFor(packet, ApprovalHumanReviewItemKind.UnresolvedContradictionBlocker).Concat(risk.Where(result => result.HasIssue(ApprovalRiskDecisionReadOnlyIssueKind.UnresolvedContradiction)).SelectMany(result => result.Blockers)).Concat(links.Where(result => result.HasIssue(HumanReviewEvidenceContextLinkIssueKind.UnresolvedContradictionLink)).SelectMany(result => result.Blockers)).ToList(), true, proof),
        Section("critical.risk.blockers", "Critical risk blockers", ApprovalHumanReviewItemStatus.Blocked, ApprovalHumanReviewSeverity.Blocked, ApprovalHumanReviewSourceKind.Fixture, EvidenceRefs(packet), ContextRefs(packet), [], BlockersFor(packet, ApprovalHumanReviewItemKind.CriticalRiskBlocker).Concat(risk.Where(result => result.RiskLevel == ApprovalRiskLevel.Critical).SelectMany(result => result.Blockers)).Concat(links.Where(result => result.HasIssue(HumanReviewEvidenceContextLinkIssueKind.CriticalRiskLink)).SelectMany(result => result.Blockers)).ToList(), true, proof),
        Section("decision.options.preview", "Decision option preview labels", ApprovalHumanReviewItemStatus.PreviewOnly, ApprovalHumanReviewSeverity.Info, ApprovalHumanReviewSourceKind.Fixture, EvidenceRefs(packet).Take(2).ToList(), ContextRefs(packet).Take(2).ToList(), packet.DecisionOptions.SelectMany(option => option.Warnings).Concat(["Decision labels are preview-only and copy-safe for audit review."]).ToList(), packet.DecisionOptions.SelectMany(option => option.Blockers).ToList(), false, proof),
        DecisionSection("approve.preview.label", "Approve preview label", ApprovalDecisionOptionKind.ApprovePreviewOnly, packet, risk, proof),
        DecisionSection("reject.preview.label", "Reject preview label", ApprovalDecisionOptionKind.RejectPreviewOnly, packet, risk, proof),
        DecisionSection("request.evidence.preview.label", "Request evidence preview label", ApprovalDecisionOptionKind.RequestMoreEvidence, packet, risk, proof),
        DecisionSection("request.context.refresh.preview.label", "Request context refresh preview label", ApprovalDecisionOptionKind.RequestContextRefresh, packet, risk, proof),
        DecisionSection("defer.decision.preview.label", "Defer decision preview label", ApprovalDecisionOptionKind.Defer, packet, risk, proof),
        Section("human.review.requirements", "Human review requirements and blockers", ApprovalHumanReviewItemStatus.Warning, ApprovalHumanReviewSeverity.Warning, ApprovalHumanReviewSourceKind.DocumentedDebt, EvidenceRefs(packet), ContextRefs(packet), HumanReviewRequirements(packet, risk, links), [], true, proof),
        DisabledSection("runtime.live.disabled", "Runtime/live disabled notice", ["Runtime/live/browser/CDP/WCU/OCR remain disabled."], proof),
        DisabledSection("filesystem.db.disabled", "Filesystem/DB disabled notice", ["Filesystem product IO and DB remain disabled."], proof),
        DisabledSection("provider.cloud.disabled", "Provider/cloud disabled notice", ["Provider/cloud/network remains disabled."], proof),
        DisabledSection("semantic.vector.disabled", "Semantic/vector disabled notice", ["Semantic/vector backend remains disabled."], proof),
        DisabledSection("llm.live.disabled", "LLM live disabled notice", ["LLM live/provider calls remain disabled."], proof),
        DisabledSection("durable.memory.disabled", "Durable memory disabled notice", ["Durable memory remains disabled."], proof),
        DisabledSection("approval.execution.disabled", "Approval execution disabled notice", ["Approval execution remains disabled."], proof),
        DisabledSection("approval.state.mutation.disabled", "Approval state mutation disabled notice", ["Approval state mutation remains disabled."], proof),
        Section("no.side.effect.proof", "No-side-effect proof summary", ApprovalHumanReviewItemStatus.Ready, ApprovalHumanReviewSeverity.Info, ApprovalHumanReviewSourceKind.NoSideEffectProof, [], [], ["Proof is declarative, deterministic and fixture-safe."], [], false, proof),
        Section("documented.debt", "Documented debt", ApprovalHumanReviewItemStatus.Deferred, ApprovalHumanReviewSeverity.Deferred, ApprovalHumanReviewSourceKind.DocumentedDebt, [], [], packet.DocumentedDebt, [], true, proof),
        Section("next.recommended.block", "Next recommended block", ApprovalHumanReviewItemStatus.Deferred, ApprovalHumanReviewSeverity.Deferred, ApprovalHumanReviewSourceKind.DocumentedDebt, [], [], ["PHASE_E_HUMAN_REVIEW_PACKET_EXPORT_PREVIEW_READ_ONLY"], [], false, proof)
    ];

    private static ApprovalPacketSurfaceSection Section(
        string id,
        string title,
        ApprovalHumanReviewItemStatus status,
        ApprovalHumanReviewSeverity severity,
        ApprovalHumanReviewSourceKind source,
        IReadOnlyList<string> evidenceRefs,
        IReadOnlyList<string> contextRefs,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> blockers,
        bool humanReviewRequired,
        ApprovalReviewNoSideEffectProof proof) =>
        new(id, title, status, severity, source, Clean(evidenceRefs), Clean(contextRefs), Clean(warnings), Clean(blockers), humanReviewRequired, ProductActionsCount: 0, StateMutationsCount: 0, ExportActionsCount: 0, proof);

    private static ApprovalPacketSurfaceSection DisabledSection(
        string id,
        string title,
        IReadOnlyList<string> blockers,
        ApprovalReviewNoSideEffectProof proof) =>
        Section(id, title, ApprovalHumanReviewItemStatus.Disabled, ApprovalHumanReviewSeverity.Disabled, ApprovalHumanReviewSourceKind.CapabilityNotice, [], [], [], blockers, false, proof);

    private static ApprovalPacketSurfaceSection DecisionSection(
        string id,
        string title,
        ApprovalDecisionOptionKind option,
        ApprovalHumanReviewPacketReadOnly packet,
        IReadOnlyList<ApprovalRiskDecisionReadOnlyResult> risk,
        ApprovalReviewNoSideEffectProof proof)
    {
        var decision = packet.DecisionOptions.First(item => item.OptionKind == option);
        var relatedRisk = risk.Where(result => result.DecisionOptionKind == option).ToList();
        var blockers = decision.Blockers.Concat(relatedRisk.SelectMany(result => result.Blockers)).Distinct().ToList();
        var warnings = decision.Warnings.Concat(relatedRisk.SelectMany(result => result.Warnings)).Concat(["Decision option is a preview label, not a command.", "Label is safe for copy review and cannot change state."]).Distinct().ToList();

        return Section(id, title, ApprovalHumanReviewItemStatus.PreviewOnly, blockers.Count > 0 ? ApprovalHumanReviewSeverity.Blocked : ApprovalHumanReviewSeverity.Info, ApprovalHumanReviewSourceKind.Fixture, EvidenceRefs(packet).Take(2).ToList(), ContextRefs(packet).Take(2).ToList(), warnings, blockers, relatedRisk.Any(result => result.RequiresHumanReview), proof);
    }

    private static IReadOnlyList<string> CandidateActionPreviews(ApprovalHumanReviewPacketReadOnly packet) =>
        packet.CandidateActions
            .Select(action => $"{action.CandidateActionId}: {action.Title}; risk={action.RiskLevel}; preview-only label; productActions={action.ProductActionCount}; stateMutations={action.StateMutationCount}; safeNextStep=human review only.")
            .ToList();

    private static IReadOnlyList<string> RiskDecisionSummaries(IReadOnlyList<ApprovalRiskDecisionReadOnlyResult> risk) =>
    [
        $"Risk/decision fixtures: {risk.Count}; blocked/excluded: {risk.Count(result => result.Blocked)}.",
        $"Approve preview blockers: {risk.Count(result => result.DecisionOptionKind == ApprovalDecisionOptionKind.ApprovePreviewOnly && result.Blocked)}.",
        $"Preview-only labels allowed: {risk.Count(result => result.AllowsDecisionOptionPreview)}.",
        "Risk is not approval; decision option is a label-only preview, not execution."
    ];

    private static IReadOnlyList<string> EvidenceContextLinkSummaries(IReadOnlyList<HumanReviewEvidenceContextLinkResult> links) =>
    [
        $"Evidence/context link fixtures: {links.Count}; blocked/excluded: {links.Count(result => result.Blocked)}.",
        $"Human-review link warnings: {links.Count(result => result.Warnings.Count > 0)}.",
        $"Invalid safe-next-step links blocked: {links.Count(result => result.HasIssue(HumanReviewEvidenceContextLinkIssueKind.InvalidSafeNextStepLink))}.",
        "Evidence link is not durable evidence; context link is not trusted context; both remain review references only."
    ];

    private static IReadOnlyList<string> DecisionOptionPreviews(ApprovalHumanReviewPacketReadOnly packet) =>
        packet.DecisionOptions
            .Select(option => $"{option.OptionKind}: {option.Label}; labelOnly={option.PreviewOnly}; executesAction={option.ExecutesAction}; mutatesState={option.MutatesState}; safeForCopyReview=True.")
            .ToList();

    private static IReadOnlyList<string> HumanReviewRequirements(
        ApprovalHumanReviewPacketReadOnly packet,
        IReadOnlyList<ApprovalRiskDecisionReadOnlyResult> risk,
        IReadOnlyList<HumanReviewEvidenceContextLinkResult> links) =>
        packet.Items.Where(item => item.HumanReviewRequired).Select(item => $"Packet review: {item.ItemId}.")
            .Concat(packet.RiskSummaries.Where(item => item.HumanReviewRequired).Select(item => $"Risk review: {item.RiskId}."))
            .Concat(risk.Where(result => result.RequiresHumanReview).Select(result => $"Risk/decision guard review: {result.FixtureId}."))
            .Concat(links.Where(result => result.Issues.Any(issue => issue.RequiresHumanReview)).Select(result => $"Evidence/context link review: {result.FixtureId}."))
            .Distinct()
            .Take(24)
            .ToList();

    private static IReadOnlyList<string> DisabledNotices() =>
    [
        "Runtime/live disabled; visible surface is fixture-only.",
        "Filesystem product IO and DB disabled; no product persistence is attached.",
        "Provider/cloud disabled; no external provider call is available.",
        "Semantic/vector disabled; no semantic backend is attached.",
        "LLM live disabled; no live model call is available.",
        "Durable memory disabled; no memory write is available.",
        "Approval execution disabled; decision labels stay non-command previews.",
        "Approval state mutation disabled; packet review cannot change approval state.",
        "Export actions disabled; preview text stays in memory."
    ];

    private static IReadOnlyList<string> ContextBlockers(
        ApprovalHumanReviewPacketReadOnly packet,
        IReadOnlyList<ApprovalRiskDecisionReadOnlyResult> risk,
        IReadOnlyList<HumanReviewEvidenceContextLinkResult> links) =>
        BlockersFor(packet, ApprovalHumanReviewItemKind.MissingContextBlocker)
            .Concat(BlockersFor(packet, ApprovalHumanReviewItemKind.StaleContextBlocker))
            .Concat(risk.Where(result => result.HasIssue(ApprovalRiskDecisionReadOnlyIssueKind.MissingContext) || result.HasIssue(ApprovalRiskDecisionReadOnlyIssueKind.StaleContext) || result.HasIssue(ApprovalRiskDecisionReadOnlyIssueKind.ExcludedContext)).SelectMany(result => result.Blockers))
            .Concat(links.Where(result => result.HasIssue(HumanReviewEvidenceContextLinkIssueKind.MissingContextLink) || result.HasIssue(HumanReviewEvidenceContextLinkIssueKind.StaleContextLink) || result.HasIssue(HumanReviewEvidenceContextLinkIssueKind.ExcludedContextLink) || result.HasIssue(HumanReviewEvidenceContextLinkIssueKind.UnknownContextAuthority) || result.HasIssue(HumanReviewEvidenceContextLinkIssueKind.MissingContextFreshness)).SelectMany(result => result.Blockers))
            .Distinct()
            .ToList();

    private static IReadOnlyList<string> BlockersFor(ApprovalHumanReviewPacketReadOnly packet, ApprovalHumanReviewItemKind kind) =>
        packet.Items.Where(item => item.Kind == kind).SelectMany(item => item.Blockers).Distinct().ToList();

    private static IReadOnlyList<string> EvidenceRefs(ApprovalHumanReviewPacketReadOnly packet) =>
        packet.EvidenceLinks.Select(link => link.EvidenceId)
            .Concat(packet.Items.SelectMany(item => item.EvidenceRefs))
            .Distinct()
            .ToList();

    private static IReadOnlyList<string> ContextRefs(ApprovalHumanReviewPacketReadOnly packet) =>
        packet.ContextLinks.Select(link => link.ContextId)
            .Concat(packet.Items.SelectMany(item => item.ContextRefs))
            .Distinct()
            .ToList();

    private static IReadOnlyList<string> RiskRefs(IReadOnlyList<ApprovalRiskDecisionReadOnlyResult> risk) =>
        risk.Select(result => result.FixtureId).Take(10).ToList();

    private static IReadOnlyList<string> LinkRefs(IReadOnlyList<HumanReviewEvidenceContextLinkResult> links) =>
        links.Select(result => result.FixtureId).Take(10).ToList();

    private static string Summary(
        ApprovalHumanReviewPacketReadOnly packet,
        IReadOnlyList<ApprovalPacketSurfaceSection> sections,
        IReadOnlyList<string> riskDecision,
        IReadOnlyList<string> linkSummaries,
        IReadOnlyList<string> decisionOptions) =>
        string.Join(
            "\n",
            new[]
            {
                "# Approval Packet Read-Only Human Review Surface",
                "Mode: READ_ONLY_FIXTURE_SAFE_NO_APPROVAL_EXECUTION_NO_ACTIONS_NO_EXPORT",
                "Visible polish: grouped read-only sections, disabled capability notices, blocker copy and label-only decision previews.",
                $"Packet: {packet.PacketId}.",
                $"Sections: {sections.Count}.",
                $"Product actions: {sections.Sum(section => section.ProductActionsCount) + packet.ProductActionCount}.",
                $"State mutations: {sections.Sum(section => section.StateMutationsCount) + packet.StateMutationCount}.",
                $"Export actions: {sections.Sum(section => section.ExportActionsCount)}.",
                string.Join(" ", riskDecision),
                string.Join(" ", linkSummaries),
                string.Join(" ", decisionOptions),
                "All execution, mutation, product control, filesystem product IO, DB, provider/cloud, semantic/vector, LLM live, durable memory, export and runtime capabilities remain unavailable."
            });

    private static IReadOnlyList<string> Clean(IReadOnlyList<string> values) =>
        values.Select(Clean).Where(value => value.Length > 0).Distinct().ToList();

    private static string Clean(string value) =>
        value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("raw payload", "excluded payload", StringComparison.OrdinalIgnoreCase)
            .Replace("secret", "sensitive value", StringComparison.OrdinalIgnoreCase);
}
