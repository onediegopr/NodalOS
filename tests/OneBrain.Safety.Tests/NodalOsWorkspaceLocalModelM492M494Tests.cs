using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
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
public sealed class NodalOsWorkspaceLocalModelM492M494Tests
{
    private static readonly string[] ForbiddenNames = ["Nexa", "NEXA", "NODRIX", "HOTEP"];
    private static readonly string[] SecretMarkers = ["Bearer ", "Authorization:", "Cookie:", "password", "secret", "api_key", "access_token", "refresh_token", "id_token", "private key"];
    private readonly NodalOsWorkspaceService service = new();
    private readonly NodalOsWorkspaceValidator validator = new();
    private readonly NodalOsWorkspaceJsonSerializer serializer = new();

    [TestMethod]
    public void WorkspaceDraft_IsValidReadOnlyNoRuntime()
    {
        var workspace = service.CreateWorkspaceDraft();
        var result = validator.ValidateWorkspace(workspace);

        Assert.IsTrue(result.IsValid, string.Join(" | ", result.Errors));
        Assert.AreEqual(NodalOsWorkspaceStatus.Draft, workspace.Status);
        Assert.IsTrue(workspace.ReadOnlyPreview);
        Assert.IsFalse(workspace.RuntimeExecutionAllowed);
        Assert.IsFalse(workspace.CloudSyncAllowed);
        Assert.IsFalse(workspace.LlmProviderCallsAllowed);
    }

    [TestMethod]
    public void WorkspaceActiveReadOnly_IsValid()
    {
        var workspace = service.CreateActiveReadOnlyWorkspace();

        Assert.AreEqual(NodalOsWorkspaceStatus.ActiveReadOnly, workspace.Status);
        Assert.IsTrue(validator.ValidateWorkspace(workspace).IsValid);
    }

    [TestMethod]
    public void WorkspaceRequiresIdAndDisplayName()
    {
        var workspace = service.CreateWorkspaceDraft() with { WorkspaceId = "", DisplayNameRedacted = "" };
        var result = validator.ValidateWorkspace(workspace);

        Assert.IsFalse(result.IsValid);
        AssertContains(string.Join(" | ", result.Errors), "WorkspaceId");
        AssertContains(string.Join(" | ", result.Errors), "DisplayName");
    }

    [TestMethod]
    public void WorkspaceKeepsRefsAndSerializesSafely()
    {
        var workspace = service.CreateWorkspaceDraft();
        var json = serializer.SerializeWorkspace(workspace);

        Assert.IsFalse(string.IsNullOrWhiteSpace(workspace.PathJailBindingId));
        Assert.IsTrue(workspace.EvidenceRefs.Count > 0);
        Assert.IsTrue(workspace.TimelineRefs.Count > 0);
        AssertSafeSerialized(json);
    }

    [TestMethod]
    public void WorkspaceRejectsRuntimeCloudAndLlmEnabled()
    {
        var workspace = service.CreateWorkspaceDraft() with
        {
            RuntimeExecutionAllowed = true,
            CloudSyncAllowed = true,
            LlmProviderCallsAllowed = true
        };

        var result = validator.ValidateWorkspace(workspace);

        Assert.IsFalse(result.IsValid);
        AssertContains(string.Join(" | ", result.Errors), "runtime");
        AssertContains(string.Join(" | ", result.Errors), "cloud");
        AssertContains(string.Join(" | ", result.Errors), "LLM");
    }

    [TestMethod]
    public void PathJailBinding_IsValidContractOnly()
    {
        var binding = service.CreatePathJailBinding();
        var result = validator.ValidatePathJailBinding(binding);

        Assert.IsTrue(result.IsValid, string.Join(" | ", result.Errors));
        Assert.IsFalse(binding.CanMutateFilesystem);
        Assert.IsFalse(binding.CanExecuteShell);
        Assert.IsFalse(binding.CanAccessOutsideJail);
        Assert.IsTrue(binding.RequiresPositiveExecutionGate);
        Assert.IsTrue(binding.RequiresApprovalForFutureMutations);
        Assert.IsFalse(binding.RealFilesystemAccessAllowed);
    }

    [TestMethod]
    public void PathJailRejectsAdversarialPaths()
    {
        var paths = new[]
        {
            "..\\outside.txt",
            "../outside.txt",
            "C:\\outside\\file.txt",
            "\\\\server\\share\\file.txt",
            "folder/..\\outside.txt",
            "",
            "reports/access_token_value.txt"
        };

        foreach (var path in paths)
        {
            var result = service.ValidateRelativePath(service.CreatePathJailBinding(), path);

            Assert.IsFalse(result.IsValid, path);
            Assert.IsFalse(result.FilesystemTouched);
            Assert.IsFalse(result.CanMutateFilesystem);
        }
    }

    [TestMethod]
    public void PathJailAllowsSafeRelativePathWithoutTouchingFilesystem()
    {
        var result = service.ValidateRelativePath(service.CreatePathJailBinding(), "docs/report.md");

        Assert.IsTrue(result.IsValid, string.Join(" | ", result.Errors));
        Assert.IsTrue(result.IsInsideJail);
        Assert.IsFalse(result.FilesystemTouched);
    }

    [TestMethod]
    public void ImportWizardStartsAtChooseLocalFolder()
    {
        var wizard = service.CreateImportWizard();

        Assert.AreEqual(NodalOsProjectImportWizardStepKind.ChooseLocalFolder, wizard.CurrentStep);
        Assert.AreEqual(8, wizard.Steps.Count);
        Assert.IsTrue(wizard.Steps.All(step => step.IsNoOp));
    }

    [TestMethod]
    public void ImportWizardExplainsPrivacyRuntimeCloudAndLlm()
    {
        var wizard = service.CreateImportWizard();
        var text = string.Join(" ", wizard.Steps.Select(step => step.ExplanationRedacted));

        AssertContains(text, "local");
        AssertContains(text, "Runtime");
        AssertContains(text, "cloud");
        AssertContains(text, "provider");
    }

    [TestMethod]
    public void ImportWizardCreatesWorkspaceDraftMockSafe()
    {
        var wizard = service.CreateImportWizard();
        var result = validator.ValidateImportWizard(wizard);

        Assert.IsTrue(result.IsValid, string.Join(" | ", result.Errors));
        Assert.AreEqual(NodalOsWorkspaceStatus.Draft, wizard.WorkspaceDraft.Status);
        Assert.IsTrue(wizard.ProjectImportMockOnly);
        Assert.IsFalse(wizard.ScansFilesystem);
        Assert.IsFalse(wizard.CreatesFolders);
        Assert.IsFalse(wizard.ImportsFiles);
        Assert.IsFalse(wizard.ProductivePersistenceAllowed);
    }

    [TestMethod]
    public void ImportWizardOptionsAreNoOpAndSerializationIsSafe()
    {
        var wizard = service.CreateImportWizard();
        var json = serializer.SerializeImportWizard(wizard);

        Assert.IsTrue(wizard.UserOptions.Contains(NodalOsProjectImportWizardOptionKind.ContinuePreview));
        Assert.IsTrue(wizard.NoOpIntents.All(intent => intent.IsNoOp && !intent.CanAuthorizeExecution && !intent.RuntimeExecutionAllowed));
        AssertSafeSerialized(json);
    }

    [TestMethod]
    public void Boundary_NewWorkspaceFiles_DoNotReferenceRuntimePrimitives()
    {
        var source = NewWorkspaceSource();

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
        AssertDoesNotContain(source, "Directory.CreateDirectory");
    }

    [TestMethod]
    public void ExistingSafetyContinuity_MissionControlRemainsReadOnly()
    {
        var shell = NodalOsMissionControlShellFixtures.ShellPreview();
        var visual = new NodalOsMissionControlVisualService().CreateResponsiveDesktopLayoutSpec();
        var intent = NodalOsMissionControlInteractionFixtures.SelectTimelineIntent();

        Assert.IsTrue(shell.ReadOnlyUi);
        Assert.IsFalse(shell.CanAuthorizeExecution);
        Assert.IsFalse(visual.CanExecuteOrMutateState);
        Assert.IsTrue(intent.IsNoOp);
        Assert.IsFalse(intent.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void ArtifactMarksWorkspacePathJailImportContract()
    {
        var artifact = File.ReadAllText(PathFor("artifacts", "agent-operations", "m494", "workspace-local-model-summary.json"));

        AssertContains(artifact, "\"workspaceLocalModel\": true");
        AssertContains(artifact, "\"pathJailBinding\": true");
        AssertContains(artifact, "\"projectImportWizardContract\": true");
        AssertContains(artifact, "\"filesystemMutationIntroduced\": false");
        AssertContains(artifact, "\"realFilesystemScanIntroduced\": false");
    }

    private static void AssertSafeSerialized(string text)
    {
        foreach (var marker in SecretMarkers)
            AssertDoesNotContain(text, marker);
        foreach (var name in ForbiddenNames)
            AssertDoesNotContain(text, name);
    }

    private static string NewWorkspaceSource() =>
        string.Join(Environment.NewLine,
            new[]
            {
                PathFor("src", "OneBrain.AgentOperations.Contracts", "NodalOsWorkspaceContracts.cs"),
                PathFor("src", "OneBrain.AgentOperations.Core", "NodalOsWorkspaceServices.cs")
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
