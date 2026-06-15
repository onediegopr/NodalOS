using System.Security.Cryptography;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class BrowserDownloadManager
{
    public BrowserDownloadResult CompleteFixtureDownload(BrowserDownloadRequest request, BrowserDownloadPolicy policy, string fixturePath)
    {
        var policyValidation = policy.Validate();
        if (!policyValidation.IsValid)
            return Block(request, policy, string.Join("; ", policyValidation.Errors));

        if (!File.Exists(fixturePath))
            return Block(request, policy, "download fixture does not exist");

        var fileName = SafeFileName(request.SuggestedFileName);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!policy.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return Block(request, policy, "download extension blocked by policy");

        var size = new FileInfo(fixturePath).Length;
        if (size <= 0 || size > policy.MaxBytes)
            return Block(request, policy, "download size blocked by policy");

        var targetPath = Path.Combine(policy.ControlledDirectory, fileName);
        if (!IsWithin(policy.ControlledDirectory, targetPath))
            return Block(request, policy, "download path outside controlled directory");

        Directory.CreateDirectory(policy.ControlledDirectory);
        File.Copy(fixturePath, targetPath, overwrite: true);
        var artifact = new BrowserDownloadArtifact(fileName, extension, BrowserCredentialRedactor.Redact(targetPath), request.MimeType, size, Sha256File(targetPath), Quarantined: true, Redacted: true);
        if (policy.RequireHash && string.IsNullOrWhiteSpace(artifact.Sha256))
            return Block(request, policy, "download hash required");

        var audit = Audit(BrowserAuditLedgerEventKind.DownloadCompleted, request.RunId, request.ActionId, request.CorrelationId, request.SessionId, "Completed", "download fixture completed", artifact.FileName);
        return new BrowserDownloadResult(BrowserTransferDecisionKind.Completed, artifact, "download fixture completed", [$"download:{artifact.Sha256}"], audit, Redacted: true);
    }

    public BrowserDownloadResult Block(BrowserDownloadRequest request, BrowserDownloadPolicy policy, string reason)
    {
        var audit = Audit(BrowserAuditLedgerEventKind.DownloadBlocked, request.RunId, request.ActionId, request.CorrelationId, request.SessionId, "Blocked", reason, request.SuggestedFileName);
        return new BrowserDownloadResult(BrowserTransferDecisionKind.Blocked, null, BrowserCredentialRedactor.Redact(reason), [], audit, Redacted: true);
    }

    internal static string SafeFileName(string fileName)
    {
        var safe = BrowserCredentialRedactor.Redact(Path.GetFileName(fileName));
        foreach (var invalid in Path.GetInvalidFileNameChars())
            safe = safe.Replace(invalid, '_');
        safe = safe.Replace("..", "_", StringComparison.Ordinal);
        return string.IsNullOrWhiteSpace(safe) ? "download.bin" : safe;
    }

    internal static bool IsWithin(string root, string path)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }

    internal static string Sha256File(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    internal static BrowserAuditLedgerEvent Audit(BrowserAuditLedgerEventKind kind, string runId, string actionId, string correlationId, string sessionId, string decision, string reason, string subject) =>
        BrowserPersistentAuditLedger.Create(kind, runId, actionId, correlationId, "profile-runtime", sessionId, null, null, null, decision, reason, new Dictionary<string, string>
        {
            ["subject"] = BrowserCredentialRedactor.Redact(subject)
        });
}

public sealed class BrowserUploadManager
{
    public BrowserUploadResult PrepareFixtureUpload(BrowserUploadRequest request, BrowserUploadPolicy policy)
    {
        if (!request.HasApproval && policy.RequireApproval)
            return Block(request, "upload requires policy approval");
        if (policy.AllowExternalTargets)
            return Block(request, "external upload targets are disabled in M16");
        if (!File.Exists(request.FilePath))
            return Block(request, "upload fixture does not exist");
        if (!BrowserDownloadManager.IsWithin(policy.SandboxDirectory, request.FilePath))
            return Block(request, "upload path outside sandbox");

        var fileName = BrowserDownloadManager.SafeFileName(Path.GetFileName(request.FilePath));
        if (BrowserCredentialRedactor.ContainsSecret(fileName) || BrowserCredentialRedactor.ContainsSecret(request.FilePath))
            return Block(request, "upload path contains secret-like content");

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!policy.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return Block(request, "upload extension blocked by policy");

        var size = new FileInfo(request.FilePath).Length;
        if (size <= 0 || size > policy.MaxBytes)
            return Block(request, "upload size blocked by policy");

        var artifact = new BrowserUploadArtifact(fileName, extension, BrowserCredentialRedactor.Redact(Path.Combine("[SANDBOX]", fileName)), size, BrowserDownloadManager.Sha256File(request.FilePath), Redacted: true);
        var audit = BrowserDownloadManager.Audit(BrowserAuditLedgerEventKind.UploadPrepared, request.RunId, request.ActionId, request.CorrelationId, request.SessionId, "Completed", "upload fixture prepared", artifact.FileName);
        return new BrowserUploadResult(BrowserTransferDecisionKind.Completed, artifact, "upload fixture prepared", [$"upload:{artifact.Sha256}"], audit, Redacted: true);
    }

    private static BrowserUploadResult Block(BrowserUploadRequest request, string reason)
    {
        var audit = BrowserDownloadManager.Audit(BrowserAuditLedgerEventKind.UploadBlocked, request.RunId, request.ActionId, request.CorrelationId, request.SessionId, "Blocked", reason, Path.GetFileName(request.FilePath));
        return new BrowserUploadResult(BrowserTransferDecisionKind.Blocked, null, BrowserCredentialRedactor.Redact(reason), [], audit, Redacted: true);
    }
}

public sealed class BrowserNetworkCapture
{
    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "authorization",
        "cookie",
        "set-cookie",
        "x-api-key",
        "x-csrf-token"
    };

    public BrowserNetworkCaptureSummary Capture(BrowserNetworkCapturePolicy policy, IEnumerable<BrowserNetworkCaptureEvent> rawEvents)
    {
        var events = rawEvents.Select(e => RedactEvent(policy, e)).ToArray();
        var audit = BrowserPersistentAuditLedger.Create(
            BrowserAuditLedgerEventKind.NetworkCaptureRecorded,
            "run-network",
            "action-network",
            events.FirstOrDefault()?.CorrelationId ?? "corr-network",
            "profile-runtime",
            "session-network",
            null,
            null,
            null,
            events.All(e => e.Validate().IsValid) ? "Completed" : "Blocked",
            "network metadata captured",
            new Dictionary<string, string> { ["eventCount"] = events.Length.ToString() });
        return new BrowserNetworkCaptureSummary(events, audit, Redacted: true);
    }

    private static BrowserNetworkCaptureEvent RedactEvent(BrowserNetworkCapturePolicy policy, BrowserNetworkCaptureEvent raw)
    {
        var headers = raw.ResponseHeaders
            .Where(pair => policy.CaptureSensitiveHeaders || !SensitiveHeaders.Contains(pair.Key))
            .ToDictionary(
                pair => BrowserCredentialRedactor.Redact(pair.Key),
                pair => BrowserCredentialRedactor.Redact(pair.Value),
                StringComparer.Ordinal);
        return raw with
        {
            Method = policy.AllowedMethods.Contains(raw.Method, StringComparer.OrdinalIgnoreCase) ? raw.Method.ToUpperInvariant() : "BLOCKED",
            RedactedUrl = RedactUrl(raw.RedactedUrl),
            ResponseHeaders = headers,
            BodyCaptured = policy.CaptureBodies && false,
            Redacted = true
        };
    }

    public static string RedactUrl(string url)
    {
        var redacted = BrowserCredentialRedactor.Redact(url);
        if (!Uri.TryCreate(redacted, UriKind.Absolute, out var uri))
            return redacted;

        var safeQuery = string.IsNullOrEmpty(uri.Query)
            ? ""
            : "?[REDACTED_QUERY]";
        return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}{safeQuery}";
    }
}

public sealed class BrowserSessionExportService
{
    public BrowserSessionExportPackage CreatePackage(BrowserSessionReplayManifest manifest)
    {
        var redacted = manifest with
        {
            RunId = BrowserCredentialRedactor.Redact(manifest.RunId),
            CorrelationId = BrowserCredentialRedactor.Redact(manifest.CorrelationId),
            Redacted = true,
            DiagnosticOnly = true
        };
        var hash = redacted.ComputeHash();
        return new BrowserSessionExportPackage(redacted, hash, Redacted: true, DiagnosticOnly: true);
    }
}

public sealed class BrowserRuntimePhaseCloseGate
{
    public BrowserRuntimePhaseCloseReport Evaluate(
        BrowserAuditLedgerExport auditExport,
        BrowserDownloadResult download,
        BrowserUploadResult upload,
        BrowserNetworkCaptureSummary network,
        BrowserSessionExportPackage export,
        bool companionAuthoritative,
        bool serviceWorkerBrain,
        bool realProfileActive,
        bool realVaultActive,
        bool loginRealActive)
    {
        var passed = new List<string>();
        var failed = new List<string>();
        Check(auditExport.Validate().IsValid, "audit ledger", passed, failed);
        Check(download.IsSuccess, "download manager", passed, failed);
        Check(upload.IsSuccess, "upload manager", passed, failed);
        Check(network.IsSafe, "network metadata-only", passed, failed);
        Check(export.Validate().IsValid, "diagnostic replay export", passed, failed);
        Check(!companionAuthoritative, "companion non-authoritative", passed, failed);
        Check(!serviceWorkerBrain, "service worker not brain", passed, failed);
        Check(!realProfileActive, "no real profile", passed, failed);
        Check(!realVaultActive, "no real vault", passed, failed);
        Check(!loginRealActive, "no real login", passed, failed);

        var status = failed.Count == 0 ? BrowserRuntimePhaseCloseStatus.Passed : BrowserRuntimePhaseCloseStatus.Failed;
        var audit = BrowserPersistentAuditLedger.Create(BrowserAuditLedgerEventKind.PhaseCloseGateEvaluated, "run-phase-close", "action-phase-close", "corr-phase-close", "profile-runtime", "session-phase-close", null, null, null, status.ToString(), "browser runtime phase close gate evaluated");
        return new BrowserRuntimePhaseCloseReport(status, status == BrowserRuntimePhaseCloseStatus.Passed ? "Browser runtime phase close passed." : "Browser runtime phase close failed.", passed, failed, auditExport.Validate().IsValid, download.IsSuccess, upload.IsSuccess, network.IsSafe, export.Validate().IsValid, !companionAuthoritative, !serviceWorkerBrain, !realProfileActive, !realVaultActive, !loginRealActive, audit);
    }

    private static void Check(bool condition, string name, List<string> passed, List<string> failed)
    {
        if (condition)
            passed.Add(name);
        else
            failed.Add(name);
    }
}
