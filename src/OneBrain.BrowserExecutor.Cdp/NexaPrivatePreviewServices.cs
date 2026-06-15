using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaPrivatePreviewLocalEvaluator
{
    public NexaPrivatePreviewLocalResult Evaluate(
        NexaPrivatePreviewLocalProfile profile,
        NexaPrivatePreviewLocalSession session,
        NexaPrivatePreviewLocalReadiness readiness)
    {
        var violations = new List<string>();

        if (!profile.LocalMachineOnly)
            violations.Add("private preview must be local machine only");
        if (!profile.SingleTenant)
            violations.Add("private preview must be single tenant");
        if (!profile.SyntheticDataOnly)
            violations.Add("private preview requires synthetic data only");
        if (!profile.MockBillingOnly)
            violations.Add("real billing is blocked");
        if (!profile.MockEmailOnly)
            violations.Add("real email is blocked");
        if (!profile.PublicApiListenerDisabled || !profile.PublicSaasActivationDisabled)
            violations.Add("public SaaS/API activation is blocked");
        if (!profile.SensitiveRealPilotDisabled)
            violations.Add("sensitive real pilot is blocked");
        if (!profile.ProductiveRecorderReplayDisabled)
            violations.Add("productive recorder/replay is blocked");
        if (!session.M51ExternalProofDeferred)
            violations.Add("M51 status must be explicit");
        if (!readiness.ConfigProfileCompatible)
            violations.Add("configuration profile incompatible");
        if (!readiness.LicenseMockValid)
            violations.Add("mock license invalid");
        if (!readiness.AdminRuntimeAvailable)
            violations.Add("admin runtime unavailable");
        if (!readiness.TenantGovernanceAvailable)
            violations.Add("tenant governance unavailable");
        if (!readiness.DiagnosticsRedacted)
            violations.Add("diagnostics redaction missing");
        if (!readiness.AuditKeyCustodyAvailable)
            violations.Add("audit key custody missing");
        if (!readiness.ProductShellRoutesAvailable)
            violations.Add("product shell routes unavailable");
        if (!readiness.PublicApiDesignOnly)
            violations.Add("public API must remain design-only");
        if (!readiness.BillingEmailMockOnly)
            violations.Add("billing/email must remain mock-only");
        if (!readiness.SensitiveFeaturesDisabled)
            violations.Add("sensitive features must remain disabled");
        if (!readiness.LeakHardeningCompleted)
            violations.Add("leak hardening incomplete");
        if (!readiness.SkippedTestsAuditCompleted)
            violations.Add("skipped tests audit incomplete");

        return new NexaPrivatePreviewLocalResult(
            violations.Count == 0 ? NexaPrivatePreviewLocalStatus.Allowed : NexaPrivatePreviewLocalStatus.Blocked,
            profile,
            session,
            readiness,
            violations,
            Redacted: true);
    }

    public static NexaPrivatePreviewLocalProfile SafeProfile() =>
        new(
            "private-preview-local-single-tenant",
            NexaConfigurationProfileKind.LocalSandbox,
            LocalMachineOnly: true,
            SingleTenant: true,
            SyntheticDataOnly: true,
            MockBillingOnly: true,
            MockEmailOnly: true,
            PublicApiListenerDisabled: true,
            PublicSaasActivationDisabled: true,
            SensitiveRealPilotDisabled: true,
            ProductiveRecorderReplayDisabled: true);

    public static NexaPrivatePreviewLocalSession SafeSession() =>
        new("preview-session-local", "tenant-local", "owner-local", DateTimeOffset.UtcNow, M51ExternalProofDeferred: true);

    public static NexaPrivatePreviewLocalReadiness SafeReadiness() =>
        new(
            ConfigProfileCompatible: true,
            LicenseMockValid: true,
            AdminRuntimeAvailable: true,
            TenantGovernanceAvailable: true,
            DiagnosticsRedacted: true,
            AuditKeyCustodyAvailable: true,
            ProductShellRoutesAvailable: true,
            PublicApiDesignOnly: true,
            BillingEmailMockOnly: true,
            SensitiveFeaturesDisabled: true,
            LeakHardeningCompleted: true,
            SkippedTestsAuditCompleted: true);
}

public sealed class NexaPrivatePreviewRunbookFactory
{
    public NexaPrivatePreviewRunbook Create() =>
        new(
            "private-preview-local-runbook",
            new NexaPrivatePreviewOperationalChecklist(
                [
                    "Select LocalSandbox configuration profile.",
                    "Start local product shell with in-memory single-tenant state.",
                    "Verify mock license, diagnostics redaction, and audit key custody.",
                    "Confirm public API listener, real billing, and real email remain disabled.",
                    "Generate diagnostics bundle and audit export only in redacted mode."
                ]),
            new NexaPrivatePreviewSupportProcedure(
                [
                    "Collect metadata-only diagnostics bundle.",
                    "Review redacted audit export manifest.",
                    "Do not request secrets, cookies, session material, document bodies, or raw vault payloads.",
                    "Escalate external proof issues to M51 debt item."
                ]),
            new NexaPrivatePreviewRollbackProcedure(
                [
                    "Stop local shell process.",
                    "Discard in-memory preview state.",
                    "Run installer rollback dry-run report for documentation only.",
                    "Re-run diagnostics to confirm no public listener or real billing/email is active."
                ],
                ManualOnly: true,
                ExecutesRollback: false),
            new NexaPrivatePreviewKnownLimitations(
                [
                    "M51 external read-only target proof is deferred.",
                    "No public SaaS/API exposure.",
                    "No real billing or payment provider.",
                    "No real email delivery.",
                    "No real client credentials.",
                    "No sensitive real sites, AFIP, banking, ERP, submit, pay, sign, or delete.",
                    "Productive recorder/replay remains blocked."
                ]),
            DeclaresM51Deferred: true,
            DeclaresNoPublicSaas: true,
            DeclaresNoRealBilling: true,
            DeclaresNoRealCredentials: true,
            DeclaresNoSensitiveSites: true,
            Redacted: true);
}
