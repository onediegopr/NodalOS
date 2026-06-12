using System.Text.RegularExpressions;

namespace OneBrain.Core.Recording;

public static partial class SensitiveTextSanitizer
{
    private const string Redacted = "[REDACTED]";

    private static readonly string[] SensitiveTerms =
    [
        "password",
        "contrasena",
        "secret",
        "token",
        "api key",
        "apikey",
        "cvv",
        "card",
        "tarjeta",
        "credit",
        "credito",
        "dni",
        "cedula"
    ];

    public static string Sanitize(string? value, out bool redacted)
    {
        redacted = false;
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var trimmed = value.Trim();
        if (SensitiveTerms.Any(term => trimmed.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
            EmailRegex().IsMatch(trimmed) ||
            LongNumberRegex().IsMatch(trimmed))
        {
            redacted = true;
            return Redacted;
        }

        var normalized = WhitespaceRegex().Replace(trimmed, " ");
        if (normalized.Length <= 160)
            return normalized;

        return normalized[..157] + "...";
    }

    public static string Sanitize(string? value)
    {
        return Sanitize(value, out _);
    }

    [GeneratedRegex(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b\d{12,19}\b")]
    private static partial Regex LongNumberRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
