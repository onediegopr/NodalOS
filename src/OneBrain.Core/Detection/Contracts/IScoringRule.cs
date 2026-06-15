namespace OneBrain.Core.Detection.Contracts;

/// <summary>Regla de scoring individual, explícita y debuggeable.</summary>
public interface IScoringRule
{
    string RuleId { get; }
    double Weight { get; }
    InteractionState TargetState { get; }
    double Evaluate(StructuralFeatures features);
}
