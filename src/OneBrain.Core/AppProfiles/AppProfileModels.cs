namespace OneBrain.Core.AppProfiles;

public static class AppProfileKinds
{
    public const string DesktopApp = "desktop_app";
    public const string BrowserSite = "browser_site";
    public const string WebApp = "web_app";
    public const string Fixture = "fixture";
    public const string Unknown = "unknown";
}

public static class AppProfileStatuses
{
    public const string Draft = "draft";
    public const string Active = "active";
    public const string Deprecated = "deprecated";
    public const string Blocked = "blocked";
    public const string ExternalFragile = "external_fragile";
}

public static class AppProfileCapabilities
{
    public const string ReadOnly = "read_only";
    public const string SafeClick = "safe_click";
    public const string TextInput = "text_input";
    public const string FileAttach = "file_attach";
    public const string SubmitRequiresApproval = "submit_requires_approval";
    public const string ExternalFragile = "external_fragile";
    public const string DiagnosticAllowed = "diagnostic_allowed";
    public const string Login = "login";
    public const string Payment = "payment";
    public const string Purchase = "purchase";
    public const string AcceptCookies = "accept_cookies";
}

public sealed record AppProfileSelectorAlias(
    string Alias,
    string Strategy,
    string Value,
    string Notes);

public sealed record AppProfileVersion(
    int Version,
    string CreatedAtUtc,
    string ChangeSummary,
    string Status);

public sealed record AppProfileRiskPolicy(
    bool ReadOnlyByDefault,
    bool DiagnosticAllowed,
    bool RequiresApprovalForSubmit,
    bool BlocksLogin,
    bool BlocksCookies,
    bool BlocksPayment,
    bool BlocksPurchase,
    bool AllowsSafeClick);

public sealed record AppProfile(
    string Id,
    string Name,
    string Kind,
    string Status,
    string? AppName,
    string? ProcessName,
    string? SiteDomain,
    IReadOnlyList<string> SupportedCapabilities,
    AppProfileRiskPolicy RiskPolicy,
    IReadOnlyList<AppProfileSelectorAlias> SelectorAliases,
    string LastVerifiedAtUtc,
    AppProfileVersion Version,
    IReadOnlyList<string> Notes);

public sealed record AppProfileValidationIssue(
    string Severity,
    string Code,
    string Message,
    string Remediation);

public sealed record AppProfileValidationResult(
    bool CanActivate,
    bool RequiresValidationBeforePromotion,
    IReadOnlyList<AppProfileValidationIssue> Issues);

public sealed record AppProfileArtifactWriteResult
{
    public bool Success { get; init; }
    public string Path { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public string Error { get; init; } = "";
}
