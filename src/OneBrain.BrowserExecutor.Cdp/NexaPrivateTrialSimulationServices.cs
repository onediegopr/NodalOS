using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaPrivateTrialSimulationService
{
    public NexaPrivateTrialSimulationResult Run(NexaPrivateTrialSimulationRequest request)
    {
        var states = new List<NexaPrivateTrialLifecycleState> { NexaPrivateTrialLifecycleState.Requested };
        var violations = new List<string>();
        if (!request.AdminApproved || request.ApproverRole is not (NexaRole.Owner or NexaRole.Admin))
            violations.Add("trial requires owner/admin approval");
        if (request.BillingProvider.RealBillingEnabled || request.BillingProvider.ProviderKind == NexaPaymentProviderKind.RealProviderFuture)
            violations.Add("real billing provider blocked");
        if (request.EmailProvider.RealEmailDeliveryEnabled || request.EmailProvider.ProviderKind == NexaEmailProviderKind.RealProviderFuture)
            violations.Add("real email provider blocked");
        if (request.RequestedFeatures.Contains(NexaFeatureFlag.SensitiveRealPilot))
            violations.Add("SensitiveRealPilot blocked");
        if (request.RequestedFeatures.Contains(NexaFeatureFlag.ProductiveVault))
            violations.Add("ProductiveVault blocked by default");
        if (request.RequestedFeatures.Contains(NexaFeatureFlag.ReplayProductive) || request.RequestedFeatures.Contains(NexaFeatureFlag.RecorderProductive))
            violations.Add("productive recorder/replay blocked");

        if (violations.Count > 0)
            return Blocked(request, states, violations);

        states.AddRange([
            NexaPrivateTrialLifecycleState.AdminApproved,
            NexaPrivateTrialLifecycleState.LicenseCreated,
            NexaPrivateTrialLifecycleState.BillingPreviewCreated,
            NexaPrivateTrialLifecycleState.EmailDraftQueued,
            NexaPrivateTrialLifecycleState.Active
        ]);

        var plan = NexaPlan.Trial(request.RequestedFeatures, request.Limits, TimeSpan.FromDays(14));
        var license = new NexaLicense($"license-private-trial-{Guid.NewGuid():N}", request.AccountId, plan, NexaLicenseStatus.Active, request.RequestedAtUtc, request.RequestedAtUtc.AddDays(14), [], ManualAdminOverride: false);
        return new NexaPrivateTrialSimulationResult(
            states,
            license,
            new NexaPrivateTrialBillingSimulation(InvoicePreviewCreated: true, RealChargeCreated: false, PaymentCardDataStored: false, [], Redacted: true),
            new NexaPrivateTrialEmailSimulation(EmailDraftQueued: true, RealEmailSent: false, $"email-outbox-{Guid.NewGuid():N}", [], Redacted: true),
            new NexaPrivateTrialAuditEvent($"private-trial-audit-{Guid.NewGuid():N}", request.RequestId, request.AccountId, NexaPrivateTrialLifecycleState.Active, "private trial activated in sandbox", Redacted: true),
            [],
            Redacted: true);
    }

    public NexaPrivateTrialSimulationResult Expire(NexaPrivateTrialSimulationResult result) =>
        result with
        {
            States = result.States.Concat([NexaPrivateTrialLifecycleState.Expiring, NexaPrivateTrialLifecycleState.Expired]).ToList(),
            License = result.License is null ? null : result.License with { Status = NexaLicenseStatus.Expired },
            Audit = result.Audit with { State = NexaPrivateTrialLifecycleState.Expired, Reason = "private trial expired in sandbox" }
        };

    public NexaPrivateTrialSimulationResult Revoke(NexaPrivateTrialSimulationResult result) =>
        result with
        {
            States = result.States.Concat([NexaPrivateTrialLifecycleState.Revoked]).ToList(),
            License = result.License is null ? null : result.License with { Status = NexaLicenseStatus.Revoked },
            Audit = result.Audit with { State = NexaPrivateTrialLifecycleState.Revoked, Reason = "private trial revoked in sandbox" }
        };

    private static NexaPrivateTrialSimulationResult Blocked(NexaPrivateTrialSimulationRequest request, IReadOnlyList<NexaPrivateTrialLifecycleState> states, IReadOnlyList<string> violations) =>
        new(
            states.Concat([NexaPrivateTrialLifecycleState.Blocked]).ToList(),
            null,
            new NexaPrivateTrialBillingSimulation(false, false, false, violations, Redacted: true),
            new NexaPrivateTrialEmailSimulation(false, false, "", violations, Redacted: true),
            new NexaPrivateTrialAuditEvent($"private-trial-audit-{Guid.NewGuid():N}", request.RequestId, request.AccountId, NexaPrivateTrialLifecycleState.Blocked, BrowserCredentialRedactor.Redact(string.Join("; ", violations)), Redacted: true),
            violations,
            Redacted: true);
}
