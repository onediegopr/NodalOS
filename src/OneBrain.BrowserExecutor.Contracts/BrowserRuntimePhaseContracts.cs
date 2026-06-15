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
    bool CaptureBodies,
    bool CaptureSensitiveHeaders,
    bool AllowDirectHttpReplay,
    IReadOnlySet<string> AllowedMethods);

public sealed record BrowserNetworkCaptureEvent(
    string RequestId,
    string CorrelationId,
    string Method,
    string RedactedUrl,
    int? StatusCode,
    string ResourceType,
    TimeSpan Duration,
    IReadOnlyDictionary<string, string> ResponseHeaders,
    bool ApiCandidate,
    bool BodyCaptured,
    bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(RequestId, nameof(RequestId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(CorrelationId, nameof(CorrelationId), errors);
        if (BodyCaptured)
            errors.Add("Network capture cannot include bodies by default.");
        if (!Redacted)
            errors.Add("Network capture must be redacted.");
        if (BrowserCredentialRedactor.ContainsSecret(RedactedUrl) ||
            ResponseHeaders.Any(pair => BrowserCredentialRedactor.ContainsSecret(pair.Key) || BrowserCredentialRedactor.ContainsSecret(pair.Value)))
            errors.Add("Network capture contains secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserNetworkCaptureSummary(
    IReadOnlyList<BrowserNetworkCaptureEvent> Events,
    BrowserAuditLedgerEvent AuditEvent,
    bool Redacted)
{
    public bool MetadataOnly => Events.All(e => !e.BodyCaptured);
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
    BrowserAuditLedgerEvent AuditEvent)
{
    public bool Passed => Status == BrowserRuntimePhaseCloseStatus.Passed && FailedChecks.Count == 0;
}
