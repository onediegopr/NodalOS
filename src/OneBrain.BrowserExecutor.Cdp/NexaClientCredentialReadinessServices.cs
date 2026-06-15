using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaClientCredentialReadinessEvaluator
{
    public NexaClientCredentialReadinessReport Evaluate(NexaClientCredentialReadinessInput input)
    {
        var checks = new List<NexaClientCredentialReadinessCheck>
        {
            Check("audit-key-custody", input.AuditKeyCustodyOk, "M50 audit key custody required"),
            Check("vault-os-backed", input.VaultOsBackedOk, "M39 OS-backed vault required"),
            Check("vault-threat-tests", input.VaultThreatTestsPassed, "M56 vault threat tests required"),
            Check("vault-lifecycle-policy", input.VaultRotationRecoveryExportPolicyOk, "M57 lifecycle policy required"),
            Check("leak-hardening", input.LeakHardeningOk, "M52 leak hardening required"),
            Check("diagnostics-support-redaction", input.DiagnosticsSupportRedacted, "diagnostics/support redaction required"),
            Check("tenant-governance", input.TenantGovernanceOk, "tenant governance required"),
            Check("license-gating", input.LicenseGatingOk, "license gating required"),
            Check("core-only-boundary", input.CoreOnlyBoundaryOk, "core-only vault boundary required"),
            Check("companion-no-authority", input.CompanionNonAuthoritative, "companion must be non-authoritative"),
            Check("profile-raw-blocked", input.ProfileRawBlocked, "raw profile must remain blocked"),
            Check("public-api-not-exposed", input.PublicApiNotExposed, "public API must remain disabled"),
            Check("m51-external-proof", input.M51ExternalProofValidated, "M51 external proof deferred")
        };

        var blockers = new List<NexaClientCredentialBlocker>();
        if (!input.M51ExternalProofValidated)
            blockers.Add(Blocker("m51-deferred", "M51 external proof is deferred.", "Validate external test-owned read-only target."));
        if (!input.ExternalSecurityAuditCompleted)
            blockers.Add(Blocker("external-security-audit", "No external audit after vault hardening.", "Run external security audit before real credentials."));
        if (!input.RealCustomerCredentialPolicyApproved)
            blockers.Add(Blocker("real-credential-policy", "No real customer credential policy approved.", "Approve legal/security policy for real credentials."));
        if (!input.RealCredentialIncidentSupportProcessApproved)
            blockers.Add(Blocker("credential-incident-support", "No real credential incident support process approved.", "Approve incident support process."));

        var coreReady = checks.Where(check => check.CheckId != "m51-external-proof").All(check => check.Passed);
        var status = blockers.Count > 0
            ? NexaClientCredentialReadinessStatus.BlockedForRealClientCredentials
            : coreReady ? NexaClientCredentialReadinessStatus.ReadyForControlledInternalCredentialPilot : NexaClientCredentialReadinessStatus.ReadyForSyntheticOnly;
        if (!input.ExternalSecurityAuditCompleted)
            status = NexaClientCredentialReadinessStatus.RequiresExternalSecurityAudit;
        if (blockers.Any(blocker => blocker.BlockerId is "m51-deferred" or "real-credential-policy" or "credential-incident-support"))
            status = NexaClientCredentialReadinessStatus.BlockedForRealClientCredentials;

        var recommendation = new NexaClientCredentialRecommendation(
            status,
            status == NexaClientCredentialReadinessStatus.BlockedForRealClientCredentials
                ? "Keep real client credentials blocked; continue synthetic/local only."
                : "Proceed only with controlled internal synthetic credential pilot.",
            RealClientCredentialsAllowed: false,
            Redacted: true);
        return new NexaClientCredentialReadinessReport(status, checks, new NexaClientCredentialRiskRegister(blockers, Redacted: true), recommendation, Redacted: true);
    }

    public static NexaClientCredentialReadinessInput SafeSyntheticInput() =>
        new(
            AuditKeyCustodyOk: true,
            VaultOsBackedOk: true,
            VaultThreatTestsPassed: true,
            VaultRotationRecoveryExportPolicyOk: true,
            LeakHardeningOk: true,
            DiagnosticsSupportRedacted: true,
            TenantGovernanceOk: true,
            LicenseGatingOk: true,
            CoreOnlyBoundaryOk: true,
            CompanionNonAuthoritative: true,
            ProfileRawBlocked: true,
            PublicApiNotExposed: true,
            M51ExternalProofValidated: false,
            ExternalSecurityAuditCompleted: false,
            RealCustomerCredentialPolicyApproved: false,
            RealCredentialIncidentSupportProcessApproved: false);

    private static NexaClientCredentialReadinessCheck Check(string id, bool passed, string reason) =>
        new(id, passed, reason, Redacted: true);

    private static NexaClientCredentialBlocker Blocker(string id, string description, string action) =>
        new(id, description, action, Redacted: true);
}
