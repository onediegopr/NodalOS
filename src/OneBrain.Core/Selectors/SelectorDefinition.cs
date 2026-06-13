using OneBrain.Core.Contracts;
using OneBrain.Core.Models;

namespace OneBrain.Core.Selectors;

public sealed record SelectorDefinition
{
    public int SchemaVersion { get; init; } = 1;
    public string? SelectorId { get; init; }
    public Provenance Provenance { get; init; } = Provenance.Inferred;
    public string? Role { get; init; }
    public string? Name { get; init; }
    public string? AutomationId { get; init; }
    public string? HelpText { get; init; }
    public string? LegacyName { get; init; }
    public string? ClassName { get; init; }
    public string? AncestorPath { get; init; }
    public string? AppProfileAlias { get; init; }
    public double StabilityScore { get; init; }
    public double SpecificityScore { get; init; }
    public double SafetyScore { get; init; }
    public ElementIdentity? ExpectedIdentity { get; init; }
}
