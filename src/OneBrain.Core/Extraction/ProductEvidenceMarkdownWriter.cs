namespace OneBrain.Core.Extraction;

public static class ProductEvidenceMarkdownWriter
{
    public const string RelativeReportDirectory = "artifacts/product-evidence-reports";
    public const string RelativeArtifactsDirectory = "artifacts";

    public static ProductEvidenceMarkdownWriteResult Write(
        string baseDirectory,
        ProductEvidenceSummary summary,
        string? outputDirectory = null)
    {
        try
        {
            var outputRoot = ResolveRoot(baseDirectory, outputDirectory, RelativeReportDirectory);
            EnsureInsideRoot(GetDefaultRoot(baseDirectory, RelativeArtifactsDirectory), outputRoot, "output path escaped artifacts root");

            Directory.CreateDirectory(outputRoot);
            var fileName = BuildFileName(summary);
            var fullPath = BuildUniquePath(outputRoot, fileName);
            EnsureInsideRoot(outputRoot, fullPath, "markdown path escaped artifacts/product-evidence-reports root");
            fileName = Path.GetFileName(fullPath);

            var markdown = ProductEvidenceMarkdownRenderer.Render(summary);
            File.WriteAllText(fullPath, markdown);

            return new ProductEvidenceMarkdownWriteResult
            {
                Success = true,
                Path = fullPath,
                RelativePath = Path.GetRelativePath(baseDirectory, fullPath).Replace('\\', '/'),
                Markdown = markdown
            };
        }
        catch (Exception ex)
        {
            return new ProductEvidenceMarkdownWriteResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public static string BuildFileName(ProductEvidenceSummary summary)
    {
        var created = DateTimeOffset.TryParse(summary.CreatedAtUtc, out var parsed)
            ? parsed.UtcDateTime.ToString("yyyyMMdd-HHmmss")
            : DateTimeOffset.UtcNow.UtcDateTime.ToString("yyyyMMdd-HHmmss");

        return $"{created}-product-evidence-report.md";
    }

    public static string GetDefaultRoot(string baseDirectory, string relativeDirectory = RelativeReportDirectory)
    {
        var fullBase = Path.GetFullPath(baseDirectory);
        return Path.GetFullPath(Path.Combine(fullBase, relativeDirectory.Replace('/', Path.DirectorySeparatorChar)));
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
}
