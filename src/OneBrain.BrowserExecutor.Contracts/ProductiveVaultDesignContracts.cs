namespace OneBrain.BrowserExecutor.Contracts;

public enum ProductiveVaultDesignProviderKind
{
    DpapiCurrentUser,
    DpapiLocalMachine,
    WindowsCredentialManager,
    OsBackedEncryptedFile,
    ExternalVaultFuture,
    Unknown
}

public enum ProductiveVaultDesignReadiness
{
    DesignOnly,
    Blocked,
    FutureCandidate
}

public sealed record ProductiveVaultStorageOption(
    ProductiveVaultDesignProviderKind ProviderKind,
    ProductiveVaultDesignReadiness Readiness,
    string Advantages,
    string Risks,
    string Complexity,
    string MultiUserModel,
    string Portability,
    string BackupRestore,
    string Rotation,
    string DiskCopyRisk,
    string SameUserProcessRisk,
    string AuditIntegration)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (Readiness != ProductiveVaultDesignReadiness.DesignOnly &&
            Readiness != ProductiveVaultDesignReadiness.FutureCandidate)
            errors.Add("Productive vault storage options must remain design-only or future-candidate.");
        if (BrowserCredentialRedactor.ContainsSecret(Advantages) ||
            BrowserCredentialRedactor.ContainsSecret(Risks) ||
            BrowserCredentialRedactor.ContainsSecret(AuditIntegration))
            errors.Add("Productive vault design option contains secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record ProductiveVaultDesign(
    string DesignId,
    bool RealVaultEnabled,
    bool ReturnsSecretValues,
    bool CompanionCanReceiveSecretValues,
    bool RequiresScopedAccessPolicy,
    bool DocumentsRotation,
    bool DocumentsRevocation,
    IReadOnlyList<ProductiveVaultStorageOption> StorageOptions,
    IReadOnlyList<string> Guardrails)
{
    public static ProductiveVaultDesign Current => new(
        DesignId: "productive-vault-design-m20",
        RealVaultEnabled: false,
        ReturnsSecretValues: false,
        CompanionCanReceiveSecretValues: false,
        RequiresScopedAccessPolicy: true,
        DocumentsRotation: true,
        DocumentsRevocation: true,
        StorageOptions:
        [
            new(ProductiveVaultDesignProviderKind.DpapiCurrentUser, ProductiveVaultDesignReadiness.DesignOnly, "Strong Windows user binding and simple local deployment.", "Same-user malware can request decrypt operations; machine/user migration is hard.", "Medium", "Per Windows user.", "Low without explicit migration.", "Requires export/re-encrypt flow and emergency recovery policy.", "Reference rotation requires re-protecting material and audit.", "Disk copy alone is insufficient without user context, but profile compromise remains high impact.", "Processes under same user remain a risk boundary.", "Audit only references, provider kind, scope, consent, and decision."),
            new(ProductiveVaultDesignProviderKind.DpapiLocalMachine, ProductiveVaultDesignReadiness.DesignOnly, "Works for service-style local agents.", "Any authorized local machine context may become too broad.", "Medium", "Machine-wide with strict ACL requirement.", "Low; machine-bound.", "Requires machine recovery key and re-key plan.", "Rotation must include ACL review.", "Disk copy alone is insufficient, but machine compromise exposes broader scope.", "Other privileged processes are a major risk.", "Audit must bind machine, process, tenant, profile, and session."),
            new(ProductiveVaultDesignProviderKind.WindowsCredentialManager, ProductiveVaultDesignReadiness.DesignOnly, "Native user-facing credential storage and OS policy integration.", "Credential enumeration and UI consent semantics need hardening.", "Medium", "Per user with OS credential scopes.", "Medium across Windows profile backup scenarios.", "Can align with Windows account backup if allowed by policy.", "Rotation can update credential records by reference.", "Disk copy risk depends on Windows credential protection.", "Same-user process access must be constrained by policy and audit.", "Audit stores credential target reference, never value."),
            new(ProductiveVaultDesignProviderKind.OsBackedEncryptedFile, ProductiveVaultDesignReadiness.DesignOnly, "Portable format and explicit manifest control.", "Key custody becomes the main hard problem.", "High", "Tenant/company/person scopes need explicit key hierarchy.", "Medium if keys are portable by design.", "Requires encrypted backups, restore challenge, and revoke semantics.", "Rotation requires key wrapping and manifest migration.", "Disk copy risk depends entirely on key handling.", "Same-user process risk depends on local key access.", "Audit must seal manifest hash and key reference only."),
            new(ProductiveVaultDesignProviderKind.ExternalVaultFuture, ProductiveVaultDesignReadiness.FutureCandidate, "Best central governance for enterprise deployments.", "Introduces network dependency, tenant isolation, and availability risks.", "High", "External tenant/role based.", "High by provider design.", "Provider-specific backup/restore and disaster recovery.", "Provider-native rotation possible.", "Disk copy does not include vault material.", "Local process can still request access if policy is weak.", "Audit must correlate external audit ids with local HMAC ledger.")
        ],
        Guardrails:
        [
            "reference-only end-to-end",
            "core authority",
            "companion never sees secret values",
            "unknown secret fails closed",
            "consent does not equal authorization",
            "retrieval is scoped and audited",
            "values never appear in logs/evidence/protocol/UI",
            "rotation and revocation are mandatory before activation",
            "no automatic login without policy"
        ]);

    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(DesignId, nameof(DesignId), errors);
        if (RealVaultEnabled)
            errors.Add("M20 design must not enable a real productive vault.");
        if (ReturnsSecretValues)
            errors.Add("M20 design must not return secret values.");
        if (CompanionCanReceiveSecretValues)
            errors.Add("Companion must never receive secret values.");
        if (!RequiresScopedAccessPolicy)
            errors.Add("Productive vault design requires scoped access policy.");
        if (!DocumentsRotation || !DocumentsRevocation)
            errors.Add("Productive vault design must document rotation and revocation.");
        if (StorageOptions.Count < 5)
            errors.Add("Productive vault design must compare all required storage candidates.");
        foreach (var option in StorageOptions)
            errors.AddRange(option.Validate().Errors);
        if (!Guardrails.Any(g => g.Contains("fail", StringComparison.OrdinalIgnoreCase)) &&
            !Guardrails.Any(g => g.Contains("unknown secret", StringComparison.OrdinalIgnoreCase)))
            errors.Add("Productive vault design must include fail-closed guardrails.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record ProductiveVaultDesignDecision(
    BrowserProductiveVaultProviderKind ProviderKind,
    BrowserProductiveVaultDecisionKind Decision,
    string Message,
    bool SecretValueReturned)
{
    public bool IsFailClosed => Decision is BrowserProductiveVaultDecisionKind.FailClosed or BrowserProductiveVaultDecisionKind.Unsupported or BrowserProductiveVaultDecisionKind.Unconfigured;
}

public sealed class ProductiveVaultDesignGuard
{
    public ProductiveVaultDesignDecision EvaluateProvider(BrowserProductiveVaultProviderKind providerKind) =>
        providerKind switch
        {
            BrowserProductiveVaultProviderKind.Null => new(providerKind, BrowserProductiveVaultDecisionKind.Denied, "null vault remains default deny", SecretValueReturned: false),
            BrowserProductiveVaultProviderKind.WindowsDpapi => new(providerKind, BrowserProductiveVaultDecisionKind.FailClosed, "DPAPI is design-only until real key custody, consent UI, and audit activation are complete", SecretValueReturned: false),
            BrowserProductiveVaultProviderKind.WindowsCredentialManager => new(providerKind, BrowserProductiveVaultDecisionKind.FailClosed, "Windows Credential Manager is design-only until activation preconditions are complete", SecretValueReturned: false),
            BrowserProductiveVaultProviderKind.ExternalVaultFuture => new(providerKind, BrowserProductiveVaultDecisionKind.Unsupported, "external vault is a future option only", SecretValueReturned: false),
            BrowserProductiveVaultProviderKind.Unsupported => new(providerKind, BrowserProductiveVaultDecisionKind.Unsupported, "unsupported vault provider fails closed", SecretValueReturned: false),
            BrowserProductiveVaultProviderKind.InMemoryTestOnly => new(providerKind, BrowserProductiveVaultDecisionKind.Denied, "test-only vault is not productive vault", SecretValueReturned: false),
            _ => new(providerKind, BrowserProductiveVaultDecisionKind.FailClosed, "unknown vault provider fails closed", SecretValueReturned: false)
        };
}
