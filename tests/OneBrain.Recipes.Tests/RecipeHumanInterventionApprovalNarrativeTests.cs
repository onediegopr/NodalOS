using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("RecipeHumanInterventionApprovalNarrative")]
public sealed class RecipeHumanInterventionApprovalNarrativeTests
{
    [TestMethod]
    public void HumanInterventionRequestCanReferenceRecipeRunStepWorkitemEvidenceValidationPolicyTimeline()
    {
        var request = Intervention(RecipeHumanInterventionKind.PaymentConfirmationRequired);

        Assert.AreEqual("recipe.1", request.RecipeId);
        Assert.AreEqual("run.1", request.RunId);
        Assert.AreEqual("step.1", request.StepId);
        Assert.AreEqual("block.1", request.BlockId);
        Assert.AreEqual("workitem.1", request.WorkitemId);
        CollectionAssert.Contains(request.EvidenceRefs.ToArray(), "evidence.ref");
        CollectionAssert.Contains(request.ValidationRefs.ToArray(), "validation.ref");
        CollectionAssert.Contains(request.PolicyDecisionRefs.ToArray(), "policy.ref");
        CollectionAssert.Contains(request.TimelineRefs.ToArray(), "timeline.ref");
        Assert.IsFalse(request.LiveRuntimeEnabled);
        Assert.IsFalse(request.ActionAuthorityGranted);
    }

    [TestMethod]
    public void LoginRequiredMapsToAwaitingOperator()
    {
        var request = Intervention(RecipeHumanInterventionKind.LoginRequired);

        Assert.AreEqual(RecipeHumanInterventionStatus.AwaitingOperator, request.Status);
        Assert.AreEqual(RecipeHumanInterventionReason.AuthRequired, request.Reason);
    }

    [TestMethod]
    public void TwoFactorRequiredMapsToHumanInterventionAndCannotAutoApproveAutomation()
    {
        var request = Intervention(RecipeHumanInterventionKind.TwoFactorRequired);
        var scenario = RecipeHumanBlockingScenarioCatalog.For(RecipeHumanInterventionKind.TwoFactorRequired);

        Assert.AreEqual(RecipeHumanInterventionTrigger.AuthChallenge, request.SourceTrigger);
        Assert.IsFalse(scenario.ApprovalCanResolveInThisPhase);
        AssertNoLiveOptions(scenario.DefaultDecisionOptions);
    }

    [TestMethod]
    public void CaptchaOrChallengeDetectedMapsToHumanBlockAndNeverBypass()
    {
        var request = Intervention(RecipeHumanInterventionKind.CaptchaOrChallengeDetected);
        var scenario = RecipeHumanBlockingScenarioCatalog.For(RecipeHumanInterventionKind.CaptchaOrChallengeDetected);

        Assert.AreEqual(RecipeHumanInterventionReason.ChallengeDetected, request.Reason);
        Assert.IsFalse(scenario.ApprovalCanResolveInThisPhase);
        Assert.IsFalse(request.SafeNextAction.Summary.Contains("bypass", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void PaymentConfirmationRequiredRequiresApprovalNarrative()
    {
        AssertRequiresNarrative(RecipeHumanInterventionKind.PaymentConfirmationRequired);
    }

    [TestMethod]
    public void FiscalOrLegalSubmissionReviewRequiresApprovalNarrative()
    {
        AssertRequiresNarrative(RecipeHumanInterventionKind.FiscalOrLegalSubmissionReview);
    }

    [TestMethod]
    public void EmailOrMessageSendReviewRequiresApprovalNarrative()
    {
        AssertRequiresNarrative(RecipeHumanInterventionKind.EmailOrMessageSendReview);
    }

    [TestMethod]
    public void PublicPostingReviewRequiresApprovalNarrative()
    {
        AssertRequiresNarrative(RecipeHumanInterventionKind.PublicPostingReview);
    }

    [TestMethod]
    public void DataDeletionReviewRequiresApprovalNarrativeAndSafeNextAction()
    {
        var narrative = Narrative(RecipeHumanInterventionKind.DataDeletionReview);

        AssertRequiresNarrative(RecipeHumanInterventionKind.DataDeletionReview);
        Assert.IsFalse(string.IsNullOrWhiteSpace(narrative.SafeNextAction.Summary));
        Assert.IsFalse(narrative.SafeNextAction.AllowsExternalMutation);
    }

    [TestMethod]
    public void UnknownUnsafeStateDefaultsToBlockedOrHumanIntervention()
    {
        var request = Intervention(RecipeHumanInterventionKind.UnknownUnsafeState);
        var scenario = RecipeHumanBlockingScenarioCatalog.For(RecipeHumanInterventionKind.UnknownUnsafeState);

        Assert.AreEqual(RecipeHumanInterventionStatus.BlockedByPolicy, request.Status);
        Assert.AreEqual(RecipeSafeNextActionKind.KeepBlocked, scenario.SafeNextAction.Kind);
        Assert.IsFalse(scenario.ApprovalCanResolveInThisPhase);
    }

    [TestMethod]
    public void HighCriticalRiskWithoutApprovalNarrativeBlocksReadiness()
    {
        var result = RecipeApprovalReadinessEvaluator.Evaluate(
            Risk(RecipeRiskLevel.Critical, SensitiveActionCategory.Payment),
            narrative: null,
            humanInterventionPathPresent: true);

        AssertBlocked(result, RecipeReadinessStatus.BlockedMissingApprovalPolicy, "approval-narrative-required");
    }

    [TestMethod]
    public void SensitiveActionWithoutHumanInterventionPathBlocksReadiness()
    {
        var result = RecipeApprovalReadinessEvaluator.Evaluate(
            Risk(RecipeRiskLevel.High, SensitiveActionCategory.Payment),
            Narrative(RecipeHumanInterventionKind.PaymentConfirmationRequired),
            humanInterventionPathPresent: false);

        AssertBlocked(result, RecipeReadinessStatus.BlockedMissingApprovalPolicy, "human-path-required");
    }

    [TestMethod]
    public void BrowserLiveActionRemainsBlockedEvenWithApprovalNarrative()
    {
        var result = RecipeApprovalReadinessEvaluator.Evaluate(
            Risk(RecipeRiskLevel.High, SensitiveActionCategory.BrowserLiveAction),
            Narrative(RecipeHumanInterventionKind.PolicyBlockedReview),
            humanInterventionPathPresent: true);

        AssertBlocked(result, RecipeReadinessStatus.BlockedLiveRuntimeDisabled, "live-runtime-remains-blocked");
    }

    [TestMethod]
    public void DesktopLiveActionRemainsBlockedEvenWithApprovalNarrative()
    {
        var result = RecipeApprovalReadinessEvaluator.Evaluate(
            Risk(RecipeRiskLevel.High, SensitiveActionCategory.DesktopLiveAction),
            Narrative(RecipeHumanInterventionKind.PolicyBlockedReview),
            humanInterventionPathPresent: true);

        AssertBlocked(result, RecipeReadinessStatus.BlockedLiveRuntimeDisabled, "live-runtime-remains-blocked");
    }

    [TestMethod]
    public void ApprovalDecisionOptionsDoNotIncludeLiveExecutionOptionsAsUsableChoices()
    {
        var optionNames = Enum.GetNames<RecipeApprovalDecisionOption>();

        Assert.IsFalse(optionNames.Any(name => name.Contains("LiveBrowser", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(optionNames.Any(name => name.Contains("LiveDesktop", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(optionNames.Any(name => name.Contains("PaymentExecution", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(optionNames.Any(name => name.Contains("DeleteExecution", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ApprovalNarrativeIncludesRequestedActionRiskEvidenceValidationLimitsConsequencesAndSafeNextAction()
    {
        var narrative = Narrative(RecipeHumanInterventionKind.PaymentConfirmationRequired);

        Assert.IsTrue(narrative.HasRequiredNarrativeParts);
        Assert.AreEqual(RecipeRiskLevel.High, narrative.RiskExplanation.RiskLevel);
        Assert.IsTrue(narrative.EvidenceSummary.EvidenceRefs.Count > 0);
        Assert.IsFalse(string.IsNullOrWhiteSpace(narrative.ValidationSummary));
        Assert.IsFalse(string.IsNullOrWhiteSpace(narrative.RedactionSummary));
        Assert.IsNotNull(narrative.LimitsSummary);
        Assert.IsTrue(narrative.IfApprovedConsequences.Count > 0);
        Assert.IsTrue(narrative.IfRejectedConsequences.Count > 0);
        Assert.IsFalse(narrative.SafeNextAction.AllowsLiveRuntime);
    }

    [TestMethod]
    public void MissingEvidenceCausesRequestMoreEvidenceOrKeepBlockedNotApproval()
    {
        var narrative = Narrative(RecipeHumanInterventionKind.PaymentConfirmationRequired, missingEvidence: true);
        var decision = RecipeApprovalDecisionPolicy.Decide("decision.1", narrative, RecipeApprovalDecisionOption.ApproveFixtureRunOnly);

        Assert.AreEqual(RecipeApprovalDecisionOption.RequestMoreEvidence, decision.Option);
        Assert.AreEqual(RecipeApprovalDecisionStatus.MoreEvidenceRequested, decision.Status);
        Assert.IsFalse(decision.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void ApprovalDecisionRefsAreSafeForTimelineAndHandoff()
    {
        var decision = RecipeApprovalDecisionPolicy.Decide("decision.1", Narrative(RecipeHumanInterventionKind.PaymentConfirmationRequired), RecipeApprovalDecisionOption.ApproveDryRunOnly);

        Assert.IsTrue(decision.SafeForTimeline);
        Assert.IsTrue(decision.SafeForHandoff);
        Assert.IsFalse(decision.LiveRuntimeEnabled);
        Assert.IsFalse(decision.ActionAuthorityGranted);
        Assert.IsFalse(decision.AllowsExternalMutation);
    }

    [TestMethod]
    public void OperatorNoteIsRepresentedAsRefSummaryNoRawSecretValues()
    {
        var request = Intervention(RecipeHumanInterventionKind.SecretHandlingReview);
        var resolution = new RecipeManualResolution("manual.1", RecipeManualResolutionOutcome.ResolvedByOperator, "operator.note.ref", "redacted note summary", ["evidence.ref"], DateTimeOffset.Parse("2026-06-27T00:00:00Z"));
        var json = JsonSerializer.Serialize(new { request, resolution }, JsonOptions());

        Assert.AreEqual("operator.note.ref", request.OperatorNoteRef);
        Assert.IsFalse(resolution.ContainsRawSecretValue);
        Assert.IsFalse(json.Contains("secret-value", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("sk-", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ManualResolutionMapsToTimelineEvent()
    {
        var narrative = Narrative(RecipeHumanInterventionKind.ManualCheckpoint);
        var decision = RecipeApprovalDecisionPolicy.Decide("decision.manual", narrative, RecipeApprovalDecisionOption.MarkManuallyResolved);
        var ev = RecipeApprovalTimelineProjector.FromApprovalDecision(decision, narrative.RunId, narrative.RecipeId);

        Assert.AreEqual(RecipeTimelineEventKind.HandoffCreated, ev.EventKind);
        Assert.AreEqual(RecipeTimelineEventSeverity.Info, ev.Severity);
    }

    [TestMethod]
    public void RejectedApprovalMapsToBlockedOrCancelledTimelineEvent()
    {
        var narrative = Narrative(RecipeHumanInterventionKind.PaymentConfirmationRequired);
        var decision = RecipeApprovalDecisionPolicy.Decide("decision.reject", narrative, RecipeApprovalDecisionOption.Reject);
        var ev = RecipeApprovalTimelineProjector.FromApprovalDecision(decision, narrative.RunId, narrative.RecipeId);

        Assert.AreEqual(RecipeTimelineEventKind.RecipeStepBlocked, ev.EventKind);
        Assert.AreEqual(RecipeTimelineEventSeverity.Blocking, ev.Severity);
    }

    [TestMethod]
    public void RequestMoreEvidenceMapsToEvidenceMissingTimelineEvent()
    {
        var narrative = Narrative(RecipeHumanInterventionKind.PaymentConfirmationRequired, missingEvidence: true);
        var decision = RecipeApprovalDecisionPolicy.Decide("decision.more", narrative, RecipeApprovalDecisionOption.RequestMoreEvidence);
        var ev = RecipeApprovalTimelineProjector.FromApprovalDecision(decision, narrative.RunId, narrative.RecipeId);

        Assert.AreEqual(RecipeTimelineEventKind.EvidenceMissing, ev.EventKind);
        Assert.AreEqual(RecipeTimelineEventSeverity.Blocking, ev.Severity);
    }

    [TestMethod]
    public void CriticalRiskWithoutSafeNextActionOrRollbackBoundaryProducesIssue()
    {
        var missingSafeNext = Narrative(RecipeHumanInterventionKind.PaymentConfirmationRequired, risk: RecipeRiskLevel.Critical, includeSafeNextAction: false);
        var blocked = RecipeApprovalReadinessEvaluator.Evaluate(Risk(RecipeRiskLevel.Critical, SensitiveActionCategory.Payment), missingSafeNext, humanInterventionPathPresent: true);
        AssertBlocked(blocked, RecipeReadinessStatus.BlockedMissingApprovalPolicy, "critical-missing-safe-next-action");

        var missingRollback = Narrative(RecipeHumanInterventionKind.PaymentConfirmationRequired, risk: RecipeRiskLevel.Critical, includeRollbackBoundary: false);
        var warning = RecipeApprovalReadinessEvaluator.Evaluate(Risk(RecipeRiskLevel.Critical, SensitiveActionCategory.Payment), missingRollback, humanInterventionPathPresent: true);
        Assert.IsTrue(warning.Warnings.Any(w => w.IssueId == "critical-missing-rollback-boundary"));
    }

    [TestMethod]
    public void HandoffSummaryExcludesRawSecretsPayloadsAndIncludesApprovalNarrativeSummaryOnly()
    {
        var narrative = Narrative(RecipeHumanInterventionKind.PaymentConfirmationRequired);
        var decision = RecipeApprovalDecisionPolicy.Decide("decision.1", narrative, RecipeApprovalDecisionOption.ApproveFixtureRunOnly);
        var handoff = RecipeApprovalTimelineProjector.CreateHandoffSummary(narrative, decision, SafeRedaction());

        Assert.AreEqual(narrative.NarrativeId, handoff.ApprovalNarrativeRef);
        Assert.IsFalse(handoff.IncludesRawPayloads);
        Assert.IsFalse(handoff.IncludesSecretValues);
        Assert.IsFalse(handoff.LiveRuntimeEnabled);
        CollectionAssert.Contains(handoff.RawDataOmitted.ToArray(), "secret values");
    }

    [TestMethod]
    public void NoSchedulerBackgroundWorkerRealBrowserDesktopCdpNetworkWatcherRecorderReplayIsIntroduced()
    {
        var request = Intervention(RecipeHumanInterventionKind.PaymentConfirmationRequired);
        var narrative = Narrative(RecipeHumanInterventionKind.PaymentConfirmationRequired);
        var decision = RecipeApprovalDecisionPolicy.Decide("decision.1", narrative, RecipeApprovalDecisionOption.ApproveFixtureRunOnly);

        Assert.IsFalse(request.LiveRuntimeEnabled);
        Assert.IsFalse(request.ActionAuthorityGranted);
        Assert.IsFalse(narrative.LiveRuntimeEnabled);
        Assert.IsFalse(narrative.ActionAuthorityGranted);
        Assert.IsFalse(decision.LiveRuntimeEnabled);
        Assert.IsFalse(decision.ActionAuthorityGranted);
        Assert.IsFalse(decision.AllowsExternalMutation);
    }

    private static void AssertRequiresNarrative(RecipeHumanInterventionKind kind)
    {
        var request = Intervention(kind);
        var narrative = Narrative(kind);

        Assert.IsNull(request.ApprovalNarrativeRef);
        Assert.IsTrue(narrative.HasRequiredNarrativeParts);
        Assert.IsTrue(narrative.DecisionOptions.Contains(RecipeApprovalDecisionOption.KeepBlocked));
        Assert.IsFalse(narrative.LiveRuntimeEnabled);
    }

    private static void AssertBlocked(RecipeApprovalReadinessResult result, RecipeReadinessStatus status, string issueId)
    {
        Assert.IsFalse(result.IsReady);
        Assert.AreEqual(status, result.Status);
        Assert.IsTrue(result.BlockingIssues.Any(i => i.IssueId == issueId), issueId);
        Assert.IsFalse(result.LiveRuntimeEnabled);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    private static void AssertNoLiveOptions(IReadOnlyList<RecipeApprovalDecisionOption> options)
    {
        var names = options.Select(option => option.ToString()).ToArray();
        Assert.IsFalse(names.Any(name => name.Contains("Live", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(names.Any(name => name.Contains("Execution", StringComparison.OrdinalIgnoreCase)));
    }

    private static RecipeHumanInterventionRequest Intervention(RecipeHumanInterventionKind kind) =>
        RecipeHumanInterventionFactory.Create(
            "intervention.1",
            "recipe.1",
            "4.0.0",
            "run.1",
            kind,
            blockId: "block.1",
            stepId: "step.1",
            workitemId: "workitem.1");

    private static RecipeApprovalNarrative Narrative(
        RecipeHumanInterventionKind kind,
        bool missingEvidence = false,
        RecipeRiskLevel risk = RecipeRiskLevel.High,
        bool includeSafeNextAction = true,
        bool includeRollbackBoundary = true) =>
        RecipeApprovalNarrativeFactory.Create(
            "narrative.1",
            "recipe.1",
            "4.0.0",
            "run.1",
            kind,
            risk,
            missingEvidence,
            includeSafeNextAction,
            includeRollbackBoundary);

    private static RecipeRiskProfile Risk(RecipeRiskLevel level, SensitiveActionCategory category) =>
        new(
            "risk.1",
            level,
            new HashSet<SensitiveActionCategory> { category },
            [],
            ApprovalPolicyPresent: true,
            HumanInterventionPathPresent: true,
            SecretRefs: ["secret.ref"]);

    private static RecipeEvidenceRedactionSummary SafeRedaction() =>
        new(
            RedactionApplied: true,
            RedactionPolicyRef: "redaction.policy",
            SensitiveFields: [],
            SecretRefs: ["secret.ref"],
            RecipeEvidenceSecretHandlingStatus.SecretRefsOnly,
            RawPayloadExposed: false,
            EvidenceSafeForHandoff: true,
            EvidenceSafeForTimeline: true);

    private static JsonSerializerOptions JsonOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
