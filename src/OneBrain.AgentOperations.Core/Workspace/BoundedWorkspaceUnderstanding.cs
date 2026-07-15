using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace OneBrain.AgentOperations.Core.Workspace;

public sealed record BoundedWorkspaceScanLimits(
    int MaximumFiles = 200,
    long MaximumTotalBytes = 2 * 1024 * 1024,
    int MaximumFileBytes = 64 * 1024,
    int MaximumDepth = 8,
    int MaximumPreviewCharacters = 800)
{
    public void Validate()
    {
        if (MaximumFiles is < 1 or > 10_000)
            throw new ArgumentOutOfRangeException(nameof(MaximumFiles));
        if (MaximumTotalBytes is < 1 or > 512L * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(MaximumTotalBytes));
        if (MaximumFileBytes is < 1 or > 16 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(MaximumFileBytes));
        if (MaximumFileBytes > MaximumTotalBytes)
            throw new ArgumentException("Maximum file bytes cannot exceed the total byte budget.");
        if (MaximumDepth is < 0 or > 64)
            throw new ArgumentOutOfRangeException(nameof(MaximumDepth));
        if (MaximumPreviewCharacters is < 0 or > 16_384)
            throw new ArgumentOutOfRangeException(nameof(MaximumPreviewCharacters));
    }
}

public sealed record BoundedWorkspaceScanRequest(
    string RootPath,
    bool IncludeTextPreviews = true,
    IReadOnlyCollection<string>? AllowedExtensions = null,
    IReadOnlyCollection<string>? ExcludedDirectoryNames = null,
    BoundedWorkspaceScanLimits? Limits = null);

public enum BoundedWorkspaceScanDecision
{
    Accepted,
    InvalidRoot,
    Cancelled,
    FailedClosed
}

public sealed record BoundedWorkspaceFileSummary(
    string RelativePathRedacted,
    string Extension,
    long FileLength,
    int BytesRead,
    string ContentSampleSha256,
    string? PreviewRedacted,
    bool SecretLikeContentRedacted,
    bool FileSampleTruncated);

public sealed record BoundedWorkspaceScanResult(
    BoundedWorkspaceScanDecision Decision,
    string RootFingerprint,
    IReadOnlyList<BoundedWorkspaceFileSummary> Files,
    IReadOnlyDictionary<string, int> ExtensionCounts,
    int FilesSeen,
    int FilesRead,
    int FilesSkipped,
    long TotalBytesRead,
    bool Truncated,
    bool Cancelled,
    bool RealFilesystemRead,
    bool FilesystemMutationAllowed,
    bool NetworkUsed,
    bool ProductAuthorityGranted,
    bool SecretsExcluded,
    string EvidenceDigest,
    IReadOnlyList<string> Findings);

public sealed class BoundedWorkspaceUnderstandingService
{
    private static readonly string[] DefaultAllowedExtensions =
    [
        ".cs", ".csproj", ".props", ".targets", ".sln", ".slnx",
        ".json", ".jsonc", ".md", ".txt", ".xml", ".yml", ".yaml",
        ".ts", ".tsx", ".js", ".jsx", ".mjs", ".cjs",
        ".rs", ".toml", ".py", ".html", ".css", ".scss"
    ];

    private static readonly string[] DefaultExcludedDirectories =
    [
        ".git", ".hg", ".svn", ".idea", ".vs", ".vscode",
        "node_modules", "bin", "obj", "target", "dist", "build",
        "coverage", ".next", ".nuxt", ".turbo", "artifacts"
    ];

    private static readonly Regex AssignmentSecretRegex = new(
        "(?im)(api[_-]?key|access[_-]?token|refresh[_-]?token|authorization|password|passwd|secret|connection[_-]?string)\\s*[:=]\\s*[\\\"']?([^\\s\\\"',;]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex ApiKeyRegex = new(
        "(?:sk-[A-Za-z0-9_-]{16,}|gh[pousr]_[A-Za-z0-9]{20,}|AKIA[0-9A-Z]{16})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex ConnectionSecretRegex = new(
        "(?im)(AccountKey|SharedAccessKey|ClientSecret|Password)=[^;\\r\\n]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    public async ValueTask<BoundedWorkspaceScanResult> ScanAsync(
        BoundedWorkspaceScanRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var limits = request.Limits ?? new BoundedWorkspaceScanLimits();
        limits.Validate();

        if (cancellationToken.IsCancellationRequested)
            return CancelledResult("Scan cancelled before the workspace was opened.");

        string root;
        try
        {
            root = Path.GetFullPath(request.RootPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return InvalidRoot("Workspace root is invalid.");
        }

        if (!Directory.Exists(root))
            return InvalidRoot("Workspace root does not exist.");

        try
        {
            if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
                return InvalidRoot("Workspace root cannot be a symbolic link or reparse point.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return InvalidRoot("Workspace root attributes could not be verified.");
        }

        var allowedExtensions = new HashSet<string>(
            request.AllowedExtensions ?? DefaultAllowedExtensions,
            StringComparer.OrdinalIgnoreCase);
        var excludedDirectories = new HashSet<string>(
            request.ExcludedDirectoryNames ?? DefaultExcludedDirectories,
            StringComparer.OrdinalIgnoreCase);
        var files = new List<BoundedWorkspaceFileSummary>();
        var findings = new List<string>();
        var pending = new Queue<(string Path, int Depth)>();
        pending.Enqueue((root, 0));
        var filesSeen = 0;
        var filesSkipped = 0;
        long totalBytesRead = 0;
        var truncated = false;

        try
        {
            while (pending.Count > 0 && !truncated)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (directory, depth) = pending.Dequeue();
                if (depth > limits.MaximumDepth)
                {
                    truncated = true;
                    findings.Add("Maximum directory depth reached.");
                    break;
                }

                IEnumerable<string> childDirectories;
                IEnumerable<string> childFiles;
                try
                {
                    childDirectories = Directory.EnumerateDirectories(directory)
                        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    childFiles = Directory.EnumerateFiles(directory)
                        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    findings.Add("An unreadable directory was skipped.");
                    continue;
                }

                foreach (var childDirectory in childDirectories)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var name = Path.GetFileName(childDirectory);
                    if (excludedDirectories.Contains(name) || IsSensitiveName(name) || IsHiddenSystemOrReparse(childDirectory))
                    {
                        filesSkipped++;
                        continue;
                    }

                    if (IsInsideRoot(root, childDirectory))
                        pending.Enqueue((childDirectory, depth + 1));
                    else
                        filesSkipped++;
                }

                foreach (var path in childFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    filesSeen++;
                    if (files.Count >= limits.MaximumFiles || totalBytesRead >= limits.MaximumTotalBytes)
                    {
                        truncated = true;
                        findings.Add("Workspace file or byte budget reached.");
                        break;
                    }

                    var fileName = Path.GetFileName(path);
                    var extension = Path.GetExtension(path);
                    if (IsSensitiveName(fileName) || !allowedExtensions.Contains(extension) || IsHiddenSystemOrReparse(path))
                    {
                        filesSkipped++;
                        continue;
                    }

                    if (!IsInsideRoot(root, path))
                    {
                        filesSkipped++;
                        continue;
                    }

                    FileInfo info;
                    try
                    {
                        info = new FileInfo(path);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        filesSkipped++;
                        continue;
                    }

                    var remainingBudget = limits.MaximumTotalBytes - totalBytesRead;
                    var requestedBytes = (int)Math.Min(Math.Min(info.Length, limits.MaximumFileBytes), remainingBudget);
                    if (requestedBytes <= 0)
                    {
                        truncated = true;
                        findings.Add("Workspace byte budget reached.");
                        break;
                    }

                    byte[] sample;
                    try
                    {
                        sample = await ReadSampleAsync(path, requestedBytes, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        filesSkipped++;
                        findings.Add("A file that changed or became unreadable was skipped.");
                        continue;
                    }

                    totalBytesRead += sample.Length;
                    var sampleTruncated = info.Length > sample.Length;
                    var contentHash = Convert.ToHexString(SHA256.HashData(sample)).ToLowerInvariant();
                    string? preview = null;
                    var redacted = false;
                    if (request.IncludeTextPreviews && limits.MaximumPreviewCharacters > 0 && !LooksBinary(sample))
                    {
                        var decoded = Encoding.UTF8.GetString(sample);
                        var redaction = Redact(decoded);
                        redacted = redaction.Redacted;
                        preview = NormalizeAndLimit(redaction.Value, limits.MaximumPreviewCharacters);
                    }

                    files.Add(new BoundedWorkspaceFileSummary(
                        RelativePathRedacted: SanitizeRelativePath(Path.GetRelativePath(root, path)),
                        Extension: extension.ToLowerInvariant(),
                        FileLength: info.Length,
                        BytesRead: sample.Length,
                        ContentSampleSha256: contentHash,
                        PreviewRedacted: preview,
                        SecretLikeContentRedacted: redacted,
                        FileSampleTruncated: sampleTruncated));
                }
            }
        }
        catch (OperationCanceledException)
        {
            return BuildResult(
                BoundedWorkspaceScanDecision.Cancelled,
                root,
                files,
                filesSeen,
                filesSkipped,
                totalBytesRead,
                truncated: true,
                cancelled: true,
                findings.Append("Scan cancelled by the operator.").ToArray());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return BuildResult(
                BoundedWorkspaceScanDecision.FailedClosed,
                root,
                files,
                filesSeen,
                filesSkipped,
                totalBytesRead,
                truncated: true,
                cancelled: false,
                findings.Append("Workspace scan failed closed after an IO boundary changed.").ToArray());
        }

        return BuildResult(
            BoundedWorkspaceScanDecision.Accepted,
            root,
            files,
            filesSeen,
            filesSkipped,
            totalBytesRead,
            truncated,
            cancelled: false,
            findings);
    }

    private static BoundedWorkspaceScanResult BuildResult(
        BoundedWorkspaceScanDecision decision,
        string root,
        IReadOnlyList<BoundedWorkspaceFileSummary> files,
        int filesSeen,
        int filesSkipped,
        long totalBytesRead,
        bool truncated,
        bool cancelled,
        IReadOnlyList<string> findings)
    {
        var ordered = files.OrderBy(value => value.RelativePathRedacted, StringComparer.OrdinalIgnoreCase).ToArray();
        var canonical = string.Join("\n", ordered.Select(value =>
            $"{value.RelativePathRedacted}|{value.FileLength}|{value.BytesRead}|{value.ContentSampleSha256}|{value.SecretLikeContentRedacted}|{value.FileSampleTruncated}"));
        var evidenceDigest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        var rootFingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            "nodal-workspace-v1|" + root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToUpperInvariant())))
            .ToLowerInvariant();
        var extensionCounts = ordered
            .GroupBy(value => value.Extension, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        return new BoundedWorkspaceScanResult(
            Decision: decision,
            RootFingerprint: rootFingerprint,
            Files: ordered,
            ExtensionCounts: extensionCounts,
            FilesSeen: filesSeen,
            FilesRead: ordered.Length,
            FilesSkipped: filesSkipped,
            TotalBytesRead: totalBytesRead,
            Truncated: truncated,
            Cancelled: cancelled,
            RealFilesystemRead: ordered.Length > 0,
            FilesystemMutationAllowed: false,
            NetworkUsed: false,
            ProductAuthorityGranted: false,
            SecretsExcluded: true,
            EvidenceDigest: evidenceDigest,
            Findings: findings.Distinct(StringComparer.Ordinal).ToArray());
    }

    private static BoundedWorkspaceScanResult InvalidRoot(string finding) => new(
        BoundedWorkspaceScanDecision.InvalidRoot,
        RootFingerprint: string.Empty,
        Files: Array.Empty<BoundedWorkspaceFileSummary>(),
        ExtensionCounts: new Dictionary<string, int>(),
        FilesSeen: 0,
        FilesRead: 0,
        FilesSkipped: 0,
        TotalBytesRead: 0,
        Truncated: false,
        Cancelled: false,
        RealFilesystemRead: false,
        FilesystemMutationAllowed: false,
        NetworkUsed: false,
        ProductAuthorityGranted: false,
        SecretsExcluded: true,
        EvidenceDigest: EmptyDigest(),
        Findings: [finding]);

    private static BoundedWorkspaceScanResult CancelledResult(string finding) => new(
        BoundedWorkspaceScanDecision.Cancelled,
        RootFingerprint: string.Empty,
        Files: Array.Empty<BoundedWorkspaceFileSummary>(),
        ExtensionCounts: new Dictionary<string, int>(),
        FilesSeen: 0,
        FilesRead: 0,
        FilesSkipped: 0,
        TotalBytesRead: 0,
        Truncated: true,
        Cancelled: true,
        RealFilesystemRead: false,
        FilesystemMutationAllowed: false,
        NetworkUsed: false,
        ProductAuthorityGranted: false,
        SecretsExcluded: true,
        EvidenceDigest: EmptyDigest(),
        Findings: [finding]);

    private static async ValueTask<byte[]> ReadSampleAsync(
        string path,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: Math.Min(maximumBytes, 16 * 1024),
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var buffer = new byte[maximumBytes];
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(total), cancellationToken).ConfigureAwait(false);
            if (read == 0)
                break;
            total += read;
        }

        return total == buffer.Length ? buffer : buffer[..total];
    }

    private static bool IsInsideRoot(string root, string candidate)
    {
        var full = Path.GetFullPath(candidate);
        var relative = Path.GetRelativePath(root, full);
        return !Path.IsPathRooted(relative) &&
               !relative.Equals("..", StringComparison.Ordinal) &&
               !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
               !relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal);
    }

    private static bool IsHiddenSystemOrReparse(string path)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            return (attributes & (FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint)) != 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return true;
        }
    }

    private static bool IsSensitiveName(string name)
    {
        var normalized = name.Trim().ToLowerInvariant();
        return normalized is ".env" or ".env.local" or ".env.development" or ".env.production" ||
               normalized.Contains("credential", StringComparison.Ordinal) ||
               normalized.Contains("private-key", StringComparison.Ordinal) ||
               normalized.Contains("private_key", StringComparison.Ordinal) ||
               normalized.EndsWith(".pem", StringComparison.Ordinal) ||
               normalized.EndsWith(".pfx", StringComparison.Ordinal) ||
               normalized.EndsWith(".p12", StringComparison.Ordinal) ||
               normalized.EndsWith(".key", StringComparison.Ordinal) ||
               normalized.EndsWith(".kdbx", StringComparison.Ordinal);
    }

    private static bool LooksBinary(ReadOnlySpan<byte> sample)
    {
        if (sample.IsEmpty)
            return false;
        var control = 0;
        foreach (var value in sample)
        {
            if (value == 0)
                return true;
            if (value < 0x09 || value is > 0x0D and < 0x20)
                control++;
        }
        return control > Math.Max(2, sample.Length / 20);
    }

    private static (string Value, bool Redacted) Redact(string value)
    {
        var redacted = false;
        string Replace(Regex regex, string input, MatchEvaluator evaluator)
        {
            var output = regex.Replace(input, match =>
            {
                redacted = true;
                return evaluator(match);
            });
            return output;
        }

        var result = Replace(AssignmentSecretRegex, value, match => $"{match.Groups[1].Value}=[REDACTED]");
        result = Replace(ApiKeyRegex, result, _ => "[REDACTED_TOKEN]");
        result = Replace(ConnectionSecretRegex, result, match => $"{match.Groups[1].Value}=[REDACTED]");
        if (result.Contains("-----BEGIN PRIVATE KEY-----", StringComparison.OrdinalIgnoreCase) ||
            result.Contains("-----BEGIN RSA PRIVATE KEY-----", StringComparison.OrdinalIgnoreCase) ||
            result.Contains("-----BEGIN OPENSSH PRIVATE KEY-----", StringComparison.OrdinalIgnoreCase))
        {
            redacted = true;
            result = "[REDACTED_PRIVATE_KEY_FILE]";
        }
        return (result, redacted);
    }

    private static string NormalizeAndLimit(string value, int maximumCharacters)
    {
        var normalized = value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Trim();
        return normalized.Length <= maximumCharacters
            ? normalized
            : normalized[..Math.Max(0, maximumCharacters - 1)] + "…";
    }

    private static string SanitizeRelativePath(string value)
    {
        var normalized = value.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => IsSensitiveName(segment) ? "[REDACTED_NAME]" : segment)
            .ToArray();
        return string.Join('/', segments);
    }

    private static string EmptyDigest() =>
        Convert.ToHexString(SHA256.HashData(Array.Empty<byte>())).ToLowerInvariant();
}
