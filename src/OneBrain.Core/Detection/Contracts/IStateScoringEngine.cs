namespace OneBrain.Core.Detection.Contracts;

public interface IStateScoringEngine
{
    string ConfigurationHash { get; }
    StateDetectionResult Score(StructuralFeatures structural, NetworkFeatures? network = null);
}
