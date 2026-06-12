using System.Text.Json;

namespace OneBrain.Core.Recipes.Editing;

public static class RecipeEditorService
{
    private static readonly HashSet<string> SensitiveKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "safe.click",
        "click",
        "invoke",
        "type",
        "submit",
        "login",
        "cookies.accept",
        "payment",
        "purchase",
        "pay"
    };

    public static RecipeEditorModel Load(string baseDirectory, string recipeId, string recipePath, string confidenceStatus = "new", string? lastVerifiedAt = null)
    {
        var fullPath = Path.GetFullPath(Path.Combine(Path.GetFullPath(baseDirectory), recipePath.Replace('/', Path.DirectorySeparatorChar)));
        using var document = JsonDocument.Parse(File.ReadAllText(fullPath));
        var root = document.RootElement;

        var name = GetString(root, "name") ?? recipeId;
        var description = GetString(root, "description") ?? "";
        var tags = ReadTags(root).ToList();
        var notes = ReadNotes(root).ToList();
        var riskLevel = InferRiskLevel(root);
        var steps = ReadSteps(root).ToList();

        return new RecipeEditorModel(
            RecipeId: recipeId,
            RecipePath: recipePath.Replace('\\', '/'),
            Title: name,
            Description: description,
            RiskLevel: riskLevel,
            ConfidenceStatus: confidenceStatus,
            LastVerifiedAt: lastVerifiedAt,
            Tags: tags,
            Notes: notes,
            Steps: steps,
            EditableFields:
            [
                new("title", "Name/title", true, "Safe metadata field"),
                new("description", "Description", true, "Safe metadata field"),
                new("notes", "Notes", true, "Safe metadata field"),
                new("tags", "Tags", true, "Safe metadata field"),
                new("steps.kind", "Step action kind", false, "Action kinds cannot be edited freely"),
                new("steps.args", "Step args/paths", false, "Paths and action args require validation")
            ]);
    }

    public static RecipeDraft CreateDraft(RecipeEditorModel model, RecipeEditRequest request, RecipeEditPolicyResult policyResult)
    {
        var status = policyResult.Allowed ? RecipeDraftStatuses.NeedsValidation : RecipeDraftStatuses.Rejected;
        var validationNotes = policyResult.Errors.Concat(policyResult.Warnings).ToList();
        return new RecipeDraft(
            DraftId: Guid.NewGuid().ToString("N"),
            CreatedAtUtc: DateTimeOffset.UtcNow.ToString("O"),
            SourceRecipeId: model.RecipeId,
            SourceRecipePath: model.RecipePath,
            Status: status,
            Title: request.Title ?? model.Title,
            Description: request.Description ?? model.Description,
            Tags: request.Tags.Count == 0 ? model.Tags : request.Tags,
            Notes: request.Notes.Count == 0 ? model.Notes : request.Notes,
            HumanReadableLabels: request.HumanReadableLabels,
            ValidationNotes: validationNotes);
    }

    private static IEnumerable<RecipeEditorStepSummary> ReadSteps(JsonElement root)
    {
        if (!root.TryGetProperty("steps", out var steps) || steps.ValueKind != JsonValueKind.Array)
            yield break;

        var number = 1;
        foreach (var step in steps.EnumerateArray())
        {
            var kind = GetString(step, "kind") ?? "unknown";
            var stepId = GetString(step, "id");
            var risk = SensitiveKinds.Contains(kind) ? "high" : "low";
            yield return new RecipeEditorStepSummary(
                StepNumber: number++,
                StepId: stepId,
                Kind: kind,
                HumanLabel: BuildHumanLabel(stepId, kind),
                RiskLevel: risk,
                RequiresApproval: SensitiveKinds.Contains(kind));
        }
    }

    private static string BuildHumanLabel(string? stepId, string kind)
    {
        if (!string.IsNullOrWhiteSpace(stepId))
            return stepId.Replace('-', ' ');
        return kind.Replace('.', ' ');
    }

    private static IEnumerable<string> ReadTags(JsonElement root)
    {
        if (root.TryGetProperty("metadata", out var metadata))
        {
            foreach (var property in metadata.EnumerateObject())
            {
                if (property.Name.Contains("category", StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String)
                    yield return property.Value.GetString() ?? "";
                if (property.Value.ValueKind == JsonValueKind.True && property.Name is "readOnly" or "localOnly" or "oneCommandDemo")
                    yield return property.Name;
            }
        }
    }

    private static IEnumerable<string> ReadNotes(JsonElement root)
    {
        var description = GetString(root, "description");
        if (!string.IsNullOrWhiteSpace(description))
            yield return description;
    }

    private static string InferRiskLevel(JsonElement root)
    {
        if (!root.TryGetProperty("steps", out var steps) || steps.ValueKind != JsonValueKind.Array)
            return "medium";

        foreach (var step in steps.EnumerateArray())
        {
            var kind = GetString(step, "kind");
            if (kind != null && SensitiveKinds.Contains(kind))
                return "high";
        }

        return "low";
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }
}
