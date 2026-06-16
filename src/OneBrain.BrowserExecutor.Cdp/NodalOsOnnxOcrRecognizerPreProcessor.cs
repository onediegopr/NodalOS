using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M198 — ONNX OCR recognizer pre-processor.
// Resizes text-line crop to fixed height (32) and padded width, normalizes, returns NCHW tensor.
public sealed class NodalOsOnnxOcrRecognizerPreProcessor
{
    public const int TargetHeight = 32;

    public NodalOsOnnxOcrRecognizerPreProcessingResult Prepare(
        NodalOsOnnxOcrImagePreProcessingResult image,
        int maxWidth = 320)
    {
        if (image.Status != NodalOsOnnxOcrPreProcessingStatus.Success)
        {
            return Failure(image.Reason, image.Status);
        }

        var aspect = (double)image.Width / Math.Max(image.Height, 1);
        var targetWidth = Math.Min(maxWidth, (int)(TargetHeight * aspect));
        targetWidth = Math.Max(targetWidth, 1);

        var tensor = new float[1 * 3 * TargetHeight * targetWidth];
        var scaleX = (float)image.Width / targetWidth;
        var scaleY = (float)image.Height / TargetHeight;

        for (var y = 0; y < TargetHeight; y++)
        {
            for (var x = 0; x < targetWidth; x++)
            {
                var srcX = Math.Clamp((int)(x * scaleX), 0, image.Width - 1);
                var srcY = Math.Clamp((int)(y * scaleY), 0, image.Height - 1);
                var idx = (srcY * image.Width + srcX) * 4;
                var r = idx < image.NormalizedData.Length ? image.NormalizedData[idx + 0] : 0;
                var g = idx < image.NormalizedData.Length ? image.NormalizedData[idx + 1] : 0;
                var b = idx < image.NormalizedData.Length ? image.NormalizedData[idx + 2] : 0;

                tensor[0 * TargetHeight * targetWidth + y * targetWidth + x] = (r - NodalOsOnnxOcrImagePreProcessor.MeanR) / NodalOsOnnxOcrImagePreProcessor.StdR;
                tensor[1 * TargetHeight * targetWidth + y * targetWidth + x] = (g - NodalOsOnnxOcrImagePreProcessor.MeanG) / NodalOsOnnxOcrImagePreProcessor.StdG;
                tensor[2 * TargetHeight * targetWidth + y * targetWidth + x] = (b - NodalOsOnnxOcrImagePreProcessor.MeanB) / NodalOsOnnxOcrImagePreProcessor.StdB;
            }
        }

        return new NodalOsOnnxOcrRecognizerPreProcessingResult(
            $"rec-prep-{Guid.NewGuid():N}",
            NodalOsOnnxOcrPreProcessingStatus.Success,
            tensor,
            new[] { 1, 3, TargetHeight, targetWidth },
            MaxWidth: targetWidth,
            NoAuthority: true,
            Redacted: true,
            "recognizer input tensor prepared");
    }

    private static NodalOsOnnxOcrRecognizerPreProcessingResult Failure(string reason, NodalOsOnnxOcrPreProcessingStatus status)
    {
        return new NodalOsOnnxOcrRecognizerPreProcessingResult(
            $"rec-prep-{Guid.NewGuid():N}",
            status,
            Array.Empty<float>(),
            Array.Empty<int>(),
            MaxWidth: 0,
            NoAuthority: true,
            Redacted: true,
            BrowserCredentialRedactor.Redact(reason));
    }
}
