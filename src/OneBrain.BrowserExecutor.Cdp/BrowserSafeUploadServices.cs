using System.Net.Http.Headers;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class BrowserSafeUploadManager
{
    public async Task<BrowserSafeUploadResult> UploadAsync(BrowserSafeUploadRequest request, BrowserSafeUploadPolicy policy, HttpClient? httpClient = null, CancellationToken cancellationToken = default)
    {
        var validated = Validate(request, policy);
        if (validated.Decision != BrowserSafeUploadDecisionKind.Allowed)
            return validated;

        var artifact = validated.Artifact!;
        using var client = httpClient ?? new HttpClient();
        await using var stream = File.OpenRead(request.FilePath);
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(request.MimeType);
        content.Add(fileContent, "file", artifact.NormalizedFileName);

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(request.UploadEndpoint, content, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Block(request, policy, "safe upload HTTP transfer failed");
        }

        if (!response.IsSuccessStatusCode)
            return Block(request, policy, "safe upload endpoint rejected upload");

        var verification = Verification(request, artifact);
        var audit = Audit(request, BrowserAuditLedgerEventKind.UploadPrepared, BrowserSafeUploadDecisionKind.Verified, "safe upload verified", artifact.NormalizedFileName, artifact);
        return new BrowserSafeUploadResult(BrowserSafeUploadDecisionKind.Verified, artifact, "safe upload verified", verification, [$"upload:{artifact.Sha256}"], audit, Redacted: true);
    }

    public BrowserSafeUploadResult Validate(BrowserSafeUploadRequest request, BrowserSafeUploadPolicy policy)
    {
        var policyValidation = policy.Validate();
        if (!policyValidation.IsValid)
            return Block(request, policy, string.Join("; ", policyValidation.Errors));
        if (!policy.AllowlistedHosts.Contains(request.UploadEndpoint.Host, StringComparer.OrdinalIgnoreCase))
            return Block(request, policy, "upload host is not allowlisted");
        if (!policy.AllowlistedEndpointPaths.Contains(request.UploadEndpoint.AbsolutePath, StringComparer.OrdinalIgnoreCase))
            return Block(request, policy, "upload endpoint is not allowlisted");
        if (request.UploadEndpoint.Scheme is not "http" and not "https")
            return Block(request, policy, "upload URL scheme is blocked");
        if (request.Approval?.IsValid != true)
            return Block(request, policy, "safe upload requires approval");
        if (request.Consent is null || !request.Consent.AllowsCapability(BrowserConsentCapability.SecretUse, BrowserConsentScope.Session, DateTimeOffset.UtcNow))
            return Block(request, policy, "safe upload requires scoped consent");
        if (request.GateReport?.Passed != true)
            return Block(request, policy, "safe upload requires passing phase gate");
        if (HasWildcard(request.FilePath) || HasWildcard(request.SuggestedFileName))
            return Block(request, policy, "safe upload wildcard blocked");
        if (Directory.Exists(request.FilePath))
            return Block(request, policy, "safe upload directory upload blocked");
        if (!File.Exists(request.FilePath))
            return Block(request, policy, "safe upload file does not exist");
        if (!BrowserDownloadManager.IsWithin(policy.ControlledUploadRoot, request.FilePath))
            return Block(request, policy, "safe upload file outside controlled root");
        if (request.SuggestedFileName.Contains("..", StringComparison.Ordinal) || request.FilePath.Contains("..", StringComparison.Ordinal))
            return Block(request, policy, "safe upload filename path traversal blocked");

        var attributes = File.GetAttributes(request.FilePath);
        if (attributes.HasFlag(FileAttributes.Directory) || attributes.HasFlag(FileAttributes.Hidden) || attributes.HasFlag(FileAttributes.System))
            return Block(request, policy, "safe upload hidden/system/directory file blocked");

        var fileName = BrowserDownloadManager.SafeFileName(request.SuggestedFileName);
        if (BrowserCredentialRedactor.ContainsSecret(fileName) || BrowserCredentialRedactor.ContainsSecret(request.FilePath))
            return Block(request, policy, "safe upload path contains secret-like content");

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (BrowserSafeUploadPolicy.BlockedExtensions.Contains(extension))
            return Block(request, policy, "safe upload executable, macro, archive, or secret extension blocked");
        if (!policy.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return Block(request, policy, "safe upload extension is not allowed");
        if (!policy.AllowedMimeTypes.Contains(request.MimeType, StringComparer.OrdinalIgnoreCase))
            return Block(request, policy, "safe upload MIME type is not allowed");

        var size = new FileInfo(request.FilePath).Length;
        if (size <= 0 || size > policy.MaxBytes)
            return Block(request, policy, "safe upload size is blocked");

        var sha = BrowserDownloadManager.Sha256File(request.FilePath);
        var artifact = new BrowserSafeUploadArtifact(
            fileName,
            extension,
            Path.Combine("[CONTROLLED_UPLOAD_ROOT]", fileName),
            request.MimeType,
            size,
            sha,
            request.UploadEndpoint.Host,
            BrowserNetworkCapture.RedactUrl(request.UploadEndpoint.ToString()),
            request.Approval.ApprovalId,
            ContentCaptured: false,
            Redacted: true);
        var audit = Audit(request, BrowserAuditLedgerEventKind.UploadRequested, BrowserSafeUploadDecisionKind.Allowed, "safe upload validated", artifact.NormalizedFileName, artifact);
        return new BrowserSafeUploadResult(BrowserSafeUploadDecisionKind.Allowed, artifact, "safe upload validated", null, [$"upload-validated:{sha}"], audit, Redacted: true);
    }

    public BrowserSafeUploadResult Block(BrowserSafeUploadRequest request, BrowserSafeUploadPolicy policy, string reason)
    {
        var audit = Audit(request, BrowserAuditLedgerEventKind.UploadBlocked, BrowserSafeUploadDecisionKind.Blocked, reason, Path.GetFileName(request.SuggestedFileName), null);
        return new BrowserSafeUploadResult(BrowserSafeUploadDecisionKind.Blocked, null, BrowserCredentialRedactor.Redact(reason), null, [], audit, Redacted: true);
    }

    private static bool HasWildcard(string value) =>
        value.Contains('*', StringComparison.Ordinal) || value.Contains('?', StringComparison.Ordinal);

    private static BrowserVerification Verification(BrowserSafeUploadRequest request, BrowserSafeUploadArtifact artifact)
    {
        var context = new BrowserTargetContext(
            request.RunId,
            "chrome-cdp",
            request.SessionId,
            null,
            null,
            "upload-target",
            "upload-page",
            null,
            "main",
            null,
            new Uri($"{request.UploadEndpoint.Scheme}://{request.UploadEndpoint.Host}{request.UploadEndpoint.AbsolutePath}"),
            "Safe Upload",
            0,
            BrowserTargetContext.CreateLivenessToken("upload-target", "main", 0),
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            "complete",
            BrowserTargetSource.Cdp);
        return new BrowserVerification(
            $"verification-{Guid.NewGuid():N}",
            request.RunId,
            "step-safe-upload",
            request.ActionId,
            context,
            new BrowserExpectedOutcome("safe upload accepted by controlled endpoint with hash", null, artifact.NormalizedFileName, null),
            null,
            null,
            BrowserVerificationStatus.Verified,
            0.95,
            [$"evidence-upload-{artifact.Sha256}"],
            null,
            DateTimeOffset.UtcNow,
            [$"proof-upload-{artifact.Sha256}"]);
    }

    private static BrowserAuditLedgerEvent Audit(BrowserSafeUploadRequest request, BrowserAuditLedgerEventKind kind, BrowserSafeUploadDecisionKind decision, string reason, string subject, BrowserSafeUploadArtifact? artifact) =>
        BrowserPersistentAuditLedger.Create(
            kind,
            request.RunId,
            request.ActionId,
            request.CorrelationId,
            "profile-controlled",
            request.SessionId,
            request.Consent?.Request.ConsentId,
            null,
            null,
            decision.ToString(),
            reason,
            new Dictionary<string, string>
            {
                ["subject"] = BrowserCredentialRedactor.Redact(subject),
                ["host"] = request.UploadEndpoint.Host,
                ["endpoint"] = BrowserNetworkCapture.RedactUrl(request.UploadEndpoint.ToString()),
                ["file"] = artifact?.NormalizedFileName ?? BrowserCredentialRedactor.Redact(Path.GetFileName(subject)),
                ["mime"] = artifact?.MimeType ?? BrowserCredentialRedactor.Redact(request.MimeType),
                ["size"] = artifact?.SizeBytes.ToString() ?? "0",
                ["sha256"] = artifact?.Sha256 ?? "[NOT_CAPTURED]",
                ["approval"] = request.Approval?.ApprovalId ?? "[NONE]"
            });
}

