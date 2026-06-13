using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using OneBrain.Core.Contracts;
using OneBrain.Core.Selectors;
using OneBrain.Core.Selectors.Web;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class WebSelectorBridgeTests
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [TestMethod]
    public void SingleCandidateAgreesWithLegacy()
    {
        var fixture = LoadFixture("single-candidate.json");

        var parity = WebSelectorBridge.Evaluate(fixture.Candidates, fixture.TargetText, fixture.LegacySelectedName);

        Assert.IsTrue(parity.EngineFound);
        Assert.AreEqual("LikelySame", parity.EngineVerdict);
        Assert.AreEqual("More information...", parity.EngineSelectedName);
        Assert.IsTrue(parity.AgreesWithLegacy);
    }

    [TestMethod]
    public void DuplicateEquivalentCandidatesReturnAmbiguousWithoutFirstWins()
    {
        var fixture = LoadFixture("duplicate-equivalent-candidates.json");

        var parity = WebSelectorBridge.Evaluate(fixture.Candidates, fixture.TargetText, fixture.LegacySelectedName);

        Assert.IsFalse(parity.EngineFound);
        Assert.IsTrue(parity.Ambiguous);
        Assert.AreEqual(FailureKind.Ambiguous, parity.FailureKind);
        Assert.AreEqual("Ambiguous", parity.EngineVerdict);
        Assert.IsTrue(parity.AgreesWithLegacy);
    }

    [TestMethod]
    public void AutomationIdCandidatePreferredWhenSelectorSupportsIt()
    {
        var fixture = LoadFixture("automation-id-strong.json");
        var selector = new SelectorDefinition
        {
            Provenance = Provenance.Inferred,
            AutomationId = "OneBrainSearchInput"
        };

        var parity = WebSelectorBridge.Evaluate(selector, fixture.Candidates, fixture.LegacySelectedName);

        Assert.IsTrue(parity.EngineFound);
        Assert.AreEqual("Buscar", parity.EngineSelectedName);
        Assert.AreEqual("LikelySame", parity.EngineVerdict);
        Assert.IsTrue(parity.AgreesWithLegacy);
    }

    [TestMethod]
    public void EmptyCandidatesReturnNotFound()
    {
        var fixture = LoadFixture("empty-candidates.json");

        var parity = WebSelectorBridge.Evaluate(fixture.Candidates, fixture.TargetText, fixture.LegacySelectedName);

        Assert.IsFalse(parity.EngineFound);
        Assert.AreEqual(FailureKind.NotFound, parity.FailureKind);
        Assert.AreEqual("NotFound", parity.EngineVerdict);
        Assert.IsTrue(parity.AgreesWithLegacy);
    }

    [TestMethod]
    public void WeakWebIdentityNeverReturnsSame()
    {
        var fixture = LoadFixture("weak-identity-no-runtimeid.json");

        var parity = WebSelectorBridge.Evaluate(fixture.Candidates, fixture.TargetText, fixture.LegacySelectedName);

        Assert.IsTrue(parity.EngineFound);
        Assert.AreNotEqual("Same", parity.EngineVerdict);
    }

    [TestMethod]
    public void DisabledOrOffscreenCandidatesProduceReasons()
    {
        var fixture = LoadFixture("disabled-offscreen-candidates.json");

        var parity = WebSelectorBridge.Evaluate(fixture.Candidates, fixture.TargetText, fixture.LegacySelectedName);
        var reasons = string.Join(" | ", parity.Reasons);

        StringAssert.Contains(reasons, "disabled");
        StringAssert.Contains(reasons, "offscreen");
    }

    [TestMethod]
    public void MalformedInputFailsClosed()
    {
        var parity = WebSelectorBridge.Evaluate(candidates: null, targetText: " ", legacySelectedName: "");

        Assert.IsFalse(parity.EngineFound);
        Assert.AreEqual("InvalidInput", parity.EngineVerdict);
        Assert.IsTrue(parity.AgreesWithLegacy);
    }

    [TestMethod]
    public void NameOnlyCollisionDoesNotReturnStrongSame()
    {
        var fixture = LoadFixture("duplicate-equivalent-candidates.json");

        var parity = WebSelectorBridge.Evaluate(fixture.Candidates, fixture.TargetText, fixture.LegacySelectedName);

        Assert.AreNotEqual("Same", parity.EngineVerdict);
        Assert.IsTrue(parity.Ambiguous);
    }

    [TestMethod]
    public void ShadowParityDoesNotMutateLegacySelection()
    {
        var fixture = LoadFixture("single-candidate.json");
        var legacySelectedName = fixture.LegacySelectedName;

        _ = WebSelectorBridge.Evaluate(fixture.Candidates, fixture.TargetText, legacySelectedName);

        Assert.AreEqual("More information...", legacySelectedName);
    }

    private static WebParityFixture LoadFixture(string fileName)
    {
        var path = Path.Combine(RepoRoot, "tools", "fixtures", "web-parity", fileName);
        var fixture = JsonSerializer.Deserialize<WebParityFixture>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(fixture);
        Assert.AreEqual(1, fixture.SchemaVersion);
        return fixture;
    }

    private sealed record WebParityFixture
    {
        public int SchemaVersion { get; init; }
        public string TargetText { get; init; } = "";
        public string LegacySelectedName { get; init; } = "";
        public IReadOnlyList<WebCandidate> Candidates { get; init; } = Array.Empty<WebCandidate>();
    }
}
