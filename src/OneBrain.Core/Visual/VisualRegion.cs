namespace OneBrain.Core.Visual;

public sealed record VisualRegion(
    int Left,
    int Top,
    int Width,
    int Height,
    string Source,
    string Reason,
    string? ProcessName = null,
    string? WindowTitle = null,
    string? TargetRef = null)
{
    public bool IsValid => Width > 0 && Height > 0;
    public int Right => Left + Width;
    public int Bottom => Top + Height;
}
