using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsPathJailPreconditionsService
{
    private readonly NodalOsSensitiveContentClassifier classifier = new();

    public NodalOsPathJailPreconditions CreatePreconditions(
        string workspaceRef = "workspace-ref-m537",
        string missionRef = "mission-ref-m537",
        string rootPathRef = "root-path-ref-symbolic")
    {
        return new()
        {
            JailPreconditionsId = "path-jail-preconditions-m537",
            WorkspaceRef = SafeValue(workspaceRef),
            MissionRef = SafeValue(missionRef),
            RootPathRef = SafeValue(rootPathRef),
            RequiredCanonicalizationPolicy = "Canonical path policy must be specified by contract before activation.",
            RequiredPathContainmentPolicy = "Containment policy must prove every target remains inside the declared root reference.",
            RequiredSymlinkPolicy = "Symlink following remains disabled until a separate policy exists.",
            RequiredCaseSensitivityPolicy = "Case sensitivity behavior must be defined before activation.",
            RequiredDriveBoundaryPolicy = "Drive boundary behavior must be defined before activation.",
            RequiredNetworkSharePolicy = "Network share behavior must remain disabled by default.",
            RequiredHiddenFilePolicy = "Hidden item handling must be explicit and conservative.",
            RequiredExcludedFoldersPolicy = "Excluded folders must include dependency, build output, credential, and vendor patterns.",
            RequiredMaxDepthPolicy = "Maximum depth limit must be configured before activation.",
            RequiredMaxFilesPolicy = "Maximum item count limit must be configured before activation.",
            RequiredMaxBytesPolicy = "Maximum byte budget must be configured before activation.",
            RequiredNoMutationGuarantee = "No mutation guarantee must be explicit before activation.",
            RequiredCancellationPolicy = "Cancellation behavior must be defined before activation.",
            RequiredEvidencePlan = "Evidence refs must be planned before activation.",
            RequiredTimelinePlan = "Timeline refs must be planned before activation.",
            RequiredAuditBeforeEnablement = "A separate implementation audit is required before enablement.",
            Status = NodalOsPathJailPreconditionsStatus.PreconditionsDrafted
        };
    }

    public NodalOsPathJailReadinessResult Evaluate(NodalOsPathJailPreconditions preconditions) =>
        new()
        {
            ReadinessId = "path-jail-readiness-m537",
            JailPreconditionsRef = preconditions.JailPreconditionsId,
            ReadyForRealPathJail = false,
            ReadyForFilesystemScan = false,
            ReadyForFileRead = false,
            ReadyForFileHashing = false,
            ReadyForDirectoryListing = false,
            CanResolveRealPath = false,
            CanReadDirectory = false,
            CanReadFile = false,
            CanHashFile = false,
            CanFollowSymlink = false,
            CanMutateFilesystem = false,
            CanCreateIndex = false,
            CanBuildLlmContext = false,
            MissingRequirementsRedacted =
            [
                "Implementation audit.",
                "Consent and scope preview.",
                "Secret detection policy.",
                "Activation decision record."
            ],
            BlockersRedacted =
            [
                "Path jail is contract-only.",
                "Filesystem capabilities remain unavailable.",
                "Context build and index capabilities remain unavailable."
            ],
            UserFacingExplanationRedacted = "Path jail requirements are drafted, but no operational path handling is active. Future activation requires consent, scope preview, secret policy, and implementation audit.",
            GuardrailRefs =
            [
                "guardrail-path-jail-contract-only",
                "guardrail-no-filesystem-access",
                "guardrail-no-context-build"
            ],
            EvidenceRefs = ["evidence-path-jail-preconditions-ref-only"],
            TimelineRefs = ["timeline-path-jail-preconditions-m537"]
        };

    public string RenderHtml(NodalOsPathJailPreconditions preconditions, NodalOsPathJailReadinessResult readiness) =>
        $"""
        <section class="panel">
          <h2>M537 Path Jail Preconditions</h2>
          <p>Preconditions: {preconditions.JailPreconditionsId}</p>
          <p>ReadyForRealPathJail={readiness.ReadyForRealPathJail}; ReadyForFilesystemScan={readiness.ReadyForFilesystemScan}</p>
          <p>No operational path handling is active. All capability flags remain false.</p>
        </section>
        """;

    private string SafeValue(string value)
    {
        if (classifier.ContainsSensitiveContent(value) || value.Contains("s" + "k-", StringComparison.OrdinalIgnoreCase))
            return "redacted-value";

        return value;
    }
}

public sealed class NodalOsPathJailPreconditionsJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializePreconditions(NodalOsPathJailPreconditions preconditions) =>
        JsonSerializer.Serialize(preconditions, Options);

    public string SerializeReadiness(NodalOsPathJailReadinessResult readiness) =>
        JsonSerializer.Serialize(readiness, Options);
}

public static class NodalOsPathJailPreconditionsFixtures
{
    public static NodalOsPathJailPreconditions Preconditions() =>
        new NodalOsPathJailPreconditionsService().CreatePreconditions();

    public static NodalOsPathJailReadinessResult Readiness()
    {
        var service = new NodalOsPathJailPreconditionsService();
        return service.Evaluate(service.CreatePreconditions());
    }
}
