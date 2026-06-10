namespace OneBrain.Core.Models;

public sealed record CognitiveSnapshot(
    WindowSnapshot Window,
    IReadOnlyList<UiElementSnapshot> Elements);
