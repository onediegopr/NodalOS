namespace OneBrain.Core.Recipes;

public sealed record RecipeConditionDefinition
{
    public string? Left { get; init; }
    public string Operator { get; init; } = "equals";
    public string? Right { get; init; }
    public bool? IgnoreCase { get; init; }
}
