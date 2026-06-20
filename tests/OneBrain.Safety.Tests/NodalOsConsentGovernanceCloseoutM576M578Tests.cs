using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ConsentAdversarialTestMatrix")]
[TestCategory("ProductiveConsentStorageAdr")]
[TestCategory("ConsentGovernanceCloseout")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsConsentGovernanceCloseoutM576M578Tests
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
    private readonly NodalOsConsentAdversarialTestMatrixJsonSerializer matrixSerializer = new();
    private readonly NodalOsProductiveConsentStorageAdrService adrService = new();
    private readonly NodalOsProductiveConsentStorageAdrJsonSerializer adrSerializer = new();
    private readonly NodalOsConsentGovernanceCloseoutService closeoutService = new();
    private readonly NodalOsConsentGovernanceCloseoutJsonSerializer closeoutSerializer = new();

    [TestMethod]
    public void ConsentAdversarialMatrix_IsSyntheticMatrixOnlyAndNonProductive()
    {
        var matrix = Matrix();

        Assert.IsTrue(matrix.IsSyntheticOnly);
        Assert.IsTrue(matrix.IsAdversarialMatrixOnly);
        Assert.IsFalse(matrix.UsesProductivePersistence);
        Assert.IsFalse(matrix.PersistsConsent);
        Assert.IsFalse(matrix.EnforcesConsent);
        Assert.IsFalse(matrix.CanAuthorizeCapability);
        Assert.IsFalse(matrix.CanAuthorizeFilesystemAccess);
        Assert.IsFalse(matrix.CanAuthorizeLlmContext);
        Assert.IsFalse(matrix.CanUseCloud);
        AssertSafeOutput(matrixSerializer.Serialize(matrix));
    }

    [TestMethod]
    public void ConsentAdversarialMatrix_IncludesAllRequiredCategories()
    {
        var categories = Matrix().TestCases.Select(test => test.Category).ToHashSet();

        foreach (var category in Enum.GetValues<NodalOsConsentAdversarialCategory>())
            Assert.IsTrue(categories.Contains(category), $"Missing category: {category}");
    }

    [TestMethod]
    public void ConsentAdversarialCases_RequireFailClosedAndNeverAuthorizeOrExport()
    {
        foreach (var testCase in Matrix().TestCases)
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
    public void ConsentAdversarialMatrixDecision_BlocksProductiveImplementationAndEnforcement()
    {
        var decision = Matrix().Decision;

        Assert.IsTrue(decision.ReadyForAdversarialReview);
        Assert.IsFalse(decision.ReadyForProductiveConsentImplementation);
        Assert.IsFalse(decision.ReadyForProductivePersistence);
        Assert.IsFalse(decision.ReadyForConsentEnforcement);
        Assert.IsFalse(decision.ReadyForCapabilityAuthorization);
        Assert.IsFalse(decision.ReadyForFilesystemAccess);
        Assert.IsFalse(decision.ReadyForLlmContext);
    }

    [TestMethod]
    public void ProductiveConsentStorageAdr_DeclaresFutureStorageOnlyAndDisabled()
    {
        var adr = Adr();

        Assert.AreEqual(NodalOsProductiveConsentStorageAdrDecisionStatus.ProductiveConsentStorageNotImplementedAdrReady, adr.DecisionStatus);
        Assert.IsFalse(adr.ProductiveConsentStorageImplemented);
        Assert.IsTrue(adr.FutureImplementationDisabledByDefault);
        Assert.IsTrue(adr.FutureImplementationLocalFirst);
        Assert.IsTrue(adr.RequiresScopeBoundRecords);
        Assert.IsTrue(adr.RequiresCapabilityBoundRecords);
        Assert.IsTrue(adr.RequiresWorkspaceBoundRecords);
        Assert.IsTrue(adr.RequiresMissionBoundRecords);
        AssertSafeOutput(adrSerializer.Serialize(adr));
    }

    [TestMethod]
    public void ProductiveConsentStorageAdr_BlocksSensitiveContentAndPermissionImplication()
    {
        var adr = Adr();

        Assert.IsFalse(adr.StorageMayContainSensitiveMaterial);
        Assert.IsFalse(adr.StorageMayContainContentPayloads);
        Assert.IsFalse(adr.StorageMayContainUnredactedBroadPaths);
        Assert.IsFalse(adr.StorageCanImplyFilesystemAccess);
        Assert.IsFalse(adr.StorageCanImplyLlmCloudProviderRuntimePermission);
        Assert.IsFalse(adr.ConsentCanImplyAnotherCapability);
        Assert.IsTrue(adr.RevokedStaleMissingConsentFailsClosed);
    }

    [TestMethod]
    public void ProductiveConsentStorageAdr_SeparatesStorageEnforcementAndCapabilityEnablement()
    {
        var adr = Adr();

        Assert.IsTrue(adr.StorageAndEnforcementSeparateMilestones);
        Assert.IsTrue(adr.StorageAndCapabilityEnablementSeparateMilestones);
        Assert.IsTrue(adr.RequiredBeforeImplementationRedacted.Any(item => item.Contains("ADR", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(adr.RequiredBeforeImplementationRedacted.Any(item => item.Contains("guard", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ArchitectureAdr_DeclaresExpectedDecisionStatusAndNonGoals()
    {
        var content = TextStore.ReadAllText(PathFor("docs", "architecture", "productive-consent-storage-implementation-adr.md"));

        AssertContains(content, "PRODUCTIVE_CONSENT_STORAGE_NOT_IMPLEMENTED_ADR_READY");
        AssertContains(content, "disabled-by-default");
        AssertContains(content, "local-first");
        AssertContains(content, "scope-bound");
        AssertContains(content, "capability-bound");
        AssertContains(content, "workspace-bound");
        AssertContains(content, "mission-bound");
        AssertContains(content, "Storage and enforcement remain separate milestones");
        AssertContains(content, "Storage and capability enablement remain separate milestones");
        AssertSafeOutput(content);
    }

    [TestMethod]
    public void ConsentGovernanceCloseout_ClosesGovernanceBaselineOnly()
    {
        var closeout = Closeout();

        Assert.IsTrue(closeout.ClosedAsGovernanceBaseline);
        Assert.IsTrue(closeout.ProductiveConsentStillBlocked);
        Assert.IsTrue(closeout.ProductiveStorageStillBlocked);
        Assert.IsTrue(closeout.ConsentEnforcementStillBlocked);
        AssertSafeOutput(closeoutSerializer.Serialize(closeout));
    }

    [TestMethod]
    public void ConsentGovernanceCloseout_CoversRequiredDecisionsAndFindings()
    {
        var closeout = Closeout();
        var decisions = closeout.CoveredDecisions.ToHashSet();
        var findings = closeout.Findings.Select(finding => finding.Kind).ToHashSet();

        Assert.IsTrue(decisions.Contains(NodalOsConsentGovernanceCoveredDecision.AccessImplementationCheckpointReady));
        Assert.IsTrue(decisions.Contains(NodalOsConsentGovernanceCoveredDecision.ProductiveConsentDesignReviewReady));
        Assert.IsTrue(decisions.Contains(NodalOsConsentGovernanceCoveredDecision.ProductiveConsentStorageNotImplementedAdrReady));

        foreach (var finding in Enum.GetValues<NodalOsConsentGovernanceFindingKind>())
            Assert.IsTrue(findings.Contains(finding), $"Missing finding: {finding}");
    }

    [TestMethod]
    public void ConsentGovernanceCloseoutDecision_BlocksProductiveUseAndDirectImplementation()
    {
        var decision = Closeout().Decision;

        Assert.IsTrue(decision.ConsentGovernanceBaselineReady);
        Assert.IsFalse(decision.ReadyForProductiveConsentImplementation);
        Assert.IsFalse(decision.ReadyForProductiveConsentStorage);
        Assert.IsFalse(decision.ReadyForProductiveConsentEnforcement);
        Assert.IsFalse(decision.ReadyForCapabilityAuthorization);
        Assert.IsFalse(decision.ReadyForFilesystemAccess);
        Assert.IsFalse(decision.ReadyForLlmContext);
        Assert.IsFalse(decision.ReadyForCloud);
        Assert.IsFalse(decision.ReadyForRuntime);
        AssertDoesNotContain(decision.RecommendedNextMilestoneRedacted, "direct");
    }

    [TestMethod]
    public void StaticPreviewArtifact_HasNoExternalScriptsOrNetworkResources()
    {
        var html = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m578", "consent-governance-closeout-preview.html"));

        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "cdn");
        AssertDoesNotContain(html, "tele" + "metry");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Artifacts_DeclareMatrixAdrAndCloseoutBlocked()
    {
        var matrix = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m578", "consent-adversarial-test-matrix.json"));
        var adr = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m578", "productive-consent-storage-adr-summary.json"));
        var closeout = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m578", "consent-governance-closeout.json"));

        AssertContains(matrix, "\"isSyntheticOnly\": true");
        AssertContains(matrix, "\"enforcesConsent\": false");
        AssertContains(adr, "\"productiveConsentStorageImplemented\": false");
        AssertContains(adr, "\"futureImplementationDisabledByDefault\": true");
        AssertContains(closeout, "\"closedAsGovernanceBaseline\": true");
        AssertContains(closeout, "\"productiveConsentStillBlocked\": true");
        AssertSafeOutput(matrix + adr + closeout);
    }

    [TestMethod]
    public void Boundary_NewConsentGovernanceFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
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
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsConsentAdversarialTestMatrixContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsProductiveConsentStorageAdrContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsConsentGovernanceCloseoutContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsConsentAdversarialTestMatrixServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsProductiveConsentStorageAdrServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsConsentGovernanceCloseoutServices.cs")
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
