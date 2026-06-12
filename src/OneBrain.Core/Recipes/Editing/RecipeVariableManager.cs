using System.Text.Json;
using System.Text.RegularExpressions;

namespace OneBrain.Core.Recipes.Editing;

public static partial class RecipeVariableManager
{
    public static IReadOnlyList<RecipeVariableDefinition> ExtractVariablesFromJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        var variables = new Dictionary<string, RecipeVariableDefinition>(StringComparer.OrdinalIgnoreCase);

        if (document.RootElement.TryGetProperty("variables", out var explicitVariables) &&
            explicitVariables.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in explicitVariables.EnumerateObject())
                variables[property.Name] = BuildDefinition(property.Name, required: true, defaultValue: property.Value.ToString());
        }

        foreach (var variableName in FindTemplateVariables(document.RootElement))
        {
            if (!variables.ContainsKey(variableName))
                variables[variableName] = BuildDefinition(variableName, required: true, defaultValue: null);
        }

        return variables.Values.OrderBy(variable => variable.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static string DisplayValue(RecipeVariableDefinition variable)
    {
        var value = variable.DefaultValue ?? variable.ExampleValue ?? "";
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return variable.Sensitivity is RecipeVariableSensitivity.Secret or RecipeVariableSensitivity.Sensitive
            ? Mask(value)
            : value;
    }

    public static IReadOnlyList<string> ValidateVariables(IReadOnlyList<RecipeVariableDefinition> variables, IReadOnlyDictionary<string, string?> values)
    {
        var issues = new List<string>();
        foreach (var variable in variables)
        {
            values.TryGetValue(variable.Name, out var value);
            if (variable.Required && string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(variable.DefaultValue))
                issues.Add($"Missing required variable: {variable.Name}");

            if (variable.Sensitivity == RecipeVariableSensitivity.Secret && !string.IsNullOrWhiteSpace(value))
                issues.Add($"Secret variable must not be stored as plain value: {variable.Name}");

            if (!string.IsNullOrWhiteSpace(variable.Regex) && !string.IsNullOrWhiteSpace(value) && !Regex.IsMatch(value!, variable.Regex))
                issues.Add($"Variable does not match regex: {variable.Name}");
        }

        return issues;
    }

    private static IEnumerable<string> FindTemplateVariables(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            var value = element.GetString() ?? "";
            foreach (Match match in TemplateRegex().Matches(value))
                yield return match.Groups[1].Value.Trim();
            yield break;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            foreach (var variable in FindTemplateVariables(property.Value))
                yield return variable;
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            foreach (var variable in FindTemplateVariables(item))
                yield return variable;
        }
    }

    private static RecipeVariableDefinition BuildDefinition(string name, bool required, string? defaultValue)
    {
        var type = InferType(name);
        var sensitivity = InferSensitivity(name);
        return new RecipeVariableDefinition(
            Name: name,
            Type: type,
            Required: required,
            DefaultValue: sensitivity == RecipeVariableSensitivity.Secret ? null : defaultValue,
            ExampleValue: BuildExample(type),
            Sensitivity: sensitivity,
            Redacted: sensitivity is RecipeVariableSensitivity.Secret or RecipeVariableSensitivity.Sensitive,
            Regex: type == RecipeVariableTypes.Url ? @"^https?://" : null,
            AllowedValues: [],
            Min: null,
            Max: null);
    }

    private static string InferType(string name)
    {
        var lower = name.ToLowerInvariant();
        if (lower.Contains("url", StringComparison.Ordinal)) return RecipeVariableTypes.Url;
        if (lower.Contains("path", StringComparison.Ordinal) || lower.Contains("file", StringComparison.Ordinal)) return RecipeVariableTypes.FilePath;
        if (lower.Contains("selector", StringComparison.Ordinal) || lower.Contains("automationid", StringComparison.Ordinal)) return RecipeVariableTypes.Selector;
        if (lower.Contains("product", StringComparison.Ordinal)) return RecipeVariableTypes.Product;
        if (lower.Contains("contact", StringComparison.Ordinal) || lower.Contains("phone", StringComparison.Ordinal)) return RecipeVariableTypes.Contact;
        if (lower.Contains("message", StringComparison.Ordinal) || lower.Contains("text", StringComparison.Ordinal)) return RecipeVariableTypes.Message;
        if (lower.Contains("amount", StringComparison.Ordinal) || lower.Contains("price", StringComparison.Ordinal)) return RecipeVariableTypes.Amount;
        if (lower.Contains("date", StringComparison.Ordinal) || lower.Contains("time", StringComparison.Ordinal)) return RecipeVariableTypes.Date;
        if (lower.StartsWith("is", StringComparison.Ordinal) || lower.StartsWith("allow", StringComparison.Ordinal)) return RecipeVariableTypes.Boolean;
        if (lower.Contains("count", StringComparison.Ordinal) || lower.Contains("total", StringComparison.Ordinal)) return RecipeVariableTypes.Number;
        return RecipeVariableTypes.Text;
    }

    private static string InferSensitivity(string name)
    {
        var lower = name.ToLowerInvariant();
        if (lower.Contains("key", StringComparison.Ordinal) ||
            lower.Contains("secret", StringComparison.Ordinal) ||
            lower.Contains("token", StringComparison.Ordinal) ||
            lower.Contains("password", StringComparison.Ordinal))
            return RecipeVariableSensitivity.Secret;
        if (lower.Contains("contact", StringComparison.Ordinal) ||
            lower.Contains("phone", StringComparison.Ordinal) ||
            lower.Contains("message", StringComparison.Ordinal))
            return RecipeVariableSensitivity.Sensitive;
        if (lower.Contains("path", StringComparison.Ordinal) || lower.Contains("profile", StringComparison.Ordinal))
            return RecipeVariableSensitivity.Internal;
        return RecipeVariableSensitivity.Public;
    }

    private static string BuildExample(string type)
    {
        return type switch
        {
            RecipeVariableTypes.Url => "https://example.invalid/demo",
            RecipeVariableTypes.FilePath => "samples/example.json",
            RecipeVariableTypes.Amount => "199.00",
            RecipeVariableTypes.Date => "2026-06-12",
            RecipeVariableTypes.Boolean => "false",
            RecipeVariableTypes.Message => "Demo message preview",
            RecipeVariableTypes.Contact => "Demo Contact",
            _ => "demo-value"
        };
    }

    private static string Mask(string value)
    {
        return value.Length <= 4 ? "[redacted]" : value[..2] + "..." + value[^2..];
    }

    [GeneratedRegex(@"\{\{(.+?)\}\}")]
    private static partial Regex TemplateRegex();
}
