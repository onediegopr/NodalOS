using OneBrain.Core.Contracts;
using OneBrain.Core.Perception;
using OneBrain.Core.Runtime;

namespace OneBrain.Core.Verification;

public enum SemanticVerificationRuleKind
{
    ElementPresent,
    ElementAbsent,
    ElementAdded,
    ElementRemoved,
    PropertyEquals,
    PropertyChanged,
    PropertyUnchanged,
    ProcessUnchanged,
    WindowTitleContains,
    StateFingerprintChanged,
    StateFingerprintUnchanged,
    NoBlockingConflicts,
    EvidenceRefPresent
}

public enum SemanticVerificationFailureClass
{
    None,
    TargetNotFound,
    ActionRejected,
    ActionExecutionFailed,
    ExpectedStateNotReached,
    UnexpectedSideEffect,
    ApplicationCrashed,
    EnvironmentChanged,
    UserInterrupted,
    VerifierInconclusive
}

public sealed record SemanticVerificationRule(
    string RuleId,
    SemanticVerificationRuleKind Kind,
    string? SubjectRef = null,
    string? Property = null,
    string? ExpectedValueRedacted = null,
    bool Required = true,
    string Description = "");

public sealed record SemanticVerificationPlan(
    string PlanId,
    IReadOnlyList<SemanticVerificationRule> Preconditions,
    IReadOnlyList<SemanticVerificationRule> ExpectedTransition,
    IReadOnlyList<SemanticVerificationRule> ExpectedOutcome,
    IReadOnlyList<SemanticVerificationRule> ForbiddenSideEffects,
    IReadOnlyList<string> RequiredEvidenceRefs,
    TimeSpan Timeout,
    string? AppProfileRef = null,
    bool RequireActionExecuted = true,
    bool AllowProcessChange = false,
    bool FailOnBlockingConflicts = true);

public sealed record SemanticVerificationContext(
    CognitiveSnapshotV2 Before,
    CognitiveSnapshotV2? After,
    bool ActionExecuted,
    bool ActionRejected,
    bool UserInterrupted,
    TimeSpan Elapsed,
    IReadOnlyList<string> EvidenceRefs);

public sealed record SemanticVerificationFact(
    string RuleId,
    string Stage,
    bool Passed,
    string ObservedRedacted,
    string Reason);

public sealed record SemanticVerificationReport(
    bool Success,
    bool ActionExecuted,
    bool ProcessVerified,
    bool StateTransitionVerified,
    bool OutcomeVerified,
    bool SideEffectsChecked,
    bool EvidenceComplete,
    SemanticVerificationFailureClass FailureClass,
    FailureKind? MappedFailureKind,
    IReadOnlyList<SemanticVerificationFact> FactsObserved,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> Reasons);

public sealed class SemanticVerifierV2
{
    public SemanticVerificationReport Verify(
        SemanticVerificationPlan plan,
        SemanticVerificationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Before);
        ArgumentNullException.ThrowIfNull(context.EvidenceRefs);

        ValidatePlan(plan);

        var evidenceRefs = context.EvidenceRefs
            .Concat(context.After?.EvidenceRefs ?? Array.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => SafeRuntimeText.Sanitize(value, 160))
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .Take(512)
            .ToArray();
        var facts = new List<SemanticVerificationFact>();
        var reasons = new List<string>();

        if (context.UserInterrupted)
        {
            reasons.Add("User input interrupted semantic verification.");
            return Failure(
                context,
                facts,
                evidenceRefs,
                reasons,
                SemanticVerificationFailureClass.UserInterrupted,
                FailureKind.HumanInterrupted);
        }

        if (context.ActionRejected)
        {
            reasons.Add("The action was rejected before execution.");
            return Failure(
                context,
                facts,
                evidenceRefs,
                reasons,
                SemanticVerificationFailureClass.ActionRejected,
                FailureKind.PolicyDenied);
        }

        if (context.Elapsed > plan.Timeout)
        {
            reasons.Add($"Semantic verification exceeded its {plan.Timeout.TotalMilliseconds:0} ms timeout.");
            return Failure(
                context,
                facts,
                evidenceRefs,
                reasons,
                SemanticVerificationFailureClass.VerifierInconclusive,
                FailureKind.Unverified);
        }

        var preconditionResults = EvaluateRules(
            plan.Preconditions,
            "precondition",
            context.Before,
            context.Before,
            diff: null,
            evidenceRefs,
            forbidden: false,
            facts);
        if (!preconditionResults.Passed)
        {
            reasons.AddRange(preconditionResults.Reasons);
            var targetMissing = plan.Preconditions.Any(rule =>
                rule.Required &&
                rule.Kind == SemanticVerificationRuleKind.ElementPresent &&
                facts.Any(fact => fact.RuleId == rule.RuleId && !fact.Passed));
            return Failure(
                context,
                facts,
                evidenceRefs,
                reasons,
                targetMissing
                    ? SemanticVerificationFailureClass.TargetNotFound
                    : SemanticVerificationFailureClass.EnvironmentChanged,
                targetMissing ? FailureKind.NotFound : FailureKind.Stale);
        }

        if (plan.RequireActionExecuted && !context.ActionExecuted)
        {
            reasons.Add("The action did not report execution.");
            return Failure(
                context,
                facts,
                evidenceRefs,
                reasons,
                SemanticVerificationFailureClass.ActionExecutionFailed,
                FailureKind.Unverified);
        }

        if (context.After is null)
        {
            reasons.Add("The application snapshot was unavailable after execution.");
            return Failure(
                context,
                facts,
                evidenceRefs,
                reasons,
                SemanticVerificationFailureClass.ApplicationCrashed,
                FailureKind.Stale);
        }

        var diff = CognitiveSnapshotV2Differ.Diff(context.Before, context.After);
        var processVerified = !diff.ProcessChanged;
        if (!plan.AllowProcessChange && !processVerified)
        {
            facts.Add(Fact(
                "process-stability",
                "process",
                passed: false,
                $"before={context.Before.Application.ProcessNameRedacted};after={context.After.Application.ProcessNameRedacted}",
                "Process identity changed during the verified action."));
            reasons.Add("Process identity changed during execution.");
            return Failure(
                context,
                facts,
                evidenceRefs,
                reasons,
                SemanticVerificationFailureClass.EnvironmentChanged,
                FailureKind.Stale,
                processVerified: false);
        }

        if (plan.FailOnBlockingConflicts && context.After.HasBlockingConflicts)
        {
            var conflictSummary = string.Join(
                "; ",
                context.After.Conflicts
                    .Where(conflict => conflict.Severity == PerceptionConflictSeverity.Blocking)
                    .Select(conflict => $"{conflict.SubjectRef}.{conflict.Property}")
                    .OrderBy(value => value, StringComparer.Ordinal));
            facts.Add(Fact(
                "cross-channel-conflicts",
                "perception",
                passed: false,
                conflictSummary,
                "Action-critical perception claims disagree across channels."));
            reasons.Add("Blocking cross-channel perception conflict requires re-grounding.");
            return Failure(
                context,
                facts,
                evidenceRefs,
                reasons,
                SemanticVerificationFailureClass.VerifierInconclusive,
                FailureKind.Ambiguous,
                processVerified: processVerified);
        }

        var transitionResults = EvaluateRules(
            plan.ExpectedTransition,
            "transition",
            context.Before,
            context.After,
            diff,
            evidenceRefs,
            forbidden: false,
            facts);
        if (!transitionResults.Passed)
        {
            reasons.AddRange(transitionResults.Reasons);
            return Failure(
                context,
                facts,
                evidenceRefs,
                reasons,
                SemanticVerificationFailureClass.ExpectedStateNotReached,
                FailureKind.Unverified,
                processVerified: processVerified,
                stateTransitionVerified: false);
        }

        var outcomeResults = EvaluateRules(
            plan.ExpectedOutcome,
            "outcome",
            context.Before,
            context.After,
            diff,
            evidenceRefs,
            forbidden: false,
            facts);
        if (!outcomeResults.Passed)
        {
            reasons.AddRange(outcomeResults.Reasons);
            return Failure(
                context,
                facts,
                evidenceRefs,
                reasons,
                SemanticVerificationFailureClass.ExpectedStateNotReached,
                FailureKind.Unverified,
                processVerified: processVerified,
                stateTransitionVerified: true,
                outcomeVerified: false);
        }

        var sideEffectResults = EvaluateRules(
            plan.ForbiddenSideEffects,
            "forbidden-side-effect",
            context.Before,
            context.After,
            diff,
            evidenceRefs,
            forbidden: true,
            facts);
        if (!sideEffectResults.Passed)
        {
            reasons.AddRange(sideEffectResults.Reasons);
            return Failure(
                context,
                facts,
                evidenceRefs,
                reasons,
                SemanticVerificationFailureClass.UnexpectedSideEffect,
                FailureKind.PolicyDenied,
                processVerified: processVerified,
                stateTransitionVerified: true,
                outcomeVerified: true,
                sideEffectsChecked: false);
        }

        var missingEvidence = plan.RequiredEvidenceRefs
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => SafeRuntimeText.Sanitize(value, 160))
            .Where(value => !evidenceRefs.Contains(value, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var evidenceComplete = missingEvidence.Length == 0;
        foreach (var requiredEvidence in plan.RequiredEvidenceRefs
                     .Where(value => !string.IsNullOrWhiteSpace(value))
                     .Select(value => SafeRuntimeText.Sanitize(value, 160))
                     .Distinct(StringComparer.Ordinal))
        {
            facts.Add(Fact(
                $"evidence:{requiredEvidence}",
                "evidence",
                evidenceRefs.Contains(requiredEvidence, StringComparer.Ordinal),
                requiredEvidence,
                evidenceRefs.Contains(requiredEvidence, StringComparer.Ordinal)
                    ? "Required evidence reference is present."
                    : "Required evidence reference is missing."));
        }

        if (!evidenceComplete)
        {
            reasons.Add("Required evidence is incomplete: " + string.Join(", ", missingEvidence));
            return Failure(
                context,
                facts,
                evidenceRefs,
                reasons,
                SemanticVerificationFailureClass.VerifierInconclusive,
                FailureKind.Unverified,
                processVerified: processVerified,
                stateTransitionVerified: true,
                outcomeVerified: true,
                sideEffectsChecked: true,
                evidenceComplete: false);
        }

        reasons.Add("Semantic verification passed with expected transition, outcome, side-effect checks and evidence.");
        return new SemanticVerificationReport(
            Success: true,
            ActionExecuted: context.ActionExecuted,
            ProcessVerified: processVerified,
            StateTransitionVerified: true,
            OutcomeVerified: true,
            SideEffectsChecked: true,
            EvidenceComplete: true,
            FailureClass: SemanticVerificationFailureClass.None,
            MappedFailureKind: null,
            FactsObserved: SanitizeFacts(facts),
            EvidenceRefs: evidenceRefs,
            Reasons: SanitizeReasons(reasons));
    }

    private static RuleEvaluationResult EvaluateRules(
        IReadOnlyList<SemanticVerificationRule> rules,
        string stage,
        CognitiveSnapshotV2 before,
        CognitiveSnapshotV2 after,
        CognitiveSnapshotV2Diff? diff,
        IReadOnlyList<string> evidenceRefs,
        bool forbidden,
        List<SemanticVerificationFact> facts)
    {
        var reasons = new List<string>();
        var passed = true;
        foreach (var rawRule in rules)
        {
            var rule = SanitizeRule(rawRule);
            var evaluation = EvaluateRule(rule, before, after, diff, evidenceRefs, stage == "precondition");
            var rulePassed = forbidden ? !evaluation.Matched : evaluation.Matched;
            if (rule.Required && !rulePassed)
            {
                passed = false;
                reasons.Add(forbidden
                    ? $"Forbidden side effect matched rule '{rule.RuleId}'."
                    : $"Required {stage} rule '{rule.RuleId}' did not match.");
            }

            facts.Add(Fact(
                rule.RuleId,
                stage,
                !rule.Required || rulePassed,
                evaluation.Observed,
                forbidden
                    ? rulePassed
                        ? "Forbidden condition was not observed."
                        : "Forbidden condition was observed."
                    : evaluation.Reason));
        }

        return new RuleEvaluationResult(passed, reasons);
    }

    private static RuleMatch EvaluateRule(
        SemanticVerificationRule rule,
        CognitiveSnapshotV2 before,
        CognitiveSnapshotV2 after,
        CognitiveSnapshotV2Diff? diff,
        IReadOnlyList<string> evidenceRefs,
        bool useBeforeSnapshot)
    {
        var snapshot = useBeforeSnapshot ? before : after;
        var subjectRef = rule.SubjectRef ?? string.Empty;
        var property = rule.Property ?? string.Empty;
        var expected = rule.ExpectedValueRedacted ?? string.Empty;
        var element = snapshot.Elements.FirstOrDefault(value =>
            string.Equals(value.SemanticRef, subjectRef, StringComparison.Ordinal));

        return rule.Kind switch
        {
            SemanticVerificationRuleKind.ElementPresent => new(
                element is not null,
                element?.SemanticRef ?? "absent",
                element is not null ? "Element is present." : "Element is absent."),
            SemanticVerificationRuleKind.ElementAbsent => new(
                element is null,
                element?.SemanticRef ?? "absent",
                element is null ? "Element is absent." : "Element is present."),
            SemanticVerificationRuleKind.ElementAdded => new(
                diff?.AddedElementRefs.Contains(subjectRef, StringComparer.Ordinal) == true,
                string.Join(',', diff?.AddedElementRefs ?? Array.Empty<string>()),
                "Element addition evaluated from the semantic diff."),
            SemanticVerificationRuleKind.ElementRemoved => new(
                diff?.RemovedElementRefs.Contains(subjectRef, StringComparer.Ordinal) == true,
                string.Join(',', diff?.RemovedElementRefs ?? Array.Empty<string>()),
                "Element removal evaluated from the semantic diff."),
            SemanticVerificationRuleKind.PropertyEquals => PropertyEquals(element, property, expected),
            SemanticVerificationRuleKind.PropertyChanged => new(
                diff?.ChangedProperties.Any(change =>
                    string.Equals(change.SubjectRef, subjectRef, StringComparison.Ordinal) &&
                    string.Equals(change.Property, property, StringComparison.OrdinalIgnoreCase)) == true,
                ChangedValue(diff, subjectRef, property),
                "Property change evaluated from the semantic diff."),
            SemanticVerificationRuleKind.PropertyUnchanged => new(
                diff is not null && !diff.ChangedProperties.Any(change =>
                    string.Equals(change.SubjectRef, subjectRef, StringComparison.Ordinal) &&
                    string.Equals(change.Property, property, StringComparison.OrdinalIgnoreCase)),
                ChangedValue(diff, subjectRef, property),
                "Property stability evaluated from the semantic diff."),
            SemanticVerificationRuleKind.ProcessUnchanged => new(
                diff is not null && !diff.ProcessChanged,
                $"before={before.Application.ProcessNameRedacted};after={after.Application.ProcessNameRedacted}",
                "Process stability evaluated."),
            SemanticVerificationRuleKind.WindowTitleContains => new(
                after.Application.WindowTitleRedacted.Contains(expected, StringComparison.OrdinalIgnoreCase),
                after.Application.WindowTitleRedacted,
                "Window title evaluated."),
            SemanticVerificationRuleKind.StateFingerprintChanged => new(
                diff is not null && !string.Equals(
                    diff.BeforeFingerprint,
                    diff.AfterFingerprint,
                    StringComparison.Ordinal),
                diff is null ? "no-diff" : $"{diff.BeforeFingerprint[..12]}->{diff.AfterFingerprint[..12]}",
                "State fingerprint change evaluated."),
            SemanticVerificationRuleKind.StateFingerprintUnchanged => new(
                diff is not null && string.Equals(
                    diff.BeforeFingerprint,
                    diff.AfterFingerprint,
                    StringComparison.Ordinal),
                diff is null ? "no-diff" : $"{diff.BeforeFingerprint[..12]}->{diff.AfterFingerprint[..12]}",
                "State fingerprint stability evaluated."),
            SemanticVerificationRuleKind.NoBlockingConflicts => new(
                !snapshot.HasBlockingConflicts,
                snapshot.HasBlockingConflicts ? "blocking-conflict" : "none",
                "Blocking perception conflicts evaluated."),
            SemanticVerificationRuleKind.EvidenceRefPresent => new(
                evidenceRefs.Contains(expected, StringComparer.Ordinal),
                expected,
                "Evidence reference presence evaluated."),
            _ => new(false, "unsupported", "Unsupported semantic verification rule.")
        };
    }

    private static RuleMatch PropertyEquals(
        UnifiedElementSnapshot? element,
        string property,
        string expected)
    {
        if (element is null)
            return new RuleMatch(false, "element-absent", "Element is absent.");
        if (!element.CanonicalProperties.TryGetValue(property, out var actual))
            return new RuleMatch(false, "property-absent", "Canonical property is absent.");

        return new RuleMatch(
            string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
            actual,
            "Canonical property value evaluated.");
    }

    private static string ChangedValue(CognitiveSnapshotV2Diff? diff, string subjectRef, string property)
    {
        if (diff is null)
            return "no-diff";
        var change = diff.ChangedProperties.FirstOrDefault(value =>
            string.Equals(value.SubjectRef, subjectRef, StringComparison.Ordinal) &&
            string.Equals(value.Property, property, StringComparison.OrdinalIgnoreCase));
        return change is null
            ? "unchanged"
            : $"{change.BeforeValueRedacted}->{change.AfterValueRedacted}";
    }

    private static SemanticVerificationReport Failure(
        SemanticVerificationContext context,
        IReadOnlyList<SemanticVerificationFact> facts,
        IReadOnlyList<string> evidenceRefs,
        IReadOnlyList<string> reasons,
        SemanticVerificationFailureClass failureClass,
        FailureKind mappedFailureKind,
        bool processVerified = false,
        bool stateTransitionVerified = false,
        bool outcomeVerified = false,
        bool sideEffectsChecked = false,
        bool evidenceComplete = false) =>
        new(
            Success: false,
            ActionExecuted: context.ActionExecuted,
            ProcessVerified: processVerified,
            StateTransitionVerified: stateTransitionVerified,
            OutcomeVerified: outcomeVerified,
            SideEffectsChecked: sideEffectsChecked,
            EvidenceComplete: evidenceComplete,
            FailureClass: failureClass,
            MappedFailureKind: mappedFailureKind,
            FactsObserved: SanitizeFacts(facts),
            EvidenceRefs: evidenceRefs,
            Reasons: SanitizeReasons(reasons));

    private static SemanticVerificationFact Fact(
        string ruleId,
        string stage,
        bool passed,
        string observed,
        string reason) =>
        new(
            SafeRuntimeText.Sanitize(ruleId, 120),
            SafeRuntimeText.Sanitize(stage, 80),
            passed,
            SafeRuntimeText.Sanitize(observed, 500),
            SafeRuntimeText.Sanitize(reason, 500));

    private static IReadOnlyList<SemanticVerificationFact> SanitizeFacts(
        IEnumerable<SemanticVerificationFact> facts) =>
        facts
            .Select(fact => Fact(fact.RuleId, fact.Stage, fact.Passed, fact.ObservedRedacted, fact.Reason))
            .Take(256)
            .ToArray();

    private static IReadOnlyList<string> SanitizeReasons(IEnumerable<string> reasons) =>
        reasons
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => SafeRuntimeText.Sanitize(value, 500))
            .Distinct(StringComparer.Ordinal)
            .Take(128)
            .ToArray();

    private static SemanticVerificationRule SanitizeRule(SemanticVerificationRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return rule with
        {
            RuleId = SafeRuntimeText.Sanitize(rule.RuleId, 120),
            SubjectRef = string.IsNullOrWhiteSpace(rule.SubjectRef)
                ? null
                : SafeRuntimeText.Sanitize(rule.SubjectRef, 160),
            Property = string.IsNullOrWhiteSpace(rule.Property)
                ? null
                : SafeRuntimeText.Sanitize(rule.Property, 80),
            ExpectedValueRedacted = string.IsNullOrWhiteSpace(rule.ExpectedValueRedacted)
                ? null
                : SafeRuntimeText.Sanitize(rule.ExpectedValueRedacted, 500),
            Description = SafeRuntimeText.Sanitize(rule.Description, 500)
        };
    }

    private static void ValidatePlan(SemanticVerificationPlan plan)
    {
        if (string.IsNullOrWhiteSpace(plan.PlanId))
            throw new ArgumentException("Semantic verification plan id is required.", nameof(plan));
        if (plan.Timeout <= TimeSpan.Zero || plan.Timeout > TimeSpan.FromMinutes(10))
            throw new ArgumentOutOfRangeException(nameof(plan), "Semantic verification timeout must be between zero and ten minutes.");
        ArgumentNullException.ThrowIfNull(plan.Preconditions);
        ArgumentNullException.ThrowIfNull(plan.ExpectedTransition);
        ArgumentNullException.ThrowIfNull(plan.ExpectedOutcome);
        ArgumentNullException.ThrowIfNull(plan.ForbiddenSideEffects);
        ArgumentNullException.ThrowIfNull(plan.RequiredEvidenceRefs);

        var ruleIds = plan.Preconditions
            .Concat(plan.ExpectedTransition)
            .Concat(plan.ExpectedOutcome)
            .Concat(plan.ForbiddenSideEffects)
            .Select(rule => SafeRuntimeText.Sanitize(rule.RuleId, 120))
            .Where(value => value.Length > 0)
            .ToArray();
        if (ruleIds.Length != ruleIds.Distinct(StringComparer.Ordinal).Count())
            throw new ArgumentException("Semantic verification rule ids must be unique.", nameof(plan));
    }

    private sealed record RuleEvaluationResult(bool Passed, IReadOnlyList<string> Reasons);

    private sealed record RuleMatch(bool Matched, string Observed, string Reason);
}
