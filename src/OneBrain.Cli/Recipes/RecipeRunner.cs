using System.Diagnostics;
using System.Linq;
using OneBrain.Actions.Uia;
using OneBrain.Cli.Browser;
using OneBrain.Core.Profiles;
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

public static class RecipeRunner_ExtractHelper
{
    public static Dictionary<string, string> Extract(string combined)
    {
        return RecipeRunner.ExtractCommercialFields(combined, combined, "product");
    }
}

public sealed class RecipeRunner
{
    private const int DefaultStepTimeoutMs = 15000;
    private const int OuterGraceMs = 3000;
    private RecipeDefinition? _currentRecipe;
    private CancellationTokenSource? _stepCts;
    private readonly RecipeExecutionContext _ctx = new();

    public RecipeRunResult Run(RecipeDefinition recipe, bool forceContinueOnError = false, string? approvalMode = null)
    {
        _currentRecipe = recipe;
        _ctx.Variables.Clear();
        _ctx.Notes.Clear();

        var denySensitive = string.Equals(approvalMode, "deny", StringComparison.OrdinalIgnoreCase);
        var reportSensitive = string.Equals(approvalMode, "auto", StringComparison.OrdinalIgnoreCase) || denySensitive;

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
                    TryCleanupOwnedSessions();
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

                if (denySensitive && IsSensitiveStep(step))
                {
                    stepResult = new RecipeStepRunResult(step.Id, step.Kind, false,
                        $"Step '{step.Kind}' requires approval. Use --approve allow to execute.", 0);
                    stepResults.Add(stepResult);
                    failed++;
                    if (ShouldStop(recipe, step, forceContinueOnError, false))
                        break;
                    continue;
                }

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

            if (reportSensitive && IsSensitiveStep(step))
            {
                _ctx.Notes.Add($"[SENSITIVE] {step.Kind} executed (approval mode: {approvalMode})");
            }

            if (stepResult.Success) passed++;
            else
            {
                failed++;
                TryCaptureFailureArtifact(recipe.Name, step, stepResult);
            }

            if (ShouldStop(recipe, step, forceContinueOnError, stepResult.Success))
            {
                if (!stepResult.Success)
                    TryCleanupOwnedSessions();
                break;
            }
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
            "browser.close" => 5000,
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
                "browser.close"          => ExecuteBrowserClose(step, sw),
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
                "profile.load"           => ExecuteProfileLoad(step, sw),
                "extract.visiblefields"  => ExecuteExtractVisibleFields(step, sw),
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

        var browserName = (step.Args?.GetValueOrDefault("browser") ?? "edge").Trim();
        var normalized = browserName.ToLowerInvariant();
        if (normalized is not ("edge" or "chrome"))
            return Fail(step, sw, $"Unsupported browser: {browserName}. Supported: edge, chrome.");

        var forceAccessibility = step.Args?.ContainsKey("forceAccessibility") == true
            || (step.Args?.GetValueOrDefault("forceAccessibility") ?? "") == "true";
        var reuseExisting = step.Args?.GetValueOrDefault("reuseExisting") ?? "";
        var allowReuse = reuseExisting.Equals("true", StringComparison.OrdinalIgnoreCase);
        var useSession = !string.IsNullOrWhiteSpace(step.SaveAs) && !allowReuse;

        var browserTimeoutMs = step.TimeoutMs ?? _currentRecipe?.DefaultTimeoutMs ?? 15000;

        if (allowReuse)
        {
            // Legacy mode: just find any matching window
            var processToFind = BrowserSession.ResolveProcessName(normalized) ?? "msedge";
            var finder = new WindowFinder();
            IntPtr hwnd = IntPtr.Zero;
            var openSw = Stopwatch.StartNew();
            while (openSw.ElapsedMilliseconds < browserTimeoutMs)
            {
                if (_stepCts?.IsCancellationRequested == true) break;
                hwnd = finder.FindWindow(processToFind, null);
                if (hwnd != IntPtr.Zero) break;
                Thread.Sleep(250);
            }

            if (hwnd == IntPtr.Zero)
            {
                sw.Stop();
                return new RecipeStepRunResult(step.Id, step.Kind, false,
                    $"Timeout waiting for {browserName} window (timeout: {browserTimeoutMs}ms)", sw.ElapsedMilliseconds);
            }

            Thread.Sleep(500);
            sw.Stop();

            SaveSessionVars(step, id: "reused", process: processToFind, hwnd: hwnd, url: url, owned: false);

            return new RecipeStepRunResult(step.Id, step.Kind, true,
                $"Reused existing {browserName} window (hwnd: {hwnd})", sw.ElapsedMilliseconds, hwnd.ToInt64());
        }

        // Standard launch through BrowserSession (handles both normal and accessibility)
        var extraArgs = forceAccessibility ? "--force-renderer-accessibility" : null;
        var sessionResult = BrowserSession.Open(url, normalized, browserTimeoutMs,
            useNewWindow: useSession, extraArgs: extraArgs, token: _stepCts?.Token);

        if (!sessionResult.Success || sessionResult.Session == null)
        {
            sw.Stop();
            return new RecipeStepRunResult(step.Id, step.Kind, false,
                sessionResult.Error ?? $"Failed to open {browserName}", sw.ElapsedMilliseconds);
        }

        SaveSessionVars(step,
            id: sessionResult.Session.Id,
            process: sessionResult.Session.ProcessName,
            hwnd: sessionResult.Session.Hwnd,
            url: sessionResult.Session.Url,
            owned: sessionResult.Session.Owned);

        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, true,
            $"Launched {browserName} with {url} (hwnd: {sessionResult.Session.Hwnd}, owned: {sessionResult.Session.Owned})",
            sw.ElapsedMilliseconds, sessionResult.Session.Hwnd.ToInt64());
    }

    private void SaveSessionVars(RecipeStepDefinition step, string id, string process, IntPtr hwnd, string url, bool owned)
    {
        if (string.IsNullOrWhiteSpace(step.SaveAs)) return;
        _ctx.Variables[step.SaveAs] = id;
        _ctx.Variables[step.SaveAs + ".id"] = id;
        _ctx.Variables[step.SaveAs + ".process"] = process;
        _ctx.Variables[step.SaveAs + ".hwnd"] = hwnd.ToString();
        _ctx.Variables[step.SaveAs + ".url"] = url;
        _ctx.Variables[step.SaveAs + ".owned"] = owned ? "true" : "false";
    }

    // ── browser.close ───────────────────────────────────────────────────────
    private RecipeStepRunResult ExecuteBrowserClose(RecipeStepDefinition step, Stopwatch sw)
    {
        if (step.Args?.ContainsKey("hwnd") == true)
            return Fail(step, sw, "browser.close does not support raw hwnd; use session variable via saveAs.");

        if (string.IsNullOrWhiteSpace(step.SaveAs))
            return Fail(step, sw, "browser.close requires 'saveAs' with session variable prefix.");

        // Resolve session prefix: args.session or saveAs
        var sessionPrefix = step.Args?.GetValueOrDefault("session") ?? step.SaveAs;
        if (!_ctx.Variables.TryGetValue(sessionPrefix + ".hwnd", out var hwndStr) || string.IsNullOrWhiteSpace(hwndStr))
            return Fail(step, sw, $"Browser session not found: {sessionPrefix}");

        if (!long.TryParse(hwndStr, out var hwndLong))
            return Fail(step, sw, $"Invalid session hwnd: {hwndStr}");

        var hwnd = new IntPtr(hwndLong);
        if (hwnd == IntPtr.Zero)
            return Fail(step, sw, "Session HWND is zero (session was empty).");

        // Verify ownership from the same prefix
        _ctx.Variables.TryGetValue(sessionPrefix + ".owned", out var owned);
        if (!string.Equals(owned, "true", StringComparison.OrdinalIgnoreCase))
        {
            sw.Stop();
            return new RecipeStepRunResult(step.Id, step.Kind, false,
                "Cannot close browser window not owned by this recipe.", sw.ElapsedMilliseconds);
        }

        // Liveness check
        if (!BrowserSession.IsHwndAlive(hwnd))
        {
            sw.Stop();
            return new RecipeStepRunResult(step.Id, step.Kind, true,
                "Browser session already closed.", sw.ElapsedMilliseconds);
        }

        if (_stepCts?.IsCancellationRequested == true)
        {
            sw.Stop();
            return new RecipeStepRunResult(step.Id, step.Kind, false,
                "Step expired before closing browser.", sw.ElapsedMilliseconds);
        }

        var closeTimeout = step.TimeoutMs ?? 5000;
        var closeResult = BrowserSession.Close(hwnd, closeTimeout);

        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, closeResult.Success, closeResult.Message, sw.ElapsedMilliseconds);
    }

    // ── browser.read ────────────────────────────────────────────────────────
    private RecipeStepRunResult ExecuteBrowserRead(RecipeStepDefinition step, Stopwatch sw)
    {
        var sessionName = step.Args?.GetValueOrDefault("session")?.Trim();
        var hasExplicitSession = !string.IsNullOrWhiteSpace(sessionName);

        if (hasExplicitSession && sessionName!.Contains("{{"))
            return Fail(step, sw, $"browser.read: session must be a plain variable name, not a template. Use \"browser.session\" instead of \"{{{{browser.session}}}}\".");

        if (hasExplicitSession)
        {
            if (!_ctx.Variables.TryGetValue(sessionName + ".process", out _))
                return Fail(step, sw, $"Browser session not found: {sessionName}");
        }

        var sessionHwnd = TryGetSessionHwnd(step);

        if (hasExplicitSession)
        {
            if (sessionHwnd == null || sessionHwnd.Value == IntPtr.Zero)
                return Fail(step, sw, $"browser.read: session '{sessionName}' has no valid HWND.");

            if (!BrowserSession.IsHwndAlive(sessionHwnd.Value))
                return Fail(step, sw, $"browser.read: session HWND {sessionHwnd.Value} is not alive.");
        }

        if (_stepCts?.IsCancellationRequested == true)
            return Fail(step, sw, "Step expired before reading browser.");

        var property = (step.Property ?? "title").ToLowerInvariant();
        if (property is not ("title" or "text" or "url"))
            return Fail(step, sw, $"browser.read unsupported property: '{step.Property}'. Supported: title, text, url.");

        CognitiveSnapshot? snap;
        var reader = new CognitiveSnapshotReader();

        if (sessionHwnd.HasValue && sessionHwnd.Value != IntPtr.Zero)
        {
            // Use HWND directly — never fall back to process-based search
            snap = reader.ReadFromHwnd(sessionHwnd.Value);
        }
        else
        {
            // No session: use process-based search (legacy)
            var proc = ResolveBrowserProcess(step);
            if (string.IsNullOrWhiteSpace(proc))
                return Fail(step, sw, "browser.read requires 'process' field or session variable with .hwnd");
            var win = ResolveBrowserWindow(step);
            snap = reader.Read(proc, win);
        }

        if (snap == null)
        {
            var detail = sessionHwnd.HasValue ? $"hwnd {sessionHwnd.Value}" : $"process '{ResolveBrowserProcess(step)}'";
            return Fail(step, sw, $"browser.read: could not read snapshot ({detail}).");
        }

        string? value = property switch
        {
            "title" => snap.Window.Title,
            "url" => ExtractUrlFromSnapshot(snap),
            "text" => ExtractTextFromSnapshot(snap),
            _ => null
        };

        if (string.IsNullOrWhiteSpace(value))
        {
            var detail = sessionHwnd.HasValue ? $"hwnd {sessionHwnd.Value}" : $"process '{ResolveBrowserProcess(step)}'";
            return Fail(step, sw, $"browser.read: '{property}' not found ({detail}).");
        }

        if (!string.IsNullOrWhiteSpace(step.SaveAs))
        {
            _ctx.Variables[step.SaveAs] = value;
            _ctx.Variables[step.SaveAs + ".raw"] = value;
        }

        sw.Stop();
        var msg = sessionHwnd.HasValue
            ? $"browser.read {property} = '{value}' (session: {sessionName}, hwnd: {sessionHwnd.Value})"
            : $"browser.read {property} = '{value}'";
        return new RecipeStepRunResult(step.Id, step.Kind, true, msg, sw.ElapsedMilliseconds, value);
    }

    private string ResolveBrowserProcess(RecipeStepDefinition step)
    {
        var sessionName = step.Args?.GetValueOrDefault("session")?.Trim();
        if (!string.IsNullOrWhiteSpace(sessionName))
        {
            // Reject template syntax
            if (sessionName.Contains("{{"))
                return ""; // will cause a clear failure below

            if (_ctx.Variables.TryGetValue(sessionName + ".process", out var sp) && !string.IsNullOrWhiteSpace(sp))
                return sp;

            // Session was explicitly declared but not found
            return "";
        }

        // Fallback: saveAs prefix or explicit process field
        if (!string.IsNullOrWhiteSpace(step.SaveAs))
        {
            if (_ctx.Variables.TryGetValue(step.SaveAs + ".process", out var savedProc) && !string.IsNullOrWhiteSpace(savedProc))
                return savedProc;
        }

        return R(step.Process);
    }

    private string? ResolveBrowserWindow(RecipeStepDefinition step)
    {
        var sessionName = step.Args?.GetValueOrDefault("session")?.Trim();
        if (!string.IsNullOrWhiteSpace(sessionName) && !sessionName.Contains("{{"))
        {
            // When using session, prefer window title if set, otherwise null
            // Don't use session URL as window filter (URL is not a window title)
            return R(step.Window); // could be null, which is fine
        }

        if (!string.IsNullOrWhiteSpace(step.SaveAs))
        {
            if (_ctx.Variables.TryGetValue(step.SaveAs + ".url", out var savedUrl) && !string.IsNullOrWhiteSpace(savedUrl))
                return savedUrl;
        }

        return R(step.Window);
    }

    private IntPtr? TryGetSessionHwnd(RecipeStepDefinition step)
    {
        var sessionName = step.Args?.GetValueOrDefault("session")?.Trim();
        if (!string.IsNullOrWhiteSpace(sessionName) && !sessionName.Contains("{{"))
        {
            if (_ctx.Variables.TryGetValue(sessionName + ".hwnd", out var varHwnd) && long.TryParse(varHwnd, out var hwndLong))
                return new IntPtr(hwndLong);
        }

        if (!string.IsNullOrWhiteSpace(step.SaveAs))
        {
            if (_ctx.Variables.TryGetValue(step.SaveAs + ".hwnd", out var savedHwnd) && long.TryParse(savedHwnd, out var sh))
                return new IntPtr(sh);
        }

        var hwndStr = step.Args?.GetValueOrDefault("hwnd");
        if (!string.IsNullOrWhiteSpace(hwndStr) && long.TryParse(hwndStr, out var h))
            return new IntPtr(h);

        return null;
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
        if (_stepCts?.IsCancellationRequested == true)
            return Fail(step, sw, "Step expired before capturing.");

        var mode = (step.Args?.GetValueOrDefault("mode") ?? "window").ToLowerInvariant();
        if (mode is not ("window" or "region" or "element" or "fullscreen"))
            return Fail(step, sw, $"visual.capture unsupported mode: '{mode}'. Supported: window, region, element, fullscreen.");

        var proc = ResolveBrowserProcess(step);
        var win = ResolveBrowserWindow(step);
        var outPath = R(step.Out);

        // Liveness check for session-based captures
        if (TryGetSessionHwnd(step) is { } hwnd && hwnd != IntPtr.Zero && !BrowserSession.IsHwndAlive(hwnd))
            return Fail(step, sw, $"visual.capture: target window (hwnd: {hwnd}) is not alive.");

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

    private void TryCaptureFailureArtifact(string recipeName, RecipeStepDefinition step, RecipeStepRunResult result)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var stepLabel = SanitizeFileName(step.Id ?? step.Kind ?? "unknown");
            var recipeSafe = SanitizeFileName(recipeName);
            var dirName = $"{timestamp}-{recipeSafe}-{stepLabel}";
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "failures", dirName);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var screenshotPath = Path.Combine(dir, "screenshot.png");
            var metadataPath = Path.Combine(dir, "failure.json");
            var snapshotPath = Path.Combine(dir, "snapshot.txt");

            // Try screenshot (best-effort post-mortem)
            string? actualScreenshot = null;
            try
            {
                // Try session-owned browser capture first
                var ownedHwnd = IntPtr.Zero;
                foreach (var key in _ctx.Variables.Keys.Where(k => k.EndsWith(".owned")))
                {
                    if (_ctx.Variables[key] != "true") continue;
                    var pfx = key[..^6];
                    if (_ctx.Variables.TryGetValue(pfx + ".hwnd", out var hs) && long.TryParse(hs, out var hl))
                    {
                        ownedHwnd = new IntPtr(hl);
                        break;
                    }
                }

                VisualCaptureResult captureResult;
                if (ownedHwnd != IntPtr.Zero && BrowserSession.IsHwndAlive(ownedHwnd))
                {
                    captureResult = new VisualCaptureService().Capture(new VisualCaptureRequest(
                        FullScreen: false, AllowFullScreen: false, OutputPath: screenshotPath));
                }
                else
                {
                    captureResult = new VisualCaptureService().Capture(new VisualCaptureRequest(
                        FullScreen: true, AllowFullScreen: true, OutputPath: screenshotPath));
                }

                if (captureResult.Success) actualScreenshot = screenshotPath;
            }
            catch { /* best-effort */ }

            // Try text dump from known variables
            try
            {
                var lines = new List<string>();
                foreach (var kv in _ctx.Variables.Where(v => v.Key.Contains(".text") || v.Key.Contains(".title")))
                    lines.Add($"{kv.Key}: {kv.Value}");
                if (lines.Count > 0)
                    File.WriteAllLines(snapshotPath, lines);
            }
            catch { /* best-effort */ }

            // Gather browser session info
            var sessions = new List<object>();
            foreach (var key in _ctx.Variables.Keys.Where(k => k.EndsWith(".hwnd")))
            {
                var prefix = key[..^5];
                _ctx.Variables.TryGetValue(prefix + ".owned", out var owned);
                _ctx.Variables.TryGetValue(prefix + ".url", out var url);
                sessions.Add(new { Prefix = prefix, Hwnd = _ctx.Variables[key], Owned = owned, Url = url });
            }

            var metadata = new
            {
                Timestamp = DateTime.UtcNow.ToString("o"),
                Recipe = recipeName,
                StepIndex = _currentRecipe?.Steps.IndexOf(step) ?? -1,
                StepId = step.Id,
                StepKind = step.Kind,
                ErrorMessage = result.Message,
                Screenshot = actualScreenshot,
                SnapshotDump = File.Exists(snapshotPath) ? snapshotPath : (string?)null,
                Sessions = sessions,
            };

            File.WriteAllText(metadataPath,
                System.Text.Json.JsonSerializer.Serialize(metadata,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            _ctx.Notes.Add($"[ARTIFACT] {dirName} saved (screenshot: {actualScreenshot != null}, metadata: true)");
        }
        catch
        {
            // Best-effort: never let artifact capture mask the original error
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (invalid.Contains(chars[i]))
                chars[i] = '-';
        }
        return new string(chars);
    }

    private void TryCleanupOwnedSessions()
    {
        var cleaned = new List<string>();
        try
        {
            foreach (var key in _ctx.Variables.Keys.Where(k => k.EndsWith(".owned")))
            {
                if (_ctx.Variables[key] != "true") continue;

                var prefix = key[..^6];
                if (_ctx.Variables.TryGetValue(prefix + ".hwnd", out var hwndStr) &&
                    long.TryParse(hwndStr, out var hwndLong))
                {
                    var hwnd = new IntPtr(hwndLong);
                    if (BrowserSession.IsHwndAlive(hwnd))
                    {
                        var closeResult = BrowserSession.Close(hwnd, 3000);
                        cleaned.Add($"{prefix}: {closeResult.Message}");
                        _ctx.Notes.Add($"[CLEANUP] {closeResult.Message}");
                    }
                    else
                    {
                        cleaned.Add($"{prefix}: already closed");
                    }
                }
            }
        }
        catch { /* best-effort */ }

        // Write cleanup result to artifact if available
        try
        {
            if (cleaned.Count > 0)
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var dir = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "failures");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var cleanupPath = Path.Combine(dir, $"{timestamp}-cleanup.json");
                File.WriteAllText(cleanupPath,
                    System.Text.Json.JsonSerializer.Serialize(new { Cleaned = cleaned },
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }
        }
        catch { /* best-effort */ }
    }

    private static bool IsSensitiveStep(RecipeStepDefinition step)
    {
        var kind = (step.Kind ?? "").ToLowerInvariant();
        return kind is "actv.invoke" or "actv.type" or "key" or "app.open" or "browser.open" or "browser.close";
    }

    /// <summary>Validate template variables in a recipe against known/provided sources.</summary>
    public static IReadOnlyList<string> ValidateTemplates(RecipeDefinition recipe)
    {
        var warnings = new List<string>();
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Built-in runtime variables
        known.Add("runtime.timestamp");
        known.Add("runtime.date");
        known.Add("runtime.temp");

        // Recipe-defined variables
        if (recipe.Variables != null)
        {
            foreach (var vk in recipe.Variables.Keys)
                known.Add(vk);
        }

        // Simulate step outputs: profile.load and browser.open with saveAs produce variables
        foreach (var step in recipe.Steps)
        {
            var kind = (step.Kind ?? "").ToLowerInvariant();

            if (kind == "profile.load" && !string.IsNullOrWhiteSpace(step.SaveAs))
            {
                var prefix = step.SaveAs;
                known.Add(prefix);
                foreach (var suffix in new[] { ".id", ".type", ".displayName", ".url", ".process",
                    ".expected.titleContains", ".expected.textContains",
                    ".read.preferredProperty", ".read.fallbackProperty",
                    ".safety.allowForms", ".safety.allowLogin", ".safety.allowPurchase", ".safety.allowSensitiveActions" })
                    known.Add(prefix + suffix);
            }

            if ((kind == "browser.open" || kind == "browser.read" || kind == "visual.capture") &&
                !string.IsNullOrWhiteSpace(step.SaveAs))
            {
                var prefix = step.SaveAs;
                known.Add(prefix);
                foreach (var suffix in new[] { ".id", ".process", ".hwnd", ".url", ".owned",
                    ".path", ".width", ".height", ".timestamp", ".raw" })
                    known.Add(prefix + suffix);
            }

            if (kind == "snapshot.read" && !string.IsNullOrWhiteSpace(step.SaveAs))
            {
                known.Add(step.SaveAs);
                known.Add(step.SaveAs + "Raw");
            }

            if (!string.IsNullOrWhiteSpace(step.SaveAs))
                known.Add(step.SaveAs);
        }

        // Scan all steps for template references
        var templateRegex = new System.Text.RegularExpressions.Regex(@"\{\{([^}]+)\}\}");
        foreach (var step in recipe.Steps)
        {
            var rawStep = System.Text.Json.JsonSerializer.Serialize(step);
            var matches = templateRegex.Matches(rawStep);
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                var varName = m.Groups[1].Value.Trim();
                if (!known.Contains(varName) && !known.Any(k => varName.StartsWith(k + ".", StringComparison.OrdinalIgnoreCase)))
                {
                    var msg = $"Unknown template variable '{{{{{varName}}}}}' in step '{step.Id ?? step.Kind}'. " +
                              "It may be misspelled or depend on a missing profile.load/profile step.";
                    if (!warnings.Contains(msg))
                        warnings.Add(msg);
                }
            }
        }

        return warnings;
    }

    private RecipeStepRunResult ExecuteProfileLoad(RecipeStepDefinition step, Stopwatch sw)
    {
        var path = R(step.Path);
        if (string.IsNullOrWhiteSpace(path))
            return Fail(step, sw, "profile.load requires 'path' field.");

        var loader = new ProfileLoader();
        var result = loader.Load(path);

        if (!result.Success || result.Profile == null)
        {
            sw.Stop();
            return new RecipeStepRunResult(step.Id, step.Kind, false,
                result.Error ?? "Failed to load profile.", sw.ElapsedMilliseconds);
        }

        if (!string.IsNullOrWhiteSpace(step.SaveAs))
        {
            var prefix = step.SaveAs;
            var vars = loader.ToVariables(result.Profile, prefix);
            foreach (var kv in vars)
                _ctx.Variables[kv.Key] = kv.Value;
            _ctx.Variables[step.SaveAs] = result.Profile.Id;
        }

        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, true,
            $"Loaded profile '{result.Profile.Id}' ({result.Profile.Type})", sw.ElapsedMilliseconds, result.Profile);
    }

    private RecipeStepRunResult ExecuteExtractVisibleFields(RecipeStepDefinition step, Stopwatch sw)
    {
        var mode = (step.Args?.GetValueOrDefault("mode") ?? "commercialProduct").ToLowerInvariant();
        if (mode != "commercialproduct")
            return Fail(step, sw, $"extract.visibleFields unsupported mode: '{mode}'. Supported: commercialProduct.");

        var prefix = step.SaveAs ?? "extract";
        var title = _ctx.Variables.TryGetValue("bt", out var t) ? t : (_ctx.Variables.TryGetValue("browser.title", out var bt) ? bt : "");
        var text = _ctx.Variables.TryGetValue("btext", out var tx) ? tx : (_ctx.Variables.TryGetValue("browser.text", out var btx) ? btx : "");

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(text))
            return Fail(step, sw, "extract.visibleFields: no title/text variables found. Run browser.read first.");

        var result = ExtractCommercialFields(title, text, prefix);

        foreach (var kv in result)
            _ctx.Variables[kv.Key] = kv.Value;

        sw.Stop();
        var priceStr = result.TryGetValue(prefix + ".priceCandidate", out var pc) ? pc : "null";
        return new RecipeStepRunResult(step.Id, step.Kind, true,
            $"Extracted fields: title=yes, price={priceStr}, confidence={result[prefix + ".confidence"]}", sw.ElapsedMilliseconds, result);
    }

    internal static Dictionary<string, string> ExtractCommercialFields(string title, string text, string prefix)
    {
        var vars = new Dictionary<string, string>();
        var combined = title + " | " + text;

        // Title candidate
        var titleCandidate = title.Trim();
        if (!string.IsNullOrWhiteSpace(titleCandidate))
            vars[prefix + ".titleCandidate"] = titleCandidate;

        // Price detection
        var priceMatch = System.Text.RegularExpressions.Regex.Match(combined, @"\$[\s]*([\d]{1,3}(?:\.[\d]{3})*(?:,[\d]{2})?)");
        if (priceMatch.Success)
        {
            vars[prefix + ".priceCandidate"] = priceMatch.Value.Trim();
            vars[prefix + ".currencyCandidate"] = "ARS";
        }
        else
        {
            vars[prefix + ".priceCandidate"] = "null";
            vars[prefix + ".currencyCandidate"] = "null";
        }

        // Shipping
        bool hasShipping = combined.Contains("Envío", StringComparison.OrdinalIgnoreCase) ||
                          combined.Contains("envío", StringComparison.OrdinalIgnoreCase) ||
                          combined.Contains("Llega", StringComparison.OrdinalIgnoreCase) ||
                          combined.Contains("Retiro", StringComparison.OrdinalIgnoreCase);
        vars[prefix + ".shippingCandidate"] = hasShipping ? "detected" : "null";

        // Sensitive words
        var sensitive = new List<string>();
        if (combined.Contains("Comprar", StringComparison.OrdinalIgnoreCase) || combined.Contains("comprar", StringComparison.OrdinalIgnoreCase))
            sensitive.Add("comprar");
        if (combined.Contains("carrito", StringComparison.OrdinalIgnoreCase))
            sensitive.Add("carrito");
        if (combined.Contains("Pagar", StringComparison.OrdinalIgnoreCase) || combined.Contains("pagar", StringComparison.OrdinalIgnoreCase))
            sensitive.Add("pagar");
        if (combined.Contains("Iniciar sesión", StringComparison.OrdinalIgnoreCase) || combined.Contains("iniciar sesión", StringComparison.OrdinalIgnoreCase))
            sensitive.Add("login");
        vars[prefix + ".sensitiveWordsDetected"] = sensitive.Count > 0 ? string.Join(", ", sensitive) : "null";

        // Confidence
        bool hasTitle = !string.IsNullOrWhiteSpace(titleCandidate);
        bool hasPrice = priceMatch.Success;
        vars[prefix + ".confidence"] = (hasTitle && hasPrice) ? "high" : (hasTitle ? "medium" : "low");

        // Raw evidence (truncated)
        var evidence = combined.Length > 400 ? combined[..400] + "..." : combined;
        vars[prefix + ".rawEvidence"] = evidence;

        return vars;
    }
}
