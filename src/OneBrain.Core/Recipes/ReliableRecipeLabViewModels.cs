namespace OneBrain.Core.Recipes;

public sealed record ReliableRecipeLabViewModel(
    string RecipeId,
    string RecipeName,
    string Version,
    string ReadinessLabel,
    string ModeRequestedLabel,
    string ModeAllowedLabel,
    double OverallScore,
    string DecisionLabel,
    string StatusTone,
    string Summary,
    IReadOnlyList<ReliableRecipeLabCategoryCard> CategoryCards,
    IReadOnlyList<ReliableRecipeLabFindingViewModel> BlockingFindings,
    IReadOnlyList<ReliableRecipeLabFindingViewModel> Warnings,
    IReadOnlyList<string> RecommendedFixes,
    ReliableRecipeLabPanelViewModel EvidencePanel,
    ReliableRecipeLabPanelViewModel ValidationPanel,
    ReliableRecipeLabPanelViewModel TargetResolutionPanel,
    ReliableRecipeLabPanelViewModel RiskPanel,
    ReliableRecipeLabPanelViewModel SandboxPanel,
    ReliableRecipeLabPanelViewModel HumanInterventionPanel,
    ReliableRecipeLabPanelViewModel PerceptionPanel,
    ReliableRecipeLabPerceptionIntegrationPanel PerceptionIntegrationPanel,
    ReliableRecipeLabPanelViewModel RecorderDraftPanel,
    ReliableRecipeLabRecorderDraftReviewPanel RecorderDraftReview,
    ReliableRecipeLabEvalPanel EvalPanel,
    ReliableRecipeLabSandboxReadinessPanel SandboxReadinessReportPanel,
    IReadOnlyList<ReliableRecipeLabTimelinePreviewItem> TimelinePreview,
    ReliableRecipeLabNoLiveRuntimeNotice NoLiveRuntimeNotice,
    IReadOnlyList<string> ReadOnlyActionLabels)
{
    public bool ReadOnly => true;
    public bool FixtureSafePreview => true;
    public bool CanStartRecipeRun => false;
    public bool CanExecuteRecipe => false;
    public bool CanEnableLiveRuntime => false;
    public bool CanEnableBrowserRuntime => false;
    public bool CanEnableDesktopRuntime => false;
    public bool CanCallProviderOrNetwork => false;
    public bool CanReadSecrets => false;
    public bool CanActivateOcrRuntime => false;
    public bool CanCaptureScreenshot => false;
}

public sealed record ReliableRecipeLabCategoryCard(
    ReliableRecipeQualityCategory Category,
    double Score,
    string Label,
    string Tone,
    string Summary,
    IReadOnlyList<string> TopFindings);

public sealed record ReliableRecipeLabFindingViewModel(
    string Code,
    ReliableRecipeQualitySeverity Severity,
    string Title,
    string Message,
    string? AffectedBlockId,
    string RecommendedFix,
    bool IsBlocking);

public sealed record ReliableRecipeLabPanelViewModel(
    string Title,
    double Score,
    string Tone,
    IReadOnlyList<ReliableRecipeLabPanelRow> Rows,
    string EmptyState,
    string FooterNote);

public sealed record ReliableRecipeLabPanelRow(
    string Label,
    string Value,
    string Tone = "info");

public sealed record ReliableRecipeLabTimelinePreviewItem(
    string StepId,
    string Label,
    string Status,
    string EvidenceExpectationSummary,
    string ValidationExpectationSummary,
    string RiskLabel);

public sealed record ReliableRecipeLabNoLiveRuntimeNotice(
    string Title,
    string Message,
    IReadOnlyList<string> BlockedCapabilities,
    string AllowedMode);

public sealed record ReliableRecipeLabRecorderDraftReviewPanel(
    string SourceTrajectoryLabel,
    string DraftReviewStateLabel,
    IReadOnlyList<ReliableRecipeLabRecorderChecklistItem> ReviewChecklist,
    IReadOnlyList<ReliableRecipeLabDetectedVariableRow> DetectedVariables,
    string SensitiveInputSummary,
    string DraftConversionNotice)
{
    public bool ReadOnly => true;
    public bool FixtureOnly => true;
    public bool CanStartRecorder => false;
    public bool CanPlaybackRecording => false;
    public bool CanCaptureMouseOrKeyboard => false;
    public bool CanCaptureScreenOrScreenshot => false;
    public bool CanPromoteToLiveRun => false;
}

public sealed record ReliableRecipeLabRecorderChecklistItem(
    string Code,
    string Title,
    string Description,
    string Severity,
    bool IsBlocking,
    string RecommendedFix);

public sealed record ReliableRecipeLabDetectedVariableRow(
    string Name,
    string Source,
    string ReplacementToken,
    bool IsSensitive,
    bool NeedsUserConfirmation);

public sealed record ReliableRecipeLabEvalPanel(
    string ScenarioId,
    string FinalDecision,
    string SuccessRateLabel,
    string ExpectedOutcomeMatchRateLabel,
    string FlakinessLabel,
    string EvidenceCompletenessLabel,
    string ValidationCompletenessLabel,
    IReadOnlyList<string> TopFailureKinds,
    ReliableRecipeLabEvalNotice EvalNotice,
    IReadOnlyList<string> ReadOnlyActionLabels)
{
    public bool ReadOnly => true;
    public bool FixtureOnly => true;
    public bool CanRunEvalLive => false;
    public bool CanExecuteRecipe => false;
    public bool CanReplayRecording => false;
    public bool CanCreateBrowserOrDesktopRuntime => false;
}

public sealed record ReliableRecipeLabEvalScenarioRow(
    string ScenarioId,
    string Name,
    string ExpectedOutcome,
    string FinalDecision,
    string Tone,
    string Summary);

public sealed record ReliableRecipeLabEvalNotice(string Message);

public sealed record ReliableRecipeLabSandboxReadinessPanel(
    string DecisionLabel,
    double ReadinessScore,
    string AllowedAssessmentModeLabel,
    string RequiredIsolationModeLabel,
    IReadOnlyList<string> BlockedCapabilities,
    IReadOnlyList<string> MissingRequirements,
    IReadOnlyList<string> FutureUnlockConditions,
    string FixtureOnlyNotice,
    IReadOnlyList<string> ReadOnlyActionLabels)
{
    public bool ReadOnly => true;
    public bool FixtureOnly => true;
    public bool CanRunInSandbox => false;
    public bool CanLaunchSandbox => false;
    public bool CanExecuteIsolated => false;
    public bool CanStartBrowser => false;
    public bool CanStartDesktop => false;
    public bool SandboxRuntimeEnabled => false;
}

public sealed record ReliableRecipeLabFixture(
    string FixtureId,
    ReliableRecipeDefinition Recipe,
    ReliableRecipePreflightReport PreflightReport,
    ReliableRecipeLabViewModel ViewModel);

public static class ReliableRecipeLabViewModelMapper
{
    private static readonly string[] ReadOnlyActions = ["Review", "Open details", "Copy report", "Create draft fix", "Export summary"];

    public static ReliableRecipeLabViewModel Map(ReliableRecipeDefinition recipe, ReliableRecipePreflightReport report)
    {
        var quality = report.QualityReport;
        var tone = ToneFor(quality.Decision);
        var findings = quality.BlockingFindings.Select(ToFinding).ToArray();
        var warnings = quality.Warnings.Select(ToFinding).ToArray();

        return new ReliableRecipeLabViewModel(
            recipe.Id,
            recipe.Name,
            recipe.Version,
            ReadinessLabel(quality.Decision),
            report.ModeRequested.ToString(),
            report.ModeAllowed == ReliableRecipeRunMode.DryRun ? "Dry-run candidate" : "Draft only",
            quality.OverallScore,
            DecisionLabel(quality.Decision),
            tone,
            SummaryFor(quality),
            quality.CategoryScores.Select(c => ToCategoryCard(c, quality)).ToArray(),
            findings,
            warnings,
            quality.RecommendedNextActions,
            EvidencePanel(quality),
            ValidationPanel(quality),
            TargetPanel(quality),
            RiskPanel(quality),
            SandboxPanel(quality),
            HumanPanel(quality, report),
            PerceptionPanel(quality),
            ReliableRecipePerceptionIntegrationReportMapper.ToLabPanel(ReliableRecipePerceptionIntegrationEvaluator.Evaluate(recipe, report)),
            RecorderPanel(recipe, quality),
            EmptyRecorderDraftReview(recipe),
            EmptyEvalPanel(),
            ComputerUseSandboxReadinessReportMapper.ToLabPanel(ComputerUseSandboxReadinessEvaluator.Evaluate(recipe, report)),
            TimelineItems(recipe),
            NoLiveNotice(report),
            ReadOnlyActions);
    }

    private static string ReadinessLabel(ReliableRecipeQualityDecision decision) =>
        decision switch
        {
            ReliableRecipeQualityDecision.PassDryRun => "Dry-run candidate",
            ReliableRecipeQualityDecision.PassDraftOnly => "Draft only",
            ReliableRecipeQualityDecision.NeedsReview => "Needs review",
            _ => "Blocked by policy"
        };

    private static string DecisionLabel(ReliableRecipeQualityDecision decision) =>
        decision switch
        {
            ReliableRecipeQualityDecision.PassDryRun => "Quality and readiness report: dry-run candidate",
            ReliableRecipeQualityDecision.PassDraftOnly => "Quality and readiness report: draft only",
            ReliableRecipeQualityDecision.NeedsReview => "Quality and readiness report: needs review",
            _ => "Quality and readiness report: blocked by policy"
        };

    private static string ToneFor(ReliableRecipeQualityDecision decision) =>
        decision switch
        {
            ReliableRecipeQualityDecision.PassDryRun => "success",
            ReliableRecipeQualityDecision.PassDraftOnly => "info",
            ReliableRecipeQualityDecision.NeedsReview => "warning",
            _ => "danger"
        };

    private static string SummaryFor(ReliableRecipeQualityReport quality) =>
        quality.Decision == ReliableRecipeQualityDecision.PassDryRun
            ? "Read-only Recipe Lab shows a fixture-safe dry-run candidate. Runtime not enabled."
            : "Read-only Recipe Lab shows readiness gaps, blocked policy reasons and recommended fixes. Runtime not enabled.";

    private static ReliableRecipeLabFindingViewModel ToFinding(ReliableRecipeQualityFinding finding) =>
        new(finding.Code, finding.Severity, TitleFor(finding.Code), finding.Message, finding.AffectedBlockId, finding.RecommendedFix, finding.IsBlocking);

    private static string TitleFor(string code)
    {
        if (code.Contains("validation", StringComparison.OrdinalIgnoreCase)) return "Missing validation";
        if (code.Contains("evidence", StringComparison.OrdinalIgnoreCase)) return "Evidence incomplete";
        if (code.Contains("target", StringComparison.OrdinalIgnoreCase)) return "Target confidence needs review";
        if (code.Contains("sandbox", StringComparison.OrdinalIgnoreCase)) return "Sandbox readiness blocked";
        if (code.Contains("risk", StringComparison.OrdinalIgnoreCase)) return "Risk posture requires review";
        if (code.Contains("recorder", StringComparison.OrdinalIgnoreCase)) return "Recorder draft needs review";
        if (code.Contains("human", StringComparison.OrdinalIgnoreCase)) return "Human handoff required";
        return "Readiness finding";
    }

    private static ReliableRecipeLabCategoryCard ToCategoryCard(ReliableRecipeQualityCategoryScore score, ReliableRecipeQualityReport quality)
    {
        var related = quality.BlockingFindings
            .Where(f => f.Code.Contains(score.Category.ToString(), StringComparison.OrdinalIgnoreCase) || CategoryMatches(score.Category, f.Code))
            .Take(3)
            .Select(f => f.Message)
            .ToArray();

        return new ReliableRecipeLabCategoryCard(
            score.Category,
            score.Score,
            score.Category.ToString(),
            score.Decision == ReliableRecipeQualityDecision.Blocked ? "danger" : score.Decision == ReliableRecipeQualityDecision.NeedsReview ? "warning" : "success",
            score.Score >= 0.72 ? "Looks complete for fixture review." : "Needs attention before fixture review.",
            related);
    }

    private static bool CategoryMatches(ReliableRecipeQualityCategory category, string code) =>
        category switch
        {
            ReliableRecipeQualityCategory.Validation => code.Contains("validation", StringComparison.OrdinalIgnoreCase),
            ReliableRecipeQualityCategory.Evidence => code.Contains("evidence", StringComparison.OrdinalIgnoreCase),
            ReliableRecipeQualityCategory.Risk => code.Contains("risk", StringComparison.OrdinalIgnoreCase),
            ReliableRecipeQualityCategory.TargetResolution => code.Contains("target", StringComparison.OrdinalIgnoreCase),
            ReliableRecipeQualityCategory.Sandbox => code.Contains("sandbox", StringComparison.OrdinalIgnoreCase),
            ReliableRecipeQualityCategory.HumanIntervention => code.Contains("human", StringComparison.OrdinalIgnoreCase),
            ReliableRecipeQualityCategory.RecorderDraftSafety => code.Contains("recorder", StringComparison.OrdinalIgnoreCase),
            _ => false
        };

    private static ReliableRecipeLabPanelViewModel EvidencePanel(ReliableRecipeQualityReport quality) =>
        new(
            "Evidence completeness",
            quality.EvidenceCompleteness.Score,
            quality.EvidenceCompleteness.MissingEvidenceKinds.Count == 0 ? "success" : "danger",
            quality.EvidenceCompleteness.StepReports.Select(r => new ReliableRecipeLabPanelRow(
                r.StepId,
                r.MissingKinds.Count == 0 ? "Evidence expectations complete" : $"Missing: {string.Join(", ", r.MissingKinds)}",
                r.MissingKinds.Count == 0 ? "success" : "danger")).ToArray(),
            "No evidence expectations were required.",
            "Evidence is reference-only; no raw payloads or screenshots are captured here.");

    private static ReliableRecipeLabPanelViewModel ValidationPanel(ReliableRecipeQualityReport quality) =>
        new(
            "Validation completeness",
            quality.ValidationCompleteness.Score,
            quality.ValidationCompleteness.MissingValidations.Count == 0 ? "success" : "danger",
            quality.ValidationCompleteness.CriticalBlockReports.Select(r => new ReliableRecipeLabPanelRow(
                r.BlockId,
                r.MissingChecks.Count == 0 ? "Validation coverage complete" : $"Missing: {string.Join(", ", r.MissingChecks)}",
                r.MissingChecks.Count == 0 ? "success" : "danger")).ToArray(),
            "No validation checks were required.",
            "No success is implied without validation coverage.");

    private static ReliableRecipeLabPanelViewModel TargetPanel(ReliableRecipeQualityReport quality) =>
        new(
            "Target confidence",
            quality.TargetResolutionQuality.Score,
            quality.TargetResolutionQuality.Decision == TargetResolutionQualityDecision.Blocked ? "danger" : quality.TargetResolutionQuality.Decision == TargetResolutionQualityDecision.NeedsHumanReview ? "warning" : "success",
            [
                new("Resolution mode", quality.TargetResolutionQuality.ResolutionMode.ToString()),
                new("Decision", quality.TargetResolutionQuality.Decision.ToString(), quality.TargetResolutionQuality.Decision == TargetResolutionQualityDecision.Blocked ? "danger" : "info"),
                new("Ambiguity", quality.TargetResolutionQuality.Ambiguity ?? "None")
            ],
            "No target descriptor supplied.",
            "Confidence is a planning signal, not action authority.");

    private static ReliableRecipeLabPanelViewModel RiskPanel(ReliableRecipeQualityReport quality) =>
        new(
            "Risk posture",
            Math.Round(1 - quality.RiskPosture.RiskScore, 2),
            quality.RiskPosture.BlockedReasons.Count == 0 ? "success" : "danger",
            [
                new("Highest risk", quality.RiskPosture.HighestRisk.ToString(), quality.RiskPosture.BlockedReasons.Count == 0 ? "info" : "danger"),
                new("Required approvals", quality.RiskPosture.RequiredApprovals.Count == 0 ? "None" : string.Join(", ", quality.RiskPosture.RequiredApprovals)),
                new("Blocked reasons", quality.RiskPosture.BlockedReasons.Count == 0 ? "None" : string.Join(", ", quality.RiskPosture.BlockedReasons), quality.RiskPosture.BlockedReasons.Count == 0 ? "info" : "danger")
            ],
            "No risk posture was supplied.",
            "Risk cannot be reduced by confidence alone.");

    private static ReliableRecipeLabPanelViewModel SandboxPanel(ReliableRecipeQualityReport quality) =>
        new(
            "Sandbox readiness",
            quality.SandboxReadiness.Score,
            quality.SandboxReadiness.BlockedCapabilities.Count == 0 ? "success" : "danger",
            [
                new("Isolation mode", quality.SandboxReadiness.IsolationMode.ToString()),
                new("Fixture evaluation", quality.SandboxReadiness.CanEvaluateInFixture ? "Fixture-safe only" : "Not available", quality.SandboxReadiness.CanEvaluateInFixture ? "success" : "danger"),
                new("Blocked capabilities", quality.SandboxReadiness.BlockedCapabilities.Count == 0 ? "None" : string.Join(", ", quality.SandboxReadiness.BlockedCapabilities), quality.SandboxReadiness.BlockedCapabilities.Count == 0 ? "info" : "danger")
            ],
            "No sandbox profile supplied.",
            "No VM, container, desktop control or sandbox runtime is created.");

    private static ReliableRecipeLabPanelViewModel HumanPanel(ReliableRecipeQualityReport quality, ReliableRecipePreflightReport report)
    {
        var rows = new List<ReliableRecipeLabPanelRow>
        {
            new("Score", quality.HumanInterventionPlanQuality.Score.ToString("0.00")),
            new("Required interventions", report.RequiredHumanInterventions.Count == 0 ? "None" : string.Join(", ", report.RequiredHumanInterventions), report.RequiredHumanInterventions.Count == 0 ? "info" : "warning")
        };

        if (report.RequiredHumanInterventions.Any(r => r is ReliableHumanInterventionReason.CaptchaDetected or ReliableHumanInterventionReason.TwoFactorRequired or ReliableHumanInterventionReason.CredentialRequired))
            rows.Add(new ReliableRecipeLabPanelRow("Challenge boundary", "Human handoff required. No bypass or solver.", "warning"));

        return new ReliableRecipeLabPanelViewModel(
            "Human handoff",
            quality.HumanInterventionPlanQuality.Score,
            quality.HumanInterventionPlanQuality.MissingUserInstructions.Count == 0 ? "success" : "danger",
            rows,
            "No human handoff required by this fixture.",
            "Handoff explains what happened, what was tried and what the operator must do.");
    }

    private static ReliableRecipeLabPanelViewModel PerceptionPanel(ReliableRecipeQualityReport quality)
    {
        var rows = new List<ReliableRecipeLabPanelRow>
        {
            new("Signal agreement", quality.PerceptionSignalQuality.SignalAgreement.ToString("0.00")),
            new("Sensitive data risk", quality.PerceptionSignalQuality.SensitiveDataRisk ? "Present" : "Not detected", quality.PerceptionSignalQuality.SensitiveDataRisk ? "warning" : "info")
        };

        if (quality.TargetResolutionQuality.ResolutionMode == ReliableActionResolutionMode.OcrRegion && quality.TargetResolutionQuality.SensitiveActionRisk)
            rows.Add(new ReliableRecipeLabPanelRow("OCR boundary", "OCR signal is supporting evidence only and cannot authorize sensitive actions.", "danger"));
        else if (quality.TargetResolutionQuality.ResolutionMode == ReliableActionResolutionMode.OcrRegion)
            rows.Add(new ReliableRecipeLabPanelRow("OCR boundary", "OCR signal used as supporting signal for read-only review.", "warning"));

        return new ReliableRecipeLabPanelViewModel(
            "Perception and OCR",
            quality.PerceptionSignalQuality.OverallScore,
            quality.TargetResolutionQuality.Decision == TargetResolutionQualityDecision.Blocked ? "danger" : "info",
            rows,
            "No perception snapshot supplied.",
            "OCR is read-only supporting evidence, not action authority.");
    }

    private static ReliableRecipeLabPanelViewModel RecorderPanel(ReliableRecipeDefinition recipe, ReliableRecipeQualityReport quality) =>
        new(
            "Recorder draft safety",
            recipe.CreatedFrom == ReliableRecipeCreatedFrom.RecorderDraft ? 0.35 : 1,
            recipe.CreatedFrom == ReliableRecipeCreatedFrom.RecorderDraft ? "warning" : "success",
            [
                new("Created from", recipe.CreatedFrom.ToString()),
                new("Recorder status", recipe.CreatedFrom == ReliableRecipeCreatedFrom.RecorderDraft ? "Draft only until reviewed and validated." : "Not a recorder draft.")
            ],
            "No recorder draft metadata.",
            "Recorder output cannot become run-ready from this surface.");

    private static ReliableRecipeLabRecorderDraftReviewPanel EmptyRecorderDraftReview(ReliableRecipeDefinition recipe) =>
        new(
            recipe.CreatedFrom == ReliableRecipeCreatedFrom.RecorderDraft ? "Recorder draft fixture" : "Not sourced from recorder fixture",
            recipe.CreatedFrom == ReliableRecipeCreatedFrom.RecorderDraft ? "Draft only / needs review" : "Not applicable",
            [],
            [],
            "No sensitive recorder input summary is attached to this lab snapshot.",
            "Recorder-to-recipe conversion is fixture-only and cannot create live-ready recipes.");

    private static ReliableRecipeLabEvalPanel EmptyEvalPanel() =>
        new(
            "not-evaluated",
            "Not evaluated",
            "No fixture eval attached",
            "No fixture eval attached",
            "No fixture eval attached",
            "No fixture eval attached",
            "No fixture eval attached",
            [],
            new ReliableRecipeLabEvalNotice("Fixture-only evaluation. Runtime not enabled."),
            ["Review scenario", "Open report", "Copy summary"]);

    private static IReadOnlyList<ReliableRecipeLabTimelinePreviewItem> TimelineItems(ReliableRecipeDefinition recipe) =>
        recipe.Blocks.Select(b => new ReliableRecipeLabTimelinePreviewItem(
            b.Id,
            b.Label,
            b.ValidationRequirements.Count == 0 || b.EvidenceExpectations.Count == 0 ? "Needs review" : "Fixture preview",
            b.EvidenceExpectations.Count == 0 ? "No evidence expectations configured" : string.Join(", ", b.EvidenceExpectations),
            b.ValidationRequirements.Count == 0 ? "No validation requirements configured" : string.Join(", ", b.ValidationRequirements),
            b.Risk.ToString())).ToArray();

    private static ReliableRecipeLabNoLiveRuntimeNotice NoLiveNotice(ReliableRecipePreflightReport report) =>
        new(
            "Runtime not enabled",
            "This Recipe Lab is read-only and fixture-safe. It shows readiness, confidence, evidence gaps and review steps without enabling automation.",
            [
                "Browser runtime",
                "CDP runtime",
                "Desktop automation",
                "Connector/API execution",
                "Recorder/replay/capture",
                "Sandbox/VM runtime",
                "Provider/LLM calls",
                "OCR runtime activation",
                "No screenshot capture"
            ],
            report.ModeAllowed == ReliableRecipeRunMode.DryRun ? "Dry-run candidate" : "Draft only");
}

public static class ReliableRecipeLabFixtureCatalog
{
    public static IReadOnlyList<ReliableRecipeLabFixture> All() =>
    [
        Create("safe_invoice_download_quality_pass", SafeInvoiceDownloadDryRunCandidate(), ReliableRecipeRunMode.DryRun, GoodContext()),
        Create("submit_without_validation_quality_blocked", SubmitWithoutValidationBlocked(), ReliableRecipeRunMode.DryRun, GoodContext()),
        Create("ocr_only_sensitive_submit_blocked", OcrOnlySensitiveSubmitBlocked(), ReliableRecipeRunMode.DryRun, OcrSensitiveContext()),
        Create("recorder_draft_quality_draft_only", RecorderDraftNeedsReview(), ReliableRecipeRunMode.DryRun, GoodContext()),
        Create("captcha_handoff_quality_review", CaptchaTwoFactorHumanHandoff(), ReliableRecipeRunMode.DryRun, GoodContext() with { HumanInterventionRequests = [ReliableHumanInterventionFactory.ForChallenge(ReliableHumanInterventionReason.TwoFactorRequired, ["evidence.2fa"])] }),
        Create("desktop_live_sandbox_blocked", DesktopLiveSandboxBlocked(), ReliableRecipeRunMode.DryRun, GoodContext() with { SandboxProfile = new ComputerUseSandboxProfile(ReliableSandboxIsolationMode.VmFuture, [ReliableComputerUseSurface.Desktop], "blocked", "fixture", "secret-refs-only", "rollback", "evidence", 60) }),
        Create("external_side_effect_needs_approval", ExternalSideEffectNeedsApproval(), ReliableRecipeRunMode.DryRun, GoodContext()),
        Create("ambiguous_target_needs_review", AmbiguousTargetNeedsReview(), ReliableRecipeRunMode.DryRun, GoodContext() with { TargetDescriptor = Target(ReliableActionResolutionMode.VisibleTextApproximate, [ReliableActionResolutionMode.VisibleTextApproximate], 0.48, "Multiple matching Continue buttons") }),
        Create("high_quality_high_risk_blocked", HighQualityHighRiskBlocked(), ReliableRecipeRunMode.DryRun, GoodContext())
    ];

    public static ReliableRecipeLabFixture Get(string fixtureId) =>
        All().Single(f => f.FixtureId == fixtureId);

    private static ReliableRecipeLabFixture Create(string fixtureId, ReliableRecipeDefinition recipe, ReliableRecipeRunMode mode, ReliableRecipeQualityContext context)
    {
        var report = ReliableRecipePreflightComposer.Compose(recipe, mode, context: context);
        var view = ReliableRecipeLabViewModelMapper.Map(recipe, report);
        return new ReliableRecipeLabFixture(fixtureId, recipe, report, view);
    }

    private static ReliableRecipeDefinition SafeInvoiceDownloadDryRunCandidate() =>
        Base("safe_invoice_download_quality_pass", ReliableRecipeRiskProfile.ReadOnly, [
            Block("download", ReliableRecipeBlockKind.FileDownloadEvidence, ReliableRecipeRiskProfile.LocalWrite, FullCriticalEvidence().Concat(["evidence.download"]).ToArray(), ["validation.download", "validation.timeline"])
        ]);

    private static ReliableRecipeDefinition SubmitWithoutValidationBlocked() =>
        Base("submit_without_validation_quality_blocked", ReliableRecipeRiskProfile.ExternalSideEffect, [
            Block("submit", ReliableRecipeBlockKind.BrowserAction, ReliableRecipeRiskProfile.ExternalSideEffect, FullCriticalEvidence(), [])
        ]);

    private static ReliableRecipeDefinition OcrOnlySensitiveSubmitBlocked() =>
        Base("ocr_only_sensitive_submit_blocked", ReliableRecipeRiskProfile.ExternalSideEffect | ReliableRecipeRiskProfile.SensitiveData, [
            Block("submit", ReliableRecipeBlockKind.BrowserAction, ReliableRecipeRiskProfile.ExternalSideEffect | ReliableRecipeRiskProfile.SensitiveData, FullCriticalEvidence(), ["validation.visible", "validation.timeline"])
        ]);

    private static ReliableRecipeDefinition RecorderDraftNeedsReview() =>
        Base("recorder_draft_quality_draft_only", ReliableRecipeRiskProfile.ReadOnly, [
            Block("draft", ReliableRecipeBlockKind.BrowserAction, ReliableRecipeRiskProfile.ReadOnly, FullCriticalEvidence(), ["validation.visible", "validation.timeline"])
        ]) with { CreatedFrom = ReliableRecipeCreatedFrom.RecorderDraft, Readiness = ReliableRecipeReadiness.DesignOnly };

    private static ReliableRecipeDefinition CaptchaTwoFactorHumanHandoff() =>
        Base("captcha_two_factor_handoff", ReliableRecipeRiskProfile.Credentialed, [
            Block("handoff", ReliableRecipeBlockKind.HumanIntervention, ReliableRecipeRiskProfile.Credentialed, FullCriticalEvidence().Concat(["evidence.approval"]).ToArray(), ["validation.manual", "validation.timeline"])
        ]);

    private static ReliableRecipeDefinition DesktopLiveSandboxBlocked() =>
        Base("desktop_live_sandbox_blocked", ReliableRecipeRiskProfile.ExternalSideEffect, [
            Block("desktop", ReliableRecipeBlockKind.DesktopFuture, ReliableRecipeRiskProfile.ExternalSideEffect, FullCriticalEvidence(), ["validation.manual"])
        ]);

    private static ReliableRecipeDefinition ExternalSideEffectNeedsApproval() =>
        Base("external_side_effect_needs_approval", ReliableRecipeRiskProfile.ExternalSideEffect, [
            Block("submit", ReliableRecipeBlockKind.BrowserAction, ReliableRecipeRiskProfile.ExternalSideEffect, FullCriticalEvidence().Where(e => !e.Contains("after", StringComparison.Ordinal)).ToArray(), ["validation.visible"])
        ]);

    private static ReliableRecipeDefinition AmbiguousTargetNeedsReview() =>
        Base("ambiguous_target_needs_review", ReliableRecipeRiskProfile.ReadOnly, [
            Block("choose", ReliableRecipeBlockKind.BrowserAction, ReliableRecipeRiskProfile.ReadOnly, FullCriticalEvidence(), ["validation.visible", "validation.timeline"])
        ]);

    private static ReliableRecipeDefinition HighQualityHighRiskBlocked() =>
        Base("high_quality_high_risk_blocked", ReliableRecipeRiskProfile.Financial | ReliableRecipeRiskProfile.ExternalSideEffect, [
            Block("payment", ReliableRecipeBlockKind.BrowserAction, ReliableRecipeRiskProfile.Financial | ReliableRecipeRiskProfile.ExternalSideEffect, FullCriticalEvidence().Concat(["evidence.approval"]).ToArray(), ["validation.visible", "validation.timeline"])
        ]);

    private static ReliableRecipeDefinition Base(string id, ReliableRecipeRiskProfile risk, IReadOnlyList<ReliableRecipeBlock> blocks) =>
        new(
            id,
            id.Replace('_', ' '),
            "1.0.0",
            "workspace.fixture",
            [],
            blocks,
            new ReliableRecipeRunLimits(10, 1, 2, 60, ["fixture.local"], blocks.Select(b => b.Kind).Distinct().ToArray(), new ReliableCompleteCriteria([new ReliableValidationCheck("complete", ReliableValidationCheckKind.TimelineEventCreated, "timeline.complete", Passed: true)]), new ReliableTerminateCriteria([new ReliableValidationCheck("terminate", ReliableValidationCheckKind.ManualConfirmationRequired, "human.stop", Passed: true)])),
            risk,
            ReliableRecipeReadiness.RunnableDryRun,
            ReliableRecipeCreatedFrom.ManualDesign);

    private static ReliableRecipeBlock Block(string id, ReliableRecipeBlockKind kind, ReliableRecipeRiskProfile risk, IReadOnlyList<string> evidence, IReadOnlyList<string> validation) =>
        new(id, kind, id.Replace('-', ' '), new Dictionary<string, string>(), [], [], risk, evidence, validation);

    private static IReadOnlyList<string> FullCriticalEvidence() =>
    [
        "evidence.before",
        "evidence.after",
        "evidence.proposal",
        "evidence.result",
        "evidence.validation",
        "evidence.timeline",
        "evidence.perception"
    ];

    private static ReliableRecipeQualityContext GoodContext() =>
        new(
            TargetDescriptor: Target(ReliableActionResolutionMode.StableSelector, [ReliableActionResolutionMode.StableSelector, ReliableActionResolutionMode.AccessibilityTree, ReliableActionResolutionMode.OcrRegion], 0.94),
            PerceptionSnapshot: AgreementSnapshot(),
            SandboxProfile: new ComputerUseSandboxProfile(ReliableSandboxIsolationMode.DryRunFixture, [ReliableComputerUseSurface.Browser], "blocked-by-default", "fixture-only", "secret-refs-only", "rollback-fixture-state", "reference-only", 60),
            HumanInterventionRequests: []);

    private static ReliableRecipeQualityContext OcrSensitiveContext() =>
        GoodContext() with
        {
            TargetDescriptor = Target(ReliableActionResolutionMode.OcrRegion, [ReliableActionResolutionMode.OcrRegion], 0.9),
            PerceptionSnapshot = new ReliablePerceptionStackSnapshot("snapshot.ocr-sensitive", ReliablePerceptionSourceSurface.FixtureBrowser, [], [], [new ReliablePerceptionSignal(ReliablePerceptionSignalKind.OcrText, "ocr.1", "Submit official form", 0.9, SensitiveDataRisk: true)], [], [], true, new ReliablePerceptionConfidence(0, 0, [], true))
        };

    private static ReliableTargetDescriptor Target(ReliableActionResolutionMode mode, IReadOnlyList<ReliableActionResolutionMode> signals, double score, string? ambiguity = null) =>
        new("Submit invoice", "button", mode == ReliableActionResolutionMode.StableSelector ? "#submit-invoice" : null, new ReliableBoundingBox(1, 2, 3, 4), null, "element.1", "tab.fixture", mode, new ReliableTargetResolutionConfidence(score, signals, ambiguity, ReliableRecipePolicyDecision.AllowDryRunOnly));

    private static ReliablePerceptionStackSnapshot AgreementSnapshot() =>
        new(
            "snapshot.agree",
            ReliablePerceptionSourceSurface.FixtureBrowser,
            [new ReliablePerceptionSignal(ReliablePerceptionSignalKind.DomElement, "dom.1", "Submit invoice", 0.96)],
            [new ReliablePerceptionSignal(ReliablePerceptionSignalKind.AccessibilityNode, "a11y.1", "Submit invoice", 0.94)],
            [new ReliablePerceptionSignal(ReliablePerceptionSignalKind.OcrText, "ocr.1", "Submit invoice", 0.9)],
            [],
            [],
            true,
            new ReliablePerceptionConfidence(0, 0, [], false));
}
