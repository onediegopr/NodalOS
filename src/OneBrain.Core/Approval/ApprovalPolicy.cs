using System.Security.Cryptography;
using System.Text;
using OneBrain.Core.Recording;

namespace OneBrain.Core.Approval;

public static class ApprovalPolicy
{
    private static readonly HashSet<string> SensitiveActionKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        ApprovalActionKinds.Send,
        ApprovalActionKinds.Submit,
        ApprovalActionKinds.Delete,
        ApprovalActionKinds.Publish,
        ApprovalActionKinds.Pay,
        ApprovalActionKinds.Purchase,
        ApprovalActionKinds.Login,
        ApprovalActionKinds.AcceptTerms,
        ApprovalActionKinds.AcceptCookies,
        ApprovalActionKinds.ModifyFinancialData,
        ApprovalActionKinds.ModifyLegalData,
        ApprovalActionKinds.RunScript,
        ApprovalActionKinds.InstallSoftware
    };

    public static PlatformApprovalPolicy DefaultPlatformPolicy { get; } = new(
        HumanInTheLoopMode: HumanInTheLoopModes.Conservative,
        MinConfidenceForLowRiskAutoProceed: 85,
        SensitiveActionKinds: SensitiveActionKinds.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToList(),
        CriticalEnvironments: ["production", "live", "financial", "legal"],
        FailClosedWhenMissingInformation: true,
        FailClosedWithoutSafeExecutor: true);

    public static bool AlwaysRequiresApproval(string actionKind)
    {
        return SensitiveActionKinds.Contains(actionKind);
    }

    public static bool RequiresHumanInTheLoop(
        string actionKind,
        string riskLevel,
        int confidenceScore = 0,
        string? environment = null,
        string? profileId = null,
        PlatformApprovalPolicy? policy = null,
        bool hasMissingInformation = false,
        bool hasSafeExecutor = false)
    {
        var effectivePolicy = policy ?? DefaultPlatformPolicy;
        var normalizedRisk = string.IsNullOrWhiteSpace(riskLevel) ? ApprovalRiskLevels.High : riskLevel.ToLowerInvariant();
        var sensitive = effectivePolicy.SensitiveActionKinds.Contains(actionKind, StringComparer.OrdinalIgnoreCase) || AlwaysRequiresApproval(actionKind);
        var criticalEnvironment = effectivePolicy.CriticalEnvironments.Any(item =>
            string.Equals(item, environment, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item, profileId, StringComparison.OrdinalIgnoreCase));

        if (effectivePolicy.FailClosedWhenMissingInformation && hasMissingInformation)
            return true;

        if (effectivePolicy.FailClosedWithoutSafeExecutor && !hasSafeExecutor)
            return true;

        if (sensitive || normalizedRisk is ApprovalRiskLevels.Critical or ApprovalRiskLevels.High || criticalEnvironment)
            return true;

        return effectivePolicy.HumanInTheLoopMode switch
        {
            HumanInTheLoopModes.AlwaysRequired => true,
            HumanInTheLoopModes.ManualOnlyForSensitive => false,
            HumanInTheLoopModes.ConfidenceBased => confidenceScore < effectivePolicy.MinConfidenceForLowRiskAutoProceed,
            _ => confidenceScore < effectivePolicy.MinConfidenceForLowRiskAutoProceed
        };
    }

    public static ApprovalRequest CreateRequest(
        string source,
        string? candidateFlowId,
        string actionKind,
        string riskLevel,
        string title,
        string description,
        string preview,
        IReadOnlyList<string>? missingInformation = null,
        IReadOnlyList<string>? notes = null,
        PlatformApprovalPolicy? policy = null,
        int confidenceScore = 0,
        string? environment = null,
        string? profileId = null,
        bool hasSafeExecutor = false,
        DateTimeOffset? createdAtUtc = null)
    {
        var sanitizedPreview = SensitiveTextSanitizer.Sanitize(preview, out var redacted);
        var missing = missingInformation?.Where(item => !string.IsNullOrWhiteSpace(item)).Select(SensitiveTextSanitizer.Sanitize).ToList() ?? [];
        var requiresApproval = RequiresHumanInTheLoop(
            actionKind,
            riskLevel,
            confidenceScore,
            environment,
            profileId,
            policy,
            hasMissingInformation: missing.Count > 0 || redacted,
            hasSafeExecutor: hasSafeExecutor);
        var effectivePolicy = policy ?? DefaultPlatformPolicy;
        var failClosed = requiresApproval &&
                         ((effectivePolicy.FailClosedWhenMissingInformation && (missing.Count > 0 || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))) ||
                          (effectivePolicy.FailClosedWithoutSafeExecutor && !hasSafeExecutor));

        var allNotes = notes?.ToList() ?? [];
        allNotes.Add("approval records decision only; no action execution is performed");
        allNotes.Add($"human-in-the-loop mode: {effectivePolicy.HumanInTheLoopMode}");
        allNotes.Add("human-in-the-loop is platform configurable globally, per recipe/candidate flow, by confidence, risk, environment, and profile");
        if (redacted)
            allNotes.Add("preview contained sensitive text and was redacted");
        if (failClosed)
            allNotes.Add("fail closed until required information is present");
        if (effectivePolicy.FailClosedWithoutSafeExecutor && !hasSafeExecutor)
            allNotes.Add("fail closed because no safe executor is available");

        var timestamp = (createdAtUtc ?? DateTimeOffset.UtcNow).UtcDateTime.ToString("o");
        return new ApprovalRequest(
            ApprovalRequestId: $"approval-{HashSegment(source, candidateFlowId, actionKind, title, timestamp)}",
            CreatedAtUtc: timestamp,
            Source: SensitiveTextSanitizer.Sanitize(source),
            CandidateFlowId: SensitiveTextSanitizer.Sanitize(candidateFlowId),
            ActionKind: string.IsNullOrWhiteSpace(actionKind) ? "unknown" : actionKind,
            RiskLevel: string.IsNullOrWhiteSpace(riskLevel) ? ApprovalRiskLevels.High : riskLevel,
            Title: SensitiveTextSanitizer.Sanitize(title),
            Description: SensitiveTextSanitizer.Sanitize(description),
            Preview: sanitizedPreview,
            RequiresApproval: requiresApproval,
            FailClosed: failClosed,
            Status: requiresApproval ? "pending" : "not_required",
            MissingInformation: missing,
            Notes: allNotes);
    }

    public static ApprovalRequest FromTimelineStep(RecipeTimeline timeline, TimelineStep step, string actionKind, string preview, DateTimeOffset? createdAtUtc = null)
    {
        var riskLevel = step.RiskLevel == "high" ? ApprovalRiskLevels.High : ApprovalRiskLevels.Medium;
        return CreateRequest(
            source: "candidate_timeline",
            candidateFlowId: timeline.TimelineId,
            actionKind: actionKind,
            riskLevel: riskLevel,
            title: $"Approval required: {step.SuggestedActionLabel}",
            description: "Human approval is required before any sensitive action could be executed by a future safe executor.",
            preview: preview,
            notes:
            [
                "derived from recording/shadow timeline",
                "candidate flow only; no executable recipe generated"
            ],
            createdAtUtc: createdAtUtc);
    }

    public static ApprovalDecision Decide(
        ApprovalRequest request,
        string decision,
        string reason,
        string decidedBy = "human",
        DateTimeOffset? decidedAtUtc = null)
    {
        var normalizedDecision = string.Equals(decision, ApprovalDecisionKinds.Approved, StringComparison.OrdinalIgnoreCase)
            ? ApprovalDecisionKinds.Approved
            : ApprovalDecisionKinds.Rejected;

        var sanitizedReason = SensitiveTextSanitizer.Sanitize(reason);
        if (normalizedDecision == ApprovalDecisionKinds.Rejected && string.IsNullOrWhiteSpace(sanitizedReason))
            sanitizedReason = "rejection reason required";

        return new ApprovalDecision(
            ApprovalDecisionId: $"decision-{Guid.NewGuid():N}",
            ApprovalRequestId: request.ApprovalRequestId,
            DecidedAtUtc: (decidedAtUtc ?? DateTimeOffset.UtcNow).UtcDateTime.ToString("o"),
            Decision: normalizedDecision,
            Reason: sanitizedReason,
            DecidedBy: SensitiveTextSanitizer.Sanitize(decidedBy),
            ExecutionAllowed: false,
            Notes:
            [
                "decision recorded for audit only",
                "executionAllowed remains false because no safe action executor exists in this hito"
            ]);
    }

    private static string HashSegment(params string?[] values)
    {
        var joined = string.Join("|", values.Select(value => value ?? ""));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(joined.ToLowerInvariant()));
        return Convert.ToHexString(bytes)[..12].ToLowerInvariant();
    }
}
