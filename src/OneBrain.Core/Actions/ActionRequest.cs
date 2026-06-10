namespace OneBrain.Core.Actions;
public sealed record ActionRequest(
    string Kind,
    string TargetRef,
    string? Text,
    string? ProcessName = null,
    string? WindowTitle = null,
    CancellationToken? CancellationToken = null);
