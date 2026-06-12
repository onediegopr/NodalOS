using System.Text.Json;

namespace OneBrain.Core.Confidence;

public static class RecipeConfidenceArtifactWriter
{
    public const string SchemaVersion = "onebrain-recipe-confidence/v1";
    public const string RelativeConfidenceDirectory = "artifacts/recipe-confidence";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static RecipeConfidenceArtifactWriteResult Write(string baseDirectory, RecipeConfidenceProfile profile)
    {
        try
        {
            var root = GetRoot(baseDirectory);
            Directory.CreateDirectory(root);

            var fullPath = Path.GetFullPath(Path.Combine(root, BuildFileName(profile)));
            EnsureInsideRoot(root, fullPath);
            fullPath = BuildUniquePath(fullPath);

            File.WriteAllText(fullPath, JsonSerializer.Serialize(new
            {
                schemaVersion = SchemaVersion,
                profile
            }, JsonOptions));

            return new RecipeConfidenceArtifactWriteResult
            {
                Success = true,
                Path = fullPath,
                RelativePath = ToRelativePath(baseDirectory, fullPath)
            };
        }
        catch (Exception ex)
        {
            return new RecipeConfidenceArtifactWriteResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public static string BuildFileName(RecipeConfidenceProfile profile)
    {
        var timestamp = DateTimeOffset.TryParse(profile.LastVerifiedAt, out var parsed)
            ? parsed.UtcDateTime.ToString("yyyyMMdd-HHmmss")
            : DateTimeOffset.UtcNow.UtcDateTime.ToString("yyyyMMdd-HHmmss");

        return $"{timestamp}-{SanitizeSegment(FirstNonEmpty(profile.RecipeId, profile.CandidateFlowId, "unknown"))}-confidence.json";
    }

    private static string GetRoot(string baseDirectory)
    {
        return Path.GetFullPath(Path.Combine(Path.GetFullPath(baseDirectory), RelativeConfidenceDirectory.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static void EnsureInsideRoot(string root, string fullPath)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(fullPath).StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("recipe confidence artifact path escaped artifacts root");
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

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }
}
