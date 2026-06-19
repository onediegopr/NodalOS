using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public static class NodalOsPackageSkillManifestJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string SerializePackage(NodalOsPackageManifest manifest) =>
        JsonSerializer.Serialize(manifest, Options);

    public static NodalOsPackageManifest DeserializePackage(string json) =>
        JsonSerializer.Deserialize<NodalOsPackageManifest>(json, Options) ??
            throw new InvalidOperationException("Package manifest JSON did not deserialize.");

    public static string SerializeSkill(NodalOsSkillManifest manifest) =>
        JsonSerializer.Serialize(manifest, Options);

    public static NodalOsSkillManifest DeserializeSkill(string json) =>
        JsonSerializer.Deserialize<NodalOsSkillManifest>(json, Options) ??
            throw new InvalidOperationException("Skill manifest JSON did not deserialize.");
}

public sealed class NodalOsPackageSkillManifestValidator
{
    private readonly NodalOsRedactionService redaction;

    public NodalOsPackageSkillManifestValidator()
        : this(new NodalOsRedactionService())
    {
    }

    public NodalOsPackageSkillManifestValidator(NodalOsRedactionService redaction) =>
        this.redaction = redaction;

    public NodalOsPackageSkillManifestValidationResult ValidatePackage(NodalOsPackageManifest manifest)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, manifest.PackageId, "PackageId is required.");
        AddRequired(errors, manifest.Name, "Name is required.");
        AddRequired(errors, manifest.Version, "Version is required.");
        AddRequired(errors, manifest.Publisher, "Publisher is required.");
        AddRequired(errors, manifest.Provenance, "Provenance is required.");

        if (!manifest.InternalOnly)
            errors.Add("Package manifests must be InternalOnly in V1.");

        ValidateRuntimeFlags(
            manifest.RuntimeExecutionAllowed,
            manifest.RuntimeExecutionDeferred,
            manifest.RequiresGlobalPolicyEvaluation,
            "Package",
            errors);

        ValidatePackageStatus(manifest, errors, warnings);

        if (manifest.Skills.Count == 0)
        {
            if (manifest.Status == NodalOsPackageStatus.Draft)
                warnings.Add("Draft package manifests may be empty, but at least one skill is recommended.");
            else
                errors.Add("Non-draft package manifests require at least one skill.");
        }

        foreach (var skill in manifest.Skills)
        {
            var skillResult = ValidateSkill(skill);
            errors.AddRange(skillResult.Errors.Select(error => $"Skill {skill.SkillId}: {error}"));
            warnings.AddRange(skillResult.Warnings.Select(warning => $"Skill {skill.SkillId}: {warning}"));
        }

        if (ContainsSensitiveContent(PackageValues(manifest)))
            errors.Add("Package manifest contains sensitive or secret-like content.");

        var canPassCatalogPolicy = errors.Count == 0 &&
            manifest.Status is NodalOsPackageStatus.InternalPreview or NodalOsPackageStatus.ApprovedForCatalog;

        return Result(errors, warnings, canPassCatalogPolicy);
    }

    public NodalOsPackageSkillManifestValidationResult ValidateSkill(NodalOsSkillManifest manifest)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, manifest.SkillId, "SkillId is required.");
        AddRequired(errors, manifest.Name, "Name is required.");
        AddRequired(errors, manifest.Version, "Version is required.");

        if (!manifest.InternalOnly)
            errors.Add("Skill manifests must be InternalOnly in V1.");

        ValidateRuntimeFlags(
            manifest.RuntimeExecutionAllowed,
            manifest.RuntimeExecutionDeferred,
            manifest.RequiresGlobalPolicyEvaluation,
            "Skill",
            errors);

        ValidateSkillStatus(manifest, errors, warnings);
        ValidateSkillCapabilities(manifest, errors, warnings);

        if (ContainsSensitiveContent(SkillValues(manifest)))
            errors.Add("Skill manifest contains sensitive or secret-like content.");

        var canPassCatalogPolicy = errors.Count == 0 &&
            manifest.Status is NodalOsSkillStatus.InternalPreview or NodalOsSkillStatus.ApprovedForCatalog;

        return Result(errors, warnings, canPassCatalogPolicy);
    }

    public bool ContainsSensitiveContent(string? value) =>
        redaction.ContainsSensitiveContent(value);

    public bool ContainsSensitiveContent(IEnumerable<string?> values) =>
        values.Any(value => !string.IsNullOrWhiteSpace(value) && ContainsSensitiveContent(value));

    public NodalOsPackageSkillManifestValidationResult ValidateCatalogPolicy(NodalOsPackageManifest manifest) =>
        ValidatePackage(manifest);

    public NodalOsPackageSkillManifestValidationResult ValidateCatalogPolicy(NodalOsSkillManifest manifest) =>
        ValidateSkill(manifest);

    private static void ValidateRuntimeFlags(
        bool runtimeExecutionAllowed,
        bool runtimeExecutionDeferred,
        bool requiresGlobalPolicyEvaluation,
        string subject,
        List<string> errors)
    {
        if (runtimeExecutionAllowed)
            errors.Add($"{subject} manifest cannot grant runtime execution in V1.");

        if (!runtimeExecutionDeferred)
            errors.Add($"{subject} manifest must defer runtime execution in V1.");

        if (!requiresGlobalPolicyEvaluation)
            errors.Add($"{subject} manifest must require global policy evaluation.");
    }

    private static void ValidatePackageStatus(
        NodalOsPackageManifest manifest,
        List<string> errors,
        List<string> warnings)
    {
        switch (manifest.Status)
        {
            case NodalOsPackageStatus.Draft:
                warnings.Add("Draft package manifests are design artifacts only.");
                break;
            case NodalOsPackageStatus.InternalPreview:
                warnings.Add("InternalPreview package can pass catalog policy only; runtime execution remains deferred.");
                break;
            case NodalOsPackageStatus.ApprovedForCatalog:
                warnings.Add("ApprovedForCatalog is catalog governance only and does not grant runtime execution.");
                break;
            case NodalOsPackageStatus.Deprecated:
                errors.Add("Deprecated package manifests cannot pass catalog policy in V1.");
                break;
            case NodalOsPackageStatus.Blocked:
                errors.Add("Blocked package manifests cannot pass catalog policy.");
                break;
        }
    }

    private static void ValidateSkillStatus(
        NodalOsSkillManifest manifest,
        List<string> errors,
        List<string> warnings)
    {
        switch (manifest.Status)
        {
            case NodalOsSkillStatus.Draft:
                warnings.Add("Draft skill manifests are design artifacts only.");
                break;
            case NodalOsSkillStatus.InternalPreview:
                warnings.Add("InternalPreview skill can pass catalog policy only; runtime execution remains deferred.");
                break;
            case NodalOsSkillStatus.ApprovedForCatalog:
                warnings.Add("ApprovedForCatalog is catalog governance only and does not grant runtime execution.");
                break;
            case NodalOsSkillStatus.Deprecated:
                errors.Add("Deprecated skill manifests cannot pass catalog policy in V1.");
                break;
            case NodalOsSkillStatus.Blocked:
                errors.Add("Blocked skill manifests cannot pass catalog policy.");
                break;
        }
    }

    private static void ValidateSkillCapabilities(
        NodalOsSkillManifest manifest,
        List<string> errors,
        List<string> warnings)
    {
        if (manifest.Capabilities.Count == 0 && manifest.Status != NodalOsSkillStatus.Draft)
            errors.Add("Non-draft skill manifests require at least one capability.");

        if (manifest.RiskLevel is NodalOsSkillRiskLevel.High or NodalOsSkillRiskLevel.Critical &&
            manifest.RequiredApprovals.Count == 0)
            errors.Add("High or Critical risk skills require approval metadata.");

        if (manifest.Capabilities.Contains(NodalOsSkillCapabilityKind.FileTransfer) &&
            manifest.RequiredApprovals.Count == 0)
            errors.Add("FileTransfer capability requires approval metadata.");

        if (manifest.Capabilities.Contains(NodalOsSkillCapabilityKind.DataEntry) &&
            manifest.RequiredApprovals.Count == 0)
            errors.Add("DataEntry capability requires approval metadata.");

        if (manifest.Capabilities.Contains(NodalOsSkillCapabilityKind.Interaction) &&
            manifest.RequiredApprovals.Count == 0)
            warnings.Add("Interaction capability should declare approval metadata before runtime integration.");

        if (manifest.Capabilities.Contains(NodalOsSkillCapabilityKind.Navigation) &&
            manifest.Status != NodalOsSkillStatus.Draft &&
            manifest.AllowedDomains.Count == 0)
            errors.Add("Navigation capability requires AllowedDomains for non-draft skills.");

        if (manifest.Status != NodalOsSkillStatus.Draft && manifest.EvidenceRequirements.Count == 0)
            warnings.Add("EvidenceRequirements are recommended for non-draft skills.");
    }

    private static NodalOsPackageSkillManifestValidationResult Result(
        IReadOnlyList<string> errors,
        IReadOnlyList<string> warnings,
        bool canPassCatalogPolicy) =>
        new()
        {
            IsValid = errors.Count == 0,
            CanPassCatalogPolicy = canPassCatalogPolicy,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            Errors = errors,
            Warnings = warnings
        };

    private static IEnumerable<string?> PackageValues(NodalOsPackageManifest manifest)
    {
        yield return manifest.PackageId;
        yield return manifest.Name;
        yield return manifest.Description;
        yield return manifest.Version;
        yield return manifest.Publisher;
        yield return manifest.Provenance;

        foreach (var value in manifest.Tags)
            yield return value;

        foreach (var value in manifest.EvidenceRequirements)
            yield return value;
    }

    private static IEnumerable<string?> SkillValues(NodalOsSkillManifest manifest)
    {
        yield return manifest.SkillId;
        yield return manifest.Name;
        yield return manifest.Description;
        yield return manifest.Version;

        foreach (var value in manifest.AllowedDomains)
            yield return value;

        foreach (var value in manifest.RequiredApprovals)
            yield return value;

        foreach (var value in manifest.EvidenceRequirements)
            yield return value;

        foreach (var value in manifest.RelatedRecipeIds)
            yield return value;

        foreach (var value in manifest.RelatedStepKinds)
            yield return value;
    }

    private static void AddRequired(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(message);
    }
}

public static class NodalOsPackageSkillManifestFixtures
{
    public static readonly DateTimeOffset FixedTimestamp = new(2026, 6, 19, 0, 0, 0, TimeSpan.Zero);

    public static NodalOsPackageManifest InternalReadOnlyPackage() =>
        BasePackage(
            packageId: "pkg-internal-readonly-001",
            name: "Internal read-only package",
            status: NodalOsPackageStatus.ApprovedForCatalog,
            skills: [ReadOnlySkill()]);

    public static NodalOsPackageManifest DraftEmptyPackage() =>
        BasePackage(
            packageId: "pkg-draft-empty-001",
            name: "Draft empty package",
            status: NodalOsPackageStatus.Draft,
            skills: []);

    public static NodalOsPackageManifest BlockedPackage() =>
        InternalReadOnlyPackage() with
        {
            PackageId = "pkg-blocked-001",
            Name = "Blocked package",
            Status = NodalOsPackageStatus.Blocked
        };

    public static NodalOsPackageManifest DeprecatedPackage() =>
        InternalReadOnlyPackage() with
        {
            PackageId = "pkg-deprecated-001",
            Name = "Deprecated package",
            Status = NodalOsPackageStatus.Deprecated
        };

    public static NodalOsSkillManifest ReadOnlySkill() =>
        BaseSkill(
            skillId: "skill-readonly-001",
            name: "Read-only reporting skill",
            status: NodalOsSkillStatus.ApprovedForCatalog,
            capabilities:
            [
                NodalOsSkillCapabilityKind.ReadOnly,
                NodalOsSkillCapabilityKind.Reporting,
                NodalOsSkillCapabilityKind.EvidenceProcessing
            ],
            riskLevel: NodalOsSkillRiskLevel.Low,
            allowedDomains: [],
            requiredApprovals: []);

    public static NodalOsSkillManifest NavigationSkill() =>
        BaseSkill(
            skillId: "skill-navigation-001",
            name: "Navigation catalog skill",
            status: NodalOsSkillStatus.InternalPreview,
            capabilities: [NodalOsSkillCapabilityKind.Navigation, NodalOsSkillCapabilityKind.ReadOnly],
            riskLevel: NodalOsSkillRiskLevel.Medium,
            allowedDomains: ["example.invalid"],
            requiredApprovals: ["human-review-before-runtime"]);

    public static NodalOsSkillManifest FileTransferSkill() =>
        BaseSkill(
            skillId: "skill-file-transfer-001",
            name: "File transfer request skill",
            status: NodalOsSkillStatus.InternalPreview,
            capabilities: [NodalOsSkillCapabilityKind.FileTransfer],
            riskLevel: NodalOsSkillRiskLevel.High,
            allowedDomains: ["example.invalid"],
            requiredApprovals: ["human-file-transfer-approval"]);

    public static NodalOsSkillManifest DataEntrySkill() =>
        BaseSkill(
            skillId: "skill-data-entry-001",
            name: "Data entry request skill",
            status: NodalOsSkillStatus.InternalPreview,
            capabilities: [NodalOsSkillCapabilityKind.DataEntry],
            riskLevel: NodalOsSkillRiskLevel.High,
            allowedDomains: ["example.invalid"],
            requiredApprovals: ["human-data-entry-approval"]);

    private static NodalOsPackageManifest BasePackage(
        string packageId,
        string name,
        NodalOsPackageStatus status,
        IReadOnlyList<NodalOsSkillManifest> skills) =>
        new()
        {
            PackageId = packageId,
            Name = name,
            Description = "Package Manifest V1 fixture; no runtime execution.",
            Version = "1.0.0",
            Status = status,
            Publisher = "NODAL OS internal",
            Provenance = "internal-fixture",
            Skills = skills,
            Tags = ["agent-operations", "internal-only"],
            EvidenceRequirements = ["manifest-review", "policy-review"],
            InternalOnly = true,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            CreatedAt = FixedTimestamp,
            UpdatedAt = FixedTimestamp
        };

    private static NodalOsSkillManifest BaseSkill(
        string skillId,
        string name,
        NodalOsSkillStatus status,
        IReadOnlyList<NodalOsSkillCapabilityKind> capabilities,
        NodalOsSkillRiskLevel riskLevel,
        IReadOnlyList<string> allowedDomains,
        IReadOnlyList<string> requiredApprovals) =>
        new()
        {
            SkillId = skillId,
            Name = name,
            Description = "Skill Manifest V1 fixture; catalog metadata only.",
            Version = "1.0.0",
            Status = status,
            Capabilities = capabilities,
            RiskLevel = riskLevel,
            AllowedDomains = allowedDomains,
            RequiredApprovals = requiredApprovals,
            EvidenceRequirements = ["run-report", "evidence-ref-bridge"],
            RelatedRecipeIds = ["recipe-read-only-001"],
            RelatedStepKinds = ["Read", "Extract"],
            InternalOnly = true,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true
        };
}
