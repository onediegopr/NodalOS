using OneBrain.Core.Approval;
using OneBrain.Core.Recording;

namespace OneBrain.Core.Flows;

public static class CandidateFlowPromotionService
{
    private static readonly string[] BlockedActionTerms =
    [
        "pay",
        "payment",
        "purchase",
        "comprar",
        "pagar",
        "checkout",
        "login",
        "cookie",
        "cookies",
        "delete",
        "borrar",
        "install",
        "script"
    ];

    public static CandidateFlowPromotionResult Promote(CandidateFlowPromotionRequest request)
    {
        var issues = Validate(request);
        if (issues.Count > 0)
        {
            return new CandidateFlowPromotionResult(
                Success: false,
                Status: CandidateFlowStatuses.Rejected,
                Flow: null,
                Issues: issues,
                Notes:
                [
                    "promotion failed closed",
                    "no executable recipe was generated"
                ]);
        }

        var timestamp = NormalizeTimestamp(request.CreatedAtUtc);
        var steps = request.Timeline.Steps.Select((step, index) => BuildPromotedStep(step, index + 1)).ToList();
        var flow = new PromotedCandidateFlow(
            FlowId: $"flow-{SanitizeId(request.CandidateFlowId)}",
            CandidateFlowId: Sanitize(request.CandidateFlowId),
            TimelineId: Sanitize(request.Timeline.TimelineId),
            Title: Sanitize(request.Title),
            Description: Sanitize(request.Description),
            Status: CandidateFlowStatuses.ApprovedForSupervisedPlayback,
            CreatedAtUtc: timestamp,
            UpdatedAtUtc: timestamp,
            RiskLevel: DetermineRisk(steps),
            ConfidenceScore: CalculateConfidence(steps),
            RequiresHumanApproval: steps.Any(step => step.RequiresApproval),
            AllowsAutonomousPlayback: false,
            Variables: request.Variables.Select(Sanitize).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            Steps: steps,
            ArtifactPaths: [],
            Notes: BuildNotes(request));

        return new CandidateFlowPromotionResult(
            Success: true,
            Status: flow.Status,
            Flow: flow,
            Issues: [],
            Notes:
            [
                "candidate flow approved for supervised playback only",
                "no autonomous playback enabled",
                "no executable recipe generated"
            ]);
    }

    private static List<string> Validate(CandidateFlowPromotionRequest request)
    {
        var issues = new List<string>();
        if (string.IsNullOrWhiteSpace(request.CandidateFlowId))
            issues.Add("candidateFlowId is required");
        if (string.IsNullOrWhiteSpace(request.Title))
            issues.Add("title is required");
        if (request.Timeline.Steps.Count == 0)
            issues.Add("timeline has no steps");
        if (!request.LinterPassed)
            issues.Add("linter must pass before promotion");
        if (!request.VariablesResolvedOrDeclared)
            issues.Add("variables must be resolved or declared");
        if (!request.RiskPolicyConsistent)
            issues.Add("risk policy must be consistent");
        if (!request.ApprovalPolicyPresent && request.Timeline.Steps.Any(step => step.RequiresApproval))
            issues.Add("approval policy is required for sensitive steps");
        if (request.HasBlockedActions || request.Timeline.Steps.Any(ContainsBlockedAction))
            issues.Add("blocked action detected");
        if (request.Timeline.Steps.Any(step => step.Confidence < 0.45))
            issues.Add("low confidence step requires more annotation before promotion");

        return issues;
    }

    private static PromotedFlowStep BuildPromotedStep(TimelineStep step, int stepNumber)
    {
        var actionKind = DetermineActionKind(step);
        var requiresApproval = ApprovalPolicy.AlwaysRequiresApproval(actionKind) ||
                               (step.RequiresApproval && actionKind != ApprovalActionKinds.ViewReport);
        var hasSafeExecutor = !requiresApproval && actionKind is ApprovalActionKinds.ViewReport or ApprovalActionKinds.PrepareMessage;
        var executionMode = requiresApproval
            ? CandidateFlowStepExecutionModes.PreviewOnly
            : CandidateFlowStepExecutionModes.FixtureOnly;

        return new PromotedFlowStep(
            StepNumber: stepNumber,
            SourceTimelineStepNumber: step.StepNumber,
            Title: $"Paso {stepNumber}: {Sanitize(step.SuggestedActionLabel)}",
            Description: Sanitize(step.ElementSummary),
            ActionKind: actionKind,
            RiskLevel: NormalizeRisk(step.RiskLevel),
            Confidence: step.Confidence,
            RequiresApproval: requiresApproval,
            HasSafeExecutor: hasSafeExecutor,
            ExecutionMode: executionMode,
            CanSkip: !requiresApproval,
            EvidenceLabels:
            [
                $"window={Sanitize(step.WindowOrApp)}",
                $"event={Sanitize(step.EventType)}"
            ],
            Notes:
            [
                "supervised playback v0 step",
                "no UI automation action is executed by this model"
            ]);
    }

    private static bool ContainsBlockedAction(TimelineStep step)
    {
        var combined = $"{step.SuggestedActionLabel} {step.ElementSummary} {step.EventType}".ToLowerInvariant();
        return BlockedActionTerms.Any(term => combined.Contains(term, StringComparison.OrdinalIgnoreCase)) &&
               !combined.Contains("send", StringComparison.OrdinalIgnoreCase);
    }

    private static string DetermineActionKind(TimelineStep step)
    {
        var combined = $"{step.SuggestedActionLabel} {step.ElementSummary}".ToLowerInvariant();
        if (combined.Contains("send", StringComparison.OrdinalIgnoreCase) || combined.Contains("enviar", StringComparison.OrdinalIgnoreCase))
            return ApprovalActionKinds.Send;
        if (combined.Contains("message", StringComparison.OrdinalIgnoreCase) || combined.Contains("mensaje", StringComparison.OrdinalIgnoreCase))
            return ApprovalActionKinds.PrepareMessage;
        return ApprovalActionKinds.ViewReport;
    }

    private static string DetermineRisk(IReadOnlyList<PromotedFlowStep> steps)
    {
        if (steps.Any(step => step.RiskLevel == ApprovalRiskLevels.Critical))
            return ApprovalRiskLevels.Critical;
        if (steps.Any(step => step.RiskLevel == ApprovalRiskLevels.High))
            return ApprovalRiskLevels.High;
        if (steps.Any(step => step.RiskLevel == ApprovalRiskLevels.Medium))
            return ApprovalRiskLevels.Medium;
        return ApprovalRiskLevels.Low;
    }

    private static int CalculateConfidence(IReadOnlyList<PromotedFlowStep> steps)
    {
        if (steps.Count == 0)
            return 0;

        var average = steps.Average(step => step.Confidence);
        var penalty = steps.Count(step => step.RequiresApproval) * 5;
        return Math.Clamp((int)Math.Round(average * 100, MidpointRounding.AwayFromZero) - penalty, 0, 100);
    }

    private static IReadOnlyList<string> BuildNotes(CandidateFlowPromotionRequest request)
    {
        var notes = request.Notes.Select(Sanitize).Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
        notes.Add("approved for supervised playback only");
        notes.Add("autonomous playback disabled");
        notes.Add("no clicks, login, cookies, cart, purchase, or payment");
        return notes;
    }

    private static string NormalizeRisk(string risk)
    {
        return risk?.ToLowerInvariant() switch
        {
            ApprovalRiskLevels.Low => ApprovalRiskLevels.Low,
            ApprovalRiskLevels.Medium => ApprovalRiskLevels.Medium,
            ApprovalRiskLevels.High => ApprovalRiskLevels.High,
            ApprovalRiskLevels.Critical => ApprovalRiskLevels.Critical,
            _ => ApprovalRiskLevels.Medium
        };
    }

    private static string NormalizeTimestamp(string value)
    {
        return DateTimeOffset.TryParse(value, out var parsed)
            ? parsed.UtcDateTime.ToString("o")
            : DateTimeOffset.UtcNow.UtcDateTime.ToString("o");
    }

    private static string Sanitize(string? value)
    {
        return SensitiveTextSanitizer.Sanitize(value);
    }

    private static string SanitizeId(string value)
    {
        var sanitized = new string(Sanitize(value).Select(c =>
            char.IsLetterOrDigit(c) || c is '-' or '_' ? char.ToLowerInvariant(c) : '-').ToArray()).Trim('-');
        while (sanitized.Contains("--", StringComparison.Ordinal))
            sanitized = sanitized.Replace("--", "-", StringComparison.Ordinal);
        return string.IsNullOrWhiteSpace(sanitized) ? Guid.NewGuid().ToString("N") : sanitized;
    }
}
