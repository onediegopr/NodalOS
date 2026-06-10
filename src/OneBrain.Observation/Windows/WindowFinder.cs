using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

namespace OneBrain.Observation.Windows;

public sealed class WindowFinder
{
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public IntPtr FindWindow(string? processName, string? titlePart)
    {
        IntPtr foundHwnd = IntPtr.Zero;

        EnumWindows((hWnd, lParam) =>
        {
            if (!IsWindowVisible(hWnd)) return true;

            if (!string.IsNullOrEmpty(processName))
            {
                GetWindowThreadProcessId(hWnd, out uint pid);

                try
                {
                    using var proc = Process.GetProcessById((int)pid);

                    if (!proc.ProcessName.Contains(processName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch
                {
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(titlePart))
            {
                var sb = new StringBuilder(256);
                GetWindowText(hWnd, sb, sb.Capacity);

                if (!sb.ToString().Contains(titlePart, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            foundHwnd = hWnd;
            return false;
        }, IntPtr.Zero);

        return foundHwnd;
    }

    public void Activate(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return;

        ShowWindow(hWnd, 9);
        SetForegroundWindow(hWnd);
        System.Threading.Thread.Sleep(500);
    }
}
