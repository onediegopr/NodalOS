using OneBrain.AgentOperations.Core.Models;
using OneBrain.Core.Models;

namespace OneBrain.Pilot;

public static class NodalOsByokModelConfigurationRuntime
{
    private static readonly HttpClient SharedHttpClient = new();

    public const string MetadataPathEnvironmentVariable = "NODAL_OS_BYOK_MODEL_METADATA_PATH";
    public const string SecretRootEnvironmentVariable = "NODAL_OS_BYOK_MODEL_SECRET_ROOT";

    public static NodalOsByokModelConfigurationService CreateDefault()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var productRoot = Path.Combine(localAppData, "NodalOS");
        var metadataPath = Environment.GetEnvironmentVariable(MetadataPathEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(metadataPath))
            metadataPath = Path.Combine(productRoot, "models", "byok.v1.json");

        var secretRoot = Environment.GetEnvironmentVariable(SecretRootEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(secretRoot))
            secretRoot = Path.Combine(productRoot, "secrets", "model-credentials");

        return new NodalOsByokModelConfigurationService(
            metadataPath,
            new WindowsDpapiSecretReferenceStore(secretRoot),
            SharedHttpClient);
    }
}
