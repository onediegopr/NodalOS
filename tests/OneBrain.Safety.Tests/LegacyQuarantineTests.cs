using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Execution;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class LegacyQuarantineTests
{
    [TestMethod]
    public void LegacyGuardDeniesByDefault()
    {
        var decision = LegacyExecutionGuard.Evaluate(
            "actv.type",
            "actv.type",
            new Dictionary<string, string>(),
            explicitRecipeOptIn: false);

        Assert.IsFalse(decision.Allowed);
        Assert.IsTrue(decision.IsQuarantined);
        StringAssert.Contains(decision.Reason, "LegacyQuarantined");
    }

    [TestMethod]
    public void LegacyGuardDeniesNullOrUnknownSurface()
    {
        var missing = LegacyExecutionGuard.Evaluate("actv.type", null, new Dictionary<string, string>(), true);
        var unknown = LegacyExecutionGuard.Evaluate("actv.type", "unknown", new Dictionary<string, string>(), true);

        Assert.IsFalse(missing.Allowed);
        Assert.IsFalse(unknown.Allowed);
        Assert.AreEqual("LegacyUnknownSurface", missing.Reason);
        Assert.AreEqual("LegacyUnknownSurface", unknown.Reason);
    }

    [TestMethod]
    public void LegacyGuardAllowsOnlyWithExplicitOptIn()
    {
        var environment = new Dictionary<string, string>
        {
            [LegacyExecutionGuard.EnvironmentVariable] = "1"
        };

        var missingRecipe = LegacyExecutionGuard.Evaluate("actv.invoke", "actv.invoke", environment, explicitRecipeOptIn: false);
        var missingEnv = LegacyExecutionGuard.Evaluate("actv.invoke", "actv.invoke", new Dictionary<string, string>(), explicitRecipeOptIn: true);
        var allowed = LegacyExecutionGuard.Evaluate("actv.invoke", "actv.invoke", environment, explicitRecipeOptIn: true);

        Assert.IsFalse(missingRecipe.Allowed);
        Assert.IsFalse(missingEnv.Allowed);
        Assert.IsTrue(allowed.Allowed);
        Assert.AreEqual($"{LegacyExecutionGuard.EnvironmentVariable}+allowLegacyActions", allowed.OptInSource);
    }

    [TestMethod]
    public void LegacyGuardSafeActionsNeverAllowLegacy()
    {
        var environment = new Dictionary<string, string>
        {
            [LegacyExecutionGuard.EnvironmentVariable] = "1"
        };

        foreach (var stepKind in new[] { "safe.click", "safe.read", "safe.type" })
        {
            var decision = LegacyExecutionGuard.Evaluate(stepKind, "UiaActionExecutor", environment, explicitRecipeOptIn: true);

            Assert.IsFalse(decision.Allowed, stepKind);
            Assert.AreEqual("LegacyBlockedForSafeAction", decision.Reason);
        }
    }

    [TestMethod]
    public void LegacyGuardDeniesLegacySurfacesByDefault()
    {
        foreach (var pair in new[]
                 {
                     ("actv.type", "actv.type"),
                     ("actv.invoke", "actv.invoke"),
                     ("key", "key")
                 })
        {
            var decision = LegacyExecutionGuard.Evaluate(pair.Item1, pair.Item2, new Dictionary<string, string>(), false);

            Assert.IsFalse(decision.Allowed, pair.Item1);
        }
    }
}
