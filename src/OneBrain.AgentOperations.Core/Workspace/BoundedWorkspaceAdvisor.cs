using OneBrain.Core.Runtime;

namespace OneBrain.AgentOperations.Core.Workspace;

public enum BoundedWorkspaceAdvisorProfile
{
    Silent,
    Balanced,
    Active,
    CriticalSenior,
    Auditor,
    TechnicalCofounder
}

public enum BoundedWorkspaceAdvisorCategory
{
    Risk,
    Improvement,
    CriticalQuestion,
    Contradiction,
    Opportunity,
    Audit,
    Architecture,
    ProductBusiness
}

public enum BoundedWorkspaceAdvisorSeverity
{
    Info,
    Low,
    Medium,
    High
}

public enum BoundedWorkspaceAdvisorActionOption
{
    Dismiss,
    ReviewEvidence,
    RequestExplanation,
    ConvertToDraftTask
}

public sealed record BoundedWorkspaceAdvisorSettings(
    BoundedWorkspaceAdvisorProfile Profile = BoundedWorkspaceAdvisorProfile.Balanced,
    int InterventionLevel = 50)
{
    public void Validate()
    {
        if (InterventionLevel is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(InterventionLevel));
    }
}

public sealed record BoundedWorkspaceAdvisorSuggestion(
    string SuggestionId,
    BoundedWorkspaceAdvisorCategory Category,
    BoundedWorkspaceAdvisorSeverity Severity,
    string Title,
    string MessageRedacted,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<BoundedWorkspaceAdvisorActionOption> ActionOptions,
    bool RequiresHumanAttention,
    bool NonExecutable,
    bool CanAuthorizeExecution,
    bool CallsModelProvider,
    bool CreatesPrompt,
    bool FilesystemMutationAllowed,
    bool NetworkUsed,
    bool ProductAuthorityGranted);

public sealed record BoundedWorkspaceAdvisorResult(
    string Decision,
    bool Accepted,
    BoundedWorkspaceAdvisorProfile Profile,
    int InterventionLevel,
    IReadOnlyList<BoundedWorkspaceAdvisorSuggestion> Suggestions,
    IReadOnlyList<string> Blockers,
    bool NonExecutor,
    bool ReadOnly,
    bool CallsModelProvider,
    bool CreatesPrompt,
    bool FilesystemMutationAllowed,
    bool NetworkUsed,
    bool ProductAuthorityGranted);

public sealed class BoundedWorkspaceAdvisorService
{
    private static readonly HashSet<string> CodeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".ts", ".tsx", ".js", ".jsx", ".mjs", ".cjs", ".rs", ".py"
    };

    public BoundedWorkspaceAdvisorResult Evaluate(
        BoundedWorkspaceScanResult scan,
        BoundedWorkspacePlanningProjectionResult planning,
        BoundedWorkspaceAdvisorSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(scan);
        ArgumentNullException.ThrowIfNull(planning);
        settings ??= new BoundedWorkspaceAdvisorSettings();
        settings.Validate();

        var blockers = Validate(scan, planning).ToArray();
        if (blockers.Length > 0)
            return Blocked(settings, blockers);

        var context = planning.Context!;
        var evidenceRefs = planning.TaskGraph!.EvidenceRefs
            .Select(value => SafeRuntimeText.Sanitize(value.EvidenceId, 180))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .Take(20)
            .ToArray();
        if (evidenceRefs.Length == 0)
            evidenceRefs = [$"evidence:bounded-workspace:{scan.EvidenceDigest}"];

        var candidates = new List<Candidate>();
        var redactedFileCount = scan.Files.Count(value => value.SecretLikeContentRedacted);
        if (redactedFileCount > 0)
        {
            candidates.Add(new Candidate(
                "secret-redaction",
                BoundedWorkspaceAdvisorCategory.Audit,
                BoundedWorkspaceAdvisorSeverity.High,
                "Secret-like values were redacted",
                $"{redactedFileCount} sampled file(s) contained secret-like values. Keep raw samples local and do not widen provider or export scope without a new explicit decision."));
        }

        if (scan.Truncated)
        {
            candidates.Add(new Candidate(
                "bounded-context",
                BoundedWorkspaceAdvisorCategory.Risk,
                BoundedWorkspaceAdvisorSeverity.Medium,
                "The reviewed context is intentionally incomplete",
                "The scan reached an explicit file, byte, depth, or sample budget. Treat the current plan as bounded evidence; a larger context requires a new operator-authorized mission."));
        }

        if (scan.Findings.Count > 0)
        {
            candidates.Add(new Candidate(
                "scan-findings",
                BoundedWorkspaceAdvisorCategory.Audit,
                BoundedWorkspaceAdvisorSeverity.Medium,
                "Review bounded scan findings",
                $"The scanner recorded {scan.Findings.Count} bounded finding(s). Review them before promoting the draft plan or increasing the scan budget."));
        }

        var safeFileRefs = context.SafeFileRefs;
        var hasCode = safeFileRefs.Any(value => CodeExtensions.Contains(Path.GetExtension(value)));
        var hasTests = safeFileRefs.Any(IsTestPath);
        if (hasCode && !hasTests)
        {
            candidates.Add(new Candidate(
                "tests-not-visible",
                BoundedWorkspaceAdvisorCategory.Improvement,
                BoundedWorkspaceAdvisorSeverity.Medium,
                "Test coverage is not visible in the bounded evidence",
                "Source files were observed, but no test or spec path was present in the reviewed evidence. Confirm the test strategy before approving future mutations."));
        }

        if (hasCode && !safeFileRefs.Any(IsManifestPath))
        {
            candidates.Add(new Candidate(
                "manifest-not-visible",
                BoundedWorkspaceAdvisorCategory.CriticalQuestion,
                BoundedWorkspaceAdvisorSeverity.Medium,
                "How should this project be built and verified?",
                "The bounded evidence contains source code but no recognized project manifest or solution file. Identify the canonical build and verification entry point before execution work begins."));
        }

        var ecosystems = DetectEcosystems(safeFileRefs);
        if (ecosystems.Count > 1)
        {
            candidates.Add(new Candidate(
                "multi-ecosystem",
                BoundedWorkspaceAdvisorCategory.Architecture,
                BoundedWorkspaceAdvisorSeverity.Low,
                "Multiple implementation ecosystems are present",
                $"The bounded evidence includes {ecosystems.Count} ecosystems. Review ownership and verification boundaries so one runtime does not silently become authority over another."));
        }

        if (scan.FilesSkipped > scan.FilesRead && scan.FilesSkipped > 0)
        {
            candidates.Add(new Candidate(
                "exclusions-dominate",
                BoundedWorkspaceAdvisorCategory.Audit,
                BoundedWorkspaceAdvisorSeverity.Low,
                "Excluded or unreadable entries exceed reviewed files",
                $"{scan.FilesSkipped} entries were skipped while {scan.FilesRead} files were reviewed. Confirm that exclusions are expected before treating the summary as broad project coverage."));
        }

        var threshold = ResolveThreshold(settings);
        var suggestions = candidates
            .Select(candidate => (Candidate: candidate, Score: Score(candidate, settings.Profile)))
            .Where(value => value.Score >= threshold)
            .OrderByDescending(value => value.Score)
            .ThenBy(value => value.Candidate.Category)
            .ThenBy(value => value.Candidate.Key, StringComparer.Ordinal)
            .Take(8)
            .Select(value => ToSuggestion(value.Candidate, scan.EvidenceDigest, evidenceRefs))
            .ToArray();

        return new BoundedWorkspaceAdvisorResult(
            Decision: suggestions.Length == 0
                ? "GO_EXPERT_ADVISOR_NO_MATERIAL_FINDINGS"
                : "GO_EXPERT_ADVISOR_SUGGESTIONS_READY",
            Accepted: true,
            Profile: settings.Profile,
            InterventionLevel: settings.InterventionLevel,
            Suggestions: suggestions,
            Blockers: Array.Empty<string>(),
            NonExecutor: true,
            ReadOnly: true,
            CallsModelProvider: false,
            CreatesPrompt: false,
            FilesystemMutationAllowed: false,
            NetworkUsed: false,
            ProductAuthorityGranted: false);
    }

    private static IEnumerable<string> Validate(
        BoundedWorkspaceScanResult scan,
        BoundedWorkspacePlanningProjectionResult planning)
    {
        if (scan.Decision != BoundedWorkspaceScanDecision.Accepted)
            yield return "Workspace scan was not accepted.";
        if (!scan.SecretsExcluded)
            yield return "Workspace evidence is not marked as secret-excluded.";
        if (scan.FilesystemMutationAllowed || scan.NetworkUsed || scan.ProductAuthorityGranted)
            yield return "Workspace evidence crossed a forbidden boundary.";
        if (!planning.Accepted || planning.Context is null || planning.TaskGraph is null || planning.MissionPlan is null)
            yield return "Reviewed planning context is not available.";
        if (planning.CanExecute || planning.CallsModelProvider || planning.CreatesPrompt ||
            planning.FilesystemMutationAllowed || planning.NetworkUsed || planning.ProductAuthorityGranted)
        {
            yield return "Advisor input must remain non-executable and non-authoritative.";
        }
    }

    private static int ResolveThreshold(BoundedWorkspaceAdvisorSettings settings)
    {
        var profileThreshold = settings.Profile switch
        {
            BoundedWorkspaceAdvisorProfile.Silent => 85,
            BoundedWorkspaceAdvisorProfile.Balanced => 50,
            BoundedWorkspaceAdvisorProfile.Active => 25,
            BoundedWorkspaceAdvisorProfile.CriticalSenior => 20,
            BoundedWorkspaceAdvisorProfile.Auditor => 20,
            BoundedWorkspaceAdvisorProfile.TechnicalCofounder => 10,
            _ => 50
        };
        return Math.Max(profileThreshold, 100 - settings.InterventionLevel);
    }

    private static int Score(Candidate candidate, BoundedWorkspaceAdvisorProfile profile)
    {
        var score = candidate.Severity switch
        {
            BoundedWorkspaceAdvisorSeverity.Info => 10,
            BoundedWorkspaceAdvisorSeverity.Low => 30,
            BoundedWorkspaceAdvisorSeverity.Medium => 60,
            BoundedWorkspaceAdvisorSeverity.High => 90,
            _ => 0
        };
        if (profile == BoundedWorkspaceAdvisorProfile.Auditor &&
            candidate.Category is BoundedWorkspaceAdvisorCategory.Audit or BoundedWorkspaceAdvisorCategory.Risk)
        {
            score += 15;
        }
        if (profile == BoundedWorkspaceAdvisorProfile.CriticalSenior &&
            candidate.Category is BoundedWorkspaceAdvisorCategory.Risk or
                BoundedWorkspaceAdvisorCategory.CriticalQuestion or
                BoundedWorkspaceAdvisorCategory.Contradiction)
        {
            score += 10;
        }
        if (profile == BoundedWorkspaceAdvisorProfile.TechnicalCofounder &&
            candidate.Category is BoundedWorkspaceAdvisorCategory.Architecture or
                BoundedWorkspaceAdvisorCategory.ProductBusiness or
                BoundedWorkspaceAdvisorCategory.CriticalQuestion)
        {
            score += 10;
        }
        return Math.Min(score, 100);
    }

    private static BoundedWorkspaceAdvisorSuggestion ToSuggestion(
        Candidate candidate,
        string evidenceDigest,
        IReadOnlyList<string> evidenceRefs)
    {
        var safeTitle = SafeRuntimeText.Sanitize(candidate.Title, 160);
        var safeMessage = SafeRuntimeText.Sanitize(candidate.Message, 600);
        return new BoundedWorkspaceAdvisorSuggestion(
            SuggestionId: $"advisor-{evidenceDigest[..12]}-{candidate.Key}",
            Category: candidate.Category,
            Severity: candidate.Severity,
            Title: safeTitle,
            MessageRedacted: safeMessage,
            EvidenceRefs: evidenceRefs,
            ActionOptions:
            [
                BoundedWorkspaceAdvisorActionOption.Dismiss,
                BoundedWorkspaceAdvisorActionOption.ReviewEvidence,
                BoundedWorkspaceAdvisorActionOption.RequestExplanation,
                BoundedWorkspaceAdvisorActionOption.ConvertToDraftTask
            ],
            RequiresHumanAttention: candidate.Severity is BoundedWorkspaceAdvisorSeverity.Medium or BoundedWorkspaceAdvisorSeverity.High ||
                                    candidate.Category == BoundedWorkspaceAdvisorCategory.CriticalQuestion,
            NonExecutable: true,
            CanAuthorizeExecution: false,
            CallsModelProvider: false,
            CreatesPrompt: false,
            FilesystemMutationAllowed: false,
            NetworkUsed: false,
            ProductAuthorityGranted: false);
    }

    private static IReadOnlySet<string> DetectEcosystems(IReadOnlyList<string> paths)
    {
        var ecosystems = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in paths)
        {
            var fileName = Path.GetFileName(path);
            var extension = Path.GetExtension(path);
            if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".sln", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase))
            {
                ecosystems.Add("dotnet");
            }
            if (extension is ".ts" or ".tsx" or ".js" or ".jsx" or ".mjs" or ".cjs" ||
                fileName.Equals("package.json", StringComparison.OrdinalIgnoreCase))
            {
                ecosystems.Add("javascript");
            }
            if (extension.Equals(".rs", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("Cargo.toml", StringComparison.OrdinalIgnoreCase))
            {
                ecosystems.Add("rust");
            }
            if (extension.Equals(".py", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("pyproject.toml", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("requirements.txt", StringComparison.OrdinalIgnoreCase))
            {
                ecosystems.Add("python");
            }
        }
        return ecosystems;
    }

    private static bool IsManifestPath(string path)
    {
        var fileName = Path.GetFileName(path);
        var extension = Path.GetExtension(path);
        return extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".sln", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("package.json", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("Cargo.toml", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("pyproject.toml", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("requirements.txt", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("go.mod", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("pom.xml", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTestPath(string path)
    {
        var normalized = path.Replace('\\', '/').ToLowerInvariant();
        var fileName = Path.GetFileName(normalized);
        if (fileName.Contains(".test.", StringComparison.Ordinal) ||
            fileName.Contains(".tests.", StringComparison.Ordinal) ||
            fileName.Contains(".spec.", StringComparison.Ordinal) ||
            fileName.EndsWith("tests.cs", StringComparison.Ordinal) ||
            fileName.EndsWith("test.cs", StringComparison.Ordinal))
        {
            return true;
        }

        return normalized.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => segment is "test" or "tests" or "spec" or "specs");
    }

    private static BoundedWorkspaceAdvisorResult Blocked(
        BoundedWorkspaceAdvisorSettings settings,
        IReadOnlyList<string> blockers) => new(
            Decision: "BLOCKED_EXPERT_ADVISOR_INPUT_FAIL_CLOSED",
            Accepted: false,
            Profile: settings.Profile,
            InterventionLevel: settings.InterventionLevel,
            Suggestions: Array.Empty<BoundedWorkspaceAdvisorSuggestion>(),
            Blockers: blockers
                .Select(value => SafeRuntimeText.Sanitize(value, 240))
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            NonExecutor: true,
            ReadOnly: true,
            CallsModelProvider: false,
            CreatesPrompt: false,
            FilesystemMutationAllowed: false,
            NetworkUsed: false,
            ProductAuthorityGranted: false);

    private sealed record Candidate(
        string Key,
        BoundedWorkspaceAdvisorCategory Category,
        BoundedWorkspaceAdvisorSeverity Severity,
        string Title,
        string Message);
}
