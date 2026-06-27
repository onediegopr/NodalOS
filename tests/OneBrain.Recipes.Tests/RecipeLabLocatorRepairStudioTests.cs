using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("RecipeLabLocatorRepairStudio")]
public sealed class RecipeLabLocatorRepairStudioTests
{
    [TestMethod]
    public void RecipeLabSnapshotSummarizesRecipeReadinessRiskEvidenceTimelineToolAndTriggerRefs()
    {
        var recipe = ValidRecipe();
        var readiness = RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun);
        var snapshot = LabSnapshot(recipe, readiness);

        Assert.AreEqual("recipe.lab", snapshot.RecipeId);
        Assert.AreEqual("7.0.0", snapshot.RecipeVersion);
        Assert.AreEqual(RecipeReadinessStatus.ReadyForFixtureRun, snapshot.Readiness.CanonicalStatus);
        CollectionAssert.Contains(snapshot.CapabilitySummary.RequiredToolTrustRefs.ToArray(), "tool.fixture");
        CollectionAssert.Contains(snapshot.CapabilitySummary.RequiredSecretAliasesOrRefs.ToArray(), "secret.fixture:fixture secret alias");
        StringAssert.Contains(snapshot.TriggerObserveOnlySummary, "observe-only");
        StringAssert.Contains(snapshot.EvidenceCompletenessSummary, "Complete");
        StringAssert.Contains(snapshot.TimelineProjectionSummary, "Projected");
        Assert.IsFalse(snapshot.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void RecipeLabIsReadOnlyAndCannotStartRunsProcessWorkitemsOrUnlockLive()
    {
        var snapshot = LabSnapshot(ValidRecipe(), RecipePolicyPreflightEvaluator.Evaluate(ValidRecipe(), RecipeRunMode.FixtureRun));

        Assert.IsTrue(snapshot.ReadOnly);
        Assert.IsTrue(snapshot.PreviewSafe);
        Assert.IsFalse(snapshot.CanStartRecipeRun);
        Assert.IsFalse(snapshot.CanProcessWorkitems);
        Assert.IsFalse(snapshot.CanUnlockLiveRuntime);
        Assert.IsFalse(snapshot.ViewModel.CanStartRecipeRun);
        Assert.IsFalse(snapshot.ViewModel.CanProcessWorkitems);
        Assert.IsFalse(snapshot.ViewModel.CanUnlockLiveRuntime);
    }

    [TestMethod]
    public void SecretReferencesDisplayAliasAndRefOnlyNoRawValue()
    {
        var snapshot = LabSnapshot(ValidRecipe(), RecipePolicyPreflightEvaluator.Evaluate(ValidRecipe(), RecipeRunMode.FixtureRun));
        var notebook = RecipeLabSnapshotFactory.CreateNotebook("notebook.1", snapshot, LocatorSnapshot(KnownTargetCandidate()));
        var secretCell = notebook.Cells.First(c => c.Kind == RecipeLabCellKind.SecretReference);

        StringAssert.Contains(secretCell.RedactedSummary, "fixture secret alias");
        Assert.IsFalse(secretCell.RedactedSummary.Contains("secret-value", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(secretCell.RedactedSummary.Contains("sk-", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(secretCell.RawSecretValuesShown);
    }

    [TestMethod]
    public void CanonicalReadinessUsesPhaseTwoPolicyPreflightNotFoundationOnly()
    {
        var recipe = ValidRecipe() with { RunLimits = ValidLimits() with { MaxSteps = null } };
        var snapshot = LabSnapshot(recipe, RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun));

        Assert.AreEqual("RecipePolicyPreflightEvaluator", snapshot.Readiness.CanonicalEvaluatorName);
        Assert.IsFalse(snapshot.Readiness.FoundationOnlyReadinessUsedAsCanonical);
        Assert.IsFalse(snapshot.Readiness.IsReady);
        Assert.IsTrue(snapshot.Readiness.BlockingIssues.Any(i => i.IssueId == "missing-max-steps"));
    }

    [TestMethod]
    public void MissingValidationLimitsAndEvidenceAppearAsBlockingIssuesOrCells()
    {
        var recipe = ValidRecipe() with
        {
            RunLimits = null,
            ValidationPolicy = new RecipeValidationPolicy([])
        };
        var snapshot = LabSnapshot(recipe, RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun));
        var notebook = RecipeLabSnapshotFactory.CreateNotebook("notebook.blocked", snapshot, LocatorSnapshot(KnownTargetCandidate()));

        Assert.IsFalse(snapshot.Readiness.IsReady);
        Assert.IsTrue(snapshot.Readiness.BlockingIssues.Any(i => i.Status == RecipeReadinessStatus.BlockedMissingLimits));
        Assert.IsTrue(notebook.Cells.Any(c => c.Kind == RecipeLabCellKind.Preflight && c.Status == RecipeLabCellStatus.Blocked));
        Assert.IsTrue(notebook.Cells.Any(c => c.Kind == RecipeLabCellKind.Evidence && c.EvidenceSummary!.ReferenceOnly));
    }

    [TestMethod]
    public void HumanInterventionApprovalTriggerAndToolTrustStatusesAreVisibleAndSafe()
    {
        var recipe = ValidRecipe();
        var narrative = RecipeApprovalNarrativeFactory.Create("narrative.1", recipe.RecipeId!, recipe.Version!, "run.1", RecipeHumanInterventionKind.PaymentConfirmationRequired);
        var snapshot = LabSnapshot(recipe, RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun), narrative: narrative);
        var notebook = RecipeLabSnapshotFactory.CreateNotebook("notebook.integration", snapshot, LocatorSnapshot(KnownTargetCandidate()));

        Assert.IsTrue(notebook.Cells.Any(c => c.Kind == RecipeLabCellKind.ApprovalNarrative && c.Status == RecipeLabCellStatus.NeedsHuman));
        Assert.IsTrue(notebook.Cells.Any(c => c.Kind == RecipeLabCellKind.TriggerObservation && c.Status == RecipeLabCellStatus.FixtureOnly));
        Assert.IsTrue(notebook.Cells.Any(c => c.Kind == RecipeLabCellKind.ToolTrust && c.Status == RecipeLabCellStatus.ReferenceOnly));
        Assert.IsFalse(snapshot.SafeNextAction.ActionAuthorityGranted);
    }

    [TestMethod]
    public void ApprovalNarrativeCellKeepsDecisionsNarrativeBound()
    {
        var narrative = RecipeApprovalNarrativeFactory.Create("narrative.challenge", "recipe.lab", "7.0.0", "run.1", RecipeHumanInterventionKind.CaptchaOrChallengeDetected);
        var decision = RecipeApprovalDecisionPolicy.Decide("decision.absent", narrative, RecipeApprovalDecisionOption.ApproveFixtureRunOnly);

        Assert.AreEqual(RecipeApprovalDecisionOption.KeepBlocked, decision.Option);
        Assert.AreEqual(RecipeApprovalDecisionStatus.KeptBlocked, decision.Status);
        Assert.IsFalse(decision.LiveRuntimeEnabled);
        Assert.IsFalse(decision.ActionAuthorityGranted);
    }

    [TestMethod]
    public void ToolTrustLiveBlockedStatusAppearsAsBlockedOrFutureGated()
    {
        var entry = RecipeToolTrustEntry.CandidateConnector("tool.live", "Live blocked connector") with
        {
            TrustLevel = RecipeToolTrustLevel.LiveBlocked,
            RuntimeStatus = RecipeToolRuntimeStatus.LiveBlocked
        };
        var registry = new RecipeToolTrustRegistry([entry]);
        var snapshot = RecipeLabSnapshotFactory.Create(
            "snapshot.tool",
            ValidRecipe() with { RequiredToolTrustRefs = [entry.ToolId] },
            RecipeRunMode.FixtureRun,
            RecipePolicyPreflightEvaluator.Evaluate(ValidRecipe(), RecipeRunMode.FixtureRun),
            toolTrustRegistry: registry);

        Assert.IsTrue(entry.IsLiveBlocked);
        var section = snapshot.ViewModel.Sections.Single(s => s.SectionId == "tool-trust");
        Assert.AreEqual(RecipeLabSectionStatus.LiveBlocked, section.Status);
        StringAssert.Contains(section.RedactedSummary, "live-blocked");
        Assert.IsFalse(snapshot.CanUnlockLiveRuntime);
    }

    [TestMethod]
    public void LocatorCandidatesRepresentKnownTargetStableSelectorAndDomAccessibility()
    {
        var candidates = new[]
        {
            Candidate("known", RecipeLocatorStrategy.KnownTarget, RecipeLocatorConfidence.Deterministic),
            Candidate("stable", RecipeLocatorStrategy.StableSelector, RecipeLocatorConfidence.High),
            Candidate("dom", RecipeLocatorStrategy.DomOrAccessibility, RecipeLocatorConfidence.High)
        };

        var snapshot = RecipeLocatorRepairPolicy.CreateSnapshot("locator.snapshot", "recipe.lab", "7.0.0", candidates);

        Assert.AreEqual(3, snapshot.Candidates.Count);
        Assert.IsTrue(snapshot.Candidates.Any(c => c.Strategy == RecipeLocatorStrategy.KnownTarget));
        Assert.IsTrue(snapshot.Candidates.Any(c => c.Strategy == RecipeLocatorStrategy.StableSelector));
        Assert.IsTrue(snapshot.Candidates.Any(c => c.Strategy == RecipeLocatorStrategy.DomOrAccessibility));
        Assert.IsFalse(snapshot.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void RelativeCoordinateLocatorIsWarningLastResortAndCannotApplyLive()
    {
        var candidate = Candidate("relative", RecipeLocatorStrategy.RelativeCoordinate, RecipeLocatorConfidence.Low) with
        {
            FallbackOrder = 8
        };
        var decision = RecipeLocatorRepairPolicy.EvaluateCandidate(candidate, sensitiveAction: false);

        Assert.AreEqual(RecipeLocatorRepairDecisionStatus.BlockedRelativeCoordinate, decision.Status);
        Assert.AreEqual(RecipeLocatorReplayEligibility.ManualReviewOnly, decision.ReplayEligibility);
        Assert.IsFalse(decision.CanApplyLive);
        Assert.IsFalse(decision.CanReplayLive);
    }

    [TestMethod]
    public void AiFallbackLocatorForSensitiveActionIsBlockedOrRequiresHumanPolicy()
    {
        var candidate = Candidate("ai", RecipeLocatorStrategy.AIFallback, RecipeLocatorConfidence.Medium);
        var decision = RecipeLocatorRepairPolicy.EvaluateCandidate(candidate, sensitiveAction: true, aiFallbackAllowedByPolicy: true);

        Assert.AreEqual(RecipeLocatorRepairDecisionStatus.BlockedAIFallback, decision.Status);
        Assert.AreEqual(RecipeLocatorSafetyStatus.BlockedAIFallback, decision.SafetyStatus);
        Assert.IsFalse(decision.ActionAuthorityGranted);
    }

    [TestMethod]
    public void LocatorRepairSuggestionCannotApplyLiveAndReplayIsFixturePreviewOrBlocked()
    {
        var candidate = KnownTargetCandidate();
        var snapshot = RecipeLocatorRepairPolicy.CreateSnapshot("locator.snapshot", "recipe.lab", "7.0.0", [candidate]);

        Assert.AreEqual(RecipeLocatorRepairDecisionStatus.SuggestedPreviewOnly, snapshot.Decision.Status);
        Assert.IsFalse(candidate.RepairSuggestion!.AppliesLive);
        Assert.IsFalse(snapshot.CanApplyLiveRepair);
        Assert.IsFalse(snapshot.CanReplayLive);
        Assert.IsTrue(snapshot.Decision.ReplayEligibility is RecipeLocatorReplayEligibility.FixtureOnly or RecipeLocatorReplayEligibility.PreviewOnly);
    }

    [TestMethod]
    public void BrokenOrAmbiguousLocatorMapsToDriftReportAndSafeNextAction()
    {
        foreach (var drift in new[] { RecipeLocatorDriftStatus.Broken, RecipeLocatorDriftStatus.Ambiguous })
        {
            var candidate = Candidate($"locator.{drift}", RecipeLocatorStrategy.StableSelector, RecipeLocatorConfidence.Low) with
            {
                DriftReport = new RecipeLocatorDriftReport(drift, $"{drift} signal", ["evidence.locator"])
            };
            var snapshot = RecipeLocatorRepairPolicy.CreateSnapshot($"snapshot.{drift}", "recipe.lab", "7.0.0", [candidate]);

            Assert.AreEqual(RecipeLocatorRepairDecisionStatus.RequiresHumanReview, snapshot.Decision.Status, drift.ToString());
            StringAssert.Contains(snapshot.SafeNextAction, "human review");
            Assert.IsFalse(snapshot.Decision.CanApplyLive);
        }
    }

    [TestMethod]
    public void LocatorEvidenceRefsAreReferenceOnly()
    {
        var candidate = KnownTargetCandidate();

        Assert.IsTrue(candidate.ReferenceOnlyEvidence);
        Assert.IsFalse(candidate.DriftReport.UsesLiveObservation);
        Assert.AreEqual(RecipeEvidenceRedactionStatus.Applied, candidate.RedactionStatus);
    }

    [TestMethod]
    public void BrowserAndDesktopLiveActionsRemainBlockedInLab()
    {
        foreach (var category in new[] { SensitiveActionCategory.BrowserLiveAction, SensitiveActionCategory.DesktopLiveAction })
        {
            var recipe = ValidRecipe() with
            {
                RuntimeRiskProfile = ValidRiskProfile() with { SensitiveCategories = new HashSet<SensitiveActionCategory> { category } }
            };
            var snapshot = LabSnapshot(recipe, RecipePolicyPreflightEvaluator.Evaluate(recipe, RecipeRunMode.FixtureRun));

            Assert.IsFalse(snapshot.Readiness.IsReady, category.ToString());
            Assert.IsTrue(snapshot.Readiness.BlockingIssues.Any(i => i.Status == RecipeReadinessStatus.BlockedLiveRuntimeDisabled), category.ToString());
            Assert.IsFalse(snapshot.CanUnlockLiveRuntime);
        }
    }

    [TestMethod]
    public void HandoffAndLabSummaryExcludeRawSecretsPayloadsAndLiveCapture()
    {
        var snapshot = LabSnapshot(ValidRecipe(), RecipePolicyPreflightEvaluator.Evaluate(ValidRecipe(), RecipeRunMode.FixtureRun));

        CollectionAssert.Contains(snapshot.OperatorSummary.RawDataOmitted.ToArray(), "raw secrets");
        CollectionAssert.Contains(snapshot.OperatorSummary.RawDataOmitted.ToArray(), "raw payloads");
        Assert.IsFalse(snapshot.RawSecretValuesExposed);
        Assert.IsFalse(snapshot.RedactionSafetySummary.Contains("secret-value", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(snapshot.RedactionSafetySummary.Contains("sk-", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NoRealBrowserDesktopCdpConnectorVaultSchedulerWatcherHookListenerNetworkRecorderReplayOrAutorun()
    {
        var snapshot = LabSnapshot(ValidRecipe(), RecipePolicyPreflightEvaluator.Evaluate(ValidRecipe(), RecipeRunMode.FixtureRun));
        var locator = RecipeLocatorRepairPolicy.CreateSnapshot("locator.snapshot", "recipe.lab", "7.0.0", [KnownTargetCandidate()]);
        var notebook = RecipeLabSnapshotFactory.CreateNotebook("notebook.safety", snapshot, locator);

        Assert.IsFalse(snapshot.CanStartRecipeRun);
        Assert.IsFalse(snapshot.CanProcessWorkitems);
        Assert.IsFalse(snapshot.LiveRuntimeEnabled);
        Assert.IsFalse(locator.CanApplyLiveRepair);
        Assert.IsFalse(locator.CanReplayLive);
        Assert.IsFalse(notebook.CanExecuteCells);
        Assert.IsFalse(notebook.CanApplyLocatorRepair);
        Assert.IsFalse(notebook.CanStartRecipeRun);
    }

    private static RecipeLabSnapshot LabSnapshot(
        RecipeDefinition recipe,
        RecipePolicyPreflightResult readiness,
        RecipeApprovalNarrative? narrative = null) =>
        RecipeLabSnapshotFactory.Create(
            "snapshot.lab",
            recipe,
            RecipeRunMode.FixtureRun,
            readiness,
            EvidencePack(),
            TimelineProjection(),
            narrative,
            ToolRegistry(),
            [SecretRef()],
            TriggerReadiness(),
            LocatorSnapshot(KnownTargetCandidate()));

    private static RecipeDefinition ValidRecipe() =>
        new("recipe-lab")
        {
            RecipeId = "recipe.lab",
            DisplayName = "Recipe Lab fixture",
            Version = "7.0.0",
            Category = "Fixture",
            SystemTarget = "fixture-system",
            RegionCountry = "AR",
            RequiredToolTrustRefs = ["tool.fixture"],
            RequiredSecretRefs = ["secret.fixture"],
            TriggerRefs = ["trigger.fixture"],
            DetectorRefs = ["detector.fixture"],
            OutputSchemaRef = "schema.output",
            Blocks = [Block("submit", RecipeBlockType.BrowserAction, "submit fixture")],
            RunLimits = ValidLimits(),
            CompleteCriteria = new RecipeCompleteCriteria([new("complete", RecipeCompleteCriterionType.ExpectedOutputExists, "output.ref")]),
            TerminateCriteria = new RecipeTerminateCriteria([
                new("policy", RecipeTerminateCriterionType.PolicyBlocked, "policy.ref"),
                new("unknown", RecipeTerminateCriterionType.UnknownUnsafeState, "unknown.ref")
            ]),
            ValidationPolicy = new RecipeValidationPolicy([
                new("submit-validation", RecipeValidationKind.VisibleTextExists, RecipeValidationSeverity.Blocking, AppliesToBlockId: "submit", PostValidation: true),
                new("evidence-validation", RecipeValidationKind.EvidenceRefExists, RecipeValidationSeverity.Blocking, AppliesToBlockId: "submit", PostValidation: true)
            ]),
            RuntimeRiskProfile = ValidRiskProfile(),
            ActionResolutionPolicy = new ActionResolutionPolicy([
                new ActionResolutionAttempt(1, ActionResolutionStrategy.KnownTarget, "target.fixture", "evidence.target"),
                new ActionResolutionAttempt(2, ActionResolutionStrategy.StableSelector, "selector.fixture", "evidence.selector")
            ]),
            EvidenceExpectationRefs = ["evidence.expected"],
            ApprovalCheckpointRefs = ["approval.checkpoint"]
        };

    private static RecipeRunLimits ValidLimits() =>
        new(MaxSteps: 10, MaxRuntimeSeconds: 60, MaxRetries: 1, MaxLoopIterations: 2, MaxNestedLoops: 1, LiveRuntimeAllowed: false);

    private static RecipeRiskProfile ValidRiskProfile() =>
        new(
            "risk.fixture",
            RecipeRiskLevel.Low,
            new HashSet<SensitiveActionCategory>(),
            [],
            ApprovalPolicyPresent: true,
            HumanInterventionPathPresent: true,
            SecretRefs: ["secret.fixture"]);

    private static RecipeBlock Block(string id, RecipeBlockType type, string intent) =>
        new(id, type, id, intent, "target.ref", "input.ref", "output.ref", [], [], ["validation.ref"], RecipeRiskLevel.Low, RecipeApprovalRequirement.None, "evidence.ref", "failure.ref", []);

    private static RecipeEvidencePack EvidencePack() =>
        new(
            "evidence.pack",
            "recipe.lab",
            "7.0.0",
            "run.1",
            null,
            WorkitemRefs: ["workitem.ref"],
            StepEvidenceRefs: ["step.evidence"],
            ValidationEvidenceRefs: ["validation.evidence"],
            ApprovalRefs: ["approval.ref"],
            TimelineEventRefs: ["timeline.ref"],
            ArtifactRefs: ["artifact.ref"],
            RedactionReportRef: "redaction.report",
            RecipeEvidenceSensitivity.Confidential,
            RecipeEvidenceCompleteness.Complete,
            RecipeEvidenceCaptureMode.ReferenceOnly,
            CreatedAt: null,
            RecipeRunMode.FixtureRun,
            FailureSummary: null,
            SafeRedaction());

    private static RecipeTimelineProjection TimelineProjection() =>
        new(
            "timeline.projection",
            "run.1",
            "recipe.lab",
            RecipeTimelineProjectionStatus.Projected,
            [],
            "redacted timeline summary",
            RedactionApplied: true);

    private static RecipeEvidenceRedactionSummary SafeRedaction() =>
        new(
            RedactionApplied: true,
            RedactionPolicyRef: "redaction.policy",
            SensitiveFields: [],
            SecretRefs: ["secret.fixture"],
            RecipeEvidenceSecretHandlingStatus.SecretRefsOnly,
            RawPayloadExposed: false,
            EvidenceSafeForHandoff: true,
            EvidenceSafeForTimeline: true);

    private static RecipeToolTrustRegistry ToolRegistry() =>
        new([RecipeToolTrustEntry.CandidateConnector("tool.fixture", "Fixture connector") with
        {
            TrustLevel = RecipeToolTrustLevel.ApprovedForFixture,
            RuntimeStatus = RecipeToolRuntimeStatus.FixtureOnly
        }]);

    private static RecipeSecretRef SecretRef() =>
        new(
            "secret.fixture",
            "fixture secret alias",
            RecipeSecretKind.ApiKey,
            RecipeSecretScope.Tool,
            "tool.fixture",
            RecipeSecretPresenceStatus.PresentByReference,
            "rotation.ref",
            "verified.ref",
            "redaction.policy",
            [RecipeRunMode.FixtureRun],
            [RecipeRunMode.LiveRunBlocked, RecipeRunMode.LiveRunAllowedFuture],
            "Secret present by reference only.",
            RawValuePresent: false);

    private static RecipeTriggerReadiness TriggerReadiness() =>
        new(
            true,
            RecipeTriggerStatus.ObserveOnlyReady,
            new RecipeTriggerDecision(RecipeTriggerRunMode.CreateObservationOnly, RecipeTriggerBlockedReason.None, "observe-only"),
            BlockingIssues: [],
            Warnings: []);

    private static RecipeLocatorStudioSnapshot LocatorSnapshot(RecipeLocatorCandidate candidate) =>
        RecipeLocatorRepairPolicy.CreateSnapshot("locator.snapshot", "recipe.lab", "7.0.0", [candidate]);

    private static RecipeLocatorCandidate KnownTargetCandidate() =>
        Candidate("known.target", RecipeLocatorStrategy.KnownTarget, RecipeLocatorConfidence.Deterministic);

    private static RecipeLocatorCandidate Candidate(string id, RecipeLocatorStrategy strategy, RecipeLocatorConfidence confidence) =>
        new(
            id,
            "recipe.lab",
            "7.0.0",
            "block.1",
            "step.1",
            strategy,
            "[REDACTED] selector summary",
            confidence,
            ["evidence.locator"],
            new RecipeLocatorDriftReport(RecipeLocatorDriftStatus.Stable, "stable fixture signal", ["evidence.locator"]),
            new RecipeLocatorRepairSuggestion($"repair.{id}", strategy, "[REDACTED] repaired selector", "fixture suggestion", ["evidence.locator"], AppliesLive: false),
            RecipeLocatorSafetyStatus.SafeForPreview,
            RecipeLocatorReplayEligibility.FixtureOnly,
            ApprovalRequired: false,
            HumanReviewRequired: false,
            RecipeEvidenceRedactionStatus.Applied,
            PreferredOrRejectedReason: "fixture candidate",
            FallbackOrder: 1,
            LastKnownSafeObservationRef: "observation.fixture");
}
