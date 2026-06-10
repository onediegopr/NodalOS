namespace OneBrain.Core.Models;

public sealed record WindowSnapshot(
    string Title,
    string ProcessName,
    int ProcessId,
    WindowBounds Bounds,
    bool IsForeground);
