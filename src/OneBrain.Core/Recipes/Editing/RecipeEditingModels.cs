namespace OneBrain.Core.Recipes.Editing;

public static class RecipeDraftStatuses
{
    public const string Draft = "draft";
    public const string NeedsValidation = "needs_validation";
    public const string Validated = "validated";
    public const string Rejected = "rejected";
    public const string Promoted = "promoted";
}

public static class RecipeVariableTypes
{
    public const string Text = "text";
    public const string Number = "number";
    public const string Url = "url";
    public const string FilePath = "file_path";
    public const string Selector = "selector";
    public const string Product = "product";
    public const string Contact = "contact";
    public const string Message = "message";
    public const string Amount = "amount";
    public const string Date = "date";
    public const string Boolean = "boolean";
}

public static class RecipeVariableSensitivity
{
    public const string Public = "public";
    public const string Internal = "internal";
    public const string Sensitive = "sensitive";
    public const string Secret = "secret";
}

public sealed record RecipeEditableField(
    string Field,
    string Label,
    bool CanEdit,
    string Reason);

public sealed record RecipeEditorStepSummary(
    int StepNumber,
    string? StepId,
    string Kind,
    string HumanLabel,
    string RiskLevel,
    bool RequiresApproval);

public sealed record RecipeEditorModel(
    string RecipeId,
    string RecipePath,
    string Title,
    string Description,
    string RiskLevel,
    string ConfidenceStatus,
    string? LastVerifiedAt,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Notes,
    IReadOnlyList<RecipeEditorStepSummary> Steps,
    IReadOnlyList<RecipeEditableField> EditableFields);

public sealed record RecipeEditRequest(
    string RecipeId,
    string RecipePath,
    string? Title,
    string? Description,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Notes,
    IReadOnlyDictionary<string, string> HumanReadableLabels,
    IReadOnlyDictionary<string, string> UnsafeFieldAttempts);

public sealed record RecipeEditPolicyResult(
    bool Allowed,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);

public sealed record RecipeDraft(
    string DraftId,
    string CreatedAtUtc,
    string SourceRecipeId,
    string SourceRecipePath,
    string Status,
    string Title,
    string Description,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Notes,
    IReadOnlyDictionary<string, string> HumanReadableLabels,
    IReadOnlyList<string> ValidationNotes);

public sealed record RecipeDraftArtifactWriteResult
{
    public bool Success { get; init; }
    public string Path { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public string Error { get; init; } = "";
}

public sealed record RecipeVariableDefinition(
    string Name,
    string Type,
    bool Required,
    string? DefaultValue,
    string? ExampleValue,
    string Sensitivity,
    bool Redacted,
    string? Regex,
    IReadOnlyList<string> AllowedValues,
    decimal? Min,
    decimal? Max);
