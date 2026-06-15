namespace OneBrain.Core.Execution;

using OneBrain.Core.Detection.Audit;
using OneBrain.Core.Detection.Contracts;
using OneBrain.Core.Detection.Policy;

/// <summary>
/// Integración del detector de estados con el pipeline de ejecución.
/// Separa percepción (detector), scoring (engine), y decisión (policy).
/// </summary>
public class InteractionStateDecisionIntegration
{
    private readonly IInteractionStateDetector _detector;
    private readonly ISafetyPolicyEngine _policy;
    private readonly IAuditLogger _audit;
    private const double MINIMUM_CONFIDENCE = 0.60;

    public InteractionStateDecisionIntegration(
        IInteractionStateDetector detector,
        ISafetyPolicyEngine? policy = null,
        IAuditLogger? audit = null)
    {
        _detector = detector;
        _policy = policy ?? new StateSafetyPolicyEngine();
        _audit = audit ?? new InMemoryAuditLogger();
    }

    public IAuditLogger AuditLog => _audit;

    /// <summary>
    /// Evalúa el estado actual del target y decide si proceder.
    /// </summary>
    public async Task<StateDecision> EvaluateAsync(TargetContext ctx, string stepId, CancellationToken ct = default)
    {
        // 1. PRE-FLIGHT
        var preFlight = await _detector.AssessPreFlightAsync(ctx, ct);
        await _audit.LogAsync(new StateScoringEvent
        {
            Phase = "PRE_FLIGHT",
            DetectedState = preFlight.DetectedState,
            ConfidenceScore = preFlight.ConfidenceScore,
            ConfigHash = preFlight.ScoringConfigHash,
            Vector = preFlight.Vector,
            Timestamp = DateTimeOffset.UtcNow
        }, ct);

        var decision = await _policy.EvaluateStateAsync(preFlight, ct);

        await _audit.LogAsync(new StateDecisionEvent
        {
            DecisionType = decision.Type,
            ReasonCode = decision.ReasonCode,
            DetectionResult = preFlight,
            StepId = stepId,
            Timestamp = DateTimeOffset.UtcNow
        }, ct);

        return decision;
    }
}
