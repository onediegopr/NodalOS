using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("CapabilityGateUiReview")]
[TestCategory("ConsentScopeLedger")]
[TestCategory("FailClosedAcceptance")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsCapabilityGateReviewM564M566Tests
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
    private readonly NodalOsSyntheticFailureModesService failureService = new();
    private readonly NodalOsCapabilityGateUiReviewService reviewService = new();
    private readonly NodalOsCapabilityGateUiReviewJsonSerializer reviewSerializer = new();
    private readonly NodalOsConsentScopeLedgerMockService ledgerService = new();
    private readonly NodalOsConsentScopeLedgerMockJsonSerializer ledgerSerializer = new();
    private readonly NodalOsFailClosedAcceptancePackService acceptanceService = new();
    private readonly NodalOsFailClosedAcceptancePackJsonSerializer acceptanceSerializer = new();

    [TestMethod]
    public void CapabilityGateUiReview_IsStaticReadOnlyNoOpAndBlocked()
    {
        var review = Review();

        Assert.IsTrue(review.IsStaticPreview);
        Assert.IsTrue(review.IsReadOnly);
        Assert.IsTrue(review.IsNoOp);
        Assert.IsFalse(review.UsesRealFilesystem);
        Assert.IsFalse(review.CanEnableGate);
        Assert.IsFalse(review.CanAuthorizeCapability);
        Assert.IsFalse(review.CanPersistConsent);
        Assert.IsFalse(review.CanAccessFilesystem);
        Assert.IsFalse(review.CanReadContent);
        Assert.IsFalse(review.CanFingerprintContent);
        Assert.IsFalse(review.CanBuildRepresentation);
        Assert.IsFalse(review.CanSendToLlm);
        Assert.IsFalse(review.CanSendToCloud);
        AssertSafeOutput(reviewSerializer.Serialize(review));
    }

    [TestMethod]
    public void ReviewCards_ShowDisabledByDefaultAndFailClosed()
    {
        foreach (var card in Review().ReviewCards)
        {
            Assert.IsFalse(card.GateEnabled);
            Assert.IsTrue(card.DisabledByDefault);
            Assert.IsTrue(card.RequiredConsent);
            Assert.IsTrue(card.RequiredAudit);
            Assert.IsTrue(card.FailClosed);
        }
    }

    [TestMethod]
    public void ReviewOptions_AreNoOpAndNonAuthorizing()
    {
        foreach (var option in Enum.GetValues<NodalOsCapabilityGateReviewOption>())
        {
            var result = reviewService.ApplyOption(option);

            Assert.IsTrue(result.IsNoOp);
            Assert.IsFalse(result.MutatesState);
            Assert.IsFalse(result.CanEnableGate);
            Assert.IsFalse(result.AuthorizesCapability);
            Assert.IsFalse(result.PersistsConsent);
            AssertSafeOutput(reviewSerializer.SerializeOption(result));
        }
    }

    [TestMethod]
    public void ConsentScopeLedger_IsMockOnlyAndCannotAuthorize()
    {
        var ledger = Ledger();

        Assert.IsTrue(ledger.IsMockOnly);
        Assert.IsFalse(ledger.UsesProductivePersistence);
        Assert.IsFalse(ledger.UsesRealFilesystem);
        Assert.IsFalse(ledger.CanPersistConsentProductively);
        Assert.IsFalse(ledger.CanAuthorizeCapability);
        Assert.IsFalse(ledger.CanAuthorizeFilesystemAccess);
        Assert.IsFalse(ledger.CanAuthorizeLlmContext);
        Assert.IsFalse(ledger.CanSendToCloud);
        AssertSafeOutput(ledgerSerializer.Serialize(ledger));
    }

    [TestMethod]
    public void ConsentScopeEntries_AreMockOnlyNonAuthoritativeAndNonAuthorizing()
    {
        foreach (var entry in Ledger().Entries)
        {
            Assert.IsTrue(entry.IsMockOnly);
            Assert.IsFalse(entry.IsAuthoritative);
            Assert.IsFalse(entry.CanAuthorizeRealUse);
        }
    }

    [TestMethod]
    public void LedgerOperations_NeverBecomeProductiveAuthorization()
    {
        foreach (var operation in Enum.GetValues<NodalOsConsentScopeLedgerOperationKind>())
        {
            var result = ledgerService.ApplyOperation(operation);

            Assert.IsTrue(result.IsMockOnly);
            Assert.IsFalse(result.UsesProductivePersistence);
            Assert.IsFalse(result.AuthorizesCapability);
            Assert.IsFalse(result.AuthorizesFilesystemAccess);
            Assert.IsFalse(result.AuthorizesLlmContext);
            AssertSafeOutput(ledgerSerializer.SerializeOperation(result));
        }
    }

    [TestMethod]
    public void LedgerResult_BlocksProductiveLedgerAndOperationalUse()
    {
        var result = Ledger().Result;

        Assert.IsTrue(result.ReadyForMockReview);
        Assert.IsFalse(result.ReadyForProductiveConsentLedger);
        Assert.IsFalse(result.ReadyForRealCapabilityAuthorization);
        Assert.IsFalse(result.ReadyForFilesystemAccess);
        Assert.IsFalse(result.ReadyForLlmContext);
    }

    [TestMethod]
    public void FailClosedAcceptancePack_CoversRequiredCriteria()
    {
        var pack = AcceptancePack();
        var kinds = pack.AcceptanceCriteria.Select(item => item.Kind).ToHashSet();

        Assert.AreEqual(NodalOsFailClosedAcceptanceStatus.ContractOnlyPass, pack.AcceptanceStatus);
        AssertContains(kinds, NodalOsFailClosedCriterionKind.GatesDisabledByDefault);
        AssertContains(kinds, NodalOsFailClosedCriterionKind.MissingConsentFailsClosed);
        AssertContains(kinds, NodalOsFailClosedCriterionKind.MissingAuditFailsClosed);
        AssertContains(kinds, NodalOsFailClosedCriterionKind.MissingDependencyFailsClosed);
        AssertContains(kinds, NodalOsFailClosedCriterionKind.MissingRedactionPolicyFailsClosed);
        AssertContains(kinds, NodalOsFailClosedCriterionKind.MissingSecretPolicyFailsClosed);
        AssertContains(kinds, NodalOsFailClosedCriterionKind.MissingExclusionPolicyFailsClosed);
        AssertSafeOutput(acceptanceSerializer.Serialize(pack));
    }

    [TestMethod]
    public void FailClosedDecision_IsReadyOnlyForContractLayer()
    {
        var decision = AcceptancePack().Decision;

        Assert.IsTrue(decision.FailClosedLayerReady);
        Assert.IsFalse(decision.ReadyForRealCapabilityEnablement);
        Assert.IsFalse(decision.ReadyForFilesystemAccess);
        Assert.IsFalse(decision.ReadyForRealScan);
        Assert.IsFalse(decision.ReadyForRealPathJail);
        Assert.IsFalse(decision.ReadyForIndexing);
        Assert.IsFalse(decision.ReadyForRepresentationBuild);
        Assert.IsFalse(decision.ReadyForLlmContext);
        Assert.IsFalse(decision.ReadyForCloud);
        Assert.IsFalse(decision.ReadyForRuntime);
    }

    [TestMethod]
    public void StaticPreviewArtifact_HasNoExternalScriptsOrNetworkResources()
    {
        var html = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m566", "capability-gate-review-preview.html"));

        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "cdn");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Artifacts_DeclareReviewLedgerAndAcceptanceBlocked()
    {
        var review = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m566", "capability-gate-ui-review.json"));
        var ledger = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m566", "consent-scope-ledger-mock.json"));
        var acceptance = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m566", "fail-closed-acceptance-pack.json"));

        AssertContains(review, "\"isStaticPreview\": true");
        AssertContains(review, "\"canEnableGate\": false");
        AssertContains(ledger, "\"isMockOnly\": true");
        AssertContains(ledger, "\"usesProductivePersistence\": false");
        AssertContains(acceptance, "\"failClosedLayerReady\": true");
        AssertContains(acceptance, "\"readyForRealCapabilityEnablement\": false");
        AssertSafeOutput(review + ledger + acceptance);
    }

    [TestMethod]
    public void Boundary_NewCapabilityGateReviewFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
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

    private NodalOsCapabilityGateUiReview Review() =>
        reviewService.CreateReview(Gates(), gateService.CreateDependencyMatrix());

    private NodalOsConsentScopeLedgerMock Ledger() => ledgerService.CreateLedger(Gates());

    private NodalOsFailClosedAcceptancePack AcceptancePack() =>
        acceptanceService.CreatePack(Review(), Ledger(), failureService.CreateMatrix());

    private IReadOnlyList<NodalOsCapabilityAccessGate> Gates() => gateService.CreateGates();

    private static void AssertContains(HashSet<NodalOsFailClosedCriterionKind> kinds, NodalOsFailClosedCriterionKind expected) =>
        Assert.IsTrue(kinds.Contains(expected), $"Missing acceptance criterion: {expected}");

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
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsCapabilityGateUiReviewContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsConsentScopeLedgerMockContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsFailClosedAcceptancePackContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsCapabilityGateUiReviewServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsConsentScopeLedgerMockServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsFailClosedAcceptancePackServices.cs")
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
