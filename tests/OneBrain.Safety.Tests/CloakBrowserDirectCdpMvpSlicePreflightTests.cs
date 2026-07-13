using OneBrain.BrowserRuntime;
using System.Security.Cryptography;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class CloakBrowserDirectCdpMvpSlicePreflightTests
{
    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    [TestCategory("MvpVerticalSlice")]
    public void PreflightClassifiesMissingBinaryAsExternalBlockerWithoutUsingFallbacks()
    {
        var result = new CloakBrowserDirectCdpMvpSlicePreflight().Evaluate(
            FindRepositoryRoot(),
            Path.Combine(FindRepositoryRoot(), "browser-runtime.lock.json"),
            runtimeArtifactPath: Path.Combine(Path.GetTempPath(), "missing-cloakbrowser.exe"),
            environment: new Dictionary<string, string?>());

        Assert.AreEqual("BLOCKED_EXTERNAL_CLOAKBROWSER_BINARY", result.Decision);
        Assert.AreEqual("REAL_PINNED_RUNTIME", result.RuntimeClassification);
        Assert.AreEqual("CONFIGURED_BUT_MISSING_BINARY", result.BinaryClassification);
        Assert.IsTrue(result.LockValid);
        Assert.IsTrue(result.RuntimeArtifactPinned);
        Assert.IsTrue(result.RuntimeArtifactConfigured);
        Assert.IsFalse(result.RuntimeArtifactPresent);
        Assert.IsFalse(result.LaunchAllowed);
        Assert.IsFalse(result.SystemBrowserAllowed);
        Assert.IsFalse(result.ExtensionEnabled);
        Assert.IsTrue(result.SystemBrowserFallbackRejected);
        Assert.IsTrue(result.PlaywrightDefaultRejected);
        Assert.IsTrue(result.ChromeLabExtensionRejectedAsRuntime);
        Assert.IsFalse(result.RuntimeSmokeAvailable);
        Assert.IsTrue(result.EvidenceSanitized);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    [TestCategory("MvpVerticalSlice")]
    public void PreflightClassifiesHashMismatchWithoutLaunchingRuntime()
    {
        var artifactPath = Path.Combine(Path.GetTempPath(), "nodal-fake-cloakbrowser-" + Guid.NewGuid().ToString("N") + ".exe");
        File.WriteAllText(artifactPath, "not the pinned runtime");

        try
        {
            var result = new CloakBrowserDirectCdpMvpSlicePreflight().Evaluate(
                FindRepositoryRoot(),
                Path.Combine(FindRepositoryRoot(), "browser-runtime.lock.json"),
                runtimeArtifactPath: artifactPath,
                environment: new Dictionary<string, string?>());

            Assert.AreEqual("BLOCKED_CLOAKBROWSER_BINARY_HASH_MISMATCH", result.Decision);
            Assert.AreEqual("CONFIGURED_BUT_HASH_MISMATCH", result.BinaryClassification);
            Assert.IsTrue(result.RuntimeArtifactPresent);
            Assert.IsFalse(result.RuntimeArtifactHashMatches);
            Assert.AreEqual("MISMATCH", result.ActualBinarySha256Status);
            Assert.IsFalse(result.LaunchAllowed);
            Assert.AreEqual("REPLACE_BINARY_WITH_PINNED_HASH_OR_UPDATE_LOCK_AFTER_AUDIT", result.NextStep);
        }
        finally
        {
            File.Delete(artifactPath);
        }
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    [TestCategory("MvpVerticalSlice")]
    public void PreflightRejectsChromeLabExtensionAsCanonicalRuntime()
    {
        Assert.IsTrue(CloakBrowserDirectCdpMvpSlicePreflight.IsChromeLabExtensionPath(
            "browser-extension/onebrain-chrome-lab/manifest.json"));
        Assert.IsFalse(CloakBrowserDirectCdpMvpSlicePreflight.IsChromeLabExtensionPath(
            "src/OneBrain.BrowserRuntime/CloakBrowserRuntimeProvider.cs"));
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    [TestCategory("MvpVerticalSlice")]
    public void PreflightDoesNotExposeRawArtifactPathInEvidenceFields()
    {
        var fakePath = Path.Combine(Path.GetTempPath(), "missing-cloakbrowser-" + Guid.NewGuid().ToString("N") + ".exe");
        var result = new CloakBrowserDirectCdpMvpSlicePreflight().Evaluate(
            FindRepositoryRoot(),
            Path.Combine(FindRepositoryRoot(), "browser-runtime.lock.json"),
            runtimeArtifactPath: fakePath,
            environment: new Dictionary<string, string?>());

        var serialized = string.Join(
            "|",
            result.Decision,
            result.RuntimeClassification,
            result.BinaryClassification,
            result.Blocker,
            result.NextStep,
            result.ExpectedBinarySha256,
            result.ActualBinarySha256Status);

        Assert.IsFalse(serialized.Contains(fakePath, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serialized.Contains(Environment.UserName, StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    [TestCategory("MvpVerticalSlice")]
    public void PreflightCanRecognizeAvailableLocalBinaryWhenHashMatches()
    {
        var bytes = "fixture cloakbrowser binary"u8.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var tempRoot = Path.Combine(Path.GetTempPath(), "nodal-cloakbrowser-preflight-" + Guid.NewGuid().ToString("N"));
        var artifactPath = Path.Combine(tempRoot, ".cloakbrowser", "chromium-146.0.7680.177.5", "chrome.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(artifactPath)!);
        File.WriteAllBytes(artifactPath, bytes);
        var lockPath = Path.Combine(tempRoot, "browser-runtime.lock.json");
        File.WriteAllText(
            lockPath,
            $$"""
            {
              "provider": "cloakbrowser",
              "mode": "cdp-direct",
              "extension_enabled": false,
              "runtime_source": "fork",
              "runtime_repo": "nodal-cloakbrowser-runtime",
              "runtime_channel": "nodal-runtime",
              "runtime_path_policy": "env-or-local-config",
              "runtime_version": "146.0.7680.177.5",
              "runtime_commit": "8432254124667a3d2742b1727132d8a045e115da",
              "upstream_commit": "0bb3737a29d9133f6207793eb0eeeefe36c9d910",
              "binary_sha256": "{{hash}}",
              "cdp_host": "127.0.0.1",
              "cdp_port_policy": "ephemeral-or-reserved",
              "system_browser_allowed": false
            }
            """);

        try
        {
            var result = new CloakBrowserDirectCdpMvpSlicePreflight().Evaluate(
                tempRoot,
                lockPath,
                runtimeArtifactPath: artifactPath,
                environment: new Dictionary<string, string?>());

            Assert.AreEqual("GO_CLOAKBROWSER_DIRECT_CDP_MVP_SLICE_PREFLIGHT_READY", result.Decision);
            Assert.AreEqual("AVAILABLE_LOCAL_BINARY", result.BinaryClassification);
            Assert.IsTrue(result.RuntimeArtifactPresent);
            Assert.IsTrue(result.RuntimeArtifactHashMatches);
            Assert.AreEqual("MATCH", result.ActualBinarySha256Status);
            Assert.IsTrue(result.LaunchAllowed);
            Assert.IsTrue(result.RuntimeSmokeAvailable);
            Assert.AreEqual("RUN_CLOAKBROWSER_CDP_LIVE_SMOKE", result.NextStep);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "browser-runtime.lock.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("Repository root with browser-runtime.lock.json was not found.");
        return string.Empty;
    }
}
