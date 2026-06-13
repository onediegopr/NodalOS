using System.Runtime.InteropServices;
using OneBrain.Core.Execution;
using OneBrain.Observation.Windows;

namespace OneBrain.Observation.Input;

public sealed class DesktopOwnershipMonitor : IDesktopOwnershipMonitor
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetLastInputInfo(ref LastInputInfo plii);

    private readonly ForegroundWindowReader _foregroundWindowReader = new();

    public OwnershipSnapshot Capture()
    {
        var info = new LastInputInfo
        {
            cbSize = (uint)Marshal.SizeOf<LastInputInfo>()
        };

        ulong lastInput = 0;
        if (GetLastInputInfo(ref info))
            lastInput = info.dwTime;

        var hWnd = ForegroundWindowReader.GetForegroundWindow();
        var snapshot = _foregroundWindowReader.ReadFromHandle(hWnd);
        return new OwnershipSnapshot(
            LastInputTick: lastInput,
            ForegroundHandle: hWnd.ToInt64(),
            ForegroundTitle: snapshot?.Title ?? "",
            CapturedAtUtc: DateTimeOffset.UtcNow);
    }

    public bool HumanInputSince(OwnershipSnapshot baseline)
    {
        var current = Capture();
        return current.LastInputTick > baseline.LastInputTick;
    }

    public bool ForegroundChanged(OwnershipSnapshot baseline)
    {
        var current = Capture();
        return current.ForegroundHandle != baseline.ForegroundHandle;
    }
}
