using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M198 — ONNX OCR recognizer post-processor.
// Greedy CTC decode + dictionary lookup. Low confidence => human review.
public sealed class NodalOsOnnxOcrRecognizerPostProcessor
{
    private readonly NodalOsOnnxOcrCharacterDictionary _dictionary;
    private readonly NodalOsOnnxOcrConfidenceAggregator _confidenceAggregator;

    public NodalOsOnnxOcrRecognizerPostProcessor(
        NodalOsOnnxOcrCharacterDictionary? dictionary = null,
        NodalOsOnnxOcrConfidenceAggregator? confidenceAggregator = null)
    {
        _dictionary = dictionary ?? new NodalOsOnnxOcrCharacterDictionary().Load("en-ascii", "en");
        _confidenceAggregator = confidenceAggregator ?? new NodalOsOnnxOcrConfidenceAggregator();
    }

    public NodalOsOnnxOcrRecognizerPostProcessingResult Decode(
        float[] output,
        int[] outputShape,
        double confidenceThreshold = 0.6)
    {
        if (output is null || output.Length == 0 || outputShape is null || outputShape.Length == 0)
        {
            return Failure(NodalOsOnnxOcrPostProcessingStatus.InvalidOutput, "empty recognizer output");
        }

        // Expected shape: [T, 1, VocabSize+1] or [T, VocabSize+1]
        int timeSteps;
        int vocabSize;
        if (outputShape.Length == 3 && outputShape[1] == 1)
        {
            timeSteps = outputShape[0];
            vocabSize = outputShape[2];
        }
        else if (outputShape.Length == 2)
        {
            timeSteps = outputShape[0];
            vocabSize = outputShape[1];
        }
        else
        {
            return Failure(NodalOsOnnxOcrPostProcessingStatus.UnsupportedModelShape, $"unsupported recognizer output shape [{string.Join(",", outputShape)}]");
        }

        if (output.Length != timeSteps * vocabSize)
        {
            return Failure(NodalOsOnnxOcrPostProcessingStatus.UnsupportedModelShape, "recognizer output length does not match shape");
        }

        var sequence = new List<int>();
        var confidences = new List<double>();
        for (var t = 0; t < timeSteps; t++)
        {
            var maxIndex = 0;
            var maxValue = output[t * vocabSize];
            for (var v = 1; v < vocabSize; v++)
            {
                var value = output[t * vocabSize + v];
                if (value > maxValue)
                {
                    maxValue = value;
                    maxIndex = v;
                }
            }

            sequence.Add(maxIndex);
            confidences.Add(maxValue);
        }

        var blankIndex = _dictionary.BlankIndex;
        var text = _dictionary.DecodeCtc(sequence, _dictionary.Characters, blankIndex);
        var aggregation = _confidenceAggregator.Aggregate(confidences);

        var lowConfidence = !aggregation.AllAboveThreshold || aggregation.AverageConfidence < confidenceThreshold;
        var status = lowConfidence ? NodalOsOnnxOcrPostProcessingStatus.RequiresHumanReview : NodalOsOnnxOcrPostProcessingStatus.Success;

        var candidate = new NodalOsOnnxOcrRecognitionCandidate(
            $"candidate-{Guid.NewGuid():N}",
            text,
            aggregation.AverageConfidence,
            sequence,
            lowConfidence,
            lowConfidence);

        return new NodalOsOnnxOcrRecognizerPostProcessingResult(
            $"rec-post-{Guid.NewGuid():N}",
            status,
            new[] { candidate },
            NoAuthority: true,
            Redacted: true,
            lowConfidence ? "low confidence; requires human review" : "recognizer text decoded");
    }

    private NodalOsOnnxOcrRecognizerPostProcessingResult Failure(NodalOsOnnxOcrPostProcessingStatus status, string reason)
    {
        return new NodalOsOnnxOcrRecognizerPostProcessingResult(
            $"rec-post-{Guid.NewGuid():N}",
            status,
            Array.Empty<NodalOsOnnxOcrRecognitionCandidate>(),
            NoAuthority: true,
            Redacted: true,
            BrowserCredentialRedactor.Redact(reason));
    }
}
