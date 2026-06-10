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
    private readonly RecipeExecutionContext _ctx = new();

    public RecipeRunResult Run(RecipeDefinition recipe, bool forceContinueOnError = false)
    {
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
                if (ShouldStop(recipe, step, forceContinueOnError))
                    break;
                continue;
            }

            RecipeStepRunResult stepResult;
            try
            {
                stepResult = ExecuteStep(step);
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
                "wait"                   => ExecuteWait(step, sw),
                "actv.type"              => ExecuteActvType(step, sw),
                "actv.invoke"            => ExecuteActvInvoke(step, sw),
                "key"                    => ExecuteKey(step, sw),
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
        var fresh = step.Args?.GetValueOrDefault("fresh") == "true";

        if (fresh)
        {
            try { Process.Start(new ProcessStartInfo("taskkill", "/f /im msedge.exe") { UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(3000); }
            catch { }
            Thread.Sleep(1500);
        }

        var fileUrl = url;
        if (!fileUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !fileUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !fileUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            fileUrl = new Uri(Path.GetFullPath(fileUrl)).AbsoluteUri;
        }

        var extra = forceAccessibility ? "--force-renderer-accessibility " : "";
        var urlArg = fresh
            ? $"--app=\"{fileUrl}\""
            : $"\"{fileUrl}\"";
        var browserArgs = $"{extra}{urlArg}";

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

        var waitMs = fresh ? 4000 : 1500;
        Thread.Sleep(waitMs);

        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, true,
            $"Launched {browserName} with {url}", sw.ElapsedMilliseconds);
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

        if (string.IsNullOrWhiteSpace(proc) && string.IsNullOrWhiteSpace(win))
            return Fail(step, sw, "wait requires 'process' or 'window' field.");

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(role) && string.IsNullOrWhiteSpace(titleContains))
            return Fail(step, sw, "wait requires 'name', 'role', or 'titleContains'.");

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
                    SaveOutput(step, matchObj);
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
    private RecipeStepRunResult ExecuteActvType(RecipeStepDefinition step, Stopwatch sw)
    {
        var targetRef = BuildTargetRef(step);
        if (targetRef == null)
            return Fail(step, sw, "actv.type requires a selector (role, name, automationId, or class).");

        var text = R(step.Text);

        var req = new ActionRequest("type", targetRef, text, R(step.Process), R(step.Window));

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

        var req = new ActionRequest("invoke", targetRef, null, R(step.Process), R(step.Window));

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

        var req = new ActionRequest("key", "role:Window", text, R(step.Process), R(step.Window));
        var result = new UiaActionExecutor().Execute(req);
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
        var timeout = step.TimeoutMs ?? 5000;
        var interval = Math.Max(100, step.IntervalMs ?? 500);

        while (sw.ElapsedMilliseconds < timeout)
        {
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
