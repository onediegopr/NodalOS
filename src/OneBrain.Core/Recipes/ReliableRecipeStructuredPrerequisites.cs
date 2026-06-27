namespace OneBrain.Core.Recipes;

public sealed record ReliableRecipeStructuredPrerequisiteProfile(
    string ProfileId,
    string SubjectId,
    ReliableRecipeStructuredPrerequisiteSubjectKind SubjectKind,
    IReadOnlyList<StructuredEvidenceRequirement> EvidenceRequirements,
    IReadOnlyList<StructuredValidationRequirement> ValidationRequirements,
    ReliableRecipeRequirementSource RequirementSource,
    ReliableRecipeStructuredCompletenessReport CompletenessReport,
    ReliableRecipeStructuredAdapterGateDecision AdapterGateDecision,
    string NoRuntimeNotice)
{
    public bool ReadOnly => true;
    public bool FixtureOnly => true;
    public bool RuntimeEnabled => false;
    public bool AdapterRuntimeAdded => false;
}

public enum ReliableRecipeStructuredPrerequisiteSubjectKind
{
    Recipe,
    RecipeBlock,
    RecorderDraft,
    EvalScenario,
    SandboxReadinessScenario,
    DryRunAdapterReadinessScenario
}

public enum ReliableRecipeRequirementSource
{
    Explicit,
    FixtureExplicit,
    MappedFromLegacyContract,
    InferredFromBlockKind,
    InferredFromLabel,
    Missing
}

public sealed record StructuredEvidenceRequirement(
    string RequirementId,
    string TargetBlockId,
    StructuredEvidenceRequirementKind Kind,
    IReadOnlyList<ReliableRecipeRunMode> RequiredForModes,
    IReadOnlyList<ReliableRecipeRiskProfile> RequiredForRiskLevels,
    bool IsCritical,
    ReliableRecipeRequirementSource Source,
    string Description,
    ReliableRecipeRequirementMissingBehavior MissingBehavior,
    bool RedactionRequired,
    ReliableRecipeRequirementAdapterGateImpact AdapterGateImpact);

public enum StructuredEvidenceRequirementKind
{
    BeforeState,
    AfterState,
    ActionProposal,
    ActionResult,
    ValidationReport,
    TimelineEvent,
    ApprovalDecision,
    HumanIntervention,
    PerceptionSnapshot,
    OcrSupportingSignalRef,
    SandboxReadinessReport,
    EvalScenarioReport,
    DownloadArtifactRef,
    ExtractedDataSchema,
    RedactionReport,
    RollbackOrRestorePlan,
    PolicyDecision,
    SecretHandlingReport
}

public sealed record StructuredValidationRequirement(
    string RequirementId,
    string TargetBlockId,
    StructuredValidationRequirementKind Kind,
    IReadOnlyList<ReliableRecipeRunMode> RequiredForModes,
    IReadOnlyList<ReliableRecipeRiskProfile> RequiredForRiskLevels,
    bool IsCritical,
    ReliableRecipeRequirementSource Source,
    string ExpectedAssertion,
    ReliableRecipeRequirementMissingBehavior FailureBehavior,
    ReliableRecipeRequirementAdapterGateImpact AdapterGateImpact);

public enum StructuredValidationRequirementKind
{
    VisibleTextAssertion,
    ElementStateAssertion,
    UrlOrRouteAssertion,
    FileDownloadedAssertion,
    DataExtractedAssertion,
    SchemaMatchAssertion,
    FieldValueAssertion,
    ModalStateAssertion,
    LoopTerminationAssertion,
    ExternalSideEffectConfirmation,
    HumanConfirmation,
    PolicyDecisionAssertion,
    PerceptionConfidenceAssertion,
    SandboxReadinessAssertion,
    EvalExpectedOutcomeAssertion
}

public enum ReliableRecipeRequirementMissingBehavior
{
    Warn,
    BlockDryRun,
    BlockAdapterReadiness,
    BlockAllNonDraftModes,
    RequireHumanReview
}

public enum ReliableRecipeRequirementAdapterGateImpact
{
    None,
    Warning,
    BlocksFutureAdapter,
    BlocksDryRunCandidate,
    RequiresExternalAudit
}

public enum ReliableRecipeStructuredAdapterGateDecision
{
    PassDesignOnly,
    PassWithWarnings,
    BlockedMissingCriticalRequirements,
    BlockedInferredCriticalRequirements,
    BlockedByPolicy
}

public sealed record ReliableRecipeStructuredCompletenessReport(
    double EvidenceCompletenessScore,
    double ValidationCompletenessScore,
    double ExplicitRequirementRatio,
    double InferredRequirementRatio,
    IReadOnlyList<string> MissingCriticalRequirements,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> BlockingFindings,
    ReliableRecipeRequirementAdapterGateImpact AdapterReadinessImpact)
{
    public bool EvidenceGateSatisfied =>
        !MissingCriticalRequirements.Any(m => m.Contains("evidence", StringComparison.OrdinalIgnoreCase)) &&
        AdapterReadinessImpact is not ReliableRecipeRequirementAdapterGateImpact.BlocksFutureAdapter and not ReliableRecipeRequirementAdapterGateImpact.BlocksDryRunCandidate;

    public bool ValidationGateSatisfied =>
        !MissingCriticalRequirements.Any(m => m.Contains("validation", StringComparison.OrdinalIgnoreCase)) &&
        AdapterReadinessImpact is not ReliableRecipeRequirementAdapterGateImpact.BlocksFutureAdapter and not ReliableRecipeRequirementAdapterGateImpact.BlocksDryRunCandidate;
}

public sealed record ReliableRecipeStructuredPrerequisiteScenario(
    string ScenarioId,
    ReliableRecipeStructuredPrerequisiteSubjectKind SubjectKind,
    ReliableRecipeDefinition Recipe,
    ReliableRecipeStructuredAdapterGateDecision ExpectedDecision,
    string Summary)
{
    public bool FixtureOnly => true;
    public bool RuntimeEnabled => false;
    public bool UsesLiveBrowser => false;
    public bool UsesLiveDesktop => false;
    public bool UsesNetwork => false;
    public bool StoresSecrets => false;
}

public sealed record ReliableRecipeStructuredPrerequisiteSummary(
    double EvidenceCompletenessScore,
    double ValidationCompletenessScore,
    double ExplicitRequirementRatio,
    double InferredRequirementRatio,
    IReadOnlyList<string> MissingCriticalRequirements,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> BlockingFindings,
    ReliableRecipeRequirementAdapterGateImpact AdapterReadinessImpact,
    string NoRuntimeNotice)
{
    public bool ReadOnly => true;
    public bool RuntimeEnabled => false;
    public bool AdapterRuntimeEnabled => false;
}

public sealed record ReliableRecipeLabStructuredPrerequisitesPanel(
    double EvidenceCompletenessScore,
    double ValidationCompletenessScore,
    double ExplicitRequirementRatio,
    double InferredRequirementRatio,
    IReadOnlyList<string> MissingCriticalRequirements,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> BlockingFindings,
    string AdapterReadinessImpact,
    string NoRuntimeNotice,
    IReadOnlyList<string> ReadOnlyActionLabels)
{
    public bool ReadOnly => true;
    public bool FixtureOnly => true;
    public bool RuntimeActionExposed => false;
    public bool CanExecute => false;
    public bool CanEnableAdapter => false;
}

public static class ReliableRecipeStructuredPrerequisiteScenarioCatalog
{
    public static IReadOnlyList<ReliableRecipeStructuredPrerequisiteScenario> All() =>
    [
        Scenario("complete_explicit_invoice_download_prerequisites", CompleteExplicitInvoiceDownload(), ReliableRecipeStructuredAdapterGateDecision.PassDesignOnly, "Explicit evidence and validation requirements pass design readiness."),
        Scenario("inferred_download_requirements_warn", InferredDownloadWarning(), ReliableRecipeStructuredAdapterGateDecision.PassWithWarnings, "Download wording inferred from label creates warning only."),
        Scenario("missing_download_artifact_evidence_blocks", MissingDownloadArtifactEvidence(), ReliableRecipeStructuredAdapterGateDecision.BlockedMissingCriticalRequirements, "Download artifact evidence is missing."),
        Scenario("submit_without_explicit_post_validation_blocks", SubmitWithoutExplicitPostValidation(), ReliableRecipeStructuredAdapterGateDecision.BlockedMissingCriticalRequirements, "Submit action lacks explicit post validation."),
        Scenario("external_side_effect_missing_approval_evidence_blocks", ExternalSideEffectMissingApprovalEvidence(), ReliableRecipeStructuredAdapterGateDecision.BlockedMissingCriticalRequirements, "External side effect lacks approval and policy evidence."),
        Scenario("ocr_only_sensitive_missing_perception_validation_blocks", OcrOnlySensitiveMissingPerceptionValidation(), ReliableRecipeStructuredAdapterGateDecision.BlockedMissingCriticalRequirements, "OCR-only sensitive target lacks perception validation."),
        Scenario("recorder_draft_missing_human_review_evidence_blocks", RecorderDraftMissingHumanReviewEvidence(), ReliableRecipeStructuredAdapterGateDecision.BlockedMissingCriticalRequirements, "Recorder draft lacks human review evidence."),
        Scenario("captcha_handoff_missing_human_intervention_evidence_blocks", CaptchaMissingHumanInterventionEvidence(), ReliableRecipeStructuredAdapterGateDecision.BlockedMissingCriticalRequirements, "Challenge handoff lacks human intervention evidence."),
        Scenario("sandbox_future_missing_readiness_assertion_blocks", SandboxFutureMissingReadinessAssertion(), ReliableRecipeStructuredAdapterGateDecision.BlockedMissingCriticalRequirements, "Sandbox future block lacks readiness assertion."),
        Scenario("eval_scenario_missing_expected_outcome_assertion_blocks", EvalScenarioMissingExpectedOutcomeAssertion(), ReliableRecipeStructuredAdapterGateDecision.BlockedMissingCriticalRequirements, "Eval scenario lacks expected outcome assertion."),
        Scenario("high_risk_with_explicit_requirements_still_policy_blocked", HighRiskExplicitStillBlocked(), ReliableRecipeStructuredAdapterGateDecision.BlockedByPolicy, "Explicit requirements do not override high-risk policy."),
        Scenario("legacy_mapped_requirements_pass_with_warning", LegacyMappedRequirements(), ReliableRecipeStructuredAdapterGateDecision.PassWithWarnings, "Legacy refs map to structured requirements with warning.")
    ];

    public static ReliableRecipeStructuredPrerequisiteScenario Get(string scenarioId) =>
        All().Single(s => s.ScenarioId == scenarioId);

    private static ReliableRecipeStructuredPrerequisiteScenario Scenario(string scenarioId, ReliableRecipeDefinition recipe, ReliableRecipeStructuredAdapterGateDecision decision, string summary) =>
        new(scenarioId, ReliableRecipeStructuredPrerequisiteSubjectKind.Recipe, recipe, decision, summary);

    private static ReliableRecipeDefinition CompleteExplicitInvoiceDownload() =>
        Base("complete_explicit_invoice_download_prerequisites", ReliableRecipeRiskProfile.ReadOnly, [
            Block("download", ReliableRecipeBlockKind.FileDownloadEvidence, ReliableRecipeRiskProfile.LocalWrite,
                ExplicitEvidence("before", "after", "proposal", "result", "validation", "timeline", "download-artifact", "policy"),
                ExplicitValidation("file-downloaded", "timeline", "policy"))
        ]);

    private static ReliableRecipeDefinition InferredDownloadWarning() =>
        Base("inferred_download_requirements_warn", ReliableRecipeRiskProfile.ReadOnly, [
            new ReliableRecipeBlock("preview", ReliableRecipeBlockKind.Wait, "Download invoice preview label", new Dictionary<string, string>(), [], [], ReliableRecipeRiskProfile.ReadOnly, [], [])
        ]);

    private static ReliableRecipeDefinition MissingDownloadArtifactEvidence() =>
        Base("missing_download_artifact_evidence_blocks", ReliableRecipeRiskProfile.LocalWrite, [
            Block("download", ReliableRecipeBlockKind.FileDownloadEvidence, ReliableRecipeRiskProfile.LocalWrite,
                ExplicitEvidence("before", "after", "proposal", "result", "validation", "timeline"),
                ExplicitValidation("file-downloaded", "timeline"))
        ]);

    private static ReliableRecipeDefinition SubmitWithoutExplicitPostValidation() =>
        Base("submit_without_explicit_post_validation_blocks", ReliableRecipeRiskProfile.ExternalSideEffect, [
            Block("submit", ReliableRecipeBlockKind.BrowserAction, ReliableRecipeRiskProfile.ExternalSideEffect,
                ExplicitEvidence("before", "after", "proposal", "result", "validation", "timeline", "approval", "policy", "perception"),
                [])
        ]);

    private static ReliableRecipeDefinition ExternalSideEffectMissingApprovalEvidence() =>
        Base("external_side_effect_missing_approval_evidence_blocks", ReliableRecipeRiskProfile.ExternalSideEffect, [
            Block("submit", ReliableRecipeBlockKind.BrowserAction, ReliableRecipeRiskProfile.ExternalSideEffect,
                ExplicitEvidence("before", "after", "proposal", "result", "validation", "timeline", "perception"),
                ExplicitValidation("visible", "external-confirmation", "policy"))
        ]);

    private static ReliableRecipeDefinition OcrOnlySensitiveMissingPerceptionValidation() =>
        Base("ocr_only_sensitive_missing_perception_validation_blocks", ReliableRecipeRiskProfile.ExternalSideEffect | ReliableRecipeRiskProfile.SensitiveData, [
            Block("submit", ReliableRecipeBlockKind.BrowserAction, ReliableRecipeRiskProfile.ExternalSideEffect | ReliableRecipeRiskProfile.SensitiveData,
                ExplicitEvidence("before", "after", "proposal", "result", "validation", "timeline", "ocr-supporting"),
                ExplicitValidation("visible", "policy"))
        ]);

    private static ReliableRecipeDefinition RecorderDraftMissingHumanReviewEvidence() =>
        Base("recorder_draft_missing_human_review_evidence_blocks", ReliableRecipeRiskProfile.ReadOnly, [
            Block("draft", ReliableRecipeBlockKind.BrowserAction, ReliableRecipeRiskProfile.ReadOnly,
                ExplicitEvidence("before", "after", "proposal", "result", "validation", "timeline", "perception"),
                ExplicitValidation("visible", "timeline"))
        ]) with { CreatedFrom = ReliableRecipeCreatedFrom.RecorderDraft, Readiness = ReliableRecipeReadiness.DesignOnly };

    private static ReliableRecipeDefinition CaptchaMissingHumanInterventionEvidence() =>
        Base("captcha_handoff_missing_human_intervention_evidence_blocks", ReliableRecipeRiskProfile.Credentialed, [
            Block("handoff", ReliableRecipeBlockKind.HumanIntervention, ReliableRecipeRiskProfile.Credentialed,
                ExplicitEvidence("before", "after", "proposal", "result", "validation", "timeline", "redaction", "secret-handling"),
                ExplicitValidation("human-confirmation", "timeline"))
        ]);

    private static ReliableRecipeDefinition SandboxFutureMissingReadinessAssertion() =>
        Base("sandbox_future_missing_readiness_assertion_blocks", ReliableRecipeRiskProfile.ExternalSideEffect, [
            Block("sandbox", ReliableRecipeBlockKind.SandboxFuture, ReliableRecipeRiskProfile.ExternalSideEffect,
                ExplicitEvidence("before", "after", "proposal", "result", "validation", "timeline", "policy"),
                ExplicitValidation("human-confirmation", "policy"))
        ]);

    private static ReliableRecipeDefinition EvalScenarioMissingExpectedOutcomeAssertion() =>
        Base("eval_scenario_missing_expected_outcome_assertion_blocks", ReliableRecipeRiskProfile.ReadOnly, [
            Block("eval", ReliableRecipeBlockKind.Validate, ReliableRecipeRiskProfile.ReadOnly,
                ExplicitEvidence("before", "after", "validation", "timeline", "eval-report"),
                ExplicitValidation("timeline"))
        ]);

    private static ReliableRecipeDefinition HighRiskExplicitStillBlocked() =>
        Base("high_risk_with_explicit_requirements_still_policy_blocked", ReliableRecipeRiskProfile.Financial | ReliableRecipeRiskProfile.ExternalSideEffect, [
            Block("payment", ReliableRecipeBlockKind.BrowserAction, ReliableRecipeRiskProfile.Financial | ReliableRecipeRiskProfile.ExternalSideEffect,
                ExplicitEvidence("before", "after", "proposal", "result", "validation", "timeline", "approval", "policy", "perception", "redaction", "secret-handling"),
                ExplicitValidation("visible", "external-confirmation", "policy", "perception-confidence"))
        ]);

    private static ReliableRecipeDefinition LegacyMappedRequirements() =>
        Base("legacy_mapped_requirements_pass_with_warning", ReliableRecipeRiskProfile.ReadOnly, [
            Block("download", ReliableRecipeBlockKind.FileDownloadEvidence, ReliableRecipeRiskProfile.LocalWrite,
                ["evidence.before", "evidence.after", "evidence.proposal", "evidence.result", "evidence.validation", "evidence.timeline", "evidence.download", "evidence.policy"],
                ["validation.download", "validation.timeline", "validation.policy"])
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

    private static IReadOnlyList<string> ExplicitEvidence(params string[] kinds) =>
        kinds.Select(k => $"fixture.structured.evidence.{k}").ToArray();

    private static IReadOnlyList<string> ExplicitValidation(params string[] kinds) =>
        kinds.Select(k => $"fixture.structured.validation.{k}").ToArray();
}

public static class ReliableRecipeStructuredPrerequisiteEvaluator
{
    public static ReliableRecipeStructuredPrerequisiteProfile Evaluate(ReliableRecipeStructuredPrerequisiteScenario scenario) =>
        Evaluate(scenario.Recipe, subjectKind: scenario.SubjectKind, subjectId: scenario.ScenarioId);

    public static ReliableRecipeStructuredPrerequisiteProfile Evaluate(
        ReliableRecipeDefinition recipe,
        ReliableRecipePreflightReport? preflightReport = null,
        RecorderToRecipeDraft? draft = null,
        ReliableRecipeFixtureEvalRun? evalRun = null,
        ComputerUseSandboxReadinessReport? sandboxReport = null,
        ReliableRecipePerceptionIntegrationReport? perceptionReport = null,
        ReliableRecipeStructuredPrerequisiteSubjectKind subjectKind = ReliableRecipeStructuredPrerequisiteSubjectKind.Recipe,
        string? subjectId = null)
    {
        var evidence = recipe.Blocks.SelectMany(b => EvidenceRequirementsFor(b, recipe, preflightReport, draft, evalRun, sandboxReport, perceptionReport)).ToArray();
        var validation = recipe.Blocks.SelectMany(b => ValidationRequirementsFor(b, recipe, preflightReport, draft, evalRun, sandboxReport, perceptionReport)).ToArray();
        var report = Completeness(evidence, validation, recipe);
        var source = DominantSource(evidence.Select(e => e.Source).Concat(validation.Select(v => v.Source)).ToArray());
        var decision = DecisionFor(report, recipe);

        return new ReliableRecipeStructuredPrerequisiteProfile(
            $"structured-prerequisites.{subjectId ?? recipe.Id}",
            subjectId ?? recipe.Id,
            subjectKind,
            evidence,
            validation,
            source,
            report,
            decision,
            "Structured prerequisite report only. Runtime not enabled; no adapter or evidence capture is present.");
    }

    public static ReliableRecipeStructuredPrerequisiteSummary ToSummary(ReliableRecipeStructuredPrerequisiteProfile profile) =>
        new(
            profile.CompletenessReport.EvidenceCompletenessScore,
            profile.CompletenessReport.ValidationCompletenessScore,
            profile.CompletenessReport.ExplicitRequirementRatio,
            profile.CompletenessReport.InferredRequirementRatio,
            profile.CompletenessReport.MissingCriticalRequirements,
            profile.CompletenessReport.Warnings,
            profile.CompletenessReport.BlockingFindings,
            profile.CompletenessReport.AdapterReadinessImpact,
            profile.NoRuntimeNotice);

    private static IReadOnlyList<StructuredEvidenceRequirement> EvidenceRequirementsFor(
        ReliableRecipeBlock block,
        ReliableRecipeDefinition recipe,
        ReliableRecipePreflightReport? preflightReport,
        RecorderToRecipeDraft? draft,
        ReliableRecipeFixtureEvalRun? evalRun,
        ComputerUseSandboxReadinessReport? sandboxReport,
        ReliableRecipePerceptionIntegrationReport? perceptionReport)
    {
        var expected = ExpectedEvidenceKinds(block, recipe, preflightReport, draft, evalRun, sandboxReport, perceptionReport).Distinct().ToArray();
        var provided = block.EvidenceExpectations.SelectMany(e => MapEvidence(block.Id, e)).ToArray();
        var requirements = new List<StructuredEvidenceRequirement>();

        foreach (var kind in expected)
        {
            var match = provided.FirstOrDefault(p => p.Kind == kind);
            if (match is not null)
            {
                requirements.Add(match);
                continue;
            }

            var inferredSource = InferEvidenceSource(block, kind);
            requirements.Add(Evidence(
                $"structured.evidence.{block.Id}.{kind}".ToLowerInvariant(),
                block.Id,
                kind,
                IsCriticalEvidence(block, recipe, kind),
                inferredSource,
                $"Structured evidence required for {kind}.",
                MissingBehavior(block, recipe, kind),
                RedactionRequired(recipe, kind),
                GateImpact(block, recipe, kind, inferredSource)));
        }

        requirements.AddRange(provided.Where(p => !expected.Contains(p.Kind)));
        return requirements.DistinctBy(r => (r.TargetBlockId, r.Kind, r.Source)).ToArray();
    }

    private static IReadOnlyList<StructuredValidationRequirement> ValidationRequirementsFor(
        ReliableRecipeBlock block,
        ReliableRecipeDefinition recipe,
        ReliableRecipePreflightReport? preflightReport,
        RecorderToRecipeDraft? draft,
        ReliableRecipeFixtureEvalRun? evalRun,
        ComputerUseSandboxReadinessReport? sandboxReport,
        ReliableRecipePerceptionIntegrationReport? perceptionReport)
    {
        var expected = ExpectedValidationKinds(block, recipe, preflightReport, draft, evalRun, sandboxReport, perceptionReport).Distinct().ToArray();
        var provided = block.ValidationRequirements.SelectMany(v => MapValidation(block.Id, v)).ToArray();
        var requirements = new List<StructuredValidationRequirement>();

        foreach (var kind in expected)
        {
            var match = provided.FirstOrDefault(p => p.Kind == kind);
            if (match is not null)
            {
                requirements.Add(match);
                continue;
            }

            var inferredSource = InferValidationSource(block, kind);
            requirements.Add(Validation(
                $"structured.validation.{block.Id}.{kind}".ToLowerInvariant(),
                block.Id,
                kind,
                IsCriticalValidation(block, recipe, kind),
                inferredSource,
                $"Structured validation required for {kind}.",
                MissingBehavior(block, recipe, kind),
                GateImpact(block, recipe, kind, inferredSource)));
        }

        requirements.AddRange(provided.Where(p => !expected.Contains(p.Kind)));
        return requirements.DistinctBy(r => (r.TargetBlockId, r.Kind, r.Source)).ToArray();
    }

    private static IReadOnlyList<StructuredEvidenceRequirementKind> ExpectedEvidenceKinds(
        ReliableRecipeBlock block,
        ReliableRecipeDefinition recipe,
        ReliableRecipePreflightReport? preflightReport,
        RecorderToRecipeDraft? draft,
        ReliableRecipeFixtureEvalRun? evalRun,
        ComputerUseSandboxReadinessReport? sandboxReport,
        ReliableRecipePerceptionIntegrationReport? perceptionReport)
    {
        var expected = new List<StructuredEvidenceRequirementKind>();
        if (IsCriticalBlock(block, recipe))
            expected.AddRange([StructuredEvidenceRequirementKind.BeforeState, StructuredEvidenceRequirementKind.AfterState, StructuredEvidenceRequirementKind.ActionProposal, StructuredEvidenceRequirementKind.ActionResult, StructuredEvidenceRequirementKind.ValidationReport, StructuredEvidenceRequirementKind.TimelineEvent]);
        if (block.Kind == ReliableRecipeBlockKind.FileDownloadEvidence)
            expected.Add(StructuredEvidenceRequirementKind.DownloadArtifactRef);
        if (block.Kind is ReliableRecipeBlockKind.BrowserAction or ReliableRecipeBlockKind.Extract)
            expected.Add(StructuredEvidenceRequirementKind.PerceptionSnapshot);
        if (block.Kind == ReliableRecipeBlockKind.HumanIntervention || recipe.CreatedFrom == ReliableRecipeCreatedFrom.RecorderDraft || draft is not null)
            expected.Add(StructuredEvidenceRequirementKind.HumanIntervention);
        if (block.Kind is ReliableRecipeBlockKind.SandboxFuture)
            expected.Add(StructuredEvidenceRequirementKind.SandboxReadinessReport);
        if (block.Kind == ReliableRecipeBlockKind.Validate)
            expected.Add(StructuredEvidenceRequirementKind.EvalScenarioReport);
        if (block.Label.Contains("download", StringComparison.OrdinalIgnoreCase))
            expected.Add(StructuredEvidenceRequirementKind.DownloadArtifactRef);
        if (RequiresApproval(recipe, block) || preflightReport?.RequiredApprovals.Count > 0)
            expected.AddRange([StructuredEvidenceRequirementKind.ApprovalDecision, StructuredEvidenceRequirementKind.PolicyDecision]);
        if (recipe.RiskProfile.HasFlag(ReliableRecipeRiskProfile.Credentialed) || recipe.RiskProfile.HasFlag(ReliableRecipeRiskProfile.SensitiveData))
            expected.AddRange([StructuredEvidenceRequirementKind.RedactionReport, StructuredEvidenceRequirementKind.SecretHandlingReport]);
        if (perceptionReport?.OverallDecision is ReliableRecipePerceptionDecision.BlockedSensitiveActionAuthority or ReliableRecipePerceptionDecision.NeedsHumanReview)
            expected.Add(StructuredEvidenceRequirementKind.OcrSupportingSignalRef);
        return expected;
    }

    private static IReadOnlyList<StructuredValidationRequirementKind> ExpectedValidationKinds(
        ReliableRecipeBlock block,
        ReliableRecipeDefinition recipe,
        ReliableRecipePreflightReport? preflightReport,
        RecorderToRecipeDraft? draft,
        ReliableRecipeFixtureEvalRun? evalRun,
        ComputerUseSandboxReadinessReport? sandboxReport,
        ReliableRecipePerceptionIntegrationReport? perceptionReport)
    {
        var expected = new List<StructuredValidationRequirementKind>();
        expected.AddRange(block.Kind switch
        {
            ReliableRecipeBlockKind.BrowserAction => [StructuredValidationRequirementKind.VisibleTextAssertion, StructuredValidationRequirementKind.PolicyDecisionAssertion],
            ReliableRecipeBlockKind.FileDownloadEvidence => [StructuredValidationRequirementKind.FileDownloadedAssertion, StructuredValidationRequirementKind.PolicyDecisionAssertion],
            ReliableRecipeBlockKind.Extract => [StructuredValidationRequirementKind.DataExtractedAssertion, StructuredValidationRequirementKind.SchemaMatchAssertion],
            ReliableRecipeBlockKind.Loop => [StructuredValidationRequirementKind.LoopTerminationAssertion],
            ReliableRecipeBlockKind.HumanIntervention => [StructuredValidationRequirementKind.HumanConfirmation, StructuredValidationRequirementKind.PolicyDecisionAssertion],
            ReliableRecipeBlockKind.SandboxFuture or ReliableRecipeBlockKind.DesktopFuture => [StructuredValidationRequirementKind.SandboxReadinessAssertion, StructuredValidationRequirementKind.HumanConfirmation],
            ReliableRecipeBlockKind.Validate => [StructuredValidationRequirementKind.EvalExpectedOutcomeAssertion],
            _ => []
        });

        if (RequiresApproval(recipe, block) || preflightReport?.RequiredApprovals.Count > 0)
            expected.Add(StructuredValidationRequirementKind.ExternalSideEffectConfirmation);
        if (block.Label.Contains("download", StringComparison.OrdinalIgnoreCase))
            expected.Add(StructuredValidationRequirementKind.FileDownloadedAssertion);
        if (perceptionReport?.OverallDecision is ReliableRecipePerceptionDecision.BlockedSensitiveActionAuthority or ReliableRecipePerceptionDecision.NeedsHumanReview)
            expected.Add(StructuredValidationRequirementKind.PerceptionConfidenceAssertion);
        return expected;
    }

    private static IEnumerable<StructuredEvidenceRequirement> MapEvidence(string blockId, string value)
    {
        var source = SourceFor(value, "evidence");
        foreach (var kind in MapEvidenceKind(value))
            yield return Evidence(
                $"mapped.evidence.{blockId}.{kind}.{Math.Abs(value.GetHashCode())}".ToLowerInvariant(),
                blockId,
                kind,
                true,
                source,
                $"Mapped evidence requirement from `{value}`.",
                ReliableRecipeRequirementMissingBehavior.BlockAdapterReadiness,
                kind is StructuredEvidenceRequirementKind.RedactionReport or StructuredEvidenceRequirementKind.SecretHandlingReport,
                source is ReliableRecipeRequirementSource.Explicit or ReliableRecipeRequirementSource.FixtureExplicit ? ReliableRecipeRequirementAdapterGateImpact.None : ReliableRecipeRequirementAdapterGateImpact.Warning);
    }

    private static IEnumerable<StructuredValidationRequirement> MapValidation(string blockId, string value)
    {
        var source = SourceFor(value, "validation");
        foreach (var kind in MapValidationKind(value))
            yield return Validation(
                $"mapped.validation.{blockId}.{kind}.{Math.Abs(value.GetHashCode())}".ToLowerInvariant(),
                blockId,
                kind,
                true,
                source,
                $"Mapped validation requirement from `{value}`.",
                ReliableRecipeRequirementMissingBehavior.BlockAdapterReadiness,
                source is ReliableRecipeRequirementSource.Explicit or ReliableRecipeRequirementSource.FixtureExplicit ? ReliableRecipeRequirementAdapterGateImpact.None : ReliableRecipeRequirementAdapterGateImpact.Warning);
    }

    private static IEnumerable<StructuredEvidenceRequirementKind> MapEvidenceKind(string value)
    {
        var lower = value.ToLowerInvariant();
        if (lower.Contains("before", StringComparison.Ordinal)) yield return StructuredEvidenceRequirementKind.BeforeState;
        if (lower.Contains("after", StringComparison.Ordinal)) yield return StructuredEvidenceRequirementKind.AfterState;
        if (lower.Contains("proposal", StringComparison.Ordinal)) yield return StructuredEvidenceRequirementKind.ActionProposal;
        if (lower.Contains("result", StringComparison.Ordinal)) yield return StructuredEvidenceRequirementKind.ActionResult;
        if (lower.Contains("validation", StringComparison.Ordinal)) yield return StructuredEvidenceRequirementKind.ValidationReport;
        if (lower.Contains("timeline", StringComparison.Ordinal)) yield return StructuredEvidenceRequirementKind.TimelineEvent;
        if (lower.Contains("approval", StringComparison.Ordinal)) yield return StructuredEvidenceRequirementKind.ApprovalDecision;
        if (lower.Contains("human", StringComparison.Ordinal) || lower.Contains("handoff", StringComparison.Ordinal)) yield return StructuredEvidenceRequirementKind.HumanIntervention;
        if (lower.Contains("perception", StringComparison.Ordinal) || lower.Contains("dom", StringComparison.Ordinal)) yield return StructuredEvidenceRequirementKind.PerceptionSnapshot;
        if (lower.Contains("ocr", StringComparison.Ordinal)) yield return StructuredEvidenceRequirementKind.OcrSupportingSignalRef;
        if (lower.Contains("sandbox", StringComparison.Ordinal)) yield return StructuredEvidenceRequirementKind.SandboxReadinessReport;
        if (lower.Contains("eval", StringComparison.Ordinal)) yield return StructuredEvidenceRequirementKind.EvalScenarioReport;
        if (lower.Contains("download", StringComparison.Ordinal) || lower.Contains("artifact", StringComparison.Ordinal)) yield return StructuredEvidenceRequirementKind.DownloadArtifactRef;
        if (lower.Contains("schema", StringComparison.Ordinal) || lower.Contains("extract", StringComparison.Ordinal)) yield return StructuredEvidenceRequirementKind.ExtractedDataSchema;
        if (lower.Contains("redaction", StringComparison.Ordinal) || lower.Contains("redacted", StringComparison.Ordinal)) yield return StructuredEvidenceRequirementKind.RedactionReport;
        if (lower.Contains("rollback", StringComparison.Ordinal)) yield return StructuredEvidenceRequirementKind.RollbackOrRestorePlan;
        if (lower.Contains("policy", StringComparison.Ordinal)) yield return StructuredEvidenceRequirementKind.PolicyDecision;
        if (lower.Contains("secret", StringComparison.Ordinal)) yield return StructuredEvidenceRequirementKind.SecretHandlingReport;
    }

    private static IEnumerable<StructuredValidationRequirementKind> MapValidationKind(string value)
    {
        var lower = value.ToLowerInvariant();
        if (lower.Contains("visible", StringComparison.Ordinal) || lower.Contains("text", StringComparison.Ordinal)) yield return StructuredValidationRequirementKind.VisibleTextAssertion;
        if (lower.Contains("element", StringComparison.Ordinal)) yield return StructuredValidationRequirementKind.ElementStateAssertion;
        if (lower.Contains("url", StringComparison.Ordinal) || lower.Contains("route", StringComparison.Ordinal)) yield return StructuredValidationRequirementKind.UrlOrRouteAssertion;
        if (lower.Contains("download", StringComparison.Ordinal) || lower.Contains("file", StringComparison.Ordinal)) yield return StructuredValidationRequirementKind.FileDownloadedAssertion;
        if (lower.Contains("data", StringComparison.Ordinal) || lower.Contains("extract", StringComparison.Ordinal)) yield return StructuredValidationRequirementKind.DataExtractedAssertion;
        if (lower.Contains("schema", StringComparison.Ordinal) || lower.Contains("content", StringComparison.Ordinal)) yield return StructuredValidationRequirementKind.SchemaMatchAssertion;
        if (lower.Contains("field", StringComparison.Ordinal)) yield return StructuredValidationRequirementKind.FieldValueAssertion;
        if (lower.Contains("modal", StringComparison.Ordinal)) yield return StructuredValidationRequirementKind.ModalStateAssertion;
        if (lower.Contains("loop", StringComparison.Ordinal) || lower.Contains("terminate", StringComparison.Ordinal)) yield return StructuredValidationRequirementKind.LoopTerminationAssertion;
        if (lower.Contains("external", StringComparison.Ordinal) || lower.Contains("side-effect", StringComparison.Ordinal) || lower.Contains("submit", StringComparison.Ordinal)) yield return StructuredValidationRequirementKind.ExternalSideEffectConfirmation;
        if (lower.Contains("human", StringComparison.Ordinal) || lower.Contains("manual", StringComparison.Ordinal) || lower.Contains("confirmation", StringComparison.Ordinal)) yield return StructuredValidationRequirementKind.HumanConfirmation;
        if (lower.Contains("policy", StringComparison.Ordinal)) yield return StructuredValidationRequirementKind.PolicyDecisionAssertion;
        if (lower.Contains("perception", StringComparison.Ordinal) || lower.Contains("ocr", StringComparison.Ordinal)) yield return StructuredValidationRequirementKind.PerceptionConfidenceAssertion;
        if (lower.Contains("sandbox", StringComparison.Ordinal)) yield return StructuredValidationRequirementKind.SandboxReadinessAssertion;
        if (lower.Contains("eval", StringComparison.Ordinal) || lower.Contains("expected-outcome", StringComparison.Ordinal)) yield return StructuredValidationRequirementKind.EvalExpectedOutcomeAssertion;
        if (lower.Contains("timeline", StringComparison.Ordinal)) yield return StructuredValidationRequirementKind.PolicyDecisionAssertion;
    }

    private static ReliableRecipeStructuredCompletenessReport Completeness(IReadOnlyList<StructuredEvidenceRequirement> evidence, IReadOnlyList<StructuredValidationRequirement> validation, ReliableRecipeDefinition recipe)
    {
        var allSources = evidence.Select(e => e.Source).Concat(validation.Select(v => v.Source)).ToArray();
        var total = Math.Max(1, allSources.Length);
        var explicitCount = allSources.Count(s => s is ReliableRecipeRequirementSource.Explicit or ReliableRecipeRequirementSource.FixtureExplicit);
        var inferredCount = allSources.Count(s => s is ReliableRecipeRequirementSource.InferredFromBlockKind or ReliableRecipeRequirementSource.InferredFromLabel or ReliableRecipeRequirementSource.Missing);
        var missingEvidence = evidence.Where(e => e.IsCritical && e.Source is ReliableRecipeRequirementSource.InferredFromBlockKind or ReliableRecipeRequirementSource.InferredFromLabel or ReliableRecipeRequirementSource.Missing).Select(e => $"evidence:{e.TargetBlockId}:{e.Kind}").ToArray();
        var missingValidation = validation.Where(v => v.IsCritical && v.Source is ReliableRecipeRequirementSource.InferredFromBlockKind or ReliableRecipeRequirementSource.InferredFromLabel or ReliableRecipeRequirementSource.Missing).Select(v => $"validation:{v.TargetBlockId}:{v.Kind}").ToArray();
        var inferredCritical = evidence.Any(e => e.IsCritical && e.Source == ReliableRecipeRequirementSource.InferredFromLabel) ||
            validation.Any(v => v.IsCritical && v.Source == ReliableRecipeRequirementSource.InferredFromLabel);
        var warnings = new List<string>();
        if (allSources.Any(s => s == ReliableRecipeRequirementSource.MappedFromLegacyContract))
            warnings.Add("Mapped legacy evidence/validation requirement; explicit structured requirement is preferred.");
        warnings.AddRange(evidence.Where(e => e.Source == ReliableRecipeRequirementSource.InferredFromLabel).Select(e => $"Inferred requirement from label: evidence {e.Kind} on {e.TargetBlockId}."));
        warnings.AddRange(validation.Where(v => v.Source == ReliableRecipeRequirementSource.InferredFromLabel).Select(v => $"Inferred requirement from label: validation {v.Kind} on {v.TargetBlockId}."));

        var missing = missingEvidence.Concat(missingValidation).ToArray();
        var blocking = new List<string>();
        blocking.AddRange(missing.Select(m => $"Missing critical requirement: {m}."));
        if (RequiresApproval(recipe, null) && !evidence.Any(e => e.Kind == StructuredEvidenceRequirementKind.ApprovalDecision && e.Source is ReliableRecipeRequirementSource.Explicit or ReliableRecipeRequirementSource.FixtureExplicit))
            blocking.Add("External side effect requires explicit approval evidence.");
        if (RequiresApproval(recipe, null) && !validation.Any(v => v.Kind == StructuredValidationRequirementKind.ExternalSideEffectConfirmation && v.Source is ReliableRecipeRequirementSource.Explicit or ReliableRecipeRequirementSource.FixtureExplicit))
            blocking.Add("External side effect requires explicit post-action validation.");

        var evidenceScore = Score(evidence);
        var validationScore = Score(validation);
        var impact = blocking.Count > 0 || inferredCritical
            ? ReliableRecipeRequirementAdapterGateImpact.BlocksFutureAdapter
            : warnings.Count > 0
                ? ReliableRecipeRequirementAdapterGateImpact.Warning
                : ReliableRecipeRequirementAdapterGateImpact.None;

        return new ReliableRecipeStructuredCompletenessReport(
            evidenceScore,
            validationScore,
            Math.Round(explicitCount / (double)total, 2),
            Math.Round(inferredCount / (double)total, 2),
            missing,
            warnings.Distinct().ToArray(),
            blocking.Distinct().ToArray(),
            impact);
    }

    private static ReliableRecipeStructuredAdapterGateDecision DecisionFor(ReliableRecipeStructuredCompletenessReport report, ReliableRecipeDefinition recipe)
    {
        if (recipe.RiskProfile.HasFlag(ReliableRecipeRiskProfile.Financial) || recipe.RiskProfile.HasFlag(ReliableRecipeRiskProfile.Irreversible))
            return ReliableRecipeStructuredAdapterGateDecision.BlockedByPolicy;
        if (report.BlockingFindings.Any(b => b.Contains("Missing critical", StringComparison.OrdinalIgnoreCase)))
            return ReliableRecipeStructuredAdapterGateDecision.BlockedMissingCriticalRequirements;
        if (report.AdapterReadinessImpact == ReliableRecipeRequirementAdapterGateImpact.BlocksFutureAdapter)
            return ReliableRecipeStructuredAdapterGateDecision.BlockedInferredCriticalRequirements;
        if (report.Warnings.Count > 0)
            return ReliableRecipeStructuredAdapterGateDecision.PassWithWarnings;
        return ReliableRecipeStructuredAdapterGateDecision.PassDesignOnly;
    }

    private static double Score<T>(IReadOnlyList<T> requirements) where T : notnull
    {
        if (requirements.Count == 0)
            return 0;

        var complete = requirements.Count(r => r switch
        {
            StructuredEvidenceRequirement e => e.Source is ReliableRecipeRequirementSource.Explicit or ReliableRecipeRequirementSource.FixtureExplicit or ReliableRecipeRequirementSource.MappedFromLegacyContract,
            StructuredValidationRequirement v => v.Source is ReliableRecipeRequirementSource.Explicit or ReliableRecipeRequirementSource.FixtureExplicit or ReliableRecipeRequirementSource.MappedFromLegacyContract,
            _ => false
        });

        return Math.Round(complete / (double)requirements.Count, 2);
    }

    private static ReliableRecipeRequirementSource DominantSource(IReadOnlyList<ReliableRecipeRequirementSource> sources)
    {
        if (sources.Count == 0) return ReliableRecipeRequirementSource.Missing;
        if (sources.Any(s => s is ReliableRecipeRequirementSource.Missing or ReliableRecipeRequirementSource.InferredFromBlockKind)) return ReliableRecipeRequirementSource.Missing;
        if (sources.Any(s => s == ReliableRecipeRequirementSource.InferredFromLabel)) return ReliableRecipeRequirementSource.InferredFromLabel;
        if (sources.Any(s => s == ReliableRecipeRequirementSource.MappedFromLegacyContract)) return ReliableRecipeRequirementSource.MappedFromLegacyContract;
        if (sources.Any(s => s == ReliableRecipeRequirementSource.FixtureExplicit)) return ReliableRecipeRequirementSource.FixtureExplicit;
        return ReliableRecipeRequirementSource.Explicit;
    }

    private static ReliableRecipeRequirementSource SourceFor(string value, string kind)
    {
        var lower = value.ToLowerInvariant();
        if (lower.StartsWith($"fixture.structured.{kind}.", StringComparison.Ordinal)) return ReliableRecipeRequirementSource.FixtureExplicit;
        if (lower.StartsWith($"structured.{kind}.", StringComparison.Ordinal)) return ReliableRecipeRequirementSource.Explicit;
        return ReliableRecipeRequirementSource.MappedFromLegacyContract;
    }

    private static ReliableRecipeRequirementSource InferEvidenceSource(ReliableRecipeBlock block, StructuredEvidenceRequirementKind kind) =>
        block.Label.Contains(kind.ToString().Replace("Ref", string.Empty), StringComparison.OrdinalIgnoreCase) ||
        block.Label.Contains("download", StringComparison.OrdinalIgnoreCase) && kind == StructuredEvidenceRequirementKind.DownloadArtifactRef ||
        block.Label.Contains("submit", StringComparison.OrdinalIgnoreCase) && kind == StructuredEvidenceRequirementKind.ApprovalDecision
            ? ReliableRecipeRequirementSource.InferredFromLabel
            : ReliableRecipeRequirementSource.InferredFromBlockKind;

    private static ReliableRecipeRequirementSource InferValidationSource(ReliableRecipeBlock block, StructuredValidationRequirementKind kind) =>
        block.Label.Contains("download", StringComparison.OrdinalIgnoreCase) && kind == StructuredValidationRequirementKind.FileDownloadedAssertion ||
        block.Label.Contains("submit", StringComparison.OrdinalIgnoreCase) && kind == StructuredValidationRequirementKind.ExternalSideEffectConfirmation
            ? ReliableRecipeRequirementSource.InferredFromLabel
            : ReliableRecipeRequirementSource.InferredFromBlockKind;

    private static StructuredEvidenceRequirement Evidence(
        string id,
        string blockId,
        StructuredEvidenceRequirementKind kind,
        bool critical,
        ReliableRecipeRequirementSource source,
        string description,
        ReliableRecipeRequirementMissingBehavior missing,
        bool redactionRequired,
        ReliableRecipeRequirementAdapterGateImpact impact) =>
        new(id, blockId, kind, [ReliableRecipeRunMode.DryRun], [ReliableRecipeRiskProfile.ReadOnly, ReliableRecipeRiskProfile.ExternalSideEffect, ReliableRecipeRiskProfile.SensitiveData], critical, source, description, missing, redactionRequired, impact);

    private static StructuredValidationRequirement Validation(
        string id,
        string blockId,
        StructuredValidationRequirementKind kind,
        bool critical,
        ReliableRecipeRequirementSource source,
        string assertion,
        ReliableRecipeRequirementMissingBehavior missing,
        ReliableRecipeRequirementAdapterGateImpact impact) =>
        new(id, blockId, kind, [ReliableRecipeRunMode.DryRun], [ReliableRecipeRiskProfile.ReadOnly, ReliableRecipeRiskProfile.ExternalSideEffect, ReliableRecipeRiskProfile.SensitiveData], critical, source, assertion, missing, impact);

    private static bool IsCriticalBlock(ReliableRecipeBlock block, ReliableRecipeDefinition recipe) =>
        block.Kind is ReliableRecipeBlockKind.BrowserAction or ReliableRecipeBlockKind.FileDownloadEvidence or ReliableRecipeBlockKind.CaptureArtifact or ReliableRecipeBlockKind.ConnectorDraft or ReliableRecipeBlockKind.DesktopFuture or ReliableRecipeBlockKind.SandboxFuture or ReliableRecipeBlockKind.HumanIntervention ||
        block.Risk.HasFlag(ReliableRecipeRiskProfile.ExternalSideEffect) ||
        block.Risk.HasFlag(ReliableRecipeRiskProfile.Financial) ||
        block.Risk.HasFlag(ReliableRecipeRiskProfile.Irreversible) ||
        block.Risk.HasFlag(ReliableRecipeRiskProfile.SensitiveData) ||
        recipe.RiskProfile.HasFlag(ReliableRecipeRiskProfile.ExternalSideEffect);

    private static bool RequiresApproval(ReliableRecipeDefinition recipe, ReliableRecipeBlock? block) =>
        recipe.RiskProfile.HasFlag(ReliableRecipeRiskProfile.ExternalSideEffect) ||
        recipe.RiskProfile.HasFlag(ReliableRecipeRiskProfile.Financial) ||
        recipe.RiskProfile.HasFlag(ReliableRecipeRiskProfile.Irreversible) ||
        block?.Risk.HasFlag(ReliableRecipeRiskProfile.ExternalSideEffect) == true ||
        block?.Risk.HasFlag(ReliableRecipeRiskProfile.Financial) == true ||
        block?.Risk.HasFlag(ReliableRecipeRiskProfile.Irreversible) == true;

    private static bool IsCriticalEvidence(ReliableRecipeBlock block, ReliableRecipeDefinition recipe, StructuredEvidenceRequirementKind kind) =>
        IsCriticalBlock(block, recipe) || kind is StructuredEvidenceRequirementKind.ApprovalDecision or StructuredEvidenceRequirementKind.HumanIntervention or StructuredEvidenceRequirementKind.SecretHandlingReport or StructuredEvidenceRequirementKind.SandboxReadinessReport or StructuredEvidenceRequirementKind.EvalScenarioReport;

    private static bool IsCriticalValidation(ReliableRecipeBlock block, ReliableRecipeDefinition recipe, StructuredValidationRequirementKind kind) =>
        IsCriticalBlock(block, recipe) || kind is StructuredValidationRequirementKind.ExternalSideEffectConfirmation or StructuredValidationRequirementKind.HumanConfirmation or StructuredValidationRequirementKind.SandboxReadinessAssertion or StructuredValidationRequirementKind.EvalExpectedOutcomeAssertion;

    private static ReliableRecipeRequirementMissingBehavior MissingBehavior(ReliableRecipeBlock block, ReliableRecipeDefinition recipe, object kind) =>
        IsCriticalBlock(block, recipe) ? ReliableRecipeRequirementMissingBehavior.BlockAdapterReadiness : ReliableRecipeRequirementMissingBehavior.Warn;

    private static bool RedactionRequired(ReliableRecipeDefinition recipe, StructuredEvidenceRequirementKind kind) =>
        recipe.RiskProfile.HasFlag(ReliableRecipeRiskProfile.SensitiveData) ||
        recipe.RiskProfile.HasFlag(ReliableRecipeRiskProfile.Credentialed) ||
        kind is StructuredEvidenceRequirementKind.RedactionReport or StructuredEvidenceRequirementKind.SecretHandlingReport or StructuredEvidenceRequirementKind.OcrSupportingSignalRef;

    private static ReliableRecipeRequirementAdapterGateImpact GateImpact(ReliableRecipeBlock block, ReliableRecipeDefinition recipe, object kind, ReliableRecipeRequirementSource source)
    {
        if (source is ReliableRecipeRequirementSource.Explicit or ReliableRecipeRequirementSource.FixtureExplicit)
            return ReliableRecipeRequirementAdapterGateImpact.None;
        if (source == ReliableRecipeRequirementSource.MappedFromLegacyContract)
            return ReliableRecipeRequirementAdapterGateImpact.Warning;
        return IsCriticalBlock(block, recipe)
            ? ReliableRecipeRequirementAdapterGateImpact.BlocksFutureAdapter
            : ReliableRecipeRequirementAdapterGateImpact.Warning;
    }
}

public static class ReliableRecipeStructuredPrerequisiteReportMapper
{
    public static ReliableRecipeLabStructuredPrerequisitesPanel ToLabPanel(ReliableRecipeStructuredPrerequisiteProfile profile) =>
        new(
            profile.CompletenessReport.EvidenceCompletenessScore,
            profile.CompletenessReport.ValidationCompletenessScore,
            profile.CompletenessReport.ExplicitRequirementRatio,
            profile.CompletenessReport.InferredRequirementRatio,
            profile.CompletenessReport.MissingCriticalRequirements,
            profile.CompletenessReport.Warnings,
            profile.CompletenessReport.BlockingFindings,
            profile.CompletenessReport.AdapterReadinessImpact.ToString(),
            profile.NoRuntimeNotice,
            ["Review structured prerequisites", "Open report", "Copy summary"]);
}
