using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;
using OneBrain.Core.Contracts;
using OneBrain.Core.Execution;
using OneBrain.Core.Models;
using OneBrain.Core.Safety;
using OneBrain.Core.Selectors;
using OneBrain.Core.Selectors.Web;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class WebIdentityStrengtheningTests
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [TestMethod]
    public void WebCandidateWithRuntimeIdMapsToStrongIdentity()
    {
        var candidate = LoadFixture("runtime-id-strong.json").Candidates.Single();

        var identity = WebCandidateMapper.ToElementIdentity(candidate);

        Assert.IsTrue(identity.IsStrong);
        Assert.AreEqual(Provenance.Uia, identity.Provenance);
        Assert.AreEqual("42.1.9", identity.RuntimeId);
    }

    [TestMethod]
    public void WebCandidateWithoutRuntimeIdMapsToWeakIdentity()
    {
        var candidate = LoadFixture("weak-identity-no-runtimeid.json").Candidates.Single();

        var identity = WebCandidateMapper.ToElementIdentity(candidate);

        Assert.IsFalse(identity.IsStrong);
        Assert.AreEqual(Provenance.Inferred, identity.Provenance);
        Assert.AreEqual("Pricing", identity.Name);
    }

    [TestMethod]
    public void WebCandidateMapperMapsClassFrameworkHelpLegacyAncestor()
    {
        var candidate = LoadFixture("runtime-id-strong.json").Candidates.Single();

        var identity = WebCandidateMapper.ToElementIdentity(candidate);

        Assert.AreEqual("Chrome_RenderWidgetHostHWND", identity.ClassName);
        Assert.AreEqual("UIA", identity.FrameworkId);
        Assert.AreEqual("Abrir categorias", identity.HelpText);
        Assert.AreEqual("Categorias", identity.LegacyName);
        Assert.AreEqual("Window:ONE Brain > Pane:Catalog > Document:Main", identity.AncestorPath);
        Assert.AreEqual("msedge", identity.ProcessName);
        Assert.AreEqual("ONE Brain", identity.WindowTitle);
    }

    [TestMethod]
    public void SafeClickPlannerProjectsBoundWithStrongMatchingRuntimeId()
    {
        var candidate = LoadFixture("runtime-id-strong.json").Candidates.Single();
        var manifest = ApprovalManifestBuilder.Build(
            ClickPreflightEvaluator.Evaluate("Categorias"),
            "controlled",
            new ApprovedIdentityInput(
                new ElementIdentity
                {
                    RuntimeId = "42.1.9",
                    Name = "Categorias",
                    ControlType = "Button",
                    AutomationId = "categories-button",
                    ClassName = "Chrome_RenderWidgetHostHWND",
                    FrameworkId = "UIA",
                    AncestorPath = "Window:ONE Brain > Pane:Catalog > Document:Main",
                    Provenance = Provenance.Uia
                },
                "web-shadow",
                new WebSelectorParity
                {
                    EngineFound = true,
                    EngineVerdict = "Same",
                    EngineSelectedName = "Categorias",
                    AgreesWithLegacy = true
                }));

        var plan = SafeClickPlanner.Plan(new SafeClickPlanInput
        {
            Mode = manifest.Mode,
            TargetText = manifest.TargetText,
            Manifest = manifest,
            Candidates = [candidate]
        });

        Assert.AreEqual(StepState.Bound, plan.ProjectedState);
        Assert.AreEqual(IdentityStrength.Strong, plan.IdentityStrength);
        Assert.IsFalse(plan.WouldDispatch);
    }

    [TestMethod]
    public void SafeClickPlannerBlocksRecreatedRuntimeIdForIrreversible()
    {
        var candidate = LoadFixture("recreated-runtime-id.json").Candidates.Single();
        var manifest = ApprovalManifestBuilder.Build(
            ClickPreflightEvaluator.Evaluate("Categorias"),
            "controlled",
            new ApprovedIdentityInput(
                new ElementIdentity
                {
                    RuntimeId = "42.1.9",
                    Name = "Categorias",
                    ControlType = "Button",
                    AutomationId = "categories-button",
                    ClassName = "Chrome_RenderWidgetHostHWND",
                    FrameworkId = "UIA",
                    AncestorPath = "Window:ONE Brain > Pane:Catalog > Document:Main",
                    Provenance = Provenance.Uia
                },
                "web-shadow",
                null));

        var plan = SafeClickPlanner.Plan(new SafeClickPlanInput
        {
            Mode = manifest.Mode,
            TargetText = manifest.TargetText,
            Manifest = manifest,
            Candidates = [candidate]
        });

        Assert.AreEqual(StepState.Blocked, plan.ProjectedState);
        Assert.AreEqual(FailureKind.Stale, plan.FailureKind);
        Assert.AreEqual("ApprovalInvalidated", plan.BlockReason);
    }

    [TestMethod]
    public void AncestorPathDisambiguatesDuplicateNames()
    {
        var fixture = LoadFixture("ancestor-disambiguation.json");
        var selector = new SelectorDefinition
        {
            Provenance = Provenance.Inferred,
            Name = "Continue",
            Role = "Button",
            AncestorPath = "Window:ONE Brain > Pane:Primary > Document:Main"
        };

        var parity = WebSelectorBridge.Evaluate(selector, fixture.Candidates, legacySelectedName: null);

        Assert.IsTrue(parity.EngineFound);
        Assert.AreEqual("Continue", parity.EngineSelectedName);
        Assert.AreEqual("LikelySame", parity.EngineVerdict);
        Assert.IsFalse(parity.Ambiguous);
    }

    [TestMethod]
    public void LegacyValueSiblingIndexParentFingerprintRemainAbsent()
    {
        var identity = WebCandidateMapper.ToElementIdentity(LoadFixture("runtime-id-strong.json").Candidates.Single());

        Assert.AreEqual("", identity.LegacyValue);
        Assert.IsNull(identity.SiblingIndex);
        Assert.AreEqual("", identity.ParentFingerprint);
    }

    [TestMethod]
    public void RuntimeIdStrongDoesNotChangeSelectorEngineBehaviorUnlessUsedByMatcher()
    {
        var candidate = LoadFixture("runtime-id-strong.json").Candidates.Single();
        var parity = WebSelectorBridge.Evaluate([candidate], "Categorias", "Categorias");

        Assert.IsTrue(parity.EngineFound);
        Assert.AreEqual("LikelySame", parity.EngineVerdict);
        Assert.IsTrue(parity.AgreesWithLegacy);
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
