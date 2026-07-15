using System.Text.RegularExpressions;

namespace OneBrain.Core.Runtime;

public static partial class SafeRuntimeText
{
    public const int DefaultMaximumLength = 500;

    public static string Sanitize(string? value, int maximumLength = DefaultMaximumLength)
    {
        if (maximumLength < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumLength));

        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = WhitespaceRegex().Replace(value, " ").Trim();
        normalized = BearerRegex().Replace(normalized, "Bearer [REDACTED]");
        normalized = ApiKeyRegex().Replace(normalized, "[REDACTED_KEY]");
        normalized = AssignmentSecretRegex().Replace(
            normalized,
            match => $"{match.Groups[1].Value}=[REDACTED]");

        if (normalized.Length <= maximumLength)
            return normalized;

        return maximumLength == 1
            ? "…"
            : normalized[..(maximumLength - 1)] + "…";
    }

    public static IReadOnlyDictionary<string, string> SanitizeDimensions(
        IEnumerable<KeyValuePair<string, string?>>? dimensions,
        int maximumValueLength = 160)
    {
        if (maximumValueLength < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumValueLength));

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (dimensions is null)
            return result;

        foreach (var pair in dimensions)
        {
            var key = Sanitize(pair.Key, 80);
            if (key.Length == 0)
                continue;
            result[key] = Sanitize(pair.Value, maximumValueLength);
        }

        return result;
    }

    [GeneratedRegex(@"[\r\n\t]+|\s{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"(?i)\bBearer\s+[A-Za-z0-9._~+/=-]{8,}", RegexOptions.CultureInvariant)]
    private static partial Regex BearerRegex();

    [GeneratedRegex(@"\bsk-[A-Za-z0-9_-]{12,}\b|\bgh[pousr]_[A-Za-z0-9]{12,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex ApiKeyRegex();

    [GeneratedRegex(@"(?i)\b(api[_-]?key|secret|password|token)\s*[:=]\s*[^\s,;]{8,}", RegexOptions.CultureInvariant)]
    private static partial Regex AssignmentSecretRegex();
}
