using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.EventHandlers;
using FlaUI.UIA3;
using OneBrain.Observation.Windows;

namespace OneBrain.Observation.Uia;

public sealed record UiaWaitResult(
    bool Success,
    bool UsedEvents,
    bool FellBackToPolling,
    int Attempts,
    long ElapsedMs,
    string LastWindowTitle,
    object? Match,
    string Message);

internal sealed record UiaWaitCheck(bool Found, string LastWindowTitle, object? Match);

public static class UiaEventWaiter
{
    private const int SafetyRepollMs = 2000;

    public static UiaWaitResult WaitForTitleContains(
        string? processName,
        string? windowTitle,
        string titleContains,
        int timeoutMs,
        int intervalMs,
        CancellationToken cancellationToken = default,
        bool forcePolling = false)
    {
        var finder = new WindowFinder();
        var windowReader = new ForegroundWindowReader();

        UiaWaitCheck Check()
        {
            var handles = finder.FindAllWindows(processName, windowTitle);
            foreach (var hwnd in handles)
            {
                var window = windowReader.ReadFromHandle(hwnd);
                var title = window?.Title ?? "";
                if (title.Contains(titleContains, StringComparison.OrdinalIgnoreCase))
                    return new UiaWaitCheck(true, title, new { Title = title, UsedEvents = !forcePolling });
            }

            var lastTitle = handles.Count > 0
                ? windowReader.ReadFromHandle(handles[0])?.Title ?? ""
                : "";
            return new UiaWaitCheck(false, lastTitle, null);
        }

        IDisposable Subscribe(Action signal)
        {
            var automation = new UIA3Automation();
            var hwnd = finder.FindWindow(processName, windowTitle);

            if (hwnd != IntPtr.Zero)
            {
                var root = automation.FromHandle(hwnd);
                var handler = root.RegisterPropertyChangedEvent(
                    TreeScope.Element,
                    (_, _, _) => signal(),
                    automation.PropertyLibrary.Element.Name);

                return new DelegateDisposable(() =>
                {
                    try { root.FrameworkAutomationElement.UnregisterPropertyChangedEventHandler(handler); } catch { }
                    automation.Dispose();
                });
            }

            var desktop = automation.GetDesktop();
            var openedHandler = desktop.RegisterAutomationEvent(
                automation.EventLibrary.Window.WindowOpenedEvent,
                TreeScope.Children,
                (_, _) => signal());

            return new DelegateDisposable(() =>
            {
                try { desktop.FrameworkAutomationElement.UnregisterAutomationEventHandler(openedHandler); } catch { }
                automation.Dispose();
            });
        }

        return WaitCore(Check, Subscribe, timeoutMs, intervalMs, cancellationToken, forcePolling);
    }

    public static UiaWaitResult WaitForWindowOpen(
        string? processName,
        string? windowTitle,
        int timeoutMs,
        int intervalMs,
        CancellationToken cancellationToken = default,
        bool forcePolling = false)
    {
        var finder = new WindowFinder();
        var windowReader = new ForegroundWindowReader();

        UiaWaitCheck Check()
        {
            var hwnd = finder.FindWindow(processName, windowTitle);
            var window = windowReader.ReadFromHandle(hwnd);
            var title = window?.Title ?? "";
            return hwnd != IntPtr.Zero
                ? new UiaWaitCheck(true, title, new { Title = title, UsedEvents = !forcePolling })
                : new UiaWaitCheck(false, title, null);
        }

        IDisposable Subscribe(Action signal)
        {
            var automation = new UIA3Automation();
            var desktop = automation.GetDesktop();
            var handler = desktop.RegisterAutomationEvent(
                automation.EventLibrary.Window.WindowOpenedEvent,
                TreeScope.Children,
                (_, _) => signal());

            return new DelegateDisposable(() =>
            {
                try { desktop.FrameworkAutomationElement.UnregisterAutomationEventHandler(handler); } catch { }
                automation.Dispose();
            });
        }

        return WaitCore(Check, Subscribe, timeoutMs, intervalMs, cancellationToken, forcePolling);
    }

    internal static UiaWaitResult WaitCore(
        Func<UiaWaitCheck> check,
        Func<Action, IDisposable> subscribe,
        int timeoutMs,
        int intervalMs,
        CancellationToken cancellationToken,
        bool forcePolling = false)
    {
        timeoutMs = Math.Max(0, timeoutMs);
        intervalMs = Math.Max(100, intervalMs);

        if (forcePolling)
            return Poll(check, timeoutMs, intervalMs, cancellationToken, usedEvents: false, fellBackToPolling: false);

        var signal = new AutoResetEvent(false);
        IDisposable? subscription = null;

        try
        {
            subscription = subscribe(() => signal.Set());
        }
        catch
        {
            return Poll(check, timeoutMs, intervalMs, cancellationToken, usedEvents: false, fellBackToPolling: true);
        }

        var sw = Stopwatch.StartNew();
        var attempts = 0;
        var lastTitle = "";

        try
        {
            while (sw.ElapsedMilliseconds <= timeoutMs && !cancellationToken.IsCancellationRequested)
            {
                attempts++;
                var current = check();
                lastTitle = current.LastWindowTitle;
                if (current.Found)
                {
                    return new UiaWaitResult(
                        true,
                        UsedEvents: true,
                        FellBackToPolling: false,
                        attempts,
                        sw.ElapsedMilliseconds,
                        lastTitle,
                        current.Match,
                        "Found.");
                }

                var remaining = timeoutMs - (int)sw.ElapsedMilliseconds;
                if (remaining <= 0) break;
                var waitMs = Math.Min(SafetyRepollMs, remaining);
                signal.WaitOne(waitMs);
            }
        }
        finally
        {
            subscription.Dispose();
            signal.Dispose();
        }

        return new UiaWaitResult(
            false,
            UsedEvents: true,
            FellBackToPolling: false,
            attempts,
            sw.ElapsedMilliseconds,
            lastTitle,
            null,
            $"Timeout after {timeoutMs}ms ({attempts} attempts).");
    }

    private static UiaWaitResult Poll(
        Func<UiaWaitCheck> check,
        int timeoutMs,
        int intervalMs,
        CancellationToken cancellationToken,
        bool usedEvents,
        bool fellBackToPolling)
    {
        var sw = Stopwatch.StartNew();
        var attempts = 0;
        var lastTitle = "";

        while (sw.ElapsedMilliseconds <= timeoutMs && !cancellationToken.IsCancellationRequested)
        {
            attempts++;
            var current = check();
            lastTitle = current.LastWindowTitle;
            if (current.Found)
            {
                return new UiaWaitResult(
                    true,
                    usedEvents,
                    fellBackToPolling,
                    attempts,
                    sw.ElapsedMilliseconds,
                    lastTitle,
                    current.Match,
                    "Found.");
            }

            var remaining = timeoutMs - (int)sw.ElapsedMilliseconds;
            if (remaining <= 0) break;
            Thread.Sleep(Math.Min(intervalMs, remaining));
        }

        return new UiaWaitResult(
            false,
            usedEvents,
            fellBackToPolling,
            attempts,
            sw.ElapsedMilliseconds,
            lastTitle,
            null,
            $"Timeout after {timeoutMs}ms ({attempts} attempts).");
    }

    private sealed class DelegateDisposable(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}
