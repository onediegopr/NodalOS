namespace OneBrain.Core.Recipes;

public sealed record ReliableRecipeFixtureEvalScenario(
    string ScenarioId,
    string Name,
    string Description,
    string FixtureId,
    ReliableRecipeFixtureEvalSourceKind SourceKind,
    string InitialFixtureState,
    ReliableRecipeFixtureEvalExpectedOutcome ExpectedOutcome,
    ReliableRecipeRiskProfile RiskConstraints,
    IReadOnlyList<EvidenceRequirementKind> EvidenceExpectations,
    IReadOnlyList<ReliableValidationCheckKind> ValidationExpectations,
    IReadOnlyList<ReliableRecipeFixtureEvalFailureKind> AllowedFailureKinds,
    int IterationCount,
    IReadOnlyList<string> Tags,
    IReadOnlyList<ReliableRecipeFixtureEvalIterationProfile> IterationProfiles)
{
    public bool FixtureOnly => true;
    public bool LiveRuntimeEnabled => false;
    public bool UsesNetwork => false;
    public bool UsesProviderOrLlm => false;
}

public enum ReliableRecipeFixtureEvalSourceKind
{
    ManualRecipe,
    RecorderDraft,
    SopDraft,
    ImportedFixture,
    PolicyRegressionFixture
}

public enum ReliableRecipeFixtureEvalExpectedOutcome
{
    DryRunCandidate,
    DraftOnly,
    NeedsReview,
    Blocked,
    HumanHandoffRequired,
    ValidationFailureExpected,
    EvidenceFailureExpected
}

public sealed record ReliableRecipeFixtureEvalIterationProfile(
    string VariantId,
    IReadOnlyList<ReliableRecipeFixtureEvalFailureKind> InjectedFailureKinds,
    double QualityScoreDelta = 0,
    string? VariantSummary = null);

public sealed record ReliableRecipeFixtureEvalRun(
    string RunId,
    string ScenarioId,
    IReadOnlyList<ReliableRecipeFixtureEvalIterationResult> IterationResults,
    string Summary,
    ReliableRecipeFixtureEvalMetrics Metrics,
    ReliableRecipeFixtureEvalFinalDecision FinalDecision,
    ReliableRecipeFixtureEvalReport Report)
{
    public bool FixtureOnly => true;
    public bool Deterministic => true;
    public bool LiveEvalAdded => false;
    public bool RuntimeNotEnabled => true;
}

public sealed record ReliableRecipeFixtureEvalIterationResult(
    int Iteration,
    ReliableRecipePolicyDecision PreflightDecision,
    double QualityScore,
    double EvidenceScore,
    double ValidationScore,
    double TargetResolutionScore,
    double SandboxReadinessScore,
    double HumanInterventionScore,
    IReadOnlyList<ReliableRecipeFixtureEvalFailureKind> FailureKinds,
    IReadOnlyList<ReliableRecipeQualityFinding> Findings,
    bool PassedExpectedOutcome);

public enum ReliableRecipeFixtureEvalFailureKind
{
    None,
    TargetNotFound,
    TargetAmbiguous,
    OcrOnlySensitiveTarget,
    ValidationMissing,
    ValidationFailed,
    EvidenceMissing,
    PolicyBlocked,
    RiskBlocked,
    SecretExposureBlocked,
    LoopLimitReached,
    HumanHandoffRequired,
    SandboxNotReady,
    RecorderDraftNotReviewed,
    FixtureMismatch,
    ExpectedBlockDidNotOccur,
    UnexpectedPass
}

public sealed record ReliableRecipeFixtureEvalMetrics(
    int TotalIterations,
    int PassedIterations,
    int FailedIterations,
    int ExpectedBlockIterations,
    int UnexpectedPassIterations,
    double SuccessRate,
    double ExpectedOutcomeMatchRate,
    double AverageQualityScore,
    double AverageEvidenceScore,
    double AverageValidationScore,
    double AverageTargetResolutionScore,
    double AverageSandboxReadinessScore,
    double HumanInterventionRate,
    double FlakinessScore,
    double EvidenceCompletenessScore,
    double ValidationCompletenessScore);

public sealed record ReliableRecipeFlakinessReport(
    double FlakinessScore,
    ReliableRecipeFlakinessLevel FlakinessLevel,
    IReadOnlyList<ReliableRecipeFixtureEvalFailureKind> ObservedFailureKinds,
    IReadOnlyList<string> InconsistentSignals,
    string Recommendation);

public enum ReliableRecipeFlakinessLevel
{
    None,
    Low,
    Medium,
    High
}

public enum ReliableRecipeFixtureEvalFinalDecision
{
    FixturePass,
    FixturePassWithWarnings,
    NeedsReview,
    Blocked,
    RegressionDetected
}

public sealed record ReliableRecipeFixtureEvalReport(
    string ReportId,
    string ScenarioId,
    ReliableRecipeFixtureEvalExpectedOutcome ExpectedOutcome,
    ReliableRecipeFixtureEvalFinalDecision FinalDecision,
    ReliableRecipeFixtureEvalMetrics Metrics,
    ReliableRecipeFlakinessReport Flakiness,
    IReadOnlyList<ReliableRecipeFixtureEvalFailureKind> FailureTaxonomy,
    IReadOnlyList<string> ProductFacingSummaries,
    ReliableRecipeLabEvalPanel LabEvalPanel)
{
    public bool FixtureOnly => true;
    public bool Deterministic => true;
    public bool RuntimeEnabled => false;
    public bool BrowserExecutionEnabled => false;
    public bool DesktopExecutionEnabled => false;
    public bool ProviderCallEnabled => false;
    public bool RecorderRuntimeEnabled => false;
    public bool OcrLiveActivationEnabled => false;
}

public static class ReliableRecipeFixtureEvalRunner
{
    public static ReliableRecipeFixtureEvalRun Evaluate(ReliableRecipeFixtureEvalScenario scenario)
    {
        var fixture = ResolveFixture(scenario);
        var profiles = ProfilesFor(scenario).ToArray();
        var results = profiles.Select((profile, index) => EvaluateIteration(scenario, fixture, profile, index + 1)).ToArray();
        var metrics = Metrics(results);
        var flakiness = Flakiness(results, metrics);
        var finalDecision = FinalDecision(scenario, results, metrics);
        var taxonomy = results.SelectMany(r => r.FailureKinds).Where(f => f != ReliableRecipeFixtureEvalFailureKind.None).Distinct().ToArray();
        var panel = ReliableRecipeFixtureEvalReportMapper.ToLabPanel(scenario, metrics, flakiness, taxonomy, finalDecision);
        var report = new ReliableRecipeFixtureEvalReport(
            $"report.{scenario.ScenarioId}",
            scenario.ScenarioId,
            scenario.ExpectedOutcome,
            finalDecision,
            metrics,
            flakiness,
            taxonomy,
            ProductSummaries(scenario, finalDecision, metrics, taxonomy),
            panel);

        return new ReliableRecipeFixtureEvalRun(
            $"eval.{scenario.ScenarioId}",
            scenario.ScenarioId,
            results,
            SummaryFor(scenario, finalDecision),
            metrics,
            finalDecision,
            report);
    }

    public static ReliableRecipeLabViewModel AttachEvalPanel(ReliableRecipeLabViewModel lab, ReliableRecipeFixtureEvalRun run) =>
        lab with { EvalPanel = run.Report.LabEvalPanel };

    private static ReliableRecipeEvalFixtureBundle ResolveFixture(ReliableRecipeFixtureEvalScenario scenario) =>
        scenario.SourceKind switch
        {
            ReliableRecipeFixtureEvalSourceKind.RecorderDraft when scenario.FixtureId == "invoice_download_validated_demonstration" => Bundle(ReliableRecipeRecorderFixtureCatalog.InvoiceDownloadWithValidation()),
            ReliableRecipeFixtureEvalSourceKind.RecorderDraft => Bundle(ReliableRecipeRecorderFixtureCatalog.Get(scenario.FixtureId).Draft),
            ReliableRecipeFixtureEvalSourceKind.PolicyRegressionFixture => Bundle(ReliableRecipeLabFixtureCatalog.Get("safe_invoice_download_quality_pass")),
            _ => Bundle(ReliableRecipeLabFixtureCatalog.Get(scenario.FixtureId))
        };

    private static ReliableRecipeEvalFixtureBundle Bundle(RecorderToRecipeDraft draft) =>
        new(draft.Recipe, draft.PreflightReport, draft.QualityReport, draft.LabViewModel, draft);

    private static ReliableRecipeEvalFixtureBundle Bundle(ReliableRecipeLabFixture fixture) =>
        new(fixture.Recipe, fixture.PreflightReport, fixture.PreflightReport.QualityReport, fixture.ViewModel, null);

    private static IReadOnlyList<ReliableRecipeFixtureEvalIterationProfile> ProfilesFor(ReliableRecipeFixtureEvalScenario scenario)
    {
        if (scenario.IterationProfiles.Count > 0)
            return scenario.IterationProfiles;

        var count = Math.Max(1, scenario.IterationCount);
        return Enumerable.Range(1, count)
            .Select(i => new ReliableRecipeFixtureEvalIterationProfile($"stable.{i}", []))
            .ToArray();
    }

    private static ReliableRecipeFixtureEvalIterationResult EvaluateIteration(
        ReliableRecipeFixtureEvalScenario scenario,
        ReliableRecipeEvalFixtureBundle fixture,
        ReliableRecipeFixtureEvalIterationProfile profile,
        int iteration)
    {
        var quality = fixture.QualityReport;
        var failures = FailureKinds(scenario, fixture, profile).Distinct().ToArray();
        var qualityScore = Math.Round(Math.Clamp(quality.OverallScore + profile.QualityScoreDelta, 0, 1), 2);
        var passed = ExpectedOutcomeMatches(scenario.ExpectedOutcome, fixture, failures);
        var findings = quality.BlockingFindings.Concat(quality.Warnings).ToArray();

        return new ReliableRecipeFixtureEvalIterationResult(
            iteration,
            fixture.PreflightReport.PolicyDecision,
            qualityScore,
            quality.EvidenceCompleteness.Score,
            quality.ValidationCompleteness.Score,
            quality.TargetResolutionQuality.Score,
            quality.SandboxReadiness.Score,
            quality.HumanInterventionPlanQuality.Score,
            failures.Length == 0 ? [ReliableRecipeFixtureEvalFailureKind.None] : failures,
            findings,
            passed);
    }

    private static IEnumerable<ReliableRecipeFixtureEvalFailureKind> FailureKinds(
        ReliableRecipeFixtureEvalScenario scenario,
        ReliableRecipeEvalFixtureBundle fixture,
        ReliableRecipeFixtureEvalIterationProfile profile)
    {
        foreach (var injected in profile.InjectedFailureKinds)
            yield return injected;

        var quality = fixture.QualityReport;
        if (quality.ValidationCompleteness.MissingValidations.Count > 0)
            yield return ReliableRecipeFixtureEvalFailureKind.ValidationMissing;
        if (quality.EvidenceCompleteness.MissingEvidenceKinds.Count > 0)
            yield return ReliableRecipeFixtureEvalFailureKind.EvidenceMissing;
        if (quality.TargetResolutionQuality.Decision == TargetResolutionQualityDecision.Blocked &&
            quality.TargetResolutionQuality.ResolutionMode == ReliableActionResolutionMode.OcrRegion)
            yield return ReliableRecipeFixtureEvalFailureKind.OcrOnlySensitiveTarget;
        if (!string.IsNullOrWhiteSpace(quality.TargetResolutionQuality.Ambiguity))
            yield return ReliableRecipeFixtureEvalFailureKind.TargetAmbiguous;
        if (quality.RiskPosture.BlockedReasons.Count > 0)
            yield return ReliableRecipeFixtureEvalFailureKind.RiskBlocked;
        if (quality.Decision == ReliableRecipeQualityDecision.Blocked || fixture.PreflightReport.PolicyDecision == ReliableRecipePolicyDecision.Reject)
            yield return ReliableRecipeFixtureEvalFailureKind.PolicyBlocked;
        if (quality.SandboxReadiness.BlockedCapabilities.Count > 0)
            yield return ReliableRecipeFixtureEvalFailureKind.SandboxNotReady;
        if (fixture.Draft?.ReviewState is RecorderDraftReviewState.BlockedChallenge)
            yield return ReliableRecipeFixtureEvalFailureKind.HumanHandoffRequired;
        if (fixture.Draft?.ReviewState is RecorderDraftReviewState.BlockedSensitiveInput)
            yield return ReliableRecipeFixtureEvalFailureKind.SecretExposureBlocked;
        if (fixture.Draft is not null && fixture.Draft.ReviewState != RecorderDraftReviewState.DryRunCandidate)
            yield return ReliableRecipeFixtureEvalFailureKind.RecorderDraftNotReviewed;
        if (scenario.ExpectedOutcome is ReliableRecipeFixtureEvalExpectedOutcome.Blocked or ReliableRecipeFixtureEvalExpectedOutcome.HumanHandoffRequired &&
            fixture.PreflightReport.PolicyDecision != ReliableRecipePolicyDecision.Reject &&
            quality.Decision == ReliableRecipeQualityDecision.PassDryRun)
            yield return ReliableRecipeFixtureEvalFailureKind.UnexpectedPass;
    }

    private static bool ExpectedOutcomeMatches(
        ReliableRecipeFixtureEvalExpectedOutcome expected,
        ReliableRecipeEvalFixtureBundle fixture,
        IReadOnlyList<ReliableRecipeFixtureEvalFailureKind> failures) =>
        expected switch
        {
            ReliableRecipeFixtureEvalExpectedOutcome.DryRunCandidate => fixture.PreflightReport.ModeAllowed == ReliableRecipeRunMode.DryRun && !failures.Contains(ReliableRecipeFixtureEvalFailureKind.UnexpectedPass),
            ReliableRecipeFixtureEvalExpectedOutcome.DraftOnly => fixture.PreflightReport.ModeAllowed == ReliableRecipeRunMode.DraftOnly,
            ReliableRecipeFixtureEvalExpectedOutcome.NeedsReview => failures.Any(f => f is ReliableRecipeFixtureEvalFailureKind.TargetAmbiguous or ReliableRecipeFixtureEvalFailureKind.RecorderDraftNotReviewed or ReliableRecipeFixtureEvalFailureKind.ValidationMissing or ReliableRecipeFixtureEvalFailureKind.EvidenceMissing),
            ReliableRecipeFixtureEvalExpectedOutcome.Blocked => failures.Any(f => f is ReliableRecipeFixtureEvalFailureKind.PolicyBlocked or ReliableRecipeFixtureEvalFailureKind.RiskBlocked or ReliableRecipeFixtureEvalFailureKind.SandboxNotReady or ReliableRecipeFixtureEvalFailureKind.OcrOnlySensitiveTarget),
            ReliableRecipeFixtureEvalExpectedOutcome.HumanHandoffRequired => failures.Contains(ReliableRecipeFixtureEvalFailureKind.HumanHandoffRequired) || fixture.PreflightReport.RequiredHumanInterventions.Count > 0,
            ReliableRecipeFixtureEvalExpectedOutcome.ValidationFailureExpected => failures.Any(f => f is ReliableRecipeFixtureEvalFailureKind.ValidationMissing or ReliableRecipeFixtureEvalFailureKind.ValidationFailed),
            ReliableRecipeFixtureEvalExpectedOutcome.EvidenceFailureExpected => failures.Contains(ReliableRecipeFixtureEvalFailureKind.EvidenceMissing),
            _ => false
        };

    private static ReliableRecipeFixtureEvalMetrics Metrics(IReadOnlyList<ReliableRecipeFixtureEvalIterationResult> results)
    {
        var total = results.Count;
        var passed = results.Count(r => r.PassedExpectedOutcome);
        var unexpected = results.Count(r => r.FailureKinds.Contains(ReliableRecipeFixtureEvalFailureKind.UnexpectedPass));
        var expectedBlocks = results.Count(r => r.PassedExpectedOutcome && r.FailureKinds.Any(f => f is ReliableRecipeFixtureEvalFailureKind.PolicyBlocked or ReliableRecipeFixtureEvalFailureKind.RiskBlocked or ReliableRecipeFixtureEvalFailureKind.HumanHandoffRequired or ReliableRecipeFixtureEvalFailureKind.OcrOnlySensitiveTarget or ReliableRecipeFixtureEvalFailureKind.SandboxNotReady));
        var human = results.Count(r => r.FailureKinds.Contains(ReliableRecipeFixtureEvalFailureKind.HumanHandoffRequired));
        var flakiness = total <= 1 ? 0 : results.Select(r => string.Join("|", r.FailureKinds.OrderBy(f => f))).Distinct().Count() == 1 ? 0 : Math.Round((results.Select(r => string.Join("|", r.FailureKinds.OrderBy(f => f))).Distinct().Count() - 1) / (double)total, 2);

        return new ReliableRecipeFixtureEvalMetrics(
            total,
            passed,
            total - passed,
            expectedBlocks,
            unexpected,
            Math.Round(passed / (double)total, 2),
            Math.Round(passed / (double)total, 2),
            Average(results, r => r.QualityScore),
            Average(results, r => r.EvidenceScore),
            Average(results, r => r.ValidationScore),
            Average(results, r => r.TargetResolutionScore),
            Average(results, r => r.SandboxReadinessScore),
            Math.Round(human / (double)total, 2),
            flakiness,
            Average(results, r => r.EvidenceScore),
            Average(results, r => r.ValidationScore));
    }

    private static ReliableRecipeFlakinessReport Flakiness(IReadOnlyList<ReliableRecipeFixtureEvalIterationResult> results, ReliableRecipeFixtureEvalMetrics metrics)
    {
        var observed = results.SelectMany(r => r.FailureKinds).Where(f => f != ReliableRecipeFixtureEvalFailureKind.None).Distinct().ToArray();
        IReadOnlyList<string> inconsistent = results.Select(r => string.Join("|", r.FailureKinds.OrderBy(f => f))).Distinct().Count() > 1
            ? ["Predefined fixture variants produced different failure taxonomy."]
            : Array.Empty<string>();
        var level = metrics.FlakinessScore switch
        {
            0 => ReliableRecipeFlakinessLevel.None,
            < 0.25 => ReliableRecipeFlakinessLevel.Low,
            < 0.5 => ReliableRecipeFlakinessLevel.Medium,
            _ => ReliableRecipeFlakinessLevel.High
        };

        return new ReliableRecipeFlakinessReport(
            metrics.FlakinessScore,
            level,
            observed,
            inconsistent,
            metrics.FlakinessScore == 0 ? "Fixture scenario is deterministic." : "Review fixture variants before any future runtime evaluation.");
    }

    private static ReliableRecipeFixtureEvalFinalDecision FinalDecision(
        ReliableRecipeFixtureEvalScenario scenario,
        IReadOnlyList<ReliableRecipeFixtureEvalIterationResult> results,
        ReliableRecipeFixtureEvalMetrics metrics)
    {
        if (results.Any(r => r.FailureKinds.Contains(ReliableRecipeFixtureEvalFailureKind.UnexpectedPass)))
            return ReliableRecipeFixtureEvalFinalDecision.RegressionDetected;
        if (metrics.ExpectedOutcomeMatchRate == 1 && metrics.FlakinessScore == 0 && scenario.ExpectedOutcome == ReliableRecipeFixtureEvalExpectedOutcome.DryRunCandidate)
            return ReliableRecipeFixtureEvalFinalDecision.FixturePass;
        if (metrics.ExpectedOutcomeMatchRate == 1 && metrics.ExpectedBlockIterations > 0)
            return ReliableRecipeFixtureEvalFinalDecision.FixturePassWithWarnings;
        if (metrics.ExpectedOutcomeMatchRate == 1 && metrics.FlakinessScore > 0)
            return ReliableRecipeFixtureEvalFinalDecision.FixturePassWithWarnings;
        if (metrics.ExpectedOutcomeMatchRate >= 0.5)
            return ReliableRecipeFixtureEvalFinalDecision.NeedsReview;
        return ReliableRecipeFixtureEvalFinalDecision.Blocked;
    }

    private static IReadOnlyList<string> ProductSummaries(
        ReliableRecipeFixtureEvalScenario scenario,
        ReliableRecipeFixtureEvalFinalDecision decision,
        ReliableRecipeFixtureEvalMetrics metrics,
        IReadOnlyList<ReliableRecipeFixtureEvalFailureKind> taxonomy) =>
    [
        "Fixture eval only. Runtime not enabled.",
        $"Scenario result: {decision}.",
        $"Expected outcome match rate: {metrics.ExpectedOutcomeMatchRate:0.00}.",
        taxonomy.Count == 0 ? "No fixture failure kinds observed." : $"Failure taxonomy: {string.Join(", ", taxonomy)}."
    ];

    private static string SummaryFor(ReliableRecipeFixtureEvalScenario scenario, ReliableRecipeFixtureEvalFinalDecision finalDecision) =>
        finalDecision == ReliableRecipeFixtureEvalFinalDecision.RegressionDetected
            ? $"Scenario {scenario.ScenarioId} detected an unexpected pass regression."
            : $"Scenario {scenario.ScenarioId} completed deterministic fixture evaluation. Runtime not enabled.";

    private static double Average(IReadOnlyList<ReliableRecipeFixtureEvalIterationResult> results, Func<ReliableRecipeFixtureEvalIterationResult, double> selector) =>
        Math.Round(results.Average(selector), 2);

    private sealed record ReliableRecipeEvalFixtureBundle(
        ReliableRecipeDefinition Recipe,
        ReliableRecipePreflightReport PreflightReport,
        ReliableRecipeQualityReport QualityReport,
        ReliableRecipeLabViewModel LabViewModel,
        RecorderToRecipeDraft? Draft);
}

public static class ReliableRecipeFixtureEvalReportMapper
{
    public static ReliableRecipeLabEvalPanel ToLabPanel(
        ReliableRecipeFixtureEvalScenario scenario,
        ReliableRecipeFixtureEvalMetrics metrics,
        ReliableRecipeFlakinessReport flakiness,
        IReadOnlyList<ReliableRecipeFixtureEvalFailureKind> failureTaxonomy,
        ReliableRecipeFixtureEvalFinalDecision finalDecision) =>
        new(
            scenario.ScenarioId,
            finalDecision.ToString(),
            $"Success rate: {metrics.SuccessRate:0.00}",
            $"Expected outcome match: {metrics.ExpectedOutcomeMatchRate:0.00}",
            $"Flakiness risk: {flakiness.FlakinessLevel} ({flakiness.FlakinessScore:0.00})",
            $"Evidence completeness: {metrics.EvidenceCompletenessScore:0.00}",
            $"Validation completeness: {metrics.ValidationCompletenessScore:0.00}",
            failureTaxonomy.Select(f => f.ToString()).Take(5).ToArray(),
            new ReliableRecipeLabEvalNotice("Fixture-only evaluation. Runtime not enabled."),
            ["Review scenario", "Open report", "Copy summary"]);

    public static ReliableRecipeLabEvalScenarioRow ToScenarioRow(ReliableRecipeFixtureEvalScenario scenario, ReliableRecipeFixtureEvalRun run) =>
        new(
            scenario.ScenarioId,
            scenario.Name,
            scenario.ExpectedOutcome.ToString(),
            run.FinalDecision.ToString(),
            run.FinalDecision == ReliableRecipeFixtureEvalFinalDecision.RegressionDetected ? "danger" : run.FinalDecision == ReliableRecipeFixtureEvalFinalDecision.FixturePass ? "success" : "warning",
            run.Summary);
}

public static class ReliableRecipeEvalScenarioCatalog
{
    public static IReadOnlyList<ReliableRecipeFixtureEvalScenario> All() =>
    [
        Scenario("safe_invoice_download_dry_run_candidate_eval", "Safe invoice download", "Fixture invoice download has complete evidence and validation.", "safe_invoice_download_quality_pass", ReliableRecipeFixtureEvalSourceKind.ManualRecipe, ReliableRecipeFixtureEvalExpectedOutcome.DryRunCandidate, ReliableRecipeRiskProfile.ReadOnly),
        Scenario("invoice_download_missing_validation_eval", "Invoice download missing validation", "Recorder invoice demonstration is missing validation design.", "invoice_download_demonstration", ReliableRecipeFixtureEvalSourceKind.RecorderDraft, ReliableRecipeFixtureEvalExpectedOutcome.ValidationFailureExpected, ReliableRecipeRiskProfile.LocalWrite, [ReliableRecipeFixtureEvalFailureKind.ValidationMissing]),
        Scenario("ocr_only_sensitive_submit_blocked_eval", "OCR-only sensitive submit", "OCR-only sensitive action is blocked as expected.", "ocr_only_canvas_button_demonstration", ReliableRecipeFixtureEvalSourceKind.RecorderDraft, ReliableRecipeFixtureEvalExpectedOutcome.Blocked, ReliableRecipeRiskProfile.ExternalSideEffect | ReliableRecipeRiskProfile.SensitiveData, [ReliableRecipeFixtureEvalFailureKind.OcrOnlySensitiveTarget]),
        Scenario("recorder_password_redaction_eval", "Recorder password redaction", "Password fixture remains redacted and draft-only.", "login_password_redacted_demonstration", ReliableRecipeFixtureEvalSourceKind.RecorderDraft, ReliableRecipeFixtureEvalExpectedOutcome.DraftOnly, ReliableRecipeRiskProfile.Credentialed | ReliableRecipeRiskProfile.SensitiveData, [ReliableRecipeFixtureEvalFailureKind.SecretExposureBlocked]),
        Scenario("captcha_two_factor_handoff_eval", "CAPTCHA two-factor handoff", "Challenge fixture requires human handoff.", "captcha_two_factor_challenge_demonstration", ReliableRecipeFixtureEvalSourceKind.RecorderDraft, ReliableRecipeFixtureEvalExpectedOutcome.HumanHandoffRequired, ReliableRecipeRiskProfile.Credentialed, [ReliableRecipeFixtureEvalFailureKind.HumanHandoffRequired]),
        Scenario("ambiguous_target_needs_review_eval", "Ambiguous target review", "Ambiguous Continue target requires review.", "ambiguous_continue_button_demonstration", ReliableRecipeFixtureEvalSourceKind.RecorderDraft, ReliableRecipeFixtureEvalExpectedOutcome.NeedsReview, ReliableRecipeRiskProfile.ReadOnly, [ReliableRecipeFixtureEvalFailureKind.TargetAmbiguous]),
        Scenario("government_submit_high_risk_blocked_eval", "Government submit high risk", "Government form submit remains blocked by risk and policy.", "government_form_submit_demonstration", ReliableRecipeFixtureEvalSourceKind.RecorderDraft, ReliableRecipeFixtureEvalExpectedOutcome.Blocked, ReliableRecipeRiskProfile.ExternalSideEffect | ReliableRecipeRiskProfile.Irreversible | ReliableRecipeRiskProfile.SensitiveData, [ReliableRecipeFixtureEvalFailureKind.RiskBlocked]),
        Scenario("desktop_future_sandbox_blocked_eval", "Desktop future sandbox blocked", "Desktop future surface is blocked by sandbox readiness.", "desktop_future_demonstration", ReliableRecipeFixtureEvalSourceKind.RecorderDraft, ReliableRecipeFixtureEvalExpectedOutcome.Blocked, ReliableRecipeRiskProfile.ExternalSideEffect, [ReliableRecipeFixtureEvalFailureKind.SandboxNotReady]),
        Scenario("corrected_user_click_review_eval", "Corrected click review", "User correction marker creates target repair review.", "corrected_user_click_demonstration", ReliableRecipeFixtureEvalSourceKind.RecorderDraft, ReliableRecipeFixtureEvalExpectedOutcome.NeedsReview, ReliableRecipeRiskProfile.ReadOnly),
        Scenario("high_quality_high_risk_still_blocked_eval", "High quality high risk", "High target quality cannot override financial risk.", "high_quality_high_risk_blocked", ReliableRecipeFixtureEvalSourceKind.ManualRecipe, ReliableRecipeFixtureEvalExpectedOutcome.Blocked, ReliableRecipeRiskProfile.Financial | ReliableRecipeRiskProfile.ExternalSideEffect, [ReliableRecipeFixtureEvalFailureKind.RiskBlocked]),
        Scenario("unexpected_pass_regression_fixture", "Unexpected pass regression fixture", "A safe fixture is intentionally expected to block to prove regression detection.", "safe_invoice_download_quality_pass", ReliableRecipeFixtureEvalSourceKind.PolicyRegressionFixture, ReliableRecipeFixtureEvalExpectedOutcome.Blocked, ReliableRecipeRiskProfile.ReadOnly, [ReliableRecipeFixtureEvalFailureKind.ExpectedBlockDidNotOccur]),
        Scenario(
            "predefined_flaky_fixture_eval",
            "Predefined flakiness fixture",
            "Predefined variants inject fixture mismatch without randomness.",
            "ambiguous_continue_button_demonstration",
            ReliableRecipeFixtureEvalSourceKind.RecorderDraft,
            ReliableRecipeFixtureEvalExpectedOutcome.NeedsReview,
            ReliableRecipeRiskProfile.ReadOnly,
            [ReliableRecipeFixtureEvalFailureKind.TargetAmbiguous],
            [
                new ReliableRecipeFixtureEvalIterationProfile("stable.1", []),
                new ReliableRecipeFixtureEvalIterationProfile("variant.fixture-mismatch", [ReliableRecipeFixtureEvalFailureKind.FixtureMismatch], -0.1)
            ])
    ];

    public static ReliableRecipeFixtureEvalScenario Get(string scenarioId) =>
        All().Single(s => s.ScenarioId == scenarioId);

    private static ReliableRecipeFixtureEvalScenario Scenario(
        string scenarioId,
        string name,
        string description,
        string fixtureId,
        ReliableRecipeFixtureEvalSourceKind sourceKind,
        ReliableRecipeFixtureEvalExpectedOutcome expectedOutcome,
        ReliableRecipeRiskProfile risk,
        IReadOnlyList<ReliableRecipeFixtureEvalFailureKind>? allowedFailures = null,
        IReadOnlyList<ReliableRecipeFixtureEvalIterationProfile>? profiles = null) =>
        new(
            scenarioId,
            name,
            description,
            fixtureId,
            sourceKind,
            "fixture.initial-state.ref",
            expectedOutcome,
            risk,
            [EvidenceRequirementKind.BeforeState, EvidenceRequirementKind.AfterState, EvidenceRequirementKind.ValidationReport, EvidenceRequirementKind.TimelineEvent],
            [ReliableValidationCheckKind.TimelineEventCreated],
            allowedFailures ?? [],
            profiles?.Count ?? 1,
            ["fixture-only", "no-runtime"],
            profiles ?? []);
}
