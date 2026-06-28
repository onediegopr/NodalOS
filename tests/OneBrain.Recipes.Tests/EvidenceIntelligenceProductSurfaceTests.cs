using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Evidence;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("EvidenceIntelligence")]
[TestCategory("EvidenceIntelligenceProductSurface")]
public sealed class EvidenceIntelligenceProductSurfaceTests
{
    [TestMethod]
    public void SurfaceRendersLexicalSearchResultsAndIndexSummary()
    {
        var surface = EvidenceIntelligenceReadOnlyPresenter.CreateFixture();

        Assert.AreEqual("evidence-intelligence.read-only-surface.v1", surface.SurfaceId);
        Assert.IsTrue(surface.IndexSummary.TotalEvidenceItems >= 6);
        Assert.IsTrue(surface.IndexSummary.SourceTypeCounts.ContainsKey(EvidenceSourceType.Dom.ToString()));
        Assert.IsTrue(surface.SearchPanel.ResultCount > 0);
        Assert.IsTrue(surface.SearchPanel.Results.Any(result => result.EvidenceId == "ev.uia.delete"));
        StringAssert.Contains(surface.SearchPanel.FooterNote, "read-only");
    }

    [TestMethod]
    public void SurfaceShowsSemanticBackendDisabledWithoutFakeSemanticSuccess()
    {
        var surface = EvidenceIntelligenceReadOnlyPresenter.CreateFixture();

        Assert.AreEqual(EvidenceSemanticBackendStatus.Disabled.ToString(), surface.SemanticNotice.StatusLabel);
        Assert.IsFalse(surface.SemanticNotice.SemanticSearchAvailable);
        Assert.IsTrue(surface.SemanticNotice.LexicalFallbackIsReal);
        StringAssert.Contains(surface.SemanticNotice.Copy, "Semantic/vector backend is disabled");
        AssertSurfaceDoesNotContain(surface, "semantic backend active");
        AssertSurfaceDoesNotContain(surface, "semantic success");
    }

    [TestMethod]
    public void ClaimScanSurfaceShowsSupportedContradictedAndInsufficientStates()
    {
        var supportedIndex = new InMemoryEvidenceIntelligenceIndex();
        supportedIndex.Ingest(EvidenceItem.Create("ev.dom.support", EvidenceSourceType.Dom, "fixture", "invoice total visible"));
        var supported = EvidenceIntelligenceReadOnlyPresenter.Create(supportedIndex, Request("invoice total visible", EvidenceSourceType.Dom));
        Assert.AreEqual(ClaimEvidenceVerdict.SUPPORTED.ToString(), supported.ClaimScanPanel.VerdictLabel);
        CollectionAssert.Contains(supported.ClaimScanPanel.SupportingEvidenceIds.ToList(), "ev.dom.support");

        var contradictedIndex = new InMemoryEvidenceIntelligenceIndex();
        contradictedIndex.Ingest(EvidenceItem.Create("ev.claim.support", EvidenceSourceType.Ocr, "fixture", "invoice paid", metadata: new Dictionary<string, string> { ["relation"] = "supports" }));
        contradictedIndex.Ingest(EvidenceItem.Create("ev.claim.contradict", EvidenceSourceType.Uia, "fixture", "invoice unpaid", metadata: new Dictionary<string, string> { ["relation"] = "contradicts_claim" }));
        var contradicted = EvidenceIntelligenceReadOnlyPresenter.Create(contradictedIndex, new EvidenceIntelligenceSurfaceRequest(
            new EvidenceQuery("invoice paid", EvidenceQueryPurpose.Audit, [], EvidenceItem.DefaultWorkspaceId, EvidenceItem.DefaultSessionId, 10, true, true),
            new ClaimEvidenceScanRequest("invoice paid", EvidenceItem.DefaultWorkspaceId, EvidenceItem.DefaultSessionId, [EvidenceSourceType.Ocr, EvidenceSourceType.Uia]),
            new ActionEvidenceScanRequest("action.read", EvidenceActionType.Read, "invoice paid", EvidenceItem.DefaultWorkspaceId, EvidenceItem.DefaultSessionId, [EvidenceSourceType.Ocr])));
        Assert.AreEqual(ClaimEvidenceVerdict.CONTRADICTED.ToString(), contradicted.ClaimScanPanel.VerdictLabel);
        CollectionAssert.Contains(contradicted.ClaimScanPanel.ContradictingEvidenceIds.ToList(), "ev.claim.contradict");

        var insufficient = EvidenceIntelligenceReadOnlyPresenter.Create(new InMemoryEvidenceIntelligenceIndex(), EvidenceIntelligenceSurfaceFixtureCatalog.MissingEvidenceRequest());
        Assert.AreEqual(ClaimEvidenceVerdict.INSUFFICIENT_EVIDENCE.ToString(), insufficient.ClaimScanPanel.VerdictLabel);
        CollectionAssert.Contains(insufficient.ClaimScanPanel.MissingRequiredSourceTypes.ToList(), EvidenceSourceType.Approval.ToString());
    }

    [TestMethod]
    public void ClaimScanSurfaceShowsStaleAndLowConfidenceStates()
    {
        var staleIndex = new InMemoryEvidenceIntelligenceIndex();
        staleIndex.Ingest(EvidenceItem.Create("ev.stale", EvidenceSourceType.Dom, "fixture", "invoice present", stalenessStatus: EvidenceStalenessStatus.Stale));
        var stale = EvidenceIntelligenceReadOnlyPresenter.Create(staleIndex, Request("invoice present", EvidenceSourceType.Dom));
        Assert.AreEqual(ClaimEvidenceVerdict.STALE_EVIDENCE.ToString(), stale.ClaimScanPanel.VerdictLabel);

        var lowIndex = new InMemoryEvidenceIntelligenceIndex();
        lowIndex.Ingest(EvidenceItem.Create("ev.low", EvidenceSourceType.Dom, "fixture", "invoice present", confidence: 0.2));
        var low = EvidenceIntelligenceReadOnlyPresenter.Create(lowIndex, Request("invoice present", EvidenceSourceType.Dom));
        Assert.AreEqual(ClaimEvidenceVerdict.LOW_CONFIDENCE.ToString(), low.ClaimScanPanel.VerdictLabel);
    }

    [TestMethod]
    public void ContradictionWinsOverSupportInProductSurface()
    {
        var index = new InMemoryEvidenceIntelligenceIndex();
        index.Ingest(EvidenceItem.Create("ev.ocr.pay", EvidenceSourceType.Ocr, "fixture", "button text Pagar", metadata: new Dictionary<string, string> { ["target"] = "Pagar" }));
        index.Ingest(EvidenceItem.Create("ev.uia.delete", EvidenceSourceType.Uia, "fixture", "button automation name Eliminar cuenta", metadata: new Dictionary<string, string> { ["target"] = "Eliminar cuenta", ["relation"] = "contradicts_observation" }));
        var surface = EvidenceIntelligenceReadOnlyPresenter.Create(index, ActionRequest("Pagar", EvidenceActionType.Click, [EvidenceSourceType.Ocr, EvidenceSourceType.Uia], isFixtureSafe: true));

        Assert.AreEqual(ActionEvidenceVerdict.BLOCKED_BY_CONTRADICTION.ToString(), surface.ActionScanPanel.VerdictLabel);
        Assert.AreEqual(EvidenceIntelligenceSurfaceDecision.BlockedByEvidence, surface.Header.Decision);
        CollectionAssert.Contains(surface.ActionScanPanel.SupportingEvidenceIds.ToList(), "ev.ocr.pay");
        CollectionAssert.Contains(surface.ActionScanPanel.ContradictingEvidenceIds.ToList(), "ev.uia.delete");
        Assert.IsTrue(surface.ReadinessMatrixPanel.Rows.Any(row => row.Status == EvidenceReadinessRowStatus.Contradicts.ToString()));
    }

    [TestMethod]
    public void ActionScanSurfaceFailClosesUnsafeMissingStaleAndPolicyBlocked()
    {
        var fixture = EvidenceIntelligenceReadOnlyPresenter.CreateFixture();
        Assert.AreEqual(ActionEvidenceVerdict.BLOCKED_BY_POLICY.ToString(), fixture.ActionScanPanel.VerdictLabel);
        Assert.IsTrue(fixture.ReadinessMatrixPanel.Rows.Any(row => row.Status == EvidenceReadinessRowStatus.Contradicts.ToString()));

        var missing = EvidenceIntelligenceReadOnlyPresenter.Create(new InMemoryEvidenceIntelligenceIndex(), EvidenceIntelligenceSurfaceFixtureCatalog.MissingEvidenceRequest());
        Assert.AreEqual(ActionEvidenceVerdict.BLOCKED_BY_MISSING_EVIDENCE.ToString(), missing.ActionScanPanel.VerdictLabel);

        var staleIndex = new InMemoryEvidenceIntelligenceIndex();
        staleIndex.Ingest(EvidenceItem.Create("ev.stale.action", EvidenceSourceType.Dom, "fixture", "submit invoice", stalenessStatus: EvidenceStalenessStatus.Stale));
        var stale = EvidenceIntelligenceReadOnlyPresenter.Create(staleIndex, ActionRequest("submit invoice", EvidenceActionType.Submit, [EvidenceSourceType.Dom], isFixtureSafe: true));
        Assert.AreEqual(ActionEvidenceVerdict.BLOCKED_BY_STALE_EVIDENCE.ToString(), stale.ActionScanPanel.VerdictLabel);

        var policyIndex = new InMemoryEvidenceIntelligenceIndex();
        policyIndex.Ingest(EvidenceItem.Create("ev.policy", EvidenceSourceType.Policy, "fixture", "policy blocks submit invoice", policyScope: EvidencePolicyScope.BlocksAction, metadata: new Dictionary<string, string> { ["target"] = "submit invoice" }));
        var policy = EvidenceIntelligenceReadOnlyPresenter.Create(policyIndex, ActionRequest("submit invoice", EvidenceActionType.Submit, [EvidenceSourceType.Policy]));
        Assert.AreEqual(ActionEvidenceVerdict.BLOCKED_BY_POLICY.ToString(), policy.ActionScanPanel.VerdictLabel);

        var live = EvidenceIntelligenceReadOnlyPresenter.Create(EvidenceIntelligenceSurfaceFixtureCatalog.CreateContradictionFirstFixtureIndex(), ActionRequest("download invoice", EvidenceActionType.Click, [EvidenceSourceType.Dom], isLiveAction: true));
        Assert.AreEqual(ActionEvidenceVerdict.BLOCKED_UNSAFE_LIVE_ACTION.ToString(), live.ActionScanPanel.VerdictLabel);
        Assert.AreEqual(EvidenceIntelligenceSurfaceDecision.RuntimeBlocked, live.Header.Decision);
    }

    [TestMethod]
    public void ReadinessMatrixExposesBlockingReasonsRequiredHumanActionsAndSafeNextStep()
    {
        var index = new InMemoryEvidenceIntelligenceIndex();
        index.Ingest(EvidenceItem.Create("ev.approval", EvidenceSourceType.Approval, "fixture", "human approval required for submit invoice", policyScope: EvidencePolicyScope.RequiresApproval, metadata: new Dictionary<string, string> { ["target"] = "submit invoice" }));

        var surface = EvidenceIntelligenceReadOnlyPresenter.Create(index, ActionRequest("submit invoice", EvidenceActionType.Submit, [EvidenceSourceType.Approval], requiresApproval: true));

        Assert.AreEqual(ActionEvidenceVerdict.REQUIRES_APPROVAL.ToString(), surface.ActionScanPanel.VerdictLabel);
        Assert.IsTrue(surface.ReadinessMatrixPanel.RequiredHumanActions.Count > 0);
        Assert.IsTrue(surface.ReadinessMatrixPanel.Rows.Any(row => row.Status == EvidenceReadinessRowStatus.RequiresApproval.ToString()));
        StringAssert.Contains(surface.ReadinessMatrixPanel.SafeNextStep, "approval");
    }

    [TestMethod]
    public void GraphSummaryIncludesTypedContradictionAndPolicyEdges()
    {
        var surface = EvidenceIntelligenceReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(surface.GraphSummary.EdgeCount > 0);
        Assert.IsTrue(surface.GraphSummary.RelationCounts.ContainsKey(EvidenceRelationType.contradicts_action.ToString()));
        Assert.IsTrue(surface.GraphSummary.RelationCounts.ContainsKey(EvidenceRelationType.policy_blocks.ToString()));
        Assert.IsTrue(surface.GraphSummary.TopEdges.Any(edge => edge.Contains("ev.uia.delete", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void SurfaceExposesNoRuntimeNoProviderNoNetworkNoLiveFlags()
    {
        var surface = EvidenceIntelligenceReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(surface.ReadOnly);
        Assert.IsTrue(surface.LocalOnly);
        Assert.IsFalse(surface.RuntimeEnabled);
        Assert.IsFalse(surface.ActionExecutionEnabled);
        Assert.IsFalse(surface.CallsProviderOrNetwork);
        Assert.IsFalse(surface.UsesCloud);
        Assert.IsFalse(surface.UsesSemanticBackend);
        Assert.IsFalse(surface.UsesLiveBrowser);
        Assert.IsFalse(surface.UsesLiveDesktop);
        Assert.IsFalse(surface.ActivatesOcrRuntime);
        Assert.IsFalse(surface.StartsRecorder);
        Assert.IsFalse(surface.StartsSandbox);
        Assert.IsFalse(surface.WritesProductFiles);
        Assert.IsFalse(surface.ActionScanPanel.ActionExecutionEnabled);
        StringAssert.Contains(surface.NoRuntimeNotice, "Runtime not enabled");
    }

    [TestMethod]
    public void SurfaceDoesNotExposeForbiddenLiveActionLabelsOrRawSecrets()
    {
        var surface = EvidenceIntelligenceReadOnlyPresenter.CreateFixture();
        var text = SurfaceText(surface);
        var forbiddenSurfacePhrases = new[]
        {
            "Execute now",
            "Start adapter",
            "Launch browser",
            "Connect CDP",
            "Replay",
            "Record live",
            "Capture screen",
            "Enable runtime",
            "Approve runtime",
            "AI understands evidence semantically",
            "semantic search active",
            "semantic success",
            "fixture-secret-value"
        };

        foreach (var term in forbiddenSurfacePhrases)
        {
            Assert.IsFalse(text.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }

        CollectionAssert.DoesNotContain(surface.ReadOnlyActionLabels.ToList(), "Run");
        Assert.IsTrue(surface.IndexSummary.RedactedCount > 0);
    }

    [TestMethod]
    public void SurfacePresenterSourceDoesNotContainRuntimePrimitives()
    {
        var root = GitRoot();
        var source = File.ReadAllText(Path.Combine(root, "src", "OneBrain.Core", "Evidence", "EvidenceIntelligenceProductSurface.cs"));
        var forbidden = new[]
        {
            "Process.Start",
            "HttpClient",
            "ClientWebSocket",
            "Playwright",
            "Puppeteer",
            "Selenium",
            "connectOverCDP",
            "ChromeCdpBrowserLauncher",
            "UIA3Automation",
            "CaptureScreen",
            "Docker",
            "OpenAI"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(source.Contains(term, StringComparison.Ordinal), term);
        }
    }

    private static EvidenceIntelligenceSurfaceRequest Request(string claim, EvidenceSourceType source) =>
        new(
            new EvidenceQuery(claim, EvidenceQueryPurpose.Audit, [], EvidenceItem.DefaultWorkspaceId, EvidenceItem.DefaultSessionId, 10, true, true),
            new ClaimEvidenceScanRequest(claim, EvidenceItem.DefaultWorkspaceId, EvidenceItem.DefaultSessionId, [source]),
            new ActionEvidenceScanRequest("action.read", EvidenceActionType.Read, claim, EvidenceItem.DefaultWorkspaceId, EvidenceItem.DefaultSessionId, [source]));

    private static EvidenceIntelligenceSurfaceRequest ActionRequest(
        string target,
        EvidenceActionType actionType,
        IReadOnlyList<EvidenceSourceType> required,
        bool isFixtureSafe = false,
        bool isLiveAction = false,
        bool requiresApproval = false) =>
        new(
            new EvidenceQuery(target, EvidenceQueryPurpose.Audit, [], EvidenceItem.DefaultWorkspaceId, EvidenceItem.DefaultSessionId, 10, true, true),
            new ClaimEvidenceScanRequest(target, EvidenceItem.DefaultWorkspaceId, EvidenceItem.DefaultSessionId, required),
            new ActionEvidenceScanRequest("action.surface", actionType, target, EvidenceItem.DefaultWorkspaceId, EvidenceItem.DefaultSessionId, required, IsLiveAction: isLiveAction, IsFixtureSafe: isFixtureSafe, RequiresApproval: requiresApproval));

    private static string SurfaceText(EvidenceIntelligenceSurfaceViewModel surface)
    {
        var parts = new List<string>
        {
            surface.Header.Title,
            surface.Header.Subtitle,
            surface.Header.Summary,
            surface.IndexSummary.Summary,
            surface.SemanticNotice.Copy,
            surface.SearchPanel.FooterNote,
            surface.ClaimScanPanel.Rationale,
            surface.ClaimScanPanel.MarkdownPreview,
            surface.ActionScanPanel.Rationale,
            surface.ActionScanPanel.MarkdownPreview,
            surface.GraphSummary.Summary,
            surface.ReadinessMatrixPanel.SafeNextStep,
            surface.NoRuntimeNotice
        };
        parts.AddRange(surface.Header.Badges);
        parts.AddRange(surface.LocalOnlyNotices);
        parts.AddRange(surface.ReadOnlyActionLabels);
        parts.AddRange(surface.SearchPanel.Results.Select(result => $"{result.EvidenceId} {result.Snippet} {result.MatchReason} {result.RelationHint}"));
        parts.AddRange(surface.ReadinessMatrixPanel.Rows.Select(row => $"{row.Signal} {row.Source} {row.Status} {row.Decision} {row.EvidenceId} {row.Rationale}"));
        return string.Join(" ", parts);
    }

    private static void AssertSurfaceDoesNotContain(EvidenceIntelligenceSurfaceViewModel surface, string term)
    {
        Assert.IsFalse(SurfaceText(surface).Contains(term, StringComparison.OrdinalIgnoreCase), term);
    }

    private static string GitRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OneBrain.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.IsNotNull(directory);
        return directory.FullName;
    }
}
