namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("NoExtensionDefaultHarness")]
public sealed class CdpNoExtensionDefaultHarnessTests
{
    private const string NoExtensionHarnessPath = "scripts/verify-cloakbrowser-cdp-no-extension-default.ps1";
    private const string InstalledSidepanelHarnessPath = "scripts/verify-installed-sidepanel.mjs";

    [TestMethod]
    public void NoExtensionDefaultHarness_ScriptExistsAndDeclaresDefaultDecision()
    {
        var script = ReadRepoText(NoExtensionHarnessPath);

        StringAssert.Contains(script, "NODAL_OS_CLOAKBROWSER_CDP_NO_EXTENSION_DEFAULT_HARNESS_READY");
        StringAssert.Contains(script, "defaultHarness = \"cloakbrowser-cdp-no-extension\"");
        StringAssert.Contains(script, "runtimeProvider = \"cloakbrowser\"");
        StringAssert.Contains(script, "source = \"cloakbrowser-cdp-direct\"");
        StringAssert.Contains(script, "metadataOnly = $true");
        StringAssert.Contains(script, "readOnly = $true");
    }

    [TestMethod]
    public void NoExtensionDefaultHarness_DoesNotCallInstalledSidepanelHarness()
    {
        var script = ReadRepoText(NoExtensionHarnessPath);

        Assert.IsFalse(script.Contains("verify-installed-sidepanel.mjs", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("extensionPath", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("chrome-extension://", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("--load-extension", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("disable-extensions-except", StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(script, "installedSidepanelHarnessUsed = $false");
        StringAssert.Contains(script, "extensionOpened = $false");
    }

    [TestMethod]
    public void NoExtensionDefaultHarness_UsesCdpRuntimeSkillsSessionAndSnapshotExport()
    {
        var script = ReadRepoText(NoExtensionHarnessPath);

        StringAssert.Contains(script, "scripts/verify-cloakbrowser-cdp-runtime.ps1");
        StringAssert.Contains(script, "scripts/verify-cloakbrowser-cdp-browser-skills.ps1");
        StringAssert.Contains(script, "scripts/verify-cloakbrowser-cdp-browser-skills-session.ps1");
        StringAssert.Contains(script, "scripts/export-cloakbrowser-cdp-ui-status-snapshot.ps1");
        StringAssert.Contains(script, "snapshotChannel = Get-Field -Step $Snapshot -Name \"channel\"");
        StringAssert.Contains(script, "\"safe-local-status-snapshot\"");
    }

    [TestMethod]
    public void NoExtensionDefaultHarness_RejectsExtensionAndSystemBrowserFallback()
    {
        var script = ReadRepoText(NoExtensionHarnessPath);

        foreach (var expected in new[]
        {
            "extensionUsed = $false",
            "extensionFallbackUsed = $false",
            "systemBrowserUsed = $false",
            "systemBrowserFallbackUsed = $false",
            "fallbackUsed = $false",
            "(Get-BoolField -Step $Runtime -Name \"extensionUsed\")",
            "(Get-BoolField -Step $BrowserSkills -Name \"systemBrowserUsed\")",
            "(Get-BoolField -Step $Session -Name \"extensionUsed\")"
        })
        {
            StringAssert.Contains(script, expected);
        }
    }

    [TestMethod]
    public void NoExtensionDefaultHarness_WritesMetadataOnlyRedactedEvidence()
    {
        var script = ReadRepoText(NoExtensionHarnessPath);

        foreach (var expected in new[]
        {
            "rawDomStored = $false",
            "rawHtmlStored = $false",
            "inputValuesStored = $false",
            "cookiesOrStorageStored = $false",
            "secretsStored = $false",
            "New-EvidenceName -Step $Runtime",
            "New-EvidenceName -Step $BrowserSkills",
            "New-EvidenceName -Step $Session"
        })
        {
            StringAssert.Contains(script, expected);
        }

        Assert.IsFalse(script.Contains("rawHtml = ", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("document.cookie", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("localStorage", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("sessionStorage", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NoExtensionDefaultHarness_DoesNotUseSystemBrowserLaunchTokens()
    {
        var script = ReadRepoText(NoExtensionHarnessPath);

        foreach (var forbidden in ForbiddenSystemBrowserTokens())
        {
            Assert.IsFalse(script.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
        }
    }

    [TestMethod]
    public void InstalledSidepanelHarness_IsMarkedLegacyCompatibilityOnly()
    {
        var installedHarness = ReadRepoText(InstalledSidepanelHarnessPath);

        StringAssert.Contains(installedHarness, "browserRuntimeDefault: 'cloakbrowser-cdp-no-extension'");
        StringAssert.Contains(installedHarness, "defaultRuntimeHarness: 'legacy-installed-sidepanel-compat-only'");
        StringAssert.Contains(installedHarness, "installedSidepanelHarnessDefault: false");
        StringAssert.Contains(installedHarness, "extensionRuntimeDefault: false");
        StringAssert.Contains(installedHarness, "installed sidepanel harness remains legacy compatibility only");
    }

    private static IEnumerable<string> ForbiddenSystemBrowserTokens()
    {
        yield return "channel: " + "\"" + "chrome" + "\"";
        yield return "channel: " + "\"" + "msedge" + "\"";
        yield return "chromium" + "." + "launch";
        yield return "playwright install " + "chromium";
        yield return "chrome" + "." + "exe";
        yield return "msedge" + "." + "exe";
        yield return "chromium" + "." + "exe";
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
}
