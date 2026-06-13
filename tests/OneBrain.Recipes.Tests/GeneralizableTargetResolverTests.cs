using System.Diagnostics;
using OneBrain.Core.Contracts;
using OneBrain.Core.ExecutorHarness;
using OneBrain.Core.Identity;
using OneBrain.Core.Models;
using OneBrain.Core.Selectors;
using OneBrain.Observation.Sensors;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class GeneralizableTargetResolverTests
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [TestMethod]
    public void SameRuntimeIdReturnsSame()
    {
        var expected = new ElementIdentity("42.1.9", "Button", "Enviar", "SendButton");
        var candidate = new ElementIdentity("42.1.9", "Edit", "Otro", "Other");

        var result = ElementMatcher.Match(expected, [candidate]);

        Assert.AreEqual(ElementMatchVerdict.Same, result.Verdict);
        Assert.AreEqual(candidate, result.BestMatch);
    }

    [TestMethod]
    public void RecreatedRuntimeIdWithStrongWeakMatchReturnsLikelySame()
    {
        var expected = new ElementIdentity("old-runtime", "Edit", "Cliente", "CustomerInput")
        {
            ControlType = "Edit",
            AncestorPath = "Window:Recreated Runtime Id > Group:Form"
        };
        var candidate = LoadFixtureIdentities("recreated-runtime-id.json")
            .Single(identity => identity.AutomationId == "CustomerInput");

        var result = ElementMatcher.Match(expected, [candidate]);

        Assert.AreEqual(ElementMatchVerdict.LikelySame, result.Verdict);
        StringAssert.Contains(string.Join(" | ", result.ReasonsFor), "recreated");
    }

    [TestMethod]
    public void NoCandidatesReturnsStaleOrUnknown()
    {
        var staleExpected = new ElementIdentity("runtime", "Button", "Enviar", "Send");
        var stale = ElementMatcher.Match(staleExpected, []);
        Assert.AreEqual(ElementMatchVerdict.Stale, stale.Verdict);

        var unknownExpected = new ElementIdentity();
        var unknown = ElementMatcher.Match(unknownExpected, []);
        Assert.AreEqual(ElementMatchVerdict.Unknown, unknown.Verdict);
    }

    [TestMethod]
    public void DuplicateEquivalentCandidatesReturnAmbiguous()
    {
        var candidates = LoadFixtureIdentities("duplicate-buttons.json")
            .Where(identity => identity.EffectiveControlType == "Button")
            .ToList();
        var expected = new ElementIdentity("", "Button", "Enviar", "");

        var result = ElementMatcher.Match(expected, candidates);

        Assert.AreEqual(ElementMatchVerdict.Ambiguous, result.Verdict);
        Assert.AreEqual(2, result.Candidates.Count);
    }

    [TestMethod]
    public void NameOnlyDoesNotOverrideAmbiguity()
    {
        var candidates = LoadFixtureIdentities("duplicate-buttons.json");
        var selector = new SelectorDefinition
        {
            Name = "Enviar",
            Provenance = Provenance.Fixture
        };

        var resolution = SelectorEngine.Resolve(selector, candidates);

        Assert.IsFalse(resolution.Success);
        Assert.IsTrue(resolution.Ambiguous);
        Assert.AreEqual(FailureKind.Ambiguous, resolution.FailureKind);
    }

    [TestMethod]
    public void DifferentAutomationAndDifferentRoleReturnsDifferent()
    {
        var expected = new ElementIdentity("", "Edit", "Cliente", "CustomerInput")
        {
            ControlType = "Edit"
        };
        var candidate = new ElementIdentity("", "Button", "Guardar", "SaveButton")
        {
            ControlType = "Button"
        };

        var result = ElementMatcher.Match(expected, [candidate]);

        Assert.AreEqual(ElementMatchVerdict.Different, result.Verdict);
    }

    [TestMethod]
    public void SpanishEnglishNameVarianceDoesNotBreakStrongAutomationMatch()
    {
        var expected = new ElementIdentity("", "Button", "Close", "CloseButtonEs")
        {
            ControlType = "Button",
            ClassName = "Button"
        };
        var candidate = LoadFixtureIdentities("close-cerrar-dangerous.json")
            .Single(identity => identity.AutomationId == "CloseButtonEs");

        var result = ElementMatcher.Match(expected, [candidate]);

        Assert.AreEqual(ElementMatchVerdict.LikelySame, result.Verdict);
    }

    [TestMethod]
    public void GenerateThenResolveSameTree()
    {
        var identities = LoadFixtureIdentities("automation-id-strong.json");
        var observed = identities.Single(identity => identity.AutomationId == "OneBrainSearchInput");
        var selector = SelectorEngine.GenerateSelector(observed);

        var resolution = SelectorEngine.Resolve(selector, identities);

        Assert.IsTrue(resolution.Success);
        Assert.AreEqual(observed.RuntimeId, resolution.BestMatch?.RuntimeId);
    }

    [TestMethod]
    public void LegacyRoleNameSelectorParses()
    {
        var parsed = SelectorEngine.TryParseLegacySelector("role:Edit|name:ONE Brain Search", out var selector);

        Assert.IsTrue(parsed);
        Assert.AreEqual("Edit", selector.Role);
        Assert.AreEqual("ONE Brain Search", selector.Name);
    }

    [TestMethod]
    public void LegacyAutomationIdSelectorParses()
    {
        var parsed = SelectorEngine.TryParseLegacySelector("automation-id:OneBrainSearchInput", out var selector);

        Assert.IsTrue(parsed);
        Assert.AreEqual("OneBrainSearchInput", selector.AutomationId);
    }

    [TestMethod]
    public void DuplicateButtonsReturnAmbiguous()
    {
        var selector = new SelectorDefinition
        {
            Role = "Button",
            Name = "Enviar",
            Provenance = Provenance.Fixture
        };

        var resolution = SelectorEngine.Resolve(selector, LoadFixtureIdentities("duplicate-buttons.json"));

        Assert.IsFalse(resolution.Success);
        Assert.AreEqual(FailureKind.Ambiguous, resolution.FailureKind);
    }

    [TestMethod]
    public void TextLabelDoesNotWinOverEditWhenRoleRequiresEdit()
    {
        var selector = new SelectorDefinition
        {
            Role = "Edit",
            Name = "ONE Brain Search",
            Provenance = Provenance.Fixture
        };

        var resolution = SelectorEngine.Resolve(selector, LoadFixtureIdentities("edit-vs-text-label.json"));

        Assert.IsTrue(resolution.Success);
        Assert.AreEqual("OneBrainSearchInput", resolution.BestMatch?.AutomationId);
    }

    [TestMethod]
    public void OneBrainSearchHistoricalCaseResolvesExpectedEdit()
    {
        var selector = new SelectorDefinition
        {
            Name = "ONE Brain Search",
            Provenance = Provenance.Fixture,
            ExpectedIdentity = new ElementIdentity("", "Edit", "", "OneBrainSearchInput")
            {
                ControlType = "Edit",
                ClassName = "TextBox"
            }
        };

        var resolution = SelectorEngine.Resolve(selector, LoadFixtureIdentities("edit-vs-text-label.json"));

        Assert.IsTrue(resolution.Success);
        Assert.AreEqual("Edit", resolution.BestMatch?.EffectiveControlType);
        Assert.AreEqual("OneBrainSearchInput", resolution.BestMatch?.AutomationId);
    }

    [TestMethod]
    public void CloseCerrarDangerousNameDoesNotBecomeSafeBySelectorEngine()
    {
        var identities = LoadFixtureIdentities("close-cerrar-dangerous.json");
        var selector = new SelectorDefinition
        {
            AutomationId = "CloseButtonEn",
            Provenance = Provenance.Fixture
        };

        var resolution = SelectorEngine.Resolve(selector, identities);

        Assert.IsTrue(resolution.Success);
        Assert.AreEqual("Close", resolution.BestMatch?.Name);
        Assert.AreEqual(0d, selector.SafetyScore);
    }

    [TestMethod]
    public void UnknownSelectorReturnsNotFound()
    {
        var resolution = SelectorEngine.Resolve(new SelectorDefinition(), LoadFixtureIdentities("automation-id-strong.json"));

        Assert.IsFalse(resolution.Success);
        Assert.AreEqual(FailureKind.NotFound, resolution.FailureKind);
    }

    [TestMethod]
    public void HarnessResolver_RemainsScoped_To_Allowlisted_Target_Through_SelectorEngine()
    {
        var target = ExecutorHarnessDemoFixture.CreateTarget();

        var resolution = ExecutorHarnessTargetResolver.ResolveTarget(target);

        Assert.IsTrue(resolution.Success);
        CollectionAssert.Contains(resolution.Signals.ToList(), "selectorEngine.success=true");
    }

    private static IReadOnlyList<ElementIdentity> LoadFixtureIdentities(string fixtureFile)
    {
        var path = Path.Combine(RepoRoot, "tools", "fixtures", "trees", "parity", fixtureFile);
        var tree = new FixtureSensor().Load(path);
        Assert.IsNotNull(tree.Root, "fixture root should exist");

        var identities = new List<ElementIdentity>();
        Flatten(tree.Root!, tree.Provenance, tree.Root!.Name, [], "", 0, identities);
        return identities;
    }

    private static void Flatten(
        FixtureControlNode node,
        Provenance provenance,
        string windowTitle,
        IReadOnlyList<string> ancestorSegments,
        string parentFingerprint,
        int siblingIndex,
        ICollection<ElementIdentity> identities)
    {
        var ancestorPath = string.Join(" > ", ancestorSegments);
        var identity = new ElementIdentity(node.RuntimeId, node.Role, node.Name, node.AutomationId)
        {
            Role = node.Role,
            ControlType = node.Role,
            HelpText = node.HelpText,
            LegacyName = node.LegacyName,
            LegacyValue = node.LegacyValue,
            LabeledByName = node.LabeledByName,
            ClassName = node.ClassName,
            WindowTitle = windowTitle,
            AncestorPath = ancestorPath,
            ParentFingerprint = parentFingerprint,
            SiblingIndex = siblingIndex,
            BoundsHint = $"{node.Bounds.X},{node.Bounds.Y},{node.Bounds.Width},{node.Bounds.Height}",
            Provenance = provenance
        };

        identities.Add(identity);
        var fingerprint = ElementFingerprintBuilder.Build(identity);
        var currentSegment = string.IsNullOrWhiteSpace(node.Name) ? node.Role : $"{node.Role}:{node.Name}";
        var nextAncestors = ancestorSegments.Concat([currentSegment]).ToList();

        for (var index = 0; index < node.Children.Count; index++)
        {
            Flatten(node.Children[index], provenance, windowTitle, nextAncestors, fingerprint, index, identities);
        }
    }
}
