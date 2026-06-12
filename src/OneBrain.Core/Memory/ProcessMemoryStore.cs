using System.Text.Json;
using OneBrain.Core.History;

namespace OneBrain.Core.Memory;

public static class ProcessMemoryStore
{
    public const string SchemaVersion = "onebrain-process-memory/v1";
    public const string RelativeProcessMemoryDirectory = "artifacts/process-memory";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static ProcessMemoryArtifactWriteResult Write(string baseDirectory, ProcessMemoryEntry entry)
    {
        try
        {
            EnsureNoSecrets(entry);

            var root = GetRoot(baseDirectory);
            Directory.CreateDirectory(root);

            var sanitized = Sanitize(baseDirectory, entry);
            var fullPath = Path.GetFullPath(Path.Combine(root, BuildFileName(sanitized)));
            EnsureInsideRoot(root, fullPath);
            fullPath = BuildUniquePath(fullPath);

            File.WriteAllText(fullPath, JsonSerializer.Serialize(new ProcessMemoryEnvelope(SchemaVersion, sanitized), JsonOptions));

            return new ProcessMemoryArtifactWriteResult
            {
                Success = true,
                Path = fullPath,
                RelativePath = ToRelativePath(baseDirectory, fullPath)
            };
        }
        catch (Exception ex)
        {
            return new ProcessMemoryArtifactWriteResult { Success = false, Error = ex.Message };
        }
    }

    public static IReadOnlyList<ProcessMemoryEntry> ReadAll(string baseDirectory, int maxCount = 100)
    {
        var root = GetRoot(baseDirectory);
        if (!Directory.Exists(root))
            return [];

        return Directory.EnumerateFiles(root, "*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Take(maxCount)
            .Select(ReadOne)
            .Where(entry => entry != null)
            .Cast<ProcessMemoryEntry>()
            .ToList();
    }

    public static ProcessMemoryEntry? ReadById(string baseDirectory, string id)
    {
        return ReadAll(baseDirectory, 500).FirstOrDefault(entry =>
            string.Equals(entry.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public static string BuildFileName(ProcessMemoryEntry entry)
    {
        return $"{TimestampSegment(entry.CreatedAtUtc)}-{SanitizeSegment(entry.Id)}-process-memory.json";
    }

    private static ProcessMemoryEntry? ReadOne(string path)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<ProcessMemoryEnvelope>(File.ReadAllText(path), JsonOptions);
            return envelope?.Entry;
        }
        catch
        {
            return null;
        }
    }

    private static ProcessMemoryEntry Sanitize(string baseDirectory, ProcessMemoryEntry entry)
    {
        return entry with
        {
            Title = HistorySanitizer.SanitizeText(entry.Title),
            Description = HistorySanitizer.SanitizeText(entry.Description),
            AppOrSite = HistorySanitizer.SanitizeText(entry.AppOrSite),
            Domain = HistorySanitizer.SanitizeText(entry.Domain),
            Tags = entry.Tags.Select(HistorySanitizer.SanitizeText).Where(NotBlank).ToList(),
            Summary = entry.Summary with
            {
                Summary = HistorySanitizer.SanitizeText(entry.Summary.Summary),
                StepSummaries = entry.Summary.StepSummaries.Select(HistorySanitizer.SanitizeText).Where(NotBlank).ToList(),
                KeyRisks = entry.Summary.KeyRisks.Select(HistorySanitizer.SanitizeText).Where(NotBlank).ToList(),
                NextActions = entry.Summary.NextActions.Select(HistorySanitizer.SanitizeText).Where(NotBlank).ToList()
            },
            Decisions = entry.Decisions.Select(decision => decision with
            {
                Summary = HistorySanitizer.SanitizeText(decision.Summary),
                Reason = HistorySanitizer.SanitizeText(decision.Reason),
                Outcome = HistorySanitizer.SanitizeText(decision.Outcome)
            }).ToList(),
            Errors = entry.Errors.Select(error => error with
            {
                Code = HistorySanitizer.SanitizeText(error.Code),
                Message = HistorySanitizer.SanitizeText(error.Message)
            }).ToList(),
            EvidenceLinks = entry.EvidenceLinks.Select(link => link with
            {
                RelativePath = HistorySanitizer.NormalizeArtifactPath(baseDirectory, link.RelativePath),
                Label = HistorySanitizer.SanitizeText(link.Label)
            }).ToList(),
            ArtifactPaths = entry.ArtifactPaths
                .Select(path => HistorySanitizer.NormalizeArtifactPath(baseDirectory, path))
                .Where(NotBlank)
                .ToList(),
            Notes = entry.Notes.Select(HistorySanitizer.SanitizeText).Where(NotBlank).ToList()
        };
    }

    private static void EnsureNoSecrets(ProcessMemoryEntry entry)
    {
        var values = new List<string?>
        {
            entry.Id,
            entry.Title,
            entry.Description,
            entry.Source,
            entry.Status,
            entry.AppOrSite,
            entry.Domain,
            entry.RiskLevel,
            entry.Summary.Summary,
            entry.Links.RecordingSessionId,
            entry.Links.TimelineId,
            entry.Links.CandidateFlowId,
            entry.Links.RecipeDraftId,
            entry.Links.RecipeId,
            entry.Links.ApprovalRequestId,
            entry.Links.ApprovalDecisionId,
            entry.Links.RunId,
            entry.Links.AiAuditId,
            entry.Links.ConfidenceId
        };

        values.AddRange(entry.Tags);
        values.AddRange(entry.Summary.StepSummaries);
        values.AddRange(entry.Summary.KeyRisks);
        values.AddRange(entry.Summary.NextActions);
        values.AddRange(entry.Decisions.SelectMany(decision => new[] { decision.Summary, decision.Reason, decision.Outcome }));
        values.AddRange(entry.Errors.SelectMany(error => new[] { error.Code, error.Message }));
        values.AddRange(entry.EvidenceLinks.SelectMany(link => new[] { link.Kind, link.RelativePath, link.Label }));
        values.AddRange(entry.ArtifactPaths);
        values.AddRange(entry.Notes);

        if (values.Any(HistorySanitizer.ContainsSecretLikeContent))
            throw new InvalidOperationException("process memory contains secret-like content");
    }

    private static string GetRoot(string baseDirectory)
    {
        return Path.GetFullPath(Path.Combine(Path.GetFullPath(baseDirectory), RelativeProcessMemoryDirectory.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static void EnsureInsideRoot(string root, string fullPath)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(fullPath).StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("process memory artifact path escaped artifacts root");
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

    private static bool NotBlank(string value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    private sealed record ProcessMemoryEnvelope(string SchemaVersion, ProcessMemoryEntry Entry);
}
