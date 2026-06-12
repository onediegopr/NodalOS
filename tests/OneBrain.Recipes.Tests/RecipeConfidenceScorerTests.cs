using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;
using OneBrain.Core.Confidence;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class RecipeConfidenceScorerTests
{
    [TestMethod]
    public void New_Candidate_With_No_Runs_Remains_Candidate()
    {
        var profile = RecipeConfidenceScorer.Score(new RecipeConfidenceInput(
            RecipeId: "demo",
            CandidateFlowId: null,
            Status: RecipeConfidenceStatuses.Candidate,
            RiskLevel: RecipeConfidenceRiskLevels.Medium,
            Runs: 0,
            Successes: 0,
            Failures: 0,
            LastError: null,
            LastVerifiedAt: null,
            ApprovalRequiredUntil: null,
            Notes: []));

        Assert.AreEqual(RecipeConfidenceStatuses.Candidate, profile.Status);
        Assert.IsTrue(profile.ConfidenceScore < 50);
    }

    [TestMethod]
    public void Successful_Low_Risk_History_Becomes_Stable()
    {
        var profile = RecipeConfidenceScorer.Score(new RecipeConfidenceInput(
            RecipeId: "demo",
            CandidateFlowId: null,
            Status: RecipeConfidenceStatuses.Supervised,
            RiskLevel: RecipeConfidenceRiskLevels.Low,
            Runs: 5,
            Successes: 5,
            Failures: 0,
            LastError: null,
            LastVerifiedAt: "2026-06-12T10:00:00Z",
            ApprovalRequiredUntil: null,
            Notes: []));

        Assert.AreEqual(RecipeConfidenceStatuses.Stable, profile.Status);
        Assert.IsTrue(profile.ConfidenceScore >= 85);
    }

    [TestMethod]
    public void Failures_Degrade_Confidence()
    {
        var good = RecipeConfidenceScorer.Score(new RecipeConfidenceInput("demo", null, RecipeConfidenceStatuses.Supervised, RecipeConfidenceRiskLevels.Low, 4, 4, 0, null, null, null, []));
        var degraded = RecipeConfidenceScorer.Score(new RecipeConfidenceInput("demo", null, RecipeConfidenceStatuses.Supervised, RecipeConfidenceRiskLevels.Low, 4, 2, 2, "failed", null, null, []));

        Assert.IsTrue(degraded.ConfidenceScore < good.ConfidenceScore);
        Assert.AreNotEqual(RecipeConfidenceStatuses.Stable, degraded.Status);
    }

    [TestMethod]
    public void Critical_Without_Approval_Is_Blocked()
    {
        var profile = BusinessFlowDemoFixture.CreateConfidenceProfile();

        Assert.AreEqual(RecipeConfidenceStatuses.Blocked, profile.Status);
        Assert.AreEqual(RecipeConfidenceRiskLevels.Critical, profile.RiskLevel);
        Assert.IsTrue(profile.ConfidenceScore <= 20);
        StringAssert.Contains(string.Join(" ", profile.Notes), "blocked");
    }

    [TestMethod]
    public void Critical_With_Approval_Remains_Critical()
    {
        var profile = RecipeConfidenceScorer.Score(new RecipeConfidenceInput(
            RecipeId: "critical-demo",
            CandidateFlowId: "candidate",
            Status: RecipeConfidenceStatuses.Critical,
            RiskLevel: RecipeConfidenceRiskLevels.Critical,
            Runs: 1,
            Successes: 1,
            Failures: 0,
            LastError: null,
            LastVerifiedAt: "2026-06-12T10:00:00Z",
            ApprovalRequiredUntil: "2026-06-13T10:00:00Z",
            Notes: []));

        Assert.AreEqual(RecipeConfidenceStatuses.Critical, profile.Status);
        Assert.IsTrue(profile.ConfidenceScore > 20);
    }
}
