using OneBrain.Core.Approval;
using OneBrain.Core.Contracts;

namespace OneBrain.Core.Execution;

public sealed record SafeExecutionRequest(
    RecipeSafetyContract Contract,
    IReadOnlyList<OneBrain.Core.Models.ElementIdentity> Candidates,
    PatternExecutionRequest DispatchRequest,
    CancellationToken CancellationToken = default);

public sealed record SafeExecutionResult(
    StepState FinalState,
    EvidenceLedger Ledger,
    bool Success,
    FailureKind? FailureKind,
    string BlockReason,
    IReadOnlyList<string> Reasons,
    ApprovalBindingResult? BindingResult = null,
    PatternExecutionResult? DispatchResult = null,
    StepVerificationResult? VerificationResult = null);

public sealed class SafeExecutionFsm
{
    private readonly ContractValidator _contractValidator;
    private readonly ApprovalBindingValidator _bindingValidator;
    private readonly IDesktopOwnershipMonitor _ownershipMonitor;
    private readonly IUiaPatternExecutor _patternExecutor;
    private readonly IStepVerifier _verifier;

    public SafeExecutionFsm(
        ContractValidator contractValidator,
        ApprovalBindingValidator bindingValidator,
        IDesktopOwnershipMonitor ownershipMonitor,
        IUiaPatternExecutor patternExecutor,
        IStepVerifier verifier)
    {
        _contractValidator = contractValidator;
        _bindingValidator = bindingValidator;
        _ownershipMonitor = ownershipMonitor;
        _patternExecutor = patternExecutor;
        _verifier = verifier;
    }

    public StepState CurrentState { get; private set; } = StepState.Created;

    public SafeExecutionResult Execute(SafeExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        CurrentState = StepState.Created;
        var ledger = new EvidenceLedger();
        var startOwnership = _ownershipMonitor.Capture();

        if (IsCancellationRequested(request.CancellationToken))
            return AbortForCancellation(ledger, request.Contract, "execution cancelled before validation");

        var validation = _contractValidator.Validate(request.Contract);
        if (!validation.IsValid)
        {
            Transition(
                ledger,
                StepTransition.ContractInvalid,
                StepState.Blocked,
                validation.FailureKind ?? FailureKind.PolicyDenied,
                blockReason: "ContractInvalid",
                request.Contract,
                matchVerdict: null,
                observedIdentityDigest: null,
                ownership: startOwnership,
                reasons: validation.Reasons);
            return Result(ledger, false, validation.FailureKind ?? FailureKind.PolicyDenied, "ContractInvalid", validation.Reasons);
        }

        Transition(
            ledger,
            StepTransition.ContractValid,
            StepState.Validated,
            failureKind: null,
            blockReason: null,
            request.Contract,
            matchVerdict: null,
            observedIdentityDigest: null,
            ownership: startOwnership,
            reasons: validation.Reasons);

        if (IsCancellationRequested(request.CancellationToken))
            return AbortForCancellation(ledger, request.Contract, "execution cancelled after validation");

        var binding = _bindingValidator.Validate(
            request.Contract.ApprovalRef!,
            request.Contract.ExpectedIdentity!,
            request.Candidates,
            request.Contract.Reversible);

        if (!binding.Success)
        {
            Transition(
                ledger,
                binding.FailureKind switch
                {
                    FailureKind.Ambiguous => StepTransition.BindingAmbiguous,
                    FailureKind.NotFound => StepTransition.BindingNotFound,
                    _ => StepTransition.BindingDifferentOrStale
                },
                StepState.Blocked,
                binding.FailureKind ?? FailureKind.Unverified,
                binding.BlockReason,
                request.Contract,
                binding.MatchVerdict,
                binding.ObservedIdentityDigest,
                startOwnership,
                binding.Reasons);
            return Result(ledger, false, binding.FailureKind ?? FailureKind.Unverified, binding.BlockReason, binding.Reasons, bindingResult: binding);
        }

        Transition(
            ledger,
            StepTransition.BindingSame,
            StepState.Bound,
            failureKind: null,
            blockReason: null,
            request.Contract,
            binding.MatchVerdict,
            binding.ObservedIdentityDigest,
            startOwnership,
            binding.Reasons);

        if (IsCancellationRequested(request.CancellationToken) && CancellationPolicy.CanCancel(CurrentState))
            return AbortForCancellation(ledger, request.Contract, "execution cancelled before dispatch", binding);

        if (_ownershipMonitor.HumanInputSince(startOwnership) || _ownershipMonitor.ForegroundChanged(startOwnership))
        {
            Transition(
                ledger,
                StepTransition.HumanInputBeforeDispatch,
                StepState.Aborted,
                FailureKind.HumanInterrupted,
                "HumanInterrupted",
                request.Contract,
                binding.MatchVerdict,
                binding.ObservedIdentityDigest,
                _ownershipMonitor.Capture(),
                ["human input or foreground change detected before dispatch"]);
            return Result(ledger, false, FailureKind.HumanInterrupted, "HumanInterrupted", ["human input or foreground change detected before dispatch"], binding);
        }

        var dispatchOwnership = _ownershipMonitor.Capture();
        Transition(
            ledger,
            StepTransition.DispatchStarted,
            StepState.Executing,
            failureKind: null,
            blockReason: null,
            request.Contract,
            binding.MatchVerdict,
            binding.ObservedIdentityDigest,
            dispatchOwnership,
            ["dispatch started"]);

        var dispatch = _patternExecutor.Invoke(request.DispatchRequest);
        if (_ownershipMonitor.HumanInputSince(dispatchOwnership) || _ownershipMonitor.ForegroundChanged(dispatchOwnership))
        {
            Transition(
                ledger,
                StepTransition.HumanInputDetected,
                StepState.Paused,
                failureKind: null,
                blockReason: null,
                request.Contract,
                binding.MatchVerdict,
                binding.ObservedIdentityDigest,
                _ownershipMonitor.Capture(),
                ["human input or foreground change detected during executing"]);
            Transition(
                ledger,
                StepTransition.HumanInputDetected,
                StepState.Aborted,
                FailureKind.HumanInterrupted,
                "HumanInterrupted",
                request.Contract,
                binding.MatchVerdict,
                binding.ObservedIdentityDigest,
                _ownershipMonitor.Capture(),
                ["paused step aborts fail-closed because human input was detected"]);
            return Result(ledger, false, FailureKind.HumanInterrupted, "HumanInterrupted", ["human input or foreground change detected during executing"], binding, dispatch);
        }

        if (!dispatch.Success)
        {
            Transition(
                ledger,
                StepTransition.ExecutorError,
                StepState.Failed,
                dispatch.FailureKind ?? FailureKind.Unverified,
                "ExecutorError",
                request.Contract,
                binding.MatchVerdict,
                binding.ObservedIdentityDigest,
                _ownershipMonitor.Capture(),
                dispatch.Reasons);
            return Result(ledger, false, dispatch.FailureKind ?? FailureKind.Unverified, "ExecutorError", dispatch.Reasons, binding, dispatch);
        }

        Transition(
            ledger,
            StepTransition.ExecutorReturned,
            StepState.Verifying,
            failureKind: null,
            blockReason: null,
            request.Contract,
            binding.MatchVerdict,
            binding.ObservedIdentityDigest,
            _ownershipMonitor.Capture(),
            dispatch.Reasons);

        if (IsCancellationRequested(request.CancellationToken) && CancellationPolicy.CanCancel(CurrentState))
            return AbortForCancellation(ledger, request.Contract, "execution cancelled while verifying", binding, dispatch);

        if (_ownershipMonitor.HumanInputSince(dispatchOwnership) || _ownershipMonitor.ForegroundChanged(dispatchOwnership))
        {
            Transition(
                ledger,
                StepTransition.HumanInputDetected,
                StepState.Paused,
                failureKind: null,
                blockReason: null,
                request.Contract,
                binding.MatchVerdict,
                binding.ObservedIdentityDigest,
                _ownershipMonitor.Capture(),
                ["human input or foreground change detected during verifying"]);
            Transition(
                ledger,
                StepTransition.HumanInputDetected,
                StepState.Aborted,
                FailureKind.HumanInterrupted,
                "HumanInterrupted",
                request.Contract,
                binding.MatchVerdict,
                binding.ObservedIdentityDigest,
                _ownershipMonitor.Capture(),
                ["paused verification aborts fail-closed because human input was detected"]);
            return Result(ledger, false, FailureKind.HumanInterrupted, "HumanInterrupted", ["human input or foreground change detected during verifying"], binding, dispatch);
        }

        var verification = _verifier.Verify(request.Contract, dispatch);
        if (!verification.Success)
        {
            Transition(
                ledger,
                StepTransition.NotVerified,
                StepState.Failed,
                verification.FailureKind ?? FailureKind.Unverified,
                "NotVerified",
                request.Contract,
                verification.MatchVerdict,
                binding.ObservedIdentityDigest,
                _ownershipMonitor.Capture(),
                verification.Reasons);
            return Result(ledger, false, verification.FailureKind ?? FailureKind.Unverified, "NotVerified", verification.Reasons, binding, dispatch, verification);
        }

        Transition(
            ledger,
            StepTransition.Verified,
            StepState.Succeeded,
            failureKind: null,
            blockReason: null,
            request.Contract,
            verification.MatchVerdict,
            binding.ObservedIdentityDigest,
            _ownershipMonitor.Capture(),
            verification.Reasons);

        return Result(ledger, true, null, "", verification.Reasons, binding, dispatch, verification);
    }

    private SafeExecutionResult AbortForCancellation(
        EvidenceLedger ledger,
        RecipeSafetyContract contract,
        string reason,
        ApprovalBindingResult? binding = null,
        PatternExecutionResult? dispatch = null)
    {
        Transition(
            ledger,
            StepTransition.CancellationRequested,
            StepState.Aborted,
            FailureKind.Cancelled,
            "Cancelled",
            contract,
            binding?.MatchVerdict,
            binding?.ObservedIdentityDigest,
            _ownershipMonitor.Capture(),
            [reason]);
        return Result(ledger, false, FailureKind.Cancelled, "Cancelled", [reason], binding, dispatch);
    }

    private SafeExecutionResult Result(
        EvidenceLedger ledger,
        bool success,
        FailureKind? failureKind,
        string blockReason,
        IReadOnlyList<string> reasons,
        ApprovalBindingResult? bindingResult = null,
        PatternExecutionResult? dispatchResult = null,
        StepVerificationResult? verificationResult = null)
    {
        return new SafeExecutionResult(
            FinalState: CurrentState,
            Ledger: ledger,
            Success: success,
            FailureKind: failureKind,
            BlockReason: blockReason,
            Reasons: reasons,
            BindingResult: bindingResult,
            DispatchResult: dispatchResult,
            VerificationResult: verificationResult);
    }

    private void Transition(
        EvidenceLedger ledger,
        StepTransition @event,
        StepState toState,
        FailureKind? failureKind,
        string? blockReason,
        RecipeSafetyContract contract,
        string? matchVerdict,
        string? observedIdentityDigest,
        OwnershipSnapshot ownership,
        IReadOnlyList<string> reasons)
    {
        var fromState = CurrentState;
        EnsureAllowed(fromState, toState, @event);
        ledger.Append(
            occurredAtUtc: DateTimeOffset.UtcNow,
            fromState: fromState,
            toState: toState,
            @event: @event,
            failureKind: failureKind,
            blockReason: blockReason,
            contractId: contract.ContractId,
            approvalDecisionId: contract.ApprovalRef?.ApprovalDecisionId,
            approvedIdentityDigest: contract.ApprovalRef?.ApprovedIdentityDigest,
            observedIdentityDigest: observedIdentityDigest,
            matchVerdict: matchVerdict,
            ownershipSnapshotHash: ownership.Hash,
            reasons: reasons);
        CurrentState = toState;
    }

    private static void EnsureAllowed(StepState fromState, StepState toState, StepTransition @event)
    {
        if (IsTerminal(fromState))
            throw new InvalidOperationException($"terminal state '{fromState}' cannot transition to '{toState}'");

        var allowed = (fromState, toState, @event) switch
        {
            (StepState.Created, StepState.Validated, StepTransition.ContractValid) => true,
            (StepState.Created, StepState.Blocked, StepTransition.ContractInvalid) => true,
            (StepState.Created, StepState.Aborted, StepTransition.CancellationRequested) => true,

            (StepState.Validated, StepState.Bound, StepTransition.BindingSame) => true,
            (StepState.Validated, StepState.Blocked, StepTransition.BindingDifferentOrStale) => true,
            (StepState.Validated, StepState.Blocked, StepTransition.BindingAmbiguous) => true,
            (StepState.Validated, StepState.Blocked, StepTransition.BindingNotFound) => true,
            (StepState.Validated, StepState.Aborted, StepTransition.CancellationRequested) => true,

            (StepState.Bound, StepState.Executing, StepTransition.DispatchStarted) => true,
            (StepState.Bound, StepState.Aborted, StepTransition.HumanInputBeforeDispatch) => true,
            (StepState.Bound, StepState.Aborted, StepTransition.CancellationRequested) => true,

            (StepState.Executing, StepState.Paused, StepTransition.HumanInputDetected) => true,
            (StepState.Executing, StepState.Failed, StepTransition.ExecutorError) => true,
            (StepState.Executing, StepState.Verifying, StepTransition.ExecutorReturned) => true,

            (StepState.Verifying, StepState.Succeeded, StepTransition.Verified) => true,
            (StepState.Verifying, StepState.Failed, StepTransition.NotVerified) => true,
            (StepState.Verifying, StepState.Paused, StepTransition.HumanInputDetected) => true,
            (StepState.Verifying, StepState.Aborted, StepTransition.CancellationRequested) => true,

            (StepState.Paused, StepState.Aborted, StepTransition.HumanInputDetected) => true,
            _ => false
        };

        if (!allowed)
            throw new InvalidOperationException($"transition '{@event}' is not allowed from '{fromState}' to '{toState}'");
    }

    private static bool IsTerminal(StepState state) =>
        state is StepState.Succeeded or StepState.Blocked or StepState.Failed or StepState.Aborted;

    private static bool IsCancellationRequested(CancellationToken token) =>
        token.CanBeCanceled && token.IsCancellationRequested;
}
