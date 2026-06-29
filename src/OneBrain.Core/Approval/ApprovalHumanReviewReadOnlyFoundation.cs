using OneBrain.Core.Context;
using OneBrain.Core.Evidence;

namespace OneBrain.Core.Approval;

public enum ApprovalHumanReviewItemKind
{
    ApprovalPacketIdentity,
    HumanReviewSummary,
    CandidateActionPreview,
    CandidateActionKind,
    RiskLevel,
    RiskRationale,
    EvidenceLink,
    ContextLink,
    AuthorityFreshnessSummary,
    SelectionLockExclusionSummary,
    MemoryCandidateRiskContradictionSummary,
    RequiredHumanDecision,
    DecisionOptionPreview,
    ApprovalBlocker,
    ApprovalWarning,
    MissingEvidenceBlocker,
    MissingContextBlocker,
    StaleContextBlocker,
    UnresolvedContradictionBlocker,
    CriticalRiskBlocker,
    RuntimeLiveDisabledNotice,
    FilesystemDatabaseDisabledNotice,
    ProviderCloudDisabledNotice,
    DurableMemoryDisabledNotice,
    SafeNextStep,
    NoSideEffectProof,
    DeferredCapability
}

public enum ApprovalHumanReviewItemStatus
{
    Ready,
    PreviewOnly,
    Warning,
    Blocked,
    Disabled,
    Deferred
}

public enum ApprovalHumanReviewSeverity
{
    Info,
    Warning,
    Blocked,
    Disabled,
    Deferred
}

public enum ApprovalHumanReviewSourceKind
{
    Fixture,
    PhaseCEvidence,
    PhaseDContext,
    CapabilityNotice,
    NoSideEffectProof,
    DocumentedDebt
}

public enum ApprovalCandidateActionKind
{
    ReviewOnly,
    ProposedRecipeAction,
    ProposedContextDecision,
    ProposedEvidenceDecision,
    SafeNextStepReview
}

public enum ApprovalRiskLevel
{
    Low,
    Medium,
    Critical,
    Missing
}

public enum ApprovalDecisionOptionKind
{
    ApprovePreviewOnly,
    RejectPreviewOnly,
    RequestMoreEvidence,
    RequestContextRefresh,
    Defer
}

public sealed record ApprovalReviewNoSideEffectProof(
    bool ReadOnly,
    bool Deterministic,
    bool FixtureSafe,
    bool FilesystemReadAttempted,
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
    bool RecipeExecutionStarted,
    bool ApprovalExecutionStarted,
    bool ApprovalStateMutationAttempted,
    bool ProductActionExposed,
    bool ProductServiceRegistered)
{
    public bool Passes =>
        ReadOnly
        && Deterministic
        && FixtureSafe
        && !FilesystemReadAttempted
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
        && !RecipeExecutionStarted
        && !ApprovalExecutionStarted
        && !ApprovalStateMutationAttempted
        && !ProductActionExposed
        && !ProductServiceRegistered;

    public static ApprovalReviewNoSideEffectProof FixtureReadOnly() =>
        new(
            ReadOnly: true,
            Deterministic: true,
            FixtureSafe: true,
            FilesystemReadAttempted: false,
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
            RecipeExecutionStarted: false,
            ApprovalExecutionStarted: false,
            ApprovalStateMutationAttempted: false,
            ProductActionExposed: false,
            ProductServiceRegistered: false);
}

public sealed record ApprovalReviewEvidenceLink(
    string EvidenceId,
    string Label,
    ApprovalHumanReviewSourceKind Source,
    bool FixtureOnly,
    bool PayloadValuesExcluded,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    ApprovalReviewNoSideEffectProof NoSideEffectProof);

public sealed record ApprovalReviewContextLink(
    string ContextId,
    string Label,
    ApprovalHumanReviewSourceKind Source,
    bool FixtureOnly,
    bool Stale,
    bool Missing,
    bool Excluded,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    ApprovalReviewNoSideEffectProof NoSideEffectProof);

public sealed record ApprovalReviewRiskSummary(
    string RiskId,
    ApprovalRiskLevel RiskLevel,
    string Rationale,
    bool BlocksDecision,
    bool BlocksSafeNextStep,
    bool HumanReviewRequired,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ContextRefs,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    ApprovalReviewNoSideEffectProof NoSideEffectProof);

public sealed record ApprovalCandidateActionPreview(
    string CandidateActionId,
    ApprovalCandidateActionKind ActionKind,
    string Title,
    string Summary,
    ApprovalRiskLevel RiskLevel,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ContextRefs,
    bool HumanReviewRequired,
    bool DecisionAllowedOnlyAsPreview,
    int ProductActionCount,
    int StateMutationCount,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    ApprovalReviewNoSideEffectProof NoSideEffectProof);

public sealed record ApprovalHumanDecisionOption(
    ApprovalDecisionOptionKind OptionKind,
    string Label,
    string Summary,
    bool PreviewOnly,
    bool ExecutesAction,
    bool MutatesState,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    ApprovalReviewNoSideEffectProof NoSideEffectProof);

public sealed record ApprovalHumanReviewItem(
    string ItemId,
    ApprovalHumanReviewItemKind Kind,
    ApprovalHumanReviewItemStatus Status,
    ApprovalHumanReviewSeverity Severity,
    ApprovalHumanReviewSourceKind Source,
    string Title,
    string Summary,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ContextRefs,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    bool HumanReviewRequired,
    bool DecisionAllowedOnlyAsPreview,
    int ProductActionCount,
    int StateMutationCount,
    ApprovalReviewNoSideEffectProof NoSideEffectProof);

public sealed record ApprovalHumanReviewPacketReadOnly(
    string PacketId,
    string Title,
    string Mode,
    string SourceLabel,
    string Summary,
    IReadOnlyList<ApprovalReviewEvidenceLink> EvidenceLinks,
    IReadOnlyList<ApprovalReviewContextLink> ContextLinks,
    IReadOnlyList<ApprovalReviewRiskSummary> RiskSummaries,
    IReadOnlyList<ApprovalCandidateActionPreview> CandidateActions,
    IReadOnlyList<ApprovalHumanDecisionOption> DecisionOptions,
    IReadOnlyList<ApprovalHumanReviewItem> Items,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    IReadOnlyList<string> DocumentedDebt,
    string SafeNextStep,
    string ReadOnlySummary,
    ApprovalReviewNoSideEffectProof NoSideEffectProof)
{
    public bool ReadOnly => NoSideEffectProof.ReadOnly;
    public bool Deterministic => NoSideEffectProof.Deterministic;
    public bool FixtureSafe => NoSideEffectProof.FixtureSafe;
    public int ProductActionCount => Items.Sum(item => item.ProductActionCount) + CandidateActions.Sum(action => action.ProductActionCount);
    public int StateMutationCount => Items.Sum(item => item.StateMutationCount) + CandidateActions.Sum(action => action.StateMutationCount);
    public bool HasApprovalExecution => NoSideEffectProof.ApprovalExecutionStarted || DecisionOptions.Any(option => option.ExecutesAction);
    public bool HasApprovalStateMutation => NoSideEffectProof.ApprovalStateMutationAttempted || DecisionOptions.Any(option => option.MutatesState);
    public bool HasDurableMemory => NoSideEffectProof.DurableMemoryActive;
}

public static class ApprovalHumanReviewReadOnlyPresenter
{
    public static ApprovalHumanReviewPacketReadOnly CreateFixture()
    {
        var evidence = EvidenceIntelligenceAuditDashboardReadOnlyPresenter.CreateFixture();
        var context = WorkspaceContextPacketExportReadOnlyPresenter.CreateFixture();
        var proof = ApprovalReviewNoSideEffectProof.FixtureReadOnly();
        var evidenceLinks = EvidenceLinks(evidence, proof);
        var contextLinks = ContextLinks(context, proof);
        var riskSummaries = RiskSummaries(evidenceLinks, contextLinks, proof);
        var candidateActions = CandidateActions(evidenceLinks, contextLinks, riskSummaries, proof);
        var decisionOptions = DecisionOptions(proof);
        var warnings = Warnings();
        var blockers = Blockers();
        var debt = DocumentedDebt();
        var items = Items(evidenceLinks, contextLinks, riskSummaries, warnings, blockers, debt, proof);
        var summary = Summary(items, candidateActions, decisionOptions, evidenceLinks, contextLinks, riskSummaries);

        return new ApprovalHumanReviewPacketReadOnly(
            PacketId: "phase-e.approval-human-review.read-only.foundation.fixture.v1",
            Title: "Approval Human Review Read-Only Foundation",
            Mode: "READ_ONLY_FIXTURE_SAFE_NO_APPROVAL_EXECUTION_NO_STATE_MUTATION",
            SourceLabel: "EvidenceIntelligenceAuditDashboardReadOnlyPresenter.CreateFixture + WorkspaceContextPacketExportReadOnlyPresenter.CreateFixture",
            Summary: "Human review packet preview composes Phase C evidence and Phase D context without executing approval, mutating state, or exposing product actions.",
            EvidenceLinks: evidenceLinks,
            ContextLinks: contextLinks,
            RiskSummaries: riskSummaries,
            CandidateActions: candidateActions,
            DecisionOptions: decisionOptions,
            Items: items,
            Warnings: warnings,
            Blockers: blockers,
            DocumentedDebt: debt,
            SafeNextStep: "PHASE_E_APPROVAL_RISK_DECISION_GUARDS",
            ReadOnlySummary: summary,
            NoSideEffectProof: proof);
    }

    private static IReadOnlyList<ApprovalReviewEvidenceLink> EvidenceLinks(
        EvidenceIntelligenceAuditDashboardResult evidence,
        ApprovalReviewNoSideEffectProof proof) =>
    [
        new(
            "phase-c.evidence.audit-dashboard",
            evidence.Title,
            ApprovalHumanReviewSourceKind.PhaseCEvidence,
            FixtureOnly: true,
            PayloadValuesExcluded: true,
            Warnings: Clean(evidence.Warnings.Take(3).Concat(["Phase C evidence is read-only and fixture-safe."]).ToList()),
            Blockers: Clean(evidence.Blockers.Take(3).ToList()),
            NoSideEffectProof: proof),
        new(
            "phase-c.evidence.timeline-export-preview",
            evidence.TimelineExport.Manifest.Title,
            ApprovalHumanReviewSourceKind.PhaseCEvidence,
            FixtureOnly: true,
            PayloadValuesExcluded: true,
            Warnings: Clean(evidence.TimelineExport.Warnings.Take(3).ToList()),
            Blockers: Clean(evidence.TimelineExport.Blockers.Take(3).ToList()),
            NoSideEffectProof: proof),
        new(
            "phase-c.evidence.release-no-go",
            evidence.ReleaseCommercialDecision,
            ApprovalHumanReviewSourceKind.PhaseCEvidence,
            FixtureOnly: true,
            PayloadValuesExcluded: true,
            Warnings: ["Release/commercial remains NO-GO."],
            Blockers: ["Runtime/live and durable persistence remain blocked."],
            NoSideEffectProof: proof)
    ];

    private static IReadOnlyList<ApprovalReviewContextLink> ContextLinks(
        WorkspaceContextPacketExportReadOnlyPreview context,
        ApprovalReviewNoSideEffectProof proof) =>
    [
        new(
            "phase-d.context.packet-export-preview",
            context.Title,
            ApprovalHumanReviewSourceKind.PhaseDContext,
            FixtureOnly: true,
            Stale: false,
            Missing: false,
            Excluded: false,
            EvidenceRefs: ["phase-c.evidence.audit-dashboard"],
            Warnings: Clean(context.Warnings.Take(4).Concat(["Context packet export is in-memory preview only."]).ToList()),
            Blockers: Clean(context.Blockers.Take(4).ToList()),
            NoSideEffectProof: proof),
        new(
            "phase-d.context.stale-review-required",
            "Stale context blocker preview",
            ApprovalHumanReviewSourceKind.PhaseDContext,
            FixtureOnly: true,
            Stale: true,
            Missing: false,
            Excluded: false,
            EvidenceRefs: ["phase-c.evidence.timeline-export-preview"],
            Warnings: ["Stale context requires refresh before any decision preview can be trusted."],
            Blockers: ["Stale context blocks approval decision use."],
            NoSideEffectProof: proof),
        new(
            "phase-d.context.missing-review-required",
            "Missing context blocker preview",
            ApprovalHumanReviewSourceKind.PhaseDContext,
            FixtureOnly: true,
            Stale: false,
            Missing: true,
            Excluded: false,
            EvidenceRefs: [],
            Warnings: ["Missing context is shown as a review gap."],
            Blockers: ["Missing context blocks approval decision use."],
            NoSideEffectProof: proof),
        new(
            "phase-d.context.excluded-review-required",
            "Excluded context blocker preview",
            ApprovalHumanReviewSourceKind.PhaseDContext,
            FixtureOnly: true,
            Stale: false,
            Missing: false,
            Excluded: true,
            EvidenceRefs: [],
            Warnings: ["Excluded context remains visible only as a blocker label."],
            Blockers: ["Excluded context cannot support candidate actions."],
            NoSideEffectProof: proof)
    ];

    private static IReadOnlyList<ApprovalReviewRiskSummary> RiskSummaries(
        IReadOnlyList<ApprovalReviewEvidenceLink> evidenceLinks,
        IReadOnlyList<ApprovalReviewContextLink> contextLinks,
        ApprovalReviewNoSideEffectProof proof) =>
    [
        new(
            "approval.risk.missing-evidence",
            ApprovalRiskLevel.Critical,
            "Candidate action lacks enough linked evidence for approval execution.",
            BlocksDecision: true,
            BlocksSafeNextStep: true,
            HumanReviewRequired: true,
            EvidenceRefs: [],
            ContextRefs: contextLinks.Select(link => link.ContextId).Take(1).ToList(),
            Warnings: ["Evidence gap is retained for human review."],
            Blockers: ["Missing evidence blocks approval execution."],
            NoSideEffectProof: proof),
        new(
            "approval.risk.stale-context",
            ApprovalRiskLevel.Critical,
            "Stale or missing context cannot be used for approval decisions.",
            BlocksDecision: true,
            BlocksSafeNextStep: true,
            HumanReviewRequired: true,
            EvidenceRefs: evidenceLinks.Select(link => link.EvidenceId).Take(1).ToList(),
            ContextRefs: contextLinks.Where(link => link.Stale || link.Missing).Select(link => link.ContextId).ToList(),
            Warnings: ["Context refresh must be reviewed by a human before any future action flow."],
            Blockers: ["Stale or missing context blocks approval decision use."],
            NoSideEffectProof: proof),
        new(
            "approval.risk.unresolved-contradiction",
            ApprovalRiskLevel.Critical,
            "Unresolved contradiction remains a human-review blocker.",
            BlocksDecision: true,
            BlocksSafeNextStep: true,
            HumanReviewRequired: true,
            EvidenceRefs: evidenceLinks.Select(link => link.EvidenceId).ToList(),
            ContextRefs: contextLinks.Select(link => link.ContextId).Take(2).ToList(),
            Warnings: ["Contradiction is surfaced, not resolved automatically."],
            Blockers: ["Unresolved contradiction blocks safe next step approval."],
            NoSideEffectProof: proof),
        new(
            "approval.risk.preview-only-action",
            ApprovalRiskLevel.Medium,
            "Decision options are labels for review and do not execute or mutate state.",
            BlocksDecision: false,
            BlocksSafeNextStep: false,
            HumanReviewRequired: true,
            EvidenceRefs: evidenceLinks.Select(link => link.EvidenceId).Take(2).ToList(),
            ContextRefs: contextLinks.Select(link => link.ContextId).Take(2).ToList(),
            Warnings: ["Approval preview is not approval execution."],
            Blockers: [],
            NoSideEffectProof: proof)
    ];

    private static IReadOnlyList<ApprovalCandidateActionPreview> CandidateActions(
        IReadOnlyList<ApprovalReviewEvidenceLink> evidenceLinks,
        IReadOnlyList<ApprovalReviewContextLink> contextLinks,
        IReadOnlyList<ApprovalReviewRiskSummary> risks,
        ApprovalReviewNoSideEffectProof proof) =>
    [
        CandidateAction(
            "approval.candidate.review-safe-next-step",
            ApprovalCandidateActionKind.SafeNextStepReview,
            "Review next safe step",
            "Preview asks a human to review the next Phase E guard block; it does not approve or run it.",
            ApprovalRiskLevel.Medium,
            evidenceLinks.Select(link => link.EvidenceId).Take(2).ToList(),
            contextLinks.Select(link => link.ContextId).Take(2).ToList(),
            ["Human review required before any future approval capability."],
            risks.Where(risk => risk.BlocksSafeNextStep).SelectMany(risk => risk.Blockers).Distinct().ToList(),
            proof),
        CandidateAction(
            "approval.candidate.request-evidence-gap-review",
            ApprovalCandidateActionKind.ProposedEvidenceDecision,
            "Request evidence gap review",
            "Preview records that missing evidence must be reviewed before approval execution can exist.",
            ApprovalRiskLevel.Critical,
            [],
            contextLinks.Select(link => link.ContextId).Take(1).ToList(),
            ["Missing evidence is a blocker, not an executable task."],
            ["Missing evidence blocks approval decision use."],
            proof),
        CandidateAction(
            "approval.candidate.request-context-refresh-review",
            ApprovalCandidateActionKind.ProposedContextDecision,
            "Request context refresh review",
            "Preview records stale/missing context as a human-review need without scanning the workspace.",
            ApprovalRiskLevel.Critical,
            evidenceLinks.Select(link => link.EvidenceId).Take(1).ToList(),
            contextLinks.Where(link => link.Stale || link.Missing).Select(link => link.ContextId).ToList(),
            ["Context refresh is not executed by this foundation."],
            ["Stale or missing context blocks approval decision use."],
            proof)
    ];

    private static ApprovalCandidateActionPreview CandidateAction(
        string id,
        ApprovalCandidateActionKind kind,
        string title,
        string summary,
        ApprovalRiskLevel risk,
        IReadOnlyList<string> evidenceRefs,
        IReadOnlyList<string> contextRefs,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> blockers,
        ApprovalReviewNoSideEffectProof proof) =>
        new(
            id,
            kind,
            title,
            Clean(summary),
            risk,
            evidenceRefs,
            contextRefs,
            HumanReviewRequired: true,
            DecisionAllowedOnlyAsPreview: true,
            ProductActionCount: 0,
            StateMutationCount: 0,
            Warnings: Clean(warnings),
            Blockers: Clean(blockers),
            NoSideEffectProof: proof);

    private static IReadOnlyList<ApprovalHumanDecisionOption> DecisionOptions(ApprovalReviewNoSideEffectProof proof) =>
    [
        DecisionOption(ApprovalDecisionOptionKind.ApprovePreviewOnly, "Approve preview only", "Shows what a future approve label would require; no approval is recorded.", ["Approval execution disabled."], ["No approval state mutation exists."], proof),
        DecisionOption(ApprovalDecisionOptionKind.RejectPreviewOnly, "Reject preview only", "Shows what a future reject label would require; no state changes.", ["Human review remains preview-only."], [], proof),
        DecisionOption(ApprovalDecisionOptionKind.RequestMoreEvidence, "Request more evidence", "Preview labels a missing-evidence blocker for future review.", ["Evidence request is not sent anywhere."], ["Missing evidence remains blocked."], proof),
        DecisionOption(ApprovalDecisionOptionKind.RequestContextRefresh, "Request context refresh", "Preview labels stale or missing context for future review.", ["No workspace scan or context refresh is started."], ["Stale or missing context remains blocked."], proof),
        DecisionOption(ApprovalDecisionOptionKind.Defer, "Defer", "Preview labels deferral as the safest current outcome.", ["Deferral is not persisted."], [], proof)
    ];

    private static ApprovalHumanDecisionOption DecisionOption(
        ApprovalDecisionOptionKind kind,
        string label,
        string summary,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> blockers,
        ApprovalReviewNoSideEffectProof proof) =>
        new(
            kind,
            label,
            Clean(summary),
            PreviewOnly: true,
            ExecutesAction: false,
            MutatesState: false,
            Warnings: Clean(warnings),
            Blockers: Clean(blockers),
            NoSideEffectProof: proof);

    private static IReadOnlyList<ApprovalHumanReviewItem> Items(
        IReadOnlyList<ApprovalReviewEvidenceLink> evidenceLinks,
        IReadOnlyList<ApprovalReviewContextLink> contextLinks,
        IReadOnlyList<ApprovalReviewRiskSummary> risks,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> blockers,
        IReadOnlyList<string> debt,
        ApprovalReviewNoSideEffectProof proof) =>
    [
        Item("approval.packet.identity.fixture", ApprovalHumanReviewItemKind.ApprovalPacketIdentity, ApprovalHumanReviewItemStatus.Ready, ApprovalHumanReviewSeverity.Info, ApprovalHumanReviewSourceKind.Fixture, "Approval packet identity fixture", "Deterministic Phase E approval/human review packet fixture.", [], [], [], [], false, true, proof),
        Item("human.review.summary", ApprovalHumanReviewItemKind.HumanReviewSummary, ApprovalHumanReviewItemStatus.Warning, ApprovalHumanReviewSeverity.Warning, ApprovalHumanReviewSourceKind.Fixture, "Human review summary", "Human review is required before any future approval execution capability.", EvidenceIds(evidenceLinks), ContextIds(contextLinks), warnings, [], true, true, proof),
        Item("candidate.action.preview", ApprovalHumanReviewItemKind.CandidateActionPreview, ApprovalHumanReviewItemStatus.PreviewOnly, ApprovalHumanReviewSeverity.Warning, ApprovalHumanReviewSourceKind.Fixture, "Candidate action preview", "Candidate actions are preview-only and expose zero product actions.", EvidenceIds(evidenceLinks), ContextIds(contextLinks), [], [], true, true, proof),
        Item("candidate.action.kind", ApprovalHumanReviewItemKind.CandidateActionKind, ApprovalHumanReviewItemStatus.PreviewOnly, ApprovalHumanReviewSeverity.Info, ApprovalHumanReviewSourceKind.Fixture, "Candidate action kind", "Review-only, proposed evidence/context decision, and safe-next-step review labels are represented without execution.", [], [], [], [], true, true, proof),
        Item("risk.level", ApprovalHumanReviewItemKind.RiskLevel, ApprovalHumanReviewItemStatus.Blocked, ApprovalHumanReviewSeverity.Blocked, ApprovalHumanReviewSourceKind.Fixture, "Risk level", "Critical risk blocks approval decision use and safe next step approval.", EvidenceIds(evidenceLinks), ContextIds(contextLinks), [], ["Critical risk blocks approval decision use."], true, true, proof),
        Item("risk.rationale", ApprovalHumanReviewItemKind.RiskRationale, ApprovalHumanReviewItemStatus.Warning, ApprovalHumanReviewSeverity.Warning, ApprovalHumanReviewSourceKind.Fixture, "Risk rationale", string.Join(" ", risks.Select(risk => risk.Rationale)), EvidenceIds(evidenceLinks), ContextIds(contextLinks), [], [], true, true, proof),
        Item("phase.c.evidence.links", ApprovalHumanReviewItemKind.EvidenceLink, ApprovalHumanReviewItemStatus.Ready, ApprovalHumanReviewSeverity.Info, ApprovalHumanReviewSourceKind.PhaseCEvidence, "Evidence links from Phase C", "Links to read-only Phase C evidence/dashboard/export previews.", EvidenceIds(evidenceLinks), [], [], [], true, true, proof),
        Item("phase.d.context.links", ApprovalHumanReviewItemKind.ContextLink, ApprovalHumanReviewItemStatus.Warning, ApprovalHumanReviewSeverity.Warning, ApprovalHumanReviewSourceKind.PhaseDContext, "Context links from Phase D", "Links to read-only Phase D context packet export preview and blocker labels.", [], ContextIds(contextLinks), warnings, blockers.Take(2).ToList(), true, true, proof),
        Item("authority.freshness.summary", ApprovalHumanReviewItemKind.AuthorityFreshnessSummary, ApprovalHumanReviewItemStatus.Warning, ApprovalHumanReviewSeverity.Warning, ApprovalHumanReviewSourceKind.PhaseDContext, "Authority/freshness summary", "Unknown authority, missing freshness, stale and contradictory context remain blockers via Phase D guards.", [], ContextIds(contextLinks), ["No trust by default."], ["Stale context blocks decision use."], true, true, proof),
        Item("selection.lock.exclusion.summary", ApprovalHumanReviewItemKind.SelectionLockExclusionSummary, ApprovalHumanReviewItemStatus.Warning, ApprovalHumanReviewSeverity.Warning, ApprovalHumanReviewSourceKind.PhaseDContext, "Selection/lock/exclusion summary", "Excluded wins over selected; locked context needs review before influence.", [], ContextIds(contextLinks), ["Locked context is read-only until reviewed."], ["Excluded context blocks candidate action support."], true, true, proof),
        Item("memory.candidate.risk.contradiction.summary", ApprovalHumanReviewItemKind.MemoryCandidateRiskContradictionSummary, ApprovalHumanReviewItemStatus.Warning, ApprovalHumanReviewSeverity.Warning, ApprovalHumanReviewSourceKind.PhaseDContext, "Memory candidate risk/contradiction summary", "Candidate is not memory; risk is not decision; unresolved contradiction blocks safe next step approval.", EvidenceIds(evidenceLinks), ContextIds(contextLinks), ["Candidate influence is preview-only."], ["Unresolved contradiction blocks safe next step approval."], true, true, proof),
        Item("required.human.decision", ApprovalHumanReviewItemKind.RequiredHumanDecision, ApprovalHumanReviewItemStatus.Blocked, ApprovalHumanReviewSeverity.Blocked, ApprovalHumanReviewSourceKind.Fixture, "Required human decision", "Human decision is required but available only as preview labels in this foundation.", EvidenceIds(evidenceLinks), ContextIds(contextLinks), [], ["Approval execution disabled."], true, true, proof),
        Item("decision.options.preview", ApprovalHumanReviewItemKind.DecisionOptionPreview, ApprovalHumanReviewItemStatus.PreviewOnly, ApprovalHumanReviewSeverity.Warning, ApprovalHumanReviewSourceKind.Fixture, "Decision options preview", "Approve/reject/request/defer options are labels only and mutate no state.", [], [], ["Decision options are preview-only."], ["No approval state mutation exists."], true, true, proof),
        Item("approval.blockers", ApprovalHumanReviewItemKind.ApprovalBlocker, ApprovalHumanReviewItemStatus.Blocked, ApprovalHumanReviewSeverity.Blocked, ApprovalHumanReviewSourceKind.Fixture, "Approval blockers", "Aggregated approval blockers are retained for human review.", EvidenceIds(evidenceLinks), ContextIds(contextLinks), [], blockers, true, true, proof),
        Item("approval.warnings", ApprovalHumanReviewItemKind.ApprovalWarning, ApprovalHumanReviewItemStatus.Warning, ApprovalHumanReviewSeverity.Warning, ApprovalHumanReviewSourceKind.Fixture, "Approval warnings", "Aggregated approval warnings are retained for human review.", EvidenceIds(evidenceLinks), ContextIds(contextLinks), warnings, [], true, true, proof),
        Item("missing.evidence.blocker", ApprovalHumanReviewItemKind.MissingEvidenceBlocker, ApprovalHumanReviewItemStatus.Blocked, ApprovalHumanReviewSeverity.Blocked, ApprovalHumanReviewSourceKind.PhaseCEvidence, "Missing evidence blocker", "Missing evidence blocks approval decision use.", [], ContextIds(contextLinks).Take(1).ToList(), [], ["Missing evidence blocks approval decision use."], true, true, proof),
        Item("missing.context.blocker", ApprovalHumanReviewItemKind.MissingContextBlocker, ApprovalHumanReviewItemStatus.Blocked, ApprovalHumanReviewSeverity.Blocked, ApprovalHumanReviewSourceKind.PhaseDContext, "Missing context blocker", "Missing context blocks approval decision use.", EvidenceIds(evidenceLinks).Take(1).ToList(), contextLinks.Where(link => link.Missing).Select(link => link.ContextId).ToList(), [], ["Missing context blocks approval decision use."], true, true, proof),
        Item("stale.context.blocker", ApprovalHumanReviewItemKind.StaleContextBlocker, ApprovalHumanReviewItemStatus.Blocked, ApprovalHumanReviewSeverity.Blocked, ApprovalHumanReviewSourceKind.PhaseDContext, "Stale context blocker", "Stale context blocks approval decision use and safe next step approval.", EvidenceIds(evidenceLinks).Take(1).ToList(), contextLinks.Where(link => link.Stale).Select(link => link.ContextId).ToList(), [], ["Stale context blocks approval decision use."], true, true, proof),
        Item("unresolved.contradiction.blocker", ApprovalHumanReviewItemKind.UnresolvedContradictionBlocker, ApprovalHumanReviewItemStatus.Blocked, ApprovalHumanReviewSeverity.Blocked, ApprovalHumanReviewSourceKind.PhaseDContext, "Unresolved contradiction blocker", "Unresolved contradiction blocks safe next step approval until reviewed.", EvidenceIds(evidenceLinks), ContextIds(contextLinks), [], ["Unresolved contradiction blocks safe next step approval."], true, true, proof),
        Item("critical.risk.blocker", ApprovalHumanReviewItemKind.CriticalRiskBlocker, ApprovalHumanReviewItemStatus.Blocked, ApprovalHumanReviewSeverity.Blocked, ApprovalHumanReviewSourceKind.Fixture, "Critical risk blocker", "Critical risk blocks approval decision use and safe next step approval.", EvidenceIds(evidenceLinks), ContextIds(contextLinks), [], ["Critical risk blocks approval decision use."], true, true, proof),
        Item("runtime.live.disabled", ApprovalHumanReviewItemKind.RuntimeLiveDisabledNotice, ApprovalHumanReviewItemStatus.Disabled, ApprovalHumanReviewSeverity.Disabled, ApprovalHumanReviewSourceKind.CapabilityNotice, "Runtime/live disabled notice", "Runtime/live/browser/CDP/WCU/OCR remain disabled.", [], [], [], ["Runtime/live disabled."], false, true, proof),
        Item("filesystem.db.disabled", ApprovalHumanReviewItemKind.FilesystemDatabaseDisabledNotice, ApprovalHumanReviewItemStatus.Disabled, ApprovalHumanReviewSeverity.Disabled, ApprovalHumanReviewSourceKind.CapabilityNotice, "Filesystem/DB disabled notice", "Filesystem and DB access remain disabled for this foundation.", [], [], [], ["Filesystem/DB disabled."], false, true, proof),
        Item("provider.cloud.disabled", ApprovalHumanReviewItemKind.ProviderCloudDisabledNotice, ApprovalHumanReviewItemStatus.Disabled, ApprovalHumanReviewSeverity.Disabled, ApprovalHumanReviewSourceKind.CapabilityNotice, "Provider/cloud disabled notice", "Provider/cloud/network access remains disabled.", [], [], [], ["Provider/cloud disabled."], false, true, proof),
        Item("durable.memory.disabled", ApprovalHumanReviewItemKind.DurableMemoryDisabledNotice, ApprovalHumanReviewItemStatus.Disabled, ApprovalHumanReviewSeverity.Disabled, ApprovalHumanReviewSourceKind.CapabilityNotice, "Durable memory disabled notice", "Durable memory remains disabled; review packet is not memory.", [], [], [], ["Durable memory disabled."], false, true, proof),
        Item("safe.next.step", ApprovalHumanReviewItemKind.SafeNextStep, ApprovalHumanReviewItemStatus.Warning, ApprovalHumanReviewSeverity.Warning, ApprovalHumanReviewSourceKind.DocumentedDebt, "Safe next step", "PHASE_E_APPROVAL_RISK_DECISION_GUARDS", [], [], ["Next step is guard hardening, not execution."], [], true, true, proof),
        Item("no.side.effect.proof", ApprovalHumanReviewItemKind.NoSideEffectProof, ApprovalHumanReviewItemStatus.Ready, ApprovalHumanReviewSeverity.Info, ApprovalHumanReviewSourceKind.NoSideEffectProof, "No-side-effect proof", $"Proof passes: {proof.Passes}. Product actions: 0. State mutations: 0.", [], [], [], [], false, true, proof),
        Item("deferred.capabilities.debt", ApprovalHumanReviewItemKind.DeferredCapability, ApprovalHumanReviewItemStatus.Deferred, ApprovalHumanReviewSeverity.Deferred, ApprovalHumanReviewSourceKind.DocumentedDebt, "Deferred capabilities/debt", string.Join(" ", debt), [], [], debt, [], true, true, proof)
    ];

    private static ApprovalHumanReviewItem Item(
        string id,
        ApprovalHumanReviewItemKind kind,
        ApprovalHumanReviewItemStatus status,
        ApprovalHumanReviewSeverity severity,
        ApprovalHumanReviewSourceKind source,
        string title,
        string summary,
        IReadOnlyList<string> evidenceRefs,
        IReadOnlyList<string> contextRefs,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> blockers,
        bool humanReviewRequired,
        bool decisionAllowedOnlyAsPreview,
        ApprovalReviewNoSideEffectProof proof) =>
        new(
            id,
            kind,
            status,
            severity,
            source,
            title,
            Clean(summary),
            evidenceRefs,
            contextRefs,
            Clean(warnings),
            Clean(blockers),
            humanReviewRequired,
            decisionAllowedOnlyAsPreview,
            ProductActionCount: 0,
            StateMutationCount: 0,
            NoSideEffectProof: proof);

    private static IReadOnlyList<string> Warnings() =>
    [
        "Approval/human review packet is fixture-safe and read-only.",
        "Decision options are preview labels only.",
        "Phase C evidence and Phase D context are referenced through read-only fixtures.",
        "Human review remains required before any future approval capability."
    ];

    private static IReadOnlyList<string> Blockers() =>
    [
        "Approval execution disabled.",
        "Approval state mutation disabled.",
        "Missing evidence blocks approval decision use.",
        "Missing context blocks approval decision use.",
        "Stale context blocks approval decision use.",
        "Unresolved contradiction blocks safe next step approval.",
        "Critical risk blocks approval decision use.",
        "Runtime/live, filesystem/DB, provider/cloud and durable memory remain disabled."
    ];

    private static IReadOnlyList<string> DocumentedDebt() =>
    [
        "Future approval risk/decision guards.",
        "Future human-review evidence/context link hardening.",
        "Future approval packet surface read-only.",
        "Any real approval execution requires a separate protected audit and explicit unlock.",
        "Any state mutation requires a separate persistence/runtime approval design."
    ];

    private static string Summary(
        IReadOnlyList<ApprovalHumanReviewItem> items,
        IReadOnlyList<ApprovalCandidateActionPreview> candidates,
        IReadOnlyList<ApprovalHumanDecisionOption> options,
        IReadOnlyList<ApprovalReviewEvidenceLink> evidenceLinks,
        IReadOnlyList<ApprovalReviewContextLink> contextLinks,
        IReadOnlyList<ApprovalReviewRiskSummary> risks) =>
        string.Join(
            "\n",
            new[]
            {
                "# Approval Human Review Read-Only Foundation",
                "Mode: READ_ONLY_FIXTURE_SAFE_NO_APPROVAL_EXECUTION_NO_STATE_MUTATION",
                $"Items: {items.Count}.",
                $"CandidateActionPreviews: {candidates.Count}.",
                $"DecisionOptionsPreview: {options.Count}.",
                $"EvidenceLinks: {evidenceLinks.Count}.",
                $"ContextLinks: {contextLinks.Count}.",
                $"CriticalRisks: {risks.Count(risk => risk.RiskLevel == ApprovalRiskLevel.Critical)}.",
                $"ProductActionCount: {items.Sum(item => item.ProductActionCount) + candidates.Sum(candidate => candidate.ProductActionCount)}.",
                $"StateMutationCount: {items.Sum(item => item.StateMutationCount) + candidates.Sum(candidate => candidate.StateMutationCount)}.",
                "Approval preview is not approval execution.",
                "Human review packet is not state mutation.",
                "No runtime/live, filesystem/DB, provider/cloud, semantic/vector, LLM live or durable memory is enabled.",
                "NextSafeStep: PHASE_E_APPROVAL_RISK_DECISION_GUARDS"
            });

    private static IReadOnlyList<string> EvidenceIds(IReadOnlyList<ApprovalReviewEvidenceLink> links) =>
        links.Select(link => link.EvidenceId).ToList();

    private static IReadOnlyList<string> ContextIds(IReadOnlyList<ApprovalReviewContextLink> links) =>
        links.Select(link => link.ContextId).ToList();

    private static IReadOnlyList<string> Clean(IReadOnlyList<string> values) =>
        values.Select(Clean).Distinct().ToList();

    private static string Clean(string value) =>
        value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("raw payload", "excluded payload", StringComparison.OrdinalIgnoreCase)
            .Replace("secret", "sensitive value", StringComparison.OrdinalIgnoreCase);
}
