using System.Text.Json;
using OneBrain.Core.History;

namespace OneBrain.Core.Recipes.Editing;

public static class RecipeLinter
{
    private static readonly HashSet<string> AllowedActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "artifact.summarizeProductEvidence",
        "artifact.writeProductEvidence",
        "report.writeProductEvidenceMarkdown",
        "report.writeProductEvidenceHtml",
        "extract.productEvidence",
        "note",
        "if",
        "assert.contains",
        "assert.equals",
        "delay",
        "sleep",
        "profile.load",
        "browser.open",
        "browser.read",
        "browser.close"
    };

    private static readonly HashSet<string> SensitiveActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "safe.click",
        "click",
        "invoke",
        "type",
        "submit",
        "login",
        "accept_terms",
        "accept_cookies",
        "cookies.accept",
        "pay",
        "payment",
        "purchase",
        "run_script",
        "script",
        "shell",
        "command"
    };

    public static RecipeValidationResult ValidateJson(string json, string? recipePath = null)
    {
        var issues = new List<RecipeValidationIssue>();
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (Exception ex)
        {
            return new RecipeValidationResult(false, false,
            [
                Issue(RecipeValidationSeverities.Blocked, "invalid_json", ex.Message, "$", "Fix JSON before saving or running.")
            ]);
        }

        using (document)
        {
            var root = document.RootElement;
            if (!root.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(name.GetString()))
                issues.Add(Issue(RecipeValidationSeverities.Error, "missing_metadata", "Recipe is missing name.", "$.name", "Add recipe name."));
            if (!root.TryGetProperty("description", out var description) || description.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(description.GetString()))
                issues.Add(Issue(RecipeValidationSeverities.Warning, "missing_description", "Recipe is missing description.", "$.description", "Add human-readable description."));

            var allText = root.GetRawText();
            if (HistorySanitizer.ContainsSecretLikeContent(allText))
                issues.Add(Issue(RecipeValidationSeverities.Blocked, "secret_like_content", "Recipe contains secret-like content.", "$", "Move secrets to secure environment references."));

            var variables = RecipeVariableManager.ExtractVariablesFromJson(json);
            var missingVariables = RecipeVariableManager.ValidateVariables(variables, new Dictionary<string, string?>());
            foreach (var missing in missingVariables)
                issues.Add(Issue(RecipeValidationSeverities.Warning, "missing_variable_value", missing, "$.variables", "Provide variable default/example before promotion."));

            if (root.TryGetProperty("steps", out var steps) && steps.ValueKind == JsonValueKind.Array)
                ValidateSteps(steps, "$.steps", issues);
            else
                issues.Add(Issue(RecipeValidationSeverities.Error, "missing_steps", "Recipe has no steps array.", "$.steps", "Add explicit steps."));

            if (recipePath != null && recipePath.Contains("mercadolibre", StringComparison.OrdinalIgnoreCase) &&
                !allText.Contains("Diagnostic allowed", StringComparison.OrdinalIgnoreCase) &&
                !allText.Contains("diagnosticAllowed", StringComparison.OrdinalIgnoreCase))
                issues.Add(Issue(RecipeValidationSeverities.Warning, "external_fragile_missing_diagnostic", "External fragile recipe should document diagnosticAllowed.", recipePath, "Document diagnostic mode for external blocking."));
        }

        var hasBlocked = issues.Any(issue => issue.Severity == RecipeValidationSeverities.Blocked);
        var hasError = issues.Any(issue => issue.Severity == RecipeValidationSeverities.Error);
        return new RecipeValidationResult(
            CanRun: !hasBlocked && !hasError,
            CanPromote: !hasBlocked && !hasError && issues.All(issue => issue.Severity != RecipeValidationSeverities.Warning),
            Issues: issues);
    }

    private static void ValidateSteps(JsonElement steps, string path, List<RecipeValidationIssue> issues)
    {
        var index = 0;
        foreach (var step in steps.EnumerateArray())
        {
            var stepPath = $"{path}[{index}]";
            var kind = GetString(step, "kind");
            if (string.IsNullOrWhiteSpace(kind))
            {
                issues.Add(Issue(RecipeValidationSeverities.Error, "missing_step_kind", "Step is missing kind.", $"{stepPath}.kind", "Add explicit allowlisted action kind."));
            }
            else
            {
                if (SensitiveActions.Contains(kind))
                    issues.Add(Issue(RecipeValidationSeverities.Blocked, "sensitive_action_requires_approval", $"Sensitive action '{kind}' requires explicit approval policy.", $"{stepPath}.kind", "Bind approval policy before run/promotion."));
                else if (!AllowedActions.Contains(kind))
                    issues.Add(Issue(RecipeValidationSeverities.Blocked, "action_not_allowlisted", $"Action '{kind}' is not allowlisted.", $"{stepPath}.kind", "Use an allowlisted recipe action."));

                if (kind.Contains("payment", StringComparison.OrdinalIgnoreCase) ||
                    kind.Contains("purchase", StringComparison.OrdinalIgnoreCase) ||
                    kind.Contains("login", StringComparison.OrdinalIgnoreCase) ||
                    kind.Contains("cookie", StringComparison.OrdinalIgnoreCase))
                    issues.Add(Issue(RecipeValidationSeverities.Blocked, "critical_action_blocked", $"Critical action '{kind}' is blocked by default.", $"{stepPath}.kind", "Do not run payment/purchase/login/cookie actions without platform-level approval."));
            }

            var raw = step.GetRawText();
            if (raw.Contains("powershell", StringComparison.OrdinalIgnoreCase) ||
                raw.Contains("cmd.exe", StringComparison.OrdinalIgnoreCase) ||
                raw.Contains("bash", StringComparison.OrdinalIgnoreCase))
                issues.Add(Issue(RecipeValidationSeverities.Blocked, "arbitrary_command", "Step contains arbitrary command content.", stepPath, "Remove command execution from recipe."));

            if (HistorySanitizer.ContainsSecretLikeContent(raw))
                issues.Add(Issue(RecipeValidationSeverities.Blocked, "secret_like_content", "Step contains secret-like content.", stepPath, "Use secure secret references."));

            if (step.TryGetProperty("path", out var pathValue) && pathValue.ValueKind == JsonValueKind.String && Path.IsPathRooted(pathValue.GetString() ?? ""))
                issues.Add(Issue(RecipeValidationSeverities.Warning, "absolute_path", "Step contains an absolute path.", $"{stepPath}.path", "Use repo-relative paths for portable recipes."));

            if (kind is "browser.open" && !raw.Contains("readOnly", StringComparison.OrdinalIgnoreCase) && !raw.Contains("diagnostic", StringComparison.OrdinalIgnoreCase))
                issues.Add(Issue(RecipeValidationSeverities.Warning, "browser_external_requires_diagnostic", "Browser open should be marked read-only/diagnostic for external pages.", stepPath, "Document read-only/diagnostic mode."));

            if (step.TryGetProperty("then", out var thenSteps) && thenSteps.ValueKind == JsonValueKind.Array)
                ValidateSteps(thenSteps, $"{stepPath}.then", issues);
            if (step.TryGetProperty("else", out var elseSteps) && elseSteps.ValueKind == JsonValueKind.Array)
                ValidateSteps(elseSteps, $"{stepPath}.else", issues);

            index++;
        }
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static RecipeValidationIssue Issue(string severity, string code, string message, string fieldPath, string remediation)
    {
        return new RecipeValidationIssue(severity, code, message, fieldPath, remediation);
    }
}
