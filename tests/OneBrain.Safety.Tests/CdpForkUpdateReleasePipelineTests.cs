using System.Diagnostics;
using OneBrain.BrowserRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ForkUpdateReleasePipeline")]
public sealed class CdpForkUpdateReleasePipelineTests
{
    private const string ScriptPath = "scripts/verify-cloakbrowser-cdp-fork-update-release-pipeline.ps1";
    private const string RunbookPath = "docs/browser-runtime/cloakbrowser-fork-update-release-pipeline.md";

    [TestMethod]
    public void ForkUpdateReleasePipeline_EvaluatorVerifiesPinnedRuntimeArtifact()
    {
        var root = RepoRoot();
        var runtimeRepo = RuntimeRepoRoot();
        var result = new CdpForkUpdateReleasePipeline().Evaluate(
            new CdpForkUpdateReleasePipelineRequest(
                RepositoryRoot: root,
                RuntimeRepositoryPath: runtimeRepo,
                LockfilePath: Path.Combine(root, "browser-runtime.lock.json"),
                LocalConfigPath: Path.Combine(root, ".local", "browser-runtime.local.json"),
                RuntimeOriginUrl: Git(runtimeRepo, "remote", "get-url", "origin"),
                RuntimeUpstreamUrl: Git(runtimeRepo, "remote", "get-url", "upstream"),
                RuntimeBranch: Git(runtimeRepo, "branch", "--show-current"),
                RuntimeHead: Git(runtimeRepo, "rev-parse", "HEAD")));

        Assert.IsTrue(result.IsReady, string.Join(", ", result.Errors));
        Assert.AreEqual("nodal-cloakbrowser-runtime", result.RuntimeRepository);
        Assert.AreEqual("nodal/runtime", result.RuntimeBranch);
        Assert.AreEqual("146.0.7680.177.5", result.RuntimeVersion);
        Assert.IsTrue(result.LockfileValid);
        Assert.IsTrue(result.LocalConfigPresent);
        Assert.IsTrue(result.RuntimeRepositoryPresent);
        Assert.IsTrue(result.RuntimeArtifactPresent);
        Assert.IsTrue(result.RuntimeArtifactInsideFork);
        Assert.IsTrue(result.RuntimeArtifactUnderManagedCache);
        Assert.IsTrue(result.ArtifactHashVerified);
        Assert.IsFalse(result.ExtensionUsed);
        Assert.IsFalse(result.SystemBrowserUsed);
        Assert.IsFalse(result.ExtensionFallbackAllowed);
        Assert.IsFalse(result.SystemBrowserFallbackAllowed);
    }

    [TestMethod]
    public void ForkUpdateReleasePipeline_LockfilePinsForkRuntimeMetadata()
    {
        var runtimeLock = BrowserRuntimeLock.Load(Path.Combine(RepoRoot(), "browser-runtime.lock.json"));

        Assert.AreEqual("cloakbrowser", runtimeLock.Provider);
        Assert.AreEqual("cdp-direct", runtimeLock.Mode);
        Assert.AreEqual("fork", runtimeLock.RuntimeSource);
        Assert.AreEqual("nodal-cloakbrowser-runtime", runtimeLock.RuntimeRepo);
        Assert.AreEqual("nodal-runtime", runtimeLock.RuntimeChannel);
        Assert.AreEqual("env-or-local-config", runtimeLock.RuntimePathPolicy);
        Assert.AreEqual("146.0.7680.177.5", runtimeLock.RuntimeVersion);
        Assert.AreEqual("8432254124667a3d2742b1727132d8a045e115da", runtimeLock.RuntimeCommit);
        Assert.AreEqual("0bb3737a29d9133f6207793eb0eeeefe36c9d910", runtimeLock.UpstreamCommit);
        Assert.AreEqual("03f53661a5c47e7b0a661bee2bce8a0d302b7a60834c328df417561fa0636d80", runtimeLock.BinarySha256);
        Assert.IsFalse(runtimeLock.ExtensionEnabled);
        Assert.IsFalse(runtimeLock.SystemBrowserAllowed);
        Assert.IsTrue(runtimeLock.HasPinnedRuntimeArtifact);
        Assert.IsTrue(runtimeLock.Validate().IsValid);
    }

    [TestMethod]
    public void ForkUpdateReleasePipeline_LocalConfigPointsToExternalManagedRuntime()
    {
        var root = RepoRoot();
        var runtimeRepo = RuntimeRepoRoot();
        var localConfig = BrowserRuntimeLocalConfig.Load(Path.Combine(root, ".local", "browser-runtime.local.json"));

        Assert.IsTrue(localConfig.HasExecutablePath);
        Assert.IsTrue(File.Exists(localConfig.CloakBrowserExecutablePath));
        StringAssert.Contains(localConfig.CloakBrowserExecutablePath, "nodal-cloakbrowser-runtime");
        StringAssert.Contains(localConfig.CloakBrowserExecutablePath, ".cloakbrowser");
        Assert.IsTrue(Path.GetFullPath(localConfig.CloakBrowserExecutablePath).StartsWith(Path.GetFullPath(runtimeRepo), StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(File.Exists(Path.Combine(root, ".cloakbrowser")));
    }

    [TestMethod]
    public void ForkUpdateReleasePipeline_RuntimeForkRemoteBranchAndHeadAreExpected()
    {
        var runtimeRepo = RuntimeRepoRoot();

        Assert.AreEqual("https://github.com/onediegopr/nodal-cloakbrowser-runtime", NormalizeRemote(Git(runtimeRepo, "remote", "get-url", "origin")));
        Assert.AreEqual("https://github.com/CloakHQ/cloakbrowser", NormalizeRemote(Git(runtimeRepo, "remote", "get-url", "upstream")));
        Assert.AreEqual("nodal/runtime", Git(runtimeRepo, "branch", "--show-current"));
        Assert.AreEqual("8432254124667a3d2742b1727132d8a045e115da", Git(runtimeRepo, "rev-parse", "HEAD"));
        Assert.AreEqual(string.Empty, Git(runtimeRepo, "status", "--short"));
    }

    [TestMethod]
    public void ForkUpdateReleasePipeline_ScriptIsReadinessOnly()
    {
        var script = ReadRepoText(ScriptPath);

        StringAssert.Contains(script, "NODAL_OS_CLOAKBROWSER_CDP_FORK_UPDATE_RELEASE_PIPELINE_READY");
        StringAssert.Contains(script, "cloakbrowser-cdp-fork-update-release-minimal");
        StringAssert.Contains(script, "Get-FileHash");
        StringAssert.Contains(script, "cloakbrowser-cdp-no-extension-default-*.redacted.json");
        StringAssert.Contains(script, "cloakbrowser-cdp-minimal-product-surface-*.redacted.json");
        StringAssert.Contains(script, "cloakbrowser-cdp-extension-deprecation-hardening-*.redacted.json");
        StringAssert.Contains(script, "extensionUsed = $false");
        StringAssert.Contains(script, "systemBrowserUsed = $false");
        StringAssert.Contains(script, "fallbackUsed = $false");
        Assert.IsFalse(script.Contains("Start-Process", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("Invoke-WebRequest", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("git pull", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("git push", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("verify-installed-sidepanel.mjs", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("chrome-extension", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ForkUpdateReleasePipeline_RunbookDocumentsRepeatableUpdateWithoutVendoring()
    {
        var runbook = ReadRepoText(RunbookPath);

        StringAssert.Contains(runbook, "CloakBrowser Fork Update Release Pipeline");
        StringAssert.Contains(runbook, "browser-runtime.lock.json");
        StringAssert.Contains(runbook, ".local/browser-runtime.local.json");
        StringAssert.Contains(runbook, "nodal-cloakbrowser-runtime");
        StringAssert.Contains(runbook, "https://github.com/CloakHQ/cloakbrowser");
        StringAssert.Contains(runbook, "Do not commit");
        StringAssert.Contains(runbook, "binary artifacts");
        StringAssert.Contains(runbook, "no fallback to the extension is used");
        StringAssert.Contains(runbook, "no fallback to a system browser is used");
    }

    [TestMethod]
    public void ForkUpdateReleasePipeline_DoesNotDependOnExtensionOrSystemBrowser()
    {
        var script = ReadRepoText(ScriptPath);

        foreach (var expected in new[]
        {
            "extensionFallbackAllowed = $false",
            "systemBrowserFallbackAllowed = $false",
            "playwrightDefaultUsed = $false",
            "channelSystemBrowserUsed = $false",
            "bridgeWebSocketUsed = $false",
            "metadataOnly = $true",
            "readOnly = $true"
        })
        {
            StringAssert.Contains(script, expected);
        }
    }

    private static string ReadRepoText(string relativePath) =>
        File.ReadAllText(Path.Combine(RepoRoot(), relativePath));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string RuntimeRepoRoot()
    {
        var root = RepoRoot();
        var runtimeRepo = Path.GetFullPath(Path.Combine(root, "..", "nodal-cloakbrowser-runtime"));
        Assert.IsTrue(Directory.Exists(runtimeRepo), runtimeRepo);
        return runtimeRepo;
    }

    private static string Git(string repository, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = repository,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("git process failed to start.");
        var output = process.StandardOutput.ReadToEnd().Trim();
        var error = process.StandardError.ReadToEnd().Trim();
        process.WaitForExit();
        Assert.AreEqual(0, process.ExitCode, error);
        return output;
    }

    private static string NormalizeRemote(string remote) =>
        remote.Trim().TrimEnd('/').EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? remote.Trim().TrimEnd('/')[..^4]
            : remote.Trim().TrimEnd('/');
}
