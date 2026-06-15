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
    ProductionActive,
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
    bool SensitiveSiteRealPilotActive = false,
    bool SensitiveSiteIrreversibleActionActive = false,
    bool SensitiveSiteSubmitEnabled = false,
    bool SensitiveSitePaymentEnabled = false,
    bool SensitiveSiteSigningEnabled = false)
{
    public bool UsesHmacLedgerIntegrity =>
        AuditLedgerIntegrityProviderKind.Contains("hmac", StringComparison.OrdinalIgnoreCase);

    public bool RawUserProfileActive =>
        ProfileState == BrowserRuntimeProfileState.RawUserProfileActive ||
        (RealProfileActive && ProfileState != BrowserRuntimeProfileState.UserProfileControlledWithConsent);

    public bool ControlledProfileAllowed =>
        ProfileState != BrowserRuntimeProfileState.UserProfileControlledWithConsent || ControlledProfileConsentValid;

    public bool ProductionVaultUnsafe =>
        VaultState == BrowserRuntimeVaultState.ProductionActive ||
        (RealVaultActive && VaultState != BrowserRuntimeVaultState.MinimalSandboxActive);

    public bool MinimalSandboxVaultAllowed =>
        VaultState != BrowserRuntimeVaultState.MinimalSandboxActive ||
        (MinimalSandboxVaultConsentValid && !VaultReturnsPublicValues && !VaultCompanionExposure && VaultProviderKnown);

    public bool VaultModeAllowed =>
        VaultProviderKnown &&
        VaultState != BrowserRuntimeVaultState.UnknownProvider &&
        !ProductionVaultUnsafe &&
        MinimalSandboxVaultAllowed &&
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
        !SensitiveSiteRealPilotActive &&
        !SensitiveSiteIrreversibleActionActive &&
        !SensitiveSiteSubmitEnabled &&
        !SensitiveSitePaymentEnabled &&
        !SensitiveSiteSigningEnabled &&
        RecorderState != BrowserRuntimeRecorderState.ProductiveActive &&
        ReplayState != BrowserRuntimeReplayState.ProductiveActive &&
        !RequestBodyCaptureSupported &&
        !ResponseBodyCaptureSupported &&
        !SensitiveHeaderValueCaptureSupported;

    private bool SensitiveSiteSurfaceActive =>
        SensitiveSitesPolicyDefined ||
        SensitiveSiteReadOnlySimulationAllowed ||
        SensitiveSiteRealPilotActive ||
        SensitiveSiteIrreversibleActionActive ||
        SensitiveSiteSubmitEnabled ||
        SensitiveSitePaymentEnabled ||
        SensitiveSiteSigningEnabled;
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
