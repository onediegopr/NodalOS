namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserVaultLifecycleDecisionKind
{
    Allowed,
    Blocked,
    DesignOnly,
    FailClosed
}

public sealed record BrowserVaultRotationPolicy(
    bool RotationEnabled,
    bool RequirePolicy,
    bool RequireAudit,
    bool RequireOwnerOrAdminApproval,
    bool ExposeOldSecret,
    bool ExposeNewSecret);

public sealed record BrowserVaultRotationPolicyRequest(
    string RequestId,
    BrowserSecretReference Reference,
    NexaRole ActorRole,
    bool PolicyPresent,
    bool ApprovalPresent,
    bool AuditEnabled,
    string ReasonRedacted);

public sealed record BrowserVaultRotationDecision(
    BrowserVaultLifecycleDecisionKind Decision,
    IReadOnlyList<string> ReasonCodes,
    string RotationAuditRef,
    bool OldSecretExposed,
    bool NewSecretExposed,
    bool Redacted);

public sealed record BrowserVaultRecoveryPolicy(
    bool RecoveryEnabled,
    bool RequireOwnerOrAdminApproval,
    bool RequireLocalMachineUserBinding,
    bool FailClosedWhenProviderUnavailable,
    bool AuditWithoutValue);

public sealed record BrowserVaultRecoveryRequest(
    string RequestId,
    BrowserSecretReference Reference,
    NexaRole ActorRole,
    bool ApprovalPresent,
    bool ProviderAvailable,
    bool LocalMachineUserBindingPresent,
    string ReasonRedacted);

public sealed record BrowserVaultRecoveryDecision(
    BrowserVaultLifecycleDecisionKind Decision,
    IReadOnlyList<string> ReasonCodes,
    string RecoveryAuditRef,
    bool SecretExposed,
    bool Redacted);

public enum BrowserVaultExportMode
{
    Disabled,
    DesignOnly,
    ManifestOnly,
    EncryptedFuture,
    Cleartext
}

public sealed record BrowserVaultExportPolicy(
    BrowserVaultExportMode Mode,
    bool RequireEnterpriseControlled,
    bool RequireStrongApproval,
    bool RequireEncryptionPolicy,
    bool AllowCleartext);

public sealed record BrowserVaultExportRequest(
    string RequestId,
    BrowserSecretReference Reference,
    NexaConfigurationProfileKind ConfigurationProfile,
    NexaRole ActorRole,
    bool StrongApprovalPresent,
    bool EncryptionPolicyPresent,
    string ReasonRedacted);

public sealed record BrowserVaultExportManifest(
    string ExportId,
    string ReferenceId,
    BrowserVaultExportMode Mode,
    string Hash,
    bool ContainsRawSecret,
    bool Redacted);

public sealed record BrowserVaultExportDecision(
    BrowserVaultLifecycleDecisionKind Decision,
    IReadOnlyList<string> ReasonCodes,
    BrowserVaultExportManifest Manifest,
    bool CleartextBlocked,
    bool RawSecretExposed,
    bool Redacted);
