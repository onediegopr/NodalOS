using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class ChromeCdpBrowserExecutorTests
{
    [TestMethod]
    public async Task LauncherStartsChromeWithTemporaryProfileAndCdpEndpoint()
    {
        var browserPath = RequireBrowser();
        await using var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath));

        using var version = await session.GetVersionAsync();

        Assert.IsTrue(session.IsProcessAlive);
        Assert.IsTrue(Directory.Exists(session.UserDataDir));
        Assert.AreEqual("127.0.0.1", session.VersionEndpoint.Host);
        Assert.IsTrue(version.RootElement.TryGetProperty("Browser", out _));
    }

    [TestMethod]
    public async Task TargetDiscoveryFindsFixtureAndBuildsRealTargetContext()
    {
        var browserPath = RequireBrowser();
        await using var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath));
        await using var page = await session.CreatePageAsync(FixtureUri());

        var targets = await session.ListTargetsAsync();
        var observation = await page.ObserveAsync("run-cdp-1");

        Assert.IsTrue(targets.Any(target => target.Type == "page"));
        Assert.AreEqual("ONE BRAIN Browser Executor Fixture", observation.Title);
        Assert.AreEqual(BrowserTargetSource.Cdp, observation.TargetContext.Source);
        Assert.IsTrue(observation.TargetContext.Validate().IsValid);
        Assert.IsTrue(observation.TargetContext.Generation >= 1);
        Assert.AreEqual("main", observation.TargetContext.FrameId);
        Assert.IsTrue(observation.FrameCount >= 1);
    }

    [TestMethod]
    public async Task ObserveReadsFixtureUrlTitleTextAndActionables()
    {
        var browserPath = RequireBrowser();
        await using var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath));
        await using var page = await session.CreatePageAsync(FixtureUri());

        var observation = await page.ObserveAsync("run-cdp-2");

        StringAssert.Contains(observation.Url.ToString(), "basic-form.html");
        Assert.AreEqual("ONE BRAIN Browser Executor Fixture", observation.Title);
        StringAssert.Contains(observation.VisibleTextSummary, "Fixture ready for CDP observation.");
        Assert.IsTrue(observation.ActionableElements.Any(element => element.SelectorCandidates.Contains("#nameInput")));
        Assert.IsTrue(observation.ActionableElements.Any(element => element.SelectorCandidates.Contains("#applyButton")));
    }

    [TestMethod]
    public async Task TypeClickAndVerificationUseRealCdpActionsWithoutConflatingExecutedWithVerified()
    {
        var browserPath = RequireBrowser();
        await using var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath));
        await using var page = await session.CreatePageAsync(FixtureUri());
        var target = await page.GetCurrentTargetContextAsync("run-cdp-3");

        var type = CreateAction(
            "run-cdp-3",
            "step-type",
            "action-type",
            "idem-type",
            target,
            BrowserActionType.TypeText,
            "#nameInput",
            input: "NODAL OS",
            expected: new BrowserExpectedOutcome("input contains typed text", null, null, "NODAL OS"));
        var typeResult = await page.ExecuteActionAsync(type);
        var typeVerification = await page.VerifyAsync(type);

        Assert.IsTrue(typeResult.Executed);
        Assert.AreEqual("Executed", typeResult.Status);
        Assert.IsFalse(ChromeCdpPageSession.ActionResultIsVerified(typeResult));
        Assert.AreEqual(BrowserVerificationStatus.Verified, typeVerification.Status);
        Assert.IsTrue(typeVerification.AllowsStepDone());

        var click = CreateAction(
            "run-cdp-3",
            "step-click",
            "action-click",
            "idem-click",
            await page.GetCurrentTargetContextAsync("run-cdp-3"),
            BrowserActionType.Click,
            "#applyButton",
            expected: new BrowserExpectedOutcome("result text changes", null, "Result: NODAL OS", null));
        var clickResult = await page.ExecuteActionAsync(click);
        var clickVerification = await page.VerifyAsync(click);

        Assert.IsTrue(clickResult.Executed);
        Assert.AreEqual(BrowserVerificationStatus.Verified, clickVerification.Status);
        Assert.IsTrue(clickVerification.EvidenceRefs.Count > 0);
        Assert.IsTrue(clickVerification.HasSemanticProof);
    }

    [TestMethod]
    public async Task VerificationReturnsUncertainWhenExpectationIsNotDeterministic()
    {
        var browserPath = RequireBrowser();
        await using var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath));
        await using var page = await session.CreatePageAsync(FixtureUri());

        var action = CreateAction(
            "run-cdp-4",
            "step-read",
            "action-read",
            "",
            await page.GetCurrentTargetContextAsync("run-cdp-4"),
            BrowserActionType.Read,
            "#intro",
            expected: new BrowserExpectedOutcome("ambiguous read expectation", null, null, null));

        var verification = await page.VerifyAsync(action);

        Assert.AreEqual(BrowserVerificationStatus.Uncertain, verification.Status);
        Assert.IsFalse(verification.AllowsStepDone());
    }

    [TestMethod]
    public async Task TargetStaleBlocksModifyingAction()
    {
        var browserPath = RequireBrowser();
        await using var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath));
        await using var page = await session.CreatePageAsync(FixtureUri());

        var staleTarget = await page.GetCurrentTargetContextAsync("run-cdp-5");
        await page.NavigateAsync(FixtureUri("basic-form.html#new-generation"));

        var action = CreateAction(
            "run-cdp-5",
            "step-stale",
            "action-stale",
            "idem-stale",
            staleTarget,
            BrowserActionType.Click,
            "#applyButton");
        var result = await page.ExecuteActionAsync(action);

        Assert.IsFalse(result.Executed);
        StringAssert.Contains(result.Error ?? "", "stale");
    }

    [TestMethod]
    public async Task IdempotencyPreventsDoubleModifyingActionAndDifferentFingerprint()
    {
        var browserPath = RequireBrowser();
        await using var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath));
        await using var page = await session.CreatePageAsync(FixtureUri());
        var target = await page.GetCurrentTargetContextAsync("run-cdp-6");
        var action = CreateAction("run-cdp-6", "step-click", "action-click", "idem-same", target, BrowserActionType.Click, "#applyButton");

        var first = await page.ExecuteActionAsync(action);
        var duplicate = await page.ExecuteActionAsync(action);
        var different = await page.ExecuteActionAsync(action with { Target = new BrowserActionTarget("candidate-2", "#nameInput", "Name", null) });

        Assert.IsTrue(first.Executed);
        Assert.IsFalse(duplicate.Executed);
        Assert.IsTrue(duplicate.Status is nameof(BrowserReplayStatus.Completed) or nameof(BrowserReplayStatus.RejectedDuplicate));
        Assert.IsFalse(different.Executed);
        Assert.AreEqual(nameof(BrowserReplayStatus.Failed), different.Status);
    }

    [TestMethod]
    public async Task ModifyingActionWithoutIdempotencyIsRejectedButReadOnlyCanRun()
    {
        var browserPath = RequireBrowser();
        await using var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath));
        await using var page = await session.CreatePageAsync(FixtureUri());
        var target = await page.GetCurrentTargetContextAsync("run-cdp-7");

        var click = CreateAction("run-cdp-7", "step-click", "action-click", "", target, BrowserActionType.Click, "#applyButton");
        var read = CreateAction("run-cdp-7", "step-read", "action-read", "", target, BrowserActionType.Read, "#intro", risk: BrowserRiskClass.ReadOnly);

        var clickResult = await page.ExecuteActionAsync(click);
        var readResult = await page.ExecuteActionAsync(read);

        Assert.IsFalse(clickResult.Executed);
        StringAssert.Contains(clickResult.Error ?? "", "IdempotencyKey");
        Assert.IsTrue(readResult.Executed);
    }

    [TestMethod]
    public async Task LivenessDistinguishesAliveFromGenerationMismatch()
    {
        var browserPath = RequireBrowser();
        await using var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath));
        await using var page = await session.CreatePageAsync(FixtureUri());
        var initial = await page.GetCurrentTargetContextAsync("run-cdp-8");

        var alive = await page.ProbeLivenessAsync(initial);
        await page.NavigateAsync(FixtureUri("basic-form.html#stale"));
        var stale = await page.ProbeLivenessAsync(initial);

        Assert.AreEqual(BrowserHeartbeatStatus.Alive, alive.Status);
        Assert.IsTrue(alive.IsStrongAlive);
        Assert.AreEqual(BrowserHeartbeatStatus.Stale, stale.Status);
    }

    [TestMethod]
    public async Task CleanupClosesChromeAndUsesDisposableProfile()
    {
        var browserPath = RequireBrowser();
        int pid;
        string userDataDir;

        await using (var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath)))
        {
            pid = session.ProcessId;
            userDataDir = session.UserDataDir;
            Assert.IsTrue(session.IsProcessAlive);
            Assert.IsTrue(Directory.Exists(userDataDir));
            StringAssert.Contains(userDataDir, "onebrain-cdp-");
        }

        await Task.Delay(500);
        Assert.IsFalse(IsProcessAlive(pid));
        Assert.IsFalse(Directory.Exists(userDataDir));
    }

    private static string RequireBrowser()
    {
        var path = ChromeCdpBrowserLauncher.FindBrowserExecutable();
        if (path is null)
            Assert.Inconclusive("Chrome/Edge executable is not available in this environment.");
        return path!;
    }

    private static Uri FixtureUri(string fileName = "basic-form.html")
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;

        Assert.IsNotNull(dir, "repo root not found");
        return new Uri(Path.Combine(dir.FullName, "tests", "fixtures", "browser-executor", fileName));
    }

    private static BrowserAction CreateAction(
        string runId,
        string stepId,
        string actionId,
        string idempotencyKey,
        BrowserTargetContext target,
        BrowserActionType type,
        string selector,
        string? input = null,
        BrowserExpectedOutcome? expected = null,
        BrowserRiskClass risk = BrowserRiskClass.Low)
    {
        return new BrowserAction(
            ActionId: actionId,
            IdempotencyKey: idempotencyKey,
            RunId: runId,
            StepId: stepId,
            TargetContext: target,
            FrameId: target.FrameId,
            ActionType: type,
            Target: new BrowserActionTarget(selector.TrimStart('#'), selector, selector, null),
            Input: input is null ? null : new BrowserActionInput(input, input, HasModifyingValue: true),
            ExpectedOutcome: expected ?? new BrowserExpectedOutcome("fixture text is visible", null, "Browser Executor Fixture", null),
            RiskClass: risk,
            TimeoutMs: 8000,
            RequiresApproval: false,
            CreatedAtUtc: DateTimeOffset.UtcNow);
    }

    private static bool IsProcessAlive(int pid)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }
}
