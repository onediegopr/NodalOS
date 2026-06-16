using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaVercelTestOwnedTargetM83Tests
{
    private static readonly string[] Routes =
    [
        "",
        "products",
        "document",
        "report",
        "disabled-form",
        "blocked-login",
        "blocked-checkout",
        "blocked-destructive-action",
        "health",
        "ownership"
    ];

    [TestMethod]
    public void VercelTestOwnedTargetAppExists()
    {
        Assert.IsTrue(Directory.Exists(AppRoot()));
        Assert.IsTrue(File.Exists(Path.Combine(AppRoot(), "vercel.json")));
        Assert.IsTrue(File.Exists(Path.Combine(AppRoot(), "robots.txt")));
    }

    [TestMethod]
    public void VercelTestOwnedTargetExpectedRoutesExist()
    {
        foreach (var route in Routes)
        {
            var file = string.IsNullOrWhiteSpace(route)
                ? Path.Combine(AppRoot(), "index.html")
                : Path.Combine(AppRoot(), route, "index.html");
            Assert.IsTrue(File.Exists(file), file);
        }
    }

    [TestMethod]
    public void VercelTestOwnedTargetMetadataIsPresent()
    {
        var home = File.ReadAllText(Path.Combine(AppRoot(), "index.html"));

        StringAssert.Contains(home, "project: NODAL OS");
        StringAssert.Contains(home, "purpose: test-owned-read-only-target");
        StringAssert.Contains(home, "owner: synthetic-lab");
        StringAssert.Contains(home, "environment: vercel-hobby-lab");
        StringAssert.Contains(home, "no-real-users");
        StringAssert.Contains(home, "no-real-credentials");
        StringAssert.Contains(home, "no-real-payments");
        StringAssert.Contains(home, "no-submit");
        StringAssert.Contains(home, "read-only");
    }

    [TestMethod]
    public void VercelTestOwnedTargetHasNoPostOrMutatingApi()
    {
        var text = AppText();

        Assert.IsFalse(text.Contains("method=\"post\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("method='post'", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("fetch(", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("XMLHttpRequest", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("/api/", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void VercelTestOwnedTargetHasNoRealLoginCheckoutSecretsOrPii()
    {
        var text = AppText(includeReadme: false);

        Assert.IsFalse(text.Contains("type=\"password\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("action=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("sk_live", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("pk_live", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("@example.com", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("dni", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("cuit", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void VercelTestOwnedTargetBlockedPagesAreClearlyFixtures()
    {
        foreach (var route in new[] { "disabled-form", "blocked-login", "blocked-checkout", "blocked-destructive-action" })
        {
            var html = File.ReadAllText(Path.Combine(AppRoot(), route, "index.html"));
            StringAssert.Contains(html, "NODAL_OS_BLOCKED_FIXTURE");
            StringAssert.Contains(html, "Read-only observation allowed");
        }
    }

    private static string AppText(bool includeReadme = true)
    {
        var files = Directory.GetFiles(AppRoot(), "*", SearchOption.AllDirectories)
            .Where(path => includeReadme || !path.EndsWith("README.md", StringComparison.OrdinalIgnoreCase));
        return string.Join("\n", files.Select(File.ReadAllText));
    }

    private static string AppRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "apps", "nexa-test-owned-target");
            if (Directory.Exists(candidate))
                return candidate;
            current = current.Parent;
        }

        Assert.Fail("Could not locate apps/nexa-test-owned-target from test base directory.");
        return "";
    }
}
