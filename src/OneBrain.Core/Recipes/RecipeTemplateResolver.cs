using System.Text.RegularExpressions;

namespace OneBrain.Core.Recipes;

public static class RecipeTemplateResolver
{
    private static readonly Regex TemplateRegex = new(@"\{\{(.+?)\}\}", RegexOptions.Compiled);

    public static string Resolve(string template, Dictionary<string, string> variables)
    {
        return TemplateRegex.Replace(template, match =>
        {
            var key = match.Groups[1].Value.Trim();
            if (variables.TryGetValue(key, out var value))
                return value;
            throw new RecipeVariableException($"Missing template variable: {key}");
        });
    }

    public static string? ResolveOrNull(string? template, Dictionary<string, string> variables)
    {
        return template == null ? null : Resolve(template, variables);
    }

    public static bool ContainsTemplate(string? text)
    {
        return text != null && TemplateRegex.IsMatch(text);
    }
}

public sealed class RecipeVariableException : Exception
{
    public RecipeVariableException(string message) : base(message) { }
}
