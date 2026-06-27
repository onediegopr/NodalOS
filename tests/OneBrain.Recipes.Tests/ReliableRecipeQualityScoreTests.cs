using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ReliableRecipeQualityScore")]
public sealed class ReliableRecipeQualityScoreTests
{
    [TestMethod]
    public void CompleteDryRunRecipeQualityHighAndPreflightDryRunAllowed()
    {
        var report = ReliableRecipePreflightComposer.Compose(CompleteDryRunReadyRecipe(), ReliableRecipeRunMode.DryRun, context: GoodContext());

        Assert.AreEqual(ReliableRecipeRunMode.DryRun, report.ModeAllowed);
        Assert.IsTrue(report.CanProceedToDryRun);
        Assert.IsTrue(report.QualityReport.OverallScore >= 0.72);
        Assert.AreEqual(ReliableRecipeQualityDecision.PassDryRun, report.QualityReport.Decision);
    }

    [TestMethod]
    public void MissingLimitsBlocksQuality()
    {
        var report = Score(CompleteDryRunReadyRecipe() with { Limits = null });

        AssertBlocked(report, "missing-limits");
    }

    [TestMethod]
    public void MissingValidationBlocksQuality()
    {
        var recipe = CompleteDryRunReadyRecipe() with
        {
            Blocks = [CriticalAction("submit", evidence: FullCriticalEvidence(), validation: [])]
        };

        var report = Score(recipe);

        AssertBlocked(report, "missing-validation");
    }

    [TestMethod]
    public void MissingEvidenceBlocksQuality()
    {
        var recipe = CompleteDryRunReadyRecipe() with
        {
            Blocks = [CriticalAction("submit", evidence: ["evidence.before"], validation: ["validation.visible", "validation.timeline"])]
        };

        var report = Score(recipe);

        AssertBlocked(report, "missing-evidence");
    }

    [TestMethod]
    public void SubmitWithoutValidationBlocks()
    {
        var recipe = CompleteDryRunReadyRecipe() with
        {
            Blocks = [CriticalAction("submit", evidence: FullCriticalEvidence(), validation: [])]
        };

        var preflight = ReliableRecipePreflightComposer.Compose(recipe, ReliableRecipeRunMode.DryRun, context: GoodContext());

        Assert.AreEqual(ReliableRecipeRunMode.DraftOnly, preflight.ModeAllowed);
        Assert.IsFalse(preflight.CanProceedToDryRun);
    }

    [TestMethod]
    public void DownloadWithoutArtifactEvidenceBlocks()
    {
        var recipe = CompleteDryRunReadyRecipe() with
        {
            Blocks =
            [
                Block("download", ReliableRecipeBlockKind.FileDownloadEvidence, ReliableRecipeRiskProfile.LocalWrite, FullCriticalEvidence().Where(e => !e.Contains("download", StringComparison.Ordinal)).ToArray(), ["validation.download", "validation.timeline"])
            ]
        };

        var report = Score(recipe);

        AssertBlocked(report, "missing-evidence-DownloadArtifact");
    }

    [TestMethod]
    public void LoopWithoutTerminateCriteriaBlocks()
    {
        var recipe = CompleteDryRunReadyRecipe() with
        {
            Blocks = [Block("loop", ReliableRecipeBlockKind.Loop, ReliableRecipeRiskProfile.ReadOnly, [], ["validation.manual"])],
            Limits = ValidLimits() with { TerminateCriteria = new ReliableTerminateCriteria([]) }
        };

        var report = Score(recipe);

        AssertBlocked(report, "missing-terminate-criteria");
    }

    [TestMethod]
    public void OcrOnlySensitiveTargetBlocks()
    {
        var context = GoodContext() with
        {
            TargetDescriptor = Target(ReliableActionResolutionMode.OcrRegion, [ReliableActionResolutionMode.OcrRegion], score: 0.9),
            PerceptionSnapshot = OcrOnlySnapshot("Pay now", sensitive: true)
        };

        var report = Score(HighRiskButHighQualityTargetRecipe(), context);

        Assert.AreEqual(TargetResolutionQualityDecision.Blocked, report.TargetResolutionQuality.Decision);
        AssertBlocked(report, "target-resolution-blocked");
    }

    [TestMethod]
    public void OcrOnlyReadExtractionAllowedWithWarningLevelScore()
    {
        var context = GoodContext() with
        {
            TargetDescriptor = Target(ReliableActionResolutionMode.OcrRegion, [ReliableActionResolutionMode.OcrRegion], score: 0.9),
            PerceptionSnapshot = OcrOnlySnapshot("Invoice total", sensitive: false)
        };

        var report = Score(ReadOnlyExtractionRecipe(), context);

        Assert.AreEqual(TargetResolutionQualityDecision.NeedsMoreSignals, report.TargetResolutionQuality.Decision);
        Assert.IsTrue(report.TargetResolutionQuality.Score <= 0.68);
        Assert.IsFalse(report.TargetResolutionQuality.SensitiveActionRisk);
    }

    [TestMethod]
    public void DomAccessibilityOcrAgreementHighTargetQuality()
    {
        var report = Score(CompleteDryRunReadyRecipe(), GoodContext());

        Assert.IsTrue(report.TargetResolutionQuality.Score >= 0.85);
        Assert.IsTrue(report.TargetResolutionQuality.SignalAgreement.DomMatchesOcr);
        Assert.AreEqual(TargetResolutionQualityDecision.AcceptForDryRun, report.TargetResolutionQuality.Decision);
    }

    [TestMethod]
    public void AiSemanticFallbackHighRiskBlocked()
    {
        var context = GoodContext() with
        {
            TargetDescriptor = Target(ReliableActionResolutionMode.AiSemanticFallback, [ReliableActionResolutionMode.AiSemanticFallback], score: 0.95)
        };

        var report = Score(HighRiskButHighQualityTargetRecipe(), context);

        Assert.AreEqual(TargetResolutionQualityDecision.Blocked, report.TargetResolutionQuality.Decision);
    }

    [TestMethod]
    public void RecorderDraftCannotExceedDraftOnly()
    {
        var recipe = CompleteDryRunReadyRecipe() with { CreatedFrom = ReliableRecipeCreatedFrom.RecorderDraft };

        var report = ReliableRecipePreflightComposer.Compose(recipe, ReliableRecipeRunMode.DryRun, context: GoodContext());

        Assert.AreEqual(ReliableRecipeRunMode.DraftOnly, report.ModeAllowed);
        AssertBlocked(report.QualityReport, "recorder-draft");
    }

    [TestMethod]
    public void LimitedAutonomyBlocked()
    {
        var report = ReliableRecipePreflightComposer.Compose(CompleteDryRunReadyRecipe(), ReliableRecipeRunMode.LimitedAutonomy, context: GoodContext());

        Assert.AreEqual(ReliableRecipeRunMode.DraftOnly, report.ModeAllowed);
        Assert.IsFalse(report.CanProceedToLimitedAutonomy);
    }

    [TestMethod]
    public void ExistingStricterPolicyWins()
    {
        var existing = new RecipePolicyPreflightResult(
            false,
            RecipeReadinessStatus.BlockedMissingValidation,
            [new RecipeReadinessIssue("existing-block", RecipeReadinessStatus.BlockedMissingValidation, RecipeReadinessIssueSeverity.Blocking, "Existing policy blocks.", "block.1")],
            [],
            LiveRuntimeEnabled: false,
            ActionAuthorityGranted: false);
        var context = GoodContext() with { ExistingRuntimePolicyResult = existing };

        var report = ReliableRecipePreflightComposer.Compose(CompleteDryRunReadyRecipe(), ReliableRecipeRunMode.DryRun, context: context);

        Assert.AreEqual(ReliableRecipeRunMode.DraftOnly, report.ModeAllowed);
        Assert.IsTrue(report.BlockingReasons.Any(f => f.Code.Contains("existing-runtime-policy-existing-block", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void QualityCannotUnblockExistingPolicyRejection()
    {
        var existing = new RecipePolicyPreflightResult(false, RecipeReadinessStatus.BlockedRiskGate, [new("risk", RecipeReadinessStatus.BlockedRiskGate, RecipeReadinessIssueSeverity.Blocking, "risk")], [], false, false);
        var report = ReliableRecipePreflightComposer.Compose(CompleteDryRunReadyRecipe(), ReliableRecipeRunMode.DryRun, context: GoodContext() with { ExistingRuntimePolicyResult = existing });

        Assert.AreEqual(ReliableRecipePolicyDecision.Reject, report.PolicyDecision);
        Assert.AreEqual(ReliableRecipeRunMode.DraftOnly, report.ModeAllowed);
    }

    [TestMethod]
    public void MissingRuntimePolicyStillProducesDeterministicReport()
    {
        var first = ReliableRecipePreflightComposer.Compose(CompleteDryRunReadyRecipe(), ReliableRecipeRunMode.DryRun, context: GoodContext());
        var second = ReliableRecipePreflightComposer.Compose(CompleteDryRunReadyRecipe(), ReliableRecipeRunMode.DryRun, context: GoodContext());

        Assert.AreEqual(first.QualityReport.OverallScore, second.QualityReport.OverallScore);
        Assert.AreEqual(first.ModeAllowed, second.ModeAllowed);
    }

    [TestMethod]
    public void HighRiskCannotBeLoweredByHighConfidence()
    {
        var report = Score(HighRiskButHighQualityTargetRecipe(), GoodContext());

        Assert.IsTrue(report.RiskPosture.BlockedReasons.Count > 0);
        AssertBlocked(report, "risk-");
    }

    [TestMethod]
    public void CaptchaTwoFactorCreatesHandoffQuality()
    {
        var request = ReliableHumanInterventionFactory.ForChallenge(ReliableHumanInterventionReason.TwoFactorRequired, ["evidence.2fa"]);
        var report = Score(ReliableRecipeFixtureFactory.Create("captcha_two_factor_handoff"), GoodContext() with { HumanInterventionRequests = [request] });

        Assert.IsTrue(report.HumanInterventionPlanQuality.Score >= 0.75);
        Assert.IsTrue(report.RiskPosture.BlockedReasons.Contains("challenge-human-handoff"));
    }

    [TestMethod]
    public void GenericHumanInterventionFailsQuality()
    {
        var request = new ReliableHumanInterventionRequest(ReliableHumanInterventionReason.AmbiguousTarget, "blocked", "blocked", "blocked", [], "none", false, []);

        var score = HumanInterventionQualityEvaluator.Score([request]);

        Assert.IsTrue(score.Score < 0.5);
        Assert.IsFalse(score.EvidenceRefsPresent);
        CollectionAssert.Contains(score.AmbiguousReasons.ToList(), ReliableHumanInterventionReason.AmbiguousTarget);
    }

    [TestMethod]
    public void SandboxDesktopLiveBlocked()
    {
        var report = ReliableSandboxQualityEvaluator.Score(new ComputerUseSandboxProfile(ReliableSandboxIsolationMode.VmFuture, [ReliableComputerUseSurface.Desktop], "blocked", "fixture", "secret-refs-only", "rollback", "evidence", 60));

        Assert.IsTrue(report.BlockedCapabilities.Contains("live-sandbox"));
        Assert.IsTrue(report.BlockedCapabilities.Contains("desktop-live-surface"));
        Assert.IsFalse(report.CanEvaluateInFutureSandbox);
    }

    [TestMethod]
    public void NetworkUnrestrictedSandboxBlocked()
    {
        var report = ReliableSandboxQualityEvaluator.Score(new ComputerUseSandboxProfile(ReliableSandboxIsolationMode.DryRunFixture, [ReliableComputerUseSurface.Browser], "unrestricted", "fixture", "secret-refs-only", "rollback", "evidence", 60));

        CollectionAssert.Contains(report.MissingRequirements.ToList(), SandboxRequirementKind.NetworkPolicy);
        Assert.IsTrue(report.Score < 0.9);
    }

    [TestMethod]
    public void ExternalSideEffectRequiresApprovalValidationAndEvidence()
    {
        var report = Score(ExternalSideEffectNeedsApproval());

        Assert.IsTrue(report.RiskPosture.RequiredApprovals.Contains("external-side-effect-approval"));
        AssertBlocked(report, "missing-validation");
        AssertBlocked(report, "missing-evidence");
    }

    [TestMethod]
    public void SecretLikeEvidenceIsRejected()
    {
        var recipe = CompleteDryRunReadyRecipe() with
        {
            Blocks = [CriticalAction("secret", evidence: ["evidence.before", "token.after", "evidence.proposal", "evidence.result", "evidence.validation", "evidence.timeline", "evidence.perception"], validation: ["validation.visible", "validation.timeline"])]
        };

        var evidence = ReliableEvidenceCompletenessEvaluator.Score(recipe);

        Assert.IsFalse(evidence.StepReports.Single().CanComplete);
    }

    [TestMethod]
    public void QualityReportIncludesActionableFindings()
    {
        var report = Score(ExternalSideEffectNeedsApproval());

        Assert.IsTrue(report.BlockingFindings.All(f => !string.IsNullOrWhiteSpace(f.RecommendedFix)));
        Assert.IsTrue(report.RecommendedNextActions.Count > 0);
    }

    [TestMethod]
    public void ScoreDeterministicAcrossRepeatedRuns()
    {
        var first = Score(CompleteDryRunReadyRecipe(), GoodContext());
        var second = Score(CompleteDryRunReadyRecipe(), GoodContext());

        Assert.AreEqual(first.OverallScore, second.OverallScore);
        CollectionAssert.AreEqual(first.CategoryScores.Select(s => s.Score).ToArray(), second.CategoryScores.Select(s => s.Score).ToArray());
    }

    [TestMethod]
    public void NoLiveNoActionInvariants()
    {
        var report = ReliableRecipePreflightComposer.Compose(CompleteDryRunReadyRecipe(), ReliableRecipeRunMode.DryRun, context: GoodContext());

        Assert.IsFalse(report.LiveRuntimeEnabled);
        Assert.IsFalse(report.ActionAuthorityGranted);
        Assert.IsFalse(report.QualityReport.ExecutesRecipe);
        Assert.IsFalse(report.QualityReport.InspectsLiveBrowserOrDesktop);
        Assert.IsFalse(report.QualityReport.UsesAiScoring);
    }

    private static ReliableRecipeQualityReport Score(ReliableRecipeDefinition recipe) => Score(recipe, GoodContext());

    private static ReliableRecipeQualityReport Score(ReliableRecipeDefinition recipe, ReliableRecipeQualityContext context) =>
        ReliableRecipeQualityScorer.Score(recipe, context: context);

    private static ReliableRecipeDefinition CompleteDryRunReadyRecipe() =>
        new(
            "complete_dry_run_ready_recipe",
            "Complete dry run ready recipe",
            "1.0.0",
            "workspace.fixture",
            [],
            [CriticalAction("review", FullCriticalEvidence(), ["validation.visible", "validation.timeline"])],
            ValidLimits(),
            ReliableRecipeRiskProfile.ReadOnly,
            ReliableRecipeReadiness.RunnableDryRun,
            ReliableRecipeCreatedFrom.ManualDesign);

    private static ReliableRecipeDefinition ReadOnlyExtractionRecipe() =>
        CompleteDryRunReadyRecipe() with
        {
            Id = "ocr_only_read_extraction",
            Blocks = [Block("extract", ReliableRecipeBlockKind.Extract, ReliableRecipeRiskProfile.ReadOnly, ["evidence.ocr", "evidence.validation"], ["validation.visible"])]
        };

    private static ReliableRecipeDefinition HighRiskButHighQualityTargetRecipe() =>
        CompleteDryRunReadyRecipe() with
        {
            Id = "high_quality_but_high_risk_blocked",
            RiskProfile = ReliableRecipeRiskProfile.Financial | ReliableRecipeRiskProfile.ExternalSideEffect,
            Blocks = [CriticalAction("pay", FullCriticalEvidence().Concat(["evidence.approval"]).ToArray(), ["validation.visible", "validation.timeline"]) with { Risk = ReliableRecipeRiskProfile.Financial | ReliableRecipeRiskProfile.ExternalSideEffect }]
        };

    private static ReliableRecipeDefinition ExternalSideEffectNeedsApproval() =>
        CompleteDryRunReadyRecipe() with
        {
            Id = "external_side_effect_needs_approval",
            RiskProfile = ReliableRecipeRiskProfile.ExternalSideEffect,
            Blocks = [Block("submit", ReliableRecipeBlockKind.BrowserAction, ReliableRecipeRiskProfile.ExternalSideEffect, ["evidence.before"], [])]
        };

    private static ReliableRecipeRunLimits ValidLimits() =>
        new(
            10,
            1,
            2,
            60,
            ["fixture.local"],
            [ReliableRecipeBlockKind.BrowserAction, ReliableRecipeBlockKind.Extract, ReliableRecipeBlockKind.Validate],
            new ReliableCompleteCriteria([new ReliableValidationCheck("complete", ReliableValidationCheckKind.TimelineEventCreated, "timeline.complete", Passed: true)]),
            new ReliableTerminateCriteria([new ReliableValidationCheck("terminate", ReliableValidationCheckKind.ManualConfirmationRequired, "human.stop", Passed: true)]));

    private static ReliableRecipeBlock CriticalAction(string id, IReadOnlyList<string> evidence, IReadOnlyList<string> validation) =>
        Block(id, ReliableRecipeBlockKind.BrowserAction, ReliableRecipeRiskProfile.ReadOnly, evidence, validation);

    private static ReliableRecipeBlock Block(string id, ReliableRecipeBlockKind kind, ReliableRecipeRiskProfile risk, IReadOnlyList<string> evidence, IReadOnlyList<string> validation) =>
        new(id, kind, id, new Dictionary<string, string>(), [], [], risk, evidence, validation);

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
            ActionResolutionPolicy: new ReliableActionResolutionPolicy([ReliableActionResolutionMode.StableSelector], ReliableActionResolutionMode.StableSelector, [], true, false, 1, 0.8, true, 0.4),
            TargetDescriptor: Target(ReliableActionResolutionMode.StableSelector, [ReliableActionResolutionMode.StableSelector, ReliableActionResolutionMode.AccessibilityTree, ReliableActionResolutionMode.OcrRegion], score: 0.95),
            PerceptionSnapshot: AgreementSnapshot(),
            SandboxProfile: new ComputerUseSandboxProfile(ReliableSandboxIsolationMode.DryRunFixture, [ReliableComputerUseSurface.Browser], "blocked-by-default", "fixture-only", "secret-refs-only", "rollback-fixture-state", "reference-only", 60),
            HumanInterventionRequests: []);

    private static ReliableTargetDescriptor Target(ReliableActionResolutionMode mode, IReadOnlyList<ReliableActionResolutionMode> signals, double score, string? ambiguity = null) =>
        new("Submit invoice", "button", mode == ReliableActionResolutionMode.StableSelector ? "#submit-invoice" : null, new ReliableBoundingBox(1, 2, 3, 4), null, "element.1", "tab.fixture", mode, new ReliableTargetResolutionConfidence(score, signals, ambiguity, ReliableRecipePolicyDecision.AllowDryRunOnly));

    private static ReliablePerceptionStackSnapshot AgreementSnapshot() =>
        new(
            "snapshot.agree",
            ReliablePerceptionSourceSurface.FixtureBrowser,
            [new ReliablePerceptionSignal(ReliablePerceptionSignalKind.DomElement, "dom.1", "Submit invoice", 0.96)],
            [new ReliablePerceptionSignal(ReliablePerceptionSignalKind.AccessibilityNode, "a11y.1", "Submit invoice", 0.94)],
            [new ReliablePerceptionSignal(ReliablePerceptionSignalKind.OcrText, "ocr.1", "Submit invoice", 0.9)],
            [new ReliablePerceptionSignal(ReliablePerceptionSignalKind.VisualBoundingBox, "visual.1", "Submit invoice", 0.82)],
            [],
            RedactionApplied: true,
            new ReliablePerceptionConfidence(0, 0, [], false));

    private static ReliablePerceptionStackSnapshot OcrOnlySnapshot(string text, bool sensitive) =>
        new(
            "snapshot.ocr",
            ReliablePerceptionSourceSurface.FixtureBrowser,
            [],
            [],
            [new ReliablePerceptionSignal(ReliablePerceptionSignalKind.OcrText, "ocr.1", text, 0.9, sensitive)],
            [],
            [],
            RedactionApplied: true,
            new ReliablePerceptionConfidence(0, 0, [], sensitive));

    private static void AssertBlocked(ReliableRecipeQualityReport report, string codePrefix)
    {
        Assert.AreEqual(ReliableRecipeQualityDecision.Blocked, report.Decision);
        Assert.IsTrue(report.BlockingFindings.Any(f => f.Code.StartsWith(codePrefix, StringComparison.Ordinal)), codePrefix);
    }
}
