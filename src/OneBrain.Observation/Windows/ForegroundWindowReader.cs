using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using OneBrain.Core.Models;

namespace OneBrain.Observation.Windows;

public sealed class ForegroundWindowReader
{
    public WindowSnapshot? Read()
    {
        var hwnd = NativeMethods.GetForegroundWindow();

        if (hwnd == IntPtr.Zero)
        {
            return null;
        }

        var title = GetWindowTitle(hwnd);

        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);

        var processName = "unknown";

        try
        {
            using var process = Process.GetProcessById((int)processId);
            processName = process.ProcessName;
        }
        catch
        {
            // Process may have exited or may not be accessible.
        }

        if (!NativeMethods.GetWindowRect(hwnd, out var rect))
        {
            rect = default;
        }

        return new WindowSnapshot(
            Title: title,
            ProcessName: processName,
            ProcessId: (int)processId,
            Bounds: new WindowBounds(rect.Left, rect.Top, rect.Right, rect.Bottom),
            IsForeground: true);
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        var length = NativeMethods.GetWindowTextLength(hwnd);

        if (length <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length + 1);
        _ = NativeMethods.GetWindowText(hwnd, builder, builder.Capacity);

        return builder.ToString();
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Rect
    {
        public readonly int Left;
        public readonly int Top;
        public readonly int Right;
        public readonly int Bottom;
    }
}
