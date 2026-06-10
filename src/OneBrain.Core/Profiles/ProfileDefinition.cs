namespace OneBrain.Core.Profiles;

public sealed record ProfileDefinition
{
    public string Id { get; init; } = "";
    public string Type { get; init; } = ""; // web, app
    public string? DisplayName { get; init; }
    public string? Url { get; init; }
    public string? Process { get; init; }
    public ProfileExpected? Expected { get; init; }
    public ProfileRead? Read { get; init; }
    public ProfileSafety? Safety { get; init; }
    public ProfileKnownInput? KnownInput { get; init; }
}

public sealed record ProfileExpected
{
    public string? TitleContains { get; init; }
    public string? TextContains { get; init; }
}

public sealed record ProfileRead
{
    public string? PreferredProperty { get; init; }
    public string? FallbackProperty { get; init; }
}

public sealed record ProfileSafety
{
    public bool AllowForms { get; init; }
    public bool AllowLogin { get; init; }
    public bool AllowPurchase { get; init; }
    public bool AllowSensitiveActions { get; init; }
    public bool AllowClose { get; init; } = true;
    public bool AllowDelete { get; init; }
}

public sealed record ProfileKnownInput
{
    public string? Role { get; init; }
    public string? Name { get; init; }
}
