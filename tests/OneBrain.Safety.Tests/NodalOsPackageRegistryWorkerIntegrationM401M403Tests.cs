using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PackageRegistryWorkerIntegration")]
[TestCategory("WorkerBoundary")]
[TestCategory("InternalSkillRegistry")]
[TestCategory("PackageSkillManifest")]
[TestCategory("EvidenceRefLedgerBridge")]
[TestCategory("CommonRedaction")]
public sealed class NodalOsPackageRegistryWorkerIntegrationM401M403Tests
{
    private readonly NodalOsWorkerBoundaryValidator workerValidator = new();
    private readonly NodalOsInternalSkillRegistryValidator registryValidator = new();

    [TestMethod]
    public void WorkerResponse_WithValidNoAuthorityEvidenceRef_Passes()
    {
        var response = ValidResponse();

        var result = workerValidator.ValidateResponseEnvelope(response);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void WorkerResponse_WithEvidenceRefRedactionRequired_Fails()
    {
        var response = ValidResponseWithEvidence(ValidEvidenceRef() with
        {
            Sensitivity = NodalOsEvidenceSensitivity.Sensitive,
            RedactionState = NodalOsEvidenceRedactionState.RedactionRequired
        });

        var result = workerValidator.ValidateResponseEnvelope(response);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("redaction", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void WorkerResponse_WithEvidenceRefRejectedSensitive_Fails()
    {
        var response = ValidResponseWithEvidence(ValidEvidenceRef() with
        {
            Sensitivity = NodalOsEvidenceSensitivity.Sensitive,
            RedactionState = NodalOsEvidenceRedactionState.RejectedSensitive
        });

        var result = workerValidator.ValidateResponseEnvelope(response);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("Rejected sensitive", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void WorkerResponse_WithSensitiveEvidenceWithoutRedaction_Fails()
    {
        var response = ValidResponseWithEvidence(ValidEvidenceRef() with
        {
            Sensitivity = NodalOsEvidenceSensitivity.Sensitive,
            RedactionState = NodalOsEvidenceRedactionState.NotRequired
        });

        var result = workerValidator.ValidateResponseEnvelope(response);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("Sensitive evidence must be redacted", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void WorkerResponse_WithInvalidAuthority_Fails()
    {
        var response = ValidResponseWithEvidence(ValidEvidenceRef() with
        {
            Authority = NodalOsEvidenceBridgeAuthority.SupportsVerificationOnly
        });

        var result = workerValidator.ValidateResponseEnvelope(response);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("cannot carry verification or action authority", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void WorkerResponse_EvidenceRefRawSecret_IsRedactedOrRejected()
    {
        var response = ValidResponseWithEvidence(ValidEvidenceRef() with
        {
            Ref = "authorization: Bearer fake_worker_evidence_token_12345",
            Sensitivity = NodalOsEvidenceSensitivity.Sensitive,
            RedactionState = NodalOsEvidenceRedactionState.RedactionRequired
        });

        var result = workerValidator.ValidateResponseEnvelope(response);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("sensitive", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void WorkerResponse_EvidenceValidationErrors_DoNotExposeRawSecrets()
    {
        const string rawSecret = "fake_worker_evidence_token_12345";
        var response = ValidResponseWithEvidence(ValidEvidenceRef() with
        {
            Ref = $"Bearer {rawSecret}",
            Sensitivity = NodalOsEvidenceSensitivity.Sensitive,
            RedactionState = NodalOsEvidenceRedactionState.RedactionRequired
        });

        var result = workerValidator.ValidateResponseEnvelope(response);
        var joinedErrors = string.Join("\n", result.Errors);

        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(joinedErrors.Contains(rawSecret, StringComparison.Ordinal));
    }

    [TestMethod]
    public void BuildValidatedSnapshot_FromValidPackageSkill_Passes()
    {
        var result = BuildValidatedSnapshot(NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage());

        Assert.IsTrue(result.Validation.IsValid);
        Assert.IsTrue(result.Snapshot.Entries.Count >= 2);
    }

    [TestMethod]
    public void BuildValidatedSnapshot_DoesNotPropagateRuntimeExecutionAllowedTrue()
    {
        var package = RuntimeAttemptPackage();

        var result = BuildValidatedSnapshot(package);

        Assert.IsTrue(result.Snapshot.Entries.All(entry => !entry.RuntimeExecutionAllowed));
        Assert.IsTrue(result.Validation.Warnings.Any(warning => warning.Contains("normalized", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void BuildValidatedSnapshot_RuntimeExecutionAllowed_RemainsFalse()
    {
        var result = BuildValidatedSnapshot(RuntimeAttemptPackage());

        Assert.IsTrue(result.Snapshot.Entries.All(entry => !entry.RuntimeExecutionAllowed));
    }

    [TestMethod]
    public void BuildValidatedSnapshot_RuntimeExecutionDeferred_RemainsTrue()
    {
        var result = BuildValidatedSnapshot(RuntimeAttemptPackage());

        Assert.IsTrue(result.Snapshot.Entries.All(entry => entry.RuntimeExecutionDeferred));
    }

    [TestMethod]
    public void BuildValidatedSnapshot_RequiresGlobalPolicyEvaluation_RemainsTrue()
    {
        var result = BuildValidatedSnapshot(RuntimeAttemptPackage());

        Assert.IsTrue(result.Snapshot.Entries.All(entry => entry.RequiresGlobalPolicyEvaluation));
    }

    [TestMethod]
    public void RegistryStatus_DoesNotDependOnNameContainsBlocked()
    {
        var entry = FirstVisibleSkill() with { Name = "blocked helper visible by enum" };

        var result = registryValidator.ValidateEntry(entry);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void RegistryStatus_DoesNotDependOnNameContainsDeprecated()
    {
        var entry = FirstVisibleSkill() with { Name = "deprecated helper visible by enum" };

        var result = registryValidator.ValidateEntry(entry);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void PackageNamedDeprecatedHelper_CanStillBeVisibleIfStatusValid()
    {
        var entry = FirstVisiblePackage() with { Name = "deprecated package helper" };

        var result = registryValidator.ValidateEntry(entry);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void PackageNamedBlockedHelper_CanStillBeVisibleIfStatusValid()
    {
        var entry = FirstVisiblePackage() with { Name = "blocked package helper" };

        var result = registryValidator.ValidateEntry(entry);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void SkillCapabilityMapper_CoversAllSkillCapabilities()
    {
        foreach (var capability in Enum.GetValues<NodalOsSkillCapabilityKind>())
            _ = NodalOsWorkerSkillCapabilityMapper.Map(capability);
    }

    [TestMethod]
    public void SkillCapabilityMapper_MapsReadOnly()
    {
        Assert.AreEqual(
            NodalOsWorkerCapabilityKind.ReadOnly,
            NodalOsWorkerSkillCapabilityMapper.Map(NodalOsSkillCapabilityKind.ReadOnly));
    }

    [TestMethod]
    public void SkillCapabilityMapper_MapsFileTransfer()
    {
        Assert.AreEqual(
            NodalOsWorkerCapabilityKind.FileTransfer,
            NodalOsWorkerSkillCapabilityMapper.Map(NodalOsSkillCapabilityKind.FileTransfer));
    }

    [TestMethod]
    public void SkillCapabilityMapper_MapsDataEntry()
    {
        Assert.AreEqual(
            NodalOsWorkerCapabilityKind.DataEntry,
            NodalOsWorkerSkillCapabilityMapper.Map(NodalOsSkillCapabilityKind.DataEntry));
    }

    [TestMethod]
    public void SkillCapabilityMapper_MapsInteraction()
    {
        Assert.AreEqual(
            NodalOsWorkerCapabilityKind.Interaction,
            NodalOsWorkerSkillCapabilityMapper.Map(NodalOsSkillCapabilityKind.Interaction));
    }

    [TestMethod]
    public void SkillCapabilityMapper_DoesNotDropCapabilities()
    {
        var mapped = NodalOsWorkerSkillCapabilityMapper.MapMany(Enum.GetValues<NodalOsSkillCapabilityKind>());

        Assert.AreEqual(Enum.GetValues<NodalOsSkillCapabilityKind>().Length, mapped.Count);
    }

    [TestMethod]
    public void PackageRegistryWorker_RuntimeExecutionAllowedFalseAcrossLayers()
    {
        var (package, snapshot, worker) = CrossLayer();

        Assert.IsFalse(package.RuntimeExecutionAllowed);
        Assert.IsTrue(snapshot.Entries.All(entry => !entry.RuntimeExecutionAllowed));
        Assert.IsFalse(worker.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void PackageRegistryWorker_RuntimeExecutionDeferredTrueAcrossLayers()
    {
        var (package, snapshot, worker) = CrossLayer();

        Assert.IsTrue(package.RuntimeExecutionDeferred);
        Assert.IsTrue(snapshot.Entries.All(entry => entry.RuntimeExecutionDeferred));
        Assert.IsTrue(worker.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void PackageRegistryWorker_GlobalPolicyRequiredAcrossLayers()
    {
        var (package, snapshot, worker) = CrossLayer();

        Assert.IsTrue(package.RequiresGlobalPolicyEvaluation);
        Assert.IsTrue(snapshot.Entries.All(entry => entry.RequiresGlobalPolicyEvaluation));
        Assert.IsTrue(worker.RequiresGlobalPolicyEvaluation);
    }

    [TestMethod]
    public void PackageRegistryWorker_InternalOnlyTrueAcrossLayers()
    {
        var (package, snapshot, worker) = CrossLayer();

        Assert.IsTrue(package.InternalOnly);
        Assert.IsTrue(snapshot.Entries.All(entry => entry.InternalOnly));
        Assert.IsTrue(worker.InternalOnly);
    }

    [TestMethod]
    public void PackageRegistryWorker_SupportedPackageSkillIdsMatchRegistryEntries()
    {
        var (_, snapshot, worker) = CrossLayer();
        var packageIds = snapshot.Entries.Select(entry => entry.PackageId).Distinct(StringComparer.Ordinal).ToArray();
        var skillIds = snapshot.Entries.Where(entry => entry.SkillId is not null).Select(entry => entry.SkillId!).ToArray();

        CollectionAssert.IsSubsetOf(packageIds.ToList(), worker.SupportedPackageIds.ToList());
        CollectionAssert.IsSubsetOf(skillIds.ToList(), worker.SupportedSkillIds.ToList());
    }

    [TestMethod]
    public void PackageRegistryWorker_VisibleHealthyDoesNotGrantRuntimePermission()
    {
        var (_, snapshot, worker) = CrossLayer();
        var visible = new NodalOsInternalSkillRegistryQueryService(snapshot).GetVisibleEntries();
        var health = NodalOsWorkerBoundaryFixtures.WorkerHealthReport() with { HealthStatus = NodalOsWorkerHealthStatus.Healthy };

        Assert.IsTrue(visible.Count > 0);
        Assert.IsFalse(workerValidator.ValidateHealthReport(health).RuntimeExecutionAllowed);
        Assert.IsFalse(worker.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void PackageRegistryWorker_CanPassCatalogPolicyDoesNotGrantRuntimePermission()
    {
        var packageResult = new NodalOsPackageSkillManifestValidator().ValidatePackage(
            NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage());

        Assert.IsTrue(packageResult.CanPassCatalogPolicy);
        Assert.IsFalse(packageResult.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void PackageRegistryWorker_CanPassBoundaryPolicyDoesNotGrantRuntimePermission()
    {
        var workerResult = workerValidator.ValidateManifest(CrossLayer().Worker);

        Assert.IsTrue(workerResult.CanPassBoundaryPolicy);
        Assert.IsFalse(workerResult.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void NoRuntimeExecutionOrOrchestrationIntroduced()
    {
        var artifact = ReadArtifact();

        StringAssert.Contains(artifact, "\"noWorkerRuntimeImplemented\": true");
        StringAssert.Contains(artifact, "\"noSkillExecutionImplemented\": true");
        StringAssert.Contains(artifact, "\"noRecipeExecutionImplemented\": true");
        StringAssert.Contains(artifact, "\"noOrchestrationApiImplemented\": true");
    }

    private static NodalOsWorkerResponseEnvelope ValidResponse() =>
        NodalOsWorkerBoundaryFixtures.ResponseEnvelope();

    private static NodalOsWorkerResponseEnvelope ValidResponseWithEvidence(NodalOsEvidenceBridgeRef evidenceRef) =>
        ValidResponse() with { EvidenceRefs = [evidenceRef] };

    private static NodalOsEvidenceBridgeRef ValidEvidenceRef() =>
        ValidResponse().EvidenceRefs[0];

    private static NodalOsInternalSkillRegistryBuildResult BuildValidatedSnapshot(NodalOsPackageManifest package) =>
        new NodalOsInternalSkillRegistryBuilder()
            .AddPackage(package)
            .BuildValidatedSnapshot(
                "registry-m401-m403",
                "1.0.0",
                NodalOsPackageSkillManifestFixtures.FixedTimestamp);

    private static NodalOsPackageManifest RuntimeAttemptPackage() =>
        NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage() with
        {
            RuntimeExecutionAllowed = true,
            RuntimeExecutionDeferred = false,
            RequiresGlobalPolicyEvaluation = false,
            Skills =
            [
                NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage().Skills[0] with
                {
                    RuntimeExecutionAllowed = true,
                    RuntimeExecutionDeferred = false,
                    RequiresGlobalPolicyEvaluation = false
                }
            ]
        };

    private static (NodalOsPackageManifest Package, NodalOsInternalSkillRegistrySnapshot Snapshot, NodalOsWorkerBoundaryManifest Worker) CrossLayer()
    {
        var package = NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage();
        var build = BuildValidatedSnapshot(package);
        var skillIds = build.Snapshot.Entries
            .Where(entry => entry.Kind == NodalOsRegistryEntryKind.Skill && entry.SkillId is not null)
            .Select(entry => entry.SkillId!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var workerCapabilities = NodalOsWorkerSkillCapabilityMapper.MapMany(
            build.Snapshot.Entries.SelectMany(entry => entry.Capabilities));
        var worker = NodalOsWorkerBoundaryFixtures.ValidRegisteredWorker() with
        {
            SupportedPackageIds = [package.PackageId],
            SupportedSkillIds = skillIds,
            Capabilities = workerCapabilities
        };

        return (package, build.Snapshot, worker);
    }

    private static NodalOsSkillRegistryEntry FirstVisiblePackage() =>
        NodalOsInternalSkillRegistryFixtures.Snapshot().Entries.First(entry => entry.Kind == NodalOsRegistryEntryKind.Package);

    private static NodalOsSkillRegistryEntry FirstVisibleSkill() =>
        NodalOsInternalSkillRegistryFixtures.Snapshot().Entries.First(entry => entry.Kind == NodalOsRegistryEntryKind.Skill);

    private static string ReadArtifact()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "artifacts",
            "agent-operations",
            "m403",
            "package-registry-worker-integration-no-divergence-summary.json");

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
