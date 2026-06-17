using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsOnnxModelInventoryService
{
    private readonly NodalOsPaddleOcrOnnxModelCatalogService _catalog = new();

    public NodalOsOnnxModelInventory BuildInventory(
        string repositoryRoot,
        IReadOnlySet<string>? committedRelativePaths = null,
        IReadOnlySet<string>? gitIgnoredRelativePaths = null)
    {
        var manifestPath = Path.Combine(repositoryRoot, "tools", "ocr-worker", "models", "onnx", "paddleocr-onnx-model-manifest.json");
        var manifest = _catalog.LoadManifestFromFile(manifestPath);
        if (manifest is null)
        {
            return new NodalOsOnnxModelInventory(
                $"onnx-model-inventory-{Guid.NewGuid():N}",
                repositoryRoot,
                manifestPath,
                ManifestExists: false,
                Entries: [],
                NoSaas: true,
                NoRawPersistence: true,
                NoAuthority: true,
                DateTimeOffset.UtcNow);
        }

        var entries = manifest.Models.Select(m => BuildEntry(repositoryRoot, m, committedRelativePaths, gitIgnoredRelativePaths)).ToList();
        return new NodalOsOnnxModelInventory(
            $"onnx-model-inventory-{Guid.NewGuid():N}",
            repositoryRoot,
            manifestPath,
            ManifestExists: true,
            entries,
            NoSaas: true,
            NoRawPersistence: true,
            NoAuthority: true,
            DateTimeOffset.UtcNow);
    }

    private static NodalOsOnnxModelInventoryEntry BuildEntry(
        string repositoryRoot,
        NodalOsPaddleOcrOnnxModelRef model,
        IReadOnlySet<string>? committedRelativePaths,
        IReadOnlySet<string>? gitIgnoredRelativePaths)
    {
        var expectedFileName = Path.GetFileName(model.LocalRelativePath);
        var resolved = Path.GetFullPath(Path.Combine(repositoryRoot, model.LocalRelativePath));
        var underRepo = resolved.StartsWith(Path.GetFullPath(repositoryRoot), StringComparison.OrdinalIgnoreCase);
        var fileNameMatches = string.Equals(Path.GetFileName(resolved), expectedFileName, StringComparison.OrdinalIgnoreCase);
        var exists = File.Exists(resolved);
        var actualSize = exists ? new FileInfo(resolved).Length : (long?)null;
        var actualHash = exists ? NodalOsPaddleOcrOnnxModelCatalogService.ComputeChecksum(resolved, model.Integrity.Algorithm) : null;
        var relative = NormalizeRelative(model.LocalRelativePath);
        var committed = committedRelativePaths?.Contains(relative) ?? IsCommittedModel(repositoryRoot, relative);
        var ignored = gitIgnoredRelativePaths?.Contains(relative) ?? IsGitIgnored(repositoryRoot, relative);

        var pathResolution = new NodalOsOnnxModelPathResolution(
            model.ModelId,
            model.LocalRelativePath,
            expectedFileName,
            resolved,
            underRepo,
            fileNameMatches,
            NoAuthority: true);

        var status = DetermineStatus(model, exists, actualSize, actualHash, committed, underRepo, fileNameMatches);
        return new NodalOsOnnxModelInventoryEntry(
            model.ModelId,
            model.Kind,
            expectedFileName,
            model.LocalRelativePath,
            pathResolution,
            exists,
            model.Integrity.Checksum,
            actualHash,
            model.Integrity.FileSizeBytes,
            actualSize,
            model.Source.Url,
            ignored,
            committed,
            status,
            Reason(status, model.ModelId));
    }

    private static NodalOsOnnxModelAvailabilityStatus DetermineStatus(
        NodalOsPaddleOcrOnnxModelRef model,
        bool exists,
        long? actualSize,
        string? actualHash,
        bool committed,
        bool underRepo,
        bool fileNameMatches)
    {
        if (!underRepo || !fileNameMatches)
            return NodalOsOnnxModelAvailabilityStatus.PathMismatch;

        if (committed)
            return NodalOsOnnxModelAvailabilityStatus.UnexpectedCommittedModel;

        if (!exists)
            return NodalOsOnnxModelAvailabilityStatus.Missing;

        if (model.Integrity.FileSizeBytes > 0 && actualSize != model.Integrity.FileSizeBytes)
            return NodalOsOnnxModelAvailabilityStatus.SizeMismatch;

        if (!string.IsNullOrWhiteSpace(model.Integrity.Checksum) &&
            !string.Equals(actualHash, model.Integrity.Checksum, StringComparison.OrdinalIgnoreCase))
        {
            return NodalOsOnnxModelAvailabilityStatus.HashMismatch;
        }

        return NodalOsOnnxModelAvailabilityStatus.PresentAndVerified;
    }

    private static string Reason(NodalOsOnnxModelAvailabilityStatus status, string modelId) => status switch
    {
        NodalOsOnnxModelAvailabilityStatus.PresentAndVerified => $"{modelId} present and SHA-256/size verified",
        NodalOsOnnxModelAvailabilityStatus.Missing => $"{modelId} model file missing at manifest path",
        NodalOsOnnxModelAvailabilityStatus.PathMismatch => $"{modelId} path resolution does not match repository-safe expected path",
        NodalOsOnnxModelAvailabilityStatus.HashMismatch => $"{modelId} SHA-256 mismatch",
        NodalOsOnnxModelAvailabilityStatus.SizeMismatch => $"{modelId} size mismatch",
        NodalOsOnnxModelAvailabilityStatus.UnexpectedCommittedModel => $"{modelId} .onnx file is unexpectedly tracked by git",
        _ => $"{modelId} inventory status {status}"
    };

    private static bool IsCommittedModel(string repositoryRoot, string relativePath)
    {
        var gitDir = Path.Combine(repositoryRoot, ".git");
        if (!Directory.Exists(gitDir) && !File.Exists(gitDir))
            return false;

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = repositoryRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            psi.ArgumentList.Add("ls-files");
            psi.ArgumentList.Add("--error-unmatch");
            psi.ArgumentList.Add(relativePath.Replace('\\', '/'));
            using var process = System.Diagnostics.Process.Start(psi);
            process?.WaitForExit(5000);
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsGitIgnored(string repositoryRoot, string relativePath)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = repositoryRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            psi.ArgumentList.Add("check-ignore");
            psi.ArgumentList.Add("-q");
            psi.ArgumentList.Add(relativePath.Replace('\\', '/'));
            using var process = System.Diagnostics.Process.Start(psi);
            process?.WaitForExit(5000);
            return process?.ExitCode == 0;
        }
        catch
        {
            return relativePath.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string NormalizeRelative(string path) => path.Replace('\\', '/');
}

public sealed class NodalOsOnnxModelAvailabilityReadinessGate
{
    public NodalOsOnnxModelManifestReconciliationResult Evaluate(NodalOsOnnxModelInventory inventory)
    {
        var det = inventory.Entries.FirstOrDefault(e => e.Role == NodalOsPaddleOcrOnnxModelKind.TextDetection);
        var rec = inventory.Entries.FirstOrDefault(e => e.Role == NodalOsPaddleOcrOnnxModelKind.TextRecognition);
        var cls = inventory.Entries.FirstOrDefault(e => e.Role == NodalOsPaddleOcrOnnxModelKind.TextDirectionClassification);
        var scriptsPresent = ScriptsPresent(inventory.RepositoryRoot);

        var anyHash = inventory.Entries.Any(e => e.AvailabilityStatus == NodalOsOnnxModelAvailabilityStatus.HashMismatch);
        var anySize = inventory.Entries.Any(e => e.AvailabilityStatus == NodalOsOnnxModelAvailabilityStatus.SizeMismatch);
        var anyPath = inventory.Entries.Any(e => e.AvailabilityStatus == NodalOsOnnxModelAvailabilityStatus.PathMismatch);
        var anyCommitted = inventory.Entries.Any(e => e.AvailabilityStatus == NodalOsOnnxModelAvailabilityStatus.UnexpectedCommittedModel);
        var detVerified = det?.AvailabilityStatus == NodalOsOnnxModelAvailabilityStatus.PresentAndVerified;
        var recVerified = rec?.AvailabilityStatus == NodalOsOnnxModelAvailabilityStatus.PresentAndVerified;
        var clsVerified = cls?.AvailabilityStatus == NodalOsOnnxModelAvailabilityStatus.PresentAndVerified;

        var decision = Decide(inventory, scriptsPresent, detVerified, recVerified, anyHash, anySize, anyPath, anyCommitted);
        return new NodalOsOnnxModelManifestReconciliationResult(
            $"onnx-model-reconciliation-{Guid.NewGuid():N}",
            inventory,
            detVerified,
            recVerified,
            clsVerified,
            anyHash,
            anySize,
            anyPath,
            anyCommitted,
            scriptsPresent,
            "pwsh -NoProfile -ExecutionPolicy Bypass -File tools/ocr-worker/models/onnx/download-models.ps1 -Confirm",
            "pwsh -NoProfile -ExecutionPolicy Bypass -File tools/ocr-worker/models/onnx/verify-models.ps1",
            "pwsh -NoProfile -ExecutionPolicy Bypass -File tools/ocr-worker/models/onnx/rollback-models.ps1 -Confirm",
            decision,
            Reason(decision, det, rec, scriptsPresent));
    }

    public NodalOsGuardedSyntheticTextRetryPlan BuildRetryPlan(NodalOsOnnxModelManifestReconciliationResult reconciliation)
    {
        var expectedFiles = reconciliation.Inventory.Entries.Select(e => e.ExpectedRelativePath).ToList();
        var hashes = reconciliation.Inventory.Entries.ToDictionary(e => e.ExpectedRelativePath, e => e.ExpectedSha256, StringComparer.Ordinal);
        var sizes = reconciliation.Inventory.Entries.ToDictionary(e => e.ExpectedRelativePath, e => e.ExpectedSizeBytes, StringComparer.Ordinal);
        var allowed = reconciliation.Decision == NodalOsOnnxModelAcquisitionReadinessDecision.ReadyForGuardedSyntheticTextRetry;
        return new NodalOsGuardedSyntheticTextRetryPlan(
            $"guarded-synthetic-text-retry-plan-{Guid.NewGuid():N}",
            RetryExecuted: false,
            RetryAllowed: allowed,
            UsesOutOfProcessGuard: true,
            reconciliation.DownloadCommand,
            reconciliation.VerifyCommand,
            expectedFiles,
            hashes,
            sizes,
            reconciliation.Decision,
            allowed
                ? "detector and recognizer are verified; run guarded synthetic retry out-of-process only"
                : "retry blocked until required detector and recognizer models are downloaded and verified");
    }

    private static NodalOsOnnxModelAcquisitionReadinessDecision Decide(
        NodalOsOnnxModelInventory inventory,
        bool scriptsPresent,
        bool detVerified,
        bool recVerified,
        bool anyHash,
        bool anySize,
        bool anyPath,
        bool anyCommitted)
    {
        if (!inventory.ManifestExists)
            return NodalOsOnnxModelAcquisitionReadinessDecision.BlockedByModelManifestMismatch;
        if (!scriptsPresent)
            return NodalOsOnnxModelAcquisitionReadinessDecision.BlockedByAcquisitionScript;
        if (anyCommitted)
            return NodalOsOnnxModelAcquisitionReadinessDecision.BlockedByUnexpectedCommittedModel;
        if (anyPath)
            return NodalOsOnnxModelAcquisitionReadinessDecision.BlockedByModelPathDiscovery;
        if (anyHash || anySize)
            return NodalOsOnnxModelAcquisitionReadinessDecision.BlockedByModelHashMismatch;
        if (detVerified && recVerified)
            return NodalOsOnnxModelAcquisitionReadinessDecision.ReadyForGuardedSyntheticTextRetry;
        if (inventory.Entries.Any(e => e.AvailabilityStatus == NodalOsOnnxModelAvailabilityStatus.Missing))
            return NodalOsOnnxModelAcquisitionReadinessDecision.ReadyForModelDownload;
        return NodalOsOnnxModelAcquisitionReadinessDecision.NotReady;
    }

    private static string Reason(
        NodalOsOnnxModelAcquisitionReadinessDecision decision,
        NodalOsOnnxModelInventoryEntry? det,
        NodalOsOnnxModelInventoryEntry? rec,
        bool scriptsPresent) =>
        decision switch
        {
            NodalOsOnnxModelAcquisitionReadinessDecision.ReadyForModelDownload =>
                $"required models not verified: detection={det?.AvailabilityStatus}, recognition={rec?.AvailabilityStatus}; scriptsPresent={scriptsPresent}",
            NodalOsOnnxModelAcquisitionReadinessDecision.ReadyForGuardedSyntheticTextRetry =>
                "detection and recognition models are present and verified",
            _ => $"{decision}: detection={det?.AvailabilityStatus}, recognition={rec?.AvailabilityStatus}, scriptsPresent={scriptsPresent}"
        };

    private static bool ScriptsPresent(string repositoryRoot)
    {
        var dir = Path.Combine(repositoryRoot, "tools", "ocr-worker", "models", "onnx");
        return File.Exists(Path.Combine(dir, "download-models.ps1")) &&
               File.Exists(Path.Combine(dir, "verify-models.ps1")) &&
               File.Exists(Path.Combine(dir, "rollback-models.ps1"));
    }
}
