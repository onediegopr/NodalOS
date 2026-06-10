namespace OneBrain.Core.Recipes;

public sealed record RecipeDefinition(
    string Name,
    string? Description = null,
    int? DefaultTimeoutMs = null,
    bool StopOnFirstFailure = true)
{
    public List<RecipeStepDefinition> Steps { get; init; } = new();
    public Dictionary<string, string>? Variables { get; init; }
}
