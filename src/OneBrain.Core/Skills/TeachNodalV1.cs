using OneBrain.Core.History;
using OneBrain.Core.Memory;
using OneBrain.Core.Perception;
using OneBrain.Core.Recipes;
using OneBrain.Core.Runtime;
using OneBrain.Core.Verification;

namespace OneBrain.Core.Skills;

public enum TeachNodalSurface
{
    BrowserFixture,
    DesktopFixture
}

public enum TeachNodalActionKind
{
    Click,
    Type,
    Select,
    Navigate,
    Wait
}

public enum TeachNodalCompilationDecision
{
    CompiledVerifiedSkill,
    CompiledVerifiedSkillRecipeNeedsReview,
    DraftNeedsReview,
    RejectedUnsafeDemonstration,
    RejectedUnverifiedTransition
}

public sealed record TeachNodalParameterObservation(
    string Name,
    string Placeholder,
    string ValueRef,
    TrustedControlSource Source,
    bool SecretByReference = false);

public sealed record TeachNodalObservedAction(
    string ActionId,
    TeachNodalActionKind Kind,
    string IntentRedacted,
    string CapabilityId,
    string Operation,
    string SemanticTargetRef,
    string TargetLabelRedacted,
    string TargetRoleRedacted,
    TrustedControlSource TargetLabelSource,
    IReadOnlyList<TeachNodalParameterObservation> Parameters,
    IReadOnlyList<string> SelectorAliasRefs,
    double Confidence,
    bool UserCorrectionMarker = false,
    string? AmbiguityReasonRedacted = null);

public sealed record TeachNodalDemonstrationStep(
    string StepId,
    CognitiveSnapshotV2 Before,
    TeachNodalObservedAction Action,
    CognitiveSnapshotV2 After,
    SemanticVerificationPlan VerificationPlan,
    SemanticVerificationReport VerificationReport,
    IReadOnlyList<string> EvidenceRefs);

public sealed record TeachNodalDemonstration(
    string DemonstrationId,
    string TitleRedacted,
    string WorkspaceScope,
    string AppProfileId,
    int AppProfileVersion,
    TeachNodalSurface Surface,
    IReadOnlyList<TeachNodalDemonstrationStep> Steps,
    IReadOnlySet<string> AuthorizedCapabilities,
    ReliableRecipeRiskProfile RiskProfile,
    DateTimeOffset ObservedAtUtc,
    IReadOnlyList<string> EvidenceRefs);

public sealed record TeachNodalCompilationResult(
    TeachNodalCompilationDecision Decision,
    string Code,
    string Reason,
    RecorderToRecipeDraft? RecipeDraft,
    ExecutableSkill? Skill,
    ProcessMemoryEntry? ProcessMemoryProjection,
    IReadOnlyList<SkillPromotionResult> Promotions,
    IReadOnlyList<TrustedControlFlowDecision> ControlDecisions,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> Findings,
    bool FixtureOnly,
    bool LiveRecorderUsed,
    bool MouseOrKeyboardHooksUsed,
    bool RawScreenshotStored,
    bool RawDomStored,
    bool NetworkUsed,
    bool ProductAuthorityGranted);

public sealed class TeachNodalCompilerV1
{
    public const int MaximumSteps = 64;
    public const int MaximumParametersPerStep = 16;

    public TeachNodalCompilationResult Compile(TeachNodalDemonstration demonstration)
    {
        ArgumentNullException.ThrowIfNull(demonstration);
        ArgumentNullException.ThrowIfNull(demonstration.Steps);
        ArgumentNullException.ThrowIfNull(demonstration.AuthorizedCapabilities);
        ArgumentNullException.ThrowIfNull(demonstration.EvidenceRefs);

        var findings = new List<string>();
        var controlDecisions = new List<TrustedControlFlowDecision>();
        var validation = NormalizeAndValidate(demonstration, findings);
        if (!validation.Valid)
            return Rejected(TeachNodalCompilationDecision.RejectedUnsafeDemonstration, validation.Code, validation.Reason, null, controlDecisions, Evidence(demonstration.EvidenceRefs), findings);

        var normalized = validation.Demonstration!;
        var context = BuildControlContext(normalized);
        foreach (var step in normalized.Steps)
        {
            var intentDecision = TrustedControlFlowBoundary.Evaluate(
                context,
                new TrustedControlFlowRequest(
                    Source: TrustedControlSource.UserInstruction,
                    Operation: TrustedControlOperation.AddPlanStep,
                    TargetRef: step.StepId,
                    ValueRedacted: step.Action.IntentRedacted,
                    EvidenceRefs: step.EvidenceRefs,
                    CapabilityId: step.Action.CapabilityId));
            controlDecisions.Add(intentDecision);
            if (intentDecision.Decision != TrustedControlDecisionKind.Allowed)
            {
                findings.Add($"Trusted operator intent for step '{step.StepId}' was rejected.");
                return Rejected(TeachNodalCompilationDecision.RejectedUnsafeDemonstration, "TEACH_NODAL_TRUSTED_INTENT_REJECTED", intentDecision.Reason, null, controlDecisions, Evidence(normalized.EvidenceRefs), findings);
            }

            var targetDecision = TrustedControlFlowBoundary.Evaluate(
                context,
                new TrustedControlFlowRequest(
                    Source: step.Action.TargetLabelSource,
                    Operation: TrustedControlOperation.AttachEvidence,
                    TargetRef: step.Action.SemanticTargetRef,
                    ValueRedacted: step.Action.TargetLabelRedacted,
                    EvidenceRefs: step.EvidenceRefs,
                    CapabilityId: step.Action.CapabilityId));
            controlDecisions.Add(targetDecision);
            if (targetDecision.Decision != TrustedControlDecisionKind.Allowed)
            {
                findings.Add($"Observed target data for step '{step.StepId}' could not be attached as evidence.");
                return Rejected(TeachNodalCompilationDecision.RejectedUnsafeDemonstration, "TEACH_NODAL_TARGET_OBSERVATION_REJECTED", targetDecision.Reason, null, controlDecisions, Evidence(normalized.EvidenceRefs), findings);
            }
            if (targetDecision.PossiblePromptInjection)
                findings.Add($"Possible prompt injection was preserved as untrusted observed data for step '{step.StepId}'.");

            foreach (var parameter in step.Action.Parameters)
            {
                var parameterDecision = TrustedControlFlowBoundary.Evaluate(
                    context,
                    new TrustedControlFlowRequest(
                        Source: parameter.Source,
                        Operation: TrustedControlOperation.BindVariable,
                        TargetRef: parameter.Name,
                        ValueRedacted: parameter.ValueRef,
                        EvidenceRefs: step.EvidenceRefs,
                        CapabilityId: step.Action.CapabilityId));
                controlDecisions.Add(parameterDecision);
                if (parameterDecision.Decision != TrustedControlDecisionKind.Allowed)
                {
                    findings.Add($"Parameter '{parameter.Name}' failed the trusted control-flow boundary.");
                    return Rejected(TeachNodalCompilationDecision.RejectedUnsafeDemonstration, "TEACH_NODAL_PARAMETER_BINDING_REJECTED", parameterDecision.Reason, null, controlDecisions, Evidence(normalized.EvidenceRefs), findings);
                }
            }
        }

        var trajectory = BuildTrajectory(normalized);
        var draft = RecorderToRecipeDraftConverter.Convert(
            trajectory,
            new RecorderToRecipeDraftConversionOptions(
                IncludeDownloadValidation: true,
                IncludeFullValidation: true,
                IncludeFullEvidence: true));

        if (normalized.Steps.Any(step => step.Action.UserCorrectionMarker || !string.IsNullOrWhiteSpace(step.Action.AmbiguityReasonRedacted)))
        {
            findings.Add("A correction or ambiguous target remains a human review item and is not promoted to executable skill memory.");
            return new TeachNodalCompilationResult(
                Decision: TeachNodalCompilationDecision.DraftNeedsReview,
                Code: "TEACH_NODAL_DRAFT_NEEDS_HUMAN_REVIEW",
                Reason: "The demonstration compiled to the existing recipe draft model, but target correction/ambiguity blocks skill promotion.",
                RecipeDraft: draft,
                Skill: null,
                ProcessMemoryProjection: null,
                Promotions: [],
                ControlDecisions: controlDecisions,
                EvidenceRefs: Evidence(normalized.EvidenceRefs.Concat(normalized.Steps.SelectMany(step => step.EvidenceRefs))),
                Findings: findings,
                FixtureOnly: true,
                LiveRecorderUsed: false,
                MouseOrKeyboardHooksUsed: false,
                RawScreenshotStored: false,
                RawDomStored: false,
                NetworkUsed: false,
                ProductAuthorityGranted: false);
        }

        var skillMemory = new ExecutableSkillMemory();
        var candidate = BuildCandidate(normalized, draft);
        skillMemory.RegisterCandidate(candidate);
        skillMemory.MarkSupervised(
            candidate.SkillId,
            normalized.ObservedAtUtc,
            Evidence(normalized.EvidenceRefs.Concat(normalized.Steps.SelectMany(step => step.EvidenceRefs))));

        var promotions = new List<SkillPromotionResult>();
        foreach (var step in normalized.Steps)
        {
            var promotion = skillMemory.PromoteVerifiedTransition(new SkillPromotionRequest(
                Candidate: candidate,
                Before: step.Before,
                After: step.After,
                Action: BuildActionTemplate(step),
                VerificationPlan: step.VerificationPlan,
                VerificationReport: step.VerificationReport,
                VerifiedAtUtc: normalized.ObservedAtUtc.AddMilliseconds(promotions.Count + 1),
                EvidenceRefs: Evidence(normalized.EvidenceRefs.Concat(step.EvidenceRefs))));
            promotions.Add(promotion);
            if (promotion.Decision == SkillPromotionDecision.Rejected)
            {
                findings.Add($"Step '{step.StepId}' did not satisfy verified skill promotion requirements.");
                return new TeachNodalCompilationResult(
                    Decision: TeachNodalCompilationDecision.RejectedUnverifiedTransition,
                    Code: "TEACH_NODAL_TRANSITION_NOT_VERIFIED",
                    Reason: promotion.Reason,
                    RecipeDraft: draft,
                    Skill: null,
                    ProcessMemoryProjection: null,
                    Promotions: promotions,
                    ControlDecisions: controlDecisions,
                    EvidenceRefs: Evidence(normalized.EvidenceRefs.Concat(step.EvidenceRefs)),
                    Findings: findings,
                    FixtureOnly: true,
                    LiveRecorderUsed: false,
                    MouseOrKeyboardHooksUsed: false,
                    RawScreenshotStored: false,
                    RawDomStored: false,
                    NetworkUsed: false,
                    ProductAuthorityGranted: false);
            }
        }

        var skill = skillMemory.Get(candidate.SkillId);
        if (skill is null || skill.State != ExecutableSkillState.Verified || skill.Transitions.Count != normalized.Steps.Count)
        {
            findings.Add("Compiled skill did not converge to one verified transition per demonstration step.");
            return Rejected(TeachNodalCompilationDecision.RejectedUnverifiedTransition, "TEACH_NODAL_SKILL_DID_NOT_CONVERGE", "The verified skill graph did not converge after demonstration compilation.", draft, controlDecisions, Evidence(normalized.EvidenceRefs), findings, promotions);
        }

        var recipeNeedsReview = draft.ReviewState != RecorderDraftReviewState.DryRunCandidate;
        if (recipeNeedsReview)
            findings.Add($"The existing recipe draft remains '{draft.ReviewState}' even though all semantic skill transitions were verified.");

        return new TeachNodalCompilationResult(
            Decision: recipeNeedsReview
                ? TeachNodalCompilationDecision.CompiledVerifiedSkillRecipeNeedsReview
                : TeachNodalCompilationDecision.CompiledVerifiedSkill,
            Code: recipeNeedsReview
                ? "TEACH_NODAL_VERIFIED_SKILL_RECIPE_REVIEW_REQUIRED"
                : "TEACH_NODAL_VERIFIED_SKILL_READY",
            Reason: recipeNeedsReview
                ? "Verified skill memory was compiled, while the conservative recipe draft remains review-only."
                : "The demonstration compiled into the existing recipe draft and a verified, non-authoritative executable skill graph.",
            RecipeDraft: draft,
            Skill: skill,
            ProcessMemoryProjection: skill.ToProcessMemoryEntry(),
            Promotions: promotions,
            ControlDecisions: controlDecisions,
            EvidenceRefs: Evidence(normalized.EvidenceRefs.Concat(skill.EvidenceRefs)),
            Findings: findings,
            FixtureOnly: true,
            LiveRecorderUsed: false,
            MouseOrKeyboardHooksUsed: false,
            RawScreenshotStored: false,
            RawDomStored: false,
            NetworkUsed: false,
            ProductAuthorityGranted: false);
    }

    private static CompilationValidation NormalizeAndValidate(
        TeachNodalDemonstration demonstration,
        List<string> findings)
    {
        try
        {
            if (demonstration.Steps.Count is < 1 or > MaximumSteps)
                return Invalid("TEACH_NODAL_STEP_COUNT_INVALID", $"A demonstration requires between 1 and {MaximumSteps} steps.");
            if (demonstration.AppProfileVersion < 1)
                return Invalid("TEACH_NODAL_APP_PROFILE_VERSION_INVALID", "App profile version must be positive.");
            if (demonstration.AuthorizedCapabilities.Count == 0)
                return Invalid("TEACH_NODAL_CAPABILITY_SCOPE_EMPTY", "The teaching mission requires an explicit capability scope.");

            var demonstrationId = Identifier(demonstration.DemonstrationId, 120);
            var title = SafeText(demonstration.TitleRedacted, 240, "demonstration title");
            var workspace = Identifier(demonstration.WorkspaceScope, 160);
            var appProfile = Identifier(demonstration.AppProfileId, 160);
            var capabilities = demonstration.AuthorizedCapabilities
                .Select(capability => Identifier(capability, 160))
                .ToHashSet(StringComparer.Ordinal);
            var evidence = Evidence(demonstration.EvidenceRefs);
            var stepIds = new HashSet<string>(StringComparer.Ordinal);
            var normalizedSteps = new List<TeachNodalDemonstrationStep>();

            foreach (var rawStep in demonstration.Steps)
            {
                ArgumentNullException.ThrowIfNull(rawStep);
                ArgumentNullException.ThrowIfNull(rawStep.Before);
                ArgumentNullException.ThrowIfNull(rawStep.Action);
                ArgumentNullException.ThrowIfNull(rawStep.After);
                ArgumentNullException.ThrowIfNull(rawStep.VerificationPlan);
                ArgumentNullException.ThrowIfNull(rawStep.VerificationReport);
                ArgumentNullException.ThrowIfNull(rawStep.EvidenceRefs);
                ArgumentNullException.ThrowIfNull(rawStep.Action.Parameters);
                ArgumentNullException.ThrowIfNull(rawStep.Action.SelectorAliasRefs);

                var stepId = Identifier(rawStep.StepId, 120);
                if (!stepIds.Add(stepId))
                    return Invalid("TEACH_NODAL_DUPLICATE_STEP", $"Duplicate demonstration step '{stepId}'.");
                if (rawStep.Action.Parameters.Count > MaximumParametersPerStep)
                    return Invalid("TEACH_NODAL_PARAMETER_LIMIT_EXCEEDED", $"Step '{stepId}' exceeds the parameter limit.");
                if (rawStep.Before.ContainsRawScreenshot || rawStep.After.ContainsRawScreenshot ||
                    rawStep.Before.ContainsRawDom || rawStep.After.ContainsRawDom)
                    return Invalid("TEACH_NODAL_RAW_PERCEPTION_REJECTED", "Raw screenshots and raw DOM cannot enter Teach NODAL V1.");
                if (!rawStep.Before.SecretsExcluded || !rawStep.After.SecretsExcluded)
                    return Invalid("TEACH_NODAL_SECRET_BOUNDARY_REJECTED", "All demonstration snapshots must exclude secrets.");
                if (rawStep.Before.ObservedContentCanChangeMissionGoal || rawStep.After.ObservedContentCanChangeMissionGoal)
                    return Invalid("TEACH_NODAL_OBSERVED_AUTHORITY_REJECTED", "Observed content cannot carry mission goal authority.");
                if (!capabilities.Contains(rawStep.Action.CapabilityId))
                    return Invalid("TEACH_NODAL_CAPABILITY_OUTSIDE_SCOPE", $"Capability '{rawStep.Action.CapabilityId}' is outside the authorized teaching mission.");

                var actionId = Identifier(rawStep.Action.ActionId, 120);
                var intent = SafeText(rawStep.Action.IntentRedacted, 300, "step intent");
                var capability = Identifier(rawStep.Action.CapabilityId, 160);
                var operation = Identifier(rawStep.Action.Operation, 120);
                var semanticTarget = Identifier(rawStep.Action.SemanticTargetRef, 200);
                var targetLabel = SafeText(rawStep.Action.TargetLabelRedacted, 240, "target label");
                var targetRole = SafeText(rawStep.Action.TargetRoleRedacted, 120, "target role");
                var aliases = rawStep.Action.SelectorAliasRefs
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => Identifier(value, 180))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
                if (aliases.Length == 0)
                    findings.Add($"Step '{stepId}' has no stable selector alias and will remain conservative in the recipe draft.");
                var ambiguity = string.IsNullOrWhiteSpace(rawStep.Action.AmbiguityReasonRedacted)
                    ? null
                    : SafeText(rawStep.Action.AmbiguityReasonRedacted, 300, "ambiguity reason");
                var parameters = rawStep.Action.Parameters.Select(parameter => NormalizeParameter(parameter)).ToArray();
                if (parameters.Select(parameter => parameter.Name).Distinct(StringComparer.Ordinal).Count() != parameters.Length)
                    return Invalid("TEACH_NODAL_DUPLICATE_PARAMETER", $"Step '{stepId}' contains duplicate parameter names.");

                var normalizedAction = rawStep.Action with
                {
                    ActionId = actionId,
                    IntentRedacted = intent,
                    CapabilityId = capability,
                    Operation = operation,
                    SemanticTargetRef = semanticTarget,
                    TargetLabelRedacted = targetLabel,
                    TargetRoleRedacted = targetRole,
                    Parameters = parameters,
                    SelectorAliasRefs = aliases,
                    Confidence = Math.Clamp(rawStep.Action.Confidence, 0d, 1d),
                    AmbiguityReasonRedacted = ambiguity
                };
                normalizedSteps.Add(rawStep with
                {
                    StepId = stepId,
                    Action = normalizedAction,
                    EvidenceRefs = Evidence(rawStep.EvidenceRefs)
                });
            }

            var normalized = demonstration with
            {
                DemonstrationId = demonstrationId,
                TitleRedacted = title,
                WorkspaceScope = workspace,
                AppProfileId = appProfile,
                Steps = normalizedSteps,
                AuthorizedCapabilities = capabilities,
                EvidenceRefs = evidence
            };
            return new CompilationValidation(true, "TEACH_NODAL_VALID", "Demonstration structure is valid.", normalized);
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentNullException)
        {
            return Invalid("TEACH_NODAL_INPUT_INVALID", SafeRuntimeText.Sanitize(exception.Message, 500));
        }
    }

    private static TeachNodalParameterObservation NormalizeParameter(TeachNodalParameterObservation parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        var name = Identifier(parameter.Name, 80).ToUpperInvariant();
        var placeholder = SafeText(parameter.Placeholder, 120, "parameter placeholder");
        if (!string.Equals(placeholder, $"{{{name}}}", StringComparison.Ordinal))
            throw new ArgumentException($"Parameter '{name}' placeholder must be '{{{name}}}'.");
        var valueRef = SafeText(parameter.ValueRef, 240, "parameter value reference");
        if (parameter.SecretByReference &&
            !valueRef.StartsWith("secret-ref:", StringComparison.Ordinal) &&
            !valueRef.StartsWith("secret://", StringComparison.Ordinal))
            throw new ArgumentException($"Secret parameter '{name}' requires an opaque secret reference.");
        if (!parameter.SecretByReference &&
            !valueRef.StartsWith("variable-ref:", StringComparison.Ordinal) &&
            !valueRef.StartsWith("literal-ref:", StringComparison.Ordinal))
            throw new ArgumentException($"Parameter '{name}' must use a variable or literal reference, not a raw value.");
        return parameter with { Name = name, Placeholder = placeholder, ValueRef = valueRef };
    }

    private static TrustedControlFlowContext BuildControlContext(TeachNodalDemonstration demonstration)
    {
        var variables = demonstration.Steps
            .SelectMany(step => step.Action.Parameters)
            .GroupBy(parameter => parameter.Name, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => new ControlVariableDefinition(
                    Name: group.Key,
                    Sensitivity: group.Any(parameter => parameter.SecretByReference)
                        ? ControlVariableSensitivity.SecretReference
                        : ControlVariableSensitivity.Private,
                    AllowedSources: group.Select(parameter => parameter.Source).ToHashSet()),
                StringComparer.Ordinal);
        return new TrustedControlFlowContext(
            MissionId: demonstration.DemonstrationId,
            DeclaredVariables: variables,
            AllowedBranchIds: new HashSet<string>(StringComparer.Ordinal),
            AuthorizedCapabilities: demonstration.AuthorizedCapabilities,
            ExternalCommunicationAllowed: false,
            CloudAllowed: false);
    }

    private static ReliableRecorderTrajectory BuildTrajectory(TeachNodalDemonstration demonstration)
    {
        var prefix = demonstration.Surface == TeachNodalSurface.DesktopFixture
            ? "trajectory.teach-desktop."
            : "trajectory.teach-browser.";
        var interactions = demonstration.Steps.Select((step, index) =>
        {
            var mode = step.Action.SelectorAliasRefs.Count > 0
                ? ReliableActionResolutionMode.StableSelector
                : ReliableActionResolutionMode.VisibleTextExact;
            var policy = step.Action.UserCorrectionMarker || !string.IsNullOrWhiteSpace(step.Action.AmbiguityReasonRedacted)
                ? ReliableRecipePolicyDecision.NeedsHumanIntervention
                : ReliableRecipePolicyDecision.AllowDryRunOnly;
            var target = new ReliableTargetDescriptor(
                Text: step.Action.TargetLabelRedacted,
                Role: step.Action.TargetRoleRedacted,
                Selector: step.Action.SelectorAliasRefs.FirstOrDefault(),
                BoundingBox: null,
                RelativeCoordinates: null,
                ElementIndex: step.Action.SemanticTargetRef,
                WindowOrTabRef: demonstration.AppProfileId,
                FallbackStrategy: mode,
                Confidence: new ReliableTargetResolutionConfidence(
                    Score: step.Action.Confidence,
                    SignalsUsed: [mode],
                    AmbiguityReason: step.Action.AmbiguityReasonRedacted,
                    RiskAdjustedDecision: policy));
            var input = step.Action.Kind == TeachNodalActionKind.Type
                ? step.Action.Parameters.FirstOrDefault()?.Placeholder
                : null;
            return new ReliableRecordedInteraction(
                Timestamp: demonstration.ObservedAtUtc.AddMilliseconds(index),
                ObservationRef: $"teach.{step.StepId}",
                InputEventKind: MapActionKind(step.Action.Kind),
                TargetDescriptor: target,
                TextInputRedacted: input,
                WindowOrTabRef: demonstration.AppProfileId,
                UserCorrectionMarker: step.Action.UserCorrectionMarker,
                SensitiveInputDetected: step.Action.Parameters.Any(parameter => parameter.SecretByReference));
        }).ToArray();
        var variables = demonstration.Steps
            .SelectMany(step => step.Action.Parameters)
            .Select(parameter => parameter.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var sensitive = demonstration.Steps.Any(step => step.Action.Parameters.Any(parameter => parameter.SecretByReference))
            ? "Secret parameters are reference-only; raw values were never captured."
            : "No raw sensitive input is stored by Teach NODAL V1.";
        return new ReliableRecorderTrajectory(
            Id: prefix + demonstration.DemonstrationId,
            WorkspaceScope: demonstration.WorkspaceScope,
            Interactions: interactions,
            SensitiveDataSummary: sensitive,
            DetectedVariables: variables,
            RiskProfile: demonstration.RiskProfile);
    }

    private static SkillCandidateRequest BuildCandidate(
        TeachNodalDemonstration demonstration,
        RecorderToRecipeDraft draft) =>
        new(
            SkillId: $"skill.teach.{demonstration.DemonstrationId}",
            TitleRedacted: demonstration.TitleRedacted,
            AppProfileId: demonstration.AppProfileId,
            AppProfileVersion: demonstration.AppProfileVersion,
            RecipeId: draft.Recipe.Id,
            ProcessMemoryId: null,
            RunId: demonstration.DemonstrationId,
            RequiredCapabilities: demonstration.AuthorizedCapabilities,
            RiskLevel: demonstration.RiskProfile.ToString(),
            ObservedAtUtc: demonstration.ObservedAtUtc,
            EvidenceRefs: Evidence(demonstration.EvidenceRefs.Concat(demonstration.Steps.SelectMany(step => step.EvidenceRefs))),
            InitialState: ExecutableSkillState.Candidate);

    private static SkillActionTemplate BuildActionTemplate(TeachNodalDemonstrationStep step)
    {
        var recoveries = new List<SkillRecoveryAlternative>
        {
            new(
                RecoveryId: $"reobserve-{step.StepId}",
                Kind: SkillRecoveryKind.ReobserveApplication,
                SummaryRedacted: "Capture a fresh CognitiveSnapshotV2 for the affected application before retrying.",
                SelectorAliasRef: null,
                EvidenceRefs: step.EvidenceRefs)
        };
        foreach (var alias in step.Action.SelectorAliasRefs.Skip(1))
        {
            recoveries.Add(new SkillRecoveryAlternative(
                RecoveryId: $"alternate-{step.StepId}-{recoveries.Count}",
                Kind: SkillRecoveryKind.AlternateSelectorAlias,
                SummaryRedacted: "Try a previously reviewed alternate selector alias for this semantic target.",
                SelectorAliasRef: alias,
                EvidenceRefs: step.EvidenceRefs));
        }
        recoveries.Add(new SkillRecoveryAlternative(
            RecoveryId: $"human-{step.StepId}",
            Kind: SkillRecoveryKind.HumanHandoff,
            SummaryRedacted: "Stop and request human target confirmation if deterministic re-grounding remains ambiguous.",
            SelectorAliasRef: null,
            EvidenceRefs: step.EvidenceRefs,
            RequiresOperatorDecision: true));

        return new SkillActionTemplate(
            TemplateId: $"teach-action-{step.Action.ActionId}",
            CapabilityId: step.Action.CapabilityId,
            Operation: step.Action.Operation,
            SemanticTargetRef: step.Action.SemanticTargetRef,
            Parameters: step.Action.Parameters
                .Select(parameter => new SkillParameterBinding(
                    Name: parameter.Name,
                    ValueRef: parameter.ValueRef,
                    SecretByReference: parameter.SecretByReference,
                    RawValuePresent: false))
                .ToArray(),
            SelectorAliasRefs: step.Action.SelectorAliasRefs,
            RecoveryAlternatives: recoveries,
            RiskLevel: step.Action.Kind == TeachNodalActionKind.Wait ? "low" : "medium",
            RequiresExistingMissionAuthorization: true);
    }

    private static ReliableRecordedInputEventKind MapActionKind(TeachNodalActionKind kind) => kind switch
    {
        TeachNodalActionKind.Click => ReliableRecordedInputEventKind.Click,
        TeachNodalActionKind.Type => ReliableRecordedInputEventKind.Type,
        TeachNodalActionKind.Select => ReliableRecordedInputEventKind.Select,
        TeachNodalActionKind.Navigate => ReliableRecordedInputEventKind.Navigate,
        TeachNodalActionKind.Wait => ReliableRecordedInputEventKind.Wait,
        _ => ReliableRecordedInputEventKind.Unknown
    };

    private static TeachNodalCompilationResult Rejected(
        TeachNodalCompilationDecision decision,
        string code,
        string reason,
        RecorderToRecipeDraft? draft,
        IReadOnlyList<TrustedControlFlowDecision> controlDecisions,
        IReadOnlyList<string> evidenceRefs,
        IReadOnlyList<string> findings,
        IReadOnlyList<SkillPromotionResult>? promotions = null) =>
        new(
            Decision: decision,
            Code: code,
            Reason: SafeRuntimeText.Sanitize(reason, 1000),
            RecipeDraft: draft,
            Skill: null,
            ProcessMemoryProjection: null,
            Promotions: promotions ?? [],
            ControlDecisions: controlDecisions,
            EvidenceRefs: evidenceRefs,
            Findings: findings,
            FixtureOnly: true,
            LiveRecorderUsed: false,
            MouseOrKeyboardHooksUsed: false,
            RawScreenshotStored: false,
            RawDomStored: false,
            NetworkUsed: false,
            ProductAuthorityGranted: false);

    private static CompilationValidation Invalid(string code, string reason) =>
        new(false, code, SafeRuntimeText.Sanitize(reason, 500), null);

    private static string Identifier(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A required identifier is missing.");
        if (HistorySanitizer.ContainsSecretLikeContent(value) ||
            HistorySanitizer.SanitizeText(value).Contains("[LOCAL_PATH]", StringComparison.Ordinal))
            throw new ArgumentException("An identifier contains unsafe raw content.");
        var normalized = SafeRuntimeText.Sanitize(value, maximumLength);
        if (normalized.Length == 0)
            throw new ArgumentException("A required identifier is missing after sanitization.");
        return normalized;
    }

    private static string SafeText(string? value, int maximumLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{field} is required.");
        if (HistorySanitizer.ContainsSecretLikeContent(value) ||
            HistorySanitizer.SanitizeText(value).Contains("[LOCAL_PATH]", StringComparison.Ordinal))
            throw new ArgumentException($"{field} contains unsafe raw content.");
        return SafeRuntimeText.Sanitize(value, maximumLength);
    }

    private static IReadOnlyList<string> Evidence(IEnumerable<string> values) =>
        values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Identifier(value, 180))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .Take(512)
            .ToArray();

    private sealed record CompilationValidation(
        bool Valid,
        string Code,
        string Reason,
        TeachNodalDemonstration? Demonstration);
}
