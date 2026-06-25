namespace OneBrain.ChromeLab.Bridge.Stealth;

public sealed class StealthHandoffGateway
{
    private readonly StealthRunnerRegistry _registry;
    private readonly HandoffVerificationService _verifier;
    private readonly ProtocolEventBuffer _events;

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
