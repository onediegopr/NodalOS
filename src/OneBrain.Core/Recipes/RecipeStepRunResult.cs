namespace OneBrain.Core.Recipes;

public sealed record RecipeStepRunResult(
    string? Id,
    string Kind,
    bool Success,
    string Message,
    long DurationMs,
    object? RawResult = null)
{
    public List<string> Notes { get; init; } = new();
}
