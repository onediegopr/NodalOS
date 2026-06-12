using FlaUI.UIA3;

namespace OneBrain.Observation.Sessions;

public sealed class PerceptionSession : IDisposable
{
    private UIA3Automation? _automation = new();

    public UIA3Automation Automation
    {
        get
        {
            ThrowIfDisposed();
            return _automation!;
        }
    }

    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        if (IsDisposed)
            return;

        IsDisposed = true;
        _automation?.Dispose();
        _automation = null;
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        if (IsDisposed || _automation is null)
            throw new ObjectDisposedException(nameof(PerceptionSession));
    }
}
