using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaAdminConsoleModelBuilder
{
    public NexaAdminConsoleDashboardModel Build(NexaProductAccount account, NexaLicense license, IReadOnlyList<NexaUsageCounter> counters, IReadOnlyList<NexaAdminAuditEvent> auditEvents)
    {
        var disabledSensitive = SensitiveDisabledFeatures();
        var features = Enum.GetValues<NexaFeatureFlag>()
            .Where(feature => feature != NexaFeatureFlag.RecorderProductive && feature != NexaFeatureFlag.ReplayProductive)
            .Select(feature => FeatureView(license, feature))
            .ToArray();
        var usage = license.Plan.Limits
            .Select(limit =>
            {
                var counter = counters.FirstOrDefault(c => c.LimitId.Equals(limit.LimitId, StringComparison.OrdinalIgnoreCase));
                var count = counter?.Count ?? 0;
                return new NexaAdminUsageViewModel(limit.LimitId, count, limit.MaxCount, limit.Window, count >= limit.MaxCount);
            })
            .ToArray();
        return new NexaAdminConsoleDashboardModel(
            new NexaAdminAccountViewModel(account.AccountId, account.Kind, account.Status, BrowserCredentialRedactor.Redact(account.Person?.Email ?? account.Company?.CompanyId ?? account.AccountId), Redacted: true),
            new NexaAdminOrganizationViewModel(account.Organization.OrganizationId, BrowserCredentialRedactor.Redact(account.Organization.DisplayName), account.Workspaces.Select(w => w.WorkspaceId).ToArray(), Redacted: true),
            account.Workers.Select(w => new NexaAdminWorkerViewModel(w.WorkerId, w.WorkspaceId, w.SeatId, w.Role, w.Active, w.Capabilities.ToArray())).ToArray(),
            new NexaAdminLicenseViewModel(license.LicenseId, license.Plan.Kind, license.Status, license.ExpiresAtUtc, license.Plan.EnabledFeatures.ToArray(), disabledSensitive, Redacted: true),
            features,
            usage,
            Warnings(license),
            disabledSensitive.Select(f => f.ToString()).ToArray(),
            auditEvents.Select(e => new NexaAdminAuditViewModel(e.EventId, e.Action, e.Decision, BrowserCredentialRedactor.Redact(e.Reason), e.TimestampUtc, Redacted: true)).ToArray(),
            Redacted: true);
    }

    private static NexaAdminFeatureFlagViewModel FeatureView(NexaLicense license, NexaFeatureFlag feature)
    {
        var sensitiveBlocked = SensitiveDisabledFeatures().Contains(feature);
        var enabled = license.Plan.Enables(feature) && !sensitiveBlocked;
        return new NexaAdminFeatureFlagViewModel(
            feature,
            enabled,
            Blocked: sensitiveBlocked,
            BlockedReason: sensitiveBlocked ? "disabled by default; requires compliance or future milestone" : "",
            RequiresEnterpriseCompliance: feature == NexaFeatureFlag.SensitiveRealPilot);
    }

    private static IReadOnlyList<string> Warnings(NexaLicense license)
    {
        var warnings = new List<string>();
        if (license.Status != NexaLicenseStatus.Active)
            warnings.Add("license is not active");
        if (license.ExpiresAtUtc <= DateTimeOffset.UtcNow.AddDays(3))
            warnings.Add("license expiration is near");
        if (!license.Plan.Enables(NexaFeatureFlag.AdminConsole))
            warnings.Add("admin console feature is not enabled");
        return warnings;
    }

    private static IReadOnlyList<NexaFeatureFlag> SensitiveDisabledFeatures() =>
    [
        NexaFeatureFlag.SensitiveRealPilot,
        NexaFeatureFlag.ProductiveVault,
        NexaFeatureFlag.RecorderProductive,
        NexaFeatureFlag.ReplayProductive
    ];
}

public sealed class NexaAdminCommandHandler
{
    private readonly NexaAdminPolicyEvaluator _adminPolicy = new();

    public NexaAdminCommandResult Handle(NexaAdminCommand command, NexaProductAccount account, NexaLicense? license = null)
    {
        var decision = _adminPolicy.Evaluate(account, command.ActorId, command.ActorRole, command.RequestedAction);
        var finalDecision = decision.Decision;
        var reason = decision.Reason;

        if (decision.Allowed && command is NexaAdminSetFeatureFlagCommand featureCommand)
        {
            if (featureCommand.Feature is NexaFeatureFlag.ReplayProductive or NexaFeatureFlag.RecorderProductive)
            {
                finalDecision = NexaAdminDecisionKind.Denied;
                reason = "productive replay/recorder feature cannot be enabled";
            }
            else if (featureCommand.Feature == NexaFeatureFlag.SensitiveRealPilot && !featureCommand.ComplianceApproved)
            {
                finalDecision = NexaAdminDecisionKind.Denied;
                reason = "sensitive real pilot feature requires compliance approval";
            }
            else if (featureCommand.Feature == NexaFeatureFlag.ProductiveVault && !ProductiveVaultEntitledForControlledAdmin(license))
            {
                finalDecision = NexaAdminDecisionKind.Denied;
                reason = "productive vault requires explicit controlled admin entitlement";
            }
        }

        var audit = new NexaAdminAuditViewModel(
            decision.AuditEvent.EventId,
            command.RequestedAction,
            finalDecision,
            BrowserCredentialRedactor.Redact(reason),
            DateTimeOffset.UtcNow,
            Redacted: true);
        return new NexaAdminCommandResult(
            finalDecision,
            command.CommandId,
            BrowserCredentialRedactor.Redact(reason),
            [audit.EventId],
            audit,
            Redacted: true);
    }

    private static bool ProductiveVaultEntitledForControlledAdmin(NexaLicense? license) =>
        license is { ManualAdminOverride: true } &&
        license.Entitlements.Any(e => e.Feature == NexaFeatureFlag.ProductiveVault && e.Enabled);
}
