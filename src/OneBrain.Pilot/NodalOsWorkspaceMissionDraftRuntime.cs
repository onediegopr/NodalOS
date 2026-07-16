using OneBrain.AgentOperations.Core.Workspace;
using OneBrain.Core.Models;

namespace OneBrain.Pilot;

public static class NodalOsWorkspaceMissionDraftRuntime
{
    public const string MetadataPathEnvironmentVariable = "NODAL_OS_WORKSPACE_MISSION_METADATA_PATH";

    public static NodalOsWorkspaceMissionDraftService CreateDefault()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var productRoot = Path.Combine(localAppData, "NodalOS");
        var missionMetadataPath = Environment.GetEnvironmentVariable(MetadataPathEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(missionMetadataPath))
            missionMetadataPath = Path.Combine(productRoot, "missions", "active.v1.json");

        var secretRoot = Environment.GetEnvironmentVariable(NodalOsWorkspaceSelectionRuntime.SecretRootEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(secretRoot))
            secretRoot = Path.Combine(productRoot, "secrets", "workspace-roots");

        var rootStore = new WindowsDpapiSecretReferenceStore(secretRoot);
        var workspaceSelection = NodalOsWorkspaceSelectionRuntime.CreateDefault();
        return new NodalOsWorkspaceMissionDraftService(
            missionMetadataPath,
            workspaceSelection,
            rootStore);
    }
}
