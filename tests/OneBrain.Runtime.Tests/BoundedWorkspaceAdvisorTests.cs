using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Core.Workspace;

namespace OneBrain.Runtime.Tests;

[TestClass]
[TestCategory("AiriSelectiveRuntime")]
[TestCategory("MvpVerticalSlice")]
[TestCategory("ExpertAdvisor")]
public sealed class BoundedWorkspaceAdvisorTests
{
    [TestMethod]
    public async Task BalancedAdvisorReportsSecretRedactionAndMissingTestsWithoutExecutionAuthority()
    {
        var root = CreateRoot();
        var fakeSecret = "s" + "k-advisor-fixture-secret-value-123456789";
        try
        {
            await WriteAsync(root, "app.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
            await WriteAsync(
                root,
                "src/Program.cs",
                $"var api_key = \"{fakeSecret}\";{Environment.NewLine}Console.WriteLine(\"fixture\");");
            var (scan, planning) = await ScanAndPlanAsync(root);

            var result = new BoundedWorkspaceAdvisorService().Evaluate(
                scan,
                planning,
                new BoundedWorkspaceAdvisorSettings(BoundedWorkspaceAdvisorProfile.Balanced, 60));

            Assert.IsTrue(result.Accepted);
            Assert.AreEqual("GO_EXPERT_ADVISOR_SUGGESTIONS_READY", result.Decision);
            Assert.IsTrue(result.Suggestions.Any(value =>
                value.Category == BoundedWorkspaceAdvisorCategory.Audit &&
                value.Severity == BoundedWorkspaceAdvisorSeverity.High));
            Assert.IsTrue(result.Suggestions.Any(value =>
                value.Category == BoundedWorkspaceAdvisorCategory.Improvement &&
                value.Title.Contains("Test coverage", StringComparison.Ordinal)));
            Assert.IsTrue(result.Suggestions.All(value => value.NonExecutable));
            Assert.IsTrue(result.Suggestions.All(value => !value.CanAuthorizeExecution));
            Assert.IsTrue(result.Suggestions.All(value => !value.CallsModelProvider));
            Assert.IsTrue(result.Suggestions.All(value => !value.CreatesPrompt));
            Assert.IsTrue(result.Suggestions.All(value => !value.FilesystemMutationAllowed));
            Assert.IsTrue(result.Suggestions.All(value => !value.NetworkUsed));
            Assert.IsTrue(result.Suggestions.All(value => !value.ProductAuthorityGranted));
            Assert.IsFalse(result.CallsModelProvider);
            Assert.IsFalse(result.FilesystemMutationAllowed);
            Assert.IsFalse(result.NetworkUsed);
            Assert.IsFalse(result.ProductAuthorityGranted);
            var rendered = string.Join(" ", result.Suggestions.Select(value => value.Title + " " + value.MessageRedacted));
            Assert.IsFalse(rendered.Contains(root, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(rendered.Contains(fakeSecret, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task SilentProfileSuppressesOrdinaryAdviceButRetainsHighSeveritySecretAudit()
    {
        var root = CreateRoot();
        var fakeSecret = "s" + "k-silent-advisor-secret-value-123456789";
        try
        {
            await WriteAsync(root, "app.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
            await WriteAsync(root, "src/Program.cs", $"var secret = \"{fakeSecret}\";");
            var (scan, planning) = await ScanAndPlanAsync(root);

            var result = new BoundedWorkspaceAdvisorService().Evaluate(
                scan,
                planning,
                new BoundedWorkspaceAdvisorSettings(BoundedWorkspaceAdvisorProfile.Silent, 100));

            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(1, result.Suggestions.Count);
            Assert.AreEqual(BoundedWorkspaceAdvisorCategory.Audit, result.Suggestions[0].Category);
            Assert.AreEqual(BoundedWorkspaceAdvisorSeverity.High, result.Suggestions[0].Severity);
            Assert.IsTrue(result.Suggestions[0].RequiresHumanAttention);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task ActiveAdvisorSurfacesMultiEcosystemBoundaryAndRemainsDeterministic()
    {
        var root = CreateRoot();
        try
        {
            await WriteAsync(root, "app.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
            await WriteAsync(root, "src/Program.cs", "Console.WriteLine(\"fixture\");");
            await WriteAsync(root, "tests/ProgramTests.cs", "public sealed class ProgramTests { }");
            await WriteAsync(root, "package.json", "{\"scripts\":{\"test\":\"node --test\"}}");
            await WriteAsync(root, "web/app.ts", "export const ready = true;");
            var (scan, planning) = await ScanAndPlanAsync(root);
            var settings = new BoundedWorkspaceAdvisorSettings(BoundedWorkspaceAdvisorProfile.Active, 100);

            var first = new BoundedWorkspaceAdvisorService().Evaluate(scan, planning, settings);
            var second = new BoundedWorkspaceAdvisorService().Evaluate(scan, planning, settings);

            Assert.IsTrue(first.Suggestions.Any(value =>
                value.Category == BoundedWorkspaceAdvisorCategory.Architecture &&
                value.Title.Contains("Multiple implementation ecosystems", StringComparison.Ordinal)));
            CollectionAssert.AreEqual(
                first.Suggestions.Select(value => value.SuggestionId).ToArray(),
                second.Suggestions.Select(value => value.SuggestionId).ToArray());
            CollectionAssert.AreEqual(
                first.Suggestions.Select(value => value.MessageRedacted).ToArray(),
                second.Suggestions.Select(value => value.MessageRedacted).ToArray());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task TruncatedEvidenceProducesBoundedRiskWithoutAuthorizingBudgetExpansion()
    {
        var root = CreateRoot();
        try
        {
            await WriteAsync(root, "a.cs", "public sealed class A { }");
            await WriteAsync(root, "b.cs", "public sealed class B { }");
            await WriteAsync(root, "c.cs", "public sealed class C { }");
            var limits = new BoundedWorkspaceScanLimits(
                MaximumFiles: 1,
                MaximumTotalBytes: 1024,
                MaximumFileBytes: 1024,
                MaximumDepth: 4,
                MaximumPreviewCharacters: 200);
            var (scan, planning) = await ScanAndPlanAsync(root, limits);

            var result = new BoundedWorkspaceAdvisorService().Evaluate(
                scan,
                planning,
                new BoundedWorkspaceAdvisorSettings(BoundedWorkspaceAdvisorProfile.Balanced, 100));

            var risk = result.Suggestions.Single(value => value.SuggestionId.EndsWith("bounded-context", StringComparison.Ordinal));
            Assert.AreEqual(BoundedWorkspaceAdvisorCategory.Risk, risk.Category);
            StringAssert.Contains(risk.MessageRedacted, "new operator-authorized mission");
            Assert.IsFalse(risk.CanAuthorizeExecution);
            Assert.IsFalse(result.ProductAuthorityGranted);
            Assert.AreEqual(1, scan.FilesRead);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task RejectedPlanningContextFailsClosedWithoutSuggestionsOrProviderCalls()
    {
        var missingRoot = Path.Combine(Path.GetTempPath(), "nodal-advisor-missing-" + Guid.NewGuid().ToString("N"));
        var scan = await new BoundedWorkspaceUnderstandingService().ScanAsync(
            new BoundedWorkspaceScanRequest(missingRoot));
        var planning = new BoundedWorkspacePlanningContextService().Build(
            scan,
            "workspace-missing",
            "mission-missing");

        var result = new BoundedWorkspaceAdvisorService().Evaluate(scan, planning);

        Assert.IsFalse(result.Accepted);
        Assert.AreEqual("BLOCKED_EXPERT_ADVISOR_INPUT_FAIL_CLOSED", result.Decision);
        Assert.AreEqual(0, result.Suggestions.Count);
        Assert.IsTrue(result.Blockers.Count > 0);
        Assert.IsTrue(result.NonExecutor);
        Assert.IsFalse(result.CallsModelProvider);
        Assert.IsFalse(result.CreatesPrompt);
        Assert.IsFalse(result.FilesystemMutationAllowed);
        Assert.IsFalse(result.NetworkUsed);
        Assert.IsFalse(result.ProductAuthorityGranted);
    }

    private static async ValueTask<(BoundedWorkspaceScanResult Scan, BoundedWorkspacePlanningProjectionResult Planning)> ScanAndPlanAsync(
        string root,
        BoundedWorkspaceScanLimits? limits = null)
    {
        var scan = await new BoundedWorkspaceUnderstandingService().ScanAsync(
            new BoundedWorkspaceScanRequest(root, Limits: limits));
        var planning = new BoundedWorkspacePlanningContextService().Build(
            scan,
            "workspace-advisor-fixture",
            "mission-advisor-fixture");
        return (scan, planning);
    }

    private static async Task WriteAsync(string root, string relativePath, string content)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, content);
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "nodal-advisor-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
