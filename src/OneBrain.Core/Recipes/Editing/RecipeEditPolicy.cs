using OneBrain.Core.History;

namespace OneBrain.Core.Recipes.Editing;

public static class RecipeEditPolicy
{
    private static readonly HashSet<string> AllowedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "name",
        "title",
        "description",
        "notes",
        "tags",
        "humanReadableLabels"
    };

    public static RecipeEditPolicyResult Evaluate(RecipeEditRequest request)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        foreach (var field in request.UnsafeFieldAttempts.Keys)
        {
            if (!AllowedFields.Contains(field))
                errors.Add($"Unsafe edit rejected: {field}");
        }

        var values = new List<string?>();
        values.Add(request.Title);
        values.Add(request.Description);
        values.AddRange(request.Tags);
        values.AddRange(request.Notes);
        values.AddRange(request.HumanReadableLabels.Values);
        values.AddRange(request.UnsafeFieldAttempts.Values);

        if (values.Any(HistorySanitizer.ContainsSecretLikeContent))
            errors.Add("Secret-like content is not allowed in recipe drafts.");

        foreach (var value in values.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            if (Path.IsPathRooted(value!))
                warnings.Add("Absolute path-like content should be avoided in recipe drafts.");
        }

        return new RecipeEditPolicyResult(errors.Count == 0, errors, warnings);
    }
}
