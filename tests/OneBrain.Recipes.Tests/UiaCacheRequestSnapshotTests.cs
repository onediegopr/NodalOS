using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Observation.Uia;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class UiaCacheRequestSnapshotTests
{
    [TestMethod]
    public void Default_Snapshot_Options_Enable_CacheRequest()
    {
        Assert.IsTrue(UiaSnapshotOptions.Default.UseCacheRequest);
        Assert.IsTrue(UiaSnapshotOptions.Default.CacheLegacyProperties);
        Assert.IsTrue(UiaSnapshotOptions.Default.CacheValueProperties);
        Assert.IsTrue(UiaSnapshotOptions.Default.CachePatternAvailability);
    }

    [TestMethod]
    public void Snapshot_Property_Set_Includes_Properties_Used_By_Reader()
    {
        CollectionAssert.Contains(UiaSnapshotPropertySet.DefaultPropertyNames.ToList(), "Name");
        CollectionAssert.Contains(UiaSnapshotPropertySet.DefaultPropertyNames.ToList(), "AutomationId");
        CollectionAssert.Contains(UiaSnapshotPropertySet.DefaultPropertyNames.ToList(), "ControlType");
        CollectionAssert.Contains(UiaSnapshotPropertySet.DefaultPropertyNames.ToList(), "ClassName");
        CollectionAssert.Contains(UiaSnapshotPropertySet.DefaultPropertyNames.ToList(), "BoundingRectangle");
        CollectionAssert.Contains(UiaSnapshotPropertySet.DefaultPropertyNames.ToList(), "IsEnabled");
        CollectionAssert.Contains(UiaSnapshotPropertySet.DefaultPropertyNames.ToList(), "IsOffscreen");
        CollectionAssert.Contains(UiaSnapshotPropertySet.DefaultPropertyNames.ToList(), "HasKeyboardFocus");
        CollectionAssert.Contains(UiaSnapshotPropertySet.DefaultPropertyNames.ToList(), "IsKeyboardFocusable");
        CollectionAssert.Contains(UiaSnapshotPropertySet.DefaultPropertyNames.ToList(), "HelpText");
    }

    [TestMethod]
    public void Snapshot_Property_Set_Includes_Legacy_Value_And_Pattern_Availability()
    {
        CollectionAssert.Contains(UiaSnapshotPropertySet.LegacyPropertyNames.ToList(), "LegacyIAccessible.Name");
        CollectionAssert.Contains(UiaSnapshotPropertySet.LegacyPropertyNames.ToList(), "LegacyIAccessible.Value");
        CollectionAssert.Contains(UiaSnapshotPropertySet.ValuePropertyNames.ToList(), "Value.Value");
        CollectionAssert.Contains(UiaSnapshotPropertySet.PatternAvailabilityNames.ToList(), "IsInvokePatternAvailable");
        CollectionAssert.Contains(UiaSnapshotPropertySet.PatternAvailabilityNames.ToList(), "IsValuePatternAvailable");
        CollectionAssert.Contains(UiaSnapshotPropertySet.PatternAvailabilityNames.ToList(), "IsLegacyIAccessiblePatternAvailable");
    }

    [TestMethod]
    public void Hito_Documentation_Explains_CacheRequest_And_Safety_Boundary()
    {
        var root = FindRepoRoot();
        var doc = File.ReadAllText(Path.Combine(root, "docs", "hitos", "hito-115-uia-cacherequest-snapshot-v1.md"));

        StringAssert.Contains(doc, "CacheRequest");
        StringAssert.Contains(doc, "UiaSnapshotOptions");
        StringAssert.Contains(doc, "UiaSnapshotCacheRequestFactory");
        StringAssert.Contains(doc, "no habilita clicks");
        StringAssert.Contains(doc, "no cambia playback");
        StringAssert.Contains(doc, "no llama OpenAI");
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "OneBrain.slnx")))
            dir = Directory.GetParent(dir)?.FullName;

        Assert.IsNotNull(dir);
        return dir;
    }
}
