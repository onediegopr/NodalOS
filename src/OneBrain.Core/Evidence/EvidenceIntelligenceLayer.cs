using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OneBrain.Core.Evidence;

public enum EvidenceSourceType
{
    Ocr,
    Uia,
    Win32,
    Cdp,
    Dom,
    ScreenshotFixture,
    Timeline,
    Policy,
    Approval,
    AgentObservation,
    ExecutionProposal,
    ValidationReport,
    PreviousEvidenceRecord
}

public enum EvidenceSensitivityLevel
{
    Public,
    Internal,
    Sensitive,
    Secret
}

public enum EvidenceRedactionStatus
{
    None,
    Applied,
    Required,
    RejectedSecret
}

public enum EvidenceStalenessStatus
{
    Fresh,
    Stale,
    Expired,
    Unknown
}

public enum EvidencePolicyScope
{
    ReadOnly,
    FixtureSafe,
    RequiresApproval,
    BlocksAction,
    UnsafeLive
}

public enum EvidenceQueryPurpose
{
    HumanSearch,
    AgentContext,
    Audit,
    SafetyGate,
    ReadinessMatrix
}

public enum EvidenceRelationType
{
    supports_observation,
    contradicts_observation,
    supports_claim,
    contradicts_claim,
    supports_action,
    contradicts_action,
    requires_approval,
    blocks_execution,
    policy_allows,
    policy_blocks,
    stale_source,
    missing_source,
    low_confidence_source,
    redacted_source,
    derived_from,
    verified_by,
    overridden_by_user
}

public enum ClaimEvidenceVerdict
{
    SUPPORTED,
    CONTRADICTED,
    INSUFFICIENT_EVIDENCE,
    STALE_EVIDENCE,
    LOW_CONFIDENCE,
    REQUIRES_HUMAN_REVIEW
}

public enum ActionEvidenceVerdict
{
    ALLOW_READ_ONLY,
    ALLOW_FIXTURE_SAFE,
    REQUIRES_APPROVAL,
    BLOCKED_BY_POLICY,
    BLOCKED_BY_CONTRADICTION,
    BLOCKED_BY_MISSING_EVIDENCE,
    BLOCKED_BY_STALE_EVIDENCE,
    BLOCKED_UNSAFE_LIVE_ACTION
}

public enum EvidenceActionType
{
    Read,
    Click,
    Type,
    Submit,
    Navigate,
    Download,
    Upload,
    Filesystem,
    Unknown
}

public enum EvidenceSemanticBackendStatus
{
    Disabled,
    NotConfigured
}

public enum EvidenceReadinessRowStatus
{
    Supports,
    Contradicts,
    Missing,
    Stale,
    LowConfidence,
    PolicyAllows,
    PolicyBlocks,
    RequiresApproval,
    Redacted,
    Info
}

public enum EvidenceReadinessRisk
{
    Low,
    Medium,
    High,
    Blocking
}

public sealed record EvidenceItem(
    string Id,
    EvidenceSourceType SourceType,
    string SourceRef,
    DateTimeOffset CapturedAt,
    string WorkspaceId,
    string SessionId,
    string Text,
    string NormalizedText,
    string Hash,
    double Confidence,
    EvidenceSensitivityLevel SensitivityLevel,
    EvidenceRedactionStatus RedactionStatus,
    EvidenceStalenessStatus StalenessStatus,
    EvidencePolicyScope PolicyScope,
    IReadOnlyDictionary<string, string> Metadata)
{
    public const string DefaultWorkspaceId = "workspace.fixture";
    public const string DefaultSessionId = "session.fixture";

    public static EvidenceItem Create(
        string id,
        EvidenceSourceType sourceType,
        string sourceRef,
        string text,
        DateTimeOffset? capturedAt = null,
        string workspaceId = DefaultWorkspaceId,
        string sessionId = DefaultSessionId,
        double confidence = 1,
        EvidenceSensitivityLevel sensitivityLevel = EvidenceSensitivityLevel.Internal,
        EvidenceRedactionStatus redactionStatus = EvidenceRedactionStatus.None,
        EvidenceStalenessStatus stalenessStatus = EvidenceStalenessStatus.Fresh,
        EvidencePolicyScope policyScope = EvidencePolicyScope.ReadOnly,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        var normalizedText = EvidenceText.Normalize(text);
        var safeConfidence = EvidenceText.ClampConfidence(confidence);
        var safeMetadata = EvidenceText.NormalizeMetadata(metadata);
        var safeText = EvidenceText.RedactIfNeeded(text, sensitivityLevel, redactionStatus);

        if (!string.Equals(safeText, text, StringComparison.Ordinal))
        {
            normalizedText = EvidenceText.Normalize(safeText);
            redactionStatus = EvidenceRedactionStatus.Applied;
        }

        return new EvidenceItem(
            id,
            sourceType,
            sourceRef,
            capturedAt ?? DateTimeOffset.UnixEpoch,
            workspaceId,
            sessionId,
            safeText,
            normalizedText,
            EvidenceText.StableHash($"{sourceType}|{sourceRef}|{normalizedText}|{workspaceId}|{sessionId}"),
            safeConfidence,
            sensitivityLevel,
            redactionStatus,
            stalenessStatus,
            policyScope,
            safeMetadata);
    }
}

public sealed record EvidenceQuery(
    string Query,
    EvidenceQueryPurpose Purpose,
    IReadOnlyList<EvidenceSourceType> SourceTypes,
    string? WorkspaceId,
    string? SessionId,
    int MaxResults,
    bool IncludeContradictions,
    bool IncludeStale);

public sealed record EvidenceSearchResult(
    string EvidenceId,
    EvidenceSourceType SourceType,
    string Snippet,
    double Score,
    int Rank,
    string MatchReason,
    EvidenceStalenessStatus StalenessStatus,
    double Confidence,
    string RelationHint);

public sealed record ClaimEvidenceScanRequest(
    string Claim,
    string WorkspaceId,
    string SessionId,
    IReadOnlyList<EvidenceSourceType> RequiredSourceTypes,
    bool StrictMode = true,
    bool IncludeStale = false,
    int MaxEvidence = 10,
    string Context = "");

public sealed record ClaimEvidenceScanResult(
    string Claim,
    ClaimEvidenceVerdict Verdict,
    double Confidence,
    string Rationale,
    IReadOnlyList<EvidenceSearchResult> SupportingEvidence,
    IReadOnlyList<EvidenceSearchResult> ContradictingEvidence,
    IReadOnlyList<EvidenceSearchResult> NuancedEvidence,
    IReadOnlyList<EvidenceSourceType> MissingRequiredSourceTypes,
    IReadOnlyList<string> BlockingReasons,
    EvidenceGraph Graph,
    ActionReadinessMatrix ReadinessMatrix,
    string JsonReport,
    string MarkdownReport);

public sealed record ActionEvidenceScanRequest(
    string ActionId,
    EvidenceActionType ActionType,
    string Target,
    string WorkspaceId,
    string SessionId,
    IReadOnlyList<EvidenceSourceType> RequiredSourceTypes,
    bool IsLiveAction = false,
    bool IsFixtureSafe = false,
    bool IsSensitive = false,
    bool RequiresApproval = false,
    int MaxEvidence = 10);

public sealed record ActionEvidenceScanResult(
    string ActionId,
    ActionEvidenceVerdict Verdict,
    double Confidence,
    string Rationale,
    IReadOnlyList<EvidenceSearchResult> SupportingEvidence,
    IReadOnlyList<EvidenceSearchResult> ContradictingEvidence,
    IReadOnlyList<EvidenceSourceType> MissingRequiredSourceTypes,
    IReadOnlyList<string> BlockingReasons,
    IReadOnlyList<string> RequiredHumanActions,
    EvidenceGraph Graph,
    ActionReadinessMatrix ReadinessMatrix,
    string JsonReport,
    string MarkdownReport);

public sealed record EvidenceEdge(
    string FromId,
    string ToId,
    EvidenceRelationType RelationType,
    double Confidence,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    string Rationale,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record EvidenceGraph(IReadOnlyList<EvidenceEdge> Edges);

public sealed record ActionReadinessMatrixRow(
    string Signal,
    string Source,
    EvidenceReadinessRowStatus Status,
    double Confidence,
    EvidenceReadinessRisk Risk,
    string Decision,
    string EvidenceId,
    string Rationale);

public sealed record ActionReadinessMatrix(
    IReadOnlyList<ActionReadinessMatrixRow> Rows,
    string FinalVerdict,
    double Confidence,
    IReadOnlyList<string> BlockingReasons,
    IReadOnlyList<string> RequiredHumanActions,
    string SafeNextStep);

public interface IEvidenceIntelligenceIndex
{
    EvidenceItem Ingest(EvidenceItem item);

    EvidenceItem? GetById(string id);

    IReadOnlyList<EvidenceItem> All();

    IReadOnlyList<EvidenceItem> QueryItems(EvidenceQuery query);
}

public sealed class InMemoryEvidenceIntelligenceIndex : IEvidenceIntelligenceIndex
{
    private readonly Dictionary<string, EvidenceItem> _itemsById = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _idByHash = new(StringComparer.Ordinal);

    public EvidenceItem Ingest(EvidenceItem item)
    {
        if (_idByHash.TryGetValue(item.Hash, out var existingId) && _itemsById.TryGetValue(existingId, out var existing))
        {
            return existing;
        }

        _itemsById[item.Id] = item;
        _idByHash[item.Hash] = item.Id;
        return item;
    }

    public EvidenceItem? GetById(string id) => _itemsById.GetValueOrDefault(id);

    public IReadOnlyList<EvidenceItem> All() => _itemsById.Values.OrderBy(item => item.Id, StringComparer.Ordinal).ToList();

    public IReadOnlyList<EvidenceItem> QueryItems(EvidenceQuery query)
    {
        var sourceFilter = query.SourceTypes.Count == 0 ? null : query.SourceTypes.ToHashSet();
        return All()
            .Where(item => sourceFilter == null || sourceFilter.Contains(item.SourceType))
            .Where(item => query.WorkspaceId is null || string.Equals(item.WorkspaceId, query.WorkspaceId, StringComparison.Ordinal))
            .Where(item => query.SessionId is null || string.Equals(item.SessionId, query.SessionId, StringComparison.Ordinal))
            .Where(item => query.IncludeStale || item.StalenessStatus == EvidenceStalenessStatus.Fresh)
            .ToList();
    }
}

public sealed class EvidenceIntelligenceRetrievalRouter
{
    private readonly IEvidenceIntelligenceIndex _index;

    public EvidenceIntelligenceRetrievalRouter(IEvidenceIntelligenceIndex index)
    {
        _index = index;
    }

    public EvidenceSemanticBackendStatus SemanticBackendStatus => EvidenceSemanticBackendStatus.Disabled;

    public IReadOnlyList<EvidenceSearchResult> Search(EvidenceQuery query)
    {
        var tokens = EvidenceText.Tokenize(query.Query);
        if (tokens.Count == 0)
        {
            return [];
        }

        var ranked = _index.QueryItems(query)
            .Select(item => (Item: item, Score: Score(item, tokens, query), Reason: MatchReason(item, tokens, query)))
            .Where(match => match.Score > 0)
            .OrderByDescending(match => IsContradiction(match.Item) && query.IncludeContradictions)
            .ThenByDescending(match => match.Score)
            .ThenBy(match => match.Item.Id, StringComparer.Ordinal)
            .Take(Math.Max(0, query.MaxResults))
            .Select((match, index) => new EvidenceSearchResult(
                match.Item.Id,
                match.Item.SourceType,
                Snippet(match.Item.Text, query.Query),
                Math.Round(match.Score, 4),
                index + 1,
                match.Reason,
                match.Item.StalenessStatus,
                match.Item.Confidence,
                RelationHint(match.Item)))
            .ToList();

        return ranked;
    }

    private static double Score(EvidenceItem item, IReadOnlyList<string> tokens, EvidenceQuery query)
    {
        var evidenceTokens = EvidenceText.Tokenize(item.NormalizedText);
        var metadataTokens = EvidenceText.Tokenize(string.Join(" ", item.Metadata.Select(kv => $"{kv.Key} {kv.Value}")));
        var evidenceSet = evidenceTokens.Concat(metadataTokens).ToHashSet(StringComparer.Ordinal);
        var matched = tokens.Count(token => evidenceSet.Contains(token));

        if (matched == 0)
        {
            return IsContradiction(item) && query.IncludeContradictions ? 0.05 : 0;
        }

        var lexical = matched / (double)tokens.Count;
        var sourceWeight = item.SourceType switch
        {
            EvidenceSourceType.Policy => 1.25,
            EvidenceSourceType.ValidationReport => 1.2,
            EvidenceSourceType.Uia => 1.15,
            EvidenceSourceType.Dom => 1.1,
            EvidenceSourceType.Ocr => 0.9,
            _ => 1
        };
        var staleWeight = item.StalenessStatus == EvidenceStalenessStatus.Fresh ? 1 : 0.45;
        var contradictionWeight = IsContradiction(item) && query.IncludeContradictions ? 1.35 : 1;
        return lexical * sourceWeight * item.Confidence * staleWeight * contradictionWeight;
    }

    private static string MatchReason(EvidenceItem item, IReadOnlyList<string> tokens, EvidenceQuery query)
    {
        var matched = tokens.Where(token => item.NormalizedText.Contains(token, StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToArray();
        var relation = RelationHint(item);
        return $"{relation}; lexical tokens: {string.Join(",", matched)}; semantic backend: disabled";
    }

    private static string Snippet(string text, string query)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var trimmed = text.Trim();
        return trimmed.Length <= 160 ? trimmed : trimmed[..157] + "...";
    }

    internal static string RelationHint(EvidenceItem item)
    {
        if (item.Metadata.TryGetValue("relation", out var relation))
        {
            return relation;
        }

        if (item.PolicyScope == EvidencePolicyScope.BlocksAction)
        {
            return "policy_blocks";
        }

        if (item.PolicyScope == EvidencePolicyScope.RequiresApproval)
        {
            return "requires_approval";
        }

        return IsContradiction(item) ? "contradicts" : "supports";
    }

    internal static bool IsContradiction(EvidenceItem item)
    {
        return item.Metadata.TryGetValue("relation", out var relation)
            && relation.Contains("contradict", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class ClaimEvidenceScanner
{
    private readonly IEvidenceIntelligenceIndex _index;
    private readonly EvidenceIntelligenceRetrievalRouter _retrieval;

    public ClaimEvidenceScanner(IEvidenceIntelligenceIndex index)
    {
        _index = index;
        _retrieval = new EvidenceIntelligenceRetrievalRouter(index);
    }

    public ClaimEvidenceScanResult Scan(ClaimEvidenceScanRequest request)
    {
        var query = new EvidenceQuery(
            request.Claim,
            EvidenceQueryPurpose.SafetyGate,
            [],
            request.WorkspaceId,
            request.SessionId,
            request.MaxEvidence,
            IncludeContradictions: true,
            IncludeStale: true);
        var allResults = _retrieval.Search(query);
        var relatedItems = allResults.Select(result => _index.GetById(result.EvidenceId)).OfType<EvidenceItem>().ToList();
        var missingRequired = MissingRequiredSources(request.RequiredSourceTypes, relatedItems);
        var stale = relatedItems.Where(item => item.StalenessStatus != EvidenceStalenessStatus.Fresh).ToList();
        var lowConfidence = relatedItems.Where(item => item.Confidence < 0.5).ToList();
        var contradictory = allResults.Where(result => IsContradicting(_index.GetById(result.EvidenceId), request.Claim)).ToList();
        var supporting = allResults.Where(result => IsSupporting(_index.GetById(result.EvidenceId), request.Claim)).ToList();
        var nuanced = allResults.Except(supporting).Except(contradictory).ToList();
        var blocking = new List<string>();

        ClaimEvidenceVerdict verdict;
        if (contradictory.Count > 0)
        {
            verdict = ClaimEvidenceVerdict.CONTRADICTED;
            blocking.Add("Contradictory evidence was found and takes precedence over supporting evidence.");
        }
        else if (missingRequired.Count > 0 || allResults.Count == 0)
        {
            verdict = ClaimEvidenceVerdict.INSUFFICIENT_EVIDENCE;
            blocking.Add("Required evidence source is missing.");
        }
        else if (supporting.Count == 0)
        {
            verdict = ClaimEvidenceVerdict.INSUFFICIENT_EVIDENCE;
            blocking.Add("No supporting evidence matched the claim.");
        }
        else if (stale.Count > 0 && stale.Count == relatedItems.Count)
        {
            verdict = ClaimEvidenceVerdict.STALE_EVIDENCE;
            blocking.Add("Only stale evidence matched the claim.");
        }
        else if (lowConfidence.Count > 0 && lowConfidence.Count == relatedItems.Count)
        {
            verdict = ClaimEvidenceVerdict.LOW_CONFIDENCE;
            blocking.Add("Only low-confidence evidence matched the claim.");
        }
        else
        {
            verdict = ClaimEvidenceVerdict.SUPPORTED;
        }

        if (request.StrictMode && verdict == ClaimEvidenceVerdict.SUPPORTED && missingRequired.Count > 0)
        {
            verdict = ClaimEvidenceVerdict.REQUIRES_HUMAN_REVIEW;
            blocking.Add("Strict mode requires human review for missing source coverage.");
        }

        var confidence = ComputeConfidence(verdict, supporting, contradictory, relatedItems);
        var graph = EvidenceGraphBuilder.ForClaim(request.Claim, supporting, contradictory, nuanced);
        var matrix = EvidenceReadinessMatrixBuilder.ForClaim(verdict, supporting, contradictory, missingRequired, blocking, confidence);
        var report = new ClaimEvidenceScanResult(
            request.Claim,
            verdict,
            confidence,
            BuildRationale(verdict, blocking),
            supporting,
            contradictory,
            nuanced,
            missingRequired,
            blocking,
            graph,
            matrix,
            "",
            "");

        return report with
        {
            JsonReport = EvidenceReportFormatter.ToStableJson(report),
            MarkdownReport = EvidenceReportFormatter.ToMarkdown(report)
        };
    }

    private static bool IsSupporting(EvidenceItem? item, string claim)
    {
        if (item is null || EvidenceIntelligenceRetrievalRouter.IsContradiction(item))
        {
            return false;
        }

        if (item.PolicyScope == EvidencePolicyScope.BlocksAction)
        {
            return false;
        }

        var claimTokens = EvidenceText.Tokenize(claim);
        var textTokens = EvidenceText.Tokenize(item.NormalizedText).ToHashSet(StringComparer.Ordinal);
        return claimTokens.Count > 0 && claimTokens.Count(token => textTokens.Contains(token)) >= Math.Max(1, claimTokens.Count / 2);
    }

    private static bool IsContradicting(EvidenceItem? item, string claim)
    {
        if (item is null)
        {
            return false;
        }

        if (EvidenceIntelligenceRetrievalRouter.IsContradiction(item))
        {
            return true;
        }

        if (item.PolicyScope == EvidencePolicyScope.BlocksAction)
        {
            return true;
        }

        var normalizedClaim = EvidenceText.Normalize(claim);
        return item.NormalizedText.Contains($"not {normalizedClaim}", StringComparison.Ordinal)
            || item.NormalizedText.Contains($"no {normalizedClaim}", StringComparison.Ordinal);
    }

    private static IReadOnlyList<EvidenceSourceType> MissingRequiredSources(IReadOnlyList<EvidenceSourceType> required, IReadOnlyList<EvidenceItem> items)
    {
        var present = items.Select(item => item.SourceType).ToHashSet();
        return required.Where(source => !present.Contains(source)).OrderBy(source => source.ToString(), StringComparer.Ordinal).ToList();
    }

    private static double ComputeConfidence(
        ClaimEvidenceVerdict verdict,
        IReadOnlyList<EvidenceSearchResult> supporting,
        IReadOnlyList<EvidenceSearchResult> contradictory,
        IReadOnlyList<EvidenceItem> relatedItems)
    {
        if (verdict == ClaimEvidenceVerdict.INSUFFICIENT_EVIDENCE)
        {
            return 0;
        }

        var basis = contradictory.Count > 0 ? contradictory : supporting;
        if (basis.Count == 0 && relatedItems.Count > 0)
        {
            return Math.Round(relatedItems.Average(item => item.Confidence), 4);
        }

        return Math.Round(basis.Count == 0 ? 0 : basis.Average(result => result.Confidence), 4);
    }

    private static string BuildRationale(ClaimEvidenceVerdict verdict, IReadOnlyList<string> blocking)
    {
        return blocking.Count == 0
            ? $"Claim scan verdict: {verdict}."
            : $"Claim scan verdict: {verdict}. {string.Join(" ", blocking)}";
    }
}

public sealed class ActionEvidenceScanner
{
    private readonly IEvidenceIntelligenceIndex _index;
    private readonly EvidenceIntelligenceRetrievalRouter _retrieval;

    public ActionEvidenceScanner(IEvidenceIntelligenceIndex index)
    {
        _index = index;
        _retrieval = new EvidenceIntelligenceRetrievalRouter(index);
    }

    public ActionEvidenceScanResult Scan(ActionEvidenceScanRequest request)
    {
        var allResults = _retrieval.Search(new EvidenceQuery(
            $"{request.ActionType} {request.Target}",
            EvidenceQueryPurpose.SafetyGate,
            [],
            request.WorkspaceId,
            request.SessionId,
            request.MaxEvidence,
            IncludeContradictions: true,
            IncludeStale: true));
        var relatedItems = allResults.Select(result => _index.GetById(result.EvidenceId)).OfType<EvidenceItem>().ToList();
        var missingRequired = MissingRequiredSources(request.RequiredSourceTypes, relatedItems);
        var supporting = allResults.Where(result => IsSupporting(_index.GetById(result.EvidenceId))).ToList();
        var contradictions = allResults.Where(result => IsContradicting(_index.GetById(result.EvidenceId))).ToList();
        var blocking = new List<string>();
        var humanActions = new List<string>();
        var staleSensitive = relatedItems.Any(item => item.StalenessStatus != EvidenceStalenessStatus.Fresh) && (request.IsSensitive || request.ActionType != EvidenceActionType.Read);
        var policyBlocked = relatedItems.Any(item => item.PolicyScope is EvidencePolicyScope.BlocksAction or EvidencePolicyScope.UnsafeLive);
        var approvalPresent = relatedItems.Any(item => item.SourceType == EvidenceSourceType.Approval && item.PolicyScope != EvidencePolicyScope.BlocksAction);
        var targetContradiction = HasTargetContradiction(request, relatedItems);

        ActionEvidenceVerdict verdict;
        if (request.IsLiveAction)
        {
            verdict = ActionEvidenceVerdict.BLOCKED_UNSAFE_LIVE_ACTION;
            blocking.Add("Live action requested. Runtime is not enabled.");
        }
        else if (policyBlocked)
        {
            verdict = ActionEvidenceVerdict.BLOCKED_BY_POLICY;
            blocking.Add("Policy evidence blocks the proposed action.");
        }
        else if (contradictions.Count > 0 || targetContradiction)
        {
            verdict = ActionEvidenceVerdict.BLOCKED_BY_CONTRADICTION;
            blocking.Add("Contradictory target/action evidence was found.");
        }
        else if (missingRequired.Count > 0 || supporting.Count == 0)
        {
            verdict = ActionEvidenceVerdict.BLOCKED_BY_MISSING_EVIDENCE;
            blocking.Add("Critical evidence is missing.");
        }
        else if (staleSensitive)
        {
            verdict = ActionEvidenceVerdict.BLOCKED_BY_STALE_EVIDENCE;
            blocking.Add("Stale evidence cannot authorize a sensitive or non-read action.");
        }
        else if ((request.RequiresApproval || request.IsSensitive || RequiresApprovalByActionType(request.ActionType)) && !approvalPresent)
        {
            verdict = ActionEvidenceVerdict.REQUIRES_APPROVAL;
            humanActions.Add("Human approval required before fixture-only continuation.");
        }
        else if (request.ActionType == EvidenceActionType.Read)
        {
            verdict = ActionEvidenceVerdict.ALLOW_READ_ONLY;
        }
        else if (request.IsFixtureSafe)
        {
            verdict = ActionEvidenceVerdict.ALLOW_FIXTURE_SAFE;
        }
        else
        {
            verdict = ActionEvidenceVerdict.REQUIRES_APPROVAL;
            humanActions.Add("Action is not marked fixture-safe.");
        }

        var confidence = ComputeConfidence(verdict, supporting, contradictions, relatedItems);
        var graph = EvidenceGraphBuilder.ForAction(request.ActionId, supporting, contradictions, relatedItems, policyBlocked);
        var matrix = EvidenceReadinessMatrixBuilder.ForAction(request, verdict, supporting, contradictions, missingRequired, blocking, humanActions, relatedItems, confidence);
        var report = new ActionEvidenceScanResult(
            request.ActionId,
            verdict,
            confidence,
            BuildRationale(verdict, blocking, humanActions),
            supporting,
            contradictions,
            missingRequired,
            blocking,
            humanActions,
            graph,
            matrix,
            "",
            "");

        return report with
        {
            JsonReport = EvidenceReportFormatter.ToStableJson(report),
            MarkdownReport = EvidenceReportFormatter.ToMarkdown(report)
        };
    }

    private static bool RequiresApprovalByActionType(EvidenceActionType actionType)
    {
        return actionType is EvidenceActionType.Submit or EvidenceActionType.Upload or EvidenceActionType.Filesystem;
    }

    private static bool IsSupporting(EvidenceItem? item)
    {
        return item is not null
            && !EvidenceIntelligenceRetrievalRouter.IsContradiction(item)
            && item.PolicyScope is EvidencePolicyScope.ReadOnly or EvidencePolicyScope.FixtureSafe or EvidencePolicyScope.RequiresApproval;
    }

    private static bool IsContradicting(EvidenceItem? item)
    {
        return item is not null
            && (EvidenceIntelligenceRetrievalRouter.IsContradiction(item) || item.PolicyScope is EvidencePolicyScope.BlocksAction or EvidencePolicyScope.UnsafeLive);
    }

    private static bool HasTargetContradiction(ActionEvidenceScanRequest request, IReadOnlyList<EvidenceItem> items)
    {
        var targetLabels = items
            .Select(item => item.Metadata.TryGetValue("target", out var target) ? EvidenceText.Normalize(target) : EvidenceText.Normalize(item.Text))
            .Where(target => !string.IsNullOrWhiteSpace(target))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var requestedTarget = EvidenceText.Normalize(request.Target);
        return targetLabels.Any(target => IsKnownDangerousConflict(requestedTarget, target));
    }

    private static bool IsKnownDangerousConflict(string requestedTarget, string observedTarget)
    {
        var destructiveWords = new[] { "delete", "eliminar", "remove", "pagar", "pay", "submit", "send" };
        return destructiveWords.Any(word => requestedTarget.Contains(word, StringComparison.Ordinal))
            && destructiveWords.Any(word => observedTarget.Contains(word, StringComparison.Ordinal))
            && !string.Equals(requestedTarget, observedTarget, StringComparison.Ordinal);
    }

    private static IReadOnlyList<EvidenceSourceType> MissingRequiredSources(IReadOnlyList<EvidenceSourceType> required, IReadOnlyList<EvidenceItem> items)
    {
        var present = items.Select(item => item.SourceType).ToHashSet();
        return required.Where(source => !present.Contains(source)).OrderBy(source => source.ToString(), StringComparer.Ordinal).ToList();
    }

    private static double ComputeConfidence(
        ActionEvidenceVerdict verdict,
        IReadOnlyList<EvidenceSearchResult> supporting,
        IReadOnlyList<EvidenceSearchResult> contradictions,
        IReadOnlyList<EvidenceItem> relatedItems)
    {
        if (verdict is ActionEvidenceVerdict.BLOCKED_BY_MISSING_EVIDENCE or ActionEvidenceVerdict.BLOCKED_UNSAFE_LIVE_ACTION)
        {
            return 0;
        }

        var basis = contradictions.Count > 0 ? contradictions : supporting;
        if (basis.Count == 0 && relatedItems.Count > 0)
        {
            return Math.Round(relatedItems.Average(item => item.Confidence), 4);
        }

        return Math.Round(basis.Count == 0 ? 0 : basis.Average(result => result.Confidence), 4);
    }

    private static string BuildRationale(ActionEvidenceVerdict verdict, IReadOnlyList<string> blocking, IReadOnlyList<string> humanActions)
    {
        var lines = blocking.Concat(humanActions).ToList();
        return lines.Count == 0
            ? $"Action scan verdict: {verdict}."
            : $"Action scan verdict: {verdict}. {string.Join(" ", lines)}";
    }
}

public static class EvidenceGraphBuilder
{
    public static EvidenceGraph ForClaim(
        string claim,
        IReadOnlyList<EvidenceSearchResult> supporting,
        IReadOnlyList<EvidenceSearchResult> contradicting,
        IReadOnlyList<EvidenceSearchResult> nuanced)
    {
        var claimId = $"claim:{EvidenceText.StableHash(EvidenceText.Normalize(claim))[..12]}";
        var edges = new List<EvidenceEdge>();
        edges.AddRange(supporting.Select(result => Edge(result.EvidenceId, claimId, EvidenceRelationType.supports_claim, result.Confidence, "Evidence supports claim.")));
        edges.AddRange(contradicting.Select(result => Edge(result.EvidenceId, claimId, EvidenceRelationType.contradicts_claim, result.Confidence, "Evidence contradicts claim.")));
        edges.AddRange(nuanced.Select(result => Edge(result.EvidenceId, claimId, EvidenceRelationType.derived_from, result.Confidence, "Evidence is related but not decisive.")));
        return new EvidenceGraph(edges.OrderBy(edge => edge.FromId, StringComparer.Ordinal).ThenBy(edge => edge.RelationType.ToString(), StringComparer.Ordinal).ToList());
    }

    public static EvidenceGraph ForAction(
        string actionId,
        IReadOnlyList<EvidenceSearchResult> supporting,
        IReadOnlyList<EvidenceSearchResult> contradicting,
        IReadOnlyList<EvidenceItem> relatedItems,
        bool policyBlocked)
    {
        var edges = new List<EvidenceEdge>();
        edges.AddRange(supporting.Select(result => Edge(result.EvidenceId, actionId, EvidenceRelationType.supports_action, result.Confidence, "Evidence supports action in the current fixture/read-only scope.")));
        edges.AddRange(contradicting.Select(result => Edge(result.EvidenceId, actionId, EvidenceRelationType.contradicts_action, result.Confidence, "Evidence contradicts action.")));
        edges.AddRange(relatedItems
            .Where(item => item.PolicyScope == EvidencePolicyScope.RequiresApproval)
            .Select(item => Edge(item.Id, actionId, EvidenceRelationType.requires_approval, item.Confidence, "Evidence requires approval.")));
        if (policyBlocked)
        {
            edges.AddRange(relatedItems
                .Where(item => item.PolicyScope is EvidencePolicyScope.BlocksAction or EvidencePolicyScope.UnsafeLive)
                .Select(item => Edge(item.Id, actionId, EvidenceRelationType.policy_blocks, item.Confidence, "Policy blocks action.")));
        }

        return new EvidenceGraph(edges.OrderBy(edge => edge.FromId, StringComparer.Ordinal).ThenBy(edge => edge.RelationType.ToString(), StringComparer.Ordinal).ToList());
    }

    private static EvidenceEdge Edge(string fromId, string toId, EvidenceRelationType type, double confidence, string rationale)
    {
        return new EvidenceEdge(
            fromId,
            toId,
            type,
            EvidenceText.ClampConfidence(confidence),
            DateTimeOffset.UnixEpoch,
            "evidence-intelligence-layer",
            rationale,
            new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["runtime"] = "not_enabled",
                ["mode"] = "fixture_safe_report"
            });
    }
}

public static class EvidenceReadinessMatrixBuilder
{
    public static ActionReadinessMatrix ForClaim(
        ClaimEvidenceVerdict verdict,
        IReadOnlyList<EvidenceSearchResult> supporting,
        IReadOnlyList<EvidenceSearchResult> contradicting,
        IReadOnlyList<EvidenceSourceType> missingRequired,
        IReadOnlyList<string> blocking,
        double confidence)
    {
        var rows = new List<ActionReadinessMatrixRow>();
        rows.AddRange(supporting.Select(result => Row("claim support", result.SourceType.ToString(), EvidenceReadinessRowStatus.Supports, result.Confidence, EvidenceReadinessRisk.Low, "supports", result.EvidenceId, result.MatchReason)));
        rows.AddRange(contradicting.Select(result => Row("claim contradiction", result.SourceType.ToString(), EvidenceReadinessRowStatus.Contradicts, result.Confidence, EvidenceReadinessRisk.Blocking, "blocks", result.EvidenceId, result.MatchReason)));
        rows.AddRange(missingRequired.Select(source => Row("required source", source.ToString(), EvidenceReadinessRowStatus.Missing, 0, EvidenceReadinessRisk.Blocking, "missing", "", "Required evidence source is missing.")));
        return new ActionReadinessMatrix(rows, verdict.ToString(), confidence, blocking, [], verdict == ClaimEvidenceVerdict.SUPPORTED ? "Read-only review can cite this evidence." : "Collect stronger local evidence or request human review.");
    }

    public static ActionReadinessMatrix ForAction(
        ActionEvidenceScanRequest request,
        ActionEvidenceVerdict verdict,
        IReadOnlyList<EvidenceSearchResult> supporting,
        IReadOnlyList<EvidenceSearchResult> contradicting,
        IReadOnlyList<EvidenceSourceType> missingRequired,
        IReadOnlyList<string> blocking,
        IReadOnlyList<string> humanActions,
        IReadOnlyList<EvidenceItem> relatedItems,
        double confidence)
    {
        var rows = new List<ActionReadinessMatrixRow>();
        rows.AddRange(supporting.Select(result => Row("action support", result.SourceType.ToString(), EvidenceReadinessRowStatus.Supports, result.Confidence, EvidenceReadinessRisk.Low, "supports", result.EvidenceId, result.MatchReason)));
        rows.AddRange(contradicting.Select(result => Row("action contradiction", result.SourceType.ToString(), EvidenceReadinessRowStatus.Contradicts, result.Confidence, EvidenceReadinessRisk.Blocking, "blocks", result.EvidenceId, result.MatchReason)));
        rows.AddRange(missingRequired.Select(source => Row("required source", source.ToString(), EvidenceReadinessRowStatus.Missing, 0, EvidenceReadinessRisk.Blocking, "missing", "", "Required evidence source is missing.")));
        rows.AddRange(relatedItems
            .Where(item => item.PolicyScope == EvidencePolicyScope.RequiresApproval)
            .Select(item => Row("policy", item.SourceType.ToString(), EvidenceReadinessRowStatus.RequiresApproval, item.Confidence, EvidenceReadinessRisk.High, "requires approval", item.Id, "Policy requires human approval.")));
        rows.AddRange(relatedItems
            .Where(item => item.PolicyScope is EvidencePolicyScope.BlocksAction or EvidencePolicyScope.UnsafeLive)
            .Select(item => Row("policy", item.SourceType.ToString(), EvidenceReadinessRowStatus.PolicyBlocks, item.Confidence, EvidenceReadinessRisk.Blocking, "blocks", item.Id, "Policy blocks action.")));
        rows.AddRange(relatedItems
            .Where(item => item.StalenessStatus != EvidenceStalenessStatus.Fresh)
            .Select(item => Row("staleness", item.SourceType.ToString(), EvidenceReadinessRowStatus.Stale, item.Confidence, EvidenceReadinessRisk.High, "review", item.Id, "Evidence is stale.")));
        rows.Add(Row("runtime boundary", request.ActionType.ToString(), request.IsLiveAction ? EvidenceReadinessRowStatus.PolicyBlocks : EvidenceReadinessRowStatus.Info, request.IsLiveAction ? 0 : 1, request.IsLiveAction ? EvidenceReadinessRisk.Blocking : EvidenceReadinessRisk.Low, "runtime not enabled", "", "No live browser, desktop, recorder, OCR or sandbox runtime is enabled by EIL."));

        var safeNext = verdict switch
        {
            ActionEvidenceVerdict.ALLOW_READ_ONLY => "Read-only review is allowed.",
            ActionEvidenceVerdict.ALLOW_FIXTURE_SAFE => "Fixture-safe review may continue; runtime remains disabled.",
            ActionEvidenceVerdict.REQUIRES_APPROVAL => "Request human approval for fixture-only review.",
            _ => "Stop and resolve blocking evidence before any next step."
        };

        return new ActionReadinessMatrix(rows, verdict.ToString(), confidence, blocking, humanActions, safeNext);
    }

    private static ActionReadinessMatrixRow Row(string signal, string source, EvidenceReadinessRowStatus status, double confidence, EvidenceReadinessRisk risk, string decision, string evidenceId, string rationale)
    {
        return new ActionReadinessMatrixRow(signal, source, status, EvidenceText.ClampConfidence(confidence), risk, decision, evidenceId, rationale);
    }
}

public static class EvidenceReportFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string ToStableJson(ClaimEvidenceScanResult result)
    {
        var payload = new
        {
            result.Claim,
            Verdict = result.Verdict.ToString(),
            result.Confidence,
            result.Rationale,
            SupportingEvidence = result.SupportingEvidence.Select(ToPayload).ToList(),
            ContradictingEvidence = result.ContradictingEvidence.Select(ToPayload).ToList(),
            MissingRequiredSourceTypes = result.MissingRequiredSourceTypes.Select(s => s.ToString()).ToList(),
            result.BlockingReasons,
            Matrix = MatrixPayload(result.ReadinessMatrix),
            Runtime = "not_enabled"
        };
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public static string ToStableJson(ActionEvidenceScanResult result)
    {
        var payload = new
        {
            result.ActionId,
            Verdict = result.Verdict.ToString(),
            result.Confidence,
            result.Rationale,
            SupportingEvidence = result.SupportingEvidence.Select(ToPayload).ToList(),
            ContradictingEvidence = result.ContradictingEvidence.Select(ToPayload).ToList(),
            MissingRequiredSourceTypes = result.MissingRequiredSourceTypes.Select(s => s.ToString()).ToList(),
            result.BlockingReasons,
            result.RequiredHumanActions,
            Matrix = MatrixPayload(result.ReadinessMatrix),
            Runtime = "not_enabled"
        };
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public static string ToMarkdown(ClaimEvidenceScanResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Evidence Intelligence Claim Scan");
        builder.AppendLine();
        builder.AppendLine($"- Verdict: `{result.Verdict}`");
        builder.AppendLine($"- Confidence: `{result.Confidence.ToString("0.####", CultureInfo.InvariantCulture)}`");
        builder.AppendLine("- Runtime: `not_enabled`");
        builder.AppendLine();
        AppendEvidenceTable(builder, result.SupportingEvidence, "Supporting Evidence");
        AppendEvidenceTable(builder, result.ContradictingEvidence, "Contradicting Evidence");
        AppendMatrix(builder, result.ReadinessMatrix);
        return builder.ToString().TrimEnd();
    }

    public static string ToMarkdown(ActionEvidenceScanResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Evidence Intelligence Action Scan");
        builder.AppendLine();
        builder.AppendLine($"- Verdict: `{result.Verdict}`");
        builder.AppendLine($"- Confidence: `{result.Confidence.ToString("0.####", CultureInfo.InvariantCulture)}`");
        builder.AppendLine("- Runtime: `not_enabled`");
        builder.AppendLine();
        AppendEvidenceTable(builder, result.SupportingEvidence, "Supporting Evidence");
        AppendEvidenceTable(builder, result.ContradictingEvidence, "Contradicting Evidence");
        AppendMatrix(builder, result.ReadinessMatrix);
        return builder.ToString().TrimEnd();
    }

    private static object ToPayload(EvidenceSearchResult result)
    {
        return new
        {
            result.EvidenceId,
            SourceType = result.SourceType.ToString(),
            result.Score,
            result.Rank,
            result.MatchReason,
            StalenessStatus = result.StalenessStatus.ToString(),
            result.Confidence,
            result.RelationHint
        };
    }

    private static object MatrixPayload(ActionReadinessMatrix matrix)
    {
        return new
        {
            matrix.FinalVerdict,
            matrix.Confidence,
            matrix.BlockingReasons,
            matrix.RequiredHumanActions,
            matrix.SafeNextStep,
            Rows = matrix.Rows.Select(row => new
            {
                row.Signal,
                row.Source,
                Status = row.Status.ToString(),
                row.Confidence,
                Risk = row.Risk.ToString(),
                row.Decision,
                row.EvidenceId,
                row.Rationale
            }).ToList()
        };
    }

    private static void AppendEvidenceTable(StringBuilder builder, IReadOnlyList<EvidenceSearchResult> results, string title)
    {
        builder.AppendLine($"## {title}");
        builder.AppendLine();
        builder.AppendLine("| Evidence | Source | Score | Relation |");
        builder.AppendLine("| --- | --- | ---: | --- |");
        foreach (var result in results.OrderBy(result => result.Rank))
        {
            builder.AppendLine($"| `{result.EvidenceId}` | `{result.SourceType}` | `{result.Score.ToString("0.####", CultureInfo.InvariantCulture)}` | {Escape(result.RelationHint)} |");
        }

        if (results.Count == 0)
        {
            builder.AppendLine("| none | none | 0 | none |");
        }

        builder.AppendLine();
    }

    private static void AppendMatrix(StringBuilder builder, ActionReadinessMatrix matrix)
    {
        builder.AppendLine("## Readiness Matrix");
        builder.AppendLine();
        builder.AppendLine("| Signal | Source | Status | Decision | Evidence | Rationale |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");
        foreach (var row in matrix.Rows)
        {
            builder.AppendLine($"| {Escape(row.Signal)} | `{row.Source}` | `{row.Status}` | {Escape(row.Decision)} | `{row.EvidenceId}` | {Escape(row.Rationale)} |");
        }
    }

    private static string Escape(string value) => value.Replace("|", "/", StringComparison.Ordinal);
}

public static class EvidenceText
{
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex SecretLike = new(@"(?i)\b(api[_-]?key|password|token|authorization)\s*[:=]\s*[^,\s;]+", RegexOptions.Compiled);

    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var normalized = text.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"[^\p{L}\p{Nd}\s:_\-.]", " ");
        return Whitespace.Replace(normalized, " ").Trim();
    }

    public static IReadOnlyList<string> Tokenize(string text)
    {
        return Normalize(text)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length > 1)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    public static string StableHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static double ClampConfidence(double confidence)
    {
        if (double.IsNaN(confidence) || double.IsInfinity(confidence))
        {
            return 0;
        }

        return Math.Round(Math.Clamp(confidence, 0, 1), 4);
    }

    public static IReadOnlyDictionary<string, string> NormalizeMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return new SortedDictionary<string, string>(StringComparer.Ordinal);
        }

        return new SortedDictionary<string, string>(
            metadata.ToDictionary(kv => kv.Key, kv => RedactSecretLike(kv.Value), StringComparer.Ordinal),
            StringComparer.Ordinal);
    }

    public static string RedactIfNeeded(string text, EvidenceSensitivityLevel sensitivity, EvidenceRedactionStatus status)
    {
        if (sensitivity == EvidenceSensitivityLevel.Secret || status is EvidenceRedactionStatus.Required or EvidenceRedactionStatus.RejectedSecret)
        {
            return RedactSecretLike(text);
        }

        return RedactSecretLike(text);
    }

    private static string RedactSecretLike(string value)
    {
        return SecretLike.Replace(value, match =>
        {
            var key = match.Groups[1].Value;
            return $"{key}=<redacted>";
        });
    }
}
