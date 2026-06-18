using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M295-M297 - PaddleOCR-aligned recognizer crop preprocessing.
//
// Replaces the aspect-distorting stretch used by the detector->recognizer bridge with PaddleOCR
// `resize_norm_img` semantics:
//   * fixed target height (48 for PP-OCRv5),
//   * width = min(maxWidth, ceil(height * sourceAspect)) preserving aspect ratio,
//   * bilinear resampling (cv2.resize default) instead of per-axis nearest neighbour,
//   * normalization (pixel - 0.5) / 0.5 (NOT ImageNet mean/std, NOT re-softmax),
//   * right-pad the remaining columns with 0.0 in normalized space.
//
// The legacy stretch mode is preserved so a follow-up ONNX probe can A/B the two geometries on the same
// detector crops. Pure/deterministic and ONNX-free, so it is fully unit-testable.
public sealed class NodalOsPaddleOcrRecognizerCropPreprocessor
{
    public const int DefaultTargetHeight = 48;
    public const int DefaultMaxWidth = 320;
    public const double PadValue = 0.0d; // PaddleOCR pads the normalized image with 0.0.

    // crop: row-major RGBA, 4 floats per pixel in [0,1]; width*height*4 length.
    public NodalOsPaddleOcrRecognizerCropTensor Prepare(
        float[] cropRgba,
        int cropWidth,
        int cropHeight,
        int targetHeight = DefaultTargetHeight,
        int maxWidth = DefaultMaxWidth,
        NodalOsPaddleOcrRecognizerResizeMode mode = NodalOsPaddleOcrRecognizerResizeMode.RatioPreservingRightPad)
    {
        ArgumentNullException.ThrowIfNull(cropRgba);
        if (cropWidth <= 0 || cropHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(cropWidth), "crop dimensions must be positive");
        if (targetHeight <= 0 || maxWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetHeight), "target dimensions must be positive");

        var sourceAspect = (double)cropWidth / cropHeight;
        var idealWidth = (int)Math.Ceiling(targetHeight * sourceAspect);
        var widthCapped = idealWidth > maxWidth;

        int resizedWidth;
        if (mode == NodalOsPaddleOcrRecognizerResizeMode.StretchToFixedWidth)
            resizedWidth = maxWidth;
        else
            resizedWidth = Math.Clamp(idealWidth, 1, maxWidth);

        var channels = 3;
        // The tensor is zero-initialized, which already equals PadValue (0.0) for the right padding columns
        // (PaddleOCR pads the normalized image with 0.0); only the resized text region is written below.
        var tensor = new float[channels * targetHeight * maxWidth];

        // RatioPreservingRightPad uses bilinear resampling (cv2.resize default); the legacy stretch mode keeps
        // the original per-axis nearest-neighbour sampling so historical evidence stays bit-reproducible.
        var useBilinear = mode == NodalOsPaddleOcrRecognizerResizeMode.RatioPreservingRightPad;
        var scaleX = (float)cropWidth / resizedWidth;
        var scaleY = (float)cropHeight / targetHeight;

        for (var y = 0; y < targetHeight; y++)
        {
            var srcYf = ((y + 0.5d) * cropHeight / targetHeight) - 0.5d;
            var nearestY = Math.Clamp((int)(y * scaleY), 0, cropHeight - 1);
            for (var x = 0; x < resizedWidth; x++)
            {
                var srcXf = ((x + 0.5d) * cropWidth / resizedWidth) - 0.5d;
                var nearestX = Math.Clamp((int)(x * scaleX), 0, cropWidth - 1);
                for (var c = 0; c < channels; c++)
                {
                    var sample = useBilinear
                        ? BilinearSample(cropRgba, cropWidth, cropHeight, srcXf, srcYf, c)
                        : PixelChannel(cropRgba, cropWidth, cropHeight, nearestX, nearestY, c);
                    var normalized = (sample - 0.5f) / 0.5f;
                    tensor[c * targetHeight * maxWidth + y * maxWidth + x] = normalized;
                }
            }
        }

        var paddedColumns = Math.Max(0, maxWidth - resizedWidth);
        var aspectPreserved = mode == NodalOsPaddleOcrRecognizerResizeMode.RatioPreservingRightPad && !widthCapped;
        var resizedAspect = (double)resizedWidth / targetHeight;

        return new NodalOsPaddleOcrRecognizerCropTensor(
            $"paddle-rec-crop-{Guid.NewGuid():N}",
            tensor,
            new[] { 1, channels, targetHeight, maxWidth },
            targetHeight,
            maxWidth,
            resizedWidth,
            paddedColumns,
            aspectPreserved,
            widthCapped,
            Math.Round(sourceAspect, 4),
            Math.Round(resizedAspect, 4),
            mode,
            "(pixel/255 - 0.5) / 0.5",
            PadValue,
            NoRawPersistence: true,
            NoAuthority: true,
            BrowserCredentialRedactor.Redact(
                $"recognizer crop {cropWidth}x{cropHeight} -> [1,3,{targetHeight},{maxWidth}] mode={mode}; " +
                $"resizedWidth={resizedWidth}; paddedColumns={paddedColumns}; aspectPreserved={aspectPreserved}"));
    }

    private static float BilinearSample(float[] rgba, int width, int height, double fx, double fy, int channel)
    {
        var x0 = (int)Math.Floor(fx);
        var y0 = (int)Math.Floor(fy);
        var x1 = x0 + 1;
        var y1 = y0 + 1;
        var dx = (float)(fx - x0);
        var dy = (float)(fy - y0);

        var v00 = PixelChannel(rgba, width, height, x0, y0, channel);
        var v10 = PixelChannel(rgba, width, height, x1, y0, channel);
        var v01 = PixelChannel(rgba, width, height, x0, y1, channel);
        var v11 = PixelChannel(rgba, width, height, x1, y1, channel);

        var top = v00 + (v10 - v00) * dx;
        var bottom = v01 + (v11 - v01) * dx;
        return top + (bottom - top) * dy;
    }

    private static float PixelChannel(float[] rgba, int width, int height, int x, int y, int channel)
    {
        x = Math.Clamp(x, 0, width - 1);
        y = Math.Clamp(y, 0, height - 1);
        var idx = (y * width + x) * 4 + channel;
        return idx >= 0 && idx < rgba.Length ? rgba[idx] : 1f;
    }
}
