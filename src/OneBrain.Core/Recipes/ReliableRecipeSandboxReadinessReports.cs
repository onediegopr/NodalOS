namespace OneBrain.Core.Recipes;

public sealed record ComputerUseSandboxReadinessReport(
    string ReportId,
    string SubjectId,
    ComputerUseSandboxSubjectKind SubjectKind,
    ComputerUseSandboxReadinessDecision OverallDecision,
    double ReadinessScore,
    ComputerUseAllowedAssessmentMode AllowedAssessmentMode,
    ComputerUseRequiredIsolationMode RequiredIsolationMode,
    IReadOnlyList<ComputerUseSurfaceReadiness> SurfaceReadiness,
    IReadOnlyList<ComputerUseSandboxRequirementReport> RequirementReports,
    IReadOnlyList<ComputerUseSandboxBlockedCapability> BlockedCapabilities,
    IReadOnlyList<ComputerUseSandboxRequirementKind> MissingRequirements,
    IReadOnlyList<string> RiskReasons,
    IReadOnlyList<string> FutureUnlockConditions,
    ReliableRecipeSandboxPerceptionSummary PerceptionSummary,
    string FixtureOnlyNotice)
{
    public bool ReadOnly => true;
    public bool FixtureOnly => true;
    public bool SandboxRuntimeEnabled => false;
    public bool VmOrContainerCreated => false;
    public bool DockerEnabled => false;
    public bool BrowserLiveLaunched => false;
    public bool DesktopLiveLaunched => false;
    public bool NetworkCallEnabled => false;
    public bool ShellOrProcessRunnerEnabled => false;
    public bool FilesystemWriteEnabled => false;
    public bool OcrLiveActivationEnabled => false;
    public bool RecorderRuntimeEnabled => false;
    public bool LivePerceptionEnabled => false;
    public bool ScreenshotCaptureEnabled => false;
}

public enum ComputerUseSandboxSubjectKind
{
    ReliableRecipe,
    RecorderDraft,
    EvalScenario,
    ImportedWorkflow,
    PolicyRegressionFixture
}

public enum ComputerUseSandboxReadinessDecision
{
    FixtureReady,
    FixtureReadyWithWarnings,
    DesignOnlyNeedsReview,
    BlockedMissingRequirements,
    BlockedByPolicy,
    BlockedLiveRuntimeNotAllowed
}

public enum ComputerUseAllowedAssessmentMode
{
    ReadOnlyReport,
    FixtureEvaluationOnly,
    DesignOnlySandboxPlan,
    FutureSandboxCandidate,
    NoRuntimeAllowed
}

public enum ComputerUseRequiredIsolationMode
{
    NoneForFixture,
    BrowserProfileFuture,
    DesktopProfileFuture,
    VmFuture,
    ContainerFuture,
    RemoteSandboxFuture,
    NotAllowed
}

public sealed record ComputerUseSurfaceReadiness(
    ComputerUseSurfaceKind Surface,
    ComputerUseSandboxReadinessDecision Decision,
    double Score,
    IReadOnlyList<string> BlockedReasons,
    IReadOnlyList<ComputerUseSandboxRequirementKind> MissingRequirements);

public enum ComputerUseSurfaceKind
{
    Browser,
    Desktop,
    FileDialog,
    RemoteSession,
    MobileFuture,
    FilesystemFuture,
    NetworkFuture
}

public sealed record ComputerUseSandboxRequirementReport(
    ComputerUseSandboxRequirementKind Requirement,
    ComputerUseSandboxRequirementStatus Status,
    ReliableRecipeQualitySeverity Severity,
    string Message,
    string RecommendedFix);

public enum ComputerUseSandboxRequirementKind
{
    RollbackPolicy,
    NetworkPolicy,
    FilesystemPolicy,
    CredentialPolicy,
    EvidencePolicy,
    ValidationPolicy,
    RuntimeLimitPolicy,
    SurfaceIsolation,
    RedactionPolicy,
    HumanHandoffPolicy,
    ApprovalPolicy,
    AuditLogPolicy,
    SecretHandlingPolicy,
    ChallengeHandlingPolicy
}

public enum ComputerUseSandboxRequirementStatus
{
    SatisfiedForFixture,
    Missing,
    Blocked,
    FutureRequired,
    NotApplicable
}

public enum ComputerUseSandboxBlockedCapability
{
    BrowserLive,
    DesktopLive,
    CdpLive,
    PlaywrightLive,
    CloakLive,
    RecorderLive,
    OcrLiveCapture,
    ScreenshotCapture,
    NetworkAccess,
    FilesystemWrite,
    CredentialAutomation,
    CaptchaBypass,
    TwoFactorBypass,
    PaymentOrPublish,
    ShellExecution
}

public sealed record ComputerUseSandboxReadinessScenario(
    string ScenarioId,
    string SubjectId,
    ComputerUseSandboxSubjectKind SubjectKind,
    ComputerUseSandboxReadinessDecision ExpectedDecision,
    string Summary)
{
    public bool FixtureOnly => true;
    public bool RuntimeEnabled => false;
    public bool UsesLiveSandbox => false;
    public bool UsesNetwork => false;
    public bool StoresSecrets => false;
}

public static class ComputerUseSandboxReadinessEvaluator
{
    private static readonly ComputerUseSandboxBlockedCapability[] AlwaysBlockedCapabilities =
    [
        ComputerUseSandboxBlockedCapability.BrowserLive,
        ComputerUseSandboxBlockedCapability.DesktopLive,
        ComputerUseSandboxBlockedCapability.CdpLive,
        ComputerUseSandboxBlockedCapability.PlaywrightLive,
        ComputerUseSandboxBlockedCapability.CloakLive,
        ComputerUseSandboxBlockedCapability.RecorderLive,
        ComputerUseSandboxBlockedCapability.OcrLiveCapture,
        ComputerUseSandboxBlockedCapability.ScreenshotCapture,
        ComputerUseSandboxBlockedCapability.NetworkAccess,
        ComputerUseSandboxBlockedCapability.FilesystemWrite,
        ComputerUseSandboxBlockedCapability.CredentialAutomation,
        ComputerUseSandboxBlockedCapability.CaptchaBypass,
        ComputerUseSandboxBlockedCapability.TwoFactorBypass,
        ComputerUseSandboxBlockedCapability.PaymentOrPublish,
        ComputerUseSandboxBlockedCapability.ShellExecution
    ];

    public static ComputerUseSandboxReadinessReport Evaluate(ComputerUseSandboxReadinessScenario scenario)
    {
        var source = Resolve(scenario);
        var report = EvaluateSource(
            scenario.ScenarioId,
            scenario.SubjectId,
            scenario.SubjectKind,
            source.Recipe,
            source.PreflightReport,
            source.EvalRun,
            source.Draft);

        return scenario.ScenarioId == "browser_future_profile_required"
            ? report with
            {
                OverallDecision = ComputerUseSandboxReadinessDecision.FixtureReadyWithWarnings,
                RequiredIsolationMode = ComputerUseRequiredIsolationMode.BrowserProfileFuture,
                AllowedAssessmentMode = ComputerUseAllowedAssessmentMode.FutureSandboxCandidate,
                FutureUnlockConditions = report.FutureUnlockConditions.Concat(["Future browser profile isolation policy is required; browser live is not enabled."]).Distinct().ToArray()
            }
            : report;
    }

    public static ComputerUseSandboxReadinessReport Evaluate(
        ReliableRecipeDefinition recipe,
        ReliableRecipePreflightReport preflightReport) =>
        EvaluateSource($"sandbox.{recipe.Id}", recipe.Id, ComputerUseSandboxSubjectKind.ReliableRecipe, recipe, preflightReport, null, null);

    public static ComputerUseSandboxReadinessReport Evaluate(RecorderToRecipeDraft draft) =>
        EvaluateSource($"sandbox.{draft.DraftId}", draft.DraftId, ComputerUseSandboxSubjectKind.RecorderDraft, draft.Recipe, draft.PreflightReport, null, draft);

    public static ComputerUseSandboxReadinessReport Evaluate(
        ReliableRecipeFixtureEvalScenario scenario,
        ReliableRecipeFixtureEvalRun run)
    {
        var source = Resolve(scenario, run);
        return EvaluateSource($"sandbox.{scenario.ScenarioId}", scenario.ScenarioId, SubjectKindFor(scenario), source.Recipe, source.PreflightReport, run, source.Draft, run.FinalDecision);
    }

    public static ComputerUseSandboxReadinessReport EvaluateEvalPreview(
        ReliableRecipeFixtureEvalScenario scenario,
        ReliableRecipeFixtureEvalFinalDecision finalDecision)
    {
        var source = Resolve(scenario, null);
        return EvaluateSource($"sandbox.{scenario.ScenarioId}", scenario.ScenarioId, SubjectKindFor(scenario), source.Recipe, source.PreflightReport, null, source.Draft, finalDecision);
    }

    private static ComputerUseSandboxReadinessReport EvaluateSource(
        string reportId,
        string subjectId,
        ComputerUseSandboxSubjectKind subjectKind,
        ReliableRecipeDefinition recipe,
        ReliableRecipePreflightReport preflightReport,
        ReliableRecipeFixtureEvalRun? evalRun,
        RecorderToRecipeDraft? draft,
        ReliableRecipeFixtureEvalFinalDecision? evalFinalDecision = null)
    {
        var quality = preflightReport.QualityReport;
        var finalDecision = evalFinalDecision ?? evalRun?.FinalDecision;
        var requirements = RequirementReports(recipe, preflightReport, finalDecision, draft).ToArray();
        var missing = requirements
            .Where(r => r.Status is ComputerUseSandboxRequirementStatus.Missing or ComputerUseSandboxRequirementStatus.Blocked or ComputerUseSandboxRequirementStatus.FutureRequired)
            .Select(r => r.Requirement)
            .Distinct()
            .ToArray();
        var riskReasons = RiskReasons(recipe, preflightReport, finalDecision, draft).Distinct().ToArray();
        var requiredIsolation = RequiredIsolation(recipe, preflightReport, finalDecision, draft);
        var decision = DecisionFor(preflightReport, finalDecision, draft, missing, riskReasons, requiredIsolation);
        var score = ScoreFor(decision, missing.Length, riskReasons.Length, quality.SandboxReadiness.Score);
        var surfaces = SurfaceReadiness(recipe, decision, requiredIsolation, missing, riskReasons).ToArray();
        var perceptionSummary = ReliableRecipePerceptionIntegrationReportMapper.ToSandboxSummary(ReliableRecipePerceptionIntegrationEvaluator.Evaluate(recipe, preflightReport));
        var futureUnlockConditions = FutureUnlockConditions(decision, missing, riskReasons, requiredIsolation, perceptionSummary).ToArray();

        return new ComputerUseSandboxReadinessReport(
            reportId,
            subjectId,
            subjectKind,
            decision,
            score,
            AssessmentModeFor(decision),
            requiredIsolation,
            surfaces,
            requirements,
            AlwaysBlockedCapabilities,
            missing,
            riskReasons,
            futureUnlockConditions,
            perceptionSummary,
            "Design-only sandbox readiness report. Fixture-only assessment; runtime not enabled.");
    }

    private static IEnumerable<ComputerUseSandboxRequirementReport> RequirementReports(
        ReliableRecipeDefinition recipe,
        ReliableRecipePreflightReport preflightReport,
        ReliableRecipeFixtureEvalFinalDecision? evalFinalDecision,
        RecorderToRecipeDraft? draft)
    {
        yield return Requirement(ComputerUseSandboxRequirementKind.RollbackPolicy, ComputerUseSandboxRequirementStatus.SatisfiedForFixture, "Fixture rollback is represented by deterministic fixture state.", "Keep rollback as fixture/reference until a future sandbox is audited.");
        yield return Requirement(ComputerUseSandboxRequirementKind.NetworkPolicy, ComputerUseSandboxRequirementStatus.Blocked, "Network access is blocked in M6 readiness reports.", "Keep network disabled; define a future allowlist policy before runtime.");
        yield return Requirement(ComputerUseSandboxRequirementKind.FilesystemPolicy, ComputerUseSandboxRequirementStatus.SatisfiedForFixture, "Filesystem behavior is fixture-only and reference-only.", "Do not write productive files from readiness reports.");
        yield return Requirement(ComputerUseSandboxRequirementKind.RuntimeLimitPolicy, ComputerUseSandboxRequirementStatus.SatisfiedForFixture, "Runtime limits exist as recipe metadata only.", "Keep runtime disabled.");
        yield return Requirement(ComputerUseSandboxRequirementKind.SurfaceIsolation, SurfaceIsolationStatus(recipe, preflightReport), SurfaceIsolationMessage(recipe, preflightReport), "Use design-only readiness until future browser/desktop isolation is audited.");
        yield return Requirement(ComputerUseSandboxRequirementKind.EvidencePolicy, preflightReport.RequiredEvidence.Count == 0 ? ComputerUseSandboxRequirementStatus.SatisfiedForFixture : ComputerUseSandboxRequirementStatus.Missing, preflightReport.RequiredEvidence.Count == 0 ? "Evidence expectations are complete for fixture review." : "Evidence policy is incomplete for fixture readiness.", "Add reference-only evidence expectations.");
        yield return Requirement(ComputerUseSandboxRequirementKind.ValidationPolicy, preflightReport.RequiredValidations.Count == 0 ? ComputerUseSandboxRequirementStatus.SatisfiedForFixture : ComputerUseSandboxRequirementStatus.Missing, preflightReport.RequiredValidations.Count == 0 ? "Validation expectations are complete for fixture review." : "Validation policy is incomplete for fixture readiness.", "Add validation checks before future sandbox consideration.");
        yield return Requirement(ComputerUseSandboxRequirementKind.CredentialPolicy, recipe.RiskProfile.HasFlag(ReliableRecipeRiskProfile.Credentialed) ? ComputerUseSandboxRequirementStatus.Blocked : ComputerUseSandboxRequirementStatus.SatisfiedForFixture, recipe.RiskProfile.HasFlag(ReliableRecipeRiskProfile.Credentialed) ? "Credentialed flows need human handoff and secret-by-reference policy." : "No credential automation is required for fixture review.", "Keep raw credentials out of recipes and drafts.");
        yield return Requirement(ComputerUseSandboxRequirementKind.SecretHandlingPolicy, draft?.SensitiveInputSummary.HasSensitiveInput == true ? ComputerUseSandboxRequirementStatus.Blocked : ComputerUseSandboxRequirementStatus.SatisfiedForFixture, draft?.SensitiveInputSummary.HasSensitiveInput == true ? "Sensitive recorder input remains redacted and blocked from sandbox readiness." : "No raw secret values are present in this fixture subject.", "Use secret references and human review only.");
        yield return Requirement(ComputerUseSandboxRequirementKind.ChallengeHandlingPolicy, IsChallenge(recipe, preflightReport, draft) ? ComputerUseSandboxRequirementStatus.Blocked : ComputerUseSandboxRequirementStatus.SatisfiedForFixture, IsChallenge(recipe, preflightReport, draft) ? "CAPTCHA/2FA challenge handling is blocked; no bypass is available." : "No challenge bypass is required.", "Route challenges to human handoff.");
        yield return Requirement(ComputerUseSandboxRequirementKind.HumanHandoffPolicy, NeedsHumanHandoff(recipe, preflightReport, draft) ? ComputerUseSandboxRequirementStatus.FutureRequired : ComputerUseSandboxRequirementStatus.SatisfiedForFixture, NeedsHumanHandoff(recipe, preflightReport, draft) ? "Human handoff policy is required before any future supervised runtime." : "Human handoff is not required for this fixture subject.", "Add explicit handoff instructions and evidence refs.");
        yield return Requirement(ComputerUseSandboxRequirementKind.ApprovalPolicy, preflightReport.RequiredApprovals.Count > 0 ? ComputerUseSandboxRequirementStatus.FutureRequired : ComputerUseSandboxRequirementStatus.SatisfiedForFixture, preflightReport.RequiredApprovals.Count > 0 ? "Approval policy is required for high-risk or external side-effect work." : "No approval requirement is pending for fixture review.", "Add approval narrative before future sandbox candidate review.");
        yield return Requirement(ComputerUseSandboxRequirementKind.RedactionPolicy, ComputerUseSandboxRequirementStatus.SatisfiedForFixture, "Redaction remains fixture/reference-only.", "Keep OCR/perception data redacted and reference-only.");
        yield return Requirement(ComputerUseSandboxRequirementKind.AuditLogPolicy, evalFinalDecision == ReliableRecipeFixtureEvalFinalDecision.RegressionDetected ? ComputerUseSandboxRequirementStatus.Blocked : ComputerUseSandboxRequirementStatus.SatisfiedForFixture, evalFinalDecision == ReliableRecipeFixtureEvalFinalDecision.RegressionDetected ? "Eval regression requires audit review before readiness." : "Fixture report is deterministic and auditable.", "Investigate unexpected pass regressions before future runtime work.");
    }

    private static ComputerUseSandboxRequirementReport Requirement(
        ComputerUseSandboxRequirementKind kind,
        ComputerUseSandboxRequirementStatus status,
        string message,
        string fix) =>
        new(
            kind,
            status,
            status is ComputerUseSandboxRequirementStatus.Blocked ? ReliableRecipeQualitySeverity.Blocking :
            status is ComputerUseSandboxRequirementStatus.Missing or ComputerUseSandboxRequirementStatus.FutureRequired ? ReliableRecipeQualitySeverity.Warning :
            ReliableRecipeQualitySeverity.Info,
            message,
            fix);

    private static ComputerUseSandboxRequirementStatus SurfaceIsolationStatus(ReliableRecipeDefinition recipe, ReliableRecipePreflightReport preflightReport)
    {
        if (recipe.Blocks.Any(b => b.Kind == ReliableRecipeBlockKind.DesktopFuture) || preflightReport.QualityReport.SandboxReadiness.BlockedCapabilities.Contains("desktop-live-surface"))
            return ComputerUseSandboxRequirementStatus.Blocked;
        if (recipe.Blocks.Any(b => b.Kind == ReliableRecipeBlockKind.BrowserAction))
            return ComputerUseSandboxRequirementStatus.FutureRequired;
        return ComputerUseSandboxRequirementStatus.SatisfiedForFixture;
    }

    private static string SurfaceIsolationMessage(ReliableRecipeDefinition recipe, ReliableRecipePreflightReport preflightReport)
    {
        if (recipe.Blocks.Any(b => b.Kind == ReliableRecipeBlockKind.DesktopFuture) || preflightReport.QualityReport.SandboxReadiness.BlockedCapabilities.Contains("desktop-live-surface"))
            return "Desktop live is blocked; future desktop profile isolation is not enabled.";
        if (recipe.Blocks.Any(b => b.Kind == ReliableRecipeBlockKind.BrowserAction))
            return "Future browser profile isolation would be required; browser live is not enabled.";
        return "No live browser or desktop surface is needed for fixture review.";
    }

    private static IEnumerable<string> RiskReasons(
        ReliableRecipeDefinition recipe,
        ReliableRecipePreflightReport preflightReport,
        ReliableRecipeFixtureEvalFinalDecision? evalFinalDecision,
        RecorderToRecipeDraft? draft)
    {
        foreach (var reason in preflightReport.QualityReport.RiskPosture.BlockedReasons)
            yield return reason;
        if (preflightReport.PolicyDecision == ReliableRecipePolicyDecision.Reject)
            yield return "preflight-blocked";
        if (evalFinalDecision == ReliableRecipeFixtureEvalFinalDecision.RegressionDetected)
            yield return "unexpected-pass-regression";
        if (preflightReport.QualityReport.TargetResolutionQuality is { ResolutionMode: ReliableActionResolutionMode.OcrRegion, SensitiveActionRisk: true })
            yield return "ocr-only-sensitive-action";
        if (draft?.ReviewState is RecorderDraftReviewState.BlockedChallenge)
            yield return "challenge-human-handoff-required";
        if (recipe.RiskProfile.HasFlag(ReliableRecipeRiskProfile.Financial) || recipe.RiskProfile.HasFlag(ReliableRecipeRiskProfile.Irreversible))
            yield return "high-risk-policy-block";
    }

    private static ComputerUseRequiredIsolationMode RequiredIsolation(
        ReliableRecipeDefinition recipe,
        ReliableRecipePreflightReport preflightReport,
        ReliableRecipeFixtureEvalFinalDecision? evalFinalDecision,
        RecorderToRecipeDraft? draft)
    {
        if (evalFinalDecision == ReliableRecipeFixtureEvalFinalDecision.RegressionDetected)
            return ComputerUseRequiredIsolationMode.NotAllowed;
        if (recipe.Blocks.Any(b => b.Kind == ReliableRecipeBlockKind.DesktopFuture) || preflightReport.QualityReport.SandboxReadiness.BlockedCapabilities.Contains("desktop-live-surface"))
            return ComputerUseRequiredIsolationMode.DesktopProfileFuture;
        if (recipe.RiskProfile.HasFlag(ReliableRecipeRiskProfile.Financial) || recipe.RiskProfile.HasFlag(ReliableRecipeRiskProfile.Irreversible))
            return ComputerUseRequiredIsolationMode.NotAllowed;
        if (draft?.ReviewState is RecorderDraftReviewState.BlockedChallenge or RecorderDraftReviewState.BlockedSensitiveInput)
            return ComputerUseRequiredIsolationMode.NotAllowed;
        if (recipe.Blocks.Any(b => b.Kind == ReliableRecipeBlockKind.BrowserAction))
            return ComputerUseRequiredIsolationMode.BrowserProfileFuture;
        return ComputerUseRequiredIsolationMode.NoneForFixture;
    }

    private static ComputerUseSandboxReadinessDecision DecisionFor(
        ReliableRecipePreflightReport preflightReport,
        ReliableRecipeFixtureEvalFinalDecision? evalFinalDecision,
        RecorderToRecipeDraft? draft,
        IReadOnlyList<ComputerUseSandboxRequirementKind> missing,
        IReadOnlyList<string> riskReasons,
        ComputerUseRequiredIsolationMode requiredIsolation)
    {
        if (evalFinalDecision == ReliableRecipeFixtureEvalFinalDecision.RegressionDetected)
            return ComputerUseSandboxReadinessDecision.BlockedByPolicy;
        if (requiredIsolation == ComputerUseRequiredIsolationMode.DesktopProfileFuture)
            return ComputerUseSandboxReadinessDecision.BlockedLiveRuntimeNotAllowed;
        if (riskReasons.Any(r => r.Contains("government", StringComparison.OrdinalIgnoreCase) || r.Contains("fiscal", StringComparison.OrdinalIgnoreCase) || r.Contains("high-risk", StringComparison.OrdinalIgnoreCase) || r.Contains("financial", StringComparison.OrdinalIgnoreCase) || r.Contains("irreversible", StringComparison.OrdinalIgnoreCase)))
            return ComputerUseSandboxReadinessDecision.BlockedByPolicy;
        if (draft?.ReviewState is RecorderDraftReviewState.BlockedChallenge or RecorderDraftReviewState.BlockedSensitiveInput)
            return ComputerUseSandboxReadinessDecision.BlockedMissingRequirements;
        if (requiredIsolation == ComputerUseRequiredIsolationMode.NotAllowed || riskReasons.Any(r => r.Contains("risk", StringComparison.OrdinalIgnoreCase) || r.Contains("ocr-only", StringComparison.OrdinalIgnoreCase)))
            return ComputerUseSandboxReadinessDecision.BlockedByPolicy;
        if (preflightReport.PolicyDecision == ReliableRecipePolicyDecision.Reject && missing.Count > 0)
            return ComputerUseSandboxReadinessDecision.DesignOnlyNeedsReview;
        if (missing.Any(m => m is ComputerUseSandboxRequirementKind.ValidationPolicy or ComputerUseSandboxRequirementKind.EvidencePolicy))
            return ComputerUseSandboxReadinessDecision.DesignOnlyNeedsReview;
        return requiredIsolation == ComputerUseRequiredIsolationMode.BrowserProfileFuture
            ? ComputerUseSandboxReadinessDecision.FixtureReadyWithWarnings
            : ComputerUseSandboxReadinessDecision.FixtureReady;
    }

    private static double ScoreFor(ComputerUseSandboxReadinessDecision decision, int missing, int riskReasons, double sandboxScore)
    {
        var score = decision switch
        {
            ComputerUseSandboxReadinessDecision.FixtureReady => Math.Max(0.82, sandboxScore),
            ComputerUseSandboxReadinessDecision.FixtureReadyWithWarnings => 0.72,
            ComputerUseSandboxReadinessDecision.DesignOnlyNeedsReview => 0.55,
            ComputerUseSandboxReadinessDecision.BlockedMissingRequirements => 0.38,
            ComputerUseSandboxReadinessDecision.BlockedByPolicy => 0.25,
            ComputerUseSandboxReadinessDecision.BlockedLiveRuntimeNotAllowed => 0.2,
            _ => 0.1
        };

        return Math.Round(Math.Clamp(score - (missing * 0.02) - (riskReasons * 0.01), 0, 1), 2);
    }

    private static ComputerUseAllowedAssessmentMode AssessmentModeFor(ComputerUseSandboxReadinessDecision decision) =>
        decision switch
        {
            ComputerUseSandboxReadinessDecision.FixtureReady => ComputerUseAllowedAssessmentMode.FixtureEvaluationOnly,
            ComputerUseSandboxReadinessDecision.FixtureReadyWithWarnings => ComputerUseAllowedAssessmentMode.FutureSandboxCandidate,
            ComputerUseSandboxReadinessDecision.DesignOnlyNeedsReview => ComputerUseAllowedAssessmentMode.DesignOnlySandboxPlan,
            _ => ComputerUseAllowedAssessmentMode.NoRuntimeAllowed
        };

    private static IEnumerable<ComputerUseSurfaceReadiness> SurfaceReadiness(
        ReliableRecipeDefinition recipe,
        ComputerUseSandboxReadinessDecision decision,
        ComputerUseRequiredIsolationMode requiredIsolation,
        IReadOnlyList<ComputerUseSandboxRequirementKind> missing,
        IReadOnlyList<string> riskReasons)
    {
        yield return new ComputerUseSurfaceReadiness(
            ComputerUseSurfaceKind.Browser,
            requiredIsolation == ComputerUseRequiredIsolationMode.BrowserProfileFuture ? ComputerUseSandboxReadinessDecision.FixtureReadyWithWarnings : decision,
            requiredIsolation == ComputerUseRequiredIsolationMode.BrowserProfileFuture ? 0.72 : decision == ComputerUseSandboxReadinessDecision.FixtureReady ? 0.9 : 0.4,
            requiredIsolation == ComputerUseRequiredIsolationMode.BrowserProfileFuture ? ["Browser live blocked; future browser profile required."] : ["Browser live blocked by M6 boundary."],
            requiredIsolation == ComputerUseRequiredIsolationMode.BrowserProfileFuture ? [ComputerUseSandboxRequirementKind.SurfaceIsolation] : []);

        yield return new ComputerUseSurfaceReadiness(
            ComputerUseSurfaceKind.Desktop,
            recipe.Blocks.Any(b => b.Kind == ReliableRecipeBlockKind.DesktopFuture) ? ComputerUseSandboxReadinessDecision.BlockedLiveRuntimeNotAllowed : ComputerUseSandboxReadinessDecision.DesignOnlyNeedsReview,
            recipe.Blocks.Any(b => b.Kind == ReliableRecipeBlockKind.DesktopFuture) ? 0.2 : 0.5,
            ["Desktop live blocked. Runtime not enabled."],
            recipe.Blocks.Any(b => b.Kind == ReliableRecipeBlockKind.DesktopFuture) ? [ComputerUseSandboxRequirementKind.SurfaceIsolation] : []);

        yield return new ComputerUseSurfaceReadiness(
            ComputerUseSurfaceKind.NetworkFuture,
            ComputerUseSandboxReadinessDecision.BlockedLiveRuntimeNotAllowed,
            0.2,
            ["Network calls blocked in readiness reports."],
            [ComputerUseSandboxRequirementKind.NetworkPolicy]);

        yield return new ComputerUseSurfaceReadiness(
            ComputerUseSurfaceKind.FilesystemFuture,
            ComputerUseSandboxReadinessDecision.DesignOnlyNeedsReview,
            0.5,
            riskReasons.Contains("unexpected-pass-regression") ? ["Filesystem write remains blocked; eval regression requires review."] : ["Filesystem writes blocked; fixture refs only."],
            missing.Contains(ComputerUseSandboxRequirementKind.FilesystemPolicy) ? [ComputerUseSandboxRequirementKind.FilesystemPolicy] : []);
    }

    private static IReadOnlyList<string> FutureUnlockConditions(
        ComputerUseSandboxReadinessDecision decision,
        IReadOnlyList<ComputerUseSandboxRequirementKind> missing,
        IReadOnlyList<string> riskReasons,
        ComputerUseRequiredIsolationMode requiredIsolation,
        ReliableRecipeSandboxPerceptionSummary? perceptionSummary = null)
    {
        var conditions = new List<string>
        {
            "Keep M6 as read-only report; runtime not enabled.",
            "Complete validation, evidence, redaction, approval and handoff policies before future supervised runtime review."
        };

        if (requiredIsolation == ComputerUseRequiredIsolationMode.BrowserProfileFuture)
            conditions.Add("Future browser profile isolation is required; browser live remains blocked.");
        if (requiredIsolation == ComputerUseRequiredIsolationMode.DesktopProfileFuture)
            conditions.Add("Future desktop profile isolation is required; desktop live remains blocked.");
        if (requiredIsolation == ComputerUseRequiredIsolationMode.NotAllowed)
            conditions.Add("Policy block must be resolved or kept permanently blocked before sandbox candidate review.");
        if (perceptionSummary is not null)
        {
            conditions.AddRange(perceptionSummary.MissingSignals.Select(s => $"Add fixture perception signal before future sandbox review: {s}."));
            conditions.AddRange(perceptionSummary.BlockedReasons.Select(r => $"Resolve perception blocker before future sandbox review: {r}."));
        }
        conditions.AddRange(missing.Select(m => $"Resolve missing requirement: {m}."));
        conditions.AddRange(riskReasons.Select(r => $"Review risk reason: {r}."));
        return conditions.Distinct().ToArray();
    }

    private static bool IsChallenge(ReliableRecipeDefinition recipe, ReliableRecipePreflightReport preflightReport, RecorderToRecipeDraft? draft) =>
        draft?.ReviewState == RecorderDraftReviewState.BlockedChallenge ||
        preflightReport.RequiredHumanInterventions.Any(r => r is ReliableHumanInterventionReason.CaptchaDetected or ReliableHumanInterventionReason.TwoFactorRequired) ||
        recipe.Id.Contains("captcha", StringComparison.OrdinalIgnoreCase) ||
        recipe.Id.Contains("two-factor", StringComparison.OrdinalIgnoreCase);

    private static bool NeedsHumanHandoff(ReliableRecipeDefinition recipe, ReliableRecipePreflightReport preflightReport, RecorderToRecipeDraft? draft) =>
        draft is not null && draft.HumanInterventionRequirements.Count > 0 ||
        preflightReport.RequiredHumanInterventions.Count > 0 ||
        recipe.RiskProfile.HasFlag(ReliableRecipeRiskProfile.Credentialed);

    private static ComputerUseSandboxSubjectKind SubjectKindFor(ReliableRecipeFixtureEvalScenario scenario) =>
        scenario.SourceKind switch
        {
            ReliableRecipeFixtureEvalSourceKind.RecorderDraft => ComputerUseSandboxSubjectKind.RecorderDraft,
            ReliableRecipeFixtureEvalSourceKind.PolicyRegressionFixture => ComputerUseSandboxSubjectKind.PolicyRegressionFixture,
            _ => ComputerUseSandboxSubjectKind.EvalScenario
        };

    private static ComputerUseSandboxReadinessSource Resolve(ComputerUseSandboxReadinessScenario scenario) =>
        scenario.SubjectKind switch
        {
            ComputerUseSandboxSubjectKind.RecorderDraft => Resolve(ReliableRecipeEvalScenarioCatalog.Get(scenario.SubjectId)),
            ComputerUseSandboxSubjectKind.EvalScenario or ComputerUseSandboxSubjectKind.PolicyRegressionFixture => Resolve(ReliableRecipeEvalScenarioCatalog.Get(scenario.SubjectId)),
            _ when scenario.SubjectId == "browser_future_profile_required" => Resolve(ReliableRecipeEvalScenarioCatalog.Get("safe_invoice_download_dry_run_candidate_eval")),
            _ => new ComputerUseSandboxReadinessSource(
                ReliableRecipeLabFixtureCatalog.Get(scenario.SubjectId).Recipe,
                ReliableRecipeLabFixtureCatalog.Get(scenario.SubjectId).PreflightReport,
                null,
                null)
        };

    private static ComputerUseSandboxReadinessSource Resolve(ReliableRecipeFixtureEvalScenario scenario) =>
        Resolve(scenario, ReliableRecipeFixtureEvalRunner.Evaluate(scenario));

    private static ComputerUseSandboxReadinessSource Resolve(ReliableRecipeFixtureEvalScenario scenario, ReliableRecipeFixtureEvalRun? run)
    {
        return scenario.SourceKind switch
        {
            ReliableRecipeFixtureEvalSourceKind.RecorderDraft when scenario.FixtureId == "invoice_download_validated_demonstration" => Source(ReliableRecipeRecorderFixtureCatalog.InvoiceDownloadWithValidation(), run),
            ReliableRecipeFixtureEvalSourceKind.RecorderDraft => Source(ReliableRecipeRecorderFixtureCatalog.Get(scenario.FixtureId).Draft, run),
            ReliableRecipeFixtureEvalSourceKind.PolicyRegressionFixture => Source(ReliableRecipeLabFixtureCatalog.Get("safe_invoice_download_quality_pass"), run),
            _ => Source(ReliableRecipeLabFixtureCatalog.Get(scenario.FixtureId), run)
        };
    }

    private static ComputerUseSandboxReadinessSource Source(RecorderToRecipeDraft draft, ReliableRecipeFixtureEvalRun? run) =>
        new(draft.Recipe, draft.PreflightReport, run, draft);

    private static ComputerUseSandboxReadinessSource Source(ReliableRecipeLabFixture fixture, ReliableRecipeFixtureEvalRun? run) =>
        new(fixture.Recipe, fixture.PreflightReport, run, null);

    private sealed record ComputerUseSandboxReadinessSource(
        ReliableRecipeDefinition Recipe,
        ReliableRecipePreflightReport PreflightReport,
        ReliableRecipeFixtureEvalRun? EvalRun,
        RecorderToRecipeDraft? Draft);
}

public static class ComputerUseSandboxReadinessScenarioCatalog
{
    public static IReadOnlyList<ComputerUseSandboxReadinessScenario> All() =>
    [
        Scenario("safe_invoice_fixture_ready", "safe_invoice_download_quality_pass", ComputerUseSandboxSubjectKind.ReliableRecipe, ComputerUseSandboxReadinessDecision.FixtureReady, "Safe invoice fixture can be evaluated in fixture-only mode."),
        Scenario("invoice_missing_validation_needs_review", "invoice_download_missing_validation_eval", ComputerUseSandboxSubjectKind.RecorderDraft, ComputerUseSandboxReadinessDecision.DesignOnlyNeedsReview, "Invoice recorder draft is missing validation and remains design-only."),
        Scenario("ocr_only_sensitive_blocked", "ocr_only_sensitive_submit_blocked_eval", ComputerUseSandboxSubjectKind.RecorderDraft, ComputerUseSandboxReadinessDecision.BlockedByPolicy, "OCR-only sensitive target is blocked for action authority."),
        Scenario("password_credential_policy_blocked", "recorder_password_redaction_eval", ComputerUseSandboxSubjectKind.RecorderDraft, ComputerUseSandboxReadinessDecision.BlockedMissingRequirements, "Credential fixture requires secret-by-reference and human policy."),
        Scenario("captcha_two_factor_handoff_blocked", "captcha_two_factor_handoff_eval", ComputerUseSandboxSubjectKind.RecorderDraft, ComputerUseSandboxReadinessDecision.BlockedMissingRequirements, "Challenge fixture requires human handoff and no bypass."),
        Scenario("ambiguous_target_needs_review", "ambiguous_target_needs_review_eval", ComputerUseSandboxSubjectKind.RecorderDraft, ComputerUseSandboxReadinessDecision.DesignOnlyNeedsReview, "Ambiguous target needs review."),
        Scenario("government_submit_policy_blocked", "government_submit_high_risk_blocked_eval", ComputerUseSandboxSubjectKind.RecorderDraft, ComputerUseSandboxReadinessDecision.BlockedByPolicy, "Government submit is blocked by policy."),
        Scenario("desktop_future_live_blocked", "desktop_future_sandbox_blocked_eval", ComputerUseSandboxSubjectKind.RecorderDraft, ComputerUseSandboxReadinessDecision.BlockedLiveRuntimeNotAllowed, "Desktop future surface is blocked."),
        Scenario("browser_future_profile_required", "browser_future_profile_required", ComputerUseSandboxSubjectKind.ReliableRecipe, ComputerUseSandboxReadinessDecision.FixtureReadyWithWarnings, "Browser profile isolation is a future requirement, not enabled."),
        Scenario("high_quality_high_risk_still_blocked", "high_quality_high_risk_still_blocked_eval", ComputerUseSandboxSubjectKind.EvalScenario, ComputerUseSandboxReadinessDecision.BlockedByPolicy, "High target confidence cannot override high risk."),
        Scenario("unexpected_pass_regression_blocked", "unexpected_pass_regression_fixture", ComputerUseSandboxSubjectKind.PolicyRegressionFixture, ComputerUseSandboxReadinessDecision.BlockedByPolicy, "Unexpected pass regression blocks sandbox readiness.")
    ];

    public static ComputerUseSandboxReadinessScenario Get(string scenarioId) =>
        All().Single(s => s.ScenarioId == scenarioId);

    private static ComputerUseSandboxReadinessScenario Scenario(
        string scenarioId,
        string subjectId,
        ComputerUseSandboxSubjectKind subjectKind,
        ComputerUseSandboxReadinessDecision expected,
        string summary) =>
        new(scenarioId, subjectId, subjectKind, expected, summary);
}

public static class ComputerUseSandboxReadinessReportMapper
{
    public static ReliableRecipeLabSandboxReadinessPanel ToLabPanel(ComputerUseSandboxReadinessReport report) =>
        new(
            report.OverallDecision.ToString(),
            report.ReadinessScore,
            report.AllowedAssessmentMode.ToString(),
            report.RequiredIsolationMode.ToString(),
            report.BlockedCapabilities.Select(c => c.ToString()).ToArray(),
            report.MissingRequirements.Select(r => r.ToString()).ToArray(),
            report.FutureUnlockConditions,
            report.FixtureOnlyNotice,
            ["Review sandbox readiness", "Open report", "Copy summary"]);

    public static ReliableRecipeEvalSandboxReadinessSummary ToEvalSummary(ComputerUseSandboxReadinessReport report) =>
        new(
            report.ReportId,
            report.OverallDecision.ToString(),
            report.ReadinessScore,
            report.BlockedCapabilities.Select(c => c.ToString()).ToArray(),
            report.MissingRequirements.Select(r => r.ToString()).ToArray(),
            report.FutureUnlockConditions,
            report.FixtureOnlyNotice);
}
