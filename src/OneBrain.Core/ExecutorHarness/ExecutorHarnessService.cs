using OneBrain.Core.Approval;
using OneBrain.Core.History;
using OneBrain.Core.Recording;

namespace OneBrain.Core.ExecutorHarness;

public static class ExecutorHarnessService
{
    public static ApprovalRequest CreateApprovalRequest(ExecutorHarnessTarget target, DateTimeOffset? now = null)
    {
        return ApprovalPolicy.CreateRequest(
            source: "executor_harness",
            candidateFlowId: target.HarnessId,
            actionKind: target.ActionKind,
            riskLevel: ApprovalRiskLevels.Low,
            title: "Aprobacion para click benigno supervisado",
            description: "Permite un unico click real sobre el harness local controlado de ONE BRAIN Pilot.",
            preview: $"{target.Title} -> {target.ExpectedTargetName}",
            notes: target.Notes.Concat(
            [
                "approval required even though the target is benign",
                "scope is limited to the controlled local harness"
            ]).ToList(),
            policy: ApprovalPolicy.DefaultPlatformPolicy with
            {
                HumanInTheLoopMode = HumanInTheLoopModes.AlwaysRequired,
                SensitiveActionKinds = ApprovalPolicy.DefaultPlatformPolicy.SensitiveActionKinds
                    .Concat([ApprovalActionKinds.BenignHarnessClick])
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
            },
            confidenceScore: 100,
            environment: "local_harness",
            profileId: target.AppProfileId,
            hasSafeExecutor: target.HasSafeExecutor,
            createdAtUtc: now);
    }

    public static ExecutorHarnessRunResult ExecuteSupervisedClick(
        ExecutorHarnessTarget target,
        ApprovalDecision? decision,
        IExecutorHarnessClickExecutor executor,
        DateTimeOffset? now = null)
    {
        var timestamp = Timestamp(now);
        var approval = CreateApprovalRequest(target, now);
        var guardIssues = Guard(target, decision);
        if (guardIssues.Count > 0)
        {
            var verification = new ExecutorHarnessPostActionVerification(
                Success: false,
                Status: ExecutorHarnessStatuses.Blocked,
                Message: string.Join("; ", guardIssues),
                TargetFound: false,
                ClickObserved: false,
                Signals: guardIssues);
            return BuildResult(target, approval, decision, verification, ExecutorHarnessStatuses.Blocked, "blocked before executor", timestamp);
        }

        ExecutorHarnessExecutorResult executorResult;
        try
        {
            executorResult = executor.Click(new ExecutorHarnessClickCommand(
                HarnessId: target.HarnessId,
                WindowTitleContains: target.WindowTitleContains,
                TargetRef: target.TargetRef,
                ExpectedTargetName: target.ExpectedTargetName,
                ActionKind: "click"));
        }
        catch (Exception ex)
        {
            executorResult = new ExecutorHarnessExecutorResult(false, $"executor error: {ex.Message}", false, 0, ["executor threw"]);
        }

        var verificationResult = VerifyPostAction(target, executorResult);
        var status = verificationResult.Success ? ExecutorHarnessStatuses.Succeeded : ExecutorHarnessStatuses.Failed;
        return BuildResult(target, approval, decision, verificationResult, status, verificationResult.Message, timestamp);
    }

    public static ExecutorHarnessEvidenceRecord ToEvidenceRecord(ExecutorHarnessRunResult result, DateTimeOffset? now = null)
    {
        var timestamp = Timestamp(now);
        return new ExecutorHarnessEvidenceRecord(
            EvidenceId: $"evidence-{Guid.NewGuid():N}",
            CreatedAtUtc: timestamp,
            HarnessId: result.Target.HarnessId,
            Status: result.Status,
            Message: SensitiveTextSanitizer.Sanitize(result.Message),
            ApprovalRequestId: result.ApprovalRequest.ApprovalRequestId,
            ApprovalDecisionId: result.ApprovalDecision?.ApprovalDecisionId,
            Verification: result.Verification,
            SafetyCounters: result.RunHistory.SafetyCounters,
            Notes:
            [
                "first real click is scoped to a benign local harness target",
                "no MercadoLibre, no external commercial website, no login, no cookies, no cart, no purchase, no payment",
                "post-action verification is required before marking the harness result succeeded"
            ]);
    }

    private static IReadOnlyList<string> Guard(ExecutorHarnessTarget target, ApprovalDecision? decision)
    {
        var issues = new List<string>();
        if (!string.Equals(target.HarnessId, ExecutorHarnessDemoFixture.HarnessId, StringComparison.OrdinalIgnoreCase))
            issues.Add("harness id is not allowlisted");
        if (!string.Equals(target.AppProfileId, "onebrain-pilot-local", StringComparison.OrdinalIgnoreCase))
            issues.Add("app profile is not the local Pilot harness");
        if (!string.Equals(target.WindowTitleContains, "ONE BRAIN Pilot", StringComparison.OrdinalIgnoreCase))
            issues.Add("window target is not the local Pilot harness");
        if (!string.Equals(target.TargetRef, $"name:{ExecutorHarnessDemoFixture.TargetName}", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(target.ExpectedTargetName, ExecutorHarnessDemoFixture.TargetName, StringComparison.OrdinalIgnoreCase))
            issues.Add("target identity is not the benign harness target");
        if (!target.ControlledSurface)
            issues.Add("target is not a controlled harness surface");
        if (!target.IsBenign)
            issues.Add("target is not marked benign");
        if (!target.HasSafeExecutor)
            issues.Add("safe executor is missing");
        if (!string.Equals(target.ActionKind, ApprovalActionKinds.BenignHarnessClick, StringComparison.OrdinalIgnoreCase))
            issues.Add("action kind is not the benign harness click action");
        if (string.IsNullOrWhiteSpace(target.TargetRef) || string.IsNullOrWhiteSpace(target.ExpectedTargetName))
            issues.Add("target identity is incomplete");
        if (decision == null)
            issues.Add("approval decision is required");
        else
        {
            if (!string.Equals(decision.Decision, ApprovalDecisionKinds.Approved, StringComparison.OrdinalIgnoreCase))
                issues.Add("approval decision is not approved");
            if (!decision.ExecutionAllowed)
                issues.Add("approval decision does not allow executor harness action");
        }

        return issues;
    }

    private static ExecutorHarnessPostActionVerification VerifyPostAction(ExecutorHarnessTarget target, ExecutorHarnessExecutorResult executorResult)
    {
        var signals = executorResult.Signals
            .Concat(
            [
                $"target={target.ExpectedTargetName}",
                $"clicks={executorResult.Clicks}"
            ])
            .Select(SensitiveTextSanitizer.Sanitize)
            .ToList();

        if (!executorResult.Success)
        {
            return new ExecutorHarnessPostActionVerification(
                Success: false,
                Status: ExecutorHarnessStatuses.Failed,
                Message: SensitiveTextSanitizer.Sanitize(executorResult.Message),
                TargetFound: executorResult.TargetFound,
                ClickObserved: false,
                Signals: signals);
        }

        var hasIndependentWindowCheck = signals.Contains("postAction.windowFound=true", StringComparer.OrdinalIgnoreCase);
        var hasIndependentTargetCheck = signals.Contains("postAction.targetVisible=true", StringComparer.OrdinalIgnoreCase);

        if (!executorResult.TargetFound || executorResult.Clicks != 1 || !hasIndependentWindowCheck || !hasIndependentTargetCheck)
        {
            return new ExecutorHarnessPostActionVerification(
                Success: false,
                Status: ExecutorHarnessStatuses.Failed,
                Message: "post-action verification failed",
                TargetFound: executorResult.TargetFound,
                ClickObserved: executorResult.Clicks == 1 && hasIndependentWindowCheck && hasIndependentTargetCheck,
                Signals: signals);
        }

        return new ExecutorHarnessPostActionVerification(
            Success: true,
            Status: ExecutorHarnessStatuses.Succeeded,
            Message: "post-action verification passed",
            TargetFound: true,
            ClickObserved: true,
            Signals: signals);
    }

    private static ExecutorHarnessRunResult BuildResult(
        ExecutorHarnessTarget target,
        ApprovalRequest approval,
        ApprovalDecision? decision,
        ExecutorHarnessPostActionVerification verification,
        string status,
        string errorSummary,
        string timestamp)
    {
        var success = status == ExecutorHarnessStatuses.Succeeded;
        var safety = success ? new RunSafetyCounters(1, 0, 0, 0, 0, 0) : RunSafetyCounters.Zero;
        var artifactPaths = success ? [$"artifacts/executor-harness/{target.HarnessId}-evidence.json"] : Array.Empty<string>();
        var record = new RunHistoryRecord(
            RunId: $"run-{target.HarnessId}-{Guid.NewGuid():N}",
            StartedAtUtc: timestamp,
            EndedAtUtc: timestamp,
            Status: success ? RunHistoryStatuses.Succeeded : status == ExecutorHarnessStatuses.Blocked ? RunHistoryStatuses.Blocked : RunHistoryStatuses.Failed,
            Source: RunHistorySources.Pilot,
            RecipeId: null,
            CandidateFlowId: target.HarnessId,
            ApprovalRequestId: approval.ApprovalRequestId,
            ApprovalDecisionId: decision?.ApprovalDecisionId,
            RecordingSessionId: null,
            TimelineId: null,
            ConfidenceId: null,
            AiRoutingDecisionId: null,
            ExitCode: success ? 0 : 1,
            SafetyCounters: safety,
            ArtifactPaths: artifactPaths,
            ErrorSummary: success ? "" : SensitiveTextSanitizer.Sanitize(errorSummary),
            Notes:
            [
                "executor harness supervised click",
                success ? "exactly 1 benign local harness click observed" : "no click executed or verified",
                "0 cookies, 0 login, 0 cart, 0 purchase, 0 payment"
            ]);

        var evidence = verification.Signals
            .Concat(
            [
                verification.Message,
                success ? "run history records one benign harness click" : "run blocked or failed closed"
            ])
            .Select(SensitiveTextSanitizer.Sanitize)
            .ToList();

        return new ExecutorHarnessRunResult(
            Success: success,
            Status: status,
            Message: verification.Message,
            Target: target,
            ApprovalRequest: approval,
            ApprovalDecision: decision,
            Verification: verification,
            RunHistory: record,
            Evidence: evidence,
            ArtifactPaths: artifactPaths);
    }

    private static string Timestamp(DateTimeOffset? now)
    {
        return (now ?? DateTimeOffset.UtcNow).UtcDateTime.ToString("o");
    }
}
