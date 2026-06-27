using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("RecipeLimitsValidationRiskPolicy")]
public sealed class RecipeLimitsValidationRiskPolicyTests
{
    [TestMethod]
    public void RecipeWithActionBlockButNoMaxStepsIsBlocked()
    {
        var recipe = ValidRecipe() with { RunLimits = ValidLimits() with { MaxSteps = null } };
        var result = RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun);

        AssertBlocked(result, RecipeReadinessStatus.BlockedMissingLimits, "missing-max-steps");
    }

    [TestMethod]
    public void RecipeWithLoopBlockButNoLoopLimitIsBlocked()
    {
        var recipe = ValidRecipeWithLoop() with { RunLimits = ValidLimits() with { MaxLoopIterations = null } };
        var result = RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun);

        AssertBlocked(result, RecipeReadinessStatus.BlockedMissingLimits, "missing-loop-limits");
    }

    [TestMethod]
    public void RecipeWithLoopBlockAndNoTerminateCriteriaIsBlocked()
    {
        var recipe = ValidRecipeWithLoop() with { TerminateCriteria = new RecipeTerminateCriteria([]) };
        var result = RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun);

        AssertBlocked(result, RecipeReadinessStatus.BlockedMissingTerminateCriteria, "missing-terminate-criteria");
    }

    [TestMethod]
    public void BrowserActionSubmitLikeBlockWithoutPostValidationIsBlocked()
    {
        var recipe = ValidRecipe() with { ValidationPolicy = new RecipeValidationPolicy([]) };
        var result = RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun);

        AssertBlocked(result, RecipeReadinessStatus.BlockedMissingValidation, "missing-submit-post-validation");
    }

    [TestMethod]
    public void FileDownloadEvidenceBlockWithoutEvidenceFileRefValidationIsBlocked()
    {
        var block = Block("download", RecipeBlockType.FileDownloadEvidence, "download fixture artifact");
        var recipe = ValidRecipe() with
        {
            Blocks = [block],
            CompleteCriteria = new RecipeCompleteCriteria([
                Complete("file", RecipeCompleteCriterionType.FileArtifactRefExists)
            ]),
            ValidationPolicy = new RecipeValidationPolicy([])
        };

        var result = RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun);

        AssertBlocked(result, RecipeReadinessStatus.BlockedMissingValidation, "missing-download-validation");
    }

    [TestMethod]
    public void WorkitemUpdateWithoutWorkitemStatusValidationIsBlocked()
    {
        var block = Block("workitem-update", RecipeBlockType.WorkitemUpdate, "mark item succeeded");
        var recipe = ValidRecipe() with
        {
            Blocks = [block],
            CompleteCriteria = new RecipeCompleteCriteria([
                Complete("workitem", RecipeCompleteCriterionType.WorkitemMarkedSucceeded)
            ]),
            ValidationPolicy = new RecipeValidationPolicy([])
        };

        var result = RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun);

        AssertBlocked(result, RecipeReadinessStatus.BlockedMissingValidation, "missing-workitem-validation");
    }

    [TestMethod]
    public void HighCriticalRiskRecipeRequiresApprovalOrHumanIntervention()
    {
        var recipe = ValidRecipe() with
        {
            RuntimeRiskProfile = ValidRiskProfile() with
            {
                OverallRisk = RecipeRiskLevel.High,
                ApprovalPolicyPresent = false,
                HumanInterventionPathPresent = false
            }
        };

        var result = RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun);

        AssertBlocked(result, RecipeReadinessStatus.BlockedMissingApprovalPolicy, "high-critical-missing-approval");
    }

    [TestMethod]
    public void PaymentFiscalEmailAndPublicPostingCategoriesRequireApproval()
    {
        var categories = new[]
        {
            SensitiveActionCategory.Payment,
            SensitiveActionCategory.FiscalOrLegalSubmission,
            SensitiveActionCategory.EmailOrMessageSend,
            SensitiveActionCategory.PublicPosting
        };

        foreach (var category in categories)
        {
            var recipe = ValidRecipe() with
            {
                RuntimeRiskProfile = ValidRiskProfile() with
                {
                    SensitiveCategories = new HashSet<SensitiveActionCategory> { category },
                    ApprovalPolicyPresent = false,
                    HumanInterventionPathPresent = true
                },
                TerminateCriteria = SensitiveTerminateCriteria()
            };

            var result = RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun);
            AssertBlocked(result, RecipeReadinessStatus.BlockedMissingApprovalPolicy, "category-requires-approval");
        }
    }

    [TestMethod]
    public void CaptchaTwoFactorChallengeBecomesHumanInterventionOrBlockedNeverAutoBypass()
    {
        var categories = new[] { SensitiveActionCategory.CaptchaOrChallenge, SensitiveActionCategory.TwoFactor };

        foreach (var category in categories)
        {
            var recipe = ValidRecipe() with
            {
                RuntimeRiskProfile = ValidRiskProfile() with
                {
                    SensitiveCategories = new HashSet<SensitiveActionCategory> { category },
                    HumanInterventionPathPresent = true
                },
                TerminateCriteria = SensitiveTerminateCriteria()
            };

            var result = RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun);
            AssertBlocked(result, RecipeReadinessStatus.BlockedRiskGate, "challenge-human-required");
            Assert.IsFalse(result.LiveRuntimeEnabled);
            Assert.IsFalse(result.ActionAuthorityGranted);
        }
    }

    [TestMethod]
    public void BrowserLiveActionRemainsBlocked()
    {
        var recipe = SensitiveRecipe(SensitiveActionCategory.BrowserLiveAction);
        var result = RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun);

        AssertBlocked(result, RecipeReadinessStatus.BlockedLiveRuntimeDisabled, "live-action-blocked");
    }

    [TestMethod]
    public void DesktopLiveActionRemainsBlocked()
    {
        var recipe = SensitiveRecipe(SensitiveActionCategory.DesktopLiveAction);
        var result = RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun);

        AssertBlocked(result, RecipeReadinessStatus.BlockedLiveRuntimeDisabled, "live-action-blocked");
    }

    [TestMethod]
    public void AiFallbackBeforeDeterministicStrategiesIsBlocked()
    {
        var recipe = ValidRecipe() with
        {
            ActionResolutionPolicy = new ActionResolutionPolicy([
                new ActionResolutionAttempt(1, ActionResolutionStrategy.AIFallback, EvidenceExpectationRef: "evidence.ai"),
                new ActionResolutionAttempt(2, ActionResolutionStrategy.StableSelector, "selector.fixture")
            ], AiFallbackAllowed: true)
        };

        var result = RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun);

        AssertBlocked(result, RecipeReadinessStatus.BlockedActionResolutionPolicy, "ai-before-deterministic");
    }

    [TestMethod]
    public void SensitiveActionWithAiFallbackAsFirstStrategyIsBlocked()
    {
        var recipe = SensitiveRecipe(SensitiveActionCategory.Payment) with
        {
            RuntimeRiskProfile = ValidRiskProfile() with
            {
                SensitiveCategories = new HashSet<SensitiveActionCategory> { SensitiveActionCategory.Payment },
                ApprovalPolicyPresent = true,
                HumanInterventionPathPresent = true
            },
            ActionResolutionPolicy = new ActionResolutionPolicy([
                new ActionResolutionAttempt(1, ActionResolutionStrategy.AIFallback, EvidenceExpectationRef: "evidence.ai")
            ], AiFallbackAllowed: true, SensitiveActionsAllowAiFallback: false)
        };

        var result = RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun);

        AssertBlocked(result, RecipeReadinessStatus.BlockedActionResolutionPolicy, "sensitive-ai-first");
    }

    [TestMethod]
    public void RelativeCoordinateStrategyCarriesWarning()
    {
        var recipe = ValidRecipe() with
        {
            ActionResolutionPolicy = new ActionResolutionPolicy([
                new ActionResolutionAttempt(1, ActionResolutionStrategy.KnownTarget, "target.fixture"),
                new ActionResolutionAttempt(2, ActionResolutionStrategy.RelativeCoordinate, "relative.fixture")
            ])
        };

        var result = RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun);

        Assert.IsTrue(result.IsReady);
        Assert.IsTrue(result.Warnings.Any(w => w.IssueId == "relative-coordinate-warning"));
        Assert.IsFalse(result.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void SecretHandlingRequiresSecretRefsAndDoesNotExposeSecretValues()
    {
        var recipe = ValidRecipe() with
        {
            RequiredSecretRefs = [],
            RuntimeRiskProfile = ValidRiskProfile() with
            {
                SensitiveCategories = new HashSet<SensitiveActionCategory> { SensitiveActionCategory.SecretHandling },
                SecretRefs = [],
                SecretValuesExposed = false
            },
            TerminateCriteria = SensitiveTerminateCriteria()
        };

        var result = RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun);

        AssertBlocked(result, RecipeReadinessStatus.BlockedMissingSecretReference, "secret-handling-ref-only");
    }

    [TestMethod]
    public void CompleteCriteriaRequiredForOutputProducingRecipes()
    {
        var recipe = ValidRecipe() with { CompleteCriteria = new RecipeCompleteCriteria([]) };
        var result = RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun);

        AssertBlocked(result, RecipeReadinessStatus.BlockedMissingCompleteCriteria, "missing-complete-criteria");
    }

    [TestMethod]
    public void TerminateCriteriaRequiredForExternalSystemSensitiveRecipes()
    {
        var recipe = SensitiveRecipe(SensitiveActionCategory.ExternalSystemMutation) with { TerminateCriteria = new RecipeTerminateCriteria([]) };
        var result = RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun);

        AssertBlocked(result, RecipeReadinessStatus.BlockedMissingTerminateCriteria, "missing-terminate-criteria");
    }

    [TestMethod]
    public void ValidFixtureRecipeBecomesReadyForFixtureRun()
    {
        var result = RecipePolicyPreflightEvaluator.Evaluate(ValidRecipe(), RecipeRunMode.FixtureRun);

        Assert.IsTrue(result.IsReady);
        Assert.AreEqual(RecipeReadinessStatus.ReadyForFixtureRun, result.Status);
        Assert.AreEqual(0, result.BlockingIssues.Count);
        Assert.IsFalse(result.LiveRuntimeEnabled);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void LiveRuntimeModeRemainsBlockedEvenWithFutureEligibilityFlag()
    {
        var recipe = ValidRecipe() with { RunLimits = ValidLimits() with { FutureLiveEligibility = true } };
        var result = RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.LiveRunAllowedFuture);

        AssertBlocked(result, RecipeReadinessStatus.BlockedLiveRuntimeDisabled, "live-runtime-disabled");
    }

    [TestMethod]
    public void NoSchedulerBackgroundWorkerRealBrowserDesktopCdpNetworkWatcherRecorderReplayIsIntroduced()
    {
        var limits = ValidLimits();
        var queue = new WorkitemQueueContract("queue", "fixture", WorkitemRetryPolicy.Default);
        var result = RecipePolicyPreflightEvaluator.Evaluate(ValidRecipe(), RecipeRunMode.FixtureRun);

        Assert.IsFalse(limits.LiveRuntimeAllowed);
        Assert.IsFalse(limits.FutureLiveEligibility);
        Assert.IsFalse(queue.LiveExecutionEnabled);
        Assert.IsFalse(queue.SchedulerEnabled);
        Assert.IsFalse(queue.BackgroundWorkerEnabled);
        Assert.IsTrue(result.IsReady);
        Assert.IsFalse(result.LiveRuntimeEnabled);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    private static void AssertBlocked(RecipePolicyPreflightResult result, RecipeReadinessStatus status, string issueId)
    {
        Assert.IsFalse(result.IsReady);
        Assert.AreEqual(status, result.Status);
        Assert.IsTrue(result.BlockingIssues.Any(i => i.IssueId == issueId), issueId);
        Assert.IsFalse(result.LiveRuntimeEnabled);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    private static RecipeDefinition ValidRecipe() =>
        new("phase-2-valid")
        {
            RecipeId = "recipe.phase2.valid",
            DisplayName = "Phase 2 valid fixture",
            Version = "2.0.0",
            RequiredToolTrustRefs = ["tooltrust.fixture"],
            RequiredSecretRefs = ["secret.fixture.id"],
            OutputSchemaRef = "schema.output.fixture",
            Blocks =
            [
                Block("submit", RecipeBlockType.BrowserAction, "submit fixture form"),
                Block("validate", RecipeBlockType.Validate, "validate output")
            ],
            RunLimits = ValidLimits(),
            CompleteCriteria = ValidCompleteCriteria(),
            TerminateCriteria = ValidTerminateCriteria(),
            ValidationPolicy = ValidValidationPolicy(),
            RuntimeRiskProfile = ValidRiskProfile(),
            ActionResolutionPolicy = ValidActionResolutionPolicy(),
            EvidenceExpectationRefs = ["evidence.expected.fixture"],
            ApprovalCheckpointRefs = ["approval.fixture"]
        };

    private static RecipeDefinition ValidRecipeWithLoop() =>
        ValidRecipe() with
        {
            Blocks =
            [
                Block("loop", RecipeBlockType.Loop, "loop over fixture workitems"),
                Block("submit", RecipeBlockType.BrowserAction, "submit fixture form")
            ]
        };

    private static RecipeDefinition SensitiveRecipe(SensitiveActionCategory category) =>
        ValidRecipe() with
        {
            RuntimeRiskProfile = ValidRiskProfile() with
            {
                OverallRisk = RecipeRiskLevel.High,
                SensitiveCategories = new HashSet<SensitiveActionCategory> { category },
                ApprovalPolicyPresent = true,
                HumanInterventionPathPresent = true
            },
            TerminateCriteria = SensitiveTerminateCriteria()
        };

    private static RecipeBlock Block(string id, RecipeBlockType type, string intent) =>
        new(
            id,
            type,
            id,
            intent,
            TargetRef: "target.fixture",
            InputBinding: "input.fixture",
            OutputBinding: "output.fixture",
            Preconditions: ["pre.fixture"],
            Postconditions: ["post.fixture"],
            ValidationRefs: ["validation.fixture"],
            RecipeRiskLevel.Low,
            RecipeApprovalRequirement.Required,
            EvidenceExpectationRef: "evidence.fixture",
            FailurePolicyRef: "failure.fixture",
            NextBlockRefs: []);

    private static RecipeRunLimits ValidLimits() =>
        new(
            MaxSteps: 25,
            MaxRuntimeSeconds: 120,
            MaxRetries: 2,
            MaxLoopIterations: 5,
            MaxNestedLoops: 1,
            MaxWorkitemsPerRun: 10,
            MaxDownloadedFiles: 1,
            MaxCapturedArtifacts: 2,
            MaxExternalSystemCalls: 0,
            AllowedDomains: ["example.test"],
            AllowedApps: ["fixture-app"],
            AllowedFileScopeRefs: ["file-scope.fixture"],
            AllowedActionCategories: new HashSet<RecipeActionCategory> { RecipeActionCategory.ReadOnlyObservation, RecipeActionCategory.Validate, RecipeActionCategory.Submit },
            BlockedActionCategories: new HashSet<RecipeActionCategory> { RecipeActionCategory.ExternalSystemCall },
            RequireExplicitApprovalForSensitiveActions: true,
            RequireValidationAfterSideEffects: true,
            RequireEvidenceAfterDownloadsCaptures: true,
            LiveRuntimeAllowed: false,
            FutureLiveEligibility: false);

    private static RecipeCompleteCriteria ValidCompleteCriteria() =>
        new([
            Complete("output", RecipeCompleteCriterionType.ExpectedOutputExists),
            Complete("validation", RecipeCompleteCriterionType.ValidationResultPassed),
            Complete("evidence", RecipeCompleteCriterionType.AllRequiredEvidenceRefsPresent)
        ]);

    private static RecipeCompleteCriterion Complete(string id, RecipeCompleteCriterionType type) =>
        new(id, type, $"ref.{id}", UsesRealWorldProbe: false);

    private static RecipeTerminateCriteria ValidTerminateCriteria() =>
        new([
            Terminate("max-steps", RecipeTerminateCriterionType.MaxStepsExceeded),
            Terminate("max-runtime", RecipeTerminateCriterionType.MaxRuntimeExceeded),
            Terminate("policy", RecipeTerminateCriterionType.PolicyBlocked),
            Terminate("loop", RecipeTerminateCriterionType.LoopLimitExceeded),
            Terminate("unknown", RecipeTerminateCriterionType.UnknownUnsafeState)
        ]);

    private static RecipeTerminateCriteria SensitiveTerminateCriteria() =>
        new([
            Terminate("policy", RecipeTerminateCriterionType.PolicyBlocked),
            Terminate("auth", RecipeTerminateCriterionType.AuthChallengeDetected),
            Terminate("human", RecipeTerminateCriterionType.HumanInterventionRequired),
            Terminate("unsafe", RecipeTerminateCriterionType.UnknownUnsafeState)
        ]);

    private static RecipeTerminateCriterion Terminate(string id, RecipeTerminateCriterionType type) =>
        new(id, type, $"ref.{id}", UsesRealTimerOrHook: false);

    private static RecipeValidationPolicy ValidValidationPolicy() =>
        new([
            Requirement("submit-post", RecipeValidationKind.VisibleTextExists, "submit", post: true),
            Requirement("download-ref", RecipeValidationKind.DownloadedFileRefExists, "download", post: true),
            Requirement("download-evidence", RecipeValidationKind.EvidenceRefExists, "download", post: true),
            Requirement("workitem-status", RecipeValidationKind.WorkitemStatusEquals, "workitem-update", post: true),
            Requirement("human-resolution", RecipeValidationKind.ApprovalDecisionExists, "human", post: true),
            Requirement("no-secret", RecipeValidationKind.NoSecretExposure, null, post: true)
        ]);

    private static RecipeValidationRequirement Requirement(string id, RecipeValidationKind kind, string? blockId, bool post) =>
        new(id, kind, RecipeValidationSeverity.Blocking, AppliesToBlockId: blockId, PostValidation: post, RefId: $"validation.{id}");

    private static RecipeRiskProfile ValidRiskProfile() =>
        new(
            "risk.fixture",
            RecipeRiskLevel.Low,
            new HashSet<SensitiveActionCategory>(),
            [],
            ApprovalPolicyPresent: true,
            HumanInterventionPathPresent: true,
            SecretRefs: ["secret.fixture.id"],
            SecretValuesExposed: false);

    private static ActionResolutionPolicy ValidActionResolutionPolicy() =>
        new([
            new ActionResolutionAttempt(1, ActionResolutionStrategy.KnownTarget, "target.known", "evidence.target"),
            new ActionResolutionAttempt(2, ActionResolutionStrategy.StableSelector, "selector.stable", "evidence.selector"),
            new ActionResolutionAttempt(3, ActionResolutionStrategy.HumanHandoff, "handoff.fixture", "evidence.handoff")
        ]);
}
