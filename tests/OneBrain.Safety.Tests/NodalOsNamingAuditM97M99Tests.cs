using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.ChromeLab.Bridge;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NodalOsNamingAuditM97M99Tests
{
    [TestMethod]
    public void NodalOsRenameAppTargetVisibleMetadataUsesNodalOs()
    {
        var text = ReadAll(AppRoot());

        StringAssert.Contains(text, "project: NODAL OS");
        StringAssert.Contains(text, "NODAL_OS_EXTERNAL_READONLY_TARGET_OK");
        StringAssert.Contains(text, "NODAL_OS_READONLY_PROOF_READY");
        StringAssert.Contains(text, "NODAL_OS_BLOCKED_FIXTURE");
        AssertNoVisibleNexa(text);
    }

    [TestMethod]
    public void NodalOsRenameOperatorFacingSourceDoesNotUseVisibleNexa()
    {
        var files = new[]
        {
            SourcePath("src", "OneBrain.BrowserExecutor.Cdp", "BrowserConsentAndProfileActivationServices.cs"),
            SourcePath("src", "OneBrain.BrowserExecutor.Cdp", "BrowserHumanHandoffCompanionAdapter.cs"),
            SourcePath("src", "OneBrain.BrowserExecutor.Cdp", "BrowserProductiveVaultServices.cs"),
            SourcePath("src", "OneBrain.BrowserExecutor.Cdp", "NexaPrivatePreviewControlSurfaceServices.cs"),
            SourcePath("src", "OneBrain.ChromeLab.Bridge", "Program.cs")
        };

        foreach (var file in files)
            AssertNoVisibleNexaInStringLiterals(File.ReadAllLines(file), file);
    }

    [TestMethod]
    public void NodalOsRenameEnvVarCurrentAndLegacyAliasWork()
    {
        Assert.IsTrue(NodalOsExternalLiveProofOptIn.IsEnabled(name =>
            name == NodalOsExternalLiveProofOptIn.CurrentEnvironmentVariable ? "true" : null));
        Assert.IsTrue(NodalOsExternalLiveProofOptIn.IsEnabled(name =>
            name == NodalOsExternalLiveProofOptIn.LegacyEnvironmentVariable ? "true" : null));
        Assert.IsFalse(NodalOsExternalLiveProofOptIn.IsEnabled(_ => null));
        AssertChromeBridgeTokenEnvironmentAliasesWork();
    }

    [TestMethod]
    public void NodalOsLegacyNamingAllowlistAndMigrationReportExist()
    {
        var allowlist = ReadDoc("docs", "adr", "nodal-os-legacy-naming-allowlist.md");
        var report = ReadDoc("docs", "roadmap", "nodal-os-rename-migration-report.md");
        var adr = ReadDoc("docs", "adr", "nodal-os-global-rename-m97-m99.md");

        StringAssert.Contains(allowlist, "Technical symbols");
        StringAssert.Contains(allowlist, "NEXA_EXTERNAL_LIVE_PROOF_OPT_IN");
        StringAssert.Contains(allowlist, "apps/nexa-test-owned-target");
        StringAssert.Contains(report, "Official name: NODAL OS");
        StringAssert.Contains(report, "Former name: NEXA");
        StringAssert.Contains(adr, "NODAL OS is the official product name");
    }

    [TestMethod]
    public void NodalOsRenameMigrationReportExists()
    {
        var report = ReadDoc("docs", "roadmap", "nodal-os-rename-migration-report.md");

        StringAssert.Contains(report, "Official name: NODAL OS");
        StringAssert.Contains(report, "Former name: NEXA");
        StringAssert.Contains(report, "M65: deferred");
    }

    [TestMethod]
    public void NodalOsNamingAuditDetectsUnallowlistedVisibleNexa()
    {
        var findings = VisibleNamingAudit("Welcome to NEXA private preview", allowLegacy: false);

        Assert.AreEqual(1, findings.Count);
        StringAssert.Contains(findings[0], "NEXA");
    }

    private static IReadOnlyList<string> VisibleNamingAudit(string text, bool allowLegacy)
    {
        if (allowLegacy)
            return [];
        return text.Contains("NEXA", StringComparison.Ordinal) ||
            text.Contains("Nexa", StringComparison.Ordinal) ||
            text.Contains("nexa", StringComparison.Ordinal)
            ? ["visible legacy NEXA naming found"]
            : [];
    }

    private static void AssertNoVisibleNexa(string text, string? context = null)
    {
        var findings = VisibleNamingAudit(text, allowLegacy: false);
        Assert.AreEqual(0, findings.Count, context ?? string.Join(Environment.NewLine, findings));
    }

    private static void AssertChromeBridgeTokenEnvironmentAliasesWork()
    {
        var oldCurrent = Environment.GetEnvironmentVariable(ChromeLabOptions.CurrentConnectionTokenEnvironmentVariable);
        var oldLegacy = Environment.GetEnvironmentVariable(ChromeLabOptions.LegacyConnectionTokenEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(ChromeLabOptions.CurrentConnectionTokenEnvironmentVariable, "current-token");
            Environment.SetEnvironmentVariable(ChromeLabOptions.LegacyConnectionTokenEnvironmentVariable, null);
            Assert.AreEqual("current-token", ChromeLabOptions.Load([]).ConnectionToken);

            Environment.SetEnvironmentVariable(ChromeLabOptions.CurrentConnectionTokenEnvironmentVariable, null);
            Environment.SetEnvironmentVariable(ChromeLabOptions.LegacyConnectionTokenEnvironmentVariable, "legacy-token");
            Assert.AreEqual("legacy-token", ChromeLabOptions.Load([]).ConnectionToken);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ChromeLabOptions.CurrentConnectionTokenEnvironmentVariable, oldCurrent);
            Environment.SetEnvironmentVariable(ChromeLabOptions.LegacyConnectionTokenEnvironmentVariable, oldLegacy);
        }
    }

    private static void AssertNoVisibleNexaInStringLiterals(IReadOnlyList<string> lines, string context)
    {
        var offending = lines
            .SelectMany(ExtractStringLiteralText)
            .Where(text => VisibleNamingAudit(text, allowLegacy: false).Count > 0)
            .ToArray();
        Assert.AreEqual(0, offending.Length, $"{context}{Environment.NewLine}{string.Join(Environment.NewLine, offending)}");
    }

    private static IEnumerable<string> ExtractStringLiteralText(string line)
    {
        var parts = line.Split('"');
        for (var i = 1; i < parts.Length; i += 2)
            yield return parts[i];
    }

    private static string ReadAll(string root)
    {
        var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => Path.GetExtension(path) is ".html" or ".md" or ".txt" or ".json" or ".css")
            .Where(path => !path.EndsWith("README.md", StringComparison.OrdinalIgnoreCase));
        return string.Join("\n", files.Select(File.ReadAllText));
    }

    private static string ReadDoc(params string[] relativePath)
    {
        var path = SourcePath(relativePath);
        Assert.IsTrue(File.Exists(path), path);
        return File.ReadAllText(path);
    }

    private static string AppRoot() =>
        SourcePath("apps", "nexa-test-owned-target");

    private static string SourcePath(params string[] relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "OneBrain.slnx")))
                return Path.Combine(new[] { current.FullName }.Concat(relativePath).ToArray());
            current = current.Parent;
        }

        Assert.Fail("Could not locate repository root.");
        return "";
    }
}
