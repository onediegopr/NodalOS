using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserRecipeReplayM31Tests
{
    [TestMethod]
    [TestCategory("BrowserRecipeReplaySafeMode")]
    public void BrowserRecipeReplaySafeModeCompletesReadOnlyRecipe()
    {
        RequireReplaySafeMode();
        var result = new BrowserRecipeReplaySafeMode().Replay(Request());

        Assert.IsTrue(result.AllowsDone, result.Reason);
    }

    [TestMethod]
    public void BrowserRecipeReplaySafeModeBlocksDuplicateStep()
    {
        var replay = new BrowserRecipeReplaySafeMode();
        _ = replay.Replay(Request());
        var second = replay.Replay(Request());

        Assert.IsTrue(second.DuplicateBlocked);
    }

    [TestMethod]
    public void BrowserRecipeReplaySafeModeReportsIdempotentReplay()
    {
        var replay = new BrowserRecipeReplaySafeMode();
        var first = replay.Replay(Request());
        var second = replay.Replay(Request());

        Assert.IsTrue(first.AllowsDone);
        Assert.AreEqual("duplicate replay blocked", second.Reason);
    }

    [TestMethod]
    public void BrowserRecipeReplaySafeModeDoesNotRepeatUnsafeActions() =>
        AssertBlocked(Request(recipe: Recipe(action: BrowserRecordedActionKind.Submit, risk: BrowserRecorderRiskAssessment.Risky)), "safe mode replay blocks");

    [TestMethod]
    public void BrowserRecipeReplaySafeModeRejectsProductiveReplay() =>
        AssertBlocked(Request(policy: Policy(BrowserRecipeReplayMode.ProductiveReplay)), "only safe mode");

    [TestMethod]
    public void BrowserRecipeReplaySafeModeRejectsCredentialReplay() =>
        AssertBlocked(Request(policy: Policy(BrowserRecipeReplayMode.CredentialReplay)), "only safe mode");

    [TestMethod]
    public void BrowserRecipeReplaySafeModeRejectsSubmit() =>
        AssertBlocked(Request(recipe: Recipe(action: BrowserRecordedActionKind.Submit, risk: BrowserRecorderRiskAssessment.Risky)), "safe mode replay blocks");

    [TestMethod]
    public void BrowserRecipeReplaySafeModeRejectsUpload() =>
        AssertBlocked(Request(recipe: Recipe(action: BrowserRecordedActionKind.Upload, risk: BrowserRecorderRiskAssessment.Risky)), "safe mode replay blocks");

    [TestMethod]
    public void BrowserRecipeReplaySafeModeRejectsPayment() =>
        AssertBlocked(Request(recipe: Recipe(action: BrowserRecordedActionKind.Click, risk: BrowserRecorderRiskAssessment.Risky, label: "Pay now")), "safe mode replay blocks");

    [TestMethod]
    public void BrowserRecipeReplaySafeModeRejectsDelete() =>
        AssertBlocked(Request(recipe: Recipe(action: BrowserRecordedActionKind.Click, risk: BrowserRecorderRiskAssessment.Risky, label: "Delete")), "safe mode replay blocks");

    [TestMethod]
    public void BrowserRecipeReplaySafeModeRejectsRecipeWithSecrets() =>
        AssertBlocked(Request(recipe: Recipe(storesSecret: true)), "recipe draft is not safe");

    [TestMethod]
    public void BrowserRecipeReplaySafeModeRejectsRecipeWithCookies() =>
        AssertBlocked(Request(recipe: Recipe(storesCookie: true)), "recipe draft is not safe");

    [TestMethod]
    public void BrowserRecipeReplaySafeModeRejectsRecipeWithBodies() =>
        AssertBlocked(Request(recipe: Recipe(storesBody: true)), "recipe draft is not safe");

    [TestMethod]
    public void BrowserRecipeReplaySafeModeRejectsMissingVerificationRule() =>
        AssertBlocked(Request(recipe: Recipe(requiredVerification: false)), "replay requires verification rules");

    [TestMethod]
    public void BrowserRecipeReplaySafeModeRejectsStaleTarget() =>
        AssertBlocked(Request(targetLive: false), "target is stale");

    [TestMethod]
    public void BrowserRecipeReplaySafeModeRequiresSemanticProof()
    {
        var result = new BrowserRecipeReplaySafeMode().Replay(Request(policy: Policy(requireSemanticProof: true)));

        Assert.IsTrue(result.Evidence.SemanticProofPresent);
        Assert.IsTrue(result.AllowsDone);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateAllowsReplaySafeModeReadOnly()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { ReplayState = BrowserRuntimeReplayState.SafeModeReadOnlyActive });

        Assert.IsTrue(report.Passed, string.Join("; ", report.FailedChecks));
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsReplayProductive() =>
        AssertGateFails(BrowserRuntimeReplayState.ProductiveActive);

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsReplayWithoutVerification()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { ReplayState = BrowserRuntimeReplayState.SafeModeReadOnlyActive, ReplayVerificationRequired = false });
        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsReplayWithoutIdempotency()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { ReplayState = BrowserRuntimeReplayState.SafeModeReadOnlyActive, ReplayIdempotencyRequired = false });
        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsReplayWithSensitiveActions()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { ReplayState = BrowserRuntimeReplayState.SafeModeReadOnlyActive, ReplaySupportsSensitiveActions = true });
        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
    }

    private static void AssertGateFails(BrowserRuntimeReplayState state)
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { ReplayState = state });
        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "replay safe-mode read-only");
    }

    private static void AssertBlocked(BrowserRecipeReplayRequest request, string reason)
    {
        var result = new BrowserRecipeReplaySafeMode().Replay(request);
        Assert.IsTrue(result.Blocked);
        StringAssert.Contains(result.Reason, reason);
    }

    private static BrowserRecipeReplayRequest Request(BrowserRecorderDraftRecipe? recipe = null, BrowserRecipeReplayPolicy? policy = null, bool targetLive = true) =>
        new("run-replay", recipe ?? Recipe(), policy ?? Policy(), BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true)), "scope-replay", targetLive);

    private static BrowserRecipeReplayPolicy Policy(BrowserRecipeReplayMode mode = BrowserRecipeReplayMode.SafeModeReadOnly, bool requireSemanticProof = true) =>
        new(mode, Set("127.0.0.1"), RequireGate: true, RequireIdempotency: true, RequireVerification: true, requireSemanticProof);

    private static BrowserRecorderDraftRecipe Recipe(BrowserRecordedActionKind action = BrowserRecordedActionKind.Read, BrowserRecorderRiskAssessment risk = BrowserRecorderRiskAssessment.ReadOnly, bool storesSecret = false, bool storesCookie = false, bool storesBody = false, bool requiredVerification = true, string label = "Read dashboard") =>
        new(
            "recipe-replay",
            1,
            [
                new BrowserRecorderCapturedStep("step-read", action, new BrowserRecorderTargetDescriptor(label, "[data-testid='dashboard']", "http://127.0.0.1/page-a", "127.0.0.1"), new BrowserRecorderVerificationCandidate("verify-read", "dashboard visible", requiredVerification), risk, ["host allowlisted"], ["evidence-read"], Executable: false, storesSecret, storesCookie, storesBody)
            ],
            ExecutableByDefault: false,
            Redacted: true,
            new BrowserRecipeVersioningPolicy(1, true, true));

    private static IReadOnlySet<string> Set(params string[] values) =>
        new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);

    private static void RequireReplaySafeMode()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ONEBRAIN_RUN_RECIPE_REPLAY_SAFE_MODE_TESTS"), "1", StringComparison.Ordinal))
            Assert.Inconclusive("Recipe replay safe-mode tests are opt-in.");
    }
}
