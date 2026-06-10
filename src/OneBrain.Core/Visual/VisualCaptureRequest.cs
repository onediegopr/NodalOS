namespace OneBrain.Core.Visual;

public sealed record VisualCaptureRequest(
    string? ProcessName = null,
    string? WindowTitle = null,
    VisualElementTarget? Target = null,
    ManualRegion? ManualRegion = null,
    bool FullScreen = false,
    bool AllowFullScreen = false,
    string? OutputPath = null,
    bool IncludeMetadata = false)
{
    public bool IsValid => !string.IsNullOrEmpty(ProcessName) || ManualRegion != null || FullScreen;
}

public sealed record VisualElementTarget(
    string? Role = null,
    string? Name = null,
    string? AutomationId = null,
    string? ClassName = null,
    string? Ref = null);

public sealed record ManualRegion(
    int X,
    int Y,
    int Width,
    int Height)
{
    public bool IsValid => Width > 0 && Height > 0;
}
