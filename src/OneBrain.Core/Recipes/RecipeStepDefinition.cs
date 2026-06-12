namespace OneBrain.Core.Recipes;

public sealed record RecipeStepDefinition
{
    public string? Id { get; init; }
    public string Kind { get; init; } = null!;
    public string? Process { get; init; }
    public string? Window { get; init; }
    public string? Role { get; init; }
    public string? Name { get; init; }
    public string? AutomationId { get; init; }
    public string? Class { get; init; }
    public string? TitleContains { get; init; }
    public string? Text { get; init; }
    public string? App { get; init; }
    public string? Path { get; init; }
    public string? Url { get; init; }
    public int? TimeoutMs { get; init; }
    public int? IntervalMs { get; init; }
    public bool? Poll { get; init; }
    public bool? ContinueOnError { get; init; }
    public string? Out { get; init; }
    public string? Before { get; init; }
    public string? After { get; init; }
    public double? Threshold { get; init; }
    public string? SaveAs { get; init; }
    public string? Property { get; init; }
    public string? Transform { get; init; }
    public string? Value { get; init; }
    public string? Expected { get; init; }
    public bool? IgnoreCase { get; init; }
    public RecipeConditionDefinition? Condition { get; init; }
    public List<RecipeStepDefinition>? Then { get; init; }
    public List<RecipeStepDefinition>? Else { get; init; }
    public Dictionary<string, string>? Args { get; init; }
}
