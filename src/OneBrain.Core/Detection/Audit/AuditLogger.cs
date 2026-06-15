namespace OneBrain.Core.Detection.Audit;

/// <summary>Logger de auditoría mínima para eventos de detección.</summary>
public interface IAuditLogger
{
    Task LogAsync(object evt, CancellationToken ct = default);
}

/// <summary>Implementación in-memory para tests y Fase B.</summary>
public class InMemoryAuditLogger : IAuditLogger
{
    private readonly List<object> _events = new();

    public Task LogAsync(object evt, CancellationToken ct = default)
    {
        lock (_events)
            _events.Add(evt);
        return Task.CompletedTask;
    }

    public IReadOnlyList<T> GetEventsOfType<T>() where T : class
    {
        lock (_events)
            return _events.OfType<T>().ToList();
    }

    public IReadOnlyList<object> AllEvents
    {
        get { lock (_events) return _events.ToList(); }
    }

    public void Clear()
    {
        lock (_events) _events.Clear();
    }
}
