using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("RecipeRuntimeFoundation")]
public sealed class RecipeRuntimeFoundationWorkitemsTests
{
    [TestMethod]
    public void RecipeDefinitionCanBeCreatedWithBlocksAndMetadata()
    {
        var recipe = CreateValidRecipe();

        Assert.AreEqual("recipe.customer-onboarding", recipe.RecipeId);
        Assert.AreEqual("Customer onboarding preview", recipe.DisplayName);
        Assert.AreEqual("1.0.0", recipe.Version);
        Assert.AreEqual("customer-ops", recipe.Category);
        Assert.AreEqual("mission-control", recipe.SystemTarget);
        CollectionAssert.Contains(recipe.RequiredCapabilities, "evidence-ref");
        CollectionAssert.Contains(recipe.RequiredToolTrustRefs, "tooltrust.local-fixture");
        CollectionAssert.Contains(recipe.RequiredSecretRefs, "secret.crm.api-key");
        CollectionAssert.Contains(recipe.LifecycleStages, RecipeLifecycleStage.Preflight);
        CollectionAssert.Contains(recipe.LifecycleStages, RecipeLifecycleStage.Handoff);
        Assert.AreEqual(2, recipe.Blocks.Count);
    }

    [TestMethod]
    public void RecipeBlocksPreserveTypeRiskValidationEvidenceAndNextReferences()
    {
        var block = CreateActionBlock("b.browser", RecipeBlockType.BrowserAction);

        Assert.AreEqual(RecipeBlockType.BrowserAction, block.BlockType);
        Assert.AreEqual(RecipeRiskLevel.Medium, block.RiskLevel);
        Assert.AreEqual(RecipeApprovalRequirement.RequiredBeforeLive, block.ApprovalRequirement);
        CollectionAssert.Contains(block.ValidationRefs.ToArray(), "validation.visible-result");
        Assert.AreEqual("evidence.expected.browser.redacted", block.EvidenceExpectationRef);
        CollectionAssert.Contains(block.NextBlockRefs.ToArray(), "b.validate");
    }

    [TestMethod]
    public void RecipeRunCannotBeConsideredLiveEnabledByDefault()
    {
        var run = new RecipeRun(
            RunId: "run-1",
            RecipeId: "recipe.customer-onboarding",
            RecipeVersion: "1.0.0",
            MissionIdRef: "mission-123",
            RecipeRunStatus.ReadyForPreview,
            RecipeRunMode.CatalogPreview,
            StartedAt: null,
            CompletedAt: null,
            CurrentBlockId: "b.pop",
            StepCount: 0,
            AttemptCount: 0,
            EvidencePackRef: "evidence-pack-1",
            TimelineRefs: ["timeline-1"],
            ApprovalRefs: ["approval-1"],
            WorkitemRefs: ["workitem-1"],
            FailureSummary: null,
            ReadinessResult: new RecipeReadinessResult(true, RecipeReadinessStatus.ReadyForPreview, [], LiveRuntimeEnabled: false));

        Assert.IsFalse(run.LiveRuntimeEnabled);
        Assert.IsFalse(run.ActionAuthorityGranted);
        Assert.AreEqual(RecipeRunMode.CatalogPreview, run.Mode);
    }

    [TestMethod]
    public void WorkitemBusinessFailureIsNonRetryableByDefault()
    {
        var decision = WorkitemRetryPolicyEvaluator.Decide(
            CreateWorkitem(attemptCount: 0),
            WorkitemFailureType.Business,
            WorkitemRetryPolicy.Default,
            DateTimeOffset.Parse("2026-06-27T10:00:00Z"));

        Assert.AreEqual(WorkitemRetryDecisionKind.DoNotRetry, decision.Decision);
        Assert.AreEqual(WorkitemStatus.FailedBusiness, decision.ResultingStatus);
        Assert.IsNull(decision.NextRunAt);
        Assert.IsFalse(decision.LiveExecutionEnabled);
        Assert.IsFalse(decision.ActionAuthorityGranted);
    }

    [TestMethod]
    public void WorkitemApplicationFailureCanBecomeRetryScheduledWhenPolicyAllows()
    {
        var observedAt = DateTimeOffset.Parse("2026-06-27T10:00:00Z");
        var decision = WorkitemRetryPolicyEvaluator.Decide(
            CreateWorkitem(attemptCount: 1),
            WorkitemFailureType.Application,
            WorkitemRetryPolicy.Default,
            observedAt);

        Assert.AreEqual(WorkitemRetryDecisionKind.RetryScheduled, decision.Decision);
        Assert.AreEqual(WorkitemStatus.RetryScheduled, decision.ResultingStatus);
        Assert.AreEqual(observedAt.AddMinutes(5), decision.NextRunAt);
        Assert.IsFalse(decision.LiveExecutionEnabled);
        Assert.IsFalse(decision.ActionAuthorityGranted);
    }

    [TestMethod]
    public void PolicyAuthAndChallengeFailuresRequireHumanOrBlockedState()
    {
        var failures = new[] { WorkitemFailureType.Policy, WorkitemFailureType.Auth, WorkitemFailureType.Challenge };
        foreach (var failure in failures)
        {
            var decision = WorkitemRetryPolicyEvaluator.Decide(
                CreateWorkitem(attemptCount: 0),
                failure,
                WorkitemRetryPolicy.Default,
                DateTimeOffset.Parse("2026-06-27T10:00:00Z"));

            Assert.AreEqual(WorkitemRetryDecisionKind.NeedsHuman, decision.Decision, failure.ToString());
            Assert.AreEqual(WorkitemStatus.NeedsHuman, decision.ResultingStatus, failure.ToString());
            Assert.IsNull(decision.NextRunAt, failure.ToString());
            Assert.IsFalse(decision.LiveExecutionEnabled, failure.ToString());
            Assert.IsFalse(decision.ActionAuthorityGranted, failure.ToString());
        }
    }

    [TestMethod]
    public void WorkitemPriorityAndNextRunConceptsAreSerializable()
    {
        var item = CreateWorkitem(attemptCount: 1) with
        {
            Priority = 90,
            NextRunAt = DateTimeOffset.Parse("2026-06-27T10:05:00Z"),
            Status = WorkitemStatus.RetryScheduled
        };

        var json = JsonSerializer.Serialize(item, JsonOptions());

        StringAssert.Contains(json, "\"Priority\":90");
        StringAssert.Contains(json, "RetryScheduled");
        StringAssert.Contains(json, "2026-06-27T10:05:00");
        Assert.IsFalse(json.Contains("secret-value", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(item.LiveExecutionEnabled);
        Assert.IsFalse(item.ActionAuthorityGranted);
    }

    [TestMethod]
    public void MissingLimitsReadinessReturnsBlockedStatus()
    {
        var recipe = CreateValidRecipe() with { LimitsRef = null };
        var readiness = RecipeReadinessEvaluator.Evaluate(recipe, RecipeRunMode.DryRun);

        Assert.IsFalse(readiness.IsReady);
        Assert.AreEqual(RecipeReadinessStatus.BlockedMissingLimits, readiness.Status);
        Assert.IsFalse(readiness.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void MissingValidationOnActionBlocksIsDetected()
    {
        var recipe = CreateValidRecipe() with
        {
            Blocks =
            [
                CreateActionBlock("b.action", RecipeBlockType.BrowserAction) with { ValidationRefs = [] }
            ]
        };

        var readiness = RecipeReadinessEvaluator.Evaluate(recipe, RecipeRunMode.DryRun);

        Assert.IsFalse(readiness.IsReady);
        Assert.AreEqual(RecipeReadinessStatus.BlockedMissingValidation, readiness.Status);
        Assert.IsFalse(readiness.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void EvidenceTimelineAndApprovalReferencesAreReferencesOnly()
    {
        var item = CreateWorkitem(attemptCount: 0);
        var step = new RecipeRunStep(
            StepId: "step-1",
            RunId: "run-1",
            BlockId: "b.validate",
            SequenceNumber: 1,
            IntendedAction: "validate redacted output",
            ResolvedTargetSummary: "fixture target",
            StateBeforeRef: "state.before.redacted",
            StateAfterRef: "state.after.redacted",
            RecipeRunStepResult.DryRunPlanned,
            ValidationResultRefs: ["validation.result.redacted"],
            EvidenceRefs: ["evidence.ref.redacted"],
            FailureType: null,
            RetryDecision: new WorkitemRetryDecision(WorkitemRetryDecisionKind.DoNotRetry, WorkitemStatus.Skipped, null, "not needed"),
            ApprovalDecisionRef: "approval.decision.redacted");

        Assert.IsFalse(item.Payload.ContainsRawSecretValue);
        Assert.IsTrue(item.AttachmentRefs.All(a => !a.RawBytesEmbedded));
        Assert.IsFalse(step.LiveRuntimeEnabled);
        Assert.IsFalse(step.ActionAuthorityGranted);
        Assert.IsTrue(step.EvidenceRefs.All(r => r.Contains("ref", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(JsonSerializer.Serialize(step, JsonOptions()).Contains("raw-secret", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void LiveRunModeRemainsBlockedAndNoRealRuntimeCapabilitiesAreIntroduced()
    {
        var readiness = RecipeReadinessEvaluator.Evaluate(CreateValidRecipe(), RecipeRunMode.LiveRunAllowedFuture);
        var queue = new WorkitemQueueContract("queue-1", "Fixture Queue", WorkitemRetryPolicy.Default);

        Assert.IsFalse(readiness.IsReady);
        Assert.AreEqual(RecipeReadinessStatus.BlockedLiveRuntimeDisabled, readiness.Status);
        Assert.IsFalse(readiness.LiveRuntimeEnabled);
        Assert.IsFalse(queue.LiveExecutionEnabled);
        Assert.IsFalse(queue.SchedulerEnabled);
        Assert.IsFalse(queue.BackgroundWorkerEnabled);
    }

    private static RecipeDefinition CreateValidRecipe() =>
        new("customer-onboarding")
        {
            RecipeId = "recipe.customer-onboarding",
            DisplayName = "Customer onboarding preview",
            Description = "Fixture-safe recipe runtime contract sample.",
            Version = "1.0.0",
            Category = "customer-ops",
            SystemTarget = "mission-control",
            RegionCountry = "US",
            RiskProfileRef = "risk.low.fixture",
            RequiredCapabilities = ["evidence-ref", "timeline-ref", "approval-ref"],
            RequiredToolTrustRefs = ["tooltrust.local-fixture"],
            RequiredSecretRefs = ["secret.crm.api-key"],
            InputSchemaRef = "schema.input.customer-onboarding",
            OutputSchemaRef = "schema.output.customer-onboarding",
            LifecycleStages =
            [
                RecipeLifecycleStage.Preflight,
                RecipeLifecycleStage.Prepare,
                RecipeLifecycleStage.Perceive,
                RecipeLifecycleStage.Plan,
                RecipeLifecycleStage.Verify,
                RecipeLifecycleStage.Evidence,
                RecipeLifecycleStage.Cleanup,
                RecipeLifecycleStage.Handoff
            ],
            Blocks =
            [
                new RecipeBlock(
                    "b.pop",
                    RecipeBlockType.WorkitemPop,
                    "Pop workitem",
                    "Pop next fixture workitem by priority and next-run metadata.",
                    TargetRef: "queue.customer-onboarding",
                    InputBinding: "queue.ref",
                    OutputBinding: "workitem.ref",
                    Preconditions: ["queue.contract.exists"],
                    Postconditions: ["workitem.ref.created"],
                    ValidationRefs: [],
                    RecipeRiskLevel.Low,
                    RecipeApprovalRequirement.None,
                    EvidenceExpectationRef: "evidence.expected.queue.metadata",
                    FailurePolicyRef: "failure.policy.default",
                    NextBlockRefs: ["b.validate"]),
                CreateActionBlock("b.validate", RecipeBlockType.Validate)
            ],
            LimitsRef = "limits.recipe.fixture",
            CompleteCriteriaRef = "criteria.complete.fixture",
            TerminateCriteriaRef = "criteria.terminate.fixture",
            ApprovalCheckpointRefs = ["approval.checkpoint.fixture"],
            EvidenceExpectationRefs = ["evidence.expected.recipe.redacted"],
            CreatedBy = "fixture",
            UpdatedBy = "fixture",
            CreatedAt = DateTimeOffset.Parse("2026-06-27T00:00:00Z"),
            UpdatedAt = DateTimeOffset.Parse("2026-06-27T00:00:00Z")
        };

    private static RecipeBlock CreateActionBlock(string blockId, RecipeBlockType type) =>
        new(
            blockId,
            type,
            "Action block",
            "Fixture-safe action draft with validation refs.",
            TargetRef: "target.fixture.redacted",
            InputBinding: "input.fixture.redacted",
            OutputBinding: "output.fixture.redacted",
            Preconditions: ["precondition.fixture"],
            Postconditions: ["postcondition.fixture"],
            ValidationRefs: ["validation.visible-result"],
            RecipeRiskLevel.Medium,
            RecipeApprovalRequirement.RequiredBeforeLive,
            EvidenceExpectationRef: "evidence.expected.browser.redacted",
            FailurePolicyRef: "failure.policy.default",
            NextBlockRefs: ["b.validate"]);

    private static MissionWorkItem CreateWorkitem(int attemptCount) =>
        new(
            ItemId: "workitem-1",
            QueueId: "queue-1",
            QueueName: "Fixture Queue",
            RecipeId: "recipe.customer-onboarding",
            MissionId: "mission-1",
            Payload: new WorkitemPayloadRef("payload.ref.redacted", "{\"customerId\":\"fixture\"}", "schema.payload", ContainsRawSecretValue: false),
            AttachmentRefs:
            [
                new WorkitemAttachmentRef("attachment-1", "redacted-json", "evidence.attachment.ref", RawBytesEmbedded: false)
            ],
            Priority: 50,
            WorkitemStatus.Ready,
            NextRunAt: DateTimeOffset.Parse("2026-06-27T10:00:00Z"),
            AttemptCount: attemptCount,
            MaxAttempts: 3,
            LockedBy: null,
            LockedAt: null,
            CreatedAt: DateTimeOffset.Parse("2026-06-27T09:00:00Z"),
            UpdatedAt: DateTimeOffset.Parse("2026-06-27T09:00:00Z"),
            CompletedAt: null,
            FailureType: null,
            FailureReason: null,
            BusinessKey: "customer-123",
            IdempotencyKey: "idem-customer-123",
            EvidencePackRef: "evidence.pack.ref",
            TimelineEventRefs: ["timeline.event.ref"],
            ParentItemRef: null,
            ChildItemRefs: []);

    private static JsonSerializerOptions JsonOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
