using OneBrain.Core.Runtime;

namespace OneBrain.AgentOperations.Core.Runtime;

/// <summary>
/// Transient current-run event buffer. It projects into the existing evidence/timeline
/// boundary and is deliberately not a second durable ledger.
/// </summary>
public sealed class MissionEventStream
{
    private static readonly HashSet<MissionEventKind> TerminalKinds =
    [
        MissionEventKind.RunCompleted,
        MissionEventKind.RunFailed,
        MissionEventKind.RunCancelled,
        MissionEventKind.RunTimeout
    ];

    private readonly object _gate = new();
    private readonly List<MissionEventEnvelope> _events = [];
    private readonly IRuntimeSignalObserver _observer;
    private long _sequence;
    private bool _terminal;

    public MissionEventStream(string runId, string missionId, IRuntimeSignalObserver? observer = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(missionId);
        RunId = runId.Trim();
        MissionId = missionId.Trim();
        _observer = observer ?? NullRuntimeSignalObserver.Instance;
    }

    public string RunId { get; }

    public string MissionId { get; }

    public MissionEventEnvelope Append(
        MissionEventKind kind,
        string actor,
        string correlationId,
        string summary,
        string? stepId = null,
        string? causationId = null,
        IEnumerable<string>? evidenceRefs = null,
        MissionEventSeverity severity = MissionEventSeverity.Info)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        MissionEventEnvelope envelope;
        lock (_gate)
        {
            if (_terminal)
                throw new InvalidOperationException("The mission event stream is already terminal.");

            envelope = new MissionEventEnvelope(
                RunId,
                ++_sequence,
                DateTimeOffset.UtcNow,
                kind,
                actor,
                MissionId,
                stepId,
                correlationId,
                causationId,
                summary,
                (evidenceRefs ?? Array.Empty<string>()).ToArray(),
                severity).Sanitize();
            _events.Add(envelope);
            if (TerminalKinds.Contains(kind))
                _terminal = true;
        }

        _observer.TryObserve(RuntimeSignal.Create(
            "mission",
            ToSignalName(kind),
            envelope.CorrelationId,
            envelope.MissionId,
            envelope.StepId,
            dimensions:
            [
                new KeyValuePair<string, string?>("run_id", envelope.RunId),
                new KeyValuePair<string, string?>("sequence", envelope.Sequence.ToString()),
                new KeyValuePair<string, string?>("severity", envelope.Severity.ToString())
            ]));
        return envelope;
    }

    public IReadOnlyList<MissionEventEnvelope> Snapshot()
    {
        lock (_gate)
            return _events.ToArray();
    }

    private static string ToSignalName(MissionEventKind kind)
    {
        var value = kind.ToString();
        var result = new System.Text.StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (index > 0 && char.IsUpper(current))
                result.Append('_');
            result.Append(char.ToLowerInvariant(current));
        }
        return result.ToString();
    }
}
