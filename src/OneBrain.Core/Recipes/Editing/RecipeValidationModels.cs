namespace OneBrain.Core.Recipes.Editing;

public static class RecipeValidationSeverities
{
    public const string Info = "info";
    public const string Warning = "warning";
    public const string Error = "error";
    public const string Blocked = "blocked";
}

public sealed record RecipeValidationIssue(
    string Severity,
    string Code,
    string Message,
    string FieldPath,
    string Remediation);

public sealed record RecipeValidationResult(
    bool CanRun,
    bool CanPromote,
    IReadOnlyList<RecipeValidationIssue> Issues);
