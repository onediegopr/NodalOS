namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaPlanKind
{
    Free,
    Trial,
    Pro,
    Enterprise
}

public enum NexaLicenseStatus
{
    Active,
    Suspended,
    Expired,
    Revoked
}

public enum NexaFeatureFlag
{
    BrowserRuntime,
    CdpLiveReadOnly,
    SafeDownload,
    SafeUpload,
    RecorderReadOnly,
    ReplaySafeMode,
    DocumentWorkflowSandbox,
    SensitiveSimulation,
    ExternalLowRiskAuth,
    SensitiveRealPilot,
    ProductiveVault,
    ProfileControlled,
    AdminConsole,
    RecorderProductive,
    ReplayProductive
}

public enum NexaLicenseDecisionKind
{
    Allowed,
    Denied,
    RequiresAdminOverride,
    FailClosed
}

public sealed record NexaUsageLimit(
    string LimitId,
    int MaxCount,
    TimeSpan Window)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(LimitId, nameof(LimitId), errors);
        if (MaxCount <= 0)
            errors.Add("Usage limit max count must be positive.");
        if (Window <= TimeSpan.Zero)
            errors.Add("Usage limit window must be positive.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaUsageCounter(
    string LimitId,
    int Count,
    DateTimeOffset WindowStartedAtUtc);

public sealed record NexaLicenseEntitlement(
    NexaFeatureFlag Feature,
    bool Enabled,
    NexaUsageLimit? Limit);

public sealed record NexaPlan(
    NexaPlanKind Kind,
    IReadOnlySet<NexaFeatureFlag> EnabledFeatures,
    IReadOnlyList<NexaUsageLimit> Limits,
    TimeSpan? DefaultDuration,
    int MaxWorkers)
{
    public static NexaPlan Free() =>
        new(
            NexaPlanKind.Free,
            Set(NexaFeatureFlag.BrowserRuntime, NexaFeatureFlag.CdpLiveReadOnly, NexaFeatureFlag.AdminConsole),
            [new NexaUsageLimit("limit-free-browser-runs", 20, TimeSpan.FromDays(7))],
            TimeSpan.FromDays(7),
            MaxWorkers: 1);

    public static NexaPlan Trial(IReadOnlySet<NexaFeatureFlag>? features = null, IReadOnlyList<NexaUsageLimit>? limits = null, TimeSpan? duration = null) =>
        new(
            NexaPlanKind.Trial,
            features ?? Set(NexaFeatureFlag.BrowserRuntime, NexaFeatureFlag.CdpLiveReadOnly, NexaFeatureFlag.SafeDownload, NexaFeatureFlag.SafeUpload, NexaFeatureFlag.RecorderReadOnly, NexaFeatureFlag.ReplaySafeMode, NexaFeatureFlag.DocumentWorkflowSandbox, NexaFeatureFlag.SensitiveSimulation, NexaFeatureFlag.ProfileControlled, NexaFeatureFlag.AdminConsole),
            limits ?? [new NexaUsageLimit("limit-trial-browser-runs", 200, TimeSpan.FromDays(30))],
            duration ?? TimeSpan.FromDays(14),
            MaxWorkers: 3);

    public static NexaPlan Pro() =>
        new(
            NexaPlanKind.Pro,
            Set(NexaFeatureFlag.BrowserRuntime, NexaFeatureFlag.CdpLiveReadOnly, NexaFeatureFlag.SafeDownload, NexaFeatureFlag.SafeUpload, NexaFeatureFlag.RecorderReadOnly, NexaFeatureFlag.ReplaySafeMode, NexaFeatureFlag.DocumentWorkflowSandbox, NexaFeatureFlag.ExternalLowRiskAuth, NexaFeatureFlag.ProfileControlled, NexaFeatureFlag.AdminConsole),
            [new NexaUsageLimit("limit-pro-browser-runs", 2000, TimeSpan.FromDays(30))],
            null,
            MaxWorkers: 10);

    public static NexaPlan Enterprise(IReadOnlySet<NexaFeatureFlag>? features = null, IReadOnlyList<NexaUsageLimit>? limits = null, int maxWorkers = 100) =>
        new(
            NexaPlanKind.Enterprise,
            features ?? Set(NexaFeatureFlag.BrowserRuntime, NexaFeatureFlag.CdpLiveReadOnly, NexaFeatureFlag.SafeDownload, NexaFeatureFlag.SafeUpload, NexaFeatureFlag.RecorderReadOnly, NexaFeatureFlag.ReplaySafeMode, NexaFeatureFlag.DocumentWorkflowSandbox, NexaFeatureFlag.ExternalLowRiskAuth, NexaFeatureFlag.SensitiveSimulation, NexaFeatureFlag.ProfileControlled, NexaFeatureFlag.AdminConsole),
            limits ?? [new NexaUsageLimit("limit-enterprise-browser-runs", 10000, TimeSpan.FromDays(30))],
            null,
            maxWorkers);

    public bool Enables(NexaFeatureFlag feature) =>
        EnabledFeatures.Contains(feature);

    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (MaxWorkers <= 0)
            errors.Add("Plan max workers must be positive.");
        if (EnabledFeatures.Contains(NexaFeatureFlag.SensitiveRealPilot))
            errors.Add("SensitiveRealPilot is disabled by default and requires explicit compliance policy.");
        if (EnabledFeatures.Contains(NexaFeatureFlag.ProductiveVault))
            errors.Add("ProductiveVault is disabled by default and requires OS-backed vault approval.");
        if (EnabledFeatures.Contains(NexaFeatureFlag.RecorderProductive) || EnabledFeatures.Contains(NexaFeatureFlag.ReplayProductive))
            errors.Add("Productive recorder/replay are disabled by default.");
        foreach (var limit in Limits)
            errors.AddRange(limit.Validate().Errors);
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }

    private static IReadOnlySet<NexaFeatureFlag> Set(params NexaFeatureFlag[] values) =>
        new HashSet<NexaFeatureFlag>(values);
}

public sealed record NexaLicense(
    string LicenseId,
    string AccountId,
    NexaPlan Plan,
    NexaLicenseStatus Status,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyList<NexaLicenseEntitlement> Entitlements,
    bool ManualAdminOverride)
{
    public bool IsActive(DateTimeOffset now) =>
        Status == NexaLicenseStatus.Active && ExpiresAtUtc > now;

    public ContractValidationResult Validate(DateTimeOffset now)
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(LicenseId, nameof(LicenseId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(AccountId, nameof(AccountId), errors);
        errors.AddRange(Plan.Validate().Errors);
        if (IssuedAtUtc >= ExpiresAtUtc)
            errors.Add("License expiration must be after issue time.");
        if (Status == NexaLicenseStatus.Active && ExpiresAtUtc <= now)
            errors.Add("Active license is expired.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaFreeLicenseRequest(
    string Email,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? PreviousFreeIssuedAtUtc,
    TimeSpan OneFreePerEmailWindow)
{
    public bool CanGenerate =>
        !string.IsNullOrWhiteSpace(Email) &&
        !BrowserCredentialRedactor.ContainsSecret(Email) &&
        (PreviousFreeIssuedAtUtc is null || RequestedAtUtc - PreviousFreeIssuedAtUtc.Value > OneFreePerEmailWindow);

    public NexaLicense Generate(string accountId) =>
        new(
            $"license-free-{Guid.NewGuid():N}",
            accountId,
            NexaPlan.Free(),
            NexaLicenseStatus.Active,
            RequestedAtUtc,
            RequestedAtUtc.AddDays(7),
            [],
            ManualAdminOverride: false);
}

public sealed record NexaLicensePolicyRequest(
    NexaProductAccount Account,
    NexaLicense License,
    NexaFeatureFlag Feature,
    NexaWorker? Worker,
    NexaUsageCounter? UsageCounter,
    DateTimeOffset NowUtc,
    bool SensitiveCompliancePolicyApproved);

public sealed record NexaLicenseAuditEvent(
    string EventId,
    string AccountId,
    string LicenseId,
    NexaPlanKind Plan,
    NexaFeatureFlag Feature,
    NexaLicenseDecisionKind Decision,
    string Reason,
    DateTimeOffset TimestampUtc,
    bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(EventId, nameof(EventId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(AccountId, nameof(AccountId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(LicenseId, nameof(LicenseId), errors);
        if (!Redacted)
            errors.Add("License audit must be redacted.");
        if (BrowserCredentialRedactor.ContainsSecret(Reason))
            errors.Add("License audit contains secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaLicensePolicyDecision(
    NexaLicenseDecisionKind Decision,
    NexaFeatureFlag Feature,
    string Reason,
    NexaLicenseAuditEvent AuditEvent)
{
    public bool Allowed => Decision == NexaLicenseDecisionKind.Allowed && AuditEvent.Validate().IsValid;
}
