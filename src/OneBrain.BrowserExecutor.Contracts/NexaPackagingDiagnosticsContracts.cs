using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaPackageChannel
{
    Dev,
    Test,
    Preview,
    Stable
}

public enum NexaHealthCheckStatus
{
    Healthy,
    Warning,
    Failed,
    Unknown
}

public enum NexaSupportMode
{
    Disabled,
    MetadataOnlyReadOnly
}

public sealed record NexaPackageVersion(string AppVersion, string RuntimeVersion, string BrowserRuntimeVersion);

public sealed record NexaPackageComponent(string ComponentId, string Name, string Version, bool Available)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(ComponentId, nameof(ComponentId), errors);
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Version))
            errors.Add("Package component name and version are required.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaPackageEnvironment(
    string OperatingSystem,
    string ExpectedDotnetRuntime,
    bool BrowserAvailable,
    bool CdpAvailable,
    bool VaultProviderAvailable,
    bool AdminLicensingAvailable);

public sealed record NexaPackageManifest(
    string PackageId,
    NexaPackageVersion Version,
    NexaPackageChannel Channel,
    NexaPackageEnvironment Environment,
    IReadOnlyList<NexaPackageComponent> Components,
    IReadOnlyList<NexaFeatureFlag> FeatureFlags,
    bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(PackageId, nameof(PackageId), errors);
        if (!Redacted)
            errors.Add("Package manifest must be redacted.");
        foreach (var component in Components)
            errors.AddRange(component.Validate().Errors);
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaPackageReadinessCheck(string CheckId, NexaHealthCheckStatus Status, string Reason, bool Redacted)
{
    public bool Passed => Status == NexaHealthCheckStatus.Healthy;
}

public sealed record NexaPackageReadinessReport(IReadOnlyList<NexaPackageReadinessCheck> Checks, bool Redacted)
{
    public bool Ready => Redacted && Checks.All(c => c.Passed && c.Redacted);
}

public sealed record NexaHealthCheck(string CheckId, string ComponentId, bool Required);

public sealed record NexaHealthCheckResult(string CheckId, NexaHealthCheckStatus Status, string Reason, bool Redacted)
{
    public bool Passed => Status == NexaHealthCheckStatus.Healthy && Redacted && !BrowserCredentialRedactor.ContainsSecret(Reason);
}

public sealed record NexaHealthReport(IReadOnlyList<NexaHealthCheckResult> Results, bool Redacted)
{
    public bool Healthy => Redacted && Results.All(r => r.Passed);
}

public sealed record NexaDiagnosticsRedactionPolicy(
    string PolicyId,
    bool RedactSecrets,
    bool RedactCookies,
    bool RedactBodies,
    bool RedactPaths)
{
    public static NexaDiagnosticsRedactionPolicy Strict() =>
        new("diagnostics-redaction-strict", true, true, true, true);
}

public sealed record NexaDiagnosticsSection(string SectionId, string Title, string Content, bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(SectionId, nameof(SectionId), errors);
        if (!Redacted)
            errors.Add("Diagnostics section must be redacted.");
        if (BrowserCredentialRedactor.ContainsSecret(Content))
            errors.Add("Diagnostics section contains secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaDiagnosticsManifest(
    string BundleId,
    int SectionCount,
    string RedactionPolicyId,
    string Hash,
    bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(BundleId, nameof(BundleId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(RedactionPolicyId, nameof(RedactionPolicyId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(Hash, nameof(Hash), errors);
        if (!Redacted)
            errors.Add("Diagnostics manifest must be redacted.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaDiagnosticsBundle(
    NexaDiagnosticsManifest Manifest,
    IReadOnlyList<NexaDiagnosticsSection> Sections,
    NexaHealthReport HealthReport,
    bool Redacted)
{
    public bool IsSafe =>
        Redacted &&
        Manifest.Validate().IsValid &&
        Sections.All(s => s.Validate().IsValid) &&
        HealthReport.Redacted &&
        !BrowserCredentialRedactor.ContainsSecret(string.Join('\n', Sections.Select(s => s.Content)));
}

public sealed record NexaDiagnosticsBundleRequest(
    string BundleId,
    NexaDiagnosticsRedactionPolicy RedactionPolicy,
    bool IncludeRecentErrors,
    bool IncludeAuditSummary);

public sealed record NexaDiagnosticsBundleResult(NexaDiagnosticsBundle? Bundle, NexaHealthCheckStatus Status, string Reason, bool Redacted);

public sealed record NexaSupportModePolicy(
    NexaSupportMode Mode,
    TimeSpan Ttl,
    bool MetadataOnly,
    bool ReadOnly,
    bool AllowSecretAccess,
    bool AllowVaultRawAccess,
    bool AllowSessionAccess,
    bool AllowCrossTenantAccess)
{
    public static NexaSupportModePolicy StrictMetadataOnly(TimeSpan? ttl = null) =>
        new(NexaSupportMode.MetadataOnlyReadOnly, ttl ?? TimeSpan.FromMinutes(30), true, true, false, false, false, false);
}

public sealed record NexaSupportModeDecision(NexaHealthCheckStatus Status, string Reason, bool Redacted)
{
    public bool Allowed => Status == NexaHealthCheckStatus.Healthy && Redacted;
}

public sealed record NexaSupportBundleRequest(string RequestId, string ActorId, NexaSupportModePolicy Policy, NexaTenant ActorTenant, NexaTenant TargetTenant);

public sealed record NexaSupportBundleResult(NexaSupportModeDecision Decision, NexaDiagnosticsBundle? Bundle, bool Redacted);

public static class NexaDiagnosticsHash
{
    public static string Sha256(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();

    public static string Json(object value) => JsonSerializer.Serialize(value);
}
