using OneBrain.Core.Contracts;

namespace OneBrain.Observation.Sensors;

public sealed record FixtureBounds(
    int X,
    int Y,
    int Width,
    int Height);

public sealed record FixtureControlNode
{
    public string Role { get; init; } = "";
    public string Name { get; init; } = "";
    public string HelpText { get; init; } = "";
    public string LegacyName { get; init; } = "";
    public string LegacyValue { get; init; } = "";
    public string LabeledByName { get; init; } = "";
    public string AutomationId { get; init; } = "";
    public string ClassName { get; init; } = "";
    public string RuntimeId { get; init; } = "";
    public FixtureBounds Bounds { get; init; } = new(0, 0, 0, 0);
    public bool Enabled { get; init; } = true;
    public bool Offscreen { get; init; }
    public IReadOnlyList<string> Patterns { get; init; } = [];
    public IReadOnlyList<FixtureControlNode> Children { get; init; } = [];
}

public sealed record FixtureControlTree
{
    public int SchemaVersion { get; init; }
    public FixtureControlNode? Root { get; init; }
    public Provenance Provenance { get; init; } = Provenance.Fixture;
}
