namespace OneBrain.Core.Recipes;

public sealed record StructuredPrerequisiteAuthoringReport(
    string ReportId,
    string SubjectId,
    ReliableRecipeStructuredPrerequisiteSubjectKind SubjectKind,
    StructuredPrerequisiteAuthoringDecision OverallDecision,
    IReadOnlyList<StructuredPrerequisiteProposal> Proposals,
    IReadOnlyList<StructuredPrerequisiteReviewChecklistItem> ReviewChecklist,
    StructuredPrerequisiteMigrationSummary MigrationSummary,
    IReadOnlyList<string> AcceptedRequirements,
    IReadOnlyList<string> RejectedRequirements,
    IReadOnlyList<string> StillMissingCriticalRequirements,
    StructuredPrerequisiteAdapterGateImpact AdapterGateImpact,
    string NoRuntimeNotice)
{
    public bool ReadOnly => true;
    public bool FixtureOnly => true;
    public bool RuntimeEnabled => false;
    public bool AdapterRuntimeEnabled => false;
    public bool CanApplyMigrationLive => false;
    public bool CanEnableAdapter => false;
}

public enum StructuredPrerequisiteAuthoringDecision
{
    NoChangesNeeded,
    ProposalsNeedReview,
    BlockedMissingCriticalRequirements,
    BlockedRejectedCriticalRequirements,
    DesignOnlyReadyWithReviewedRequirements,
    RuntimeStillBlocked
}

public sealed record StructuredPrerequisiteProposal(
    string ProposalId,
    string TargetBlockId,
    string RequirementKind,
    StructuredPrerequisiteRequirementType RequirementType,
    ReliableRecipeRequirementSource ProposedSource,
    string Reason,
    double Confidence,
    string RiskImpact,
    StructuredPrerequisiteReviewDecision RecommendedReviewDecision,
    bool WouldBlockAdapterIfRejected,
    string NoRuntimeNotice)
{
    public bool ReadOnly => true;
    public bool FixtureOnly => true;
    public bool RuntimeEnabled => false;
}

public enum StructuredPrerequisiteRequirementType
{
    Evidence,
    Validation
}

public enum StructuredPrerequisiteReviewDecision
{
    PendingReview,
    AcceptedForFixture,
    AcceptedMappedLegacy,
    RejectedNeedsRedesign,
    RejectedUnsafe,
    Deferred
}

public sealed record StructuredPrerequisiteReviewChecklistItem(
    string Code,
    string Title,
    string Description,
    ReliableRecipeQualitySeverity Severity,
    string TargetBlockId,
    string ProposalId,
    bool IsBlocking,
    string RecommendedFix);

public sealed record StructuredPrerequisiteMigrationSummary(
    int ExplicitCount,
    int FixtureExplicitCount,
    int MappedLegacyCount,
    int InferredCount,
    int MissingCount,
    int ProposedCount,
    int AcceptedCount,
    int RejectedCount,
    int StillBlockingCount);

public enum StructuredPrerequisiteAdapterGateImpact
{
    NoImpact,
    WarningOnly,
    BlocksUntilReview,
    BlocksUntilExplicit,
    BlocksUntilExternalAudit,
    RuntimeAlwaysBlockedInM10
}

public sealed record StructuredPrerequisiteAuthoringScenario(
    string ScenarioId,
    ReliableRecipeStructuredPrerequisiteProfile Profile,
    IReadOnlyDictionary<string, StructuredPrerequisiteReviewDecision> ReviewDecisions,
    StructuredPrerequisiteAuthoringDecision ExpectedDecision,
    string Summary)
{
    public bool FixtureOnly => true;
    public bool RuntimeEnabled => false;
    public bool UsesLiveBrowser => false;
    public bool UsesLiveDesktop => false;
    public bool UsesNetwork => false;
    public bool StoresSecrets => false;
}

public sealed record ReliableRecipeLabStructuredPrerequisiteAuthoringPanel(
    string DecisionLabel,
    int ProposalCount,
    int PendingReviewCount,
    int AcceptedCount,
    int RejectedCount,
    int StillBlockingCount,
    IReadOnlyList<string> TopProposals,
    IReadOnlyList<string> ReviewChecklist,
    string MigrationSummary,
    string AdapterGateImpactLabel,
    string NoRuntimeNotice,
    IReadOnlyList<string> ReadOnlyActionLabels)
{
    public bool ReadOnly => true;
    public bool FixtureOnly => true;
    public bool RuntimeActionExposed => false;
    public bool CanApplyLive => false;
    public bool CanRunMigration => false;
    public bool CanEnableAdapter => false;
    public bool CanExecute => false;
}

public static class StructuredPrerequisiteAuthoringScenarioCatalog
{
    public static IReadOnlyList<StructuredPrerequisiteAuthoringScenario> All()
    {
        var mapped = Profile("legacy_mapped_requirements_pass_with_warning");
        var rejected = Profile("submit_without_explicit_post_validation_blocks");
        var accepted = Profile("submit_without_explicit_post_validation_blocks");

        return
        [
            Scenario("complete_explicit_no_changes_needed", Profile("complete_explicit_invoice_download_prerequisites"), EmptyDecisions(), StructuredPrerequisiteAuthoringDecision.NoChangesNeeded, "Explicit requirements need no migration."),
            Scenario("inferred_download_evidence_proposal", Profile("inferred_download_requirements_warn"), EmptyDecisions(), StructuredPrerequisiteAuthoringDecision.ProposalsNeedReview, "Inferred download wording needs review before becoming structured."),
            Scenario("missing_post_submit_validation_proposal", Profile("submit_without_explicit_post_validation_blocks"), EmptyDecisions(), StructuredPrerequisiteAuthoringDecision.BlockedMissingCriticalRequirements, "Missing post-submit validation blocks adapter readiness."),
            Scenario("external_side_effect_approval_evidence_proposal", Profile("external_side_effect_missing_approval_evidence_blocks"), EmptyDecisions(), StructuredPrerequisiteAuthoringDecision.BlockedMissingCriticalRequirements, "External side effect needs approval evidence proposal."),
            Scenario("ocr_only_sensitive_perception_validation_proposal", Profile("ocr_only_sensitive_missing_perception_validation_blocks"), EmptyDecisions(), StructuredPrerequisiteAuthoringDecision.BlockedMissingCriticalRequirements, "OCR-only sensitive target needs perception validation proposal."),
            Scenario("recorder_draft_human_review_evidence_proposal", Profile("recorder_draft_missing_human_review_evidence_blocks"), EmptyDecisions(), StructuredPrerequisiteAuthoringDecision.BlockedMissingCriticalRequirements, "Recorder draft needs human review evidence proposal."),
            Scenario("captcha_handoff_evidence_proposal", Profile("captcha_handoff_missing_human_intervention_evidence_blocks"), EmptyDecisions(), StructuredPrerequisiteAuthoringDecision.BlockedMissingCriticalRequirements, "Challenge handoff needs human intervention evidence proposal."),
            Scenario("sandbox_readiness_assertion_proposal", Profile("sandbox_future_missing_readiness_assertion_blocks"), EmptyDecisions(), StructuredPrerequisiteAuthoringDecision.BlockedMissingCriticalRequirements, "Sandbox future needs readiness assertion proposal."),
            Scenario("eval_expected_outcome_assertion_proposal", Profile("eval_scenario_missing_expected_outcome_assertion_blocks"), EmptyDecisions(), StructuredPrerequisiteAuthoringDecision.BlockedMissingCriticalRequirements, "Eval scenario needs expected outcome assertion proposal."),
            Scenario("mapped_legacy_accepted_with_warning", mapped, Decisions(mapped, StructuredPrerequisiteReviewDecision.AcceptedMappedLegacy), StructuredPrerequisiteAuthoringDecision.DesignOnlyReadyWithReviewedRequirements, "Mapped legacy requirements can be accepted for fixture with warning."),
            Scenario("rejected_critical_requirement_blocks", rejected, Decisions(rejected, StructuredPrerequisiteReviewDecision.RejectedUnsafe), StructuredPrerequisiteAuthoringDecision.BlockedRejectedCriticalRequirements, "Rejected critical proposal blocks adapter readiness."),
            Scenario("accepted_fixture_requirements_still_runtime_blocked", accepted, Decisions(accepted, StructuredPrerequisiteReviewDecision.AcceptedForFixture), StructuredPrerequisiteAuthoringDecision.DesignOnlyReadyWithReviewedRequirements, "Accepted fixture proposals improve design-only readiness but runtime remains blocked.")
        ];
    }

    public static StructuredPrerequisiteAuthoringScenario Get(string scenarioId) =>
        All().Single(s => s.ScenarioId == scenarioId);

    private static StructuredPrerequisiteAuthoringScenario Scenario(
        string scenarioId,
        ReliableRecipeStructuredPrerequisiteProfile profile,
        IReadOnlyDictionary<string, StructuredPrerequisiteReviewDecision> decisions,
        StructuredPrerequisiteAuthoringDecision expected,
        string summary) =>
        new(scenarioId, profile, decisions, expected, summary);

    private static ReliableRecipeStructuredPrerequisiteProfile Profile(string scenarioId) =>
        ReliableRecipeStructuredPrerequisiteEvaluator.Evaluate(ReliableRecipeStructuredPrerequisiteScenarioCatalog.Get(scenarioId));

    private static IReadOnlyDictionary<string, StructuredPrerequisiteReviewDecision> Decisions(
        ReliableRecipeStructuredPrerequisiteProfile profile,
        StructuredPrerequisiteReviewDecision decision) =>
        StructuredPrerequisiteAuthoringEvaluator.Propose(profile)
            .ToDictionary(p => p.ProposalId, _ => decision);

    private static IReadOnlyDictionary<string, StructuredPrerequisiteReviewDecision> EmptyDecisions() =>
        new Dictionary<string, StructuredPrerequisiteReviewDecision>();
}

public static class StructuredPrerequisiteAuthoringEvaluator
{
    public static StructuredPrerequisiteAuthoringReport Evaluate(StructuredPrerequisiteAuthoringScenario scenario) =>
        Evaluate(scenario.Profile, scenario.ReviewDecisions);

    public static StructuredPrerequisiteAuthoringReport Evaluate(
        ReliableRecipeStructuredPrerequisiteProfile profile,
        IReadOnlyDictionary<string, StructuredPrerequisiteReviewDecision>? reviewDecisions = null)
    {
        var decisions = reviewDecisions ?? new Dictionary<string, StructuredPrerequisiteReviewDecision>();
        var proposals = Propose(profile).ToArray();
        var applied = proposals
            .Select(p => (Proposal: p, Decision: decisions.TryGetValue(p.ProposalId, out var decision) ? decision : p.RecommendedReviewDecision))
            .ToArray();
        var checklist = Checklist(profile, applied).ToArray();
        var accepted = applied
            .Where(p => p.Decision is StructuredPrerequisiteReviewDecision.AcceptedForFixture or StructuredPrerequisiteReviewDecision.AcceptedMappedLegacy)
            .Select(p => p.Proposal.ProposalId)
            .ToArray();
        var rejected = applied
            .Where(p => p.Decision is StructuredPrerequisiteReviewDecision.RejectedNeedsRedesign or StructuredPrerequisiteReviewDecision.RejectedUnsafe)
            .Select(p => p.Proposal.ProposalId)
            .ToArray();
        var stillMissing = StillMissing(profile, applied).Distinct().ToArray();
        var summary = Summary(profile, proposals, accepted.Length, rejected.Length, stillMissing.Length);
        var impact = Impact(applied, stillMissing, profile);
        var decisionForReport = DecisionFor(applied, stillMissing, rejected, profile);

        return new StructuredPrerequisiteAuthoringReport(
            $"structured-prerequisite-authoring.{profile.SubjectId}",
            profile.SubjectId,
            profile.SubjectKind,
            decisionForReport,
            proposals,
            checklist,
            summary,
            accepted,
            rejected,
            stillMissing,
            impact,
            "Structured prerequisite authoring report only. Runtime not enabled; accepting proposals cannot enable an adapter.");
    }

    public static IReadOnlyList<StructuredPrerequisiteProposal> Propose(ReliableRecipeStructuredPrerequisiteProfile profile) =>
        profile.EvidenceRequirements.Select(ToProposal)
            .Concat(profile.ValidationRequirements.Select(ToProposal))
            .Where(p => p is not null)
            .Select(p => p!)
            .OrderBy(p => p.ProposalId, StringComparer.Ordinal)
            .ToArray();

    public static bool AllowsAdapterDesignGates(StructuredPrerequisiteAuthoringReport? report)
    {
        if (report is null)
            return true;

        return (report.OverallDecision is StructuredPrerequisiteAuthoringDecision.NoChangesNeeded or StructuredPrerequisiteAuthoringDecision.DesignOnlyReadyWithReviewedRequirements or StructuredPrerequisiteAuthoringDecision.RuntimeStillBlocked) &&
            (report.AdapterGateImpact is StructuredPrerequisiteAdapterGateImpact.NoImpact or StructuredPrerequisiteAdapterGateImpact.WarningOnly or StructuredPrerequisiteAdapterGateImpact.RuntimeAlwaysBlockedInM10) &&
            report.StillMissingCriticalRequirements.Count == 0;
    }

    private static StructuredPrerequisiteProposal? ToProposal(StructuredEvidenceRequirement requirement)
    {
        if (requirement.Source == ReliableRecipeRequirementSource.Explicit || requirement.Source == ReliableRecipeRequirementSource.FixtureExplicit)
            return null;

        return new StructuredPrerequisiteProposal(
            ProposalId(StructuredPrerequisiteRequirementType.Evidence, requirement.TargetBlockId, requirement.Kind.ToString()),
            requirement.TargetBlockId,
            requirement.Kind.ToString(),
            StructuredPrerequisiteRequirementType.Evidence,
            ProposedSource(requirement.Source),
            ReasonFor(requirement.Source, requirement.IsCritical, requirement.Description),
            ConfidenceFor(requirement.Source),
            requirement.AdapterGateImpact.ToString(),
            ReviewDecisionFor(requirement.Source),
            requirement.IsCritical || requirement.AdapterGateImpact is ReliableRecipeRequirementAdapterGateImpact.BlocksFutureAdapter or ReliableRecipeRequirementAdapterGateImpact.BlocksDryRunCandidate,
            "Proposed structured requirement only. Runtime not enabled.");
    }

    private static StructuredPrerequisiteProposal? ToProposal(StructuredValidationRequirement requirement)
    {
        if (requirement.Source == ReliableRecipeRequirementSource.Explicit || requirement.Source == ReliableRecipeRequirementSource.FixtureExplicit)
            return null;

        return new StructuredPrerequisiteProposal(
            ProposalId(StructuredPrerequisiteRequirementType.Validation, requirement.TargetBlockId, requirement.Kind.ToString()),
            requirement.TargetBlockId,
            requirement.Kind.ToString(),
            StructuredPrerequisiteRequirementType.Validation,
            ProposedSource(requirement.Source),
            ReasonFor(requirement.Source, requirement.IsCritical, requirement.ExpectedAssertion),
            ConfidenceFor(requirement.Source),
            requirement.AdapterGateImpact.ToString(),
            ReviewDecisionFor(requirement.Source),
            requirement.IsCritical || requirement.AdapterGateImpact is ReliableRecipeRequirementAdapterGateImpact.BlocksFutureAdapter or ReliableRecipeRequirementAdapterGateImpact.BlocksDryRunCandidate,
            "Proposed structured requirement only. Runtime not enabled.");
    }

    private static IEnumerable<StructuredPrerequisiteReviewChecklistItem> Checklist(
        ReliableRecipeStructuredPrerequisiteProfile profile,
        IReadOnlyList<(StructuredPrerequisiteProposal Proposal, StructuredPrerequisiteReviewDecision Decision)> applied)
    {
        foreach (var proposal in applied)
        {
            if (proposal.Decision == StructuredPrerequisiteReviewDecision.PendingReview)
            {
                yield return new StructuredPrerequisiteReviewChecklistItem(
                    "structured-prerequisite.pending-review",
                    "Needs review",
                    $"{proposal.Proposal.RequirementType} requirement {proposal.Proposal.RequirementKind} must be reviewed before it can become explicit.",
                    proposal.Proposal.WouldBlockAdapterIfRejected ? ReliableRecipeQualitySeverity.Blocking : ReliableRecipeQualitySeverity.Warning,
                    proposal.Proposal.TargetBlockId,
                    proposal.Proposal.ProposalId,
                    proposal.Proposal.WouldBlockAdapterIfRejected,
                    "Review proposed structured requirement and accept for fixture only or redesign it.");
            }

            if (proposal.Decision is StructuredPrerequisiteReviewDecision.RejectedNeedsRedesign or StructuredPrerequisiteReviewDecision.RejectedUnsafe)
            {
                yield return new StructuredPrerequisiteReviewChecklistItem(
                    "structured-prerequisite.rejected-critical",
                    "Rejected requirement blocks adapter gate",
                    $"{proposal.Proposal.RequirementType} requirement {proposal.Proposal.RequirementKind} was rejected and must be redesigned.",
                    ReliableRecipeQualitySeverity.Blocking,
                    proposal.Proposal.TargetBlockId,
                    proposal.Proposal.ProposalId,
                    true,
                    "Redesign the prerequisite explicitly before adapter readiness review.");
            }
        }

        foreach (var missing in profile.CompletenessReport.MissingCriticalRequirements)
        {
            yield return new StructuredPrerequisiteReviewChecklistItem(
                "structured-prerequisite.missing-critical",
                "Missing critical requirement",
                missing,
                ReliableRecipeQualitySeverity.Blocking,
                BlockIdFromMissing(missing),
                ProposalIdFromMissing(missing),
                true,
                "Add an explicit fixture structured requirement before adapter readiness review.");
        }
    }

    private static IReadOnlyList<string> StillMissing(
        ReliableRecipeStructuredPrerequisiteProfile profile,
        IReadOnlyList<(StructuredPrerequisiteProposal Proposal, StructuredPrerequisiteReviewDecision Decision)> applied)
    {
        var accepted = applied
            .Where(p => p.Decision is StructuredPrerequisiteReviewDecision.AcceptedForFixture or StructuredPrerequisiteReviewDecision.AcceptedMappedLegacy)
            .Select(p => p.Proposal.ProposalId)
            .ToHashSet(StringComparer.Ordinal);
        var rejected = applied
            .Where(p => p.Decision is StructuredPrerequisiteReviewDecision.RejectedNeedsRedesign or StructuredPrerequisiteReviewDecision.RejectedUnsafe)
            .Select(p => p.Proposal.ProposalId)
            .ToHashSet(StringComparer.Ordinal);

        return profile.CompletenessReport.MissingCriticalRequirements
            .Where(m => !accepted.Contains(ProposalIdFromMissing(m)) || rejected.Contains(ProposalIdFromMissing(m)))
            .Concat(applied.Where(p => p.Proposal.WouldBlockAdapterIfRejected && p.Decision is StructuredPrerequisiteReviewDecision.PendingReview or StructuredPrerequisiteReviewDecision.Deferred).Select(p => $"pending:{p.Proposal.ProposalId}"))
            .Concat(applied.Where(p => p.Proposal.WouldBlockAdapterIfRejected && p.Decision is StructuredPrerequisiteReviewDecision.RejectedNeedsRedesign or StructuredPrerequisiteReviewDecision.RejectedUnsafe).Select(p => $"rejected:{p.Proposal.ProposalId}"))
            .Distinct()
            .ToArray();
    }

    private static StructuredPrerequisiteMigrationSummary Summary(
        ReliableRecipeStructuredPrerequisiteProfile profile,
        IReadOnlyList<StructuredPrerequisiteProposal> proposals,
        int accepted,
        int rejected,
        int stillBlocking)
    {
        var sources = profile.EvidenceRequirements.Select(e => e.Source).Concat(profile.ValidationRequirements.Select(v => v.Source)).ToArray();
        return new StructuredPrerequisiteMigrationSummary(
            sources.Count(s => s == ReliableRecipeRequirementSource.Explicit),
            sources.Count(s => s == ReliableRecipeRequirementSource.FixtureExplicit),
            sources.Count(s => s == ReliableRecipeRequirementSource.MappedFromLegacyContract),
            sources.Count(s => s is ReliableRecipeRequirementSource.InferredFromBlockKind or ReliableRecipeRequirementSource.InferredFromLabel),
            sources.Count(s => s == ReliableRecipeRequirementSource.Missing),
            proposals.Count,
            accepted,
            rejected,
            stillBlocking);
    }

    private static StructuredPrerequisiteAdapterGateImpact Impact(
        IReadOnlyList<(StructuredPrerequisiteProposal Proposal, StructuredPrerequisiteReviewDecision Decision)> applied,
        IReadOnlyList<string> stillMissing,
        ReliableRecipeStructuredPrerequisiteProfile profile)
    {
        if (applied.Any(p => p.Proposal.WouldBlockAdapterIfRejected && p.Decision is StructuredPrerequisiteReviewDecision.RejectedNeedsRedesign or StructuredPrerequisiteReviewDecision.RejectedUnsafe))
            return StructuredPrerequisiteAdapterGateImpact.BlocksUntilExplicit;
        if (stillMissing.Count > 0 && applied.Any(p => p.Decision is StructuredPrerequisiteReviewDecision.PendingReview or StructuredPrerequisiteReviewDecision.Deferred))
            return StructuredPrerequisiteAdapterGateImpact.BlocksUntilReview;
        if (stillMissing.Count > 0)
            return StructuredPrerequisiteAdapterGateImpact.BlocksUntilExplicit;
        if (profile.CompletenessReport.AdapterReadinessImpact == ReliableRecipeRequirementAdapterGateImpact.RequiresExternalAudit)
            return StructuredPrerequisiteAdapterGateImpact.BlocksUntilExternalAudit;
        if (applied.Any(p => p.Decision is StructuredPrerequisiteReviewDecision.AcceptedForFixture or StructuredPrerequisiteReviewDecision.AcceptedMappedLegacy))
            return StructuredPrerequisiteAdapterGateImpact.RuntimeAlwaysBlockedInM10;
        if (profile.CompletenessReport.Warnings.Count > 0 || applied.Count > 0)
            return StructuredPrerequisiteAdapterGateImpact.WarningOnly;
        return StructuredPrerequisiteAdapterGateImpact.NoImpact;
    }

    private static StructuredPrerequisiteAuthoringDecision DecisionFor(
        IReadOnlyList<(StructuredPrerequisiteProposal Proposal, StructuredPrerequisiteReviewDecision Decision)> applied,
        IReadOnlyList<string> stillMissing,
        IReadOnlyList<string> rejected,
        ReliableRecipeStructuredPrerequisiteProfile profile)
    {
        if (rejected.Count > 0)
            return StructuredPrerequisiteAuthoringDecision.BlockedRejectedCriticalRequirements;
        if (stillMissing.Count > 0 && applied.Any(p => p.Decision is StructuredPrerequisiteReviewDecision.PendingReview or StructuredPrerequisiteReviewDecision.Deferred))
            return profile.CompletenessReport.BlockingFindings.Count > 0
                ? StructuredPrerequisiteAuthoringDecision.BlockedMissingCriticalRequirements
                : StructuredPrerequisiteAuthoringDecision.ProposalsNeedReview;
        if (stillMissing.Count > 0)
            return StructuredPrerequisiteAuthoringDecision.BlockedMissingCriticalRequirements;
        if (applied.Any(p => p.Decision == StructuredPrerequisiteReviewDecision.PendingReview))
            return StructuredPrerequisiteAuthoringDecision.ProposalsNeedReview;
        if (applied.Any(p => p.Decision is StructuredPrerequisiteReviewDecision.AcceptedForFixture or StructuredPrerequisiteReviewDecision.AcceptedMappedLegacy))
            return StructuredPrerequisiteAuthoringDecision.DesignOnlyReadyWithReviewedRequirements;
        if (profile.CompletenessReport.Warnings.Count > 0)
            return StructuredPrerequisiteAuthoringDecision.ProposalsNeedReview;
        return StructuredPrerequisiteAuthoringDecision.NoChangesNeeded;
    }

    private static ReliableRecipeRequirementSource ProposedSource(ReliableRecipeRequirementSource source) =>
        source == ReliableRecipeRequirementSource.MappedFromLegacyContract
            ? ReliableRecipeRequirementSource.MappedFromLegacyContract
            : ReliableRecipeRequirementSource.FixtureExplicit;

    private static string ReasonFor(ReliableRecipeRequirementSource source, bool critical, string description) =>
        source switch
        {
            ReliableRecipeRequirementSource.MappedFromLegacyContract => $"Mapped legacy requirement should be reviewed as compatibility evidence: {description}.",
            ReliableRecipeRequirementSource.InferredFromBlockKind => $"Requirement was inferred from block kind and needs human review: {description}.",
            ReliableRecipeRequirementSource.InferredFromLabel => $"Requirement was inferred from label text and needs human review: {description}.",
            ReliableRecipeRequirementSource.Missing => $"Missing {(critical ? "critical" : "non-critical")} structured requirement must be authored: {description}.",
            _ => description
        };

    private static double ConfidenceFor(ReliableRecipeRequirementSource source) =>
        source switch
        {
            ReliableRecipeRequirementSource.MappedFromLegacyContract => 0.74,
            ReliableRecipeRequirementSource.InferredFromBlockKind => 0.52,
            ReliableRecipeRequirementSource.InferredFromLabel => 0.46,
            ReliableRecipeRequirementSource.Missing => 0.2,
            _ => 0.9
        };

    private static StructuredPrerequisiteReviewDecision ReviewDecisionFor(ReliableRecipeRequirementSource source) =>
        source == ReliableRecipeRequirementSource.MappedFromLegacyContract
            ? StructuredPrerequisiteReviewDecision.AcceptedMappedLegacy
            : StructuredPrerequisiteReviewDecision.PendingReview;

    private static string ProposalId(StructuredPrerequisiteRequirementType type, string blockId, string kind) =>
        $"proposal.{type.ToString().ToLowerInvariant()}.{blockId}.{kind.ToLowerInvariant()}";

    private static string ProposalIdFromMissing(string missing)
    {
        var parts = missing.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length >= 3
            ? $"proposal.{parts[0].ToLowerInvariant()}.{parts[1]}.{parts[2].ToLowerInvariant()}"
            : $"proposal.missing.{missing.ToLowerInvariant().Replace(' ', '-')}";
    }

    private static string BlockIdFromMissing(string missing)
    {
        var parts = missing.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length >= 2 ? parts[1] : "unknown";
    }
}

public static class StructuredPrerequisiteAuthoringReportMapper
{
    public static ReliableRecipeLabStructuredPrerequisiteAuthoringPanel ToLabPanel(StructuredPrerequisiteAuthoringReport report) =>
        new(
            report.OverallDecision.ToString(),
            report.Proposals.Count,
            report.Proposals.Count(p => !report.AcceptedRequirements.Contains(p.ProposalId) && !report.RejectedRequirements.Contains(p.ProposalId)),
            report.MigrationSummary.AcceptedCount,
            report.MigrationSummary.RejectedCount,
            report.MigrationSummary.StillBlockingCount,
            report.Proposals.Take(5).Select(p => $"Proposed structured requirement: {p.RequirementType} {p.RequirementKind} on {p.TargetBlockId}.").ToArray(),
            report.ReviewChecklist.Take(5).Select(i => $"{i.Title}: {i.Description}").ToArray(),
            $"Explicit {report.MigrationSummary.ExplicitCount}; fixture explicit {report.MigrationSummary.FixtureExplicitCount}; mapped legacy {report.MigrationSummary.MappedLegacyCount}; inferred {report.MigrationSummary.InferredCount}; missing {report.MigrationSummary.MissingCount}; proposed {report.MigrationSummary.ProposedCount}.",
            report.AdapterGateImpact.ToString(),
            report.NoRuntimeNotice,
            ["Review proposals", "Open migration report", "Copy summary"]);
}
