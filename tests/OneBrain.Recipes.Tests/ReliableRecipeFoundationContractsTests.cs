using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ReliableRecipeFoundation")]
public sealed class ReliableRecipeFoundationContractsTests
{
    [TestMethod]
    public void RecipeWithoutLimitsIsBlocked()
    {
        var recipe = ValidRecipe() with { Limits = null };

        var result = ReliableRecipePreflightValidator.Validate(recipe, ReliableRecipeRunMode.DryRun);

        AssertBlocked(result, "missing-limits");
    }

    [TestMethod]
    public void LoopWithoutMaxIterationsOrTerminateCriteriaIsBlocked()
    {
        var recipe = ValidRecipe() with
        {
            Blocks = [Block("loop", ReliableRecipeBlockKind.Loop, ReliableRecipeRiskProfile.ReadOnly)],
            Limits = ValidLimits() with { MaxLoopIterations = null, TerminateCriteria = new ReliableTerminateCriteria([]) }
        };

        var result = ReliableRecipePreflightValidator.Validate(recipe, ReliableRecipeRunMode.DryRun);

        AssertBlocked(result, "missing-loop-limit");
        AssertBlocked(result, "missing-terminate-criteria");
    }

    [TestMethod]
    public void SensitiveBlockWithoutRiskIsBlocked()
    {
        var recipe = ValidRecipe() with
        {
            Blocks = [Block("submit", ReliableRecipeBlockKind.BrowserAction, ReliableRecipeRiskProfile.None, ["evidence.submit"], ["validation.submit"])]
        };

        var result = ReliableRecipePreflightValidator.Validate(recipe, ReliableRecipeRunMode.DryRun);

        AssertBlocked(result, "sensitive-block-missing-risk");
    }

    [TestMethod]
    public void LimitedAutonomyIsBlockedByDefault()
    {
        var result = ReliableRecipePreflightValidator.Validate(ValidRecipe(), ReliableRecipeRunMode.LimitedAutonomy);

        AssertBlocked(result, "limited-autonomy-blocked");
        Assert.IsFalse(result.LiveRuntimeEnabled);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void SubmitDownloadOrExternalSideEffectWithoutValidationIsBlocked()
    {
        var recipe = ValidRecipe() with
        {
            Blocks =
            [
                Block("download", ReliableRecipeBlockKind.FileDownloadEvidence, ReliableRecipeRiskProfile.LocalWrite, ["evidence.download"], [])
            ]
        };

        var result = ReliableRecipePreflightValidator.Validate(recipe, ReliableRecipeRunMode.DryRun);

        AssertBlocked(result, "side-effect-missing-validation");
    }

    [TestMethod]
    public void RawSecretInVariableOrConfigIsBlocked()
    {
        var recipe = ValidRecipe() with
        {
            Variables = [new ReliableRecipeVariable("password", "raw-password-value", RawValuePresent: true)],
            Blocks =
            [
                Block("extract", ReliableRecipeBlockKind.Extract, ReliableRecipeRiskProfile.ReadOnly) with
                {
                    Config = new Dictionary<string, string> { ["Authorization"] = "Bearer token-value" }
                }
            ]
        };

        var result = ReliableRecipePreflightValidator.Validate(recipe, ReliableRecipeRunMode.DryRun);

        AssertBlocked(result, "raw-secret-variable");
        AssertBlocked(result, "raw-secret-config");
    }

    [TestMethod]
    public void RecorderDraftCannotBecomeRunReady()
    {
        var draft = ValidRecipe() with { CreatedFrom = ReliableRecipeCreatedFrom.RecorderDraft };

        var result = ReliableRecipePreflightValidator.Validate(draft, ReliableRecipeRunMode.DryRun);

        AssertBlocked(result, "recorder-draft-not-run-ready");
    }

    [TestMethod]
    public void DeterministicActionResolutionWinsBeforeOcr()
    {
        var policy = new ReliableActionResolutionPolicy(
            [ReliableActionResolutionMode.StableSelector, ReliableActionResolutionMode.OcrRegion],
            ReliableActionResolutionMode.StableSelector,
            [ReliableActionResolutionMode.OcrRegion],
            FallbackAllowed: true,
            FallbackRequiresApproval: false,
            MaxAttempts: 2,
            MinimumConfidence: 0.75,
            EvidenceRequired: true,
            HumanHandoffThreshold: 0.4);
        var target = Target(ReliableActionResolutionMode.OcrRegion, score: 0.92);

        var decision = ReliableActionResolutionPolicyEvaluator.Decide(policy, target, ReliableRecipeRiskProfile.ReadOnly);

        Assert.AreEqual(ReliableActionResolutionMode.StableSelector, decision.SelectedMode);
        Assert.IsFalse(decision.RequiresHumanHandoff);
    }

    [TestMethod]
    public void OcrFallbackDoesNotPretendKnownTargetWhenNoDeterministicSignalExists()
    {
        var policy = Policy([ReliableActionResolutionMode.OcrRegion]);
        var target = Target(ReliableActionResolutionMode.OcrRegion, score: 0.86);

        var decision = ReliableActionResolutionPolicyEvaluator.Decide(policy, target, ReliableRecipeRiskProfile.ReadOnly);

        Assert.AreEqual(ReliableActionResolutionMode.OcrRegion, decision.SelectedMode);
        Assert.AreEqual(ReliableRecipePolicyDecision.AllowDryRunOnly, decision.PolicyDecision);
    }

    [TestMethod]
    public void AiFallbackIsBlockedForSensitiveAction()
    {
        var decision = ReliableActionResolutionPolicyEvaluator.Decide(
            Policy([ReliableActionResolutionMode.AiSemanticFallback]),
            Target(ReliableActionResolutionMode.AiSemanticFallback, score: 0.95),
            ReliableRecipeRiskProfile.Financial | ReliableRecipeRiskProfile.ExternalSideEffect);

        Assert.AreEqual(ReliableRecipePolicyDecision.NeedsApproval, decision.PolicyDecision);
        Assert.IsTrue(decision.RequiresHumanHandoff);
    }

    [TestMethod]
    public void LowConfidenceTargetEscalatesToHumanHandoff()
    {
        var decision = ReliableActionResolutionPolicyEvaluator.Decide(
            Policy([ReliableActionResolutionMode.VisibleTextApproximate]),
            Target(ReliableActionResolutionMode.VisibleTextApproximate, score: 0.2),
            ReliableRecipeRiskProfile.ReadOnly);

        Assert.AreEqual(ReliableRecipePolicyDecision.NeedsHumanIntervention, decision.PolicyDecision);
    }

    [TestMethod]
    public void AbsoluteCoordinateIsBlockedOutsideLowRiskFixture()
    {
        var target = Target(ReliableActionResolutionMode.VisualBoundingBox, score: 0.88, relativeCoordinates: "x:20,y:30");

        var decision = ReliableActionResolutionPolicyEvaluator.Decide(
            Policy([ReliableActionResolutionMode.VisualBoundingBox]),
            target,
            ReliableRecipeRiskProfile.ExternalSideEffect);

        Assert.AreEqual(ReliableRecipePolicyDecision.NeedsHumanIntervention, decision.PolicyDecision);
    }

    [TestMethod]
    public void ValidationFailedPreventsSuccess()
    {
        var block = new ReliableValidationBlock(
            "validate.submit",
            "confirmation visible",
            [new ReliableValidationCheck("visible", ReliableValidationCheckKind.VisibleTextExists, "text.confirmation", Passed: false)],
            EvidenceRequired: true,
            ReliableValidationFailurePolicy.Block);

        var result = ReliableValidationEvaluator.Evaluate(block);

        Assert.IsFalse(result.Passed);
        Assert.AreEqual("Stop and request human review.", result.NextRecommendedAction);
    }

    [TestMethod]
    public void ValidationWithRawSecretIsRejected()
    {
        var block = new ReliableValidationBlock(
            "validate.secret",
            "field equals redacted value",
            [new ReliableValidationCheck("secret", ReliableValidationCheckKind.FieldValueEquals, "password", RawSecretPresent: true)],
            EvidenceRequired: true,
            ReliableValidationFailurePolicy.Block);

        var result = ReliableValidationEvaluator.Evaluate(block);

        Assert.IsFalse(result.Passed);
        StringAssert.Contains(result.FailureReason!, "raw secret");
    }

    [TestMethod]
    public void StepCompletedRequiresEvidenceRefs()
    {
        var stepEvidence = new ReliableRecipeStepEvidencePack(
            "step.critical",
            "state.before",
            "state.after",
            "download invoice",
            "completed",
            "validation.download",
            null,
            [],
            RedactionApplied: true,
            "timeline.step");

        Assert.AreEqual(0, stepEvidence.EvidenceRefs.Count);
        Assert.IsFalse(stepEvidence.RawEvidenceInline);
        Assert.IsFalse(stepEvidence.SecretsInline);
    }

    [TestMethod]
    public void HumanInterventionForCaptchaAndTwoFactorDoesNotBypass()
    {
        var captcha = ReliableHumanInterventionFactory.ForChallenge(ReliableHumanInterventionReason.CaptchaDetected, ["evidence.challenge"]);
        var twoFactor = ReliableHumanInterventionFactory.ForChallenge(ReliableHumanInterventionReason.TwoFactorRequired, ["evidence.2fa"]);

        Assert.IsFalse(captcha.BypassAttempted);
        Assert.IsFalse(twoFactor.BypassAttempted);
        Assert.IsFalse(captcha.CanContinueAfterUserAction);
        StringAssert.Contains(captcha.WhatUserMustDo, "manually");
    }

    [TestMethod]
    public void RecorderDraftRedactsSensitiveInputAndRequiresHumanReview()
    {
        var trajectory = new ReliableRecorderTrajectory(
            "trajectory.login",
            "workspace.fixture",
            [new ReliableRecordedInteraction(DateTimeOffset.UnixEpoch, "obs.login", ReliableRecordedInputEventKind.Type, Target(ReliableActionResolutionMode.StableSelector), "***", "tab.fixture", false, SensitiveInputDetected: true)],
            "credential field redacted",
            ["username"],
            ReliableRecipeRiskProfile.Credentialed);

        var draft = ReliableRecorderDraftPolicy.CreateDraft(trajectory, ValidRecipe());

        Assert.IsFalse(draft.RunReady);
        Assert.IsTrue(draft.RequiredHumanReview);
        Assert.AreEqual(ReliableRecipeCreatedFrom.RecorderDraft, draft.DraftRecipe.CreatedFrom);
        Assert.IsTrue(draft.Warnings.Any(w => w.Contains("redacted", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void EvalHarnessUsesFixturesOnlyAndFailsLowEvidenceCompleteness()
    {
        var scenario = new ReliableRecipeEvalScenario("eval.low-evidence", "recipe.fixture", "fixture.start", "fixture.done", [], ReliableRecipeRiskProfile.ReadOnly);
        var metrics = new ReliableRecipeEvalMetric(1, 0, 0.9, 0, 0, EvidenceCompletenessScore: 0.2, FlakinessScore: 0);

        var run = ReliableEvalHarness.EvaluateFixture(scenario, ReliableRecipePreflightValidator.Validate(ValidRecipe(), ReliableRecipeRunMode.DryRun), metrics);

        Assert.IsFalse(run.UsesLiveBrowser);
        Assert.IsFalse(run.UsesNetwork);
        Assert.AreEqual(ReliableRecipeEvalResult.BlockedByPolicy, run.Result);
        CollectionAssert.Contains(run.FailureTaxonomy.ToList(), ReliableRecipeEvalFailureKind.ValidationFailed);
    }

    [TestMethod]
    public void SandboxReadinessBlocksDesktopLiveAndUnrestrictedNetwork()
    {
        var profile = new ComputerUseSandboxProfile(
            ReliableSandboxIsolationMode.VmFuture,
            [ReliableComputerUseSurface.Desktop],
            "unrestricted",
            "fixture-only",
            "raw credentials allowed",
            "none",
            "reference-only",
            60);

        var report = ReliableSandboxReadinessEvaluator.Evaluate(profile);

        Assert.IsFalse(report.Ready);
        CollectionAssert.Contains(report.BlockedCapabilities.ToList(), "live-sandbox");
        CollectionAssert.Contains(report.BlockedCapabilities.ToList(), "desktop-live-surface");
    }

    [TestMethod]
    public void DryRunFixtureSandboxCanBeReady()
    {
        var profile = new ComputerUseSandboxProfile(
            ReliableSandboxIsolationMode.DryRunFixture,
            [ReliableComputerUseSurface.Browser],
            "blocked-by-default",
            "fixture-only",
            "secret-refs-only",
            "rollback-fixture-state",
            "reference-only",
            60);

        var report = ReliableSandboxReadinessEvaluator.Evaluate(profile);

        Assert.IsTrue(report.Ready);
        CollectionAssert.Contains(report.AllowedRunModes.ToList(), ReliableRecipeRunMode.DryRun);
    }

    [TestMethod]
    public void PerceptionDomAndOcrAgreementKeepsHighConfidence()
    {
        var snapshot = new ReliablePerceptionStackSnapshot(
            "snapshot.agree",
            ReliablePerceptionSourceSurface.FixtureBrowser,
            [new ReliablePerceptionSignal(ReliablePerceptionSignalKind.DomElement, "dom.1", "Submit invoice", 0.95)],
            [],
            [new ReliablePerceptionSignal(ReliablePerceptionSignalKind.OcrText, "ocr.1", "Submit invoice", 0.9)],
            [],
            [],
            RedactionApplied: true,
            new ReliablePerceptionConfidence(0, 0, [], false));

        var confidence = ReliablePerceptionConfidenceEvaluator.Evaluate(snapshot, ReliableRecipeRiskProfile.ReadOnly);

        Assert.IsTrue(confidence.OverallScore >= 0.9);
        Assert.AreEqual(0, confidence.Contradictions.Count);
    }

    [TestMethod]
    public void OcrOnlySensitiveTargetNeedsReviewConfidence()
    {
        var snapshot = new ReliablePerceptionStackSnapshot(
            "snapshot.ocr-only",
            ReliablePerceptionSourceSurface.FixtureBrowser,
            [],
            [],
            [new ReliablePerceptionSignal(ReliablePerceptionSignalKind.OcrText, "ocr.1", "Pay now", 0.92, SensitiveDataRisk: true)],
            [],
            [],
            RedactionApplied: true,
            new ReliablePerceptionConfidence(0, 0, [], false));

        var confidence = ReliablePerceptionConfidenceEvaluator.Evaluate(snapshot, ReliableRecipeRiskProfile.Financial);

        Assert.IsTrue(confidence.OverallScore <= 0.45);
        Assert.IsTrue(confidence.SensitiveDataRisk);
    }

    [TestMethod]
    public void RequiredFixturesRemainContractOnly()
    {
        var fixtureNames = new[]
        {
            "safe_invoice_download_dry_run",
            "government_form_submit_high_risk",
            "captcha_two_factor_handoff",
            "ocr_only_canvas_low_confidence",
            "desktop_future_sandbox_blocked",
            "ambiguous_button_target",
            "secret_in_visible_state"
        };

        foreach (var name in fixtureNames)
        {
            var fixture = ReliableRecipeFixtureFactory.Create(name);
            Assert.IsFalse(fixture.LiveRuntimeEnabled, name);
            Assert.IsFalse(fixture.BrowserAutomationEnabled, name);
            Assert.IsFalse(fixture.DesktopAutomationEnabled, name);
            Assert.IsFalse(fixture.RecorderEnabled, name);
            Assert.IsFalse(fixture.SandboxEnabled, name);
        }
    }

    private static ReliableRecipeDefinition ValidRecipe() =>
        new(
            "recipe.fixture",
            "Fixture recipe",
            "1.0.0",
            "workspace.fixture",
            [],
            [Block("extract", ReliableRecipeBlockKind.Extract, ReliableRecipeRiskProfile.ReadOnly, ["evidence.extract"], ["validation.extract"])],
            ValidLimits(),
            ReliableRecipeRiskProfile.ReadOnly,
            ReliableRecipeReadiness.RunnableDryRun,
            ReliableRecipeCreatedFrom.ManualDesign);

    private static ReliableRecipeRunLimits ValidLimits() =>
        new(
            MaxSteps: 10,
            MaxRetries: 1,
            MaxLoopIterations: 2,
            MaxRuntimeSeconds: 60,
            AllowedDomains: ["fixture.local"],
            AllowedActions: [ReliableRecipeBlockKind.Extract, ReliableRecipeBlockKind.Validate, ReliableRecipeBlockKind.Loop],
            CompleteCriteria: new ReliableCompleteCriteria([new ReliableValidationCheck("complete", ReliableValidationCheckKind.TimelineEventCreated, "timeline.complete", Passed: true)]),
            TerminateCriteria: new ReliableTerminateCriteria([new ReliableValidationCheck("terminate", ReliableValidationCheckKind.ManualConfirmationRequired, "human.stop", Passed: true)]));

    private static ReliableRecipeBlock Block(
        string id,
        ReliableRecipeBlockKind kind,
        ReliableRecipeRiskProfile risk,
        IReadOnlyList<string>? evidence = null,
        IReadOnlyList<string>? validation = null) =>
        new(id, kind, id, new Dictionary<string, string>(), [], [], risk, evidence ?? [], validation ?? []);

    private static ReliableActionResolutionPolicy Policy(IReadOnlyList<ReliableActionResolutionMode> modes) =>
        new(modes, modes[0], [], FallbackAllowed: true, FallbackRequiresApproval: false, MaxAttempts: 2, MinimumConfidence: 0.75, EvidenceRequired: true, HumanHandoffThreshold: 0.4);

    private static ReliableTargetDescriptor Target(ReliableActionResolutionMode mode, double score = 0.9, string? relativeCoordinates = null) =>
        new(
            "target",
            "button",
            mode is ReliableActionResolutionMode.StableSelector ? "#target" : null,
            new ReliableBoundingBox(10, 10, 20, 20),
            relativeCoordinates,
            "element.1",
            "tab.fixture",
            mode,
            new ReliableTargetResolutionConfidence(score, [mode], null, ReliableRecipePolicyDecision.AllowDryRunOnly));

    private static void AssertBlocked(ReliableRecipePreflightResult result, string issueId)
    {
        Assert.IsFalse(result.IsReady);
        Assert.AreEqual(ReliableRecipeReadiness.BlockedNeedsReview, result.Readiness);
        Assert.IsTrue(result.BlockingIssues.Any(i => i.IssueId == issueId), issueId);
    }
}
