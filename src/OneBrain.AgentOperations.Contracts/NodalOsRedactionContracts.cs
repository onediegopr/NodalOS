namespace OneBrain.BrowserExecutor.Contracts;

public sealed record NodalOsRedactionResult
{
    public required string Value { get; init; }

    public required bool WasRedacted { get; init; }

    public IReadOnlyList<NodalOsRedactionMatch> Matches { get; init; } = [];
}

public sealed record NodalOsRedactionMatch
{
    public required NodalOsSensitiveContentKind Kind { get; init; }

    public required string Reason { get; init; }

    public string? FieldName { get; init; }
}

public enum NodalOsSensitiveContentKind
{
    Cookie,
    SetCookie,
    AuthorizationHeader,
    BearerToken,
    BasicAuth,
    Password,
    Secret,
    ApiKey,
    AccessToken,
    RefreshToken,
    IdToken,
    JwtLikeToken,
    PrivateBody,
    GenericToken,
    UnknownSensitive
}

public sealed record NodalOsStructuredRedactionResult
{
    public required bool WasRedacted { get; init; }

    public IReadOnlyDictionary<string, string> Values { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<NodalOsRedactionMatch> Matches { get; init; } = [];
}

public sealed record NodalOsRedactionOptions
{
    public string RedactedPlaceholder { get; init; } = "[REDACTED]";

    public bool RedactWholeValue { get; init; } = true;

    public bool PreserveFieldNames { get; init; } = true;

    public bool DetectJwtLikeTokens { get; init; } = true;

    public bool DetectBearerTokens { get; init; } = true;

    public bool DetectBasicAuth { get; init; } = true;
}
