namespace OneBrain.Core.Recipes;

public sealed record ReliableRecipePerceptionIntegrationReport(
    string ReportId,
    string SubjectId,
    ReliableRecipePerceptionSubjectKind SubjectKind,
    ReliableRecipePerceptionDecision OverallDecision,
    double OverallConfidence,
    IReadOnlyList<ReliableRecipePerceptionSignalReport> SignalReports,
    ReliableRecipeSignalAgreementReport SignalAgreementReport,
    IReadOnlyList<string> SignalContradictionReport,
    IReadOnlyList<ReliableRecipeTargetConfidenceReport> TargetConfidenceReports,
    IReadOnlyList<ReliableRecipeActionAuthorityReport> ActionAuthorityReports,
    IReadOnlyList<string> HumanReviewReasons,
    IReadOnlyList<ReliableRecipePerceptionSignalKind> MissingSignals,
    string FixtureOnlyNotice)
{
    public bool ReadOnly => true;
    public bool FixtureOnly => true;
    public bool LivePerceptionEnabled => false;
    public bool ScreenshotCaptureEnabled => false;
    public bool BrowserDomLiveCaptureEnabled => false;
    public bool AccessibilityLiveCaptureEnabled => false;
    public bool OcrLiveActivationEnabled => false;
    public bool ProviderOrVlmCallEnabled => false;
    public bool ActionAuthorityGranted => false;
}

public enum ReliableRecipePerceptionSubjectKind
{
    ReliableRecipe,
    RecorderDraft,
    EvalScenario,
    SandboxReadinessScenario,
    PolicyRegressionFixture
}

public enum ReliableRecipePerceptionDecision
{
    FixtureSignalsSufficient,
    FixtureSignalsSufficientWithWarnings,
    NeedsMoreSignals,
    NeedsHumanReview,
    BlockedWeakPerception,
    BlockedSensitiveActionAuthority,
    BlockedContradictorySignals
}

public sealed record ReliableRecipePerceptionSignalReport(
    string SignalId,
    ReliableRecipePerceptionSignalKind SignalKind,
    ReliableRecipePerceptionSignalSource Source,
    double Confidence,
    bool SupportsTarget,
    bool IsSensitive,
    bool IsActionAuthorityEligible,
    string Notes);

public enum ReliableRecipePerceptionSignalKind
{
    DomFixture,
    AccessibilityFixture,
    OcrFixture,
    VisualBoundingBoxFixture,
    SetOfMarksFixture,
    StateClassifierFixture,
    HumanCorrectionFixture,
    UnknownFixture
}

public enum ReliableRecipePerceptionSignalSource
{
    RecipeDefinition,
    RecorderTrajectory,
    EvalScenario,
    SandboxScenario,
    FixtureCatalog
}

public sealed record ReliableRecipeSignalAgreementReport(
    double AgreementScore,
    IReadOnlyList<ReliableRecipeSignalAgreement> Agreements,
    IReadOnlyList<ReliableRecipeSignalAgreement> Disagreements,
    ReliableRecipePerceptionSignalKind DominantSignalKind,
    string Recommendation);

public sealed record ReliableRecipeSignalAgreement(
    string SignalA,
    string SignalB,
    ReliableRecipeSignalAgreementKind AgreementKind,
    double ConfidenceImpact,
    string Notes);

public enum ReliableRecipeSignalAgreementKind
{
    SameTarget,
    SameText,
    SameRole,
    SameBoundingRegion,
    CompatibleState,
    ContradictoryText,
    ContradictoryRole,
    ContradictoryRegion,
    ContradictoryState,
    InsufficientData
}

public sealed record ReliableRecipeTargetConfidenceReport(
    string TargetId,
    string TargetLabel,
    double OverallConfidence,
    IReadOnlyList<ReliableRecipePerceptionSignalKind> SignalsUsed,
    ReliableRecipeTargetAmbiguityLevel AmbiguityLevel,
    string RiskAdjustedDecision,
    string RecommendedAction);

public enum ReliableRecipeTargetAmbiguityLevel
{
    None,
    Low,
    Medium,
    High,
    Critical
}

public sealed record ReliableRecipeActionAuthorityReport(
    string ActionId,
    ReliableRecipeRiskProfile ActionRisk,
    ReliableRecipePerceptionDecision PerceptionDecision,
    bool CanAuthorizeForFixtureDryRun,
    bool CanAuthorizeForSensitiveAction,
    IReadOnlyList<ReliableRecipePerceptionBlockingReason> BlockingReasons,
    bool RequiredHumanReview);

public enum ReliableRecipePerceptionBlockingReason
{
    OcrOnlySensitiveTarget,
    MissingDomSignal,
    MissingAccessibilitySignal,
    ContradictorySignals,
    AmbiguousTarget,
    LowConfidence,
    SensitiveActionRequiresHumanReview,
    VisualOnlyTargetNotEnough,
    SetOfMarksOnlyTargetNotEnough,
    UnknownSignalSource
}

public sealed record ReliableRecipePerceptionScenario(
    string ScenarioId,
    string SubjectId,
    ReliableRecipePerceptionSubjectKind SubjectKind,
    ReliableRecipeRiskProfile Risk,
    ReliableRecipePerceptionDecision ExpectedDecision,
    IReadOnlyList<ReliableRecipePerceptionSignalReport> Signals,
    string TargetLabel,
    bool SensitiveAction,
    bool AmbiguousTarget,
    bool ChallengeDetected,
    string Summary)
{
    public bool FixtureOnly => true;
    public bool UsesLiveBrowser => false;
    public bool UsesLiveDesktop => false;
    public bool UsesScreenshotCapture => false;
    public bool UsesOcrRuntime => false;
    public bool StoresSecrets => false;
}

public sealed record ReliableRecipeEvalPerceptionSummary(
    string ReportId,
    string DecisionLabel,
    double OverallConfidence,
    double SignalAgreementScore,
    IReadOnlyList<string> TopPerceptionFailureKinds,
    string FixtureOnlyNotice)
{
    public bool ReadOnly => true;
    public bool FixtureOnly => true;
    public bool RuntimeActionExposed => false;
}

public sealed record ReliableRecipeSandboxPerceptionSummary(
    string ReportId,
    string DecisionLabel,
    double OverallConfidence,
    IReadOnlyList<string> MissingSignals,
    IReadOnlyList<string> BlockedReasons,
    string FixtureOnlyNotice)
{
    public bool ReadOnly => true;
    public bool FixtureOnly => true;
    public bool LivePerceptionEnabled => false;
}

public sealed record ReliableRecipeLabPerceptionIntegrationPanel(
    string DecisionLabel,
    double OverallConfidence,
    string SignalAgreementLabel,
    string DominantSignalLabel,
    IReadOnlyList<ReliableRecipeLabPanelRow> Signals,
    IReadOnlyList<string> Contradictions,
    IReadOnlyList<string> MissingSignals,
    string ActionAuthorityLabel,
    IReadOnlyList<string> HumanReviewReasons,
    string FixtureOnlyNotice,
    IReadOnlyList<string> ReadOnlyActionLabels)
{
    public bool ReadOnly => true;
    public bool FixtureOnly => true;
    public bool CanRunPerception => false;
    public bool CanCaptureScreen => false;
    public bool CanAnalyzeBrowserNow => false;
    public bool CanAuthorizeClick => false;
    public bool LivePerceptionEnabled => false;
}

public static class ReliableRecipePerceptionScenarioCatalog
{
    public static IReadOnlyList<ReliableRecipePerceptionScenario> All() =>
    [
        Scenario("dom_accessibility_ocr_agreement_fixture", "safe_invoice_download_quality_pass", ReliableRecipePerceptionSubjectKind.ReliableRecipe, ReliableRecipeRiskProfile.ReadOnly, ReliableRecipePerceptionDecision.FixtureSignalsSufficient, "Submit invoice", false, false, false,
        [
            Signal("dom.submit", ReliableRecipePerceptionSignalKind.DomFixture, ReliableRecipePerceptionSignalSource.RecipeDefinition, 0.96, "DOM fixture target matches Submit invoice."),
            Signal("a11y.submit", ReliableRecipePerceptionSignalKind.AccessibilityFixture, ReliableRecipePerceptionSignalSource.FixtureCatalog, 0.94, "Accessibility fixture target matches Submit invoice."),
            Signal("ocr.submit", ReliableRecipePerceptionSignalKind.OcrFixture, ReliableRecipePerceptionSignalSource.FixtureCatalog, 0.9, "OCR supporting signal matches Submit invoice.", actionEligible: false)
        ], "DOM, accessibility and OCR fixture signals agree for low-risk review."),
        Scenario("ocr_only_sensitive_target_blocked", "ocr_only_sensitive_submit_blocked", ReliableRecipePerceptionSubjectKind.RecorderDraft, ReliableRecipeRiskProfile.ExternalSideEffect | ReliableRecipeRiskProfile.SensitiveData, ReliableRecipePerceptionDecision.BlockedSensitiveActionAuthority, "Submit official form", true, false, false,
        [
            Signal("ocr.submit", ReliableRecipePerceptionSignalKind.OcrFixture, ReliableRecipePerceptionSignalSource.RecorderTrajectory, 0.88, "OCR supporting signal only for sensitive submit.", sensitive: true, actionEligible: false)
        ], "OCR-only sensitive action authority is blocked."),
        Scenario("visual_only_target_blocked", "visual_only_target_blocked", ReliableRecipePerceptionSubjectKind.ReliableRecipe, ReliableRecipeRiskProfile.ExternalSideEffect, ReliableRecipePerceptionDecision.BlockedSensitiveActionAuthority, "Continue", true, false, false,
        [
            Signal("visual.continue", ReliableRecipePerceptionSignalKind.VisualBoundingBoxFixture, ReliableRecipePerceptionSignalSource.FixtureCatalog, 0.72, "Visual fixture target lacks DOM/accessibility confirmation.", actionEligible: false)
        ], "Visual-only target is not enough for sensitive action authority."),
        Scenario("contradictory_dom_ocr_text", "contradictory_dom_ocr_text", ReliableRecipePerceptionSubjectKind.EvalScenario, ReliableRecipeRiskProfile.ReadOnly, ReliableRecipePerceptionDecision.BlockedContradictorySignals, "Download invoice", false, false, false,
        [
            Signal("dom.download", ReliableRecipePerceptionSignalKind.DomFixture, ReliableRecipePerceptionSignalSource.EvalScenario, 0.9, "DOM fixture says Download invoice."),
            Signal("ocr.cancel", ReliableRecipePerceptionSignalKind.OcrFixture, ReliableRecipePerceptionSignalSource.EvalScenario, 0.86, "OCR supporting signal says Cancel request.", actionEligible: false)
        ], "Contradictory DOM/OCR fixture text requires review."),
        Scenario("ambiguous_continue_targets", "ambiguous_target_needs_review", ReliableRecipePerceptionSubjectKind.RecorderDraft, ReliableRecipeRiskProfile.ReadOnly, ReliableRecipePerceptionDecision.NeedsHumanReview, "Continue", false, true, false,
        [
            Signal("dom.continue.primary", ReliableRecipePerceptionSignalKind.DomFixture, ReliableRecipePerceptionSignalSource.RecorderTrajectory, 0.62, "First Continue target."),
            Signal("dom.continue.secondary", ReliableRecipePerceptionSignalKind.DomFixture, ReliableRecipePerceptionSignalSource.RecorderTrajectory, 0.61, "Second Continue target.")
        ], "Multiple fixture targets need human confirmation."),
        Scenario("human_corrected_target_needs_review", "corrected_user_click_review_eval", ReliableRecipePerceptionSubjectKind.RecorderDraft, ReliableRecipeRiskProfile.ReadOnly, ReliableRecipePerceptionDecision.NeedsHumanReview, "Corrected target", false, false, false,
        [
            Signal("human.correction", ReliableRecipePerceptionSignalKind.HumanCorrectionFixture, ReliableRecipePerceptionSignalSource.RecorderTrajectory, 0.8, "Human correction marker requires target review.")
        ], "Human correction is captured as review signal, not automatic repair."),
        Scenario("high_confidence_high_risk_still_blocked", "high_quality_high_risk_blocked", ReliableRecipePerceptionSubjectKind.ReliableRecipe, ReliableRecipeRiskProfile.Financial | ReliableRecipeRiskProfile.ExternalSideEffect, ReliableRecipePerceptionDecision.BlockedSensitiveActionAuthority, "Pay invoice", true, false, false,
        [
            Signal("dom.pay", ReliableRecipePerceptionSignalKind.DomFixture, ReliableRecipePerceptionSignalSource.RecipeDefinition, 0.97, "DOM fixture target matches Pay invoice."),
            Signal("a11y.pay", ReliableRecipePerceptionSignalKind.AccessibilityFixture, ReliableRecipePerceptionSignalSource.FixtureCatalog, 0.95, "Accessibility fixture target matches Pay invoice."),
            Signal("ocr.pay", ReliableRecipePerceptionSignalKind.OcrFixture, ReliableRecipePerceptionSignalSource.FixtureCatalog, 0.92, "OCR supporting signal matches Pay invoice.", sensitive: true, actionEligible: false)
        ], "High confidence cannot authorize high-risk action."),
        Scenario("missing_accessibility_signal_warning", "missing_accessibility_signal_warning", ReliableRecipePerceptionSubjectKind.ReliableRecipe, ReliableRecipeRiskProfile.ReadOnly, ReliableRecipePerceptionDecision.FixtureSignalsSufficientWithWarnings, "Invoice total", false, false, false,
        [
            Signal("dom.total", ReliableRecipePerceptionSignalKind.DomFixture, ReliableRecipePerceptionSignalSource.RecipeDefinition, 0.9, "DOM fixture target matches Invoice total."),
            Signal("ocr.total", ReliableRecipePerceptionSignalKind.OcrFixture, ReliableRecipePerceptionSignalSource.FixtureCatalog, 0.82, "OCR supporting signal matches Invoice total.", actionEligible: false)
        ], "Low-risk fixture can proceed with missing accessibility warning."),
        Scenario("set_of_marks_only_not_enough", "set_of_marks_only_not_enough", ReliableRecipePerceptionSubjectKind.EvalScenario, ReliableRecipeRiskProfile.ReadOnly, ReliableRecipePerceptionDecision.NeedsMoreSignals, "Mark 4", false, false, false,
        [
            Signal("som.4", ReliableRecipePerceptionSignalKind.SetOfMarksFixture, ReliableRecipePerceptionSignalSource.EvalScenario, 0.7, "Set-of-marks fixture index without semantic confirmation.", actionEligible: false)
        ], "Set-of-marks-only target needs more signals."),
        Scenario("credential_field_detected", "recorder_password_redaction_eval", ReliableRecipePerceptionSubjectKind.RecorderDraft, ReliableRecipeRiskProfile.Credentialed | ReliableRecipeRiskProfile.SensitiveData, ReliableRecipePerceptionDecision.BlockedSensitiveActionAuthority, "Password field", true, false, false,
        [
            Signal("dom.password", ReliableRecipePerceptionSignalKind.DomFixture, ReliableRecipePerceptionSignalSource.RecorderTrajectory, 0.91, "Credential field fixture detected.", sensitive: true, actionEligible: false)
        ], "Credential field requires human review and redaction."),
        Scenario("captcha_challenge_detected", "captcha_two_factor_handoff_eval", ReliableRecipePerceptionSubjectKind.RecorderDraft, ReliableRecipeRiskProfile.Credentialed, ReliableRecipePerceptionDecision.NeedsHumanReview, "Challenge step", true, false, true,
        [
            Signal("state.challenge", ReliableRecipePerceptionSignalKind.StateClassifierFixture, ReliableRecipePerceptionSignalSource.RecorderTrajectory, 0.93, "Challenge fixture detected.", sensitive: true, actionEligible: false)
        ], "Challenge perception routes to human handoff.")
    ];

    public static ReliableRecipePerceptionScenario Get(string scenarioId) =>
        All().Single(s => s.ScenarioId == scenarioId);

    private static ReliableRecipePerceptionScenario Scenario(
        string scenarioId,
        string subjectId,
        ReliableRecipePerceptionSubjectKind subjectKind,
        ReliableRecipeRiskProfile risk,
        ReliableRecipePerceptionDecision expectedDecision,
        string targetLabel,
        bool sensitiveAction,
        bool ambiguousTarget,
        bool challengeDetected,
        IReadOnlyList<ReliableRecipePerceptionSignalReport> signals,
        string summary) =>
        new(scenarioId, subjectId, subjectKind, risk, expectedDecision, signals, targetLabel, sensitiveAction, ambiguousTarget, challengeDetected, summary);

    private static ReliableRecipePerceptionSignalReport Signal(
        string id,
        ReliableRecipePerceptionSignalKind kind,
        ReliableRecipePerceptionSignalSource source,
        double confidence,
        string notes,
        bool supportsTarget = true,
        bool sensitive = false,
        bool actionEligible = true) =>
        new(id, kind, source, confidence, supportsTarget, sensitive, actionEligible && kind is ReliableRecipePerceptionSignalKind.DomFixture or ReliableRecipePerceptionSignalKind.AccessibilityFixture, notes);
}

public static class ReliableRecipePerceptionIntegrationEvaluator
{
    public static ReliableRecipePerceptionIntegrationReport Evaluate(ReliableRecipePerceptionScenario scenario) =>
        EvaluateCore(
            $"perception.{scenario.ScenarioId}",
            scenario.SubjectId,
            scenario.SubjectKind,
            scenario.Risk,
            scenario.TargetLabel,
            scenario.SensitiveAction,
            scenario.AmbiguousTarget,
            scenario.ChallengeDetected,
            scenario.Signals);

    public static ReliableRecipePerceptionIntegrationReport Evaluate(ReliableRecipeDefinition recipe, ReliableRecipePreflightReport report) =>
        EvaluateCore(
            $"perception.{recipe.Id}",
            recipe.Id,
            ReliableRecipePerceptionSubjectKind.ReliableRecipe,
            recipe.RiskProfile,
            recipe.Blocks.FirstOrDefault()?.Label ?? recipe.Name,
            IsSensitive(recipe.RiskProfile),
            !string.IsNullOrWhiteSpace(report.QualityReport.TargetResolutionQuality.Ambiguity),
            report.RequiredHumanInterventions.Any(r => r is ReliableHumanInterventionReason.CaptchaDetected or ReliableHumanInterventionReason.TwoFactorRequired),
            SignalsFromQuality(report.QualityReport));

    public static ReliableRecipePerceptionIntegrationReport EvaluateEvalPreview(ReliableRecipeFixtureEvalScenario scenario)
    {
        var mapped = TryMapEvalScenario(scenario.ScenarioId) ?? TryMapSubject(scenario.FixtureId);
        return mapped is not null
            ? Evaluate(mapped)
            : EvaluateCore($"perception.{scenario.ScenarioId}", scenario.ScenarioId, ReliableRecipePerceptionSubjectKind.EvalScenario, scenario.RiskConstraints, scenario.Name, IsSensitive(scenario.RiskConstraints), false, false, []);
    }

    public static ReliableRecipePerceptionIntegrationReport EvaluateSandboxPreview(ComputerUseSandboxReadinessScenario scenario)
    {
        var mapped = TryMapSandboxScenario(scenario.ScenarioId) ?? TryMapSubject(scenario.SubjectId);
        return mapped is not null
            ? Evaluate(mapped)
            : EvaluateCore($"perception.{scenario.ScenarioId}", scenario.SubjectId, ReliableRecipePerceptionSubjectKind.SandboxReadinessScenario, ReliableRecipeRiskProfile.ReadOnly, scenario.Summary, false, false, false, []);
    }

    private static ReliableRecipePerceptionIntegrationReport EvaluateCore(
        string reportId,
        string subjectId,
        ReliableRecipePerceptionSubjectKind subjectKind,
        ReliableRecipeRiskProfile risk,
        string targetLabel,
        bool sensitiveAction,
        bool ambiguousTarget,
        bool challengeDetected,
        IReadOnlyList<ReliableRecipePerceptionSignalReport> signals)
    {
        var agreement = Agreement(signals);
        var missing = MissingSignals(signals, risk, sensitiveAction).ToArray();
        var blocking = BlockingReasons(signals, risk, sensitiveAction, ambiguousTarget, challengeDetected, agreement, missing).Distinct().ToArray();
        var decision = DecisionFor(blocking, missing, ambiguousTarget, challengeDetected, risk, agreement, signals);
        var confidence = ConfidenceFor(signals, agreement, decision, risk);
        var humanReasons = HumanReviewReasons(blocking, ambiguousTarget, challengeDetected, signals.Any(s => s.SignalKind == ReliableRecipePerceptionSignalKind.HumanCorrectionFixture), risk).Distinct().ToArray();
        var target = new ReliableRecipeTargetConfidenceReport(
            $"target.{subjectId}",
            targetLabel,
            confidence,
            signals.Select(s => s.SignalKind).Distinct().ToArray(),
            ambiguousTarget ? ReliableRecipeTargetAmbiguityLevel.High : blocking.Contains(ReliableRecipePerceptionBlockingReason.ContradictorySignals) ? ReliableRecipeTargetAmbiguityLevel.Medium : ReliableRecipeTargetAmbiguityLevel.None,
            decision.ToString(),
            decision is ReliableRecipePerceptionDecision.FixtureSignalsSufficient or ReliableRecipePerceptionDecision.FixtureSignalsSufficientWithWarnings
                ? "Continue read-only fixture review."
                : "Request human review or add stronger fixture signals.");
        var authority = new ReliableRecipeActionAuthorityReport(
            $"action.{subjectId}",
            risk,
            decision,
            decision is ReliableRecipePerceptionDecision.FixtureSignalsSufficient or ReliableRecipePerceptionDecision.FixtureSignalsSufficientWithWarnings && !sensitiveAction,
            false,
            blocking,
            humanReasons.Length > 0);

        return new ReliableRecipePerceptionIntegrationReport(
            reportId,
            subjectId,
            subjectKind,
            decision,
            confidence,
            signals,
            agreement,
            agreement.Disagreements.Select(d => d.Notes).ToArray(),
            [target],
            [authority],
            humanReasons,
            missing,
            "Fixture perception report. Runtime not enabled.");
    }

    private static IReadOnlyList<ReliableRecipePerceptionSignalReport> SignalsFromQuality(ReliableRecipeQualityReport quality)
    {
        var mode = quality.TargetResolutionQuality.ResolutionMode;
        if (mode == ReliableActionResolutionMode.OcrRegion)
            return [new ReliableRecipePerceptionSignalReport("quality.ocr", ReliableRecipePerceptionSignalKind.OcrFixture, ReliableRecipePerceptionSignalSource.RecipeDefinition, quality.TargetResolutionQuality.Score, true, quality.TargetResolutionQuality.SensitiveActionRisk, false, "OCR supporting signal from quality report.")];
        if (mode is ReliableActionResolutionMode.StableSelector or ReliableActionResolutionMode.KnownTarget or ReliableActionResolutionMode.DomOrCdpSnapshot)
            return
            [
                new ReliableRecipePerceptionSignalReport("quality.dom", ReliableRecipePerceptionSignalKind.DomFixture, ReliableRecipePerceptionSignalSource.RecipeDefinition, quality.TargetResolutionQuality.Score, true, false, true, "DOM/stable selector fixture signal from quality report."),
                new ReliableRecipePerceptionSignalReport("quality.accessibility", ReliableRecipePerceptionSignalKind.AccessibilityFixture, ReliableRecipePerceptionSignalSource.RecipeDefinition, Math.Min(quality.TargetResolutionQuality.Score, 0.9), true, false, true, "Accessibility fixture support from quality report."),
                new ReliableRecipePerceptionSignalReport("quality.ocr", ReliableRecipePerceptionSignalKind.OcrFixture, ReliableRecipePerceptionSignalSource.RecipeDefinition, Math.Min(quality.TargetResolutionQuality.Score, 0.86), true, false, false, "OCR supporting signal from quality report.")
            ];
        return [];
    }

    private static ReliableRecipeSignalAgreementReport Agreement(IReadOnlyList<ReliableRecipePerceptionSignalReport> signals)
    {
        if (signals.Count < 2)
            return new ReliableRecipeSignalAgreementReport(signals.Count == 0 ? 0 : Math.Round(signals.Average(s => s.Confidence) * 0.6, 2), [], [], signals.FirstOrDefault()?.SignalKind ?? ReliableRecipePerceptionSignalKind.UnknownFixture, "Add DOM/accessibility fixture signals before future action authority review.");

        var agreements = new List<ReliableRecipeSignalAgreement>();
        var disagreements = new List<ReliableRecipeSignalAgreement>();
        for (var i = 0; i < signals.Count; i++)
        {
            for (var j = i + 1; j < signals.Count; j++)
            {
                var a = signals[i];
                var b = signals[j];
                var contradictory = LooksContradictory(a.Notes, b.Notes);
                var agreement = new ReliableRecipeSignalAgreement(
                    a.SignalId,
                    b.SignalId,
                    contradictory ? ReliableRecipeSignalAgreementKind.ContradictoryText : ReliableRecipeSignalAgreementKind.SameTarget,
                    contradictory ? -0.35 : 0.18,
                    contradictory ? $"Fixture signals disagree: {a.SignalId} vs {b.SignalId}." : $"Fixture signals agree: {a.SignalId} and {b.SignalId}.");
                if (contradictory) disagreements.Add(agreement); else agreements.Add(agreement);
            }
        }

        var score = disagreements.Count > 0
            ? 0.35
            : Math.Round(Math.Clamp(signals.Average(s => s.Confidence) + agreements.Count * 0.02, 0, 1), 2);
        var dominant = signals.Any(s => s.SignalKind == ReliableRecipePerceptionSignalKind.DomFixture) ? ReliableRecipePerceptionSignalKind.DomFixture :
            signals.Any(s => s.SignalKind == ReliableRecipePerceptionSignalKind.AccessibilityFixture) ? ReliableRecipePerceptionSignalKind.AccessibilityFixture :
            signals.First().SignalKind;
        return new ReliableRecipeSignalAgreementReport(score, agreements, disagreements, dominant, disagreements.Count == 0 ? "Fixture signals agree for read-only review." : "Resolve fixture contradictions before any future runtime consideration.");
    }

    private static bool LooksContradictory(string a, string b) =>
        (a.Contains("Cancel", StringComparison.OrdinalIgnoreCase) && !b.Contains("Cancel", StringComparison.OrdinalIgnoreCase)) ||
        (b.Contains("Cancel", StringComparison.OrdinalIgnoreCase) && !a.Contains("Cancel", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<ReliableRecipePerceptionSignalKind> MissingSignals(IReadOnlyList<ReliableRecipePerceptionSignalReport> signals, ReliableRecipeRiskProfile risk, bool sensitiveAction)
    {
        if (!signals.Any(s => s.SignalKind == ReliableRecipePerceptionSignalKind.DomFixture))
            yield return ReliableRecipePerceptionSignalKind.DomFixture;
        if (!signals.Any(s => s.SignalKind == ReliableRecipePerceptionSignalKind.AccessibilityFixture))
            yield return ReliableRecipePerceptionSignalKind.AccessibilityFixture;
        if ((sensitiveAction || IsSensitive(risk)) && !signals.Any(s => s.SignalKind == ReliableRecipePerceptionSignalKind.OcrFixture))
            yield return ReliableRecipePerceptionSignalKind.OcrFixture;
    }

    private static IEnumerable<ReliableRecipePerceptionBlockingReason> BlockingReasons(
        IReadOnlyList<ReliableRecipePerceptionSignalReport> signals,
        ReliableRecipeRiskProfile risk,
        bool sensitiveAction,
        bool ambiguousTarget,
        bool challengeDetected,
        ReliableRecipeSignalAgreementReport agreement,
        IReadOnlyList<ReliableRecipePerceptionSignalKind> missing)
    {
        var signalKinds = signals.Select(s => s.SignalKind).Distinct().ToArray();
        if (agreement.Disagreements.Count > 0)
            yield return ReliableRecipePerceptionBlockingReason.ContradictorySignals;
        if (ambiguousTarget)
            yield return ReliableRecipePerceptionBlockingReason.AmbiguousTarget;
        if (challengeDetected || IsSensitive(risk) || sensitiveAction)
            yield return ReliableRecipePerceptionBlockingReason.SensitiveActionRequiresHumanReview;
        if (signalKinds.SequenceEqual([ReliableRecipePerceptionSignalKind.OcrFixture]) && (sensitiveAction || IsSensitive(risk)))
            yield return ReliableRecipePerceptionBlockingReason.OcrOnlySensitiveTarget;
        if (signalKinds.SequenceEqual([ReliableRecipePerceptionSignalKind.VisualBoundingBoxFixture]) && (sensitiveAction || IsSensitive(risk)))
            yield return ReliableRecipePerceptionBlockingReason.VisualOnlyTargetNotEnough;
        if (signalKinds.SequenceEqual([ReliableRecipePerceptionSignalKind.SetOfMarksFixture]))
            yield return ReliableRecipePerceptionBlockingReason.SetOfMarksOnlyTargetNotEnough;
        if (signals.Count == 0)
            yield return ReliableRecipePerceptionBlockingReason.UnknownSignalSource;
        if (missing.Contains(ReliableRecipePerceptionSignalKind.DomFixture) && IsSensitive(risk))
            yield return ReliableRecipePerceptionBlockingReason.MissingDomSignal;
        if (missing.Contains(ReliableRecipePerceptionSignalKind.AccessibilityFixture) && IsSensitive(risk))
            yield return ReliableRecipePerceptionBlockingReason.MissingAccessibilitySignal;
        if (signals.Count > 0 && signals.Average(s => s.Confidence) < 0.55)
            yield return ReliableRecipePerceptionBlockingReason.LowConfidence;
    }

    private static ReliableRecipePerceptionDecision DecisionFor(
        IReadOnlyList<ReliableRecipePerceptionBlockingReason> blocking,
        IReadOnlyList<ReliableRecipePerceptionSignalKind> missing,
        bool ambiguousTarget,
        bool challengeDetected,
        ReliableRecipeRiskProfile risk,
        ReliableRecipeSignalAgreementReport agreement,
        IReadOnlyList<ReliableRecipePerceptionSignalReport> signals)
    {
        if (blocking.Contains(ReliableRecipePerceptionBlockingReason.ContradictorySignals))
            return ReliableRecipePerceptionDecision.BlockedContradictorySignals;
        if (ambiguousTarget || challengeDetected || signals.Any(s => s.SignalKind == ReliableRecipePerceptionSignalKind.HumanCorrectionFixture))
            return ReliableRecipePerceptionDecision.NeedsHumanReview;
        if (blocking.Any(b => b is ReliableRecipePerceptionBlockingReason.OcrOnlySensitiveTarget or ReliableRecipePerceptionBlockingReason.VisualOnlyTargetNotEnough or ReliableRecipePerceptionBlockingReason.SensitiveActionRequiresHumanReview) && IsSensitive(risk))
            return ReliableRecipePerceptionDecision.BlockedSensitiveActionAuthority;
        if (blocking.Any(b => b is ReliableRecipePerceptionBlockingReason.SetOfMarksOnlyTargetNotEnough or ReliableRecipePerceptionBlockingReason.UnknownSignalSource or ReliableRecipePerceptionBlockingReason.LowConfidence))
            return ReliableRecipePerceptionDecision.NeedsMoreSignals;
        if (missing.Any(m => m is ReliableRecipePerceptionSignalKind.AccessibilityFixture or ReliableRecipePerceptionSignalKind.DomFixture))
            return IsSensitive(risk) ? ReliableRecipePerceptionDecision.BlockedWeakPerception : ReliableRecipePerceptionDecision.FixtureSignalsSufficientWithWarnings;
        return agreement.AgreementScore >= 0.8 ? ReliableRecipePerceptionDecision.FixtureSignalsSufficient : ReliableRecipePerceptionDecision.FixtureSignalsSufficientWithWarnings;
    }

    private static double ConfidenceFor(
        IReadOnlyList<ReliableRecipePerceptionSignalReport> signals,
        ReliableRecipeSignalAgreementReport agreement,
        ReliableRecipePerceptionDecision decision,
        ReliableRecipeRiskProfile risk)
    {
        var baseScore = signals.Count == 0 ? 0 : Math.Round(signals.Average(s => s.Confidence) * agreement.AgreementScore, 2);
        if (decision is ReliableRecipePerceptionDecision.BlockedContradictorySignals or ReliableRecipePerceptionDecision.BlockedSensitiveActionAuthority)
            baseScore = Math.Min(baseScore, 0.45);
        if (decision == ReliableRecipePerceptionDecision.NeedsHumanReview)
            baseScore = Math.Min(baseScore, 0.58);
        if (IsSensitive(risk))
            baseScore = Math.Min(baseScore, 0.65);
        return Math.Round(Math.Clamp(baseScore, 0, 1), 2);
    }

    private static IEnumerable<string> HumanReviewReasons(
        IReadOnlyList<ReliableRecipePerceptionBlockingReason> blocking,
        bool ambiguousTarget,
        bool challengeDetected,
        bool humanCorrection,
        ReliableRecipeRiskProfile risk)
    {
        if (ambiguousTarget || blocking.Contains(ReliableRecipePerceptionBlockingReason.AmbiguousTarget))
            yield return "Ambiguous target requires human review.";
        if (challengeDetected)
            yield return "Challenge perception requires human handoff.";
        if (humanCorrection)
            yield return "Human correction marker requires review before target stabilization.";
        if (IsSensitive(risk) || blocking.Contains(ReliableRecipePerceptionBlockingReason.SensitiveActionRequiresHumanReview))
            yield return "Sensitive action authority remains blocked; approval and human review are required.";
        if (blocking.Contains(ReliableRecipePerceptionBlockingReason.ContradictorySignals))
            yield return "Signal contradiction requires review.";
    }

    private static ReliableRecipePerceptionScenario? TryMapEvalScenario(string scenarioId) =>
        scenarioId switch
        {
            "safe_invoice_download_dry_run_candidate_eval" => ReliableRecipePerceptionScenarioCatalog.Get("dom_accessibility_ocr_agreement_fixture"),
            "ocr_only_sensitive_submit_blocked_eval" => ReliableRecipePerceptionScenarioCatalog.Get("ocr_only_sensitive_target_blocked"),
            "captcha_two_factor_handoff_eval" => ReliableRecipePerceptionScenarioCatalog.Get("captcha_challenge_detected"),
            "ambiguous_target_needs_review_eval" => ReliableRecipePerceptionScenarioCatalog.Get("ambiguous_continue_targets"),
            "corrected_user_click_review_eval" => ReliableRecipePerceptionScenarioCatalog.Get("human_corrected_target_needs_review"),
            "high_quality_high_risk_still_blocked_eval" => ReliableRecipePerceptionScenarioCatalog.Get("high_confidence_high_risk_still_blocked"),
            _ => null
        };

    private static ReliableRecipePerceptionScenario? TryMapSandboxScenario(string scenarioId) =>
        scenarioId switch
        {
            "safe_invoice_fixture_ready" => ReliableRecipePerceptionScenarioCatalog.Get("dom_accessibility_ocr_agreement_fixture"),
            "ocr_only_sensitive_blocked" => ReliableRecipePerceptionScenarioCatalog.Get("ocr_only_sensitive_target_blocked"),
            "captcha_two_factor_handoff_blocked" => ReliableRecipePerceptionScenarioCatalog.Get("captcha_challenge_detected"),
            "ambiguous_target_needs_review" => ReliableRecipePerceptionScenarioCatalog.Get("ambiguous_continue_targets"),
            "high_quality_high_risk_still_blocked" => ReliableRecipePerceptionScenarioCatalog.Get("high_confidence_high_risk_still_blocked"),
            _ => null
        };

    private static ReliableRecipePerceptionScenario? TryMapSubject(string subjectId) =>
        subjectId switch
        {
            "safe_invoice_download_quality_pass" => ReliableRecipePerceptionScenarioCatalog.Get("dom_accessibility_ocr_agreement_fixture"),
            "ocr_only_canvas_button_demonstration" or "ocr_only_sensitive_submit_blocked" => ReliableRecipePerceptionScenarioCatalog.Get("ocr_only_sensitive_target_blocked"),
            "login_password_redacted_demonstration" or "recorder_password_redaction_eval" => ReliableRecipePerceptionScenarioCatalog.Get("credential_field_detected"),
            "captcha_two_factor_challenge_demonstration" or "captcha_two_factor_handoff_eval" => ReliableRecipePerceptionScenarioCatalog.Get("captcha_challenge_detected"),
            "ambiguous_continue_button_demonstration" or "ambiguous_target_needs_review_eval" => ReliableRecipePerceptionScenarioCatalog.Get("ambiguous_continue_targets"),
            "corrected_user_click_demonstration" => ReliableRecipePerceptionScenarioCatalog.Get("human_corrected_target_needs_review"),
            "high_quality_high_risk_blocked" => ReliableRecipePerceptionScenarioCatalog.Get("high_confidence_high_risk_still_blocked"),
            _ => null
        };

    private static bool IsSensitive(ReliableRecipeRiskProfile risk) =>
        risk.HasFlag(ReliableRecipeRiskProfile.Credentialed) ||
        risk.HasFlag(ReliableRecipeRiskProfile.Financial) ||
        risk.HasFlag(ReliableRecipeRiskProfile.Destructive) ||
        risk.HasFlag(ReliableRecipeRiskProfile.Irreversible) ||
        risk.HasFlag(ReliableRecipeRiskProfile.ExternalSideEffect) ||
        risk.HasFlag(ReliableRecipeRiskProfile.SensitiveData);
}

public static class ReliableRecipePerceptionIntegrationReportMapper
{
    public static ReliableRecipeEvalPerceptionSummary ToEvalSummary(ReliableRecipePerceptionIntegrationReport report) =>
        new(
            report.ReportId,
            report.OverallDecision.ToString(),
            report.OverallConfidence,
            report.SignalAgreementReport.AgreementScore,
            report.ActionAuthorityReports.SelectMany(a => a.BlockingReasons).Select(r => r.ToString()).Distinct().Take(5).ToArray(),
            report.FixtureOnlyNotice);

    public static ReliableRecipeSandboxPerceptionSummary ToSandboxSummary(ReliableRecipePerceptionIntegrationReport report) =>
        new(
            report.ReportId,
            report.OverallDecision.ToString(),
            report.OverallConfidence,
            report.MissingSignals.Select(s => s.ToString()).ToArray(),
            report.ActionAuthorityReports.SelectMany(a => a.BlockingReasons).Select(r => r.ToString()).Distinct().ToArray(),
            report.FixtureOnlyNotice);

    public static ReliableRecipeLabPerceptionIntegrationPanel ToLabPanel(ReliableRecipePerceptionIntegrationReport report) =>
        new(
            report.OverallDecision.ToString(),
            report.OverallConfidence,
            $"Signal agreement: {report.SignalAgreementReport.AgreementScore:0.00}",
            report.SignalAgreementReport.DominantSignalKind.ToString(),
            report.SignalReports.Select(s => new ReliableRecipeLabPanelRow(s.SignalId, $"{s.SignalKind}: {s.Notes}", s.IsSensitive ? "warning" : "info")).ToArray(),
            report.SignalContradictionReport,
            report.MissingSignals.Select(s => s.ToString()).ToArray(),
            report.ActionAuthorityReports.Any(a => a.CanAuthorizeForSensitiveAction) ? "Fixture review only; sensitive authority still requires policy." : "Action authority blocked for live or sensitive actions.",
            report.HumanReviewReasons,
            report.FixtureOnlyNotice,
            ["Review signals", "Open report", "Copy summary"]);
}
