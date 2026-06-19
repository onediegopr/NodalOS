using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PackageSkillManifest")]
[TestCategory("NamespaceNamingAdr")]
[TestCategory("RecipeStepRuntimePermission")]
[TestCategory("StepLibrary")]
[TestCategory("RecipeManifest")]
[TestCategory("CommonRedaction")]
public sealed class NodalOsPackageSkillManifestV1M392M394Tests
{
    private readonly NodalOsPackageSkillManifestValidator validator = new();

    [TestMethod]
    public void ValidInternalPackageManifest_PassesCatalogPolicy()
    {
        var result = validator.ValidatePackage(NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage());

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.CanPassCatalogPolicy);
    }

    [TestMethod]
    public void PackageManifest_RuntimeExecutionAllowed_IsFalse()
    {
        var result = validator.ValidatePackage(NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage());

        Assert.IsFalse(result.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void PackageManifest_RuntimeExecutionDeferred_IsTrue()
    {
        var result = validator.ValidatePackage(NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage());

        Assert.IsTrue(result.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void PackageManifest_RequiresGlobalPolicyEvaluation()
    {
        var result = validator.ValidatePackage(NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage());

        Assert.IsTrue(result.RequiresGlobalPolicyEvaluation);
    }

    [TestMethod]
    public void PackageManifest_InternalOnlyRequired()
    {
        var package = NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage() with { InternalOnly = false };

        var result = validator.ValidatePackage(package);

        Assert.IsFalse(result.IsValid);
        CollectionAssert.Contains(result.Errors.ToList(), "Package manifests must be InternalOnly in V1.");
    }

    [TestMethod]
    public void PackageManifest_BlockedCannotPassCatalogPolicy()
    {
        var result = validator.ValidatePackage(NodalOsPackageSkillManifestFixtures.BlockedPackage());

        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(result.CanPassCatalogPolicy);
    }

    [TestMethod]
    public void PackageManifest_DeprecatedCannotPassCatalogPolicy()
    {
        var result = validator.ValidatePackage(NodalOsPackageSkillManifestFixtures.DeprecatedPackage());

        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(result.CanPassCatalogPolicy);
    }

    [TestMethod]
    public void PackageManifest_ApprovedForCatalog_DoesNotGrantRuntimeExecution()
    {
        var result = validator.ValidatePackage(NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage());

        Assert.IsTrue(result.CanPassCatalogPolicy);
        Assert.IsFalse(result.RuntimeExecutionAllowed);
        StringAssert.Contains(string.Join(" ", result.Warnings), "does not grant runtime execution");
    }

    [TestMethod]
    public void PackageManifest_EmptySkillsDraftWarns()
    {
        var result = validator.ValidatePackage(NodalOsPackageSkillManifestFixtures.DraftEmptyPackage());

        Assert.IsTrue(result.IsValid);
        Assert.IsFalse(result.CanPassCatalogPolicy);
        StringAssert.Contains(string.Join(" ", result.Warnings), "at least one skill is recommended");
    }

    [TestMethod]
    public void SkillManifest_ValidReadOnlySkill_PassesCatalogPolicy()
    {
        var result = validator.ValidateSkill(NodalOsPackageSkillManifestFixtures.ReadOnlySkill());

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.CanPassCatalogPolicy);
    }

    [TestMethod]
    public void SkillManifest_RuntimeExecutionAllowed_IsFalse()
    {
        var result = validator.ValidateSkill(NodalOsPackageSkillManifestFixtures.ReadOnlySkill());

        Assert.IsFalse(result.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void SkillManifest_RuntimeExecutionDeferred_IsTrue()
    {
        var result = validator.ValidateSkill(NodalOsPackageSkillManifestFixtures.ReadOnlySkill());

        Assert.IsTrue(result.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void SkillManifest_RequiresGlobalPolicyEvaluation()
    {
        var result = validator.ValidateSkill(NodalOsPackageSkillManifestFixtures.ReadOnlySkill());

        Assert.IsTrue(result.RequiresGlobalPolicyEvaluation);
    }

    [TestMethod]
    public void SkillManifest_HighRiskRequiresApproval()
    {
        var skill = NodalOsPackageSkillManifestFixtures.DataEntrySkill() with { RequiredApprovals = [] };

        var result = validator.ValidateSkill(skill);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("High or Critical risk skills require approval metadata", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void SkillManifest_FileTransferRequiresApproval()
    {
        var skill = NodalOsPackageSkillManifestFixtures.FileTransferSkill() with { RequiredApprovals = [] };

        var result = validator.ValidateSkill(skill);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("FileTransfer capability requires approval metadata", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void SkillManifest_DataEntryRequiresApproval()
    {
        var skill = NodalOsPackageSkillManifestFixtures.DataEntrySkill() with { RequiredApprovals = [] };

        var result = validator.ValidateSkill(skill);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("DataEntry capability requires approval metadata", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void SkillManifest_NavigationRequiresAllowedDomain()
    {
        var skill = NodalOsPackageSkillManifestFixtures.NavigationSkill() with { AllowedDomains = [] };

        var result = validator.ValidateSkill(skill);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("Navigation capability requires AllowedDomains", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void SkillManifest_EvidenceRequirementsPreserved()
    {
        var skill = NodalOsPackageSkillManifestFixtures.ReadOnlySkill();

        Assert.IsTrue(skill.EvidenceRequirements.Contains("run-report"));
        Assert.IsTrue(skill.EvidenceRequirements.Contains("evidence-ref-bridge"));
    }

    [TestMethod]
    public void PackageSkillManifest_SerializesAndDeserializes()
    {
        var package = NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage();

        var json = NodalOsPackageSkillManifestJsonSerializer.SerializePackage(package);
        var roundTrip = NodalOsPackageSkillManifestJsonSerializer.DeserializePackage(json);

        Assert.AreEqual(package.PackageId, roundTrip.PackageId);
        Assert.AreEqual(package.Skills[0].SkillId, roundTrip.Skills[0].SkillId);
        Assert.IsFalse(roundTrip.RuntimeExecutionAllowed);
        Assert.IsTrue(roundTrip.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void SecretLikeField_IsRejectedOrRedactedUsingCommonRedaction()
    {
        var package = NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage() with
        {
            Provenance = "authorization: Bearer fake_token_value_12345"
        };

        var result = validator.ValidatePackage(package);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("sensitive or secret-like", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void RuntimePermission_NotGrantedByCatalogApproval()
    {
        var packageResult = validator.ValidatePackage(NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage());
        var skillResult = validator.ValidateSkill(NodalOsPackageSkillManifestFixtures.ReadOnlySkill());

        Assert.IsTrue(packageResult.CanPassCatalogPolicy);
        Assert.IsTrue(skillResult.CanPassCatalogPolicy);
        Assert.IsFalse(packageResult.RuntimeExecutionAllowed);
        Assert.IsFalse(skillResult.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void NoRegistryImplemented()
    {
        var artifact = ReadArtifact();

        StringAssert.Contains(artifact, "\"noRegistryImplemented\": true");
    }

    [TestMethod]
    public void NoWorkerRuntimeImplemented()
    {
        var artifact = ReadArtifact();

        StringAssert.Contains(artifact, "\"noWorkerRuntimeImplemented\": true");
    }

    [TestMethod]
    public void NoUiOrOrchestrationImplemented()
    {
        var artifact = ReadArtifact();

        StringAssert.Contains(artifact, "\"noUiImplemented\": true");
        StringAssert.Contains(artifact, "\"noOrchestrationApiImplemented\": true");
        StringAssert.Contains(artifact, "\"noRecipeExecutionImplemented\": true");
        StringAssert.Contains(artifact, "\"noStepExecutionImplemented\": true");
    }

    [TestMethod]
    public void NewTypesUseNodalOsPrefix()
    {
        var contractTypes = new[]
        {
            typeof(NodalOsPackageManifest),
            typeof(NodalOsSkillManifest),
            typeof(NodalOsPackageSkillManifestValidationResult),
            typeof(NodalOsPackageStatus),
            typeof(NodalOsSkillStatus),
            typeof(NodalOsSkillCapabilityKind),
            typeof(NodalOsSkillRiskLevel)
        };

        Assert.IsTrue(contractTypes.All(type => type.Name.StartsWith("NodalOs", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ValidationResult_SerializesRuntimePermissionFields()
    {
        var result = validator.ValidatePackage(NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage());

        var json = JsonSerializer.Serialize(result);
        var roundTrip = JsonSerializer.Deserialize<NodalOsPackageSkillManifestValidationResult>(json);

        Assert.IsNotNull(roundTrip);
        Assert.AreEqual(result.CanPassCatalogPolicy, roundTrip.CanPassCatalogPolicy);
        Assert.AreEqual(result.RuntimeExecutionAllowed, roundTrip.RuntimeExecutionAllowed);
        Assert.AreEqual(result.RuntimeExecutionDeferred, roundTrip.RuntimeExecutionDeferred);
        Assert.AreEqual(result.RequiresGlobalPolicyEvaluation, roundTrip.RequiresGlobalPolicyEvaluation);
    }

    private static string ReadArtifact()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "artifacts",
            "agent-operations",
            "m394",
            "package-skill-manifest-v1-summary.json");

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
