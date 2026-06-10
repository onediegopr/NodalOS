namespace OneBrain.Core.Models;

public sealed record WindowBounds(
    int Left,
    int Top,
    int Right,
    int Bottom)
{
    public int Width => Right - Left;
    public int Height => Bottom - Top;
}
