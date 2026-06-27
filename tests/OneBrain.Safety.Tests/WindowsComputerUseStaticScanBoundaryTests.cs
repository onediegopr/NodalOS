using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.WindowsComputerUse;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("WindowsComputerUseFixtureSafe")]
[TestCategory("WindowsComputerUseStaticScanBoundary")]
public sealed class WindowsComputerUseStaticScanBoundaryTests
{
    [TestMethod]
    public void StaticScanCatalogContainsRequiredRulesAndNoAuthority()
    {
        var ids = ComputerUseStaticScanCatalog.Rules.Select(r => r.RuleId).ToArray();

        CollectionAssert.Contains(ids, ComputerUseStaticScanRuleId.NoPInvokeOrDllImport);
        CollectionAssert.Contains(ids, ComputerUseStaticScanRuleId.NoFlaUiOrLiveUia);
        CollectionAssert.Contains(ids, ComputerUseStaticScanRuleId.NoRealMouseKeyboardControl);
        CollectionAssert.Contains(ids, ComputerUseStaticScanRuleId.NoWindowManipulation);
        CollectionAssert.Contains(ids, ComputerUseStaticScanRuleId.NoClipboardReal);
        CollectionAssert.Contains(ids, ComputerUseStaticScanRuleId.NoRawScreenshotCapture);
        CollectionAssert.Contains(ids, ComputerUseStaticScanRuleId.NoProcessShellSubprocess);
        CollectionAssert.Contains(ids, ComputerUseStaticScanRuleId.NoBrowserLiveCdpWebSocketSafeInjection);
        CollectionAssert.Contains(ids, ComputerUseStaticScanRuleId.NoProductUiEnablement);
        CollectionAssert.Contains(ids, ComputerUseStaticScanRuleId.NoLiveReadyWording);
        CollectionAssert.Contains(ids, ComputerUseStaticScanRuleId.NoReleasePublicPaidBetaUnlock);
        CollectionAssert.Contains(ids, ComputerUseStaticScanRuleId.NoClaimDrift);
        CollectionAssert.Contains(ids, ComputerUseStaticScanRuleId.NoSecretLeakage);
        CollectionAssert.Contains(ids, ComputerUseStaticScanRuleId.ProtectedScopeNoDiff);
        Assert.IsTrue(ComputerUseStaticScanCatalog.Rules.All(r =>
            r.ForbiddenPatterns.Count > 0 &&
            r.AllowedNegativeReferences.Count > 0 &&
            !r.ActionAuthorityGranted &&
            !r.LiveUnlockGranted));
    }

    [TestMethod]
    public void ForbiddenExecutablePatternSamplesFailClosed()
    {
        var samples = new[]
        {
            "[DllImport(\"user32.dll\")] static extern bool SetCursorPos(int x, int y);",
            "SendInput(1, inputs, size);",
            "SetForegroundWindow(hwnd); PostMessage(hwnd, msg, w, l); SendMessage(hwnd, msg, w, l);",
            "Clipboard.GetText();",
            "FlaUI.Core.Application.Attach(process); AddAutomationEventHandler();",
            "Graphics.CopyFromScreen(0, 0, 0, 0, size);",
            "Process.Start(\"powershell.exe\");",
            "chrome.debugger attach CDP live WebSocket live bridge Safe Injection live",
            "ProductAutomationEnabled=true product automation enabled",
            "safe to start live implementation live-ready production-ready",
            "public release unlocked paid beta unlocked",
            "\"live_prototype_authorized\": true \"LiveReadPermitted\": true \"ActionAuthorityGranted\": true containment pass grants live go",
            "sk-live-secret-value AKIA1234567890123456 -----BEGIN PRIVATE KEY-----"
        };

        foreach (var sample in samples)
        {
            var result = ComputerUseStaticScanCatalog.EvaluateText("sample", "fixture.cs", sample);
            Assert.IsFalse(result.Passed, sample);
            Assert.IsTrue(result.Findings.Any(f => !f.AllowedNegativeReference), sample);
            AssertNoAuthority(result);
        }
    }

    [TestMethod]
    public void AllowedNegativeReferencesPassWithoutUnlockingLive()
    {
        var samples = new[]
        {
            "NO_P_INVOKE_OR_DLLIMPORT: No DllImport or LibraryImport allowed.",
            "NO_REAL_MOUSE_KEYBOARD_CONTROL: No real mouse or keyboard; SendInput is prohibited.",
            "NO_CLIPBOARD_REAL: Clipboard access remains prohibited.",
            "NO_SAFE_INJECTION_LIVE: Safe Injection live remains blocked.",
            "BLOCKED_PENDING_HUMAN_POLICY_DECISION_AND_EXTERNAL_GO",
            "LiveReadPermitted=false ActionAuthorityGranted=false ProductAutomationEnabled=false",
            "public_release_unlock: false paid_beta_unlock: false Release/public/paid beta unlock: 0%",
            "synthetic redaction fixture sk-testSecret ghp_fakeSecretToken"
        };

        foreach (var sample in samples)
        {
            var result = ComputerUseStaticScanCatalog.EvaluateText("negative", "docs/fixture.md", sample);
            Assert.IsTrue(result.Passed, sample);
            Assert.IsTrue(result.Findings.All(f => f.AllowedNegativeReference), sample);
            AssertNoAuthority(result);
        }
    }

    [TestMethod]
    public void ProtectedBoundaryCatalogConsolidatesStealthAndWcuScopes()
    {
        var boundary = ComputerUseStaticScanCatalog.ProtectedBoundary;

        CollectionAssert.Contains(boundary.ProtectedPaths.ToArray(), "stealth-engine/src/evasion/**");
        CollectionAssert.Contains(boundary.ProtectedPaths.ToArray(), "stealth-engine/src/StealthSession.js");
        CollectionAssert.Contains(boundary.ProtectedPaths.ToArray(), "stealth-engine/tests/stealth-suite.test.js");
        CollectionAssert.Contains(boundary.AllowedWcuPaths.ToArray(), "src/OneBrain.WindowsComputerUse/**");
        CollectionAssert.Contains(boundary.AllowedWcuPaths.ToArray(), "tests/OneBrain.Safety.Tests/**");
        Assert.IsFalse(boundary.AllowsStealthCoreDiff);
        Assert.IsFalse(boundary.AllowsSidepanelHashDebtMixing);
        Assert.IsFalse(boundary.AllowsLivePrototype);
    }

    [TestMethod]
    public void StaticScanReportsKeepContainmentAndPauseRecommendation()
    {
        var repoRoot = FindRepoRoot();
        var reportPath = Path.Combine(repoRoot, "docs", "qa", "computer-use", "wcu-containment-property-audit-004-static-scan-boundary", "static-scan-report.json");
        var blockReportPath = Path.Combine(repoRoot, "docs", "qa", "computer-use", "wcu-containment-property-audit-004-static-scan-boundary", "report.json");
        using var scanReport = JsonDocument.Parse(File.ReadAllText(reportPath));
        using var blockReport = JsonDocument.Parse(File.ReadAllText(blockReportPath));

        AssertJsonString(scanReport.RootElement, "static_scan_harness", "LOCKED", "static-scan-report");
        AssertJsonString(scanReport.RootElement, "protected_boundary", "CONSOLIDATED", "static-scan-report");
        AssertJsonBool(scanReport.RootElement, "live_prototype_authorized", false, "static-scan-report");
        AssertJsonBool(scanReport.RootElement, "sidepanel_hash_debt_touched", false, "static-scan-report");
        AssertJsonBool(blockReport.RootElement, "wcu_safe_pause_recommended", true, "report");
        AssertJsonBool(blockReport.RootElement, "live_prototype_authorized", false, "report");
        AssertJsonString(blockReport.RootElement, "wcu_037_044_status", ComputerUseClaimConsistencyCatalog.BlockedLivePrototypeStatus, "report");
    }

    [TestMethod]
    public void NextPromptRecommendsSafePauseOrSeparateDebtOnly()
    {
        var repoRoot = FindRepoRoot();
        var prompt = File.ReadAllText(Path.Combine(repoRoot, "docs", "prompts", "computer-use", "next-wcu-safe-pause-or-containment-audit-005-prompt.md"));

        StringAssert.Contains(prompt, "Pausar WCU");
        StringAssert.Contains(prompt, "SIDE_PANEL_EXTENSION_HASH_BASELINE_RECONCILIATION");
        StringAssert.Contains(prompt, ComputerUseClaimConsistencyCatalog.BlockedLivePrototypeStatus);
        Assert.IsFalse(prompt.Contains("safe to start live implementation", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(prompt.Contains("READ-ONLY LIVE PROTOTYPE GATED", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(prompt.Contains("browser live enabled", StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertNoAuthority(ComputerUseStaticScanResult result)
    {
        Assert.IsFalse(result.ActionAuthorityGranted);
        Assert.IsFalse(result.LiveUnlockGranted);
        Assert.IsFalse(result.ProductAutomationEnabled);
    }

    private static void AssertJsonBool(JsonElement element, string propertyName, bool expected, string context)
    {
        Assert.IsTrue(element.TryGetProperty(propertyName, out var value), $"{context}:{propertyName}");
        Assert.AreEqual(expected, value.GetBoolean(), $"{context}:{propertyName}");
    }

    private static void AssertJsonString(JsonElement element, string propertyName, string expected, string context)
    {
        Assert.IsTrue(element.TryGetProperty(propertyName, out var value), $"{context}:{propertyName}");
        Assert.AreEqual(expected, value.GetString(), $"{context}:{propertyName}");
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "OneBrain.slnx")))
                return current.FullName;

            current = current.Parent;
        }

        Assert.Fail("Could not locate repository root from test output directory.");
        return AppContext.BaseDirectory;
    }
}
