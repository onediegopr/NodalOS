using OneBrain.Core.Approval;
using OneBrain.Core.History;
using OneBrain.Core.Recording;

namespace OneBrain.Core.ExecutorHarness;

public static class ExecutorHarnessService
{
    public static ExecutorHarnessFlowPlan BuildFlowPlan(
        ApprovalDecision? decision = null,
        DateTimeOffset? now = null)
    {
        var steps = ExecutorHarnessDemoFixture.CreateFlowTargets()
            .Select((target, index) => BuildFlowStep(target, $"step-{index + 1}", decision, now))
            .ToList();

        var blockedSteps = steps.Count(step => !step.SafetyDecision.Allowed);
        return new ExecutorHarnessFlowPlan(
            FlowId: ExecutorHarnessDemoFixture.FlowId,
            Status: blockedSteps == 0 ? "ready_if_supervised" : "fail_closed_dry_run",
            Summary: blockedSteps == 0
                ? "Flow dry-run ready: all benign local steps are allowlisted, but execution still requires supervised approval."
                : "Flow dry-run blocked: at least one benign local step remains fail-closed until approval and all safety checks pass.",
            Steps: steps,
            FailureRecoveryPolicy: "fail_closed_stop_on_first_failed_step",
            Notes:
            [
                "multi-step harness flow is local-only and benign",
                "no target is configurable from user input",
                "if any step fails, ONE BRAIN stops and requires human intervention"
            ]);
    }

    public static ExecutorHarnessDryRunExplanation BuildDryRunExplanation(
        ExecutorHarnessTarget target,
        ApprovalDecision? decision = null,
        DateTimeOffset? now = null)
    {
        var approval = CreateApprovalRequest(target, now);
        var contract = BuildInteractionContract(target, approval, decision, dryRunOnly: true, now: now);
        var blocking = contract.SafetyMatrix.Blocked
            .Concat(contract.ApprovalState.RequiresApproval && !contract.ApprovalState.ExecutionAllowed
                ? ["approval is required before executor can run"]
                : Array.Empty<string>())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ExecutorHarnessDryRunExplanation(
            Contract: contract,
            WouldExecute: contract.SafetyMatrix.Allowed && contract.ApprovalState.ExecutionAllowed,
            Status: contract.SafetyMatrix.Allowed && contract.ApprovalState.ExecutionAllowed ? "would_execute_if_submitted" : "fail_closed_dry_run",
            Summary: contract.SafetyMatrix.Allowed && contract.ApprovalState.ExecutionAllowed
                ? "Dry-run: the scoped benign harness click would be allowed if the supervised POST is submitted."
                : "Dry-run: execution remains blocked until scoped approval and all safety checks pass.",
            Element: $"{target.TargetRef} ({target.ExpectedTargetName})",
            SelectionReason: contract.ResolvedTarget.Success
                ? "Selected only because it matches the exact benign local Pilot harness target."
                : contract.ResolvedTarget.Message,
            SafetyRules:
            [
                "only one allowlisted harness id is accepted",
                "only one local Pilot app profile is accepted",
                "only the exact benign target name is accepted",
                "no user-configurable target is accepted",
                "approval and ExecutionAllowed=true are required",
                "post-action state must verify the same target and exactly one click"
            ],
            BlockingConditions: blocking,
            Notes:
            [
                "dry-run does not call the UIA executor",
                "dry-run does not click",
                "dry-run does not write runtime evidence"
            ]);
    }

    public static ExecutorHarnessFlowRunResult ExecuteSupervisedFlow(
        IReadOnlyList<ExecutorHarnessTarget> targets,
        ApprovalDecision? decision,
        IExecutorHarnessClickExecutor executor,
        DateTimeOffset? now = null)
    {
        var timestamp = Timestamp(now);
        var stepEvidence = new List<ExecutorHarnessStepEvidence>();
        var flowId = ExecutorHarnessDemoFixture.FlowId;

        foreach (var (target, index) in targets.Select((target, index) => (target, index)))
        {
            var stepId = $"step-{index + 1}";
            var approval = CreateApprovalRequest(target, now);
            var contract = BuildInteractionContract(target, approval, decision, dryRunOnly: false, now: now);
            var commandSummary = $"{target.ActionKind} -> {target.TargetRef}";

            if (!contract.SafetyMatrix.Allowed || !contract.ApprovalState.ExecutionAllowed)
            {
                var blockedReason = contract.SafetyMatrix.Blocked.Count == 0
                    ? "approval is required before executor can run"
                    : string.Join("; ", contract.SafetyMatrix.Blocked);

                stepEvidence.Add(new ExecutorHarnessStepEvidence(
                    StepId: stepId,
                    Status: ExecutorHarnessStatuses.Blocked,
                    ActionKind: target.ActionKind,
                    InteractionContract: contract,
                    TargetResolution: contract.ResolvedTarget,
                    ApprovalDecision: decision?.Decision ?? "missing",
                    SafetyDecision: contract.SafetyMatrix.Status,
                    CommandSummary: commandSummary,
                    PreActionState: contract.PreActionState,
                    PostActionState: null,
                    VerificationResult: "not_executed",
                    BlockedReason: blockedReason,
                    Notes:
                    [
                        "flow stopped before executing this step",
                        "failure recovery policy requires human intervention"
                    ]));

                return BuildFlowFailureResult(flowId, stepEvidence, stepId, blockedReason, timestamp);
            }

            var result = ExecuteSupervisedClick(target, decision, executor, now);
            var postActionState = BuildPostActionStateFromVerification(target, result.Verification, result.TargetResolution);
            stepEvidence.Add(new ExecutorHarnessStepEvidence(
                StepId: stepId,
                Status: result.Status,
                ActionKind: target.ActionKind,
                InteractionContract: contract,
                TargetResolution: result.TargetResolution,
                ApprovalDecision: decision?.Decision ?? "missing",
                SafetyDecision: result.SafetyMatrix?.Status ?? ExecutorHarnessStatuses.Blocked,
                CommandSummary: commandSummary,
                PreActionState: contract.PreActionState,
                PostActionState: postActionState,
                VerificationResult: result.Verification.Status,
                BlockedReason: result.Success ? "" : result.Message,
                Notes: result.Evidence));

            if (!result.Success)
            {
                return BuildFlowFailureResult(flowId, stepEvidence, stepId, result.Message, timestamp);
            }
        }

        return BuildFlowSuccessResult(flowId, stepEvidence, timestamp);
    }

    public static ExecutorHarnessInteractionContract BuildInteractionContract(
        ExecutorHarnessTarget target,
        ApprovalRequest approval,
        ApprovalDecision? decision,
        bool dryRunOnly,
        DateTimeOffset? now = null)
    {
        var targetResolution = ExecutorHarnessTargetResolver.ResolveTarget(target);
        var safetyMatrix = ExecutorHarnessSafetyMatrix.Evaluate(target, decision);
        var approved = decision != null &&
                       string.Equals(decision.Decision, ApprovalDecisionKinds.Approved, StringComparison.OrdinalIgnoreCase);
        var executionAllowed = decision?.ExecutionAllowed == true && safetyMatrix.Allowed;

        return new ExecutorHarnessInteractionContract(
            ContractId: $"contract-{Guid.NewGuid():N}",
            CreatedAtUtc: Timestamp(now),
            HarnessId: target.HarnessId,
            AppProfileId: target.AppProfileId,
            WindowConstraints: new ExecutorHarnessWindowConstraints(
                TitleContains: target.WindowTitleContains,
                LocalPilotOnly: true,
                ExternalNavigationBlocked: true),
            TargetConstraints: new ExecutorHarnessTargetConstraints(
                TargetRef: target.TargetRef,
                ExpectedTargetName: target.ExpectedTargetName,
                AllowOnlyExactBenignHarnessTarget: true,
                UserConfigurableTargetAllowed: false),
            ResolvedTarget: targetResolution,
            ActionKind: target.ActionKind,
            ApprovalState: new ExecutorHarnessApprovalState(
                ApprovalRequestId: approval.ApprovalRequestId,
                ApprovalDecisionId: decision?.ApprovalDecisionId,
                RequiresApproval: approval.RequiresApproval,
                Approved: approved,
                ExecutionAllowed: executionAllowed,
                FailClosed: approval.FailClosed || !safetyMatrix.Allowed),
            SafetyMatrix: safetyMatrix,
            PreActionState: new ExecutorHarnessPreActionState(
                DryRunOnly: dryRunOnly,
                ExecutorWillRun: !dryRunOnly && executionAllowed,
                Checks:
                [
                    targetResolution.Success ? "target resolution passed" : "target resolution blocked",
                    safetyMatrix.Allowed ? "safety matrix allowed" : "safety matrix blocked",
                    executionAllowed ? "approval allows scoped execution" : "approval does not allow execution"
                ]),
            PostActionExpectation: new ExecutorHarnessPostActionExpectation(
                WindowMustRemainVisible: true,
                TargetMustRemainVisible: true,
                ExpectedTargetName: target.ExpectedTargetName,
                ExpectedClickCount: 1,
                RequiredSignals:
                [
                    "postAction.windowFound=true",
                    "postAction.targetVisible=true",
                    $"postAction.targetName={target.ExpectedTargetName}",
                    "postAction.observedClicks=1"
                ]),
            LogicalEvidencePath: $"{ExecutorHarnessArtifactStore.RelativeDirectory}/{target.HarnessId}-evidence.json");
    }

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
        var contract = BuildInteractionContract(
            result.Target,
            result.ApprovalRequest,
            result.ApprovalDecision,
            dryRunOnly: false,
            now: now);

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
            ],
            InteractionContract: contract);
    }

    public static ExecutorHarnessEvidenceRecord ToEvidenceRecord(ExecutorHarnessFlowRunResult result, DateTimeOffset? now = null)
    {
        var timestamp = Timestamp(now);
        var firstStep = result.Steps.FirstOrDefault();
        var verification = firstStep?.VerificationResult == ExecutorHarnessStatuses.Succeeded && result.Success
            ? new ExecutorHarnessPostActionVerification(
                Success: true,
                Status: ExecutorHarnessStatuses.Succeeded,
                Message: "multi-step harness flow verified",
                TargetFound: true,
                ClickObserved: true,
                Signals: result.Steps.SelectMany(step => step.Notes).ToList())
            : new ExecutorHarnessPostActionVerification(
                Success: false,
                Status: result.Status,
                Message: result.Message,
                TargetFound: false,
                ClickObserved: false,
                Signals: result.Steps.SelectMany(step => step.Notes).ToList());

        return new ExecutorHarnessEvidenceRecord(
            EvidenceId: $"evidence-{Guid.NewGuid():N}",
            CreatedAtUtc: timestamp,
            HarnessId: ExecutorHarnessDemoFixture.HarnessId,
            Status: result.Status,
            Message: SensitiveTextSanitizer.Sanitize(result.Message),
            ApprovalRequestId: firstStep?.InteractionContract?.ApprovalState.ApprovalRequestId ?? "",
            ApprovalDecisionId: firstStep?.InteractionContract?.ApprovalState.ApprovalDecisionId,
            Verification: verification,
            SafetyCounters: result.RunHistory.SafetyCounters,
            Notes:
            [
                "multi-step harness flow evidence",
                "step-level evidence is embedded when available",
                "failure recovery policy is fail-closed stop on first failed step"
            ],
            InteractionContract: firstStep?.InteractionContract,
            FlowId: result.FlowId,
            FlowStatus: result.Status,
            FailureRecoveryPolicy: result.RecoveryDecision.PolicyName,
            Steps: result.Steps);
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

    private static ExecutorHarnessPostActionState BuildPostActionStateFromVerification(
        ExecutorHarnessTarget target,
        ExecutorHarnessPostActionVerification verification,
        ExecutorHarnessTargetResolution? targetResolution)
    {
        var targetName = verification.TargetFound ? target.ExpectedTargetName : "";
        var windowFound = targetResolution?.Success == true;
        return new ExecutorHarnessPostActionState(
            WindowFound: windowFound,
            TargetVisible: verification.TargetFound,
            TargetName: targetName,
            ObservedClicks: verification.ClickObserved ? 1 : 0,
            ClickCountVerified: verification.ClickObserved,
            Signals: verification.Signals.Where(signal => signal.StartsWith("postAction.", StringComparison.OrdinalIgnoreCase)).ToList());
    }

    private static ExecutorHarnessFlowStep BuildFlowStep(
        ExecutorHarnessTarget target,
        string stepId,
        ApprovalDecision? decision,
        DateTimeOffset? now)
    {
        var approval = CreateApprovalRequest(target, now);
        var contract = BuildInteractionContract(target, approval, decision, dryRunOnly: true, now: now);
        return new ExecutorHarnessFlowStep(
            StepId: stepId,
            Title: target.Title,
            ActionKind: target.ActionKind,
            TargetConstraints: contract.TargetConstraints,
            ResolvedTarget: contract.ResolvedTarget,
            ExpectedPostState: contract.PostActionExpectation,
            SafetyDecision: contract.SafetyMatrix);
    }

    private static ExecutorHarnessFlowRunResult BuildFlowFailureResult(
        string flowId,
        IReadOnlyList<ExecutorHarnessStepEvidence> steps,
        string failedStepId,
        string reason,
        string timestamp)
    {
        var verifiedStepCount = steps.Count(step => step.VerificationResult == ExecutorHarnessStatuses.Succeeded);
        var recovery = new ExecutorHarnessFailureRecoveryDecision(
            PolicyName: "fail_closed_stop_on_first_failed_step",
            ContinueAllowed: false,
            Status: ExecutorHarnessStatuses.Blocked,
            Message: "Flow stopped after the first failed or blocked step.",
            FailedStepId: failedStepId,
            Notes:
            [
                "no further step may continue automatically",
                "human intervention is required before retrying the flow"
            ]);

        return new ExecutorHarnessFlowRunResult(
            Success: false,
            FlowId: flowId,
            Status: ExecutorHarnessStatuses.Blocked,
            Message: reason,
            Steps: steps,
            RecoveryDecision: recovery,
            RunHistory: new RunHistoryRecord(
                RunId: $"run-{flowId}-{Guid.NewGuid():N}",
                StartedAtUtc: timestamp,
                EndedAtUtc: timestamp,
                Status: RunHistoryStatuses.Blocked,
                Source: RunHistorySources.Pilot,
                RecipeId: null,
                CandidateFlowId: flowId,
                ApprovalRequestId: steps.LastOrDefault()?.InteractionContract?.ApprovalState.ApprovalRequestId,
                ApprovalDecisionId: steps.LastOrDefault()?.InteractionContract?.ApprovalState.ApprovalDecisionId,
                RecordingSessionId: null,
                TimelineId: null,
                ConfidenceId: null,
                AiRoutingDecisionId: null,
                ExitCode: 1,
                SafetyCounters: new RunSafetyCounters(verifiedStepCount, 0, 0, 0, 0, 0),
                ArtifactPaths: [$"artifacts/executor-harness/{flowId}-evidence.json"],
                ErrorSummary: SensitiveTextSanitizer.Sanitize(reason),
                Notes:
                [
                    "multi-step harness flow blocked",
                    "failure recovery policy stopped the flow",
                    "0 cookies, 0 login, 0 cart, 0 purchase, 0 payment"
                ]),
            ArtifactPaths: [$"artifacts/executor-harness/{flowId}-evidence.json"],
            Notes:
            [
                "flow did not continue after the failed step",
                "manual review is required"
            ]);
    }

    private static ExecutorHarnessFlowRunResult BuildFlowSuccessResult(
        string flowId,
        IReadOnlyList<ExecutorHarnessStepEvidence> steps,
        string timestamp)
    {
        var clickCount = steps.Count(step => step.VerificationResult == ExecutorHarnessStatuses.Succeeded);
        var recovery = new ExecutorHarnessFailureRecoveryDecision(
            PolicyName: "fail_closed_stop_on_first_failed_step",
            ContinueAllowed: false,
            Status: ExecutorHarnessStatuses.Succeeded,
            Message: "Flow completed without triggering recovery.",
            FailedStepId: "",
            Notes:
            [
                "flow finished with all benign local steps verified",
                "no automatic continuation policy is needed after success"
            ]);

        return new ExecutorHarnessFlowRunResult(
            Success: true,
            FlowId: flowId,
            Status: ExecutorHarnessStatuses.Succeeded,
            Message: "multi-step harness flow completed",
            Steps: steps,
            RecoveryDecision: recovery,
            RunHistory: new RunHistoryRecord(
                RunId: $"run-{flowId}-{Guid.NewGuid():N}",
                StartedAtUtc: timestamp,
                EndedAtUtc: timestamp,
                Status: RunHistoryStatuses.Succeeded,
                Source: RunHistorySources.Pilot,
                RecipeId: null,
                CandidateFlowId: flowId,
                ApprovalRequestId: steps.FirstOrDefault()?.InteractionContract?.ApprovalState.ApprovalRequestId,
                ApprovalDecisionId: steps.FirstOrDefault()?.InteractionContract?.ApprovalState.ApprovalDecisionId,
                RecordingSessionId: null,
                TimelineId: null,
                ConfidenceId: null,
                AiRoutingDecisionId: null,
                ExitCode: 0,
                SafetyCounters: new RunSafetyCounters(clickCount, 0, 0, 0, 0, 0),
                ArtifactPaths: [$"artifacts/executor-harness/{flowId}-evidence.json"],
                ErrorSummary: "",
                Notes:
                [
                    "multi-step harness flow succeeded",
                    $"{clickCount} benign local harness steps verified",
                    "0 cookies, 0 login, 0 cart, 0 purchase, 0 payment"
                ]),
            ArtifactPaths: [$"artifacts/executor-harness/{flowId}-evidence.json"],
            Notes:
            [
                "all steps were verified before completion",
                "flow remains local-only and benign"
            ]);
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
