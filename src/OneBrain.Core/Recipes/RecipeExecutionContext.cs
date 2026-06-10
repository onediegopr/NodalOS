namespace OneBrain.Core.Recipes;

public sealed class RecipeExecutionContext
{
    public Dictionary<string, string> Variables { get; } = new();
    public List<string> Notes { get; } = new();
}
