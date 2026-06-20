using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsExclusionPolicyPackService
{
    public NodalOsExclusionPolicyPack CreatePack(
        string workspaceRef = "workspace-ref-m541",
        string missionRef = "mission-ref-m541",
        string scopePreviewRef = "scope-preview-contract-m538")
    {
        var rules = Enum.GetValues<NodalOsExclusionPolicyGroup>()
            .Select((group, index) => CreateRule(group, index + 1))
            .ToArray();

        return new()
        {
            ExclusionPolicyId = "exclusion-policy-pack-m541",
            WorkspaceRef = workspaceRef,
            MissionRef = missionRef,
            ScopePreviewRef = scopePreviewRef,
            IsPreviewOnly = true,
            UsesRealFilesystem = false,
            DirectoryListingPerformed = false,
            FileReadPerformed = false,
            Rules = rules
        };
    }

    public NodalOsExclusionPolicyReadinessResult Evaluate(NodalOsExclusionPolicyPack pack) =>
        new()
        {
            ReadinessId = "exclusion-policy-readiness-m541",
            ExclusionPolicyRef = pack.ExclusionPolicyId,
            ReadyForRealExclusionEnforcement = false,
            ReadyForRealScan = false,
            ReadyForIndexing = false,
            ReadyForVectorization = false,
            CanReadDirectory = false,
            CanReadFile = false,
            CanApplyToRealFilesystem = false,
            CanCreateIndex = false,
            CanBuildLlmContext = false,
            MissingRequirementsRedacted =
            [
                "Future path jail integration.",
                "Future consent confirmation.",
                "Future audit of exclusion enforcement."
            ],
            BlockersRedacted =
            [
                "Policy pack is preview-only.",
                "No real folder or content inspection is performed.",
                "No index or LLM context can be built from this pack."
            ],
            UserFacingExplanationRedacted = "Exclusion policy is defined as a preview pack only. It does not apply rules to a real filesystem and cannot build index or model context."
        };

    public string RenderHtml(NodalOsExclusionPolicyPack pack, NodalOsExclusionPolicyReadinessResult readiness) =>
        $"""
        <section class="panel">
          <h2>M541 Exclusion Policy Pack</h2>
          <p>Policy: {pack.ExclusionPolicyId}; rules: {pack.Rules.Count}</p>
          <p>UsesRealFilesystem={pack.UsesRealFilesystem}; DirectoryListingPerformed={pack.DirectoryListingPerformed}</p>
          <p>ReadyForRealExclusionEnforcement={readiness.ReadyForRealExclusionEnforcement}</p>
        </section>
        """;

    private static NodalOsExclusionRulePreview CreateRule(NodalOsExclusionPolicyGroup group, int index)
    {
        var sensitive = group is NodalOsExclusionPolicyGroup.EnvironmentFiles or NodalOsExclusionPolicyGroup.SecretLikeFiles;
        return new()
        {
            RuleId = $"exclusion-rule-{index:000}",
            Group = group,
            PatternDisplayRedacted = DisplayFor(group),
            ReasonRedacted = $"Preview exclusion for {group}.",
            Severity = sensitive ? NodalOsExclusionSeverity.Required : NodalOsExclusionSeverity.Review,
            CanUserOverride = !sensitive,
            RequiresReview = true,
            EvidenceRefs = ["evidence-exclusion-policy-ref-only"],
            TimelineRefs = ["timeline-exclusion-policy-m541"]
        };
    }

    private static string DisplayFor(NodalOsExclusionPolicyGroup group) =>
        group switch
        {
            NodalOsExclusionPolicyGroup.DependencyFolders => "dependency-folders/**",
            NodalOsExclusionPolicyGroup.BuildOutputs => "build-outputs/**",
            NodalOsExclusionPolicyGroup.CacheFolders => "cache-folders/**",
            NodalOsExclusionPolicyGroup.VcsMetadata => "vcs-metadata/**",
            NodalOsExclusionPolicyGroup.BinaryMediaHeavyFolders => "binary-media-heavy/**",
            NodalOsExclusionPolicyGroup.EnvironmentFiles => "environment-files",
            NodalOsExclusionPolicyGroup.SecretLikeFiles => "sensitive-marker-files",
            NodalOsExclusionPolicyGroup.GeneratedArtifacts => "generated-artifacts/**",
            NodalOsExclusionPolicyGroup.Logs => "logs/**",
            NodalOsExclusionPolicyGroup.TemporaryFiles => "temporary-files/**",
            NodalOsExclusionPolicyGroup.VendorFolders => "vendor-folders/**",
            NodalOsExclusionPolicyGroup.NodeModulesLikeFolders => "node-modules-like/**",
            NodalOsExclusionPolicyGroup.BinObjLikeFolders => "bin-obj-like/**",
            _ => "unknown-policy-group"
        };
}

public sealed class NodalOsExclusionPolicyPackJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializePack(NodalOsExclusionPolicyPack pack) =>
        JsonSerializer.Serialize(pack, Options);

    public string SerializeReadiness(NodalOsExclusionPolicyReadinessResult readiness) =>
        JsonSerializer.Serialize(readiness, Options);
}

public static class NodalOsExclusionPolicyPackFixtures
{
    public static NodalOsExclusionPolicyPack Pack() =>
        new NodalOsExclusionPolicyPackService().CreatePack();

    public static NodalOsExclusionPolicyReadinessResult Readiness()
    {
        var service = new NodalOsExclusionPolicyPackService();
        return service.Evaluate(service.CreatePack());
    }
}
