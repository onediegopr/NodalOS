using System.Text.Json;
using OneBrain.Core.AI;

namespace OneBrain.Core.History;

public static class AIAuditLogStore
{
    public const string SchemaVersion = "onebrain-ai-audit/v1";
    public const string RelativeAIAuditDirectory = "artifacts/ai-audit";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static AIAuditRecord FromRoutingResult(string aiAuditId, AIModelRouterResult result, bool requiresHumanApproval)
    {
        var profile = result.Profile;
        var decision = result.Decision;
        var fallbackUsed = !string.IsNullOrWhiteSpace(decision.FallbackProfileId);
        var budgetDecision = decision.Status.Contains("budget", StringComparison.OrdinalIgnoreCase)
            ? AIBudgetDecisions.Blocked
            : AIBudgetDecisions.Allowed;

        return new AIAuditRecord(
            AiAuditId: aiAuditId,
            TimestampUtc: DateTimeOffset.UtcNow.ToString("O"),
            RecommendedProfileId: decision.SelectedProfileId,
            UsedProfileId: decision.Success ? decision.SelectedProfileId : null,
            Provider: profile?.Provider ?? "none",
            Model: string.IsNullOrWhiteSpace(profile?.Model) ? "[not configured]" : profile.Model,
            TaskType: result.Request.Capability,
            RiskLevel: result.Request.RiskLevel,
            RequiresVision: result.Request.RequiresVision,
            RequiresHumanApproval: requiresHumanApproval,
            FallbackUsed: fallbackUsed,
            FallbackFrom: fallbackUsed ? decision.SelectedProfileId : null,
            FallbackTo: decision.FallbackProfileId,
            BudgetDecision: budgetDecision,
            EstimatedCostUsd: result.Request.EstimatedCostUsd,
            ActualCostUsd: null,
            TokensIn: null,
            TokensOut: null,
            ResultStatus: decision.Success ? AIAuditResultStatuses.Routed : AIAuditResultStatuses.FailedClosed,
            Reason: decision.Reason,
            Error: decision.Success ? "" : decision.Reason);
    }

    public static AIAuditArtifactWriteResult Write(string baseDirectory, AIAuditRecord record)
    {
        try
        {
            EnsureNoSecrets(record);

            var root = GetRoot(baseDirectory);
            Directory.CreateDirectory(root);

            var sanitized = Sanitize(record);
            var fullPath = Path.GetFullPath(Path.Combine(root, BuildFileName(sanitized)));
            EnsureInsideRoot(root, fullPath);
            fullPath = BuildUniquePath(fullPath);

            File.WriteAllText(fullPath, JsonSerializer.Serialize(new AIAuditEnvelope(SchemaVersion, sanitized), JsonOptions));

            return new AIAuditArtifactWriteResult
            {
                Success = true,
                Path = fullPath,
                RelativePath = ToRelativePath(baseDirectory, fullPath)
            };
        }
        catch (Exception ex)
        {
            return new AIAuditArtifactWriteResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public static IReadOnlyList<AIAuditRecord> ReadAll(string baseDirectory, int maxCount = 50)
    {
        var root = GetRoot(baseDirectory);
        if (!Directory.Exists(root))
            return [];

        return Directory.EnumerateFiles(root, "*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Take(maxCount)
            .Select(ReadOne)
            .Where(record => record != null)
            .Cast<AIAuditRecord>()
            .ToList();
    }

    public static string BuildFileName(AIAuditRecord record)
    {
        return $"{TimestampSegment(record.TimestampUtc)}-{SanitizeSegment(record.AiAuditId)}-ai-audit.json";
    }

    private static AIAuditRecord? ReadOne(string path)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<AIAuditEnvelope>(File.ReadAllText(path), JsonOptions);
            return envelope?.Audit;
        }
        catch
        {
            return null;
        }
    }

    private static AIAuditRecord Sanitize(AIAuditRecord record)
    {
        return record with
        {
            Reason = HistorySanitizer.SanitizeText(record.Reason),
            Error = HistorySanitizer.SanitizeText(record.Error)
        };
    }

    private static void EnsureNoSecrets(AIAuditRecord record)
    {
        string?[] values =
        [
            record.AiAuditId,
            record.RecommendedProfileId,
            record.UsedProfileId,
            record.Provider,
            record.Model,
            record.TaskType,
            record.RiskLevel,
            record.FallbackFrom,
            record.FallbackTo,
            record.BudgetDecision,
            record.ResultStatus,
            record.Reason,
            record.Error
        ];

        if (values.Any(HistorySanitizer.ContainsSecretLikeContent))
            throw new InvalidOperationException("ai audit contains secret-like content");
    }

    private static string GetRoot(string baseDirectory)
    {
        return Path.GetFullPath(Path.Combine(Path.GetFullPath(baseDirectory), RelativeAIAuditDirectory.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static void EnsureInsideRoot(string root, string fullPath)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(fullPath).StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ai audit artifact path escaped artifacts root");
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

    private sealed record AIAuditEnvelope(string SchemaVersion, AIAuditRecord Audit);
}
