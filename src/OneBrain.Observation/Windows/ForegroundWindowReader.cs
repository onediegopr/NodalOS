using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using OneBrain.Core.Models;

namespace OneBrain.Observation.Windows;

public sealed class ForegroundWindowReader
{
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", SetLastError = true)] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public WindowSnapshot? Read() => ReadFromHandle(GetForegroundWindow());

    public WindowSnapshot? ReadFromHandle(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return null;

        var sb = new StringBuilder(256);
        GetWindowText(hWnd, sb, sb.Capacity);

        GetWindowThreadProcessId(hWnd, out uint pid);
        GetWindowRect(hWnd, out Rect r);

        string procName = "Unknown";

        try
        {
            procName = Process.GetProcessById((int)pid).ProcessName;
        }
        catch
        {
        }

        return new WindowSnapshot(
            Title: sb.ToString(),
            ProcessName: procName,
            ProcessId: (int)pid,
            Bounds: new WindowBounds(r.Left, r.Top, r.Right, r.Bottom),
            IsForeground: hWnd == GetForegroundWindow());
    }
}

