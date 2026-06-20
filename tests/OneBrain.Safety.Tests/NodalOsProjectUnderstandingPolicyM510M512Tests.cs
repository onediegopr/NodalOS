using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ProjectUnderstandingPolicy")]
[TestCategory("ContextIntakePreview")]
[TestCategory("UserContext")]
[TestCategory("WorkspaceReadinessContext")]
[TestCategory("WorkspaceMetadataHealth")]
[TestCategory("WorkspaceStorageMissionSwitcher")]
[TestCategory("WorkspaceLocalModel")]
[TestCategory("MissionControlVisualPolish")]
[TestCategory("MissionControlGuidance")]
[TestCategory("MissionControlInteractionNoOp")]
[TestCategory("MissionControlShellReadOnly")]
[TestCategory("AuditAPreUiBoundaryNaming")]
[TestCategory("ApprovalUxHandoffObservability")]
[TestCategory("ApprovalTimelineEvidence")]
[TestCategory("CoreRuntimeRegistryEventBusRedaction")]
[TestCategory("NewTopicsIntake")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsProjectUnderstandingPolicyM510M512Tests
{
    private static readonly string[] ForbiddenNames = ["Nexa", "NEXA", "NODRIX", "HOTEP"];
    private static readonly string[] SensitiveMarkers = ["Bearer ", "Authorization:", "Cookie:", "password", "secret", "api_key", "access_token", "refresh_token", "id_token", "private key"];
    private readonly NodalOsProjectUnderstandingPolicyService service = new();
    private readonly NodalOsProjectUnderstandingPolicyJsonSerializer serializer = new();

    [TestMethod]
    public void Adr_ExistsAndDefinesProjectUnderstandingPolicy()
    {
        var adr = System.IO.File.ReadAllText(PathFor("docs", "architecture", "nodal-os-project-understanding-policy-decision-record.md"));

        AssertContains(adr, "NODAL_OS_PROJECT_UNDERSTANDING_POLICY_DEFINED");
        AssertContains(adr, "Project Understanding means");
        AssertContains(adr, "User-provided context");
        AssertContains(adr, "Mock metadata");
        AssertContains(adr, "Future real scan");
        AssertContains(adr, "Future real project understanding");
        AssertContains(adr, "Future LLM-assisted understanding");
        AssertContains(adr, "Local-first");
        AssertContains(adr, "Explicit consent");
        AssertContains(adr, "Path jail first");
        AssertContains(adr, "Redaction first");
        AssertContains(adr, "Evidence first");
        AssertContains(adr, "BYOK and policy before any future LLM usage");
        AssertContains(adr, "No cloud by default");
        AssertContains(adr, "No execution authority");
        AssertContains(adr, "Safe Context Boundary");
        AssertContains(adr, "Workspace Readiness Gate");
        AssertContains(adr, "Evidence/Timeline");
        AssertContains(adr, "Real understanding is blocked by");
    }

    [TestMethod]
    public void RealScanPreconditions_CreateAllRequiredStates()
    {
        var workspace = NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace();
        var jail = NodalOsWorkspaceFixtures.PathJailBinding();

        foreach (var state in Enum.GetValues<NodalOsRealScanPreconditionState>())
        {
            var preconditions = service.CreateRealScanPreconditions(workspace, jail, state);

            Assert.AreEqual(state, preconditions.State);
            Assert.IsFalse(preconditions.PerformsRealScan);
            Assert.IsFalse(preconditions.CanAuthorizeExecution);
        }
    }

    [TestMethod]
    public void RealScanPreconditions_IncludeScopeLimitsExcludedPatternsAndNoMutation()
    {
        var preconditions = NodalOsProjectUnderstandingPolicyFixtures.PreviewOnlyPreconditions();

        CollectionAssert.Contains(preconditions.ExcludedPatternsRedacted.ToList(), ".git");
        CollectionAssert.Contains(preconditions.ExcludedPatternsRedacted.ToList(), "node_modules");
        CollectionAssert.Contains(preconditions.ExcludedPatternsRedacted.ToList(), "bin");
        CollectionAssert.Contains(preconditions.ExcludedPatternsRedacted.ToList(), "obj");
        CollectionAssert.Contains(preconditions.ExcludedPatternsRedacted.ToList(), ".next");
        CollectionAssert.Contains(preconditions.ExcludedPatternsRedacted.ToList(), "dist");
        CollectionAssert.Contains(preconditions.ExcludedPatternsRedacted.ToList(), "build");
        Assert.IsTrue(preconditions.MaxFileCount > 0);
        Assert.IsTrue(preconditions.MaxFileSizeBytes > 0);
        Assert.IsTrue(preconditions.NoMutationGuaranteed);
        Assert.IsTrue(preconditions.NoCloudUpload);
        Assert.IsTrue(preconditions.NoLlmCall);
        Assert.IsTrue(preconditions.NoEmbeddingsUntilSeparatePolicy);
        Assert.IsTrue(preconditions.PreviewBeforeScanRequired);
        Assert.IsTrue(preconditions.CancelStopRequired);
    }

    [TestMethod]
    public void RealScanPreconditions_DoNotTouchFilesystemOrRuntime()
    {
        var json = serializer.SerializeRealScanPreconditions(NodalOsProjectUnderstandingPolicyFixtures.PreviewOnlyPreconditions());
        var preconditions = NodalOsProjectUnderstandingPolicyFixtures.PreviewOnlyPreconditions();

        Assert.IsFalse(preconditions.PerformsRealScan);
        Assert.IsFalse(preconditions.ListsFilesystem);
        Assert.IsFalse(preconditions.ReadsFiles);
        Assert.IsFalse(preconditions.HashesFiles);
        Assert.IsFalse(preconditions.UsesGit);
        Assert.IsFalse(preconditions.MutatesFilesystem);
        Assert.IsFalse(preconditions.CanAuthorizeExecution);
        AssertSafeOutput(json);
    }

    [TestMethod]
    public void ContextToLlmGovernance_CreateAllRequiredStates()
    {
        var workspace = NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace();

        foreach (var state in Enum.GetValues<NodalOsContextToLlmGovernanceState>())
        {
            var governance = service.CreateContextToLlmGovernanceDraft(workspace, state);

            Assert.AreEqual(state, governance.State);
            Assert.IsFalse(governance.CreatesPrompt);
            Assert.IsFalse(governance.CallsLlmProvider);
            Assert.IsFalse(governance.CanAuthorizeExecution);
        }
    }

    [TestMethod]
    public void ContextToLlmGovernance_RequiresRedactionConsentByokPromptBudgetAndLabels()
    {
        var governance = NodalOsProjectUnderstandingPolicyFixtures.FutureLlmGovernance();

        Assert.IsTrue(governance.RequiresRedaction);
        Assert.IsTrue(governance.RequiresUserConsent);
        Assert.IsTrue(governance.RequiresFutureByok);
        Assert.IsTrue(governance.RequiresPromptGovernance);
        Assert.IsTrue(governance.RequiresBudgetGuardrails);
        Assert.IsTrue(governance.RequiresHumanReview);
        Assert.IsTrue(governance.EvidenceRequirementsRedacted.Count > 0);
        Assert.IsTrue(governance.ProvenanceConfidenceFreshnessRequirementsRedacted.Count >= 3);
        Assert.IsTrue(governance.HumanReviewRequirementsRedacted.Count > 0);
        Assert.IsTrue(governance.ProhibitedContextRedacted.Count > 0);
    }

    [TestMethod]
    public void ContextToLlmGovernance_DoesNotCreatePromptCallProviderNetworkCloudOrRuntime()
    {
        var governance = NodalOsProjectUnderstandingPolicyFixtures.FutureLlmGovernance();
        var json = serializer.SerializeContextToLlmGovernance(governance);

        Assert.IsFalse(governance.CreatesPrompt);
        Assert.IsFalse(governance.CallsLlmProvider);
        Assert.IsFalse(governance.SendsNetworkData);
        Assert.IsFalse(governance.CallsCloud);
        Assert.IsFalse(governance.CanAuthorizeExecution);
        AssertSafeOutput(json);
    }

    [TestMethod]
    public void Boundary_NewProjectUnderstandingPolicyFiles_DoNotReferenceForbiddenRuntimePrimitives()
    {
        var source = NewSource();

        AssertDoesNotContain(source, "OneBrain.BrowserExecutor.Cdp");
        AssertDoesNotContain(source, "HttpClient");
        AssertDoesNotContain(source, "ClientWebSocket");
        AssertDoesNotContain(source, "Process.Start");
        AssertDoesNotContain(source, "System.Diagnostics.Process");
        AssertDoesNotContain(source, "BackgroundService");
        AssertDoesNotContain(source, "Task.Run");
        AssertDoesNotContain(source, "IHostedService");
        AssertDoesNotContain(source, "QueueClient");
        AssertDoesNotContain(source, "RecorderRuntime");
        AssertDoesNotContain(source, "ReplayRuntime");
        AssertDoesNotContain(source, "DslParserRuntime");
        AssertDoesNotContain(source, "ProviderClient");
        AssertDoesNotContain(source, "CloudSync");
        AssertDoesNotContain(source, "File.Write");
        AssertDoesNotContain(source, "File.Read");
        AssertDoesNotContain(source, "Directory.");
        AssertDoesNotContain(source, "git ");
        AssertDoesNotContain(source, "embeddings client");
    }

    [TestMethod]
    public void ExistingSafetyContinuity_PreviousLayersRemainSafe()
    {
        var contextSet = NodalOsContextIntakePreviewFixtures.SafeContextIntakeSet();
        var userCapture = NodalOsUserContextFixtures.UserSummaryCapture();
        var shell = NodalOsMissionControlShellFixtures.ShellPreview();

        Assert.IsFalse(contextSet.Report.CanAuthorizeExecution);
        Assert.IsFalse(contextSet.Report.CallsLlmProvider);
        Assert.IsFalse(contextSet.Report.CreatesPrompt);
        Assert.IsFalse(userCapture.CanAuthorizeExecution);
        Assert.IsFalse(shell.RuntimeExecutionAllowed);
        Assert.IsTrue(shell.ReadOnlyUi);
    }

    [TestMethod]
    public void ArtifactMarksProjectUnderstandingPolicyGovernanceReady()
    {
        var artifact = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m512", "project-understanding-policy-summary.json"));

        AssertContains(artifact, "\"projectUnderstandingPolicyAdr\": true");
        AssertContains(artifact, "\"realScanPreconditions\": true");
        AssertContains(artifact, "\"contextToLlmGovernanceDraft\": true");
        AssertContains(artifact, "\"policyOnly\": true");
        AssertContains(artifact, "\"projectUnderstandingRealIntroduced\": false");
        AssertContains(artifact, "\"promptCreationIntroduced\": false");
    }

    private static void AssertSafeOutput(string text)
    {
        foreach (var marker in SensitiveMarkers)
            AssertDoesNotContain(text, marker);
        foreach (var name in ForbiddenNames)
            AssertDoesNotContain(text, name);
    }

    private static string NewSource() =>
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Contracts", "NodalOsProjectUnderstandingPolicyContracts.cs")) +
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Core", "NodalOsProjectUnderstandingPolicyServices.cs"));

    private static string PathFor(params string[] parts) =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", Path.Combine(parts));

    private static void AssertContains(string text, string expected) =>
        StringAssert.Contains(text, expected);

    private static void AssertDoesNotContain(string text, string unexpected) =>
        Assert.IsFalse(text.Contains(unexpected, StringComparison.Ordinal), $"Unexpected marker found: {unexpected}");
}
