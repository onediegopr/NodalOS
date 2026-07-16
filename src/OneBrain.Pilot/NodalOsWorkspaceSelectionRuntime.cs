using OneBrain.AgentOperations.Core.Workspace;
using OneBrain.Core.Models;

namespace OneBrain.Pilot;

public static class NodalOsWorkspaceSelectionRuntime
{
    public const string MetadataPathEnvironmentVariable = "NODAL_OS_WORKSPACE_SELECTION_METADATA_PATH";
    public const string SecretRootEnvironmentVariable = "NODAL_OS_WORKSPACE_SELECTION_SECRET_ROOT";

    public static NodalOsWorkspaceSelectionService CreateDefault()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var productRoot = Path.Combine(localAppData, "NodalOS");
        var metadataPath = Environment.GetEnvironmentVariable(MetadataPathEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(metadataPath))
            metadataPath = Path.Combine(productRoot, "workspaces", "selection.v1.json");

        var secretRoot = Environment.GetEnvironmentVariable(SecretRootEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(secretRoot))
            secretRoot = Path.Combine(productRoot, "secrets", "workspace-roots");

        return new NodalOsWorkspaceSelectionService(
            metadataPath,
            new WindowsDpapiSecretReferenceStore(secretRoot));
    }
}
