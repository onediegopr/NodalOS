using System.Text.RegularExpressions;

namespace OneBrain.BrowserPerception;

public sealed record BrowserEvidenceRedactionResult(
    string Value,
    IReadOnlyList<string> SensitiveFieldsRedacted,
    BrowserEvidenceRedactionStatus Status);

public sealed class BrowserEvidenceRedactor
{
    public const string RedactedValue = "[REDACTED]";

    private static readonly string[] SensitiveFieldNames =
    [
        "password",
        "passwd",
        "pwd",
        "token",
        "api_key",
        "apikey",
        "api key",
        "secret",
        "client_secret",
        "bearer",
        "authorization",
        "credential",
        "otp",
        "2fa",
        "mfa",
        "captcha",
        "session",
        "cookie",
        "refresh_token",
        "access_token"
    ];

    private static readonly Regex[] SecretPatterns =
    [
        new(@"Bearer\s+[A-Za-z0-9._\-~+/]+=*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"sk-[A-Za-z0-9]{12,}", RegexOptions.Compiled),
        new(@"ghp_[A-Za-z0-9]{12,}", RegexOptions.Compiled),
        new(@"\beyJ[A-Za-z0-9_\-]+?\.[A-Za-z0-9_\-]+?\.[A-Za-z0-9_\-]+?\b", RegexOptions.Compiled),
        new(@"(?i)\b(api[_ -]?key|access[_ -]?token|refresh[_ -]?token|client[_ -]?secret|password|secret|authorization|cookie|session)\s*[:=]\s*[^;,\s]+", RegexOptions.Compiled),
        new(@"\b[A-Za-z0-9_\-]{32,}\b", RegexOptions.Compiled)
    ];

    public BrowserEvidenceRedactionResult RedactField(string fieldName, string? value)
    {
        var redactedFields = new List<string>();
        var safeValue = value ?? "";
        var sensitiveField = IsSensitiveFieldName(fieldName);
        if (sensitiveField)
        {
            redactedFields.Add(NormalizeFieldName(fieldName));
            return new BrowserEvidenceRedactionResult(RedactedValue, redactedFields, BrowserEvidenceRedactionStatus.Partial);
        }

        var redactedValue = RedactText(safeValue, redactedFields);
        return new BrowserEvidenceRedactionResult(
            redactedValue,
            redactedFields.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            redactedFields.Count > 0 ? BrowserEvidenceRedactionStatus.Partial : BrowserEvidenceRedactionStatus.None);
    }

    public BrowserEvidenceRedactionResult RedactSummary(string? summary)
    {
        var redactedFields = new List<string>();
        var redactedValue = RedactText(summary ?? "", redactedFields);
        return new BrowserEvidenceRedactionResult(
            redactedValue,
            redactedFields.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            redactedFields.Count > 0 ? BrowserEvidenceRedactionStatus.Partial : BrowserEvidenceRedactionStatus.None);
    }

    public BrowserEvidenceRedactionStatus MergeStatuses(params BrowserEvidenceRedactionStatus[] statuses)
    {
        if (statuses.Any(status => status == BrowserEvidenceRedactionStatus.Full))
            return BrowserEvidenceRedactionStatus.Full;
        return statuses.Any(status => status == BrowserEvidenceRedactionStatus.Partial)
            ? BrowserEvidenceRedactionStatus.Partial
            : BrowserEvidenceRedactionStatus.None;
    }

    public bool IsSensitiveFieldName(string fieldName) =>
        !string.IsNullOrWhiteSpace(fieldName)
        && SensitiveFieldNames.Any(sensitive => fieldName.Contains(sensitive, StringComparison.OrdinalIgnoreCase));

    private static string RedactText(string value, ICollection<string> redactedFields)
    {
        var redacted = value;
        foreach (var pattern in SecretPatterns)
        {
            if (!pattern.IsMatch(redacted))
                continue;

            redacted = pattern.Replace(redacted, RedactedValue);
            redactedFields.Add("secret-pattern");
        }

        return redacted;
    }

    private static string NormalizeFieldName(string fieldName) =>
        string.IsNullOrWhiteSpace(fieldName) ? "sensitive-field" : fieldName.Trim();
}
