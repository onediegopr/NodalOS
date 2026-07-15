using System.Security;
using System.Security.Cryptography;
using System.Text;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core.Runtime;

public enum NodalOsTestOwnedFileUpdateDecision
{
    UpdatedAndVerified,
    InvalidRequest,
    RootOutsideFixtureBoundary,
    InvalidRelativePath,
    ReparsePointRejected,
    TargetMissing,
    TargetTooLarge,
    PreconditionMismatch,
    NoChangeRequested,
    SnapshotFailed,
    AtomicReplaceFailed,
    VerificationFailed,
    RollbackFailed
}

public enum NodalOsTestOwnedFileRollbackDecision
{
    RestoredAndVerified,
    InvalidPlan,
    BoundaryRejected,
    ReparsePointRejected,
    TargetMissing,
    SnapshotMissing,
    CurrentHashMismatch,
    SnapshotHashMismatch,
    RestoreFailed,
    VerificationFailed
}

public sealed record NodalOsTestOwnedFileUpdateRequest(
    string OperationId,
    string ApprovalDecisionId,
    string TestOwnedRootPath,
    string RelativePath,
    string ExpectedCurrentSha256,
    string ReplacementContent);

public sealed record NodalOsTestOwnedFileRestorePlan(
    string RestorePlanId,
    string OperationId,
    string ApprovalDecisionId,
    string RootFingerprint,
    string TargetFingerprint,
    string RelativePath,
    string OriginalSha256,
    string UpdatedSha256,
    string SnapshotId,
    DateTimeOffset CreatedAt,
    bool RequiresExactCurrentHash,
    bool RestrictedToTestOwnedFixture,
    bool CanRestoreUserWorkspace);

public sealed record NodalOsTestOwnedFileUpdateResult(
    NodalOsTestOwnedFileUpdateDecision Decision,
    bool Success,
    bool ExistingTargetRequired,
    bool PreconditionMatched,
    bool SnapshotCreated,
    bool AtomicReplaceUsed,
    bool Verified,
    bool RollbackAvailable,
    bool RollbackPerformedAfterFailure,
    bool NetworkUsed,
    bool ExternalProcessUsed,
    string RootFingerprint,
    string TargetFingerprint,
    string RelativePath,
    string? OriginalSha256,
    string? UpdatedSha256,
    long OriginalBytes,
    long UpdatedBytes,
    string SafeMessage,
    NodalOsTestOwnedFileRestorePlan? RestorePlan,
    NodalOsEvidenceBridgeRef? Evidence);

public sealed record NodalOsTestOwnedFileRollbackResult(
    NodalOsTestOwnedFileRollbackDecision Decision,
    bool Success,
    bool Restored,
    bool Verified,
    bool SnapshotRemoved,
    bool NetworkUsed,
    bool ExternalProcessUsed,
    string RootFingerprint,
    string TargetFingerprint,
    string RelativePath,
    string? RestoredSha256,
    string SafeMessage,
    NodalOsEvidenceBridgeRef? Evidence);

public sealed class NodalOsTestOwnedFileUpdateAction
{
    public const int MaximumContentBytes = NodalOsTestOwnedFileCreateAction.MaximumContentBytes;
    private const string SnapshotDirectoryName = ".nodal-restore";

    public NodalOsTestOwnedFileUpdateResult Execute(
        NodalOsTestOwnedFileUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsSafeIdentifier(request.OperationId) ||
            !IsSafeIdentifier(request.ApprovalDecisionId) ||
            !IsSha256(request.ExpectedCurrentSha256) ||
            string.IsNullOrEmpty(request.ReplacementContent))
        {
            return Failure(
                NodalOsTestOwnedFileUpdateDecision.InvalidRequest,
                "INVALID",
                "INVALID",
                string.Empty,
                "Operation id, approval decision, exact current SHA-256 and non-empty replacement content are required.");
        }

        var replacementBytes = Encoding.UTF8.GetBytes(request.ReplacementContent);
        if (replacementBytes.Length > MaximumContentBytes)
        {
            return Failure(
                NodalOsTestOwnedFileUpdateDecision.InvalidRequest,
                "INVALID",
                "INVALID",
                string.Empty,
                $"Replacement content exceeds the {MaximumContentBytes}-byte fixture limit.");
        }

        if (!TryResolve(request.TestOwnedRootPath, request.RelativePath, out var resolved, out var resolutionFailure))
            return resolutionFailure!;

        var rootFingerprint = Fingerprint(resolved.Root);
        var targetFingerprint = Fingerprint(resolved.Target);
        var normalizedExpectedHash = request.ExpectedCurrentSha256.ToLowerInvariant();
        string? tempPath = null;
        string? snapshotPath = null;
        var snapshotCreated = false;
        var replacementCommitted = false;
        NodalOsTestOwnedFileRestorePlan? restorePlan = null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(resolved.Root) ||
                IsReparsePoint(resolved.BaseRoot) ||
                IsReparsePoint(resolved.Root) ||
                IsReparsePoint(resolved.OutputDirectory))
            {
                return Failure(
                    NodalOsTestOwnedFileUpdateDecision.ReparsePointRejected,
                    rootFingerprint,
                    targetFingerprint,
                    resolved.RelativePath,
                    "The update boundary is missing or contains a reparse point.");
            }

            if (!File.Exists(resolved.Target) || Directory.Exists(resolved.Target))
            {
                return Failure(
                    NodalOsTestOwnedFileUpdateDecision.TargetMissing,
                    rootFingerprint,
                    targetFingerprint,
                    resolved.RelativePath,
                    "The update target must already exist as a regular test-owned file.");
            }

            if (IsReparsePoint(resolved.Target))
            {
                return Failure(
                    NodalOsTestOwnedFileUpdateDecision.ReparsePointRejected,
                    rootFingerprint,
                    targetFingerprint,
                    resolved.RelativePath,
                    "The update target cannot be a reparse point.");
            }

            var fileInfo = new FileInfo(resolved.Target);
            if (fileInfo.Length > MaximumContentBytes)
            {
                return Failure(
                    NodalOsTestOwnedFileUpdateDecision.TargetTooLarge,
                    rootFingerprint,
                    targetFingerprint,
                    resolved.RelativePath,
                    "The existing test-owned target exceeds the bounded update limit.");
            }

            var originalBytes = File.ReadAllBytes(resolved.Target);
            var originalHash = Sha256(originalBytes);
            if (!string.Equals(originalHash, normalizedExpectedHash, StringComparison.Ordinal))
            {
                return Failure(
                    NodalOsTestOwnedFileUpdateDecision.PreconditionMismatch,
                    rootFingerprint,
                    targetFingerprint,
                    resolved.RelativePath,
                    "The exact current SHA-256 precondition did not match; the target was preserved.",
                    originalSha256: originalHash,
                    originalBytes: originalBytes.LongLength);
            }

            var updatedHash = Sha256(replacementBytes);
            if (originalBytes.AsSpan().SequenceEqual(replacementBytes) ||
                string.Equals(originalHash, updatedHash, StringComparison.Ordinal))
            {
                return Failure(
                    NodalOsTestOwnedFileUpdateDecision.NoChangeRequested,
                    rootFingerprint,
                    targetFingerprint,
                    resolved.RelativePath,
                    "The replacement is identical to the current file; no update was performed.",
                    originalSha256: originalHash,
                    updatedSha256: updatedHash,
                    originalBytes: originalBytes.LongLength,
                    updatedBytes: replacementBytes.LongLength);
            }

            var snapshotDirectory = Path.Combine(resolved.Root, SnapshotDirectoryName);
            if (Directory.Exists(snapshotDirectory) && IsReparsePoint(snapshotDirectory))
            {
                return Failure(
                    NodalOsTestOwnedFileUpdateDecision.ReparsePointRejected,
                    rootFingerprint,
                    targetFingerprint,
                    resolved.RelativePath,
                    "The restore snapshot directory cannot be a reparse point.");
            }
            Directory.CreateDirectory(snapshotDirectory);
            if (IsReparsePoint(snapshotDirectory))
            {
                return Failure(
                    NodalOsTestOwnedFileUpdateDecision.ReparsePointRejected,
                    rootFingerprint,
                    targetFingerprint,
                    resolved.RelativePath,
                    "The restore snapshot directory cannot be a reparse point.");
            }

            var snapshotId = $"snapshot-{request.OperationId}";
            snapshotPath = Path.Combine(snapshotDirectory, snapshotId + ".bak");
            if (File.Exists(snapshotPath) || Directory.Exists(snapshotPath))
            {
                return Failure(
                    NodalOsTestOwnedFileUpdateDecision.SnapshotFailed,
                    rootFingerprint,
                    targetFingerprint,
                    resolved.RelativePath,
                    "A restore snapshot for this operation already exists.");
            }

            WriteNewFile(snapshotPath, originalBytes);
            snapshotCreated = true;
            var snapshotBytes = File.ReadAllBytes(snapshotPath);
            if (!snapshotBytes.AsSpan().SequenceEqual(originalBytes) ||
                !string.Equals(Sha256(snapshotBytes), originalHash, StringComparison.Ordinal))
            {
                TryDelete(snapshotPath);
                snapshotCreated = false;
                return Failure(
                    NodalOsTestOwnedFileUpdateDecision.SnapshotFailed,
                    rootFingerprint,
                    targetFingerprint,
                    resolved.RelativePath,
                    "The pre-update restore snapshot did not pass byte-for-byte verification.");
            }

            restorePlan = new NodalOsTestOwnedFileRestorePlan(
                RestorePlanId: $"restore-plan-{request.OperationId}",
                OperationId: request.OperationId,
                ApprovalDecisionId: request.ApprovalDecisionId,
                RootFingerprint: rootFingerprint,
                TargetFingerprint: targetFingerprint,
                RelativePath: resolved.RelativePath,
                OriginalSha256: originalHash,
                UpdatedSha256: updatedHash,
                SnapshotId: snapshotId,
                CreatedAt: DateTimeOffset.UtcNow,
                RequiresExactCurrentHash: true,
                RestrictedToTestOwnedFixture: true,
                CanRestoreUserWorkspace: false);

            cancellationToken.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(resolved.Target);
            tempPath = Path.Combine(resolved.OutputDirectory, $".{fileName}.{Guid.NewGuid():N}.update.tmp");
            WriteNewFile(tempPath, replacementBytes);
            cancellationToken.ThrowIfCancellationRequested();

            File.Replace(tempPath, resolved.Target, destinationBackupFileName: null, ignoreMetadataErrors: true);
            tempPath = null;
            replacementCommitted = true;

            var persisted = File.ReadAllBytes(resolved.Target);
            var actualUpdatedHash = Sha256(persisted);
            if (!persisted.AsSpan().SequenceEqual(replacementBytes) ||
                !string.Equals(actualUpdatedHash, updatedHash, StringComparison.Ordinal))
            {
                var rollback = RestoreInternal(resolved, restorePlan, allowUnexpectedCurrentHash: true);
                return Failure(
                    rollback.Success
                        ? NodalOsTestOwnedFileUpdateDecision.VerificationFailed
                        : NodalOsTestOwnedFileUpdateDecision.RollbackFailed,
                    rootFingerprint,
                    targetFingerprint,
                    resolved.RelativePath,
                    rollback.Success
                        ? "The replacement failed verification and the original snapshot was restored."
                        : "The replacement failed verification and guarded rollback also failed.",
                    preconditionMatched: true,
                    snapshotCreated: true,
                    atomicReplaceUsed: true,
                    rollbackAvailable: !rollback.Success,
                    rollbackPerformedAfterFailure: rollback.Success,
                    originalSha256: originalHash,
                    updatedSha256: actualUpdatedHash,
                    originalBytes: originalBytes.LongLength,
                    updatedBytes: persisted.LongLength,
                    restorePlan: rollback.Success ? null : restorePlan);
            }

            var evidence = new NodalOsEvidenceBridgeRef
            {
                EvidenceId = $"evidence-file-update-{request.OperationId}",
                Kind = "test-owned-file-update-verification",
                Ref = null,
                Hash = actualUpdatedHash,
                SourceKind = NodalOsEvidenceBridgeSourceKind.VerificationGate,
                UseKind = NodalOsEvidenceBridgeUseKind.VerificationSupport,
                Authority = NodalOsEvidenceBridgeAuthority.SupportsVerificationOnly,
                Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
                RedactionState = NodalOsEvidenceRedactionState.NotRequired,
                LedgerRef = $"fixture-file-update:{request.OperationId}:before:{originalHash}",
                Provenance = "Exact-hash preconditioned atomic replacement inside a unique NODAL OS test-owned fixture root.",
                CreatedAt = DateTimeOffset.UtcNow
            };

            return new NodalOsTestOwnedFileUpdateResult(
                NodalOsTestOwnedFileUpdateDecision.UpdatedAndVerified,
                Success: true,
                ExistingTargetRequired: true,
                PreconditionMatched: true,
                SnapshotCreated: true,
                AtomicReplaceUsed: true,
                Verified: true,
                RollbackAvailable: true,
                RollbackPerformedAfterFailure: false,
                NetworkUsed: false,
                ExternalProcessUsed: false,
                RootFingerprint: rootFingerprint,
                TargetFingerprint: targetFingerprint,
                RelativePath: resolved.RelativePath,
                OriginalSha256: originalHash,
                UpdatedSha256: actualUpdatedHash,
                OriginalBytes: originalBytes.LongLength,
                UpdatedBytes: persisted.LongLength,
                SafeMessage: "The existing test-owned file matched its exact SHA-256 precondition, was replaced atomically and passed verification.",
                RestorePlan: restorePlan,
                Evidence: evidence);
        }
        catch (OperationCanceledException)
        {
            if (replacementCommitted && restorePlan is not null)
                RestoreInternal(resolved, restorePlan, allowUnexpectedCurrentHash: true);
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException or PlatformNotSupportedException)
        {
            var rollback = replacementCommitted && restorePlan is not null
                ? RestoreInternal(resolved, restorePlan, allowUnexpectedCurrentHash: true)
                : null;
            return Failure(
                rollback is { Success: false }
                    ? NodalOsTestOwnedFileUpdateDecision.RollbackFailed
                    : NodalOsTestOwnedFileUpdateDecision.AtomicReplaceFailed,
                rootFingerprint,
                targetFingerprint,
                resolved.RelativePath,
                rollback is { Success: true }
                    ? "The atomic replacement failed and the verified original snapshot was restored."
                    : "The exact-hash test-owned update failed closed.",
                preconditionMatched: restorePlan is not null,
                snapshotCreated: snapshotCreated,
                atomicReplaceUsed: replacementCommitted,
                rollbackAvailable: snapshotCreated && rollback is not { Success: true },
                rollbackPerformedAfterFailure: rollback is { Success: true },
                restorePlan: rollback is { Success: true } ? null : restorePlan);
        }
        finally
        {
            if (tempPath is not null)
                TryDelete(tempPath);
            if (!snapshotCreated && snapshotPath is not null)
                TryDelete(snapshotPath);
        }
    }

    public NodalOsTestOwnedFileRollbackResult Rollback(
        string testOwnedRootPath,
        NodalOsTestOwnedFileRestorePlan restorePlan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(restorePlan);
        cancellationToken.ThrowIfCancellationRequested();
        if (!ValidatePlan(restorePlan) ||
            !TryResolve(testOwnedRootPath, restorePlan.RelativePath, out var resolved, out _))
        {
            return RollbackFailure(
                NodalOsTestOwnedFileRollbackDecision.InvalidPlan,
                restorePlan.RootFingerprint,
                restorePlan.TargetFingerprint,
                restorePlan.RelativePath,
                "The restore plan is invalid.");
        }

        if (!string.Equals(Fingerprint(resolved.Root), restorePlan.RootFingerprint, StringComparison.Ordinal) ||
            !string.Equals(Fingerprint(resolved.Target), restorePlan.TargetFingerprint, StringComparison.Ordinal))
        {
            return RollbackFailure(
                NodalOsTestOwnedFileRollbackDecision.BoundaryRejected,
                Fingerprint(resolved.Root),
                Fingerprint(resolved.Target),
                resolved.RelativePath,
                "The restore plan does not match the test-owned root or target fingerprint.");
        }

        return RestoreInternal(resolved, restorePlan, allowUnexpectedCurrentHash: false, cancellationToken);
    }

    private static NodalOsTestOwnedFileRollbackResult RestoreInternal(
        ResolvedTarget resolved,
        NodalOsTestOwnedFileRestorePlan plan,
        bool allowUnexpectedCurrentHash,
        CancellationToken cancellationToken = default)
    {
        string? tempPath = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var snapshotDirectory = Path.Combine(resolved.Root, SnapshotDirectoryName);
            var snapshotPath = Path.Combine(snapshotDirectory, plan.SnapshotId + ".bak");
            if (IsReparsePoint(resolved.BaseRoot) ||
                IsReparsePoint(resolved.Root) ||
                IsReparsePoint(resolved.OutputDirectory) ||
                IsReparsePoint(resolved.Target) ||
                (Directory.Exists(snapshotDirectory) && IsReparsePoint(snapshotDirectory)) ||
                (File.Exists(snapshotPath) && IsReparsePoint(snapshotPath)))
            {
                return RollbackFailure(
                    NodalOsTestOwnedFileRollbackDecision.ReparsePointRejected,
                    plan.RootFingerprint,
                    plan.TargetFingerprint,
                    resolved.RelativePath,
                    "Guarded rollback rejected a reparse point.");
            }

            if (!File.Exists(resolved.Target))
            {
                return RollbackFailure(
                    NodalOsTestOwnedFileRollbackDecision.TargetMissing,
                    plan.RootFingerprint,
                    plan.TargetFingerprint,
                    resolved.RelativePath,
                    "Guarded rollback requires the updated target to exist.");
            }
            if (!File.Exists(snapshotPath))
            {
                return RollbackFailure(
                    NodalOsTestOwnedFileRollbackDecision.SnapshotMissing,
                    plan.RootFingerprint,
                    plan.TargetFingerprint,
                    resolved.RelativePath,
                    "The verified restore snapshot is missing.");
            }

            var currentBytes = File.ReadAllBytes(resolved.Target);
            var currentHash = Sha256(currentBytes);
            if (!allowUnexpectedCurrentHash &&
                !string.Equals(currentHash, plan.UpdatedSha256, StringComparison.Ordinal))
            {
                return RollbackFailure(
                    NodalOsTestOwnedFileRollbackDecision.CurrentHashMismatch,
                    plan.RootFingerprint,
                    plan.TargetFingerprint,
                    resolved.RelativePath,
                    "The current target SHA-256 no longer matches the update result; rollback was rejected.");
            }

            var snapshotBytes = File.ReadAllBytes(snapshotPath);
            var snapshotHash = Sha256(snapshotBytes);
            if (!string.Equals(snapshotHash, plan.OriginalSha256, StringComparison.Ordinal))
            {
                return RollbackFailure(
                    NodalOsTestOwnedFileRollbackDecision.SnapshotHashMismatch,
                    plan.RootFingerprint,
                    plan.TargetFingerprint,
                    resolved.RelativePath,
                    "The restore snapshot SHA-256 does not match the approved restore plan.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(resolved.Target);
            tempPath = Path.Combine(resolved.OutputDirectory, $".{fileName}.{Guid.NewGuid():N}.restore.tmp");
            WriteNewFile(tempPath, snapshotBytes);
            File.Replace(tempPath, resolved.Target, destinationBackupFileName: null, ignoreMetadataErrors: true);
            tempPath = null;

            var restoredBytes = File.ReadAllBytes(resolved.Target);
            var restoredHash = Sha256(restoredBytes);
            if (!restoredBytes.AsSpan().SequenceEqual(snapshotBytes) ||
                !string.Equals(restoredHash, plan.OriginalSha256, StringComparison.Ordinal))
            {
                return RollbackFailure(
                    NodalOsTestOwnedFileRollbackDecision.VerificationFailed,
                    plan.RootFingerprint,
                    plan.TargetFingerprint,
                    resolved.RelativePath,
                    "The restored file did not pass byte-for-byte verification.",
                    restoredSha256: restoredHash);
            }

            TryDelete(snapshotPath);
            var snapshotRemoved = !File.Exists(snapshotPath);
            var evidence = new NodalOsEvidenceBridgeRef
            {
                EvidenceId = $"evidence-file-rollback-{plan.OperationId}",
                Kind = "test-owned-file-rollback-verification",
                Ref = null,
                Hash = restoredHash,
                SourceKind = NodalOsEvidenceBridgeSourceKind.VerificationGate,
                UseKind = NodalOsEvidenceBridgeUseKind.VerificationSupport,
                Authority = NodalOsEvidenceBridgeAuthority.SupportsVerificationOnly,
                Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
                RedactionState = NodalOsEvidenceRedactionState.NotRequired,
                LedgerRef = $"fixture-file-rollback:{plan.OperationId}:from:{plan.UpdatedSha256}",
                Provenance = "Exact-hash guarded restore inside a unique NODAL OS test-owned fixture root.",
                CreatedAt = DateTimeOffset.UtcNow
            };
            return new NodalOsTestOwnedFileRollbackResult(
                NodalOsTestOwnedFileRollbackDecision.RestoredAndVerified,
                Success: snapshotRemoved,
                Restored: true,
                Verified: true,
                SnapshotRemoved: snapshotRemoved,
                NetworkUsed: false,
                ExternalProcessUsed: false,
                RootFingerprint: plan.RootFingerprint,
                TargetFingerprint: plan.TargetFingerprint,
                RelativePath: resolved.RelativePath,
                RestoredSha256: restoredHash,
                SafeMessage: snapshotRemoved
                    ? "The exact-hash guarded rollback restored and verified the original test-owned file."
                    : "The original file was restored, but the snapshot could not be removed.",
                Evidence: evidence);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException or PlatformNotSupportedException)
        {
            return RollbackFailure(
                NodalOsTestOwnedFileRollbackDecision.RestoreFailed,
                plan.RootFingerprint,
                plan.TargetFingerprint,
                resolved.RelativePath,
                "The guarded test-owned rollback failed closed.");
        }
        finally
        {
            if (tempPath is not null)
                TryDelete(tempPath);
        }
    }

    private static bool TryResolve(
        string rootPath,
        string relativePathValue,
        out ResolvedTarget resolved,
        out NodalOsTestOwnedFileUpdateResult? failure)
    {
        resolved = default!;
        failure = null;
        string baseRoot;
        string root;
        try
        {
            baseRoot = NodalOsTestOwnedFileCreateAction.AllowedBaseRoot;
            root = Path.GetFullPath(rootPath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            failure = Failure(
                NodalOsTestOwnedFileUpdateDecision.RootOutsideFixtureBoundary,
                "INVALID",
                "INVALID",
                string.Empty,
                "The test-owned fixture root is invalid.");
            return false;
        }

        var rootFingerprint = Fingerprint(root);
        if (!IsDirectFixtureChild(baseRoot, root))
        {
            failure = Failure(
                NodalOsTestOwnedFileUpdateDecision.RootOutsideFixtureBoundary,
                rootFingerprint,
                "INVALID",
                string.Empty,
                "The update action is restricted to a unique child of the NODAL OS test fixture root.");
            return false;
        }

        if (!TryNormalizeRelativePath(relativePathValue, out var relativePath))
        {
            failure = Failure(
                NodalOsTestOwnedFileUpdateDecision.InvalidRelativePath,
                rootFingerprint,
                "INVALID",
                string.Empty,
                "Only output/<name>.md or output/<name>.txt is allowed.");
            return false;
        }

        var target = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var targetFingerprint = Fingerprint(target);
        if (!IsInside(root, target))
        {
            failure = Failure(
                NodalOsTestOwnedFileUpdateDecision.InvalidRelativePath,
                rootFingerprint,
                targetFingerprint,
                relativePath,
                "The update target escaped the test-owned fixture root.");
            return false;
        }

        resolved = new ResolvedTarget(
            BaseRoot: baseRoot,
            Root: root,
            OutputDirectory: Path.GetDirectoryName(target)!,
            Target: target,
            RelativePath: relativePath);
        return true;
    }

    private static bool ValidatePlan(NodalOsTestOwnedFileRestorePlan plan) =>
        IsSafeIdentifier(plan.RestorePlanId) &&
        IsSafeIdentifier(plan.OperationId) &&
        IsSafeIdentifier(plan.ApprovalDecisionId) &&
        IsSafeIdentifier(plan.SnapshotId) &&
        IsSha256(plan.RootFingerprint) &&
        IsSha256(plan.TargetFingerprint) &&
        IsSha256(plan.OriginalSha256) &&
        IsSha256(plan.UpdatedSha256) &&
        plan.RequiresExactCurrentHash &&
        plan.RestrictedToTestOwnedFixture &&
        !plan.CanRestoreUserWorkspace;

    private static void WriteNewFile(string path, ReadOnlySpan<byte> bytes)
    {
        using var stream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            FileOptions.WriteThrough);
        stream.Write(bytes);
        stream.Flush(flushToDisk: true);
    }

    private static bool TryNormalizeRelativePath(string value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value) || value.Length > 180 || value.Contains('\\'))
            return false;

        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(value.Trim());
        }
        catch (UriFormatException)
        {
            return false;
        }

        if (Path.IsPathRooted(decoded) || decoded.Contains("..", StringComparison.Ordinal))
            return false;
        var parts = decoded.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !string.Equals(parts[0], "output", StringComparison.Ordinal))
            return false;
        var fileName = parts[1];
        if (fileName is "." or ".." || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return false;
        var extension = Path.GetExtension(fileName);
        if (!string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        normalized = $"output/{fileName}";
        return true;
    }

    private static bool IsDirectFixtureChild(string baseRoot, string root)
    {
        var parent = Directory.GetParent(root)?.FullName;
        var leaf = Path.GetFileName(root);
        return parent is not null &&
               PathsEqual(parent, baseRoot) &&
               leaf.StartsWith("run-", StringComparison.Ordinal) &&
               leaf.Length > "run-".Length;
    }

    private static bool IsInside(string root, string candidate)
    {
        var prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return candidate.StartsWith(prefix, PathComparison);
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            PathComparison);

    private static bool IsReparsePoint(string path) =>
        File.Exists(path) || Directory.Exists(path)
            ? (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0
            : false;

    private static bool IsSafeIdentifier(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 128 &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');

    private static bool IsSha256(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length == 64 &&
        value.All(Uri.IsHexDigit);

    private static string Fingerprint(string value) =>
        Sha256(Encoding.UTF8.GetBytes(Path.GetFullPath(value)));

    private static string Sha256(ReadOnlySpan<byte> value) =>
        Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static NodalOsTestOwnedFileUpdateResult Failure(
        NodalOsTestOwnedFileUpdateDecision decision,
        string rootFingerprint,
        string targetFingerprint,
        string relativePath,
        string safeMessage,
        bool preconditionMatched = false,
        bool snapshotCreated = false,
        bool atomicReplaceUsed = false,
        bool rollbackAvailable = false,
        bool rollbackPerformedAfterFailure = false,
        string? originalSha256 = null,
        string? updatedSha256 = null,
        long originalBytes = 0,
        long updatedBytes = 0,
        NodalOsTestOwnedFileRestorePlan? restorePlan = null) => new(
            Decision: decision,
            Success: false,
            ExistingTargetRequired: true,
            PreconditionMatched: preconditionMatched,
            SnapshotCreated: snapshotCreated,
            AtomicReplaceUsed: atomicReplaceUsed,
            Verified: false,
            RollbackAvailable: rollbackAvailable,
            RollbackPerformedAfterFailure: rollbackPerformedAfterFailure,
            NetworkUsed: false,
            ExternalProcessUsed: false,
            RootFingerprint: rootFingerprint,
            TargetFingerprint: targetFingerprint,
            RelativePath: relativePath,
            OriginalSha256: originalSha256,
            UpdatedSha256: updatedSha256,
            OriginalBytes: originalBytes,
            UpdatedBytes: updatedBytes,
            SafeMessage: safeMessage,
            RestorePlan: restorePlan,
            Evidence: null);

    private static NodalOsTestOwnedFileRollbackResult RollbackFailure(
        NodalOsTestOwnedFileRollbackDecision decision,
        string rootFingerprint,
        string targetFingerprint,
        string relativePath,
        string safeMessage,
        string? restoredSha256 = null) => new(
            Decision: decision,
            Success: false,
            Restored: false,
            Verified: false,
            SnapshotRemoved: false,
            NetworkUsed: false,
            ExternalProcessUsed: false,
            RootFingerprint: rootFingerprint,
            TargetFingerprint: targetFingerprint,
            RelativePath: relativePath,
            RestoredSha256: restoredSha256,
            SafeMessage: safeMessage,
            Evidence: null);

    private sealed record ResolvedTarget(
        string BaseRoot,
        string Root,
        string OutputDirectory,
        string Target,
        string RelativePath);
}
