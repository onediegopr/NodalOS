using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ProjectUnderstandingDryRunReview")]
[TestCategory("DryRunUiPreview")]
[TestCategory("ScanConsentReview")]
[TestCategory("DryRunEvidencePlan")]
[TestCategory("ProjectUnderstandingDryRunPolicy")]
[TestCategory("ProjectUnderstandingScanGate")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsProjectUnderstandingDryRunReviewM543M545Tests
{
    private static readonly string[] ForbiddenNames = ["Nexa", "NEXA", "NODRIX", "HOTEP"];
    private static readonly string[] SensitiveMarkers =
    [
        "Bear" + "er ",
        "Authorization:",
        "Cook" + "ie:",
        "password",
        "raw " + "secret",
        "api" + "_key",
        "access" + "_token",
        "refresh" + "_token",
        "private key",
        "s" + "k-",
        "connection string"
    ];

    private readonly NodalOsProjectUnderstandingDryRunUiPreviewService uiService = new();
    private readonly NodalOsProjectUnderstandingDryRunUiPreviewJsonSerializer uiSerializer = new();
    private readonly NodalOsScanConsentReviewCardsService cardService = new();
    private readonly NodalOsScanConsentReviewCardsJsonSerializer cardSerializer = new();
    private readonly NodalOsDryRunEvidencePlanService evidenceService = new();
    private readonly NodalOsDryRunEvidencePlanJsonSerializer evidenceSerializer = new();

    [TestMethod]
    public void DryRunUiPreview_IsStaticReadOnlyNoOpAndUsesNoOperationalSurface()
    {
        var preview = UiPreview();

        Assert.IsTrue(preview.IsStaticPreview);
        Assert.IsTrue(preview.IsReadOnly);
        Assert.IsTrue(preview.IsNoOp);
        Assert.IsFalse(preview.UsesRealFilesystem);
        Assert.IsFalse(preview.PerformsDirectoryListing);
        Assert.IsFalse(preview.PerformsFileRead);
        Assert.IsFalse(preview.PerformsFileHash);
        Assert.IsFalse(preview.PerformsIndexing);
        Assert.IsFalse(preview.PerformsVectorization);
        Assert.IsFalse(preview.BuildsLlmContext);
        Assert.IsFalse(preview.CallsProvider);
        Assert.IsFalse(preview.UsesCloud);
        AssertSafeOutput(uiSerializer.Serialize(preview));
    }

    [TestMethod]
    public void DryRunUiPreview_IncludesRequiredSectionsAndDisclosures()
    {
        var preview = UiPreview();
        var sectionIds = preview.Sections.Select(s => s.SectionId).ToHashSet();

        Assert.IsTrue(sectionIds.Contains("dry-run-summary"));
        Assert.IsTrue(sectionIds.Contains("scope-preview-summary"));
        Assert.IsTrue(sectionIds.Contains("consent-status"));
        Assert.IsTrue(sectionIds.Contains("secret-policy-summary"));
        Assert.IsTrue(sectionIds.Contains("exclusion-policy-summary"));
        Assert.IsTrue(sectionIds.Contains("blocked-capabilities"));
        Assert.IsTrue(sectionIds.Contains("missing-requirements"));
        Assert.IsTrue(sectionIds.Contains("evidence-plan-preview"));
        Assert.IsTrue(sectionIds.Contains("timeline-events-preview"));
        Assert.IsTrue(sectionIds.Contains("guardrails"));
        Assert.IsTrue(preview.RequiredDisclosuresRedacted.Count >= 7);
    }

    [TestMethod]
    public void ScanConsentReviewCards_AreNoOpAndCannotAuthorizeOperationalScan()
    {
        foreach (var card in Cards())
        {
            Assert.IsTrue(card.IsNoOp);
            Assert.IsFalse(card.CanAuthorizeRealScan);
            Assert.IsFalse(card.CanAuthorizeFileRead);
            Assert.IsFalse(card.CanAuthorizeIndexing);
            Assert.IsFalse(card.CanAuthorizeVectorization);
            Assert.IsFalse(card.CanAuthorizeLlmContext);
        }

        AssertSafeOutput(cardSerializer.SerializeCards(Cards()));
    }

    [TestMethod]
    public void ScanConsentReviewCards_IncludeBlockedStates()
    {
        var statuses = Cards().Select(c => c.ReviewStatus).ToHashSet();

        Assert.IsTrue(statuses.Contains(NodalOsScanConsentReviewStatus.Draft));
        Assert.IsTrue(statuses.Contains(NodalOsScanConsentReviewStatus.NeedsReview));
        Assert.IsTrue(statuses.Contains(NodalOsScanConsentReviewStatus.BlockedByMissingPathJail));
        Assert.IsTrue(statuses.Contains(NodalOsScanConsentReviewStatus.BlockedByMissingSecretDetection));
        Assert.IsTrue(statuses.Contains(NodalOsScanConsentReviewStatus.BlockedByMissingExclusionPolicy));
        Assert.IsTrue(statuses.Contains(NodalOsScanConsentReviewStatus.BlockedByRealScanAuditGate));
        Assert.IsTrue(statuses.Contains(NodalOsScanConsentReviewStatus.AcknowledgedDraftOnly));
        Assert.IsTrue(statuses.Contains(NodalOsScanConsentReviewStatus.RejectedDraftOnly));
    }

    [TestMethod]
    public void ScanConsentReviewResult_IsNoOpAndNonAuthorizingForEveryOption()
    {
        var card = Cards()[0];
        foreach (var option in Enum.GetValues<NodalOsScanConsentReviewOption>())
        {
            var result = cardService.ApplyOption(card, option);

            Assert.IsTrue(result.IsNoOp);
            Assert.IsFalse(result.MutatesState);
            Assert.IsFalse(result.AuthorizesRealScan);
            Assert.IsFalse(result.AuthorizesDirectoryListing);
            Assert.IsFalse(result.AuthorizesFileRead);
            Assert.IsFalse(result.AuthorizesFileHash);
            Assert.IsFalse(result.AuthorizesIndexing);
            Assert.IsFalse(result.AuthorizesVectorization);
            Assert.IsFalse(result.AuthorizesLlmContext);
            Assert.IsFalse(result.AuthorizesCloud);
            Assert.IsTrue(result.RequiresFutureExplicitGate);
            AssertSafeOutput(cardSerializer.SerializeResult(result));
        }
    }

    [TestMethod]
    public void DryRunEvidencePlan_IsPlanOnlyAndDoesNotEmitOrVerify()
    {
        var plan = EvidencePlan();

        Assert.IsTrue(plan.IsPlanOnly);
        Assert.IsFalse(plan.EmitsRealEvidence);
        Assert.IsFalse(plan.VerifiesRealContent);
        Assert.IsFalse(plan.UsesRealFilesystem);
        Assert.IsFalse(plan.BuildsLlmContext);
        Assert.IsTrue(plan.PlannedEvidenceItems.Count > 0);
        Assert.IsTrue(plan.PlannedTimelineEvents.Count > 0);
        AssertSafeOutput(evidenceSerializer.SerializePlan(plan));
    }

    [TestMethod]
    public void DryRunEvidenceItems_CannotContainSensitiveOrVerifiedContent()
    {
        foreach (var item in EvidencePlan().PlannedEvidenceItems)
        {
            Assert.IsFalse(item.CanContainRawContent);
            Assert.IsFalse(item.CanContainRawSecret);
            Assert.IsFalse(item.CanVerifyFilesystemContent);
        }
    }

    [TestMethod]
    public void DryRunTimelineEvents_ArePreviewOnlyAndNotEmitted()
    {
        foreach (var item in EvidencePlan().PlannedTimelineEvents)
        {
            Assert.IsTrue(item.IsPreviewOnly);
            Assert.IsFalse(item.Emitted);
        }
    }

    [TestMethod]
    public void DryRunEvidencePlanReadiness_RemainsBlocked()
    {
        var readiness = evidenceService.Evaluate(EvidencePlan());

        Assert.IsFalse(readiness.ReadyForRealDryRunEvidence);
        Assert.IsFalse(readiness.ReadyForRealScan);
        Assert.IsFalse(readiness.ReadyForRealEvidenceVerification);
        Assert.IsTrue(readiness.MissingRequirementsRedacted.Count > 0);
        Assert.IsTrue(readiness.BlockersRedacted.Count > 0);
        AssertSafeOutput(evidenceSerializer.SerializeReadiness(readiness));
    }

    [TestMethod]
    public void StaticPreviewArtifact_HasNoExternalScriptsOrNetworkResources()
    {
        var html = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m545", "project-understanding-dry-run-review-preview.html"));

        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "cdn");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Artifacts_MarkDryRunReviewReadyAndKeepOperationalReadinessFalse()
    {
        var ui = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m545", "project-understanding-dry-run-ui-preview.json"));
        var cards = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m545", "scan-consent-review-cards.json"));
        var evidence = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m545", "dry-run-evidence-plan.json"));

        AssertContains(ui, "\"decision\": \"PROJECT_UNDERSTANDING_DRY_RUN_REVIEW_READY\"");
        AssertContains(ui, "\"isStaticPreview\": true");
        AssertContains(ui, "\"usesRealFilesystem\": false");
        AssertContains(cards, "\"canAuthorizeRealScan\": false");
        AssertContains(evidence, "\"isPlanOnly\": true");
        AssertContains(evidence, "\"readyForRealScan\": false");
        AssertSafeOutput(ui + cards + evidence);
    }

    [TestMethod]
    public void Serializers_AreDeterministicAndSafe()
    {
        Assert.AreEqual(uiSerializer.Serialize(UiPreview()), uiSerializer.Serialize(UiPreview()));
        Assert.AreEqual(cardSerializer.SerializeCards(Cards()), cardSerializer.SerializeCards(Cards()));
        Assert.AreEqual(evidenceSerializer.SerializePlan(EvidencePlan()), evidenceSerializer.SerializePlan(EvidencePlan()));
    }

    [TestMethod]
    public void Boundary_NewDryRunReviewFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
    {
        var source = NewSource();

        AssertDoesNotContain(source, "OneBrain." + "BrowserExecutor" + ".Cdp");
        AssertDoesNotContain(source, "Http" + "Client");
        AssertDoesNotContain(source, "Client" + "WebSocket");
        AssertDoesNotContain(source, "Process" + ".Start");
        AssertDoesNotContain(source, "System.Diagnostics." + "Process");
        AssertDoesNotContain(source, "Background" + "Service");
        AssertDoesNotContain(source, "Task.Run");
        AssertDoesNotContain(source, "File" + ".Read");
        AssertDoesNotContain(source, "File" + ".Write");
        AssertDoesNotContain(source, "Directory" + ".");
        AssertDoesNotContain(source, "File" + "Info");
        AssertDoesNotContain(source, "Directory" + "Info");
    }

    private NodalOsProjectUnderstandingDryRunUiPreview UiPreview() =>
        uiService.CreatePreview(NodalOsScanDryRunFixtures.Request(), NodalOsScanDryRunFixtures.Result());

    private IReadOnlyList<NodalOsScanConsentReviewCard> Cards() =>
        cardService.CreateCards(NodalOsConsentScopePreviewFixtures.Consent(), NodalOsConsentScopePreviewFixtures.Scope());

    private NodalOsDryRunEvidencePlan EvidencePlan() =>
        evidenceService.CreatePlan(NodalOsScanDryRunFixtures.Request(), Cards());

    private static string NewSource()
    {
        var root = FindRepoRoot();
        var files = new[]
        {
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsProjectUnderstandingDryRunUiPreviewContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsScanConsentReviewCardsContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsDryRunEvidencePlanContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsProjectUnderstandingDryRunUiPreviewServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsScanConsentReviewCardsServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsDryRunEvidencePlanServices.cs")
        };

        return string.Join(Environment.NewLine, files.Select(TextStore.ReadAllText));
    }

    private static void AssertSafeOutput(string value)
    {
        foreach (var name in ForbiddenNames)
            AssertDoesNotContain(value, name);

        foreach (var marker in SensitiveMarkers)
            AssertDoesNotContain(value, marker);
    }

    private static void AssertContains(string value, string expected) =>
        StringAssert.Contains(value, expected);

    private static void AssertDoesNotContain(string value, string forbidden) =>
        Assert.IsFalse(value.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Unexpected content: {forbidden}");

    private static string PathFor(params string[] segments) =>
        Path.Combine([FindRepoRoot(), .. segments]);

    private static string FindRepoRoot()
    {
        var current = Path.GetFullPath(AppContext.BaseDirectory);
        while (!string.IsNullOrEmpty(current) && !TextStore.Exists(Path.Combine(current, "OneBrain.slnx")))
            current = Path.GetDirectoryName(current) ?? string.Empty;

        return string.IsNullOrEmpty(current) ? throw new InvalidOperationException("Repository root not found.") : current;
    }
}
