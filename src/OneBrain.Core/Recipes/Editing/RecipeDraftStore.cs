using System.Text.Json;

namespace OneBrain.Core.Recipes.Editing;

public static class RecipeDraftStore
{
    public const string SchemaVersion = "onebrain-recipe-draft/v1";
    public const string RelativeDraftDirectory = "artifacts/recipe-drafts";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static RecipeDraftArtifactWriteResult Write(string baseDirectory, RecipeDraft draft)
    {
        try
        {
            var policyResult = RecipeEditPolicy.Evaluate(new RecipeEditRequest(
                RecipeId: draft.SourceRecipeId,
                RecipePath: draft.SourceRecipePath,
                Title: draft.Title,
                Description: draft.Description,
                Tags: draft.Tags,
                Notes: draft.Notes,
                HumanReadableLabels: draft.HumanReadableLabels,
                UnsafeFieldAttempts: new Dictionary<string, string>()));

            if (!policyResult.Allowed)
                throw new InvalidOperationException(string.Join("; ", policyResult.Errors));

            var root = GetRoot(baseDirectory);
            Directory.CreateDirectory(root);

            var fullPath = Path.GetFullPath(Path.Combine(root, BuildFileName(draft)));
            EnsureInsideRoot(root, fullPath);
            fullPath = BuildUniquePath(fullPath);

            File.WriteAllText(fullPath, JsonSerializer.Serialize(new RecipeDraftEnvelope(SchemaVersion, draft), JsonOptions));

            return new RecipeDraftArtifactWriteResult
            {
                Success = true,
                Path = fullPath,
                RelativePath = ToRelativePath(baseDirectory, fullPath)
            };
        }
        catch (Exception ex)
        {
            return new RecipeDraftArtifactWriteResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public static IReadOnlyList<RecipeDraft> ReadAll(string baseDirectory, int maxCount = 50)
    {
        var root = GetRoot(baseDirectory);
        if (!Directory.Exists(root))
            return [];

        return Directory.EnumerateFiles(root, "*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Take(maxCount)
            .Select(ReadOne)
            .Where(draft => draft != null)
            .Cast<RecipeDraft>()
            .ToList();
    }

    public static string BuildFileName(RecipeDraft draft)
    {
        return $"{TimestampSegment(draft.CreatedAtUtc)}-{SanitizeSegment(draft.SourceRecipeId)}-{SanitizeSegment(draft.DraftId)}-recipe-draft.json";
    }

    private static RecipeDraft? ReadOne(string path)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<RecipeDraftEnvelope>(File.ReadAllText(path), JsonOptions);
            return envelope?.Draft;
        }
        catch
        {
            return null;
        }
    }

    private static string GetRoot(string baseDirectory)
    {
        return Path.GetFullPath(Path.Combine(Path.GetFullPath(baseDirectory), RelativeDraftDirectory.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static void EnsureInsideRoot(string root, string fullPath)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(fullPath).StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("recipe draft artifact path escaped artifacts root");
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

    private sealed record RecipeDraftEnvelope(string SchemaVersion, RecipeDraft Draft);
}
