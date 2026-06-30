namespace OneBrain.Core.Approval;

public enum ApprovalRiskDecisionReadOnlyDecision
{
    AllowedPreviewOnly,
    NeedsHumanReview,
    WarningPreviewOnly,
    Blocked,
    Excluded
}

public enum ApprovalRiskDecisionReadOnlyIssueKind
{
    None,
    HumanReviewRequired,
    CriticalRisk,
    MissingEvidence,
    MissingContext,
    StaleContext,
    ExcludedContext,
    UnresolvedContradiction,
    ApproveWithCriticalRisk,
    ApproveWithoutEvidence,
    ProviderCloudDerivedWhileDisabled,
    SemanticVectorDerivedWhileDisabled,
    LlmDerivedWhileDisabled,
    FilesystemWriteImplication,
    RuntimeLiveImplication,
    ApprovalStateMutationImplication,
    ProductActionCountNonZero,
    StateMutationCountNonZero,
    RawSensitivePayload,
    FixtureOnlyNotProductionTrusted,
    ConflictingRiskSummaries,
    HumanReviewMissing
}

public enum ApprovalRiskDecisionEvidenceState
{
    Present,
    Missing
}

public enum ApprovalRiskDecisionContextState
{
    PresentFresh,
    Missing,
    Stale,
    Excluded
}

public enum ApprovalRiskDecisionContradictionState
{
    None,
    Unresolved,
    ConflictingRiskSummaries
}

public enum ApprovalRiskDecisionHumanReviewState
{
    NotRequired,
    Present,
    RequiredMissing
}

public enum ApprovalCandidateActionImplication
{
    None,
    ProviderCloud,
    SemanticVector,
    LlmLive,
    FilesystemWrite,
    RuntimeLive,
    ApprovalStateMutation,
    RawSensitivePayload
}

public sealed record ApprovalRiskDecisionReadOnlyFixture(
    string FixtureId,
    ApprovalRiskLevel RiskLevel,
    ApprovalDecisionOptionKind DecisionOptionKind,
    ApprovalRiskDecisionEvidenceState EvidenceState,
    ApprovalRiskDecisionContextState ContextState,
    ApprovalRiskDecisionContradictionState ContradictionState,
    ApprovalRiskDecisionHumanReviewState HumanReviewState,
    ApprovalCandidateActionImplication CandidateActionImplication,
    bool FixtureOnly,
    int ProductActionCount,
    int StateMutationCount,
    ApprovalRiskDecisionReadOnlyDecision ExpectedDecision,
    ApprovalRiskDecisionReadOnlyIssueKind ExpectedIssue,
    string ExpectedBlockerOrWarning,
    ApprovalReviewNoSideEffectProof NoSideEffectProof);

public sealed record ApprovalRiskDecisionReadOnlyIssue(
    ApprovalRiskDecisionReadOnlyIssueKind Kind,
    string Reason,
    bool BlocksApprove,
    bool BlocksSafeNextStep,
    bool RequiresHumanReview,
    bool WarningOnly);

public sealed record ApprovalRiskDecisionReadOnlyResult(
    string FixtureId,
    ApprovalRiskDecisionReadOnlyDecision Decision,
    ApprovalRiskLevel RiskLevel,
    ApprovalDecisionOptionKind DecisionOptionKind,
    IReadOnlyList<ApprovalRiskDecisionReadOnlyIssue> Issues,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    bool PreviewOnly,
    bool AllowsDecisionOptionPreview,
    bool AllowsApprovePreview,
    bool ApprovalExecutionAllowed,
    bool StateMutationAllowed,
    bool ProductActionAllowed,
    bool RequiresHumanReview,
    bool ProviderCloudDisabled,
    bool SemanticVectorDisabled,
    bool LlmLiveDisabled,
    int ProductActionCount,
    int StateMutationCount,
    ApprovalReviewNoSideEffectProof NoSideEffectProof)
{
    public bool Blocked => Decision is ApprovalRiskDecisionReadOnlyDecision.Blocked or ApprovalRiskDecisionReadOnlyDecision.Excluded;

    public bool HasIssue(ApprovalRiskDecisionReadOnlyIssueKind kind) =>
        Issues.Any(issue => issue.Kind == kind);
}

public static class ApprovalRiskDecisionReadOnlyGuard
{
    public static IReadOnlyList<ApprovalRiskDecisionReadOnlyFixture> CreateFixtureCatalog()
    {
        var proof = ApprovalReviewNoSideEffectProof.FixtureReadOnly();

        return
        [
            Fixture("approval.low-risk.evidence-context", ApprovalRiskLevel.Low, ApprovalDecisionOptionKind.ApprovePreviewOnly, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.NotRequired, ApprovalCandidateActionImplication.None, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.AllowedPreviewOnly, ApprovalRiskDecisionReadOnlyIssueKind.None, "", proof),
            Fixture("approval.medium-risk.human-review", ApprovalRiskLevel.Medium, ApprovalDecisionOptionKind.ApprovePreviewOnly, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.Present, ApprovalCandidateActionImplication.None, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.NeedsHumanReview, ApprovalRiskDecisionReadOnlyIssueKind.HumanReviewRequired, "Medium risk remains preview-only and requires human review.", proof),
            Fixture("approval.critical-risk", ApprovalRiskLevel.Critical, ApprovalDecisionOptionKind.Defer, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.Present, ApprovalCandidateActionImplication.None, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.CriticalRisk, "Critical risk blocks approval decision use.", proof),
            Fixture("approval.missing-evidence", ApprovalRiskLevel.Low, ApprovalDecisionOptionKind.Defer, ApprovalRiskDecisionEvidenceState.Missing, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.Present, ApprovalCandidateActionImplication.None, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.MissingEvidence, "Missing evidence blocks approval decision use.", proof),
            Fixture("approval.missing-context", ApprovalRiskLevel.Low, ApprovalDecisionOptionKind.Defer, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.Missing, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.Present, ApprovalCandidateActionImplication.None, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.MissingContext, "Missing context blocks approval decision use.", proof),
            Fixture("approval.stale-context", ApprovalRiskLevel.Low, ApprovalDecisionOptionKind.Defer, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.Stale, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.Present, ApprovalCandidateActionImplication.None, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.StaleContext, "Stale context blocks approval decision use.", proof),
            Fixture("approval.excluded-context", ApprovalRiskLevel.Low, ApprovalDecisionOptionKind.Defer, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.Excluded, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.Present, ApprovalCandidateActionImplication.None, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.ExcludedContext, "Excluded context blocks approval decision use.", proof),
            Fixture("approval.unresolved-contradiction", ApprovalRiskLevel.Low, ApprovalDecisionOptionKind.Defer, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.Unresolved, ApprovalRiskDecisionHumanReviewState.Present, ApprovalCandidateActionImplication.None, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.UnresolvedContradiction, "Unresolved contradiction blocks approval decision use.", proof),
            Fixture("approval.approve-critical-risk", ApprovalRiskLevel.Critical, ApprovalDecisionOptionKind.ApprovePreviewOnly, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.Present, ApprovalCandidateActionImplication.None, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.ApproveWithCriticalRisk, "Approve preview with critical risk is blocked.", proof),
            Fixture("approval.approve-without-evidence", ApprovalRiskLevel.Low, ApprovalDecisionOptionKind.ApprovePreviewOnly, ApprovalRiskDecisionEvidenceState.Missing, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.Present, ApprovalCandidateActionImplication.None, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.ApproveWithoutEvidence, "Approve preview without evidence is blocked.", proof),
            Fixture("approval.reject-preview", ApprovalRiskLevel.Low, ApprovalDecisionOptionKind.RejectPreviewOnly, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.NotRequired, ApprovalCandidateActionImplication.None, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.AllowedPreviewOnly, ApprovalRiskDecisionReadOnlyIssueKind.None, "", proof),
            Fixture("approval.request-more-evidence", ApprovalRiskLevel.Low, ApprovalDecisionOptionKind.RequestMoreEvidence, ApprovalRiskDecisionEvidenceState.Missing, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.NotRequired, ApprovalCandidateActionImplication.None, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.AllowedPreviewOnly, ApprovalRiskDecisionReadOnlyIssueKind.None, "", proof),
            Fixture("approval.request-context-refresh", ApprovalRiskLevel.Low, ApprovalDecisionOptionKind.RequestContextRefresh, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.Stale, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.NotRequired, ApprovalCandidateActionImplication.None, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.AllowedPreviewOnly, ApprovalRiskDecisionReadOnlyIssueKind.None, "", proof),
            Fixture("approval.defer-decision", ApprovalRiskLevel.Low, ApprovalDecisionOptionKind.Defer, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.NotRequired, ApprovalCandidateActionImplication.None, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.AllowedPreviewOnly, ApprovalRiskDecisionReadOnlyIssueKind.None, "", proof),
            Fixture("approval.provider-derived-disabled", ApprovalRiskLevel.Low, ApprovalDecisionOptionKind.Defer, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.Present, ApprovalCandidateActionImplication.ProviderCloud, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.ProviderCloudDerivedWhileDisabled, "Provider/cloud-derived rationale is blocked while provider/cloud remains disabled.", proof),
            Fixture("approval.semantic-derived-disabled", ApprovalRiskLevel.Low, ApprovalDecisionOptionKind.Defer, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.Present, ApprovalCandidateActionImplication.SemanticVector, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.SemanticVectorDerivedWhileDisabled, "Semantic/vector-derived rationale is blocked while semantic/vector remains disabled.", proof),
            Fixture("approval.llm-derived-disabled", ApprovalRiskLevel.Low, ApprovalDecisionOptionKind.Defer, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.Present, ApprovalCandidateActionImplication.LlmLive, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.LlmDerivedWhileDisabled, "LLM-derived rationale is blocked while LLM live remains disabled.", proof),
            Fixture("approval.action-filesystem-write", ApprovalRiskLevel.Low, ApprovalDecisionOptionKind.Defer, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.Present, ApprovalCandidateActionImplication.FilesystemWrite, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.FilesystemWriteImplication, "Candidate action implying filesystem write is blocked.", proof),
            Fixture("approval.action-runtime-live", ApprovalRiskLevel.Low, ApprovalDecisionOptionKind.Defer, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.Present, ApprovalCandidateActionImplication.RuntimeLive, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.RuntimeLiveImplication, "Candidate action implying runtime/live is blocked.", proof),
            Fixture("approval.action-state-mutation", ApprovalRiskLevel.Low, ApprovalDecisionOptionKind.Defer, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.Present, ApprovalCandidateActionImplication.ApprovalStateMutation, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.ApprovalStateMutationImplication, "Candidate action implying approval state mutation is blocked.", proof),
            Fixture("approval.option-product-action-count", ApprovalRiskLevel.Low, ApprovalDecisionOptionKind.ApprovePreviewOnly, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.Present, ApprovalCandidateActionImplication.None, fixtureOnly: false, productActions: 1, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.ProductActionCountNonZero, "Decision option with product action count greater than zero is blocked.", proof),
            Fixture("approval.option-state-mutation-count", ApprovalRiskLevel.Low, ApprovalDecisionOptionKind.ApprovePreviewOnly, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.Present, ApprovalCandidateActionImplication.None, fixtureOnly: false, productActions: 0, stateMutations: 1, ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.StateMutationCountNonZero, "Decision option with state mutation count greater than zero is blocked.", proof),
            Fixture("approval.raw-sensitive-payload", ApprovalRiskLevel.Critical, ApprovalDecisionOptionKind.Defer, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.Present, ApprovalCandidateActionImplication.RawSensitivePayload, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.Excluded, ApprovalRiskDecisionReadOnlyIssueKind.RawSensitivePayload, "Packet with raw or sensitive payload is excluded.", proof),
            Fixture("approval.fixture-only", ApprovalRiskLevel.Low, ApprovalDecisionOptionKind.Defer, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.NotRequired, ApprovalCandidateActionImplication.None, fixtureOnly: true, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.WarningPreviewOnly, ApprovalRiskDecisionReadOnlyIssueKind.FixtureOnlyNotProductionTrusted, "Fixture-only packet is not production trusted.", proof),
            Fixture("approval.conflicting-risk-summaries", ApprovalRiskLevel.Medium, ApprovalDecisionOptionKind.Defer, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.ConflictingRiskSummaries, ApprovalRiskDecisionHumanReviewState.Present, ApprovalCandidateActionImplication.None, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.ConflictingRiskSummaries, "Conflicting risk summaries block approval decision use.", proof),
            Fixture("approval.human-review-missing", ApprovalRiskLevel.Medium, ApprovalDecisionOptionKind.ApprovePreviewOnly, ApprovalRiskDecisionEvidenceState.Present, ApprovalRiskDecisionContextState.PresentFresh, ApprovalRiskDecisionContradictionState.None, ApprovalRiskDecisionHumanReviewState.RequiredMissing, ApprovalCandidateActionImplication.None, fixtureOnly: false, productActions: 0, stateMutations: 0, ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.HumanReviewMissing, "Human review is required but missing.", proof)
        ];
    }

    public static IReadOnlyList<ApprovalRiskDecisionReadOnlyResult> EvaluateCatalog() =>
        CreateFixtureCatalog().Select(Evaluate).ToList();

    public static ApprovalRiskDecisionReadOnlyResult Evaluate(ApprovalRiskDecisionReadOnlyFixture fixture)
    {
        var issues = new List<ApprovalRiskDecisionReadOnlyIssue>();

        AddActionImplicationIssues(fixture, issues);
        AddEvidenceContextIssues(fixture, issues);
        AddRiskContradictionIssues(fixture, issues);
        AddHumanReviewIssues(fixture, issues);
        AddFixtureOnlyIssue(fixture, issues);

        var decision = Decide(issues);
        var blockers = issues.Where(issue => !issue.WarningOnly).Select(issue => issue.Reason).ToList();
        var warnings = issues.Where(issue => issue.WarningOnly).Select(issue => issue.Reason).ToList();
        var blocksApprove = issues.Any(issue => issue.BlocksApprove);

        return new(
            FixtureId: fixture.FixtureId,
            Decision: decision,
            RiskLevel: fixture.RiskLevel,
            DecisionOptionKind: fixture.DecisionOptionKind,
            Issues: issues,
            Warnings: warnings,
            Blockers: blockers,
            PreviewOnly: true,
            AllowsDecisionOptionPreview: decision is ApprovalRiskDecisionReadOnlyDecision.AllowedPreviewOnly or ApprovalRiskDecisionReadOnlyDecision.NeedsHumanReview or ApprovalRiskDecisionReadOnlyDecision.WarningPreviewOnly,
            AllowsApprovePreview: fixture.DecisionOptionKind == ApprovalDecisionOptionKind.ApprovePreviewOnly && !blocksApprove && decision != ApprovalRiskDecisionReadOnlyDecision.Blocked && decision != ApprovalRiskDecisionReadOnlyDecision.Excluded,
            ApprovalExecutionAllowed: false,
            StateMutationAllowed: false,
            ProductActionAllowed: false,
            RequiresHumanReview: issues.Any(issue => issue.RequiresHumanReview) || fixture.HumanReviewState == ApprovalRiskDecisionHumanReviewState.Present,
            ProviderCloudDisabled: true,
            SemanticVectorDisabled: true,
            LlmLiveDisabled: true,
            ProductActionCount: fixture.ProductActionCount,
            StateMutationCount: fixture.StateMutationCount,
            NoSideEffectProof: fixture.NoSideEffectProof);
    }

    private static ApprovalRiskDecisionReadOnlyFixture Fixture(
        string id,
        ApprovalRiskLevel risk,
        ApprovalDecisionOptionKind option,
        ApprovalRiskDecisionEvidenceState evidence,
        ApprovalRiskDecisionContextState context,
        ApprovalRiskDecisionContradictionState contradiction,
        ApprovalRiskDecisionHumanReviewState humanReview,
        ApprovalCandidateActionImplication implication,
        bool fixtureOnly,
        int productActions,
        int stateMutations,
        ApprovalRiskDecisionReadOnlyDecision decision,
        ApprovalRiskDecisionReadOnlyIssueKind issue,
        string message,
        ApprovalReviewNoSideEffectProof proof) =>
        new(id, risk, option, evidence, context, contradiction, humanReview, implication, fixtureOnly, productActions, stateMutations, decision, issue, message, proof);

    private static void AddActionImplicationIssues(ApprovalRiskDecisionReadOnlyFixture fixture, List<ApprovalRiskDecisionReadOnlyIssue> issues)
    {
        if (fixture.ProductActionCount > 0)
        {
            issues.Add(Block(ApprovalRiskDecisionReadOnlyIssueKind.ProductActionCountNonZero, "Decision option with product action count greater than zero is blocked."));
        }

        if (fixture.StateMutationCount > 0)
        {
            issues.Add(Block(ApprovalRiskDecisionReadOnlyIssueKind.StateMutationCountNonZero, "Decision option with state mutation count greater than zero is blocked."));
        }

        switch (fixture.CandidateActionImplication)
        {
            case ApprovalCandidateActionImplication.ProviderCloud:
                issues.Add(Block(ApprovalRiskDecisionReadOnlyIssueKind.ProviderCloudDerivedWhileDisabled, "Provider/cloud-derived rationale is blocked while provider/cloud remains disabled."));
                break;
            case ApprovalCandidateActionImplication.SemanticVector:
                issues.Add(Block(ApprovalRiskDecisionReadOnlyIssueKind.SemanticVectorDerivedWhileDisabled, "Semantic/vector-derived rationale is blocked while semantic/vector remains disabled."));
                break;
            case ApprovalCandidateActionImplication.LlmLive:
                issues.Add(Block(ApprovalRiskDecisionReadOnlyIssueKind.LlmDerivedWhileDisabled, "LLM-derived rationale is blocked while LLM live remains disabled."));
                break;
            case ApprovalCandidateActionImplication.FilesystemWrite:
                issues.Add(Block(ApprovalRiskDecisionReadOnlyIssueKind.FilesystemWriteImplication, "Candidate action implying filesystem write is blocked."));
                break;
            case ApprovalCandidateActionImplication.RuntimeLive:
                issues.Add(Block(ApprovalRiskDecisionReadOnlyIssueKind.RuntimeLiveImplication, "Candidate action implying runtime/live is blocked."));
                break;
            case ApprovalCandidateActionImplication.ApprovalStateMutation:
                issues.Add(Block(ApprovalRiskDecisionReadOnlyIssueKind.ApprovalStateMutationImplication, "Candidate action implying approval state mutation is blocked."));
                break;
            case ApprovalCandidateActionImplication.RawSensitivePayload:
                issues.Add(Exclude(ApprovalRiskDecisionReadOnlyIssueKind.RawSensitivePayload, "Packet with raw or sensitive payload is excluded."));
                break;
        }
    }

    private static void AddEvidenceContextIssues(ApprovalRiskDecisionReadOnlyFixture fixture, List<ApprovalRiskDecisionReadOnlyIssue> issues)
    {
        if (fixture.EvidenceState == ApprovalRiskDecisionEvidenceState.Missing && fixture.DecisionOptionKind != ApprovalDecisionOptionKind.RequestMoreEvidence)
        {
            issues.Add(Block(
                fixture.DecisionOptionKind == ApprovalDecisionOptionKind.ApprovePreviewOnly
                    ? ApprovalRiskDecisionReadOnlyIssueKind.ApproveWithoutEvidence
                    : ApprovalRiskDecisionReadOnlyIssueKind.MissingEvidence,
                fixture.DecisionOptionKind == ApprovalDecisionOptionKind.ApprovePreviewOnly
                    ? "Approve preview without evidence is blocked."
                    : "Missing evidence blocks approval decision use."));
        }

        if (fixture.ContextState == ApprovalRiskDecisionContextState.Missing && fixture.DecisionOptionKind != ApprovalDecisionOptionKind.RequestContextRefresh)
        {
            issues.Add(Block(ApprovalRiskDecisionReadOnlyIssueKind.MissingContext, "Missing context blocks approval decision use."));
        }

        if (fixture.ContextState == ApprovalRiskDecisionContextState.Stale && fixture.DecisionOptionKind != ApprovalDecisionOptionKind.RequestContextRefresh)
        {
            issues.Add(Block(ApprovalRiskDecisionReadOnlyIssueKind.StaleContext, "Stale context blocks approval decision use."));
        }

        if (fixture.ContextState == ApprovalRiskDecisionContextState.Excluded)
        {
            // Phase E treats excluded context as a structural blocker, not as payload exclusion.
            issues.Add(Block(ApprovalRiskDecisionReadOnlyIssueKind.ExcludedContext, "Excluded context blocks approval decision use."));
        }
    }

    private static void AddRiskContradictionIssues(ApprovalRiskDecisionReadOnlyFixture fixture, List<ApprovalRiskDecisionReadOnlyIssue> issues)
    {
        if (fixture.RiskLevel == ApprovalRiskLevel.Critical)
        {
            issues.Add(Block(
                fixture.DecisionOptionKind == ApprovalDecisionOptionKind.ApprovePreviewOnly
                    ? ApprovalRiskDecisionReadOnlyIssueKind.ApproveWithCriticalRisk
                    : ApprovalRiskDecisionReadOnlyIssueKind.CriticalRisk,
                fixture.DecisionOptionKind == ApprovalDecisionOptionKind.ApprovePreviewOnly
                    ? "Approve preview with critical risk is blocked."
                    : "Critical risk blocks approval decision use."));
        }

        if (fixture.ContradictionState == ApprovalRiskDecisionContradictionState.Unresolved)
        {
            issues.Add(Block(ApprovalRiskDecisionReadOnlyIssueKind.UnresolvedContradiction, "Unresolved contradiction blocks approval decision use."));
        }

        if (fixture.ContradictionState == ApprovalRiskDecisionContradictionState.ConflictingRiskSummaries)
        {
            issues.Add(Block(ApprovalRiskDecisionReadOnlyIssueKind.ConflictingRiskSummaries, "Conflicting risk summaries block approval decision use."));
        }
    }

    private static void AddHumanReviewIssues(ApprovalRiskDecisionReadOnlyFixture fixture, List<ApprovalRiskDecisionReadOnlyIssue> issues)
    {
        if (fixture.HumanReviewState == ApprovalRiskDecisionHumanReviewState.RequiredMissing)
        {
            issues.Add(Block(ApprovalRiskDecisionReadOnlyIssueKind.HumanReviewMissing, "Human review is required but missing."));
        }
        else if (fixture.RiskLevel == ApprovalRiskLevel.Medium && fixture.HumanReviewState == ApprovalRiskDecisionHumanReviewState.Present)
        {
            issues.Add(Warning(ApprovalRiskDecisionReadOnlyIssueKind.HumanReviewRequired, "Medium risk remains preview-only and requires human review.", requiresHumanReview: true));
        }
    }

    private static void AddFixtureOnlyIssue(ApprovalRiskDecisionReadOnlyFixture fixture, List<ApprovalRiskDecisionReadOnlyIssue> issues)
    {
        if (fixture.FixtureOnly)
        {
            issues.Add(Warning(ApprovalRiskDecisionReadOnlyIssueKind.FixtureOnlyNotProductionTrusted, "Fixture-only packet is not production trusted.", requiresHumanReview: false));
        }
    }

    private static ApprovalRiskDecisionReadOnlyDecision Decide(IReadOnlyList<ApprovalRiskDecisionReadOnlyIssue> issues)
    {
        if (issues.Any(issue => issue.Kind == ApprovalRiskDecisionReadOnlyIssueKind.RawSensitivePayload))
            return ApprovalRiskDecisionReadOnlyDecision.Excluded;

        if (issues.Any(issue => !issue.WarningOnly))
            return ApprovalRiskDecisionReadOnlyDecision.Blocked;

        if (issues.Any(issue => issue.Kind == ApprovalRiskDecisionReadOnlyIssueKind.HumanReviewRequired))
            return ApprovalRiskDecisionReadOnlyDecision.NeedsHumanReview;

        if (issues.Any(issue => issue.WarningOnly))
            return ApprovalRiskDecisionReadOnlyDecision.WarningPreviewOnly;

        return ApprovalRiskDecisionReadOnlyDecision.AllowedPreviewOnly;
    }

    private static ApprovalRiskDecisionReadOnlyIssue Block(ApprovalRiskDecisionReadOnlyIssueKind kind, string reason) =>
        new(kind, reason, BlocksApprove: true, BlocksSafeNextStep: true, RequiresHumanReview: true, WarningOnly: false);

    private static ApprovalRiskDecisionReadOnlyIssue Exclude(ApprovalRiskDecisionReadOnlyIssueKind kind, string reason) =>
        new(kind, reason, BlocksApprove: true, BlocksSafeNextStep: true, RequiresHumanReview: true, WarningOnly: false);

    private static ApprovalRiskDecisionReadOnlyIssue Warning(ApprovalRiskDecisionReadOnlyIssueKind kind, string reason, bool requiresHumanReview) =>
        new(kind, reason, BlocksApprove: false, BlocksSafeNextStep: false, RequiresHumanReview: requiresHumanReview, WarningOnly: true);
}
