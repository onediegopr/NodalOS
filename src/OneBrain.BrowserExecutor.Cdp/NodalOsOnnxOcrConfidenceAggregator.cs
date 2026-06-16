using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M198 — ONNX OCR confidence aggregator.
public sealed class NodalOsOnnxOcrConfidenceAggregator
{
    public const double DefaultConfidenceThreshold = 0.6;

    public NodalOsOnnxOcrConfidenceAggregation Aggregate(IReadOnlyList<double> confidences)
    {
        if (confidences is null || confidences.Count == 0)
        {
            return new NodalOsOnnxOcrConfidenceAggregation(
                $"conf-{Guid.NewGuid():N}",
                AverageConfidence: 0,
                MinimumConfidence: 0,
                AllAboveThreshold: false,
                RequiresHumanReview: true,
                NoAuthority: true);
        }

        var avg = confidences.Average();
        var min = confidences.Min();
        var allAbove = confidences.All(c => c >= DefaultConfidenceThreshold);

        return new NodalOsOnnxOcrConfidenceAggregation(
            $"conf-{Guid.NewGuid():N}",
            avg,
            min,
            allAbove,
            RequiresHumanReview: !allAbove,
            NoAuthority: true);
    }
}
