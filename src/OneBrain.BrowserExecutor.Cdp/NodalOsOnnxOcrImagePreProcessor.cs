using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M198 — ONNX OCR image pre-processor.
// Accepts only verified redacted pixel crops; blocks full-screen, sensitive, and raw persistence.
public sealed class NodalOsOnnxOcrImagePreProcessor
{
    public const float MeanR = 0.485f;
    public const float MeanG = 0.456f;
    public const float MeanB = 0.406f;
    public const float StdR = 0.229f;
    public const float StdG = 0.224f;
    public const float StdB = 0.225f;

    public NodalOsOnnxOcrImagePreProcessingResult Prepare(
        byte[] imageBytes,
        int sourceWidth,
        int sourceHeight,
        NodalOsPixelRedactionResult? pixelRedactionResult,
        NodalOsOcrVisionSensitivity sensitivity,
        bool allowFullScreen,
        bool allowRawPersistence)
    {
        if (pixelRedactionResult is null)
        {
            return Failure(NodalOsOnnxOcrPreProcessingStatus.MissingRedaction, "pixel redaction V2 result required");
        }

        if (pixelRedactionResult.OriginalRawPersisted)
        {
            return Failure(NodalOsOnnxOcrPreProcessingStatus.BlockedRawPersistence, "raw image persistence detected");
        }

        if (allowRawPersistence)
        {
            return Failure(NodalOsOnnxOcrPreProcessingStatus.BlockedRawPersistence, "raw persistence not allowed");
        }

        if (allowFullScreen)
        {
            return Failure(NodalOsOnnxOcrPreProcessingStatus.BlockedFullScreen, "full-screen OCR blocked");
        }

        if (sensitivity >= NodalOsOcrVisionSensitivity.SensitiveSurface)
        {
            return Failure(NodalOsOnnxOcrPreProcessingStatus.BlockedSensitive, "sensitive surface blocked");
        }

        if (!pixelRedactionResult.SafeForOcr ||
            pixelRedactionResult.Decision is NodalOsPixelRedactionDecision.RedactionFailed or NodalOsPixelRedactionDecision.BlockedSensitive)
        {
            return Failure(NodalOsOnnxOcrPreProcessingStatus.MissingRedaction, "pixel redaction decision does not allow OCR");
        }

        var format = DetectFormat(imageBytes);
        if (format == NodalOsOnnxOcrImageFormat.Unknown)
        {
            return Failure(NodalOsOnnxOcrPreProcessingStatus.UnsupportedFormat, "image format not recognized");
        }

        // Decode to RGBA bytes. For real formats use a decoder; for tests accept synthetic raw RGBA.
        var rgba = DecodeToRgba(imageBytes, format, sourceWidth, sourceHeight);
        if (rgba is null)
        {
            return Failure(NodalOsOnnxOcrPreProcessingStatus.InvalidImage, "image decode failed");
        }

        return new NodalOsOnnxOcrImagePreProcessingResult(
            $"img-prep-{Guid.NewGuid():N}",
            NodalOsOnnxOcrPreProcessingStatus.Success,
            rgba,
            BatchSize: 1,
            Channels: 4,
            sourceHeight,
            sourceWidth,
            ScaleX: 1.0f,
            ScaleY: 1.0f,
            PadLeft: 0,
            PadTop: 0,
            format,
            sourceWidth,
            sourceHeight,
            NoAuthority: true,
            Redacted: true,
            "redacted crop prepared for model-specific preprocessing");
    }

    private static NodalOsOnnxOcrImagePreProcessingResult Failure(NodalOsOnnxOcrPreProcessingStatus status, string reason)
    {
        return new NodalOsOnnxOcrImagePreProcessingResult(
            $"img-prep-{Guid.NewGuid():N}",
            status,
            Array.Empty<float>(),
            0, 0, 0, 0,
            ScaleX: 0,
            ScaleY: 0,
            PadLeft: 0,
            PadTop: 0,
            NodalOsOnnxOcrImageFormat.Unknown,
            0, 0,
            NoAuthority: true,
            Redacted: true,
            BrowserCredentialRedactor.Redact(reason));
    }

    private static NodalOsOnnxOcrImageFormat DetectFormat(byte[] bytes)
    {
        if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50) return NodalOsOnnxOcrImageFormat.Png;
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8) return NodalOsOnnxOcrImageFormat.Jpeg;
        if (bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D) return NodalOsOnnxOcrImageFormat.Bmp;
        // Treat uniform synthetic buffers as raw RGBA for offline fixtures.
        if (bytes.Length > 0 && bytes.Length % 4 == 0) return NodalOsOnnxOcrImageFormat.RawRgba32;
        return NodalOsOnnxOcrImageFormat.Unknown;
    }

    private static float[]? DecodeToRgba(byte[] bytes, NodalOsOnnxOcrImageFormat format, int width, int height)
    {
        if (format == NodalOsOnnxOcrImageFormat.RawRgba32)
        {
            if (bytes.Length != width * height * 4)
                return null;

            var rgba = new float[bytes.Length];
            for (var i = 0; i < bytes.Length; i++)
                rgba[i] = bytes[i] / 255.0f;
            return rgba;
        }

        // Real image decoding requires SixLabors.ImageSharp or similar. Not implemented without model authority.
        return null;
    }
}
