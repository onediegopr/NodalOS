using System.Text.RegularExpressions;

namespace OneBrain.ChromeLab.Bridge;

public sealed record ToolValidationResult(bool Allowed, string Reason);

public static partial class ChromeLabToolPolicy
{
    private static readonly HashSet<string> AllowedTools = new(StringComparer.Ordinal)
    {
        "observePage",
        "getElementCatalog",
        "resolveTarget",
        "getCurrentTab",
        "navigate",
        "query",
        "read",
        "readElement",
        "click",
        "clickElement",
        "setValue",
        "setElementValue",
        "focusElement",
        "selectOption",
        "scrollIntoView",
        "scrollElementIntoView",
        "waitForSelector",
        "highlight",
        "highlightElement",
        "clearHighlight",
        "pauseForHuman",
        "stop"
    };

    public static ToolValidationResult Validate(string tool, IReadOnlyDictionary<string, object?> args)
    {
        if (!AllowedTools.Contains(tool))
            return new ToolValidationResult(false, "ToolNotAllowed");

        if (args.TryGetValue("selector", out var selectorValue) &&
            selectorValue is string selector &&
            !string.IsNullOrWhiteSpace(selector) &&
            !IsReasonableSelector(selector))
        {
            return new ToolValidationResult(false, "SelectorRejected");
        }

        if (args.TryGetValue("elementId", out var elementIdValue) &&
            elementIdValue is string elementId &&
            string.IsNullOrWhiteSpace(elementId))
        {
            return new ToolValidationResult(false, "ElementIdRejected");
        }

        if (string.Equals(tool, "navigate", StringComparison.Ordinal) &&
            (!args.TryGetValue("url", out var urlValue) ||
             urlValue is not string url ||
             !UrlValidator.IsAllowedNavigationUrl(url)))
        {
            return new ToolValidationResult(false, "UrlRejected");
        }

        if ((string.Equals(tool, "setValue", StringComparison.Ordinal) ||
             string.Equals(tool, "setElementValue", StringComparison.Ordinal)) &&
            args.TryGetValue("selector", out var setSelectorValue) &&
            LooksCredentialLike(setSelectorValue as string ?? ""))
        {
            return new ToolValidationResult(false, "CredentialFieldRejected");
        }

        if (string.Equals(tool, "resolveTarget", StringComparison.Ordinal) &&
            (!args.TryGetValue("targetText", out var targetTextValue) ||
             targetTextValue is not string targetText ||
             string.IsNullOrWhiteSpace(targetText) ||
             targetText.Length > 500))
        {
            return new ToolValidationResult(false, "TargetTextRejected");
        }

        return new ToolValidationResult(true, "Allowed");
    }

    public static bool IsAllowedTool(string tool) => AllowedTools.Contains(tool);

    public static bool LooksCredentialLike(string value)
    {
        var normalized = value.ToLowerInvariant();
        return normalized.Contains("password", StringComparison.Ordinal) ||
               normalized.Contains("contraseña", StringComparison.Ordinal) ||
               normalized.Contains("clave", StringComparison.Ordinal) ||
               normalized.Contains("token", StringComparison.Ordinal) ||
               normalized.Contains("otp", StringComparison.Ordinal) ||
               normalized.Contains("captcha", StringComparison.Ordinal);
    }

    private static bool IsReasonableSelector(string selector) =>
        !string.IsNullOrWhiteSpace(selector) &&
        selector.Length <= 500 &&
        !DangerousSelectorChars().IsMatch(selector);

    [GeneratedRegex("[<>`]", RegexOptions.CultureInvariant)]
    private static partial Regex DangerousSelectorChars();
}

public static class UrlValidator
{
    public static bool IsAllowedNavigationUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme is "http" or "https";
    }
}

public static class ChromeLabSecrets
{
    public const string ApiKeyPlaceholder = "PUT_YOUR_OPENAI_API_KEY_IN_ENV_OPENAI_API_KEY";
}
