using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OnnxOutOfProcessGuard")]
[TestCategory("OnnxOcrProbeRunner")]
[TestCategory("OnnxRuntimeCrashIsolation")]
[TestCategory("OnnxNativeRuntimeCrash")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsOnnxOutOfProcessGuardM210Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    private static string Configuration =>
        AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release") ? "Release" : "Debug";

    // Locates the runner's own built exe (built as part of the solution before --no-build test).
    private static string? RunnerExePath()
    {
        var exe = Path.Combine(RepoRoot, "tools", "onnx-ocr-probe-runner", "bin", Configuration, "net10.0",
            "OneBrain.Tools.OnnxOcrProbeRunner.exe");
        return File.Exists(exe) ? exe : null;
    }

    private static NodalOsOnnxNativeRuntimeCrashProbeRequest SafeProbe(
        NodalOsOnnxNativeRuntimeCrashFixtureKind kind = NodalOsOnnxNativeRuntimeCrashFixtureKind.StripeSafe) =>
        new(
            $"probe-{Guid.NewGuid():N}",
            kind,
            NodalOsSyntheticOcrTextRenderMode.AntiAliasedPixelFont,
            64, 32,
            NodalOsOnnxNativeRuntimeCrashStage.SessionCreation,
            NodalOsOcrVisionSensitivity.Low,
            FullScreen: false, Sensitive: false, OriginalRawPersisted: false,
            Synthetic: true, NoAuthority: true, RunOutOfProcess: true);

    private static NodalOsOnnxOutOfProcessGuardRequest GuardRequest(
        string runner, IEnumerable<string> args, int timeoutMs = 15000,
        NodalOsOnnxNativeRuntimeCrashProbeRequest? probe = null) =>
        new(
            $"guard-{Guid.NewGuid():N}",
            probe ?? SafeProbe(),
            runner,
            args.ToList(),
            timeoutMs,
            MaxOutputBytes: 64 * 1024,
            AllowRawPersistence: false);

    private static int CountRunnerProcesses()
    {
        try
        {
            return Process.GetProcessesByName("OneBrain.Tools.OnnxOcrProbeRunner").Length;
        }
        catch
        {
            return 0;
        }
    }

    [TestMethod]
    public void Parent_MapsChildCrashExitCode_To_NativeRuntimeCrash()
    {
        var runner = RunnerExePath();
        if (runner is null) { Assert.Inconclusive("Probe runner not built; skipping."); return; }

        var result = new NodalOsOnnxOutOfProcessGuard().Run(
            GuardRequest(runner, new[] { "--self-test", "crash" }));

        Assert.IsTrue(result.ParentSurvived);
        Assert.IsTrue(result.ChildLaunched);
        Assert.AreEqual(NodalOsOnnxNativeRuntimeCrashProbeStatus.NativeRuntimeCrash, result.ProbeResult.Status);
        Assert.AreEqual(NodalOsOnnxNativeRuntimeCrashKind.NativeAccessViolation, result.ProbeResult.CrashKind);
        Assert.IsTrue(result.ProbeResult.HostSurvived);
    }

    [TestMethod]
    public void Parent_MapsTimeout_To_TimedOut()
    {
        var runner = RunnerExePath();
        if (runner is null) { Assert.Inconclusive("Probe runner not built; skipping."); return; }

        var result = new NodalOsOnnxOutOfProcessGuard().Run(
            GuardRequest(runner, new[] { "--self-test", "timeout" }, timeoutMs: 1500));

        Assert.IsTrue(result.ParentSurvived);
        Assert.IsTrue(result.TimedOut);
        Assert.AreEqual(NodalOsOnnxNativeRuntimeCrashProbeStatus.TimedOut, result.ProbeResult.Status);
        Assert.IsFalse(result.OrphanProcessLeft);
    }

    [TestMethod]
    public void Parent_MapsInvalidJson_To_BlockedByModelRuntime()
    {
        var runner = RunnerExePath();
        if (runner is null) { Assert.Inconclusive("Probe runner not built; skipping."); return; }

        var result = new NodalOsOnnxOutOfProcessGuard().Run(
            GuardRequest(runner, new[] { "--self-test", "garbage" }));

        Assert.IsTrue(result.ParentSurvived);
        Assert.AreEqual(NodalOsOnnxNativeRuntimeCrashKind.InvalidOutput, result.ProbeResult.CrashKind);
        Assert.AreEqual(NodalOsOnnxNativeRuntimeCrashProbeStatus.BlockedByModelRuntime, result.ProbeResult.Status);
    }

    [TestMethod]
    public void Parent_MapsNonZeroExit_To_ProcessCrashed()
    {
        var runner = RunnerExePath();
        if (runner is null) { Assert.Inconclusive("Probe runner not built; skipping."); return; }

        var result = new NodalOsOnnxOutOfProcessGuard().Run(
            GuardRequest(runner, new[] { "--self-test", "nonzero" }));

        Assert.IsTrue(result.ParentSurvived);
        Assert.AreEqual(7, result.ExitCode);
        Assert.AreEqual(NodalOsOnnxNativeRuntimeCrashProbeStatus.ProcessCrashed, result.ProbeResult.Status);
    }

    [TestMethod]
    public void SafeFixture_RunsThroughChild_WithoutCrashingParent()
    {
        var runner = RunnerExePath();
        if (runner is null) { Assert.Inconclusive("Probe runner not built; skipping."); return; }

        var result = new NodalOsOnnxOutOfProcessGuard().Run(
            GuardRequest(runner, new[] { "--self-test", "safe" }));

        Assert.IsTrue(result.ParentSurvived);
        Assert.IsTrue(result.ChildLaunched);
        Assert.AreEqual(0, result.ExitCode);
        Assert.AreEqual(NodalOsOnnxNativeRuntimeCrashProbeStatus.NoTextDetected, result.ProbeResult.Status);
        Assert.IsTrue(result.NoAuthority);
        Assert.IsFalse(result.CallsSaas);
        Assert.IsFalse(result.RawPersisted);
    }

    [TestMethod]
    public void Parent_CleansTempFiles()
    {
        var runner = RunnerExePath();
        if (runner is null) { Assert.Inconclusive("Probe runner not built; skipping."); return; }

        var result = new NodalOsOnnxOutOfProcessGuard().Run(
            GuardRequest(runner, new[] { "--self-test", "safe" }));

        Assert.IsTrue(result.TempFilesCleaned);
    }

    [TestMethod]
    public void Parent_DoesNotPersistRawImage()
    {
        var runner = RunnerExePath();
        if (runner is null) { Assert.Inconclusive("Probe runner not built; skipping."); return; }

        var result = new NodalOsOnnxOutOfProcessGuard().Run(
            GuardRequest(runner, new[] { "--self-test", "safe" }));

        Assert.IsFalse(result.RawPersisted);
        Assert.IsFalse(result.ProbeResult.RawPersisted);
    }

    [TestMethod]
    public void Parent_BlocksSensitiveFixture_BeforeLaunch()
    {
        var runner = RunnerExePath() ?? "dotnet";

        var sensitiveProbe = SafeProbe() with { Sensitive = true };
        var result = new NodalOsOnnxOutOfProcessGuard().Run(
            GuardRequest(runner, new[] { "--self-test", "safe" }, probe: sensitiveProbe));

        Assert.IsTrue(result.BlockedBeforeLaunch);
        Assert.IsFalse(result.ChildLaunched);
        Assert.AreEqual(NodalOsOnnxNativeRuntimeCrashProbeStatus.UnsupportedFixture, result.ProbeResult.Status);
    }

    [TestMethod]
    public void Parent_BlocksFullScreenFixture_BeforeLaunch()
    {
        var runner = RunnerExePath() ?? "dotnet";

        var fullScreenProbe = SafeProbe() with { FullScreen = true };
        var result = new NodalOsOnnxOutOfProcessGuard().Run(
            GuardRequest(runner, new[] { "--self-test", "safe" }, probe: fullScreenProbe));

        Assert.IsTrue(result.BlockedBeforeLaunch);
        Assert.IsFalse(result.ChildLaunched);
    }

    [TestMethod]
    public void Parent_PreservesNoAuthority()
    {
        var runner = RunnerExePath();
        if (runner is null) { Assert.Inconclusive("Probe runner not built; skipping."); return; }

        var result = new NodalOsOnnxOutOfProcessGuard().Run(
            GuardRequest(runner, new[] { "--self-test", "safe" }));

        Assert.IsTrue(result.NoAuthority);
        Assert.IsTrue(result.ProbeResult.NoAuthority);
    }

    [TestMethod]
    public void NoOrphanChildProcess_AfterRun()
    {
        var runner = RunnerExePath();
        if (runner is null) { Assert.Inconclusive("Probe runner not built; skipping."); return; }

        var before = CountRunnerProcesses();

        var crash = new NodalOsOnnxOutOfProcessGuard().Run(GuardRequest(runner, new[] { "--self-test", "crash" }));
        var timeout = new NodalOsOnnxOutOfProcessGuard().Run(GuardRequest(runner, new[] { "--self-test", "timeout" }, timeoutMs: 1500));

        Assert.IsFalse(crash.OrphanProcessLeft);
        Assert.IsFalse(timeout.OrphanProcessLeft);

        // Allow brief teardown, then confirm we did not leak runner processes.
        Thread.Sleep(500);
        var after = CountRunnerProcesses();
        Assert.IsTrue(after <= before, $"orphan runner processes detected: before={before}, after={after}");
    }

    [TestMethod]
    [Ignore("M211 quarantine: real ONNX text-fixture probe crashes the child by design; run manually to confirm guard containment.")]
    public void QuarantinedTextFixture_AttemptedOnlyThroughGuard_ParentSurvives()
    {
        var runner = RunnerExePath();
        if (runner is null) { Assert.Inconclusive("Probe runner not built; skipping."); return; }

        var textProbe = SafeProbe(NodalOsOnnxNativeRuntimeCrashFixtureKind.AntiAliasedPixelFontText) with
        {
            Width = 256,
            Height = 96,
            Stage = NodalOsOnnxNativeRuntimeCrashStage.DetectionRun
        };

        var result = new NodalOsOnnxOutOfProcessGuard().Run(
            GuardRequest(runner, new[] { "--probe", "--repo-root", RepoRoot }, timeoutMs: 60000, probe: textProbe));

        // The parent must survive regardless of whether the child crashed.
        Assert.IsTrue(result.ParentSurvived);
        Assert.IsFalse(result.OrphanProcessLeft);
        Assert.IsTrue(result.TempFilesCleaned);
    }

    [TestMethod]
    public void SafeFixture_RealOnnxThroughChild_ParentSurvives()
    {
        var runner = RunnerExePath();
        if (runner is null) { Assert.Inconclusive("Probe runner not built; skipping."); return; }

        var modelDir = Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx");
        if (!File.Exists(Path.Combine(modelDir, "ch_PP-OCRv4_det.onnx")))
        {
            Assert.Inconclusive("Real ONNX models not present; skipping real-child probe.");
            return;
        }

        var stripeProbe = SafeProbe(NodalOsOnnxNativeRuntimeCrashFixtureKind.StripeSafe) with
        {
            Width = 64,
            Height = 32,
            Stage = NodalOsOnnxNativeRuntimeCrashStage.DetectionRun
        };

        var result = new NodalOsOnnxOutOfProcessGuard().Run(
            GuardRequest(runner, new[] { "--probe", "--repo-root", RepoRoot }, timeoutMs: 60000, probe: stripeProbe));

        Assert.IsTrue(result.ParentSurvived);
        Assert.IsFalse(result.OrphanProcessLeft);
        Assert.IsTrue(result.TempFilesCleaned);
        // Safe shape must not crash the child: expect a controlled NoTextDetected/Passed (or a contained block).
        Assert.AreNotEqual(NodalOsOnnxNativeRuntimeCrashProbeStatus.NativeRuntimeCrash, result.ProbeResult.Status);
    }
}
