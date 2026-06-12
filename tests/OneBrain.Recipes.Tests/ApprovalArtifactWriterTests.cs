using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class ApprovalArtifactWriterTests
{
    [TestMethod]
    public void WriteRequest_Creates_File_Under_Artifacts_Approvals()
    {
        var temp = CreateTempDir();
        var request = BusinessFlowDemoFixture.CreateSendMessageApproval();

        var result = ApprovalArtifactWriter.WriteRequest(temp, request);

        Assert.IsTrue(result.Success, result.Error);
        Assert.IsTrue(File.Exists(result.Path));
        StringAssert.Contains(result.RelativePath.Replace('\\', '/'), "artifacts/approvals/");
        StringAssert.Contains(File.ReadAllText(result.Path), ApprovalArtifactWriter.RequestSchemaVersion);
    }

    [TestMethod]
    public void WriteDecision_Creates_File_Under_Artifacts_Approvals()
    {
        var temp = CreateTempDir();
        var decision = BusinessFlowDemoFixture.CreateRejectedDecision();

        var result = ApprovalArtifactWriter.WriteDecision(temp, decision);

        Assert.IsTrue(result.Success, result.Error);
        Assert.IsTrue(File.Exists(result.Path));
        StringAssert.Contains(result.RelativePath.Replace('\\', '/'), "artifacts/approvals/");
        StringAssert.Contains(File.ReadAllText(result.Path), ApprovalArtifactWriter.DecisionSchemaVersion);
    }

    [TestMethod]
    public void Writer_Uses_Unique_Path_When_File_Exists()
    {
        var temp = CreateTempDir();
        var request = BusinessFlowDemoFixture.CreateSendMessageApproval();

        var first = ApprovalArtifactWriter.WriteRequest(temp, request);
        var second = ApprovalArtifactWriter.WriteRequest(temp, request);

        Assert.IsTrue(first.Success);
        Assert.IsTrue(second.Success);
        Assert.AreNotEqual(first.Path, second.Path);
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "onebrain-approval-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
