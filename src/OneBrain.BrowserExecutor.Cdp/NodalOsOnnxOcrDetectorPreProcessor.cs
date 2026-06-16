using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M198 — ONNX OCR detector pre-processor.
// Resizes redacted crop to model input size (divisible by 32), normalizes ImageNet-style, returns NCHW tensor.
public sealed class NodalOsOnnxOcrDetectorPreProcessor
{
    public const int Divisor = 32;

    public NodalOsOnnxOcrDetectorPreProcessingResult Prepare(
        NodalOsOnnxOcrImagePreProcessingResult image,
        int? targetWidth = null,
        int? targetHeight = null)
    {
        if (image.Status != NodalOsOnnxOcrPreProcessingStatus.Success)
        {
            return Failure(image.Reason, image.Status);
        }

        var tw = targetWidth ?? RoundToDivisible(image.Width, Divisor);
        var th = targetHeight ?? RoundToDivisible(image.Height, Divisor);
        if (tw <= 0 || th <= 0)
        {
            return Failure("invalid detector target size", NodalOsOnnxOcrPreProcessingStatus.Failed);
        }

        var tensor = new float[1 * 3 * th * tw];
        // Bilinear resize + normalize. Synthetic RGBA fixture only; real RGB conversion deferred.
        var scaleX = (float)image.Width / tw;
        var scaleY = (float)image.Height / th;

        for (var y = 0; y < th; y++)
        {
            for (var x = 0; x < tw; x++)
            {
                var srcX = Math.Clamp((int)(x * scaleX), 0, image.Width - 1);
                var srcY = Math.Clamp((int)(y * scaleY), 0, image.Height - 1);
                var idx = (srcY * image.Width + srcX) * 4;
                var r = idx < image.NormalizedData.Length ? image.NormalizedData[idx + 0] : 0;
                var g = idx < image.NormalizedData.Length ? image.NormalizedData[idx + 1] : 0;
                var b = idx < image.NormalizedData.Length ? image.NormalizedData[idx + 2] : 0;

                tensor[0 * th * tw + y * tw + x] = (r - NodalOsOnnxOcrImagePreProcessor.MeanR) / NodalOsOnnxOcrImagePreProcessor.StdR;
                tensor[1 * th * tw + y * tw + x] = (g - NodalOsOnnxOcrImagePreProcessor.MeanG) / NodalOsOnnxOcrImagePreProcessor.StdG;
                tensor[2 * th * tw + y * tw + x] = (b - NodalOsOnnxOcrImagePreProcessor.MeanB) / NodalOsOnnxOcrImagePreProcessor.StdB;
            }
        }

        return new NodalOsOnnxOcrDetectorPreProcessingResult(
            $"det-prep-{Guid.NewGuid():N}",
            NodalOsOnnxOcrPreProcessingStatus.Success,
            tensor,
            new[] { 1, 3, th, tw },
            Scale: Math.Max(scaleX, scaleY),
            PaddedWidth: tw,
            PaddedHeight: th,
            NoAuthority: true,
            Redacted: true,
            "detector input tensor prepared");
    }

    private static int RoundToDivisible(int value, int divisor)
    {
        return (int)(Math.Ceiling((double)value / divisor) * divisor);
    }

    private static NodalOsOnnxOcrDetectorPreProcessingResult Failure(string reason, NodalOsOnnxOcrPreProcessingStatus status)
    {
        return new NodalOsOnnxOcrDetectorPreProcessingResult(
            $"det-prep-{Guid.NewGuid():N}",
            status,
            Array.Empty<float>(),
            Array.Empty<int>(),
            Scale: 0,
            PaddedWidth: 0,
            PaddedHeight: 0,
            NoAuthority: true,
            Redacted: true,
            BrowserCredentialRedactor.Redact(reason));
    }
}
