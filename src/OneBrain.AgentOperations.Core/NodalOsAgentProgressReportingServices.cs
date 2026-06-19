using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsAgentProgressReportValidator
{
    public NodalOsAgentProgressReportValidationResult Validate(NodalOsAgentProgressReport report)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, report.ReportId, "ReportId is required.");
        AddRequired(errors, report.Summary, "Summary is required.");

        if (report.CreatedAt == default)
            errors.Add("CreatedAt is required.");

        if (report.Kind != NodalOsAgentProgressReportKind.Diagnostic &&
            string.IsNullOrWhiteSpace(report.MissionId) &&
            string.IsNullOrWhiteSpace(report.TaskId) &&
            string.IsNullOrWhiteSpace(report.RunId))
        {
            errors.Add("Report requires MissionId, TaskId, or RunId unless it is Diagnostic.");
        }

        if (report.Kind == NodalOsAgentProgressReportKind.Blocker && report.Blockers.Count == 0)
            errors.Add("Blocker report kind requires at least one blocker.");

        if (report.Kind == NodalOsAgentProgressReportKind.CompletionCandidate &&
            report.VerificationSummaries.Count == 0)
        {
            errors.Add("Completion candidate requires at least one verification summary.");
        }

        if (report.Status == NodalOsAgentProgressReportStatus.WaitingForHuman &&
            report.HumanDecisionRequests.Count == 0)
        {
            errors.Add("WaitingForHuman report requires a human decision request.");
        }

        if (report.Status == NodalOsAgentProgressReportStatus.WaitingForApproval &&
            !report.HumanDecisionRequests.Any(r => r.Kind == NodalOsHumanDecisionKind.ApprovalRequired))
        {
            errors.Add("WaitingForApproval report requires an ApprovalRequired human decision request.");
        }

        if (report.Kind == NodalOsAgentProgressReportKind.Handoff &&
            string.IsNullOrWhiteSpace(report.Detail) &&
            report.EvidenceRefs.Count == 0)
        {
            errors.Add("Handoff report requires detail or evidence.");
        }

        if (!NodalOsAgentProgressReportSanitizer.IsSafe(report))
            errors.Add("Progress report contains sensitive fields.");

        var readyResult = ValidateReadyToClose(report);
        warnings.AddRange(readyResult.Warnings);

        if (report.Status == NodalOsAgentProgressReportStatus.ReadyToClose && !readyResult.ReadyToClose)
            errors.AddRange(readyResult.Errors);

        return new NodalOsAgentProgressReportValidationResult
        {
            IsValid = errors.Count == 0,
            ReadyToClose = errors.Count == 0 && readyResult.ReadyToClose,
            Errors = errors.Distinct(StringComparer.Ordinal).ToArray(),
            Warnings = warnings.Distinct(StringComparer.Ordinal).ToArray()
        };
    }

    public NodalOsAgentProgressReportValidationResult ValidateReadyToClose(NodalOsAgentProgressReport report)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (report.Status != NodalOsAgentProgressReportStatus.ReadyToClose)
            errors.Add("ReadyToClose requires report status ReadyToClose.");

        foreach (var blocker in report.Blockers.Where(IsBlockingOrCritical))
            errors.Add($"{blocker.Severity} blocker {blocker.BlockerId} prevents ready-to-close.");

        if (report.VerificationSummaries.Count == 0)
            errors.Add("ReadyToClose requires at least one verification summary.");

        foreach (var summary in report.VerificationSummaries.Where(v => !v.CanMarkDone))
            errors.Add($"Verification summary for {summary.SubjectKind} {summary.SubjectId} is not ready to close.");

        if (report.VerificationSummaries.Count > 0 && !report.VerificationSummaries.Any(v => v.CanMarkDone))
            errors.Add("ReadyToClose requires at least one positive verification summary.");

        if (report.HumanDecisionRequests.Any(r => r.Urgency == NodalOsHumanDecisionUrgency.Blocking))
            errors.Add("Blocking human decision request prevents ready-to-close.");

        var hasEvidence = CollectEvidence(report).Count > 0;
        if (!hasEvidence && string.IsNullOrWhiteSpace(report.Detail) && string.IsNullOrWhiteSpace(report.Summary))
            errors.Add("ReadyToClose requires evidence, detail, or summary.");

        if (report.Warnings.Count > 0)
            warnings.Add("Report includes warning entries.");

        return new NodalOsAgentProgressReportValidationResult
        {
            IsValid = errors.Count == 0,
            ReadyToClose = errors.Count == 0,
            Errors = errors.Distinct(StringComparer.Ordinal).ToArray(),
            Warnings = warnings.Distinct(StringComparer.Ordinal).ToArray()
        };
    }

    internal static IReadOnlyList<NexaEvidenceRef> CollectEvidence(NodalOsAgentProgressReport report)
    {
        var evidence = new List<NexaEvidenceRef>();
        evidence.AddRange(report.EvidenceRefs);
        evidence.AddRange(report.ProgressNotes.SelectMany(n => n.EvidenceRefs));
        evidence.AddRange(report.Blockers.SelectMany(b => b.EvidenceRefs));
        evidence.AddRange(report.HumanDecisionRequests.SelectMany(r => r.EvidenceRefs));
        evidence.AddRange(report.VerificationSummaries.SelectMany(v => v.EvidenceRefs));
        return DistinctEvidence(evidence);
    }

    private static IReadOnlyList<NexaEvidenceRef> DistinctEvidence(IEnumerable<NexaEvidenceRef> evidenceRefs) =>
        evidenceRefs
            .Where(e => !string.IsNullOrWhiteSpace(e.EvidenceId))
            .GroupBy(e => e.EvidenceId, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToArray();

    private static bool IsBlockingOrCritical(NexaBlockerReport blocker) =>
        blocker.Severity is NexaBlockerSeverity.Blocking or NexaBlockerSeverity.Critical;

    private static void AddRequired(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(message);
    }
}

public sealed class NodalOsAgentProgressReportBuilder
{
    public NodalOsAgentProgressReport CreateProgress(
        string reportId,
        string missionId,
        string taskId,
        string summary,
        IReadOnlyList<NexaEvidenceRef>? evidenceRefs = null) =>
        new()
        {
            ReportId = reportId,
            MissionId = missionId,
            TaskId = taskId,
            Kind = NodalOsAgentProgressReportKind.Progress,
            Status = NodalOsAgentProgressReportStatus.InProgress,
            Summary = summary,
            ReportingAgent = "codex",
            HumanOwner = "operator",
            EvidenceRefs = evidenceRefs ?? [],
            CreatedAt = NodalOsAgentProgressReportFixtures.FixedTimestamp
        };

    public NodalOsAgentProgressReport CreateBlocker(
        string reportId,
        string missionId,
        string taskId,
        NexaBlockerReport blocker) =>
        new()
        {
            ReportId = reportId,
            MissionId = missionId,
            TaskId = taskId,
            Kind = NodalOsAgentProgressReportKind.Blocker,
            Status = blocker.Severity is NexaBlockerSeverity.Blocking or NexaBlockerSeverity.Critical
                ? NodalOsAgentProgressReportStatus.Blocked
                : NodalOsAgentProgressReportStatus.InProgress,
            Summary = blocker.Summary,
            Detail = blocker.Detail,
            ReportingAgent = "codex",
            HumanOwner = "operator",
            Blockers = [blocker],
            EvidenceRefs = blocker.EvidenceRefs,
            CreatedAt = NodalOsAgentProgressReportFixtures.FixedTimestamp
        };

    public NodalOsAgentProgressReport CreateCompletionCandidate(
        string reportId,
        NodalOsVerificationBeforeDoneResult result) =>
        new()
        {
            ReportId = reportId,
            MissionId = result.SubjectKind == NodalOsVerificationBeforeDoneSubjectKind.Mission ? result.SubjectId : null,
            TaskId = result.SubjectKind == NodalOsVerificationBeforeDoneSubjectKind.AgentTask ? result.SubjectId : null,
            RunId = result.SubjectKind == NodalOsVerificationBeforeDoneSubjectKind.RunReport ? result.SubjectId : null,
            Kind = NodalOsAgentProgressReportKind.CompletionCandidate,
            Status = result.CanMarkDone
                ? NodalOsAgentProgressReportStatus.ReadyToClose
                : NodalOsAgentProgressReportStatus.NotReadyToClose,
            Summary = result.CanMarkDone ? "Verification gate passed." : "Verification gate did not pass.",
            Detail = result.CompletionReason,
            ReportingAgent = "codex",
            HumanOwner = "operator",
            VerificationSummaries = [FromVerificationBeforeDoneResult(result)],
            EvidenceRefs = result.EvidenceRefs,
            Warnings = result.Warnings.Select((w, i) => NodalOsAgentProgressReportFixtures.Warning($"warning-{i + 1}", w)).ToArray(),
            CreatedAt = NodalOsAgentProgressReportFixtures.FixedTimestamp
        };

    public NodalOsAgentProgressReport CreateHandoff(
        string reportId,
        string missionId,
        string taskId,
        string detail,
        IReadOnlyList<NexaEvidenceRef>? evidenceRefs = null) =>
        new()
        {
            ReportId = reportId,
            MissionId = missionId,
            TaskId = taskId,
            Kind = NodalOsAgentProgressReportKind.Handoff,
            Status = NodalOsAgentProgressReportStatus.WaitingForHuman,
            Summary = "Human handoff required.",
            Detail = detail,
            ReportingAgent = "codex",
            HumanOwner = "operator",
            HumanDecisionRequests =
            [
                NodalOsAgentProgressReportFixtures.HumanDecision(
                    kind: NodalOsHumanDecisionKind.MissingContext,
                    urgency: NodalOsHumanDecisionUrgency.Normal,
                    evidenceRefs: evidenceRefs)
            ],
            EvidenceRefs = evidenceRefs ?? [],
            CreatedAt = NodalOsAgentProgressReportFixtures.FixedTimestamp
        };

    public NodalOsAgentProgressReport FromTask(NexaAgentTask task) =>
        new()
        {
            ReportId = $"progress-task-{task.TaskId}",
            MissionId = task.MissionId,
            TaskId = task.TaskId,
            Kind = task.Blockers.Count > 0 ? NodalOsAgentProgressReportKind.Blocker : NodalOsAgentProgressReportKind.Progress,
            Status = task.Status switch
            {
                NexaAgentTaskStatus.Blocked => NodalOsAgentProgressReportStatus.Blocked,
                NexaAgentTaskStatus.WaitingForHuman => NodalOsAgentProgressReportStatus.WaitingForHuman,
                NexaAgentTaskStatus.WaitingForApproval => NodalOsAgentProgressReportStatus.WaitingForApproval,
                NexaAgentTaskStatus.InReview => NodalOsAgentProgressReportStatus.ReadyForReview,
                NexaAgentTaskStatus.Completed => NodalOsAgentProgressReportStatus.ReadyForReview,
                NexaAgentTaskStatus.Failed => NodalOsAgentProgressReportStatus.Failed,
                NexaAgentTaskStatus.Cancelled => NodalOsAgentProgressReportStatus.Cancelled,
                _ => NodalOsAgentProgressReportStatus.InProgress
            },
            Summary = task.Title,
            Detail = task.CompletionReason ?? task.Description,
            ReportingAgent = task.AssignedAgent,
            HumanOwner = task.HumanOwner,
            ProgressNotes = task.ProgressNotes,
            Blockers = task.Blockers,
            EvidenceRefs = NodalOsAgentProgressReportValidator.CollectEvidence(new NodalOsAgentProgressReport
            {
                ReportId = $"collect-task-{task.TaskId}",
                TaskId = task.TaskId,
                Kind = NodalOsAgentProgressReportKind.Progress,
                Status = NodalOsAgentProgressReportStatus.InProgress,
                Summary = task.Title,
                ProgressNotes = task.ProgressNotes,
                Blockers = task.Blockers,
                EvidenceRefs = task.EvidenceRefs,
                CreatedAt = task.UpdatedAt == default ? NodalOsAgentProgressReportFixtures.FixedTimestamp : task.UpdatedAt
            }),
            CreatedAt = task.UpdatedAt == default ? NodalOsAgentProgressReportFixtures.FixedTimestamp : task.UpdatedAt
        };

    public NodalOsAgentProgressReport FromRunReport(NexaRunReport report)
    {
        var blockers = report.Failures.Select((failure, index) => FailureToBlocker(failure, index)).ToArray();
        var status = report.Status switch
        {
            NexaRunStatus.Blocked => NodalOsAgentProgressReportStatus.Blocked,
            NexaRunStatus.Failed => NodalOsAgentProgressReportStatus.Failed,
            NexaRunStatus.AwaitingApproval => NodalOsAgentProgressReportStatus.WaitingForApproval,
            NexaRunStatus.Paused => NodalOsAgentProgressReportStatus.WaitingForHuman,
            NexaRunStatus.Completed or NexaRunStatus.CompletedWithWarnings => NodalOsAgentProgressReportStatus.ReadyForReview,
            NexaRunStatus.Cancelled => NodalOsAgentProgressReportStatus.Cancelled,
            _ => NodalOsAgentProgressReportStatus.InProgress
        };

        return new NodalOsAgentProgressReport
        {
            ReportId = $"progress-run-{report.RunId}",
            MissionId = report.MissionId,
            TaskId = report.TaskId,
            RunId = report.RunId,
            Kind = blockers.Length > 0 ? NodalOsAgentProgressReportKind.Blocker : NodalOsAgentProgressReportKind.Progress,
            Status = status,
            Summary = report.Goal,
            Detail = report.FinalSummary,
            ReportingAgent = "runtime-report",
            HumanOwner = "operator",
            Blockers = blockers,
            EvidenceRefs = NodalOsAgentProgressReportValidator.CollectEvidence(new NodalOsAgentProgressReport
            {
                ReportId = $"collect-run-{report.RunId}",
                RunId = report.RunId,
                Kind = NodalOsAgentProgressReportKind.Progress,
                Status = NodalOsAgentProgressReportStatus.InProgress,
                Summary = report.Goal,
                Blockers = blockers,
                EvidenceRefs = [.. report.EvidenceRefs, .. report.Steps.SelectMany(s => s.EvidenceRefs), .. report.Failures.SelectMany(f => f.EvidenceRefs)],
                CreatedAt = report.CompletedAt ?? report.StartedAt
            }),
            CreatedAt = report.CompletedAt ?? report.StartedAt
        };
    }

    public NodalOsVerificationSummary FromVerificationBeforeDoneResult(NodalOsVerificationBeforeDoneResult result) =>
        new()
        {
            SubjectId = result.SubjectId,
            SubjectKind = result.SubjectKind,
            CanMarkDone = result.CanMarkDone,
            Errors = result.Errors,
            Warnings = result.Warnings,
            VerificationLabels = result.VerificationLabels,
            EvidenceRefs = result.EvidenceRefs
        };

    private static NexaBlockerReport FailureToBlocker(NexaFailureReport failure, int index) =>
        new()
        {
            BlockerId = $"blocker-from-failure-{index + 1}-{failure.FailureId}",
            TaskId = failure.StepId,
            Kind = failure.Kind switch
            {
                NexaFailureKind.PolicyBlocked => NexaBlockerKind.PolicyBlocked,
                NexaFailureKind.ApprovalRequired => NexaBlockerKind.ApprovalRequired,
                NexaFailureKind.HumanInputRequired => NexaBlockerKind.HumanDecisionRequired,
                NexaFailureKind.ExternalServiceUnavailable => NexaBlockerKind.ExternalDependency,
                NexaFailureKind.RuntimeDisconnected or NexaFailureKind.ContentScriptUnreachable or NexaFailureKind.TabCrashed => NexaBlockerKind.RuntimeBlocked,
                _ => NexaBlockerKind.Unknown
            },
            Summary = failure.Summary,
            Detail = failure.Detail ?? failure.SuggestedRecovery,
            Severity = failure.Severity switch
            {
                NexaFailureSeverity.Critical => NexaBlockerSeverity.Critical,
                NexaFailureSeverity.Blocking => NexaBlockerSeverity.Blocking,
                NexaFailureSeverity.Warning => NexaBlockerSeverity.Warning,
                _ => NexaBlockerSeverity.Info
            },
            SuggestedResolution = failure.Kind is NexaFailureKind.ApprovalRequired or NexaFailureKind.HumanInputRequired
                ? NexaBlockerResolutionMode.AskHuman
                : NexaBlockerResolutionMode.StopWithEvidence,
            EvidenceRefs = failure.EvidenceRefs,
            CreatedAt = failure.CreatedAt
        };
}

public static class NodalOsAgentProgressReportSanitizer
{
    private static readonly NodalOsRedactionService Redaction = new();

    public static bool ContainsSensitiveTokenLikeContent(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        Redaction.ContainsSensitiveContent(value);

    public static NodalOsAgentProgressReport Sanitize(NodalOsAgentProgressReport report) =>
        report with
        {
            Summary = SanitizeValue(report.Summary)!,
            Detail = SanitizeValue(report.Detail),
            ReportingAgent = SanitizeValue(report.ReportingAgent),
            HumanOwner = SanitizeValue(report.HumanOwner),
            ProgressNotes = report.ProgressNotes.Select(SanitizeProgressNote).ToArray(),
            Blockers = report.Blockers.Select(SanitizeBlocker).ToArray(),
            HumanDecisionRequests = report.HumanDecisionRequests.Select(SanitizeHumanDecision).ToArray(),
            VerificationSummaries = report.VerificationSummaries.Select(SanitizeVerificationSummary).ToArray(),
            EvidenceRefs = report.EvidenceRefs.Select(SanitizeEvidence).ToArray(),
            Warnings = report.Warnings.Select(SanitizeWarning).ToArray()
        };

    public static bool IsSafe(NodalOsAgentProgressReport report)
    {
        var values = new List<string?>();
        values.Add(report.ReportId);
        values.Add(report.MissionId);
        values.Add(report.TaskId);
        values.Add(report.RunId);
        values.Add(report.Summary);
        values.Add(report.Detail);
        values.Add(report.ReportingAgent);
        values.Add(report.HumanOwner);
        values.AddRange(report.ProgressNotes.SelectMany(n => new[] { n.NoteId, n.TaskId, n.Author, n.Summary, n.Detail }));
        values.AddRange(report.Blockers.SelectMany(b => new[] { b.BlockerId, b.TaskId, b.Summary, b.Detail }));
        values.AddRange(report.HumanDecisionRequests.SelectMany(r => new[] { r.RequestId, r.Summary, r.Detail }));
        values.AddRange(report.VerificationSummaries.SelectMany(v => v.Errors.Concat(v.Warnings).Concat(v.VerificationLabels)));
        values.AddRange(report.EvidenceRefs.SelectMany(e => new[] { e.EvidenceId, e.Kind, e.Ref, e.Hash }));
        values.AddRange(report.Warnings.SelectMany(w => new[] { w.WarningId, w.Summary, w.Detail }));

        return values.Where(v => !string.IsNullOrWhiteSpace(v)).All(v => !ContainsSensitiveTokenLikeContent(v));
    }

    private static NexaProgressNote SanitizeProgressNote(NexaProgressNote note) =>
        note with
        {
            Author = SanitizeValue(note.Author)!,
            Summary = SanitizeValue(note.Summary)!,
            Detail = SanitizeValue(note.Detail),
            EvidenceRefs = note.EvidenceRefs.Select(SanitizeEvidence).ToArray()
        };

    private static NexaBlockerReport SanitizeBlocker(NexaBlockerReport blocker) =>
        blocker with
        {
            Summary = SanitizeValue(blocker.Summary)!,
            Detail = SanitizeValue(blocker.Detail),
            EvidenceRefs = blocker.EvidenceRefs.Select(SanitizeEvidence).ToArray()
        };

    private static NodalOsHumanDecisionRequest SanitizeHumanDecision(NodalOsHumanDecisionRequest request) =>
        request with
        {
            Summary = SanitizeValue(request.Summary)!,
            Detail = SanitizeValue(request.Detail),
            EvidenceRefs = request.EvidenceRefs.Select(SanitizeEvidence).ToArray()
        };

    private static NodalOsVerificationSummary SanitizeVerificationSummary(NodalOsVerificationSummary summary) =>
        summary with
        {
            Errors = summary.Errors.Select(SanitizeValue).Select(v => v!).ToArray(),
            Warnings = summary.Warnings.Select(SanitizeValue).Select(v => v!).ToArray(),
            VerificationLabels = summary.VerificationLabels.Select(SanitizeValue).Select(v => v!).ToArray(),
            EvidenceRefs = summary.EvidenceRefs.Select(SanitizeEvidence).ToArray()
        };

    private static NodalOsReportingWarning SanitizeWarning(NodalOsReportingWarning warning) =>
        warning with
        {
            Summary = SanitizeValue(warning.Summary)!,
            Detail = SanitizeValue(warning.Detail)
        };

    private static NexaEvidenceRef SanitizeEvidence(NexaEvidenceRef evidence) =>
        evidence with
        {
            Kind = SanitizeValue(evidence.Kind)!,
            Ref = SanitizeValue(evidence.Ref),
            Hash = SanitizeValue(evidence.Hash)
        };

    private static string? SanitizeValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return Redaction.RedactValue(value).Value;
    }
}

public static class NodalOsAgentProgressReportFixtures
{
    public static readonly DateTimeOffset FixedTimestamp = new(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);

    public static NexaEvidenceRef Evidence(string evidenceId = "evidence-progress-001") =>
        new()
        {
            EvidenceId = evidenceId,
            Kind = "agent-progress-report",
            Ref = $"artifact://agent-operations/{evidenceId}",
            Hash = "sha256:fixture",
            CreatedAt = FixedTimestamp
        };

    public static NodalOsHumanDecisionRequest HumanDecision(
        string requestId = "decision-001",
        NodalOsHumanDecisionKind kind = NodalOsHumanDecisionKind.MissingContext,
        NodalOsHumanDecisionUrgency urgency = NodalOsHumanDecisionUrgency.Normal,
        IReadOnlyList<NexaEvidenceRef>? evidenceRefs = null) =>
        new()
        {
            RequestId = requestId,
            Summary = "Human decision required.",
            Detail = "Controlled decision fixture.",
            Kind = kind,
            Urgency = urgency,
            EvidenceRefs = evidenceRefs ?? [Evidence("evidence-decision")],
            CreatedAt = FixedTimestamp
        };

    public static NodalOsReportingWarning Warning(string warningId = "warning-001", string summary = "Warning fixture.") =>
        new()
        {
            WarningId = warningId,
            Summary = summary,
            Detail = "Controlled reporting warning.",
            CreatedAt = FixedTimestamp
        };
}
