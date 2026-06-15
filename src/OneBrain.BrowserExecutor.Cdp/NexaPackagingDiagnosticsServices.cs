using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaPackageReadinessEvaluator
{
    public NexaPackageReadinessReport Evaluate(NexaPackageManifest manifest)
    {
        var checks = new List<NexaPackageReadinessCheck>
        {
            Check("package-manifest-valid", manifest.Validate().IsValid, "package manifest valid"),
            Check("browser-runtime-available", manifest.Components.Any(c => c.ComponentId == "component-browser-runtime" && c.Available), "browser runtime component available"),
            Check("admin-licensing-available", manifest.Environment.AdminLicensingAvailable, "admin licensing module available"),
            Check("vault-health-available", manifest.Environment.VaultProviderAvailable, "vault provider health available")
        };
        return new NexaPackageReadinessReport(checks, Redacted: true);
    }

    private static NexaPackageReadinessCheck Check(string id, bool ok, string reason) =>
        new(id, ok ? NexaHealthCheckStatus.Healthy : NexaHealthCheckStatus.Failed, BrowserCredentialRedactor.Redact(reason), Redacted: true);
}

public sealed class NexaHealthReportCollector
{
    public NexaHealthReport Collect(NexaPackageManifest manifest, bool diagnosticsRedactionActive = true)
    {
        var results = new[]
        {
            Result("health-build-info", manifest.Validate().IsValid, "runtime build info present"),
            Result("health-license-module", manifest.Environment.AdminLicensingAvailable, "license module available"),
            Result("health-admin-runtime", manifest.Components.Any(c => c.ComponentId == "component-admin-runtime" && c.Available), "admin runtime available"),
            Result("health-tenant-governance", manifest.Components.Any(c => c.ComponentId == "component-tenant-governance" && c.Available), "tenant governance available"),
            Result("health-audit-export", manifest.Components.Any(c => c.ComponentId == "component-audit-export" && c.Available), "audit export available"),
            Result("health-browser-gate", manifest.Components.Any(c => c.ComponentId == "component-browser-runtime" && c.Available), "browser runtime gate available"),
            Result("health-vault-provider", manifest.Environment.VaultProviderAvailable, "vault provider health available"),
            Result("health-diagnostics-redaction", diagnosticsRedactionActive, "diagnostics redaction active")
        };
        return new NexaHealthReport(results, Redacted: true);
    }

    public NexaHealthReport UnknownComponentReport() =>
        new([new NexaHealthCheckResult("health-unknown-component", NexaHealthCheckStatus.Failed, "unknown component failed closed", Redacted: true)], Redacted: true);

    private static NexaHealthCheckResult Result(string id, bool ok, string reason) =>
        new(id, ok ? NexaHealthCheckStatus.Healthy : NexaHealthCheckStatus.Failed, BrowserCredentialRedactor.Redact(reason), Redacted: true);
}

public sealed class NexaDiagnosticsCollector
{
    private readonly NexaHealthReportCollector _health = new();

    public NexaDiagnosticsBundleResult Collect(NexaDiagnosticsBundleRequest request, NexaPackageManifest manifest, NexaLicense license, NexaProductAccount account, IReadOnlyList<NexaAdminAuditEvent> recentAudit, IReadOnlyList<string>? recentErrors = null)
    {
        var health = _health.Collect(manifest);
        var sections = new List<NexaDiagnosticsSection>
        {
            Section("diagnostics-environment", "Environment summary", $"{manifest.Environment.OperatingSystem}; dotnet={manifest.Environment.ExpectedDotnetRuntime}"),
            Section("diagnostics-package", "Package manifest", NexaDiagnosticsHash.Json(new { manifest.PackageId, manifest.Channel, manifest.Version })),
            Section("diagnostics-health", "Health checks", NexaDiagnosticsHash.Json(health.Results.Select(r => new { r.CheckId, r.Status, r.Reason }))),
            Section("diagnostics-features", "Feature flags", string.Join(",", manifest.FeatureFlags)),
            Section("diagnostics-license", "License status", $"{license.Plan.Kind}:{license.Status}:{license.ExpiresAtUtc:O}"),
            Section("diagnostics-admin", "Admin tenant summary", $"{account.AccountId}:{account.Organization.OrganizationId}:{account.Workspaces.Count}:{account.Workers.Count}"),
            Section("diagnostics-browser", "Browser runtime status", "core governed metadata only"),
            Section("diagnostics-vault", "Vault provider health", manifest.Environment.VaultProviderAvailable ? "provider health available" : "provider health unavailable")
        };
        if (request.IncludeAuditSummary)
            sections.Add(Section("diagnostics-audit", "Recent audit summary", NexaDiagnosticsHash.Json(recentAudit.Select(a => new { a.EventId, a.Action, a.Decision, Reason = BrowserCredentialRedactor.Redact(a.Reason) }))));
        if (request.IncludeRecentErrors && recentErrors is not null)
            sections.Add(Section("diagnostics-errors", "Recent errors", string.Join("\n", recentErrors.Select(BrowserCredentialRedactor.Redact))));

        if (sections.Any(s => !s.Validate().IsValid))
            return new NexaDiagnosticsBundleResult(null, NexaHealthCheckStatus.Failed, "diagnostics redaction failed closed", Redacted: true);

        var payload = NexaDiagnosticsHash.Json(sections.Select(s => new { s.SectionId, s.Title, s.Content }));
        var manifestOut = new NexaDiagnosticsManifest(request.BundleId, sections.Count, request.RedactionPolicy.PolicyId, NexaDiagnosticsHash.Sha256(payload), Redacted: true);
        var bundle = new NexaDiagnosticsBundle(manifestOut, sections, health, Redacted: true);
        return new NexaDiagnosticsBundleResult(bundle, bundle.IsSafe ? NexaHealthCheckStatus.Healthy : NexaHealthCheckStatus.Failed, bundle.IsSafe ? "diagnostics bundle created" : "diagnostics bundle unsafe", Redacted: true);
    }

    private static NexaDiagnosticsSection Section(string id, string title, string content) =>
        new(id, title, BrowserCredentialRedactor.Redact(content), Redacted: true);
}

public sealed class NexaSupportModeService
{
    private readonly NexaTenantGovernanceEvaluator _tenant = new();
    private readonly NexaDiagnosticsCollector _diagnostics = new();

    public NexaSupportModeDecision Evaluate(NexaSupportModePolicy policy, NexaTenant actor, NexaTenant target)
    {
        if (policy.Mode != NexaSupportMode.MetadataOnlyReadOnly || !policy.MetadataOnly || !policy.ReadOnly)
            return Decision(NexaHealthCheckStatus.Failed, "support mode must be metadata-only read-only");
        if (policy.AllowSecretAccess || policy.AllowVaultRawAccess || policy.AllowSessionAccess)
            return Decision(NexaHealthCheckStatus.Failed, "support mode cannot access secrets vault raw values or sessions");
        var tenant = _tenant.Evaluate(new NexaTenantGovernanceRequest(actor, target, NexaRole.Support, NexaAdminAction.SupportInspect, NexaTenantDataClassification.InternalMetadata, new NexaTenantPolicy(policy.AllowCrossTenantAccess, false, false, true)));
        if (!tenant.Allowed)
            return Decision(NexaHealthCheckStatus.Failed, tenant.Reason);
        return Decision(NexaHealthCheckStatus.Healthy, "support mode metadata-only allowed");
    }

    public NexaSupportBundleResult CreateBundle(NexaSupportBundleRequest request, NexaPackageManifest manifest, NexaLicense license, NexaProductAccount account, IReadOnlyList<NexaAdminAuditEvent> recentAudit)
    {
        var decision = Evaluate(request.Policy, request.ActorTenant, request.TargetTenant);
        if (!decision.Allowed)
            return new NexaSupportBundleResult(decision, null, Redacted: true);
        var bundle = _diagnostics.Collect(new NexaDiagnosticsBundleRequest(request.RequestId, NexaDiagnosticsRedactionPolicy.Strict(), true, true), manifest, license, account, recentAudit);
        return new NexaSupportBundleResult(decision, bundle.Bundle, Redacted: true);
    }

    private static NexaSupportModeDecision Decision(NexaHealthCheckStatus status, string reason) =>
        new(status, BrowserCredentialRedactor.Redact(reason), Redacted: true);
}
