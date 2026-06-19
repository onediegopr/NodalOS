using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ScheduledReadOnlyRunContracts")]
[TestCategory("ScheduledReadOnlyRunsAdr")]
[TestCategory("OrchestrationInProcessFacadeV1")]
public sealed class NodalOsScheduledReadOnlyRunContractsV1M434M436Tests
{
    private readonly NodalOsScheduledReadOnlyRunValidator validator = new();

    [TestMethod]
    public void Schedule_RuntimeExecutionAllowed_IsFalse()
    {
        var schedule = NodalOsScheduledReadOnlyRunFixtures.ValidManualOnlySchedule();

        Assert.IsFalse(schedule.RuntimeExecutionAllowed);
        Assert.IsFalse(validator.ValidateSchedule(schedule).RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void Schedule_RuntimeExecutionDeferred_IsTrue()
    {
        var schedule = NodalOsScheduledReadOnlyRunFixtures.ValidManualOnlySchedule();

        Assert.IsTrue(schedule.RuntimeExecutionDeferred);
        Assert.IsTrue(validator.ValidateSchedule(schedule).RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void Schedule_RequiresGlobalPolicyEvaluation()
    {
        var schedule = NodalOsScheduledReadOnlyRunFixtures.ValidManualOnlySchedule();

        Assert.IsTrue(schedule.RequiresGlobalPolicyEvaluation);
        Assert.IsTrue(validator.ValidateSchedule(schedule).RequiresGlobalPolicyEvaluation);
    }

    [TestMethod]
    public void Schedule_ReadOnlyRequired()
    {
        var schedule = NodalOsScheduledReadOnlyRunFixtures.ValidManualOnlySchedule() with { ReadOnly = false };
        var result = validator.ValidateSchedule(schedule);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "ReadOnly=true");
    }

    [TestMethod]
    public void Schedule_RequiresEvidenceRedaction()
    {
        var schedule = NodalOsScheduledReadOnlyRunFixtures.ValidManualOnlySchedule() with { RequiresEvidenceRedaction = false };
        var result = validator.ValidateSchedule(schedule);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "evidence redaction");
    }

    [TestMethod]
    public void Schedule_RequiresHumanOwner()
    {
        var schedule = NodalOsScheduledReadOnlyRunFixtures.ValidManualOnlySchedule() with { HumanOwner = " " };
        var result = validator.ValidateSchedule(schedule);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "HumanOwner is required");
    }

    [TestMethod]
    public void Schedule_ManualOnly_DoesNotImplementScheduler()
    {
        var schedule = NodalOsScheduledReadOnlyRunFixtures.ValidManualOnlySchedule();
        var result = validator.ValidateSchedule(schedule);

        Assert.IsTrue(result.IsValid);
        AssertNoSchedulerImplementationInNewSource();
    }

    [TestMethod]
    public void Schedule_HourlyFuture_DoesNotImplementScheduler()
    {
        var schedule = NodalOsScheduledReadOnlyRunFixtures.ValidManualOnlySchedule() with
        {
            FrequencyKind = NodalOsScheduledReadOnlyFrequencyKind.HourlyFuture,
            Status = NodalOsScheduledReadOnlyScheduleStatus.ScheduledReadOnlyFuture
        };
        var result = validator.ValidateSchedule(schedule);

        Assert.IsTrue(result.IsValid);
        AssertContains(result.Warnings, "no scheduler is implemented");
        AssertContains(result.Warnings, "do not implement scheduler behavior");
    }

    [TestMethod]
    public void Schedule_BlockedCannotPassPolicy()
    {
        var schedule = NodalOsScheduledReadOnlyRunFixtures.ValidManualOnlySchedule() with
        {
            Status = NodalOsScheduledReadOnlyScheduleStatus.Blocked
        };
        var result = validator.ValidateSchedule(schedule);

        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(result.CanPassSchedulePolicy);
    }

    [TestMethod]
    public void Schedule_CancelledCannotPassPolicy()
    {
        var schedule = NodalOsScheduledReadOnlyRunFixtures.ValidManualOnlySchedule() with
        {
            Status = NodalOsScheduledReadOnlyScheduleStatus.Cancelled
        };
        var result = validator.ValidateSchedule(schedule);

        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(result.CanPassSchedulePolicy);
    }

    [TestMethod]
    public void Schedule_EvidenceRefsValidateViaBridge()
    {
        var schedule = NodalOsScheduledReadOnlyRunFixtures.ValidManualOnlySchedule();
        var result = validator.ValidateSchedule(schedule);

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(schedule.EvidenceRefs.Count > 0);
    }

    [TestMethod]
    public void Schedule_SecretTargetRejectedOrRedacted()
    {
        var schedule = NodalOsScheduledReadOnlyRunFixtures.ValidManualOnlySchedule() with
        {
            AllowedTargets = ["https://lab.nodalos.com.ar/read?access_token=raw-secret"]
        };
        var result = validator.ValidateSchedule(schedule);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "sensitive or secret-like content");
    }

    [TestMethod]
    public void RunRequest_ManualTriggerRequired()
    {
        var request = NodalOsScheduledReadOnlyRunFixtures.RequestFromSchedule() with { ManualTriggerRequired = false };
        var result = validator.ValidateRunRequest(request);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "ManualTriggerRequired=true");
    }

    [TestMethod]
    public void RunRequest_RuntimeExecutionAllowedFalse()
    {
        var request = NodalOsScheduledReadOnlyRunFixtures.RequestFromSchedule();

        Assert.IsFalse(request.RuntimeExecutionAllowed);
        Assert.IsTrue(validator.ValidateRunRequest(request).IsValid);
    }

    [TestMethod]
    public void RunRequest_ReadOnlyRequired()
    {
        var request = NodalOsScheduledReadOnlyRunFixtures.RequestFromSchedule() with { ReadOnly = false };
        var result = validator.ValidateRunRequest(request);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "ReadOnly=true");
    }

    [TestMethod]
    public void Preview_DryRunOnlyRequired()
    {
        var preview = NodalOsScheduledReadOnlyRunFixtures.ValidDryRunPreview() with { DryRunOnly = false };
        var result = validator.ValidatePreview(preview);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "dry-run only");
    }

    [TestMethod]
    public void Preview_ExecutedMustBeFalse()
    {
        var preview = NodalOsScheduledReadOnlyRunFixtures.ValidDryRunPreview() with { Executed = true };
        var result = validator.ValidatePreview(preview);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "Executed=false");
    }

    [TestMethod]
    public void Preview_ForbiddenClickOperationRejected()
    {
        AssertForbiddenPreviewOperation("click button");
    }

    [TestMethod]
    public void Preview_ForbiddenTypeSubmitUploadDownloadRejected()
    {
        AssertForbiddenPreviewOperation("type into field");
        AssertForbiddenPreviewOperation("submit form");
        AssertForbiddenPreviewOperation("upload file");
        AssertForbiddenPreviewOperation("download invoice");
    }

    [TestMethod]
    public void Preview_ForbiddenLoginCaptchaTwoFactorRejected()
    {
        AssertForbiddenPreviewOperation("login to account");
        AssertForbiddenPreviewOperation("captcha challenge");
        AssertForbiddenPreviewOperation("2FA approval");
    }

    [TestMethod]
    public void Preview_ForbiddenPaySendDeleteSignPublishRejected()
    {
        AssertForbiddenPreviewOperation("pay invoice");
        AssertForbiddenPreviewOperation("send message");
        AssertForbiddenPreviewOperation("delete record");
        AssertForbiddenPreviewOperation("sign document");
        AssertForbiddenPreviewOperation("publish post");
    }

    [TestMethod]
    public void Preview_EvidenceRefsValidateViaBridge()
    {
        var preview = NodalOsScheduledReadOnlyRunFixtures.ValidDryRunPreview();
        var result = validator.ValidatePreview(preview);

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(preview.EvidenceRefs.Count > 0);
    }

    [TestMethod]
    public void Serializer_RoundTripsScheduleRequestPreview()
    {
        var serializer = new NodalOsScheduledReadOnlyRunJsonSerializer();
        var schedule = NodalOsScheduledReadOnlyRunFixtures.ValidManualOnlySchedule();
        var request = NodalOsScheduledReadOnlyRunFixtures.RequestFromSchedule(schedule);
        var preview = NodalOsScheduledReadOnlyRunFixtures.ValidDryRunPreview();

        var scheduleRoundTrip = serializer.DeserializeSchedule(serializer.SerializeSchedule(schedule));
        var requestRoundTrip = serializer.DeserializeRunRequest(serializer.SerializeRunRequest(request));
        var previewRoundTrip = serializer.DeserializePreview(serializer.SerializePreview(preview));

        Assert.AreEqual(schedule.ScheduleId, scheduleRoundTrip.ScheduleId);
        Assert.AreEqual(request.RequestId, requestRoundTrip.RequestId);
        Assert.AreEqual(preview.PreviewId, previewRoundTrip.PreviewId);
    }

    [TestMethod]
    public void NoSchedulerTimerBackgroundWorkerImplemented()
    {
        AssertNoSchedulerImplementationInNewSource();
        StringAssert.Contains(ReadArtifact(), "\"noSchedulerImplemented\": true");
        StringAssert.Contains(ReadArtifact(), "\"noTimerImplemented\": true");
        StringAssert.Contains(ReadArtifact(), "\"noBackgroundWorkerImplemented\": true");
    }

    [TestMethod]
    public void NoUiOrExecutionImplemented()
    {
        StringAssert.Contains(ReadArtifact(), "\"noUiImplemented\": true");
        StringAssert.Contains(ReadArtifact(), "\"noExecutionImplemented\": true");
    }

    [TestMethod]
    public void NewTypesUseNodalOsPrefix()
    {
        var contracts = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.AgentOperations.Contracts", "NodalOsScheduledReadOnlyRunContracts.cs"));
        var services = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.AgentOperations.Core", "NodalOsScheduledReadOnlyRunServices.cs"));

        Assert.IsFalse(contracts.Contains("public sealed record Nexa", StringComparison.Ordinal));
        Assert.IsFalse(services.Contains("public sealed class Nexa", StringComparison.Ordinal));
        StringAssert.Contains(contracts, "NodalOsScheduledReadOnlySchedule");
        StringAssert.Contains(services, "NodalOsScheduledReadOnlyRunValidator");
    }

    private void AssertForbiddenPreviewOperation(string operation)
    {
        var preview = NodalOsScheduledReadOnlyRunFixtures.ValidDryRunPreview() with
        {
            PlannedReadOnlyOperations = [operation]
        };
        var result = validator.ValidatePreview(preview);

        Assert.IsFalse(result.IsValid, operation);
        AssertContains(result.Errors, "forbidden");
    }

    private static void AssertNoSchedulerImplementationInNewSource()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.AgentOperations.Core", "NodalOsScheduledReadOnlyRunServices.cs"));

        Assert.IsFalse(source.Contains("System.Threading.Timer", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("PeriodicTimer", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("BackgroundService", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("IHostedService", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("Quartz", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("CronExpression", StringComparison.Ordinal));
    }

    private static string ReadArtifact() =>
        File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "artifacts",
            "agent-operations",
            "m436",
            "scheduled-read-only-run-contracts-v1-summary.json"));

    private static void AssertContains(IEnumerable<string> values, string expected)
    {
        Assert.IsTrue(
            values.Any(value => value.Contains(expected, StringComparison.OrdinalIgnoreCase)),
            $"Expected a validation message containing '{expected}'.");
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
