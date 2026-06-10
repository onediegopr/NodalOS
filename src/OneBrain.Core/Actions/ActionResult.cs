namespace OneBrain.Core.Actions;

public sealed record ActionResult(
    bool Success,
    string Message,
    bool UsedFallback = false);
