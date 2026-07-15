namespace OneBrain.Core.Perception;

public sealed record SemanticPropertyChange(
    string SubjectRef,
    string Property,
    string BeforeValueRedacted,
    string AfterValueRedacted);

public sealed record CognitiveSnapshotV2Diff(
    string BeforeFingerprint,
    string AfterFingerprint,
    IReadOnlyList<string> AddedElementRefs,
    IReadOnlyList<string> RemovedElementRefs,
    IReadOnlyList<SemanticPropertyChange> ChangedProperties,
    bool ProcessChanged,
    bool WindowChanged,
    bool ForegroundChanged,
    bool BlockingConflictIntroduced,
    IReadOnlyList<CrossChannelConflict> NewConflicts,
    IReadOnlyList<string> EvidenceRefs)
{
    public bool HasSemanticChange =>
        !string.Equals(BeforeFingerprint, AfterFingerprint, StringComparison.Ordinal) ||
        AddedElementRefs.Count > 0 ||
        RemovedElementRefs.Count > 0 ||
        ChangedProperties.Count > 0 ||
        ProcessChanged ||
        WindowChanged ||
        ForegroundChanged ||
        BlockingConflictIntroduced;
}

public static class CognitiveSnapshotV2Differ
{
    public static CognitiveSnapshotV2Diff Diff(CognitiveSnapshotV2 before, CognitiveSnapshotV2 after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        var beforeElements = before.Elements
            .GroupBy(element => element.SemanticRef, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var afterElements = after.Elements
            .GroupBy(element => element.SemanticRef, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var added = afterElements.Keys
            .Except(beforeElements.Keys, StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var removed = beforeElements.Keys
            .Except(afterElements.Keys, StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        var changed = new List<SemanticPropertyChange>();
        foreach (var semanticRef in beforeElements.Keys.Intersect(afterElements.Keys, StringComparer.Ordinal)
                     .OrderBy(value => value, StringComparer.Ordinal))
        {
            var beforeProperties = beforeElements[semanticRef].CanonicalProperties;
            var afterProperties = afterElements[semanticRef].CanonicalProperties;
            foreach (var property in beforeProperties.Keys.Union(afterProperties.Keys, StringComparer.Ordinal)
                         .OrderBy(value => value, StringComparer.Ordinal))
            {
                beforeProperties.TryGetValue(property, out var beforeValue);
                afterProperties.TryGetValue(property, out var afterValue);
                beforeValue ??= string.Empty;
                afterValue ??= string.Empty;
                if (!string.Equals(beforeValue, afterValue, StringComparison.Ordinal))
                {
                    changed.Add(new SemanticPropertyChange(
                        semanticRef,
                        property,
                        beforeValue,
                        afterValue));
                }
            }
        }

        var beforeConflictKeys = before.Conflicts
            .Select(ConflictKey)
            .ToHashSet(StringComparer.Ordinal);
        var newConflicts = after.Conflicts
            .Where(conflict => !beforeConflictKeys.Contains(ConflictKey(conflict)))
            .OrderBy(conflict => conflict.SubjectRef, StringComparer.Ordinal)
            .ThenBy(conflict => conflict.Property, StringComparer.Ordinal)
            .ToArray();

        var evidenceRefs = before.EvidenceRefs
            .Concat(after.EvidenceRefs)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .Take(512)
            .ToArray();

        return new CognitiveSnapshotV2Diff(
            BeforeFingerprint: before.StateFingerprint,
            AfterFingerprint: after.StateFingerprint,
            AddedElementRefs: added,
            RemovedElementRefs: removed,
            ChangedProperties: changed,
            ProcessChanged: before.Application.ProcessId != after.Application.ProcessId ||
                            !string.Equals(
                                before.Application.ProcessNameRedacted,
                                after.Application.ProcessNameRedacted,
                                StringComparison.OrdinalIgnoreCase),
            WindowChanged: !string.Equals(
                               before.Application.WindowTitleRedacted,
                               after.Application.WindowTitleRedacted,
                               StringComparison.Ordinal) ||
                           before.WindowBounds != after.WindowBounds,
            ForegroundChanged: before.IsForeground != after.IsForeground,
            BlockingConflictIntroduced: newConflicts.Any(conflict =>
                conflict.Severity == PerceptionConflictSeverity.Blocking),
            NewConflicts: newConflicts,
            EvidenceRefs: evidenceRefs);
    }

    private static string ConflictKey(CrossChannelConflict conflict) =>
        string.Join(
            '|',
            conflict.SubjectRef,
            conflict.Property,
            conflict.Severity,
            string.Join('~', conflict.ConflictingValuesRedacted.OrderBy(value => value, StringComparer.Ordinal)));
}
