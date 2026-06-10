using System.Diagnostics;
using OneBrain.Actions.Uia;
using OneBrain.Core.Actions;
using OneBrain.Core.Models;
using OneBrain.Core.Recipes;
using OneBrain.Core.Visual;
using OneBrain.Observation;
using OneBrain.Observation.Visual;
using OneBrain.Observation.Windows;
using OneBrain.Verification.Engine;
using OneBrain.Verification.Reports;

namespace OneBrain.Cli.Recipes;

public sealed class RecipeRunner
{
    private const int DefaultStepTimeoutMs = 15000;
    private const int OuterGraceMs = 3000;
    private RecipeDefinition? _currentRecipe;
    private CancellationTokenSource? _stepCts;
    private readonly RecipeExecutionContext _ctx = new();

    public RecipeRunResult Run(RecipeDefinition recipe, bool forceContinueOnError = false)
    {
        _currentRecipe = recipe;
        _ctx.Variables.Clear();
        _ctx.Notes.Clear();

        if (recipe.Variables != null)
        {
            foreach (var (k, v) in recipe.Variables)
                _ctx.Variables[k] = v;
        }

        // Built-in runtime variables
        var now = DateTime.Now;
        _ctx.Variables["runtime.timestamp"] = now.ToString("yyyy-MM-ddTHH:mm:ss");
        _ctx.Variables["runtime.date"] = now.ToString("yyyy-MM-dd");
        _ctx.Variables["runtime.temp"] = Path.GetTempPath().TrimEnd('\\');

        var sw = Stopwatch.StartNew();
        var stepResults = new List<RecipeStepRunResult>();
        int passed = 0, failed = 0;

        foreach (var step in recipe.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Kind))
            {
                var bad = new RecipeStepRunResult(step.Id, step.Kind ?? "", false, "Step missing Kind", 0);
                stepResults.Add(bad);
                failed++;
                if (ShouldStop(recipe, step, forceContinueOnError, false))
                    break;
                continue;
            }

            RecipeStepRunResult stepResult;
            var stepTimeout = GetStepTimeoutMs(step, recipe);
            if (recipe.MaxDurationMs.HasValue)
            {
                var remainingGlobalMs = (int)(recipe.MaxDurationMs.Value - sw.ElapsedMilliseconds);
                if (remainingGlobalMs <= 0)
                {
                    var globalTimeoutResult = new RecipeStepRunResult(
                        step.Id, 
                        step.Kind ?? "", 
                        false, 
                        $"Recipe global duration limit exceeded ({recipe.MaxDurationMs.Value}ms limit)", 
                        0);
                    stepResults.Add(globalTimeoutResult);
                    failed++;
                    break;
                }
                if (stepTimeout > remainingGlobalMs)
                {
                    stepTimeout = remainingGlobalMs;
                }
            }

            try
            {
                using var stepCts = new CancellationTokenSource();
                _stepCts = stepCts;
                var outerTimeout = stepTimeout + OuterGraceMs;
                var capturedCts = stepCts;
                var task = Task.Run(() =>
                {
                    try { return ExecuteStep(step); }
                    catch (RecipeVariableException ex) { return FailNow(step, ex.Message); }
                    catch (Exception ex) { return new RecipeStepRunResult(step.Id, step.Kind, false, ex.Message, 0); }
                }, capturedCts.Token);

                if (task.Wait(outerTimeout, capturedCts.Token))
                {
                    stepResult = task.Result;
                }
                else
                {
                    capturedCts.Cancel();
                    var details = GetStepDetails(step);
                    stepResult = new RecipeStepRunResult(step.Id, step.Kind, false,
                        $"Step timeout after {stepTimeout}ms: {step.Kind}{details}", stepTimeout);
                }
                _stepCts = null;
            }
            catch (RecipeVariableException ex)
            {
                stepResult = FailNow(step, ex.Message);
            }

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
            Notes: _ctx.Notes.ToList())
        {
            Variables = new Dictionary<string, string>(_ctx.Variables)
        };
    }

    private int GetStepTimeoutMs(RecipeStepDefinition step, RecipeDefinition recipe)
    {
        var kind = step.Kind.ToLowerInvariant();
        if (kind is "delay" or "sleep")
        {
            var sleepMs = step.TimeoutMs ?? 1000;
            if (step.Args?.TryGetValue("ms", out var msStr) == true && int.TryParse(msStr, out var parsed))
                sleepMs = parsed;
            return sleepMs + 3000;
        }

        if (step.TimeoutMs.HasValue) return step.TimeoutMs.Value;
        if (recipe.DefaultTimeoutMs.HasValue) return recipe.DefaultTimeoutMs.Value;

        return kind switch
        {
            "browser.open" => 15000,
            "browser.read" => 10000,
            "wait" => 10000,
            "visual.capture" => 10000,
            _ => 15000
        };
    }

    private static string GetStepDetails(RecipeStepDefinition step)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(step.Url)) parts.Add($"url: {step.Url}");
        else if (!string.IsNullOrEmpty(step.Path)) parts.Add($"path: {step.Path}");

        if (!string.IsNullOrEmpty(step.Process)) parts.Add($"process: {step.Process}");
        if (!string.IsNullOrEmpty(step.Window)) parts.Add($"window: {step.Window}");
        if (!string.IsNullOrEmpty(step.Name)) parts.Add($"name: {step.Name}");
        if (!string.IsNullOrEmpty(step.Role)) parts.Add($"role: {step.Role}");
        if (!string.IsNullOrEmpty(step.AutomationId)) parts.Add($"automationId: {step.AutomationId}");
        if (!string.IsNullOrEmpty(step.Class)) parts.Add($"class: {step.Class}");
        if (!string.IsNullOrEmpty(step.TitleContains)) parts.Add($"titleContains: {step.TitleContains}");
        if (!string.IsNullOrEmpty(step.App)) parts.Add($"app: {step.App}");

        if (parts.Count == 0) return "";
        return $" ({string.Join(", ", parts)})";
    }

    private static bool ShouldStop(RecipeDefinition recipe, RecipeStepDefinition step, bool forceContinue, bool? stepSuccess = null)
    {
        if (forceContinue) return false;
        if (step.ContinueOnError == true) return false;
        if (stepSuccess == false && recipe.StopOnFirstFailure != false) return true;
        return false;
    }

    private RecipeStepRunResult ExecuteStep(RecipeStepDefinition step)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return step.Kind.ToLowerInvariant() switch
            {
                "app.open"               => ExecuteAppOpen(step, sw),
                "browser.open"           => ExecuteBrowserOpen(step, sw),
                "browser.read"           => ExecuteBrowserRead(step, sw),
                "wait"                   => ExecuteWait(step, sw),
                "actv.type"              => ExecuteActvType(step, sw),
                "actv.invoke"            => ExecuteActvInvoke(step, sw),
                "key"                    => ExecuteKey(step, sw),
                "visual.capture"         => ExecuteVisualCapture(step, sw),
                "visual.capture.window"   => ExecuteVisualCaptureWindow(step, sw),
                "visual.capture.element"  => ExecuteVisualCaptureElement(step, sw),
                "visual.verify.changed"   => ExecuteVisualVerifyChanged(step, sw),
                "snapshot.read"          => ExecuteSnapshotRead(step, sw),
                "assert.contains"        => ExecuteAssertContains(step, sw),
                "assert.equals"          => ExecuteAssertEquals(step, sw),
                "if"                     => ExecuteIf(step, sw),
                "note"                   => ExecuteNote(step, sw),
                "delay"                  => ExecuteSleep(step, sw),
                "sleep"                  => ExecuteSleep(step, sw),
                "debug.hang"             => ExecuteDebugHang(step, sw),
                _ => new RecipeStepRunResult(step.Id, step.Kind, false, $"Unsupported step kind: {step.Kind}", sw.ElapsedMilliseconds)
            };
        }
        catch (RecipeVariableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new RecipeStepRunResult(step.Id, step.Kind, false, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    private string R(string? text) => RecipeTemplateResolver.ResolveOrNull(text, _ctx.Variables) ?? "";

    // ── app.open ────────────────────────────────────────────────────────────
    private RecipeStepRunResult ExecuteAppOpen(RecipeStepDefinition step, Stopwatch sw)
    {
        var app = step.App;
        if (string.IsNullOrWhiteSpace(app))
            return Fail(step, sw, "app.open requires 'app' field (explorer|calculator|notepad).");

        var (success, message) = AppLauncher.TryLaunch(app, R(step.Path) ?? "");
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, success, message, sw.ElapsedMilliseconds);
    }

    // ── browser.open ────────────────────────────────────────────────────────
    private RecipeStepRunResult ExecuteBrowserOpen(RecipeStepDefinition step, Stopwatch sw)
    {
        var url = step.Url ?? step.Path;
        if (string.IsNullOrWhiteSpace(url))
            return Fail(step, sw, "browser.open requires 'url' or 'path' field.");

        url = R(url);

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

        var browserTimeoutMs = step.TimeoutMs ?? _currentRecipe?.DefaultTimeoutMs ?? 15000;
        var openSw = Stopwatch.StartNew();
        var finder = new WindowFinder();
        var processToFind = browserName.Equals("edge", StringComparison.OrdinalIgnoreCase) ? "msedge" : "chrome";

        IntPtr hwnd = IntPtr.Zero;
        while (openSw.ElapsedMilliseconds < browserTimeoutMs)
        {
            if (_stepCts?.IsCancellationRequested == true) break;
            hwnd = finder.FindWindow(processToFind, null);
            if (hwnd != IntPtr.Zero)
            {
                break;
            }
            Thread.Sleep(250);
        }

        if (hwnd == IntPtr.Zero)
        {
            sw.Stop();
            return new RecipeStepRunResult(step.Id, step.Kind, false,
                $"Timeout waiting for {browserName} window to appear (process: {processToFind}, timeout: {browserTimeoutMs}ms)", sw.ElapsedMilliseconds);
        }

        // Wait a brief moment to let the browser initialize and render
        Thread.Sleep(500);

        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, true,
            $"Launched {browserName} with {url} and found active window handle {hwnd}", sw.ElapsedMilliseconds);
    }

    // ── browser.read ────────────────────────────────────────────────────────
    private RecipeStepRunResult ExecuteBrowserRead(RecipeStepDefinition step, Stopwatch sw)
    {
        var proc = R(step.Process);
        if (string.IsNullOrWhiteSpace(proc))
            return Fail(step, sw, "browser.read requires 'process' field.");

        var property = (step.Property ?? "title").ToLowerInvariant();
        if (property is not ("title" or "text" or "url"))
            return Fail(step, sw, $"browser.read unsupported property: '{step.Property}'. Supported: title, text, url.");

        var reader = new CognitiveSnapshotReader();
        var snap = reader.Read(proc, R(step.Window));

        if (snap == null)
            return Fail(step, sw, $"browser.read: could not read snapshot for process '{proc}'.");

        string? value = property switch
        {
            "title" => snap.Window.Title,
            "url" => ExtractUrlFromSnapshot(snap),
            "text" => ExtractTextFromSnapshot(snap),
            _ => null
        };

        if (string.IsNullOrWhiteSpace(value))
            return Fail(step, sw, $"browser.read: '{property}' not found for process '{proc}'.");

        if (!string.IsNullOrWhiteSpace(step.SaveAs))
        {
            _ctx.Variables[step.SaveAs] = value;
            _ctx.Variables[step.SaveAs + ".raw"] = value;
        }

        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, true,
            $"browser.read {property} = '{value}'", sw.ElapsedMilliseconds, value);
    }

    private static string? ExtractUrlFromSnapshot(CognitiveSnapshot snap)
    {
        var urlEl = snap.Elements.FirstOrDefault(e =>
            e.Role != null && e.Role.Equals("Edit", StringComparison.OrdinalIgnoreCase) &&
            e.Name != null && (e.Name.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                               e.Name.Contains("http", StringComparison.OrdinalIgnoreCase)));

        if (urlEl?.Name != null && urlEl.Name.Contains("http"))
        {
            var match = System.Text.RegularExpressions.Regex.Match(urlEl.Name, @"https?://[^\s]+");
            if (match.Success) return match.Value;
        }

        return urlEl?.Name;
    }

    private static string ExtractTextFromSnapshot(CognitiveSnapshot snap)
    {
        var texts = snap.Elements
            .Where(e => !string.IsNullOrWhiteSpace(e.Name))
            .Select(e => e.Name!)
            .Where(n => n.Length > 2)
            .Distinct()
            .Take(30);
        return string.Join(" | ", texts);
    }

    // ── wait ────────────────────────────────────────────────────────────────
    private RecipeStepRunResult ExecuteWait(RecipeStepDefinition step, Stopwatch sw)
    {
        var proc = R(step.Process);
        var win = R(step.Window);
        var name = R(step.Name);
        var role = R(step.Role);
        var titleContains = R(step.TitleContains ?? step.Args?.GetValueOrDefault("titleContains"));
        if (titleContains == "") titleContains = null;
        var automationId = R(step.AutomationId);

        if (string.IsNullOrWhiteSpace(proc) && string.IsNullOrWhiteSpace(win))
            return Fail(step, sw, "wait requires 'process' or 'window' field.");

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(role) && string.IsNullOrWhiteSpace(titleContains) && string.IsNullOrWhiteSpace(automationId))
            return Fail(step, sw, "wait requires 'name', 'role', 'titleContains', or 'automationId'.");

        var timeout = step.TimeoutMs ?? _currentRecipe?.DefaultTimeoutMs ?? 10000;
        var interval = Math.Max(100, step.IntervalMs ?? 500);

        var searchCriteria = new List<string>();
        if (!string.IsNullOrEmpty(proc)) searchCriteria.Add($"process: '{proc}'");
        if (!string.IsNullOrEmpty(win)) searchCriteria.Add($"window: '{win}'");
        if (!string.IsNullOrEmpty(name)) searchCriteria.Add($"name: '{name}'");
        if (!string.IsNullOrEmpty(role)) searchCriteria.Add($"role: '{role}'");
        if (!string.IsNullOrEmpty(automationId)) searchCriteria.Add($"automationId: '{automationId}'");
        if (!string.IsNullOrEmpty(titleContains)) searchCriteria.Add($"titleContains: '{titleContains}'");
        
        var criteriaStr = string.Join(", ", searchCriteria);

        var reader = new CognitiveSnapshotReader();
        int attempts = 0;
        string lastTitle = "";

        while (sw.ElapsedMilliseconds < timeout)
        {
            if (_stepCts?.IsCancellationRequested == true) break;
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
                else if (automationId != null)
                {
                    var el = snap.Elements.FirstOrDefault(e =>
                        e.AutomationId != null && e.AutomationId.Equals(automationId, StringComparison.OrdinalIgnoreCase));
                    found = el != null;
                    if (found) matchObj = el;
                }
                else if (name != null)
                {
                    var el = role != null
                        ? snap.Elements.FirstOrDefault(e =>
                            e.Name != null && e.Name.Contains(name, StringComparison.OrdinalIgnoreCase) &&
                            e.Role != null && e.Role.Equals(role, StringComparison.OrdinalIgnoreCase))
                        : snap.Elements.FirstOrDefault(e =>
                            e.Name != null && e.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
                    found = el != null;
                    if (found) matchObj = el;
                }
                else if (role != null)
                {
                    var el = snap.Elements.FirstOrDefault(e =>
                        e.Role != null && e.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
                    found = el != null;
                    if (found) matchObj = el;
                }

                if (found)
                {
                    SaveOutput(step, matchObj);
                    return new RecipeStepRunResult(step.Id, step.Kind, true,
                        $"Found after {sw.ElapsedMilliseconds}ms ({attempts} attempts): [{criteriaStr}]",
                        sw.ElapsedMilliseconds, matchObj);
                }
            }

            Thread.Sleep(interval);
        }

        return new RecipeStepRunResult(step.Id, step.Kind, false,
            $"Timeout after {timeout}ms ({attempts} attempts) waiting for: [{criteriaStr}]. Last title: '{lastTitle}'",
            sw.ElapsedMilliseconds);
    }

    // ── actv.type ───────────────────────────────────────────────────────────
    private RecipeStepRunResult ExecuteActvType(RecipeStepDefinition step, Stopwatch sw)
    {
        var targetRef = BuildTargetRef(step);
        if (targetRef == null)
            return Fail(step, sw, "actv.type requires a selector (role, name, automationId, or class).");

        var text = R(step.Text);

        var req = new ActionRequest("type", targetRef, text, R(step.Process), R(step.Window),
            CancellationToken: _stepCts?.Token);

        var result = new BasicActionVerifier().ExecuteAndVerify(req);
        SaveOutput(step, result);
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, result.Success, result.Message,
            sw.ElapsedMilliseconds, result);
    }

    // ── actv.invoke ─────────────────────────────────────────────────────────
    private RecipeStepRunResult ExecuteActvInvoke(RecipeStepDefinition step, Stopwatch sw)
    {
        var targetRef = BuildTargetRef(step);
        if (targetRef == null)
            return Fail(step, sw, "actv.invoke requires a selector (role, name, automationId, or class).");

        var req = new ActionRequest("invoke", targetRef, null, R(step.Process), R(step.Window),
            CancellationToken: _stepCts?.Token);

        var result = new BasicActionVerifier().ExecuteAndVerify(req);
        SaveOutput(step, result);
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, result.Success, result.Message,
            sw.ElapsedMilliseconds, result);
    }

    // ── key ─────────────────────────────────────────────────────────────────
    private RecipeStepRunResult ExecuteKey(RecipeStepDefinition step, Stopwatch sw)
    {
        var text = R(step.Text);
        if (string.IsNullOrWhiteSpace(text))
        {
            text = step.Args?.GetValueOrDefault("key") ?? "";
            if (string.IsNullOrWhiteSpace(text))
                return Fail(step, sw, "key requires 'text' or 'key' field.");
        }

        if (!string.IsNullOrWhiteSpace(step.Process))
        {
            var hwnd = new WindowFinder().FindWindow(R(step.Process), R(step.Window));
            if (hwnd != IntPtr.Zero)
                new WindowFinder().Activate(hwnd);
            Thread.Sleep(300);
        }

        var req = new ActionRequest("key", "role:Window", text, R(step.Process), R(step.Window),
            CancellationToken: _stepCts?.Token);
        var result = new UiaActionExecutor().Execute(req);
        SaveOutput(step, result);
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, result.Success, result.Message,
            sw.ElapsedMilliseconds, result);
    }

    // ── visual.capture ─────────────────────────────────────────────────────
    private RecipeStepRunResult ExecuteVisualCapture(RecipeStepDefinition step, Stopwatch sw)
    {
        var mode = (step.Args?.GetValueOrDefault("mode") ?? "window").ToLowerInvariant();
        if (mode is not ("window" or "region" or "element" or "fullscreen"))
            return Fail(step, sw, $"visual.capture unsupported mode: '{mode}'. Supported: window, region, element, fullscreen.");

        var proc = R(step.Process);
        var win = R(step.Window);
        var outPath = R(step.Out);

        VisualCaptureRequest request = mode switch
        {
            "fullscreen" => new VisualCaptureRequest(FullScreen: true, AllowFullScreen: true, OutputPath: outPath),
            "region" or "element" => new VisualCaptureRequest(
                ProcessName: proc, WindowTitle: win,
                Target: new VisualElementTarget(
                    Name: R(step.Name), Role: R(step.Role),
                    AutomationId: R(step.AutomationId), ClassName: R(step.Class)),
                OutputPath: outPath),
            _ => new VisualCaptureRequest(ProcessName: proc, WindowTitle: win, OutputPath: outPath)
        };

        var result = new VisualCaptureService().Capture(request);

        if (!string.IsNullOrWhiteSpace(step.SaveAs) && result.Success)
        {
            if (result.OutputPath != null)
            {
                _ctx.Variables[step.SaveAs] = result.OutputPath;
                _ctx.Variables[step.SaveAs + ".path"] = result.OutputPath;
            }
            _ctx.Variables[step.SaveAs + ".width"] = result.Width.ToString();
            _ctx.Variables[step.SaveAs + ".height"] = result.Height.ToString();
            if (result.CapturedAtUtc != null)
                _ctx.Variables[step.SaveAs + ".timestamp"] = result.CapturedAtUtc;
        }

        SaveOutput(step, result);
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, result.Success, result.Message,
            sw.ElapsedMilliseconds, result);
    }

    // ── visual.capture.window ───────────────────────────────────────────────
    private RecipeStepRunResult ExecuteVisualCaptureWindow(RecipeStepDefinition step, Stopwatch sw)
    {
        if (string.IsNullOrWhiteSpace(step.Process))
            return Fail(step, sw, "visual.capture.window requires 'process' field.");

        var outPath = R(step.Out);
        var result = new VisualCaptureService().Capture(new VisualCaptureRequest(
            ProcessName: R(step.Process),
            WindowTitle: R(step.Window),
            OutputPath: outPath));
        SaveOutput(step, result);
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, result.Success, result.Message,
            sw.ElapsedMilliseconds, result);
    }

    // ── visual.capture.element ──────────────────────────────────────────────
    private RecipeStepRunResult ExecuteVisualCaptureElement(RecipeStepDefinition step, Stopwatch sw)
    {
        if (string.IsNullOrWhiteSpace(step.Process))
            return Fail(step, sw, "visual.capture.element requires 'process' field.");

        if (string.IsNullOrWhiteSpace(step.Name) && string.IsNullOrWhiteSpace(step.Role) &&
            string.IsNullOrWhiteSpace(step.AutomationId) && string.IsNullOrWhiteSpace(step.Class))
            return Fail(step, sw, "visual.capture.element requires a selector (name, role, automationId, or class).");

        var outPath = R(step.Out);
        var result = new VisualCaptureService().Capture(new VisualCaptureRequest(
            ProcessName: R(step.Process),
            WindowTitle: R(step.Window),
            Target: new VisualElementTarget(
                Name: R(step.Name),
                Role: R(step.Role),
                AutomationId: R(step.AutomationId),
                ClassName: R(step.Class)),
            OutputPath: outPath));
        SaveOutput(step, result);
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, result.Success, result.Message,
            sw.ElapsedMilliseconds, result);
    }

    // ── visual.verify.changed ───────────────────────────────────────────────
    private RecipeStepRunResult ExecuteVisualVerifyChanged(RecipeStepDefinition step, Stopwatch sw)
    {
        var before = R(step.Before);
        var after = R(step.After);
        if (string.IsNullOrWhiteSpace(before) || string.IsNullOrWhiteSpace(after))
            return Fail(step, sw, "visual.verify.changed requires 'before' and 'after' fields.");

        var threshold = step.Threshold ?? 0.005;
        var result = new VisualVerifier().Verify(before, after, threshold);
        SaveOutput(step, result);
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, result.Success && result.Changed, result.Message,
            sw.ElapsedMilliseconds, result);
    }

    // ── snapshot.read ───────────────────────────────────────────────────────
    private RecipeStepRunResult ExecuteSnapshotRead(RecipeStepDefinition step, Stopwatch sw)
    {
        if (string.IsNullOrWhiteSpace(step.Process))
            return Fail(step, sw, "snapshot.read requires 'process' field.");
        if (string.IsNullOrWhiteSpace(step.SaveAs))
            return Fail(step, sw, "snapshot.read requires 'saveAs' field.");
        if (string.IsNullOrWhiteSpace(step.Name) && string.IsNullOrWhiteSpace(step.Role) &&
            string.IsNullOrWhiteSpace(step.AutomationId) && string.IsNullOrWhiteSpace(step.Class))
            return Fail(step, sw, "snapshot.read requires a selector (name, role, automationId, or class).");

        var prop = (step.Property ?? "name").ToLowerInvariant();
        var proc = R(step.Process);
        var win = R(step.Window);

        UiElementSnapshot? match = null;
        string lastWindowTitle = "";
        int attempts = 0;
        var timeout = step.TimeoutMs ?? _currentRecipe?.DefaultTimeoutMs ?? 5000;
        var interval = Math.Max(100, step.IntervalMs ?? 500);

        while (sw.ElapsedMilliseconds < timeout)
        {
            if (_stepCts?.IsCancellationRequested == true) break;
            attempts++;
            var snap = new CognitiveSnapshotReader().Read(proc, win);
            lastWindowTitle = snap?.Window.Title ?? "";
            if (snap != null)
            {
                match = FindElement(snap, step);
                if (match != null) break;
            }
            Thread.Sleep(interval);
        }

        if (match == null)
        {
            return new RecipeStepRunResult(step.Id, step.Kind, false,
                $"snapshot.read: element not found after {sw.ElapsedMilliseconds}ms ({attempts} attempts).",
                sw.ElapsedMilliseconds);
        }

        string rawValue;
        switch (prop)
        {
            case "name":         rawValue = match.Name; break;
            case "automationid": rawValue = match.AutomationId; break;
            case "classname":
            case "class":        rawValue = match.ClassName; break;
            case "role":         rawValue = match.Role; break;
            case "text":         rawValue = match.Name; break;
            case "title":
                rawValue = lastWindowTitle;
                if (string.IsNullOrWhiteSpace(rawValue))
                    return new RecipeStepRunResult(step.Id, step.Kind, false,
                        "snapshot.read: title property requested but window title is empty.",
                        sw.ElapsedMilliseconds);
                break;
            default:
                return new RecipeStepRunResult(step.Id, step.Kind, false,
                    $"snapshot.read: unsupported property '{step.Property}'. Supported: name, automationId, class, role, text, title.",
                    sw.ElapsedMilliseconds);
        }

        rawValue ??= "";

        var transformed = ApplyTransform(rawValue, step.Transform);
        _ctx.Variables[step.SaveAs] = transformed;
        _ctx.Variables[step.SaveAs + "Raw"] = rawValue;

        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, true,
            $"Read {prop}='{rawValue}' -> saved as '{step.SaveAs}' = '{transformed}'",
            sw.ElapsedMilliseconds, new { RawValue = rawValue, Transformed = transformed });
    }

    // ── assert.contains ─────────────────────────────────────────────────────
    private RecipeStepRunResult ExecuteAssertContains(RecipeStepDefinition step, Stopwatch sw)
    {
        var value = R(step.Value);
        var expected = R(step.Expected);
        var ignoreCase = step.IgnoreCase ?? true;

        if (string.IsNullOrWhiteSpace(value))
            return Fail(step, sw, "assert.contains requires 'value' field.");
        if (string.IsNullOrWhiteSpace(expected))
            return Fail(step, sw, "assert.contains requires 'expected' field.");

        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var contains = value.Contains(expected, comparison);

        sw.Stop();
        var message = contains
            ? $"assert.contains passed: '{value}' contains '{expected}'"
            : $"assert.contains FAILED: '{value}' does not contain '{expected}'";
        return new RecipeStepRunResult(step.Id, step.Kind, contains, message, sw.ElapsedMilliseconds);
    }

    // ── assert.equals ───────────────────────────────────────────────────────
    private RecipeStepRunResult ExecuteAssertEquals(RecipeStepDefinition step, Stopwatch sw)
    {
        var value = R(step.Value);
        var expected = R(step.Expected);
        var ignoreCase = step.IgnoreCase ?? false;

        if (string.IsNullOrWhiteSpace(value))
            return Fail(step, sw, "assert.equals requires 'value' field.");
        if (string.IsNullOrWhiteSpace(expected))
            return Fail(step, sw, "assert.equals requires 'expected' field.");

        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var equals = value.Equals(expected, comparison);

        sw.Stop();
        var message = equals
            ? $"assert.equals passed: '{value}' == '{expected}'"
            : $"assert.equals FAILED: '{value}' != '{expected}'";
        return new RecipeStepRunResult(step.Id, step.Kind, equals, message, sw.ElapsedMilliseconds);
    }

    // ── note ─────────────────────────────────────────────────────────────────
    private RecipeStepRunResult ExecuteNote(RecipeStepDefinition step, Stopwatch sw)
    {
        var message = R(step.Text ?? step.Args?.GetValueOrDefault("message") ?? "");
        _ctx.Notes.Add(message);
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, true, message, sw.ElapsedMilliseconds);
    }

    // ── sleep ───────────────────────────────────────────────────────────────
    private RecipeStepRunResult ExecuteSleep(RecipeStepDefinition step, Stopwatch sw)
    {
        var ms = step.TimeoutMs ?? 1000;
        if (step.Args?.TryGetValue("ms", out var msStr) == true && int.TryParse(msStr, out var parsed))
            ms = parsed;

        Thread.Sleep(ms);
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, true, $"Slept {ms}ms", sw.ElapsedMilliseconds);
    }

    // ── debug.hang ─────────────────────────────────────────────────────────
    private RecipeStepRunResult ExecuteDebugHang(RecipeStepDefinition step, Stopwatch sw)
    {
        var ms = step.TimeoutMs ?? 1000;
        if (step.Args?.TryGetValue("ms", out var msStr) == true && int.TryParse(msStr, out var parsed))
            ms = parsed;

        // Non-cooperative hang: Thread.Sleep does not observe CancellationToken.
        // The outer timeout wrapper will cut this step.
        Thread.Sleep(ms);
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, false,
            $"debug.hang completed after {ms}ms (should have been cut by timeout)", sw.ElapsedMilliseconds);
    }

    // ── if ───────────────────────────────────────────────────────────────────
    private RecipeStepRunResult ExecuteIf(RecipeStepDefinition step, Stopwatch sw)
    {
        var cond = step.Condition;
        if (cond == null)
            return Fail(step, sw, "if requires 'condition' field.");

        var op = (cond.Operator ?? "equals").ToLowerInvariant();
        var ignoreCase = cond.IgnoreCase ?? false;

        string? left = null;
        string? right = null;
        bool leftMissing = false;
        bool rightMissing = false;
        string? missingVarName = null;

        try { left = R(cond.Left); }
        catch (RecipeVariableException ex) { leftMissing = true; missingVarName ??= ex.Message; }

        try { right = R(cond.Right); }
        catch (RecipeVariableException ex) { rightMissing = true; missingVarName ??= ex.Message; }

        bool evalResult;

        if (op == "exists" || op == "notexists")
        {
            var exists = !leftMissing && !string.IsNullOrWhiteSpace(left);
            evalResult = op == "exists" ? exists : !exists;
        }
        else
        {
            if (leftMissing || rightMissing)
            {
                sw.Stop();
                var missingMsg = leftMissing
                    ? $"Missing template variable for condition left: {cond.Left}"
                    : $"Missing template variable for condition right: {cond.Right}";
                return new RecipeStepRunResult(step.Id, step.Kind, false, missingMsg, sw.ElapsedMilliseconds);
            }

            var comp = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            evalResult = op switch
            {
                "equals"      => string.Equals(left, right, comp),
                "notequals"   => !string.Equals(left, right, comp),
                "contains"    => (left ?? "").Contains(right ?? "", comp),
                "notcontains" => !(left ?? "").Contains(right ?? "", comp),
                _ => throw new InvalidOperationException($"Unknown condition operator: {op}")
            };
        }

        var branch = evalResult ? step.Then : step.Else;
        var branchName = evalResult ? "then" : "else";
        List<RecipeStepRunResult> branchResults = new();
        bool allPassed = true;

        if (branch != null)
        {
            foreach (var nestedStep in branch)
            {
                RecipeStepRunResult nestedResult;
                try
                {
                    nestedResult = ExecuteStep(nestedStep);
                }
                catch (RecipeVariableException ex)
                {
                    nestedResult = FailNow(nestedStep, ex.Message);
                }

                branchResults.Add(nestedResult);
                if (!nestedResult.Success)
                {
                    allPassed = false;
                    if (nestedStep.ContinueOnError != true)
                        break;
                }
            }
        }

        sw.Stop();
        var summary = evalResult
            ? $"if: condition {op} true -> executed 'then' branch ({branchResults.Count} steps)"
            : branch != null
                ? $"if: condition {op} false -> executed 'else' branch ({branchResults.Count} steps)"
                : $"if: condition {op} false -> no 'else' branch, skipped";

        return new RecipeStepRunResult(step.Id, step.Kind, allPassed, summary, sw.ElapsedMilliseconds,
            new
            {
                Condition = new { Left = left ?? cond.Left, Operator = op, Right = right ?? cond.Right, Result = evalResult },
                Branch = branchName,
                Steps = branchResults
            });
    }

    // ── helpers ─────────────────────────────────────────────────────────────
    private static UiElementSnapshot? FindElement(CognitiveSnapshot snap, RecipeStepDefinition step)
    {
        if (!string.IsNullOrWhiteSpace(step.AutomationId))
            return snap.Elements.FirstOrDefault(e =>
                e.AutomationId.Equals(step.AutomationId, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(step.Name))
            return snap.Elements.FirstOrDefault(e =>
                e.Name.Contains(step.Name, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(step.Role))
            return snap.Elements.FirstOrDefault(e =>
                e.Role.Equals(step.Role, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(step.Class))
            return snap.Elements.FirstOrDefault(e =>
                e.ClassName.Equals(step.Class, StringComparison.OrdinalIgnoreCase));
        return null;
    }

    private string? BuildTargetRef(RecipeStepDefinition step)
    {
        if (!string.IsNullOrWhiteSpace(step.Name))
            return $"name:{R(step.Name)}";
        if (!string.IsNullOrWhiteSpace(step.Role))
            return $"role:{R(step.Role)}";
        if (!string.IsNullOrWhiteSpace(step.AutomationId))
            return $"automation-id:{R(step.AutomationId)}";
        if (!string.IsNullOrWhiteSpace(step.Class))
            return $"class:{R(step.Class)}";
        return null;
    }

    private void SaveOutput(RecipeStepDefinition step, object? result)
    {
        if (string.IsNullOrWhiteSpace(step.SaveAs) || result == null) return;

        if (result is UiElementSnapshot el)
        {
            var v = el.Name; // wait saves matched element, use Name as default
            if (!string.IsNullOrWhiteSpace(v))
                _ctx.Variables[step.SaveAs] = v;
        }
        else if (result is VisualCaptureResult vcr && vcr.OutputPath != null)
        {
            _ctx.Variables[step.SaveAs] = vcr.OutputPath;
        }
        else if (result is VerifiedActionResult var)
        {
            _ctx.Variables[step.SaveAs] = var.Message;
        }
        else if (result is ActionResult ar)
        {
            _ctx.Variables[step.SaveAs] = ar.Message;
        }
    }

    private static string ApplyTransform(string value, string? transform)
    {
        if (string.IsNullOrWhiteSpace(transform) || transform == "none" || transform == "default")
            return value;

        if (transform == "trim")
            return value.Trim();

        if (transform == "calculatorResult")
        {
            var trimmed = value.Trim();
            var parts = trimmed.Split(' ');
            if (parts.Length > 0 && int.TryParse(parts[^1], out var num))
                return num.ToString();
            return trimmed;
        }

        return value;
    }

    private static RecipeStepRunResult FailNow(RecipeStepDefinition step, string message)
    {
        return new RecipeStepRunResult(step.Id, step.Kind, false, message, 0);
    }

    private static RecipeStepRunResult Fail(RecipeStepDefinition step, Stopwatch sw, string message)
    {
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, false, message, sw.ElapsedMilliseconds);
    }
}
