namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaReleaseChannelKind
{
    Dev,
    Internal,
    Preview,
    Beta,
    Stable,
    EnterprisePinned,
    Disabled
}

public enum NexaUpdateEligibilityDecisionKind
{
    Eligible,
    Denied,
    RequiresApproval,
    FailClosed
}

public sealed record NexaReleaseVersion(int Major, int Minor, int Patch, string Label)
{
    public override string ToString() => $"{Major}.{Minor}.{Patch}{(string.IsNullOrWhiteSpace(Label) ? "" : $"-{Label}")}";
}

public sealed record NexaReleaseChannel(NexaReleaseChannelKind Kind, bool Enabled, bool RequiresAdminApproval, bool TenantPolicyRequired)
{
    public static IReadOnlyList<NexaReleaseChannel> Defaults() =>
    [
        new(NexaReleaseChannelKind.Dev, true, false, false),
        new(NexaReleaseChannelKind.Internal, true, true, false),
        new(NexaReleaseChannelKind.Preview, true, true, true),
        new(NexaReleaseChannelKind.Beta, true, true, true),
        new(NexaReleaseChannelKind.Stable, true, true, true),
        new(NexaReleaseChannelKind.EnterprisePinned, true, true, true),
        new(NexaReleaseChannelKind.Disabled, false, true, true)
    ];
}

public sealed record NexaReleaseComponent(string ComponentId, string Version, string Hash)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(ComponentId, nameof(ComponentId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(Hash, nameof(Hash), errors);
        if (string.IsNullOrWhiteSpace(Version))
            errors.Add("Release component version is required.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaReleaseCompatibility(string RequiredRuntime, string RequiredOs, string RequiredBrowser, bool RuntimeCompatible);

public sealed record NexaUpdateIntegrityDescriptor(string Hash, string SignatureMetadata, bool SignatureRequired)
{
    public bool Valid => !string.IsNullOrWhiteSpace(Hash) && (!SignatureRequired || !string.IsNullOrWhiteSpace(SignatureMetadata));
}

public sealed record NexaUpdatePackageDescriptor(string PackageId, NexaReleaseVersion Version, IReadOnlyList<NexaReleaseComponent> Components);

public sealed record NexaUpdateCompatibilityCheck(NexaReleaseCompatibility Compatibility, bool Passed);

public sealed record NexaUpdateRollbackPlan(NexaReleaseVersion TargetVersion, bool ExecuteAutomatically, string Reason);

public sealed record NexaUpdateManifest(
    string ManifestId,
    NexaReleaseChannelKind Channel,
    NexaUpdatePackageDescriptor Package,
    NexaUpdateIntegrityDescriptor Integrity,
    NexaUpdateCompatibilityCheck Compatibility,
    NexaUpdateRollbackPlan RollbackPlan,
    IReadOnlyList<string> CompatibilityNotes,
    bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(ManifestId, nameof(ManifestId), errors);
        if (!Redacted)
            errors.Add("Update manifest must be redacted.");
        if (!Integrity.Valid)
            errors.Add("Update manifest requires hash and signature metadata.");
        foreach (var component in Package.Components)
            errors.AddRange(component.Validate().Errors);
        if (RollbackPlan.ExecuteAutomatically)
            errors.Add("Rollback plan cannot execute automatically.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaReleaseManifest(string ReleaseId, NexaReleaseChannel Channel, NexaUpdateManifest UpdateManifest, bool Redacted);

public sealed record NexaReleaseAuditEvent(
    string EventId,
    NexaReleaseChannelKind Channel,
    NexaReleaseVersion Version,
    NexaUpdateEligibilityDecisionKind Decision,
    string Reason,
    DateTimeOffset TimestampUtc,
    bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(EventId, nameof(EventId), errors);
        if (!Redacted)
            errors.Add("Release audit must be redacted.");
        if (BrowserCredentialRedactor.ContainsSecret(Reason))
            errors.Add("Release audit contains secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaUpdateEligibilityRequest(
    NexaReleaseVersion CurrentVersion,
    NexaUpdateManifest TargetManifest,
    NexaReleaseChannel Channel,
    NexaPlanKind Plan,
    bool TenantPolicyAllows,
    NexaConfigurationProfileKind EnvironmentProfile,
    bool AdminApprovalPresent,
    bool AutoExecuteRequested,
    NexaReleaseVersion? EnterprisePinnedVersion);

public sealed record NexaUpdateEligibilityResult(
    NexaUpdateEligibilityDecisionKind Decision,
    string Reason,
    bool UpdateExecuted,
    NexaReleaseAuditEvent AuditEvent)
{
    public bool Eligible => Decision == NexaUpdateEligibilityDecisionKind.Eligible && !UpdateExecuted && AuditEvent.Validate().IsValid;
}

public sealed record NexaRollbackPlan(string PlanId, NexaReleaseVersion FromVersion, NexaReleaseVersion ToVersion, bool ExecuteAutomatically);

public sealed record NexaRollbackEligibility(bool Eligible, string Reason, bool ExecutesRollback);

public sealed record NexaRollbackDecision(bool Allowed, string Reason, NexaRollbackPlan Plan, bool Executed);
