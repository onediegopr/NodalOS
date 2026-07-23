using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("DesktopPackaging")]
[TestCategory("PrivateBeta")]
public sealed class PrivateBetaMonotonicPackageScriptTests
{
    [TestMethod]
    public void BuildMsixGeneratesIdentityAwareMonotonicInstaller()
    {
        var repoRoot = FindRepoRoot();
        var buildScript = File.ReadAllText(Path.Combine(repoRoot, "eng", "packaging", "build-msix.ps1"));

        Assert.IsFalse(buildScript.Contains("Add-AppxPackage -Path `$package -ForceUpdateFromAnyVersion", StringComparison.Ordinal));
        StringAssert.Contains(buildScript, "Read-MsixIdentity");
        StringAssert.Contains(buildScript, "Get-InstalledPackageForIdentity");
        StringAssert.Contains(buildScript, "Where-Object { [string]::Equals(`$_.Publisher, `$Publisher, [StringComparison]::Ordinal) }");
        StringAssert.Contains(buildScript, "Test-signed private-beta packages require clean uninstall before installing another signed revision.");
        StringAssert.Contains(buildScript, "Same-version reinstall is blocked.");
        StringAssert.Contains(buildScript, "Downgrade is blocked.");
        StringAssert.Contains(buildScript, "Add-AppxPackage -Path `$package");
    }

    [TestMethod]
    public void MonotonicUpdateSmokeIsReusableAndAvoidsOneShotWorkflowBehavior()
    {
        var repoRoot = FindRepoRoot();
        var smokeScriptPath = Path.Combine(repoRoot, "eng", "ci", "smoke-msix-monotonic-update.ps1");
        Assert.IsTrue(File.Exists(smokeScriptPath), smokeScriptPath);

        var smokeScript = File.ReadAllText(smokeScriptPath);
        StringAssert.Contains(smokeScript, "param(");
        StringAssert.Contains(smokeScript, "$PreviousCommit");
        StringAssert.Contains(smokeScript, "$PreviousVersion");
        StringAssert.Contains(smokeScript, "$CandidateVersion");
        StringAssert.Contains(smokeScript, "Create-AppOwnedState");
        StringAssert.Contains(smokeScript, "Assert-AppOwnedStatePreserved");
        StringAssert.Contains(smokeScript, "Assert-TeachNodalAvailable");
        StringAssert.Contains(smokeScript, "Downgrade package install was not rejected.");
        StringAssert.Contains(smokeScript, "Same-version candidate reinstall was not rejected.");
        Assert.IsFalse(smokeScript.Contains("ForceUpdateFromAnyVersion", StringComparison.Ordinal));
        Assert.IsFalse(smokeScript.Contains(".github/workflows", StringComparison.OrdinalIgnoreCase));
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OneBrain.slnx")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
