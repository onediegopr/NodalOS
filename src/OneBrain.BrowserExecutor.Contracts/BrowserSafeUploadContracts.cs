namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserSafeUploadDecisionKind
{
    Allowed,
    Blocked,
    Failed,
    Verified,
    RequiresHuman,
    RequiresApproval
}

public sealed record BrowserSafeUploadApproval(
    string ApprovalId,
    string ApprovedBy,
    DateTimeOffset ApprovedAtUtc,
    bool Authoritative,
    bool Redacted)
{
    public bool IsValid => Authoritative && Redacted && !BrowserCredentialRedactor.ContainsSecret(ApprovalId) && !BrowserCredentialRedactor.ContainsSecret(ApprovedBy);
}

public sealed record BrowserSafeUploadPolicy(
    string ControlledUploadRoot,
    IReadOnlySet<string> AllowlistedHosts,
    IReadOnlySet<string> AllowlistedEndpointPaths,
    IReadOnlySet<string> AllowedExtensions,
    IReadOnlySet<string> AllowedMimeTypes,
    long MaxBytes,
    bool RequireHash,
    bool RequireApproval,
    bool AllowWildcardUpload,
    bool AllowDirectoryUpload)
{
    public static IReadOnlySet<string> BlockedExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".msi", ".bat", ".cmd", ".ps1", ".vbs", ".js", ".scr", ".com", ".dll", ".zip", ".rar", ".7z", ".docm", ".xlsm", ".env", ".pfx", ".key", ".pem"
    };

    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(ControlledUploadRoot))
            errors.Add("ControlledUploadRoot is required.");
        if (AllowlistedHosts.Count == 0)
            errors.Add("At least one upload host must be allowlisted.");
        if (AllowlistedEndpointPaths.Count == 0)
            errors.Add("At least one upload endpoint path must be allowlisted.");
        if (AllowedExtensions.Count == 0)
            errors.Add("AllowedExtensions is required.");
        if (AllowedExtensions.Any(BlockedExtensions.Contains))
            errors.Add("Executable, macro, archive, or secret-bearing extensions cannot be uploaded in M27.");
        if (AllowedMimeTypes.Count == 0)
            errors.Add("AllowedMimeTypes is required.");
        if (MaxBytes <= 0)
            errors.Add("MaxBytes must be positive.");
        if (!RequireHash)
            errors.Add("Safe upload requires hash.");
        if (!RequireApproval)
            errors.Add("Safe upload requires approval.");
        if (AllowWildcardUpload)
            errors.Add("Safe upload cannot allow wildcard uploads.");
        if (AllowDirectoryUpload)
            errors.Add("Safe upload cannot allow directory uploads.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserSafeUploadRequest(
    string RunId,
    string ActionId,
    string CorrelationId,
    string SessionId,
    Uri UploadEndpoint,
    string FilePath,
    string SuggestedFileName,
    string MimeType,
    BrowserConsentGrant? Consent,
    BrowserRuntimePhaseCloseReport? GateReport,
    BrowserSafeUploadApproval? Approval);

public sealed record BrowserSafeUploadArtifact(
    string NormalizedFileName,
    string Extension,
    string RedactedPath,
    string MimeType,
    long SizeBytes,
    string Sha256,
    string TargetHost,
    string TargetPath,
    string ApprovalRef,
    bool ContentCaptured,
    bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(NormalizedFileName, nameof(NormalizedFileName), errors);
        if (BrowserCredentialRedactor.ContainsSecret(RedactedPath) || BrowserCredentialRedactor.ContainsSecret(TargetPath))
            errors.Add("Upload artifact contains secret-like content.");
        if (ContentCaptured)
            errors.Add("Upload artifact cannot capture file content.");
        if (string.IsNullOrWhiteSpace(Sha256))
            errors.Add("Upload artifact hash is required.");
        if (!Redacted)
            errors.Add("Upload artifact must be redacted.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserSafeUploadResult(
    BrowserSafeUploadDecisionKind Decision,
    BrowserSafeUploadArtifact? Artifact,
    string Reason,
    BrowserVerification? Verification,
    IReadOnlyList<string> EvidenceRefs,
    BrowserAuditLedgerEvent AuditEvent,
    bool Redacted)
{
    public bool AllowsDone =>
        Decision == BrowserSafeUploadDecisionKind.Verified &&
        Artifact?.Validate().IsValid == true &&
        Verification?.AllowsStepDone() == true &&
        EvidenceRefs.Count > 0 &&
        Redacted;
}

