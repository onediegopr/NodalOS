using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsTroubleshootingRecommendationMapper
{
    private readonly IReadOnlyDictionary<NexaFailureKind, NexaTroubleshootingRecommendation> recommendations;

    public NodalOsTroubleshootingRecommendationMapper()
    {
        recommendations = CreateRecommendations().ToDictionary(r => r.FailureKind);
    }

    public NexaTroubleshootingRecommendation GetRecommendation(NexaFailureKind kind) => recommendations[kind];

    public IReadOnlyList<NexaTroubleshootingRecommendation> GetAllRecommendations() =>
        recommendations.Values.OrderBy(r => r.FailureKind).ToArray();

    public bool ValidateCoverage() =>
        Enum.GetValues<NexaFailureKind>().All(recommendations.ContainsKey);

    private static IReadOnlyList<NexaTroubleshootingRecommendation> CreateRecommendations() =>
    [
        Recommendation(NexaFailureKind.SelectorNotFound, "Target selector was not found.", "Re-observe target and update selector evidence.", false, true, true, false),
        Recommendation(NexaFailureKind.SelectorAmbiguous, "Target selector matched multiple candidates.", "Narrow selector with stronger identity evidence.", false, false, true, false),
        Recommendation(NexaFailureKind.NavigationTimeout, "Navigation exceeded timeout.", "Retry once with bounded timeout or replan.", false, true, true, false),
        Recommendation(NexaFailureKind.PageLoadFailed, "Page failed to load.", "Check target availability and retry with limits.", false, true, true, false),
        Recommendation(NexaFailureKind.LoginRequired, "Login is required.", "Ask human to authenticate in an approved surface.", true, false, false, true),
        Recommendation(NexaFailureKind.CaptchaDetected, "Captcha was detected.", "Stop automation and ask human to resolve.", true, false, false, true),
        Recommendation(NexaFailureKind.TwoFactorRequired, "Two-factor authentication is required.", "Stop automation and ask human to complete 2FA.", true, false, false, true),
        Recommendation(NexaFailureKind.PolicyBlocked, "Policy blocked the step.", "Stop and review policy decision evidence.", false, false, false, true),
        Recommendation(NexaFailureKind.ApprovalRequired, "Human approval is required.", "Pause and request explicit human approval.", true, false, false, true),
        Recommendation(NexaFailureKind.NoProgressDetected, "Run made no observable progress.", "Replan or ask human if repeated.", false, false, true, false),
        Recommendation(NexaFailureKind.RepeatedActionDetected, "Repeated action loop was detected.", "Stop repetition and require replan.", false, false, false, true),
        Recommendation(NexaFailureKind.RuntimeDisconnected, "Runtime disconnected.", "Reconnect runtime once within limits, then replan.", false, true, true, false),
        Recommendation(NexaFailureKind.ContentScriptUnreachable, "Content script was unreachable.", "Refresh runtime context or replan.", false, true, true, false),
        Recommendation(NexaFailureKind.TabCrashed, "Browser tab crashed.", "Stop current run and collect crash evidence.", false, false, true, true),
        Recommendation(NexaFailureKind.NetworkUnavailable, "Network was unavailable.", "Retry after network health check.", false, true, true, false),
        Recommendation(NexaFailureKind.DownloadBlocked, "Download was blocked.", "Stop and review download policy.", true, false, false, true),
        Recommendation(NexaFailureKind.UploadBlocked, "Upload was blocked.", "Stop and review upload policy.", true, false, false, true),
        Recommendation(NexaFailureKind.SensitiveDataRisk, "Sensitive data risk was detected.", "Stop or request explicit approval according to policy.", true, false, false, true),
        Recommendation(NexaFailureKind.HumanInputRequired, "Human input is required.", "Pause and request human input.", true, false, false, true),
        Recommendation(NexaFailureKind.ExternalServiceUnavailable, "External service was unavailable.", "Retry with limits or wait for service recovery.", false, true, true, false),
        Recommendation(NexaFailureKind.Unknown, "Unknown failure.", "Retry only within bounded limits, then replan.", false, true, true, false)
    ];

    private static NexaTroubleshootingRecommendation Recommendation(
        NexaFailureKind kind,
        string cause,
        string action,
        bool requiresHuman,
        bool canRetry,
        bool canReplan,
        bool mustStop) =>
        new()
        {
            FailureKind = kind,
            HumanReadableCause = cause,
            SuggestedAction = action,
            RequiresHumanInput = requiresHuman,
            CanRetryAutomatically = canRetry,
            CanReplan = canReplan,
            MustStop = mustStop
        };
}

public sealed class NodalOsRunReportBuilder
{
    private readonly NodalOsTroubleshootingRecommendationMapper troubleshooting = new();
    private readonly NodalOsVerificationBeforeDoneGate completionGate = new();
    private readonly List<NexaRunStepReport> steps = [];
    private readonly List<NexaPolicyDecisionReport> policyDecisions = [];
    private readonly List<NexaApprovalReport> approvals = [];
    private readonly List<NexaFailureReport> failures = [];
    private readonly List<NexaEvidenceRef> evidenceRefs = [];
    private string? runId;
    private string? missionId;
    private string? taskId;
    private string? recipeId;
    private string? goal;
    private NexaRunStatus status = NexaRunStatus.Planned;
    private DateTimeOffset startedAt = NodalOsRunReportFixtures.FixedTimestamp;
    private DateTimeOffset? completedAt;
    private string? finalSummary;

    public NodalOsRunReportBuilder Start(
        string runId,
        string goal,
        NexaRunStatus status = NexaRunStatus.Running,
        string? missionId = null,
        string? taskId = null,
        string? recipeId = null,
        DateTimeOffset? startedAt = null)
    {
        this.runId = runId;
        this.goal = goal;
        this.status = status;
        this.missionId = missionId;
        this.taskId = taskId;
        this.recipeId = recipeId;
        this.startedAt = startedAt ?? NodalOsRunReportFixtures.FixedTimestamp;
        return this;
    }

    public NodalOsRunReportBuilder AddStep(NexaRunStepReport step)
    {
        steps.Add(step);
        evidenceRefs.AddRange(step.EvidenceRefs);
        return this;
    }

    public NodalOsRunReportBuilder AddFailure(NexaFailureReport failure)
    {
        failures.Add(failure);
        evidenceRefs.AddRange(failure.EvidenceRefs);
        return this;
    }

    public NodalOsRunReportBuilder AddPolicyDecision(NexaPolicyDecisionReport decision)
    {
        policyDecisions.Add(decision);
        return this;
    }

    public NodalOsRunReportBuilder AddApproval(NexaApprovalReport approval)
    {
        approvals.Add(approval);
        return this;
    }

    public NodalOsRunReportBuilder AddEvidence(NexaEvidenceRef evidence)
    {
        evidenceRefs.Add(evidence);
        return this;
    }

    public NodalOsRunReportBuilder Complete(NexaRunStatus status, string summary, DateTimeOffset? completedAt = null)
    {
        this.status = status;
        this.finalSummary = summary;
        this.completedAt = completedAt ?? NodalOsRunReportFixtures.FixedTimestamp.AddMinutes(1);
        return this;
    }

    public NexaRunReport Build()
    {
        var report = new NexaRunReport
        {
            RunId = Required(runId, "RunId"),
            MissionId = missionId,
            TaskId = taskId,
            RecipeId = recipeId,
            Goal = Required(goal, "Goal"),
            Status = status,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            Steps = steps.ToArray(),
            PolicyDecisions = policyDecisions.ToArray(),
            Approvals = approvals.ToArray(),
            Failures = failures.ToArray(),
            EvidenceRefs = evidenceRefs
                .GroupBy(e => e.EvidenceId, StringComparer.Ordinal)
                .Select(g => g.First())
                .ToArray(),
            FinalSummary = finalSummary
        };

        var validation = Validate(report);
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join(" ", validation.Errors));

        return report;
    }

    public NexaRunReport CreateSuccessfulRun(string runId, string goal, string? missionId = null, string? taskId = null, string? recipeId = null) =>
        Start(runId, goal, missionId: missionId, taskId: taskId, recipeId: recipeId)
            .AddStep(NodalOsRunReportFixtures.Step(status: NexaRunStepStatus.Completed))
            .AddEvidence(NodalOsRunReportFixtures.Evidence("evidence-success"))
            .Complete(NexaRunStatus.Completed, "run completed")
            .Build();

    public NexaRunReport CreateBlockedByPolicyRun(string runId, string goal) =>
        Start(runId, goal, status: NexaRunStatus.Blocked)
            .AddStep(NodalOsRunReportFixtures.Step(status: NexaRunStepStatus.Blocked))
            .AddPolicyDecision(NodalOsRunReportFixtures.PolicyDecision(decision: "Blocked"))
            .AddFailure(NodalOsRunReportFixtures.Failure(
                kind: NexaFailureKind.PolicyBlocked,
                severity: NexaFailureSeverity.Blocking,
                suggestedRecovery: troubleshooting.GetRecommendation(NexaFailureKind.PolicyBlocked).SuggestedAction))
            .Complete(NexaRunStatus.Blocked, "run blocked by policy")
            .Build();

    public NexaRunReport CreateFailedRun(string runId, string goal, NexaFailureKind kind = NexaFailureKind.SelectorNotFound) =>
        Start(runId, goal, status: NexaRunStatus.Failed)
            .AddStep(NodalOsRunReportFixtures.Step(status: NexaRunStepStatus.Failed))
            .AddFailure(NodalOsRunReportFixtures.Failure(
                kind: kind,
                severity: NexaFailureSeverity.Blocking,
                suggestedRecovery: troubleshooting.GetRecommendation(kind).SuggestedAction))
            .Complete(NexaRunStatus.Failed, "run failed")
            .Build();

    public NexaRunReportValidationResult Validate(NexaRunReport report)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, report.RunId, "RunId is required.");
        AddRequired(errors, report.Goal, "Goal is required.");

        if (IsTerminal(report.Status) && report.CompletedAt is null)
            errors.Add("CompletedAt is required for terminal run states.");

        if (report.Steps.Count == 0 && report.Status is not (NexaRunStatus.Planned or NexaRunStatus.Cancelled))
            errors.Add("RunReport requires at least one step except for planned or cancelled reports.");

        if (report.Status is NexaRunStatus.Failed && report.Failures.Count == 0)
            errors.Add("Failed run requires at least one FailureReport.");

        if (report.Status is NexaRunStatus.Blocked &&
            report.Failures.Count == 0 &&
            report.PolicyDecisions.Count == 0)
            errors.Add("Blocked run requires a FailureReport or policy decision.");

        if (report.Status is NexaRunStatus.Completed or NexaRunStatus.CompletedWithWarnings)
        {
            var completionResult = completionGate.EvaluateRunReport(report);
            if (!completionResult.CanMarkDone)
                errors.AddRange(completionResult.Errors.Select(error => $"Run completion gate: {error}"));

            warnings.AddRange(completionResult.Warnings.Select(warning => $"Run completion gate: {warning}"));
        }

        if (!NodalOsRunReportSanitizer.IsSafe(report))
            errors.Add("RunReport contains sensitive fields.");

        return new NexaRunReportValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    private static bool IsTerminal(NexaRunStatus status) =>
        status is NexaRunStatus.Completed or
            NexaRunStatus.CompletedWithWarnings or
            NexaRunStatus.Failed or
            NexaRunStatus.Cancelled or
            NexaRunStatus.Blocked;

    private static string Required(string? value, string name) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"{name} is required.")
            : value;

    private static void AddRequired(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(message);
    }
}

public static class NodalOsRunReportSanitizer
{
    private static readonly NodalOsRedactionService Redaction = new();

    public static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        return Redaction.RedactValue(value).Value;
    }

    public static bool IsSafe(NexaRunReport report)
    {
        var values = new List<string?>();
        values.Add(report.Goal);
        values.Add(report.FinalSummary);
        values.AddRange(report.Steps.SelectMany(s => new[] { s.Label, s.ActionKind, s.TargetUrl, s.TargetSelector, s.Notes }));
        values.AddRange(report.PolicyDecisions.SelectMany(d => new[] { d.PolicyName, d.Decision, d.Reason }));
        values.AddRange(report.Approvals.SelectMany(a => new[] { a.ApprovalKind, a.Status, a.HumanActor }));
        values.AddRange(report.Failures.SelectMany(f => new[] { f.Summary, f.Detail, f.SuggestedRecovery }));
        values.AddRange(report.EvidenceRefs.SelectMany(e => new[] { e.Kind, e.Ref, e.Hash }));

        return values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .All(v => IsSafeValue(v!));
    }

    public static bool IsSafeValue(string value) =>
        !Redaction.ContainsSensitiveContent(value);
}

public static class NodalOsRunReportFixtures
{
    public static readonly DateTimeOffset FixedTimestamp = new(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);

    public static NexaRunStepReport Step(
        string stepId = "step-001",
        int index = 0,
        NexaRunStepStatus status = NexaRunStepStatus.Completed) =>
        new()
        {
            StepId = stepId,
            Index = index,
            Label = "Read-only fixture step",
            Status = status,
            ActionKind = "read-only-observe",
            TargetUrl = "https://example.invalid/fixture",
            TargetSelector = "#fixture",
            StartedAt = FixedTimestamp,
            CompletedAt = status is NexaRunStepStatus.Completed or NexaRunStepStatus.Blocked or NexaRunStepStatus.Failed
                ? FixedTimestamp.AddSeconds(5)
                : null,
            EvidenceRefs = [Evidence($"evidence-{stepId}")],
            Notes = "No runtime action executed."
        };

    public static NexaPolicyDecisionReport PolicyDecision(
        string decisionId = "policy-decision-001",
        string stepId = "step-001",
        string decision = "Allowed") =>
        new()
        {
            DecisionId = decisionId,
            StepId = stepId,
            PolicyName = "NODAL OS runtime policy",
            Decision = decision,
            Reason = "Controlled fixture policy decision.",
            CreatedAt = FixedTimestamp
        };

    public static NexaApprovalReport Approval(
        string approvalId = "approval-001",
        string stepId = "step-001",
        string status = "NotRequested") =>
        new()
        {
            ApprovalId = approvalId,
            StepId = stepId,
            ApprovalKind = "human-approval",
            Status = status,
            HumanActor = status == "Approved" ? "operator" : null,
            CreatedAt = FixedTimestamp
        };

    public static NexaFailureReport Failure(
        string failureId = "failure-001",
        string stepId = "step-001",
        NexaFailureKind kind = NexaFailureKind.SelectorNotFound,
        NexaFailureSeverity severity = NexaFailureSeverity.Blocking,
        string? suggestedRecovery = null) =>
        new()
        {
            FailureId = failureId,
            StepId = stepId,
            Kind = kind,
            Summary = $"{kind} fixture.",
            Detail = "Controlled failure fixture.",
            Severity = severity,
            SuggestedRecovery = suggestedRecovery ?? "Review evidence and replan.",
            EvidenceRefs = [Evidence($"evidence-{failureId}")],
            CreatedAt = FixedTimestamp
        };

    public static NexaEvidenceRef Evidence(string evidenceId = "evidence-run-001") =>
        new()
        {
            EvidenceId = evidenceId,
            Kind = "run-report",
            Ref = $"artifact://agent-operations/{evidenceId}",
            Hash = "sha256:fixture",
            CreatedAt = FixedTimestamp
        };
}
