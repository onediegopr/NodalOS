namespace OneBrain.Core.Visual;

public sealed record VisualCaptureResult(
    bool Success,
    string Message,
    string? OutputPath = null,
    VisualRegion? Region = null,
    int Width = 0,
    int Height = 0,
    long Bytes = 0,
    string? CapturedAtUtc = null,
    string? Notes = null);
