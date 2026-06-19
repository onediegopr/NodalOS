using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("InternalSkillRegistry")]
[TestCategory("PackageSkillManifest")]
[TestCategory("NamespaceNamingAdr")]
[TestCategory("CommonRedaction")]
public sealed class NodalOsInternalSkillRegistryV1M395M397Tests
{
    private readonly NodalOsInternalSkillRegistryValidator validator = new();

    [TestMethod]
    public void RegistryEntry_FromPackageManifest_CreatesPackageEntry()
    {
        var package = NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage();

        var entries = NodalOsInternalSkillRegistryBuilder.FromPackageManifest(package);
        var packageEntry = entries.Single(entry => entry.Kind == NodalOsRegistryEntryKind.Package);

        Assert.AreEqual($"package:{package.PackageId}", packageEntry.EntryId);
        Assert.AreEqual(package.PackageId, packageEntry.PackageId);
        Assert.AreEqual(package.Provenance, packageEntry.Provenance);
    }

    [TestMethod]
    public void RegistryEntry_FromSkillManifest_CreatesSkillEntry()
    {
        var package = NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage();

        var entries = NodalOsInternalSkillRegistryBuilder.FromPackageManifest(package);
        var skillEntry = entries.Single(entry => entry.Kind == NodalOsRegistryEntryKind.Skill);

        Assert.AreEqual($"skill:{package.PackageId}:{package.Skills[0].SkillId}", skillEntry.EntryId);
        Assert.AreEqual(package.Skills[0].SkillId, skillEntry.SkillId);
        CollectionAssert.Contains(skillEntry.Capabilities.ToList(), NodalOsSkillCapabilityKind.ReadOnly);
    }

    [TestMethod]
    public void RegistryEntry_RuntimeExecutionAllowed_IsFalse()
    {
        var entry = FirstVisibleSkill();

        Assert.IsFalse(entry.RuntimeExecutionAllowed);
        Assert.IsTrue(validator.ValidateEntry(entry).IsValid);
    }

    [TestMethod]
    public void RegistryEntry_RuntimeExecutionDeferred_IsTrue()
    {
        var entry = FirstVisibleSkill();

        Assert.IsTrue(entry.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void RegistryEntry_RequiresGlobalPolicyEvaluation()
    {
        var entry = FirstVisibleSkill();

        Assert.IsTrue(entry.RequiresGlobalPolicyEvaluation);
    }

    [TestMethod]
    public void RegistryEntry_InternalOnlyRequired()
    {
        var entry = FirstVisibleSkill() with { InternalOnly = false };

        var result = validator.ValidateEntry(entry);

        Assert.IsFalse(result.IsValid);
        CollectionAssert.Contains(result.Errors.ToList(), "Registry entries must be InternalOnly in V1.");
    }

    [TestMethod]
    public void RegistryValidator_RejectsRuntimeExecutionAllowedTrue()
    {
        var entry = FirstVisibleSkill() with { RuntimeExecutionAllowed = true };

        var result = validator.ValidateEntry(entry);

        Assert.IsFalse(result.IsValid);
        CollectionAssert.Contains(result.Errors.ToList(), "Registry entries cannot grant runtime execution.");
    }

    [TestMethod]
    public void RegistryValidator_RejectsDuplicateEntryId()
    {
        var entry = FirstVisibleSkill();
        var snapshot = Snapshot([entry, entry with { Name = "Duplicate entry" }]);

        var result = validator.ValidateSnapshot(snapshot);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("Duplicate EntryId", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void RegistryValidator_RejectsDuplicatePackageSkillCombination()
    {
        var entry = FirstVisibleSkill();
        var duplicate = entry with { EntryId = "skill:duplicate-entry" };
        var snapshot = Snapshot([entry, duplicate]);

        var result = validator.ValidateSnapshot(snapshot);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("Duplicate package/skill registry combination", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void RegistryValidator_BlockedEntryNotVisible()
    {
        var entry = FirstVisibleSkill() with
        {
            EntryId = "package:blocked-visible",
            Kind = NodalOsRegistryEntryKind.Package,
            SkillId = null,
            Name = "Visible package",
            Status = NodalOsRegistryEntryStatus.Blocked
        };
        var query = new NodalOsInternalSkillRegistryQueryService(Snapshot([entry]));

        var visibleEntries = query.GetVisibleEntries();

        Assert.AreEqual(0, visibleEntries.Count);
    }

    [TestMethod]
    public void RegistryValidator_DeprecatedEntryNotVisibleAsActive()
    {
        var entry = FirstVisibleSkill() with
        {
            EntryId = "skill:deprecated-visible",
            Name = "Visible skill",
            Status = NodalOsRegistryEntryStatus.Deprecated
        };
        var query = new NodalOsInternalSkillRegistryQueryService(Snapshot([entry]));

        var visibleEntries = query.GetVisibleEntries();

        Assert.AreEqual(0, visibleEntries.Count);
    }

    [TestMethod]
    public void RegistryQuery_FindByPackageId()
    {
        var query = new NodalOsInternalSkillRegistryQueryService(NodalOsInternalSkillRegistryFixtures.Snapshot());

        var entries = query.FindByPackageId("pkg-internal-readonly-001");

        Assert.IsTrue(entries.Count >= 2);
        Assert.IsTrue(entries.All(entry => entry.PackageId == "pkg-internal-readonly-001"));
    }

    [TestMethod]
    public void RegistryQuery_FindBySkillId()
    {
        var query = new NodalOsInternalSkillRegistryQueryService(NodalOsInternalSkillRegistryFixtures.Snapshot());

        var entries = query.FindBySkillId("skill-readonly-001");

        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual(NodalOsRegistryEntryKind.Skill, entries[0].Kind);
    }

    [TestMethod]
    public void RegistryQuery_FilterByCapability()
    {
        var query = new NodalOsInternalSkillRegistryQueryService(NodalOsInternalSkillRegistryFixtures.Snapshot());

        var entries = query.Query(new NodalOsSkillRegistryQuery
        {
            Kind = NodalOsRegistryEntryKind.Skill,
            RequiredCapabilities = [NodalOsSkillCapabilityKind.Navigation]
        });

        Assert.IsTrue(entries.Count > 0);
        Assert.IsTrue(entries.All(entry => entry.Capabilities.Contains(NodalOsSkillCapabilityKind.Navigation)));
    }

    [TestMethod]
    public void RegistryQuery_FilterByMaxRiskLevel()
    {
        var query = new NodalOsInternalSkillRegistryQueryService(NodalOsInternalSkillRegistryFixtures.Snapshot());

        var entries = query.Query(new NodalOsSkillRegistryQuery
        {
            Kind = NodalOsRegistryEntryKind.Skill,
            MaxRiskLevel = NodalOsSkillRiskLevel.Medium
        });

        Assert.IsTrue(entries.Count > 0);
        Assert.IsTrue(entries.All(entry => entry.RiskLevel is null or <= NodalOsSkillRiskLevel.Medium));
    }

    [TestMethod]
    public void RegistryQuery_VisibleOnlyExcludesBlockedDeprecatedHidden()
    {
        var visible = FirstVisibleSkill();
        var blocked = visible with { EntryId = "skill:blocked", SkillId = "skill-blocked", Status = NodalOsRegistryEntryStatus.Blocked };
        var deprecated = visible with { EntryId = "skill:deprecated", SkillId = "skill-deprecated", Status = NodalOsRegistryEntryStatus.Deprecated };
        var hidden = visible with { EntryId = "skill:hidden", SkillId = "skill-hidden", Status = NodalOsRegistryEntryStatus.Hidden };
        var query = new NodalOsInternalSkillRegistryQueryService(Snapshot([visible, blocked, deprecated, hidden]));

        var entries = query.GetVisibleEntries();

        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual(visible.EntryId, entries[0].EntryId);
    }

    [TestMethod]
    public void RegistrySnapshot_SerializesAndDeserializes()
    {
        var snapshot = NodalOsInternalSkillRegistryFixtures.Snapshot();

        var json = NodalOsInternalSkillRegistryJsonSerializer.SerializeSnapshot(snapshot);
        var roundTrip = NodalOsInternalSkillRegistryJsonSerializer.DeserializeSnapshot(json);

        Assert.AreEqual(snapshot.RegistryId, roundTrip.RegistryId);
        Assert.AreEqual(snapshot.Entries.Count, roundTrip.Entries.Count);
    }

    [TestMethod]
    public void Registry_PreservesProvenance()
    {
        var entry = FirstVisibleSkill();

        Assert.AreEqual("internal-fixture", entry.Provenance);
    }

    [TestMethod]
    public void Registry_PreservesEvidenceRequirements()
    {
        var entry = FirstVisibleSkill();

        CollectionAssert.Contains(entry.EvidenceRequirements.ToList(), "manifest-review");
        CollectionAssert.Contains(entry.EvidenceRequirements.ToList(), "run-report");
    }

    [TestMethod]
    public void Registry_UsesCommonRedactionToRejectSecretLikeValues()
    {
        var entry = FirstVisibleSkill() with { Provenance = "Bearer fake_registry_token_12345" };

        var result = validator.ValidateEntry(entry);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("sensitive or secret-like", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Registry_DoesNotGrantExecutionPermission()
    {
        var snapshot = NodalOsInternalSkillRegistryFixtures.Snapshot();

        Assert.IsTrue(snapshot.Entries.All(entry => !entry.RuntimeExecutionAllowed));
        Assert.IsTrue(snapshot.Entries.All(entry => entry.RuntimeExecutionDeferred));
    }

    [TestMethod]
    public void NoRegistryPersistenceDbImplemented()
    {
        var artifact = ReadArtifact();

        StringAssert.Contains(artifact, "\"noRegistryPersistenceDbImplemented\": true");
    }

    [TestMethod]
    public void NoWorkerRuntimeImplemented()
    {
        var artifact = ReadArtifact();

        StringAssert.Contains(artifact, "\"noWorkerRuntimeImplemented\": true");
    }

    [TestMethod]
    public void NoMarketplaceImplemented()
    {
        var artifact = ReadArtifact();

        StringAssert.Contains(artifact, "\"noMarketplaceImplemented\": true");
    }

    [TestMethod]
    public void NoUiOrOrchestrationImplemented()
    {
        var artifact = ReadArtifact();

        StringAssert.Contains(artifact, "\"noUiImplemented\": true");
        StringAssert.Contains(artifact, "\"noOrchestrationApiImplemented\": true");
    }

    private static NodalOsSkillRegistryEntry FirstVisibleSkill() =>
        NodalOsInternalSkillRegistryFixtures.Snapshot().Entries.First(entry => entry.Kind == NodalOsRegistryEntryKind.Skill);

    private static NodalOsInternalSkillRegistrySnapshot Snapshot(IReadOnlyList<NodalOsSkillRegistryEntry> entries) =>
        new()
        {
            RegistryId = "registry-test",
            Version = "1.0.0",
            Entries = entries,
            CreatedAt = NodalOsPackageSkillManifestFixtures.FixedTimestamp
        };

    private static string ReadArtifact()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "artifacts",
            "agent-operations",
            "m397",
            "internal-skill-registry-v1-summary.json");

        Assert.IsTrue(File.Exists(path));
        return File.ReadAllText(path);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OneBrain.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
