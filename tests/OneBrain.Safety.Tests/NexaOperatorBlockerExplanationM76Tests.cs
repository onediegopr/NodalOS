using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaOperatorBlockerExplanationM76Tests
{
    [TestMethod]
    public void OperatorBlockerExplanationContainsRequiredFields()
    {
        foreach (var scenario in Enum.GetValues<NexaOperatorBlockerScenario>())
        {
            var explanation = new NexaOperatorBlockerExplanationService().Explain(scenario, ["evidence:local:redacted"]);

            AssertRequired(explanation);
            CollectionAssert.Contains(explanation.EvidenceRefs.ToList(), "evidence:local:redacted");
        }
    }

    [TestMethod]
    public void OperatorBlockerExplanationForExternalTargetIsSpecific()
    {
        var explanation = Explain(NexaOperatorBlockerScenario.MissingTestOwnedExternalTarget);

        Assert.AreEqual(NexaOperatorBlockerCategory.ExternalTargetMissing, explanation.Category);
        Assert.IsTrue(explanation.Cause.Contains("M51/M65", StringComparison.Ordinal));
        Assert.IsTrue(explanation.BlockedOptions.Any(option => option.Contains("external/live validated", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void OperatorBlockerExplanationForWorktreeNamesCanonicalPath()
    {
        var explanation = Explain(NexaOperatorBlockerScenario.NonCanonicalWorktree);

        Assert.AreEqual(NexaOperatorBlockerCategory.Worktree, explanation.Category);
        Assert.IsTrue(explanation.UserExpectedAction.Contains("Codigo-m12-audit", StringComparison.Ordinal));
        Assert.IsTrue(explanation.BlockedOptions.Any(option => option.Contains("dirty legacy Codigo", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void OperatorBlockerExplanationForCorePermissionPreservesCoreAuthority()
    {
        var explanation = Explain(NexaOperatorBlockerScenario.CorePermissionMissing);

        Assert.AreEqual(NexaOperatorBlockerCategory.CorePermission, explanation.Category);
        Assert.IsTrue(explanation.BlockedOptions.Any(option => option.Contains("UI override", StringComparison.Ordinal)));
        Assert.IsTrue(explanation.RecommendedNextStep.Contains("Core decide", StringComparison.Ordinal));
    }

    [TestMethod]
    public void OperatorBlockerExplanationDoesNotExposeSecrets()
    {
        var explanation = new NexaOperatorBlockerExplanationService().Explain(
            NexaOperatorBlockerScenario.RealCredentialsBlocked,
            ["opaque-token-value-123456789", "synthetic-cookie-session-value"]);
        var serialized = System.Text.Json.JsonSerializer.Serialize(explanation);

        Assert.IsTrue(explanation.Redacted);
        Assert.IsFalse(serialized.Contains("opaque-token-value-123456789", StringComparison.Ordinal));
        Assert.IsFalse(serialized.Contains("synthetic-cookie-session-value", StringComparison.Ordinal));
    }

    private static NexaOperatorBlockerExplanation Explain(NexaOperatorBlockerScenario scenario) =>
        new NexaOperatorBlockerExplanationService().Explain(scenario);

    private static void AssertRequired(NexaOperatorBlockerExplanation explanation)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(explanation.Cause));
        Assert.IsFalse(string.IsNullOrWhiteSpace(explanation.UserExpectedAction));
        Assert.IsTrue(explanation.SafeOptions.Count > 0);
        Assert.IsTrue(explanation.BlockedOptions.Count > 0);
        Assert.IsFalse(string.IsNullOrWhiteSpace(explanation.RecommendedNextStep));
        Assert.IsFalse(string.IsNullOrWhiteSpace(explanation.OperatorReadableMessage));
        Assert.IsTrue(explanation.Redacted);
    }
}
