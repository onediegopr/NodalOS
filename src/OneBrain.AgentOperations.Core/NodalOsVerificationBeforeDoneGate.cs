using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsVerificationBeforeDoneGate
{
    public NodalOsVerificationBeforeDoneResult EvaluateTask(
        NexaAgentTask task,
        NodalOsVerificationBeforeDoneOptions? options = null)
    {
        options ??= new NodalOsVerificationBeforeDoneOptions();
        var errors = new List<string>();
        var warnings = new List<string>();
        var evidenceRefs = CollectTaskEvidence(task);
        var verificationLabels = task.VerificationChecks
            .Select(c => c.Label)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        AddRequired(errors, task.TaskId, "TaskId is required.");
        AddRequired(errors, task.MissionId, "Task MissionId is required.");
        AddRequired(errors, task.Title, "Task title is required.");
        AddRequired(errors, task.HumanOwner, "Task human owner is required.");

        if (task.Status != NexaAgentTaskStatus.Completed)
            warnings.Add($"Task is evaluated as a prospective completion candidate; current status is {task.Status}.");

        if (options.BlockOnBlockingOrCriticalBlockers)
        {
            foreach (var blocker in task.Blockers.Where(IsBlockingOrCritical))
                errors.Add($"Task {task.TaskId} has {blocker.Severity} blocker {blocker.BlockerId}.");
        }

        if (options.RequirePassedRequiredVerification)
        {
            foreach (var check in task.VerificationChecks.Where(c => c.Required))
            {
                if (check.Status == NexaVerificationStatus.Pending)
                    errors.Add($"Required verification {check.CheckId} is pending.");

                if (check.Status == NexaVerificationStatus.Failed)
                    errors.Add($"Required verification {check.CheckId} failed.");

                if (check.Status == NexaVerificationStatus.SkippedWithReason && string.IsNullOrWhiteSpace(check.Detail))
                    errors.Add($"Required verification {check.CheckId} was skipped without a reason.");
            }
        }

        if (options.RequireEvidenceOrReason &&
            evidenceRefs.Count == 0 &&
            string.IsNullOrWhiteSpace(task.CompletionReason))
        {
            errors.Add("Done requires evidence refs or an explicit completion reason.");
        }

        return new NodalOsVerificationBeforeDoneResult
        {
            CanMarkDone = errors.Count == 0,
            SubjectKind = NodalOsVerificationBeforeDoneSubjectKind.AgentTask,
            SubjectId = task.TaskId,
            Errors = errors,
            Warnings = warnings,
            EvidenceRefs = evidenceRefs,
            VerificationLabels = verificationLabels,
            CompletionReason = SafeReason(task.CompletionReason)
        };
    }

    public NodalOsVerificationBeforeDoneResult EvaluateMission(
        NexaMission mission,
        NodalOsVerificationBeforeDoneOptions? options = null)
    {
        options ??= new NodalOsVerificationBeforeDoneOptions();
        var errors = new List<string>();
        var warnings = new List<string>();
        var evidenceRefs = mission.EvidenceRefs.ToList();
        var verificationLabels = new List<string>();

        AddRequired(errors, mission.MissionId, "MissionId is required.");
        AddRequired(errors, mission.HumanOwner, "Mission human owner is required.");

        if (mission.Status != NexaMissionStatus.Completed)
            warnings.Add($"Mission is evaluated as a prospective completion candidate; current status is {mission.Status}.");

        if (mission.Tasks.Count == 0 && mission.EvidenceRefs.Count == 0)
            errors.Add("Mission done requires tasks or mission evidence.");

        foreach (var task in mission.Tasks)
        {
            var taskResult = EvaluateTask(task, options);
            evidenceRefs.AddRange(taskResult.EvidenceRefs);
            verificationLabels.AddRange(taskResult.VerificationLabels);

            foreach (var error in taskResult.Errors)
                errors.Add($"Task {task.TaskId}: {error}");

            foreach (var warning in taskResult.Warnings)
                warnings.Add($"Task {task.TaskId}: {warning}");
        }

        return new NodalOsVerificationBeforeDoneResult
        {
            CanMarkDone = errors.Count == 0,
            SubjectKind = NodalOsVerificationBeforeDoneSubjectKind.Mission,
            SubjectId = mission.MissionId,
            Errors = errors,
            Warnings = warnings,
            EvidenceRefs = DistinctEvidence(evidenceRefs),
            VerificationLabels = verificationLabels.Distinct(StringComparer.Ordinal).ToArray(),
            CompletionReason = null
        };
    }

    public NodalOsVerificationBeforeDoneResult EvaluateRunReport(
        NexaRunReport report,
        NodalOsVerificationBeforeDoneOptions? options = null)
    {
        options ??= new NodalOsVerificationBeforeDoneOptions();
        var errors = new List<string>();
        var warnings = new List<string>();
        var evidenceRefs = CollectRunEvidence(report);

        AddRequired(errors, report.RunId, "RunId is required.");
        AddRequired(errors, report.Goal, "Run goal is required.");

        switch (report.Status)
        {
            case NexaRunStatus.Completed:
                ValidateCompletedRun(report, options, errors);
                break;
            case NexaRunStatus.CompletedWithWarnings:
                if (!options.AllowCompletedWithWarnings)
                    errors.Add("CompletedWithWarnings is not allowed by gate options.");
                ValidateCompletedRun(report, options, errors);
                ValidateCompletedWithWarnings(report, errors, warnings);
                break;
            case NexaRunStatus.Failed:
            case NexaRunStatus.Blocked:
            case NexaRunStatus.Cancelled:
                errors.Add($"{report.Status} run is terminal but cannot be marked done-success.");
                break;
            case NexaRunStatus.AwaitingApproval:
            case NexaRunStatus.Running:
            case NexaRunStatus.Paused:
            case NexaRunStatus.Planned:
                errors.Add($"{report.Status} run is not complete.");
                break;
        }

        if (options.RequireEvidenceOrReason &&
            evidenceRefs.Count == 0 &&
            string.IsNullOrWhiteSpace(report.FinalSummary))
        {
            errors.Add("Done run requires evidence refs or final summary.");
        }

        return new NodalOsVerificationBeforeDoneResult
        {
            CanMarkDone = errors.Count == 0,
            SubjectKind = NodalOsVerificationBeforeDoneSubjectKind.RunReport,
            SubjectId = report.RunId,
            Errors = errors,
            Warnings = warnings,
            EvidenceRefs = evidenceRefs,
            VerificationLabels = report.Steps.Select(s => s.Label).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.Ordinal).ToArray(),
            CompletionReason = SafeReason(report.FinalSummary)
        };
    }

    private static void ValidateCompletedRun(
        NexaRunReport report,
        NodalOsVerificationBeforeDoneOptions options,
        List<string> errors)
    {
        if (options.RequireRunTerminalTimestamp && report.CompletedAt is null)
            errors.Add("Completed run requires CompletedAt.");

        foreach (var step in report.Steps)
        {
            if (step.Status == NexaRunStepStatus.Skipped)
            {
                if (string.IsNullOrWhiteSpace(step.Notes))
                    errors.Add($"Skipped step {step.StepId} requires reason in Notes.");

                continue;
            }

            if (step.Status != NexaRunStepStatus.Completed)
                errors.Add($"Step {step.StepId} status {step.Status} prevents done.");
        }

        foreach (var failure in report.Failures.Where(IsBlockingOrCriticalFailure))
            errors.Add($"Failure {failure.FailureId} severity {failure.Severity} prevents done.");

        foreach (var decision in report.PolicyDecisions.Where(IsBlockingPolicyDecision))
            errors.Add($"Policy decision {decision.DecisionId} prevents done.");

        foreach (var approval in report.Approvals.Where(IsPendingOrDeniedApproval))
            errors.Add($"Approval {approval.ApprovalId} status {approval.Status} prevents done.");
    }

    private static void ValidateCompletedWithWarnings(
        NexaRunReport report,
        List<string> errors,
        List<string> warnings)
    {
        var hasNonCriticalFailure = report.Failures.Any(f =>
            f.Severity is NexaFailureSeverity.Info or NexaFailureSeverity.Warning or NexaFailureSeverity.Recoverable);
        var hasWarningNote = report.Steps.Any(s => !string.IsNullOrWhiteSpace(s.Notes)) ||
                             !string.IsNullOrWhiteSpace(report.FinalSummary);

        if (!hasNonCriticalFailure && !hasWarningNote)
            errors.Add("CompletedWithWarnings requires warning/recoverable failure, notes, or final summary.");

        if (hasNonCriticalFailure)
            warnings.Add("Run completed with non-critical failure evidence.");
    }

    private static IReadOnlyList<NexaEvidenceRef> CollectTaskEvidence(NexaAgentTask task)
    {
        var refs = new List<NexaEvidenceRef>();
        refs.AddRange(task.EvidenceRefs);
        refs.AddRange(task.ProgressNotes.SelectMany(n => n.EvidenceRefs));
        refs.AddRange(task.Blockers.SelectMany(b => b.EvidenceRefs));
        refs.AddRange(task.VerificationChecks.SelectMany(c => c.EvidenceRefs));
        return DistinctEvidence(refs);
    }

    private static IReadOnlyList<NexaEvidenceRef> CollectRunEvidence(NexaRunReport report)
    {
        var refs = new List<NexaEvidenceRef>();
        refs.AddRange(report.EvidenceRefs);
        refs.AddRange(report.Steps.SelectMany(s => s.EvidenceRefs));
        refs.AddRange(report.Failures.SelectMany(f => f.EvidenceRefs));
        return DistinctEvidence(refs);
    }

    private static IReadOnlyList<NexaEvidenceRef> DistinctEvidence(IEnumerable<NexaEvidenceRef> evidenceRefs) =>
        evidenceRefs
            .Where(e => !string.IsNullOrWhiteSpace(e.EvidenceId))
            .GroupBy(e => e.EvidenceId, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToArray();

    private static bool IsBlockingOrCritical(NexaBlockerReport blocker) =>
        blocker.Severity is NexaBlockerSeverity.Blocking or NexaBlockerSeverity.Critical;

    private static bool IsBlockingOrCriticalFailure(NexaFailureReport failure) =>
        failure.Severity is NexaFailureSeverity.Blocking or NexaFailureSeverity.Critical;

    private static bool IsBlockingPolicyDecision(NexaPolicyDecisionReport decision) =>
        ContainsAny(decision.Decision, "blocked", "denied", "rejected", "disallowed") ||
        ContainsAny(decision.Reason, "blocked", "denied", "rejected", "disallowed");

    private static bool IsPendingOrDeniedApproval(NexaApprovalReport approval) =>
        !approval.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase) &&
        !approval.Status.Equals("NotRequested", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsAny(string? value, params string[] markers) =>
        !string.IsNullOrWhiteSpace(value) &&
        markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static string? SafeReason(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : NodalOsRunReportSanitizer.Sanitize(value);

    private static void AddRequired(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(message);
    }
}

public static class NodalOsCompletionGateCompatibilityAdapter
{
    public static NexaTaskValidationResult ToTaskValidationResult(
        NodalOsVerificationBeforeDoneResult result,
        bool useWorkboardCompatibilityMessages = true) =>
        new()
        {
            CanComplete = result.CanMarkDone,
            Errors = useWorkboardCompatibilityMessages
                ? ToWorkboardTaskCompletionErrors(result.Errors)
                : result.Errors,
            Warnings = result.Warnings
        };

    public static IReadOnlyList<string> ToWorkboardTaskCompletionErrors(IReadOnlyList<string> gateErrors)
    {
        var errors = new List<string>();

        foreach (var error in gateErrors)
        {
            if (ContainsAny(error, " blocker "))
            {
                AddDistinct(errors, "Blocking or critical blocker prevents task completion.");
                continue;
            }

            if (ContainsAny(error, " is pending.", " failed."))
            {
                AddDistinct(errors, "Pending or failed required verification prevents task completion.");
                continue;
            }

            if (ContainsAny(error, "skipped without a reason"))
            {
                AddDistinct(errors, "Skipped required verification must include a reason.");
                continue;
            }

            if (ContainsAny(error, "Done requires evidence refs or an explicit completion reason."))
            {
                AddDistinct(errors, "Completed task requires evidence or explicit completion reason.");
                continue;
            }

            AddDistinct(errors, error);
        }

        return errors;
    }

    private static void AddDistinct(List<string> values, string value)
    {
        if (!values.Contains(value, StringComparer.Ordinal))
            values.Add(value);
    }

    private static bool ContainsAny(string value, params string[] markers) =>
        markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
}
