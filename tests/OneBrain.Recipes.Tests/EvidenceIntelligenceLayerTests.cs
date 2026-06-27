using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Evidence;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("EvidenceIntelligence")]
public sealed class EvidenceIntelligenceLayerTests
{
    [TestMethod]
    public void EvidenceItemContractsAreSerializableAndStable()
    {
        var item = Evidence("ev.dom.invoice", EvidenceSourceType.Dom, "Invoice total visible", confidence: 0.91);
        var json = JsonSerializer.Serialize(item);
        var roundTrip = JsonSerializer.Deserialize<EvidenceItem>(json);

        Assert.IsNotNull(roundTrip);
        Assert.AreEqual(item.Id, roundTrip.Id);
        Assert.AreEqual("invoice total visible", item.NormalizedText);
        Assert.AreEqual(item.Hash, Evidence("ev.dom.invoice.copy", EvidenceSourceType.Dom, "Invoice total visible", confidence: 0.91).Hash);
        Assert.AreEqual(64, item.Hash.Length);
    }

    [TestMethod]
    public void EvidenceItemRedactsSecretLikeTextAndMetadata()
    {
        var passwordKey = "password";
        var tokenKey = "token";
        var probeValue = "probe-value";
        var item = EvidenceItem.Create(
            "ev.secret",
            EvidenceSourceType.AgentObservation,
            "fixture",
            $"operator pasted {passwordKey}={probeValue}",
            sensitivityLevel: EvidenceSensitivityLevel.Secret,
            metadata: new Dictionary<string, string> { [tokenKey] = $"{tokenKey}={probeValue}" });

        Assert.IsFalse(item.Text.Contains(probeValue, StringComparison.Ordinal));
        Assert.IsFalse(item.Metadata[tokenKey].Contains(probeValue, StringComparison.Ordinal));
        Assert.AreEqual(EvidenceRedactionStatus.Applied, item.RedactionStatus);
    }

    [TestMethod]
    public void IndexIngestsGetsAndDeduplicatesByStableHash()
    {
        var index = new InMemoryEvidenceIntelligenceIndex();
        var first = index.Ingest(Evidence("ev.1", EvidenceSourceType.Dom, "Download invoice button"));
        var duplicate = index.Ingest(Evidence("ev.2", EvidenceSourceType.Dom, "Download invoice button"));

        Assert.AreEqual("ev.1", duplicate.Id);
        Assert.AreEqual(first, index.GetById("ev.1"));
        Assert.AreEqual(1, index.All().Count);
    }

    [TestMethod]
    public void IndexFiltersBySourceWorkspaceSessionAndStaleness()
    {
        var index = new InMemoryEvidenceIntelligenceIndex();
        index.Ingest(Evidence("ev.dom", EvidenceSourceType.Dom, "invoice visible", workspace: "w1", session: "s1"));
        index.Ingest(Evidence("ev.uia", EvidenceSourceType.Uia, "invoice visible", workspace: "w2", session: "s1"));
        index.Ingest(Evidence("ev.stale", EvidenceSourceType.Dom, "invoice visible", workspace: "w1", session: "s1", stale: EvidenceStalenessStatus.Stale));

        var query = new EvidenceQuery("invoice", EvidenceQueryPurpose.Audit, [EvidenceSourceType.Dom], "w1", "s1", 10, true, false);
        var results = index.QueryItems(query);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("ev.dom", results[0].Id);
    }

    [TestMethod]
    public void RetrievalUsesRealLexicalSearchAndNoFakeSemanticBackend()
    {
        var index = SeedInvoiceIndex();
        var router = new EvidenceIntelligenceRetrievalRouter(index);

        var hit = router.Search(Query("invoice total", includeContradictions: false));
        var miss = router.Search(Query("unrelated banana", includeContradictions: false));

        Assert.AreEqual(EvidenceSemanticBackendStatus.Disabled, router.SemanticBackendStatus);
        Assert.IsTrue(hit.Count > 0);
        Assert.AreEqual(0, miss.Count);
        StringAssert.Contains(hit[0].MatchReason, "semantic backend: disabled");
    }

    [TestMethod]
    public void RetrievalRanksByRelevanceSourceWeightConfidenceAndDedup()
    {
        var index = new InMemoryEvidenceIntelligenceIndex();
        index.Ingest(Evidence("ev.ocr", EvidenceSourceType.Ocr, "invoice total", confidence: 0.9));
        index.Ingest(Evidence("ev.validation", EvidenceSourceType.ValidationReport, "invoice total", confidence: 0.9));
        index.Ingest(Evidence("ev.validation.dup", EvidenceSourceType.ValidationReport, "invoice total", confidence: 0.9));

        var results = new EvidenceIntelligenceRetrievalRouter(index).Search(Query("invoice total", includeContradictions: false));

        Assert.AreEqual("ev.validation", results[0].EvidenceId);
        Assert.AreEqual(2, results.Count);
    }

    [TestMethod]
    public void RetrievalPrioritizesContradictionsWhenRequested()
    {
        var index = new InMemoryEvidenceIntelligenceIndex();
        index.Ingest(Evidence("ev.support", EvidenceSourceType.Ocr, "button says pay invoice", metadata: new Dictionary<string, string> { ["relation"] = "supports" }));
        index.Ingest(Evidence("ev.contradict", EvidenceSourceType.Uia, "button says delete account", metadata: new Dictionary<string, string> { ["relation"] = "contradicts_observation", ["target"] = "delete account" }));

        var results = new EvidenceIntelligenceRetrievalRouter(index).Search(Query("button invoice", includeContradictions: true));

        Assert.AreEqual("ev.contradict", results[0].EvidenceId);
        Assert.AreEqual("contradicts_observation", results[0].RelationHint);
    }

    [TestMethod]
    public void ClaimScanSupportsClaimWithEvidenceIdsInReports()
    {
        var index = SeedInvoiceIndex();
        var result = new ClaimEvidenceScanner(index).Scan(new ClaimEvidenceScanRequest(
            "invoice total visible",
            "workspace.fixture",
            "session.fixture",
            [EvidenceSourceType.Dom]));

        Assert.AreEqual(ClaimEvidenceVerdict.SUPPORTED, result.Verdict);
        StringAssert.Contains(result.MarkdownReport, "ev.dom.invoice");
        StringAssert.Contains(result.JsonReport, "\"verdict\":\"SUPPORTED\"");
    }

    [TestMethod]
    public void ClaimScanContradictionWinsOverSupport()
    {
        var index = new InMemoryEvidenceIntelligenceIndex();
        index.Ingest(Evidence("ev.ocr.support", EvidenceSourceType.Ocr, "status says invoice paid", metadata: new Dictionary<string, string> { ["relation"] = "supports" }));
        index.Ingest(Evidence("ev.uia.contradict", EvidenceSourceType.Uia, "status says invoice unpaid", metadata: new Dictionary<string, string> { ["relation"] = "contradicts_claim" }));

        var result = new ClaimEvidenceScanner(index).Scan(new ClaimEvidenceScanRequest(
            "invoice paid",
            "workspace.fixture",
            "session.fixture",
            [EvidenceSourceType.Ocr, EvidenceSourceType.Uia]));

        Assert.AreEqual(ClaimEvidenceVerdict.CONTRADICTED, result.Verdict);
        Assert.IsTrue(result.ContradictingEvidence.Any(e => e.EvidenceId == "ev.uia.contradict"));
        Assert.IsTrue(result.BlockingReasons.Any(r => r.Contains("Contradictory", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ClaimScanFailsForMissingStaleLowConfidenceAndRequiredSources()
    {
        var missing = new ClaimEvidenceScanner(new InMemoryEvidenceIntelligenceIndex()).Scan(new ClaimEvidenceScanRequest(
            "invoice present",
            "workspace.fixture",
            "session.fixture",
            [EvidenceSourceType.Dom]));
        Assert.AreEqual(ClaimEvidenceVerdict.INSUFFICIENT_EVIDENCE, missing.Verdict);

        var staleIndex = new InMemoryEvidenceIntelligenceIndex();
        staleIndex.Ingest(Evidence("ev.stale", EvidenceSourceType.Dom, "invoice present", stale: EvidenceStalenessStatus.Stale));
        var stale = new ClaimEvidenceScanner(staleIndex).Scan(new ClaimEvidenceScanRequest("invoice present", "workspace.fixture", "session.fixture", [EvidenceSourceType.Dom]));
        Assert.AreEqual(ClaimEvidenceVerdict.STALE_EVIDENCE, stale.Verdict);

        var lowIndex = new InMemoryEvidenceIntelligenceIndex();
        lowIndex.Ingest(Evidence("ev.low", EvidenceSourceType.Dom, "invoice present", confidence: 0.2));
        var low = new ClaimEvidenceScanner(lowIndex).Scan(new ClaimEvidenceScanRequest("invoice present", "workspace.fixture", "session.fixture", [EvidenceSourceType.Dom]));
        Assert.AreEqual(ClaimEvidenceVerdict.LOW_CONFIDENCE, low.Verdict);

        var missingSource = new ClaimEvidenceScanner(SeedInvoiceIndex()).Scan(new ClaimEvidenceScanRequest("invoice total visible", "workspace.fixture", "session.fixture", [EvidenceSourceType.Win32]));
        CollectionAssert.Contains(missingSource.MissingRequiredSourceTypes.ToList(), EvidenceSourceType.Win32);
    }

    [TestMethod]
    public void ActionScanAllowsReadOnlyAndFixtureSafeOnlyWithEvidence()
    {
        var index = SeedInvoiceIndex();
        var scanner = new ActionEvidenceScanner(index);

        var read = scanner.Scan(new ActionEvidenceScanRequest("action.read", EvidenceActionType.Read, "invoice total", "workspace.fixture", "session.fixture", [EvidenceSourceType.Dom]));
        var fixture = scanner.Scan(new ActionEvidenceScanRequest("action.fixture", EvidenceActionType.Click, "download invoice", "workspace.fixture", "session.fixture", [EvidenceSourceType.Dom], IsFixtureSafe: true));

        Assert.AreEqual(ActionEvidenceVerdict.ALLOW_READ_ONLY, read.Verdict);
        Assert.AreEqual(ActionEvidenceVerdict.ALLOW_FIXTURE_SAFE, fixture.Verdict);
        StringAssert.Contains(fixture.ReadinessMatrix.SafeNextStep, "runtime remains disabled");
    }

    [TestMethod]
    public void ActionScanFailsClosedForMissingStalePolicyLiveAndApproval()
    {
        var emptyResult = new ActionEvidenceScanner(new InMemoryEvidenceIntelligenceIndex()).Scan(new ActionEvidenceScanRequest(
            "action.missing",
            EvidenceActionType.Click,
            "submit",
            "workspace.fixture",
            "session.fixture",
            [EvidenceSourceType.Dom]));
        Assert.AreEqual(ActionEvidenceVerdict.BLOCKED_BY_MISSING_EVIDENCE, emptyResult.Verdict);

        var staleIndex = new InMemoryEvidenceIntelligenceIndex();
        staleIndex.Ingest(Evidence("ev.stale", EvidenceSourceType.Dom, "submit invoice", stale: EvidenceStalenessStatus.Stale));
        var stale = new ActionEvidenceScanner(staleIndex).Scan(new ActionEvidenceScanRequest("action.stale", EvidenceActionType.Submit, "submit invoice", "workspace.fixture", "session.fixture", [EvidenceSourceType.Dom], IsFixtureSafe: true));
        Assert.AreEqual(ActionEvidenceVerdict.BLOCKED_BY_STALE_EVIDENCE, stale.Verdict);

        var policyIndex = new InMemoryEvidenceIntelligenceIndex();
        policyIndex.Ingest(Evidence("ev.policy", EvidenceSourceType.Policy, "policy blocks submit", policy: EvidencePolicyScope.BlocksAction, metadata: new Dictionary<string, string> { ["target"] = "submit" }));
        var policy = new ActionEvidenceScanner(policyIndex).Scan(new ActionEvidenceScanRequest("action.policy", EvidenceActionType.Submit, "submit", "workspace.fixture", "session.fixture", [EvidenceSourceType.Policy]));
        Assert.AreEqual(ActionEvidenceVerdict.BLOCKED_BY_POLICY, policy.Verdict);

        var live = new ActionEvidenceScanner(SeedInvoiceIndex()).Scan(new ActionEvidenceScanRequest("action.live", EvidenceActionType.Click, "download invoice", "workspace.fixture", "session.fixture", [EvidenceSourceType.Dom], IsLiveAction: true));
        Assert.AreEqual(ActionEvidenceVerdict.BLOCKED_UNSAFE_LIVE_ACTION, live.Verdict);

        var approval = new ActionEvidenceScanner(SeedInvoiceIndex()).Scan(new ActionEvidenceScanRequest("action.approval", EvidenceActionType.Submit, "invoice total", "workspace.fixture", "session.fixture", [EvidenceSourceType.Dom], RequiresApproval: true));
        Assert.AreEqual(ActionEvidenceVerdict.REQUIRES_APPROVAL, approval.Verdict);
    }

    [TestMethod]
    public void ActionScanBlocksOcrUiaContradictionAndShowsBothSignals()
    {
        var index = new InMemoryEvidenceIntelligenceIndex();
        index.Ingest(Evidence("ev.ocr.pay", EvidenceSourceType.Ocr, "button text Pagar", metadata: new Dictionary<string, string> { ["target"] = "Pagar" }));
        index.Ingest(Evidence("ev.uia.delete", EvidenceSourceType.Uia, "button automation name Eliminar cuenta", metadata: new Dictionary<string, string> { ["target"] = "Eliminar cuenta", ["relation"] = "contradicts_observation" }));

        var result = new ActionEvidenceScanner(index).Scan(new ActionEvidenceScanRequest(
            "action.pay",
            EvidenceActionType.Click,
            "Pagar",
            "workspace.fixture",
            "session.fixture",
            [EvidenceSourceType.Ocr, EvidenceSourceType.Uia],
            IsFixtureSafe: true));

        Assert.AreEqual(ActionEvidenceVerdict.BLOCKED_BY_CONTRADICTION, result.Verdict);
        Assert.IsTrue(result.ReadinessMatrix.Rows.Any(r => r.EvidenceId == "ev.ocr.pay"));
        Assert.IsTrue(result.ReadinessMatrix.Rows.Any(r => r.EvidenceId == "ev.uia.delete"));
    }

    [TestMethod]
    public void GraphAndReadinessMatrixRepresentSupportContradictionPolicyMissingAndStableReports()
    {
        var index = new InMemoryEvidenceIntelligenceIndex();
        index.Ingest(Evidence("ev.dom", EvidenceSourceType.Dom, "submit invoice", metadata: new Dictionary<string, string> { ["target"] = "submit invoice" }));
        index.Ingest(Evidence("ev.policy", EvidenceSourceType.Policy, "policy blocks submit invoice", policy: EvidencePolicyScope.BlocksAction, metadata: new Dictionary<string, string> { ["target"] = "submit invoice" }));

        var result = new ActionEvidenceScanner(index).Scan(new ActionEvidenceScanRequest(
            "action.submit",
            EvidenceActionType.Submit,
            "submit invoice",
            "workspace.fixture",
            "session.fixture",
            [EvidenceSourceType.Dom, EvidenceSourceType.Policy, EvidenceSourceType.Approval]));
        var repeated = new ActionEvidenceScanner(index).Scan(new ActionEvidenceScanRequest(
            "action.submit",
            EvidenceActionType.Submit,
            "submit invoice",
            "workspace.fixture",
            "session.fixture",
            [EvidenceSourceType.Dom, EvidenceSourceType.Policy, EvidenceSourceType.Approval]));

        Assert.AreEqual(ActionEvidenceVerdict.BLOCKED_BY_POLICY, result.Verdict);
        Assert.IsTrue(result.Graph.Edges.Any(e => e.RelationType == EvidenceRelationType.policy_blocks));
        Assert.IsTrue(result.ReadinessMatrix.Rows.Any(r => r.Status == EvidenceReadinessRowStatus.PolicyBlocks));
        Assert.IsTrue(result.ReadinessMatrix.Rows.Any(r => r.Status == EvidenceReadinessRowStatus.Missing && r.Source == "Approval"));
        StringAssert.Contains(result.MarkdownReport, "| Signal | Source | Status | Decision | Evidence | Rationale |");
        Assert.AreEqual(result.JsonReport, repeated.JsonReport);
    }

    [TestMethod]
    public void EvidenceLayerSourceDoesNotContainRuntimePrimitivesOrFakeSemanticSuccess()
    {
        var root = GitRoot();
        var source = File.ReadAllText(Path.Combine(root, "src", "OneBrain.Core", "Evidence", "EvidenceIntelligenceLayer.cs"));
        var forbidden = new[]
        {
            "Process.Start",
            "HttpClient",
            "WebSocket",
            "Playwright",
            "Puppeteer",
            "Selenium",
            "ChromeCdpBrowserLauncher",
            "UIA3Automation",
            "CaptureScreen",
            "ScreenshotCapture",
            "Docker",
            "Kubernetes",
            "OpenAI",
            "SemanticBackendStatus.Available"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(source.Contains(term, StringComparison.Ordinal), term);
        }

        StringAssert.Contains(source, "EvidenceSemanticBackendStatus.Disabled");
    }

    private static InMemoryEvidenceIntelligenceIndex SeedInvoiceIndex()
    {
        var index = new InMemoryEvidenceIntelligenceIndex();
        index.Ingest(Evidence("ev.dom.invoice", EvidenceSourceType.Dom, "invoice total visible", metadata: new Dictionary<string, string> { ["target"] = "invoice total" }));
        index.Ingest(Evidence("ev.uia.invoice", EvidenceSourceType.Uia, "invoice total visible", metadata: new Dictionary<string, string> { ["target"] = "invoice total" }));
        index.Ingest(Evidence("ev.download.invoice", EvidenceSourceType.ValidationReport, "download invoice validation passed", metadata: new Dictionary<string, string> { ["target"] = "download invoice" }));
        return index;
    }

    private static EvidenceQuery Query(string query, bool includeContradictions)
    {
        return new EvidenceQuery(query, EvidenceQueryPurpose.HumanSearch, [], "workspace.fixture", "session.fixture", 10, includeContradictions, false);
    }

    private static EvidenceItem Evidence(
        string id,
        EvidenceSourceType source,
        string text,
        string workspace = "workspace.fixture",
        string session = "session.fixture",
        double confidence = 1,
        EvidenceStalenessStatus stale = EvidenceStalenessStatus.Fresh,
        EvidencePolicyScope policy = EvidencePolicyScope.ReadOnly,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return EvidenceItem.Create(
            id,
            source,
            "fixture",
            text,
            DateTimeOffset.UnixEpoch,
            workspace,
            session,
            confidence,
            EvidenceSensitivityLevel.Internal,
            EvidenceRedactionStatus.None,
            stale,
            policy,
            metadata);
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
