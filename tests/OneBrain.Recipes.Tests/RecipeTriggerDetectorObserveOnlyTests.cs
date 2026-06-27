using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("RecipeTriggerDetectorObserveOnly")]
public sealed class RecipeTriggerDetectorObserveOnlyTests
{
    [TestMethod]
    public void TriggerDefinitionDefaultsToObservationOnlyAndNeverAutorun()
    {
        var trigger = Trigger(RecipeTriggerKind.Manual);
        var detector = Detector(RecipeDetectorKind.ManualSignal);
        var readiness = RecipeTriggerPolicyEvaluator.Evaluate(trigger, detector, Policy());

        Assert.IsTrue(readiness.IsReady);
        Assert.AreEqual(RecipeTriggerStatus.ObserveOnlyReady, readiness.Status);
        AssertNoExecution(readiness.Decision);
        Assert.IsFalse(trigger.CanStartRecipeRun);
        Assert.IsFalse(trigger.CanProcessWorkitems);
        Assert.IsFalse(trigger.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void UnknownTriggerAndDetectorKindsAreBlocked()
    {
        var unknownTrigger = RecipeTriggerPolicyEvaluator.Evaluate(Trigger(RecipeTriggerKind.Unknown), Detector(RecipeDetectorKind.ManualSignal), Policy());
        var unknownDetector = RecipeTriggerPolicyEvaluator.Evaluate(Trigger(RecipeTriggerKind.Manual), Detector(RecipeDetectorKind.Unknown), Policy());

        AssertBlocked(unknownTrigger, RecipeTriggerBlockedReason.UnknownTrigger);
        AssertBlocked(unknownDetector, RecipeTriggerBlockedReason.UnknownDetector);
    }

    [TestMethod]
    public void FutureFileBrowserDesktopHotkeyScheduleWebhookAndConnectorDetectorsAreFutureGated()
    {
        var cases = new[]
        {
            (RecipeTriggerKind.FileCreatedFuture, RecipeDetectorKind.FutureFileWatcher, RecipeTriggerSource.FutureFileSystem),
            (RecipeTriggerKind.FileChangedFuture, RecipeDetectorKind.FutureFileWatcher, RecipeTriggerSource.FutureFileSystem),
            (RecipeTriggerKind.BrowserElementAppearedFuture, RecipeDetectorKind.FutureBrowserObserver, RecipeTriggerSource.FutureBrowser),
            (RecipeTriggerKind.BrowserUrlMatchedFuture, RecipeDetectorKind.FutureBrowserObserver, RecipeTriggerSource.FutureBrowser),
            (RecipeTriggerKind.BrowserDomChangedFuture, RecipeDetectorKind.FutureBrowserObserver, RecipeTriggerSource.FutureBrowser),
            (RecipeTriggerKind.DesktopWindowAppearedFuture, RecipeDetectorKind.FutureDesktopObserver, RecipeTriggerSource.FutureDesktop),
            (RecipeTriggerKind.DesktopElementAppearedFuture, RecipeDetectorKind.FutureDesktopObserver, RecipeTriggerSource.FutureDesktop),
            (RecipeTriggerKind.HotkeyFuture, RecipeDetectorKind.FutureHotkeyObserver, RecipeTriggerSource.FutureHotkey),
            (RecipeTriggerKind.ScheduleFuture, RecipeDetectorKind.FutureScheduleObserver, RecipeTriggerSource.FutureSchedule),
            (RecipeTriggerKind.ExternalWebhookFuture, RecipeDetectorKind.FutureWebhookObserver, RecipeTriggerSource.FutureWebhook),
            (RecipeTriggerKind.ConnectorEventFuture, RecipeDetectorKind.FutureConnectorObserver, RecipeTriggerSource.FutureConnector)
        };

        foreach (var (kind, detectorKind, source) in cases)
        {
            var readiness = RecipeTriggerPolicyEvaluator.Evaluate(
                Trigger(kind, source: source),
                Detector(detectorKind, source),
                Policy());

            AssertBlocked(readiness, RecipeTriggerBlockedReason.FutureDetectorGated, kind.ToString());
            Assert.IsFalse(readiness.Decision.CreatesWatcher, kind.ToString());
            Assert.IsFalse(readiness.Decision.CreatesHook, kind.ToString());
            Assert.IsFalse(readiness.Decision.CreatesScheduler, kind.ToString());
            Assert.IsFalse(readiness.Decision.CreatesListener, kind.ToString());
        }
    }

    [TestMethod]
    public void TriggerObservationDoesNotStartRecipeRunOrProcessWorkitem()
    {
        var trigger = Trigger(RecipeTriggerKind.Manual, recipeId: "recipe.1");
        var observation = RecipeTriggerPolicyEvaluator.CreateObservation(trigger, "observation.1", "Manual fixture signal observed.", ["evidence.trigger"]);

        Assert.AreEqual(RecipeTriggerRunMode.SuggestRecipeOnly, observation.Outcome);
        Assert.AreEqual("recipe-run-draft:recipe.1", observation.SuggestedRecipeRunDraftRef);
        Assert.IsFalse(observation.StartedRecipeRun);
        Assert.IsFalse(observation.ProcessedWorkitem);
        Assert.IsFalse(observation.AdvancedRunStep);
        Assert.IsFalse(observation.LiveRuntimeEnabled);
        Assert.IsFalse(observation.ActionAuthorityGranted);
    }

    [TestMethod]
    public void WorkitemDueTriggerProducesObservationOnly()
    {
        var trigger = Trigger(RecipeTriggerKind.WorkitemDue, source: RecipeTriggerSource.WorkitemQueue, workitemQueueId: "queue.1", workitemId: "workitem.1");
        var readiness = RecipeTriggerPolicyEvaluator.Evaluate(trigger, Detector(RecipeDetectorKind.WorkitemStateSignal, RecipeTriggerSource.WorkitemQueue), Policy());
        var observation = RecipeTriggerPolicyEvaluator.CreateObservation(trigger, "observation.workitem", "Due workitem observed.", ["workitem.state.ref"]);
        var projection = RecipeTriggerTimelineProjector.FromObservation("projection.workitem", observation);

        Assert.IsTrue(readiness.IsReady);
        Assert.AreEqual(RecipeTriggerRunMode.CreateObservationOnly, observation.Outcome);
        Assert.IsFalse(observation.ProcessedWorkitem);
        Assert.IsTrue(projection.Events.Any(e => e.EventKind == RecipeTriggerTimelineEventKind.WorkitemDueObserved));
    }

    [TestMethod]
    public void ManualCheckpointResolvedProducesManualRefOutcomeOnly()
    {
        var trigger = Trigger(RecipeTriggerKind.ManualCheckpointResolved, source: RecipeTriggerSource.ManualCheckpoint);
        var readiness = RecipeTriggerPolicyEvaluator.Evaluate(trigger, Detector(RecipeDetectorKind.ManualSignal, RecipeTriggerSource.ManualCheckpoint), Policy());
        var observation = RecipeTriggerPolicyEvaluator.CreateObservation(trigger, "observation.manual", "Manual checkpoint ref observed.", ["manual.checkpoint.ref"]);
        var projection = RecipeTriggerTimelineProjector.FromObservation("projection.manual", observation);

        Assert.IsTrue(readiness.IsReady);
        Assert.AreEqual(RecipeTriggerStatus.ManualAcknowledgementRequired, readiness.Status);
        Assert.AreEqual(RecipeTriggerRunMode.ManualAcknowledgeOnly, observation.Outcome);
        Assert.IsFalse(observation.StartedRecipeRun);
        Assert.IsTrue(projection.Events.Any(e => e.EventKind == RecipeTriggerTimelineEventKind.ManualCheckpointObserved));
    }

    [TestMethod]
    public void ToolTrustSecretAndRawSecretReadinessGatesBlock()
    {
        var trigger = Trigger(RecipeTriggerKind.Manual) with
        {
            RequiredToolTrustRefs = ["tool.required"],
            RequiredSecretRefs = ["secret.required"]
        };

        var missingTool = RecipeTriggerPolicyEvaluator.Evaluate(trigger, Detector(RecipeDetectorKind.ManualSignal), Policy(availableSecrets: ["secret.required"]));
        var missingSecret = RecipeTriggerPolicyEvaluator.Evaluate(trigger, Detector(RecipeDetectorKind.ManualSignal), Policy(availableTools: ["tool.required"]));
        var rawSecret = RecipeTriggerPolicyEvaluator.Evaluate(trigger, Detector(RecipeDetectorKind.ManualSignal), Policy(["tool.required"], ["secret.required"], rawSecret: true));

        AssertBlocked(missingTool, RecipeTriggerBlockedReason.MissingToolTrust);
        AssertBlocked(missingSecret, RecipeTriggerBlockedReason.MissingSecretRef);
        AssertBlocked(rawSecret, RecipeTriggerBlockedReason.RawSecretDetected);
    }

    [TestMethod]
    public void TriggerEvidenceIsReferenceOnlyAndDoesNotRequireRealCapture()
    {
        var evidence = new RecipeTriggerEvidence(
            "trigger.evidence",
            "trigger.1",
            "observation.1",
            RecipeTriggerEvidenceSourceKind.FixtureEventRef,
            "fixture.event.ref",
            "[REDACTED] fixture event",
            RecipeEvidenceRedactionStatus.Applied);

        Assert.IsTrue(evidence.ReferenceOnly);
        Assert.IsFalse(evidence.RawPayloadEmbedded);
        Assert.IsFalse(evidence.SecretValuesIncluded);
        Assert.IsFalse(evidence.RequiresRealCapture);
        Assert.IsFalse(evidence.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void FutureAutorunPathProjectsRunNotStartedByPolicy()
    {
        var trigger = Trigger(RecipeTriggerKind.Manual) with { RunMode = RecipeTriggerRunMode.FutureAutoRunBlocked };
        var readiness = RecipeTriggerPolicyEvaluator.Evaluate(trigger, Detector(RecipeDetectorKind.ManualSignal), Policy());
        var observation = RecipeTriggerPolicyEvaluator.CreateObservation(trigger, "observation.autorun", "Future autorun requested by fixture.", ["fixture.event.ref"]);
        var projection = RecipeTriggerTimelineProjector.FromObservation("projection.autorun", observation);

        AssertBlocked(readiness, RecipeTriggerBlockedReason.AutoRunBlocked);
        Assert.IsTrue(observation.FutureAutoRunRequested);
        Assert.IsTrue(projection.Events.Any(e => e.EventKind == RecipeTriggerTimelineEventKind.TriggerRunNotStartedByPolicy));
        Assert.IsFalse(projection.StartsRecipeRun);
        Assert.IsFalse(projection.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void FutureDetectorMapsToTriggerFutureGatedTimelineEvent()
    {
        var trigger = Trigger(RecipeTriggerKind.BrowserElementAppearedFuture, source: RecipeTriggerSource.FutureBrowser);
        var readiness = RecipeTriggerPolicyEvaluator.Evaluate(trigger, Detector(RecipeDetectorKind.FutureBrowserObserver, RecipeTriggerSource.FutureBrowser), Policy());
        var projection = RecipeTriggerTimelineProjector.FromReadiness("projection.future", trigger, readiness);

        Assert.AreEqual(RecipeTimelineProjectionStatus.BlockedLiveRuntimeDisabled, projection.Status);
        Assert.IsTrue(projection.Events.Any(e => e.EventKind == RecipeTriggerTimelineEventKind.TriggerFutureGated));
        Assert.IsTrue(projection.ObservationOnly);
    }

    [TestMethod]
    public void ChallengeTriggersMapToHumanBlockAndNoBypass()
    {
        var challenge = Trigger(RecipeTriggerKind.Manual, source: RecipeTriggerSource.AuthChallenge) with
        {
            ConditionSummary = "captcha or 2fa challenge detected"
        };
        var readiness = RecipeTriggerPolicyEvaluator.Evaluate(challenge, Detector(RecipeDetectorKind.ManualSignal, RecipeTriggerSource.AuthChallenge), Policy());

        AssertBlocked(readiness, RecipeTriggerBlockedReason.ChallengeRequiresHuman);
        Assert.IsFalse(readiness.Decision.Summary.Contains("bypass", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(readiness.Decision.StartsRecipeRun);
    }

    [TestMethod]
    public void ApprovalDoesNotUnlockTriggerAutorun()
    {
        var trigger = Trigger(RecipeTriggerKind.Manual) with { RunMode = RecipeTriggerRunMode.FutureAutoRunBlocked };
        var readiness = RecipeTriggerPolicyEvaluator.Evaluate(trigger, Detector(RecipeDetectorKind.ManualSignal), Policy(approval: true));

        AssertBlocked(readiness, RecipeTriggerBlockedReason.AutoRunBlocked);
        Assert.IsFalse(readiness.Decision.StartsRecipeRun);
        Assert.IsFalse(readiness.Decision.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void TriggerCannotOverrideRecipeRunLimits()
    {
        var recipe = new RecipeDefinition("trigger.recipe")
        {
            RecipeId = "recipe.1",
            TriggerRefs = ["trigger.1"],
            RunLimits = new RecipeRunLimits(MaxSteps: 1, MaxRuntimeSeconds: 30, LiveRuntimeAllowed: false)
        };
        var trigger = Trigger(RecipeTriggerKind.Manual, recipeId: recipe.RecipeId);
        var readiness = RecipeTriggerPolicyEvaluator.Evaluate(trigger, Detector(RecipeDetectorKind.ManualSignal), Policy());

        Assert.IsTrue(readiness.IsReady);
        Assert.AreEqual(1, recipe.RunLimits.MaxSteps);
        Assert.IsFalse(recipe.RunLimits.LiveRuntimeAllowed);
        Assert.IsFalse(readiness.Decision.ActionAuthorityGranted);
    }

    [TestMethod]
    public void NoSchedulerWatcherHookListenerNetworkConnectorVaultRecorderReplayIsIntroduced()
    {
        var trigger = Trigger(RecipeTriggerKind.Manual);
        var detector = Detector(RecipeDetectorKind.FixtureDetector);
        var readiness = RecipeTriggerPolicyEvaluator.Evaluate(trigger, detector, Policy());
        var detectorObservation = new RecipeDetectorObservation("detector.observation", detector.DetectorId, detector.DetectorKind, detector.Source, "Fixture detector observation.", ["evidence.ref"]);

        Assert.IsTrue(readiness.IsReady);
        Assert.IsFalse(detector.CreatesLiveSubscription);
        Assert.IsFalse(detector.CreatesWatcherOrListener);
        Assert.IsFalse(detector.LiveRuntimeEnabled);
        Assert.IsFalse(detectorObservation.StartsRecipeRun);
        Assert.IsFalse(detectorObservation.LiveRuntimeEnabled);
        AssertNoExecution(readiness.Decision);
    }

    private static RecipeTriggerDefinition Trigger(
        RecipeTriggerKind kind,
        RecipeTriggerSource source = RecipeTriggerSource.Manual,
        string? recipeId = null,
        string? workitemQueueId = null,
        string? workitemId = null) =>
        new(
            "trigger.1",
            kind,
            new RecipeDetectorRef("detector.1"),
            recipeId,
            "6.0.0",
            workitemQueueId,
            workitemId,
            source,
            source == RecipeTriggerSource.WorkitemQueue ? RecipeTriggerScope.WorkitemQueue : RecipeTriggerScope.Recipe,
            RecipeTriggerSafetyMode.ObserveOnly,
            RecipeTriggerRunMode.CreateObservationOnly,
            RecipeTriggerStatus.Defined,
            "fixture condition",
            "schema.trigger.ref",
            RequiredToolTrustRefs: [],
            RequiredSecretRefs: [],
            EvidenceRequirementRefs: ["evidence.trigger.policy"],
            TimelineProjectionRefs: ["timeline.trigger.policy"],
            ApprovalRefs: [],
            HumanInterventionRefs: [],
            AllowedOutcomes: [RecipeTriggerRunMode.CreateObservationOnly, RecipeTriggerRunMode.SuggestRecipeOnly, RecipeTriggerRunMode.ManualAcknowledgeOnly],
            BlockedOutcomes: [RecipeTriggerRunMode.FutureAutoRunBlocked]);

    private static RecipeDetectorDefinition Detector(
        RecipeDetectorKind kind,
        RecipeTriggerSource source = RecipeTriggerSource.Manual) =>
        new(
            "detector.1",
            kind,
            source,
            RecipeTriggerScope.Recipe,
            RecipeTriggerSafetyMode.ObserveOnly,
            "fixture detector condition",
            "schema.detector.ref",
            RequiredToolTrustRefs: [],
            RequiredSecretRefs: [],
            EvidenceRequirementRefs: ["evidence.detector.policy"]);

    private static RecipeTriggerPolicy Policy(
        IReadOnlyList<string>? availableTools = null,
        IReadOnlyList<string>? availableSecrets = null,
        bool rawSecret = false,
        bool approval = false) =>
        new(
            availableTools ?? [],
            availableSecrets ?? [],
            RawSecretDetected: rawSecret,
            FutureDetectorsAllowed: false,
            AutoRunAllowed: false,
            ApprovalPresent: approval);

    private static void AssertBlocked(RecipeTriggerReadiness readiness, RecipeTriggerBlockedReason reason, string? context = null)
    {
        var message = context ?? string.Empty;
        Assert.IsFalse(readiness.IsReady, message);
        Assert.AreEqual(reason, readiness.Decision.BlockedReason, message);
        AssertNoExecution(readiness.Decision, message);
    }

    private static void AssertNoExecution(RecipeTriggerDecision decision, string? context = null)
    {
        var message = context ?? string.Empty;
        Assert.IsFalse(decision.StartsRecipeRun, message);
        Assert.IsFalse(decision.ProcessesWorkitem, message);
        Assert.IsFalse(decision.CreatesWatcher, message);
        Assert.IsFalse(decision.CreatesScheduler, message);
        Assert.IsFalse(decision.CreatesHook, message);
        Assert.IsFalse(decision.CreatesListener, message);
        Assert.IsFalse(decision.LiveRuntimeEnabled, message);
        Assert.IsFalse(decision.ActionAuthorityGranted, message);
    }
}
