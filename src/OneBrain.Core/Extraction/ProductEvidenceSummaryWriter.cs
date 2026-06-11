using System.Text.Json;

namespace OneBrain.Core.Extraction;

public static class ProductEvidenceSummaryWriter
{
    public const string RelativeInputDirectory = "artifacts/product-evidence";
    public const string RelativeSummaryDirectory = "artifacts/product-evidence-summary";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static ProductEvidenceSummaryWriteResult WriteFromDirectory(
        string baseDirectory,
        string? inputDirectory = null,
        string? outputDirectory = null,
        DateTimeOffset? createdAtUtc = null)
    {
        try
        {
            var inputRoot = ResolveRoot(baseDirectory, inputDirectory, RelativeInputDirectory);
            var outputRoot = ResolveRoot(baseDirectory, outputDirectory, RelativeSummaryDirectory);

            EnsureInsideRoot(GetDefaultRoot(baseDirectory, RelativeInputDirectory), inputRoot, "input path escaped artifacts/product-evidence root");
            EnsureInsideRoot(GetDefaultRoot(baseDirectory, RelativeSummaryDirectory), outputRoot, "output path escaped artifacts/product-evidence-summary root");

            var sources = LoadSources(inputRoot, baseDirectory);
            var summary = ProductEvidenceSummaryBuilder.Build(sources, createdAtUtc);

            Directory.CreateDirectory(outputRoot);
            var fileName = BuildFileName(summary);
            var fullPath = BuildUniquePath(outputRoot, fileName);
            EnsureInsideRoot(outputRoot, fullPath, "summary path escaped artifacts/product-evidence-summary root");
            fileName = Path.GetFileName(fullPath);

            File.WriteAllText(fullPath, JsonSerializer.Serialize(summary, JsonOptions));

            return new ProductEvidenceSummaryWriteResult
            {
                Success = true,
                Path = fullPath,
                RelativePath = Path.Combine(RelativeSummaryDirectory, fileName).Replace('\\', '/'),
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            return new ProductEvidenceSummaryWriteResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public static IReadOnlyList<ProductEvidenceSummarySource> LoadSources(string inputRoot, string baseDirectory)
    {
        if (!Directory.Exists(inputRoot))
            return [];

        var files = Directory.GetFiles(inputRoot, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sources = new List<ProductEvidenceSummarySource>();
        foreach (var file in files)
        {
            var relativePath = ToRelativePath(baseDirectory, file);
            try
            {
                var json = File.ReadAllText(file);
                var artifact = JsonSerializer.Deserialize<ProductEvidenceArtifact>(json, JsonOptions);
                if (artifact == null || !artifact.SchemaVersion.Equals(ProductEvidenceArtifactWriter.SchemaVersion, StringComparison.OrdinalIgnoreCase))
                {
                    sources.Add(new ProductEvidenceSummarySource
                    {
                        ArtifactPath = relativePath,
                        Error = "unsupported or missing product evidence artifact schema"
                    });
                    continue;
                }

                sources.Add(new ProductEvidenceSummarySource
                {
                    Artifact = artifact,
                    ArtifactPath = relativePath
                });
            }
            catch (Exception ex)
            {
                sources.Add(new ProductEvidenceSummarySource
                {
                    ArtifactPath = relativePath,
                    Error = ex.Message
                });
            }
        }

        return sources;
    }

    public static string BuildFileName(ProductEvidenceSummary summary)
    {
        var created = DateTimeOffset.TryParse(summary.CreatedAtUtc, out var parsed)
            ? parsed.UtcDateTime.ToString("yyyyMMdd-HHmmss")
            : DateTimeOffset.UtcNow.UtcDateTime.ToString("yyyyMMdd-HHmmss");

        return $"{created}-product-evidence-summary.json";
    }

    private static string BuildUniquePath(string outputRoot, string fileName)
    {
        var candidate = Path.GetFullPath(Path.Combine(outputRoot, fileName));
        if (!File.Exists(candidate))
            return candidate;

        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        for (var i = 2; i < 1000; i++)
        {
            candidate = Path.GetFullPath(Path.Combine(outputRoot, $"{name}-{i}{extension}"));
            if (!File.Exists(candidate))
                return candidate;
        }

        return Path.GetFullPath(Path.Combine(outputRoot, $"{name}-{Guid.NewGuid():N}{extension}"));
    }

    public static string GetDefaultRoot(string baseDirectory, string relativeDirectory)
    {
        var fullBase = Path.GetFullPath(baseDirectory);
        return Path.GetFullPath(Path.Combine(fullBase, relativeDirectory.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static string ResolveRoot(string baseDirectory, string? requestedDirectory, string defaultRelativeDirectory)
    {
        if (string.IsNullOrWhiteSpace(requestedDirectory))
            return GetDefaultRoot(baseDirectory, defaultRelativeDirectory);

        return Path.IsPathRooted(requestedDirectory)
            ? Path.GetFullPath(requestedDirectory)
            : Path.GetFullPath(Path.Combine(baseDirectory, requestedDirectory));
    }

    private static void EnsureInsideRoot(string root, string fullPath, string message)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var checkedPath = Path.GetFullPath(fullPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!checkedPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase) &&
            !checkedPath.Equals(fullRoot.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(message);
    }

    private static string ToRelativePath(string baseDirectory, string file)
    {
        var relative = Path.GetRelativePath(baseDirectory, file);
        return relative.Replace('\\', '/');
    }
}
