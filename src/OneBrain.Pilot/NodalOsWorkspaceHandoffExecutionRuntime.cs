using OneBrain.AgentOperations.Core.Workspace;
using OneBrain.Core.Models;

namespace OneBrain.Pilot;

public static class NodalOsWorkspaceHandoffExecutionRuntime
{
    public const string MetadataPathEnvironmentVariable = "NODAL_OS_WORKSPACE_HANDOFF_EXECUTION_METADATA_PATH";
    public const string RestoreRootEnvironmentVariable = "NODAL_OS_WORKSPACE_HANDOFF_RESTORE_ROOT";

    public static NodalOsWorkspaceHandoffExecutionService CreateDefault()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var productRoot = Path.Combine(localAppData, "NodalOS");
        var metadataPath = Environment.GetEnvironmentVariable(MetadataPathEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(metadataPath))
            metadataPath = Path.Combine(productRoot, "missions", "active-handoff-execution.v1.json");

        var restoreRoot = Environment.GetEnvironmentVariable(RestoreRootEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(restoreRoot))
            restoreRoot = Path.Combine(productRoot, "missions", "restore");

        var secretRoot = Environment.GetEnvironmentVariable(NodalOsWorkspaceSelectionRuntime.SecretRootEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(secretRoot))
            secretRoot = Path.Combine(productRoot, "secrets", "workspace-roots");

        var rootStore = new WindowsDpapiSecretReferenceStore(secretRoot);
        var workspaceSelection = NodalOsWorkspaceSelectionRuntime.CreateDefault();
        var missionDraft = NodalOsWorkspaceMissionDraftRuntime.CreateDefault();
        return new NodalOsWorkspaceHandoffExecutionService(
            metadataPath,
            restoreRoot,
            workspaceSelection,
            missionDraft,
            rootStore);
    }
}