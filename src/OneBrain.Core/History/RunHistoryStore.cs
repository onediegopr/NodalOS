using System.Text.Json;

namespace OneBrain.Core.History;

public static class RunHistoryStore
{
    public const string SchemaVersion = "onebrain-run-history/v1";
    public const string RelativeRunHistoryDirectory = "artifacts/run-history";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static RunHistoryArtifactWriteResult Write(string baseDirectory, RunHistoryRecord record)
    {
        try
        {
            EnsureNoSecrets(record);

            var root = GetRoot(baseDirectory);
            Directory.CreateDirectory(root);

            var sanitized = Sanitize(baseDirectory, record);
            var fullPath = Path.GetFullPath(Path.Combine(root, BuildFileName(sanitized)));
            EnsureInsideRoot(root, fullPath);
            fullPath = BuildUniquePath(fullPath);

            File.WriteAllText(fullPath, JsonSerializer.Serialize(new RunHistoryEnvelope(SchemaVersion, sanitized), JsonOptions));

            return new RunHistoryArtifactWriteResult
            {
                Success = true,
                Path = fullPath,
                RelativePath = ToRelativePath(baseDirectory, fullPath)
            };
        }
        catch (Exception ex)
        {
            return new RunHistoryArtifactWriteResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public static IReadOnlyList<RunHistoryRecord> ReadAll(string baseDirectory, int maxCount = 50)
    {
        var root = GetRoot(baseDirectory);
        if (!Directory.Exists(root))
            return [];

        return Directory.EnumerateFiles(root, "*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Take(maxCount)
            .Select(ReadOne)
            .Where(record => record != null)
            .Cast<RunHistoryRecord>()
            .ToList();
    }

    public static RunHistoryRecord? ReadById(string baseDirectory, string runId)
    {
        return ReadAll(baseDirectory, 500).FirstOrDefault(record =>
            string.Equals(record.RunId, runId, StringComparison.OrdinalIgnoreCase));
    }

    public static string BuildFileName(RunHistoryRecord record)
    {
        return $"{TimestampSegment(record.StartedAtUtc)}-{SanitizeSegment(record.RunId)}-run-history.json";
    }

    private static RunHistoryRecord? ReadOne(string path)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<RunHistoryEnvelope>(File.ReadAllText(path), JsonOptions);
            return envelope?.Run;
        }
        catch
        {
            return null;
        }
    }

    private static RunHistoryRecord Sanitize(string baseDirectory, RunHistoryRecord record)
    {
        return record with
        {
            ArtifactPaths = record.ArtifactPaths
                .Select(path => HistorySanitizer.NormalizeArtifactPath(baseDirectory, path))
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToList(),
            ErrorSummary = HistorySanitizer.SanitizeText(record.ErrorSummary),
            Notes = record.Notes.Select(HistorySanitizer.SanitizeText).ToList()
        };
    }

    private static void EnsureNoSecrets(RunHistoryRecord record)
    {
        var values = new List<string?>
        {
            record.RunId,
            record.Source,
            record.RecipeId,
            record.CandidateFlowId,
            record.ApprovalRequestId,
            record.ApprovalDecisionId,
            record.RecordingSessionId,
            record.TimelineId,
            record.ConfidenceId,
            record.AiRoutingDecisionId,
            record.ErrorSummary
        };
        values.AddRange(record.ArtifactPaths);
        values.AddRange(record.Notes);

        if (values.Any(HistorySanitizer.ContainsSecretLikeContent))
            throw new InvalidOperationException("run history contains secret-like content");
    }

    private static string GetRoot(string baseDirectory)
    {
        return Path.GetFullPath(Path.Combine(Path.GetFullPath(baseDirectory), RelativeRunHistoryDirectory.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static void EnsureInsideRoot(string root, string fullPath)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(fullPath).StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("run history artifact path escaped artifacts root");
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

    private sealed record RunHistoryEnvelope(string SchemaVersion, RunHistoryRecord Run);
}
