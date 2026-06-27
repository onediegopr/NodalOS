namespace OneBrain.Core.Recipes;

public sealed record ReliableRecipeQualityOptions(
    double MinimumDryRunScore = 0.72,
    double MinimumEvidenceScore = 0.70,
    double MinimumValidationScore = 0.70,
    double MinimumTargetScore = 0.65,
    bool AssistedRunBlockedByDefault = true,
    bool SupervisedRunBlockedByDefault = true,
    bool LimitedAutonomyBlockedByDefault = true);

public sealed record ReliableRecipeQualityContext(
    ReliableActionResolutionPolicy? ActionResolutionPolicy = null,
    ReliableTargetDescriptor? TargetDescriptor = null,
    ReliablePerceptionStackSnapshot? PerceptionSnapshot = null,
    ComputerUseSandboxProfile? SandboxProfile = null,
    IReadOnlyList<ReliableHumanInterventionRequest>? HumanInterventionRequests = null,
    RecipePolicyPreflightResult? ExistingRuntimePolicyResult = null);

public sealed record ReliableRecipeQualityReport(
    string RecipeId,
    double OverallScore,
    ReliableRecipeReadiness Readiness,
    ReliableRecipeQualityDecision Decision,
    IReadOnlyList<ReliableRecipeQualityCategoryScore> CategoryScores,
    IReadOnlyList<ReliableRecipeQualityFinding> BlockingFindings,
    IReadOnlyList<ReliableRecipeQualityFinding> Warnings,
    IReadOnlyList<string> RecommendedNextActions,
    EvidenceCompletenessScore EvidenceCompleteness,
    ValidationCompletenessScore ValidationCompleteness,
    TargetResolutionQualityScore TargetResolutionQuality,
    RecipeRiskPostureReport RiskPosture,
    RecipeSandboxReadinessScore SandboxReadiness,
    ReliablePerceptionConfidence PerceptionSignalQuality,
    HumanInterventionPlanQualityScore HumanInterventionPlanQuality,
    ReliableRecipeStructuredPrerequisiteProfile StructuredPrerequisites)
{
    public bool UsesAiScoring => false;
    public bool ExecutesRecipe => false;
    public bool InspectsLiveBrowserOrDesktop => false;
}

public enum ReliableRecipeQualityCategory
{
    Limits,
    Validation,
    Evidence,
    Risk,
    TargetResolution,
    Perception,
    Sandbox,
    HumanIntervention,
    Replayability,
    RecorderDraftSafety
}

public sealed record ReliableRecipeQualityCategoryScore(
    ReliableRecipeQualityCategory Category,
    double Score,
    ReliableRecipeQualityDecision Decision,
    IReadOnlyList<string> Reasons);

public enum ReliableRecipeQualityDecision
{
    PassDraftOnly,
    PassDryRun,
    NeedsReview,
    Blocked
}

public sealed record ReliableRecipeQualityFinding(
    string Code,
    ReliableRecipeQualitySeverity Severity,
    string Message,
    string? AffectedBlockId,
    string RecommendedFix,
    bool IsBlocking);

public enum ReliableRecipeQualitySeverity
{
    Info,
    Warning,
    Error,
    Blocking
}

public sealed record ReliableRecipePreflightReport(
    string RecipeId,
    ReliableRecipeRunMode ModeRequested,
    ReliableRecipeRunMode ModeAllowed,
    ReliableRecipeQualityReport QualityReport,
    ReliableRecipePolicyDecision PolicyDecision,
    IReadOnlyList<ReliableRecipeQualityFinding> BlockingReasons,
    IReadOnlyList<string> RequiredApprovals,
    IReadOnlyList<ReliableHumanInterventionReason> RequiredHumanInterventions,
    IReadOnlyList<EvidenceRequirementKind> RequiredEvidence,
    IReadOnlyList<ReliableValidationCheckKind> RequiredValidations,
    ReliableRecipeStructuredPrerequisiteProfile StructuredPrerequisites,
    bool CanProceedToDraftOnly,
    bool CanProceedToDryRun,
    bool CanProceedToAssistedRun,
    bool CanProceedToSupervisedRun,
    bool CanProceedToLimitedAutonomy)
{
    public bool LiveRuntimeEnabled => false;
    public bool ActionAuthorityGranted => false;
}

public sealed record EvidenceCompletenessScore(
    double Score,
    IReadOnlyList<EvidenceRequirementKind> MissingEvidenceKinds,
    double CriticalStepCoverage,
    IReadOnlyList<RecipeStepEvidenceCompleteness> StepReports);

public sealed record RecipeStepEvidenceCompleteness(
    string StepId,
    IReadOnlyList<EvidenceRequirementKind> ExpectedKinds,
    IReadOnlyList<EvidenceRequirementKind> ProvidedKinds,
    IReadOnlyList<EvidenceRequirementKind> MissingKinds,
    bool IsCritical,
    bool CanComplete);

public enum EvidenceRequirementKind
{
    BeforeState,
    AfterState,
    ActionProposal,
    ActionResult,
    ValidationReport,
    RedactedSnapshot,
    TimelineEvent,
    HumanApproval,
    DownloadArtifact,
    PerceptionSnapshot
}

public sealed record ValidationCompletenessScore(
    double Score,
    IReadOnlyList<ReliableValidationCheckKind> MissingValidations,
    IReadOnlyList<RecipeBlockValidationCompleteness> CriticalBlockReports);

public sealed record RecipeBlockValidationCompleteness(
    string BlockId,
    IReadOnlyList<ReliableValidationCheckKind> RequiredChecks,
    IReadOnlyList<ReliableValidationCheckKind> ProvidedChecks,
    IReadOnlyList<ReliableValidationCheckKind> MissingChecks,
    bool CanProveCompletion);

public sealed record TargetResolutionQualityScore(
    double Score,
    ReliableActionResolutionMode ResolutionMode,
    TargetResolutionSignalAgreement SignalAgreement,
    string? Ambiguity,
    bool SensitiveActionRisk,
    TargetResolutionQualityDecision Decision);

public enum TargetResolutionQualityDecision
{
    AcceptForDryRun,
    NeedsMoreSignals,
    NeedsHumanReview,
    Blocked
}

public sealed record TargetResolutionSignalAgreement(
    bool DomMatchesAccessibility,
    bool DomMatchesOcr,
    bool AccessibilityMatchesOcr,
    bool VisualMatchesText,
    IReadOnlyList<string> Contradictions);

public sealed record RecipeRiskPostureReport(
    ReliableRecipeRiskProfile HighestRisk,
    double RiskScore,
    bool ExternalSideEffects,
    bool SensitiveDataRisks,
    bool IrreversibleRisks,
    IReadOnlyList<string> RequiredApprovals,
    IReadOnlyList<string> BlockedReasons);

public sealed record ReliableRecipeRiskGate(
    ReliableRecipeRiskProfile Risk,
    ReliableRecipePolicyDecision Decision,
    string Reason);

public sealed record RecipeSandboxReadinessScore(
    double Score,
    ReliableSandboxIsolationMode IsolationMode,
    IReadOnlyList<SandboxRequirementKind> MissingRequirements,
    IReadOnlyList<string> BlockedCapabilities,
    bool CanEvaluateInFixture,
    bool CanEvaluateInFutureSandbox);

public enum SandboxRequirementKind
{
    Rollback,
    NetworkPolicy,
    FilesystemPolicy,
    CredentialPolicy,
    EvidencePolicy,
    RuntimeLimit,
    SurfaceIsolation
}

public sealed record HumanInterventionPlanQualityScore(
    double Score,
    IReadOnlyList<string> MissingUserInstructions,
    IReadOnlyList<ReliableHumanInterventionReason> AmbiguousReasons,
    bool CanResume,
    bool EvidenceRefsPresent);

public static class ReliableRecipePreflightComposer
{
    public static ReliableRecipePreflightReport Compose(
        ReliableRecipeDefinition recipe,
        ReliableRecipeRunMode requestedMode,
        ReliableRecipeQualityOptions? options = null,
        ReliableRecipeQualityContext? context = null)
    {
        options ??= new ReliableRecipeQualityOptions();
        context ??= new ReliableRecipeQualityContext();

        var quality = ReliableRecipeQualityScorer.Score(recipe, options, context);
        var blocking = quality.BlockingFindings.ToList();

        if (context.ExistingRuntimePolicyResult is { IsReady: false } existing)
        {
            blocking.AddRange(existing.BlockingIssues.Select(i => Finding(
                $"existing-runtime-policy-{i.IssueId}",
                i.Message,
                i.BlockId,
                "Satisfy the existing Recipe Runtime policy preflight before advancing reliable recipe readiness.")));
        }

        var stricterBlocks = blocking.Count > 0 || quality.Decision == ReliableRecipeQualityDecision.Blocked;
        var modeAllowed = DetermineAllowedMode(requestedMode, quality, stricterBlocks, options);

        return new ReliableRecipePreflightReport(
            recipe.Id,
            requestedMode,
            modeAllowed,
            quality,
            stricterBlocks ? ReliableRecipePolicyDecision.Reject : ReliableRecipePolicyDecision.AllowDryRunOnly,
            blocking,
            quality.RiskPosture.RequiredApprovals,
            context.HumanInterventionRequests?.Select(r => r.Reason).Distinct().ToArray() ?? [],
            quality.EvidenceCompleteness.MissingEvidenceKinds,
            quality.ValidationCompleteness.MissingValidations,
            quality.StructuredPrerequisites,
            CanProceedToDraftOnly: true,
            CanProceedToDryRun: modeAllowed == ReliableRecipeRunMode.DryRun,
            CanProceedToAssistedRun: false,
            CanProceedToSupervisedRun: false,
            CanProceedToLimitedAutonomy: false);
    }

    private static ReliableRecipeRunMode DetermineAllowedMode(
        ReliableRecipeRunMode requestedMode,
        ReliableRecipeQualityReport quality,
        bool stricterBlocks,
        ReliableRecipeQualityOptions options)
    {
        if (requestedMode == ReliableRecipeRunMode.LimitedAutonomy && options.LimitedAutonomyBlockedByDefault)
            return ReliableRecipeRunMode.DraftOnly;
        if (requestedMode == ReliableRecipeRunMode.SupervisedRun && options.SupervisedRunBlockedByDefault)
            return ReliableRecipeRunMode.DraftOnly;
        if (requestedMode == ReliableRecipeRunMode.AssistedRun && options.AssistedRunBlockedByDefault)
            return ReliableRecipeRunMode.DraftOnly;
        if (stricterBlocks)
            return ReliableRecipeRunMode.DraftOnly;
        if (requestedMode == ReliableRecipeRunMode.DryRun && quality.OverallScore >= options.MinimumDryRunScore && quality.Decision == ReliableRecipeQualityDecision.PassDryRun)
            return ReliableRecipeRunMode.DryRun;
        return ReliableRecipeRunMode.DraftOnly;
    }

    private static ReliableRecipeQualityFinding Finding(string code, string message, string? blockId, string fix) =>
        new(code, ReliableRecipeQualitySeverity.Blocking, message, blockId, fix, IsBlocking: true);
}

public static class ReliableRecipeQualityScorer
{
    public static ReliableRecipeQualityReport Score(
        ReliableRecipeDefinition recipe,
        ReliableRecipeQualityOptions? options = null,
        ReliableRecipeQualityContext? context = null)
    {
        options ??= new ReliableRecipeQualityOptions();
        context ??= new ReliableRecipeQualityContext();

        var m1 = ReliableRecipePreflightValidator.Validate(recipe, ReliableRecipeRunMode.DryRun);
        var evidence = ReliableEvidenceCompletenessEvaluator.Score(recipe);
        var validation = ReliableValidationCompletenessEvaluator.Score(recipe);
        var target = ReliableTargetResolutionQualityEvaluator.Score(context.TargetDescriptor, context.PerceptionSnapshot, recipe.RiskProfile);
        var risk = ReliableRiskPostureEvaluator.Evaluate(recipe);
        var sandbox = ReliableSandboxQualityEvaluator.Score(context.SandboxProfile);
        var perception = context.PerceptionSnapshot is null
            ? new ReliablePerceptionConfidence(0.5, 0.5, [], recipe.RiskProfile.HasFlag(ReliableRecipeRiskProfile.SensitiveData))
            : ReliablePerceptionConfidenceEvaluator.Evaluate(context.PerceptionSnapshot, recipe.RiskProfile);
        var human = HumanInterventionQualityEvaluator.Score(context.HumanInterventionRequests ?? []);
        var structured = ReliableRecipeStructuredPrerequisiteEvaluator.Evaluate(recipe);

        var findings = new List<ReliableRecipeQualityFinding>();
        var warnings = new List<ReliableRecipeQualityFinding>();

        findings.AddRange(m1.BlockingIssues.Select(i => Finding(i.IssueId, i.Message, i.BlockId, "Fix the M1 reliable recipe preflight issue.")));
        findings.AddRange(EvidenceFindings(evidence));
        findings.AddRange(ValidationFindings(validation));
        findings.AddRange(TargetFindings(target));
        findings.AddRange(RiskFindings(risk));
        findings.AddRange(SandboxFindings(sandbox));
        findings.AddRange(HumanFindings(recipe, human, context.HumanInterventionRequests ?? []));

        if (recipe.CreatedFrom == ReliableRecipeCreatedFrom.RecorderDraft)
            findings.Add(Finding("recorder-draft-quality-draft-only", "Recorder draft cannot exceed draft-only readiness.", null, "Add review, validation, evidence and approval before any future dry-run eligibility."));
        if (recipe.Readiness == ReliableRecipeReadiness.RunnableSupervised || recipe.Readiness == ReliableRecipeReadiness.RunnableAssisted)
            warnings.Add(Warning("assisted-supervised-design-only", "Assisted and supervised run labels remain contract-only and blocked by composer defaults.", null, "Keep this recipe at draft or fixture dry-run readiness."));

        var categoryScores = CategoryScores(recipe, m1, evidence, validation, target, risk, sandbox, human);
        var categoryAverage = Math.Round(categoryScores.Average(c => c.Score), 2);
        var blocked = findings.Any(f => f.IsBlocking);
        var decision = blocked
            ? ReliableRecipeQualityDecision.Blocked
            : categoryAverage >= options.MinimumDryRunScore
                ? ReliableRecipeQualityDecision.PassDryRun
                : ReliableRecipeQualityDecision.NeedsReview;

        var nextActions = findings
            .Concat(warnings)
            .Select(f => f.RecommendedFix)
            .Distinct(StringComparer.Ordinal)
            .DefaultIfEmpty("Proceed with fixture-only dry-run review.")
            .ToArray();

        return new ReliableRecipeQualityReport(
            recipe.Id,
            blocked ? Math.Min(categoryAverage, 0.49) : categoryAverage,
            blocked ? ReliableRecipeReadiness.BlockedNeedsReview : recipe.Readiness,
            decision,
            categoryScores,
            findings,
            warnings,
            nextActions,
            evidence,
            validation,
            target,
            risk,
            sandbox,
            perception,
            human,
            structured);
    }

    private static IReadOnlyList<ReliableRecipeQualityCategoryScore> CategoryScores(
        ReliableRecipeDefinition recipe,
        ReliableRecipePreflightResult m1,
        EvidenceCompletenessScore evidence,
        ValidationCompletenessScore validation,
        TargetResolutionQualityScore target,
        RecipeRiskPostureReport risk,
        RecipeSandboxReadinessScore sandbox,
        HumanInterventionPlanQualityScore human)
    {
        var limitsScore = recipe.Limits is null ? 0 : m1.BlockingIssues.Any(i => i.IssueId.Contains("limit", StringComparison.OrdinalIgnoreCase)) ? 0.35 : 1;
        var recorderScore = recipe.CreatedFrom == ReliableRecipeCreatedFrom.RecorderDraft ? 0.35 : 1;
        var replayabilityScore = recipe.CreatedFrom == ReliableRecipeCreatedFrom.RecorderDraft ? 0.4 : Math.Min(evidence.Score, validation.Score);

        return
        [
            Category(ReliableRecipeQualityCategory.Limits, limitsScore),
            Category(ReliableRecipeQualityCategory.Validation, validation.Score),
            Category(ReliableRecipeQualityCategory.Evidence, evidence.Score),
            Category(ReliableRecipeQualityCategory.Risk, 1 - risk.RiskScore),
            Category(ReliableRecipeQualityCategory.TargetResolution, target.Score),
            Category(ReliableRecipeQualityCategory.Perception, target.SignalAgreement.Contradictions.Count == 0 ? Math.Max(target.Score, 0.65) : 0.35),
            Category(ReliableRecipeQualityCategory.Sandbox, sandbox.Score),
            Category(ReliableRecipeQualityCategory.HumanIntervention, human.Score),
            Category(ReliableRecipeQualityCategory.Replayability, replayabilityScore),
            Category(ReliableRecipeQualityCategory.RecorderDraftSafety, recorderScore)
        ];
    }

    private static ReliableRecipeQualityCategoryScore Category(ReliableRecipeQualityCategory category, double score) =>
        new(category, Math.Round(Math.Clamp(score, 0, 1), 2), score >= 0.72 ? ReliableRecipeQualityDecision.PassDryRun : score >= 0.5 ? ReliableRecipeQualityDecision.NeedsReview : ReliableRecipeQualityDecision.Blocked, []);

    private static IEnumerable<ReliableRecipeQualityFinding> EvidenceFindings(EvidenceCompletenessScore score) =>
        score.StepReports.SelectMany(r => r.MissingKinds.Select(k => Finding($"missing-evidence-{k}", $"Step {r.StepId} is missing {k} evidence expectation.", r.StepId, "Add reference-only evidence expectation before advancing.")));

    private static IEnumerable<ReliableRecipeQualityFinding> ValidationFindings(ValidationCompletenessScore score) =>
        score.CriticalBlockReports.SelectMany(r => r.MissingChecks.Select(k => Finding($"missing-validation-{k}", $"Block {r.BlockId} is missing {k} validation.", r.BlockId, "Add post-action validation checks before marking success.")));

    private static IEnumerable<ReliableRecipeQualityFinding> TargetFindings(TargetResolutionQualityScore score)
    {
        if (score.Decision == TargetResolutionQualityDecision.Blocked)
            yield return Finding("target-resolution-blocked", "Target resolution is blocked for this risk posture.", null, "Add deterministic DOM/accessibility/known-target signals or request human review.");
        if (score.Decision == TargetResolutionQualityDecision.NeedsHumanReview)
            yield return Finding("target-resolution-human-review", "Target resolution needs human review.", null, "Clarify target or add stronger deterministic signals.");
    }

    private static IEnumerable<ReliableRecipeQualityFinding> RiskFindings(RecipeRiskPostureReport risk) =>
        risk.BlockedReasons.Select(r => Finding($"risk-{r}", $"Risk posture blocks automation mode: {r}.", null, "Add approval, validation, evidence, or keep the recipe draft-only."));

    private static IEnumerable<ReliableRecipeQualityFinding> SandboxFindings(RecipeSandboxReadinessScore sandbox) =>
        sandbox.BlockedCapabilities.Select(b => Finding($"sandbox-{b}", $"Sandbox readiness blocks capability: {b}.", null, "Keep evaluation fixture-only until sandbox requirements are met."));

    private static IEnumerable<ReliableRecipeQualityFinding> HumanFindings(ReliableRecipeDefinition recipe, HumanInterventionPlanQualityScore human, IReadOnlyList<ReliableHumanInterventionRequest> requests)
    {
        var needsHuman = recipe.Blocks.Any(b => b.Kind == ReliableRecipeBlockKind.HumanIntervention || b.Risk.HasFlag(ReliableRecipeRiskProfile.Credentialed));
        if (needsHuman && requests.Count == 0)
            yield return Finding("missing-human-intervention-plan", "Recipe risk requires a human intervention plan.", null, "Add explicit handoff request with instructions and evidence refs.");
        foreach (var missing in human.MissingUserInstructions)
            yield return Finding("human-intervention-incomplete", missing, null, "Replace generic blocker text with actionable operator instructions.");
    }

    internal static ReliableRecipeQualityFinding Finding(string code, string message, string? blockId, string fix) =>
        new(code, ReliableRecipeQualitySeverity.Blocking, message, blockId, fix, IsBlocking: true);

    internal static ReliableRecipeQualityFinding Warning(string code, string message, string? blockId, string fix) =>
        new(code, ReliableRecipeQualitySeverity.Warning, message, blockId, fix, IsBlocking: false);
}

public static class ReliableEvidenceCompletenessEvaluator
{
    public static EvidenceCompletenessScore Score(ReliableRecipeDefinition recipe)
    {
        var reports = recipe.Blocks.Select(ScoreBlock).ToArray();
        var missing = reports.SelectMany(r => r.MissingKinds).Distinct().ToArray();
        var critical = reports.Where(r => r.IsCritical).ToArray();
        var criticalCoverage = critical.Length == 0 ? 1 : critical.Count(r => r.CanComplete) / (double)critical.Length;
        var totalExpected = reports.Sum(r => r.ExpectedKinds.Count);
        var totalProvided = reports.Sum(r => r.ProvidedKinds.Count);
        var score = totalExpected == 0 ? 0 : totalProvided / (double)totalExpected;
        return new EvidenceCompletenessScore(Math.Round(Math.Min(score, criticalCoverage), 2), missing, Math.Round(criticalCoverage, 2), reports);
    }

    private static RecipeStepEvidenceCompleteness ScoreBlock(ReliableRecipeBlock block)
    {
        var expected = ExpectedFor(block);
        var provided = block.EvidenceExpectations.SelectMany(MapEvidence).Distinct().ToArray();
        var missing = expected.Except(provided).ToArray();
        return new RecipeStepEvidenceCompleteness(block.Id, expected, provided, missing, IsCritical(block), missing.Length == 0 && !HasSecretLikeEvidence(block));
    }

    private static IReadOnlyList<EvidenceRequirementKind> ExpectedFor(ReliableRecipeBlock block)
    {
        var expected = new List<EvidenceRequirementKind>();
        if (IsCritical(block))
            expected.AddRange([EvidenceRequirementKind.BeforeState, EvidenceRequirementKind.AfterState, EvidenceRequirementKind.ActionProposal, EvidenceRequirementKind.ActionResult, EvidenceRequirementKind.ValidationReport, EvidenceRequirementKind.TimelineEvent]);
        if (block.Kind == ReliableRecipeBlockKind.FileDownloadEvidence)
            expected.Add(EvidenceRequirementKind.DownloadArtifact);
        if (block.Kind == ReliableRecipeBlockKind.HumanIntervention || block.Risk.HasFlag(ReliableRecipeRiskProfile.Financial) || block.Risk.HasFlag(ReliableRecipeRiskProfile.Irreversible))
            expected.Add(EvidenceRequirementKind.HumanApproval);
        if (block.Kind is ReliableRecipeBlockKind.BrowserAction or ReliableRecipeBlockKind.Extract)
            expected.Add(EvidenceRequirementKind.PerceptionSnapshot);
        return expected.Distinct().ToArray();
    }

    private static IEnumerable<EvidenceRequirementKind> MapEvidence(string evidence)
    {
        var lowered = evidence.ToLowerInvariant();
        if (lowered.Contains("before", StringComparison.Ordinal)) yield return EvidenceRequirementKind.BeforeState;
        if (lowered.Contains("after", StringComparison.Ordinal)) yield return EvidenceRequirementKind.AfterState;
        if (lowered.Contains("proposal", StringComparison.Ordinal)) yield return EvidenceRequirementKind.ActionProposal;
        if (lowered.Contains("result", StringComparison.Ordinal)) yield return EvidenceRequirementKind.ActionResult;
        if (lowered.Contains("validation", StringComparison.Ordinal)) yield return EvidenceRequirementKind.ValidationReport;
        if (lowered.Contains("redacted", StringComparison.Ordinal)) yield return EvidenceRequirementKind.RedactedSnapshot;
        if (lowered.Contains("timeline", StringComparison.Ordinal)) yield return EvidenceRequirementKind.TimelineEvent;
        if (lowered.Contains("approval", StringComparison.Ordinal) || lowered.Contains("human", StringComparison.Ordinal)) yield return EvidenceRequirementKind.HumanApproval;
        if (lowered.Contains("download", StringComparison.Ordinal) || lowered.Contains("artifact", StringComparison.Ordinal)) yield return EvidenceRequirementKind.DownloadArtifact;
        if (lowered.Contains("perception", StringComparison.Ordinal) || lowered.Contains("ocr", StringComparison.Ordinal) || lowered.Contains("dom", StringComparison.Ordinal)) yield return EvidenceRequirementKind.PerceptionSnapshot;
    }

    private static bool IsCritical(ReliableRecipeBlock block) =>
        block.Kind is ReliableRecipeBlockKind.BrowserAction or ReliableRecipeBlockKind.FileDownloadEvidence or ReliableRecipeBlockKind.CaptureArtifact or ReliableRecipeBlockKind.ConnectorDraft or ReliableRecipeBlockKind.DesktopFuture or ReliableRecipeBlockKind.SandboxFuture or ReliableRecipeBlockKind.HumanIntervention ||
        block.Risk.HasFlag(ReliableRecipeRiskProfile.ExternalSideEffect) ||
        block.Risk.HasFlag(ReliableRecipeRiskProfile.Financial) ||
        block.Risk.HasFlag(ReliableRecipeRiskProfile.Irreversible) ||
        block.Risk.HasFlag(ReliableRecipeRiskProfile.SensitiveData);

    private static bool HasSecretLikeEvidence(ReliableRecipeBlock block) =>
        block.EvidenceExpectations.Any(e => e.Contains("password", StringComparison.OrdinalIgnoreCase) || e.Contains("token", StringComparison.OrdinalIgnoreCase) || e.Contains("secret", StringComparison.OrdinalIgnoreCase));
}

public static class ReliableValidationCompletenessEvaluator
{
    public static ValidationCompletenessScore Score(ReliableRecipeDefinition recipe)
    {
        var reports = recipe.Blocks.Select(ScoreBlock).ToArray();
        var missing = reports.SelectMany(r => r.MissingChecks).Distinct().ToArray();
        var totalRequired = reports.Sum(r => r.RequiredChecks.Count);
        var totalProvided = reports.Sum(r => r.ProvidedChecks.Count);
        var score = totalRequired == 0 ? 0 : totalProvided / (double)totalRequired;
        return new ValidationCompletenessScore(Math.Round(score, 2), missing, reports);
    }

    private static RecipeBlockValidationCompleteness ScoreBlock(ReliableRecipeBlock block)
    {
        var required = RequiredFor(block);
        var provided = block.ValidationRequirements.SelectMany(MapValidation).Distinct().ToArray();
        var missing = required.Except(provided).ToArray();
        return new RecipeBlockValidationCompleteness(block.Id, required, provided, missing, missing.Length == 0);
    }

    private static IReadOnlyList<ReliableValidationCheckKind> RequiredFor(ReliableRecipeBlock block) =>
        block.Kind switch
        {
            ReliableRecipeBlockKind.BrowserAction => [ReliableValidationCheckKind.VisibleTextExists, ReliableValidationCheckKind.TimelineEventCreated],
            ReliableRecipeBlockKind.FileDownloadEvidence => [ReliableValidationCheckKind.FileDownloaded, ReliableValidationCheckKind.TimelineEventCreated],
            ReliableRecipeBlockKind.Extract => [ReliableValidationCheckKind.VisibleTextExists],
            ReliableRecipeBlockKind.Loop => [ReliableValidationCheckKind.ManualConfirmationRequired],
            ReliableRecipeBlockKind.HumanIntervention => [ReliableValidationCheckKind.ManualConfirmationRequired, ReliableValidationCheckKind.TimelineEventCreated],
            ReliableRecipeBlockKind.ConnectorDraft or ReliableRecipeBlockKind.DesktopFuture or ReliableRecipeBlockKind.SandboxFuture => [ReliableValidationCheckKind.ManualConfirmationRequired],
            _ => []
        };

    private static IEnumerable<ReliableValidationCheckKind> MapValidation(string validation)
    {
        var lowered = validation.ToLowerInvariant();
        if (lowered.Contains("visible", StringComparison.Ordinal) || lowered.Contains("text", StringComparison.Ordinal) || lowered.Contains("schema", StringComparison.Ordinal) || lowered.Contains("content", StringComparison.Ordinal)) yield return ReliableValidationCheckKind.VisibleTextExists;
        if (lowered.Contains("download", StringComparison.Ordinal) || lowered.Contains("file", StringComparison.Ordinal)) yield return ReliableValidationCheckKind.FileDownloaded;
        if (lowered.Contains("timeline", StringComparison.Ordinal)) yield return ReliableValidationCheckKind.TimelineEventCreated;
        if (lowered.Contains("manual", StringComparison.Ordinal) || lowered.Contains("human", StringComparison.Ordinal) || lowered.Contains("terminate", StringComparison.Ordinal) || lowered.Contains("confirmation", StringComparison.Ordinal)) yield return ReliableValidationCheckKind.ManualConfirmationRequired;
    }
}

public static class ReliableTargetResolutionQualityEvaluator
{
    public static TargetResolutionQualityScore Score(
        ReliableTargetDescriptor? target,
        ReliablePerceptionStackSnapshot? perception,
        ReliableRecipeRiskProfile risk)
    {
        if (target is null)
            return new TargetResolutionQualityScore(0.45, ReliableActionResolutionMode.HumanIntervention, new TargetResolutionSignalAgreement(false, false, false, false, ["missing-target-descriptor"]), "missing target descriptor", IsSensitive(risk), TargetResolutionQualityDecision.NeedsHumanReview);

        var agreement = Agreement(perception);
        var sensitive = IsSensitive(risk) || target.Confidence.SignalsUsed.Contains(ReliableActionResolutionMode.AiSemanticFallback);
        var score = target.Confidence.Score;
        if (target.FallbackStrategy is ReliableActionResolutionMode.KnownTarget or ReliableActionResolutionMode.StableSelector or ReliableActionResolutionMode.DomOrCdpSnapshot or ReliableActionResolutionMode.AccessibilityTree)
            score = Math.Max(score, 0.85);
        if (target.Confidence.SignalsUsed.SequenceEqual([ReliableActionResolutionMode.OcrRegion]))
            score = IsSensitive(risk) ? 0.25 : Math.Min(score, 0.68);
        if (target.FallbackStrategy == ReliableActionResolutionMode.AiSemanticFallback && IsSensitive(risk))
            return new TargetResolutionQualityScore(0.1, target.FallbackStrategy, agreement, target.Confidence.AmbiguityReason, true, TargetResolutionQualityDecision.Blocked);
        if (target.RelativeCoordinates is not null)
            score = Math.Min(score, 0.35);
        if (!string.IsNullOrWhiteSpace(target.Confidence.AmbiguityReason))
            score = Math.Min(score, 0.45);
        if (agreement.Contradictions.Count > 0)
            score = Math.Min(score, 0.4);

        var decision = score >= 0.72
            ? TargetResolutionQualityDecision.AcceptForDryRun
            : IsSensitive(risk) || !string.IsNullOrWhiteSpace(target.Confidence.AmbiguityReason)
                ? TargetResolutionQualityDecision.NeedsHumanReview
                : TargetResolutionQualityDecision.NeedsMoreSignals;

        if (target.Confidence.SignalsUsed.SequenceEqual([ReliableActionResolutionMode.OcrRegion]) && IsSensitive(risk))
            decision = TargetResolutionQualityDecision.Blocked;

        return new TargetResolutionQualityScore(Math.Round(score, 2), target.FallbackStrategy, agreement, target.Confidence.AmbiguityReason, sensitive, decision);
    }

    private static TargetResolutionSignalAgreement Agreement(ReliablePerceptionStackSnapshot? perception)
    {
        if (perception is null)
            return new TargetResolutionSignalAgreement(false, false, false, false, []);

        var contradictions = ReliablePerceptionConfidenceEvaluator.Evaluate(perception, ReliableRecipeRiskProfile.ReadOnly).Contradictions;
        var dom = perception.DomSignals.Count > 0;
        var accessibility = perception.AccessibilitySignals.Count > 0;
        var ocr = perception.OcrSignals.Count > 0;
        var visual = perception.VisualSignals.Count > 0 || perception.SetOfMarksSignals.Count > 0;
        return new TargetResolutionSignalAgreement(dom && accessibility && contradictions.Count == 0, dom && ocr && contradictions.Count == 0, accessibility && ocr && contradictions.Count == 0, visual && (dom || ocr) && contradictions.Count == 0, contradictions);
    }

    private static bool IsSensitive(ReliableRecipeRiskProfile risk) =>
        risk.HasFlag(ReliableRecipeRiskProfile.Credentialed) ||
        risk.HasFlag(ReliableRecipeRiskProfile.Financial) ||
        risk.HasFlag(ReliableRecipeRiskProfile.Destructive) ||
        risk.HasFlag(ReliableRecipeRiskProfile.Irreversible) ||
        risk.HasFlag(ReliableRecipeRiskProfile.ExternalSideEffect) ||
        risk.HasFlag(ReliableRecipeRiskProfile.SensitiveData);
}

public static class ReliableRiskPostureEvaluator
{
    public static RecipeRiskPostureReport Evaluate(ReliableRecipeDefinition recipe)
    {
        var risk = recipe.RiskProfile | recipe.Blocks.Aggregate(ReliableRecipeRiskProfile.None, (acc, b) => acc | b.Risk);
        var blocked = new List<string>();
        var approvals = new List<string>();

        if (risk.HasFlag(ReliableRecipeRiskProfile.Financial)) { blocked.Add("financial-action"); approvals.Add("financial-approval"); }
        if (risk.HasFlag(ReliableRecipeRiskProfile.Destructive)) { blocked.Add("destructive-action"); approvals.Add("destructive-approval"); }
        if (risk.HasFlag(ReliableRecipeRiskProfile.Irreversible)) { blocked.Add("irreversible-action"); approvals.Add("irreversible-approval"); }
        if (risk.HasFlag(ReliableRecipeRiskProfile.ExternalSideEffect)) approvals.Add("external-side-effect-approval");
        if (risk.HasFlag(ReliableRecipeRiskProfile.Credentialed)) blocked.Add("credentialed-human-handoff");
        if (recipe.Id.Contains("captcha", StringComparison.OrdinalIgnoreCase) || recipe.Id.Contains("two_factor", StringComparison.OrdinalIgnoreCase))
            blocked.Add("challenge-human-handoff");
        if (recipe.Id.Contains("government", StringComparison.OrdinalIgnoreCase) || recipe.Id.Contains("fiscal", StringComparison.OrdinalIgnoreCase))
            blocked.Add("government-or-fiscal-review");

        var riskScore = 0.05;
        if (risk.HasFlag(ReliableRecipeRiskProfile.ExternalSideEffect)) riskScore += 0.2;
        if (risk.HasFlag(ReliableRecipeRiskProfile.Credentialed)) riskScore += 0.2;
        if (risk.HasFlag(ReliableRecipeRiskProfile.SensitiveData)) riskScore += 0.15;
        if (risk.HasFlag(ReliableRecipeRiskProfile.Financial)) riskScore += 0.25;
        if (risk.HasFlag(ReliableRecipeRiskProfile.Destructive) || risk.HasFlag(ReliableRecipeRiskProfile.Irreversible)) riskScore += 0.25;

        return new RecipeRiskPostureReport(
            risk,
            Math.Round(Math.Clamp(riskScore, 0, 1), 2),
            risk.HasFlag(ReliableRecipeRiskProfile.ExternalSideEffect),
            risk.HasFlag(ReliableRecipeRiskProfile.SensitiveData) || risk.HasFlag(ReliableRecipeRiskProfile.Credentialed),
            risk.HasFlag(ReliableRecipeRiskProfile.Irreversible) || risk.HasFlag(ReliableRecipeRiskProfile.Destructive),
            approvals.Distinct().ToArray(),
            blocked.Distinct().ToArray());
    }
}

public static class ReliableSandboxQualityEvaluator
{
    public static RecipeSandboxReadinessScore Score(ComputerUseSandboxProfile? profile)
    {
        if (profile is null)
            return new RecipeSandboxReadinessScore(0.55, ReliableSandboxIsolationMode.DesignOnly, [], [], CanEvaluateInFixture: true, CanEvaluateInFutureSandbox: false);

        var baseReport = ReliableSandboxReadinessEvaluator.Evaluate(profile);
        var missing = new List<SandboxRequirementKind>();
        if (profile.RollbackPolicy.Contains("none", StringComparison.OrdinalIgnoreCase)) missing.Add(SandboxRequirementKind.Rollback);
        if (profile.NetworkPolicy.Contains("unrestricted", StringComparison.OrdinalIgnoreCase)) missing.Add(SandboxRequirementKind.NetworkPolicy);
        if (profile.FilesystemPolicy.Contains("unrestricted", StringComparison.OrdinalIgnoreCase)) missing.Add(SandboxRequirementKind.FilesystemPolicy);
        if (profile.CredentialPolicy.Contains("raw", StringComparison.OrdinalIgnoreCase)) missing.Add(SandboxRequirementKind.CredentialPolicy);
        if (string.IsNullOrWhiteSpace(profile.EvidencePolicy)) missing.Add(SandboxRequirementKind.EvidencePolicy);
        if (profile.MaxRuntimeSeconds <= 0) missing.Add(SandboxRequirementKind.RuntimeLimit);
        if (profile.AllowedSurfaces.Any(s => s is ReliableComputerUseSurface.Desktop or ReliableComputerUseSurface.RemoteSession) && profile.IsolationMode != ReliableSandboxIsolationMode.DryRunFixture) missing.Add(SandboxRequirementKind.SurfaceIsolation);

        var score = baseReport.Ready ? 0.9 : Math.Max(0.1, 0.65 - (missing.Count * 0.12) - (baseReport.BlockedCapabilities.Count * 0.15));
        return new RecipeSandboxReadinessScore(
            Math.Round(Math.Clamp(score, 0, 1), 2),
            profile.IsolationMode,
            missing.Distinct().ToArray(),
            baseReport.BlockedCapabilities,
            profile.IsolationMode is ReliableSandboxIsolationMode.DesignOnly or ReliableSandboxIsolationMode.DryRunFixture && !baseReport.BlockedCapabilities.Contains("desktop-live-surface"),
            false);
    }
}

public static class HumanInterventionQualityEvaluator
{
    public static HumanInterventionPlanQualityScore Score(IReadOnlyList<ReliableHumanInterventionRequest> requests)
    {
        if (requests.Count == 0)
            return new HumanInterventionPlanQualityScore(1, [], [], false, false);

        var missing = new List<string>();
        var ambiguous = new List<ReliableHumanInterventionReason>();
        foreach (var request in requests)
        {
            if (IsGeneric(request.WhatHappened) || IsGeneric(request.WhatWasTried) || IsGeneric(request.WhatUserMustDo))
                missing.Add($"Human intervention {request.Reason} needs specific operator instructions.");
            if (request.Options.Count == 0 && request.Reason is ReliableHumanInterventionReason.AmbiguousTarget)
                ambiguous.Add(request.Reason);
            if (request.EvidenceRefs.Count == 0)
                missing.Add($"Human intervention {request.Reason} needs evidence refs.");
        }

        var score = Math.Max(0.1, 1 - (missing.Count * 0.25) - (ambiguous.Count * 0.25));
        return new HumanInterventionPlanQualityScore(Math.Round(score, 2), missing, ambiguous, requests.Any(r => r.CanContinueAfterUserAction), requests.All(r => r.EvidenceRefs.Count > 0));
    }

    private static bool IsGeneric(string value) =>
        string.IsNullOrWhiteSpace(value) ||
        value.Equals("blocked", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("needs review", StringComparison.OrdinalIgnoreCase) ||
        value.Length < 12;
}
