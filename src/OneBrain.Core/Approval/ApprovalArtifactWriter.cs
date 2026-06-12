using System.Text.Json;

namespace OneBrain.Core.Approval;

public static class ApprovalArtifactWriter
{
    public const string RequestSchemaVersion = "onebrain-approval-request/v1";
    public const string DecisionSchemaVersion = "onebrain-approval-decision/v1";
    public const string RelativeApprovalDirectory = "artifacts/approvals";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static ApprovalArtifactWriteResult WriteRequest(string baseDirectory, ApprovalRequest request)
    {
        return Write(baseDirectory, BuildRequestFileName(request), new
        {
            schemaVersion = RequestSchemaVersion,
            request
        });
    }

    public static ApprovalArtifactWriteResult WriteDecision(string baseDirectory, ApprovalDecision decision)
    {
        return Write(baseDirectory, BuildDecisionFileName(decision), new
        {
            schemaVersion = DecisionSchemaVersion,
            decision
        });
    }

    public static string BuildRequestFileName(ApprovalRequest request)
    {
        return $"{TimestampSegment(request.CreatedAtUtc)}-{SanitizeSegment(request.ApprovalRequestId)}-approval-request.json";
    }

    public static string BuildDecisionFileName(ApprovalDecision decision)
    {
        return $"{TimestampSegment(decision.DecidedAtUtc)}-{SanitizeSegment(decision.ApprovalRequestId)}-approval-decision.json";
    }

    private static ApprovalArtifactWriteResult Write(string baseDirectory, string fileName, object payload)
    {
        try
        {
            var root = GetRoot(baseDirectory);
            Directory.CreateDirectory(root);

            var fullPath = Path.GetFullPath(Path.Combine(root, fileName));
            EnsureInsideRoot(root, fullPath);
            fullPath = BuildUniquePath(fullPath);

            File.WriteAllText(fullPath, JsonSerializer.Serialize(payload, JsonOptions));

            return new ApprovalArtifactWriteResult
            {
                Success = true,
                Path = fullPath,
                RelativePath = ToRelativePath(baseDirectory, fullPath)
            };
        }
        catch (Exception ex)
        {
            return new ApprovalArtifactWriteResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private static string GetRoot(string baseDirectory)
    {
        return Path.GetFullPath(Path.Combine(Path.GetFullPath(baseDirectory), RelativeApprovalDirectory.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static void EnsureInsideRoot(string root, string fullPath)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(fullPath).StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("approval artifact path escaped artifacts root");
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
