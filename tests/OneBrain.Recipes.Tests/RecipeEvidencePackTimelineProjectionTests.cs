using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("RecipeEvidencePackTimelineProjection")]
public sealed class RecipeEvidencePackTimelineProjectionTests
{
    [TestMethod]
    public void RecipeEvidencePackCanReferenceRunStepsValidationsApprovalsWorkitemsArtifactsAndTimelineRefs()
    {
        var pack = CompletePack();

        Assert.AreEqual("evidence.pack.1", pack.EvidencePackId);
        CollectionAssert.Contains(pack.WorkitemRefs.ToArray(), "workitem.1");
        CollectionAssert.Contains(pack.StepEvidenceRefs.ToArray(), "step.evidence.1");
        CollectionAssert.Contains(pack.ValidationEvidenceRefs.ToArray(), "validation.evidence.1");
        CollectionAssert.Contains(pack.ApprovalRefs.ToArray(), "approval.1");
        CollectionAssert.Contains(pack.ArtifactRefs.ToArray(), "artifact.download.ref");
        CollectionAssert.Contains(pack.TimelineEventRefs.ToArray(), "timeline.event.1");
        Assert.IsFalse(pack.LiveRuntimeEnabled);
        Assert.IsFalse(pack.ActionAuthorityGranted);
    }

    [TestMethod]
    public void EvidencePackIsReferenceOnlyAndDoesNotRequireRealCapture()
    {
        var item = EvidenceItem("item.screenshot", RecipeEvidenceSourceKind.ScreenshotBeforeRef);
        var artifact = new RecipeArtifactRef("artifact.1", "download", "artifact.download.ref");

        Assert.IsTrue(item.IsReferenceOnly);
        Assert.IsFalse(item.RawPayloadEmbedded);
        Assert.IsFalse(item.RequiresRealCapture);
        Assert.IsFalse(artifact.RawBytesEmbedded);
        Assert.IsFalse(artifact.RealFileProbeRequired);
    }

    [TestMethod]
    public void FutureBrowserRuntimeCaptureModeDoesNotEnableLiveBrowserRuntime()
    {
        var pack = CompletePack() with { CaptureMode = RecipeEvidenceCaptureMode.FutureBrowserRuntime };
        var completeness = RecipeEvidencePolicy.EvaluatePackCompleteness(pack, [SatisfiedStepResult()], [PassedValidation()]);

        Assert.IsFalse(pack.LiveBrowserRuntimeEnabled);
        Assert.IsFalse(pack.LiveRuntimeEnabled);
        Assert.AreEqual(RecipeEvidenceCompleteness.BlockedLiveRuntimeDisabled, completeness);
    }

    [TestMethod]
    public void FutureDesktopRuntimeCaptureModeDoesNotEnableLiveDesktopRuntime()
    {
        var pack = CompletePack() with { CaptureMode = RecipeEvidenceCaptureMode.FutureDesktopRuntime };
        var completeness = RecipeEvidencePolicy.EvaluatePackCompleteness(pack, [SatisfiedStepResult()], [PassedValidation()]);

        Assert.IsFalse(pack.LiveDesktopRuntimeEnabled);
        Assert.IsFalse(pack.LiveRuntimeEnabled);
        Assert.AreEqual(RecipeEvidenceCompleteness.BlockedLiveRuntimeDisabled, completeness);
    }

    [TestMethod]
    public void BrowserActionStepEvidenceRequiresMeaningfulBeforeAfterOrEquivalentRefs()
    {
        var block = Block("browser", RecipeBlockType.BrowserAction);
        var missing = StepEvidence(block.BlockId) with { BeforeStateRefs = [], AfterStateRefs = [] };

        var result = RecipeEvidencePolicy.EvaluateStepEvidence(block, missing);

        Assert.IsFalse(result.Satisfied);
        CollectionAssert.Contains(result.MissingRefs.ToArray(), "before-state-ref");
        CollectionAssert.Contains(result.MissingRefs.ToArray(), "after-state-ref");
        Assert.IsFalse(result.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void FileDownloadEvidenceRequiresDownloadedFileRefAndValidationEvidenceRef()
    {
        var block = Block("download", RecipeBlockType.FileDownloadEvidence);
        var missing = StepEvidence(block.BlockId) with { ArtifactRefs = [], ValidationRefs = [] };

        var result = RecipeEvidencePolicy.EvaluateStepEvidence(block, missing);

        Assert.IsFalse(result.Satisfied);
        CollectionAssert.Contains(result.MissingRefs.ToArray(), "downloaded-file-ref");
        CollectionAssert.Contains(result.MissingRefs.ToArray(), "validation-evidence-ref");
    }

    [TestMethod]
    public void WorkitemUpdateRequiresWorkitemStateEvidenceRef()
    {
        var block = Block("workitem", RecipeBlockType.WorkitemUpdate);
        var missing = StepEvidence(block.BlockId) with { ArtifactRefs = [] };

        var result = RecipeEvidencePolicy.EvaluateStepEvidence(block, missing);

        Assert.IsFalse(result.Satisfied);
        CollectionAssert.Contains(result.MissingRefs.ToArray(), "workitem-state-evidence-ref");
    }

    [TestMethod]
    public void HumanInterventionRequiresManualApprovalOrHumanNoteRef()
    {
        var block = Block("human", RecipeBlockType.HumanIntervention);
        var missing = StepEvidence(block.BlockId) with { ApprovalRefs = [], ArtifactRefs = [] };

        var result = RecipeEvidencePolicy.EvaluateStepEvidence(block, missing);

        Assert.IsFalse(result.Satisfied);
        CollectionAssert.Contains(result.MissingRefs.ToArray(), "human-note-or-approval-ref");
    }

    [TestMethod]
    public void ConnectorDraftRequiresPolicyEvidenceButNoConnectorExecution()
    {
        var block = Block("connector", RecipeBlockType.ConnectorDraft);
        var missing = StepEvidence(block.BlockId) with { PolicyDecisionRefs = [] };

        var result = RecipeEvidencePolicy.EvaluateStepEvidence(block, missing);

        Assert.IsFalse(result.Satisfied);
        CollectionAssert.Contains(result.MissingRefs.ToArray(), "policy-decision-ref");
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void DesktopActionDraftRemainsDraftBlockedAndCanOnlyHavePlannedFutureEvidenceRefs()
    {
        var block = Block("desktop", RecipeBlockType.DesktopActionDraft);
        var evidence = StepEvidence(block.BlockId) with { PolicyDecisionRefs = ["policy.desktop.blocked"] };

        var result = RecipeEvidencePolicy.EvaluateStepEvidence(block, evidence);

        Assert.IsTrue(result.Satisfied);
        Assert.IsTrue(result.Reasons.Any(r => r.Contains("planned/future", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(evidence.LiveRuntimeEnabled);
        Assert.IsFalse(evidence.ActionAuthorityGranted);
    }

    [TestMethod]
    public void ValidationEvidenceCanBePassedFailedBlockedWarningAndNotRunWithSeverity()
    {
        var statuses = new[]
        {
            RecipeValidationEvidenceStatus.Passed,
            RecipeValidationEvidenceStatus.Failed,
            RecipeValidationEvidenceStatus.Blocked,
            RecipeValidationEvidenceStatus.Warning,
            RecipeValidationEvidenceStatus.NotRun
        };

        foreach (var status in statuses)
        {
            var evidence = Validation(status);
            Assert.AreEqual(status, evidence.Status);
            Assert.AreEqual(RecipeValidationSeverity.Blocking, evidence.BlockingSeverity);
            Assert.IsFalse(evidence.RawSecretValueExposed);
        }
    }

    [TestMethod]
    public void BlockingValidationFailureMapsToTimelineEvent()
    {
        var validation = Validation(RecipeValidationEvidenceStatus.Failed) with { FailureReason = "redacted validation failed" };
        var ev = RecipeTimelineProjector.FromValidationEvidence("timeline.validation.failed", "run.1", "recipe.1", validation);

        Assert.AreEqual(RecipeTimelineEventKind.RecipeStepFailed, ev.EventKind);
        Assert.AreEqual(RecipeTimelineEventSeverity.Blocking, ev.Severity);
        CollectionAssert.Contains(ev.ValidationRefs.ToArray(), validation.ValidationEvidenceId);
    }

    [TestMethod]
    public void ReadinessBlockedIssueMapsToTimelineEvent()
    {
        var issue = new RecipeReadinessIssue("risk", RecipeReadinessStatus.BlockedRiskGate, RecipeReadinessIssueSeverity.Blocking, "risk blocked", "block.1");
        var ev = RecipeTimelineProjector.FromReadinessIssue("timeline.risk", "run.1", "recipe.1", issue);

        Assert.AreEqual(RecipeTimelineEventKind.RiskGateBlocked, ev.EventKind);
        Assert.AreEqual(RecipeTimelineEventSeverity.Blocking, ev.Severity);
        Assert.AreEqual("block.1", ev.BlockId);
    }

    [TestMethod]
    public void HumanInterventionMapsToTimelineEvent()
    {
        var ev = RecipeTimelineProjector.HumanIntervention("timeline.human", "run.1", "recipe.1", "human required", ["evidence.human"]);

        Assert.AreEqual(RecipeTimelineEventKind.HumanInterventionRequested, ev.EventKind);
        Assert.AreEqual(RecipeTimelineEventSeverity.Blocking, ev.Severity);
        CollectionAssert.Contains(ev.EvidenceRefs.ToArray(), "evidence.human");
    }

    [TestMethod]
    public void EvidenceMissingMapsToTimelineEvent()
    {
        var ev = RecipeTimelineProjector.EvidenceMissing("timeline.missing", "run.1", "recipe.1", "missing validation evidence");

        Assert.AreEqual(RecipeTimelineEventKind.EvidenceMissing, ev.EventKind);
        Assert.AreEqual(RecipeTimelineEventSeverity.Blocking, ev.Severity);
    }

    [TestMethod]
    public void RedactionAppliedMapsToTimelineEvent()
    {
        var ev = RecipeTimelineProjector.RedactionApplied("timeline.redaction", "run.1", "recipe.1", ["redaction.report.1"]);

        Assert.AreEqual(RecipeTimelineEventKind.RedactionApplied, ev.EventKind);
        CollectionAssert.Contains(ev.RedactionRefs.ToArray(), "redaction.report.1");
    }

    [TestMethod]
    public void SecretLikeEvidenceValuesAreNeverExposedOnlyRefsAndSummaries()
    {
        var redaction = SafeRedaction();
        var pack = CompletePack() with { RedactionSummary = redaction };
        var json = JsonSerializer.Serialize(pack, JsonOptions());

        Assert.IsFalse(redaction.HasRawSecretExposure);
        Assert.IsTrue(redaction.SecretRefs.All(r => r.StartsWith("secret.", StringComparison.Ordinal)));
        Assert.IsFalse(json.Contains("secret-value", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("sk-", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void HandoffSummaryExcludesRawSecretsAndPayloads()
    {
        var projection = RecipeTimelineProjector.CreateProjection("projection.1", "run.1", "recipe.1", [], "redacted summary");
        var handoff = RecipeEvidencePolicy.CreateHandoffSummary(CompletePack(), projection);

        Assert.IsFalse(handoff.IncludesRawPayloads);
        Assert.IsFalse(handoff.IncludesSecretValues);
        Assert.IsTrue(handoff.SafeByDefault);
        CollectionAssert.Contains(handoff.RawDataOmitted.ToArray(), "secret values");
    }

    [TestMethod]
    public void AuthChallengeTwoFactorCaptchaFailureEvidenceRequiresHumanBlockNoBypassSuggestion()
    {
        foreach (var failureType in new[] { WorkitemFailureType.Auth, WorkitemFailureType.Challenge })
        {
            var failure = RecipeFailureEvidenceFactory.Create($"failure.{failureType}", failureType);

            Assert.IsTrue(failure.HumanInterventionRequired);
            Assert.AreEqual(RecipeFailureRecoveryHint.RequestHumanIntervention, failure.RecoveryHint);
            Assert.IsFalse(failure.SuggestsBypass);
            Assert.IsTrue(failure.IsBlocking);
        }
    }

    [TestMethod]
    public void UnknownUnsafeFailureClassIsBlocking()
    {
        var failure = RecipeFailureEvidenceFactory.Create("failure.unknown", WorkitemFailureType.Unknown);

        Assert.AreEqual(RecipeFailureClass.UnknownUnsafe, failure.FailureClass);
        Assert.AreEqual(RecipeFailureRecoveryHint.AbortUnsafe, failure.RecoveryHint);
        Assert.IsTrue(failure.IsBlocking);
    }

    [TestMethod]
    public void ValidFixtureRecipeWithLimitsPolicyEvidenceExpectationsCanProduceCompleteEvidenceSummary()
    {
        var stepResult = RecipeEvidencePolicy.EvaluateStepEvidence(Block("browser", RecipeBlockType.BrowserAction), StepEvidence("browser"));
        var pack = CompletePack();
        var completeness = RecipeEvidencePolicy.EvaluatePackCompleteness(pack, [stepResult], [PassedValidation()]);
        var projection = RecipeTimelineProjector.CreateProjection("projection.1", pack.RunId, pack.RecipeId, [RecipeTimelineProjector.RedactionApplied("timeline.redaction", pack.RunId, pack.RecipeId, ["redaction.1"])], "redacted");

        Assert.AreEqual(RecipeEvidenceCompleteness.Complete, completeness);
        Assert.AreEqual(RecipeTimelineProjectionStatus.Projected, projection.Status);
        Assert.IsFalse(pack.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void NoSchedulerBackgroundWorkerRealBrowserDesktopCdpNetworkWatcherRecorderReplayIsIntroduced()
    {
        var pack = CompletePack();
        var item = EvidenceItem("item.dom", RecipeEvidenceSourceKind.DomSnapshotRef);
        var projection = RecipeTimelineProjector.CreateProjection("projection.1", "run.1", "recipe.1", [], "redacted");

        Assert.IsFalse(pack.LiveRuntimeEnabled);
        Assert.IsFalse(pack.LiveBrowserRuntimeEnabled);
        Assert.IsFalse(pack.LiveDesktopRuntimeEnabled);
        Assert.IsFalse(pack.ActionAuthorityGranted);
        Assert.IsTrue(item.IsReferenceOnly);
        Assert.IsFalse(item.RequiresRealCapture);
        Assert.IsFalse(projection.LiveRuntimeEnabled);
    }

    private static RecipeEvidencePack CompletePack() =>
        new(
            EvidencePackId: "evidence.pack.1",
            RecipeId: "recipe.1",
            RecipeVersion: "3.0.0",
            RunId: "run.1",
            MissionIdRef: "mission.1",
            WorkitemRefs: ["workitem.1"],
            StepEvidenceRefs: ["step.evidence.1"],
            ValidationEvidenceRefs: ["validation.evidence.1"],
            ApprovalRefs: ["approval.1"],
            TimelineEventRefs: ["timeline.event.1"],
            ArtifactRefs: ["artifact.download.ref"],
            RedactionReportRef: "redaction.report.1",
            RecipeEvidenceSensitivity.Confidential,
            RecipeEvidenceCompleteness.Complete,
            RecipeEvidenceCaptureMode.ReferenceOnly,
            CreatedAt: DateTimeOffset.Parse("2026-06-27T00:00:00Z"),
            RecipeRunMode.FixtureRun,
            FailureSummary: null,
            SafeRedaction());

    private static RecipeEvidenceRedactionSummary SafeRedaction() =>
        new(
            RedactionApplied: true,
            RedactionPolicyRef: "redaction.policy.1",
            SensitiveFields:
            [
                new RecipeSensitiveFieldSummary(RecipeSensitiveFieldCategory.Secret, "[REDACTED]", "secret.fixture", RawValuePresent: false)
            ],
            SecretRefs: ["secret.fixture"],
            RecipeEvidenceSecretHandlingStatus.SecretRefsOnly,
            RawPayloadExposed: false,
            EvidenceSafeForHandoff: true,
            EvidenceSafeForTimeline: true);

    private static RecipeEvidenceItem EvidenceItem(string id, RecipeEvidenceSourceKind kind) =>
        new(
            id,
            kind,
            $"ref.{id}",
            "redacted summary",
            RecipeEvidenceSensitivity.Confidential,
            RecipeEvidenceCaptureMode.ReferenceOnly,
            RecipeEvidenceRedactionStatus.Applied);

    private static RecipeStepEvidence StepEvidence(string blockId) =>
        new(
            StepEvidenceId: $"step.evidence.{blockId}",
            StepId: $"step.{blockId}",
            RunId: "run.1",
            BlockId: blockId,
            SequenceNumber: 1,
            ActionIntentSummary: "fixture intent",
            TargetSummary: "redacted target",
            BeforeStateRefs: ["state.before.ref"],
            AfterStateRefs: ["state.after.ref"],
            ValidationRefs: ["validation.evidence.1"],
            ArtifactRefs: ["artifact.download.ref", "artifact.workitem.state.ref"],
            ApprovalRefs: ["approval.1"],
            PolicyDecisionRefs: ["policy.1"],
            RecipeEvidenceRedactionStatus.Applied,
            RecipeStepEvidenceStatus.Satisfied);

    private static RecipeValidationEvidence Validation(RecipeValidationEvidenceStatus status) =>
        new(
            ValidationEvidenceId: $"validation.{status}",
            RecipeValidationKind.EvidenceRefExists,
            ExpectedValueSummary: "expected redacted ref",
            ActualValueSummary: "[REDACTED]",
            SourceEvidenceRefs: ["evidence.ref.1"],
            status,
            RecipeValidationSeverity.Blocking,
            RecipeEvidenceRedactionStatus.Applied,
            FailureReason: status == RecipeValidationEvidenceStatus.Passed ? null : "fixture validation did not pass");

    private static RecipeValidationEvidence PassedValidation() => Validation(RecipeValidationEvidenceStatus.Passed);

    private static RecipeStepEvidenceResult SatisfiedStepResult() =>
        new(true, RecipeStepEvidenceStatus.Satisfied, [], []);

    private static RecipeBlock Block(string id, RecipeBlockType type) =>
        new(
            id,
            type,
            id,
            "fixture intent",
            TargetRef: "target.fixture",
            InputBinding: "input.fixture",
            OutputBinding: "output.fixture",
            Preconditions: [],
            Postconditions: [],
            ValidationRefs: ["validation.fixture"],
            RecipeRiskLevel.Low,
            RecipeApprovalRequirement.None,
            EvidenceExpectationRef: "evidence.fixture",
            FailurePolicyRef: "failure.fixture",
            NextBlockRefs: []);

    private static JsonSerializerOptions JsonOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
