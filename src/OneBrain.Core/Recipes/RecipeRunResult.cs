namespace OneBrain.Core.Recipes;

public sealed record RecipeRunResult(
    bool Success,
    string Recipe,
    int TotalSteps,
    int Passed,
    int Failed,
    long DurationMs,
    List<RecipeStepRunResult> Steps,
    List<string> Notes);
