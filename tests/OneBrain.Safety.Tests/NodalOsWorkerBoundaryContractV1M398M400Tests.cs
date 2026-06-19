using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("WorkerBoundary")]
[TestCategory("InternalSkillRegistry")]
[TestCategory("PackageSkillManifest")]
[TestCategory("EvidenceRefLedgerBridge")]
[TestCategory("FailureTaxonomy")]
[TestCategory("CommonRedaction")]
public sealed class NodalOsWorkerBoundaryContractV1M398M400Tests
{
    private readonly NodalOsWorkerBoundaryValidator validator = new();

    [TestMethod]
    public void ValidRegisteredWorkerBoundary_PassesPolicy()
    {
        var result = validator.ValidateManifest(NodalOsWorkerBoundaryFixtures.ValidRegisteredWorker());

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.CanPassBoundaryPolicy);
    }

    [TestMethod]
    public void WorkerBoundary_RuntimeExecutionAllowed_IsFalse()
    {
        var manifest = NodalOsWorkerBoundaryFixtures.ValidRegisteredWorker();

        Assert.IsFalse(manifest.RuntimeExecutionAllowed);
        Assert.IsFalse(validator.ValidateManifest(manifest).RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void WorkerBoundary_RuntimeExecutionDeferred_IsTrue()
    {
        var manifest = NodalOsWorkerBoundaryFixtures.ValidRegisteredWorker();

        Assert.IsTrue(manifest.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void WorkerBoundary_RequiresGlobalPolicyEvaluation()
    {
        var manifest = NodalOsWorkerBoundaryFixtures.ValidRegisteredWorker();

        Assert.IsTrue(manifest.RequiresGlobalPolicyEvaluation);
    }

    [TestMethod]
    public void WorkerBoundary_CanAuthorizeActions_IsFalse()
    {
        var manifest = NodalOsWorkerBoundaryFixtures.ValidRegisteredWorker();

        Assert.IsFalse(manifest.CanAuthorizeActions);
    }

    [TestMethod]
    public void WorkerBoundary_InternalOnlyRequired()
    {
        var manifest = NodalOsWorkerBoundaryFixtures.ValidRegisteredWorker() with { InternalOnly = false };

        var result = validator.ValidateManifest(manifest);

        Assert.IsFalse(result.IsValid);
        CollectionAssert.Contains(result.Errors.ToList(), "Worker boundary manifests must be InternalOnly in V1.");
    }

    [TestMethod]
    public void WorkerBoundary_BlockedCannotPassPolicy()
    {
        var result = validator.ValidateManifest(NodalOsWorkerBoundaryFixtures.BlockedWorker());

        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(result.CanPassBoundaryPolicy);
    }

    [TestMethod]
    public void WorkerBoundary_DeprecatedCannotPassPolicy()
    {
        var manifest = NodalOsWorkerBoundaryFixtures.ValidRegisteredWorker() with { Status = NodalOsWorkerStatus.Deprecated };

        var result = validator.ValidateManifest(manifest);

        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(result.CanPassBoundaryPolicy);
    }

    [TestMethod]
    public void WorkerBoundary_DisabledCannotPassPolicy()
    {
        var manifest = NodalOsWorkerBoundaryFixtures.ValidRegisteredWorker() with { Status = NodalOsWorkerStatus.Disabled };

        var result = validator.ValidateManifest(manifest);

        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(result.CanPassBoundaryPolicy);
    }

    [TestMethod]
    public void RegisteredWorker_RequiresCapabilities()
    {
        var manifest = NodalOsWorkerBoundaryFixtures.ValidRegisteredWorker() with { Capabilities = [] };

        var result = validator.ValidateManifest(manifest);

        Assert.IsFalse(result.IsValid);
        CollectionAssert.Contains(result.Errors.ToList(), "Registered workers require declared capabilities.");
    }

    [TestMethod]
    public void WorkerHealth_HealthyDoesNotGrantRuntimePermission()
    {
        var result = validator.ValidateHealthReport(NodalOsWorkerBoundaryFixtures.WorkerHealthReport());

        Assert.IsTrue(result.IsValid);
        Assert.IsFalse(result.RuntimeExecutionAllowed);
        Assert.IsFalse(result.CanPassBoundaryPolicy);
        StringAssert.Contains(string.Join(" ", result.Warnings), "does not grant runtime permission");
    }

    [TestMethod]
    public void WorkerHealth_SerializesAndDeserializes()
    {
        var report = NodalOsWorkerBoundaryFixtures.WorkerHealthReport();

        var json = NodalOsWorkerBoundaryJsonSerializer.SerializeHealthReport(report);
        var roundTrip = NodalOsWorkerBoundaryJsonSerializer.DeserializeHealthReport(json);

        Assert.AreEqual(report.WorkerId, roundTrip.WorkerId);
        Assert.AreEqual(report.HealthStatus, roundTrip.HealthStatus);
    }

    [TestMethod]
    public void WorkerRequest_ExecutionDeferredRequired()
    {
        var request = NodalOsWorkerBoundaryFixtures.RequestEnvelope() with { ExecutionDeferred = false };

        var result = validator.ValidateRequestEnvelope(request);

        Assert.IsFalse(result.IsValid);
        CollectionAssert.Contains(result.Errors.ToList(), "Worker request envelopes must defer execution in V1.");
    }

    [TestMethod]
    public void WorkerRequest_RequiresGlobalPolicyEvaluation()
    {
        var request = NodalOsWorkerBoundaryFixtures.RequestEnvelope() with { RequiresGlobalPolicyEvaluation = false };

        var result = validator.ValidateRequestEnvelope(request);

        Assert.IsFalse(result.IsValid);
        CollectionAssert.Contains(result.Errors.ToList(), "Worker request envelopes must require global policy evaluation.");
    }

    [TestMethod]
    public void WorkerResponse_ExecutedMustBeFalseInV1()
    {
        var response = NodalOsWorkerBoundaryFixtures.ResponseEnvelope() with { Executed = true };

        var result = validator.ValidateResponseEnvelope(response);

        Assert.IsFalse(result.IsValid);
        CollectionAssert.Contains(result.Errors.ToList(), "Worker response envelopes must report Executed=false in V1.");
    }

    [TestMethod]
    public void WorkerResponse_RuntimeExecutionDeferredRequired()
    {
        var response = NodalOsWorkerBoundaryFixtures.ResponseEnvelope() with { RuntimeExecutionDeferred = false };

        var result = validator.ValidateResponseEnvelope(response);

        Assert.IsFalse(result.IsValid);
        CollectionAssert.Contains(result.Errors.ToList(), "Worker response envelopes must defer runtime execution in V1.");
    }

    [TestMethod]
    public void WorkerResponse_CanCarryEvidenceBridgeRefs()
    {
        var response = NodalOsWorkerBoundaryFixtures.ResponseEnvelope();

        Assert.AreEqual(1, response.EvidenceRefs.Count);
        Assert.AreEqual(NodalOsEvidenceBridgeAuthority.NoAuthority, response.EvidenceRefs[0].Authority);
        Assert.IsTrue(validator.ValidateResponseEnvelope(response).IsValid);
    }

    [TestMethod]
    public void WorkerResponse_CanCarryFailureKinds()
    {
        var response = NodalOsWorkerBoundaryFixtures.ResponseEnvelope();

        CollectionAssert.Contains(response.FailureKinds.ToList(), NexaFailureKind.HumanInputRequired);
    }

    [TestMethod]
    public void SecretLikeWorkerManifest_IsRejectedByCommonRedaction()
    {
        var manifest = NodalOsWorkerBoundaryFixtures.ValidRegisteredWorker() with
        {
            Provenance = "authorization: Bearer fake_worker_token_12345"
        };

        var result = validator.ValidateManifest(manifest);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("sensitive or secret-like", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void SecretLikeWorkerHealthWarning_IsRejectedByCommonRedaction()
    {
        var report = NodalOsWorkerBoundaryFixtures.WorkerHealthReport() with
        {
            Warnings = ["set-cookie: fake=value"]
        };

        var result = validator.ValidateHealthReport(report);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("sensitive or secret-like", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void WorkerManifest_SerializesAndDeserializes()
    {
        var manifest = NodalOsWorkerBoundaryFixtures.ValidRegisteredWorker();

        var json = NodalOsWorkerBoundaryJsonSerializer.SerializeManifest(manifest);
        var roundTrip = NodalOsWorkerBoundaryJsonSerializer.DeserializeManifest(json);

        Assert.AreEqual(manifest.WorkerId, roundTrip.WorkerId);
        Assert.AreEqual(manifest.BoundaryKind, roundTrip.BoundaryKind);
        Assert.IsFalse(roundTrip.RuntimeExecutionAllowed);
        Assert.IsTrue(roundTrip.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void WorkerRequestResponse_SerializesAndDeserializes()
    {
        var request = NodalOsWorkerBoundaryFixtures.RequestEnvelope();
        var response = NodalOsWorkerBoundaryFixtures.ResponseEnvelope();

        var requestRoundTrip = NodalOsWorkerBoundaryJsonSerializer.DeserializeRequestEnvelope(
            NodalOsWorkerBoundaryJsonSerializer.SerializeRequestEnvelope(request));
        var responseRoundTrip = NodalOsWorkerBoundaryJsonSerializer.DeserializeResponseEnvelope(
            NodalOsWorkerBoundaryJsonSerializer.SerializeResponseEnvelope(response));

        Assert.AreEqual(request.RequestId, requestRoundTrip.RequestId);
        Assert.AreEqual(response.ResponseId, responseRoundTrip.ResponseId);
        Assert.IsFalse(responseRoundTrip.Executed);
        Assert.IsTrue(responseRoundTrip.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void NoWorkerRuntimeImplemented()
    {
        StringAssert.Contains(ReadArtifact(), "\"noWorkerRuntimeImplemented\": true");
    }

    [TestMethod]
    public void NoUiOrOrchestrationImplemented()
    {
        var artifact = ReadArtifact();

        StringAssert.Contains(artifact, "\"noUiImplemented\": true");
        StringAssert.Contains(artifact, "\"noOrchestrationApiImplemented\": true");
    }

    [TestMethod]
    public void NoSkillOrRecipeExecutionImplemented()
    {
        var artifact = ReadArtifact();

        StringAssert.Contains(artifact, "\"noSkillExecutionImplemented\": true");
        StringAssert.Contains(artifact, "\"noRecipeExecutionImplemented\": true");
        StringAssert.Contains(artifact, "\"noStepExecutionImplemented\": true");
    }

    private static string ReadArtifact()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "artifacts",
            "agent-operations",
            "m400",
            "worker-boundary-contract-v1-summary.json");

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
