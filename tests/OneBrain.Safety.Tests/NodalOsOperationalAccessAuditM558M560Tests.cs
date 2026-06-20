using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("DisabledPathJailUiPreview")]
[TestCategory("OperationalAccessAudit")]
[TestCategory("SyntheticPolicyRegression")]
[TestCategory("ProjectUnderstandingImplementationBoundary")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsOperationalAccessAuditM558M560Tests
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

    private readonly NodalOsDisabledPathJailUiPreviewService uiService = new();
    private readonly NodalOsDisabledPathJailUiPreviewJsonSerializer uiSerializer = new();
    private readonly NodalOsOperationalAccessAuditAdrJsonSerializer adrSerializer = new();
    private readonly NodalOsSyntheticPolicyRegressionPackJsonSerializer regressionSerializer = new();

    [TestMethod]
    public void DisabledPathJailUiPreview_IsStaticReadOnlyNoOpAndDisabled()
    {
        var preview = UiPreview();

        Assert.IsTrue(preview.IsStaticPreview);
        Assert.IsTrue(preview.IsReadOnly);
        Assert.IsTrue(preview.IsNoOp);
        Assert.IsTrue(preview.DisabledByDefault);
        Assert.IsFalse(preview.UsesRealFilesystem);
        Assert.IsFalse(preview.PerformsRealCanonicalization);
        Assert.IsFalse(preview.PerformsDirectoryListing);
        Assert.IsFalse(preview.PerformsFileRead);
        Assert.IsFalse(preview.PerformsFileHash);
        Assert.IsFalse(preview.CanEnablePrototype);
        Assert.IsFalse(preview.CanAuthorizeRealScan);
        Assert.IsFalse(preview.CanAuthorizeFilesystemAccess);
        AssertSafeOutput(uiSerializer.SerializePreview(preview));
    }

    [TestMethod]
    public void DisabledPathJailUiReviewOptions_AreNoOpAndNonAuthorizing()
    {
        foreach (var option in Enum.GetValues<NodalOsDisabledPathJailUiReviewOption>())
        {
            var result = uiService.ApplyOption(option);

            Assert.IsTrue(result.IsNoOp);
            Assert.IsFalse(result.MutatesState);
            Assert.IsFalse(result.EnablesPrototype);
            Assert.IsFalse(result.AuthorizesRealScan);
            Assert.IsFalse(result.AuthorizesFilesystemAccess);
            AssertSafeOutput(uiSerializer.SerializeReviewResult(result));
        }
    }

    [TestMethod]
    public void OperationalAccessAuditAdrSummary_DeclaresNotReadyAndAuditRequired()
    {
        var summary = AdrSummary();

        Assert.AreEqual(NodalOsOperationalAccessAuditDecisionStatus.OperationalFilesystemAccessNotReadyAuditRequired, summary.DecisionStatus);
        Assert.IsFalse(summary.OperationalFilesystemAccessReady);
        Assert.IsTrue(summary.DisabledPathJailGateIsPreconditionOnly);
        Assert.IsTrue(summary.RequiresExplicitFutureMilestone);
        Assert.IsTrue(summary.RequiresDisabledByDefaultGate);
        Assert.IsTrue(summary.RequiresUserConsentEnforcement);
        Assert.IsTrue(summary.RequiresPathJailImplementationAudit);
        Assert.IsTrue(summary.RequiresCanonicalizationImplementationAudit);
        Assert.IsTrue(summary.RequiresNoMutationRuntimeProof);
        Assert.IsTrue(summary.RequiresKillSwitchRollbackDisableStrategy);
        Assert.IsTrue(summary.RequiresFullSuiteAndAdversarialTests);
        Assert.IsTrue(summary.FolderEnumerationRequiresSeparateGate);
        Assert.IsTrue(summary.ContentAccessRequiresSeparateGate);
        Assert.IsTrue(summary.ContentFingerprintingRequiresSeparateGate);
        Assert.IsTrue(summary.IndexingRepresentationAndLlmContextBlocked);
        Assert.IsTrue(summary.CloudProviderRuntimeBlocked);
        Assert.IsTrue(summary.SyntheticRegressionNecessaryNotSufficient);
        Assert.IsTrue(summary.FuturePrototypeMustFailClosed);
        AssertSafeOutput(adrSerializer.SerializeSummary(summary));
    }

    [TestMethod]
    public void OperationalAccessAuditAdrDocument_StatesRequiredDecisions()
    {
        var adr = TextStore.ReadAllText(PathFor("docs", "architecture", "operational-access-audit-before-filesystem-access-adr.md"));

        AssertContains(adr, "OPERATIONAL_FILESYSTEM_ACCESS_NOT_READY_AUDIT_REQUIRED");
        AssertContains(adr, "NODAL OS does not allow operational filesystem access yet.");
        AssertContains(adr, "Disabled Path Jail Prototype Gate is a precondition, not an authorization.");
        AssertContains(adr, "Folder enumeration and content access are separate capabilities and require separate gates.");
        AssertContains(adr, "Indexing, representation build, and LLM context remain blocked.");
        AssertContains(adr, "Cloud, provider, and runtime remain blocked.");
        AssertContains(adr, "Kill switch, rollback, and disable strategy are required.");
        AssertSafeOutput(adr);
    }

    [TestMethod]
    public void SyntheticPolicyRegressionPack_IsSyntheticOnlyAndNonOperational()
    {
        var pack = RegressionPack();

        Assert.IsTrue(pack.UsesSyntheticFixturesOnly);
        Assert.IsFalse(pack.UsesRealFilesystem);
        Assert.IsFalse(pack.PerformsRealCanonicalization);
        Assert.IsFalse(pack.PerformsDirectoryListing);
        Assert.IsFalse(pack.PerformsFileRead);
        Assert.IsFalse(pack.PerformsFileHash);
        Assert.IsFalse(pack.PerformsMutation);
        Assert.IsFalse(pack.BuildsLlmContext);
        Assert.IsFalse(pack.CallsProvider);
        Assert.IsFalse(pack.UsesCloud);
        AssertSafeOutput(regressionSerializer.SerializePack(pack));
    }

    [TestMethod]
    public void SyntheticPolicyRegressionPack_IncludesRequiredCategories()
    {
        var categories = RegressionPack().Cases.Select(c => c.Category).ToHashSet();

        foreach (var category in Enum.GetValues<NodalOsSyntheticPolicyRegressionCategory>())
            Assert.IsTrue(categories.Contains(category), $"Missing regression category: {category}");
    }

    [TestMethod]
    public void SyntheticPolicyRegressionCases_NeverLeaveLocalPolicy()
    {
        foreach (var item in RegressionPack().Cases)
        {
            Assert.IsTrue(item.NeverSentToLlm);
            Assert.IsTrue(item.NeverSentToCloud);
            Assert.IsTrue(item.IsSyntheticOnly);
            Assert.IsFalse(item.UsesRealFilesystem);
        }
    }

    [TestMethod]
    public void SyntheticPolicyRegressionResult_AllowsSyntheticRegressionOnly()
    {
        var result = RegressionPack().Result;

        Assert.IsTrue(result.ReadyForSyntheticRegression);
        Assert.IsFalse(result.ReadyForRealPathJail);
        Assert.IsFalse(result.ReadyForRealFilesystemAccess);
        Assert.IsFalse(result.ReadyForRealScan);
        Assert.IsFalse(result.ReadyForIndexing);
        Assert.IsFalse(result.ReadyForRepresentationBuild);
        Assert.IsFalse(result.ReadyForLlmContext);
        Assert.IsTrue(result.PassingSyntheticCases.Count > 0);
        Assert.AreEqual(0, result.FailingSyntheticCases.Count);
        Assert.AreEqual(0, result.MissingSyntheticCases.Count);
    }

    [TestMethod]
    public void StaticPreviewArtifact_HasNoExternalScriptsOrNetworkResources()
    {
        var html = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m560", "operational-access-audit-preview.html"));

        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "cdn");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Artifacts_DeclareOperationalAccessBlockedAndSyntheticRegressionReady()
    {
        var ui = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m560", "disabled-path-jail-ui-preview.json"));
        var adr = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m560", "operational-access-audit-adr-summary.json"));
        var regression = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m560", "synthetic-policy-regression-pack.json"));

        AssertContains(ui, "\"isStaticPreview\": true");
        AssertContains(ui, "\"canEnablePrototype\": false");
        AssertContains(adr, "\"decisionStatus\": \"OperationalFilesystemAccessNotReadyAuditRequired\"");
        AssertContains(regression, "\"usesSyntheticFixturesOnly\": true");
        AssertContains(regression, "\"readyForSyntheticRegression\": true");
        AssertContains(regression, "\"readyForRealFilesystemAccess\": false");
        AssertSafeOutput(ui + adr + regression);
    }

    [TestMethod]
    public void Boundary_NewOperationalAccessFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
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

    private NodalOsDisabledPathJailUiPreview UiPreview()
    {
        var gate = NodalOsDisabledPathJailPrototypeGateFixtures.Gate();
        var matrix = NodalOsSyntheticCanonicalizationCasesFixtures.Matrix();
        var proof = new NodalOsNoMutationProofContractService().CreateContract(gate, matrix);
        return uiService.CreatePreview(gate, matrix, proof);
    }

    private static NodalOsOperationalAccessAuditAdrSummary AdrSummary() =>
        new NodalOsOperationalAccessAuditAdrService().CreateSummary();

    private static NodalOsSyntheticPolicyRegressionPack RegressionPack()
    {
        var gate = NodalOsDisabledPathJailPrototypeGateFixtures.Gate();
        var matrix = NodalOsSyntheticCanonicalizationCasesFixtures.Matrix();
        var proof = new NodalOsNoMutationProofContractService().CreateContract(gate, matrix);
        return new NodalOsSyntheticPolicyRegressionPackService().CreatePack(gate, matrix, proof);
    }

    private static string NewSource()
    {
        var root = FindRepoRoot();
        var files = new[]
        {
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsDisabledPathJailUiPreviewContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsOperationalAccessAuditAdrContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsSyntheticPolicyRegressionPackContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsDisabledPathJailUiPreviewServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsOperationalAccessAuditAdrServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsSyntheticPolicyRegressionPackServices.cs")
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

