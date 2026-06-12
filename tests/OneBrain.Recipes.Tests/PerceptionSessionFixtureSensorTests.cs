using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Contracts;
using OneBrain.Observation.Sensors;
using OneBrain.Observation.Sessions;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class PerceptionSessionFixtureSensorTests
{
    [TestMethod]
    public void PerceptionSession_DoubleDispose_DoesNotThrow()
    {
        var session = new PerceptionSession();
        session.Dispose();
        session.Dispose();

        Assert.IsTrue(session.IsDisposed);
    }

    [TestMethod]
    public void PerceptionSession_UseAfterDispose_ThrowsObjectDisposedException()
    {
        var session = new PerceptionSession();
        session.Dispose();

        try
        {
            _ = session.Automation;
            Assert.Fail("Expected ObjectDisposedException");
        }
        catch (ObjectDisposedException)
        {
        }
    }

    [TestMethod]
    public void PerceptionSession_CreateAndDispose_DoesNotThrow()
    {
        using var session = new PerceptionSession();

        Assert.IsNotNull(session.Automation);
        Assert.IsFalse(session.IsDisposed);
    }

    [TestMethod]
    public void FixtureSensor_Loads_Notepad_Sample()
    {
        var sensor = new FixtureSensor();
        var tree = sensor.Load(GetRootPath("tools/fixtures/trees/notepad-sample.json"));

        Assert.AreEqual(FixtureSensor.CurrentSchemaVersion, tree.SchemaVersion);
        Assert.IsNotNull(tree.Root);
        Assert.AreEqual("Window", tree.Root.Role);
        Assert.AreEqual("Bloc de notas", tree.Root.Name);
    }

    [TestMethod]
    public void FixtureSensor_InvalidSchemaVersion_FailsClearly()
    {
        var sensor = new FixtureSensor();
        const string json = """
        {
          "schemaVersion": 2,
          "root": {
            "role": "Window",
            "name": "Invalid",
            "children": []
          }
        }
        """;

        InvalidDataException ex;
        try
        {
            sensor.LoadJson(json);
            Assert.Fail("Expected InvalidDataException");
            return;
        }
        catch (InvalidDataException caught)
        {
            ex = caught;
        }
        StringAssert.Contains(ex.Message, "schemaVersion");
    }

    [TestMethod]
    public void FixtureSensor_NullRoot_FailsClearly()
    {
        var sensor = new FixtureSensor();
        const string json = """
        {
          "schemaVersion": 1,
          "root": null
        }
        """;

        InvalidDataException ex;
        try
        {
            sensor.LoadJson(json);
            Assert.Fail("Expected InvalidDataException");
            return;
        }
        catch (InvalidDataException caught)
        {
            ex = caught;
        }
        StringAssert.Contains(ex.Message, "root");
    }

    [TestMethod]
    public void FixtureSensor_Provenance_IsFixture()
    {
        var sensor = new FixtureSensor();
        var tree = sensor.Load(GetRootPath("tools/fixtures/trees/notepad-sample.json"));

        Assert.AreEqual(Provenance.Fixture, sensor.Provenance);
        Assert.AreEqual(Provenance.Fixture, tree.Provenance);
    }

    [TestMethod]
    public void FixtureSensor_Loads_Large_Tree_Within_Conservative_Threshold()
    {
        var sensor = new FixtureSensor();
        var tempPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempPath, BuildSyntheticFixtureJson(1000));

            var stopwatch = Stopwatch.StartNew();
            var tree = sensor.Load(tempPath);
            stopwatch.Stop();

            Assert.IsNotNull(tree.Root);
            Assert.AreEqual(1000, CountNodes(tree.Root));
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, $"Fixture load took too long: {stopwatch.ElapsedMilliseconds}ms");
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [TestMethod]
    public void FixtureSensor_Roundtrip_Preserves_Basic_Shape()
    {
        var sensor = new FixtureSensor();
        var original = sensor.Load(GetRootPath("tools/fixtures/trees/notepad-sample.json"));
        var json = sensor.ToJson(original);
        var roundtrip = sensor.LoadJson(json);

        Assert.AreEqual(original.Root!.Name, roundtrip.Root!.Name);
        Assert.AreEqual(original.Root.Children.Count, roundtrip.Root.Children.Count);
    }

    private static int CountNodes(FixtureControlNode node)
    {
        return 1 + node.Children.Sum(CountNodes);
    }

    private static string BuildSyntheticFixtureJson(int nodeCount)
    {
        var root = BuildSyntheticNode(0, nodeCount);
        var tree = new FixtureControlTree
        {
            SchemaVersion = FixtureSensor.CurrentSchemaVersion,
            Root = root
        };
        return new FixtureSensor().ToJson(tree);
    }

    private static FixtureControlNode BuildSyntheticNode(int index, int remaining)
    {
        if (remaining <= 1)
        {
            return new FixtureControlNode
            {
                Role = "Text",
                Name = $"Node {index}",
                RuntimeId = $"fixture-{index}",
                Children = []
            };
        }

        var childCount = Math.Min(3, remaining - 1);
        var remainingForChildren = remaining - 1;
        var children = new List<FixtureControlNode>(childCount);
        var nextIndex = index + 1;

        for (var i = 0; i < childCount; i++)
        {
            var slice = i == childCount - 1 ? remainingForChildren : Math.Max(1, remainingForChildren / (childCount - i));
            children.Add(BuildSyntheticNode(nextIndex, slice));
            nextIndex += slice;
            remainingForChildren -= slice;
        }

        return new FixtureControlNode
        {
            Role = index == 0 ? "Window" : "Pane",
            Name = $"Node {index}",
            RuntimeId = $"fixture-{index}",
            Children = children
        };
    }

    private static string GetRootPath(string relative)
    {
        var baseDir = AppContext.BaseDirectory;
        var root = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return Path.Combine(root, relative);
    }
}
