using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaDomainBindingReadinessM84Tests
{
    [TestMethod]
    public void DomainBindingReadinessDefaultUsesLabDomain()
    {
        var config = new NexaTargetBindingReadinessEvaluator().CreateDefault();

        Assert.IsTrue(string.Equals(config.ExpectedDomain, "lab.nodalos.com.ar", StringComparison.Ordinal));
        Assert.IsTrue(string.Equals(config.ExpectedBaseUrl, "https://lab.nodalos.com.ar", StringComparison.Ordinal));
    }

    [TestMethod]
    public void DomainBindingReadinessAllowedHostsOnlyIncludeExpectedSubdomain()
    {
        var config = ReadyConfig();

        Assert.AreEqual(1, config.AllowedHosts.Count);
        CollectionAssert.Contains(config.AllowedHosts.ToList(), "lab.nodalos.com.ar");
        CollectionAssert.DoesNotContain(config.AllowedHosts.ToList(), "nodalos.com.ar");
        CollectionAssert.DoesNotContain(config.AllowedHosts.ToList(), "nexalab.nodalos.com.ar");
    }

    [TestMethod]
    public void DomainBindingReadinessRequiresHealthAndOwnershipPaths()
    {
        var config = ReadyConfig();

        CollectionAssert.Contains(config.AllowedPaths.ToList(), "/health");
        CollectionAssert.Contains(config.AllowedPaths.ToList(), "/ownership");
    }

    [TestMethod]
    public void DomainBindingReadinessUnknownDnsDoesNotAllowLiveProof()
    {
        var decision = new NexaTargetBindingReadinessEvaluator().Evaluate(new NexaTargetBindingReadinessEvaluator().CreateDefault());

        Assert.IsFalse(decision.CandidateLiveProofAllowed);
        Assert.IsFalse(decision.ExecutesNetwork);
    }

    [TestMethod]
    public void DomainBindingReadinessHttpsAndOwnershipVerifiedAllowsCandidateButDoesNotExecute()
    {
        var decision = new NexaTargetBindingReadinessEvaluator().Evaluate(ReadyConfig());

        Assert.IsTrue(decision.CandidateLiveProofAllowed);
        Assert.IsFalse(decision.ExecutesNetwork);
    }

    [TestMethod]
    public void DomainBindingReadinessDoesNotAssumeRootDomain()
    {
        var config = ReadyConfig() with
        {
            AllowedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "lab.nodalos.com.ar", "nodalos.com.ar" }
        };
        var decision = new NexaTargetBindingReadinessEvaluator().Evaluate(config);

        Assert.IsFalse(decision.CandidateLiveProofAllowed);
        Assert.IsTrue(decision.ReasonCodes.Any(reason => reason.Contains("subdomain", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void DomainBindingReadinessLegacyNexaLabIsNotOperationalHost()
    {
        var config = ReadyConfig() with
        {
            AllowedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "lab.nodalos.com.ar", "nexalab.nodalos.com.ar" }
        };

        var decision = new NexaTargetBindingReadinessEvaluator().Evaluate(config);

        Assert.IsFalse(decision.CandidateLiveProofAllowed);
        Assert.IsTrue(decision.ReasonCodes.Any(reason => reason.Contains("only the expected lab subdomain", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void DomainBindingReadinessDoesNotReferenceHyphenatedLegacyDomain()
    {
        var repoRoot = FindRepoRoot();
        var files = Directory.GetFiles(repoRoot, "*", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => Path.GetExtension(path) is ".cs" or ".md" or ".html" or ".json" or ".txt");

        var hyphenatedLegacyDomain = "nexa" + "-lab.nodalos.com.ar";
        var matches = files
            .Where(path => File.ReadAllText(path).Contains(hyphenatedLegacyDomain, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.AreEqual(0, matches.Length, string.Join(Environment.NewLine, matches));
    }

    internal static NexaTargetBindingConfig ReadyConfig() =>
        new NexaTargetBindingReadinessEvaluator().CreateDefault(
            NexaTargetBindingDnsMode.CnameOnly,
            NexaTargetBindingVerificationStatus.OwnershipVerified);

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "OneBrain.slnx")))
                return current.FullName;
            current = current.Parent;
        }

        Assert.Fail("Could not locate repository root.");
        return "";
    }
}
