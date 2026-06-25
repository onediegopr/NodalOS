namespace OneBrain.ChromeLab.Bridge.Stealth;

public interface IUnifiedFrictionPolicyEngine
{
    Task<UnifiedFrictionPolicyDecision> EvaluateAsync(
        FrictionSignal signal,
        string mode,
        int currentRetryCount,
        CancellationToken ct);
}
