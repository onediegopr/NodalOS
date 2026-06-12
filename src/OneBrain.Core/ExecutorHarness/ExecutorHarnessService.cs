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
        var targetResolution = ExecutorHarnessTargetResolver.ResolveTarget(target);
        var safetyMatrix = ExecutorHarnessSafetyMatrix.Evaluate(target, decision);
        if (!safetyMatrix.Allowed)
        {
            var guardIssues = safetyMatrix.Blocked;
            var verification = new ExecutorHarnessPostActionVerification(
                Success: false,
                Status: ExecutorHarnessStatuses.Blocked,
                Message: string.Join("; ", guardIssues),
                TargetFound: false,
                ClickObserved: false,
                Signals: guardIssues);
            return BuildResult(target, approval, decision, verification, ExecutorHarnessStatuses.Blocked, "blocked before executor", timestamp, safetyMatrix, targetResolution);
        }

        ExecutorHarnessExecutorResult executorResult;
        try
        {
            executorResult = executor.Click(new ExecutorHarnessClickCommand(
                HarnessId: target.HarnessId,
                WindowTitleContains: target.WindowTitleContains,
                TargetRef: target.TargetRef,
                ExpectedTargetName: target.ExpectedTargetName,
                ActionKind: target.ActionKind));
        }
        catch (Exception ex)
        {
            executorResult = new ExecutorHarnessExecutorResult(false, $"executor error: {ex.Message}", false, 0, ["executor threw"]);
        }

        var verificationResult = VerifyPostAction(target, executorResult);
        var status = verificationResult.Success ? ExecutorHarnessStatuses.Succeeded : ExecutorHarnessStatuses.Failed;
        return BuildResult(
            target,
            approval,
            decision,
            verificationResult,
            status,
            verificationResult.Message,
            timestamp,
            safetyMatrix,
            executorResult.TargetResolution ?? targetResolution);
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

    private static ExecutorHarnessPostActionVerification VerifyPostAction(ExecutorHarnessTarget target, ExecutorHarnessExecutorResult executorResult)
    {
        var postActionState = BuildPostActionState(executorResult);
        var targetResolution = executorResult.TargetResolution ?? ExecutorHarnessTargetResolver.ResolveTarget(target);
        var signals = executorResult.Signals
            .Concat(targetResolution.Signals)
            .Concat(postActionState.Signals)
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

        if (!targetResolution.Success ||
            !postActionState.WindowFound ||
            !postActionState.TargetVisible ||
            !string.Equals(postActionState.TargetName, target.ExpectedTargetName, StringComparison.OrdinalIgnoreCase) ||
            !postActionState.ClickCountVerified ||
            !executorResult.TargetFound ||
            executorResult.Clicks != 1)
        {
            return new ExecutorHarnessPostActionVerification(
                Success: false,
                Status: ExecutorHarnessStatuses.Failed,
                Message: "post-action verification failed",
                TargetFound: executorResult.TargetFound,
                ClickObserved: postActionState.ClickCountVerified && postActionState.WindowFound && postActionState.TargetVisible,
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

    private static ExecutorHarnessPostActionState BuildPostActionState(ExecutorHarnessExecutorResult executorResult)
    {
        if (executorResult.PostActionState != null)
            return executorResult.PostActionState;

        var signals = executorResult.Signals;
        var windowFound = signals.Contains("postAction.windowFound=true", StringComparer.OrdinalIgnoreCase);
        var targetVisible = signals.Contains("postAction.targetVisible=true", StringComparer.OrdinalIgnoreCase);
        var targetName = signals.FirstOrDefault(signal => signal.StartsWith("postAction.targetName=", StringComparison.OrdinalIgnoreCase))?
            .Split('=', 2)[1] ?? "";

        return new ExecutorHarnessPostActionState(
            WindowFound: windowFound,
            TargetVisible: targetVisible,
            TargetName: targetName,
            ObservedClicks: executorResult.Clicks,
            ClickCountVerified: executorResult.Clicks == 1,
            Signals: signals.Where(signal => signal.StartsWith("postAction.", StringComparison.OrdinalIgnoreCase)).ToList());
    }

    private static ExecutorHarnessRunResult BuildResult(
        ExecutorHarnessTarget target,
        ApprovalRequest approval,
        ApprovalDecision? decision,
        ExecutorHarnessPostActionVerification verification,
        string status,
        string errorSummary,
        string timestamp,
        ExecutorHarnessSafetyMatrixEvaluation safetyMatrix,
        ExecutorHarnessTargetResolution targetResolution)
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
                $"safety matrix: {safetyMatrix.Status}",
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
            ArtifactPaths: artifactPaths,
            SafetyMatrix: safetyMatrix,
            TargetResolution: targetResolution);
    }

    private static string Timestamp(DateTimeOffset? now)
    {
        return (now ?? DateTimeOffset.UtcNow).UtcDateTime.ToString("o");
    }
}
