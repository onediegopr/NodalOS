using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsSelectorSafetyHumanHandoffValidator
{
    private static readonly NodalOsSelectorStrategyKind[] VisualOcrStrategies =
    [
        NodalOsSelectorStrategyKind.VisualCheckpointFuture,
        NodalOsSelectorStrategyKind.OcrTextFallbackFuture
    ];

    private static readonly NodalOsHumanHandoffUserOptionKind[] RecommendedHandoffOptions =
    [
        NodalOsHumanHandoffUserOptionKind.PauseMission,
        NodalOsHumanHandoffUserOptionKind.ChangeInstruction,
        NodalOsHumanHandoffUserOptionKind.CopyTechnicalLog
    ];

    private readonly NodalOsRedactionService redaction;
    private readonly NodalOsEvidenceRefBridge evidenceBridge;

    public NodalOsSelectorSafetyHumanHandoffValidator()
        : this(new NodalOsRedactionService())
    {
    }

    public NodalOsSelectorSafetyHumanHandoffValidator(NodalOsRedactionService redaction)
        : this(redaction, new NodalOsEvidenceRefBridge(redaction))
    {
    }

    public NodalOsSelectorSafetyHumanHandoffValidator(
        NodalOsRedactionService redaction,
        NodalOsEvidenceRefBridge evidenceBridge)
    {
        this.redaction = redaction;
        this.evidenceBridge = evidenceBridge;
    }

    public NodalOsSelectorSafetyHumanHandoffValidationResult ValidatePolicy(NodalOsSelectorSafetyPolicy policy)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, policy.PolicyId, "PolicyId is required.");
        if (policy.CreatedAt == default)
            errors.Add("CreatedAt is required.");
        ValidateRuntimeFlags(policy.RuntimeExecutionAllowed, policy.RuntimeExecutionDeferred, errors);
        if (!policy.ObservationOnly)
            errors.Add("Selector safety policy must be observation-only.");
        if (!policy.VisualOcrFallbackOnly)
            errors.Add("Visual/OCR selectors must be fallback/evidence-only.");
        if (!policy.RequiresEvidenceRedaction)
            errors.Add("Selector safety policy must require evidence redaction.");
        if (!policy.RequiresGlobalPolicyEvaluation)
            errors.Add("Selector safety policy must require global policy evaluation.");
        ValidateStrategyOrder(policy.PreferredStrategyOrder, errors);

        foreach (var material in policy.ForbiddenSelectorMaterial)
            ValidateSafeText(errors, "ForbiddenSelectorMaterial", material);

        return Result(errors, warnings);
    }

    public NodalOsSelectorSafetyHumanHandoffValidationResult ValidateSelectorCandidate(NodalOsSelectorCandidate candidate)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, candidate.SelectorId, "SelectorId is required.");
        AddRequired(errors, candidate.SelectorPathRedacted, "SelectorPathRedacted is required.");
        if (candidate.CreatedAt == default)
            errors.Add("CreatedAt is required.");
        if (candidate.ContainsRawSecret)
            errors.Add("Selector candidate cannot contain raw secrets.");
        if (candidate.ContainsRawCookie)
            errors.Add("Selector candidate cannot contain raw cookies.");
        if (candidate.ContainsRawHeader)
            errors.Add("Selector candidate cannot contain raw headers.");
        if (candidate.ContainsRawBody)
            errors.Add("Selector candidate cannot contain raw private bodies.");
        if (candidate.StabilityScore is < 0 or > 1)
            errors.Add("Selector stability score must be between 0 and 1.");
        if (candidate.MutableIntentDetected)
            warnings.Add("Selector candidate contains mutable intent and must be rejected or reviewed.");
        if (candidate.StabilityScore < 0.70)
            warnings.Add("Selector candidate is unstable and requires human review.");
        if (IsVisualOrOcr(candidate.StrategyKind))
            warnings.Add("Visual/OCR selector strategy is fallback/evidence-only and requires human review.");

        ValidateSafeText(errors, "SelectorPathRedacted", candidate.SelectorPathRedacted);
        ValidateSafeText(errors, "HumanLabelRedacted", candidate.HumanLabelRedacted);
        ValidateTimelineCompatibility(candidate.MissionId, candidate.TaskId, candidate.RecipeId, candidate.StepId, warnings);
        ValidateEvidenceRefs(candidate.EvidenceRefs, errors, warnings);

        return Result(errors, warnings);
    }

    public NodalOsSelectorSafetyEvaluation EvaluateSelector(
        NodalOsSelectorSafetyPolicy policy,
        NodalOsSelectorCandidate candidate)
    {
        var policyResult = ValidatePolicy(policy);
        var candidateResult = ValidateSelectorCandidate(candidate);
        var reasons = new List<string>();
        var warnings = new List<string>();
        reasons.AddRange(policyResult.Errors);
        reasons.AddRange(candidateResult.Errors);
        warnings.AddRange(policyResult.Warnings);
        warnings.AddRange(candidateResult.Warnings);

        var decision = NodalOsSelectorSafetyDecision.AllowedForObservationOnly;
        var risk = NodalOsSelectorRiskKind.Low;
        var requiresHumanReview = false;

        if (ContainsRawMaterial(candidate) || candidateResult.Errors.Any(error => error.Contains("sensitive", StringComparison.OrdinalIgnoreCase)))
        {
            decision = NodalOsSelectorSafetyDecision.RejectedSensitive;
            risk = NodalOsSelectorRiskKind.Critical;
        }
        else if (candidate.MutableIntentDetected)
        {
            decision = NodalOsSelectorSafetyDecision.RejectedMutableIntent;
            risk = NodalOsSelectorRiskKind.High;
        }
        else if (candidate.StabilityScore < 0.70)
        {
            decision = NodalOsSelectorSafetyDecision.RejectedUnstable;
            risk = NodalOsSelectorRiskKind.Medium;
            requiresHumanReview = true;
        }
        else if (IsVisualOrOcr(candidate.StrategyKind))
        {
            decision = NodalOsSelectorSafetyDecision.RequiresHumanReview;
            risk = NodalOsSelectorRiskKind.Medium;
            requiresHumanReview = true;
        }
        else if (!policyResult.IsValid || !candidateResult.IsValid)
        {
            decision = NodalOsSelectorSafetyDecision.RejectedUnsupported;
            risk = NodalOsSelectorRiskKind.High;
        }

        if (decision == NodalOsSelectorSafetyDecision.AllowedForObservationOnly)
            reasons.Add("Selector allowed for observation only.");
        else
            reasons.Add($"Selector decision: {decision}.");

        return new NodalOsSelectorSafetyEvaluation
        {
            EvaluationId = $"selector-evaluation-{Guid.NewGuid():N}",
            SelectorId = candidate.SelectorId,
            Decision = decision,
            RiskKind = risk,
            CanAuthorizeAction = false,
            ObservationOnly = true,
            RequiresHumanReview = requiresHumanReview || decision != NodalOsSelectorSafetyDecision.AllowedForObservationOnly,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            Reasons = reasons.Select(Sanitize).Distinct(StringComparer.Ordinal).ToArray(),
            Warnings = warnings.Select(Sanitize).Distinct(StringComparer.Ordinal).ToArray(),
            EvidenceRefs = candidate.EvidenceRefs,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public NodalOsSelectorSafetyHumanHandoffValidationResult ValidateSelectorEvaluation(NodalOsSelectorSafetyEvaluation evaluation)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, evaluation.EvaluationId, "EvaluationId is required.");
        AddRequired(errors, evaluation.SelectorId, "SelectorId is required.");
        if (evaluation.CreatedAt == default)
            errors.Add("CreatedAt is required.");
        if (evaluation.CanAuthorizeAction)
            errors.Add("Selector evaluation cannot authorize actions.");
        if (!evaluation.ObservationOnly)
            errors.Add("Selector evaluation must remain observation-only.");
        ValidateRuntimeFlags(evaluation.RuntimeExecutionAllowed, evaluation.RuntimeExecutionDeferred, errors);
        foreach (var reason in evaluation.Reasons)
            ValidateSafeText(errors, "Reasons", reason);
        foreach (var warning in evaluation.Warnings)
            ValidateSafeText(errors, "Warnings", warning);
        ValidateEvidenceRefs(evaluation.EvidenceRefs, errors, warnings);

        return Result(errors, warnings);
    }

    public NodalOsSelectorSafetyHumanHandoffValidationResult ValidateHumanHandoff(NodalOsHumanHandoffContract handoff)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, handoff.HandoffId, "HandoffId is required.");
        AddRequired(errors, handoff.HumanReadableBlockerRedacted, "HumanReadableBlockerRedacted is required.");
        if (handoff.CreatedAt == default)
            errors.Add("CreatedAt is required.");
        if (!handoff.RequiresHumanAction)
            errors.Add("Human handoff contract must require human action.");
        if (handoff.CanAuthorizeAction)
            errors.Add("Human handoff contract cannot authorize actions.");
        ValidateRuntimeFlags(handoff.RuntimeExecutionAllowed, handoff.RuntimeExecutionDeferred, errors);
        if (IsGenericBlockedOnly(handoff.HumanReadableBlockerRedacted))
            errors.Add("Human handoff blocker must explain the exact blocker, not only say blocked.");
        ValidateSafeText(errors, "HumanReadableBlockerRedacted", handoff.HumanReadableBlockerRedacted);
        ValidateSafeText(errors, "TechnicalLogRedacted", handoff.TechnicalLogRedacted);
        ValidateUserOptions(handoff.UserOptions, errors, warnings);
        ValidateTimelineCompatibility(handoff.MissionId, handoff.TaskId, handoff.RecipeId, handoff.StepId, warnings);
        ValidateEvidenceRefs(handoff.EvidenceRefs, errors, warnings);

        return Result(errors, warnings);
    }

    public void ValidateEvidenceRefs(
        IReadOnlyList<NodalOsEvidenceBridgeRef> evidenceRefs,
        List<string> errors,
        List<string> warnings)
    {
        foreach (var evidenceRef in evidenceRefs)
        {
            var result = evidenceBridge.ValidateBridgeRef(evidenceRef);
            errors.AddRange(result.Errors.Select(Sanitize));
            warnings.AddRange(result.Warnings.Select(Sanitize));
        }
    }

    public void ValidateNoRawSecrets(string? value, List<string> errors, string fieldName) =>
        ValidateSafeText(errors, fieldName, value);

    public void ValidateUserOptions(
        IReadOnlyList<NodalOsHumanHandoffUserOptionKind> userOptions,
        List<string> errors,
        List<string> warnings)
    {
        if (userOptions.Count < 2)
            errors.Add("Human handoff must include at least two useful user options.");
        foreach (var option in RecommendedHandoffOptions)
        {
            if (!userOptions.Contains(option))
                warnings.Add($"Recommended handoff option is missing: {option}.");
        }
    }

    private void ValidateStrategyOrder(
        IReadOnlyList<NodalOsSelectorStrategyKind> strategyOrder,
        List<string> errors)
    {
        if (strategyOrder.Count == 0)
        {
            errors.Add("PreferredStrategyOrder is required.");
            return;
        }

        if (IsVisualOrOcr(strategyOrder[0]))
            errors.Add("Visual/OCR selector strategies cannot be first.");

        var firstVisualIndex = strategyOrder
            .Select((strategy, index) => (strategy, index))
            .Where(item => IsVisualOrOcr(item.strategy))
            .Select(item => item.index)
            .DefaultIfEmpty(int.MaxValue)
            .Min();
        var firstSafeIndex = strategyOrder
            .Select((strategy, index) => (strategy, index))
            .Where(item => IsPreferredSemanticOrDom(item.strategy))
            .Select(item => item.index)
            .DefaultIfEmpty(int.MaxValue)
            .Min();

        if (firstSafeIndex == int.MaxValue || firstSafeIndex > firstVisualIndex)
            errors.Add("Semantic or DOM selector strategies must be preferred before Visual/OCR fallback.");
    }

    private void ValidateSafeText(List<string> errors, string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (redaction.ContainsSensitiveField(fieldName, value) || redaction.ContainsSensitiveContent(value))
            errors.Add($"{fieldName} contains sensitive content and must be redacted before selector or handoff persistence.");
    }

    private void ValidateTimelineCompatibility(
        string? missionId,
        string? taskId,
        string? recipeId,
        string? stepId,
        List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(missionId) &&
            string.IsNullOrWhiteSpace(taskId) &&
            string.IsNullOrWhiteSpace(recipeId) &&
            string.IsNullOrWhiteSpace(stepId))
        {
            warnings.Add("Selector/handoff contract has generic Mission Control timeline context.");
        }
    }

    private static bool ContainsRawMaterial(NodalOsSelectorCandidate candidate) =>
        candidate.ContainsRawSecret ||
        candidate.ContainsRawCookie ||
        candidate.ContainsRawHeader ||
        candidate.ContainsRawBody;

    private static bool IsVisualOrOcr(NodalOsSelectorStrategyKind strategyKind) =>
        VisualOcrStrategies.Contains(strategyKind);

    private static bool IsPreferredSemanticOrDom(NodalOsSelectorStrategyKind strategyKind) =>
        strategyKind is NodalOsSelectorStrategyKind.Semantic
            or NodalOsSelectorStrategyKind.DomStableAttribute
            or NodalOsSelectorStrategyKind.DomCssPath
            or NodalOsSelectorStrategyKind.DomXPath
            or NodalOsSelectorStrategyKind.CdpAccessibilityTreeFuture;

    private static bool IsGenericBlockedOnly(string? value) =>
        string.Equals(value?.Trim(), "blocked", StringComparison.OrdinalIgnoreCase);

    private static void ValidateRuntimeFlags(bool runtimeExecutionAllowed, bool runtimeExecutionDeferred, List<string> errors)
    {
        if (runtimeExecutionAllowed)
            errors.Add("Selector safety and human handoff contracts cannot allow runtime execution.");
        if (!runtimeExecutionDeferred)
            errors.Add("Selector safety and human handoff contracts must keep runtime execution deferred.");
    }

    private string Sanitize(string value) =>
        redaction.RedactValue(value).Value;

    private static void AddRequired(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(message);
    }

    private static NodalOsSelectorSafetyHumanHandoffValidationResult Result(
        List<string> errors,
        List<string> warnings) =>
        new()
        {
            IsValid = errors.Count == 0,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            CanAuthorizeAction = false,
            Errors = errors.Distinct(StringComparer.Ordinal).ToArray(),
            Warnings = warnings.Distinct(StringComparer.Ordinal).ToArray()
        };
}

public sealed class NodalOsSelectorSafetyHumanHandoffJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializePolicy(NodalOsSelectorSafetyPolicy policy) =>
        JsonSerializer.Serialize(policy, Options);

    public NodalOsSelectorSafetyPolicy? DeserializePolicy(string json) =>
        JsonSerializer.Deserialize<NodalOsSelectorSafetyPolicy>(json, Options);

    public string SerializeCandidate(NodalOsSelectorCandidate candidate) =>
        JsonSerializer.Serialize(candidate, Options);

    public NodalOsSelectorCandidate? DeserializeCandidate(string json) =>
        JsonSerializer.Deserialize<NodalOsSelectorCandidate>(json, Options);

    public string SerializeEvaluation(NodalOsSelectorSafetyEvaluation evaluation) =>
        JsonSerializer.Serialize(evaluation, Options);

    public NodalOsSelectorSafetyEvaluation? DeserializeEvaluation(string json) =>
        JsonSerializer.Deserialize<NodalOsSelectorSafetyEvaluation>(json, Options);

    public string SerializeHandoff(NodalOsHumanHandoffContract handoff) =>
        JsonSerializer.Serialize(handoff, Options);

    public NodalOsHumanHandoffContract? DeserializeHandoff(string json) =>
        JsonSerializer.Deserialize<NodalOsHumanHandoffContract>(json, Options);
}

public static class NodalOsSelectorSafetyHumanHandoffFixtures
{
    public static NodalOsSelectorSafetyPolicy DefaultObservationOnlyPolicy() =>
        new()
        {
            PolicyId = "selector-policy-observation-only-v1",
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            ObservationOnly = true,
            VisualOcrFallbackOnly = true,
            RequiresEvidenceRedaction = true,
            RequiresGlobalPolicyEvaluation = true,
            PreferredStrategyOrder =
            [
                NodalOsSelectorStrategyKind.Semantic,
                NodalOsSelectorStrategyKind.DomStableAttribute,
                NodalOsSelectorStrategyKind.DomCssPath,
                NodalOsSelectorStrategyKind.DomXPath,
                NodalOsSelectorStrategyKind.CdpAccessibilityTreeFuture,
                NodalOsSelectorStrategyKind.VisualCheckpointFuture,
                NodalOsSelectorStrategyKind.OcrTextFallbackFuture
            ],
            ForbiddenSelectorMaterial = ["credential-marker", "session-marker", "sensitive-header-marker", "private-payload-marker"],
            CreatedAt = DateTimeOffset.UtcNow
        };

    public static NodalOsSelectorCandidate SafeSemanticSelector() =>
        Candidate(NodalOsSelectorStrategyKind.Semantic) with
        {
            SelectorPathRedacted = "semantic://continue-primary",
            HumanLabelRedacted = "Continue primary action label",
            StabilityScore = 0.96
        };

    public static NodalOsSelectorCandidate SafeDomStableAttributeSelector() =>
        Candidate(NodalOsSelectorStrategyKind.DomStableAttribute) with
        {
            SelectorPathRedacted = "dom://button[data-testid='continue-primary']",
            HumanLabelRedacted = "Stable test id selector",
            StabilityScore = 0.92
        };

    public static NodalOsSelectorCandidate UnstableSelector() =>
        Candidate(NodalOsSelectorStrategyKind.DomCssPath) with
        {
            SelectorPathRedacted = "dom://main div:nth-child(9) button:nth-child(2)",
            StabilityScore = 0.31
        };

    public static NodalOsSelectorCandidate MutableIntentSelector() =>
        Candidate(NodalOsSelectorStrategyKind.DomStableAttribute) with
        {
            SelectorPathRedacted = "dom://button[data-testid='submit-payment']",
            MutableIntentDetected = true,
            StabilityScore = 0.89
        };

    public static NodalOsSelectorCandidate SecretSelector() =>
        Candidate(NodalOsSelectorStrategyKind.DomCssPath) with
        {
            SelectorPathRedacted = "dom://input[name='password'][value='[REDACTED]']",
            ContainsRawSecret = true
        };

    public static NodalOsSelectorCandidate CookieSelector() =>
        Candidate(NodalOsSelectorStrategyKind.DomCssPath) with
        {
            SelectorPathRedacted = "dom://meta[name='cookie']",
            ContainsRawCookie = true
        };

    public static NodalOsSelectorCandidate HeaderSelector() =>
        Candidate(NodalOsSelectorStrategyKind.DomCssPath) with
        {
            SelectorPathRedacted = "dom://meta[name='authorization']",
            ContainsRawHeader = true
        };

    public static NodalOsSelectorCandidate BodySelector() =>
        Candidate(NodalOsSelectorStrategyKind.DomCssPath) with
        {
            SelectorPathRedacted = "dom://pre[data-private-body='[REDACTED]']",
            ContainsRawBody = true
        };

    public static NodalOsHumanHandoffContract LoginRequiredHandoff() =>
        Handoff(NodalOsAutomationHandoffReason.LoginRequired, "Login is required before NODAL OS can continue safely.");

    public static NodalOsHumanHandoffContract CaptchaRequiredHandoff() =>
        Handoff(NodalOsAutomationHandoffReason.CaptchaRequired, "Captcha challenge requires human decision before continuing.");

    public static NodalOsHumanHandoffContract TwoFactorRequiredHandoff() =>
        Handoff(NodalOsAutomationHandoffReason.TwoFactorRequired, "Two-factor authentication requires human action before continuing.");

    public static NodalOsHumanHandoffContract PolicyBlockedHandoff() =>
        Handoff(NodalOsAutomationHandoffReason.PolicyBlocked, "Policy blocked the requested automation until instructions change.");

    public static NodalOsHumanHandoffContract GenericBlockedHandoffInvalid() =>
        Handoff(NodalOsAutomationHandoffReason.PolicyBlocked, "blocked");

    public static NodalOsEvidenceBridgeRef ValidEvidenceRef() =>
        new()
        {
            EvidenceId = $"evidence-{Guid.NewGuid():N}",
            Kind = "selector-handoff-contract",
            Ref = "ledger:selector-handoff-contract",
            Hash = "sha256:selector-handoff-contract",
            SourceKind = NodalOsEvidenceBridgeSourceKind.AgentOperation,
            UseKind = NodalOsEvidenceBridgeUseKind.AuditTrail,
            Authority = NodalOsEvidenceBridgeAuthority.NoAuthority,
            Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
            RedactionState = NodalOsEvidenceRedactionState.NotRequired,
            LedgerRef = "ledger:selector-handoff-contract",
            Provenance = "NODAL OS:SelectorSafety:ContractOnly",
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static NodalOsSelectorCandidate Candidate(NodalOsSelectorStrategyKind strategyKind) =>
        new()
        {
            SelectorId = $"selector-{Guid.NewGuid():N}",
            MissionId = "mission-selector-safety",
            TaskId = "task-selector-safety",
            RecipeId = "recipe-selector-safety",
            StepId = "step-selector-safety",
            StrategyKind = strategyKind,
            SelectorPathRedacted = "semantic://safe-observation-target",
            HumanLabelRedacted = "Safe observation target",
            ContainsRawSecret = false,
            ContainsRawCookie = false,
            ContainsRawHeader = false,
            ContainsRawBody = false,
            MutableIntentDetected = false,
            StabilityScore = 0.90,
            EvidenceRefs = [ValidEvidenceRef()],
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static NodalOsHumanHandoffContract Handoff(NodalOsAutomationHandoffReason reason, string blocker) =>
        new()
        {
            HandoffId = $"handoff-{Guid.NewGuid():N}",
            MissionId = "mission-selector-safety",
            TaskId = "task-selector-safety",
            RecipeId = "recipe-selector-safety",
            StepId = "step-selector-safety",
            Reason = reason,
            RequiresHumanAction = true,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            CanAuthorizeAction = false,
            HumanReadableBlockerRedacted = blocker,
            TechnicalLogRedacted = "Contract-only handoff. No runtime action was executed.",
            UserOptions =
            [
                NodalOsHumanHandoffUserOptionKind.ContinueAfterUserAction,
                NodalOsHumanHandoffUserOptionKind.PauseMission,
                NodalOsHumanHandoffUserOptionKind.ChangeInstruction,
                NodalOsHumanHandoffUserOptionKind.RetryAfterFix,
                NodalOsHumanHandoffUserOptionKind.CopyTechnicalLog,
                NodalOsHumanHandoffUserOptionKind.CancelMission
            ],
            EvidenceRefs = [ValidEvidenceRef()],
            CreatedAt = DateTimeOffset.UtcNow
        };
}
