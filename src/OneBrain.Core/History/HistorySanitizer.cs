using System.Text.RegularExpressions;

namespace OneBrain.Core.History;

public static partial class HistorySanitizer
{
    public static bool ContainsSecretLikeContent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return SecretLikeRegex().IsMatch(value);
    }

    public static string SanitizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var sanitized = LocalUserPathRegex().Replace(value, "[LOCAL_PATH]");
        return SecretLikeRegex().Replace(sanitized, "[REDACTED]");
    }

    public static string NormalizeArtifactPath(string baseDirectory, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "";

        var normalized = path.Trim().Replace('\\', '/');
        if (ContainsSecretLikeContent(normalized))
            return "[REDACTED]";

        var baseFullPath = Path.GetFullPath(baseDirectory);
        if (Path.IsPathRooted(path))
        {
            var fullPath = Path.GetFullPath(path);
            if (fullPath.StartsWith(baseFullPath, StringComparison.OrdinalIgnoreCase))
                return Path.GetRelativePath(baseFullPath, fullPath).Replace('\\', '/');

            return Path.GetFileName(fullPath);
        }

        normalized = normalized.TrimStart('/');
        if (normalized.Contains("..", StringComparison.Ordinal))
            return Path.GetFileName(normalized);

        return normalized;
    }

    [GeneratedRegex(@"(?i)(sk-[a-z0-9_\-]{8,}|api[_-]?key\s*[:=]|secret\s*[:=]|password\s*[:=]|bearer\s+[a-z0-9_\.\-]{12,}|token\s*[:=])")]
    private static partial Regex SecretLikeRegex();

    [GeneratedRegex(@"(?i)[a-z]:[/\\]users[/\\][^/\\\s]+")]
    private static partial Regex LocalUserPathRegex();
}
