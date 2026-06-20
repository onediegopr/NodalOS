using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
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
public sealed class NodalOsWorkspaceMetadataHealthM498M500Tests
{
    private static readonly string[] ForbiddenNames = ["Nexa", "NEXA", "NODRIX", "HOTEP"];
    private static readonly string[] SecretMarkers = ["Bearer ", "Authorization:", "Cookie:", "password", "secret", "api_key", "access_token", "refresh_token", "id_token", "private key"];
    private readonly NodalOsWorkspaceMetadataService service = new();
    private readonly NodalOsWorkspaceMetadataValidator validator = new();
    private readonly NodalOsWorkspaceMetadataJsonSerializer serializer = new();

    [TestMethod]
    public void MetadataIndex_CreatesEmptyIndex()
    {
        var index = service.CreateEmptyIndex();
        var result = validator.ValidateMetadataIndex(index);

        Assert.IsTrue(result.IsValid, string.Join(" | ", result.Errors));
        Assert.AreEqual(NodalOsWorkspaceMetadataIndexStatus.Empty, index.Status);
        Assert.IsTrue(index.MockOnly);
    }

    [TestMethod]
    public void MetadataIndex_CreatesValidMockIndex()
    {
        var index = service.CreateMockIndex(NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace());
        var result = validator.ValidateMetadataIndex(index);

        Assert.IsTrue(result.IsValid, string.Join(" | ", result.Errors));
        Assert.AreEqual(NodalOsWorkspaceMetadataIndexStatus.MockIndexed, index.Status);
        Assert.IsTrue(index.IndexedItemRefsMock.Count > 0);
        Assert.IsTrue(index.ProjectTypeHintsMock.Count > 0);
    }

    [TestMethod]
    public void MetadataIndex_RequiresWorkspaceId()
    {
        var index = service.CreateMockIndex(NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace()) with { WorkspaceId = "" };
        var result = validator.ValidateMetadataIndex(index);

        Assert.IsFalse(result.IsValid);
        AssertContains(string.Join(" | ", result.Errors), "WorkspaceId");
    }

    [TestMethod]
    public void MetadataIndex_PreservesEvidenceAndTimelineRefs()
    {
        var workspace = NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace();
        var index = service.CreateMockIndex(workspace);

        Assert.AreEqual(workspace.EvidenceRefs.Count, index.EvidenceRefs.Count);
        Assert.AreEqual(workspace.TimelineRefs.Count, index.TimelineRefs.Count);
    }

    [TestMethod]
    public void MetadataIndex_DeclaresSourceTypes()
    {
        var sourceTypes = Enum.GetNames<NodalOsWorkspaceMetadataSourceType>();

        CollectionAssert.Contains(sourceTypes, nameof(NodalOsWorkspaceMetadataSourceType.Fixture));
        CollectionAssert.Contains(sourceTypes, nameof(NodalOsWorkspaceMetadataSourceType.UserProvidedMetadata));
        CollectionAssert.Contains(sourceTypes, nameof(NodalOsWorkspaceMetadataSourceType.ImportWizardPreview));
        CollectionAssert.Contains(sourceTypes, nameof(NodalOsWorkspaceMetadataSourceType.Mock));
    }

    [TestMethod]
    public void MetadataIndex_DeclaresRequiresRealScanLaterWhenNeeded()
    {
        var index = service.CreateRequiresRealScanLaterIndex(NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace());

        Assert.AreEqual(NodalOsWorkspaceMetadataIndexStatus.RequiresRealScanLater, index.Status);
        Assert.IsFalse(index.RealFilesystemScanAllowed);
    }

    [TestMethod]
    public void MetadataIndex_DoesNotContainRawPathsSecretsOrForbiddenNames()
    {
        var json = serializer.SerializeMetadataIndex(service.CreateMockIndex(NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace()));

        AssertSafeSerialized(json);
        AssertDoesNotContain(json, "C:\\");
    }

    [TestMethod]
    public void MetadataIndex_DoesNotEnableScanDirectoryContentShellProviderCloud()
    {
        var index = service.CreateMockIndex(NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace());

        Assert.IsFalse(index.RealFilesystemScanAllowed);
        Assert.IsFalse(index.DirectoryEnumerationAllowed);
        Assert.IsFalse(index.FileContentAccessAllowed);
        Assert.IsFalse(index.FileFingerprintingAllowed);
        Assert.IsFalse(index.ShellCommandAllowed);
        Assert.IsFalse(index.LlmProviderCallsAllowed);
        Assert.IsFalse(index.CloudSyncAllowed);
        Assert.IsFalse(index.VectorIndexAllowed);
        Assert.IsFalse(index.ProductivePersistenceAllowed);
        Assert.IsFalse(index.IsSourceOfTruthForExecution);
    }

    [TestMethod]
    public void ProjectSummary_CreatesFromMetadataMock()
    {
        var workspace = NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace();
        var index = service.CreateMockIndex(workspace);
        var summary = service.CreateSafeProjectSummary(workspace, index);
        var result = validator.ValidateProjectSummary(summary);

        Assert.IsTrue(result.IsValid, string.Join(" | ", result.Errors));
        Assert.AreEqual(workspace.WorkspaceId, summary.WorkspaceId);
        Assert.AreEqual(NodalOsProjectSummaryConfidence.Mock, summary.Confidence);
    }

    [TestMethod]
    public void ProjectSummary_DisclosesNoContentAccessAndNoRealKnowledge()
    {
        var summary = NodalOsWorkspaceMetadataFixtures.ProjectSummary();
        var text = string.Join(" ", summary.BasisDisclosureRedacted, summary.ProjectKnowledgeDisclosureRedacted, summary.NoContentAccessDisclosureRedacted);

        AssertContains(text, "metadata segura/mock");
        AssertContains(text, "Project understanding real");
        AssertContains(text, "No se leyo ningun archivo");
    }

    [TestMethod]
    public void ProjectSummary_IncludesReadinessRiskMissingInfoDisabledAndNextSteps()
    {
        var summary = NodalOsWorkspaceMetadataFixtures.ProjectSummary();

        Assert.IsTrue(summary.ReadinessSummaryRedacted.Count > 0);
        Assert.IsTrue(summary.RiskSummaryRedacted.Count > 0);
        Assert.IsTrue(summary.MissingInformationRedacted.Count > 0);
        Assert.IsTrue(summary.DisabledCapabilitiesRedacted.Count > 0);
        Assert.IsTrue(summary.NextSafeStepsRedacted.Count > 0);
    }

    [TestMethod]
    public void ProjectSummary_PreservesRefsAndHealthRef()
    {
        var summary = NodalOsWorkspaceMetadataFixtures.ProjectSummary();

        Assert.IsTrue(summary.EvidenceRefs.Count > 0);
        Assert.IsTrue(summary.TimelineRefs.Count > 0);
        Assert.IsTrue(summary.ObservabilityRefs.Count > 0);
        Assert.IsFalse(string.IsNullOrWhiteSpace(summary.WorkspaceHealthRef));
    }

    [TestMethod]
    public void ProjectSummary_ConfidenceValuesAreModeled()
    {
        var confidence = Enum.GetNames<NodalOsProjectSummaryConfidence>();

        CollectionAssert.Contains(confidence, nameof(NodalOsProjectSummaryConfidence.Mock));
        CollectionAssert.Contains(confidence, nameof(NodalOsProjectSummaryConfidence.UserProvided));
        CollectionAssert.Contains(confidence, nameof(NodalOsProjectSummaryConfidence.Low));
        CollectionAssert.Contains(confidence, nameof(NodalOsProjectSummaryConfidence.Unknown));
    }

    [TestMethod]
    public void ProjectSummary_SerializedOutputIsSafeAndNonAuthoritative()
    {
        var summary = NodalOsWorkspaceMetadataFixtures.ProjectSummary();
        var json = serializer.SerializeProjectSummary(summary);

        AssertSafeSerialized(json);
        Assert.IsTrue(summary.SafeToDisplay);
        Assert.IsFalse(summary.CanAuthorizeExecution);
        Assert.IsFalse(summary.RuntimeExecutionAllowed);
        Assert.IsFalse(summary.RealFilesystemScanAllowed);
        Assert.IsFalse(summary.LlmProviderCallsAllowed);
        Assert.IsFalse(summary.CloudSyncAllowed);
    }

    [TestMethod]
    public void HealthReport_CreatesHealthyMock()
    {
        var report = NodalOsWorkspaceMetadataFixtures.HealthReport();
        var result = validator.ValidateHealthReport(report);

        Assert.IsTrue(result.IsValid, string.Join(" | ", result.Errors));
        Assert.AreEqual(NodalOsWorkspaceHealthStatus.HealthyMock, report.HealthStatus);
        Assert.IsFalse(report.RequiresAction);
        Assert.IsFalse(report.RequiresHumanAttention);
    }

    [TestMethod]
    public void HealthReport_CreatesAllRequiredStatuses()
    {
        var statuses = new[]
        {
            NodalOsWorkspaceHealthStatus.NeedsWorkspaceValidation,
            NodalOsWorkspaceHealthStatus.NeedsPathJailValidation,
            NodalOsWorkspaceHealthStatus.NeedsMetadata,
            NodalOsWorkspaceHealthStatus.BlockedByGuardrail,
            NodalOsWorkspaceHealthStatus.BlockedByRuntimeGate,
            NodalOsWorkspaceHealthStatus.BlockedByCloudQuarantine
        };

        foreach (var status in statuses)
        {
            var report = NodalOsWorkspaceMetadataFixtures.HealthReport(status);

            Assert.AreEqual(status, report.HealthStatus);
            Assert.IsTrue(report.RequiresAction);
            Assert.IsTrue(report.BlockersRedacted.Count > 0);
            Assert.IsTrue(report.NextSafeStepsRedacted.Count > 0);
        }
    }

    [TestMethod]
    public void HealthReport_BlockedStatusesRequireHumanAttention()
    {
        var guardrail = NodalOsWorkspaceMetadataFixtures.HealthReport(NodalOsWorkspaceHealthStatus.BlockedByGuardrail);
        var runtime = NodalOsWorkspaceMetadataFixtures.HealthReport(NodalOsWorkspaceHealthStatus.BlockedByRuntimeGate);
        var cloud = NodalOsWorkspaceMetadataFixtures.HealthReport(NodalOsWorkspaceHealthStatus.BlockedByCloudQuarantine);

        Assert.IsTrue(guardrail.RequiresHumanAttention);
        Assert.IsTrue(runtime.RequiresHumanAttention);
        Assert.IsTrue(cloud.RequiresHumanAttention);
    }

    [TestMethod]
    public void HealthReport_IsReadOnlyNoRuntimeNoProviderNoCloud()
    {
        var report = NodalOsWorkspaceMetadataFixtures.HealthReport(NodalOsWorkspaceHealthStatus.NeedsMetadata);

        Assert.IsTrue(report.ReadOnlyReport);
        Assert.IsFalse(report.MutatesState);
        Assert.IsFalse(report.CanAuthorizeExecution);
        Assert.IsFalse(report.RuntimeExecutionAllowed);
        Assert.IsFalse(report.RealFilesystemScanAllowed);
        Assert.IsFalse(report.LlmProviderCallsAllowed);
        Assert.IsFalse(report.CloudSyncAllowed);
    }

    [TestMethod]
    public void HealthReport_SerializedOutputIsSafe()
    {
        var json = serializer.SerializeHealthReport(NodalOsWorkspaceMetadataFixtures.HealthReport(NodalOsWorkspaceHealthStatus.BlockedByRuntimeGate));

        AssertSafeSerialized(json);
    }

    [TestMethod]
    public void Boundary_NewWorkspaceMetadataFiles_DoNotReferenceRuntimePrimitives()
    {
        var source = NewWorkspaceMetadataSource();

        AssertDoesNotContain(source, "OneBrain.BrowserExecutor.Cdp");
        AssertDoesNotContain(source, "HttpClient");
        AssertDoesNotContain(source, "ClientWebSocket");
        AssertDoesNotContain(source, "Process.Start");
        AssertDoesNotContain(source, "System.Diagnostics.Process");
        AssertDoesNotContain(source, "BackgroundService");
        AssertDoesNotContain(source, "Task.Run");
        AssertDoesNotContain(source, "ExecuteAsync");
        AssertDoesNotContain(source, "OpenAI");
        AssertDoesNotContain(source, "TelemetryClient");
        AssertDoesNotContain(source, "AnalyticsClient");
        AssertDoesNotContain(source, "File.Write");
        AssertDoesNotContain(source, "File.Read");
        AssertDoesNotContain(source, "Directory.");
        AssertDoesNotContain(source, "Process ");
    }

    [TestMethod]
    public void ExistingSafetyContinuity_PreviousWorkspaceLayersRemainSafe()
    {
        var storage = NodalOsWorkspaceMissionFixtures.StorageWithDraftAndActive();
        var switcher = NodalOsWorkspaceMissionFixtures.WorkspaceSwitcher();
        var shell = NodalOsMissionControlShellFixtures.ShellPreview();
        var intent = NodalOsMissionControlInteractionFixtures.SelectTimelineIntent();

        Assert.AreEqual(2, storage.ListWorkspaces().Count);
        Assert.IsTrue(switcher.ReadOnlyPreview);
        Assert.IsFalse(switcher.RuntimeExecutionAllowed);
        Assert.IsFalse(shell.CanAuthorizeExecution);
        Assert.IsTrue(intent.IsNoOp);
        Assert.IsFalse(intent.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void ArtifactMarksWorkspaceMetadataHealthMock()
    {
        var artifact = File.ReadAllText(PathFor("artifacts", "agent-operations", "m500", "workspace-metadata-health-summary.json"));

        AssertContains(artifact, "\"workspaceMetadataIndexMock\": true");
        AssertContains(artifact, "\"safeProjectSummaryContract\": true");
        AssertContains(artifact, "\"workspaceHealthReport\": true");
        AssertContains(artifact, "\"realFilesystemScanIntroduced\": false");
        AssertContains(artifact, "\"gitCommandIntroduced\": false");
        AssertContains(artifact, "\"projectUnderstandingRealIntroduced\": false");
    }

    private static void AssertSafeSerialized(string text)
    {
        foreach (var marker in SecretMarkers)
            AssertDoesNotContain(text, marker);
        foreach (var name in ForbiddenNames)
            AssertDoesNotContain(text, name);
    }

    private static string NewWorkspaceMetadataSource() =>
        string.Join(Environment.NewLine,
            new[]
            {
                PathFor("src", "OneBrain.AgentOperations.Contracts", "NodalOsWorkspaceMetadataContracts.cs"),
                PathFor("src", "OneBrain.AgentOperations.Core", "NodalOsWorkspaceMetadataServices.cs")
            }.Select(File.ReadAllText));

    private static void AssertContains(string text, string expected) =>
        Assert.IsTrue(text.Contains(expected, StringComparison.OrdinalIgnoreCase), expected);

    private static void AssertDoesNotContain(string text, string unexpected) =>
        Assert.IsFalse(text.Contains(unexpected, StringComparison.Ordinal), unexpected);

    private static string PathFor(params string[] parts)
    {
        var root = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(new[] { root }.Concat(parts).ToArray());
            if (File.Exists(candidate) || Directory.Exists(candidate))
                return candidate;
            var parent = Directory.GetParent(root);
            if (parent is null)
                break;
            root = parent.FullName;
        }

        return Path.Combine(new[] { AppContext.BaseDirectory }.Concat(parts).ToArray());
    }
}
