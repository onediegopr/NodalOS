using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ConsentStorageBoundaryTestPack")]
[TestCategory("DisabledStorageUiPreview")]
[TestCategory("StorageAuditReadiness")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsConsentStorageBoundaryM579M581Tests
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
    private readonly NodalOsProductiveConsentDesignReviewService reviewService = new();
    private readonly NodalOsDisabledConsentStorageContractService storageService = new();
    private readonly NodalOsConsentAuditAcceptanceService acceptanceService = new();
    private readonly NodalOsConsentAdversarialTestMatrixService matrixService = new();
    private readonly NodalOsProductiveConsentStorageAdrService adrService = new();
    private readonly NodalOsConsentGovernanceCloseoutService closeoutService = new();
    private readonly NodalOsConsentStorageBoundaryTestPackService boundaryService = new();
    private readonly NodalOsConsentStorageBoundaryTestPackJsonSerializer boundarySerializer = new();
    private readonly NodalOsDisabledStorageUiPreviewService previewService = new();
    private readonly NodalOsDisabledStorageUiPreviewJsonSerializer previewSerializer = new();
    private readonly NodalOsStorageAuditReadinessService readinessService = new();
    private readonly NodalOsStorageAuditReadinessJsonSerializer readinessSerializer = new();

    [TestMethod]
    public void ConsentStorageBoundaryTestPack_IsBoundaryOnlyAndNonProductive()
    {
        var pack = TestPack();

        Assert.IsTrue(pack.IsBoundaryTestPackOnly);
        Assert.IsFalse(pack.UsesProductivePersistence);
        Assert.IsFalse(pack.ReadsProductiveStorage);
        Assert.IsFalse(pack.WritesProductiveStorage);
        Assert.IsFalse(pack.DeletesProductiveStorage);
        Assert.IsFalse(pack.MigratesProductiveStorage);
        Assert.IsFalse(pack.CanAuthorizeCapability);
        Assert.IsFalse(pack.CanAuthorizeFilesystemAccess);
        Assert.IsFalse(pack.CanAuthorizeLlmContext);
        AssertSafeOutput(boundarySerializer.Serialize(pack));
    }

    [TestMethod]
    public void BoundaryCategories_IncludeAllRequiredStorageBoundaries()
    {
        var categories = TestPack().TestCases.Select(test => test.Category).ToHashSet();

        foreach (var category in Enum.GetValues<NodalOsConsentStorageBoundaryCategory>())
            Assert.IsTrue(categories.Contains(category), $"Missing category: {category}");
    }

    [TestMethod]
    public void BoundaryTestCases_FailClosedAndNeverAuthorizePersistOrExport()
    {
        foreach (var testCase in TestPack().TestCases)
        {
            Assert.IsTrue(testCase.RequiresFailClosed);
            Assert.IsTrue(testCase.NeverAuthorizesRealUse);
            Assert.IsTrue(testCase.NeverPersistsProductively);
            Assert.IsTrue(testCase.NeverSendsToLlm);
            Assert.IsTrue(testCase.NeverSendsToCloud);
            Assert.IsTrue(testCase.IsSyntheticOnly);
        }
    }

    [TestMethod]
    public void TestPackDecision_BlocksProductiveStorageAndEnforcement()
    {
        var decision = TestPack().Decision;

        Assert.IsTrue(decision.ReadyForBoundaryReview);
        Assert.IsFalse(decision.ReadyForProductiveStorageImplementation);
        Assert.IsFalse(decision.ReadyForProductivePersistence);
        Assert.IsFalse(decision.ReadyForConsentEnforcement);
        Assert.IsFalse(decision.ReadyForCapabilityAuthorization);
        Assert.IsFalse(decision.ReadyForFilesystemAccess);
        Assert.IsFalse(decision.ReadyForLlmContext);
    }

    [TestMethod]
    public void DisabledStorageUiPreview_IsStaticReadOnlyNoOpAndDisabled()
    {
        var preview = Preview();

        Assert.IsTrue(preview.IsStaticPreview);
        Assert.IsTrue(preview.IsReadOnly);
        Assert.IsTrue(preview.IsNoOp);
        Assert.IsTrue(preview.DisabledByDefault);
        Assert.IsFalse(preview.UsesProductivePersistence);
        Assert.IsFalse(preview.ReadsProductiveStorage);
        Assert.IsFalse(preview.WritesProductiveStorage);
        Assert.IsFalse(preview.DeletesProductiveStorage);
        AssertSafeOutput(previewSerializer.Serialize(preview));
    }

    [TestMethod]
    public void DisabledStorageUiPreview_CannotEnablePersistAuthorizeOrExport()
    {
        var preview = Preview();

        Assert.IsFalse(preview.CanEnableStorage);
        Assert.IsFalse(preview.CanPersistConsent);
        Assert.IsFalse(preview.CanAuthorizeCapability);
        Assert.IsFalse(preview.CanAuthorizeFilesystemAccess);
        Assert.IsFalse(preview.CanAuthorizeLlmContext);
        Assert.IsFalse(preview.CanUseCloud);
        Assert.IsTrue(preview.DisclosuresRedacted.Count >= 6);
    }

    [TestMethod]
    public void UiReviewOptions_AreNoOpAndNonAuthorizing()
    {
        foreach (var option in Preview().ReviewOptions)
        {
            Assert.IsTrue(option.IsNoOp);
            Assert.IsFalse(option.CanAuthorize);
            Assert.IsFalse(option.CanPersist);
        }
    }

    [TestMethod]
    public void StorageAuditReadiness_IsReadinessOnlyAndCannotAuthorize()
    {
        var readiness = Readiness();

        Assert.IsTrue(readiness.IsReadinessOnly);
        Assert.IsFalse(readiness.CanAuthorizeImplementation);
        Assert.IsFalse(readiness.CanEnableProductiveStorage);
        Assert.IsFalse(readiness.CanPersistConsent);
        Assert.IsFalse(readiness.CanEnforceConsent);
        Assert.IsFalse(readiness.CanAuthorizeCapability);
        Assert.IsFalse(readiness.CanAccessFilesystem);
        Assert.IsFalse(readiness.CanBuildLlmContext);
        Assert.IsFalse(readiness.CanUseCloud);
        AssertSafeOutput(readinessSerializer.Serialize(readiness));
    }

    [TestMethod]
    public void AuditReadinessCriteria_IncludeRequiredPlanningInputs()
    {
        var criteria = Readiness().Criteria.Select(criterion => criterion.Kind).ToHashSet();

        Assert.IsTrue(criteria.Contains(NodalOsStorageAuditReadinessCriterionKind.AdrExists));
        Assert.IsTrue(criteria.Contains(NodalOsStorageAuditReadinessCriterionKind.DisabledByDefaultStrategyExists));
        Assert.IsTrue(criteria.Contains(NodalOsStorageAuditReadinessCriterionKind.BoundaryTestPackExists));
        Assert.IsTrue(criteria.Contains(NodalOsStorageAuditReadinessCriterionKind.AdversarialConsentMatrixExists));
        Assert.IsTrue(criteria.Contains(NodalOsStorageAuditReadinessCriterionKind.RollbackDisableStrategyExists));
        Assert.IsTrue(criteria.Contains(NodalOsStorageAuditReadinessCriterionKind.MigrationBlockedUntilFutureGate));
        Assert.IsTrue(criteria.Contains(NodalOsStorageAuditReadinessCriterionKind.CloudSyncBlocked));
        Assert.IsTrue(criteria.Contains(NodalOsStorageAuditReadinessCriterionKind.ProviderRuntimeBlocked));
        Assert.IsTrue(Readiness().Criteria.All(criterion => criterion.BlocksImplementationIfMissing));
    }

    [TestMethod]
    public void ReadinessDecision_AllowsAuditPlanningOnly()
    {
        var decision = Readiness().Decision;

        Assert.IsTrue(decision.ReadyForAuditPlanning);
        Assert.IsFalse(decision.ReadyForProductiveStorageImplementation);
        Assert.IsFalse(decision.ReadyForProductivePersistence);
        Assert.IsFalse(decision.ReadyForConsentEnforcement);
        Assert.IsFalse(decision.ReadyForCapabilityAuthorization);
        Assert.IsFalse(decision.ReadyForFilesystemAccess);
        Assert.IsFalse(decision.ReadyForLlmContext);
        AssertDoesNotContain(decision.RecommendedNextMilestoneRedacted, "direct");
    }

    [TestMethod]
    public void StaticPreviewArtifact_HasNoExternalScriptsOrNetworkResources()
    {
        var html = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m581", "consent-storage-boundary-preview.html"));

        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "cdn");
        AssertDoesNotContain(html, "tele" + "metry");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Artifacts_DeclareBoundaryPreviewAndReadinessBlocked()
    {
        var boundary = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m581", "consent-storage-boundary-test-pack.json"));
        var preview = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m581", "disabled-storage-ui-preview.json"));
        var readiness = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m581", "storage-audit-readiness.json"));

        AssertContains(boundary, "\"isBoundaryTestPackOnly\": true");
        AssertContains(boundary, "\"usesProductivePersistence\": false");
        AssertContains(preview, "\"isStaticPreview\": true");
        AssertContains(preview, "\"canEnableStorage\": false");
        AssertContains(readiness, "\"isReadinessOnly\": true");
        AssertContains(readiness, "\"readyForProductiveStorageImplementation\": false");
        AssertSafeOutput(boundary + preview + readiness);
    }

    [TestMethod]
    public void Boundary_NewConsentStorageBoundaryFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
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

    private NodalOsProductiveConsentDesignReview Review() => reviewService.CreateReview();
    private NodalOsDisabledConsentStorageContract Storage() => storageService.CreateContract(Review(), gateService.CreateGates());
    private NodalOsConsentAuditAcceptancePack Acceptance() => acceptanceService.CreateAcceptance(Review(), Storage());
    private NodalOsConsentAdversarialTestMatrix Matrix() => matrixService.CreateMatrix(Review(), Storage(), Acceptance());
    private NodalOsProductiveConsentStorageAdrSummary Adr() => adrService.CreateSummary(Matrix());
    private NodalOsConsentGovernanceCloseout Closeout() => closeoutService.CreateCloseout(Matrix(), Adr());
    private NodalOsConsentStorageBoundaryTestPack TestPack() => boundaryService.CreateTestPack(Adr(), Closeout(), Storage());
    private NodalOsDisabledStorageUiPreview Preview() => previewService.CreatePreview(TestPack());
    private NodalOsStorageAuditReadiness Readiness() => readinessService.CreateReadiness(TestPack(), Preview());

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
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsConsentStorageBoundaryTestPackContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsDisabledStorageUiPreviewContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsStorageAuditReadinessContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsConsentStorageBoundaryTestPackServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsDisabledStorageUiPreviewServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsStorageAuditReadinessServices.cs")
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
