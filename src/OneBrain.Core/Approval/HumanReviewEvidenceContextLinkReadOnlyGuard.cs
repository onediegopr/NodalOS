namespace OneBrain.Core.Approval;

public enum HumanReviewEvidenceContextLinkDecision
{
    AllowedPreviewOnly,
    NeedsHumanReview,
    WarningPreviewOnly,
    Blocked,
    Excluded
}

public enum HumanReviewEvidenceContextLinkIssueKind
{
    None,
    MissingEvidenceLink,
    MissingContextLink,
    FixtureOnlyEvidenceNotProductionTrusted,
    StaleContextLink,
    ExcludedContextLink,
    LockedContextWithoutReview,
    ContextRequiresHumanReview,
    UnresolvedContradictionLink,
    CriticalRiskLink,
    RawPayloadLink,
    SecretLikeLink,
    ProviderCloudDerivedWhileDisabled,
    SemanticVectorDerivedWhileDisabled,
    LlmDerivedWhileDisabled,
    DisabledPersistenceStoreLink,
    DurableMemoryWhileDisabled,
    EvidenceContextMismatch,
    MissingEvidenceConfidence,
    UnknownContextAuthority,
    MissingContextFreshness,
    InvalidDecisionOptionLink,
    InvalidCandidateActionLink,
    InvalidSafeNextStepLink,
    DuplicateConflictingSourceKind,
    ProductActionCountNonZero,
    StateMutationCountNonZero
}

public enum HumanReviewEvidenceLinkState
{
    Present,
    Missing,
    FixtureOnly,
    RawPayload,
    SecretLike,
    DisabledPersistenceStore,
    ProviderCloudDerived,
    ConfidenceMissing
}

public enum HumanReviewContextLinkState
{
    PresentFresh,
    Missing,
    Stale,
    Excluded,
    LockedWithoutReview,
    RequiresHumanReview,
    UnknownAuthority,
    MissingFreshness,
    SemanticVectorDerived,
    LlmDerived,
    DurableMemory,
    SecretLike
}

public enum HumanReviewLinkSourceKind
{
    PhaseCEvidenceReadOnly,
    PhaseDContextReadOnly,
    FixtureOnly,
    DisabledPersistenceStore,
    ProviderCloudDerived,
    SemanticVectorDerived,
    LlmDerived,
    DurableMemory,
    RawOrSecretPayload
}

public enum HumanReviewLinkRiskContradictionState
{
    None,
    UnresolvedContradiction,
    CriticalRisk
}

public enum HumanReviewLinkConfidenceState
{
    Present,
    Missing
}

public enum HumanReviewLinkHumanReviewState
{
    NotRequired,
    RequiredPresent,
    RequiredMissing
}

public enum HumanReviewLinkUsageKind
{
    ReviewPacket,
    DecisionOption,
    CandidateAction,
    SafeNextStep
}

public sealed record HumanReviewEvidenceContextLinkFixture(
    string FixtureId,
    HumanReviewEvidenceLinkState EvidenceLinkState,
    HumanReviewContextLinkState ContextLinkState,
    HumanReviewLinkSourceKind SourceKind,
    HumanReviewLinkRiskContradictionState RiskContradictionState,
    HumanReviewLinkConfidenceState ConfidenceState,
    HumanReviewLinkHumanReviewState HumanReviewState,
    HumanReviewLinkUsageKind UsageKind,
    bool EvidenceContextMatched,
    bool DuplicateConflictingSourceKind,
    int ProductActionCount,
    int StateMutationCount,
    HumanReviewEvidenceContextLinkDecision ExpectedDecision,
    HumanReviewEvidenceContextLinkIssueKind ExpectedIssue,
    string ExpectedBlockerOrWarning,
    ApprovalReviewNoSideEffectProof NoSideEffectProof);

public sealed record HumanReviewEvidenceContextLinkIssue(
    HumanReviewEvidenceContextLinkIssueKind Kind,
    string Reason,
    bool BlocksApprovalPreview,
    bool BlocksDecisionInfluence,
    bool BlocksSafeNextStep,
    bool RequiresHumanReview,
    bool WarningOnly);

public sealed record HumanReviewEvidenceContextLinkResult(
    string FixtureId,
    HumanReviewEvidenceContextLinkDecision Decision,
    HumanReviewEvidenceLinkState EvidenceLinkState,
    HumanReviewContextLinkState ContextLinkState,
    HumanReviewLinkSourceKind SourceKind,
    HumanReviewLinkUsageKind UsageKind,
    IReadOnlyList<HumanReviewEvidenceContextLinkIssue> Issues,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    bool PreviewOnly,
    bool EvidenceLinkIsDurableEvidence,
    bool ContextLinkTrustedByDefault,
    bool AllowsApprovalPreview,
    bool AllowsDecisionInfluence,
    bool AllowsSafeNextStep,
    bool ApprovalExecutionAllowed,
    bool StateMutationAllowed,
    bool ProductActionAllowed,
    bool ServiceRegistrationAllowed,
    bool ProviderCloudDisabled,
    bool SemanticVectorDisabled,
    bool LlmLiveDisabled,
    int ProductActionCount,
    int StateMutationCount,
    ApprovalReviewNoSideEffectProof NoSideEffectProof)
{
    public bool Blocked => Decision is HumanReviewEvidenceContextLinkDecision.Blocked or HumanReviewEvidenceContextLinkDecision.Excluded;

    public bool HasIssue(HumanReviewEvidenceContextLinkIssueKind kind) =>
        Issues.Any(issue => issue.Kind == kind);
}

public static class HumanReviewEvidenceContextLinkReadOnlyGuard
{
    public static IReadOnlyList<HumanReviewEvidenceContextLinkFixture> CreateFixtureCatalog()
    {
        var proof = ApprovalReviewNoSideEffectProof.FixtureReadOnly();

        return
        [
            Fixture("link.valid-evidence-context", HumanReviewEvidenceLinkState.Present, HumanReviewContextLinkState.PresentFresh, HumanReviewLinkSourceKind.PhaseCEvidenceReadOnly, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.AllowedPreviewOnly, HumanReviewEvidenceContextLinkIssueKind.None, "", proof),
            Fixture("link.missing-evidence", HumanReviewEvidenceLinkState.Missing, HumanReviewContextLinkState.PresentFresh, HumanReviewLinkSourceKind.PhaseCEvidenceReadOnly, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.MissingEvidenceLink, "Missing evidence link blocks human review preview.", proof),
            Fixture("link.missing-context", HumanReviewEvidenceLinkState.Present, HumanReviewContextLinkState.Missing, HumanReviewLinkSourceKind.PhaseDContextReadOnly, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.MissingContextLink, "Missing context link blocks human review preview.", proof),
            Fixture("link.fixture-only-evidence", HumanReviewEvidenceLinkState.FixtureOnly, HumanReviewContextLinkState.PresentFresh, HumanReviewLinkSourceKind.FixtureOnly, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.WarningPreviewOnly, HumanReviewEvidenceContextLinkIssueKind.FixtureOnlyEvidenceNotProductionTrusted, "Fixture-only evidence link is warning-only and not production trusted.", proof),
            Fixture("link.stale-context", HumanReviewEvidenceLinkState.Present, HumanReviewContextLinkState.Stale, HumanReviewLinkSourceKind.PhaseDContextReadOnly, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.StaleContextLink, "Stale context link blocks human review preview.", proof),
            Fixture("link.excluded-context", HumanReviewEvidenceLinkState.Present, HumanReviewContextLinkState.Excluded, HumanReviewLinkSourceKind.PhaseDContextReadOnly, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Excluded, HumanReviewEvidenceContextLinkIssueKind.ExcludedContextLink, "Excluded context link is excluded from human review preview.", proof),
            Fixture("link.locked-context-no-review", HumanReviewEvidenceLinkState.Present, HumanReviewContextLinkState.LockedWithoutReview, HumanReviewLinkSourceKind.PhaseDContextReadOnly, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.RequiredMissing, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.LockedContextWithoutReview, "Locked context without review blocks human review preview.", proof),
            Fixture("link.context-requires-human-review", HumanReviewEvidenceLinkState.Present, HumanReviewContextLinkState.RequiresHumanReview, HumanReviewLinkSourceKind.PhaseDContextReadOnly, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.RequiredPresent, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.NeedsHumanReview, HumanReviewEvidenceContextLinkIssueKind.ContextRequiresHumanReview, "Context link remains preview-only and requires human review.", proof),
            Fixture("link.unresolved-contradiction", HumanReviewEvidenceLinkState.Present, HumanReviewContextLinkState.PresentFresh, HumanReviewLinkSourceKind.PhaseCEvidenceReadOnly, HumanReviewLinkRiskContradictionState.UnresolvedContradiction, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.RequiredPresent, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.UnresolvedContradictionLink, "Unresolved contradiction link blocks human review preview.", proof),
            Fixture("link.critical-risk", HumanReviewEvidenceLinkState.Present, HumanReviewContextLinkState.PresentFresh, HumanReviewLinkSourceKind.PhaseCEvidenceReadOnly, HumanReviewLinkRiskContradictionState.CriticalRisk, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.RequiredPresent, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.CriticalRiskLink, "Critical risk link blocks human review preview.", proof),
            Fixture("link.raw-payload-evidence", HumanReviewEvidenceLinkState.RawPayload, HumanReviewContextLinkState.PresentFresh, HumanReviewLinkSourceKind.RawOrSecretPayload, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Excluded, HumanReviewEvidenceContextLinkIssueKind.RawPayloadLink, "Raw payload link is excluded.", proof),
            Fixture("link.secret-like", HumanReviewEvidenceLinkState.SecretLike, HumanReviewContextLinkState.SecretLike, HumanReviewLinkSourceKind.RawOrSecretPayload, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Excluded, HumanReviewEvidenceContextLinkIssueKind.SecretLikeLink, "Secret-like link is excluded.", proof),
            Fixture("link.provider-cloud-evidence-disabled", HumanReviewEvidenceLinkState.ProviderCloudDerived, HumanReviewContextLinkState.PresentFresh, HumanReviewLinkSourceKind.ProviderCloudDerived, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.ProviderCloudDerivedWhileDisabled, "Provider/cloud-derived evidence link blocks while provider/cloud is disabled.", proof),
            Fixture("link.semantic-context-disabled", HumanReviewEvidenceLinkState.Present, HumanReviewContextLinkState.SemanticVectorDerived, HumanReviewLinkSourceKind.SemanticVectorDerived, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.SemanticVectorDerivedWhileDisabled, "Semantic/vector-derived context link blocks while semantic/vector is disabled.", proof),
            Fixture("link.llm-rationale-disabled", HumanReviewEvidenceLinkState.Present, HumanReviewContextLinkState.LlmDerived, HumanReviewLinkSourceKind.LlmDerived, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.LlmDerivedWhileDisabled, "LLM-derived rationale link blocks while LLM live is disabled.", proof),
            Fixture("link.disabled-persistence-store", HumanReviewEvidenceLinkState.DisabledPersistenceStore, HumanReviewContextLinkState.PresentFresh, HumanReviewLinkSourceKind.DisabledPersistenceStore, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.DisabledPersistenceStoreLink, "Evidence link to disabled persistence store blocks.", proof),
            Fixture("link.durable-memory-disabled", HumanReviewEvidenceLinkState.Present, HumanReviewContextLinkState.DurableMemory, HumanReviewLinkSourceKind.DurableMemory, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.DurableMemoryWhileDisabled, "Context link to durable memory blocks while durable memory is disabled.", proof),
            Fixture("link.evidence-context-mismatch", HumanReviewEvidenceLinkState.Present, HumanReviewContextLinkState.PresentFresh, HumanReviewLinkSourceKind.PhaseCEvidenceReadOnly, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.ReviewPacket, matched: false, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.EvidenceContextMismatch, "Evidence/context link mismatch blocks human review preview.", proof),
            Fixture("link.missing-confidence", HumanReviewEvidenceLinkState.ConfidenceMissing, HumanReviewContextLinkState.PresentFresh, HumanReviewLinkSourceKind.PhaseCEvidenceReadOnly, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Missing, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.MissingEvidenceConfidence, "Missing evidence confidence blocks human review preview.", proof),
            Fixture("link.unknown-context-authority", HumanReviewEvidenceLinkState.Present, HumanReviewContextLinkState.UnknownAuthority, HumanReviewLinkSourceKind.PhaseDContextReadOnly, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.UnknownContextAuthority, "Unknown context authority blocks human review preview.", proof),
            Fixture("link.missing-context-freshness", HumanReviewEvidenceLinkState.Present, HumanReviewContextLinkState.MissingFreshness, HumanReviewLinkSourceKind.PhaseDContextReadOnly, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.MissingContextFreshness, "Missing context freshness blocks human review preview.", proof),
            Fixture("link.invalid-decision-option", HumanReviewEvidenceLinkState.Missing, HumanReviewContextLinkState.PresentFresh, HumanReviewLinkSourceKind.PhaseCEvidenceReadOnly, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.DecisionOption, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.InvalidDecisionOptionLink, "Decision option using invalid link blocks.", proof),
            Fixture("link.invalid-candidate-action", HumanReviewEvidenceLinkState.Present, HumanReviewContextLinkState.Excluded, HumanReviewLinkSourceKind.PhaseDContextReadOnly, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.CandidateAction, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.InvalidCandidateActionLink, "Candidate action using invalid link blocks.", proof),
            Fixture("link.invalid-safe-next-step", HumanReviewEvidenceLinkState.Present, HumanReviewContextLinkState.Stale, HumanReviewLinkSourceKind.PhaseDContextReadOnly, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.SafeNextStep, matched: true, duplicate: false, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.InvalidSafeNextStepLink, "Safe next step using invalid link blocks.", proof),
            Fixture("link.duplicate-conflicting-source", HumanReviewEvidenceLinkState.Present, HumanReviewContextLinkState.PresentFresh, HumanReviewLinkSourceKind.PhaseCEvidenceReadOnly, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: true, productActions: 0, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.DuplicateConflictingSourceKind, "Duplicate links with conflicting source kind block.", proof),
            Fixture("link.product-action-count", HumanReviewEvidenceLinkState.Present, HumanReviewContextLinkState.PresentFresh, HumanReviewLinkSourceKind.PhaseCEvidenceReadOnly, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 1, stateMutations: 0, HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.ProductActionCountNonZero, "Product action count greater than zero blocks.", proof),
            Fixture("link.state-mutation-count", HumanReviewEvidenceLinkState.Present, HumanReviewContextLinkState.PresentFresh, HumanReviewLinkSourceKind.PhaseCEvidenceReadOnly, HumanReviewLinkRiskContradictionState.None, HumanReviewLinkConfidenceState.Present, HumanReviewLinkHumanReviewState.NotRequired, HumanReviewLinkUsageKind.ReviewPacket, matched: true, duplicate: false, productActions: 0, stateMutations: 1, HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.StateMutationCountNonZero, "State mutation count greater than zero blocks.", proof)
        ];
    }

    public static IReadOnlyList<HumanReviewEvidenceContextLinkResult> EvaluateCatalog() =>
        CreateFixtureCatalog().Select(Evaluate).ToList();

    public static HumanReviewEvidenceContextLinkResult Evaluate(HumanReviewEvidenceContextLinkFixture fixture)
    {
        var issues = new List<HumanReviewEvidenceContextLinkIssue>();

        AddActionIssues(fixture, issues);
        AddEvidenceIssues(fixture, issues);
        AddContextIssues(fixture, issues);
        AddSourceIssues(fixture, issues);
        AddRiskContradictionIssues(fixture, issues);
        AddUsageIssues(fixture, issues);
        AddRelationshipIssues(fixture, issues);
        AddHumanReviewIssues(fixture, issues);

        var decision = Decide(issues);
        var blockers = issues.Where(issue => !issue.WarningOnly).Select(issue => issue.Reason).ToList();
        var warnings = issues.Where(issue => issue.WarningOnly).Select(issue => issue.Reason).ToList();

        return new(
            FixtureId: fixture.FixtureId,
            Decision: decision,
            EvidenceLinkState: fixture.EvidenceLinkState,
            ContextLinkState: fixture.ContextLinkState,
            SourceKind: fixture.SourceKind,
            UsageKind: fixture.UsageKind,
            Issues: issues,
            Warnings: warnings,
            Blockers: blockers,
            PreviewOnly: true,
            EvidenceLinkIsDurableEvidence: false,
            ContextLinkTrustedByDefault: false,
            AllowsApprovalPreview: decision is HumanReviewEvidenceContextLinkDecision.AllowedPreviewOnly or HumanReviewEvidenceContextLinkDecision.NeedsHumanReview or HumanReviewEvidenceContextLinkDecision.WarningPreviewOnly,
            AllowsDecisionInfluence: decision == HumanReviewEvidenceContextLinkDecision.AllowedPreviewOnly,
            AllowsSafeNextStep: decision == HumanReviewEvidenceContextLinkDecision.AllowedPreviewOnly,
            ApprovalExecutionAllowed: false,
            StateMutationAllowed: false,
            ProductActionAllowed: false,
            ServiceRegistrationAllowed: false,
            ProviderCloudDisabled: true,
            SemanticVectorDisabled: true,
            LlmLiveDisabled: true,
            ProductActionCount: fixture.ProductActionCount,
            StateMutationCount: fixture.StateMutationCount,
            NoSideEffectProof: fixture.NoSideEffectProof);
    }

    private static HumanReviewEvidenceContextLinkFixture Fixture(
        string id,
        HumanReviewEvidenceLinkState evidence,
        HumanReviewContextLinkState context,
        HumanReviewLinkSourceKind source,
        HumanReviewLinkRiskContradictionState risk,
        HumanReviewLinkConfidenceState confidence,
        HumanReviewLinkHumanReviewState humanReview,
        HumanReviewLinkUsageKind usage,
        bool matched,
        bool duplicate,
        int productActions,
        int stateMutations,
        HumanReviewEvidenceContextLinkDecision decision,
        HumanReviewEvidenceContextLinkIssueKind issue,
        string message,
        ApprovalReviewNoSideEffectProof proof) =>
        new(id, evidence, context, source, risk, confidence, humanReview, usage, matched, duplicate, productActions, stateMutations, decision, issue, message, proof);

    private static void AddActionIssues(HumanReviewEvidenceContextLinkFixture fixture, List<HumanReviewEvidenceContextLinkIssue> issues)
    {
        if (fixture.ProductActionCount > 0)
        {
            issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.ProductActionCountNonZero, "Product action count greater than zero blocks."));
        }

        if (fixture.StateMutationCount > 0)
        {
            issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.StateMutationCountNonZero, "State mutation count greater than zero blocks."));
        }
    }

    private static void AddEvidenceIssues(HumanReviewEvidenceContextLinkFixture fixture, List<HumanReviewEvidenceContextLinkIssue> issues)
    {
        switch (fixture.EvidenceLinkState)
        {
            case HumanReviewEvidenceLinkState.Missing:
                issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.MissingEvidenceLink, "Missing evidence link blocks human review preview."));
                break;
            case HumanReviewEvidenceLinkState.FixtureOnly:
                issues.Add(Warning(HumanReviewEvidenceContextLinkIssueKind.FixtureOnlyEvidenceNotProductionTrusted, "Fixture-only evidence link is warning-only and not production trusted.", requiresHumanReview: false));
                break;
            case HumanReviewEvidenceLinkState.RawPayload:
                issues.Add(Exclude(HumanReviewEvidenceContextLinkIssueKind.RawPayloadLink, "Raw payload link is excluded."));
                break;
            case HumanReviewEvidenceLinkState.SecretLike:
                issues.Add(Exclude(HumanReviewEvidenceContextLinkIssueKind.SecretLikeLink, "Secret-like link is excluded."));
                break;
            case HumanReviewEvidenceLinkState.DisabledPersistenceStore:
                issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.DisabledPersistenceStoreLink, "Evidence link to disabled persistence store blocks."));
                break;
            case HumanReviewEvidenceLinkState.ProviderCloudDerived:
                issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.ProviderCloudDerivedWhileDisabled, "Provider/cloud-derived evidence link blocks while provider/cloud is disabled."));
                break;
            case HumanReviewEvidenceLinkState.ConfidenceMissing:
                issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.MissingEvidenceConfidence, "Missing evidence confidence blocks human review preview."));
                break;
        }

        if (fixture.ConfidenceState == HumanReviewLinkConfidenceState.Missing && !issues.Any(issue => issue.Kind == HumanReviewEvidenceContextLinkIssueKind.MissingEvidenceConfidence))
        {
            issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.MissingEvidenceConfidence, "Missing evidence confidence blocks human review preview."));
        }
    }

    private static void AddContextIssues(HumanReviewEvidenceContextLinkFixture fixture, List<HumanReviewEvidenceContextLinkIssue> issues)
    {
        switch (fixture.ContextLinkState)
        {
            case HumanReviewContextLinkState.Missing:
                issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.MissingContextLink, "Missing context link blocks human review preview."));
                break;
            case HumanReviewContextLinkState.Stale:
                issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.StaleContextLink, "Stale context link blocks human review preview."));
                break;
            case HumanReviewContextLinkState.Excluded:
                issues.Add(Exclude(HumanReviewEvidenceContextLinkIssueKind.ExcludedContextLink, "Excluded context link is excluded from human review preview."));
                break;
            case HumanReviewContextLinkState.LockedWithoutReview:
                issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.LockedContextWithoutReview, "Locked context without review blocks human review preview."));
                break;
            case HumanReviewContextLinkState.RequiresHumanReview:
                issues.Add(Warning(HumanReviewEvidenceContextLinkIssueKind.ContextRequiresHumanReview, "Context link remains preview-only and requires human review.", requiresHumanReview: true));
                break;
            case HumanReviewContextLinkState.UnknownAuthority:
                issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.UnknownContextAuthority, "Unknown context authority blocks human review preview."));
                break;
            case HumanReviewContextLinkState.MissingFreshness:
                issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.MissingContextFreshness, "Missing context freshness blocks human review preview."));
                break;
            case HumanReviewContextLinkState.SemanticVectorDerived:
                issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.SemanticVectorDerivedWhileDisabled, "Semantic/vector-derived context link blocks while semantic/vector is disabled."));
                break;
            case HumanReviewContextLinkState.LlmDerived:
                issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.LlmDerivedWhileDisabled, "LLM-derived rationale link blocks while LLM live is disabled."));
                break;
            case HumanReviewContextLinkState.DurableMemory:
                issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.DurableMemoryWhileDisabled, "Context link to durable memory blocks while durable memory is disabled."));
                break;
            case HumanReviewContextLinkState.SecretLike:
                issues.Add(Exclude(HumanReviewEvidenceContextLinkIssueKind.SecretLikeLink, "Secret-like link is excluded."));
                break;
        }
    }

    private static void AddSourceIssues(HumanReviewEvidenceContextLinkFixture fixture, List<HumanReviewEvidenceContextLinkIssue> issues)
    {
        switch (fixture.SourceKind)
        {
            case HumanReviewLinkSourceKind.ProviderCloudDerived when !issues.Any(issue => issue.Kind == HumanReviewEvidenceContextLinkIssueKind.ProviderCloudDerivedWhileDisabled):
                issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.ProviderCloudDerivedWhileDisabled, "Provider/cloud-derived evidence link blocks while provider/cloud is disabled."));
                break;
            case HumanReviewLinkSourceKind.SemanticVectorDerived when !issues.Any(issue => issue.Kind == HumanReviewEvidenceContextLinkIssueKind.SemanticVectorDerivedWhileDisabled):
                issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.SemanticVectorDerivedWhileDisabled, "Semantic/vector-derived context link blocks while semantic/vector is disabled."));
                break;
            case HumanReviewLinkSourceKind.LlmDerived when !issues.Any(issue => issue.Kind == HumanReviewEvidenceContextLinkIssueKind.LlmDerivedWhileDisabled):
                issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.LlmDerivedWhileDisabled, "LLM-derived rationale link blocks while LLM live is disabled."));
                break;
            case HumanReviewLinkSourceKind.DisabledPersistenceStore when !issues.Any(issue => issue.Kind == HumanReviewEvidenceContextLinkIssueKind.DisabledPersistenceStoreLink):
                issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.DisabledPersistenceStoreLink, "Evidence link to disabled persistence store blocks."));
                break;
            case HumanReviewLinkSourceKind.DurableMemory when !issues.Any(issue => issue.Kind == HumanReviewEvidenceContextLinkIssueKind.DurableMemoryWhileDisabled):
                issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.DurableMemoryWhileDisabled, "Context link to durable memory blocks while durable memory is disabled."));
                break;
            case HumanReviewLinkSourceKind.RawOrSecretPayload when !issues.Any(issue => issue.Kind is HumanReviewEvidenceContextLinkIssueKind.RawPayloadLink or HumanReviewEvidenceContextLinkIssueKind.SecretLikeLink):
                issues.Add(Exclude(HumanReviewEvidenceContextLinkIssueKind.RawPayloadLink, "Raw payload link is excluded."));
                break;
        }
    }

    private static void AddRiskContradictionIssues(HumanReviewEvidenceContextLinkFixture fixture, List<HumanReviewEvidenceContextLinkIssue> issues)
    {
        if (fixture.RiskContradictionState == HumanReviewLinkRiskContradictionState.UnresolvedContradiction)
        {
            issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.UnresolvedContradictionLink, "Unresolved contradiction link blocks human review preview."));
        }

        if (fixture.RiskContradictionState == HumanReviewLinkRiskContradictionState.CriticalRisk)
        {
            issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.CriticalRiskLink, "Critical risk link blocks human review preview."));
        }
    }

    private static void AddUsageIssues(HumanReviewEvidenceContextLinkFixture fixture, List<HumanReviewEvidenceContextLinkIssue> issues)
    {
        if (!HasBlockingIssue(issues))
            return;

        var usageIssue = fixture.UsageKind switch
        {
            HumanReviewLinkUsageKind.DecisionOption => HumanReviewEvidenceContextLinkIssueKind.InvalidDecisionOptionLink,
            HumanReviewLinkUsageKind.CandidateAction => HumanReviewEvidenceContextLinkIssueKind.InvalidCandidateActionLink,
            HumanReviewLinkUsageKind.SafeNextStep => HumanReviewEvidenceContextLinkIssueKind.InvalidSafeNextStepLink,
            _ => HumanReviewEvidenceContextLinkIssueKind.None
        };

        if (usageIssue != HumanReviewEvidenceContextLinkIssueKind.None)
        {
            var reason = fixture.UsageKind switch
            {
                HumanReviewLinkUsageKind.DecisionOption => "Decision option using invalid link blocks.",
                HumanReviewLinkUsageKind.CandidateAction => "Candidate action using invalid link blocks.",
                HumanReviewLinkUsageKind.SafeNextStep => "Safe next step using invalid link blocks.",
                _ => ""
            };

            issues.Clear();
            issues.Add(Block(usageIssue, reason));
        }
    }

    private static void AddRelationshipIssues(HumanReviewEvidenceContextLinkFixture fixture, List<HumanReviewEvidenceContextLinkIssue> issues)
    {
        if (!fixture.EvidenceContextMatched)
        {
            issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.EvidenceContextMismatch, "Evidence/context link mismatch blocks human review preview."));
        }

        if (fixture.DuplicateConflictingSourceKind)
        {
            issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.DuplicateConflictingSourceKind, "Duplicate links with conflicting source kind block."));
        }
    }

    private static void AddHumanReviewIssues(HumanReviewEvidenceContextLinkFixture fixture, List<HumanReviewEvidenceContextLinkIssue> issues)
    {
        if (fixture.HumanReviewState == HumanReviewLinkHumanReviewState.RequiredMissing && !HasBlockingIssue(issues))
        {
            issues.Add(Block(HumanReviewEvidenceContextLinkIssueKind.LockedContextWithoutReview, "Locked context without review blocks human review preview."));
        }
    }

    private static HumanReviewEvidenceContextLinkDecision Decide(IReadOnlyList<HumanReviewEvidenceContextLinkIssue> issues)
    {
        if (issues.Any(issue => issue.Kind is HumanReviewEvidenceContextLinkIssueKind.RawPayloadLink or HumanReviewEvidenceContextLinkIssueKind.SecretLikeLink or HumanReviewEvidenceContextLinkIssueKind.ExcludedContextLink))
            return HumanReviewEvidenceContextLinkDecision.Excluded;

        if (issues.Any(issue => !issue.WarningOnly))
            return HumanReviewEvidenceContextLinkDecision.Blocked;

        if (issues.Any(issue => issue.Kind == HumanReviewEvidenceContextLinkIssueKind.ContextRequiresHumanReview))
            return HumanReviewEvidenceContextLinkDecision.NeedsHumanReview;

        if (issues.Any(issue => issue.WarningOnly))
            return HumanReviewEvidenceContextLinkDecision.WarningPreviewOnly;

        return HumanReviewEvidenceContextLinkDecision.AllowedPreviewOnly;
    }

    private static bool HasBlockingIssue(IEnumerable<HumanReviewEvidenceContextLinkIssue> issues) =>
        issues.Any(issue => !issue.WarningOnly);

    private static HumanReviewEvidenceContextLinkIssue Block(HumanReviewEvidenceContextLinkIssueKind kind, string reason) =>
        new(kind, reason, BlocksApprovalPreview: true, BlocksDecisionInfluence: true, BlocksSafeNextStep: true, RequiresHumanReview: true, WarningOnly: false);

    private static HumanReviewEvidenceContextLinkIssue Exclude(HumanReviewEvidenceContextLinkIssueKind kind, string reason) =>
        new(kind, reason, BlocksApprovalPreview: true, BlocksDecisionInfluence: true, BlocksSafeNextStep: true, RequiresHumanReview: true, WarningOnly: false);

    private static HumanReviewEvidenceContextLinkIssue Warning(HumanReviewEvidenceContextLinkIssueKind kind, string reason, bool requiresHumanReview) =>
        new(kind, reason, BlocksApprovalPreview: false, BlocksDecisionInfluence: false, BlocksSafeNextStep: false, RequiresHumanReview: requiresHumanReview, WarningOnly: true);
}
