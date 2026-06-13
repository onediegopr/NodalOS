namespace OneBrain.Cli.Safety;

public sealed record DesktopTargetObservationResult
{
    public bool Found { get; init; }
    public int CandidateCount { get; init; }
    public string Reason { get; init; } = "";
    public string? SelectedName { get; init; }
    public string? SelectedControlType { get; init; }
    public string? SelectedBoundingRect { get; init; }
    public string? SelectedRuntimeId { get; init; }
    public string? SelectedAutomationId { get; init; }
    public string? SelectedClassName { get; init; }
    public string? SelectedFrameworkId { get; init; }
    public string? SelectedAncestorPath { get; init; }
    public string? SelectedProcessName { get; init; }
    public string? SelectedWindowTitle { get; init; }
    public string? SelectedHelpText { get; init; }
    public string? SelectedLegacyName { get; init; }
    public string? RootHwnd { get; init; }
    public bool SelectedHelpTextPresent { get; init; }
    public bool SelectedLegacyNamePresent { get; init; }
    public bool HasInvoke { get; init; }
}
