namespace OneBrain.Core.Evidence;

public enum EvidenceIntelligenceSurfaceTone
{
    Success,
    Info,
    Warning,
    Danger,
    Neutral,
    Audit
}

public enum EvidenceIntelligenceSurfaceDecision
{
    ReadyForReadOnlyAudit,
    NeedsHumanReview,
    BlockedByEvidence,
    RuntimeBlocked
}

public sealed record EvidenceIntelligenceSurfaceViewModel(
    string SurfaceId,
    EvidenceIntelligenceSurfaceHeader Header,
    EvidenceIntelligenceIndexSummary IndexSummary,
    EvidenceIntelligenceSemanticNotice SemanticNotice,
    EvidenceIntelligenceSearchPanel SearchPanel,
    EvidenceIntelligenceClaimScanPanel ClaimScanPanel,
    EvidenceIntelligenceActionScanPanel ActionScanPanel,
    EvidenceIntelligenceGraphSummary GraphSummary,
    EvidenceIntelligenceReadinessMatrixPanel ReadinessMatrixPanel,
    IReadOnlyList<string> LocalOnlyNotices,
    IReadOnlyList<string> ReadOnlyActionLabels,
    string NoRuntimeNotice)
{
    public bool ReadOnly => true;
    public bool LocalOnly => true;
    public bool RuntimeEnabled => false;
    public bool ActionExecutionEnabled => false;
    public bool CallsProviderOrNetwork => false;
    public bool UsesCloud => false;
    public bool UsesSemanticBackend => false;
    public bool UsesLiveBrowser => false;
    public bool UsesLiveDesktop => false;
    public bool ActivatesOcrRuntime => false;
    public bool StartsRecorder => false;
    public bool StartsSandbox => false;
    public bool WritesProductFiles => false;
}

public sealed record EvidenceIntelligenceSurfaceHeader(
    string Title,
    string Subtitle,
    EvidenceIntelligenceSurfaceDecision Decision,
    EvidenceIntelligenceSurfaceTone Tone,
    IReadOnlyList<string> Badges,
    string Summary);

public sealed record EvidenceIntelligenceIndexSummary(
    int TotalEvidenceItems,
    IReadOnlyDictionary<string, int> SourceTypeCounts,
    int FreshCount,
    int StaleOrUnknownCount,
    int RedactedCount,
    int LowConfidenceCount,
    IReadOnlyList<string> PolicyScopes,
    string Summary);

public sealed record EvidenceIntelligenceSemanticNotice(
    string StatusLabel,
    string RetrievalModeLabel,
    string Copy,
    bool SemanticSearchAvailable,
    bool LexicalFallbackIsReal);

public sealed record EvidenceIntelligenceSearchPanel(
    string Query,
    string PurposeLabel,
    int ResultCount,
    IReadOnlyList<EvidenceIntelligenceSearchResultRow> Results,
    string EmptyState,
    string FooterNote);

public sealed record EvidenceIntelligenceSearchResultRow(
    string EvidenceId,
    string SourceType,
    string Snippet,
    string ScoreLabel,
    int Rank,
    string MatchReason,
    string RelationHint,
    EvidenceIntelligenceSurfaceTone Tone);

public sealed record EvidenceIntelligenceClaimScanPanel(
    string Claim,
    string VerdictLabel,
    EvidenceIntelligenceSurfaceTone Tone,
    double Confidence,
    string Rationale,
    IReadOnlyList<string> SupportingEvidenceIds,
    IReadOnlyList<string> ContradictingEvidenceIds,
    IReadOnlyList<string> MissingRequiredSourceTypes,
    IReadOnlyList<string> BlockingReasons,
    bool RequiresHumanReview,
    string MarkdownPreview);

public sealed record EvidenceIntelligenceActionScanPanel(
    string ActionId,
    string ActionType,
    string Target,
    string VerdictLabel,
    EvidenceIntelligenceSurfaceTone Tone,
    double Confidence,
    string Rationale,
    IReadOnlyList<string> SupportingEvidenceIds,
    IReadOnlyList<string> ContradictingEvidenceIds,
    IReadOnlyList<string> MissingRequiredSourceTypes,
    IReadOnlyList<string> BlockingReasons,
    IReadOnlyList<string> RequiredHumanActions,
    string SafeNextStep,
    bool ActionExecutionEnabled,
    string MarkdownPreview);

public sealed record EvidenceIntelligenceGraphSummary(
    int EdgeCount,
    IReadOnlyDictionary<string, int> RelationCounts,
    IReadOnlyList<string> TopEdges,
    string Summary);

public sealed record EvidenceIntelligenceReadinessMatrixPanel(
    string FinalVerdict,
    double Confidence,
    IReadOnlyList<EvidenceIntelligenceReadinessMatrixRowViewModel> Rows,
    IReadOnlyList<string> BlockingReasons,
    IReadOnlyList<string> RequiredHumanActions,
    string SafeNextStep,
    bool BlocksRuntime);

public sealed record EvidenceIntelligenceReadinessMatrixRowViewModel(
    string Signal,
    string Source,
    string Status,
    double Confidence,
    string Risk,
    string Decision,
    string EvidenceId,
    string Rationale,
    EvidenceIntelligenceSurfaceTone Tone);

public sealed record EvidenceIntelligenceSurfaceRequest(
    EvidenceQuery SearchQuery,
    ClaimEvidenceScanRequest ClaimRequest,
    ActionEvidenceScanRequest ActionRequest);

public static class EvidenceIntelligenceReadOnlyPresenter
{
    private const string RuntimeNotice = "Evidence Intelligence is read-only and local-only. Runtime not enabled; no live browser, desktop, OCR, recorder, sandbox, provider, cloud or network action is available.";

    private static readonly string[] ReadOnlyActions =
    [
        "Review evidence",
        "Inspect claim scan",
        "Inspect action scan",
        "Copy summary",
        "Open audit handoff"
    ];

    public static EvidenceIntelligenceSurfaceViewModel CreateFixture()
    {
        var index = EvidenceIntelligenceSurfaceFixtureCatalog.CreateContradictionFirstFixtureIndex();
        return Create(index, EvidenceIntelligenceSurfaceFixtureCatalog.DefaultRequest());
    }

    public static EvidenceIntelligenceSurfaceViewModel Create(
        IEvidenceIntelligenceIndex index,
        EvidenceIntelligenceSurfaceRequest request)
    {
        var router = new EvidenceIntelligenceRetrievalRouter(index);
        var searchResults = router.Search(request.SearchQuery);
        var claim = new ClaimEvidenceScanner(index).Scan(request.ClaimRequest);
        var action = new ActionEvidenceScanner(index).Scan(request.ActionRequest);
        var decision = Decision(claim, action);

        return new EvidenceIntelligenceSurfaceViewModel(
            SurfaceId: "evidence-intelligence.read-only-surface.v1",
            Header: Header(decision, claim, action),
            IndexSummary: IndexSummary(index),
            SemanticNotice: SemanticNotice(router.SemanticBackendStatus),
            SearchPanel: SearchPanel(request.SearchQuery, searchResults),
            ClaimScanPanel: ClaimPanel(claim),
            ActionScanPanel: ActionPanel(action, request.ActionRequest),
            GraphSummary: GraphSummary(claim.Graph, action.Graph),
            ReadinessMatrixPanel: MatrixPanel(action.ReadinessMatrix, action.Verdict),
            LocalOnlyNotices:
            [
                "Local fixture evidence only.",
                "Lexical retrieval is deterministic and real.",
            "Semantic backend is disabled; no semantic capability is claimed.",
                "Contradictions are shown before confirmations.",
                "Action scan evaluates readiness only; it does not execute actions."
            ],
            ReadOnlyActionLabels,
            RuntimeNotice);
    }

    public static IReadOnlyList<string> ReadOnlyActionLabels => ReadOnlyActions;

    private static EvidenceIntelligenceSurfaceHeader Header(
        EvidenceIntelligenceSurfaceDecision decision,
        ClaimEvidenceScanResult claim,
        ActionEvidenceScanResult action)
    {
        var tone = decision switch
        {
            EvidenceIntelligenceSurfaceDecision.ReadyForReadOnlyAudit => EvidenceIntelligenceSurfaceTone.Success,
            EvidenceIntelligenceSurfaceDecision.NeedsHumanReview => EvidenceIntelligenceSurfaceTone.Warning,
            EvidenceIntelligenceSurfaceDecision.BlockedByEvidence => EvidenceIntelligenceSurfaceTone.Danger,
            _ => EvidenceIntelligenceSurfaceTone.Audit
        };

        return new EvidenceIntelligenceSurfaceHeader(
            "Evidence Intelligence",
            "Read-only local evidence audit surface",
            decision,
            tone,
            ["Read-only", "Local-only", "Runtime not enabled", "Semantic backend disabled", "Contradiction-first"],
            $"Claim scan: {claim.Verdict}. Action scan: {action.Verdict}. No action is performed.");
    }

    private static EvidenceIntelligenceIndexSummary IndexSummary(IEvidenceIntelligenceIndex index)
    {
        var items = index.All();
        var sourceCounts = items
            .GroupBy(item => item.SourceType.ToString())
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var policyScopes = items
            .Select(item => item.PolicyScope.ToString())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        return new EvidenceIntelligenceIndexSummary(
            items.Count,
            sourceCounts,
            items.Count(item => item.StalenessStatus == EvidenceStalenessStatus.Fresh),
            items.Count(item => item.StalenessStatus != EvidenceStalenessStatus.Fresh),
            items.Count(item => item.RedactionStatus is EvidenceRedactionStatus.Applied or EvidenceRedactionStatus.Required or EvidenceRedactionStatus.RejectedSecret),
            items.Count(item => item.Confidence < 0.5),
            policyScopes,
            "Evidence index summary is local and fixture-safe; it does not persist or fetch external data.");
    }

    private static EvidenceIntelligenceSemanticNotice SemanticNotice(EvidenceSemanticBackendStatus status) =>
        new(
            status.ToString(),
            "Lexical deterministic retrieval",
            "Semantic/vector backend is disabled. Results come from deterministic lexical matching, source weighting, confidence and staleness handling.",
            SemanticSearchAvailable: false,
            LexicalFallbackIsReal: true);

    private static EvidenceIntelligenceSearchPanel SearchPanel(EvidenceQuery query, IReadOnlyList<EvidenceSearchResult> results) =>
        new(
            query.Query,
            query.Purpose.ToString(),
            results.Count,
            results.Select(ResultRow).ToList(),
            "No local evidence matched this lexical query.",
            "Search is read-only. It does not call cloud, provider, network or semantic services.");

    private static EvidenceIntelligenceSearchResultRow ResultRow(EvidenceSearchResult result) =>
        new(
            result.EvidenceId,
            result.SourceType.ToString(),
            result.Snippet,
            result.Score.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture),
            result.Rank,
            result.MatchReason,
            result.RelationHint,
            result.RelationHint.Contains("contradict", StringComparison.OrdinalIgnoreCase)
                ? EvidenceIntelligenceSurfaceTone.Danger
                : EvidenceIntelligenceSurfaceTone.Info);

    private static EvidenceIntelligenceClaimScanPanel ClaimPanel(ClaimEvidenceScanResult result) =>
        new(
            result.Claim,
            result.Verdict.ToString(),
            ClaimTone(result.Verdict),
            result.Confidence,
            result.Rationale,
            result.SupportingEvidence.Select(e => e.EvidenceId).ToList(),
            result.ContradictingEvidence.Select(e => e.EvidenceId).ToList(),
            result.MissingRequiredSourceTypes.Select(s => s.ToString()).ToList(),
            result.BlockingReasons,
            result.Verdict != ClaimEvidenceVerdict.SUPPORTED,
            result.MarkdownReport);

    private static EvidenceIntelligenceActionScanPanel ActionPanel(
        ActionEvidenceScanResult result,
        ActionEvidenceScanRequest request) =>
        new(
            result.ActionId,
            request.ActionType.ToString(),
            request.Target,
            result.Verdict.ToString(),
            ActionTone(result.Verdict),
            result.Confidence,
            result.Rationale,
            result.SupportingEvidence.Select(e => e.EvidenceId).ToList(),
            result.ContradictingEvidence.Select(e => e.EvidenceId).ToList(),
            result.MissingRequiredSourceTypes.Select(s => s.ToString()).ToList(),
            result.BlockingReasons,
            result.RequiredHumanActions,
            result.ReadinessMatrix.SafeNextStep,
            ActionExecutionEnabled: false,
            result.MarkdownReport);

    private static EvidenceIntelligenceGraphSummary GraphSummary(EvidenceGraph claimGraph, EvidenceGraph actionGraph)
    {
        var edges = claimGraph.Edges.Concat(actionGraph.Edges).ToList();
        var counts = edges
            .GroupBy(edge => edge.RelationType.ToString())
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        var topEdges = edges
            .OrderBy(edge => edge.FromId, StringComparer.Ordinal)
            .ThenBy(edge => edge.ToId, StringComparer.Ordinal)
            .ThenBy(edge => edge.RelationType.ToString(), StringComparer.Ordinal)
            .Take(8)
            .Select(edge => $"{edge.FromId} {edge.RelationType} {edge.ToId}")
            .ToList();

        return new EvidenceIntelligenceGraphSummary(
            edges.Count,
            counts,
            topEdges,
            "Typed evidence graph summarizes support, contradiction, policy and approval relations for read-only audit.");
    }

    private static EvidenceIntelligenceReadinessMatrixPanel MatrixPanel(ActionReadinessMatrix matrix, ActionEvidenceVerdict verdict) =>
        new(
            matrix.FinalVerdict,
            matrix.Confidence,
            matrix.Rows.Select(MatrixRow).ToList(),
            matrix.BlockingReasons,
            matrix.RequiredHumanActions,
            matrix.SafeNextStep,
            BlocksRuntime: verdict != ActionEvidenceVerdict.ALLOW_READ_ONLY && verdict != ActionEvidenceVerdict.ALLOW_FIXTURE_SAFE);

    private static EvidenceIntelligenceReadinessMatrixRowViewModel MatrixRow(ActionReadinessMatrixRow row) =>
        new(
            row.Signal,
            row.Source,
            row.Status.ToString(),
            row.Confidence,
            row.Risk.ToString(),
            row.Decision,
            row.EvidenceId,
            row.Rationale,
            row.Risk == EvidenceReadinessRisk.Blocking || row.Status is EvidenceReadinessRowStatus.Contradicts or EvidenceReadinessRowStatus.PolicyBlocks or EvidenceReadinessRowStatus.Missing
                ? EvidenceIntelligenceSurfaceTone.Danger
                : EvidenceIntelligenceSurfaceTone.Info);

    private static EvidenceIntelligenceSurfaceDecision Decision(
        ClaimEvidenceScanResult claim,
        ActionEvidenceScanResult action)
    {
        if (action.Verdict == ActionEvidenceVerdict.BLOCKED_UNSAFE_LIVE_ACTION)
        {
            return EvidenceIntelligenceSurfaceDecision.RuntimeBlocked;
        }

        if (claim.Verdict == ClaimEvidenceVerdict.CONTRADICTED
            || action.Verdict is ActionEvidenceVerdict.BLOCKED_BY_CONTRADICTION
                or ActionEvidenceVerdict.BLOCKED_BY_MISSING_EVIDENCE
                or ActionEvidenceVerdict.BLOCKED_BY_POLICY
                or ActionEvidenceVerdict.BLOCKED_BY_STALE_EVIDENCE)
        {
            return EvidenceIntelligenceSurfaceDecision.BlockedByEvidence;
        }

        if (claim.Verdict != ClaimEvidenceVerdict.SUPPORTED
            || action.Verdict == ActionEvidenceVerdict.REQUIRES_APPROVAL)
        {
            return EvidenceIntelligenceSurfaceDecision.NeedsHumanReview;
        }

        return EvidenceIntelligenceSurfaceDecision.ReadyForReadOnlyAudit;
    }

    private static EvidenceIntelligenceSurfaceTone ClaimTone(ClaimEvidenceVerdict verdict) =>
        verdict switch
        {
            ClaimEvidenceVerdict.SUPPORTED => EvidenceIntelligenceSurfaceTone.Success,
            ClaimEvidenceVerdict.CONTRADICTED => EvidenceIntelligenceSurfaceTone.Danger,
            ClaimEvidenceVerdict.REQUIRES_HUMAN_REVIEW => EvidenceIntelligenceSurfaceTone.Warning,
            _ => EvidenceIntelligenceSurfaceTone.Warning
        };

    private static EvidenceIntelligenceSurfaceTone ActionTone(ActionEvidenceVerdict verdict) =>
        verdict switch
        {
            ActionEvidenceVerdict.ALLOW_READ_ONLY or ActionEvidenceVerdict.ALLOW_FIXTURE_SAFE => EvidenceIntelligenceSurfaceTone.Success,
            ActionEvidenceVerdict.REQUIRES_APPROVAL => EvidenceIntelligenceSurfaceTone.Warning,
            _ => EvidenceIntelligenceSurfaceTone.Danger
        };
}

public static class EvidenceIntelligenceSurfaceFixtureCatalog
{
    public static InMemoryEvidenceIntelligenceIndex CreateContradictionFirstFixtureIndex()
    {
        var index = new InMemoryEvidenceIntelligenceIndex();
        const string redactedKey = "token";
        const string redactionProbe = "fixture-redaction-probe";
        index.Ingest(Evidence("ev.dom.invoice", EvidenceSourceType.Dom, "Invoice total visible and download invoice link present.", target: "download invoice"));
        index.Ingest(Evidence("ev.ocr.pay", EvidenceSourceType.Ocr, "OCR supporting signal reads button text Pagar.", target: "Pagar", confidence: 0.72));
        index.Ingest(Evidence("ev.uia.delete", EvidenceSourceType.Uia, "UIA signal reads button automation name Eliminar cuenta.", target: "Eliminar cuenta", relation: "contradicts_observation", confidence: 0.94));
        index.Ingest(Evidence("ev.policy.block", EvidenceSourceType.Policy, "Policy blocks destructive or externally ambiguous action.", target: "Pagar", policyScope: EvidencePolicyScope.BlocksAction));
        index.Ingest(Evidence("ev.validation.stale", EvidenceSourceType.ValidationReport, "Previous validation said invoice download was available.", target: "download invoice", staleness: EvidenceStalenessStatus.Stale, confidence: 0.83));
        index.Ingest(EvidenceItem.Create(
            "ev.secret.redacted",
            EvidenceSourceType.AgentObservation,
            "fixture:redacted",
            $"operator note {redactedKey}={redactionProbe}",
            DateTimeOffset.UnixEpoch,
            EvidenceItem.DefaultWorkspaceId,
            EvidenceItem.DefaultSessionId,
            0.9,
            EvidenceSensitivityLevel.Secret,
            EvidenceRedactionStatus.Required,
            EvidenceStalenessStatus.Fresh,
            EvidencePolicyScope.ReadOnly,
            new Dictionary<string, string> { ["purpose"] = "redaction proof", ["relation"] = "redacted_source" }));
        return index;
    }

    public static EvidenceIntelligenceSurfaceRequest DefaultRequest() =>
        new(
            new EvidenceQuery(
                "button invoice Pagar",
                EvidenceQueryPurpose.Audit,
                [],
                EvidenceItem.DefaultWorkspaceId,
                EvidenceItem.DefaultSessionId,
                10,
                IncludeContradictions: true,
                IncludeStale: true),
            new ClaimEvidenceScanRequest(
                "invoice total visible",
                EvidenceItem.DefaultWorkspaceId,
                EvidenceItem.DefaultSessionId,
                [EvidenceSourceType.Dom]),
            new ActionEvidenceScanRequest(
                "action.pay-or-delete",
                EvidenceActionType.Click,
                "Pagar",
                EvidenceItem.DefaultWorkspaceId,
                EvidenceItem.DefaultSessionId,
                [EvidenceSourceType.Ocr, EvidenceSourceType.Uia, EvidenceSourceType.Policy],
                IsFixtureSafe: true,
                IsSensitive: true));

    public static EvidenceIntelligenceSurfaceRequest MissingEvidenceRequest() =>
        new(
            new EvidenceQuery("missing approval", EvidenceQueryPurpose.Audit, [], EvidenceItem.DefaultWorkspaceId, EvidenceItem.DefaultSessionId, 10, true, false),
            new ClaimEvidenceScanRequest("approval present", EvidenceItem.DefaultWorkspaceId, EvidenceItem.DefaultSessionId, [EvidenceSourceType.Approval]),
            new ActionEvidenceScanRequest("action.submit.missing", EvidenceActionType.Submit, "submit invoice", EvidenceItem.DefaultWorkspaceId, EvidenceItem.DefaultSessionId, [EvidenceSourceType.Approval]));

    private static EvidenceItem Evidence(
        string id,
        EvidenceSourceType source,
        string text,
        string target,
        string relation = "supports",
        double confidence = 1,
        EvidenceStalenessStatus staleness = EvidenceStalenessStatus.Fresh,
        EvidencePolicyScope policyScope = EvidencePolicyScope.ReadOnly) =>
        EvidenceItem.Create(
            id,
            source,
            $"fixture:{id}",
            text,
            DateTimeOffset.UnixEpoch,
            EvidenceItem.DefaultWorkspaceId,
            EvidenceItem.DefaultSessionId,
            confidence,
            EvidenceSensitivityLevel.Internal,
            EvidenceRedactionStatus.None,
            staleness,
            policyScope,
            new Dictionary<string, string>
            {
                ["target"] = target,
                ["relation"] = relation,
                ["fixture"] = "evidence-intelligence-read-only-surface"
            });
}
