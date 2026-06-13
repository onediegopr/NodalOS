using OneBrain.Core.Approval;
using OneBrain.Core.Contracts;
using OneBrain.Core.Execution;
using OneBrain.Core.History;
using OneBrain.Core.Identity;
using OneBrain.Core.Models;
using OneBrain.Core.Recording;
using OneBrain.Core.Selectors;

namespace OneBrain.Core.ExecutorHarness;

public static class ExecutorHarnessService
{
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

    public static ExecutorHarnessInteractionContract BuildInteractionContract(
        ExecutorHarnessTarget target,
        ApprovalRequest approval,
        ApprovalDecision? decision,
        bool dryRunOnly,
        DateTimeOffset? now = null)
    {
        var targetResolution = ExecutorHarnessTargetResolver.ResolveTarget(target);
        var safetyMatrix = ExecutorHarnessSafetyMatrix.Evaluate(target, decision);
        var approvalBinding = BuildApprovalBinding(target, decision);
        var safetyContract = BuildSafetyContract(target, approvalBinding);
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
            LogicalEvidencePath: $"{ExecutorHarnessArtifactStore.RelativeDirectory}/{target.HarnessId}-evidence.json",
            SafetyContract: safetyContract,
            ApprovalBinding: approvalBinding);
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
        return ExecuteSupervisedClick(target, decision, executor, new PassiveDesktopOwnershipMonitor(), now);
    }

    internal static ExecutorHarnessRunResult ExecuteSupervisedClick(
        ExecutorHarnessTarget target,
        ApprovalDecision? decision,
        IExecutorHarnessClickExecutor executor,
        IDesktopOwnershipMonitor ownershipMonitor,
        DateTimeOffset? now = null)
    {
        var timestamp = Timestamp(now);
        var approval = CreateApprovalRequest(target, now);
        var targetResolution = ExecutorHarnessTargetResolver.ResolveTarget(target);
        var safetyMatrix = ExecutorHarnessSafetyMatrix.Evaluate(target, decision);
        var approvalBinding = BuildApprovalBinding(target, decision);
        var safetyContract = BuildSafetyContract(target, approvalBinding);
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
            return BuildResult(
                target,
                approval,
                decision,
                verification,
                ExecutorHarnessStatuses.Blocked,
                "blocked before executor",
                timestamp,
                safetyMatrix,
                targetResolution,
                finalState: StepState.Blocked,
                failureKind: FailureKind.PolicyDenied,
                transitionEvidence: [],
                safetyContract: safetyContract,
                approvalBinding: approvalBinding);
        }

        var selector = safetyContract.Selector ?? throw new InvalidOperationException("safe execution contract selector is required");
        var observedIdentity = targetResolution.ObservedIdentity ?? ExecutorHarnessTargetResolver.BuildAllowlistedIdentity();
        var fsm = new SafeExecutionFsm(
            new ContractValidator(),
            new ApprovalBindingValidator(),
            ownershipMonitor,
            new LegacyHarnessExecutorAdapter(executor, target),
            new HarnessStepVerifier(target));
        var fsmResult = fsm.Execute(new SafeExecutionRequest(
            Contract: safetyContract,
            Candidates: targetResolution.Candidates ?? [observedIdentity],
            DispatchRequest: new PatternExecutionRequest(
                ActionKind: target.ActionKind,
                TargetRef: target.TargetRef,
                ExpectedTargetName: target.ExpectedTargetName,
                ProcessName: "OneBrain.Pilot",
                WindowTitleContains: target.WindowTitleContains,
                Selector: selector,
                ExpectedIdentity: safetyContract.ExpectedIdentity!)));

        var verificationResult = BuildVerificationFromFsm(target, fsmResult, targetResolution);
        var status = verificationResult.Success ? ExecutorHarnessStatuses.Succeeded : fsmResult.FinalState == StepState.Blocked ? ExecutorHarnessStatuses.Blocked : ExecutorHarnessStatuses.Failed;
        return BuildResult(
            target,
            approval,
            decision,
            verificationResult,
            status,
            verificationResult.Message,
            timestamp,
            safetyMatrix,
            fsmResult.DispatchResult?.ObservedIdentity != null
                ? targetResolution with { ObservedIdentity = fsmResult.DispatchResult.ObservedIdentity }
                : targetResolution,
            fsmResult.FinalState,
            fsmResult.FailureKind,
            fsmResult.Ledger.Entries,
            safetyContract,
            approvalBinding);
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
            InteractionContract: contract,
            FailureKind: result.FailureKind,
            TransitionEvidence: result.TransitionEvidence);
    }

    private static ExecutorHarnessPostActionVerification BuildVerificationFromFsm(
        ExecutorHarnessTarget target,
        SafeExecutionResult fsmResult,
        ExecutorHarnessTargetResolution targetResolution)
    {
        var dispatchResult = fsmResult.DispatchResult;
        var signals = (dispatchResult?.Signals ?? Array.Empty<string>())
            .Concat(targetResolution.Signals)
            .Concat(fsmResult.Reasons)
            .Concat(
            [
                $"target={target.ExpectedTargetName}",
                $"clicks={dispatchResult?.ObservedActions ?? 0}"
            ])
            .Select(SensitiveTextSanitizer.Sanitize)
            .ToList();

        if (fsmResult.Success)
        {
            return new ExecutorHarnessPostActionVerification(
                Success: true,
                Status: ExecutorHarnessStatuses.Succeeded,
                Message: "post-action verification passed",
                TargetFound: true,
                ClickObserved: true,
                Signals: signals);
        }

        return new ExecutorHarnessPostActionVerification(
            Success: false,
            Status: fsmResult.FinalState == StepState.Blocked ? ExecutorHarnessStatuses.Blocked : ExecutorHarnessStatuses.Failed,
            Message: fsmResult.BlockReason == "NotVerified" || string.IsNullOrWhiteSpace(fsmResult.BlockReason)
                ? "post-action verification failed"
                : fsmResult.BlockReason,
            TargetFound: dispatchResult?.TargetVisible == true,
            ClickObserved: dispatchResult?.ObservedActions == 1 && dispatchResult.TargetVisible,
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
        ExecutorHarnessTargetResolution targetResolution,
        StepState finalState,
        FailureKind? failureKind,
        IReadOnlyList<StepTransitionEvidence> transitionEvidence,
        RecipeSafetyContract safetyContract,
        ApprovalBinding approvalBinding)
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
            TargetResolution: targetResolution,
            FinalState: finalState,
            FailureKind: failureKind,
            TransitionEvidence: transitionEvidence,
            SafetyContract: safetyContract,
            ApprovalBinding: approvalBinding);
    }

    private static ApprovalBinding BuildApprovalBinding(ExecutorHarnessTarget target, ApprovalDecision? decision)
    {
        SelectorEngine.TryParseLegacySelector(target.TargetRef, out var selector);
        var expectedIdentity = ExecutorHarnessTargetResolver.BuildAllowlistedIdentity();
        selector ??= SelectorEngine.GenerateSelector(expectedIdentity);
        selector = selector with
        {
            Provenance = Provenance.Fixture,
            ExpectedIdentity = expectedIdentity
        };

        var digest = ElementFingerprintBuilder.Build(expectedIdentity);
        return new ApprovalBinding(
            ApprovalDecisionId: decision?.ApprovalDecisionId ?? "pending-approval",
            ApprovedIdentityDigest: digest,
            Selector: selector,
            ActionKind: target.ActionKind,
            Mode: "supervised_harness",
            PolicyVersion: "hito-135/v1",
            EvidenceHash: digest);
    }

    private static RecipeSafetyContract BuildSafetyContract(ExecutorHarnessTarget target, ApprovalBinding binding)
    {
        var expectedIdentity = ExecutorHarnessTargetResolver.BuildAllowlistedIdentity();
        return new RecipeSafetyContract(
            SchemaVersion: 1,
            ContractId: $"contract-{target.HarnessId}",
            ActionKind: target.ActionKind,
            ExpectedIdentity: expectedIdentity,
            Selector: binding.Selector,
            WindowConstraints: new ExecutionWindowConstraints(LocalPilotOnly: true, ExternalNavigationBlocked: true),
            Reversible: false,
            MaxActions: 1,
            ActionCeiling: ActionCeiling.FullActionWithPreflight,
            Provenance: Provenance.Fixture,
            TrustLevel: TrustLevel.ProfileVerified,
            ApprovalRef: binding);
    }

    private static string Timestamp(DateTimeOffset? now)
    {
        return (now ?? DateTimeOffset.UtcNow).UtcDateTime.ToString("o");
    }

    private sealed class HarnessStepVerifier(ExecutorHarnessTarget target) : IStepVerifier
    {
        public StepVerificationResult Verify(RecipeSafetyContract contract, PatternExecutionResult dispatchResult)
        {
            var reasons = (dispatchResult.Signals ?? Array.Empty<string>())
                .Concat(dispatchResult.Reasons)
                .ToList();

            if (!dispatchResult.Success)
            {
                return new StepVerificationResult(
                    Success: false,
                    FailureKind: dispatchResult.FailureKind ?? FailureKind.Unverified,
                    MatchVerdict: "Different",
                    Reasons: reasons,
                    ObservedIdentity: dispatchResult.ObservedIdentity);
            }

            var matchesExpectedName = string.Equals(dispatchResult.TargetName, target.ExpectedTargetName, StringComparison.OrdinalIgnoreCase);
            var verified = dispatchResult.WindowFound &&
                           dispatchResult.TargetVisible &&
                           matchesExpectedName &&
                           dispatchResult.ObservedActions == 1;

            return new StepVerificationResult(
                Success: verified,
                FailureKind: verified ? null : FailureKind.Unverified,
                MatchVerdict: verified ? "Same" : "Different",
                Reasons: verified ? reasons.Concat(["post-action verification passed"]).ToList() : reasons.Concat(["post-action verification failed"]).ToList(),
                ObservedIdentity: dispatchResult.ObservedIdentity);
        }
    }

    private sealed class LegacyHarnessExecutorAdapter(
        IExecutorHarnessClickExecutor executor,
        ExecutorHarnessTarget target) : IUiaPatternExecutor
    {
        public PatternExecutionResult Invoke(PatternExecutionRequest request)
        {
            try
            {
                var result = executor.Click(new ExecutorHarnessClickCommand(
                    HarnessId: target.HarnessId,
                    WindowTitleContains: target.WindowTitleContains,
                    TargetRef: target.TargetRef,
                    ExpectedTargetName: target.ExpectedTargetName,
                    ActionKind: target.ActionKind));
                var postAction = result.PostActionState ?? BuildPostActionState(result);
                return new PatternExecutionResult(
                    Success: result.Success,
                    FailureKind: result.Success ? null : FailureKind.Unverified,
                    Reasons: result.Signals.Concat([result.Message]).ToList(),
                    ObservedIdentity: result.TargetResolution?.ObservedIdentity,
                    WindowFound: postAction.WindowFound,
                    TargetVisible: postAction.TargetVisible,
                    TargetName: postAction.TargetName,
                    ObservedActions: result.Clicks,
                    Signals: postAction.Signals);
            }
            catch (Exception ex)
            {
                return new PatternExecutionResult(
                    Success: false,
                    FailureKind: FailureKind.Unverified,
                    Reasons: [$"executor error: {ex.Message}"]);
            }
        }
    }

    private sealed class PassiveDesktopOwnershipMonitor : IDesktopOwnershipMonitor
    {
        private static readonly OwnershipSnapshot Snapshot = new(0, 0, "", DateTimeOffset.UnixEpoch);

        public OwnershipSnapshot Capture() => Snapshot;
        public bool HumanInputSince(OwnershipSnapshot baseline) => false;
        public bool ForegroundChanged(OwnershipSnapshot baseline) => false;
    }
}
