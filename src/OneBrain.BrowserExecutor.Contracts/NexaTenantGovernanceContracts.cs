using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaTenantScope
{
    Account,
    Organization,
    Workspace,
    Worker,
    Support
}

public enum NexaTenantDataClassification
{
    PublicMetadata,
    InternalMetadata,
    TenantConfidential,
    Sensitive,
    Secret
}

public enum NexaTenantIsolationDecisionKind
{
    Allowed,
    Denied,
    FailClosed
}

public enum NexaAuditExportFormat
{
    Json,
    Jsonl
}

public sealed record NexaTenant(
    string TenantId,
    string AccountId,
    string OrganizationId,
    string WorkspaceId,
    string WorkerId)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(TenantId, nameof(TenantId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(AccountId, nameof(AccountId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(OrganizationId, nameof(OrganizationId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(WorkspaceId, nameof(WorkspaceId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(WorkerId, nameof(WorkerId), errors);
        if (string.IsNullOrWhiteSpace(TenantId) ||
            string.IsNullOrWhiteSpace(AccountId) ||
            string.IsNullOrWhiteSpace(OrganizationId) ||
            string.IsNullOrWhiteSpace(WorkspaceId) ||
            string.IsNullOrWhiteSpace(WorkerId))
            errors.Add("Tenant boundary ids are required.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaTenantBoundary(
    string AccountId,
    string OrganizationId,
    string WorkspaceId,
    string WorkerId,
    NexaTenantScope Scope);

public sealed record NexaTenantPolicy(
    bool AllowCrossTenantAccess,
    bool AllowSupportSecretAccess,
    bool AllowSensitiveDataExport,
    bool SensitiveFeaturesRequireExplicitPolicy)
{
    public static NexaTenantPolicy Strict() =>
        new(false, false, false, true);
}

public sealed record NexaTenantGovernanceRequest(
    NexaTenant ActorTenant,
    NexaTenant TargetTenant,
    NexaRole ActorRole,
    NexaAdminAction RequestedAction,
    NexaTenantDataClassification DataClassification,
    NexaTenantPolicy Policy);

public sealed record NexaTenantGovernanceDecision(
    NexaTenantIsolationDecisionKind Decision,
    string Reason,
    NexaTenantScope Scope,
    bool Redacted)
{
    public bool Allowed => Decision == NexaTenantIsolationDecisionKind.Allowed && Redacted;
}

public sealed record NexaTenantAuditScope(
    string AccountId,
    string OrganizationId,
    string WorkspaceId,
    NexaTenantScope Scope)
{
    public bool Contains(NexaAdminAuditEvent auditEvent) =>
        string.Equals(AccountId, auditEvent.AccountId, StringComparison.Ordinal) &&
        string.Equals(OrganizationId, auditEvent.OrganizationId, StringComparison.Ordinal);
}

public sealed record NexaAuditExportRedactionPolicy(
    string PolicyId,
    bool RedactSecrets,
    bool RedactCookies,
    bool RedactBodies,
    bool RedactSensitivePaths)
{
    public static NexaAuditExportRedactionPolicy Strict() =>
        new("audit-export-redaction-strict", true, true, true, true);
}

public sealed record NexaAuditExportRequest(
    string ExportId,
    string ActorId,
    NexaRole ActorRole,
    NexaTenantAuditScope Scope,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    NexaAuditExportFormat Format,
    NexaAuditExportRedactionPolicy RedactionPolicy,
    bool SensitiveDataPolicyApproved);

public sealed record NexaAuditExportManifest(
    string ExportId,
    NexaTenantAuditScope Scope,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    int EventCount,
    string RedactionPolicyId,
    string Hash,
    IReadOnlyList<string> SourceAuditRefs,
    bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(ExportId, nameof(ExportId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(RedactionPolicyId, nameof(RedactionPolicyId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(Hash, nameof(Hash), errors);
        if (!Redacted)
            errors.Add("Audit export manifest must be redacted.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaAuditExportResult(
    NexaTenantIsolationDecisionKind Decision,
    string Reason,
    NexaAuditExportManifest? Manifest,
    string Payload,
    bool Redacted)
{
    public bool Succeeded => Decision == NexaTenantIsolationDecisionKind.Allowed && Manifest?.Validate().IsValid == true && Redacted;

    public bool ContainsSecretLikeContent() =>
        BrowserCredentialRedactor.ContainsSecret(Payload) || BrowserCredentialRedactor.ContainsSecret(Reason);

    public static string ComputeHash(string payload)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string SerializeEvents(IReadOnlyList<NexaAdminAuditEvent> events, NexaAuditExportFormat format)
    {
        var safe = events.Select(e => new
        {
            e.EventId,
            e.ActorId,
            Role = e.Role.ToString(),
            e.AccountId,
            e.OrganizationId,
            Action = e.Action.ToString(),
            Decision = e.Decision.ToString(),
            Reason = BrowserCredentialRedactor.Redact(e.Reason),
            TimestampUtc = e.TimestampUtc.ToString("O"),
            BeforeSummary = BrowserCredentialRedactor.Redact(e.BeforeSummary),
            AfterSummary = BrowserCredentialRedactor.Redact(e.AfterSummary),
            Redacted = true
        }).ToArray();
        return format == NexaAuditExportFormat.Jsonl
            ? string.Join('\n', safe.Select(e => JsonSerializer.Serialize(e)))
            : JsonSerializer.Serialize(safe);
    }
}
