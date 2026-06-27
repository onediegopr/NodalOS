using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.WindowsComputerUse;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("WindowsComputerUseFixtureSafe")]
[TestCategory("WindowsComputerUseActiveSafeMode")]
public sealed class WindowsComputerUseActiveSafeModeTests
{
    [TestMethod]
    public void StatusReportsReadySafeModeAndNoAuthority()
    {
        var status = ComputerUseActiveSafeModeCatalog.Current();

        Assert.AreEqual("Windows Computer Use Control Plane", status.CapabilityName);
        Assert.AreEqual("READY_SAFE_MODE", status.CapabilityStatus);
        Assert.AreEqual("CONTAINMENT_PERCEPTION_FOUNDATION", status.SupportedMode);
        Assert.IsTrue(status.IsUsable);
        Assert.AreEqual("FAIL_CLOSED", status.FailureBehavior);
        Assert.AreEqual("RETURNS_STRUCTURED_SAFE_MODE_RESULT", status.IfCalledBehavior);
        Assert.AreEqual("DISABLED_BY_POLICY_AND_EXTERNAL_NO_GO", status.BlockedLiveReason);
        Assert.AreEqual(ComputerUseExternalAuditReconciliation.BlockedLivePrototypeStatus, status.Wcu037044Status);
        AssertNoAuthority(status);
        Assert.IsTrue(status.OperatorQaRequiredForLive);
        Assert.IsTrue(status.ExternalGoRequiredForLive);
        Assert.IsTrue(status.HumanPolicyDecisionRequiredForLive);
    }

    [TestMethod]
    public void ReadinessExposesUsableContainmentPerceptionFoundationOnly()
    {
        var readiness = new ComputerUseSafeModeFacade().GetReadiness();

        Assert.IsTrue(readiness.SafeModeUsable);
        Assert.IsTrue(readiness.ContainmentPerceptionFoundationReady);
        Assert.IsTrue(readiness.EvidenceRedactionReady);
        Assert.IsTrue(readiness.BridgeHandoffReady);
        Assert.IsTrue(readiness.StaticBoundaryReady);
        CollectionAssert.Contains(readiness.SupportedSafeModeSurfaces.ToArray(), "containment");
        CollectionAssert.Contains(readiness.SupportedSafeModeSurfaces.ToArray(), "perception-foundation");
        Assert.IsFalse(readiness.LiveReadPermitted);
        Assert.IsFalse(readiness.ActionAuthorityGranted);
        Assert.IsFalse(readiness.ProductAutomationEnabled);
    }

    [TestMethod]
    public void SafeModeRequestReturnsReadySafeModeWithoutLiveFlags()
    {
        var result = new ComputerUseSafeModeFacade().EvaluateRequestedMode(ComputerUseRequestedMode.SafeMode);

        Assert.AreEqual(ComputerUseSafeModeRequestStatus.ReadySafeMode, result.Status);
        Assert.IsTrue(result.IsUsable);
        AssertNoAuthority(result);
        AssertNoLiveArtifacts(result);
    }

    [TestMethod]
    public void LiveActionAndProductRequestsReturnBlockedResultNotException()
    {
        var facade = new ComputerUseSafeModeFacade();
        var modes = new[]
        {
            ComputerUseRequestedMode.LiveRead,
            ComputerUseRequestedMode.ActionExecution,
            ComputerUseRequestedMode.ProductAutomation,
            ComputerUseRequestedMode.BrowserLiveCdp
        };

        foreach (var mode in modes)
        {
            var result = facade.EvaluateRequestedMode(mode);

            Assert.AreEqual(mode, result.RequestedMode);
            Assert.AreEqual(ComputerUseSafeModeRequestStatus.BlockedByPolicy, result.Status);
            Assert.IsFalse(result.IsUsable);
            AssertNoAuthority(result);
            AssertNoLiveArtifacts(result);
        }
    }

    [TestMethod]
    public void BlockedLiveAttemptFactoryAlwaysPreservesNoAuthority()
    {
        var result = new ComputerUseSafeModeFacade().CreateBlockedLiveAttemptResult("fixture blocked request");

        Assert.AreEqual(ComputerUseSafeModeRequestStatus.BlockedByPolicy, result.Status);
        Assert.AreEqual("fixture blocked request", result.Reason);
        AssertNoAuthority(result);
        AssertNoLiveArtifacts(result);
    }

    [TestMethod]
    public void SafeModeEvidenceSummaryIsRedactedAndContainsNoRawLiveArtifacts()
    {
        var summary = new ComputerUseSafeModeFacade().CreateSafeModeEvidenceSummary(
            "fixture-summary",
            ["status", "redacted-evidence"]);

        Assert.AreEqual("fixture-summary", summary.SummaryId);
        Assert.AreEqual("READY_SAFE_MODE", summary.CapabilityStatus);
        Assert.IsTrue(summary.Redacted);
        CollectionAssert.Contains(summary.SourceSignals.ToArray(), "status");
        Assert.IsFalse(summary.RawScreenshotPresent);
        Assert.IsFalse(summary.ClipboardPresent);
        Assert.IsFalse(summary.LiveProviderCalled);
        Assert.IsFalse(summary.ActionAuthorityGranted);
        Assert.IsFalse(summary.LiveReadPermitted);
        Assert.IsFalse(summary.ProductAutomationEnabled);
    }

    [TestMethod]
    public void SafeModeStatusIsSerializableWithoutUnlockingLive()
    {
        var json = JsonSerializer.Serialize(new ComputerUseSafeModeFacade().GetStatus());

        StringAssert.Contains(json, "READY_SAFE_MODE");
        StringAssert.Contains(json, "FAIL_CLOSED");
        Assert.IsFalse(json.Contains("\"LiveReadPermitted\":true", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("\"ActionAuthorityGranted\":true", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("\"ProductAutomationEnabled\":true", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("ClipboardPresent", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("RawScreenshotPresent", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ProductClaimCatalogAllowsSafeModeReadyWording()
    {
        var allowed = ComputerUseSafeModeProductClaimCatalog.Evaluate("Computer Use Control Plane - Safe Mode Ready");
        var foundation = ComputerUseSafeModeProductClaimCatalog.Evaluate("Perception/Evidence/Redaction/Handoff foundation ready");
        var disabled = ComputerUseSafeModeProductClaimCatalog.Evaluate("Live desktop control disabled by policy");

        AssertAllowedNoAuthority(allowed);
        AssertAllowedNoAuthority(foundation);
        AssertAllowedNoAuthority(disabled);
    }

    [TestMethod]
    public void ProductClaimCatalogRejectsLiveAutomationReadyWording()
    {
        var claims = new[]
        {
            "Controla tu PC real",
            "Live desktop automation ready",
            "Mouse/keyboard automation ready",
            "FlaUI/UIA live ready",
            "Screenshots/live screen capture ready",
            "real PC automation ready",
            "live desktop control ready"
        };

        foreach (var claim in claims)
        {
            var result = ComputerUseSafeModeProductClaimCatalog.Evaluate(claim);

            Assert.IsFalse(result.Allowed, claim);
            Assert.IsFalse(result.ActionAuthorityGranted, claim);
            Assert.IsFalse(result.LiveReadPermitted, claim);
            Assert.IsFalse(result.ProductAutomationEnabled, claim);
        }
    }

    private static void AssertAllowedNoAuthority(ComputerUseSafeModeProductClaimResult result)
    {
        Assert.IsTrue(result.Allowed);
        Assert.IsFalse(result.ActionAuthorityGranted);
        Assert.IsFalse(result.LiveReadPermitted);
        Assert.IsFalse(result.ProductAutomationEnabled);
    }

    private static void AssertNoAuthority(ComputerUseActiveSafeModeStatus status)
    {
        Assert.IsFalse(status.LiveReadPermitted);
        Assert.IsFalse(status.ActionAuthorityGranted);
        Assert.IsFalse(status.ProductAutomationEnabled);
        Assert.IsFalse(status.LiveDesktopAutomationEnabled);
        Assert.IsFalse(status.RealMouseKeyboardEnabled);
        Assert.IsFalse(status.RawScreenshotCaptureEnabled);
    }

    private static void AssertNoAuthority(ComputerUseSafeModeRequestResult result)
    {
        Assert.IsFalse(result.LiveReadPermitted);
        Assert.IsFalse(result.ActionAuthorityGranted);
        Assert.IsFalse(result.ProductAutomationEnabled);
        Assert.IsFalse(result.LiveDesktopAutomationEnabled);
        Assert.IsFalse(result.RealMouseKeyboardEnabled);
        Assert.IsFalse(result.RawScreenshotCaptureEnabled);
    }

    private static void AssertNoLiveArtifacts(ComputerUseSafeModeRequestResult result)
    {
        Assert.IsFalse(result.ClipboardEnabled);
        Assert.IsFalse(result.LiveProviderCalled);
        Assert.IsFalse(result.RawScreenshotPresent);
        Assert.IsFalse(result.ClipboardPresent);
        Assert.AreEqual("FAIL_CLOSED", result.FailureBehavior);
        Assert.AreEqual("RETURNS_STRUCTURED_SAFE_MODE_RESULT", result.IfCalledBehavior);
    }
}
