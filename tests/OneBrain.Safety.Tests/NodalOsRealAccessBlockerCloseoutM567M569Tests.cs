using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ConsentLedgerUiPreview")]
[TestCategory("CapabilityAuditChecklist")]
[TestCategory("RealAccessBlockerCloseout")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsRealAccessBlockerCloseoutM567M569Tests
{
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
        "s" + "k-"
    ];

    private readonly NodalOsPerCapabilityAccessGateService gateService = new();
    private readonly NodalOsConsentScopeLedgerMockService ledgerMockService = new();
    private readonly NodalOsConsentLedgerUiPreviewService ledgerPreviewService = new();
    private readonly NodalOsConsentLedgerUiPreviewJsonSerializer ledgerPreviewSerializer = new();
    private readonly NodalOsCapabilityAuditChecklistService checklistService = new();
    private readonly NodalOsCapabilityAuditChecklistJsonSerializer checklistSerializer = new();
    private readonly NodalOsRealAccessBlockerCloseoutService closeoutService = new();
    private readonly NodalOsRealAccessBlockerCloseoutJsonSerializer closeoutSerializer = new();

    [TestMethod]
    public void ConsentLedgerUiPreview_IsStaticReadOnlyNoOpAndBlocked()
    {
        var preview = LedgerPreview();

        Assert.IsTrue(preview.IsStaticPreview);
        Assert.IsTrue(preview.IsReadOnly);
        Assert.IsTrue(preview.IsNoOp);
        Assert.IsFalse(preview.UsesProductivePersistence);
        Assert.IsFalse(preview.UsesRealFilesystem);
        Assert.IsFalse(preview.CanPersistConsent);
        Assert.IsFalse(preview.CanAuthorizeCapability);
        Assert.IsFalse(preview.CanAuthorizeFilesystemAccess);
        Assert.IsFalse(preview.CanAuthorizeLlmContext);
        Assert.IsFalse(preview.CanSendToCloud);
        AssertSafeOutput(ledgerPreviewSerializer.Serialize(preview));
    }

    [TestMethod]
    public void LedgerEntryCards_AreMockOnlyAndNonAuthoritative()
    {
        foreach (var card in LedgerPreview().EntryCards)
        {
            Assert.IsTrue(card.IsMockOnly);
            Assert.IsFalse(card.IsAuthoritative);
            Assert.IsFalse(card.CanAuthorizeRealUse);
        }
    }

    [TestMethod]
    public void LedgerUiReviewOptions_AreNoOpAndNonAuthorizing()
    {
        foreach (var option in Enum.GetValues<NodalOsConsentLedgerUiReviewOption>())
        {
            var result = ledgerPreviewService.ApplyOption(option);

            Assert.IsTrue(result.IsNoOp);
            Assert.IsFalse(result.MutatesState);
            Assert.IsFalse(result.PersistsConsent);
            Assert.IsFalse(result.AuthorizesCapability);
            Assert.IsFalse(result.AuthorizesFilesystemAccess);
            Assert.IsFalse(result.AuthorizesLlmContext);
            Assert.IsFalse(result.SendsToCloud);
            AssertSafeOutput(ledgerPreviewSerializer.SerializeOption(result));
        }
    }

    [TestMethod]
    public void CapabilityAuditChecklist_IsChecklistOnlyAndCannotAuthorize()
    {
        var checklist = Checklist();

        Assert.AreEqual(NodalOsCapabilityAuditChecklistStatus.ContractChecklistComplete, checklist.ChecklistStatus);
        Assert.IsTrue(checklist.IsChecklistOnly);
        Assert.IsFalse(checklist.CanAuthorizeCapability);
        Assert.IsFalse(checklist.CanEnableGate);
        Assert.IsFalse(checklist.CanAccessFilesystem);
        Assert.IsFalse(checklist.CanBuildLlmContext);
        Assert.IsFalse(checklist.CanUseCloud);
        AssertSafeOutput(checklistSerializer.Serialize(checklist));
    }

    [TestMethod]
    public void CapabilityAuditChecklist_IncludesAllRequiredCategories()
    {
        var categories = Checklist().Items.Select(item => item.RequirementCategory).ToHashSet();

        foreach (var category in Enum.GetValues<NodalOsCapabilityAuditRequirementCategory>())
            Assert.IsTrue(categories.Contains(category), $"Missing checklist category: {category}");
    }

    [TestMethod]
    public void ChecklistItems_BlockRealUseIfMissing()
    {
        foreach (var item in Checklist().Items)
        {
            Assert.IsTrue(item.RequiredBeforeEnablement);
            Assert.IsTrue(item.BlocksRealUseIfMissing);
            Assert.AreEqual(NodalOsCapabilityAuditChecklistItemStatus.ContractDocumented, item.Status);
        }
    }

    [TestMethod]
    public void ChecklistDecision_ClosesChecklistOnlyAndBlocksRealUse()
    {
        var decision = Checklist().Decision;

        Assert.IsTrue(decision.ReadyForChecklistCloseout);
        Assert.IsFalse(decision.ReadyForRealCapabilityEnablement);
        Assert.IsFalse(decision.ReadyForFilesystemAccess);
        Assert.IsFalse(decision.ReadyForRealScan);
        Assert.IsFalse(decision.ReadyForIndexing);
        Assert.IsFalse(decision.ReadyForRepresentationBuild);
        Assert.IsFalse(decision.ReadyForLlmContext);
        Assert.IsFalse(decision.ReadyForCloud);
        Assert.IsFalse(decision.ReadyForRuntime);
    }

    [TestMethod]
    public void RealAccessBlockerCloseout_ClosesGovernanceBaselineOnly()
    {
        var closeout = Closeout();

        Assert.AreEqual(NodalOsRealAccessBlockerStatus.GovernanceBaselineClosed, closeout.BlockerStatus);
        Assert.IsTrue(closeout.ClosedAsGovernanceBaseline);
        Assert.IsTrue(closeout.RealAccessStillBlocked);
        AssertSafeOutput(closeoutSerializer.Serialize(closeout));
    }

    [TestMethod]
    public void RealAccessBlockerCloseout_IncludesAllRequiredBlockers()
    {
        var categories = Closeout().Blockers.Select(blocker => blocker.Category).ToHashSet();

        foreach (var category in Enum.GetValues<NodalOsRealAccessBlockerCategory>())
            Assert.IsTrue(categories.Contains(category), $"Missing blocker category: {category}");

        Assert.IsTrue(Closeout().Blockers.All(blocker => blocker.BlocksRealAccess));
    }

    [TestMethod]
    public void CloseoutDecision_KeepsAllOperationalReadinessFalse()
    {
        var decision = Closeout().Decision;

        Assert.IsTrue(decision.GovernanceBaselineReady);
        Assert.IsTrue(decision.RealAccessStillBlocked);
        Assert.IsFalse(decision.ReadyForRealFilesystemAccess);
        Assert.IsFalse(decision.ReadyForRealScan);
        Assert.IsFalse(decision.ReadyForRealPathJail);
        Assert.IsFalse(decision.ReadyForDirectoryListing);
        Assert.IsFalse(decision.ReadyForFileRead);
        Assert.IsFalse(decision.ReadyForFileHash);
        Assert.IsFalse(decision.ReadyForIndexing);
        Assert.IsFalse(decision.ReadyForRepresentationBuild);
        Assert.IsFalse(decision.ReadyForLlmContext);
        Assert.IsFalse(decision.ReadyForCloud);
        Assert.IsFalse(decision.ReadyForRuntime);
    }

    [TestMethod]
    public void RecommendedNextPhase_DoesNotRecommendDirectOperationalImplementation()
    {
        var next = Closeout().Decision.RecommendedNextPhaseRedacted;

        AssertContains(next, "Audit checkpoint");
        AssertDoesNotContain(next, "direct");
        AssertDoesNotContain(next, "immediate");
    }

    [TestMethod]
    public void StaticPreviewArtifact_HasNoExternalScriptsOrNetworkResources()
    {
        var html = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m569", "real-access-blocker-closeout-preview.html"));

        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "cdn");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Artifacts_DeclareLedgerChecklistAndCloseoutBlocked()
    {
        var ledger = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m569", "consent-ledger-ui-preview.json"));
        var checklist = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m569", "capability-audit-checklist.json"));
        var closeout = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m569", "real-access-blocker-closeout.json"));

        AssertContains(ledger, "\"isStaticPreview\": true");
        AssertContains(ledger, "\"canPersistConsent\": false");
        AssertContains(checklist, "\"isChecklistOnly\": true");
        AssertContains(checklist, "\"readyForChecklistCloseout\": true");
        AssertContains(closeout, "\"closedAsGovernanceBaseline\": true");
        AssertContains(closeout, "\"realAccessStillBlocked\": true");
        AssertSafeOutput(ledger + checklist + closeout);
    }

    [TestMethod]
    public void Boundary_NewRealAccessBlockerFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
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
        AssertDoesNotContain(source, "File" + ".Delete");
        AssertDoesNotContain(source, "File" + ".Move");
        AssertDoesNotContain(source, "Directory" + ".");
        AssertDoesNotContain(source, "File" + "Info");
        AssertDoesNotContain(source, "Directory" + "Info");
    }

    private NodalOsConsentLedgerUiPreview LedgerPreview() => ledgerPreviewService.CreatePreview(Ledger());
    private NodalOsCapabilityAuditChecklist Checklist() => checklistService.CreateChecklist(Gates());
    private NodalOsRealAccessBlockerCloseout Closeout() => closeoutService.CreateCloseout(LedgerPreview(), Checklist());
    private NodalOsConsentScopeLedgerMock Ledger() => ledgerMockService.CreateLedger(Gates());
    private IReadOnlyList<NodalOsCapabilityAccessGate> Gates() => gateService.CreateGates();

    private static void AssertContains(string value, string expected) => StringAssert.Contains(value, expected);

    private static void AssertSafeOutput(string value)
    {
        foreach (var marker in SensitiveMarkers)
            AssertDoesNotContain(value, marker);

        AssertDoesNotContain(value, "NEXA");
        AssertDoesNotContain(value, "NODRIX");
        AssertDoesNotContain(value, "HOTEP");
    }

    private static void AssertDoesNotContain(string value, string forbidden) =>
        Assert.IsFalse(value.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Unexpected content: {forbidden}");

    private static string NewSource()
    {
        var root = FindRepoRoot();
        var files = new[]
        {
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsConsentLedgerUiPreviewContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsCapabilityAuditChecklistContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsRealAccessBlockerCloseoutContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsConsentLedgerUiPreviewServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsCapabilityAuditChecklistServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsRealAccessBlockerCloseoutServices.cs")
        };

        return string.Join(Environment.NewLine, files.Select(TextStore.ReadAllText));
    }

    private static string PathFor(params string[] segments) => Path.Combine([FindRepoRoot(), .. segments]);

    private static string FindRepoRoot()
    {
        var current = Path.GetFullPath(AppContext.BaseDirectory);
        while (!string.IsNullOrEmpty(current) && !TextStore.Exists(Path.Combine(current, "OneBrain.slnx")))
            current = Path.GetDirectoryName(current) ?? string.Empty;

        return string.IsNullOrEmpty(current) ? throw new InvalidOperationException("Repository root not found.") : current;
    }
}
