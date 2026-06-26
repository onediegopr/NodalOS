namespace OneBrain.ChromeLab.Bridge.Stealth;

public sealed record StealthHandoffCompletionResult(
    string HandoffId,
    bool FirstCompletion,
    bool Success,
    bool Verified);

public sealed class StealthHandoffGateway
{
    private readonly StealthRunnerRegistry _registry;
    private readonly HandoffVerificationService _verifier;
    private readonly ProtocolEventBuffer _events;
    private readonly Dictionary<string, StealthHandoffCompletionResult> _completedHandoffs = new(StringComparer.Ordinal);
    private readonly object _completedHandoffsLock = new();

    public StealthHandoffGateway(
        StealthRunnerRegistry registry,
        HandoffVerificationService verifier,
        ProtocolEventBuffer events)
    {
        _registry = registry;
        _verifier = verifier;
        _events = events;
    }

    public async Task<bool> ActivateAsync(
        UnifiedFrictionPolicyDecision decision,
        string taskId,
        CancellationToken ct)
    {
        _events.Add("stealth.handoff.activating",
            $"Activating stealth handoff for task {taskId}: {decision.Message}",
            runId: taskId);

        var activateMessage = new
        {
            type = StealthProtocol.MessageTypeHandoffActivate,
            taskId,
            handoffId = $"handoff-{Guid.NewGuid():n}",
            reason = decision.HandoffReason ?? "CaptchaRequired",
            message = decision.Message,
            screenshotRequired = true
        };

        await _registry.BroadcastAsync(activateMessage, ct);

        _events.Add("stealth.handoff.activated",
            $"Stealth handoff activated for task {taskId}",
            runId: taskId);

        return true;
    }

    public async Task<StealthHandoffCompletionResult> CompleteAsync(
        string taskId,
        string handoffId,
        bool success,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(handoffId))
            handoffId = $"missing-handoff-{taskId}";

        lock (_completedHandoffsLock)
        {
            if (_completedHandoffs.TryGetValue(handoffId, out var existing))
                return existing with { FirstCompletion = false };

            _completedHandoffs[handoffId] = new StealthHandoffCompletionResult(
                handoffId,
                FirstCompletion: true,
                Success: success,
                Verified: false);
        }

        _events.Add("handoff.completed", $"Handoff completed: success={success}", runId: taskId);

        var verified = false;
        if (success)
            verified = await VerifyAndResumeAsync(taskId, ct);

        var completed = new StealthHandoffCompletionResult(
            handoffId,
            FirstCompletion: true,
            Success: success,
            Verified: verified);

        lock (_completedHandoffsLock)
            _completedHandoffs[handoffId] = completed;

        return completed;
    }

    public async Task<bool> VerifyAndResumeAsync(string taskId, CancellationToken ct)
    {
        _events.Add("stealth.handoff.verifying",
            $"Verifying handoff completion for task {taskId}", runId: taskId);

        var verified = await _verifier.VerifyAsync(taskId, ct);

        _events.Add("stealth.handoff.verified",
            $"Handoff verification for task {taskId}: {(verified ? "passed" : "failed")}",
            runId: taskId);

        return verified;
    }
}
