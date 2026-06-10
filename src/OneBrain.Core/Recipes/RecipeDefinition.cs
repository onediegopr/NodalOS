namespace OneBrain.Core.Recipes;

public sealed record RecipeDefinition(
    string Name,
    string? Description = null,
    int? DefaultTimeoutMs = null,
    bool StopOnFirstFailure = true)
{
    public List<RecipeStepDefinition> Steps { get; init; } = new();
}
