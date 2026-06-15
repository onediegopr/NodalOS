namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserSafeDownloadDecisionKind
{
    Allowed,
    Blocked,
    Failed,
    Verified,
    RequiresHuman
}

public sealed record BrowserSafeDownloadPolicy(
    string ControlledDownloadRoot,
    IReadOnlySet<string> AllowlistedHosts,
    IReadOnlySet<string> AllowedExtensions,
    IReadOnlySet<string> AllowedMimeTypes,
    long MaxBytes,
    bool RequireHash,
    bool RequireQuarantine,
    bool AllowAutoOpen,
    bool AllowExecution)
{
    public static IReadOnlySet<string> BlockedExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".msi", ".bat", ".cmd", ".ps1", ".vbs", ".js", ".scr", ".com", ".dll", ".zip", ".rar", ".7z", ".docm", ".xlsm"
    };

    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(ControlledDownloadRoot))
            errors.Add("ControlledDownloadRoot is required.");
        if (AllowlistedHosts.Count == 0)
            errors.Add("At least one download host must be allowlisted.");
        if (AllowedExtensions.Count == 0)
            errors.Add("AllowedExtensions is required.");
        if (AllowedExtensions.Any(BlockedExtensions.Contains))
            errors.Add("Executable or archive extensions cannot be allowed in M26.");
        if (AllowedMimeTypes.Count == 0)
            errors.Add("AllowedMimeTypes is required.");
        if (MaxBytes <= 0)
            errors.Add("MaxBytes must be positive.");
        if (!RequireHash)
            errors.Add("Safe download requires hash.");
        if (!RequireQuarantine)
            errors.Add("Safe download requires quarantine.");
        if (AllowAutoOpen)
            errors.Add("Safe download cannot auto-open.");
        if (AllowExecution)
            errors.Add("Safe download cannot execute files.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserSafeDownloadRequest(
    string RunId,
    string ActionId,
    string CorrelationId,
    string SessionId,
    Uri SourceUri,
    string SuggestedFileName,
    string MimeType,
    long? ExpectedBytes);

public sealed record BrowserSafeDownloadArtifact(
    string NormalizedFileName,
    string Extension,
    string QuarantinePath,
    string MimeType,
    long SizeBytes,
    string Sha256,
    string SourceHost,
    string SourcePath,
    bool Quarantined,
    bool AutoOpened,
    bool Executed,
    bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(NormalizedFileName, nameof(NormalizedFileName), errors);
        if (!Quarantined)
            errors.Add("Downloaded artifact must be quarantined.");
        if (AutoOpened || Executed)
            errors.Add("Downloaded artifact cannot auto-open or execute.");
        if (string.IsNullOrWhiteSpace(Sha256))
            errors.Add("Downloaded artifact hash is required.");
        if (!Redacted)
            errors.Add("Downloaded artifact must be redacted.");
        if (BrowserCredentialRedactor.ContainsSecret(QuarantinePath) ||
            BrowserCredentialRedactor.ContainsSecret(SourcePath))
            errors.Add("Downloaded artifact contains secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserSafeDownloadResult(
    BrowserSafeDownloadDecisionKind Decision,
    BrowserSafeDownloadArtifact? Artifact,
    string Reason,
    BrowserVerification? Verification,
    IReadOnlyList<string> EvidenceRefs,
    BrowserAuditLedgerEvent AuditEvent,
    bool Redacted)
{
    public bool AllowsDone =>
        Decision == BrowserSafeDownloadDecisionKind.Verified &&
        Artifact?.Validate().IsValid == true &&
        Verification?.AllowsStepDone() == true &&
        EvidenceRefs.Count > 0 &&
        Redacted;
}
