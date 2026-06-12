using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Memory;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class WorkflowRetrievalServiceTests
{
    [TestMethod]
    public void Retrieval_Matches_By_Tag_App_And_Domain()
    {
        var result = WorkflowRetrievalService.Search(
            ProcessMemoryDemoFixture.CreateEntries(),
            new WorkflowRetrievalQuery(Text: "report", Tags: ["product-evidence"], AppOrSite: "Pilot", Domain: "local-demo"));

        var first = result.Matches.First();

        Assert.AreEqual("process-demo-product-evidence-report", first.ProcessMemoryId);
        Assert.IsTrue(first.Score > 60);
        Assert.IsTrue(first.Reasons.Any(reason => reason.Contains("tag match", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(first.Reasons.Any(reason => reason.Contains("app/site", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(first.SafeToSuggest);
    }

    [TestMethod]
    public void Retrieval_Explains_Score_And_Links_Candidate_Flow()
    {
        var result = WorkflowRetrievalService.Search(
            ProcessMemoryDemoFixture.CreateEntries(),
            new WorkflowRetrievalQuery(Text: "message", Tags: ["whatsapp"]));

        var match = result.Matches.First();

        Assert.AreEqual("candidate-whatsapp-browser-demo-v0", match.CandidateFlowId);
        Assert.IsTrue(match.Reasons.Count > 0);
        Assert.IsTrue(match.RequiresHumanReview);
    }

    [TestMethod]
    public void Rejected_Or_Archived_Process_Is_Not_Safe_To_Suggest()
    {
        var result = WorkflowRetrievalService.Search(
            ProcessMemoryDemoFixture.CreateEntries(),
            new WorkflowRetrievalQuery(Text: "unsafe", Tags: ["archived"]));

        var match = result.Matches.First();

        Assert.AreEqual("process-archived-unsafe-demo", match.ProcessMemoryId);
        Assert.IsFalse(match.SafeToSuggest);
        Assert.IsTrue(match.RequiresHumanReview);
    }
}
