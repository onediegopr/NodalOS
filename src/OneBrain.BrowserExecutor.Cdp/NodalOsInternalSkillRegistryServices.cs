using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsInternalSkillRegistryBuilder
{
    private readonly List<NodalOsSkillRegistryEntry> entries = [];
    private readonly List<string> warnings = [];

    public static IReadOnlyList<NodalOsSkillRegistryEntry> FromPackageManifest(NodalOsPackageManifest package)
    {
        var entries = new List<NodalOsSkillRegistryEntry>
        {
            PackageEntry(package)
        };

        entries.AddRange(package.Skills.Select(skill => SkillEntry(package, skill)));
        return entries;
    }

    public NodalOsInternalSkillRegistryBuilder AddPackage(NodalOsPackageManifest package)
    {
        if (package.RuntimeExecutionAllowed ||
            !package.RuntimeExecutionDeferred ||
            !package.RequiresGlobalPolicyEvaluation ||
            package.Skills.Any(skill =>
                skill.RuntimeExecutionAllowed ||
                !skill.RuntimeExecutionDeferred ||
                !skill.RequiresGlobalPolicyEvaluation))
        {
            warnings.Add($"Package {package.PackageId} declared unsafe runtime flags; registry entries were normalized to execution-deferred metadata.");
        }

        entries.AddRange(FromPackageManifest(package));
        return this;
    }

    public NodalOsInternalSkillRegistrySnapshot BuildSnapshot(
        string registryId,
        string version,
        DateTimeOffset createdAt) =>
        new()
        {
            RegistryId = registryId,
            Version = version,
            Entries = entries.ToArray(),
            CreatedAt = createdAt
        };

    public NodalOsInternalSkillRegistryBuildResult BuildValidatedSnapshot(
        string registryId,
        string version,
        DateTimeOffset createdAt)
    {
        var snapshot = BuildSnapshot(registryId, version, createdAt);
        var validation = new NodalOsInternalSkillRegistryValidator().ValidateSnapshot(snapshot);

        return new()
        {
            Snapshot = snapshot,
            Validation = validation with
            {
                Warnings = validation.Warnings.Concat(warnings).Distinct(StringComparer.Ordinal).ToArray()
            }
        };
    }

    private static NodalOsSkillRegistryEntry PackageEntry(NodalOsPackageManifest package) =>
        new()
        {
            EntryId = $"package:{package.PackageId}",
            Kind = NodalOsRegistryEntryKind.Package,
            PackageId = package.PackageId,
            Name = package.Name,
            Description = package.Description,
            Version = package.Version,
            Status = MapStatus(package.Status),
            Provenance = package.Provenance,
            InternalOnly = package.InternalOnly,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            Tags = package.Tags,
            EvidenceRequirements = package.EvidenceRequirements,
            CreatedAt = package.CreatedAt,
            UpdatedAt = package.UpdatedAt
        };

    private static NodalOsSkillRegistryEntry SkillEntry(
        NodalOsPackageManifest package,
        NodalOsSkillManifest skill) =>
        new()
        {
            EntryId = $"skill:{package.PackageId}:{skill.SkillId}",
            Kind = NodalOsRegistryEntryKind.Skill,
            PackageId = package.PackageId,
            SkillId = skill.SkillId,
            Name = skill.Name,
            Description = skill.Description,
            Version = skill.Version,
            Status = MapStatus(skill.Status),
            Provenance = package.Provenance,
            InternalOnly = package.InternalOnly && skill.InternalOnly,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            Capabilities = skill.Capabilities,
            RiskLevel = skill.RiskLevel,
            Tags = package.Tags,
            EvidenceRequirements = package.EvidenceRequirements.Concat(skill.EvidenceRequirements).Distinct(StringComparer.Ordinal).ToArray(),
            CreatedAt = package.CreatedAt,
            UpdatedAt = package.UpdatedAt
        };

    private static NodalOsRegistryEntryStatus MapStatus(NodalOsPackageStatus status) =>
        status switch
        {
            NodalOsPackageStatus.Draft => NodalOsRegistryEntryStatus.Draft,
            NodalOsPackageStatus.InternalPreview or NodalOsPackageStatus.ApprovedForCatalog => NodalOsRegistryEntryStatus.Visible,
            NodalOsPackageStatus.Deprecated => NodalOsRegistryEntryStatus.Deprecated,
            NodalOsPackageStatus.Blocked => NodalOsRegistryEntryStatus.Blocked,
            _ => NodalOsRegistryEntryStatus.Hidden
        };

    private static NodalOsRegistryEntryStatus MapStatus(NodalOsSkillStatus status) =>
        status switch
        {
            NodalOsSkillStatus.Draft => NodalOsRegistryEntryStatus.Draft,
            NodalOsSkillStatus.InternalPreview or NodalOsSkillStatus.ApprovedForCatalog => NodalOsRegistryEntryStatus.Visible,
            NodalOsSkillStatus.Deprecated => NodalOsRegistryEntryStatus.Deprecated,
            NodalOsSkillStatus.Blocked => NodalOsRegistryEntryStatus.Blocked,
            _ => NodalOsRegistryEntryStatus.Hidden
        };
}

public sealed class NodalOsInternalSkillRegistryValidator
{
    private readonly NodalOsRedactionService redaction;

    public NodalOsInternalSkillRegistryValidator()
        : this(new NodalOsRedactionService())
    {
    }

    public NodalOsInternalSkillRegistryValidator(NodalOsRedactionService redaction) =>
        this.redaction = redaction;

    public NodalOsInternalSkillRegistryValidationResult ValidateEntry(NodalOsSkillRegistryEntry entry)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, entry.EntryId, "EntryId is required.");
        AddRequired(errors, entry.PackageId, "PackageId is required.");
        AddRequired(errors, entry.Name, "Name is required.");
        AddRequired(errors, entry.Version, "Version is required.");
        AddRequired(errors, entry.Provenance, "Provenance is required.");

        if (!entry.InternalOnly)
            errors.Add("Registry entries must be InternalOnly in V1.");

        ValidateNoRuntimePermission(entry, errors);
        ValidateKind(entry, errors);

        if (entry.Kind == NodalOsRegistryEntryKind.Skill &&
            entry.RiskLevel is NodalOsSkillRiskLevel.High or NodalOsSkillRiskLevel.Critical &&
            entry.EvidenceRequirements.Count == 0)
            errors.Add("High or Critical risk skill registry entries require evidence requirements.");

        if (entry.Kind == NodalOsRegistryEntryKind.Skill &&
            entry.RiskLevel is NodalOsSkillRiskLevel.High or NodalOsSkillRiskLevel.Critical)
            warnings.Add("High or Critical risk skill approvals must remain validated at the source manifest boundary.");

        if (ContainsSensitiveContent(EntryValues(entry)))
            errors.Add("Registry entry contains sensitive or secret-like content.");

        return new NodalOsInternalSkillRegistryValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    public NodalOsInternalSkillRegistryValidationResult ValidateSnapshot(NodalOsInternalSkillRegistrySnapshot snapshot)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, snapshot.RegistryId, "RegistryId is required.");
        AddRequired(errors, snapshot.Version, "Version is required.");

        var duplicateEntryIds = snapshot.Entries
            .GroupBy(entry => entry.EntryId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var entryId in duplicateEntryIds)
            errors.Add($"Duplicate EntryId: {entryId}.");

        var duplicatePackageSkill = snapshot.Entries
            .GroupBy(entry => $"{entry.Kind}:{entry.PackageId}:{entry.SkillId}", StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var key in duplicatePackageSkill)
            errors.Add($"Duplicate package/skill registry combination: {key}.");

        foreach (var entry in snapshot.Entries)
        {
            var result = ValidateEntry(entry);
            errors.AddRange(result.Errors.Select(error => $"{entry.EntryId}: {error}"));
            warnings.AddRange(result.Warnings.Select(warning => $"{entry.EntryId}: {warning}"));
        }

        return new NodalOsInternalSkillRegistryValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    public NodalOsInternalSkillRegistryValidationResult ValidateNoRuntimePermission(
        NodalOsSkillRegistryEntry entry)
    {
        var errors = new List<string>();
        ValidateNoRuntimePermission(entry, errors);

        return new NodalOsInternalSkillRegistryValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = []
        };
    }

    private static void ValidateNoRuntimePermission(NodalOsSkillRegistryEntry entry, List<string> errors)
    {
        if (entry.RuntimeExecutionAllowed)
            errors.Add("Registry entries cannot grant runtime execution.");

        if (!entry.RuntimeExecutionDeferred)
            errors.Add("Registry entries must defer runtime execution in V1.");

        if (!entry.RequiresGlobalPolicyEvaluation)
            errors.Add("Registry entries must require global policy evaluation.");
    }

    private static void ValidateKind(NodalOsSkillRegistryEntry entry, List<string> errors)
    {
        if (entry.Kind == NodalOsRegistryEntryKind.Skill && string.IsNullOrWhiteSpace(entry.SkillId))
            errors.Add("Skill registry entries require SkillId.");

        if (entry.Kind == NodalOsRegistryEntryKind.Package && !string.IsNullOrWhiteSpace(entry.SkillId))
            errors.Add("Package registry entries must not include SkillId.");
    }

    private bool ContainsSensitiveContent(IEnumerable<string?> values) =>
        values.Any(value => !string.IsNullOrWhiteSpace(value) && redaction.ContainsSensitiveContent(value));

    private static IEnumerable<string?> EntryValues(NodalOsSkillRegistryEntry entry)
    {
        yield return entry.EntryId;
        yield return entry.PackageId;
        yield return entry.SkillId;
        yield return entry.Name;
        yield return entry.Description;
        yield return entry.Version;
        yield return entry.Provenance;

        foreach (var value in entry.Tags)
            yield return value;

        foreach (var value in entry.EvidenceRequirements)
            yield return value;
    }

    private static void AddRequired(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(message);
    }
}

public sealed class NodalOsInternalSkillRegistryQueryService
{
    private readonly NodalOsInternalSkillRegistrySnapshot snapshot;

    public NodalOsInternalSkillRegistryQueryService(NodalOsInternalSkillRegistrySnapshot snapshot) =>
        this.snapshot = snapshot;

    public IReadOnlyList<NodalOsSkillRegistryEntry> FindByPackageId(string packageId) =>
        snapshot.Entries
            .Where(entry => entry.PackageId.Equals(packageId, StringComparison.Ordinal))
            .ToArray();

    public IReadOnlyList<NodalOsSkillRegistryEntry> FindBySkillId(string skillId) =>
        snapshot.Entries
            .Where(entry => entry.SkillId?.Equals(skillId, StringComparison.Ordinal) == true)
            .ToArray();

    public IReadOnlyList<NodalOsSkillRegistryEntry> Query(NodalOsSkillRegistryQuery query)
    {
        IEnumerable<NodalOsSkillRegistryEntry> result = snapshot.Entries;

        if (query.VisibleOnly)
            result = result.Where(entry => entry.Status == NodalOsRegistryEntryStatus.Visible);

        if (!string.IsNullOrWhiteSpace(query.PackageId))
            result = result.Where(entry => entry.PackageId.Equals(query.PackageId, StringComparison.Ordinal));

        if (!string.IsNullOrWhiteSpace(query.SkillId))
            result = result.Where(entry => entry.SkillId?.Equals(query.SkillId, StringComparison.Ordinal) == true);

        if (query.Kind is { } kind)
            result = result.Where(entry => entry.Kind == kind);

        if (query.Status is { } status)
            result = result.Where(entry => entry.Status == status);

        if (query.MaxRiskLevel is { } maxRisk)
            result = result.Where(entry => entry.RiskLevel is null || entry.RiskLevel <= maxRisk);

        foreach (var capability in query.RequiredCapabilities)
            result = result.Where(entry => entry.Capabilities.Contains(capability));

        return result.ToArray();
    }

    public IReadOnlyList<NodalOsSkillRegistryEntry> GetVisibleEntries() =>
        Query(new NodalOsSkillRegistryQuery { VisibleOnly = true });

    public IReadOnlyList<NodalOsSkillRegistryEntry> GetBlockedEntries() =>
        snapshot.Entries.Where(entry => entry.Status == NodalOsRegistryEntryStatus.Blocked).ToArray();

    public IReadOnlyList<NodalOsSkillRegistryEntry> GetDeprecatedEntries() =>
        snapshot.Entries.Where(entry => entry.Status == NodalOsRegistryEntryStatus.Deprecated).ToArray();
}

public static class NodalOsInternalSkillRegistryJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string SerializeSnapshot(NodalOsInternalSkillRegistrySnapshot snapshot) =>
        JsonSerializer.Serialize(snapshot, Options);

    public static NodalOsInternalSkillRegistrySnapshot DeserializeSnapshot(string json) =>
        JsonSerializer.Deserialize<NodalOsInternalSkillRegistrySnapshot>(json, Options) ??
            throw new InvalidOperationException("Internal skill registry snapshot JSON did not deserialize.");
}

public static class NodalOsInternalSkillRegistryFixtures
{
    public static NodalOsInternalSkillRegistrySnapshot Snapshot() =>
        new NodalOsInternalSkillRegistryBuilder()
            .AddPackage(NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage())
            .AddPackage(NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage() with
            {
                PackageId = "pkg-navigation-001",
                Name = "Navigation package",
                Status = NodalOsPackageStatus.InternalPreview,
                Skills = [NodalOsPackageSkillManifestFixtures.NavigationSkill()]
            })
            .AddPackage(NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage() with
            {
                PackageId = "pkg-file-transfer-001",
                Name = "File transfer package",
                Status = NodalOsPackageStatus.InternalPreview,
                Skills = [NodalOsPackageSkillManifestFixtures.FileTransferSkill()]
            })
            .BuildSnapshot(
                registryId: "registry-internal-skills-v1",
                version: "1.0.0",
                createdAt: NodalOsPackageSkillManifestFixtures.FixedTimestamp);
}
