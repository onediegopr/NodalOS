using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class BrowserVaultThreatEvaluator
{
    public BrowserVaultThreatDecision Evaluate(BrowserVaultThreatRequest request, BrowserSecretReference reference)
    {
        var reasons = new List<string>();
        if (request.ActorKind != BrowserVaultThreatActorKind.Core)
            reasons.Add($"{request.ActorKind} cannot access raw vault material");
        if (!string.Equals(request.ActorTenantId, request.TargetTenantId, StringComparison.OrdinalIgnoreCase))
            reasons.Add("cross-tenant vault retrieval blocked");
        if (!request.WorkerAuthorized)
            reasons.Add("worker is not authorized for vault retrieval");
        if (!request.ProductiveVaultEntitlement)
            reasons.Add("productive vault entitlement missing");
        if (!request.GatePassed)
            reasons.Add("runtime gate failed");
        if (request.AttemptsRawSecretAccess || request.AttemptsPublicDtoSecret || request.AttemptsSerialization)
            reasons.Add("raw secret exposure path blocked");

        if (reasons.Count > 0)
            return new BrowserVaultThreatDecision(BrowserVaultThreatDecisionKind.Blocked, reasons, null, RawSecretExposed: false, Redacted: true);

        var handle = new BrowserVaultCoreOnlySecretHandle(
            $"core-secret-handle-{Guid.NewGuid():N}",
            reference,
            CoreOnly: true,
            PublicDto: false,
            Serializable: false,
            Exportable: false,
            RedactedLabel: BrowserCredentialRedactor.Redact(reference.RedactedLabel));
        return new BrowserVaultThreatDecision(BrowserVaultThreatDecisionKind.AllowedCoreOnly, ["core-only handle issued"], handle, RawSecretExposed: false, Redacted: true);
    }
}

public sealed class BrowserVaultLifecyclePolicyEvaluator
{
    public BrowserVaultRotationDecision EvaluateRotation(BrowserVaultRotationPolicy policy, BrowserVaultRotationPolicyRequest request)
    {
        var reasons = new List<string>();
        if (!policy.RotationEnabled)
            reasons.Add("rotation disabled");
        if (policy.RequirePolicy && !request.PolicyPresent)
            reasons.Add("rotation policy missing");
        if (policy.RequireOwnerOrAdminApproval && (!request.ApprovalPresent || request.ActorRole is not (NexaRole.Owner or NexaRole.Admin)))
            reasons.Add("rotation owner/admin approval missing");
        if (policy.RequireAudit && !request.AuditEnabled)
            reasons.Add("rotation audit missing");
        if (policy.ExposeOldSecret || policy.ExposeNewSecret)
            reasons.Add("rotation cannot expose old or new secret");
        if (BrowserCredentialRedactor.ContainsSecret(request.ReasonRedacted))
            reasons.Add("rotation reason contains secret-like content");

        return new BrowserVaultRotationDecision(
            reasons.Count == 0 ? BrowserVaultLifecycleDecisionKind.Allowed : BrowserVaultLifecycleDecisionKind.Blocked,
            reasons,
            $"vault-rotation-audit-{Guid.NewGuid():N}",
            OldSecretExposed: false,
            NewSecretExposed: false,
            Redacted: true);
    }

    public BrowserVaultRecoveryDecision EvaluateRecovery(BrowserVaultRecoveryPolicy policy, BrowserVaultRecoveryRequest request)
    {
        var reasons = new List<string>();
        if (!policy.RecoveryEnabled)
            reasons.Add("recovery disabled");
        if (policy.RequireOwnerOrAdminApproval && (!request.ApprovalPresent || request.ActorRole is not (NexaRole.Owner or NexaRole.Admin)))
            reasons.Add("recovery owner/admin approval missing");
        if (!request.ProviderAvailable && policy.FailClosedWhenProviderUnavailable)
            reasons.Add("recovery provider unavailable");
        if (policy.RequireLocalMachineUserBinding && !request.LocalMachineUserBindingPresent)
            reasons.Add("local machine/user binding missing");
        if (!policy.AuditWithoutValue)
            reasons.Add("recovery audit must omit secret value");
        if (BrowserCredentialRedactor.ContainsSecret(request.ReasonRedacted))
            reasons.Add("recovery reason contains secret-like content");

        return new BrowserVaultRecoveryDecision(
            reasons.Count == 0 ? BrowserVaultLifecycleDecisionKind.Allowed : BrowserVaultLifecycleDecisionKind.FailClosed,
            reasons,
            $"vault-recovery-audit-{Guid.NewGuid():N}",
            SecretExposed: false,
            Redacted: true);
    }

    public BrowserVaultExportDecision EvaluateExport(BrowserVaultExportPolicy policy, BrowserVaultExportRequest request)
    {
        var reasons = new List<string>();
        if (policy.Mode == BrowserVaultExportMode.Disabled)
            reasons.Add("vault export disabled by default");
        if (policy.AllowCleartext || policy.Mode == BrowserVaultExportMode.Cleartext)
            reasons.Add("cleartext vault export blocked");
        if (policy.RequireEnterpriseControlled && request.ConfigurationProfile != NexaConfigurationProfileKind.EnterpriseControlled)
            reasons.Add("vault export requires enterprise controlled policy");
        if (policy.RequireStrongApproval && (!request.StrongApprovalPresent || request.ActorRole is not (NexaRole.Owner or NexaRole.Admin)))
            reasons.Add("vault export requires strong owner/admin approval");
        if (policy.RequireEncryptionPolicy && !request.EncryptionPolicyPresent)
            reasons.Add("vault export encryption policy missing");
        if (BrowserCredentialRedactor.ContainsSecret(request.ReasonRedacted))
            reasons.Add("vault export reason contains secret-like content");

        var manifest = new BrowserVaultExportManifest(
            $"vault-export-{Guid.NewGuid():N}",
            BrowserCredentialRedactor.Redact(request.Reference.SecretId),
            policy.Mode is BrowserVaultExportMode.Cleartext ? BrowserVaultExportMode.ManifestOnly : policy.Mode,
            NexaDiagnosticsHash.Sha256($"{request.Reference.SecretId}:{policy.Mode}:manifest"),
            ContainsRawSecret: false,
            Redacted: true);
        var allowed = reasons.Count == 0 && policy.Mode is BrowserVaultExportMode.ManifestOnly or BrowserVaultExportMode.DesignOnly;
        return new BrowserVaultExportDecision(
            allowed ? BrowserVaultLifecycleDecisionKind.DesignOnly : BrowserVaultLifecycleDecisionKind.Blocked,
            reasons,
            manifest,
            CleartextBlocked: true,
            RawSecretExposed: false,
            Redacted: true);
    }
}
