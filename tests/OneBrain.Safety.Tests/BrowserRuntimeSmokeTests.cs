using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;

namespace OneBrain.Safety.Tests;

[TestClass]
[DoNotParallelize]
public sealed class BrowserRuntimeSmokeTests
{
    [TestMethod]
    public async Task BrowserRuntimeSmokeRunnerExecutesAllGatesOnFixture()
    {
        var report = await RunSmokeAsync();

        Assert.IsTrue(report.Passed, string.Join(Environment.NewLine, report.Gates.Select(gate => $"{gate.GateName}: {gate.Status} {gate.Diagnostic.ErrorCode} {gate.Diagnostic.Message}")));
        Assert.AreEqual(10, report.Gates.Count);
        CollectionAssert.AreEquivalent(
            new[]
            {
                "Gate 1 - Control Plane / Launcher",
                "Gate 2 - Target Discovery",
                "Gate 3 - TargetContext",
                "Gate 4 - Observe",
                "Gate 5 - Act",
                "Gate 6 - Verify",
                "Gate 7 - Liveness / Stale",
                "Gate 8 - Timeout / Cancel",
                "Gate 9 - Idempotency / Replay Safety",
                "Gate 10 - Cleanup"
            },
            report.Gates.Select(gate => gate.GateName).ToArray());
    }

    [TestMethod]
    public async Task BrowserRuntimeSmokeReportContainsStructuredDiagnostics()
    {
        var report = await RunSmokeAsync();

        Assert.IsFalse(string.IsNullOrWhiteSpace(report.ReportId));
        Assert.IsTrue(report.Duration >= TimeSpan.Zero);
        Assert.IsFalse(report.ContainsSecrets);
        Assert.AreEqual("ChromeCdp", report.FinalHealth.RuntimeKind);
        Assert.AreEqual("Temporary", report.FinalHealth.ProfileMode);
        Assert.IsFalse(report.FinalHealth.UsesRealProfile);
        Assert.IsTrue(report.FinalHealth.CleanupCompleted);

        foreach (var gate in report.Gates)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(gate.GateName));
            Assert.IsFalse(string.IsNullOrWhiteSpace(gate.Diagnostic.Message));
            Assert.IsTrue(gate.Duration >= TimeSpan.Zero);
        }
    }

    [TestMethod]
    public async Task BrowserRuntimeSmokeRunnerReportsEnvironmentUnsupportedWhenBrowserMissing()
    {
        var report = await new BrowserRuntimeSmokeRunner().RunAsync(new BrowserRuntimeSmokeOptions(
            BrowserExecutablePath: Path.Combine(Path.GetTempPath(), "missing-browser.exe"),
            FixtureUri: FixtureUri()));

        Assert.IsFalse(report.Passed);
        Assert.AreEqual(BrowserRuntimeGateStatus.Skipped, report.Gates[0].Status);
        Assert.AreEqual(BrowserRuntimeErrorCode.EnvironmentUnsupported, report.Gates[0].Diagnostic.ErrorCode);
    }

    [TestMethod]
    public async Task BrowserRuntimeSmokeVerifyGatePreservesUncertainNotDone()
    {
        var report = await RunSmokeAsync();
        var gate = report.Gates.Single(result => result.GateName == "Gate 6 - Verify");

        Assert.AreEqual(BrowserRuntimeGateStatus.Passed, gate.Status);
        StringAssert.Contains(gate.Diagnostic.Message, "Uncertain != Done");
    }

    [TestMethod]
    public async Task BrowserRuntimeSmokeStaleGateBlocksModifyingAction()
    {
        var report = await RunSmokeAsync();
        var gate = report.Gates.Single(result => result.GateName == "Gate 7 - Liveness / Stale");

        Assert.AreEqual(BrowserRuntimeGateStatus.Passed, gate.Status);
        StringAssert.Contains(gate.Diagnostic.Message, "stale generation blocks modifying action");
    }

    [TestMethod]
    public async Task BrowserRuntimeSmokeTimeoutGateDoesNotHangAndReportsActionFailure()
    {
        var report = await RunSmokeAsync();
        var gate = report.Gates.Single(result => result.GateName == "Gate 8 - Timeout / Cancel");

        Assert.AreEqual(BrowserRuntimeGateStatus.Passed, gate.Status);
        StringAssert.Contains(gate.Diagnostic.Message, "did not hang");
    }

    [TestMethod]
    public async Task BrowserRuntimeSmokeIdempotencyGateReportsDuplicateReplay()
    {
        var report = await RunSmokeAsync();
        var gate = report.Gates.Single(result => result.GateName == "Gate 9 - Idempotency / Replay Safety");

        Assert.AreEqual(BrowserRuntimeGateStatus.Passed, gate.Status);
        StringAssert.Contains(gate.Diagnostic.Message, "Duplicate modifying action rejected");
    }

    [TestMethod]
    public async Task BrowserRuntimeSmokeCleanupLeavesNoManagedProcessPortOrProfile()
    {
        CleanupManagedCdpTempDirectories();
        try
        {
            var report = await RunSmokeAsync();
            var cleanup = report.Gates.Single(result => result.GateName == "Gate 10 - Cleanup");

            Assert.AreEqual(BrowserRuntimeGateStatus.Passed, cleanup.Status);
            Assert.IsTrue(report.FinalHealth.CleanupCompleted);
            Assert.IsFalse(Directory.EnumerateDirectories(Path.GetTempPath(), "onebrain-cdp-*").Any());
        }
        finally
        {
            CleanupManagedCdpTempDirectories();
        }
    }

    [TestMethod]
    public async Task BrowserRuntimeSmokeReportDoesNotContainSecretsOrSensitiveProfilePaths()
    {
        var report = await RunSmokeAsync();
        var text = string.Join("\n", report.Gates.Select(gate => $"{gate.GateName} {gate.Diagnostic.Message} {gate.Diagnostic.Health?.BrowserExecutable}"));

        Assert.IsFalse(text.Contains("sk-", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("bearer ", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("authorization", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("cookie", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("refresh_token", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("access_token", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("User Data", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("AppData", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<BrowserRuntimeSmokeReport> RunSmokeAsync()
    {
        var browser = ChromeCdpBrowserLauncher.FindBrowserExecutable();
        if (browser is null)
            Assert.Inconclusive("Chrome/Edge executable is not available in this environment.");

        return await new BrowserRuntimeSmokeRunner().RunAsync(new BrowserRuntimeSmokeOptions(browser, FixtureUri()));
    }

    private static Uri FixtureUri()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;

        Assert.IsNotNull(dir, "repo root not found");
        return new Uri(Path.Combine(dir.FullName, "tests", "fixtures", "browser-executor", "basic-form.html"));
    }

    private static void CleanupManagedCdpTempDirectories()
    {
        foreach (var directory in Directory.EnumerateDirectories(Path.GetTempPath(), "onebrain-cdp-*"))
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
                // A live process can still hold a temp profile; the assertion exposes real cleanup failures.
            }
            catch (UnauthorizedAccessException)
            {
                // Keep the test honest instead of hiding locked or permission-blocked temp profiles.
            }
        }
    }
}
