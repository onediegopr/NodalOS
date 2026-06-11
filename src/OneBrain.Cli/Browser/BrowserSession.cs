using System.Runtime.InteropServices;
using OneBrain.Observation.Windows;

namespace OneBrain.Cli.Browser;

public sealed class BrowserSession
{
    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const uint WM_CLOSE = 0x0010;

    /// <summary>Resolve browser name to process name. Returns null if unsupported.</summary>
    public static string? ResolveProcessName(string browserName)
    {
        var normalized = browserName.Trim().ToLowerInvariant();
        return normalized switch
        {
            "edge" => "msedge",
            "chrome" => "chrome",
            "firefox" => "firefox",
            _ => null
        };
    }

    /// <summary>Create a new browser session by launching and detecting a new window.</summary>
    public static BrowserOpenResult Open(
        string url,
        string browserName,
        int timeoutMs,
        bool useNewWindow = true,
        string? extraArgs = null,
        CancellationToken? token = null)
    {
        // Snapshot existing HWNDs BEFORE launch (always, even if not useNewWindow)
        var finder = new WindowFinder();
        var processToFind = ResolveProcessName(browserName);
        if (processToFind == null)
            return new BrowserOpenResult { Success = false, Error = $"Unsupported browser: {browserName}. Supported: edge, chrome." };

        var existingHwnds = new HashSet<IntPtr>(finder.FindAllWindows(processToFind, null));

        // Build browser args
        var flags = useNewWindow
            ? (browserName == "firefox" ? "-new-window " : "--new-window ")
            : "";
        if (!string.IsNullOrEmpty(extraArgs))
            flags += extraArgs + " ";
        var urlArg = browserName == "firefox" ? $"\"{url}\"" : $"\"{url}\"";
        var browserArgs = $"{flags}{urlArg}";

        string[] candidates = browserName switch
        {
            "edge" => [@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                       @"C:\Program Files\Microsoft\Edge\Application\msedge.exe"],
            "chrome" => [@"C:\Program Files\Google\Chrome\Application\chrome.exe",
                         @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"],
            "firefox" => [@"C:\Program Files\Mozilla Firefox\firefox.exe",
                          @"C:\Program Files (x86)\Mozilla Firefox\firefox.exe"],
            _ => []
        };

        var launched = false;
        var lastError = "";
        foreach (var exe in candidates)
        {
            if (!File.Exists(exe)) continue;
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe, browserArgs)
                    { UseShellExecute = false });
                launched = true;
                break;
            }
            catch (Exception ex) { lastError = ex.Message; }
        }

        if (!launched)
        {
            var fallbackExe = browserName switch
            {
                "edge" => "msedge",
                "chrome" => "chrome",
                "firefox" => "firefox",
                _ => processToFind
            };
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fallbackExe, browserArgs)
                    { UseShellExecute = false });
                launched = true;
            }
            catch (Exception ex) { lastError = ex.Message; }
        }

        if (!launched)
        {
            var tried = string.Join(", ", candidates);
            return new BrowserOpenResult { Success = false, Error = $"{browserName} not found. Tried: {tried}. PATH fallback: {lastError}" };
        }

        // Poll for a NEW window not in existing set
        var sw = System.Diagnostics.Stopwatch.StartNew();
        IntPtr newHwnd = IntPtr.Zero;

        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (token?.IsCancellationRequested == true) break;

            var allHwnds = finder.FindAllWindows(processToFind, null);
            newHwnd = allHwnds.FirstOrDefault(h => !existingHwnds.Contains(h));
            if (newHwnd != IntPtr.Zero) break;

            System.Threading.Thread.Sleep(250);
        }

        if (newHwnd == IntPtr.Zero)
        {
            // Fallback: check if there are any windows at all (Edge may have recycled)
            var anyHwnd = finder.FindWindow(processToFind, null);
            if (anyHwnd != IntPtr.Zero && existingHwnds.Count == 0)
            {
                // No pre-existing windows, so the one we found is probably ours
                return new BrowserOpenResult
                {
                    Success = true,
                    Session = new SessionInfo
                    {
                        Id = Guid.NewGuid().ToString("N")[..8],
                        ProcessName = processToFind,
                        Hwnd = anyHwnd,
                        Url = url,
                        Owned = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };
            }

            return new BrowserOpenResult
            {
                Success = false,
                Error = $"Timeout waiting for new {browserName} window (process: {processToFind}, timeout: {timeoutMs}ms)"
            };
        }

        // Brief wait for render
        System.Threading.Thread.Sleep(500);

        return new BrowserOpenResult
        {
            Success = true,
            Session = new SessionInfo
            {
                Id = Guid.NewGuid().ToString("N")[..8],
                ProcessName = processToFind,
                Hwnd = newHwnd,
                Url = url,
                Owned = true,
                CreatedAt = DateTime.UtcNow
            }
        };
    }

    /// <summary>Close a browser window owned by this session. Only closes owned windows.</summary>
    public static BrowserCloseResult Close(IntPtr hwnd, int timeoutMs = 5000)
    {
        if (hwnd == IntPtr.Zero)
            return new BrowserCloseResult { Success = true, Message = "No HWND to close (already closed or never opened)." };

        if (!IsWindow(hwnd))
            return new BrowserCloseResult { Success = true, Message = "Browser session already closed." };

        // Send WM_CLOSE to the owned window
        PostMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

        // Wait for the window to actually close
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (!IsWindow(hwnd))
                return new BrowserCloseResult { Success = true, Message = "Browser session closed successfully." };

            System.Threading.Thread.Sleep(200);
        }

        return new BrowserCloseResult
        {
            Success = false,
            Message = $"Browser window did not close within {timeoutMs}ms (may need manual intervention)."
        };
    }

    /// <summary>Check if a HWND is still alive (window exists).</summary>
    public static bool IsHwndAlive(IntPtr hwnd) => hwnd != IntPtr.Zero && IsWindow(hwnd);

    public sealed class SessionInfo
    {
        public string Id { get; init; } = "";
        public string ProcessName { get; init; } = "";
        public IntPtr Hwnd { get; init; }
        public string Url { get; init; } = "";
        public bool Owned { get; init; } = true;
        public DateTime CreatedAt { get; init; }
    }

    public sealed class BrowserOpenResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public SessionInfo? Session { get; init; }
    }

    public sealed class BrowserCloseResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = "";
    }
}
