namespace OneBrain.Pilot;

public static class PilotArtifactLocator
{
    public static string? FindLatestMarkdown(string root)
    {
        return FindLatest(root,
        [
            Path.Combine("artifacts", "product-evidence-demo-reports"),
            Path.Combine("artifacts", "product-evidence-reports")
        ], "*.md");
    }

    public static string? FindLatestHtml(string root)
    {
        return FindLatest(root,
        [
            Path.Combine("artifacts", "product-evidence-demo-html-reports"),
            Path.Combine("artifacts", "product-evidence-html-reports")
        ], "*.html");
    }

    private static string? FindLatest(string root, IReadOnlyList<string> relativeDirs, string pattern)
    {
        var files = new List<FileInfo>();

        foreach (var relativeDir in relativeDirs)
        {
            var dir = Path.GetFullPath(Path.Combine(root, relativeDir));
            var rootFull = Path.GetFullPath(root);
            if (!dir.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!Directory.Exists(dir))
                continue;

            files.AddRange(new DirectoryInfo(dir).GetFiles(pattern, SearchOption.TopDirectoryOnly));
        }

        return files
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Select(file => file.FullName)
            .FirstOrDefault();
    }
}
