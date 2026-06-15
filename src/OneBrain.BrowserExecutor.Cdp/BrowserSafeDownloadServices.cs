using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class BrowserSafeDownloadManager
{
    public BrowserSafeDownloadResult ValidateMaterializedDownload(BrowserSafeDownloadRequest request, BrowserSafeDownloadPolicy policy, string materializedFilePath)
    {
        var policyValidation = policy.Validate();
        if (!policyValidation.IsValid)
            return Block(request, policy, string.Join("; ", policyValidation.Errors));
        if (!policy.AllowlistedHosts.Contains(request.SourceUri.Host, StringComparer.OrdinalIgnoreCase))
            return Block(request, policy, "download host is not allowlisted");
        if (request.SourceUri.Scheme is not "http" and not "https")
            return Block(request, policy, "download URL scheme is blocked");
        if (!File.Exists(materializedFilePath))
            return Block(request, policy, "download file was not materialized");

        var fileName = BrowserDownloadManager.SafeFileName(request.SuggestedFileName);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (fileName != Path.GetFileName(fileName) || request.SuggestedFileName.Contains("..", StringComparison.Ordinal))
            return Block(request, policy, "download filename path traversal blocked");
        if (BrowserSafeDownloadPolicy.BlockedExtensions.Contains(extension))
            return Block(request, policy, "download executable or archive extension blocked");
        if (!policy.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return Block(request, policy, "download extension is not allowed");
        if (!policy.AllowedMimeTypes.Contains(request.MimeType, StringComparer.OrdinalIgnoreCase))
            return Block(request, policy, "download MIME type is not allowed");

        var size = new FileInfo(materializedFilePath).Length;
        if (size <= 0 || size > policy.MaxBytes)
            return Block(request, policy, "download size is blocked");

        var quarantineDir = Path.Combine(policy.ControlledDownloadRoot, "quarantine", request.RunId);
        Directory.CreateDirectory(quarantineDir);
        var targetPath = Path.Combine(quarantineDir, fileName);
        if (!BrowserDownloadManager.IsWithin(policy.ControlledDownloadRoot, targetPath))
            return Block(request, policy, "download path outside controlled root");

        File.Copy(materializedFilePath, targetPath, overwrite: true);
        var artifact = new BrowserSafeDownloadArtifact(
            fileName,
            extension,
            BrowserCredentialRedactor.Redact(targetPath),
            request.MimeType,
            size,
            BrowserDownloadManager.Sha256File(targetPath),
            request.SourceUri.Host,
            BrowserNetworkCapture.RedactUrl(request.SourceUri.ToString()),
            Quarantined: true,
            AutoOpened: false,
            Executed: false,
            Redacted: true);
        var verification = Verification(request, artifact);
        var audit = Audit(request, BrowserAuditLedgerEventKind.DownloadCompleted, BrowserSafeDownloadDecisionKind.Verified, "safe download verified", artifact.NormalizedFileName);
        return new BrowserSafeDownloadResult(BrowserSafeDownloadDecisionKind.Verified, artifact, "safe download verified", verification, [$"download:{artifact.Sha256}"], audit, Redacted: true);
    }

    public BrowserSafeDownloadResult Block(BrowserSafeDownloadRequest request, BrowserSafeDownloadPolicy policy, string reason)
    {
        var audit = Audit(request, BrowserAuditLedgerEventKind.DownloadBlocked, BrowserSafeDownloadDecisionKind.Blocked, reason, request.SuggestedFileName);
        return new BrowserSafeDownloadResult(BrowserSafeDownloadDecisionKind.Blocked, null, BrowserCredentialRedactor.Redact(reason), null, [], audit, Redacted: true);
    }

    private static BrowserVerification Verification(BrowserSafeDownloadRequest request, BrowserSafeDownloadArtifact artifact)
    {
        var context = new BrowserTargetContext(
            request.RunId,
            "chrome-cdp",
            request.SessionId,
            null,
            null,
            "download-target",
            "download-page",
            null,
            "main",
            null,
            SafeSourceUri(request.SourceUri),
            "Safe Download",
            0,
            BrowserTargetContext.CreateLivenessToken("download-target", "main", 0),
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            "complete",
            BrowserTargetSource.Cdp);
        return new BrowserVerification(
            $"verification-{Guid.NewGuid():N}",
            request.RunId,
            "step-safe-download",
            request.ActionId,
            context,
            new BrowserExpectedOutcome("safe download materialized in quarantine with hash", null, artifact.NormalizedFileName, null),
            null,
            null,
            BrowserVerificationStatus.Verified,
            0.95,
            [$"evidence-download-{artifact.Sha256}"],
            null,
            DateTimeOffset.UtcNow,
            [$"proof-download-{artifact.Sha256}"]);
    }

    private static Uri SafeSourceUri(Uri uri) =>
        new Uri($"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}");

    private static BrowserAuditLedgerEvent Audit(BrowserSafeDownloadRequest request, BrowserAuditLedgerEventKind kind, BrowserSafeDownloadDecisionKind decision, string reason, string subject) =>
        BrowserPersistentAuditLedger.Create(
            kind,
            request.RunId,
            request.ActionId,
            request.CorrelationId,
            "profile-controlled",
            request.SessionId,
            null,
            null,
            null,
            decision.ToString(),
            reason,
            new Dictionary<string, string>
            {
                ["subject"] = BrowserCredentialRedactor.Redact(subject),
                ["host"] = request.SourceUri.Host,
                ["path"] = BrowserNetworkCapture.RedactUrl(request.SourceUri.ToString())
            });
}
