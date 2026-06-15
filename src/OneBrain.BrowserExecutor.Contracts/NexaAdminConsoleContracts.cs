namespace OneBrain.BrowserExecutor.Contracts;

public sealed record NexaAdminAccountViewModel(
    string AccountId,
    NexaProductAccountKind AccountType,
    NexaAccountStatus Status,
    string SafeDisplayName,
    bool Redacted);

public sealed record NexaAdminOrganizationViewModel(
    string OrganizationId,
    string SafeDisplayName,
    IReadOnlyList<string> WorkspaceIds,
    bool Redacted);

public sealed record NexaAdminWorkerViewModel(
    string WorkerId,
    string WorkspaceId,
    string SeatId,
    NexaRole Role,
    bool Active,
    IReadOnlyList<NexaAdminCapability> Capabilities);

public sealed record NexaAdminLicenseViewModel(
    string LicenseId,
    NexaPlanKind Plan,
    NexaLicenseStatus Status,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyList<NexaFeatureFlag> EnabledFeatures,
    IReadOnlyList<NexaFeatureFlag> DisabledSensitiveFeatures,
    bool Redacted);

public sealed record NexaAdminFeatureFlagViewModel(
    NexaFeatureFlag Feature,
    bool Enabled,
    bool Blocked,
    string BlockedReason,
    bool RequiresEnterpriseCompliance);

public sealed record NexaAdminUsageViewModel(
    string LimitId,
    int Count,
    int MaxCount,
    TimeSpan Window,
    bool LimitExceeded);

public sealed record NexaAdminAuditViewModel(
    string EventId,
    NexaAdminAction Action,
    NexaAdminDecisionKind Decision,
    string Reason,
    DateTimeOffset TimestampUtc,
    bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(EventId, nameof(EventId), errors);
        if (!Redacted)
            errors.Add("Admin audit view must be redacted.");
        if (BrowserCredentialRedactor.ContainsSecret(Reason))
            errors.Add("Admin audit view contains secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaAdminConsoleDashboardModel(
    NexaAdminAccountViewModel Account,
    NexaAdminOrganizationViewModel Organization,
    IReadOnlyList<NexaAdminWorkerViewModel> Workers,
    NexaAdminLicenseViewModel License,
    IReadOnlyList<NexaAdminFeatureFlagViewModel> Features,
    IReadOnlyList<NexaAdminUsageViewModel> Usage,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> BlockedCapabilities,
    IReadOnlyList<NexaAdminAuditViewModel> Audit,
    bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (!Redacted || !Account.Redacted || !License.Redacted || Audit.Any(a => !a.Redacted))
            errors.Add("Admin console dashboard must be redacted.");
        if (BrowserCredentialRedactor.ContainsSecret(Account.SafeDisplayName) ||
            BrowserCredentialRedactor.ContainsSecret(Organization.SafeDisplayName) ||
            Warnings.Any(BrowserCredentialRedactor.ContainsSecret) ||
            BlockedCapabilities.Any(BrowserCredentialRedactor.ContainsSecret))
            errors.Add("Admin console dashboard contains secret-like content.");
        foreach (var audit in Audit)
            errors.AddRange(audit.Validate().Errors);
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public abstract record NexaAdminCommand(
    string CommandId,
    string ActorId,
    NexaRole ActorRole,
    string TargetAccountId,
    string TargetOrganizationId,
    NexaAdminAction RequestedAction);

public sealed record NexaAdminCreateAccountCommand(
    string CommandId,
    string ActorId,
    NexaRole ActorRole,
    string TargetAccountId,
    string TargetOrganizationId,
    NexaProductAccountKind AccountKind)
    : NexaAdminCommand(CommandId, ActorId, ActorRole, TargetAccountId, TargetOrganizationId, NexaAdminAction.UpdateAccount);

public sealed record NexaAdminUpdateAccountCommand(
    string CommandId,
    string ActorId,
    NexaRole ActorRole,
    string TargetAccountId,
    string TargetOrganizationId,
    NexaAccountStatus Status)
    : NexaAdminCommand(CommandId, ActorId, ActorRole, TargetAccountId, TargetOrganizationId, NexaAdminAction.UpdateAccount);

public sealed record NexaAdminCreateWorkerCommand(
    string CommandId,
    string ActorId,
    NexaRole ActorRole,
    string TargetAccountId,
    string TargetOrganizationId,
    string WorkspaceId,
    NexaRole WorkerRole)
    : NexaAdminCommand(CommandId, ActorId, ActorRole, TargetAccountId, TargetOrganizationId, NexaAdminAction.AddWorker);

public sealed record NexaAdminUpdateWorkerRoleCommand(
    string CommandId,
    string ActorId,
    NexaRole ActorRole,
    string TargetAccountId,
    string TargetOrganizationId,
    string WorkerId,
    NexaRole NewRole)
    : NexaAdminCommand(CommandId, ActorId, ActorRole, TargetAccountId, TargetOrganizationId, NexaAdminAction.UpdateWorker);

public sealed record NexaAdminAssignLicenseCommand(
    string CommandId,
    string ActorId,
    NexaRole ActorRole,
    string TargetAccountId,
    string TargetOrganizationId,
    string LicenseId)
    : NexaAdminCommand(CommandId, ActorId, ActorRole, TargetAccountId, TargetOrganizationId, NexaAdminAction.ManagePlan);

public sealed record NexaAdminSetFeatureFlagCommand(
    string CommandId,
    string ActorId,
    NexaRole ActorRole,
    string TargetAccountId,
    string TargetOrganizationId,
    NexaFeatureFlag Feature,
    bool Enabled,
    bool ComplianceApproved)
    : NexaAdminCommand(CommandId, ActorId, ActorRole, TargetAccountId, TargetOrganizationId, NexaAdminAction.ManagePlan);

public sealed record NexaAdminSetUsageLimitCommand(
    string CommandId,
    string ActorId,
    NexaRole ActorRole,
    string TargetAccountId,
    string TargetOrganizationId,
    NexaUsageLimit Limit)
    : NexaAdminCommand(CommandId, ActorId, ActorRole, TargetAccountId, TargetOrganizationId, NexaAdminAction.ManagePlan);

public sealed record NexaAdminSuspendAccountCommand(
    string CommandId,
    string ActorId,
    NexaRole ActorRole,
    string TargetAccountId,
    string TargetOrganizationId)
    : NexaAdminCommand(CommandId, ActorId, ActorRole, TargetAccountId, TargetOrganizationId, NexaAdminAction.UpdateAccount);

public sealed record NexaAdminAuditQuery(
    string QueryId,
    string ActorId,
    NexaRole ActorRole,
    string TargetAccountId,
    string TargetOrganizationId,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc)
    : NexaAdminCommand(QueryId, ActorId, ActorRole, TargetAccountId, TargetOrganizationId, NexaAdminAction.ViewAudit);

public sealed record NexaAdminCommandResult(
    NexaAdminDecisionKind Decision,
    string CommandId,
    string Reason,
    IReadOnlyList<string> AuditRefs,
    NexaAdminAuditViewModel Audit,
    bool Redacted)
{
    public bool Succeeded => Decision == NexaAdminDecisionKind.Allowed && Redacted && Audit.Validate().IsValid;
}
