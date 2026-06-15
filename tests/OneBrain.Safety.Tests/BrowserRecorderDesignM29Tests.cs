using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserRecorderDesignM29Tests
{
    [TestMethod]
    public void BrowserRecorderDesignIsDisabledByDefault() =>
        Assert.IsFalse(new BrowserRecorderDesign().Enabled);

    [TestMethod]
    public void BrowserRecorderDesignDoesNotEnableExecutableReplay()
    {
        var sanitized = new BrowserRecorderDesign().Sanitize(UnsafeDraft());

        Assert.IsFalse(sanitized.ExecutableReplayEnabled);
        Assert.IsTrue(sanitized.DesignOnly);
    }

    [TestMethod]
    public void BrowserRecorderDesignDoesNotStoreSecrets()
    {
        var sanitized = new BrowserRecorderDesign().Sanitize(UnsafeDraft());

        Assert.IsFalse(sanitized.Steps.Any(s => s.StoresSecret));
        Assert.IsFalse(sanitized.ToString()!.Contains("access_token=opaque", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BrowserRecorderDesignDoesNotStoreCookies()
    {
        var sanitized = new BrowserRecorderDesign().Sanitize(UnsafeDraft() with { Steps = [UnsafeStep() with { StoresCookie = true }] });

        Assert.IsFalse(sanitized.Steps.Any(s => s.StoresCookie));
    }

    [TestMethod]
    public void BrowserRecorderDesignDoesNotStoreBodies()
    {
        var sanitized = new BrowserRecorderDesign().Sanitize(UnsafeDraft() with { Steps = [UnsafeStep() with { StoresBody = true }] });

        Assert.IsFalse(sanitized.Steps.Any(s => s.StoresBody));
    }

    [TestMethod]
    public void BrowserRecorderDesignRequiresVerificationRules() =>
        Assert.IsFalse(UnsafeStep() with { VerificationRules = [] } is { IsSafeDraft: true });

    [TestMethod]
    public void BrowserRecorderDesignRequiresApprovalForIrreversibleActions() =>
        Assert.IsFalse(UnsafeStep() with { Risk = BrowserRecordedRiskClassification.Irreversible, RequiresApproval = false } is { IsSafeDraft: true });

    [TestMethod]
    public void BrowserRecorderDesignRequiresIdempotencyForModifyingActions() =>
        Assert.IsFalse(UnsafeStep() with { Risk = BrowserRecordedRiskClassification.Modifying, RequiresApproval = false, RequiresIdempotency = false } is { IsSafeDraft: true });

    [TestMethod]
    public void BrowserRecorderRecipeDraftRedactsSensitiveValues()
    {
        var sanitized = new BrowserRecorderDesign().Sanitize(UnsafeDraft());

        Assert.IsFalse(sanitized.ToString()!.Contains("opaque", StringComparison.Ordinal));
        Assert.IsFalse(sanitized.ToString()!.Contains("C:\\Users\\diego", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserRecorderRecipeDraftKeepsSemanticTargets()
    {
        var sanitized = new BrowserRecorderDesign().Sanitize(SafeDraft());

        Assert.IsTrue(sanitized.Steps.Single().Target.SemanticName.Contains("Dashboard", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BrowserRecorderRecipeVersioningPolicyExists() =>
        Assert.IsTrue(SafeDraft().VersioningPolicy.CurrentSchemaVersion > 0);

    [TestMethod]
    public void BrowserRuntimePhaseGateAllowsRecorderDesignOnly()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { RecorderState = BrowserRuntimeRecorderState.DesignOnly });

        Assert.IsTrue(report.Passed, string.Join("; ", report.FailedChecks));
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsRecorderExecutable()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { RecorderState = BrowserRuntimeRecorderState.ExecutableActive });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "recorder design-only");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsRecorderStoringSecrets()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { RecorderState = BrowserRuntimeRecorderState.DesignOnly, RecorderStoresSecrets = true });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "recorder design-only");
    }

    private static BrowserRecipeDraft UnsafeDraft() =>
        SafeDraft() with
        {
            ExecutableReplayEnabled = true,
            DesignOnly = false,
            Steps =
            [
                UnsafeStep() with
                {
                    Target = new BrowserRecordedTargetDescriptor("Dashboard password", "#password", "https://example.test/dashboard?access_token=opaque&file=C:\\Users\\diego\\secret.txt"),
                    StoresSecret = true,
                    StoresCookie = true,
                    StoresBody = true
                }
            ]
        };

    private static BrowserRecipeDraft SafeDraft() =>
        new(
            "recipe-draft",
            [UnsafeStep() with { Target = new BrowserRecordedTargetDescriptor("Dashboard status", "[data-testid='dashboard']", "https://example.test/dashboard"), StoresSecret = false, StoresCookie = false, StoresBody = false }],
            new BrowserRecipeRedactionPolicy(true, true, true, true, true),
            new BrowserRecipeApprovalPolicy(true, true),
            new BrowserRecipeVersioningPolicy(1, true, true),
            ExecutableReplayEnabled: false,
            DesignOnly: true);

    private static BrowserRecordedStepDraft UnsafeStep() =>
        new(
            "step-recorder",
            BrowserRecordedActionKind.Click,
            new BrowserRecordedTargetDescriptor("Dashboard", "[data-testid='dashboard']", "https://example.test/dashboard"),
            BrowserRecordedRiskClassification.Modifying,
            [new BrowserRecordedVerificationRule("verify-dashboard", "dashboard visible", true)],
            RequiresApproval: true,
            RequiresIdempotency: true,
            StoresSecret: false,
            StoresCookie: false,
            StoresBody: false);
}

