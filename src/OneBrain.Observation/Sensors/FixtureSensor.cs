using System.Text.Json;
using OneBrain.Core.Contracts;

namespace OneBrain.Observation.Sensors;

public sealed class FixtureSensor
{
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public Provenance Provenance => Provenance.Fixture;

    public FixtureControlTree Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("fixture tree not found", path);

        return LoadJson(File.ReadAllText(path));
    }

    public FixtureControlTree LoadJson(string json)
    {
        var tree = JsonSerializer.Deserialize<FixtureControlTree>(json, JsonOptions);
        if (tree is null)
            throw new InvalidDataException("fixture tree could not be deserialized");

        Validate(tree);
        return tree with { Provenance = Provenance.Fixture };
    }

    public string ToJson(FixtureControlTree tree)
    {
        Validate(tree);
        return JsonSerializer.Serialize(tree with { Provenance = Provenance.Fixture }, JsonOptions);
    }

    private static void Validate(FixtureControlTree tree)
    {
        if (tree.SchemaVersion != CurrentSchemaVersion)
            throw new InvalidDataException($"fixture tree schemaVersion must be {CurrentSchemaVersion}");

        if (tree.Root is null)
            throw new InvalidDataException("fixture tree root cannot be null");

        ValidateNode(tree.Root);
    }

    private static void ValidateNode(FixtureControlNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node.Children is null)
            throw new InvalidDataException("fixture node children cannot be null");

        if (node.Patterns is null)
            throw new InvalidDataException("fixture node patterns cannot be null");

        foreach (var child in node.Children)
            ValidateNode(child);
    }
}
