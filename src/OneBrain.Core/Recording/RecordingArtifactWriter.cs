using System.Text.Json;

namespace OneBrain.Core.Recording;

public static class RecordingArtifactWriter
{
    public const string RecordingSchemaVersion = "onebrain-recording-session/v1";
    public const string TimelineSchemaVersion = "onebrain-recipe-timeline/v1";
    public const string RelativeRecordingDirectory = "artifacts/recordings";
    public const string RelativeTimelineDirectory = "artifacts/recipe-timelines";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static RecordingArtifactWriteResult WriteRecording(string baseDirectory, RecordingSession session)
    {
        return Write(baseDirectory, RelativeRecordingDirectory, BuildRecordingFileName(session), new
        {
            schemaVersion = RecordingSchemaVersion,
            session
        });
    }

    public static RecordingArtifactWriteResult WriteTimeline(string baseDirectory, RecipeTimeline timeline)
    {
        return Write(baseDirectory, RelativeTimelineDirectory, BuildTimelineFileName(timeline), new
        {
            schemaVersion = TimelineSchemaVersion,
            timeline
        });
    }

    public static string BuildRecordingFileName(RecordingSession session)
    {
        return $"{TimestampSegment(session.StartedAtUtc)}-{SanitizeSegment(session.RecordingId)}-recording.json";
    }

    public static string BuildTimelineFileName(RecipeTimeline timeline)
    {
        return $"{TimestampSegment(timeline.CreatedAtUtc)}-{SanitizeSegment(timeline.RecordingId)}-timeline.json";
    }

    private static RecordingArtifactWriteResult Write(string baseDirectory, string relativeDirectory, string fileName, object payload)
    {
        try
        {
            var root = GetRoot(baseDirectory, relativeDirectory);
            Directory.CreateDirectory(root);

            var fullPath = Path.GetFullPath(Path.Combine(root, fileName));
            EnsureInsideRoot(root, fullPath);
            fullPath = BuildUniquePath(fullPath);

            File.WriteAllText(fullPath, JsonSerializer.Serialize(payload, JsonOptions));

            return new RecordingArtifactWriteResult
            {
                Success = true,
                Path = fullPath,
                RelativePath = ToRelativePath(baseDirectory, fullPath)
            };
        }
        catch (Exception ex)
        {
            return new RecordingArtifactWriteResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private static string GetRoot(string baseDirectory, string relativeDirectory)
    {
        return Path.GetFullPath(Path.Combine(Path.GetFullPath(baseDirectory), relativeDirectory.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static void EnsureInsideRoot(string root, string fullPath)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(fullPath).StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("recording artifact path escaped artifacts root");
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
}
