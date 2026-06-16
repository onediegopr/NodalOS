using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("LocalWorkerProcessIsolation")]
[TestCategory("LocalWorkerProcessBoundary")]
[TestCategory("LocalWorkerIpcSecurity")]
[TestCategory("LocalOcrWorkerOutOfProcess")]
[TestCategory("LocalOcrWorkerIpcContract")]
[TestCategory("LocalOcrWorkerRuntimeIsolation")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("BrowserGroundingSnapshot")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsLocalWorkerProcessIsolationM189Tests
{
    private static NodalOsLocalWorkerProcessLaunchSpec EchoSpec(string workerId, int timeoutMs = 500) =>
        new(
            workerId,
            NodalOsLocalWorkerProcessLauncher.InnocentEchoContractVersion,
            "cmd.exe",
            ["/c", "echo", "status=ok", "nodal-echo"],
            timeoutMs,
            InnocentEchoOnly: true,
            NoAuthority: true,
            Redacted: true);

    private static NodalOsLocalWorkerIpcSecurityPolicy IpcPolicy(string token) =>
        new(
            NodalOsLocalWorkerProcessLauncher.InnocentEchoContractVersion,
            token,
            MaxMessageBytes: 4096,
            MaxLifetimeMs: 2000,
            RequireAuthToken: true,
            ValidateContractVersion: true,
            NoAuthority: true);

    private static NodalOsLocalWorkerProcessMessage Message(
        string token,
        string payload = "{\"status\":\"ok\"}",
        string? version = null,
        DateTimeOffset? sentAt = null) =>
        new(
            $"msg-{Guid.NewGuid():N}",
            version ?? NodalOsLocalWorkerProcessLauncher.InnocentEchoContractVersion,
            token,
            payload,
            sentAt ?? DateTimeOffset.UtcNow,
            NoAuthority: true);

    [TestMethod]
    public void SimulatedEcho_ReturnsOkWithoutLaunchingProcess()
    {
        var launcher = new NodalOsLocalWorkerProcessLauncher();
        var result = launcher.SimulateEcho(EchoSpec("sim-1"), launcher.DefaultPolicy());

        Assert.AreEqual(NodalOsLocalWorkerProcessDecision.SimulatedOnly, result.Decision);
        Assert.IsTrue(result.NoAuthority);
        Assert.IsFalse(result.RawPersisted);
        Assert.IsFalse(result.NetworkObserved);
        Assert.IsFalse(result.FilesystemWriteObserved);
        Assert.IsTrue(result.Warnings.Any(w => w.Contains("simulated", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void InnocentEchoProcess_LaunchesAndReturnsOk()
    {
        var launcher = new NodalOsLocalWorkerProcessLauncher();
        var policy = launcher.DefaultPolicy() with { AllowExternalProcess = true };
        var result = launcher.LaunchInnocentEcho(EchoSpec("echo-1"), policy);

        Assert.AreEqual(NodalOsLocalWorkerProcessDecision.Launched, result.Decision);
        Assert.AreEqual(0, result.ExitCode);
        Assert.IsTrue(result.StdoutRedacted.Contains("status=ok", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(result.NoAuthority);
        Assert.IsFalse(result.RawPersisted);
        Assert.IsFalse(result.NetworkObserved);
    }

    [TestMethod]
    public void ExternalProcessBlockedByPolicy()
    {
        var launcher = new NodalOsLocalWorkerProcessLauncher();
        var policy = launcher.DefaultPolicy(); // AllowExternalProcess = false
        var result = launcher.LaunchInnocentEcho(EchoSpec("echo-2"), policy);

        Assert.AreEqual(NodalOsLocalWorkerProcessDecision.RejectedByPolicy, result.Decision);
        Assert.IsTrue(result.Warnings.Any(w => w.Contains("blocked", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void NonInnocentExecutable_Rejected()
    {
        var launcher = new NodalOsLocalWorkerProcessLauncher();
        var policy = launcher.DefaultPolicy() with { AllowExternalProcess = true };
        var spec = EchoSpec("echo-3") with { ExecutablePath = "malicious.exe" };
        var result = launcher.LaunchInnocentEcho(spec, policy);

        Assert.AreEqual(NodalOsLocalWorkerProcessDecision.RejectedByPolicy, result.Decision);
    }

    [TestMethod]
    public void TimeoutExceeded_KillsOrBlocks()
    {
        var launcher = new NodalOsLocalWorkerProcessLauncher();
        var policy = launcher.DefaultPolicy() with { AllowExternalProcess = true };
        var spec = EchoSpec("echo-timeout", timeoutMs: 1) with
        {
            Arguments = ["/c", "timeout", "/t", "5"]
        };
        var result = launcher.LaunchInnocentEcho(spec, policy);

        Assert.IsTrue(result.Decision is NodalOsLocalWorkerProcessDecision.TimedOut or NodalOsLocalWorkerProcessDecision.LaunchFailed);
        if (result.Decision == NodalOsLocalWorkerProcessDecision.TimedOut)
        {
            Assert.IsTrue(result.Killed);
        }
    }

    [TestMethod]
    public void IpcMissingAuthToken_Rejected()
    {
        var channel = new NodalOsLocalWorkerIpcChannel();
        var policy = IpcPolicy("secret-token");
        var message = Message("");

        var health = channel.ValidateMessage(message, policy);

        Assert.IsFalse(health.IsResponsive);
        Assert.IsFalse(health.AuthTokenValid);
    }

    [TestMethod]
    public void IpcInvalidAuthToken_Rejected()
    {
        var channel = new NodalOsLocalWorkerIpcChannel();
        var policy = IpcPolicy("secret-token");
        var message = Message("wrong-token");

        var health = channel.ValidateMessage(message, policy);

        Assert.IsFalse(health.IsResponsive);
        Assert.IsFalse(health.AuthTokenValid);
    }

    [TestMethod]
    public void IpcInvalidContractVersion_Rejected()
    {
        var channel = new NodalOsLocalWorkerIpcChannel();
        var policy = IpcPolicy("secret-token");
        var message = Message("secret-token", version: "v0.broken");

        var health = channel.ValidateMessage(message, policy);

        Assert.IsFalse(health.IsResponsive);
        Assert.IsFalse(health.ContractVersionMatch);
    }

    [TestMethod]
    public void IpcOversizeMessage_Rejected()
    {
        var channel = new NodalOsLocalWorkerIpcChannel();
        var policy = IpcPolicy("secret-token") with { MaxMessageBytes = 10 };
        var message = Message("secret-token", payload: new string('x', 100));

        var health = channel.ValidateMessage(message, policy);

        Assert.IsFalse(health.IsResponsive);
        Assert.IsFalse(health.WithinSizeLimits);
    }

    [TestMethod]
    public void IpcExpiredMessage_Rejected()
    {
        var channel = new NodalOsLocalWorkerIpcChannel();
        var policy = IpcPolicy("secret-token") with { MaxLifetimeMs = 1 };
        var message = Message("secret-token", sentAt: DateTimeOffset.UtcNow.AddMinutes(-5));

        var health = channel.ValidateMessage(message, policy);

        Assert.IsFalse(health.IsResponsive);
        Assert.IsFalse(health.WithinTimeoutLimits);
    }

    [TestMethod]
    public void IpcValidMessage_Accepted()
    {
        var channel = new NodalOsLocalWorkerIpcChannel();
        var policy = IpcPolicy("secret-token");
        var message = Message("secret-token");

        var health = channel.ValidateMessage(message, policy);

        Assert.IsTrue(health.IsResponsive);
        Assert.IsTrue(health.AuthTokenValid);
        Assert.IsTrue(health.ContractVersionMatch);
        Assert.IsTrue(health.WithinSizeLimits);
        Assert.IsTrue(health.WithinTimeoutLimits);
        Assert.IsTrue(health.NoAuthority);
    }

    [TestMethod]
    public void SandboxHonestEvaluation_SimulatedIsModeledNotEnforced()
    {
        var launcher = new NodalOsLocalWorkerProcessLauncher();
        var sandbox = new NodalOsLocalWorkerProcessSandbox();
        var policy = launcher.DefaultPolicy();
        var result = launcher.SimulateEcho(EchoSpec("sim-eval"), policy);
        var channel = new NodalOsLocalWorkerIpcChannel();
        var health = channel.ValidateMessage(Message("token"), IpcPolicy("token"));

        var evidence = sandbox.Evaluate(policy, result, health);

        Assert.AreEqual(NodalOsLocalWorkerIsolationEnforcementLevel.Modeled, evidence.NetworkIsolation);
        Assert.AreEqual(NodalOsLocalWorkerIsolationEnforcementLevel.Modeled, evidence.FilesystemIsolation);
        Assert.AreEqual(NodalOsLocalWorkerIsolationEnforcementLevel.Observed, evidence.ProcessIsolation);
        Assert.IsTrue(evidence.Notes.Contains("simulated", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(evidence.Notes.Contains("violated", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(evidence.NoNetworkIntentObserved);
        Assert.IsTrue(evidence.NoFilesystemWriteObserved);
    }

    [TestMethod]
    public void SafeDeserialization_RejectUnexpectedPayload()
    {
        var channel = new NodalOsLocalWorkerIpcChannel();
        var ok = channel.TryDeserializePayload<NodalOsLocalWorkerProcessMessage>("not valid json", out _, out var error);
        Assert.IsFalse(ok);
        Assert.IsFalse(string.IsNullOrWhiteSpace(error));
    }

    [TestMethod]
    public void NoOcrReal_NoPython_NoPaddle_NoTesseract()
    {
        var launcher = new NodalOsLocalWorkerProcessLauncher();
        var policy = launcher.DefaultPolicy();
        Assert.IsFalse(policy.AllowRealOcr);
        Assert.IsFalse(policy.AllowPython);
        Assert.IsFalse(policy.AllowNetwork);
        Assert.IsFalse(policy.AllowRawPersistence);
        Assert.IsTrue(policy.NoAuthority);
    }
}
