using System.Text.Json;

namespace OneBrain.Core.Flows;

public static class PromotedFlowStore
{
    public const string SchemaVersion = "onebrain-promoted-flow/v1";
    public const string RelativeDirectory = "artifacts/promoted-flows";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static PromotedFlowArtifactWriteResult Write(string baseDirectory, PromotedCandidateFlow flow)
    {
        try
        {
            var root = GetRoot(baseDirectory);
            Directory.CreateDirectory(root);
            var fullPath = Path.GetFullPath(Path.Combine(root, BuildFileName(flow)));
            EnsureInsideRoot(root, fullPath);
            fullPath = BuildUniquePath(fullPath);
            File.WriteAllText(fullPath, JsonSerializer.Serialize(new PromotedFlowEnvelope(SchemaVersion, flow), JsonOptions));
            return new PromotedFlowArtifactWriteResult
            {
                Success = true,
                Path = fullPath,
                RelativePath = ToRelativePath(baseDirectory, fullPath)
            };
        }
        catch (Exception ex)
        {
            return new PromotedFlowArtifactWriteResult { Success = false, Error = ex.Message };
        }
    }

    public static IReadOnlyList<PromotedCandidateFlow> ReadAll(string baseDirectory, int maxCount = 50)
    {
        var root = GetRoot(baseDirectory);
        if (!Directory.Exists(root))
            return [];

        return Directory.EnumerateFiles(root, "*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Take(maxCount)
            .Select(ReadOne)
            .Where(flow => flow != null)
            .Cast<PromotedCandidateFlow>()
            .ToList();
    }

    public static PromotedCandidateFlow? ReadById(string baseDirectory, string flowId)
    {
        return ReadAll(baseDirectory, 500).FirstOrDefault(flow =>
            string.Equals(flow.FlowId, flowId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(flow.CandidateFlowId, flowId, StringComparison.OrdinalIgnoreCase));
    }

    public static string BuildFileName(PromotedCandidateFlow flow)
    {
        return $"{TimestampSegment(flow.CreatedAtUtc)}-{SanitizeSegment(flow.FlowId)}-promoted-flow.json";
    }

    private static PromotedCandidateFlow? ReadOne(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<PromotedFlowEnvelope>(File.ReadAllText(path), JsonOptions)?.Flow;
        }
        catch
        {
            return null;
        }
    }

    private static string GetRoot(string baseDirectory)
    {
        return Path.GetFullPath(Path.Combine(Path.GetFullPath(baseDirectory), RelativeDirectory.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static void EnsureInsideRoot(string root, string fullPath)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(fullPath).StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("promoted flow artifact path escaped artifacts root");
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
        var chars = value.Trim().Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? char.ToLowerInvariant(c) : '-').ToArray();
        var sanitized = new string(chars).Trim('-');
        while (sanitized.Contains("--", StringComparison.Ordinal))
            sanitized = sanitized.Replace("--", "-", StringComparison.Ordinal);
        return string.IsNullOrWhiteSpace(sanitized) ? "unknown" : sanitized;
    }

    private static string ToRelativePath(string baseDirectory, string fullPath)
    {
        return Path.GetRelativePath(Path.GetFullPath(baseDirectory), Path.GetFullPath(fullPath)).Replace('\\', '/');
    }

    private sealed record PromotedFlowEnvelope(string SchemaVersion, PromotedCandidateFlow Flow);
}

public static class SupervisedPlaybackStore
{
    public const string SchemaVersion = "onebrain-supervised-playback/v1";
    public const string RelativeDirectory = "artifacts/supervised-playback";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static SupervisedPlaybackArtifactWriteResult Write(string baseDirectory, SupervisedPlaybackSession session)
    {
        try
        {
            var root = GetRoot(baseDirectory);
            Directory.CreateDirectory(root);
            var fullPath = Path.GetFullPath(Path.Combine(root, BuildFileName(session)));
            EnsureInsideRoot(root, fullPath);
            fullPath = BuildUniquePath(fullPath);
            File.WriteAllText(fullPath, JsonSerializer.Serialize(new SupervisedPlaybackEnvelope(SchemaVersion, session), JsonOptions));
            return new SupervisedPlaybackArtifactWriteResult
            {
                Success = true,
                Path = fullPath,
                RelativePath = ToRelativePath(baseDirectory, fullPath)
            };
        }
        catch (Exception ex)
        {
            return new SupervisedPlaybackArtifactWriteResult { Success = false, Error = ex.Message };
        }
    }

    public static IReadOnlyList<SupervisedPlaybackSession> ReadAll(string baseDirectory, int maxCount = 50)
    {
        var root = GetRoot(baseDirectory);
        if (!Directory.Exists(root))
            return [];

        return Directory.EnumerateFiles(root, "*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Take(maxCount)
            .Select(ReadOne)
            .Where(session => session != null)
            .Cast<SupervisedPlaybackSession>()
            .ToList();
    }

    public static SupervisedPlaybackSession? ReadById(string baseDirectory, string playbackId)
    {
        return ReadAll(baseDirectory, 500).FirstOrDefault(session =>
            string.Equals(session.PlaybackId, playbackId, StringComparison.OrdinalIgnoreCase));
    }

    public static string BuildFileName(SupervisedPlaybackSession session)
    {
        return $"{TimestampSegment(session.StartedAtUtc)}-{SanitizeSegment(session.PlaybackId)}-playback.json";
    }

    private static SupervisedPlaybackSession? ReadOne(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<SupervisedPlaybackEnvelope>(File.ReadAllText(path), JsonOptions)?.Session;
        }
        catch
        {
            return null;
        }
    }

    private static string GetRoot(string baseDirectory)
    {
        return Path.GetFullPath(Path.Combine(Path.GetFullPath(baseDirectory), RelativeDirectory.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static void EnsureInsideRoot(string root, string fullPath)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(fullPath).StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("supervised playback artifact path escaped artifacts root");
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
        var chars = value.Trim().Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? char.ToLowerInvariant(c) : '-').ToArray();
        var sanitized = new string(chars).Trim('-');
        while (sanitized.Contains("--", StringComparison.Ordinal))
            sanitized = sanitized.Replace("--", "-", StringComparison.Ordinal);
        return string.IsNullOrWhiteSpace(sanitized) ? "unknown" : sanitized;
    }

    private static string ToRelativePath(string baseDirectory, string fullPath)
    {
        return Path.GetRelativePath(Path.GetFullPath(baseDirectory), Path.GetFullPath(fullPath)).Replace('\\', '/');
    }

    private sealed record SupervisedPlaybackEnvelope(string SchemaVersion, SupervisedPlaybackSession Session);
}
