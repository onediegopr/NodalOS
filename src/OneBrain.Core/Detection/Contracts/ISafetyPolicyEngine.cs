namespace OneBrain.Core.Detection.Contracts;

/// <summary>Engine de política de seguridad. Decide sobre un StateDetectionResult.</summary>
public interface ISafetyPolicyEngine
{
    Task<StateDecision> EvaluateStateAsync(StateDetectionResult result, CancellationToken ct = default);
}
