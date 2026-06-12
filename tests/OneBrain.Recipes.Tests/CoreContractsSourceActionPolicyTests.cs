using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Contracts;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class CoreContractsSourceActionPolicyTests
{
    [TestMethod]
    public void EveryProvenanceHasExplicitActionCeiling()
    {
        var expected = new Dictionary<Provenance, ActionCeiling>
        {
            [Provenance.Uia] = ActionCeiling.FullActionWithPreflight,
            [Provenance.Fixture] = ActionCeiling.FullActionWithPreflight,
            [Provenance.Api] = ActionCeiling.FullActionWithPreflight,
            [Provenance.Win32] = ActionCeiling.ReadOnly,
            [Provenance.Msaa] = ActionCeiling.ReadOnly,
            [Provenance.Dom] = ActionCeiling.ReadOnly,
            [Provenance.Ocr] = ActionCeiling.ReadOnly,
            [Provenance.Vision] = ActionCeiling.ReadOnly,
            [Provenance.Inferred] = ActionCeiling.ReadOnly
        };

        foreach (var provenance in Enum.GetValues<Provenance>())
        {
            Assert.IsTrue(expected.ContainsKey(provenance), $"Missing expected mapping for {provenance}");
            Assert.AreEqual(expected[provenance], SourceActionPolicy.Resolve(provenance), $"Unexpected action ceiling for {provenance}");
        }
    }

    [TestMethod]
    public void UnknownProvenanceFailsClosedToReadOnly()
    {
        Assert.AreEqual(ActionCeiling.ReadOnly, SourceActionPolicy.Resolve((Provenance)999));
    }

    [TestMethod]
    public void OcrVisionAndInferredNeverAllowAction()
    {
        var provenances = new[] { Provenance.Ocr, Provenance.Vision, Provenance.Inferred };

        foreach (var provenance in provenances)
        {
            Assert.AreEqual(ActionCeiling.ReadOnly, SourceActionPolicy.Resolve(provenance), $"{provenance} must stay below benign action");
        }
    }

    [TestMethod]
    public void ActionCeilingOrderingIsStable()
    {
        var actual = Enum.GetValues<ActionCeiling>().Select(value => (int)value).ToArray();
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5 }, actual);
    }

    [TestMethod]
    public void ContractsSerializeAsStableNumericValues()
    {
        Assert.AreEqual("1", JsonSerializer.Serialize(ActionCeiling.ReadOnly));
        Assert.AreEqual("5", JsonSerializer.Serialize(ActionCeiling.Human));
        Assert.AreEqual(ActionCeiling.Diagnostic, JsonSerializer.Deserialize<ActionCeiling>("2"));
        Assert.AreEqual(Provenance.Fixture, JsonSerializer.Deserialize<Provenance>(((int)Provenance.Fixture).ToString()));
    }
}
