namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaLocalProductShellRouteKind
{
    Dashboard,
    Accounts,
    Organization,
    Workers,
    Licenses,
    Features,
    Usage,
    Diagnostics,
    Support,
    Audit,
    Release,
    Configuration,
    Readiness
}

public enum NexaLocalProductShellActionKind
{
    ViewDashboard,
    ViewLicense,
    ViewFeatureFlags,
    RequestMockOnboarding,
    ViewDiagnostics,
    GenerateMockDiagnosticsBundle,
    ViewAuditExportSummary,
    RequestAuditExportMock,
    ViewReleaseStatus,
    ViewConfigurationProfile,
    EnableSensitiveRealPilot,
    EnableRecorderProductive,
    EnableReplayProductive,
    EnableProductiveVault,
    EnableRealBilling,
    EnableRealEmail,
    EnablePublicSaasActivation,
    ExecuteRealDeployOrUpdate
}

public sealed record NexaLocalProductShellRoute(
    NexaLocalProductShellRouteKind Kind,
    string Path,
    string Label);

public sealed record NexaLocalProductShellNavigation(IReadOnlyList<NexaLocalProductShellRoute> Routes)
{
    public static NexaLocalProductShellNavigation Default() =>
        new(
            [
                new(NexaLocalProductShellRouteKind.Dashboard, "/dashboard", "Dashboard"),
                new(NexaLocalProductShellRouteKind.Accounts, "/accounts", "Accounts"),
                new(NexaLocalProductShellRouteKind.Organization, "/organization", "Organization"),
                new(NexaLocalProductShellRouteKind.Workers, "/workers", "Workers"),
                new(NexaLocalProductShellRouteKind.Licenses, "/licenses", "Licenses"),
                new(NexaLocalProductShellRouteKind.Features, "/features", "Features"),
                new(NexaLocalProductShellRouteKind.Usage, "/usage", "Usage"),
                new(NexaLocalProductShellRouteKind.Diagnostics, "/diagnostics", "Diagnostics"),
                new(NexaLocalProductShellRouteKind.Support, "/support", "Support"),
                new(NexaLocalProductShellRouteKind.Audit, "/audit", "Audit"),
                new(NexaLocalProductShellRouteKind.Release, "/release", "Release"),
                new(NexaLocalProductShellRouteKind.Configuration, "/configuration", "Configuration"),
                new(NexaLocalProductShellRouteKind.Readiness, "/readiness", "Readiness")
            ]);
}

public sealed record NexaLocalProductShellPage(
    string PageId,
    NexaLocalProductShellRouteKind Route,
    string Title,
    bool Redacted,
    bool TenantScoped,
    bool RoleFiltered,
    bool LicenseAware,
    bool FeatureAware,
    IReadOnlyDictionary<string, string> Summary);

public sealed record NexaLocalProductShellRenderModel(
    NexaLocalProductShellNavigation Navigation,
    IReadOnlyList<NexaLocalProductShellPage> Pages,
    bool Redacted,
    bool TenantScoped,
    bool RoleFiltered,
    bool LicenseAware,
    bool FeatureAware,
    bool ContainsSecretsCookiesBodies);

public sealed record NexaLocalProductShell(
    string ShellId,
    NexaLocalProductShellRenderModel RenderModel,
    bool LocalOnly,
    bool PublicSaasExposed,
    bool NetworkListenerEnabled)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(ShellId, nameof(ShellId), errors);
        if (!LocalOnly)
            errors.Add("Local product shell must be local-only.");
        if (PublicSaasExposed)
            errors.Add("Local product shell cannot expose public SaaS.");
        if (NetworkListenerEnabled)
            errors.Add("Local product shell cannot enable a public network listener.");
        if (!RenderModel.Redacted || RenderModel.ContainsSecretsCookiesBodies)
            errors.Add("Local product shell render model must be redacted and secret-free.");
        if (!RenderModel.TenantScoped || !RenderModel.RoleFiltered || !RenderModel.LicenseAware || !RenderModel.FeatureAware)
            errors.Add("Local product shell must be tenant, role, license, and feature aware.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaLocalProductShellAction(
    NexaLocalProductShellActionKind Kind,
    NexaRole ActorRole,
    NexaPlanKind Plan,
    bool ProductiveVaultEntitlement,
    bool AdminOverride,
    bool GatePassed);

public sealed record NexaLocalProductShellDecision(
    bool Allowed,
    string Reason,
    bool Redacted);

public sealed record NexaAdminDashboardPageModel(string AccountSummary, string LicenseSummary, string FeatureSummary, bool Redacted);
public sealed record NexaAccountAdminPageModel(string AccountId, string AccountKind, bool Redacted);
public sealed record NexaWorkerAdminPageModel(string WorkspaceId, int WorkerCount, bool RoleFiltered, bool Redacted);
public sealed record NexaLicenseAdminPageModel(NexaPlanKind Plan, NexaLicenseStatus Status, IReadOnlyList<string> BlockedFeatures, bool Redacted);
public sealed record NexaFeatureFlagAdminPageModel(IReadOnlySet<NexaFeatureFlag> Enabled, IReadOnlySet<NexaFeatureFlag> Disabled, bool Redacted);
public sealed record NexaUsageAdminPageModel(IReadOnlyList<NexaUsageLimit> Limits, bool Redacted);
public sealed record NexaDiagnosticsPageModel(string HealthSummary, bool RedactionActive, bool Redacted);
public sealed record NexaSupportModePageModel(bool MetadataOnly, bool NoSecretAccess, bool Redacted);
public sealed record NexaAuditExportPageModel(string Scope, int EventCount, bool ManifestAvailable, bool Redacted);
public sealed record NexaReleaseStatusPageModel(NexaReleaseChannelKind Channel, string Version, bool AutoUpdateDisabled, bool Redacted);
public sealed record NexaConfigurationProfilePageModel(NexaConfigurationProfileKind Profile, bool ProductionLockedFailClosed, bool Redacted);
