using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserTransferDecisionKind
{
    Allowed,
    Blocked,
    Started,
    Completed,
    Failed,
    RequiresHuman
}

public enum BrowserRuntimePhaseCloseStatus
{
    Passed,
    Failed,
    Blocked,
    RequiresAudit
}

public enum BrowserNetworkCaptureMode
{
    MetadataOnly
}

public enum BrowserNetworkHeaderRedactionReason
{
    None,
    SensitiveHeaderValueNotCaptured,
    PatternRedacted
}

public enum BrowserRuntimeProfileState
{
    None,
    Disposable,
    PersistentControlled,
    UserProfileBlocked,
    UserProfileControlledWithConsent,
    RawUserProfileActive
}

public enum BrowserRuntimeVaultState
{
    DesignOnly,
    MinimalSandboxActive,
    OsBackedMinimalActive,
    ProductionActive,
    ProductionExternalActive,
    UnknownProvider
}

public enum BrowserRuntimeExternalAuthState
{
    Disabled,
    LowRiskActive,
    SensitiveOrCriticalActive
}

public enum BrowserRuntimeDownloadState
{
    Disabled,
    SafeDownloadActive,
    UnsafeDownloadActive
}

public enum BrowserRuntimeUploadState
{
    Disabled,
    SafeUploadActive,
    UnsafeUploadActive
}

public enum BrowserRuntimeDocumentWorkflowState
{
    Disabled,
    SandboxActive,
    ExternalActive
}

public enum BrowserRuntimeRecorderState
{
    Disabled,
    DesignOnly,
    ReadOnlyPrototypeActive,
    ProductiveActive,
    ExecutableActive
}

public enum BrowserRuntimeReplayState
{
    Disabled,
    SafeModeReadOnlyActive,
    ProductiveActive,
    SensitiveActive
}

public sealed record BrowserRuntimeCapabilityState(
    string Name,
    bool Enabled,
    string EvidenceRef,
    string Details = "");

public sealed record BrowserRuntimeObservedState(
    bool CompanionAuthoritative,
    bool LegacyRunnerEnabled,
    bool RealProfileActive,
    bool RealVaultActive,
    bool LoginRealActive,
    BrowserNetworkCaptureMode NetworkCaptureMode,
    bool RequestBodyCaptureSupported,
    bool ResponseBodyCaptureSupported,
    bool SensitiveHeaderValueCaptureSupported,
    bool ReplayExecutableEnabled,
    string DownloadMode,
    string UploadMode,
    bool TargetFrameManagerHealthy,
    string AuditLedgerIntegrityProviderKind,
    bool AuditLedgerHeadSealAvailable,
    bool AuditLedgerHeadSealValid,
    bool CdpLiveProofAvailable,
    bool Browser004xLegacyIsolated,
    IReadOnlyList<BrowserRuntimeCapabilityState> Capabilities,
    BrowserRuntimeProfileState ProfileState = BrowserRuntimeProfileState.None,
    bool ControlledProfileConsentValid = false,
    BrowserRuntimeVaultState VaultState = BrowserRuntimeVaultState.DesignOnly,
    bool MinimalSandboxVaultConsentValid = false,
    bool VaultReturnsPublicValues = false,
    bool VaultCompanionExposure = false,
    bool VaultProviderKnown = true,
    bool OsBackedVaultProviderHealthy = false,
    bool OsBackedVaultPublicDtosReferenceOnly = true,
    bool ProductiveVaultFeatureControlledTestContext = false,
    BrowserRuntimeExternalAuthState ExternalAuthState = BrowserRuntimeExternalAuthState.Disabled,
    bool ExternalAuthConsentPolicyGateValid = false,
    bool ExternalAuthTargetLowRisk = false,
    bool ExternalAuthReadOnlyGuardActive = false,
    BrowserRuntimeDownloadState DownloadState = BrowserRuntimeDownloadState.Disabled,
    bool SafeDownloadAllowlistValid = false,
    bool SafeDownloadQuarantineEnabled = false,
    bool SafeDownloadHashRequired = false,
    bool SafeDownloadAutoOpenEnabled = false,
    bool SafeDownloadExecutableAllowed = false,
    bool SafeDownloadControlledRoot = true,
    BrowserRuntimeUploadState SafeUploadState = BrowserRuntimeUploadState.Disabled,
    bool SafeUploadAllowlistValid = false,
    bool SafeUploadApprovalPresent = false,
    bool SafeUploadControlledRoot = true,
    bool SafeUploadHashRequired = false,
    bool SafeUploadExecutableAllowed = false,
    bool SafeUploadWildcardAllowed = false,
    bool SafeUploadDirectoryAllowed = false,
    bool SafeUploadExposesContentOrPath = false,
    BrowserRuntimeDocumentWorkflowState DocumentWorkflowState = BrowserRuntimeDocumentWorkflowState.Disabled,
    bool ExternalDocumentWorkflowAllowed = false,
    bool DocumentWorkflowSandboxVerified = false,
    BrowserRuntimeRecorderState RecorderState = BrowserRuntimeRecorderState.Disabled,
    bool RecorderStoresSecrets = false,
    bool RecorderStoresCookies = false,
    bool RecorderStoresBodies = false,
    BrowserRuntimeReplayState ReplayState = BrowserRuntimeReplayState.Disabled,
    bool ReplayVerificationRequired = true,
    bool ReplayIdempotencyRequired = true,
    bool ReplaySupportsSensitiveActions = false,
    bool ReplaySupportsSubmitUploadPaymentDelete = false,
    bool SensitiveSitesPolicyDefined = false,
    bool SensitiveSiteReadOnlySimulationAllowed = false,
    bool SensitiveReadOnlySimulationActive = false,
    bool SensitiveDocumentSimulationActive = false,
    bool SensitiveSiteRealPilotActive = false,
    bool SensitiveDocumentRealActive = false,
    bool SensitiveSiteIrreversibleActionActive = false,
    bool SensitiveSiteSubmitEnabled = false,
    bool SensitiveSitePaymentEnabled = false,
    bool SensitiveSiteSigningEnabled = false,
    bool SensitiveDocumentContentCaptureEnabled = false,
    bool SensitiveAutomationCheckpointCompleted = false,
    bool SensitiveRealPilotDecisionApproved = false,
    bool ExternalLowRiskTargetAvailable = false,
    bool ProductTrackAllowed = false,
    bool ProductAdminFoundationDefined = false,
    bool LicensingFoundationDefined = false,
    bool FeatureFlagSensitiveRealPilotEnabled = false,
    bool FeatureFlagProductiveVaultEnabled = false,
    bool FeatureFlagReplayProductiveEnabled = false,
    bool FeatureFlagRecorderProductiveEnabled = false,
    bool ExpiredLicenseAttemptsExecution = false,
    bool AdminRuntimeServiceDefined = false,
    bool TenantGovernanceDefined = false,
    bool AuditExportDefined = false,
    bool AuditExportRedacted = true,
    bool CrossTenantIsolationEnabled = true,
    bool AuditExportLeaksSecrets = false,
    bool SupportCanViewSecrets = false,
    bool SensitiveFeatureEnabledWithoutTenantPolicy = false,
    bool PackagingFoundationDefined = false,
    bool DiagnosticsBundleDefined = false,
    bool SupportModeMetadataOnly = true,
    bool BillingMockDefined = false,
    bool OnboardingMockDefined = false,
    bool RealBillingDisabled = true,
    bool RealEmailDeliveryDisabled = true,
    bool PublicSaasActivationDisabled = true,
    bool SupportModeCanAccessSecrets = false,
    bool DiagnosticsBundleLeaksSecrets = false,
    bool BillingMockEnablesSensitiveRealPilotByDefault = false,
    bool ConfigurationProfilesDefined = false,
    bool ProductionLockedProfileDefined = false,
    bool ReleaseChannelsDefined = false,
    bool UpdateManifestDefined = false,
    bool AutoUpdateExecutionDisabled = true,
    bool RollbackModelDefined = false,
    bool UnknownConfigurationProfileActive = false,
    bool ProductionProfileEnablesSensitiveFeaturesByDefault = false,
    bool UpdateManifestMissingHashOrSignature = false,
    bool ReleaseChannelBypassesTenantPolicy = false,
    bool RollbackExecutesAutomatically = false,
    bool InstallerDryRunDefined = false,
    bool DeploymentPreflightDefined = false,
    bool RollbackDryRunDefined = false,
    bool InstallerModifiesRealSystem = false,
    bool DeploymentDryRunEnablesRealBillingEmailSaas = false,
    bool PublicApiBoundaryDefined = false,
    bool PublicApiDesignOnly = true,
    bool PublicApiNetworkExposureDisabled = true,
    bool PublicApiExposesSecretsCookiesBodies = false,
    bool PublicApiAllowsCrossTenantAccess = false,
    bool PublicApiBypassesLicensing = false,
    bool LocalProductShellDefined = false,
    bool LocalProductShellExposesSecrets = false,
    bool PreProductionCheckpointDefined = false,
    bool ProductAdminPrivatePreviewAllowed = false,
    bool PublicSaasStillDisabled = true,
    bool RealBillingStillDisabled = true,
    bool SensitiveRealPilotStillDisabled = true,
    bool ExternalAuditRecommended = false,
    bool RealDeployOrUpdateEnabled = false,
    bool AuditIntegrityKeyProviderConfigured = true,
    bool AuditIntegrityKeyProviderOsBacked = false,
    bool AuditIntegrityDefaultFailClosed = true,
    bool AuditIntegrityDevFixtureExplicitOnly = true,
    bool AuditIntegrityKeyHealthOk = true,
    bool AuditIntegrityDevFixtureInProduction = false,
    bool AuditLedgerHeadSealIncludesKeyId = true,
    bool AuditLedgerVerifiesUsingImplicitDevKey = false,
    bool ExternalReadOnlyTargetConfigured = false,
    bool ExternalReadOnlyTargetTestOwned = false,
    bool ExternalReadOnlyProofAvailable = false,
    bool ExternalReadOnlyProofBlocked = false,
    bool ExternalReadOnlyMetadataOnly = true,
    bool ExternalReadOnlyGuardActive = false,
    bool ExternalReadOnlyTargetSensitive = false,
    bool ExternalReadOnlyMutationAllowed = false,
    bool ExternalReadOnlyBrowserCleanupConfirmed = true)
{
    public bool UsesHmacLedgerIntegrity =>
        AuditLedgerIntegrityProviderKind.Contains("hmac", StringComparison.OrdinalIgnoreCase);

    public bool RawUserProfileActive =>
        ProfileState == BrowserRuntimeProfileState.RawUserProfileActive ||
        (RealProfileActive && ProfileState != BrowserRuntimeProfileState.UserProfileControlledWithConsent);

    public bool ControlledProfileAllowed =>
        ProfileState != BrowserRuntimeProfileState.UserProfileControlledWithConsent || ControlledProfileConsentValid;

    public bool ProductionVaultUnsafe =>
        VaultState is BrowserRuntimeVaultState.ProductionActive or BrowserRuntimeVaultState.ProductionExternalActive ||
        (RealVaultActive && VaultState is not BrowserRuntimeVaultState.MinimalSandboxActive and not BrowserRuntimeVaultState.OsBackedMinimalActive);

    public bool MinimalSandboxVaultAllowed =>
        VaultState != BrowserRuntimeVaultState.MinimalSandboxActive ||
        (MinimalSandboxVaultConsentValid && !VaultReturnsPublicValues && !VaultCompanionExposure && VaultProviderKnown);

    public bool VaultModeAllowed =>
        VaultProviderKnown &&
        VaultState != BrowserRuntimeVaultState.UnknownProvider &&
        !ProductionVaultUnsafe &&
        MinimalSandboxVaultAllowed &&
        (VaultState != BrowserRuntimeVaultState.OsBackedMinimalActive ||
         (OsBackedVaultProviderHealthy && OsBackedVaultPublicDtosReferenceOnly && ProductiveVaultFeatureControlledTestContext)) &&
        !VaultReturnsPublicValues &&
        !VaultCompanionExposure;

    public bool ExternalAuthAllowed =>
        ExternalAuthState != BrowserRuntimeExternalAuthState.SensitiveOrCriticalActive &&
        (ExternalAuthState != BrowserRuntimeExternalAuthState.LowRiskActive ||
         (ExternalAuthConsentPolicyGateValid && ExternalAuthTargetLowRisk && ExternalAuthReadOnlyGuardActive));

    public bool SafeDownloadAllowed =>
        DownloadState != BrowserRuntimeDownloadState.UnsafeDownloadActive &&
        (DownloadState != BrowserRuntimeDownloadState.SafeDownloadActive ||
         (SafeDownloadAllowlistValid &&
          SafeDownloadQuarantineEnabled &&
          SafeDownloadHashRequired &&
          !SafeDownloadAutoOpenEnabled &&
          !SafeDownloadExecutableAllowed &&
          SafeDownloadControlledRoot));

    public bool SafeUploadAllowed =>
        SafeUploadState != BrowserRuntimeUploadState.UnsafeUploadActive &&
        (SafeUploadState != BrowserRuntimeUploadState.SafeUploadActive ||
         (SafeUploadAllowlistValid &&
          SafeUploadApprovalPresent &&
          SafeUploadControlledRoot &&
          SafeUploadHashRequired &&
          !SafeUploadExecutableAllowed &&
          !SafeUploadWildcardAllowed &&
          !SafeUploadDirectoryAllowed &&
          !SafeUploadExposesContentOrPath));

    public bool DocumentWorkflowAllowed =>
        DocumentWorkflowState != BrowserRuntimeDocumentWorkflowState.ExternalActive &&
        (DocumentWorkflowState != BrowserRuntimeDocumentWorkflowState.SandboxActive || DocumentWorkflowSandboxVerified);

    public bool RecorderAllowed =>
        RecorderState != BrowserRuntimeRecorderState.ProductiveActive &&
        RecorderState != BrowserRuntimeRecorderState.ExecutableActive &&
        !RecorderStoresSecrets &&
        !RecorderStoresCookies &&
        !RecorderStoresBodies;

    public bool ReplayAllowed =>
        ReplayState != BrowserRuntimeReplayState.ProductiveActive &&
        ReplayState != BrowserRuntimeReplayState.SensitiveActive &&
        (ReplayState != BrowserRuntimeReplayState.SafeModeReadOnlyActive ||
         (ReplayVerificationRequired &&
          ReplayIdempotencyRequired &&
          !ReplaySupportsSensitiveActions &&
          !ReplaySupportsSubmitUploadPaymentDelete));

    public bool SensitiveSitesAllowed =>
        (!SensitiveSiteSurfaceActive || SensitiveSitesPolicyDefined) &&
        (!SensitiveSiteRealPilotActive || SensitiveRealPilotDecisionApproved) &&
        !SensitiveDocumentRealActive &&
        !SensitiveSiteIrreversibleActionActive &&
        !SensitiveSiteSubmitEnabled &&
        !SensitiveSitePaymentEnabled &&
        !SensitiveSiteSigningEnabled &&
        !SensitiveDocumentContentCaptureEnabled &&
        RecorderState != BrowserRuntimeRecorderState.ProductiveActive &&
        ReplayState != BrowserRuntimeReplayState.ProductiveActive &&
        !RequestBodyCaptureSupported &&
        !ResponseBodyCaptureSupported &&
        !SensitiveHeaderValueCaptureSupported;

    public bool ProductGovernanceAllowed =>
        (!ProductAdminFoundationDefined || LicensingFoundationDefined) &&
        (!LicensingFoundationDefined || ProductAdminFoundationDefined) &&
        (!FeatureFlagSensitiveRealPilotEnabled || SensitiveRealPilotDecisionApproved) &&
        (!FeatureFlagProductiveVaultEnabled ||
         (VaultState == BrowserRuntimeVaultState.OsBackedMinimalActive && ProductiveVaultFeatureControlledTestContext && OsBackedVaultProviderHealthy && OsBackedVaultPublicDtosReferenceOnly)) &&
        !FeatureFlagReplayProductiveEnabled &&
        !FeatureFlagRecorderProductiveEnabled &&
        !ExpiredLicenseAttemptsExecution;

    public bool TenantGovernanceAllowed =>
        (!AdminRuntimeServiceDefined || ProductAdminFoundationDefined) &&
        (!TenantGovernanceDefined || CrossTenantIsolationEnabled) &&
        (!AuditExportDefined || AuditExportRedacted) &&
        !AuditExportLeaksSecrets &&
        !SupportCanViewSecrets &&
        !SensitiveFeatureEnabledWithoutTenantPolicy;

    public bool PackagingBillingAllowed =>
        (!DiagnosticsBundleDefined || PackagingFoundationDefined) &&
        (!BillingMockDefined || OnboardingMockDefined) &&
        SupportModeMetadataOnly &&
        RealBillingDisabled &&
        RealEmailDeliveryDisabled &&
        PublicSaasActivationDisabled &&
        !SupportModeCanAccessSecrets &&
        !DiagnosticsBundleLeaksSecrets &&
        !BillingMockEnablesSensitiveRealPilotByDefault;

    public bool ReleaseConfigurationAllowed =>
        (!ProductionLockedProfileDefined || ConfigurationProfilesDefined) &&
        (!UpdateManifestDefined || ReleaseChannelsDefined) &&
        AutoUpdateExecutionDisabled &&
        !UnknownConfigurationProfileActive &&
        !ProductionProfileEnablesSensitiveFeaturesByDefault &&
        !UpdateManifestMissingHashOrSignature &&
        !ReleaseChannelBypassesTenantPolicy &&
        !RollbackExecutesAutomatically;

    public bool InstallerDeploymentAllowed =>
        (!InstallerDryRunDefined || (DeploymentPreflightDefined && RollbackDryRunDefined)) &&
        !InstallerModifiesRealSystem &&
        !DeploymentDryRunEnablesRealBillingEmailSaas;

    public bool PublicApiBoundaryAllowed =>
        (!PublicApiBoundaryDefined || (PublicApiDesignOnly && PublicApiNetworkExposureDisabled)) &&
        !PublicApiExposesSecretsCookiesBodies &&
        !PublicApiAllowsCrossTenantAccess &&
        !PublicApiBypassesLicensing;

    public bool LocalProductPreProductionAllowed =>
        (!LocalProductShellDefined || !LocalProductShellExposesSecrets) &&
        (!PreProductionCheckpointDefined || ExternalAuditRecommended) &&
        PublicSaasStillDisabled &&
        RealBillingStillDisabled &&
        SensitiveRealPilotStillDisabled &&
        !RealDeployOrUpdateEnabled;

    public bool AuditIntegrityKeyCustodyAllowed =>
        AuditIntegrityKeyProviderConfigured &&
        AuditIntegrityDefaultFailClosed &&
        AuditIntegrityDevFixtureExplicitOnly &&
        AuditIntegrityKeyHealthOk &&
        AuditLedgerHeadSealIncludesKeyId &&
        !AuditIntegrityDevFixtureInProduction &&
        !AuditLedgerVerifiesUsingImplicitDevKey;

    public bool ExternalReadOnlyAllowed =>
        (!ExternalReadOnlyTargetConfigured ||
         (ExternalReadOnlyTargetTestOwned &&
          ExternalReadOnlyProofAvailable &&
          !ExternalReadOnlyProofBlocked &&
          ExternalReadOnlyMetadataOnly &&
          ExternalReadOnlyGuardActive &&
          !ExternalReadOnlyTargetSensitive &&
          !ExternalReadOnlyMutationAllowed &&
          ExternalReadOnlyBrowserCleanupConfirmed &&
          AuditIntegrityKeyCustodyAllowed &&
          !RequestBodyCaptureSupported &&
          !ResponseBodyCaptureSupported &&
          !SensitiveHeaderValueCaptureSupported));

    private bool SensitiveSiteSurfaceActive =>
        SensitiveSitesPolicyDefined ||
        SensitiveSiteReadOnlySimulationAllowed ||
        SensitiveReadOnlySimulationActive ||
        SensitiveDocumentSimulationActive ||
        SensitiveSiteRealPilotActive ||
        SensitiveDocumentRealActive ||
        SensitiveSiteIrreversibleActionActive ||
        SensitiveSiteSubmitEnabled ||
        SensitiveSitePaymentEnabled ||
        SensitiveSiteSigningEnabled ||
        SensitiveDocumentContentCaptureEnabled ||
        SensitiveAutomationCheckpointCompleted ||
        SensitiveRealPilotDecisionApproved ||
        ExternalLowRiskTargetAvailable ||
        ProductTrackAllowed ||
        ProductAdminFoundationDefined ||
        LicensingFoundationDefined ||
        FeatureFlagSensitiveRealPilotEnabled ||
        FeatureFlagProductiveVaultEnabled ||
        FeatureFlagReplayProductiveEnabled ||
        FeatureFlagRecorderProductiveEnabled ||
        ExpiredLicenseAttemptsExecution ||
        AdminRuntimeServiceDefined ||
        TenantGovernanceDefined ||
        AuditExportDefined ||
        AuditExportLeaksSecrets ||
        SupportCanViewSecrets ||
        SensitiveFeatureEnabledWithoutTenantPolicy ||
        PackagingFoundationDefined ||
        DiagnosticsBundleDefined ||
        BillingMockDefined ||
        OnboardingMockDefined ||
        !RealBillingDisabled ||
        !RealEmailDeliveryDisabled ||
        !PublicSaasActivationDisabled ||
        SupportModeCanAccessSecrets ||
        DiagnosticsBundleLeaksSecrets ||
        BillingMockEnablesSensitiveRealPilotByDefault ||
        ConfigurationProfilesDefined ||
        ProductionLockedProfileDefined ||
        ReleaseChannelsDefined ||
        UpdateManifestDefined ||
        RollbackModelDefined ||
        !AutoUpdateExecutionDisabled ||
        UnknownConfigurationProfileActive ||
        ProductionProfileEnablesSensitiveFeaturesByDefault ||
        UpdateManifestMissingHashOrSignature ||
        ReleaseChannelBypassesTenantPolicy ||
        RollbackExecutesAutomatically ||
        InstallerDryRunDefined ||
        DeploymentPreflightDefined ||
        RollbackDryRunDefined ||
        InstallerModifiesRealSystem ||
        DeploymentDryRunEnablesRealBillingEmailSaas ||
        PublicApiBoundaryDefined ||
        !PublicApiDesignOnly ||
        !PublicApiNetworkExposureDisabled ||
        PublicApiExposesSecretsCookiesBodies ||
        PublicApiAllowsCrossTenantAccess ||
        PublicApiBypassesLicensing ||
        LocalProductShellDefined ||
        LocalProductShellExposesSecrets ||
        PreProductionCheckpointDefined ||
        ProductAdminPrivatePreviewAllowed ||
        !PublicSaasStillDisabled ||
        !RealBillingStillDisabled ||
        !SensitiveRealPilotStillDisabled ||
        ExternalAuditRecommended ||
        RealDeployOrUpdateEnabled ||
        !AuditIntegrityKeyProviderConfigured ||
        AuditIntegrityKeyProviderOsBacked ||
        !AuditIntegrityDefaultFailClosed ||
        !AuditIntegrityDevFixtureExplicitOnly ||
        !AuditIntegrityKeyHealthOk ||
        AuditIntegrityDevFixtureInProduction ||
        !AuditLedgerHeadSealIncludesKeyId ||
        AuditLedgerVerifiesUsingImplicitDevKey ||
        ExternalReadOnlyTargetConfigured ||
        ExternalReadOnlyTargetTestOwned ||
        ExternalReadOnlyProofAvailable ||
        ExternalReadOnlyProofBlocked ||
        !ExternalReadOnlyMetadataOnly ||
        ExternalReadOnlyGuardActive ||
        ExternalReadOnlyTargetSensitive ||
        ExternalReadOnlyMutationAllowed ||
        !ExternalReadOnlyBrowserCleanupConfirmed;
}

public sealed record BrowserRuntimePhaseGateProbeResult(
    BrowserRuntimeObservedState ObservedState,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> AuditRefs,
    IReadOnlyList<string> Warnings);

public interface IBrowserRuntimeSecurityProbe
{
    BrowserRuntimePhaseGateProbeResult Probe();
}

public sealed record BrowserDownloadPolicy(
    string ControlledDirectory,
    IReadOnlySet<string> AllowedExtensions,
    long MaxBytes,
    bool RequireHash,
    bool RequireEvidence,
    bool AllowAutoOpen)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(ControlledDirectory))
            errors.Add("ControlledDirectory is required.");
        if (AllowedExtensions.Count == 0)
            errors.Add("AllowedExtensions is required.");
        if (MaxBytes <= 0)
            errors.Add("MaxBytes must be positive.");
        if (AllowAutoOpen)
            errors.Add("Downloads cannot auto-open files.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserDownloadRequest(
    string RunId,
    string ActionId,
    string CorrelationId,
    string SessionId,
    string SourceUrl,
    string SuggestedFileName,
    string MimeType,
    long? ExpectedBytes);

public sealed record BrowserDownloadArtifact(
    string FileName,
    string Extension,
    string ControlledPath,
    string MimeType,
    long SizeBytes,
    string Sha256,
    bool Quarantined,
    bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(FileName, nameof(FileName), errors);
        if (BrowserCredentialRedactor.ContainsSecret(ControlledPath) || BrowserCredentialRedactor.ContainsSecret(MimeType))
            errors.Add("Download artifact contains secret-like content.");
        if (string.IsNullOrWhiteSpace(Sha256))
            errors.Add("Download artifact hash is required.");
        if (!Redacted)
            errors.Add("Download artifact must be redacted.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserDownloadResult(
    BrowserTransferDecisionKind Status,
    BrowserDownloadArtifact? Artifact,
    string Reason,
    IReadOnlyList<string> EvidenceRefs,
    BrowserAuditLedgerEvent AuditEvent,
    bool Redacted)
{
    public bool IsSuccess => Status == BrowserTransferDecisionKind.Completed &&
        Artifact is not null &&
        Artifact.Validate().IsValid &&
        EvidenceRefs.Count > 0 &&
        Redacted;
}

public sealed record BrowserUploadPolicy(
    string SandboxDirectory,
    IReadOnlySet<string> AllowedExtensions,
    long MaxBytes,
    bool RequireApproval,
    bool AllowExternalTargets);

public sealed record BrowserUploadRequest(
    string RunId,
    string ActionId,
    string CorrelationId,
    string SessionId,
    string FilePath,
    string TargetDescription,
    bool HasApproval);

public sealed record BrowserUploadArtifact(
    string FileName,
    string Extension,
    string RedactedPath,
    long SizeBytes,
    string Sha256,
    bool Redacted);

public sealed record BrowserUploadResult(
    BrowserTransferDecisionKind Status,
    BrowserUploadArtifact? Artifact,
    string Reason,
    IReadOnlyList<string> EvidenceRefs,
    BrowserAuditLedgerEvent AuditEvent,
    bool Redacted)
{
    public bool IsSuccess => Status == BrowserTransferDecisionKind.Completed &&
        Artifact is not null &&
        EvidenceRefs.Count > 0 &&
        Redacted;
}

public sealed record BrowserNetworkCapturePolicy(
    BrowserNetworkCaptureMode Mode,
    bool CaptureSensitiveHeaderPresenceOnly,
    bool AllowDirectHttpReplay,
    IReadOnlySet<string> AllowedMethods)
{
    public bool BodiesCaptureSupported => false;
    public bool RequestBodyCaptureSupported => false;
    public bool ResponseBodyCaptureSupported => false;
}

public sealed record BrowserNetworkHeaderMetadata(
    string HeaderName,
    bool Present,
    bool ValueCaptured,
    string? Value,
    BrowserNetworkHeaderRedactionReason RedactionReason)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (BrowserCredentialRedactor.ContainsSecret(HeaderName))
            errors.Add("Header name contains secret-like content.");
        if (!ValueCaptured && Value is not null and not "[NOT_CAPTURED]")
            errors.Add("Non-captured header value must be null or [NOT_CAPTURED].");
        if (ValueCaptured && BrowserCredentialRedactor.ContainsSecret(Value))
            errors.Add("Header value contains secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserNetworkCaptureEvent(
    string RequestId,
    string CorrelationId,
    string Method,
    string RedactedUrl,
    int? StatusCode,
    string ResourceType,
    TimeSpan Duration,
    IReadOnlyList<BrowserNetworkHeaderMetadata> ResponseHeaders,
    bool ApiCandidate,
    bool RequestBodyCaptured,
    bool ResponseBodyCaptured,
    bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(RequestId, nameof(RequestId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(CorrelationId, nameof(CorrelationId), errors);
        if (RequestBodyCaptured || ResponseBodyCaptured)
            errors.Add("Network capture cannot include bodies by default.");
        if (!Redacted)
            errors.Add("Network capture must be redacted.");
        if (BrowserCredentialRedactor.ContainsSecret(RedactedUrl) ||
            ResponseHeaders.Any(header => !header.Validate().IsValid))
            errors.Add("Network capture contains secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserNetworkCaptureSummary(
    IReadOnlyList<BrowserNetworkCaptureEvent> Events,
    BrowserAuditLedgerEvent AuditEvent,
    bool Redacted)
{
    public bool MetadataOnly => Events.All(e => !e.RequestBodyCaptured && !e.ResponseBodyCaptured);
    public bool IsSafe => Redacted && MetadataOnly && Events.All(e => e.Validate().IsValid);
}

public sealed record BrowserSessionReplayStep(
    string StepId,
    string State,
    string Action,
    string VerificationStatus,
    IReadOnlyList<string> EvidenceRefs,
    bool DiagnosticOnly,
    bool WouldExecuteSensitiveAction);

public sealed record BrowserSessionReplayManifest(
    string RunId,
    string CorrelationId,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<BrowserSessionReplayStep> Steps,
    IReadOnlyList<BrowserAuditLedgerEvent> AuditEvents,
    IReadOnlyList<BrowserNetworkCaptureEvent> NetworkEvents,
    IReadOnlyList<BrowserDownloadArtifact> Downloads,
    IReadOnlyList<BrowserUploadArtifact> Uploads,
    string RuntimeKind,
    bool Redacted,
    bool DiagnosticOnly)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(RunId, nameof(RunId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(CorrelationId, nameof(CorrelationId), errors);
        if (!Redacted)
            errors.Add("Replay manifest must be redacted.");
        if (!DiagnosticOnly)
            errors.Add("Replay manifest must be diagnostic-only.");
        if (Steps.Any(step => !step.DiagnosticOnly || step.WouldExecuteSensitiveAction))
            errors.Add("Replay steps cannot execute actions.");
        if (NetworkEvents.Any(e => !e.Validate().IsValid))
            errors.Add("Replay network metadata is unsafe.");
        if (AuditEvents.Any(e => !e.Validate().IsValid))
            errors.Add("Replay audit events are unsafe.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }

    public string ComputeHash()
    {
        var json = JsonSerializer.Serialize(this);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }
}

public sealed record BrowserSessionExportPackage(
    BrowserSessionReplayManifest Manifest,
    string ManifestHash,
    bool Redacted,
    bool DiagnosticOnly)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        errors.AddRange(Manifest.Validate().Errors);
        if (!Redacted || !DiagnosticOnly)
            errors.Add("Export package must be redacted and diagnostic-only.");
        if (ManifestHash != Manifest.ComputeHash())
            errors.Add("Manifest hash mismatch.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserRuntimePhaseCloseReport(
    BrowserRuntimePhaseCloseStatus Status,
    string Summary,
    IReadOnlyList<string> PassedChecks,
    IReadOnlyList<string> FailedChecks,
    bool AuditLedgerOk,
    bool DownloadOk,
    bool UploadOk,
    bool NetworkMetadataOnlyOk,
    bool ReplayDiagnosticOnlyOk,
    bool CompanionNonAuthoritative,
    bool ServiceWorkerNotBrain,
    bool NoRealProfile,
    bool NoRealVault,
    bool NoLoginReal,
    BrowserAuditLedgerEvent AuditEvent,
    BrowserRuntimeObservedState? ObservedState = null,
    IReadOnlyList<string>? Warnings = null,
    IReadOnlyList<string>? EvidenceRefs = null,
    IReadOnlyList<string>? AuditRefs = null,
    string RecommendedNextAction = "")
{
    public bool Passed => Status == BrowserRuntimePhaseCloseStatus.Passed && FailedChecks.Count == 0;
}
