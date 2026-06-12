using System.Text.Json;
using OneBrain.Core.History;

namespace OneBrain.Core.AppProfiles;

public static class AppProfileStore
{
    public const string SchemaVersion = "onebrain-app-profile/v1";
    public const string RelativeAppProfileDirectory = "artifacts/app-profiles";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static AppProfileArtifactWriteResult Write(string baseDirectory, AppProfile profile)
    {
        try
        {
            EnsureNoSecrets(profile);

            var root = GetRoot(baseDirectory);
            Directory.CreateDirectory(root);

            var sanitized = Sanitize(profile);
            var fullPath = Path.GetFullPath(Path.Combine(root, BuildFileName(sanitized)));
            EnsureInsideRoot(root, fullPath);
            fullPath = BuildUniquePath(fullPath);

            File.WriteAllText(fullPath, JsonSerializer.Serialize(new AppProfileEnvelope(SchemaVersion, sanitized), JsonOptions));

            return new AppProfileArtifactWriteResult
            {
                Success = true,
                Path = fullPath,
                RelativePath = ToRelativePath(baseDirectory, fullPath)
            };
        }
        catch (Exception ex)
        {
            return new AppProfileArtifactWriteResult { Success = false, Error = ex.Message };
        }
    }

    public static IReadOnlyList<AppProfile> ReadAll(string baseDirectory, int maxCount = 100)
    {
        var root = GetRoot(baseDirectory);
        if (!Directory.Exists(root))
            return [];

        return Directory.EnumerateFiles(root, "*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Take(maxCount)
            .Select(ReadOne)
            .Where(profile => profile != null)
            .Cast<AppProfile>()
            .ToList();
    }

    public static AppProfile? ReadById(string baseDirectory, string id)
    {
        return ReadAll(baseDirectory, 500).FirstOrDefault(profile =>
            string.Equals(profile.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public static string BuildFileName(AppProfile profile)
    {
        return $"{SanitizeSegment(profile.Id)}-v{profile.Version.Version:000}-app-profile.json";
    }

    private static AppProfile? ReadOne(string path)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<AppProfileEnvelope>(File.ReadAllText(path), JsonOptions);
            return envelope?.Profile;
        }
        catch
        {
            return null;
        }
    }

    private static AppProfile Sanitize(AppProfile profile)
    {
        return profile with
        {
            Name = HistorySanitizer.SanitizeText(profile.Name),
            AppName = HistorySanitizer.SanitizeText(profile.AppName),
            ProcessName = HistorySanitizer.SanitizeText(profile.ProcessName),
            SiteDomain = HistorySanitizer.SanitizeText(profile.SiteDomain),
            SupportedCapabilities = profile.SupportedCapabilities.Select(HistorySanitizer.SanitizeText).Where(NotBlank).ToList(),
            SelectorAliases = profile.SelectorAliases.Select(alias => alias with
            {
                Alias = HistorySanitizer.SanitizeText(alias.Alias),
                Strategy = HistorySanitizer.SanitizeText(alias.Strategy),
                Value = HistorySanitizer.SanitizeText(alias.Value),
                Notes = HistorySanitizer.SanitizeText(alias.Notes)
            }).ToList(),
            Version = profile.Version with
            {
                ChangeSummary = HistorySanitizer.SanitizeText(profile.Version.ChangeSummary)
            },
            Notes = profile.Notes.Select(HistorySanitizer.SanitizeText).Where(NotBlank).ToList()
        };
    }

    private static void EnsureNoSecrets(AppProfile profile)
    {
        var values = new List<string?>
        {
            profile.Id,
            profile.Name,
            profile.Kind,
            profile.Status,
            profile.AppName,
            profile.ProcessName,
            profile.SiteDomain,
            profile.LastVerifiedAtUtc,
            profile.Version.ChangeSummary,
            profile.Version.Status
        };
        values.AddRange(profile.SupportedCapabilities);
        values.AddRange(profile.SelectorAliases.SelectMany(alias => new[] { alias.Alias, alias.Strategy, alias.Value, alias.Notes }));
        values.AddRange(profile.Notes);

        if (values.Any(HistorySanitizer.ContainsSecretLikeContent))
            throw new InvalidOperationException("app profile contains secret-like content");
    }

    private static string GetRoot(string baseDirectory)
    {
        return Path.GetFullPath(Path.Combine(Path.GetFullPath(baseDirectory), RelativeAppProfileDirectory.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static void EnsureInsideRoot(string root, string fullPath)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(fullPath).StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("app profile artifact path escaped artifacts root");
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

    private static bool NotBlank(string value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    private sealed record AppProfileEnvelope(string SchemaVersion, AppProfile Profile);
}
