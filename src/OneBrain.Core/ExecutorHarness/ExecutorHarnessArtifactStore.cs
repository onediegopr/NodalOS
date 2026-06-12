using System.Text.Json;

namespace OneBrain.Core.ExecutorHarness;

public static class ExecutorHarnessArtifactStore
{
    public const string SchemaVersion = "onebrain-executor-harness-evidence/v1";
    public const string RelativeDirectory = "artifacts/executor-harness";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static ExecutorHarnessArtifactWriteResult Write(string baseDirectory, ExecutorHarnessEvidenceRecord evidence)
    {
        try
        {
            var root = GetRoot(baseDirectory);
            Directory.CreateDirectory(root);
            var fullPath = Path.GetFullPath(Path.Combine(root, BuildFileName(evidence)));
            EnsureInsideRoot(root, fullPath);
            fullPath = BuildUniquePath(fullPath);
            File.WriteAllText(fullPath, JsonSerializer.Serialize(new ExecutorHarnessEvidenceEnvelope(SchemaVersion, evidence), JsonOptions));

            return new ExecutorHarnessArtifactWriteResult
            {
                Success = true,
                Path = fullPath,
                RelativePath = ToRelativePath(baseDirectory, fullPath)
            };
        }
        catch (Exception ex)
        {
            return new ExecutorHarnessArtifactWriteResult { Success = false, Error = ex.Message };
        }
    }

    public static string BuildFileName(ExecutorHarnessEvidenceRecord evidence)
    {
        return $"{TimestampSegment(evidence.CreatedAtUtc)}-{SanitizeSegment(evidence.EvidenceId)}-executor-harness.json";
    }

    private static string GetRoot(string baseDirectory)
    {
        return Path.GetFullPath(Path.Combine(Path.GetFullPath(baseDirectory), RelativeDirectory.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static void EnsureInsideRoot(string root, string fullPath)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(fullPath).StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("executor harness artifact path escaped artifacts root");
    }

    private static string BuildUniquePath(string fullPath)
    {
        if (!File.Exists(fullPath))
            return fullPath;

        var directory = Path.GetDirectoryName(fullPath)!;
        var name = Path.GetFileNameWithoutExtension(fullPath);
        var extension = Path.GetExtension(fullPath);
        for (var i = 2; i < 1000; i++)
        {
            var candidate = Path.Combine(directory, $"{name}-{i}{extension}");
            if (!File.Exists(candidate))
                return candidate;
        }

        return Path.Combine(directory, $"{name}-{Guid.NewGuid():N}{extension}");
    }

    private static string TimestampSegment(string value)
    {
        return DateTimeOffset.TryParse(value, out var parsed)
            ? parsed.UtcDateTime.ToString("yyyyMMdd-HHmmss")
            : DateTimeOffset.UtcNow.UtcDateTime.ToString("yyyyMMdd-HHmmss");
    }

    private static string SanitizeSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "unknown";

        var chars = value.Trim().Select(c =>
            char.IsLetterOrDigit(c) || c is '-' or '_' ? char.ToLowerInvariant(c) : '-').ToArray();
        var sanitized = new string(chars).Trim('-');
        while (sanitized.Contains("--", StringComparison.Ordinal))
            sanitized = sanitized.Replace("--", "-", StringComparison.Ordinal);
        return string.IsNullOrWhiteSpace(sanitized) ? "unknown" : sanitized;
    }

    private static string ToRelativePath(string baseDirectory, string fullPath)
    {
        return Path.GetRelativePath(Path.GetFullPath(baseDirectory), Path.GetFullPath(fullPath)).Replace('\\', '/');
    }

    private sealed record ExecutorHarnessEvidenceEnvelope(string SchemaVersion, ExecutorHarnessEvidenceRecord Evidence);
}
