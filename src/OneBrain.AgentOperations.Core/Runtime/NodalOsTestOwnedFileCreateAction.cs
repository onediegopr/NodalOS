using System.Security;
using System.Security.Cryptography;
using System.Text;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core.Runtime;

public enum NodalOsTestOwnedFileCreateDecision
{
    CreatedAndVerified,
    InvalidRequest,
    RootOutsideFixtureBoundary,
    InvalidRelativePath,
    ReparsePointRejected,
    TargetAlreadyExists,
    WriteFailed,
    VerificationFailed
}

public sealed record NodalOsTestOwnedFileCreateRequest(
    string OperationId,
    string ApprovalDecisionId,
    string TestOwnedRootPath,
    string RelativePath,
    string Content);

public sealed record NodalOsTestOwnedFileCreateResult(
    NodalOsTestOwnedFileCreateDecision Decision,
    bool Success,
    bool Created,
    bool Verified,
    bool OverwriteAttempted,
    bool NetworkUsed,
    bool ExternalProcessUsed,
    string RootFingerprint,
    string TargetFingerprint,
    string RelativePath,
    string? ContentSha256,
    long BytesWritten,
    string SafeMessage,
    NodalOsEvidenceBridgeRef? Evidence);

public sealed record NodalOsTestOwnedFixtureCleanupResult(
    bool Success,
    bool RootRemoved,
    string Decision,
    string SafeMessage);

public sealed class NodalOsTestOwnedFileCreateAction
{
    public const int MaximumContentBytes = 64 * 1024;

    public static string AllowedBaseRoot =>
        Path.GetFullPath(Path.Combine(Path.GetTempPath(), "nodal-os-mvp-fixtures"));

    public NodalOsTestOwnedFileCreateResult Execute(
        NodalOsTestOwnedFileCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsSafeIdentifier(request.OperationId) ||
            !IsSafeIdentifier(request.ApprovalDecisionId) ||
            string.IsNullOrEmpty(request.Content))
        {
            return Failure(
                NodalOsTestOwnedFileCreateDecision.InvalidRequest,
                "INVALID",
                "INVALID",
                string.Empty,
                "Operation id, approval decision and non-empty content are required.");
        }

        var bytes = Encoding.UTF8.GetBytes(request.Content);
        if (bytes.Length > MaximumContentBytes)
        {
            return Failure(
                NodalOsTestOwnedFileCreateDecision.InvalidRequest,
                "INVALID",
                "INVALID",
                string.Empty,
                $"Content exceeds the {MaximumContentBytes}-byte fixture limit.");
        }

        string baseRoot;
        string root;
        try
        {
            baseRoot = AllowedBaseRoot;
            root = Path.GetFullPath(request.TestOwnedRootPath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return Failure(
                NodalOsTestOwnedFileCreateDecision.RootOutsideFixtureBoundary,
                "INVALID",
                "INVALID",
                string.Empty,
                "The test-owned fixture root is invalid.");
        }

        var rootFingerprint = Fingerprint(root);
        if (!IsDirectFixtureChild(baseRoot, root))
        {
            return Failure(
                NodalOsTestOwnedFileCreateDecision.RootOutsideFixtureBoundary,
                rootFingerprint,
                "INVALID",
                string.Empty,
                "The file action is restricted to a unique child of the NODAL OS test fixture root.");
        }

        if (!TryNormalizeRelativePath(request.RelativePath, out var relativePath))
        {
            return Failure(
                NodalOsTestOwnedFileCreateDecision.InvalidRelativePath,
                rootFingerprint,
                "INVALID",
                string.Empty,
                "Only output/<name>.md or output/<name>.txt is allowed.");
        }

        var target = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var targetFingerprint = Fingerprint(target);
        if (!IsInside(root, target))
        {
            return Failure(
                NodalOsTestOwnedFileCreateDecision.InvalidRelativePath,
                rootFingerprint,
                targetFingerprint,
                relativePath,
                "The target escaped the test-owned fixture root.");
        }

        string? tempPath = null;
        var targetCreated = false;
        try
        {
            Directory.CreateDirectory(baseRoot);
            if (IsReparsePoint(baseRoot))
                return ReparseFailure(rootFingerprint, targetFingerprint, relativePath);

            if (Directory.Exists(root) && IsReparsePoint(root))
                return ReparseFailure(rootFingerprint, targetFingerprint, relativePath);

            Directory.CreateDirectory(root);
            if (IsReparsePoint(root))
                return ReparseFailure(rootFingerprint, targetFingerprint, relativePath);

            var outputDirectory = Path.GetDirectoryName(target)!;
            if (Directory.Exists(outputDirectory) && IsReparsePoint(outputDirectory))
                return ReparseFailure(rootFingerprint, targetFingerprint, relativePath);

            Directory.CreateDirectory(outputDirectory);
            if (IsReparsePoint(outputDirectory))
                return ReparseFailure(rootFingerprint, targetFingerprint, relativePath);

            if (File.Exists(target) || Directory.Exists(target))
            {
                return Failure(
                    NodalOsTestOwnedFileCreateDecision.TargetAlreadyExists,
                    rootFingerprint,
                    targetFingerprint,
                    relativePath,
                    "The create-only target already exists; overwrite was rejected.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(target);
            tempPath = Path.Combine(outputDirectory, $".{fileName}.{Guid.NewGuid():N}.tmp");
            using (var stream = new FileStream(
                       tempPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 4096,
                       FileOptions.WriteThrough))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(tempPath, target, overwrite: false);
            tempPath = null;
            targetCreated = true;

            var persisted = File.ReadAllBytes(target);
            var expectedHash = Sha256(bytes);
            var actualHash = Sha256(persisted);
            if (!bytes.AsSpan().SequenceEqual(persisted) ||
                !string.Equals(expectedHash, actualHash, StringComparison.Ordinal))
            {
                TryDelete(target);
                targetCreated = false;
                return Failure(
                    NodalOsTestOwnedFileCreateDecision.VerificationFailed,
                    rootFingerprint,
                    targetFingerprint,
                    relativePath,
                    "The created fixture file did not pass byte-for-byte verification.");
            }

            var evidence = new NodalOsEvidenceBridgeRef
            {
                EvidenceId = $"evidence-file-create-{request.OperationId}",
                Kind = "test-owned-file-create-verification",
                Ref = null,
                Hash = actualHash,
                SourceKind = NodalOsEvidenceBridgeSourceKind.VerificationGate,
                UseKind = NodalOsEvidenceBridgeUseKind.VerificationSupport,
                Authority = NodalOsEvidenceBridgeAuthority.SupportsVerificationOnly,
                Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
                RedactionState = NodalOsEvidenceRedactionState.NotRequired,
                LedgerRef = $"fixture-file-create:{request.OperationId}",
                Provenance = "Create-only atomic write inside a unique NODAL OS test-owned fixture root.",
                CreatedAt = DateTimeOffset.UtcNow
            };

            return new NodalOsTestOwnedFileCreateResult(
                NodalOsTestOwnedFileCreateDecision.CreatedAndVerified,
                Success: true,
                Created: true,
                Verified: true,
                OverwriteAttempted: false,
                NetworkUsed: false,
                ExternalProcessUsed: false,
                RootFingerprint: rootFingerprint,
                TargetFingerprint: targetFingerprint,
                RelativePath: relativePath,
                ContentSha256: actualHash,
                BytesWritten: persisted.LongLength,
                SafeMessage: "A new test-owned text file was created atomically and verified.",
                Evidence: evidence);
        }
        catch (OperationCanceledException)
        {
            if (targetCreated)
                TryDelete(target);
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            if (targetCreated)
                TryDelete(target);
            return Failure(
                NodalOsTestOwnedFileCreateDecision.WriteFailed,
                rootFingerprint,
                targetFingerprint,
                relativePath,
                "The test-owned create-only write failed closed.");
        }
        finally
        {
            if (tempPath is not null)
                TryDelete(tempPath);
        }
    }

    public NodalOsTestOwnedFixtureCleanupResult CleanupOwnedRoot(
        string testOwnedRootPath,
        string expectedRootFingerprint)
    {
        string root;
        try
        {
            root = Path.GetFullPath(testOwnedRootPath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new(false, false, "INVALID_ROOT", "Fixture cleanup root is invalid.");
        }

        if (!IsDirectFixtureChild(AllowedBaseRoot, root) ||
            !string.Equals(Fingerprint(root), expectedRootFingerprint, StringComparison.Ordinal))
        {
            return new(false, false, "CLEANUP_BOUNDARY_REJECTED", "Fixture cleanup boundary verification failed.");
        }

        if (!Directory.Exists(root))
            return new(true, false, "ALREADY_ABSENT", "Test-owned fixture root was already absent.");

        try
        {
            if (ContainsReparsePoint(root))
                return new(false, false, "CLEANUP_REPARSE_REJECTED", "Fixture cleanup rejected a reparse point.");

            Directory.Delete(root, recursive: true);
            var removed = !Directory.Exists(root);
            return new(removed, removed, removed ? "CLEANED" : "CLEANUP_FAILED", removed
                ? "Test-owned fixture root was removed."
                : "Test-owned fixture cleanup failed closed.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            return new(false, false, "CLEANUP_FAILED", "Test-owned fixture cleanup failed closed.");
        }
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

    private static bool ContainsReparsePoint(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            if (IsReparsePoint(directory))
                return true;

            foreach (var entry in Directory.EnumerateFileSystemEntries(directory, "*", SearchOption.TopDirectoryOnly))
            {
                if (IsReparsePoint(entry))
                    return true;
                if (Directory.Exists(entry))
                    pending.Push(entry);
            }
        }

        return false;
    }

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private static bool IsSafeIdentifier(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 128 &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');

    private static string Fingerprint(string value) =>
        Sha256(Encoding.UTF8.GetBytes(Path.GetFullPath(value)));

    private static string Sha256(ReadOnlySpan<byte> value) =>
        Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private static NodalOsTestOwnedFileCreateResult ReparseFailure(
        string rootFingerprint,
        string targetFingerprint,
        string relativePath) =>
        Failure(
            NodalOsTestOwnedFileCreateDecision.ReparsePointRejected,
            rootFingerprint,
            targetFingerprint,
            relativePath,
            "A reparse point was rejected inside the test-owned fixture boundary.");

    private static NodalOsTestOwnedFileCreateResult Failure(
        NodalOsTestOwnedFileCreateDecision decision,
        string rootFingerprint,
        string targetFingerprint,
        string relativePath,
        string safeMessage) =>
        new(
            decision,
            Success: false,
            Created: false,
            Verified: false,
            OverwriteAttempted: false,
            NetworkUsed: false,
            ExternalProcessUsed: false,
            RootFingerprint: rootFingerprint,
            TargetFingerprint: targetFingerprint,
            RelativePath: relativePath,
            ContentSha256: null,
            BytesWritten: 0,
            SafeMessage: safeMessage,
            Evidence: null);

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
        }
    }
}
