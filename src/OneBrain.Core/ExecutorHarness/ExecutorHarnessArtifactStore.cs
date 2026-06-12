using System.Text.Json;

namespace OneBrain.Core.ExecutorHarness;

public static class ExecutorHarnessArtifactStore
{
    public const string SchemaVersion = "onebrain-executor-harness-evidence/v1";
    public const string RelativeDirectory = "artifacts/executor-harness";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static ExecutorHarnessArtifactWriteResult Write(string baseDirectory, ExecutorHarnessEvidenceRecord evidence)
    {
        try
        {
            var root = GetRoot(baseDirectory);
            Directory.CreateDirectory(root);
            var fullPath = Path.GetFullPath(Path.Combine(root, BuildFileName(evidence)));
            EnsureInsideRoot(root, fullPath);
            fullPath = BuildUniquePath(fullPath);
            File.WriteAllText(fullPath, JsonSerializer.Serialize(new ExecutorHarnessEvidenceEnvelope(SchemaVersion, evidence), JsonOptions));

            return new ExecutorHarnessArtifactWriteResult
            {
                Success = true,
                Path = fullPath,
                RelativePath = ToRelativePath(baseDirectory, fullPath)
            };
        }
        catch (Exception ex)
        {
            return new ExecutorHarnessArtifactWriteResult { Success = false, Error = ex.Message };
        }
    }

    public static ExecutorHarnessEvidenceReplay ReadLatest(string baseDirectory)
    {
        try
        {
            var root = GetRoot(baseDirectory);
            if (!Directory.Exists(root))
            {
                return new ExecutorHarnessEvidenceReplay(
                    Success: true,
                    Status: "empty",
                    Message: "No executor harness evidence exists yet.",
                    RelativePath: "",
                    Evidence: null,
                    Notes: ["runtime artifacts are local only", "no action was executed by replay"]);
            }

            var latest = Directory.GetFiles(root, "*-executor-harness.json")
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .FirstOrDefault();

            if (latest == null)
            {
                return new ExecutorHarnessEvidenceReplay(
                    Success: true,
                    Status: "empty",
                    Message: "No executor harness evidence exists yet.",
                    RelativePath: "",
                    Evidence: null,
                    Notes: ["runtime artifacts are local only", "no action was executed by replay"]);
            }

            EnsureInsideRoot(root, latest.FullName);
            var envelope = JsonSerializer.Deserialize<ExecutorHarnessEvidenceEnvelope>(
                File.ReadAllText(latest.FullName),
                JsonOptions);

            if (envelope?.Evidence == null)
            {
                return new ExecutorHarnessEvidenceReplay(
                    Success: false,
                    Status: ExecutorHarnessStatuses.Failed,
                    Message: "Executor harness evidence could not be parsed.",
                    RelativePath: ToRelativePath(baseDirectory, latest.FullName),
                    Evidence: null,
                    Notes: ["replay is read-only"]);
            }

            return new ExecutorHarnessEvidenceReplay(
                Success: true,
                Status: envelope.Evidence.Status,
                Message: "Executor harness evidence replay loaded.",
                RelativePath: ToRelativePath(baseDirectory, latest.FullName),
                Evidence: envelope.Evidence,
                Notes: ["replay is read-only", "no click is executed by replay", "artifacts remain local runtime data"],
                TraceLink: BuildTraceLink(envelope.Evidence, ToRelativePath(baseDirectory, latest.FullName)));
        }
        catch (Exception ex)
        {
            return new ExecutorHarnessEvidenceReplay(
                Success: false,
                Status: ExecutorHarnessStatuses.Failed,
                Message: ex.Message,
                RelativePath: "",
                Evidence: null,
                Notes: ["replay failed closed"]);
        }
    }

    public static ExecutorHarnessEvidenceIndex ReadIndex(string baseDirectory)
    {
        try
        {
            var root = GetRoot(baseDirectory);
            if (!Directory.Exists(root))
            {
                return new ExecutorHarnessEvidenceIndex(
                    Success: true,
                    Status: "empty",
                    Message: "No executor harness evidence exists yet.",
                    Items: [],
                    Notes: ["evidence index is read-only", "runtime artifacts are local only"]);
            }

            var items = new List<ExecutorHarnessEvidenceIndexItem>();
            foreach (var file in Directory.GetFiles(root, "*-executor-harness.json")
                         .Select(path => new FileInfo(path))
                         .OrderByDescending(file => file.LastWriteTimeUtc))
            {
                EnsureInsideRoot(root, file.FullName);
                var relativePath = ToRelativePath(baseDirectory, file.FullName);
                var envelope = JsonSerializer.Deserialize<ExecutorHarnessEvidenceEnvelope>(
                    File.ReadAllText(file.FullName),
                    JsonOptions);

                if (envelope?.Evidence == null)
                    continue;

                items.Add(BuildIndexItem(envelope.Evidence, relativePath));
            }

            if (items.Count == 0)
            {
                return new ExecutorHarnessEvidenceIndex(
                    Success: true,
                    Status: "empty",
                    Message: "No parseable executor harness evidence exists yet.",
                    Items: [],
                    Notes: ["evidence index is read-only", "invalid artifacts are ignored"]);
            }

            return new ExecutorHarnessEvidenceIndex(
                Success: true,
                Status: "indexed",
                Message: $"Executor harness evidence index loaded: {items.Count} item(s).",
                Items: items,
                Notes: ["evidence index is read-only", "no artifact is opened automatically", "no click is executed by indexing"]);
        }
        catch (Exception ex)
        {
            return new ExecutorHarnessEvidenceIndex(
                Success: false,
                Status: ExecutorHarnessStatuses.Failed,
                Message: ex.Message,
                Items: [],
                Notes: ["evidence index failed closed"]);
        }
    }

    public static string BuildFileName(ExecutorHarnessEvidenceRecord evidence)
    {
        return $"{TimestampSegment(evidence.CreatedAtUtc)}-{SanitizeSegment(evidence.EvidenceId)}-executor-harness.json";
    }

    private static string GetRoot(string baseDirectory)
    {
        return Path.GetFullPath(Path.Combine(Path.GetFullPath(baseDirectory), RelativeDirectory.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static void EnsureInsideRoot(string root, string fullPath)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(fullPath).StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("executor harness artifact path escaped artifacts root");
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

    private static ExecutorHarnessEvidenceIndexItem BuildIndexItem(
        ExecutorHarnessEvidenceRecord evidence,
        string relativePath)
    {
        var contract = evidence.InteractionContract;
        var trace = BuildTraceLink(evidence, relativePath);
        var verification = evidence.Verification.Success
            ? ExecutorHarnessStatuses.Succeeded
            : evidence.Verification.Status;

        return new ExecutorHarnessEvidenceIndexItem(
            EvidenceId: evidence.EvidenceId,
            TimestampUtc: evidence.CreatedAtUtc,
            HarnessId: evidence.HarnessId,
            ActionKind: contract?.ActionKind ?? "unknown",
            SafetyDecision: contract?.SafetyMatrix.Status ?? evidence.Status,
            VerificationResult: verification,
            LogicalPath: relativePath,
            ReplayPath: "/executor-harness/replay",
            TraceLink: trace);
    }

    private static ExecutorHarnessRunTraceLink BuildTraceLink(
        ExecutorHarnessEvidenceRecord evidence,
        string relativePath)
    {
        var contract = evidence.InteractionContract;
        var safetyDecision = contract?.SafetyMatrix.Status ?? evidence.Status;
        var approvalDecision = contract?.ApprovalState.Approved == true
            ? "approved"
            : string.IsNullOrWhiteSpace(evidence.ApprovalDecisionId) ? "missing_or_not_recorded" : "not_approved";
        var postState = evidence.Verification.Success
            ? "post-state verified"
            : evidence.Verification.Message;
        var blockedReason = evidence.Status == ExecutorHarnessStatuses.Blocked || evidence.Status == ExecutorHarnessStatuses.Failed
            ? evidence.Verification.Message
            : "";

        return new ExecutorHarnessRunTraceLink(
            TraceId: $"trace-{evidence.EvidenceId}",
            IsSynthetic: true,
            RunId: $"local-trace-{evidence.HarnessId}",
            ApprovalRequestId: evidence.ApprovalRequestId,
            ApprovalDecisionId: evidence.ApprovalDecisionId,
            ApprovalDecision: approvalDecision,
            SafetyDecision: safetyDecision,
            EvidencePath: relativePath,
            ReplayPath: "/executor-harness/replay",
            PostStateResult: postState,
            BlockedReason: blockedReason,
            Notes:
            [
                "synthetic local trace id because executor harness evidence does not yet embed full run history id",
                "trace links evidence, approval, safety decision and post-state without inventing external data"
            ]);
    }

    private sealed record ExecutorHarnessEvidenceEnvelope(string SchemaVersion, ExecutorHarnessEvidenceRecord Evidence);
}
