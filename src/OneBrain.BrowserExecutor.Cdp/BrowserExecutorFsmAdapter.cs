using OneBrain.BrowserExecutor.Contracts;
using OneBrain.Core.Contracts;
using OneBrain.Core.Execution;

namespace OneBrain.BrowserExecutor.Cdp;

public enum BrowserExecutorStepState
{
    Planned,
    PolicyChecking,
    ApprovalRequired,
    ReadyToExecute,
    Executing,
    Executed,
    Verifying,
    Verified,
    Failed,
    Uncertain,
    Cancelled,
    TimedOut,
    Blocked,
    RequiresHuman
}

public sealed record BrowserExecutorStepRequest(
    string CorrelationId,
    BrowserAction Action,
    BrowserExecutorCapabilities Capabilities,
    BrowserTargetContext CurrentTarget,
    BrowserApprovalGrant? ApprovalGrant = null);

public sealed record BrowserApprovalGrant(string ApprovalId, bool Approved, string ApprovedBy, DateTimeOffset ApprovedAtUtc);

public sealed record BrowserHumanHandoffRequest(
    string CorrelationId,
    string ActionId,
    BrowserRiskClass RiskClass,
    string Reason,
    BrowserTargetContext TargetContext);

public sealed record BrowserPolicyDecision(
    bool Allowed,
    bool RequiresApproval,
    string Reason,
    BrowserRuntimeErrorCode? ErrorCode = null)
{
    public static BrowserPolicyDecision Allow(string reason = "policy allowed") => new(true, false, reason);
    public static BrowserPolicyDecision RequireApproval(string reason) => new(false, true, reason, BrowserRuntimeErrorCode.ActionRejected);
    public static BrowserPolicyDecision Block(BrowserRuntimeErrorCode errorCode, string reason) => new(false, false, reason, errorCode);
}

public sealed record BrowserExecutorEvidenceAudit(
    IReadOnlyList<BrowserEvidence> BrowserEvidence,
    EvidenceLedger CoreLedger)
{
    public bool HasEvidenceBeforeSuccess => BrowserEvidence.Count > 0 && CoreLedger.Entries.Count > 0;
}

public sealed record BrowserExecutorStepResult(
    BrowserExecutorStepState FinalState,
    bool Success,
    BrowserPolicyDecision PolicyDecision,
    BrowserHumanHandoffRequest? HumanHandoff,
    ChromeCdpActionResult? ActionResult,
    BrowserVerification? Verification,
    BrowserExecutorEvidenceAudit Evidence,
    BrowserRuntimeErrorCode ErrorCode,
    string Message,
    IReadOnlyList<BrowserExecutorStepState> StateHistory);

public interface IBrowserActionDispatcher
{
    Task<ChromeCdpActionResult> ExecuteActionAsync(BrowserAction action, CancellationToken cancellationToken);
}

public interface IBrowserVerificationDispatcher
{
    Task<BrowserVerification> VerifyAsync(BrowserAction action, CancellationToken cancellationToken);
}

public sealed class ChromeCdpBrowserActionDispatcher(ChromeCdpPageSession page) : IBrowserActionDispatcher, IBrowserVerificationDispatcher
{
    public Task<ChromeCdpActionResult> ExecuteActionAsync(BrowserAction action, CancellationToken cancellationToken) =>
        page.ExecuteActionAsync(action, cancellationToken);

    public Task<BrowserVerification> VerifyAsync(BrowserAction action, CancellationToken cancellationToken) =>
        page.VerifyAsync(action, cancellationToken: cancellationToken);
}

public sealed class BrowserExecutorPolicyGate
{
    public BrowserPolicyDecision Evaluate(BrowserExecutorStepRequest request)
    {
        var validation = request.Action.Validate(request.CurrentTarget);
        if (!validation.IsValid)
            return BrowserPolicyDecision.Block(MapValidationError(validation.Errors), string.Join("; ", validation.Errors));

        if (!request.Capabilities.CanExecute(request.Action))
            return BrowserPolicyDecision.Block(BrowserRuntimeErrorCode.ActionRejected, "executor capabilities or risk limit block action");

        if (request.Action.RiskClass == BrowserRiskClass.Critical && request.ApprovalGrant?.Approved != true)
            return BrowserPolicyDecision.RequireApproval("critical browser action requires explicit approval");

        if (request.Action.RequiresApproval && request.ApprovalGrant?.Approved != true)
            return BrowserPolicyDecision.RequireApproval("browser action requires human approval");

        return BrowserPolicyDecision.Allow();
    }

    private static BrowserRuntimeErrorCode MapValidationError(IReadOnlyList<string> errors)
    {
        var text = string.Join(" ", errors);
        if (text.Contains("stale", StringComparison.OrdinalIgnoreCase))
            return BrowserRuntimeErrorCode.TargetStale;
        if (text.Contains("IdempotencyKey", StringComparison.OrdinalIgnoreCase))
            return BrowserRuntimeErrorCode.IdempotencyRejected;
        return BrowserRuntimeErrorCode.ActionRejected;
    }
}
public sealed class BrowserExecutorStepRunner
{
    private readonly BrowserExecutorPolicyGate _policyGate;
    private readonly IBrowserActionDispatcher _actionDispatcher;
    private readonly IBrowserVerificationDispatcher _verificationDispatcher;

    public BrowserExecutorStepRunner(
        BrowserExecutorPolicyGate policyGate,
        IBrowserActionDispatcher actionDispatcher,
        IBrowserVerificationDispatcher verificationDispatcher)
    {
        _policyGate = policyGate;
        _actionDispatcher = actionDispatcher;
        _verificationDispatcher = verificationDispatcher;
    }

    public async Task<BrowserExecutorStepResult> ExecuteAsync(BrowserExecutorStepRequest request, CancellationToken cancellationToken = default)
    {
        var history = new List<BrowserExecutorStepState>();
        var browserEvidence = new List<BrowserEvidence>();
        var ledger = new EvidenceLedger();
        var currentState = BrowserExecutorStepState.Planned;
        history.Add(currentState);

        void Transition(BrowserExecutorStepState next, StepTransition transition, FailureKind? failureKind, string? reason)
        {
            var fromCore = MapState(currentState);
            var toCore = MapState(next);
            ledger.Append(
                DateTimeOffset.UtcNow,
                fromCore,
                toCore,
                transition,
                failureKind,
                reason,
                request.CorrelationId,
                request.ApprovalGrant?.ApprovalId,
                null,
                request.Action.TargetContext.StableHash(),
                null,
                null,
                string.IsNullOrWhiteSpace(reason) ? [] : [reason]);
            currentState = next;
            history.Add(next);
        }

        try
        {
            Transition(BrowserExecutorStepState.PolicyChecking, StepTransition.ContractValid, null, "policy checking");
            var policy = _policyGate.Evaluate(request);
            if (policy.RequiresApproval)
            {
                Transition(BrowserExecutorStepState.ApprovalRequired, StepTransition.ContractInvalid, FailureKind.PolicyDenied, policy.Reason);
                return Result(BrowserExecutorStepState.ApprovalRequired, false, policy, new BrowserHumanHandoffRequest(request.CorrelationId, request.Action.ActionId, request.Action.RiskClass, policy.Reason, request.Action.TargetContext), null, null, browserEvidence, ledger, BrowserRuntimeErrorCode.ActionRejected, policy.Reason, history);
            }

            if (!policy.Allowed)
            {
                Transition(BrowserExecutorStepState.Blocked, StepTransition.ContractInvalid, ToFailureKind(policy.ErrorCode), policy.Reason);
                return Result(BrowserExecutorStepState.Blocked, false, policy, null, null, null, browserEvidence, ledger, policy.ErrorCode ?? BrowserRuntimeErrorCode.ActionRejected, policy.Reason, history);
            }

            Transition(BrowserExecutorStepState.ReadyToExecute, StepTransition.ContractValid, null, "ready to execute");
            Transition(BrowserExecutorStepState.Executing, StepTransition.DispatchStarted, null, "dispatch started");

            ChromeCdpActionResult actionResult;
            try
            {
                actionResult = await _actionDispatcher.ExecuteActionAsync(request.Action, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Transition(BrowserExecutorStepState.TimedOut, StepTransition.ExecutorError, FailureKind.Timeout, "action timed out or was cancelled");
                return Result(BrowserExecutorStepState.TimedOut, false, policy, null, null, null, browserEvidence, ledger, BrowserRuntimeErrorCode.ActionTimeout, "action timed out or was cancelled", history);
            }

            browserEvidence.Add(actionResult.Evidence);
            if (!actionResult.Executed)
            {
                var errorCode = MapActionError(actionResult);
                Transition(errorCode == BrowserRuntimeErrorCode.ActionTimeout ? BrowserExecutorStepState.TimedOut : BrowserExecutorStepState.Blocked, StepTransition.ExecutorError, ToFailureKind(errorCode), actionResult.Error ?? actionResult.Status);
                return Result(errorCode == BrowserRuntimeErrorCode.ActionTimeout ? BrowserExecutorStepState.TimedOut : BrowserExecutorStepState.Blocked, false, policy, null, actionResult, null, browserEvidence, ledger, errorCode, actionResult.Error ?? actionResult.Status, history);
            }

            Transition(BrowserExecutorStepState.Executed, StepTransition.ExecutorReturned, null, "action executed; verification required");
            Transition(BrowserExecutorStepState.Verifying, StepTransition.ExecutorReturned, null, "verification started");

            var verification = await _verificationDispatcher.VerifyAsync(request.Action, cancellationToken).ConfigureAwait(false);
            browserEvidence.Add(new BrowserEvidence(
                EvidenceId: Guid.NewGuid().ToString("N"),
                RunId: request.Action.RunId,
                StepId: request.Action.StepId,
                ActionId: request.Action.ActionId,
                VerificationId: verification.VerificationId,
                TargetContext: verification.TargetContext,
                EvidenceType: BrowserEvidenceType.VerificationResult,
                CreatedAtUtc: DateTimeOffset.UtcNow,
                Summary: $"verification:{verification.Status}",
                PayloadRef: null,
                InlinePayload: null,
                RedactionApplied: true,
                SensitivityLevel: BrowserSensitivityLevel.Low));

            if (verification.Status == BrowserVerificationStatus.Verified && verification.AllowsStepDone() && verification.HasSemanticProof && browserEvidence.Count > 0)
            {
                Transition(BrowserExecutorStepState.Verified, StepTransition.Verified, null, "verification succeeded");
                return Result(BrowserExecutorStepState.Verified, true, policy, null, actionResult, verification, browserEvidence, ledger, BrowserRuntimeErrorCode.None, "browser step verified", history);
            }

            var final = verification.Status == BrowserVerificationStatus.Uncertain ? BrowserExecutorStepState.Uncertain : BrowserExecutorStepState.Failed;
            var code = verification.Status == BrowserVerificationStatus.Uncertain ? BrowserRuntimeErrorCode.VerificationUncertain : BrowserRuntimeErrorCode.VerificationFailed;
            Transition(final, StepTransition.NotVerified, FailureKind.Unverified, verification.FailureReason ?? verification.Status.ToString());
            return Result(final, false, policy, null, actionResult, verification, browserEvidence, ledger, code, verification.FailureReason ?? verification.Status.ToString(), history);
        }
        catch (OperationCanceledException)
        {
            Transition(BrowserExecutorStepState.Cancelled, StepTransition.CancellationRequested, FailureKind.Cancelled, "browser step cancelled");
            return Result(BrowserExecutorStepState.Cancelled, false, BrowserPolicyDecision.Block(BrowserRuntimeErrorCode.ActionRejected, "cancelled"), null, null, null, browserEvidence, ledger, BrowserRuntimeErrorCode.ActionRejected, "browser step cancelled", history);
        }
        catch (Exception ex)
        {
            Transition(BrowserExecutorStepState.Failed, StepTransition.ExecutorError, FailureKind.Unverified, ex.Message);
            return Result(BrowserExecutorStepState.Failed, false, BrowserPolicyDecision.Block(BrowserRuntimeErrorCode.UnexpectedException, ex.Message), null, null, null, browserEvidence, ledger, BrowserRuntimeErrorCode.UnexpectedException, ex.Message, history);
        }
    }

    private static BrowserExecutorStepResult Result(
        BrowserExecutorStepState finalState,
        bool success,
        BrowserPolicyDecision policy,
        BrowserHumanHandoffRequest? handoff,
        ChromeCdpActionResult? actionResult,
        BrowserVerification? verification,
        IReadOnlyList<BrowserEvidence> browserEvidence,
        EvidenceLedger ledger,
        BrowserRuntimeErrorCode errorCode,
        string message,
        IReadOnlyList<BrowserExecutorStepState> history) =>
        new(
            finalState,
            success,
            policy,
            handoff,
            actionResult,
            verification,
            new BrowserExecutorEvidenceAudit(browserEvidence, ledger),
            errorCode,
            message,
            history);

    private static StepState MapState(BrowserExecutorStepState state) => state switch
    {
        BrowserExecutorStepState.Planned => StepState.Created,
        BrowserExecutorStepState.PolicyChecking => StepState.Validated,
        BrowserExecutorStepState.ApprovalRequired => StepState.Paused,
        BrowserExecutorStepState.ReadyToExecute => StepState.Bound,
        BrowserExecutorStepState.Executing => StepState.Executing,
        BrowserExecutorStepState.Executed => StepState.Verifying,
        BrowserExecutorStepState.Verifying => StepState.Verifying,
        BrowserExecutorStepState.Verified => StepState.Succeeded,
        BrowserExecutorStepState.Failed => StepState.Failed,
        BrowserExecutorStepState.Uncertain => StepState.Failed,
        BrowserExecutorStepState.Cancelled => StepState.Aborted,
        BrowserExecutorStepState.TimedOut => StepState.Failed,
        BrowserExecutorStepState.Blocked => StepState.Blocked,
        BrowserExecutorStepState.RequiresHuman => StepState.Paused,
        _ => StepState.Failed
    };

    private static FailureKind? ToFailureKind(BrowserRuntimeErrorCode? errorCode) => errorCode switch
    {
        BrowserRuntimeErrorCode.TargetStale => FailureKind.Stale,
        BrowserRuntimeErrorCode.TargetDetached => FailureKind.SourceUnavailable,
        BrowserRuntimeErrorCode.ActionTimeout => FailureKind.Timeout,
        BrowserRuntimeErrorCode.VerificationFailed => FailureKind.Unverified,
        BrowserRuntimeErrorCode.VerificationUncertain => FailureKind.Unverified,
        BrowserRuntimeErrorCode.IdempotencyRejected => FailureKind.Blocked,
        BrowserRuntimeErrorCode.ActionRejected => FailureKind.PolicyDenied,
        BrowserRuntimeErrorCode.EnvironmentUnsupported => FailureKind.SourceUnavailable,
        BrowserRuntimeErrorCode.CleanupFailed => FailureKind.Unverified,
        BrowserRuntimeErrorCode.UnexpectedException => FailureKind.Unverified,
        BrowserRuntimeErrorCode.None or null => null,
        _ => FailureKind.Unverified
    };

    private static BrowserRuntimeErrorCode MapActionError(ChromeCdpActionResult result)
    {
        var text = $"{result.Status} {result.Error}";
        if (text.Contains("timeout", StringComparison.OrdinalIgnoreCase) || text.Contains("cancel", StringComparison.OrdinalIgnoreCase))
            return BrowserRuntimeErrorCode.ActionTimeout;
        if (text.Contains("stale", StringComparison.OrdinalIgnoreCase))
            return BrowserRuntimeErrorCode.TargetStale;
        if (text.Contains("Idempotency", StringComparison.OrdinalIgnoreCase) || text.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
            return BrowserRuntimeErrorCode.IdempotencyRejected;
        return BrowserRuntimeErrorCode.ActionRejected;
    }
}
