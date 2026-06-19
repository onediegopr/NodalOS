using System.Text.RegularExpressions;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsSensitiveContentClassifier
{
    private static readonly Regex BearerRegex = new(
        @"\bBearer\s+[A-Za-z0-9._~+/=-]{8,}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex BasicAuthRegex = new(
        @"\bBasic\s+[A-Za-z0-9+/=]{8,}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex JwtLikeRegex = new(
        @"\b[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\b",
        RegexOptions.Compiled);

    private static readonly Regex SensitiveAssignmentRegex = new(
        @"(?i)(^|[?&;\s])(access_token|refresh_token|id_token|api_key|apikey|password|passwd|pass|secret|token)\s*[:=]\s*[^&;\s]+",
        RegexOptions.Compiled);

    private static readonly Regex PrivateKeyRegex = new(
        @"(?is)-----BEGIN\s+(RSA\s+|EC\s+|OPENSSH\s+)?PRIVATE\s+KEY-----.*?-----END\s+(RSA\s+|EC\s+|OPENSSH\s+)?PRIVATE\s+KEY-----|\bprivate\s+key\b",
        RegexOptions.Compiled);

    private static readonly Regex ConnectionStringRegex = new(
        @"(?i)\b(server|data source|host)\s*=\s*[^;]+;.*\b(database|initial catalog|user id|uid|password|pwd)\s*=",
        RegexOptions.Compiled);

    private static readonly Regex EmailRegex = new(
        @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex AuthorizationWordRegex = new(
        @"\bauthorization\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex BearerWordRegex = new(
        @"\bbearer\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PasswordWordRegex = new(
        @"\b(password|passwd)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SecretWordRegex = new(
        @"\bsecret\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TokenWordRegex = new(
        @"\btoken\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SetCookieHeaderRegex = new(
        @"(?i)(^|\s)set-cookie\s*:",
        RegexOptions.Compiled);

    private static readonly Regex CookieHeaderRegex = new(
        @"(?i)(^|\s)cookie\s*:",
        RegexOptions.Compiled);

    public IReadOnlyList<NodalOsRedactionMatch> ClassifyField(string fieldName, string? value)
    {
        var matches = new List<NodalOsRedactionMatch>();
        var kind = ClassifyFieldName(fieldName);
        if (kind is not null)
        {
            matches.Add(new NodalOsRedactionMatch
            {
                Kind = kind.Value,
                FieldName = fieldName,
                Reason = $"Field name classified as {kind.Value}."
            });
        }

        matches.AddRange(ClassifyText(value));
        return Deduplicate(matches);
    }

    public IReadOnlyList<NodalOsRedactionMatch> ClassifyText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        var matches = new List<NodalOsRedactionMatch>();
        var trimmedLower = value.Trim().ToLowerInvariant();

        switch (trimmedLower)
        {
            case "access_token":
                matches.Add(Match(NodalOsSensitiveContentKind.AccessToken, "Access token marker detected."));
                break;
            case "refresh_token":
                matches.Add(Match(NodalOsSensitiveContentKind.RefreshToken, "Refresh token marker detected."));
                break;
            case "id_token":
                matches.Add(Match(NodalOsSensitiveContentKind.IdToken, "ID token marker detected."));
                break;
            case "api_key":
            case "apikey":
                matches.Add(Match(NodalOsSensitiveContentKind.ApiKey, "API key marker detected."));
                break;
            case "private key":
            case "private_key":
                matches.Add(Match(NodalOsSensitiveContentKind.PrivateKey, "Private key marker detected."));
                break;
            case "connection string":
            case "connection_string":
                matches.Add(Match(NodalOsSensitiveContentKind.ConnectionString, "Connection string marker detected."));
                break;
            case "private body":
            case "private_body":
            case "body_private":
                matches.Add(Match(NodalOsSensitiveContentKind.PrivateBody, "Private body marker detected."));
                break;
        }

        if (BearerRegex.IsMatch(value))
            matches.Add(Match(NodalOsSensitiveContentKind.BearerToken, "Bearer token pattern detected."));

        if (BasicAuthRegex.IsMatch(value))
            matches.Add(Match(NodalOsSensitiveContentKind.BasicAuth, "Basic auth pattern detected."));

        if (JwtLikeRegex.IsMatch(value))
            matches.Add(Match(NodalOsSensitiveContentKind.JwtLikeToken, "JWT-like token pattern detected."));

        if (SensitiveAssignmentRegex.IsMatch(value))
            matches.Add(Match(NodalOsSensitiveContentKind.GenericToken, "Sensitive key/value assignment detected."));

        if (PrivateKeyRegex.IsMatch(value))
            matches.Add(Match(NodalOsSensitiveContentKind.PrivateKey, "Private key pattern detected."));

        if (ConnectionStringRegex.IsMatch(value))
            matches.Add(Match(NodalOsSensitiveContentKind.ConnectionString, "Connection string pattern detected."));

        if (EmailRegex.IsMatch(value))
            matches.Add(Match(NodalOsSensitiveContentKind.EmailAddress, "Email address pattern detected."));

        if (SetCookieHeaderRegex.IsMatch(value))
            matches.Add(Match(NodalOsSensitiveContentKind.SetCookie, "Set-Cookie header pattern detected."));

        if (CookieHeaderRegex.IsMatch(value))
            matches.Add(Match(NodalOsSensitiveContentKind.Cookie, "Cookie header pattern detected."));

        if (AuthorizationWordRegex.IsMatch(value))
            matches.Add(Match(NodalOsSensitiveContentKind.AuthorizationHeader, "Authorization marker detected."));

        if (BearerWordRegex.IsMatch(value))
            matches.Add(Match(NodalOsSensitiveContentKind.BearerToken, "Bearer marker detected."));

        if (PasswordWordRegex.IsMatch(value))
            matches.Add(Match(NodalOsSensitiveContentKind.Password, "Password marker detected."));

        if (SecretWordRegex.IsMatch(value))
            matches.Add(Match(NodalOsSensitiveContentKind.Secret, "Secret marker detected."));

        if (TokenWordRegex.IsMatch(value))
            matches.Add(Match(NodalOsSensitiveContentKind.GenericToken, "Token marker detected."));

        return Deduplicate(matches);
    }

    public bool ContainsSensitiveContent(string? value) => ClassifyText(value).Count > 0;

    private static NodalOsSensitiveContentKind? ClassifyFieldName(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            return null;

        var lower = fieldName.Trim().ToLowerInvariant();
        var compact = Regex.Replace(lower, @"[\s_\-]", string.Empty);

        return lower switch
        {
            "cookie" => NodalOsSensitiveContentKind.Cookie,
            "set-cookie" => NodalOsSensitiveContentKind.SetCookie,
            "authorization" => NodalOsSensitiveContentKind.AuthorizationHeader,
            "password" or "passwd" or "pass" => NodalOsSensitiveContentKind.Password,
            "secret" => NodalOsSensitiveContentKind.Secret,
            "api_key" => NodalOsSensitiveContentKind.ApiKey,
            "email" => NodalOsSensitiveContentKind.EmailAddress,
            "e-mail" => NodalOsSensitiveContentKind.EmailAddress,
            "private_key" => NodalOsSensitiveContentKind.PrivateKey,
            "connection_string" => NodalOsSensitiveContentKind.ConnectionString,
            "access_token" => NodalOsSensitiveContentKind.AccessToken,
            "refresh_token" => NodalOsSensitiveContentKind.RefreshToken,
            "id_token" => NodalOsSensitiveContentKind.IdToken,
            "token" => NodalOsSensitiveContentKind.GenericToken,
            "private_body" or "body_private" => NodalOsSensitiveContentKind.PrivateBody,
            _ => compact switch
            {
                "setcookie" => NodalOsSensitiveContentKind.SetCookie,
                "apikey" => NodalOsSensitiveContentKind.ApiKey,
                "email" => NodalOsSensitiveContentKind.EmailAddress,
                "emailaddress" => NodalOsSensitiveContentKind.EmailAddress,
                "privatekey" => NodalOsSensitiveContentKind.PrivateKey,
                "connectionstring" => NodalOsSensitiveContentKind.ConnectionString,
                "accesstoken" => NodalOsSensitiveContentKind.AccessToken,
                "refreshtoken" => NodalOsSensitiveContentKind.RefreshToken,
                "idtoken" => NodalOsSensitiveContentKind.IdToken,
                "privatebody" or "bodyprivate" => NodalOsSensitiveContentKind.PrivateBody,
                _ => null
            }
        };
    }

    private static NodalOsRedactionMatch Match(NodalOsSensitiveContentKind kind, string reason) =>
        new()
        {
            Kind = kind,
            Reason = reason
        };

    private static IReadOnlyList<NodalOsRedactionMatch> Deduplicate(IEnumerable<NodalOsRedactionMatch> matches) =>
        matches
            .GroupBy(match => (match.Kind, match.FieldName))
            .Select(group => group.First())
            .ToArray();
}

public sealed class NodalOsRedactionService
{
    private readonly NodalOsSensitiveContentClassifier classifier;

    public NodalOsRedactionService()
        : this(new NodalOsSensitiveContentClassifier())
    {
    }

    public NodalOsRedactionService(NodalOsSensitiveContentClassifier classifier) =>
        this.classifier = classifier;

    public NodalOsRedactionResult RedactValue(string? value, NodalOsRedactionOptions? options = null)
    {
        var effectiveOptions = options ?? new NodalOsRedactionOptions();
        var safeValue = value ?? string.Empty;
        var matches = classifier.ClassifyText(safeValue);

        if (matches.Count == 0)
        {
            return new NodalOsRedactionResult
            {
                Value = safeValue,
                WasRedacted = false,
                Matches = []
            };
        }

        return new NodalOsRedactionResult
        {
            Value = effectiveOptions.RedactWholeValue
                ? effectiveOptions.RedactedPlaceholder
                : RedactPatterns(safeValue, effectiveOptions.RedactedPlaceholder),
            WasRedacted = true,
            Matches = matches
        };
    }

    public NodalOsRedactionResult RedactField(
        string fieldName,
        string? value,
        NodalOsRedactionOptions? options = null)
    {
        var effectiveOptions = options ?? new NodalOsRedactionOptions();
        var safeValue = value ?? string.Empty;
        var matches = classifier.ClassifyField(fieldName, safeValue);

        if (matches.Count == 0)
        {
            return new NodalOsRedactionResult
            {
                Value = safeValue,
                WasRedacted = false,
                Matches = []
            };
        }

        return new NodalOsRedactionResult
        {
            Value = effectiveOptions.RedactWholeValue
                ? effectiveOptions.RedactedPlaceholder
                : RedactPatterns(safeValue, effectiveOptions.RedactedPlaceholder),
            WasRedacted = true,
            Matches = matches
        };
    }

    public NodalOsStructuredRedactionResult RedactDictionary(
        IReadOnlyDictionary<string, string> values,
        NodalOsRedactionOptions? options = null)
    {
        var redactedValues = new Dictionary<string, string>(StringComparer.Ordinal);
        var matches = new List<NodalOsRedactionMatch>();

        foreach (var (key, value) in values)
        {
            var result = RedactField(key, value, options);
            redactedValues[key] = result.Value;
            matches.AddRange(result.Matches);
        }

        return new NodalOsStructuredRedactionResult
        {
            WasRedacted = matches.Count > 0,
            Values = redactedValues,
            Matches = matches
        };
    }

    public bool ContainsSensitiveContent(string? value) =>
        classifier.ContainsSensitiveContent(value);

    public bool ContainsSensitiveField(string fieldName, string? value) =>
        classifier.ClassifyField(fieldName, value).Count > 0;

    private static string RedactPatterns(string value, string placeholder)
    {
        var redacted = Regex.Replace(
            value,
            @"(?i)\b(Bearer|Basic)\s+[A-Za-z0-9._~+/=-]{8,}",
            placeholder);
        redacted = Regex.Replace(
            redacted,
            @"\b[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\b",
            placeholder);
        redacted = Regex.Replace(
            redacted,
            @"(?i)(access_token|refresh_token|id_token|api_key|apikey|password|passwd|pass|secret|token)\s*[:=]\s*[^&;\s]+",
            "$1=" + placeholder);
        redacted = Regex.Replace(
            redacted,
            @"(?is)-----BEGIN\s+(RSA\s+|EC\s+|OPENSSH\s+)?PRIVATE\s+KEY-----.*?-----END\s+(RSA\s+|EC\s+|OPENSSH\s+)?PRIVATE\s+KEY-----|\bprivate\s+key\b",
            placeholder);
        redacted = Regex.Replace(
            redacted,
            @"(?i)\b(server|data source|host)\s*=\s*[^;]+;.*\b(database|initial catalog|user id|uid|password|pwd)\s*=\s*[^;]+",
            placeholder);
        redacted = Regex.Replace(
            redacted,
            @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
            placeholder,
            RegexOptions.IgnoreCase);
        redacted = Regex.Replace(
            redacted,
            @"(?i)(set-cookie|cookie|authorization)\s*:\s*[^\r\n;]+",
            "$1: " + placeholder);

        return redacted;
    }
}
