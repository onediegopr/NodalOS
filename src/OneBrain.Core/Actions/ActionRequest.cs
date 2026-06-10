namespace OneBrain.Core.Actions;

public sealed record ActionRequest(
    string Kind,
    string TargetRef,
    string? Text);
