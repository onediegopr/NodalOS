using System.Diagnostics;
using System.Text.Json;
using OneBrain.Actions.Uia;
using OneBrain.Core.Actions;
using OneBrain.Core.Models;
using OneBrain.Core.Recipes;
using OneBrain.Core.Visual;
using OneBrain.Observation;
using OneBrain.Observation.Visual;
using OneBrain.Observation.Windows;
using OneBrain.Verification.Engine;

namespace OneBrain.Cli.Recipes;

public sealed class RecipeRunner
{
    public RecipeRunResult Run(RecipeDefinition recipe, bool forceContinueOnError = false)
    {
        var sw = Stopwatch.StartNew();
        var stepResults = new List<RecipeStepRunResult>();
        int passed = 0, failed = 0;
        var notes = new List<string>();

        foreach (var step in recipe.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Kind))
            {
                var bad = new RecipeStepRunResult(step.Id, step.Kind ?? "", false, "Step missing Kind", 0);
                stepResults.Add(bad);
                failed++;
                if (ShouldStop(recipe, step, forceContinueOnError))
                    break;
                continue;
            }

            var stepResult = ExecuteStep(step);
            stepResults.Add(stepResult);

            if (stepResult.Success) passed++;
            else failed++;

            if (ShouldStop(recipe, step, forceContinueOnError, stepResult.Success))
                break;
        }

        sw.Stop();

        return new RecipeRunResult(
            Success: failed == 0,
            Recipe: recipe.Name,
            TotalSteps: recipe.Steps.Count,
            Passed: passed,
            Failed: failed,
            DurationMs: sw.ElapsedMilliseconds,
            Steps: stepResults,
            Notes: notes);
    }

    private static bool ShouldStop(RecipeDefinition recipe, RecipeStepDefinition step, bool forceContinue, bool? stepSuccess = null)
    {
        if (forceContinue) return false;
        if (step.ContinueOnError == true) return false;
        if (stepSuccess == false && recipe.StopOnFirstFailure != false) return true;
        return false;
    }

    private static RecipeStepRunResult ExecuteStep(RecipeStepDefinition step)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return step.Kind.ToLowerInvariant() switch
            {
                "app.open"              => ExecuteAppOpen(step, sw),
                "browser.open"          => ExecuteBrowserOpen(step, sw),
                "wait"                  => ExecuteWait(step, sw),
                "actv.type"             => ExecuteActvType(step, sw),
                "actv.invoke"           => ExecuteActvInvoke(step, sw),
                "key"                   => ExecuteKey(step, sw),
                "visual.capture.window"  => ExecuteVisualCaptureWindow(step, sw),
                "visual.capture.element" => ExecuteVisualCaptureElement(step, sw),
                "visual.verify.changed"  => ExecuteVisualVerifyChanged(step, sw),
                "sleep"                 => ExecuteSleep(step, sw),
                _ => new RecipeStepRunResult(step.Id, step.Kind, false, $"Unsupported step kind: {step.Kind}", sw.ElapsedMilliseconds)
            };
        }
        catch (Exception ex)
        {
            return new RecipeStepRunResult(step.Id, step.Kind, false, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    // ── app.open ────────────────────────────────────────────────────────────
    private static RecipeStepRunResult ExecuteAppOpen(RecipeStepDefinition step, Stopwatch sw)
    {
        var app = step.App;
        if (string.IsNullOrWhiteSpace(app))
            return Fail(step, sw, "app.open requires 'app' field (explorer|calculator|notepad).");

        var (success, message) = AppLauncher.TryLaunch(app, step.Path ?? "");
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, success, message, sw.ElapsedMilliseconds);
    }

    // ── browser.open ────────────────────────────────────────────────────────
    private static RecipeStepRunResult ExecuteBrowserOpen(RecipeStepDefinition step, Stopwatch sw)
    {
        var url = step.Url ?? step.Path;
        if (string.IsNullOrWhiteSpace(url))
            return Fail(step, sw, "browser.open requires 'url' or 'path' field.");

        var browserName = step.Args?.GetValueOrDefault("browser") ?? "edge";
        var forceAccessibility = step.Args?.ContainsKey("forceAccessibility") == true
            || (step.Args?.GetValueOrDefault("forceAccessibility") ?? "") == "true";

        var fileUrl = url;
        if (!fileUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !fileUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !fileUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            fileUrl = new Uri(Path.GetFullPath(fileUrl)).AbsoluteUri;
        }

        var extra = forceAccessibility ? "--force-renderer-accessibility " : "";
        var browserArgs = $"{extra}\"{fileUrl}\"";

        string[] candidates = browserName == "edge"
            ? [@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
               @"C:\Program Files\Microsoft\Edge\Application\msedge.exe"]
            : [@"C:\Program Files\Google\Chrome\Application\chrome.exe",
               @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"];

        var launched = false;
        var lastError = "";
        foreach (var exe in candidates)
        {
            if (!File.Exists(exe)) continue;
            try
            {
                Process.Start(new ProcessStartInfo(exe, browserArgs) { UseShellExecute = false });
                launched = true;
                break;
            }
            catch (Exception ex) { lastError = ex.Message; }
        }

        if (!launched)
        {
            var fallbackExe = browserName == "edge" ? "msedge" : "chrome";
            try
            {
                Process.Start(new ProcessStartInfo(fallbackExe, browserArgs) { UseShellExecute = false });
                launched = true;
            }
            catch (Exception ex) { lastError = ex.Message; }
        }

        if (!launched)
        {
            sw.Stop();
            return new RecipeStepRunResult(step.Id, step.Kind, false,
                $"Failed to launch {browserName}: {lastError}", sw.ElapsedMilliseconds);
        }

        Thread.Sleep(1500);
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, true,
            $"Launched {browserName} with {url}", sw.ElapsedMilliseconds);
    }

    // ── wait ────────────────────────────────────────────────────────────────
    private static RecipeStepRunResult ExecuteWait(RecipeStepDefinition step, Stopwatch sw)
    {
        var proc = step.Process;
        var win = step.Window;
        var name = step.Name;
        var role = step.Role;
        var titleContains = step.TitleContains ?? step.Args?.GetValueOrDefault("titleContains");

        if (string.IsNullOrWhiteSpace(proc) && string.IsNullOrWhiteSpace(win))
            return Fail(step, sw, "wait requires 'process' or 'window' field.");

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(role) && string.IsNullOrWhiteSpace(titleContains))
            return Fail(step, sw, "wait requires 'name', 'role', or 'titleContains' in args.");

        var timeout = step.TimeoutMs ?? 5000;
        var interval = Math.Max(100, step.IntervalMs ?? 500);

        var selectorUsed = titleContains != null    ? $"title-contains:{titleContains}"
                         : name != null && role != null ? $"name:{name}+role:{role}"
                         : name != null              ? $"name:{name}"
                                                      : $"role:{role}";

        var reader = new CognitiveSnapshotReader();
        int attempts = 0;
        string lastTitle = "";

        while (sw.ElapsedMilliseconds < timeout)
        {
            attempts++;
            var snap = reader.Read(proc, win);
            lastTitle = snap?.Window.Title ?? "";

            if (snap != null)
            {
                bool found = false;
                object? matchObj = null;

                if (titleContains != null)
                {
                    found = snap.Window.Title.Contains(titleContains, StringComparison.OrdinalIgnoreCase);
                    if (found) matchObj = new { Title = snap.Window.Title };
                }
                else if (name != null)
                {
                    var el = role != null
                        ? snap.Elements.FirstOrDefault(e =>
                            e.Name.Contains(name, StringComparison.OrdinalIgnoreCase) &&
                            e.Role.Equals(role, StringComparison.OrdinalIgnoreCase))
                        : snap.Elements.FirstOrDefault(e =>
                            e.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
                    found = el != null;
                    if (found) matchObj = el;
                }
                else if (role != null)
                {
                    var el = snap.Elements.FirstOrDefault(e =>
                        e.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
                    found = el != null;
                    if (found) matchObj = el;
                }

                if (found)
                {
                    return new RecipeStepRunResult(step.Id, step.Kind, true,
                        $"Found after {sw.ElapsedMilliseconds}ms ({attempts} attempts): {selectorUsed}",
                        sw.ElapsedMilliseconds, matchObj);
                }
            }

            Thread.Sleep(interval);
        }

        return new RecipeStepRunResult(step.Id, step.Kind, false,
            $"Timeout after {timeout}ms ({attempts} attempts): {selectorUsed}. Last title: '{lastTitle}'",
            sw.ElapsedMilliseconds);
    }

    // ── actv.type ───────────────────────────────────────────────────────────
    private static RecipeStepRunResult ExecuteActvType(RecipeStepDefinition step, Stopwatch sw)
    {
        var targetRef = BuildTargetRef(step);
        if (targetRef == null)
            return Fail(step, sw, "actv.type requires a selector (role, name, automationId, or class).");

        var text = step.Text ?? "";
        var req = new ActionRequest("type", targetRef, text, step.Process, step.Window);

        var result = new BasicActionVerifier().ExecuteAndVerify(req);
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, result.Success, result.Message,
            sw.ElapsedMilliseconds, result);
    }

    // ── actv.invoke ─────────────────────────────────────────────────────────
    private static RecipeStepRunResult ExecuteActvInvoke(RecipeStepDefinition step, Stopwatch sw)
    {
        var targetRef = BuildTargetRef(step);
        if (targetRef == null)
            return Fail(step, sw, "actv.invoke requires a selector (role, name, automationId, or class).");

        var req = new ActionRequest("invoke", targetRef, null, step.Process, step.Window);

        var result = new BasicActionVerifier().ExecuteAndVerify(req);
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, result.Success, result.Message,
            sw.ElapsedMilliseconds, result);
    }

    // ── key ─────────────────────────────────────────────────────────────────
    private static RecipeStepRunResult ExecuteKey(RecipeStepDefinition step, Stopwatch sw)
    {
        var text = step.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            text = step.Args?.GetValueOrDefault("key") ?? "";
            if (string.IsNullOrWhiteSpace(text))
                return Fail(step, sw, "key requires 'text' or 'key' field.");
        }

        if (!string.IsNullOrWhiteSpace(step.Process))
        {
            var hwnd = new WindowFinder().FindWindow(step.Process, step.Window);
            if (hwnd != IntPtr.Zero)
                new WindowFinder().Activate(hwnd);
            Thread.Sleep(300);
        }

        var req = new ActionRequest("key", "role:Window", text, step.Process, step.Window);
        var result = new UiaActionExecutor().Execute(req);
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, result.Success, result.Message,
            sw.ElapsedMilliseconds, result);
    }

    // ── visual.capture.window ───────────────────────────────────────────────
    private static RecipeStepRunResult ExecuteVisualCaptureWindow(RecipeStepDefinition step, Stopwatch sw)
    {
        if (string.IsNullOrWhiteSpace(step.Process))
            return Fail(step, sw, "visual.capture.window requires 'process' field.");

        var outPath = step.Out;
        var result = new VisualCaptureService().Capture(new VisualCaptureRequest(
            ProcessName: step.Process,
            WindowTitle: step.Window,
            OutputPath: outPath));
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, result.Success, result.Message,
            sw.ElapsedMilliseconds, result);
    }

    // ── visual.capture.element ──────────────────────────────────────────────
    private static RecipeStepRunResult ExecuteVisualCaptureElement(RecipeStepDefinition step, Stopwatch sw)
    {
        if (string.IsNullOrWhiteSpace(step.Process))
            return Fail(step, sw, "visual.capture.element requires 'process' field.");

        if (string.IsNullOrWhiteSpace(step.Name) && string.IsNullOrWhiteSpace(step.Role) &&
            string.IsNullOrWhiteSpace(step.AutomationId) && string.IsNullOrWhiteSpace(step.Class))
            return Fail(step, sw, "visual.capture.element requires a selector (name, role, automationId, or class).");

        var outPath = step.Out;
        var result = new VisualCaptureService().Capture(new VisualCaptureRequest(
            ProcessName: step.Process,
            WindowTitle: step.Window,
            Target: new VisualElementTarget(
                Name: step.Name,
                Role: step.Role,
                AutomationId: step.AutomationId,
                ClassName: step.Class),
            OutputPath: outPath));
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, result.Success, result.Message,
            sw.ElapsedMilliseconds, result);
    }

    // ── visual.verify.changed ───────────────────────────────────────────────
    private static RecipeStepRunResult ExecuteVisualVerifyChanged(RecipeStepDefinition step, Stopwatch sw)
    {
        if (string.IsNullOrWhiteSpace(step.Before) || string.IsNullOrWhiteSpace(step.After))
            return Fail(step, sw, "visual.verify.changed requires 'before' and 'after' fields.");

        var threshold = step.Threshold ?? 0.005;
        var result = new VisualVerifier().Verify(step.Before, step.After, threshold);
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, result.Success && result.Changed, result.Message,
            sw.ElapsedMilliseconds, result);
    }

    // ── sleep ───────────────────────────────────────────────────────────────
    private static RecipeStepRunResult ExecuteSleep(RecipeStepDefinition step, Stopwatch sw)
    {
        var ms = step.TimeoutMs ?? 1000;
        if (step.Args?.TryGetValue("ms", out var msStr) == true && int.TryParse(msStr, out var parsed))
            ms = parsed;

        Thread.Sleep(ms);
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, true,
            $"Slept {ms}ms", sw.ElapsedMilliseconds);
    }

    // ── helpers ─────────────────────────────────────────────────────────────
    private static string? BuildTargetRef(RecipeStepDefinition step)
    {
        if (!string.IsNullOrWhiteSpace(step.Name))
            return $"name:{step.Name}";
        if (!string.IsNullOrWhiteSpace(step.Role))
            return $"role:{step.Role}";
        if (!string.IsNullOrWhiteSpace(step.AutomationId))
            return $"automation-id:{step.AutomationId}";
        if (!string.IsNullOrWhiteSpace(step.Class))
            return $"class:{step.Class}";
        return null;
    }

    private static RecipeStepRunResult Fail(RecipeStepDefinition step, Stopwatch sw, string message)
    {
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, false, message, sw.ElapsedMilliseconds);
    }
}
