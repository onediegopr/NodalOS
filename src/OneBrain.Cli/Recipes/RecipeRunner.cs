using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using OneBrain.Actions.Uia;
using OneBrain.Cli.Accessibility;
using OneBrain.Cli.Browser;
using OneBrain.Cli.Safety;
using OneBrain.Core.Approval;
using OneBrain.Core.Extraction;
using OneBrain.Core.Execution;
using OneBrain.Core.Identity;
using OneBrain.Core.Profiles;
using OneBrain.Core.Safety;
using OneBrain.Core.Actions;
using OneBrain.Core.Contracts;
using OneBrain.Core.Models;
using OneBrain.Core.Recipes;
using OneBrain.Core.Selectors;
using OneBrain.Core.Selectors.Web;
using OneBrain.Core.Visual;
using OneBrain.Observation;
using OneBrain.Observation.Input;
using OneBrain.Observation.Uia;
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
    private static Func<IntPtr, string, string, int, WebTargetResult>? s_targetObserveResolverOverride = null;
    private static Func<string, string?, string?, DesktopTargetObservationResult>? s_targetObserveDesktopOverride = null;
    private static Func<IUiaPatternExecutor>? s_safeClickPatternExecutorFactoryOverride = null;
    private static Func<IUiaReadExecutor>? s_safeReadExecutorFactoryOverride = null;
    private static Func<IUiaTypeExecutor>? s_safeTypeExecutorFactoryOverride = null;
    private static Func<IDesktopOwnershipMonitor>? s_safeClickOwnershipMonitorFactoryOverride = null;
    private static Func<WebTargetResult, string, ActionResult>? s_safeClickLegacyWebActionOverride = null;
    private static Func<SafeClickDefaultMode>? s_safeClickFsmDefaultModeOverride = null;
    private RecipeDefinition? _currentRecipe;
    private CancellationTokenSource? _stepCts;
    private readonly RecipeExecutionContext _ctx = new();
    private readonly Dictionary<string, SafeClickShadowReadiness> _safeClickShadowReadiness = new(StringComparer.OrdinalIgnoreCase);

    // HITO-147 — local-first default-routing counters (reset per run; emitted as additive vars).
    private int _safeClickDefaultFsmEnabledCount;
    private int _safeClickDefaultFsmRoutedCount;
    private int _safeClickDefaultFsmEligibleButNotEnabledCount;
    private int _safeClickDefaultFsmBlockedCount;
    private int _safeClickExplicitLegacyOptOutCount;
    private int _safeClickDesktopExcludedFromDefaultCount;
    private int _safeClickUnknownDispatchPathBlockedCount;
    private int _safeClickRuntimeStabilityCheckedCount;
    private int _safeClickRuntimeStableCount;
    private int _safeClickRuntimeChangedCount;
    private int _safeClickRuntimeMissingCount;
    private int _safeClickReobserveAttemptedCount;
    private int _safeClickReobserveSucceededCount;
    private int _safeClickReobserveChangedCount;
    private int _safeClickDefaultBlockedByStaleIdentityCount;
    private int _safeClickDefaultBlockedByMissingIdentityCount;
    private int _safeClickDesktopOptInRoutedCount;
    private int _safeClickDesktopOptInBlockedCount;
    private int _safeClickDesktopDefaultFsmEnabledCount;
    private int _safeClickDesktopDefaultFsmRoutedCount;
    private int _safeClickDesktopDefaultEligibleButNotEnabledCount;
    private int _safeClickDesktopDefaultBlockedCount;
    private int _safeClickDesktopDefaultBlockedByStaleIdentityCount;
    private int _safeClickAllEligibleModeEnabledCount;
    private int _safeClickDefaultFsmScopeWebCount;
    private int _safeClickDefaultFsmScopeDesktopCount;
    private int _safeClickLegacyExplicitOptOutCompliantCount;
    private int _safeClickLegacyOptOutMissingOwnerCount;
    private int _safeClickLegacyOptOutMissingReasonCount;
    private int _safeClickLegacyOptOutMissingReviewByCount;
    private int _safeClickLegacyOptOutNonCompliantCount;
    private int _safeClickLegacyDeprecationWarningCount;
    private int _safeClickLegacyExecutionBlockedCount;
    private int _safeClickLegacyDispatchRejectedCount;
    private int _safeClickIneligibleBlockedAfterRetirementCount;
    private int _safeClickSafeExecutorRequiredCount;
    private int _safeClickDefaultLegacyUseCount;
    private int _safeClickDefaultElClickUseCount;
    private int _safeClickDefaultUiaActionExecutorUseCount;
    private int _safeClickDefaultUnsafeFallbackUseCount;
    private readonly HashSet<string> _safeClickDefaultLegacyPrefixes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _safeClickDefaultElClickCounted = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _safeClickDefaultUiaActionExecutorCounted = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _safeClickDefaultUnsafeFallbackCounted = new(StringComparer.OrdinalIgnoreCase);

    public RecipeRunResult Run(RecipeDefinition recipe, bool forceContinueOnError = false, string? approvalMode = null)
    {
        _currentRecipe = recipe;
        _ctx.Variables.Clear();
        _ctx.Notes.Clear();
        _safeClickShadowReadiness.Clear();
        _safeClickDefaultFsmEnabledCount = 0;
        _safeClickDefaultFsmRoutedCount = 0;
        _safeClickDefaultFsmEligibleButNotEnabledCount = 0;
        _safeClickDefaultFsmBlockedCount = 0;
        _safeClickExplicitLegacyOptOutCount = 0;
        _safeClickDesktopExcludedFromDefaultCount = 0;
        _safeClickUnknownDispatchPathBlockedCount = 0;
        _safeClickRuntimeStabilityCheckedCount = 0;
        _safeClickRuntimeStableCount = 0;
        _safeClickRuntimeChangedCount = 0;
        _safeClickRuntimeMissingCount = 0;
        _safeClickReobserveAttemptedCount = 0;
        _safeClickReobserveSucceededCount = 0;
        _safeClickReobserveChangedCount = 0;
        _safeClickDefaultBlockedByStaleIdentityCount = 0;
        _safeClickDefaultBlockedByMissingIdentityCount = 0;
        _safeClickDesktopOptInRoutedCount = 0;
        _safeClickDesktopOptInBlockedCount = 0;
        _safeClickDesktopDefaultFsmEnabledCount = 0;
        _safeClickDesktopDefaultFsmRoutedCount = 0;
        _safeClickDesktopDefaultEligibleButNotEnabledCount = 0;
        _safeClickDesktopDefaultBlockedCount = 0;
        _safeClickDesktopDefaultBlockedByStaleIdentityCount = 0;
        _safeClickAllEligibleModeEnabledCount = 0;
        _safeClickDefaultFsmScopeWebCount = 0;
        _safeClickDefaultFsmScopeDesktopCount = 0;
        _safeClickLegacyExplicitOptOutCompliantCount = 0;
        _safeClickLegacyOptOutMissingOwnerCount = 0;
        _safeClickLegacyOptOutMissingReasonCount = 0;
        _safeClickLegacyOptOutMissingReviewByCount = 0;
        _safeClickLegacyOptOutNonCompliantCount = 0;
        _safeClickLegacyDeprecationWarningCount = 0;
        _safeClickLegacyExecutionBlockedCount = 0;
        _safeClickLegacyDispatchRejectedCount = 0;
        _safeClickIneligibleBlockedAfterRetirementCount = 0;
        _safeClickSafeExecutorRequiredCount = 0;
        _safeClickDefaultLegacyUseCount = 0;
        _safeClickDefaultElClickUseCount = 0;
        _safeClickDefaultUiaActionExecutorUseCount = 0;
        _safeClickDefaultUnsafeFallbackUseCount = 0;
        _safeClickDefaultLegacyPrefixes.Clear();
        _safeClickDefaultElClickCounted.Clear();
        _safeClickDefaultUiaActionExecutorCounted.Clear();
        _safeClickDefaultUnsafeFallbackCounted.Clear();

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

        SetSafeClickMigrationSummaryVars();

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
                "app.close"              => ExecuteAppClose(step, sw),
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
                "extract.productevidence" => ExecuteExtractProductEvidence(step, sw),
                "artifact.writeproductevidence" => ExecuteWriteProductEvidenceArtifact(step, sw),
                "artifact.summarizeproductevidence" => ExecuteSummarizeProductEvidenceArtifacts(step, sw),
                "report.writeproductevidencemarkdown" => ExecuteWriteProductEvidenceMarkdownReport(step, sw),
                "report.writeproductevidencehtml" => ExecuteWriteProductEvidenceHtmlReport(step, sw),
                "discover.actionableelements" => ExecuteDiscoverActionableElements(step, sw),
                "plan.safenavigation"    => ExecutePlanSafeNavigation(step, sw),
                "preflight.click"         => ExecutePreflightClick(step, sw),
                "target.observe"          => ExecuteTargetObserve(step, sw),
                "target.observe.desktop"  => ExecuteTargetObserveDesktop(step, sw),
                "approval.manifest"       => ExecuteApprovalManifest(step, sw),
                "safe.read"               => ExecuteSafeRead(step, sw),
                "safe.type"               => ExecuteSafeType(step, sw),
                "safe.click"              => ExecuteSafeClick(step, sw),
                "diagnose.msaa"           => ExecuteDiagnoseMsaa(step, sw),
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

    // ── app.close ─────────────────────────────────────────────────────────────
    private RecipeStepRunResult ExecuteAppClose(RecipeStepDefinition step, Stopwatch sw)
    {
        var app = (step.App ?? step.Args?.GetValueOrDefault("app") ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(app))
            return Fail(step, sw, "app.close requires 'app' field (calculator|notepad|explorer).");

        var prefix = step.SaveAs ?? "appClose";

        var (proc, titleContains, cls) = app switch
        {
            "calculator" => ("ApplicationFrameHost", "alculador", ""),
            "notepad"    => ("Notepad", "Notepad", ""),
            "explorer"   => ("explorer", "", ""),
            _ => (null, null, null)
        };

        if (proc == null)
        {
            SetAppCloseVars(prefix, false, app, "unsupported app", 0);
            sw.Stop();
            return Fail(step, sw, $"app.close unsupported app: {app}");
        }

        if (app == "explorer")
        {
            SetAppCloseVars(prefix, false, app, "blocked: explorer close requires owned app session tracking", 0);
            sw.Stop();
            return new RecipeStepRunResult(step.Id, step.Kind, false,
                "app.close explorer blocked: Explorer close requires owned session tracking.", sw.ElapsedMilliseconds);
        }

        var finder = new OneBrain.Observation.Windows.WindowFinder();
        var windows = finder.FindAllWindows(proc, titleContains);

        if (windows.Count == 0)
        {
            SetAppCloseVars(prefix, true, app, "not found (already closed)", 0);
            sw.Stop();
            return new RecipeStepRunResult(step.Id, step.Kind, true,
                $"App '{app}' not found (already closed).", sw.ElapsedMilliseconds);
        }

        if (windows.Count > 1 && app != "explorer")
        {
            SetAppCloseVars(prefix, false, app, $"ambiguous: {windows.Count} windows", 0);
            sw.Stop();
            return new RecipeStepRunResult(step.Id, step.Kind, false,
                $"app.close: ambiguous target ({windows.Count} {app} windows).", sw.ElapsedMilliseconds);
        }

        // Close each matching window
        var closed = 0;
        var failures = new List<string>();
        foreach (var hwnd in windows)
        {
            try
            {
                var closeResult = Browser.BrowserSession.Close(hwnd, 3000);
                if (closeResult.Success)
                    closed++;
                else
                    failures.Add(closeResult.Message);
            }
            catch (Exception ex)
            {
                failures.Add(ex.Message);
            }
        }

        var success = closed > 0 && failures.Count == 0;
        var reason = success ? "closed" : failures.Count > 0 ? string.Join("; ", failures) : "failed";
        SetAppCloseVars(prefix, success, app, reason, closed);
        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, success,
            success
                ? $"App '{app}' closed ({closed} window(s))."
                : $"app.close failed for '{app}': {reason}",
            sw.ElapsedMilliseconds);
    }

    private void SetAppCloseVars(string prefix, bool success, string target, string reason, int count)
    {
        _ctx.Variables[prefix + ".success"] = success ? "true" : "false";
        _ctx.Variables[prefix + ".target"] = target;
        _ctx.Variables[prefix + ".method"] = "WM_CLOSE";
        _ctx.Variables[prefix + ".closedCount"] = count.ToString();
        _ctx.Variables[prefix + ".reason"] = reason;
        _ctx.Variables[prefix + ".warning"] = success ? "" : "app may still be open";
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
        if (normalized is not ("edge" or "chrome" or "firefox"))
            return Fail(step, sw, $"Unsupported browser: {browserName}. Supported: edge, chrome, firefox.");

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

        var timeout = step.TimeoutMs ?? _currentRecipe?.DefaultTimeoutMs ?? 10000;
        var interval = Math.Max(100, step.IntervalMs ?? 500);
        var forcePoll = step.Poll == true ||
            (step.Args?.TryGetValue("poll", out var pollValue) == true &&
             bool.TryParse(pollValue, out var parsedPoll) &&
             parsedPoll);

        var searchCriteria = new List<string>();
        if (!string.IsNullOrEmpty(proc)) searchCriteria.Add($"process: '{proc}'");
        if (!string.IsNullOrEmpty(win)) searchCriteria.Add($"window: '{win}'");
        if (!string.IsNullOrEmpty(name)) searchCriteria.Add($"name: '{name}'");
        if (!string.IsNullOrEmpty(role)) searchCriteria.Add($"role: '{role}'");
        if (!string.IsNullOrEmpty(automationId)) searchCriteria.Add($"automationId: '{automationId}'");
        if (!string.IsNullOrEmpty(titleContains)) searchCriteria.Add($"titleContains: '{titleContains}'");
        if (searchCriteria.Count == 0 || searchCriteria.All(c => c.StartsWith("process:", StringComparison.Ordinal) || c.StartsWith("window:", StringComparison.Ordinal)))
            searchCriteria.Add("window-open");
        
        var criteriaStr = string.Join(", ", searchCriteria);

        if (string.IsNullOrEmpty(titleContains) &&
            string.IsNullOrEmpty(name) &&
            string.IsNullOrEmpty(role) &&
            string.IsNullOrEmpty(automationId))
        {
            var windowOpenResult = UiaEventWaiter.WaitForWindowOpen(
                proc,
                win,
                timeout,
                interval,
                _stepCts?.Token ?? CancellationToken.None,
                forcePolling: forcePoll);

            if (windowOpenResult.Success)
            {
                SaveOutput(step, windowOpenResult.Match);
                return new RecipeStepRunResult(step.Id, step.Kind, true,
                    $"Window opened after {windowOpenResult.ElapsedMs}ms ({windowOpenResult.Attempts} attempts, usedEvents={windowOpenResult.UsedEvents}, fallbackPolling={windowOpenResult.FellBackToPolling}): [{criteriaStr}]",
                    windowOpenResult.ElapsedMs,
                    windowOpenResult.Match);
            }

            return new RecipeStepRunResult(step.Id, step.Kind, false,
                $"Timeout after {timeout}ms ({windowOpenResult.Attempts} attempts, usedEvents={windowOpenResult.UsedEvents}, fallbackPolling={windowOpenResult.FellBackToPolling}) waiting for: [{criteriaStr}]. Last title: '{windowOpenResult.LastWindowTitle}'",
                windowOpenResult.ElapsedMs);
        }

        if (!string.IsNullOrEmpty(titleContains) &&
            string.IsNullOrEmpty(name) &&
            string.IsNullOrEmpty(role) &&
            string.IsNullOrEmpty(automationId))
        {
            var eventResult = UiaEventWaiter.WaitForTitleContains(
                proc,
                win,
                titleContains,
                timeout,
                interval,
                _stepCts?.Token ?? CancellationToken.None,
                forcePolling: forcePoll);

            if (eventResult.Success)
            {
                SaveOutput(step, eventResult.Match);
                return new RecipeStepRunResult(step.Id, step.Kind, true,
                    $"Found after {eventResult.ElapsedMs}ms ({eventResult.Attempts} attempts, usedEvents={eventResult.UsedEvents}, fallbackPolling={eventResult.FellBackToPolling}): [{criteriaStr}]",
                    eventResult.ElapsedMs,
                    eventResult.Match);
            }

            return new RecipeStepRunResult(step.Id, step.Kind, false,
                $"Timeout after {timeout}ms ({eventResult.Attempts} attempts, usedEvents={eventResult.UsedEvents}, fallbackPolling={eventResult.FellBackToPolling}) waiting for: [{criteriaStr}]. Last title: '{eventResult.LastWindowTitle}'",
                eventResult.ElapsedMs);
        }

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
        var legacyDecision = EvaluateLegacyExecution(step, "actv.type");
        if (!legacyDecision.Allowed)
            return BlockLegacyExecution(step, sw, legacyDecision);

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
        var legacyDecision = EvaluateLegacyExecution(step, "actv.invoke");
        if (!legacyDecision.Allowed)
            return BlockLegacyExecution(step, sw, legacyDecision);

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
        var legacyDecision = EvaluateLegacyExecution(step, "key");
        if (!legacyDecision.Allowed)
            return BlockLegacyExecution(step, sw, legacyDecision);

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
    private LegacyExecutionDecision EvaluateLegacyExecution(RecipeStepDefinition step, string surface)
    {
        return LegacyExecutionGuard.Evaluate(
            step.Kind,
            surface,
            LegacyExecutionGuard.ReadProcessEnvironment(),
            IsExplicitLegacyOptIn(step));
    }

    private bool IsExplicitLegacyOptIn(RecipeStepDefinition step)
    {
        var value = ResolveArg(step, "allowLegacyActions");
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "1", StringComparison.Ordinal);
    }

    private RecipeStepRunResult BlockLegacyExecution(
        RecipeStepDefinition step,
        Stopwatch sw,
        LegacyExecutionDecision decision)
    {
        var prefix = string.IsNullOrWhiteSpace(step.SaveAs) ? "legacy" : step.SaveAs!;

        _ctx.Variables["legacy.success"] = "false";
        _ctx.Variables["legacy.blocked"] = "true";
        _ctx.Variables["legacy.stepKind"] = decision.StepKind;
        _ctx.Variables["legacy.surface"] = decision.Surface;
        _ctx.Variables["legacy.reason"] = decision.Reason;
        _ctx.Variables["legacy.optInRequired"] = "true";
        _ctx.Variables["legacy.guard.allowed"] = "false";
        _ctx.Variables["legacy.guard.isQuarantined"] = decision.IsQuarantined ? "true" : "false";

        _ctx.Variables[prefix + ".legacyBlocked"] = "true";
        _ctx.Variables[prefix + ".success"] = "false";
        _ctx.Variables[prefix + ".failureKind"] = FailureKind.PolicyDenied.ToString();
        _ctx.Variables[prefix + ".reason"] = decision.Reason;
        _ctx.Variables[prefix + ".legacy.stepKind"] = decision.StepKind;
        _ctx.Variables[prefix + ".legacy.surface"] = decision.Surface;
        _ctx.Variables[prefix + ".legacy.guard.allowed"] = "false";

        sw.Stop();
        return new RecipeStepRunResult(
            step.Id,
            step.Kind,
            false,
            $"{step.Kind}: legacy execution blocked: {decision.Reason}",
            sw.ElapsedMilliseconds,
            decision);
    }

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
        return SensitiveActionClassifier.IsSensitiveStepKind(step.Kind);
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

    private RecipeStepRunResult ExecuteDiscoverActionableElements(RecipeStepDefinition step, Stopwatch sw)
    {
        var text = step.Args?.GetValueOrDefault("from") ?? _ctx.Variables.GetValueOrDefault("btext", "");
        if (text is null or "") text = _ctx.Variables.GetValueOrDefault("browser.text", "");
        if (string.IsNullOrWhiteSpace(text))
            return Fail(step, sw, "discover.actionableElements: no text found. Run browser.read text first.");

        var result = CommercialActionDiscovery.Discover(text);
        var prefix = step.SaveAs ?? "actions";

        _ctx.Variables[prefix + ".count"] = result.Count.ToString();
        _ctx.Variables[prefix + ".safeCount"] = result.SafeCount.ToString();
        _ctx.Variables[prefix + ".navCount"] = result.NavCount.ToString();
        _ctx.Variables[prefix + ".dangerousCount"] = result.DangerousCount.ToString();
        _ctx.Variables[prefix + ".authCount"] = result.AuthCount.ToString();
        _ctx.Variables[prefix + ".paymentCount"] = result.PaymentCount.ToString();
        _ctx.Variables[prefix + ".unknownCount"] = result.UnknownCount.ToString();
        _ctx.Variables[prefix + ".highestRisk"] = result.HighestRisk;
        _ctx.Variables[prefix + ".hasDangerous"] = result.HasDangerous ? "true" : "false";
        _ctx.Variables[prefix + ".hasAuth"] = result.HasAuth ? "true" : "false";
        _ctx.Variables[prefix + ".hasPayment"] = result.HasPayment ? "true" : "false";
        _ctx.Variables[prefix + ".summary"] = result.Summary;
        _ctx.Variables[prefix + ".itemsJson"] = CommercialActionDiscovery.SerializeItems(result.Items);
        _ctx.Variables[prefix + ".rawEvidence"] = result.RawEvidence ?? "";

        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, true,
            $"Discovered {result.Count} actionable elements. Highest risk: {result.HighestRisk}",
            sw.ElapsedMilliseconds, result);
    }

    private RecipeStepRunResult ExecuteExtractProductEvidence(RecipeStepDefinition step, Stopwatch sw)
    {
        var prefix = step.SaveAs ?? "productEvidence";
        var title = ResolveArg(step, "title") ?? _ctx.Variables.GetValueOrDefault("bt", _ctx.Variables.GetValueOrDefault("browser.title", ""));
        var text = ResolveArg(step, "text") ?? _ctx.Variables.GetValueOrDefault("btext", _ctx.Variables.GetValueOrDefault("browser.text", ""));

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(text))
            return Fail(step, sw, "extract.productEvidence: no title/text variables found. Run browser.read first.");

        var input = new ProductEvidenceInput
        {
            SourceUrl = ResolveArg(step, "sourceUrl") ?? ResolveFirstVariableValue(".url"),
            SourceProfileId = ResolveArg(step, "sourceProfileId") ?? ResolveFirstVariableValue(".id"),
            PageTitle = title,
            VisibleText = text,
            CategoryHint = ResolveArg(step, "category"),
            RawSignals = ResolveArg(step, "rawSignals")
        };

        var evidence = ProductEvidenceExtractor.Extract(input);
        SetProductEvidenceVars(prefix, evidence);

        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, true,
            $"Product evidence: status={evidence.ExtractionStatus}, confidence={evidence.ExtractionConfidence}, product={evidence.ProductName ?? "null"}",
            sw.ElapsedMilliseconds, evidence);
    }

    private string? ResolveArg(RecipeStepDefinition step, string key)
    {
        if (step.Args == null) return null;

        foreach (var kv in step.Args)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                return R(kv.Value);
        }

        return null;
    }

    private string? ResolveFirstVariableValue(string suffix)
    {
        var kv = _ctx.Variables.FirstOrDefault(v => v.Key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(kv.Value) ? null : kv.Value;
    }

    private void SetProductEvidenceVars(string prefix, ProductEvidence evidence)
    {
        _ctx.Variables[prefix + ".sourceUrl"] = evidence.SourceUrl ?? "null";
        _ctx.Variables[prefix + ".sourceProfileId"] = evidence.SourceProfileId ?? "null";
        _ctx.Variables[prefix + ".pageTitle"] = evidence.PageTitle ?? "null";
        _ctx.Variables[prefix + ".productName"] = evidence.ProductName ?? "null";
        _ctx.Variables[prefix + ".brand"] = evidence.Brand ?? "null";
        _ctx.Variables[prefix + ".sku"] = evidence.Sku ?? "null";
        _ctx.Variables[prefix + ".category"] = evidence.Category ?? "null";
        _ctx.Variables[prefix + ".description"] = evidence.Description ?? "null";
        _ctx.Variables[prefix + ".price"] = evidence.Price ?? "null";
        _ctx.Variables[prefix + ".currency"] = evidence.Currency ?? "null";
        _ctx.Variables[prefix + ".availability"] = evidence.Availability ?? "null";
        _ctx.Variables[prefix + ".stock"] = evidence.Stock ?? "null";
        _ctx.Variables[prefix + ".seller"] = evidence.Seller ?? "null";
        _ctx.Variables[prefix + ".contactSignals"] = JoinSignals(evidence.ContactSignals);
        _ctx.Variables[prefix + ".whatsappSignals"] = JoinSignals(evidence.WhatsappSignals);
        _ctx.Variables[prefix + ".cartSignals"] = JoinSignals(evidence.CartSignals);
        _ctx.Variables[prefix + ".buySignals"] = JoinSignals(evidence.BuySignals);
        _ctx.Variables[prefix + ".paymentSignals"] = JoinSignals(evidence.PaymentSignals);
        _ctx.Variables[prefix + ".loginSignals"] = JoinSignals(evidence.LoginSignals);
        _ctx.Variables[prefix + ".cookieSignals"] = JoinSignals(evidence.CookieSignals);
        _ctx.Variables[prefix + ".geolocSignals"] = JoinSignals(evidence.GeolocSignals);
        _ctx.Variables[prefix + ".popupSignals"] = JoinSignals(evidence.PopupSignals);
        _ctx.Variables[prefix + ".evidenceTextSample"] = evidence.EvidenceTextSample ?? "null";
        _ctx.Variables[prefix + ".rawSignals"] = JoinSignals(evidence.RawSignals);
        _ctx.Variables[prefix + ".blockedOrMissingFields"] = JoinSignals(evidence.BlockedOrMissingFields);
        _ctx.Variables[prefix + ".extractionConfidence"] = evidence.ExtractionConfidence;
        _ctx.Variables[prefix + ".extractionStatus"] = evidence.ExtractionStatus;
        _ctx.Variables[prefix + ".extractionNotes"] = JoinSignals(evidence.ExtractionNotes);
        _ctx.Variables[prefix + ".json"] = evidence.ToJson();
    }

    private static string JoinSignals(IReadOnlyList<string> signals)
    {
        return signals.Count == 0 ? "null" : string.Join(", ", signals);
    }

    private RecipeStepRunResult ExecuteWriteProductEvidenceArtifact(RecipeStepDefinition step, Stopwatch sw)
    {
        var fromPrefix = step.Args?.GetValueOrDefault("from") ?? "productEvidence";
        var evidenceJson = _ctx.Variables.GetValueOrDefault(fromPrefix + ".json", "");
        if (string.IsNullOrWhiteSpace(evidenceJson))
            return Fail(step, sw, $"artifact.writeProductEvidence: no evidence JSON found for prefix '{fromPrefix}'. Run extract.productEvidence first.");

        ProductEvidence? evidence;
        try
        {
            evidence = System.Text.Json.JsonSerializer.Deserialize<ProductEvidence>(evidenceJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            return Fail(step, sw, $"artifact.writeProductEvidence: invalid evidence JSON: {ex.Message}");
        }

        if (evidence == null)
            return Fail(step, sw, "artifact.writeProductEvidence: evidence JSON parsed as null.");

        var recipeId = ResolveArg(step, "recipeId") ?? _currentRecipe?.Name ?? "unknown-recipe";
        var profileId = ResolveArg(step, "profileId") ?? evidence.SourceProfileId;
        var sourceUrl = ResolveArg(step, "sourceUrl") ?? evidence.SourceUrl;
        var pageTitle = ResolveArg(step, "pageTitle") ?? evidence.PageTitle;
        var runId = ResolveArg(step, "runId");
        var notesArg = ResolveArg(step, "notes");
        var notes = string.IsNullOrWhiteSpace(notesArg)
            ? Array.Empty<string>()
            : notesArg.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var result = ProductEvidenceArtifactWriter.Write(Directory.GetCurrentDirectory(), new ProductEvidenceArtifactInput
        {
            Evidence = evidence,
            RecipeId = recipeId,
            ProfileId = profileId,
            SourceUrl = sourceUrl,
            PageTitle = pageTitle,
            RunId = runId,
            Notes = notes
        });

        var prefix = step.SaveAs ?? "artifact";
        _ctx.Variables[prefix + ".success"] = result.Success ? "true" : "false";
        _ctx.Variables[prefix + ".path"] = result.Path;
        _ctx.Variables[prefix + ".relativePath"] = result.RelativePath;
        _ctx.Variables[prefix + ".runId"] = result.RunId;
        _ctx.Variables[prefix + ".error"] = result.Error;

        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, result.Success,
            result.Success
                ? $"Product evidence artifact written: {result.RelativePath}"
                : $"artifact.writeProductEvidence failed: {result.Error}",
            sw.ElapsedMilliseconds, result);
    }

    private RecipeStepRunResult ExecuteSummarizeProductEvidenceArtifacts(RecipeStepDefinition step, Stopwatch sw)
    {
        var prefix = step.SaveAs ?? "productEvidenceSummary";
        var inputDir = ResolveArg(step, "inputDir");
        var outputDir = ResolveArg(step, "outputDir");

        var result = ProductEvidenceSummaryWriter.WriteFromDirectory(
            Directory.GetCurrentDirectory(),
            inputDir,
            outputDir);

        SetProductEvidenceSummaryVars(prefix, result);

        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, result.Success,
            result.Success
                ? $"Product evidence summary written: {result.RelativePath} ({result.Summary.ValidArtifactCount} valid, {result.Summary.InvalidArtifactCount} invalid)"
                : $"artifact.summarizeProductEvidence failed: {result.Error}",
            sw.ElapsedMilliseconds, result);
    }

    private void SetProductEvidenceSummaryVars(string prefix, ProductEvidenceSummaryWriteResult result)
    {
        _ctx.Variables[prefix + ".success"] = result.Success ? "true" : "false";
        _ctx.Variables[prefix + ".path"] = result.Path;
        _ctx.Variables[prefix + ".relativePath"] = result.RelativePath;
        _ctx.Variables[prefix + ".error"] = result.Error;

        var summary = result.Summary;
        _ctx.Variables[prefix + ".schemaVersion"] = summary.SchemaVersion;
        _ctx.Variables[prefix + ".sourceArtifactCount"] = summary.SourceArtifactCount.ToString();
        _ctx.Variables[prefix + ".validArtifactCount"] = summary.ValidArtifactCount.ToString();
        _ctx.Variables[prefix + ".invalidArtifactCount"] = summary.InvalidArtifactCount.ToString();
        _ctx.Variables[prefix + ".productsWithPrice"] = summary.Totals.ProductsWithPrice.ToString();
        _ctx.Variables[prefix + ".productsMissingPrice"] = summary.Totals.ProductsMissingPrice.ToString();
        _ctx.Variables[prefix + ".productsWithMediumConfidence"] = summary.Totals.ProductsWithMediumConfidence.ToString();
        _ctx.Variables[prefix + ".productsWithHighConfidence"] = summary.Totals.ProductsWithHighConfidence.ToString();
        _ctx.Variables[prefix + ".productsWithDiagnosticStatus"] = summary.Totals.ProductsWithDiagnosticStatus.ToString();
        _ctx.Variables[prefix + ".safetyClicksTotal"] = summary.Totals.SafetyClicksTotal.ToString();
        _ctx.Variables[prefix + ".safetyPaymentsSignalsTotal"] = summary.Totals.SafetyPaymentsSignalsTotal.ToString();
        _ctx.Variables[prefix + ".artifactsWithWarnings"] = summary.Totals.ArtifactsWithWarnings.ToString();
        _ctx.Variables[prefix + ".sufficientCount"] = summary.Totals.SufficientCount.ToString();
        _ctx.Variables[prefix + ".partialCount"] = summary.Totals.PartialCount.ToString();
        _ctx.Variables[prefix + ".insufficientCount"] = summary.Totals.InsufficientCount.ToString();
        _ctx.Variables[prefix + ".diagnosticCount"] = summary.Totals.DiagnosticCount.ToString();
        _ctx.Variables[prefix + ".averageEvidenceScore"] = summary.Totals.AverageEvidenceScore.ToString("0.##", CultureInfo.InvariantCulture);
        _ctx.Variables[prefix + ".readyForComparisonCount"] = summary.Totals.ReadyForComparisonCount.ToString();
        _ctx.Variables[prefix + ".needsPriceVerificationCount"] = summary.Totals.NeedsPriceVerificationCount.ToString();
        _ctx.Variables[prefix + ".notes"] = summary.Notes.Count == 0 ? "null" : string.Join(" | ", summary.Notes);
        _ctx.Variables[prefix + ".json"] = System.Text.Json.JsonSerializer.Serialize(summary, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }

    private RecipeStepRunResult ExecuteWriteProductEvidenceMarkdownReport(RecipeStepDefinition step, Stopwatch sw)
    {
        var prefix = step.SaveAs ?? "productEvidenceMarkdownReport";
        var summaryPrefix = ResolveArg(step, "summaryFrom") ?? "productEvidenceSummary";
        var summaryJson = _ctx.Variables.GetValueOrDefault(summaryPrefix + ".json", "");
        if (string.IsNullOrWhiteSpace(summaryJson))
            return Fail(step, sw, $"report.writeProductEvidenceMarkdown: no summary JSON found for prefix '{summaryPrefix}'. Run artifact.summarizeProductEvidence first.");

        ProductEvidenceSummary? summary;
        try
        {
            summary = System.Text.Json.JsonSerializer.Deserialize<ProductEvidenceSummary>(summaryJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            return Fail(step, sw, $"report.writeProductEvidenceMarkdown: invalid summary JSON: {ex.Message}");
        }

        if (summary == null)
            return Fail(step, sw, "report.writeProductEvidenceMarkdown: summary JSON parsed as null.");

        var outputDir = ResolveArg(step, "outputDir");
        var result = ProductEvidenceMarkdownWriter.Write(Directory.GetCurrentDirectory(), summary, outputDir);
        SetProductEvidenceMarkdownVars(prefix, result);

        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, result.Success,
            result.Success
                ? $"Product evidence Markdown report written: {result.RelativePath}"
                : $"report.writeProductEvidenceMarkdown failed: {result.Error}",
            sw.ElapsedMilliseconds, result);
    }

    private void SetProductEvidenceMarkdownVars(string prefix, ProductEvidenceMarkdownWriteResult result)
    {
        _ctx.Variables[prefix + ".success"] = result.Success ? "true" : "false";
        _ctx.Variables[prefix + ".path"] = result.Path;
        _ctx.Variables[prefix + ".relativePath"] = result.RelativePath;
        _ctx.Variables[prefix + ".error"] = result.Error;
    }

    private RecipeStepRunResult ExecuteWriteProductEvidenceHtmlReport(RecipeStepDefinition step, Stopwatch sw)
    {
        var prefix = step.SaveAs ?? "productEvidenceHtmlReport";
        var summaryPrefix = ResolveArg(step, "summaryFrom") ?? "productEvidenceSummary";
        var summaryJson = _ctx.Variables.GetValueOrDefault(summaryPrefix + ".json", "");
        if (string.IsNullOrWhiteSpace(summaryJson))
            return Fail(step, sw, $"report.writeProductEvidenceHtml: no summary JSON found for prefix '{summaryPrefix}'. Run artifact.summarizeProductEvidence first.");

        ProductEvidenceSummary? summary;
        try
        {
            summary = System.Text.Json.JsonSerializer.Deserialize<ProductEvidenceSummary>(summaryJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            return Fail(step, sw, $"report.writeProductEvidenceHtml: invalid summary JSON: {ex.Message}");
        }

        if (summary == null)
            return Fail(step, sw, "report.writeProductEvidenceHtml: summary JSON parsed as null.");

        var outputDir = ResolveArg(step, "outputDir");
        var result = ProductEvidenceHtmlWriter.Write(Directory.GetCurrentDirectory(), summary, outputDir);
        SetProductEvidenceHtmlVars(prefix, result);

        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, result.Success,
            result.Success
                ? $"Product evidence HTML report written: {result.RelativePath}"
                : $"report.writeProductEvidenceHtml failed: {result.Error}",
            sw.ElapsedMilliseconds, result);
    }

    private void SetProductEvidenceHtmlVars(string prefix, ProductEvidenceHtmlWriteResult result)
    {
        _ctx.Variables[prefix + ".success"] = result.Success ? "true" : "false";
        _ctx.Variables[prefix + ".path"] = result.Path;
        _ctx.Variables[prefix + ".relativePath"] = result.RelativePath;
        _ctx.Variables[prefix + ".error"] = result.Error;
    }

    private RecipeStepRunResult ExecutePlanSafeNavigation(RecipeStepDefinition step, Stopwatch sw)
    {
        var itemsJson = step.Args?.GetValueOrDefault("from") ?? _ctx.Variables.GetValueOrDefault("actions.itemsJson", "");
        if (itemsJson is null or "")
            return Fail(step, sw, "plan.safeNavigation: no items found. Run discover.actionableElements first.");

        var items = CommercialActionDiscovery.DeserializeItems(itemsJson);
        if (items == null)
            return Fail(step, sw, "plan.safeNavigation: failed to parse items JSON.");

        var plan = SafeNavigationPlanner.Plan(items);
        var prefix = step.SaveAs ?? "navPlan";

        _ctx.Variables[prefix + ".allowedCount"] = plan.AllowedCount.ToString();
        _ctx.Variables[prefix + ".blockedCount"] = plan.BlockedCount.ToString();
        _ctx.Variables[prefix + ".requiresApprovalCount"] = plan.RequiresApprovalCount.ToString();
        _ctx.Variables[prefix + ".hasExecutableActions"] = plan.HasExecutableActions ? "true" : "false";
        _ctx.Variables[prefix + ".summary"] = plan.Summary;
        _ctx.Variables[prefix + ".blockedReasonsJson"] = plan.BlockedReasonsJson ?? "";
        _ctx.Variables[prefix + ".candidatesJson"] = plan.CandidatesJson ?? "";

        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, true,
            $"Navigation plan: {plan.AllowedCount} allowed, {plan.BlockedCount} blocked, hasExecutableActions: false",
            sw.ElapsedMilliseconds, plan);
    }

    private RecipeStepRunResult ExecutePreflightClick(RecipeStepDefinition step, Stopwatch sw)
    {
        var targetText = step.Args?.GetValueOrDefault("targettext") ?? "";
        if (targetText is null or "")
            return Fail(step, sw, "preflight.click requires 'targetText'.");

        var contextJson = step.Args?.GetValueOrDefault("from");
        if (contextJson == null)
            _ctx.Variables.TryGetValue("actions.itemsJson", out contextJson);

        var result = ClickPreflightEvaluator.Evaluate(targetText, contextJson);
        var prefix = step.SaveAs ?? "clickPreflight";

        _ctx.Variables[prefix + ".targetText"] = result.TargetText;
        _ctx.Variables[prefix + ".decision"] = result.Decision;
        _ctx.Variables[prefix + ".riskCategory"] = result.RiskCategory;
        _ctx.Variables[prefix + ".riskLevel"] = result.RiskLevel;
        _ctx.Variables[prefix + ".allowed"] = result.Allowed ? "true" : "false";
        _ctx.Variables[prefix + ".blocked"] = result.Blocked ? "true" : "false";
        _ctx.Variables[prefix + ".requiresApproval"] = result.RequiresApproval ? "true" : "false";
        _ctx.Variables[prefix + ".requiresReview"] = result.RequiresReview ? "true" : "false";
        _ctx.Variables[prefix + ".reason"] = result.Reason;
        _ctx.Variables[prefix + ".summary"] = result.Summary;
        _ctx.Variables[prefix + ".evidenceJson"] = result.EvidenceJson ?? "";
        _ctx.Variables[prefix + ".nearbyDangerousSignalsJson"] = result.NearbyDangerousSignalsJson ?? "";

        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, true,
            $"Preflight: '{targetText}' → {result.Decision} ({result.RiskCategory})", sw.ElapsedMilliseconds, result);
    }

    private RecipeStepRunResult ExecuteTargetObserve(RecipeStepDefinition step, Stopwatch sw)
    {
        var prefix = step.SaveAs ?? "targetObserve";
        var targetText = R(step.Args?.GetValueOrDefault("targetText")
            ?? step.Args?.GetValueOrDefault("targettext")
            ?? step.Args?.GetValueOrDefault("target")
            ?? step.Name);
        if (string.IsNullOrWhiteSpace(targetText))
            return Fail(step, sw, "target.observe requires 'targetText' or 'target'.");

        var processName = R(step.Args?.GetValueOrDefault("proc")
            ?? step.Args?.GetValueOrDefault("processName")
            ?? step.Process
            ?? "msedge");
        if (string.IsNullOrWhiteSpace(processName))
            processName = "msedge";

        var mode = R(step.Args?.GetValueOrDefault("mode"));
        _ctx.Variables[prefix + ".targetText"] = targetText;
        if (!string.IsNullOrWhiteSpace(mode))
            _ctx.Variables[prefix + ".mode"] = mode;

        var sessionHwnd = FindOwnedBrowserSessionHwnd();
        if (sessionHwnd == IntPtr.Zero)
        {
            ApplyTargetObserveResult(
                prefix,
                new WebTargetResult
                {
                    Found = false,
                    Reason = "no owned browser session active"
                },
                FailureKind.SourceUnavailable);

            sw.Stop();
            return new RecipeStepRunResult(
                step.Id,
                step.Kind,
                true,
                "target.observe: no owned browser session active.",
                sw.ElapsedMilliseconds);
        }

        try
        {
            var resolution = ResolveTargetObserve(sessionHwnd, targetText, processName);
            ApplyTargetObserveResult(prefix, resolution, MapTargetObserveFailureKind(resolution));

            sw.Stop();
            if (resolution.Found)
            {
                return new RecipeStepRunResult(
                    step.Id,
                    step.Kind,
                    true,
                    $"target.observe: observed '{resolution.SelectedName}' ({resolution.SelectedControlType})",
                    sw.ElapsedMilliseconds);
            }

            return new RecipeStepRunResult(
                step.Id,
                step.Kind,
                true,
                $"target.observe: {resolution.Reason}",
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            ApplyTargetObserveResult(
                prefix,
                new WebTargetResult
                {
                    Found = false,
                    Reason = ex.Message
                },
                FailureKind.SourceUnavailable);

            sw.Stop();
            return new RecipeStepRunResult(
                step.Id,
                step.Kind,
                true,
                $"target.observe: {ex.Message}",
                sw.ElapsedMilliseconds);
        }
    }

    private RecipeStepRunResult ExecuteTargetObserveDesktop(RecipeStepDefinition step, Stopwatch sw)
    {
        var prefix = step.SaveAs ?? "target.desktop";
        var targetText = R(step.Args?.GetValueOrDefault("targetText")
            ?? step.Args?.GetValueOrDefault("targettext")
            ?? step.Args?.GetValueOrDefault("target")
            ?? step.Name);
        if (string.IsNullOrWhiteSpace(targetText))
            return Fail(step, sw, "target.observe.desktop requires 'targetText' or 'target'.");

        var processName = EmptyToNull(R(step.Args?.GetValueOrDefault("proc")
            ?? step.Args?.GetValueOrDefault("processName")
            ?? step.Process));
        var windowTitle = EmptyToNull(R(step.Args?.GetValueOrDefault("window")
            ?? step.Args?.GetValueOrDefault("windowTitle")
            ?? step.Args?.GetValueOrDefault("titleContains")
            ?? step.Window
            ?? step.TitleContains));
        var mode = R(step.Args?.GetValueOrDefault("mode"));

        _ctx.Variables[prefix + ".targetText"] = targetText;
        if (!string.IsNullOrWhiteSpace(mode))
            _ctx.Variables[prefix + ".mode"] = mode;

        try
        {
            var result = ResolveTargetObserveDesktop(targetText, processName, windowTitle);
            ApplyDesktopTargetObserveResult(prefix, result, MapDesktopTargetObserveFailureKind(result));

            sw.Stop();
            if (result.Found)
            {
                return new RecipeStepRunResult(
                    step.Id,
                    step.Kind,
                    true,
                    $"target.observe.desktop: observed '{result.SelectedName}' ({result.SelectedControlType})",
                    sw.ElapsedMilliseconds);
            }

            return new RecipeStepRunResult(
                step.Id,
                step.Kind,
                true,
                $"target.observe.desktop: {result.Reason}",
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            ApplyDesktopTargetObserveResult(
                prefix,
                new DesktopTargetObservationResult
                {
                    Found = false,
                    CandidateCount = 0,
                    Reason = ex.Message
                },
                FailureKind.SourceUnavailable);

            sw.Stop();
            return new RecipeStepRunResult(
                step.Id,
                step.Kind,
                true,
                $"target.observe.desktop: {ex.Message}",
                sw.ElapsedMilliseconds);
        }
    }

    private RecipeStepRunResult ExecuteApprovalManifest(RecipeStepDefinition step, Stopwatch sw)
    {
        var fromPrefix = step.Args?.GetValueOrDefault("from") ?? "";
        if (fromPrefix is null or "")
            fromPrefix = "clickPreflight";

        var mode = step.Args?.GetValueOrDefault("mode") ?? "commercialWeb";
        var evidenceJson = _ctx.Variables.GetValueOrDefault(fromPrefix + ".evidenceJson", "");
        if (string.IsNullOrWhiteSpace(evidenceJson))
            return Fail(step, sw, $"approval.manifest: no evidence found for prefix '{fromPrefix}'.");

        var identityInput = TryReadApprovedIdentityInput(fromPrefix);
        var manifest = ApprovalManifestBuilder.BuildFromEvidence(evidenceJson, mode, identityInput);
        var approvedInputBinding = TryBuildManifestApprovedInputBinding(manifest, fromPrefix, "type");
        if (approvedInputBinding != null)
            manifest = ApprovalManifestBuilder.AttachApprovedInputBinding(manifest, approvedInputBinding);

        var prefix = step.SaveAs ?? "approval";

        _ctx.Variables[prefix + ".required"] = manifest.Required ? "true" : "false";
        _ctx.Variables[prefix + ".allowed"] = manifest.Allowed ? "true" : "false";
        _ctx.Variables[prefix + ".blocked"] = manifest.Blocked ? "true" : "false";
        _ctx.Variables[prefix + ".title"] = manifest.Title;
        _ctx.Variables[prefix + ".summary"] = manifest.Summary;
        _ctx.Variables[prefix + ".reason"] = manifest.Reason;
        _ctx.Variables[prefix + ".manifestJson"] = manifest.ManifestJson ?? "";
        _ctx.Variables[prefix + ".humanReadableText"] = manifest.HumanReadableText;
        _ctx.Variables[prefix + ".policyVersion"] = manifest.PolicyVersion;
        _ctx.Variables[prefix + ".targetText"] = manifest.TargetText;
        _ctx.Variables[prefix + ".mode"] = manifest.Mode;
        _ctx.Variables[prefix + ".decision"] = manifest.Decision;
        _ctx.Variables[prefix + ".riskCategory"] = manifest.RiskCategory;
        _ctx.Variables[prefix + ".riskLevel"] = manifest.RiskLevel;
        _ctx.Variables[prefix + ".evidenceHash"] = manifest.EvidenceHash;
        _ctx.Variables[prefix + ".executionAllowedInThisHito"] = manifest.ExecutionAllowedInThisHito ? "true" : "false";
        SetApprovalIdentityVars(prefix, manifest);
        CopyApprovalObservationVars(prefix, fromPrefix);
        SetApprovalInputVars(prefix, manifest);

        sw.Stop();
        return new RecipeStepRunResult(step.Id, step.Kind, true,
            $"Approval manifest: {manifest.Title} → blocked={manifest.Blocked}, executable={manifest.ExecutionAllowedInThisHito}", sw.ElapsedMilliseconds, manifest);
    }

    private RecipeStepRunResult ExecuteSafeRead(RecipeStepDefinition step, Stopwatch sw)
    {
        var targetText = step.Args?.GetValueOrDefault("targettext")
            ?? step.Args?.GetValueOrDefault("target")
            ?? "";
        if (string.IsNullOrWhiteSpace(targetText))
            return Fail(step, sw, "safe.read requires 'targetText' or 'target'.");

        var prefix = step.SaveAs ?? "safeRead";
        var approvalPrefix = step.Args?.GetValueOrDefault("approvalprefix") ?? "approval";
        var manifest = TryReadApprovalManifest(approvalPrefix);
        var manifestValidation = ValidateSafeExecutorManifest(manifest, requiredIdentitySource: "");
        if (manifestValidation != null)
        {
            SetSafeReadVars(prefix, success: false, "", "", manifestValidation.Value.FailureKind, manifestValidation.Value.Reason);
            SetSafeReadEvidenceVars(prefix, StepState.Blocked, manifestValidation.Value.FailureKind, manifestValidation.Value.BlockReason, [manifestValidation.Value.Reason]);
            sw.Stop();
            return new RecipeStepRunResult(
                step.Id,
                step.Kind,
                false,
                $"safe.read: {manifestValidation.Value.Reason}",
                sw.ElapsedMilliseconds);
        }

        var authorization = SafeExecutorAuthorizationPolicy.Evaluate(manifest, "safe.read");
        if (!authorization.Allowed)
        {
            SetSafeReadVars(prefix, success: false, "", "", authorization.FailureKind, authorization.Reason);
            SetSafeReadEvidenceVars(prefix, StepState.Blocked, authorization.FailureKind, authorization.BlockReason, [authorization.Reason]);
            sw.Stop();
            return new RecipeStepRunResult(
                step.Id,
                step.Kind,
                false,
                $"safe.read: {authorization.Reason}",
                sw.ElapsedMilliseconds);
        }

        var contract = BuildSafeExecutorContract(manifest!, "read", "", out var contractReason, out var contractFailureKind);
        if (contract == null)
        {
            SetSafeReadVars(prefix, success: false, "", "", contractFailureKind, contractReason);
            SetSafeReadEvidenceVars(prefix, StepState.Blocked, contractFailureKind, "ApprovalV3StrongIdentityRequired", [contractReason]);
            sw.Stop();
            return new RecipeStepRunResult(
                step.Id,
                step.Kind,
                false,
                $"safe.read: {contractReason}",
                sw.ElapsedMilliseconds);
        }

        var contractValidation = new ContractValidator().Validate(contract);
        if (!contractValidation.IsValid)
        {
            var reason = contractValidation.Reasons.Count == 0
                ? "safe.read contract invalid"
                : string.Join(" | ", contractValidation.Reasons);
            SetSafeReadVars(prefix, success: false, "", "", contractValidation.FailureKind, reason);
            SetSafeReadEvidenceVars(prefix, StepState.Blocked, contractValidation.FailureKind, "ContractInvalid", contractValidation.Reasons);
            sw.Stop();
            return new RecipeStepRunResult(
                step.Id,
                step.Kind,
                false,
                $"safe.read: {reason}",
                sw.ElapsedMilliseconds);
        }

        var candidates = contract.ExpectedIdentity == null
            ? Array.Empty<ElementIdentity>()
            : new[] { contract.ExpectedIdentity };
        var binding = new ApprovalBindingValidator().Validate(
            contract.ApprovalRef!,
            contract.ExpectedIdentity!,
            candidates,
            reversible: true);
        if (!binding.Success)
        {
            var reason = binding.Reasons.Count == 0
                ? binding.BlockReason
                : string.Join(" | ", binding.Reasons);
            SetSafeReadVars(prefix, success: false, "", "", binding.FailureKind, reason);
            SetSafeReadEvidenceVars(prefix, StepState.Blocked, binding.FailureKind, binding.BlockReason, binding.Reasons);
            sw.Stop();
            return new RecipeStepRunResult(
                step.Id,
                step.Kind,
                false,
                $"safe.read: {reason}",
                sw.ElapsedMilliseconds);
        }

        var rootHwnd = ParseHandle(_ctx.Variables.GetValueOrDefault(approvalPrefix + ".identity.rootHwnd", ""));
        var readExecutor = s_safeReadExecutorFactoryOverride?.Invoke() ?? new UiaReadExecutor();
        var readResult = readExecutor.Read(new PatternReadRequest(
            ActionKind: "read",
            TargetRef: targetText,
            ExpectedTargetName: contract.ExpectedIdentity?.Name ?? targetText,
            ProcessName: contract.ExpectedIdentity?.ProcessName,
            WindowTitleContains: contract.ExpectedIdentity?.WindowTitle,
            Selector: contract.Selector!,
            ExpectedIdentity: contract.ExpectedIdentity!,
            RootHwnd: rootHwnd,
            ReadMode: step.Args?.GetValueOrDefault("readMode") ?? ""));

        var verification = new SafeReadStepVerifier().Verify(contract, readResult);
        var ledger = BuildSafeReadLedger(contract, binding, readResult, verification);
        SetSafeReadEvidenceVars(prefix, ledger, readResult, verification);

        if (!readResult.Success || !verification.Success)
        {
            var failureKind = readResult.FailureKind ?? verification.FailureKind ?? FailureKind.Unverified;
            var reason = readResult.Reasons.Concat(verification.Reasons).LastOrDefault()
                ?? "safe.read failed";
            SetSafeReadVars(prefix, success: false, "", readResult.PatternUsed, failureKind, reason, readResult, verification);
            sw.Stop();
            return new RecipeStepRunResult(
                step.Id,
                step.Kind,
                false,
                $"safe.read: {reason}",
                sw.ElapsedMilliseconds);
        }

        SetSafeReadVars(prefix, success: true, readResult.Value, readResult.PatternUsed, null, "", readResult, verification);
        sw.Stop();
        return new RecipeStepRunResult(
            step.Id,
            step.Kind,
            true,
            "safe.read: read completed via UIA read-only executor",
            sw.ElapsedMilliseconds,
            readResult.Value);
    }

    private RecipeStepRunResult ExecuteSafeType(RecipeStepDefinition step, Stopwatch sw)
    {
        var targetText = ResolveArg(step, "targettext")
            ?? ResolveArg(step, "target")
            ?? "";
        if (string.IsNullOrWhiteSpace(targetText))
            return Fail(step, sw, "safe.type requires 'targetText' or 'target'.");

        var approvedText = ResolveArg(step, "text")
            ?? ResolveArg(step, "value")
            ?? ResolveArg(step, "approvedText")
            ?? "";
        var prefix = step.SaveAs ?? "safeType";
        if (string.IsNullOrWhiteSpace(approvedText))
            return BlockSafeType(step, sw, prefix, FailureKind.PolicyDenied, "ApprovedTextRequired", "safe.type requires non-empty approved text");

        var dispatchPath = (step.Args?.GetValueOrDefault("dispatchPath")
            ?? step.Args?.GetValueOrDefault("dispatchpath")
            ?? "").Trim();
        if (dispatchPath.Equals("legacy", StringComparison.OrdinalIgnoreCase))
            return BlockSafeType(step, sw, prefix, FailureKind.PolicyDenied, "SafeTypeLegacyDispatchBlocked", "safe.type legacy dispatch is not allowed");
        if (!string.IsNullOrWhiteSpace(dispatchPath) &&
            !dispatchPath.Equals("safe-executor", StringComparison.OrdinalIgnoreCase))
            return BlockSafeType(step, sw, prefix, FailureKind.PolicyDenied, "SafeTypeDispatchPathPolicyDenied", "safe.type dispatchPath must be 'safe-executor' when specified");

        var approvalPrefix = step.Args?.GetValueOrDefault("approvalprefix") ?? "approval";
        var manifest = TryReadApprovalManifest(approvalPrefix);
        var manifestValidation = ValidateSafeExecutorManifest(manifest, requiredIdentitySource: "");
        if (manifestValidation != null)
            return BlockSafeType(step, sw, prefix, manifestValidation.Value.FailureKind, manifestValidation.Value.BlockReason, manifestValidation.Value.Reason);

        var authorization = SafeExecutorAuthorizationPolicy.Evaluate(manifest, "safe.type");
        if (!authorization.Allowed)
            return BlockSafeType(step, sw, prefix, authorization.FailureKind, authorization.BlockReason, authorization.Reason);

        var resolvedDigest = ComputeApprovedValueDigest(approvedText);
        var inputValidation = ValidateApprovedInputBinding(manifest!, resolvedDigest);
        SetSafeTypeApprovedInputVars(prefix, manifest!.ApprovedInputBinding, inputValidation);
        if (!inputValidation.Success)
            return BlockSafeType(step, sw, prefix, inputValidation.FailureKind ?? FailureKind.PolicyDenied, inputValidation.Reason, ToSafeTypeApprovedInputFailureReason(inputValidation.Reason));

        var approvedDigest = manifest.ApprovedInputBinding!.ApprovedValueDigest;
        var contract = BuildSafeExecutorContract(
            manifest!,
            "type",
            approvedDigest,
            out var contractReason,
            out var contractFailureKind,
            approvedInputBindingHash: manifest.ApprovedInputBinding.ApprovedInputBindingHash,
            approvedInputBindingVersion: manifest.ApprovedInputBinding.BindingVersion,
            approvedInputDigestAlgorithm: manifest.ApprovedInputBinding.ApprovedValueDigestAlgorithm);
        if (contract == null)
            return BlockSafeType(step, sw, prefix, contractFailureKind, "ApprovalV3StrongIdentityRequired", contractReason);

        var contractValidation = new ContractValidator().Validate(contract);
        if (!contractValidation.IsValid)
        {
            var reason = contractValidation.Reasons.Count == 0
                ? "safe.type contract invalid"
                : string.Join(" | ", contractValidation.Reasons);
            return BlockSafeType(step, sw, prefix, contractValidation.FailureKind ?? FailureKind.PolicyDenied, "ContractInvalid", reason, contractValidation.Reasons);
        }

        var liveTarget = ResolveSafeTypeLiveTarget(step, targetText, prefix, manifest!);
        if (!liveTarget.Success || liveTarget.ObservedIdentity == null)
            return BlockSafeType(step, sw, prefix, liveTarget.FailureKind, liveTarget.BlockReason, liveTarget.Reason);

        var binding = new ApprovalBindingValidator().Validate(
            contract.ApprovalRef!,
            contract.ExpectedIdentity!,
            [liveTarget.ObservedIdentity],
            reversible: false);
        if (!binding.Success)
        {
            var reason = binding.Reasons.Count == 0
                ? binding.BlockReason
                : string.Join(" | ", binding.Reasons);
            return BlockSafeType(step, sw, prefix, binding.FailureKind ?? FailureKind.Unverified, binding.BlockReason, reason, binding.Reasons);
        }

        if (liveTarget.RootHwnd == null || liveTarget.RootHwnd == IntPtr.Zero)
            return BlockSafeType(step, sw, prefix, FailureKind.NotFound, "SafeTypeRootRequired", "safe.type requires observed root hwnd");

        var ownershipMonitor = s_safeClickOwnershipMonitorFactoryOverride?.Invoke() ?? new DesktopOwnershipMonitor();
        var typeExecutor = s_safeTypeExecutorFactoryOverride?.Invoke() ?? new UiaTypeExecutor(ownershipMonitor);
        var typeResult = typeExecutor.Type(new TypeExecutionRequest(
            ActionKind: "type",
            TargetRef: targetText,
            ExpectedTargetName: liveTarget.SelectedName,
            ProcessName: liveTarget.ProcessName,
            WindowTitleContains: liveTarget.WindowTitle,
            Selector: contract.Selector!,
            ExpectedIdentity: contract.ExpectedIdentity!,
            ApprovedText: approvedText,
            ApprovedTextDigest: approvedDigest,
            RootHwnd: liveTarget.RootHwnd));

        var verification = new SafeTypeStepVerifier().Verify(contract, typeResult, approvedText);
        var ledger = BuildSafeTypeLedger(contract, binding, typeResult, verification);
        SetSafeTypeVars(prefix, approvedText, typeResult, verification);
        SetSafeTypeEvidenceVars(prefix, ledger);

        if (!typeResult.Success || !verification.Success)
        {
            var failureKind = typeResult.FailureKind ?? verification.FailureKind ?? FailureKind.Unverified;
            var reason = typeResult.Reasons.Concat(verification.Reasons).LastOrDefault()
                ?? "safe.type failed";
            _ctx.Variables[prefix + ".failureKind"] = failureKind.ToString();
            _ctx.Variables[prefix + ".reason"] = reason;
            sw.Stop();
            return new RecipeStepRunResult(step.Id, step.Kind, false, $"safe.type: {reason}", sw.ElapsedMilliseconds);
        }

        _ctx.Variables[prefix + ".success"] = "true";
        _ctx.Variables[prefix + ".reason"] = "";
        sw.Stop();
        return new RecipeStepRunResult(
            step.Id,
            step.Kind,
            true,
            "safe.type: value written via UIA ValuePattern.SetValue",
            sw.ElapsedMilliseconds);
    }

    private RecipeStepRunResult ExecuteSafeClick(RecipeStepDefinition step, Stopwatch sw)
    {
        var targetText = step.Args?.GetValueOrDefault("targettext") ?? "";
        if (targetText is null or "")
            return Fail(step, sw, "safe.click requires 'targetText'.");

        var prefix = step.SaveAs ?? "safeClick";
        var approvalPrefix = step.Args?.GetValueOrDefault("approvalprefix") ?? "approval";
        var requestedMode = step.Args?.GetValueOrDefault("mode");
        var approvalMode = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".mode", "");
        var mode = !string.IsNullOrWhiteSpace(requestedMode)
            ? requestedMode
            : !string.IsNullOrWhiteSpace(approvalMode)
                ? approvalMode
                : "controlled";
        var dispatchPath = (step.Args?.GetValueOrDefault("dispatchPath")
            ?? step.Args?.GetValueOrDefault("dispatchpath")
            ?? "").Trim();

        SetSafeClickLegacyUsageVars(prefix, usedElClick: false, usedUiaActionExecutor: false);

        var defaultMode = ResolveSafeClickFsmDefaultMode();
        InitSafeClickDefaultRouteVars(prefix, defaultMode);
        InitSafeClickDesktopFsmVars(prefix);

        // Explicit opt-in always wins and is unchanged from HITO-143.
        if (dispatchPath.Equals("safe-executor", StringComparison.OrdinalIgnoreCase))
        {
            SetSafeClickDefaultRouteVars(prefix, routedByDefault: false, reason: "ExplicitSafeExecutor", eligible: true, scope: "explicit-safe-executor");
            var optInManifest = TryReadApprovalManifest(approvalPrefix);
            var requestedIdentitySource = ResolveRequestedIdentitySource(step, optInManifest);
            if (string.Equals(requestedIdentitySource, "uia", StringComparison.OrdinalIgnoreCase))
                return ExecuteSafeClickDesktopSafeExecutor(step, sw, targetText, prefix, approvalPrefix, mode);

            return ExecuteSafeClickSafeExecutor(step, sw, targetText, prefix, approvalPrefix, mode);
        }

        // Explicit legacy opt-out is now retired: preserve deprecation evidence, but do not execute.
        if (dispatchPath.Equals("legacy", StringComparison.OrdinalIgnoreCase))
        {
            _safeClickExplicitLegacyOptOutCount++;
            var deprecationPolicy = EvaluateSafeClickLegacyDeprecationPolicy(step);
            TrackSafeClickLegacyDeprecationPolicy(deprecationPolicy);
            SetSafeClickLegacyOptOutVars(prefix, deprecationPolicy);
            SetSafeClickDefaultRouteVars(prefix, routedByDefault: false, reason: "LegacyDispatchRetired", eligible: false, scope: "legacy-retired");
            return BlockSafeClickLegacyRetired(step, sw, targetText, prefix, approvalPrefix, mode, dispatchPath, ineligibleAfterRetirement: false);
        }

        // Any other explicit value is fail-closed.
        if (!string.IsNullOrWhiteSpace(dispatchPath))
        {
            _safeClickUnknownDispatchPathBlockedCount++;
            SetSafeClickVars(prefix, targetText, "blocked", $"dispatchPath '{dispatchPath}' is not allowed");
            SetSafeClickFsmVars(prefix, StepState.Blocked, FailureKind.PolicyDenied, "DispatchPathPolicyDenied", ["dispatchPath must be 'safe-executor' or 'legacy' when specified"]);
            SetSafeClickDefaultRouteVars(prefix, routedByDefault: false, reason: "UnknownDispatchPath", eligible: false, scope: "blocked");
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
            sw.Stop();
            return new RecipeStepRunResult(
                step.Id,
                step.Kind,
                false,
                $"safe.click: dispatchPath '{dispatchPath}' is not allowed.",
                sw.ElapsedMilliseconds);
        }

        // No dispatchPath: the global kill-switch decides default routing.
        var routeManifest = TryReadApprovalManifest(approvalPrefix);
        var desktopSource = IsDesktopIdentitySource(routeManifest);
        var webRouteEligible = IsWebDefaultRouteEligible(routeManifest);
        var desktopRouteEligible = false;
        if (desktopSource)
        {
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
            desktopRouteEligible =
                _safeClickShadowReadiness.TryGetValue(prefix, out var readiness) &&
                readiness.DesktopEligibleForFsm;
        }

        var webDefaultEnabled = defaultMode is SafeClickDefaultMode.WebEligible or SafeClickDefaultMode.AllEligible;
        var desktopDefaultEnabled = defaultMode is SafeClickDefaultMode.DesktopEligible or SafeClickDefaultMode.AllEligible;
        if (defaultMode == SafeClickDefaultMode.AllEligible)
            _safeClickAllEligibleModeEnabledCount++;

        if (!webDefaultEnabled && !desktopDefaultEnabled)
        {
            if (webRouteEligible)
                _safeClickDefaultFsmEligibleButNotEnabledCount++;
            if (desktopRouteEligible)
                _safeClickDesktopDefaultEligibleButNotEnabledCount++;

            var legacyReason = defaultMode == SafeClickDefaultMode.Legacy ? "KillSwitchLegacy" : "KillSwitchDisabled";
            SetSafeClickDefaultRouteVars(prefix, routedByDefault: false, reason: "SafeClickLegacyRetired", eligible: webRouteEligible || desktopRouteEligible, scope: "legacy-retired");
            SetSafeClickDesktopDefaultVars(prefix, defaultMode, defaultEnabled: false, routedByDefault: false, eligible: desktopRouteEligible, reason: legacyReason, scope: "legacy-retired");
            return BlockSafeClickLegacyRetired(step, sw, targetText, prefix, approvalPrefix, mode, dispatchPath: "", ineligibleAfterRetirement: true);
        }

        if (webDefaultEnabled)
            _safeClickDefaultFsmEnabledCount++;
        if (desktopDefaultEnabled)
            _safeClickDesktopDefaultFsmEnabledCount++;

        // Kill-switch enables web default: route strictly eligible web steps to the FSM, no silent fallback.
        if (webDefaultEnabled && webRouteEligible)
        {
            _safeClickDefaultFsmRoutedCount++;
            _safeClickDefaultFsmScopeWebCount++;
            SetSafeClickDefaultRouteVars(prefix, routedByDefault: true, reason: "WebEligible", eligible: true, scope: "web-uia");
            SetSafeClickDesktopDefaultVars(prefix, defaultMode, defaultEnabled: desktopDefaultEnabled, routedByDefault: false, eligible: desktopRouteEligible, reason: desktopRouteEligible ? "DesktopEligibleButWebRouteWon" : "NotDesktopEligible", scope: "web-uia");
            var runtimeStability = ReobserveRuntimeStabilityForDefaultDispatch(
                step,
                sw,
                targetText,
                prefix,
                approvalPrefix,
                mode,
                routeManifest,
                out var reobservedResolution,
                out var blockedResult);
            SetSafeClickRuntimeStabilityVars(prefix, runtimeStability);

            if (blockedResult != null)
            {
                _safeClickDefaultFsmBlockedCount++;
                _ctx.Variables[prefix + ".fsm.blockedWithoutLegacyFallback"] = "true";
                return blockedResult;
            }

            var routedResult = ExecuteSafeClickSafeExecutor(
                step,
                sw,
                targetText,
                prefix,
                approvalPrefix,
                mode,
                reobservedResolution);
            if (!routedResult.Success)
            {
                _safeClickDefaultFsmBlockedCount++;
                _ctx.Variables[prefix + ".fsm.blockedWithoutLegacyFallback"] = "true";
            }

            return routedResult;
        }

        // Kill-switch enables desktop default: route strictly eligible desktop steps to the FSM, no silent fallback.
        if (desktopDefaultEnabled && desktopRouteEligible)
        {
            _safeClickDesktopDefaultFsmRoutedCount++;
            _safeClickDefaultFsmScopeDesktopCount++;
            SetSafeClickDefaultRouteVars(prefix, routedByDefault: true, reason: "DesktopEligible", eligible: true, scope: "desktop-uia");
            SetSafeClickDesktopDefaultVars(prefix, defaultMode, defaultEnabled: true, routedByDefault: true, eligible: true, reason: "DesktopEligible", scope: "desktop-uia");
            var runtimeStability = ReobserveDesktopRuntimeStabilityForDefaultDispatch(
                step,
                sw,
                targetText,
                prefix,
                approvalPrefix,
                mode,
                routeManifest,
                out var reobservedDesktopResolution,
                out var blockedResult);
            SetSafeClickRuntimeStabilityVars(prefix, runtimeStability);
            SetSafeClickDesktopRuntimeStabilityVars(prefix, runtimeStability);

            if (blockedResult != null)
            {
                _safeClickDesktopDefaultBlockedCount++;
                _ctx.Variables[prefix + ".fsm.blockedWithoutLegacyFallback"] = "true";
                _ctx.Variables[prefix + ".desktopFsm.blockedWithoutLegacyFallback"] = "true";
                return blockedResult;
            }

            var routedResult = ExecuteSafeClickDesktopSafeExecutor(
                step,
                sw,
                targetText,
                prefix,
                approvalPrefix,
                mode,
                reobservedDesktopResolution,
                routedByDefault: true);
            if (!routedResult.Success)
            {
                _safeClickDesktopDefaultBlockedCount++;
                _ctx.Variables[prefix + ".fsm.blockedWithoutLegacyFallback"] = "true";
                _ctx.Variables[prefix + ".desktopFsm.blockedWithoutLegacyFallback"] = "true";
            }

            return routedResult;
        }

        if (webRouteEligible && !webDefaultEnabled)
            _safeClickDefaultFsmEligibleButNotEnabledCount++;
        if (desktopRouteEligible && !desktopDefaultEnabled)
            _safeClickDesktopDefaultEligibleButNotEnabledCount++;
        if (desktopSource && !desktopDefaultEnabled)
            _safeClickDesktopExcludedFromDefaultCount++;

        var ineligibleReason = desktopSource
            ? ResolveDesktopDefaultIneligibleReason(prefix, routeManifest, desktopRouteEligible, desktopDefaultEnabled)
            : ResolveDefaultIneligibleReason(routeManifest, desktopExcluded: false);
        SetSafeClickDefaultRouteVars(prefix, routedByDefault: false, reason: "SafeClickLegacyRetired", eligible: webRouteEligible || desktopRouteEligible, scope: "legacy-retired");
        SetSafeClickDesktopDefaultVars(prefix, defaultMode, defaultEnabled: desktopDefaultEnabled, routedByDefault: false, eligible: desktopRouteEligible, reason: ineligibleReason, scope: "legacy-retired");
        return BlockSafeClickLegacyRetired(step, sw, targetText, prefix, approvalPrefix, mode, dispatchPath: "", ineligibleAfterRetirement: true);
    }

    private static SafeClickDefaultMode ResolveSafeClickFsmDefaultMode()
    {
        if (s_safeClickFsmDefaultModeOverride != null)
            return s_safeClickFsmDefaultModeOverride();

        return SafeClickDefaultModePolicy.Parse(
            Environment.GetEnvironmentVariable(SafeClickDefaultModePolicy.EnvironmentVariableName));
    }

    private static bool IsDesktopIdentitySource(ApprovalManifest? manifest)
    {
        return manifest != null &&
               string.Equals(manifest.IdentitySource, "uia", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveRequestedIdentitySource(RecipeStepDefinition step, ApprovalManifest? manifest)
    {
        var requested = step.Args?.GetValueOrDefault("identitySource")
            ?? step.Args?.GetValueOrDefault("identitysource")
            ?? step.Args?.GetValueOrDefault("source")
            ?? "";

        return string.IsNullOrWhiteSpace(requested)
            ? manifest?.IdentitySource ?? ""
            : requested.Trim();
    }

    private bool IsWebDefaultRouteEligible(ApprovalManifest? manifest)
    {
        if (manifest == null)
            return false;

        if (ValidateSafeExecutorManifest(manifest) != null)
            return false;

        return string.Equals(manifest.IdentitySource, "web-uia", StringComparison.OrdinalIgnoreCase);
    }

    private string ResolveDefaultIneligibleReason(ApprovalManifest? manifest, bool desktopExcluded)
    {
        if (manifest == null)
            return "MissingManifest";

        if (desktopExcluded)
            return "DesktopExcludedFromDefault";

        var validation = ValidateSafeExecutorManifest(manifest);
        return validation?.BlockReason ?? "NotWebEligible";
    }

    private string ResolveDesktopDefaultIneligibleReason(string prefix, ApprovalManifest? manifest, bool desktopEligible, bool desktopDefaultEnabled)
    {
        if (!desktopDefaultEnabled)
            return desktopEligible ? "DesktopEligibleButDefaultDisabled" : "DesktopDefaultDisabled";

        if (manifest == null)
            return "MissingManifest";

        var validation = ValidateSafeExecutorManifest(manifest, requiredIdentitySource: "uia");
        if (validation != null)
            return validation.Value.BlockReason;

        if (!_safeClickShadowReadiness.TryGetValue(prefix, out var readiness))
            return desktopEligible ? "DesktopEligible" : "DesktopNotEligible";

        return readiness.DesktopEligibleForFsm ? "DesktopEligible" : readiness.Reason;
    }

    private SafeClickRuntimeStability ReobserveRuntimeStabilityForDefaultDispatch(
        RecipeStepDefinition step,
        Stopwatch sw,
        string targetText,
        string prefix,
        string approvalPrefix,
        string mode,
        ApprovalManifest? manifest,
        out WebTargetResult? reobservedResolution,
        out RecipeStepRunResult? blockedResult)
    {
        reobservedResolution = null;
        blockedResult = null;
        _safeClickRuntimeStabilityCheckedCount++;
        _safeClickReobserveAttemptedCount++;

        var sessionHwnd = FindOwnedBrowserSessionHwnd();
        if (sessionHwnd == IntPtr.Zero)
        {
            var stability = SafeClickRuntimeStabilityEvaluator.Evaluate(
                manifest,
                observedIdentity: null,
                reobserveAttempted: true,
                reobserveSucceeded: false);
            TrackRuntimeStability(stability);
            blockedResult = BlockDefaultDispatchForRuntimeStability(
                step,
                sw,
                targetText,
                prefix,
                approvalPrefix,
                mode,
                FailureKind.SourceUnavailable,
                "OwnedBrowserSessionRequired",
                "safe-executor dispatch requires owned browser session",
                stability);
            return stability;
        }

        var proc = step.Args?.GetValueOrDefault("proc") ?? "msedge";
        try
        {
            reobservedResolution = ResolveSafeClickTarget(sessionHwnd, targetText, proc);
            SetResolutionVars(prefix, reobservedResolution);
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);

            var ambiguous = reobservedResolution.CandidateCount > 1 ||
                            reobservedResolution.Reason.Contains("ambiguous", StringComparison.OrdinalIgnoreCase);
            if (!reobservedResolution.Found || ambiguous)
            {
                var observedIdentity = reobservedResolution.Found
                    ? WebTargetResultIdentityMapper.ToSelectedIdentity(reobservedResolution)
                    : null;
                var stability = SafeClickRuntimeStabilityEvaluator.Evaluate(
                    manifest,
                    observedIdentity,
                    reobserveAttempted: true,
                    reobserveSucceeded: false);
                TrackRuntimeStability(stability);

                var failureKind = ambiguous ? FailureKind.Ambiguous : FailureKind.NotFound;
                var blockReason = ambiguous ? "ApprovalAmbiguous" : "ApprovalTargetNotFound";
                var reason = ambiguous
                    ? $"ambiguous: {reobservedResolution.CandidateCount} candidates"
                    : string.IsNullOrWhiteSpace(reobservedResolution.Reason)
                        ? "target not found during runtime identity re-observe"
                        : reobservedResolution.Reason;
                blockedResult = BlockDefaultDispatchForRuntimeStability(
                    step,
                    sw,
                    targetText,
                    prefix,
                    approvalPrefix,
                    mode,
                    failureKind,
                    blockReason,
                    reason,
                    stability);
                return stability;
            }

            var selectedIdentity = WebTargetResultIdentityMapper.ToSelectedIdentity(reobservedResolution);
            var runtimeStability = SafeClickRuntimeStabilityEvaluator.Evaluate(
                manifest,
                selectedIdentity,
                reobserveAttempted: true,
                reobserveSucceeded: true);
            TrackRuntimeStability(runtimeStability);
            SetSafeClickRuntimeStabilityVars(prefix, runtimeStability);

            if (!runtimeStability.AllowsDefaultDispatch)
            {
                var missing = runtimeStability.StabilityVerdict == SafeClickRuntimeStabilityVerdict.Missing ||
                              runtimeStability.ReobserveMatch == RuntimeIdentityMatch.Missing;
                blockedResult = BlockDefaultDispatchForRuntimeStability(
                    step,
                    sw,
                    targetText,
                    prefix,
                    approvalPrefix,
                    mode,
                    FailureKind.Stale,
                    missing ? "ApprovalInvalidatedMissingIdentity" : "ApprovalInvalidated",
                    missing
                        ? "runtime identity missing before default FSM dispatch"
                        : "runtime identity changed before default FSM dispatch",
                    runtimeStability);
            }

            return runtimeStability;
        }
        catch (Exception ex)
        {
            var stability = SafeClickRuntimeStabilityEvaluator.Evaluate(
                manifest,
                observedIdentity: null,
                reobserveAttempted: true,
                reobserveSucceeded: false);
            TrackRuntimeStability(stability);
            blockedResult = BlockDefaultDispatchForRuntimeStability(
                step,
                sw,
                targetText,
                prefix,
                approvalPrefix,
                mode,
                FailureKind.Stale,
                "ApprovalInvalidatedMissingIdentity",
                $"runtime identity re-observe failed: {ex.Message}",
                stability);
            return stability;
        }
    }

    private RecipeStepRunResult BlockDefaultDispatchForRuntimeStability(
        RecipeStepDefinition step,
        Stopwatch sw,
        string targetText,
        string prefix,
        string approvalPrefix,
        string mode,
        FailureKind failureKind,
        string blockReason,
        string reason,
        SafeClickRuntimeStability stability)
    {
        if (blockReason.Contains("MissingIdentity", StringComparison.OrdinalIgnoreCase) ||
            blockReason.Contains("TargetNotFound", StringComparison.OrdinalIgnoreCase))
        {
            _safeClickDefaultBlockedByMissingIdentityCount++;
        }
        else if (blockReason.Contains("Invalidated", StringComparison.OrdinalIgnoreCase) ||
                 failureKind == FailureKind.Stale)
        {
            _safeClickDefaultBlockedByStaleIdentityCount++;
        }

        SetSafeClickVars(prefix, targetText, "blocked", reason);
        _ctx.Variables[prefix + ".method"] = "FSM safe.click";
        SetSafeClickRuntimeStabilityVars(prefix, stability with { BlockReason = blockReason });
        SetSafeClickFsmVars(prefix, StepState.Blocked, failureKind, blockReason, [reason]);
        TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
        sw.Stop();
        return new RecipeStepRunResult(
            step.Id,
            step.Kind,
            false,
            $"safe.click: {reason}",
            sw.ElapsedMilliseconds);
    }

    private SafeClickRuntimeStability ReobserveDesktopRuntimeStabilityForDefaultDispatch(
        RecipeStepDefinition step,
        Stopwatch sw,
        string targetText,
        string prefix,
        string approvalPrefix,
        string mode,
        ApprovalManifest? manifest,
        out DesktopTargetObservationResult? reobservedResolution,
        out RecipeStepRunResult? blockedResult)
    {
        reobservedResolution = null;
        blockedResult = null;
        _safeClickRuntimeStabilityCheckedCount++;
        _safeClickReobserveAttemptedCount++;

        var processName = EmptyToNull(R(step.Args?.GetValueOrDefault("proc")
            ?? step.Args?.GetValueOrDefault("processName")
            ?? step.Process));
        var windowTitle = EmptyToNull(R(step.Args?.GetValueOrDefault("window")
            ?? step.Args?.GetValueOrDefault("windowTitle")
            ?? step.Args?.GetValueOrDefault("titleContains")
            ?? step.Window
            ?? step.TitleContains));

        try
        {
            reobservedResolution = ResolveTargetObserveDesktop(targetText, processName, windowTitle);
            SetDesktopResolutionVars(prefix, reobservedResolution);
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);

            if (!reobservedResolution.Found)
            {
                var observedIdentity = DesktopTargetObservationResultIdentityMapper.ToSelectedIdentity(reobservedResolution);
                var stability = SafeClickRuntimeStabilityEvaluator.Evaluate(
                    manifest,
                    observedIdentity,
                    reobserveAttempted: true,
                    reobserveSucceeded: false);
                TrackRuntimeStability(stability);
                var failureKind = MapDesktopTargetObserveFailureKind(reobservedResolution);
                blockedResult = BlockDefaultDispatchForDesktopRuntimeStability(
                    step,
                    sw,
                    targetText,
                    prefix,
                    approvalPrefix,
                    mode,
                    failureKind,
                    failureKind == FailureKind.Ambiguous ? "ApprovalAmbiguous" : "ApprovalTargetNotFound",
                    string.IsNullOrWhiteSpace(reobservedResolution.Reason) ? "desktop target not found during runtime identity re-observe" : reobservedResolution.Reason,
                    stability);
                return stability;
            }

            var selectedIdentity = DesktopTargetObservationResultIdentityMapper.ToSelectedIdentity(reobservedResolution);
            var runtimeStability = SafeClickRuntimeStabilityEvaluator.Evaluate(
                manifest,
                selectedIdentity,
                reobserveAttempted: true,
                reobserveSucceeded: true);
            TrackRuntimeStability(runtimeStability);
            SetSafeClickRuntimeStabilityVars(prefix, runtimeStability);
            SetSafeClickDesktopRuntimeStabilityVars(prefix, runtimeStability);

            if (!runtimeStability.AllowsDefaultDispatch)
            {
                var missing = runtimeStability.StabilityVerdict == SafeClickRuntimeStabilityVerdict.Missing ||
                              runtimeStability.ReobserveMatch == RuntimeIdentityMatch.Missing;
                blockedResult = BlockDefaultDispatchForDesktopRuntimeStability(
                    step,
                    sw,
                    targetText,
                    prefix,
                    approvalPrefix,
                    mode,
                    FailureKind.Stale,
                    missing ? "ApprovalInvalidatedMissingIdentity" : "ApprovalInvalidated",
                    missing
                        ? "desktop runtime identity missing before default FSM dispatch"
                        : "desktop runtime identity changed before default FSM dispatch",
                    runtimeStability);
            }

            return runtimeStability;
        }
        catch (Exception ex)
        {
            var stability = SafeClickRuntimeStabilityEvaluator.Evaluate(
                manifest,
                observedIdentity: null,
                reobserveAttempted: true,
                reobserveSucceeded: false);
            TrackRuntimeStability(stability);
            blockedResult = BlockDefaultDispatchForDesktopRuntimeStability(
                step,
                sw,
                targetText,
                prefix,
                approvalPrefix,
                mode,
                FailureKind.Stale,
                "ApprovalInvalidatedMissingIdentity",
                $"desktop runtime identity re-observe failed: {ex.Message}",
                stability);
            return stability;
        }
    }

    private RecipeStepRunResult BlockDefaultDispatchForDesktopRuntimeStability(
        RecipeStepDefinition step,
        Stopwatch sw,
        string targetText,
        string prefix,
        string approvalPrefix,
        string mode,
        FailureKind failureKind,
        string blockReason,
        string reason,
        SafeClickRuntimeStability stability)
    {
        _safeClickDesktopDefaultBlockedByStaleIdentityCount++;
        if (blockReason.Contains("MissingIdentity", StringComparison.OrdinalIgnoreCase) ||
            blockReason.Contains("TargetNotFound", StringComparison.OrdinalIgnoreCase))
        {
            _safeClickDefaultBlockedByMissingIdentityCount++;
        }
        else if (blockReason.Contains("Invalidated", StringComparison.OrdinalIgnoreCase) ||
                 failureKind == FailureKind.Stale)
        {
            _safeClickDefaultBlockedByStaleIdentityCount++;
        }

        SetSafeClickVars(prefix, targetText, "blocked", reason);
        _ctx.Variables[prefix + ".method"] = "FSM safe.click";
        SetSafeClickRuntimeStabilityVars(prefix, stability with { BlockReason = blockReason });
        SetSafeClickDesktopRuntimeStabilityVars(prefix, stability with { BlockReason = blockReason });
        _ctx.Variables[prefix + ".desktopFsm.blockedByStaleIdentity"] = failureKind == FailureKind.Stale ? "true" : "false";
        _ctx.Variables[prefix + ".desktopFsm.blockedWithoutLegacyFallback"] = "true";
        SetSafeClickFsmVars(prefix, StepState.Blocked, failureKind, blockReason, [reason]);
        TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
        sw.Stop();
        return new RecipeStepRunResult(
            step.Id,
            step.Kind,
            false,
            $"safe.click: {reason}",
            sw.ElapsedMilliseconds);
    }

    private void InitSafeClickDefaultRouteVars(string prefix, SafeClickDefaultMode mode)
    {
        _ctx.Variables[prefix + ".fsm.defaultEnabled"] =
            mode is SafeClickDefaultMode.WebEligible or SafeClickDefaultMode.DesktopEligible or SafeClickDefaultMode.AllEligible
                ? "true"
                : "false";
        _ctx.Variables[prefix + ".fsm.defaultMode"] = SafeClickDefaultModePolicy.ToWireValue(mode);
        _ctx.Variables[prefix + ".fsm.routedByDefault"] = "false";
        _ctx.Variables[prefix + ".fsm.defaultRouteReason"] = "";
        _ctx.Variables[prefix + ".fsm.defaultRouteEligible"] = "false";
        _ctx.Variables[prefix + ".fsm.defaultRouteScope"] = "";
        _ctx.Variables[prefix + ".fsm.blockedWithoutLegacyFallback"] = "false";
    }

    private void InitSafeClickDesktopFsmVars(string prefix)
    {
        _ctx.Variables[prefix + ".desktopFsm.enabled"] = "false";
        _ctx.Variables[prefix + ".desktopFsm.eligible"] = "false";
        _ctx.Variables[prefix + ".desktopFsm.identitySource"] = "";
        _ctx.Variables[prefix + ".desktopFsm.rootHwndPresent"] = "false";
        _ctx.Variables[prefix + ".desktopFsm.routedOptIn"] = "false";
        _ctx.Variables[prefix + ".desktopFsm.blockReason"] = "";
        _ctx.Variables[prefix + ".desktopFsm.verdict"] = "";
        _ctx.Variables[prefix + ".desktopFsm.defaultEnabled"] = "false";
        _ctx.Variables[prefix + ".desktopFsm.routedByDefault"] = "false";
        _ctx.Variables[prefix + ".desktopFsm.defaultRouteEligible"] = "false";
        _ctx.Variables[prefix + ".desktopFsm.defaultRouteReason"] = "";
        _ctx.Variables[prefix + ".desktopFsm.defaultRouteScope"] = "";
        _ctx.Variables[prefix + ".desktopFsm.blockedWithoutLegacyFallback"] = "false";
        _ctx.Variables[prefix + ".desktopFsm.runtimeStabilityChecked"] = "false";
        _ctx.Variables[prefix + ".desktopFsm.runtimeStabilityVerdict"] = "";
        _ctx.Variables[prefix + ".desktopFsm.reobserveAttempted"] = "false";
        _ctx.Variables[prefix + ".desktopFsm.reobserveSucceeded"] = "false";
        _ctx.Variables[prefix + ".desktopFsm.blockedByStaleIdentity"] = "false";
    }

    private void SetSafeClickDefaultRouteVars(string prefix, bool routedByDefault, string reason, bool eligible, string scope)
    {
        _ctx.Variables[prefix + ".fsm.routedByDefault"] = routedByDefault ? "true" : "false";
        _ctx.Variables[prefix + ".fsm.defaultRouteReason"] = reason;
        _ctx.Variables[prefix + ".fsm.defaultRouteEligible"] = eligible ? "true" : "false";
        _ctx.Variables[prefix + ".fsm.defaultRouteScope"] = scope;
    }

    private void SetSafeClickDesktopDefaultVars(
        string prefix,
        SafeClickDefaultMode mode,
        bool defaultEnabled,
        bool routedByDefault,
        bool eligible,
        string reason,
        string scope)
    {
        _ctx.Variables[prefix + ".desktopFsm.defaultEnabled"] = defaultEnabled ? "true" : "false";
        _ctx.Variables[prefix + ".desktopFsm.routedByDefault"] = routedByDefault ? "true" : "false";
        _ctx.Variables[prefix + ".desktopFsm.defaultRouteEligible"] = eligible ? "true" : "false";
        _ctx.Variables[prefix + ".desktopFsm.defaultRouteReason"] = reason;
        _ctx.Variables[prefix + ".desktopFsm.defaultRouteScope"] = scope;
        _ctx.Variables[prefix + ".desktopFsm.enabled"] = defaultEnabled ? "true" : _ctx.Variables[prefix + ".desktopFsm.enabled"];
        _ctx.Variables[prefix + ".desktopFsm.identitySource"] = scope == "desktop-uia" ? "uia" : _ctx.Variables[prefix + ".desktopFsm.identitySource"];
        _ctx.Variables[prefix + ".fsm.defaultMode"] = SafeClickDefaultModePolicy.ToWireValue(mode);
    }

    private SafeClickLegacyDeprecationPolicy EvaluateSafeClickLegacyDeprecationPolicy(RecipeStepDefinition step)
    {
        var owner = step.Args?.GetValueOrDefault("legacyOwner")
            ?? step.Args?.GetValueOrDefault("legacyowner")
            ?? step.Args?.GetValueOrDefault("owner");
        var reason = step.Args?.GetValueOrDefault("legacyReason")
            ?? step.Args?.GetValueOrDefault("legacyreason")
            ?? step.Args?.GetValueOrDefault("reason");
        var reviewBy = step.Args?.GetValueOrDefault("legacyReviewBy")
            ?? step.Args?.GetValueOrDefault("legacyreviewby")
            ?? step.Args?.GetValueOrDefault("reviewBy")
            ?? step.Args?.GetValueOrDefault("reviewby");

        return SafeClickLegacyDeprecationPolicyEvaluator.Evaluate(
            isLegacyDispatch: true,
            owner,
            reason,
            reviewBy);
    }

    private void TrackSafeClickLegacyDeprecationPolicy(SafeClickLegacyDeprecationPolicy policy)
    {
        if (!policy.IsLegacyDispatch)
            return;

        if (policy.DeprecationSeverity == SafeClickLegacyDeprecationSeverity.Warning)
            _safeClickLegacyDeprecationWarningCount++;

        if (policy.IsCompliant)
        {
            _safeClickLegacyExplicitOptOutCompliantCount++;
            return;
        }

        _safeClickLegacyOptOutNonCompliantCount++;
        var violations = policy.ViolationReason.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (violations.Contains("MissingOwner", StringComparer.OrdinalIgnoreCase))
            _safeClickLegacyOptOutMissingOwnerCount++;
        if (violations.Contains("MissingReason", StringComparer.OrdinalIgnoreCase))
            _safeClickLegacyOptOutMissingReasonCount++;
        if (violations.Contains("MissingReviewBy", StringComparer.OrdinalIgnoreCase))
            _safeClickLegacyOptOutMissingReviewByCount++;
    }

    private void MarkSafeClickDefaultLegacyUse(string prefix)
    {
        if (_safeClickDefaultLegacyPrefixes.Add(prefix))
            _safeClickDefaultLegacyUseCount++;
    }

    private void SetSafeClickLegacyOptOutVars(string prefix, SafeClickLegacyDeprecationPolicy policy)
    {
        _ctx.Variables[prefix + ".legacy.explicitOptOut"] = "true";
        _ctx.Variables[prefix + ".legacy.deprecated"] = "true";
        _ctx.Variables[prefix + ".legacy.reason"] = "ExplicitLegacyDispatchPath";
        SetSafeClickLegacyDeprecationPolicyVars(prefix, policy);
    }

    private void SetSafeClickLegacyDeprecationPolicyVars(string prefix, SafeClickLegacyDeprecationPolicy policy)
    {
        _ctx.Variables[prefix + ".legacy.deprecationPolicy.enabled"] = "true";
        _ctx.Variables[prefix + ".legacy.deprecationPolicy.isLegacyDispatch"] = policy.IsLegacyDispatch ? "true" : "false";
        _ctx.Variables[prefix + ".legacy.deprecationPolicy.isDeprecated"] = policy.IsDeprecated ? "true" : "false";
        _ctx.Variables[prefix + ".legacy.deprecationPolicy.owner"] = policy.Owner;
        _ctx.Variables[prefix + ".legacy.deprecationPolicy.reason"] = policy.Reason;
        _ctx.Variables[prefix + ".legacy.deprecationPolicy.reviewBy"] = policy.ReviewBy;
        _ctx.Variables[prefix + ".legacy.deprecationPolicy.isCompliant"] = policy.IsCompliant ? "true" : "false";
        _ctx.Variables[prefix + ".legacy.deprecationPolicy.violationReason"] = policy.ViolationReason;
        _ctx.Variables[prefix + ".legacy.deprecationPolicy.severity"] = policy.DeprecationSeverity.ToString();
    }

    private RecipeStepRunResult BlockSafeClickLegacyRetired(
        RecipeStepDefinition step,
        Stopwatch sw,
        string targetText,
        string prefix,
        string approvalPrefix,
        string mode,
        string? dispatchPath,
        bool ineligibleAfterRetirement)
    {
        var preflight = ClickPreflightEvaluator.Evaluate(targetText);
        if (preflight.Blocked)
        {
            SetSafeClickVars(prefix, targetText, "blocked", preflight.Reason);
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
            sw.Stop();
            return new RecipeStepRunResult(step.Id, step.Kind, false,
                $"safe.click: target blocked: {preflight.Reason}", sw.ElapsedMilliseconds);
        }

        var bindingError = ValidateApprovalBinding(approvalPrefix, targetText, mode);
        if (bindingError != null)
        {
            SetSafeClickVars(prefix, targetText, "blocked", bindingError);
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
            sw.Stop();
            return new RecipeStepRunResult(step.Id, step.Kind, false,
                $"safe.click: approval binding invalid: {bindingError}", sw.ElapsedMilliseconds);
        }

        var execAllowed = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".executionAllowedInThisHito", "false");
        if (execAllowed != "true")
        {
            SetSafeClickVars(prefix, targetText, "blocked", "executionAllowedInThisHito is false");
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
            sw.Stop();
            return new RecipeStepRunResult(step.Id, step.Kind, false,
                "safe.click: approval does not allow execution.", sw.ElapsedMilliseconds);
        }

        var policy = SafeClickLegacyRetirementPolicyEvaluator.Evaluate(dispatchPath, ineligibleAfterRetirement);
        TrackSafeClickLegacyRetirementPolicy(policy);
        SetSafeClickLegacyRetirementPolicyVars(prefix, policy);
        SetSafeClickVars(prefix, targetText, "blocked", policy.Reason);
        SetSafeClickFsmVars(prefix, StepState.Blocked, FailureKind.PolicyDenied, policy.Reason, [policy.RequiredAction]);
        TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
        sw.Stop();
        return new RecipeStepRunResult(
            step.Id,
            step.Kind,
            false,
            $"safe.click: {policy.Reason}. {policy.RequiredAction}.",
            sw.ElapsedMilliseconds);
    }

    private void TrackSafeClickLegacyRetirementPolicy(SafeClickLegacyRetirementPolicy policy)
    {
        if (policy.Blocked)
            _safeClickLegacyExecutionBlockedCount++;
        if (policy.LegacyDispatchRejected)
            _safeClickLegacyDispatchRejectedCount++;
        if (policy.IneligibleAfterRetirement)
            _safeClickIneligibleBlockedAfterRetirementCount++;
        if (policy.Blocked)
            _safeClickSafeExecutorRequiredCount++;
    }

    private void SetSafeClickLegacyRetirementPolicyVars(string prefix, SafeClickLegacyRetirementPolicy policy)
    {
        _ctx.Variables[prefix + ".legacy.retired"] = policy.Retired ? "true" : "false";
        _ctx.Variables[prefix + ".legacy.retirementPolicy.enabled"] = policy.Enabled ? "true" : "false";
        _ctx.Variables[prefix + ".legacy.retirementPolicy.reason"] = policy.Reason;
        _ctx.Variables[prefix + ".legacy.retirementPolicy.blocked"] = policy.Blocked ? "true" : "false";
        _ctx.Variables[prefix + ".legacy.retirementPolicy.dispatchPath"] = policy.DispatchPath;
        _ctx.Variables[prefix + ".legacy.retirementPolicy.requiredAction"] = policy.RequiredAction;
        _ctx.Variables[prefix + ".retirement.blockedLegacyExecution"] = policy.Blocked ? "true" : "false";
        _ctx.Variables[prefix + ".retirement.blockReason"] = policy.Reason;
        _ctx.Variables[prefix + ".retirement.ineligibleAfterRetirement"] = policy.IneligibleAfterRetirement ? "true" : "false";
        _ctx.Variables[prefix + ".retirement.legacyDispatchRejected"] = policy.LegacyDispatchRejected ? "true" : "false";
    }

    private RecipeStepRunResult ExecuteSafeClickLegacy(
        RecipeStepDefinition step,
        Stopwatch sw,
        string targetText,
        string prefix,
        string approvalPrefix,
        string mode)
    {
        if (SafeClickLegacyRetirementPolicyEvaluator.Evaluate("", ineligibleAfterRetirement: true).Blocked)
            return BlockSafeClickLegacyRetired(step, sw, targetText, prefix, approvalPrefix, mode, dispatchPath: "", ineligibleAfterRetirement: true);

        // 1. Verify browser session owned (required only for non-controlled)
        var hasOwned = false;
        foreach (var key in _ctx.Variables.Keys.Where(k => k.EndsWith(".owned")))
            if (_ctx.Variables[key] == "true") { hasOwned = true; break; }

        if (!hasOwned && mode != "controlled")
        {
            SetSafeClickVars(prefix, targetText, "blocked", "no owned browser session");
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
            sw.Stop();
            return new RecipeStepRunResult(step.Id, step.Kind, false,
                "safe.click: no owned browser session active.", sw.ElapsedMilliseconds);
        }

        // 2. Preflight
        var pr = ClickPreflightEvaluator.Evaluate(targetText);
        if (pr.Blocked)
        {
            SetSafeClickVars(prefix, targetText, "blocked", pr.Reason);
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
            sw.Stop();
            return new RecipeStepRunResult(step.Id, step.Kind, false,
                $"safe.click: target blocked: {pr.Reason}", sw.ElapsedMilliseconds);
        }

        // 3. Approval binding check
        var bindingError = ValidateApprovalBinding(approvalPrefix, targetText, mode);
        if (bindingError != null)
        {
            SetSafeClickVars(prefix, targetText, "blocked", bindingError);
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
            sw.Stop();
            return new RecipeStepRunResult(step.Id, step.Kind, false,
                $"safe.click: approval binding invalid: {bindingError}", sw.ElapsedMilliseconds);
        }

        // 4. Approval check
        var execAllowed = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".executionAllowedInThisHito", "false");
        if (execAllowed != "true")
        {
            SetSafeClickVars(prefix, targetText, "blocked", "executionAllowedInThisHito is false");
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
            sw.Stop();
            return new RecipeStepRunResult(step.Id, step.Kind, false,
                "safe.click: approval does not allow execution.", sw.ElapsedMilliseconds);
        }

        // 5. Determine process
        var proc = step.Args?.GetValueOrDefault("proc") ?? "msedge";
        if (proc == "msedge" && !hasOwned)
        {
            // For controlled mode without browser, try to infer process from context
            foreach (var key in _ctx.Variables.Keys.Where(k => k.EndsWith(".process")))
            {
                proc = _ctx.Variables[key];
                break;
            }
        }

        // 6. Execute: try WebTargetResolver for browser, then UIA executor
        try
        {
            ActionResult result;

            if (hasOwned)
            {
                IntPtr sessionHwnd = IntPtr.Zero;
                foreach (var key in _ctx.Variables.Keys.Where(k => k.EndsWith(".hwnd")))
                {
                    var pfx = key[..^5];
                    if (_ctx.Variables.GetValueOrDefault(pfx + ".owned", "false") == "true" &&
                        long.TryParse(_ctx.Variables[key], out var hl))
                    {
                        sessionHwnd = new IntPtr(hl);
                        break;
                    }
                }

                if (sessionHwnd != IntPtr.Zero)
                {
                    var resolution = WebTargetResolver.Resolve(sessionHwnd, targetText, proc);
                    SetResolutionVars(prefix, resolution);
                    TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);

                    if (!resolution.Found)
                    {
                        if (!string.IsNullOrWhiteSpace(resolution.CandidatesJson))
                            _ctx.Variables[prefix + ".resolution.candidatesJson"] = resolution.CandidatesJson;

                        // Ambiguity: multiple candidates found → block, not fail
                        if (resolution.CandidateCount > 1 && resolution.Reason.Contains("ambiguous"))
                        {
                            SetSafeClickVars(prefix, targetText, "blocked", $"ambiguous: {resolution.CandidateCount} candidates");
                            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
                            sw.Stop();
                            return new RecipeStepRunResult(step.Id, step.Kind, false,
                                $"safe.click: ambiguous target ({resolution.CandidateCount} candidates)", sw.ElapsedMilliseconds);
                        }

                        SetSafeClickVars(prefix, targetText, "failed", resolution.Reason);
                        TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
                        sw.Stop();
                        return new RecipeStepRunResult(step.Id, step.Kind, false,
                            $"safe.click: {resolution.Reason}", sw.ElapsedMilliseconds);
                    }

                    // Execute click via FlaUI on the selected element
                    try
                    {
                        if (s_safeClickLegacyWebActionOverride != null)
                        {
                            var overrideResult = s_safeClickLegacyWebActionOverride(resolution, targetText);
                            SetSafeClickLegacyUsageVars(prefix, usedElClick: !resolution.HasInvoke, usedUiaActionExecutor: false);
                            if (overrideResult.Success)
                            {
                                SetSafeClickVars(prefix, targetText, "success", $"clicked via WebTargetResolver on {resolution.SelectedControlType}");
                                TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
                                sw.Stop();
                                return new RecipeStepRunResult(step.Id, step.Kind, true,
                                    $"safe.click: clicked '{targetText}' ({resolution.SelectedControlType})", sw.ElapsedMilliseconds);
                            }

                            SetSafeClickVars(prefix, targetText, "failed", overrideResult.Message ?? "legacy web action override failed");
                            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
                            sw.Stop();
                            return new RecipeStepRunResult(step.Id, step.Kind, false,
                                $"safe.click: {overrideResult.Message ?? "legacy web action override failed"}", sw.ElapsedMilliseconds);
                        }

                        if (long.TryParse(resolution.SelectedHwnd, out var selHwnd))
                        {
                            using var automation = new FlaUI.UIA3.UIA3Automation();
                            var el = WebTargetResolver.FindElementByName(new IntPtr(selHwnd), targetText);
                            if (el != null)
                            {
                                var usedElClick = !el.Patterns.Invoke.IsSupported;
                                if (!usedElClick)
                                    el.Patterns.Invoke.Pattern.Invoke();
                                else
                                    throw new InvalidOperationException("SafeClickLegacyRetired");

                                SetSafeClickLegacyUsageVars(prefix, usedElClick, usedUiaActionExecutor: false);
                                SetSafeClickVars(prefix, targetText, "success", $"clicked via WebTargetResolver on {resolution.SelectedControlType}");
                                TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
                                sw.Stop();
                                return new RecipeStepRunResult(step.Id, step.Kind, true,
                                    $"safe.click: clicked '{targetText}' ({resolution.SelectedControlType})", sw.ElapsedMilliseconds);
                            }
                        }
                        SetSafeClickVars(prefix, targetText, "failed", "element disappeared after resolution");
                        TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
                    }
                    catch (Exception ex)
                    {
                        SetSafeClickVars(prefix, targetText, "failed", ex.Message);
                        TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
                    }

                    sw.Stop();
                    return new RecipeStepRunResult(step.Id, step.Kind, false,
                        $"safe.click: click failed after target found. {_ctx.Variables.GetValueOrDefault(prefix + ".reason", "")}", sw.ElapsedMilliseconds);
                }
            }

            // The legacy desktop fallback is retired for safe.click.
            result = new ActionResult(false, "SafeClickLegacyRetired");

            SetSafeClickLegacyUsageVars(prefix, usedElClick: false, usedUiaActionExecutor: true);
            SetSafeClickVars(prefix, targetText, result.Success ? "success" : "failed",
                result.Message ?? (result.Success ? "clicked" : "failed"));
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);

            sw.Stop();
            return new RecipeStepRunResult(step.Id, step.Kind, result.Success,
                $"safe.click: {result.Message}", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            SetSafeClickVars(prefix, targetText, "failed", ex.Message);
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
            sw.Stop();
            return new RecipeStepRunResult(step.Id, step.Kind, false,
                $"safe.click: {ex.Message}", sw.ElapsedMilliseconds);
        }
    }

    private RecipeStepRunResult ExecuteSafeClickSafeExecutor(
        RecipeStepDefinition step,
        Stopwatch sw,
        string targetText,
        string prefix,
        string approvalPrefix,
        string mode,
        WebTargetResult? preResolvedResolution = null)
    {
        var hasOwned = false;
        foreach (var key in _ctx.Variables.Keys.Where(k => k.EndsWith(".owned")))
            if (_ctx.Variables[key] == "true") { hasOwned = true; break; }

        if (!hasOwned && mode != "controlled")
        {
            SetSafeClickVars(prefix, targetText, "blocked", "no owned browser session");
            SetSafeClickFsmVars(prefix, StepState.Blocked, FailureKind.SourceUnavailable, "OwnedBrowserSessionRequired", ["owned browser session is required for safe-executor dispatch"]);
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
            sw.Stop();
            return new RecipeStepRunResult(
                step.Id,
                step.Kind,
                false,
                "safe.click: no owned browser session active.",
                sw.ElapsedMilliseconds);
        }

        var preflight = ClickPreflightEvaluator.Evaluate(targetText);
        if (preflight.Blocked)
        {
            SetSafeClickVars(prefix, targetText, "blocked", preflight.Reason);
            SetSafeClickFsmVars(prefix, StepState.Blocked, FailureKind.PolicyDenied, "PreflightBlocked", [preflight.Reason]);
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
            sw.Stop();
            return new RecipeStepRunResult(
                step.Id,
                step.Kind,
                false,
                $"safe.click: target blocked: {preflight.Reason}",
                sw.ElapsedMilliseconds);
        }

        var bindingError = ValidateApprovalBinding(approvalPrefix, targetText, mode);
        if (bindingError != null)
        {
            SetSafeClickVars(prefix, targetText, "blocked", bindingError);
            SetSafeClickFsmVars(prefix, StepState.Blocked, FailureKind.PolicyDenied, "LegacyApprovalBindingInvalid", [bindingError]);
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
            sw.Stop();
            return new RecipeStepRunResult(
                step.Id,
                step.Kind,
                false,
                $"safe.click: approval binding invalid: {bindingError}",
                sw.ElapsedMilliseconds);
        }

        var execAllowed = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".executionAllowedInThisHito", "false");
        if (execAllowed != "true")
        {
            SetSafeClickVars(prefix, targetText, "blocked", "executionAllowedInThisHito is false");
            SetSafeClickFsmVars(prefix, StepState.Blocked, FailureKind.PolicyDenied, "ExecutionNotAllowedInThisHito", ["approval does not allow execution in this hito"]);
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
            sw.Stop();
            return new RecipeStepRunResult(
                step.Id,
                step.Kind,
                false,
                "safe.click: approval does not allow execution.",
                sw.ElapsedMilliseconds);
        }

        var manifest = TryReadApprovalManifest(approvalPrefix);
        var manifestValidation = ValidateSafeExecutorManifest(manifest);
        if (manifestValidation != null)
        {
            SetSafeClickVars(prefix, targetText, "blocked", manifestValidation.Value.Reason);
            SetSafeClickFsmVars(prefix, StepState.Blocked, manifestValidation.Value.FailureKind, manifestValidation.Value.BlockReason, [manifestValidation.Value.Reason]);
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
            sw.Stop();
            return new RecipeStepRunResult(
                step.Id,
                step.Kind,
                false,
                $"safe.click: {manifestValidation.Value.Reason}",
                sw.ElapsedMilliseconds);
        }

        var sessionHwnd = FindOwnedBrowserSessionHwnd();
        if (sessionHwnd == IntPtr.Zero)
        {
            SetSafeClickVars(prefix, targetText, "blocked", "safe-executor dispatch requires owned browser session");
            SetSafeClickFsmVars(prefix, StepState.Blocked, FailureKind.SourceUnavailable, "OwnedBrowserSessionRequired", ["safe-executor dispatch requires owned browser session"]);
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
            sw.Stop();
            return new RecipeStepRunResult(
                step.Id,
                step.Kind,
                false,
                "safe.click: no owned browser session active.",
                sw.ElapsedMilliseconds);
        }

        var proc = step.Args?.GetValueOrDefault("proc") ?? "msedge";
        if (proc == "msedge" && !hasOwned)
        {
            foreach (var key in _ctx.Variables.Keys.Where(k => k.EndsWith(".process")))
            {
                proc = _ctx.Variables[key];
                break;
            }
        }

        try
        {
            var resolution = preResolvedResolution ?? ResolveSafeClickTarget(sessionHwnd, targetText, proc);
            SetResolutionVars(prefix, resolution);
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);

            if (!resolution.Found)
            {
                if (!string.IsNullOrWhiteSpace(resolution.CandidatesJson))
                    _ctx.Variables[prefix + ".resolution.candidatesJson"] = resolution.CandidatesJson;

                var failureKind = resolution.CandidateCount > 1 || resolution.Reason.Contains("ambiguous", StringComparison.OrdinalIgnoreCase)
                    ? FailureKind.Ambiguous
                    : FailureKind.NotFound;
                var blockReason = failureKind == FailureKind.Ambiguous ? "ApprovalAmbiguous" : "ApprovalTargetNotFound";
                var reason = failureKind == FailureKind.Ambiguous
                    ? $"ambiguous: {resolution.CandidateCount} candidates"
                    : resolution.Reason;

                SetSafeClickVars(prefix, targetText, "blocked", reason);
                SetSafeClickFsmVars(prefix, StepState.Blocked, failureKind, blockReason, [reason]);
                sw.Stop();
                return new RecipeStepRunResult(
                    step.Id,
                    step.Kind,
                    false,
                    failureKind == FailureKind.Ambiguous
                        ? $"safe.click: ambiguous target ({resolution.CandidateCount} candidates)"
                        : $"safe.click: {resolution.Reason}",
                    sw.ElapsedMilliseconds);
            }

            var contract = BuildSafeExecutorContract(manifest!, "click", "", out var contractReason, out var contractFailureKind);
            if (contract == null)
            {
                SetSafeClickVars(prefix, targetText, "blocked", contractReason);
                SetSafeClickFsmVars(prefix, StepState.Blocked, contractFailureKind, "ApprovalV3StrongIdentityRequired", [contractReason]);
                sw.Stop();
                return new RecipeStepRunResult(
                    step.Id,
                    step.Kind,
                    false,
                    $"safe.click: {contractReason}",
                    sw.ElapsedMilliseconds);
            }

            var candidates = BuildSafeExecutorCandidates(prefix, resolution);
            if (candidates.Count == 0)
            {
                SetSafeClickVars(prefix, targetText, "blocked", "safe-executor path could not build any candidates");
                SetSafeClickFsmVars(prefix, StepState.Blocked, FailureKind.NotFound, "ApprovalTargetNotFound", ["safe-executor path could not build any candidates"]);
                sw.Stop();
                return new RecipeStepRunResult(
                    step.Id,
                    step.Kind,
                    false,
                    "safe.click: safe-executor path could not build any candidates.",
                    sw.ElapsedMilliseconds);
            }

            var expectedIdentity = contract.ExpectedIdentity ?? WebTargetResultIdentityMapper.ToSelectedIdentity(resolution);
            if (expectedIdentity == null)
            {
                SetSafeClickVars(prefix, targetText, "blocked", "safe-executor contract is missing expected identity");
                SetSafeClickFsmVars(prefix, StepState.Blocked, FailureKind.PolicyDenied, "ApprovalV3StrongIdentityRequired", ["safe-executor contract is missing expected identity"]);
                sw.Stop();
                return new RecipeStepRunResult(
                    step.Id,
                    step.Kind,
                    false,
                    "safe.click: safe-executor contract is missing expected identity.",
                    sw.ElapsedMilliseconds);
            }

            contract = contract with { ExpectedIdentity = expectedIdentity };

            var rootHwnd = ParseHandle(resolution.SelectedHwnd);
            if (rootHwnd == null || rootHwnd == IntPtr.Zero)
            {
                SetSafeClickVars(prefix, targetText, "blocked", "safe-executor path requires selected child hwnd");
                SetSafeClickFsmVars(prefix, StepState.Blocked, FailureKind.NotFound, "ApprovalTargetNotFound", ["safe-executor path requires selected child hwnd"]);
                sw.Stop();
                return new RecipeStepRunResult(
                    step.Id,
                    step.Kind,
                    false,
                    "safe.click: safe-executor path requires selected child hwnd.",
                    sw.ElapsedMilliseconds);
            }

            var patternExecutor = s_safeClickPatternExecutorFactoryOverride?.Invoke() ?? new UiaPatternExecutor();
            var ownershipMonitor = s_safeClickOwnershipMonitorFactoryOverride?.Invoke() ?? new DesktopOwnershipMonitor();
            var fsm = new SafeExecutionFsm(
                new ContractValidator(),
                new ApprovalBindingValidator(),
                ownershipMonitor,
                patternExecutor,
                new SafeClickStepVerifier());

            var fsmResult = fsm.Execute(new SafeExecutionRequest(
                Contract: contract,
                Candidates: candidates,
                DispatchRequest: new PatternExecutionRequest(
                    ActionKind: "click",
                    TargetRef: targetText,
                    ExpectedTargetName: resolution.SelectedName ?? targetText,
                    ProcessName: proc,
                    WindowTitleContains: resolution.SelectedWindowTitle,
                    Selector: contract.Selector!,
                    ExpectedIdentity: expectedIdentity,
                    RootHwnd: rootHwnd)));

            SetSafeClickFsmVars(prefix, fsmResult);

            var reasonMessage = fsmResult.Reasons.LastOrDefault()
                ?? (!string.IsNullOrWhiteSpace(fsmResult.BlockReason) ? fsmResult.BlockReason : fsmResult.FinalState.ToString());

            if (fsmResult.Success)
            {
                SetSafeClickVars(prefix, targetText, "success", "executed via safe-executor fsm");
                _ctx.Variables[prefix + ".method"] = "FSM safe.click";
                sw.Stop();
                return new RecipeStepRunResult(
                    step.Id,
                    step.Kind,
                    true,
                    $"safe.click: executed via safe-executor ({resolution.SelectedControlType ?? "unknown"})",
                    sw.ElapsedMilliseconds);
            }

            var blocked = fsmResult.FinalState == StepState.Blocked;
            SetSafeClickVars(prefix, targetText, blocked ? "blocked" : "failed", reasonMessage);
            _ctx.Variables[prefix + ".method"] = "FSM safe.click";
            sw.Stop();
            return new RecipeStepRunResult(
                step.Id,
                step.Kind,
                false,
                $"safe.click: {reasonMessage}",
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            SetSafeClickVars(prefix, targetText, "failed", ex.Message);
            SetSafeClickFsmVars(prefix, StepState.Failed, FailureKind.Unverified, "SafeExecutorError", [$"safe-executor path failed: {ex.Message}"]);
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
            sw.Stop();
            return new RecipeStepRunResult(
                step.Id,
                step.Kind,
                false,
                $"safe.click: {ex.Message}",
                sw.ElapsedMilliseconds);
        }
    }

    private RecipeStepRunResult ExecuteSafeClickDesktopSafeExecutor(
        RecipeStepDefinition step,
        Stopwatch sw,
        string targetText,
        string prefix,
        string approvalPrefix,
        string mode,
        DesktopTargetObservationResult? preResolvedResolution = null,
        bool routedByDefault = false)
    {
        if (!routedByDefault)
            _safeClickDesktopOptInRoutedCount++;
        _ctx.Variables[prefix + ".desktopFsm.enabled"] = "true";
        _ctx.Variables[prefix + ".desktopFsm.routedOptIn"] = routedByDefault ? "false" : "true";
        _ctx.Variables[prefix + ".desktopFsm.routedByDefault"] = routedByDefault ? "true" : "false";
        _ctx.Variables[prefix + ".desktopFsm.identitySource"] = "uia";

        var preflight = ClickPreflightEvaluator.Evaluate(targetText);
        if (preflight.Blocked)
        {
            return BlockDesktopSafeExecutor(
                step,
                sw,
                targetText,
                prefix,
                approvalPrefix,
                mode,
                FailureKind.PolicyDenied,
                "PreflightBlocked",
                preflight.Reason,
                countOptInBlock: !routedByDefault);
        }

        var bindingError = ValidateApprovalBinding(approvalPrefix, targetText, mode);
        if (bindingError != null)
        {
            return BlockDesktopSafeExecutor(
                step,
                sw,
                targetText,
                prefix,
                approvalPrefix,
                mode,
                FailureKind.PolicyDenied,
                "LegacyApprovalBindingInvalid",
                bindingError,
                countOptInBlock: !routedByDefault);
        }

        var execAllowed = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".executionAllowedInThisHito", "false");
        if (execAllowed != "true")
        {
            return BlockDesktopSafeExecutor(
                step,
                sw,
                targetText,
                prefix,
                approvalPrefix,
                mode,
                FailureKind.PolicyDenied,
                "ExecutionNotAllowedInThisHito",
                "approval does not allow execution in this hito",
                countOptInBlock: !routedByDefault);
        }

        var manifest = TryReadApprovalManifest(approvalPrefix);
        var manifestValidation = ValidateSafeExecutorManifest(manifest, requiredIdentitySource: "uia");
        if (manifestValidation != null)
        {
            return BlockDesktopSafeExecutor(
                step,
                sw,
                targetText,
                prefix,
                approvalPrefix,
                mode,
                manifestValidation.Value.FailureKind,
                manifestValidation.Value.BlockReason,
                manifestValidation.Value.Reason,
                countOptInBlock: !routedByDefault);
        }

        var processName = EmptyToNull(R(step.Args?.GetValueOrDefault("proc")
            ?? step.Args?.GetValueOrDefault("processName")
            ?? step.Process));
        var windowTitle = EmptyToNull(R(step.Args?.GetValueOrDefault("window")
            ?? step.Args?.GetValueOrDefault("windowTitle")
            ?? step.Args?.GetValueOrDefault("titleContains")
            ?? step.Window
            ?? step.TitleContains));

        try
        {
            var resolution = preResolvedResolution ?? ResolveTargetObserveDesktop(targetText, processName, windowTitle);
            SetDesktopResolutionVars(prefix, resolution);
            TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);

            if (!resolution.Found)
            {
                var failureKind = MapDesktopTargetObserveFailureKind(resolution);
                var blockReason = failureKind == FailureKind.Ambiguous ? "ApprovalAmbiguous" : "ApprovalTargetNotFound";
                return BlockDesktopSafeExecutor(
                    step,
                    sw,
                    targetText,
                    prefix,
                    approvalPrefix,
                    mode,
                    failureKind,
                    blockReason,
                    string.IsNullOrWhiteSpace(resolution.Reason) ? "desktop target not found" : resolution.Reason,
                    countOptInBlock: !routedByDefault);
            }

            var surfaceDecision = ExecutorSurfacePolicy.Decide(resolution.SelectedControlType, resolution.HasInvoke);
            if (!surfaceDecision.Allowed)
            {
                return BlockDesktopSafeExecutor(
                    step,
                    sw,
                    targetText,
                    prefix,
                    approvalPrefix,
                    mode,
                    surfaceDecision.FailureKind ?? FailureKind.PolicyDenied,
                    "ExecutorSurfaceDenied",
                    surfaceDecision.Reason,
                    countOptInBlock: !routedByDefault);
            }

            var rootHwnd = ParseHandle(resolution.RootHwnd);
            _ctx.Variables[prefix + ".desktopFsm.rootHwndPresent"] =
                rootHwnd is { } handle && handle != IntPtr.Zero ? "true" : "false";
            if (rootHwnd == null || rootHwnd == IntPtr.Zero)
            {
                return BlockDesktopSafeExecutor(
                    step,
                    sw,
                    targetText,
                    prefix,
                    approvalPrefix,
                    mode,
                    FailureKind.PolicyDenied,
                    "DesktopRootRequired",
                    "desktop safe-executor dispatch requires observed root hwnd",
                    countOptInBlock: !routedByDefault);
            }

            var contract = BuildSafeExecutorContract(manifest!, "click", "", out var contractReason, out var contractFailureKind);
            if (contract == null)
            {
                return BlockDesktopSafeExecutor(
                    step,
                    sw,
                    targetText,
                    prefix,
                    approvalPrefix,
                    mode,
                    contractFailureKind,
                    "ApprovalV3StrongIdentityRequired",
                    contractReason,
                    countOptInBlock: !routedByDefault);
            }

            var candidates = BuildDesktopSafeExecutorCandidates(prefix, resolution);
            if (candidates.Count == 0)
            {
                return BlockDesktopSafeExecutor(
                    step,
                    sw,
                    targetText,
                    prefix,
                    approvalPrefix,
                    mode,
                    FailureKind.NotFound,
                    "ApprovalTargetNotFound",
                    "desktop safe-executor path could not build any candidates",
                    countOptInBlock: !routedByDefault);
            }

            var expectedIdentity = contract.ExpectedIdentity ?? DesktopTargetObservationResultIdentityMapper.ToSelectedIdentity(resolution);
            if (expectedIdentity == null)
            {
                return BlockDesktopSafeExecutor(
                    step,
                    sw,
                    targetText,
                    prefix,
                    approvalPrefix,
                    mode,
                    FailureKind.PolicyDenied,
                    "ApprovalV3StrongIdentityRequired",
                    "desktop safe-executor contract is missing expected identity",
                    countOptInBlock: !routedByDefault);
            }

            contract = contract with { ExpectedIdentity = expectedIdentity };

            var patternExecutor = s_safeClickPatternExecutorFactoryOverride?.Invoke() ?? new UiaPatternExecutor();
            var ownershipMonitor = s_safeClickOwnershipMonitorFactoryOverride?.Invoke() ?? new DesktopOwnershipMonitor();
            var fsm = new SafeExecutionFsm(
                new ContractValidator(),
                new ApprovalBindingValidator(),
                ownershipMonitor,
                patternExecutor,
                new SafeClickStepVerifier());

            var fsmResult = fsm.Execute(new SafeExecutionRequest(
                Contract: contract,
                Candidates: candidates,
                DispatchRequest: new PatternExecutionRequest(
                    ActionKind: "click",
                    TargetRef: targetText,
                    ExpectedTargetName: resolution.SelectedName ?? targetText,
                    ProcessName: resolution.SelectedProcessName ?? processName,
                    WindowTitleContains: resolution.SelectedWindowTitle ?? windowTitle,
                    Selector: contract.Selector!,
                    ExpectedIdentity: expectedIdentity,
                    RootHwnd: rootHwnd)));

            SetSafeClickFsmVars(prefix, fsmResult);
            var reasonMessage = fsmResult.Reasons.LastOrDefault()
                ?? (!string.IsNullOrWhiteSpace(fsmResult.BlockReason) ? fsmResult.BlockReason : fsmResult.FinalState.ToString());

            if (fsmResult.Success)
            {
                _ctx.Variables[prefix + ".desktopFsm.eligible"] = "true";
                _ctx.Variables[prefix + ".desktopFsm.verdict"] = "Succeeded";
                SetSafeClickVars(prefix, targetText, "success", "executed via desktop safe-executor fsm");
                _ctx.Variables[prefix + ".method"] = "FSM safe.click";
                sw.Stop();
                return new RecipeStepRunResult(
                    step.Id,
                    step.Kind,
                    true,
                    $"safe.click: executed via desktop safe-executor ({resolution.SelectedControlType ?? "unknown"})",
                    sw.ElapsedMilliseconds);
            }

            if (!routedByDefault)
                _safeClickDesktopOptInBlockedCount++;
            _ctx.Variables[prefix + ".desktopFsm.blockReason"] = fsmResult.BlockReason;
            _ctx.Variables[prefix + ".desktopFsm.verdict"] = fsmResult.FinalState.ToString();
            _ctx.Variables[prefix + ".fsm.blockedWithoutLegacyFallback"] = "true";
            SetSafeClickVars(prefix, targetText, fsmResult.FinalState == StepState.Blocked ? "blocked" : "failed", reasonMessage);
            _ctx.Variables[prefix + ".method"] = "FSM safe.click";
            sw.Stop();
            return new RecipeStepRunResult(
                step.Id,
                step.Kind,
                false,
                $"safe.click: {reasonMessage}",
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return BlockDesktopSafeExecutor(
                step,
                sw,
                targetText,
                prefix,
                approvalPrefix,
                mode,
                FailureKind.Unverified,
                "SafeExecutorError",
                $"desktop safe-executor path failed: {ex.Message}",
                countOptInBlock: !routedByDefault);
        }
    }

    private RecipeStepRunResult BlockDesktopSafeExecutor(
        RecipeStepDefinition step,
        Stopwatch sw,
        string targetText,
        string prefix,
        string approvalPrefix,
        string mode,
        FailureKind failureKind,
        string blockReason,
        string reason,
        bool countOptInBlock = true)
    {
        if (countOptInBlock)
            _safeClickDesktopOptInBlockedCount++;
        _ctx.Variables[prefix + ".desktopFsm.eligible"] = "false";
        _ctx.Variables[prefix + ".desktopFsm.blockReason"] = blockReason;
        _ctx.Variables[prefix + ".desktopFsm.verdict"] = "Blocked";
        _ctx.Variables[prefix + ".fsm.blockedWithoutLegacyFallback"] = "true";
        _ctx.Variables[prefix + ".desktopFsm.blockedWithoutLegacyFallback"] = "true";
        SetSafeClickVars(prefix, targetText, "blocked", reason);
        _ctx.Variables[prefix + ".method"] = "FSM safe.click";
        SetSafeClickFsmVars(prefix, StepState.Blocked, failureKind, blockReason, [reason]);
        TrySetSafeClickPlanVars(prefix, approvalPrefix, targetText, mode);
        sw.Stop();
        return new RecipeStepRunResult(
            step.Id,
            step.Kind,
            false,
            $"safe.click: {reason}",
            sw.ElapsedMilliseconds);
    }

    private void SetSafeClickVars(string prefix, string targetText, string result, string reason)
    {
        _ctx.Variables[prefix + ".executed"] = result == "success" ? "true" : "false";
        _ctx.Variables[prefix + ".targetText"] = targetText;
        _ctx.Variables[prefix + ".method"] = "UIA safe.click";
        _ctx.Variables[prefix + ".reason"] = reason;
        _ctx.Variables[prefix + ".result"] = result;
        _ctx.Variables[prefix + ".summary"] = $"safe.click {targetText}: {result} ({reason})";
    }

    private void TrySetSafeClickPlanVars(string prefix, string approvalPrefix, string targetText, string mode)
    {
        var manifest = TryReadApprovalManifest(approvalPrefix);
        var observedIdentity = ReadSafeClickObservedIdentity(prefix) ?? ReadDesktopApprovalIdentityForPlanning(manifest);
        var invokePatternAvailable = ReadSafeClickInvokeAvailability(prefix);
        invokePatternAvailable ??= ReadApprovalInvokeAvailability(approvalPrefix);
        var (usedElClick, usedUiaActionExecutor) = ReadSafeClickLegacyUsage(prefix);
        var desktopRootAvailable = IsDesktopIdentitySource(manifest) &&
                                   !string.IsNullOrWhiteSpace(_ctx.Variables.GetValueOrDefault(approvalPrefix + ".identity.rootHwnd", ""));

        try
        {
            var plan = SafeClickPlanner.Plan(new SafeClickPlanInput
            {
                Mode = mode,
                TargetText = targetText,
                ActionKind = "click",
                Manifest = manifest,
                Candidates = ReadSafeClickPlanCandidates(prefix, manifest, approvalPrefix),
                Reversible = false
            });

            _ctx.Variables[prefix + ".plan.projectedState"] = plan.ProjectedState.ToString();
            _ctx.Variables[prefix + ".plan.failureKind"] = plan.FailureKind?.ToString() ?? "";
            _ctx.Variables[prefix + ".plan.blockReason"] = plan.BlockReason ?? "";
            _ctx.Variables[prefix + ".plan.identityStrength"] = plan.IdentityStrength.ToString();
            _ctx.Variables[prefix + ".plan.contractValid"] = plan.ContractValid ? "true" : "false";
            _ctx.Variables[prefix + ".plan.bindingVerdict"] = plan.BindingVerdict ?? "";
            _ctx.Variables[prefix + ".plan.parityAgrees"] = plan.ParityAgrees.HasValue
                ? (plan.ParityAgrees.Value ? "true" : "false")
                : "";
            _ctx.Variables[prefix + ".plan.wouldDispatch"] = plan.WouldDispatch ? "true" : "false";
            _ctx.Variables[prefix + ".plan.wouldUseUnsafeFallback"] = plan.WouldUseUnsafeFallback ? "true" : "false";
            _ctx.Variables[prefix + ".plan.reasons"] = plan.Reasons.Count == 0 ? "" : string.Join(" | ", plan.Reasons);

            var readiness = SafeClickShadowReadinessEvaluator.Evaluate(
                manifest,
                plan,
                observedIdentity,
                invokePatternAvailable,
                usesElClick: usedElClick,
                usesUiaActionExecutor: usedUiaActionExecutor,
                desktopRootAvailable: desktopRootAvailable);
            SetSafeClickFsmReadyVars(prefix, readiness);
            _safeClickShadowReadiness[prefix] = readiness;
        }
        catch (Exception ex)
        {
            var fallbackPlan = new SafeClickExecutionPlan(
                ProjectedState: StepState.Blocked,
                FailureKind: FailureKind.Unverified,
                BlockReason: "PlannerUnavailable",
                IdentityStrength: IdentityStrength.None,
                ContractValid: false,
                BindingVerdict: null,
                ParityAgrees: null,
                WouldDispatch: false,
                WouldUseUnsafeFallback: false,
                Reasons: [$"planner unavailable: {ex.Message}"]);

            _ctx.Variables[prefix + ".plan.projectedState"] = fallbackPlan.ProjectedState.ToString();
            _ctx.Variables[prefix + ".plan.failureKind"] = fallbackPlan.FailureKind?.ToString() ?? "";
            _ctx.Variables[prefix + ".plan.blockReason"] = fallbackPlan.BlockReason ?? "";
            _ctx.Variables[prefix + ".plan.identityStrength"] = fallbackPlan.IdentityStrength.ToString();
            _ctx.Variables[prefix + ".plan.contractValid"] = "false";
            _ctx.Variables[prefix + ".plan.bindingVerdict"] = "";
            _ctx.Variables[prefix + ".plan.parityAgrees"] = "";
            _ctx.Variables[prefix + ".plan.wouldDispatch"] = "false";
            _ctx.Variables[prefix + ".plan.wouldUseUnsafeFallback"] = "false";
            _ctx.Variables[prefix + ".plan.reasons"] = fallbackPlan.Reasons.Count == 0 ? "" : string.Join(" | ", fallbackPlan.Reasons);

            var readiness = SafeClickShadowReadinessEvaluator.Evaluate(
                manifest,
                fallbackPlan,
                observedIdentity,
                invokePatternAvailable,
                usesElClick: usedElClick,
                usesUiaActionExecutor: usedUiaActionExecutor,
                desktopRootAvailable: desktopRootAvailable);
            SetSafeClickFsmReadyVars(prefix, readiness);
            _safeClickShadowReadiness[prefix] = readiness;
        }
    }

    private ApprovalManifest? TryReadApprovalManifest(string approvalPrefix)
    {
        var target = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".targetText", "");
        var mode = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".mode", "");
        var policyVersion = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".policyVersion", "");
        var evidenceHash = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".evidenceHash", "");

        if (string.IsNullOrWhiteSpace(target) &&
            string.IsNullOrWhiteSpace(mode) &&
            string.IsNullOrWhiteSpace(policyVersion) &&
            string.IsNullOrWhiteSpace(evidenceHash))
        {
            return null;
        }

        SelectorDefinition? selector = null;
        var selectorJson = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".identity.selector", "");
        if (!string.IsNullOrWhiteSpace(selectorJson))
        {
            try
            {
                selector = JsonSerializer.Deserialize<SelectorDefinition>(selectorJson);
            }
            catch
            {
                selector = null;
            }
        }

        var identityStrength = IdentityStrength.None;
        if (Enum.TryParse(_ctx.Variables.GetValueOrDefault(approvalPrefix + ".identity.strength", ""), ignoreCase: true, out IdentityStrength parsedStrength))
            identityStrength = parsedStrength;

        bool? shadowAgrees = null;
        var rawShadowAgrees = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".identity.shadowAgreesWithLegacy", "");
        if (bool.TryParse(rawShadowAgrees, out var parsedShadowAgrees))
            shadowAgrees = parsedShadowAgrees;

        var manifestJson = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".manifestJson", "");
        var approvedInputBinding = ReadApprovedInputBindingFromManifestJson(manifestJson);

        return new ApprovalManifest
        {
            TargetText = target,
            Mode = mode,
            Decision = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".decision", ""),
            RiskCategory = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".riskCategory", ""),
            RiskLevel = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".riskLevel", ""),
            EvidenceHash = evidenceHash,
            PolicyVersion = policyVersion,
            ExecutionAllowedInThisHito = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".executionAllowedInThisHito", "false") == "true",
            IdentitySchemaVersion = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".identity.schemaVersion", ""),
            ApprovedIdentityDigest = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".identity.digest", ""),
            ApprovedSelector = selector,
            IdentityStrength = identityStrength,
            IdentitySource = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".identity.source", ""),
            ShadowAgreesWithLegacy = shadowAgrees,
            IdentityBindingHash = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".identity.bindingHash", ""),
            ManifestJson = manifestJson,
            ApprovedInputBinding = approvedInputBinding,
            ApprovedValueDigest = approvedInputBinding?.ApprovedValueDigest,
            ApprovedInputBindingHash = approvedInputBinding?.ApprovedInputBindingHash,
            ApprovedInputBindingVersion = approvedInputBinding?.BindingVersion,
            ApprovedInputDigestAlgorithm = approvedInputBinding?.ApprovedValueDigestAlgorithm,
            ApprovedInputCanonicalization = approvedInputBinding?.ApprovedValueCanonicalization
        };
    }

    private static ApprovedInputBinding? ReadApprovedInputBindingFromManifestJson(string manifestJson)
    {
        if (string.IsNullOrWhiteSpace(manifestJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(manifestJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("approvedInput", out var approvedInput) &&
                approvedInput.ValueKind == JsonValueKind.Object)
            {
                return TryReadApprovedInputBinding(approvedInput);
            }

            var bindingVersion = ReadJsonString(root, "approvedInputBindingVersion");
            var actionKind = ReadJsonString(root, "approvedInputActionKind");
            var approvedValueDigest = ReadJsonString(root, "approvedValueDigest");
            var digestAlgorithm = ReadJsonString(root, "approvedValueDigestAlgorithm");
            var canonicalization = ReadJsonString(root, "approvedValueCanonicalization");
            var bindingHash = ReadJsonString(root, "approvedInputBindingHash");
            var identityBindingHash = ReadJsonString(root, "identityBindingHash");

            if (string.IsNullOrWhiteSpace(bindingVersion) ||
                string.IsNullOrWhiteSpace(actionKind) ||
                string.IsNullOrWhiteSpace(approvedValueDigest) ||
                string.IsNullOrWhiteSpace(digestAlgorithm) ||
                string.IsNullOrWhiteSpace(canonicalization) ||
                string.IsNullOrWhiteSpace(bindingHash) ||
                string.IsNullOrWhiteSpace(identityBindingHash))
            {
                return null;
            }

            return new ApprovedInputBinding(
                bindingVersion,
                actionKind,
                identityBindingHash,
                identityBindingHash,
                approvedValueDigest,
                digestAlgorithm,
                canonicalization,
                bindingHash);
        }
        catch
        {
            return null;
        }
    }

    private static ApprovedInputBinding? TryReadApprovedInputBinding(JsonElement approvedInput)
    {
        var bindingVersion = ReadJsonString(approvedInput, "bindingVersion");
        var actionKind = ReadJsonString(approvedInput, "actionKind");
        var approvalRef = ReadJsonString(approvedInput, "approvalRef");
        var identityBindingHash = ReadJsonString(approvedInput, "identityBindingHash");
        var approvedValueDigest = ReadJsonString(approvedInput, "approvedValueDigest");
        var digestAlgorithm = ReadJsonString(approvedInput, "approvedValueDigestAlgorithm");
        var canonicalization = ReadJsonString(approvedInput, "approvedValueCanonicalization");
        var bindingHash = ReadJsonString(approvedInput, "approvedInputBindingHash");

        if (string.IsNullOrWhiteSpace(bindingVersion) ||
            string.IsNullOrWhiteSpace(actionKind) ||
            string.IsNullOrWhiteSpace(approvalRef) ||
            string.IsNullOrWhiteSpace(identityBindingHash) ||
            string.IsNullOrWhiteSpace(approvedValueDigest) ||
            string.IsNullOrWhiteSpace(digestAlgorithm) ||
            string.IsNullOrWhiteSpace(canonicalization) ||
            string.IsNullOrWhiteSpace(bindingHash))
        {
            return null;
        }

        return new ApprovedInputBinding(
            bindingVersion,
            actionKind,
            approvalRef,
            identityBindingHash,
            approvedValueDigest,
            digestAlgorithm,
            canonicalization,
            bindingHash);
    }

    private IReadOnlyList<WebCandidate> ReadSafeClickPlanCandidates(
        string prefix,
        ApprovalManifest? manifest = null,
        string? approvalPrefix = null)
    {
        var candidatesJson = _ctx.Variables.GetValueOrDefault(prefix + ".resolution.candidatesJson", "");
        if (!string.IsNullOrWhiteSpace(candidatesJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(candidatesJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return doc.RootElement
                        .EnumerateArray()
                        .Select(element => new WebCandidate
                        {
                            RuntimeId = ReadJsonString(element, "runtimeId"),
                            Name = ReadJsonString(element, "name"),
                            ControlType = ReadJsonString(element, "controlType"),
                            AutomationId = ReadJsonString(element, "automationId"),
                            BoundingRect = ReadJsonString(element, "boundingRect"),
                            ClassName = ReadJsonString(element, "className"),
                            HelpText = ReadJsonString(element, "helpText"),
                            LegacyName = ReadJsonString(element, "legacyName"),
                            FrameworkId = ReadJsonString(element, "frameworkId"),
                            AncestorPath = ReadJsonString(element, "ancestorPath"),
                            ProcessName = ReadJsonString(element, "processName"),
                            WindowTitle = ReadJsonString(element, "windowTitle"),
                            IsEnabled = ReadJsonBool(element, "isEnabled", defaultValue: true),
                            IsOffscreen = ReadJsonBool(element, "isOffscreen", defaultValue: false),
                            HasInvoke = ReadJsonBool(element, "hasInvoke", defaultValue: false)
                        })
                        .ToList();
                }
            }
            catch
            {
            }
        }

        var selectedName = _ctx.Variables.GetValueOrDefault(prefix + ".resolution.selectedName", "");
        var selectedControlType = _ctx.Variables.GetValueOrDefault(prefix + ".resolution.selectedControlType", "");
        var selectedBoundingRect = _ctx.Variables.GetValueOrDefault(prefix + ".resolution.selectedBoundingRect", "");
        if (string.IsNullOrWhiteSpace(selectedName) &&
            string.IsNullOrWhiteSpace(selectedControlType) &&
            string.IsNullOrWhiteSpace(selectedBoundingRect))
        {
            var approvedIdentity = manifest?.ApprovedSelector?.ExpectedIdentity;
            if (approvedIdentity != null &&
                string.Equals(manifest?.IdentitySource, "uia", StringComparison.OrdinalIgnoreCase))
            {
                return
                [
                    new WebCandidate
                    {
                        RuntimeId = EmptyToNull(approvedIdentity.RuntimeId),
                        Name = approvedIdentity.Name,
                        ControlType = approvedIdentity.EffectiveControlType,
                        AutomationId = EmptyToNull(approvedIdentity.AutomationId),
                        BoundingRect = approvedIdentity.BoundsHint,
                        ClassName = EmptyToNull(approvedIdentity.ClassName),
                        FrameworkId = EmptyToNull(approvedIdentity.FrameworkId),
                        AncestorPath = EmptyToNull(approvedIdentity.AncestorPath),
                        ProcessName = EmptyToNull(approvedIdentity.ProcessName),
                        WindowTitle = EmptyToNull(approvedIdentity.WindowTitle),
                        IsEnabled = true,
                        IsOffscreen = false,
                        HasInvoke = !string.IsNullOrWhiteSpace(approvalPrefix) &&
                                    _ctx.Variables.GetValueOrDefault(approvalPrefix + ".identity.hasInvoke", "false") == "true"
                    }
                ];
            }

            return Array.Empty<WebCandidate>();
        }

        return
        [
            new WebCandidate
            {
                RuntimeId = EmptyToNull(_ctx.Variables.GetValueOrDefault(prefix + ".resolution.identity.runtimeId", "")),
                Name = selectedName,
                ControlType = selectedControlType,
                AutomationId = EmptyToNull(_ctx.Variables.GetValueOrDefault(prefix + ".resolution.identity.automationId", "")),
                BoundingRect = selectedBoundingRect,
                ClassName = EmptyToNull(_ctx.Variables.GetValueOrDefault(prefix + ".resolution.identity.className", "")),
                FrameworkId = EmptyToNull(_ctx.Variables.GetValueOrDefault(prefix + ".resolution.identity.frameworkId", "")),
                AncestorPath = EmptyToNull(_ctx.Variables.GetValueOrDefault(prefix + ".resolution.identity.ancestorPath", "")),
                ProcessName = EmptyToNull(_ctx.Variables.GetValueOrDefault(prefix + ".resolution.identity.processName", "")),
                WindowTitle = EmptyToNull(_ctx.Variables.GetValueOrDefault(prefix + ".resolution.identity.windowTitle", "")),
                IsEnabled = true,
                IsOffscreen = false,
                HasInvoke = _ctx.Variables.GetValueOrDefault(prefix + ".resolution.hasInvoke", "false") == "true"
            }
        ];
    }

    private static string? ReadJsonString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static bool ReadJsonBool(JsonElement element, string propertyName, bool defaultValue)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? property.GetBoolean()
            : defaultValue;
    }

    private string? ValidateApprovalBinding(string approvalPrefix, string targetText, string mode)
    {
        var approvalTarget = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".targetText", "");
        if (!approvalTarget.Equals(targetText, StringComparison.Ordinal))
            return $"target mismatch: approval='{approvalTarget}', click='{targetText}'";

        var approvalMode = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".mode", "");
        if (!approvalMode.Equals(mode, StringComparison.Ordinal))
            return $"mode mismatch: approval='{approvalMode}', click='{mode}'";

        var policyVersion = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".policyVersion", "");
        if (policyVersion != ApprovalManifestBuilder.PolicyVersion)
            return $"unsupported approval policy version '{policyVersion}'";

        var decision = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".decision", "");
        if (decision == "requiresReview")
            return "requiresReview is never executable";
        if (decision is not ("allowedForFuture" or "requiresApproval"))
            return $"decision '{decision}' is not executable";

        var riskCategory = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".riskCategory", "");
        var riskLevel = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".riskLevel", "");
        var evidenceHash = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".evidenceHash", "");
        if (string.IsNullOrWhiteSpace(evidenceHash))
            return "missing approval evidenceHash";

        var expectedHash = ApprovalManifestBuilder.ComputeEvidenceHash(
            approvalTarget,
            approvalMode,
            decision,
            riskCategory,
            riskLevel,
            policyVersion);
        if (!evidenceHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
            return "approval evidenceHash mismatch";

        return null;
    }

    private (FailureKind FailureKind, string BlockReason, string Reason)? ValidateSafeExecutorManifest(
        ApprovalManifest? manifest,
        string requiredIdentitySource = "web-uia")
    {
        if (manifest == null)
        {
            return (
                FailureKind.PolicyDenied,
                "ApprovalV3StrongIdentityRequired",
                "safe-executor dispatch requires approval manifest v3 with strong identity");
        }

        if (!string.Equals(manifest.IdentitySchemaVersion, ApprovalManifestBuilder.IdentitySchemaVersion, StringComparison.Ordinal) ||
            manifest.IdentityStrength != IdentityStrength.Strong ||
            string.IsNullOrWhiteSpace(manifest.ApprovedIdentityDigest) ||
            manifest.ApprovedSelector == null ||
            string.IsNullOrWhiteSpace(manifest.IdentityBindingHash) ||
            (!string.IsNullOrWhiteSpace(requiredIdentitySource) &&
             !string.Equals(manifest.IdentitySource, requiredIdentitySource, StringComparison.OrdinalIgnoreCase)))
        {
            return (
                FailureKind.PolicyDenied,
                "ApprovalV3StrongIdentityRequired",
                string.IsNullOrWhiteSpace(requiredIdentitySource)
                    ? "safe-executor dispatch requires approval manifest v3 with strong identity"
                    : $"safe-executor dispatch requires approval manifest v3 with strong {requiredIdentitySource} identity");
        }

        if (manifest.ApprovedSelector.ExpectedIdentity == null || !manifest.ApprovedSelector.ExpectedIdentity.IsStrong)
        {
            return (
                FailureKind.PolicyDenied,
                "ApprovalV3StrongIdentityRequired",
                "safe-executor dispatch requires approved selector with strong expected identity");
        }

        return null;
    }

    private RecipeSafetyContract? BuildSafeExecutorContract(
        ApprovalManifest manifest,
        string actionKind,
        string approvedValueDigest,
        out string reason,
        out FailureKind failureKind,
        string approvedInputBindingHash = "",
        string approvedInputBindingVersion = "",
        string approvedInputDigestAlgorithm = "")
    {
        var binding = ApprovalManifestBuilder.TryBuildApprovalBinding(manifest);
        var expectedIdentity = manifest.ApprovedSelector?.ExpectedIdentity;
        var selector = manifest.ApprovedSelector;

        if (binding == null || selector == null || expectedIdentity == null || !expectedIdentity.IsStrong)
        {
            reason = "safe-executor dispatch requires approval manifest v3 with strong selector-bound identity";
            failureKind = FailureKind.PolicyDenied;
            return null;
        }

        reason = "";
        failureKind = FailureKind.PolicyDenied;
        var normalizedActionKind = string.IsNullOrWhiteSpace(actionKind) ? "click" : actionKind.Trim().ToLowerInvariant();
        var isRead = string.Equals(normalizedActionKind, "read", StringComparison.OrdinalIgnoreCase);
        var isType = string.Equals(normalizedActionKind, "type", StringComparison.OrdinalIgnoreCase);

        return new RecipeSafetyContract(
            SchemaVersion: 1,
            ContractId: normalizedActionKind switch
            {
                "read" => $"safe-read-{manifest.IdentityBindingHash}",
                "type" => $"safe-type-{manifest.IdentityBindingHash}-{approvedInputBindingHash}",
                _ => $"safe-click-{manifest.IdentityBindingHash}"
            },
            ActionKind: normalizedActionKind,
            ExpectedIdentity: expectedIdentity,
            Selector: selector,
            WindowConstraints: new ExecutionWindowConstraints(
                LocalPilotOnly: false,
                ExternalNavigationBlocked: true),
            Reversible: isRead,
            MaxActions: 1,
            ActionCeiling: isRead ? ActionCeiling.ReadOnly : ActionCeiling.FullActionWithPreflight,
            Provenance: Provenance.Uia,
            TrustLevel: TrustLevel.ProfileVerified,
            ApprovalRef: binding,
            ApprovedValueDigest: isType ? approvedValueDigest : "",
            ApprovedInputBindingHash: isType ? approvedInputBindingHash : "",
            ApprovedInputBindingVersion: isType ? approvedInputBindingVersion : "",
            ApprovedInputDigestAlgorithm: isType ? approvedInputDigestAlgorithm : "");
    }

    private IReadOnlyList<ElementIdentity> BuildSafeExecutorCandidates(string prefix, WebTargetResult resolution)
    {
        var planCandidates = ReadSafeClickPlanCandidates(prefix)
            .Select(WebCandidateMapper.ToElementIdentity)
            .ToList();

        if (planCandidates.Count > 0)
            return planCandidates;

        var selectedIdentity = WebTargetResultIdentityMapper.ToSelectedIdentity(resolution);
        return selectedIdentity == null ? Array.Empty<ElementIdentity>() : [selectedIdentity];
    }

    private IReadOnlyList<ElementIdentity> BuildDesktopSafeExecutorCandidates(string prefix, DesktopTargetObservationResult resolution)
    {
        var planCandidates = ReadSafeClickPlanCandidates(prefix)
            .Select(WebCandidateMapper.ToElementIdentity)
            .ToList();

        if (planCandidates.Count > 0)
            return planCandidates;

        var selectedIdentity = DesktopTargetObservationResultIdentityMapper.ToSelectedIdentity(resolution);
        return selectedIdentity == null ? Array.Empty<ElementIdentity>() : [selectedIdentity];
    }

    private static IntPtr? ParseHandle(string? rawHandle)
    {
        return long.TryParse(rawHandle, out var handle)
            ? new IntPtr(handle)
            : null;
    }

    private void SetSafeReadVars(
        string prefix,
        bool success,
        string value,
        string patternUsed,
        FailureKind? failureKind,
        string reason,
        PatternReadResult? readResult = null,
        StepVerificationResult? verification = null)
    {
        _ctx.Variables[prefix + ".success"] = success ? "true" : "false";
        _ctx.Variables[prefix + ".value"] = value ?? "";
        _ctx.Variables[prefix + ".patternUsed"] = patternUsed ?? "";
        _ctx.Variables[prefix + ".failureKind"] = failureKind?.ToString() ?? "";
        _ctx.Variables[prefix + ".reason"] = reason ?? "";
        _ctx.Variables[prefix + ".identity.verdict"] =
            verification?.MatchVerdict ??
            readResult?.InvokeTimeIdentityVerdict ??
            "";
        _ctx.Variables[prefix + ".identity.expectedDigest"] =
            readResult?.ExpectedIdentityDigest ?? "";
        _ctx.Variables[prefix + ".identity.observedDigest"] =
            readResult?.ObservedIdentityDigest ?? "";
    }

    private void SetSafeReadEvidenceVars(
        string prefix,
        StepState finalState,
        FailureKind? failureKind,
        string blockReason,
        IReadOnlyList<string> reasons)
    {
        var ledger = BuildPreFsmLedger(finalState, failureKind, blockReason ?? "", reasons);
        _ctx.Variables[prefix + ".evidence.transitionCount"] = ledger.Count.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables[prefix + ".evidence.ledgerJson"] = JsonSerializer.Serialize(ledger);
    }

    private void SetSafeReadEvidenceVars(
        string prefix,
        EvidenceLedger ledger,
        PatternReadResult readResult,
        StepVerificationResult verification)
    {
        _ctx.Variables[prefix + ".evidence.transitionCount"] = ledger.Entries.Count.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables[prefix + ".evidence.ledgerJson"] = JsonSerializer.Serialize(ledger.Entries);
        _ctx.Variables[prefix + ".identity.verdict"] = verification.MatchVerdict;
        _ctx.Variables[prefix + ".identity.expectedDigest"] = readResult.ExpectedIdentityDigest;
        _ctx.Variables[prefix + ".identity.observedDigest"] = readResult.ObservedIdentityDigest;
    }

    private RecipeStepRunResult BlockSafeType(
        RecipeStepDefinition step,
        Stopwatch sw,
        string prefix,
        FailureKind? failureKind,
        string blockReason,
        string reason,
        IReadOnlyList<string>? reasons = null)
    {
        _ctx.Variables[prefix + ".success"] = "false";
        _ctx.Variables[prefix + ".failureKind"] = failureKind?.ToString() ?? "";
        _ctx.Variables[prefix + ".reason"] = reason ?? "";
        _ctx.Variables[prefix + ".mutationObserved"] = "false";
        _ctx.Variables[prefix + ".ownership.checked"] = "false";
        _ctx.Variables[prefix + ".ownership.allowed"] = "false";
        _ctx.Variables[prefix + ".surface.allowed"] = "false";
        SetSafeTypeEvidenceVars(prefix, BuildPreFsmLedger(StepState.Blocked, failureKind, blockReason, reasons ?? [reason ?? "safe.type blocked"]));
        sw.Stop();
        return new RecipeStepRunResult(
            step.Id,
            step.Kind,
            false,
            $"safe.type: {reason}",
            sw.ElapsedMilliseconds);
    }

    private void SetSafeTypeVars(
        string prefix,
        string approvedText,
        TypeExecutionResult typeResult,
        StepVerificationResult verification)
    {
        _ctx.Variables[prefix + ".success"] = typeResult.Success && verification.Success ? "true" : "false";
        _ctx.Variables[prefix + ".valueBefore"] = typeResult.ValueBefore ?? "";
        _ctx.Variables[prefix + ".valueAfter"] = typeResult.ValueAfter ?? "";
        _ctx.Variables[prefix + ".approvedTextDigest"] = typeResult.ApprovedTextDigest;
        _ctx.Variables[prefix + ".patternUsed"] = typeResult.PatternUsed;
        _ctx.Variables[prefix + ".failureKind"] = (typeResult.FailureKind ?? verification.FailureKind)?.ToString() ?? "";
        _ctx.Variables[prefix + ".reason"] = typeResult.Reasons.Concat(verification.Reasons).LastOrDefault() ?? "";
        _ctx.Variables[prefix + ".identity.verdict"] = verification.MatchVerdict;
        _ctx.Variables[prefix + ".identity.expectedDigest"] = typeResult.ExpectedIdentityDigest;
        _ctx.Variables[prefix + ".identity.observedDigest"] = typeResult.ObservedIdentityDigest;
        _ctx.Variables[prefix + ".surface.allowed"] = typeResult.SurfaceAllowed ? "true" : "false";
        _ctx.Variables[prefix + ".surface.reason"] = typeResult.SurfaceReason;
        _ctx.Variables[prefix + ".ownership.checked"] = typeResult.OwnershipChecked ? "true" : "false";
        _ctx.Variables[prefix + ".ownership.allowed"] = typeResult.OwnershipAllowed ? "true" : "false";
        _ctx.Variables[prefix + ".mutationObserved"] = typeResult.MutationObserved ? "true" : "false";
        _ctx.Variables[prefix + ".approvedText.matchesValueAfter"] =
            string.Equals(typeResult.ValueAfter, approvedText, StringComparison.Ordinal) ? "true" : "false";
    }

    private void SetSafeTypeApprovedInputVars(
        string prefix,
        ApprovedInputBinding? binding,
        ApprovedInputBindingValidationResult validation)
    {
        _ctx.Variables[prefix + ".approvedInput.valueDigest"] = binding?.ApprovedValueDigest ?? "";
        _ctx.Variables[prefix + ".approvedInput.bindingHash"] = binding?.ApprovedInputBindingHash ?? "";
        _ctx.Variables[prefix + ".approvedInput.bindingVersion"] = binding?.BindingVersion ?? "";
        _ctx.Variables[prefix + ".approvedInput.digestAlgorithm"] = binding?.ApprovedValueDigestAlgorithm ?? "";
        _ctx.Variables[prefix + ".approvedInput.source"] = "manifest";
        _ctx.Variables[prefix + ".approvedInput.validated"] = validation.Success ? "true" : "false";
        _ctx.Variables[prefix + ".approvedInput.failureKind"] = validation.FailureKind?.ToString() ?? "";
        _ctx.Variables[prefix + ".approvedInput.reason"] = validation.Reason;
        _ctx.Variables[prefix + ".approvedInput.expectedHash"] = validation.ExpectedHash;
        _ctx.Variables[prefix + ".approvedInput.observedHash"] = validation.ActualHash;
    }

    private void SetSafeTypeEvidenceVars(string prefix, IReadOnlyList<StepTransitionEvidence> entries)
    {
        _ctx.Variables[prefix + ".evidence.transitionCount"] = entries.Count.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables[prefix + ".evidence.ledgerJson"] = JsonSerializer.Serialize(entries);
    }

    private void SetSafeTypeEvidenceVars(string prefix, EvidenceLedger ledger) =>
        SetSafeTypeEvidenceVars(prefix, ledger.Entries);

    private SafeTypeLiveTarget ResolveSafeTypeLiveTarget(
        RecipeStepDefinition step,
        string targetText,
        string prefix,
        ApprovalManifest manifest)
    {
        var expected = manifest.ApprovedSelector?.ExpectedIdentity;
        if (string.Equals(manifest.IdentitySource, "uia", StringComparison.OrdinalIgnoreCase))
        {
            var processName = ResolveArg(step, "processName") ?? ResolveArg(step, "proc") ?? expected?.ProcessName;
            var windowTitle = ResolveArg(step, "window") ?? ResolveArg(step, "windowTitle") ?? expected?.WindowTitle;
            var resolution = ResolveTargetObserveDesktop(targetText, processName, windowTitle);
            SetDesktopResolutionVars(prefix, resolution);
            if (!resolution.Found)
            {
                return SafeTypeLiveTarget.Fail(
                    MapDesktopTargetObserveFailureKind(resolution),
                    resolution.CandidateCount > 1 ? "SafeTypeTargetAmbiguous" : "SafeTypeTargetNotFound",
                    resolution.Reason);
            }

            var identity = DesktopTargetObservationResultIdentityMapper.ToSelectedIdentity(resolution);
            if (identity == null)
            {
                return SafeTypeLiveTarget.Fail(
                    FailureKind.NotFound,
                    "SafeTypeObservedIdentityMissing",
                    "safe.type could not build observed desktop identity");
            }

            return SafeTypeLiveTarget.Ok(
                identity,
                ParseHandle(resolution.RootHwnd),
                resolution.SelectedName ?? targetText,
                resolution.SelectedProcessName ?? processName,
                resolution.SelectedWindowTitle ?? windowTitle);
        }

        var sessionHwnd = FindOwnedBrowserSessionHwnd();
        if (sessionHwnd == IntPtr.Zero)
        {
            return SafeTypeLiveTarget.Fail(
                FailureKind.SourceUnavailable,
                "OwnedBrowserSessionRequired",
                "safe.type requires owned browser session for web-uia target");
        }

        var proc = ResolveArg(step, "proc") ?? expected?.ProcessName ?? "msedge";
        var webResolution = ResolveSafeClickTarget(sessionHwnd, targetText, proc);
        SetResolutionVars(prefix, webResolution);
        if (!webResolution.Found)
        {
            var failureKind = webResolution.CandidateCount > 1 || webResolution.Reason.Contains("ambiguous", StringComparison.OrdinalIgnoreCase)
                ? FailureKind.Ambiguous
                : FailureKind.NotFound;
            return SafeTypeLiveTarget.Fail(
                failureKind,
                failureKind == FailureKind.Ambiguous ? "SafeTypeTargetAmbiguous" : "SafeTypeTargetNotFound",
                webResolution.Reason);
        }

        var webIdentity = WebTargetResultIdentityMapper.ToSelectedIdentity(webResolution);
        if (webIdentity == null)
        {
            return SafeTypeLiveTarget.Fail(
                FailureKind.NotFound,
                "SafeTypeObservedIdentityMissing",
                "safe.type could not build observed web identity");
        }

        return SafeTypeLiveTarget.Ok(
            webIdentity,
            ParseHandle(webResolution.SelectedHwnd),
            webResolution.SelectedName ?? targetText,
            webResolution.SelectedProcessName ?? proc,
            webResolution.SelectedWindowTitle);
    }

    private static EvidenceLedger BuildSafeTypeLedger(
        RecipeSafetyContract contract,
        ApprovalBindingResult binding,
        TypeExecutionResult typeResult,
        StepVerificationResult verification)
    {
        var ledger = new EvidenceLedger();
        ledger.Append(
            occurredAtUtc: DateTimeOffset.UtcNow,
            fromState: StepState.Created,
            toState: StepState.Validated,
            @event: StepTransition.ContractValid,
            failureKind: null,
            blockReason: null,
            contractId: contract.ContractId,
            approvalDecisionId: contract.ApprovalRef?.ApprovalDecisionId,
            approvedIdentityDigest: contract.ApprovalRef?.ApprovedIdentityDigest,
            observedIdentityDigest: null,
            matchVerdict: null,
            ownershipSnapshotHash: null,
            reasons: ["contract valid"]);
        ledger.Append(
            occurredAtUtc: DateTimeOffset.UtcNow,
            fromState: StepState.Validated,
            toState: StepState.Bound,
            @event: StepTransition.BindingSame,
            failureKind: null,
            blockReason: null,
            contractId: contract.ContractId,
            approvalDecisionId: contract.ApprovalRef?.ApprovalDecisionId,
            approvedIdentityDigest: contract.ApprovalRef?.ApprovedIdentityDigest,
            observedIdentityDigest: binding.ObservedIdentityDigest,
            matchVerdict: binding.MatchVerdict,
            ownershipSnapshotHash: null,
            reasons: binding.Reasons);
        ledger.Append(
            occurredAtUtc: DateTimeOffset.UtcNow,
            fromState: StepState.Bound,
            toState: StepState.Executing,
            @event: StepTransition.DispatchStarted,
            failureKind: null,
            blockReason: null,
            contractId: contract.ContractId,
            approvalDecisionId: contract.ApprovalRef?.ApprovalDecisionId,
            approvedIdentityDigest: contract.ApprovalRef?.ApprovedIdentityDigest,
            observedIdentityDigest: binding.ObservedIdentityDigest,
            matchVerdict: binding.MatchVerdict,
            ownershipSnapshotHash: null,
            reasons: ["type dispatch started"]);

        if (!typeResult.Success)
        {
            ledger.Append(
                occurredAtUtc: DateTimeOffset.UtcNow,
                fromState: StepState.Executing,
                toState: StepState.Failed,
                @event: StepTransition.ExecutorError,
                failureKind: typeResult.FailureKind ?? FailureKind.Unverified,
                blockReason: "ExecutorError",
                contractId: contract.ContractId,
                approvalDecisionId: contract.ApprovalRef?.ApprovalDecisionId,
                approvedIdentityDigest: contract.ApprovalRef?.ApprovedIdentityDigest,
                observedIdentityDigest: typeResult.ObservedIdentityDigest,
                matchVerdict: typeResult.InvokeTimeIdentityVerdict,
                ownershipSnapshotHash: null,
                reasons: typeResult.Reasons);
            return ledger;
        }

        ledger.Append(
            occurredAtUtc: DateTimeOffset.UtcNow,
            fromState: StepState.Executing,
            toState: StepState.Verifying,
            @event: StepTransition.ExecutorReturned,
            failureKind: null,
            blockReason: null,
            contractId: contract.ContractId,
            approvalDecisionId: contract.ApprovalRef?.ApprovalDecisionId,
            approvedIdentityDigest: contract.ApprovalRef?.ApprovedIdentityDigest,
            observedIdentityDigest: typeResult.ObservedIdentityDigest,
            matchVerdict: typeResult.InvokeTimeIdentityVerdict,
            ownershipSnapshotHash: null,
            reasons: typeResult.Reasons);

        ledger.Append(
            occurredAtUtc: DateTimeOffset.UtcNow,
            fromState: StepState.Verifying,
            toState: verification.Success ? StepState.Succeeded : StepState.Failed,
            @event: verification.Success ? StepTransition.Verified : StepTransition.NotVerified,
            failureKind: verification.Success ? null : verification.FailureKind ?? FailureKind.Unverified,
            blockReason: verification.Success ? null : "NotVerified",
            contractId: contract.ContractId,
            approvalDecisionId: contract.ApprovalRef?.ApprovalDecisionId,
            approvedIdentityDigest: contract.ApprovalRef?.ApprovedIdentityDigest,
            observedIdentityDigest: typeResult.ObservedIdentityDigest,
            matchVerdict: verification.MatchVerdict,
            ownershipSnapshotHash: null,
            reasons: verification.Reasons);

        return ledger;
    }

    private static EvidenceLedger BuildSafeReadLedger(
        RecipeSafetyContract contract,
        ApprovalBindingResult binding,
        PatternReadResult readResult,
        StepVerificationResult verification)
    {
        var ledger = new EvidenceLedger();
        ledger.Append(
            occurredAtUtc: DateTimeOffset.UtcNow,
            fromState: StepState.Created,
            toState: StepState.Validated,
            @event: StepTransition.ContractValid,
            failureKind: null,
            blockReason: null,
            contractId: contract.ContractId,
            approvalDecisionId: contract.ApprovalRef?.ApprovalDecisionId,
            approvedIdentityDigest: contract.ApprovalRef?.ApprovedIdentityDigest,
            observedIdentityDigest: null,
            matchVerdict: null,
            ownershipSnapshotHash: null,
            reasons: ["contract valid"]);
        ledger.Append(
            occurredAtUtc: DateTimeOffset.UtcNow,
            fromState: StepState.Validated,
            toState: StepState.Bound,
            @event: StepTransition.BindingSame,
            failureKind: null,
            blockReason: null,
            contractId: contract.ContractId,
            approvalDecisionId: contract.ApprovalRef?.ApprovalDecisionId,
            approvedIdentityDigest: contract.ApprovalRef?.ApprovedIdentityDigest,
            observedIdentityDigest: binding.ObservedIdentityDigest,
            matchVerdict: binding.MatchVerdict,
            ownershipSnapshotHash: null,
            reasons: binding.Reasons);
        ledger.Append(
            occurredAtUtc: DateTimeOffset.UtcNow,
            fromState: StepState.Bound,
            toState: StepState.Executing,
            @event: StepTransition.DispatchStarted,
            failureKind: null,
            blockReason: null,
            contractId: contract.ContractId,
            approvalDecisionId: contract.ApprovalRef?.ApprovalDecisionId,
            approvedIdentityDigest: contract.ApprovalRef?.ApprovedIdentityDigest,
            observedIdentityDigest: binding.ObservedIdentityDigest,
            matchVerdict: binding.MatchVerdict,
            ownershipSnapshotHash: null,
            reasons: ["read dispatch started"]);

        if (!readResult.Success)
        {
            ledger.Append(
                occurredAtUtc: DateTimeOffset.UtcNow,
                fromState: StepState.Executing,
                toState: StepState.Failed,
                @event: StepTransition.ExecutorError,
                failureKind: readResult.FailureKind ?? FailureKind.Unverified,
                blockReason: "ExecutorError",
                contractId: contract.ContractId,
                approvalDecisionId: contract.ApprovalRef?.ApprovalDecisionId,
                approvedIdentityDigest: contract.ApprovalRef?.ApprovedIdentityDigest,
                observedIdentityDigest: readResult.ObservedIdentityDigest,
                matchVerdict: readResult.InvokeTimeIdentityVerdict,
                ownershipSnapshotHash: null,
                reasons: readResult.Reasons);
            return ledger;
        }

        ledger.Append(
            occurredAtUtc: DateTimeOffset.UtcNow,
            fromState: StepState.Executing,
            toState: StepState.Verifying,
            @event: StepTransition.ExecutorReturned,
            failureKind: null,
            blockReason: null,
            contractId: contract.ContractId,
            approvalDecisionId: contract.ApprovalRef?.ApprovalDecisionId,
            approvedIdentityDigest: contract.ApprovalRef?.ApprovedIdentityDigest,
            observedIdentityDigest: readResult.ObservedIdentityDigest,
            matchVerdict: readResult.InvokeTimeIdentityVerdict,
            ownershipSnapshotHash: null,
            reasons: readResult.Reasons);

        ledger.Append(
            occurredAtUtc: DateTimeOffset.UtcNow,
            fromState: StepState.Verifying,
            toState: verification.Success ? StepState.Succeeded : StepState.Failed,
            @event: verification.Success ? StepTransition.Verified : StepTransition.NotVerified,
            failureKind: verification.Success ? null : verification.FailureKind ?? FailureKind.Unverified,
            blockReason: verification.Success ? null : "NotVerified",
            contractId: contract.ContractId,
            approvalDecisionId: contract.ApprovalRef?.ApprovalDecisionId,
            approvedIdentityDigest: contract.ApprovalRef?.ApprovedIdentityDigest,
            observedIdentityDigest: readResult.ObservedIdentityDigest,
            matchVerdict: verification.MatchVerdict,
            ownershipSnapshotHash: null,
            reasons: verification.Reasons);

        return ledger;
    }

    private void SetSafeClickFsmVars(
        string prefix,
        StepState finalState,
        FailureKind? failureKind,
        string blockReason,
        IReadOnlyList<string> reasons)
    {
        _ctx.Variables[prefix + ".fsm.finalState"] = finalState.ToString();
        _ctx.Variables[prefix + ".fsm.failureKind"] = failureKind?.ToString() ?? "";
        _ctx.Variables[prefix + ".fsm.blockReason"] = blockReason ?? "";
        _ctx.Variables[prefix + ".fsm.success"] = (finalState == StepState.Succeeded).ToString().ToLowerInvariant();
        _ctx.Variables[prefix + ".fsm.reasons"] = reasons.Count == 0 ? "" : string.Join(" | ", reasons);
        var preFsmLedger = BuildPreFsmLedger(finalState, failureKind, blockReason ?? "", reasons);
        _ctx.Variables[prefix + ".fsm.transitionCount"] = preFsmLedger.Count.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables[prefix + ".fsm.ledgerJson"] = JsonSerializer.Serialize(preFsmLedger);
    }

    private void SetSafeClickFsmVars(string prefix, SafeExecutionResult result)
    {
        _ctx.Variables[prefix + ".fsm.finalState"] = result.FinalState.ToString();
        _ctx.Variables[prefix + ".fsm.failureKind"] = result.FailureKind?.ToString() ?? "";
        _ctx.Variables[prefix + ".fsm.blockReason"] = result.BlockReason ?? "";
        _ctx.Variables[prefix + ".fsm.success"] = result.Success ? "true" : "false";
        _ctx.Variables[prefix + ".fsm.transitionCount"] = result.Ledger.Entries.Count.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables[prefix + ".fsm.reasons"] = result.Reasons.Count == 0 ? "" : string.Join(" | ", result.Reasons);
        _ctx.Variables[prefix + ".fsm.ledgerJson"] = JsonSerializer.Serialize(result.Ledger.Entries);
    }

    private static IReadOnlyList<StepTransitionEvidence> BuildPreFsmLedger(
        StepState finalState,
        FailureKind? failureKind,
        string blockReason,
        IReadOnlyList<string> reasons)
    {
        if (finalState is not (StepState.Blocked or StepState.Failed or StepState.Aborted))
            return [];

        var ledger = new EvidenceLedger();
        ledger.Append(
            occurredAtUtc: DateTimeOffset.UtcNow,
            fromState: StepState.Created,
            toState: finalState,
            @event: StepTransition.ContractInvalid,
            failureKind: failureKind ?? FailureKind.PolicyDenied,
            blockReason: blockReason,
            contractId: "pre-fsm",
            approvalDecisionId: null,
            approvedIdentityDigest: null,
            observedIdentityDigest: null,
            matchVerdict: null,
            ownershipSnapshotHash: null,
            reasons: reasons);
        return ledger.Entries;
    }

    private void SetSafeClickFsmReadyVars(string prefix, SafeClickShadowReadiness readiness)
    {
        _ctx.Variables[prefix + ".fsmReady.success"] = readiness.Success ? "true" : "false";
        _ctx.Variables[prefix + ".fsmReady.reason"] = readiness.Reason;
        _ctx.Variables[prefix + ".fsmReady.projectedState"] = readiness.ProjectedState.ToString();
        _ctx.Variables[prefix + ".fsmReady.identityStrength"] = readiness.IdentityStrength.ToString();
        _ctx.Variables[prefix + ".fsmReady.hasTargetObserve"] = readiness.HasTargetObserve ? "true" : "false";
        _ctx.Variables[prefix + ".fsmReady.hasApprovalV3"] = readiness.HasApprovalV3 ? "true" : "false";
        _ctx.Variables[prefix + ".fsmReady.hasRuntimeId"] = readiness.HasRuntimeId ? "true" : "false";
        _ctx.Variables[prefix + ".fsmReady.runtimeIdentityMatch"] = readiness.RuntimeIdentityMatch.ToString();
        _ctx.Variables[prefix + ".fsmReady.wouldRequireLegacy"] = readiness.WouldRequireLegacy ? "true" : "false";
        _ctx.Variables[prefix + ".fsmReady.eligible"] = readiness.EligibleForFsm ? "true" : "false";
        _ctx.Variables[prefix + ".fsmReady.desktopEligible"] = readiness.DesktopEligibleForFsm ? "true" : "false";
        _ctx.Variables[prefix + ".fsmReady.desktopRootAvailable"] = readiness.DesktopRootAvailable ? "true" : "false";
        _ctx.Variables[prefix + ".fsmReady.summary"] = readiness.Summary;
    }

    private void SetSafeClickRuntimeStabilityVars(string prefix, SafeClickRuntimeStability stability)
    {
        _ctx.Variables[prefix + ".runtimeStability.verdict"] = stability.StabilityVerdict.ToString();
        _ctx.Variables[prefix + ".runtimeStability.observeAgeMs"] = stability.ObserveAgeMs?.ToString(CultureInfo.InvariantCulture) ?? "";
        _ctx.Variables[prefix + ".runtimeStability.reobserveAttempted"] = stability.ReobserveAttempted ? "true" : "false";
        _ctx.Variables[prefix + ".runtimeStability.reobserveSucceeded"] = stability.ReobserveSucceeded ? "true" : "false";
        _ctx.Variables[prefix + ".runtimeStability.reobserveMatch"] = stability.ReobserveMatch.ToString();
        _ctx.Variables[prefix + ".runtimeStability.blockReason"] = stability.BlockReason ?? "";
    }

    private void SetSafeClickDesktopRuntimeStabilityVars(string prefix, SafeClickRuntimeStability stability)
    {
        _ctx.Variables[prefix + ".desktopFsm.runtimeStabilityChecked"] = stability.ReobserveAttempted ? "true" : "false";
        _ctx.Variables[prefix + ".desktopFsm.runtimeStabilityVerdict"] = stability.StabilityVerdict.ToString();
        _ctx.Variables[prefix + ".desktopFsm.reobserveAttempted"] = stability.ReobserveAttempted ? "true" : "false";
        _ctx.Variables[prefix + ".desktopFsm.reobserveSucceeded"] = stability.ReobserveSucceeded ? "true" : "false";
        _ctx.Variables[prefix + ".desktopFsm.blockedByStaleIdentity"] =
            stability.AllowsDefaultDispatch ? "false" : "true";
    }

    private void TrackRuntimeStability(SafeClickRuntimeStability stability)
    {
        if (stability.StabilityVerdict is SafeClickRuntimeStabilityVerdict.ReobservedStable or SafeClickRuntimeStabilityVerdict.Stable)
            _safeClickRuntimeStableCount++;

        if (stability.StabilityVerdict is SafeClickRuntimeStabilityVerdict.ReobservedChanged or SafeClickRuntimeStabilityVerdict.Changed)
            _safeClickRuntimeChangedCount++;

        if (stability.StabilityVerdict == SafeClickRuntimeStabilityVerdict.Missing)
            _safeClickRuntimeMissingCount++;

        if (stability.ReobserveSucceeded)
            _safeClickReobserveSucceededCount++;

        if (stability.StabilityVerdict == SafeClickRuntimeStabilityVerdict.ReobservedChanged)
            _safeClickReobserveChangedCount++;
    }

    private void SetSafeClickMigrationSummaryVars()
    {
        if (_safeClickShadowReadiness.Count == 0)
            return;

        var report = SafeClickMigrationReportBuilder.Build(_safeClickShadowReadiness.Values);
        var summary = report.Summary;
        var blockingReasons = summary.BlockingReasons.Count == 0
            ? ""
            : string.Join("|",
                summary.BlockingReasons
                    .OrderByDescending(entry => entry.Value)
                    .ThenBy(entry => entry.Key.ToString(), StringComparer.Ordinal)
                    .Select(entry => $"{entry.Key}={entry.Value}"));

        _ctx.Variables["safeClick.migration.total"] = summary.TotalSafeClicks.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.eligibleForFsm"] = summary.EligibleForFsm.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.notEligibleForFsm"] = summary.NotEligibleForFsm.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.blockingReasons"] = blockingReasons;
        _ctx.Variables["safeClick.migration.readinessPercent"] = summary.ReadinessPercent.ToString("0.##", CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.legacyFallbackCount"] = summary.WouldUseUnsafeFallback.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.summary"] = report.Markdown;
        _ctx.Variables["safeClick.migration.reportJson"] = report.Json;
        _ctx.Variables["safeClick.migration.webUiaEligible"] = summary.WebUiaEligible.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.desktopUiaObservable"] = summary.DesktopUiaObservable.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.desktopUiaStrong"] = summary.DesktopUiaStrong.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.desktopUiaWeak"] = summary.DesktopUiaWeak.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.desktopMissingIdentity"] = summary.DesktopMissingIdentity.ToString(CultureInfo.InvariantCulture);

        // HITO-147 — additive default-routing metrics (local-first).
        _ctx.Variables["safeClick.migration.defaultFsmEnabled"] = _safeClickDefaultFsmEnabledCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.defaultFsmRouted"] = _safeClickDefaultFsmRoutedCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.defaultFsmEligibleButNotEnabled"] = _safeClickDefaultFsmEligibleButNotEnabledCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.defaultFsmBlocked"] = _safeClickDefaultFsmBlockedCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.explicitLegacyOptOut"] = _safeClickExplicitLegacyOptOutCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.desktopExcludedFromDefault"] = _safeClickDesktopExcludedFromDefaultCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.unknownDispatchPathBlocked"] = _safeClickUnknownDispatchPathBlockedCount.ToString(CultureInfo.InvariantCulture);

        // HITO-148 — runtime identity stability metrics for web eligible default dispatch.
        _ctx.Variables["safeClick.migration.runtimeStabilityChecked"] = _safeClickRuntimeStabilityCheckedCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.runtimeStable"] = _safeClickRuntimeStableCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.runtimeChanged"] = _safeClickRuntimeChangedCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.runtimeMissing"] = _safeClickRuntimeMissingCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.reobserveAttempted"] = _safeClickReobserveAttemptedCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.reobserveSucceeded"] = _safeClickReobserveSucceededCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.reobserveChanged"] = _safeClickReobserveChangedCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.defaultBlockedByStaleIdentity"] = _safeClickDefaultBlockedByStaleIdentityCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.defaultBlockedByMissingIdentity"] = _safeClickDefaultBlockedByMissingIdentityCount.ToString(CultureInfo.InvariantCulture);

        // HITO-150 — desktop readiness metrics remain shadow-only; no desktop default routing.
        _ctx.Variables["safeClick.migration.desktopEligibleForFsm"] = summary.DesktopEligibleForFsm.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.desktopNotEligibleForFsm"] = summary.DesktopNotEligibleForFsm.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.desktopRuntimeStable"] = summary.DesktopRuntimeStable.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.desktopRuntimeChanged"] = summary.DesktopRuntimeChanged.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.desktopInvokePatternAvailable"] = summary.DesktopInvokePatternAvailable.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.desktopRoleAllowed"] = summary.DesktopRoleAllowed.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.desktopRootAvailable"] = summary.DesktopRootAvailable.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.desktopOptInRouted"] = _safeClickDesktopOptInRoutedCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.desktopOptInBlocked"] = _safeClickDesktopOptInBlockedCount.ToString(CultureInfo.InvariantCulture);

        // HITO-151 — desktop eligible-only default routing metrics.
        _ctx.Variables["safeClick.migration.desktopDefaultFsmEnabled"] = _safeClickDesktopDefaultFsmEnabledCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.desktopDefaultFsmRouted"] = _safeClickDesktopDefaultFsmRoutedCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.desktopDefaultEligibleButNotEnabled"] = _safeClickDesktopDefaultEligibleButNotEnabledCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.desktopDefaultBlocked"] = _safeClickDesktopDefaultBlockedCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.desktopDefaultBlockedByStaleIdentity"] = _safeClickDesktopDefaultBlockedByStaleIdentityCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.allEligibleModeEnabled"] = _safeClickAllEligibleModeEnabledCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.defaultFsmScopeWeb"] = _safeClickDefaultFsmScopeWebCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.defaultFsmScopeDesktop"] = _safeClickDefaultFsmScopeDesktopCount.ToString(CultureInfo.InvariantCulture);

        // HITO-152 — explicit legacy opt-out deprecation metrics.
        _ctx.Variables["safeClick.migration.legacyExplicitOptOutTotal"] = _safeClickExplicitLegacyOptOutCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.legacyOptOutCompliant"] = _safeClickLegacyExplicitOptOutCompliantCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.legacyOptOutMissingOwner"] = _safeClickLegacyOptOutMissingOwnerCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.legacyOptOutMissingReason"] = _safeClickLegacyOptOutMissingReasonCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.legacyOptOutMissingReviewBy"] = _safeClickLegacyOptOutMissingReviewByCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.legacyOptOutNonCompliant"] = _safeClickLegacyOptOutNonCompliantCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.legacyDeprecationWarnings"] = _safeClickLegacyDeprecationWarningCount.ToString(CultureInfo.InvariantCulture);

        // HITO-154 — safe.click legacy execution is retired; these are per-run local counters.
        _ctx.Variables["safeClick.migration.legacyRetired"] = "1";
        _ctx.Variables["safeClick.migration.legacyExecutionBlocked"] = _safeClickLegacyExecutionBlockedCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.legacyDispatchRejected"] = _safeClickLegacyDispatchRejectedCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.ineligibleBlockedAfterRetirement"] = _safeClickIneligibleBlockedAfterRetirementCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.safeExecutorRequired"] = _safeClickSafeExecutorRequiredCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.elClickReachableFromSafeClick"] = "0";
        _ctx.Variables["safeClick.migration.uiaActionExecutorReachableFromSafeClick"] = "0";

        // HITO-153 — per-run retirement readiness gate. This is not historical telemetry.
        var retirement = SafeClickLegacyRetirementReadinessEvaluator.Evaluate(
            totalSafeClicks: summary.TotalSafeClicks,
            defaultFsmRouted: _safeClickDefaultFsmRoutedCount + _safeClickDesktopDefaultFsmRoutedCount,
            explicitLegacyOptOut: _safeClickExplicitLegacyOptOutCount,
            legacyPathUsed: _safeClickDefaultLegacyUseCount,
            elClickUsed: _safeClickDefaultElClickUseCount,
            uiaActionExecutorUsed: _safeClickDefaultUiaActionExecutorUseCount,
            unsafeFallbackUsed: _safeClickDefaultUnsafeFallbackUseCount,
            nonCompliantLegacyOptOut: _safeClickLegacyOptOutNonCompliantCount,
            desktopExcluded: _safeClickDesktopExcludedFromDefaultCount,
            webExcluded: Math.Max(0, summary.WebUiaEligible - _safeClickDefaultFsmScopeWebCount),
            allEligibleModeObserved: _safeClickAllEligibleModeEnabledCount,
            unknownDispatchPathBlocked: _safeClickUnknownDispatchPathBlockedCount);
        SetSafeClickLegacyRetirementVars(retirement);
    }

    private void SetSafeClickLegacyRetirementVars(SafeClickLegacyRetirementReadiness retirement)
    {
        var blockingReasons = retirement.BlockingReasons.Count == 0 ? "" : string.Join("|", retirement.BlockingReasons);
        _ctx.Variables["safeClick.retirement.ready"] = retirement.IsReadyForRetirement ? "true" : "false";
        _ctx.Variables["safeClick.retirement.blockingReasons"] = blockingReasons;
        _ctx.Variables["safeClick.retirement.legacyPathUsed"] = retirement.LegacyPathUsed.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.retirement.elClickUsed"] = retirement.ElClickUsed.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.retirement.uiaActionExecutorUsed"] = retirement.UiaActionExecutorUsed.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.retirement.unsafeFallbackUsed"] = retirement.UnsafeFallbackUsed.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.retirement.nonCompliantLegacyOptOut"] = retirement.NonCompliantLegacyOptOut.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.retirement.summary"] = retirement.Summary;
        _ctx.Variables["safeClick.retirement.reportJson"] = retirement.ReportJson;

        _ctx.Variables["safeClick.migration.retirementReady"] = retirement.IsReadyForRetirement ? "1" : "0";
        _ctx.Variables["safeClick.migration.retirementBlocked"] = retirement.IsReadyForRetirement ? "0" : "1";
        _ctx.Variables["safeClick.migration.retirementBlockingReasons"] = blockingReasons;
        _ctx.Variables["safeClick.migration.defaultLegacyUse"] = retirement.LegacyPathUsed.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.defaultElClickUse"] = retirement.ElClickUsed.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.defaultUiaActionExecutorUse"] = retirement.UiaActionExecutorUsed.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables["safeClick.migration.nonCompliantLegacyOptOut"] = retirement.NonCompliantLegacyOptOut.ToString(CultureInfo.InvariantCulture);
    }

    private static WebTargetResult ResolveTargetObserve(IntPtr sessionHwnd, string targetText, string processName, int maxDescendants = 500)
    {
        return s_targetObserveResolverOverride?.Invoke(sessionHwnd, targetText, processName, maxDescendants)
            ?? WebTargetResolver.Resolve(sessionHwnd, targetText, processName, maxDescendants);
    }

    private static DesktopTargetObservationResult ResolveTargetObserveDesktop(string targetText, string? processName, string? windowTitle)
    {
        return s_targetObserveDesktopOverride?.Invoke(targetText, processName, windowTitle)
            ?? ResolveTargetObserveDesktopCore(targetText, processName, windowTitle);
    }

    private static DesktopTargetObservationResult ResolveTargetObserveDesktopCore(string targetText, string? processName, string? windowTitle)
    {
        if (string.IsNullOrWhiteSpace(targetText))
        {
            return new DesktopTargetObservationResult
            {
                Found = false,
                CandidateCount = 0,
                Reason = "desktop target input is empty"
            };
        }

        if (string.IsNullOrWhiteSpace(processName) && string.IsNullOrWhiteSpace(windowTitle))
        {
            return new DesktopTargetObservationResult
            {
                Found = false,
                CandidateCount = 0,
                Reason = "desktop observe requires processName or window filter"
            };
        }

        var windows = new WindowFinder().FindAllWindows(processName, windowTitle);
        if (windows.Count == 0)
        {
            return new DesktopTargetObservationResult
            {
                Found = false,
                CandidateCount = 0,
                Reason = "no matching desktop windows"
            };
        }

        var selector = new SelectorDefinition
        {
            SchemaVersion = 1,
            Provenance = Provenance.Inferred,
            Name = targetText
        };

        var candidates = new List<DesktopObservedCandidate>();
        using var automation = new UIA3Automation();

        foreach (var hwnd in windows)
        {
            AutomationElement? root = null;
            try { root = automation.FromHandle(hwnd); } catch { }
            if (root == null)
                continue;

            var resolvedProcessName = processName ?? "";
            var resolvedWindowTitle = EmptyToNull(UiaTreeWalker.SafeName(root)) ?? windowTitle ?? "";
            var elements = new List<AutomationElement>();
            UiaTreeWalker.Walk(root, elements, UiaTreeWalker.DefaultMaxElements, UiaTreeWalker.DefaultMaxDepth);

            foreach (var element in elements)
            {
                var candidate = BuildDesktopObservedCandidate(element, resolvedProcessName, resolvedWindowTitle, hwnd);
                if (candidate != null)
                    candidates.Add(candidate);
            }
        }

        if (candidates.Count == 0)
        {
            return new DesktopTargetObservationResult
            {
                Found = false,
                CandidateCount = 0,
                Reason = $"no observable desktop candidates in {windows.Count} windows"
            };
        }

        var resolution = SelectorEngine.Resolve(selector, candidates.Select(candidate => candidate.Identity).ToList());
        if (!resolution.Success || resolution.BestMatch == null)
        {
            var candidateCount = resolution.Ambiguous ? resolution.Matches.Count : 0;
            var reason = resolution.Ambiguous
                ? $"ambiguous: {candidateCount} equivalent desktop targets"
                : $"not found in {windows.Count} windows";

            return new DesktopTargetObservationResult
            {
                Found = false,
                CandidateCount = candidateCount,
                Reason = reason
            };
        }

        var selected = candidates.FirstOrDefault(candidate => SameElementIdentity(candidate.Identity, resolution.BestMatch));
        if (selected == null)
        {
            return new DesktopTargetObservationResult
            {
                Found = false,
                CandidateCount = 0,
                Reason = "selected desktop identity could not be reattached"
            };
        }

        return new DesktopTargetObservationResult
        {
            Found = true,
            CandidateCount = 1,
            Reason = "exact desktop observe match",
            SelectedName = selected.Identity.Name,
            SelectedControlType = selected.Identity.EffectiveControlType,
            SelectedBoundingRect = selected.Identity.BoundsHint,
            SelectedRuntimeId = EmptyToNull(selected.Identity.RuntimeId),
            SelectedAutomationId = EmptyToNull(selected.Identity.AutomationId),
            SelectedClassName = EmptyToNull(selected.Identity.ClassName),
            SelectedFrameworkId = EmptyToNull(selected.Identity.FrameworkId),
            SelectedAncestorPath = EmptyToNull(selected.Identity.AncestorPath),
            SelectedProcessName = EmptyToNull(selected.Identity.ProcessName),
            SelectedWindowTitle = EmptyToNull(selected.Identity.WindowTitle),
            SelectedHelpText = null,
            SelectedLegacyName = null,
            RootHwnd = selected.RootHwnd.ToString(CultureInfo.InvariantCulture),
            SelectedHelpTextPresent = selected.HelpTextPresent,
            SelectedLegacyNamePresent = selected.LegacyNamePresent,
            HasInvoke = selected.HasInvoke
        };
    }

    private void ApplyDesktopTargetObserveResult(string prefix, DesktopTargetObservationResult result, FailureKind failureKind)
    {
        SetDesktopResolutionVars(prefix, result);
        SetDesktopObservedIdentityVars(prefix, result);
        _ctx.Variables[prefix + ".failureKind"] = failureKind.ToString();
    }

    private static WebTargetResult ResolveSafeClickTarget(IntPtr sessionHwnd, string targetText, string processName, int maxDescendants = 500)
    {
        return s_targetObserveResolverOverride?.Invoke(sessionHwnd, targetText, processName, maxDescendants)
            ?? WebTargetResolver.Resolve(sessionHwnd, targetText, processName, maxDescendants);
    }

    private IntPtr FindOwnedBrowserSessionHwnd()
    {
        foreach (var key in _ctx.Variables.Keys.Where(k => k.EndsWith(".hwnd", StringComparison.OrdinalIgnoreCase)))
        {
            var prefix = key[..^5];
            if (_ctx.Variables.GetValueOrDefault(prefix + ".owned", "false") == "true" &&
                long.TryParse(_ctx.Variables[key], out var handle))
            {
                return new IntPtr(handle);
            }
        }

        return IntPtr.Zero;
    }

    private void ApplyTargetObserveResult(string prefix, WebTargetResult result, FailureKind failureKind)
    {
        SetResolutionVars(prefix, result);
        SetObservedIdentityVars(prefix, result);
        _ctx.Variables[prefix + ".failureKind"] = failureKind.ToString();
        if (!string.IsNullOrWhiteSpace(result.CandidatesJson))
            _ctx.Variables[prefix + ".resolution.candidatesJson"] = result.CandidatesJson;
    }

    private void SetObservedIdentityVars(string prefix, WebTargetResult result)
    {
        var identity = WebTargetResultIdentityMapper.ToSelectedIdentity(result);
        var strength = WebTargetResultIdentityMapper.ResolveIdentityStrength(result);
        var hasHelpText = !string.IsNullOrWhiteSpace(result.SelectedHelpText) || result.SelectedHelpTextPresent;
        var hasLegacyName = !string.IsNullOrWhiteSpace(result.SelectedLegacyName) || result.SelectedLegacyNamePresent;

        _ctx.Variables[prefix + ".identity.source"] = "web-uia";
        _ctx.Variables[prefix + ".identity.strength"] = strength.ToString();
        _ctx.Variables[prefix + ".identity.runtimeId"] = identity?.RuntimeId ?? "";
        _ctx.Variables[prefix + ".identity.automationId"] = identity?.AutomationId ?? "";
        _ctx.Variables[prefix + ".identity.name"] = identity?.Name ?? "";
        _ctx.Variables[prefix + ".identity.role"] = identity?.Role ?? "";
        _ctx.Variables[prefix + ".identity.controlType"] = identity?.ControlType ?? "";
        _ctx.Variables[prefix + ".identity.className"] = identity?.ClassName ?? "";
        _ctx.Variables[prefix + ".identity.frameworkId"] = identity?.FrameworkId ?? "";
        _ctx.Variables[prefix + ".identity.ancestorPath"] = identity?.AncestorPath ?? "";
        _ctx.Variables[prefix + ".identity.processName"] = identity?.ProcessName ?? "";
        _ctx.Variables[prefix + ".identity.windowTitle"] = identity?.WindowTitle ?? "";
        _ctx.Variables[prefix + ".identity.boundsHint"] = identity?.BoundsHint ?? "";
        _ctx.Variables[prefix + ".identity.provenance"] = (identity?.Provenance ?? Provenance.Inferred).ToString();
        _ctx.Variables[prefix + ".identity.helpTextPresent"] = hasHelpText ? "true" : "false";
        _ctx.Variables[prefix + ".identity.legacyNamePresent"] = hasLegacyName ? "true" : "false";
        _ctx.Variables[prefix + ".identity.shadowFound"] = result.ShadowEngineFound ? "true" : "false";
        _ctx.Variables[prefix + ".identity.shadowAgreesWithLegacy"] = result.ShadowAgreesWithLegacy ? "true" : "false";
        _ctx.Variables[prefix + ".identity.shadowVerdict"] = result.ShadowEngineVerdict ?? "";
        _ctx.Variables[prefix + ".identity.shadowSelectedName"] = result.ShadowEngineSelectedName ?? "";
        _ctx.Variables[prefix + ".identity.shadowReasons"] = result.ShadowReasons ?? "";
    }

    private void SetDesktopObservedIdentityVars(string prefix, DesktopTargetObservationResult result)
    {
        var identity = DesktopTargetObservationResultIdentityMapper.ToSelectedIdentity(result);
        var strength = DesktopTargetObservationResultIdentityMapper.ResolveIdentityStrength(result);

        _ctx.Variables[prefix + ".identity.source"] = "uia";
        _ctx.Variables[prefix + ".identity.strength"] = strength.ToString();
        _ctx.Variables[prefix + ".identity.runtimeId"] = identity?.RuntimeId ?? "";
        _ctx.Variables[prefix + ".identity.automationId"] = identity?.AutomationId ?? "";
        _ctx.Variables[prefix + ".identity.name"] = identity?.Name ?? "";
        _ctx.Variables[prefix + ".identity.role"] = identity?.Role ?? "";
        _ctx.Variables[prefix + ".identity.controlType"] = identity?.ControlType ?? "";
        _ctx.Variables[prefix + ".identity.className"] = identity?.ClassName ?? "";
        _ctx.Variables[prefix + ".identity.frameworkId"] = identity?.FrameworkId ?? "";
        _ctx.Variables[prefix + ".identity.ancestorPath"] = identity?.AncestorPath ?? "";
        _ctx.Variables[prefix + ".identity.processName"] = identity?.ProcessName ?? "";
        _ctx.Variables[prefix + ".identity.windowTitle"] = identity?.WindowTitle ?? "";
        _ctx.Variables[prefix + ".identity.boundsHint"] = identity?.BoundsHint ?? "";
        _ctx.Variables[prefix + ".identity.rootHwnd"] = result.RootHwnd ?? "";
        _ctx.Variables[prefix + ".identity.hasInvoke"] = result.HasInvoke ? "true" : "false";
        _ctx.Variables[prefix + ".identity.provenance"] = (identity?.Provenance ?? Provenance.Inferred).ToString();
        _ctx.Variables[prefix + ".identity.helpTextPresent"] = result.SelectedHelpTextPresent ? "true" : "false";
        _ctx.Variables[prefix + ".identity.legacyNamePresent"] = result.SelectedLegacyNamePresent ? "true" : "false";
        _ctx.Variables[prefix + ".identity.digest"] = identity == null ? "" : ElementFingerprintBuilder.Build(identity);
    }

    private static FailureKind MapTargetObserveFailureKind(WebTargetResult result)
    {
        if (result.Found)
            return FailureKind.Unverified;

        if (result.CandidateCount > 1 || result.Reason.Contains("ambiguous", StringComparison.OrdinalIgnoreCase))
            return FailureKind.Ambiguous;

        if (result.Reason.Contains("invalid input", StringComparison.OrdinalIgnoreCase))
            return FailureKind.SourceUnavailable;

        return FailureKind.NotFound;
    }

    private static FailureKind MapDesktopTargetObserveFailureKind(DesktopTargetObservationResult result)
    {
        if (result.Found)
            return FailureKind.Unverified;

        if (result.CandidateCount > 1 || result.Reason.Contains("ambiguous", StringComparison.OrdinalIgnoreCase))
            return FailureKind.Ambiguous;

        if (result.Reason.Contains("requires processName or window filter", StringComparison.OrdinalIgnoreCase))
            return FailureKind.SourceUnavailable;

        return FailureKind.NotFound;
    }

    private void SetResolutionVars(string prefix, WebTargetResult r)
    {
        _ctx.Variables[prefix + ".resolution.found"] = r.Found ? "true" : "false";
        _ctx.Variables[prefix + ".resolution.candidateCount"] = r.CandidateCount.ToString();
        _ctx.Variables[prefix + ".resolution.selectedName"] = r.SelectedName ?? "";
        _ctx.Variables[prefix + ".resolution.selectedControlType"] = r.SelectedControlType ?? "";
        _ctx.Variables[prefix + ".resolution.selectedBoundingRect"] = r.SelectedBoundingRect ?? "";
        _ctx.Variables[prefix + ".resolution.windowsSearched"] = r.WindowsSearched.ToString();
        _ctx.Variables[prefix + ".resolution.method"] = "WebTargetResolver";
        _ctx.Variables[prefix + ".resolution.reason"] = r.Reason;
        _ctx.Variables[prefix + ".resolution.hasInvoke"] = r.HasInvoke ? "true" : "false";
        _ctx.Variables[prefix + ".resolution.hasClickablePoint"] = r.HasClickablePoint ? "true" : "false";
        _ctx.Variables[prefix + ".resolution.shadow.found"] = r.ShadowEngineFound ? "true" : "false";
        _ctx.Variables[prefix + ".resolution.shadow.verdict"] = r.ShadowEngineVerdict ?? "";
        _ctx.Variables[prefix + ".resolution.shadow.agreesWithLegacy"] = r.ShadowAgreesWithLegacy ? "true" : "false";
        _ctx.Variables[prefix + ".resolution.shadow.selectedName"] = r.ShadowEngineSelectedName ?? "";
        _ctx.Variables[prefix + ".resolution.shadow.reasons"] = r.ShadowReasons ?? "";
        _ctx.Variables[prefix + ".resolution.identity.runtimeIdPresent"] = !string.IsNullOrWhiteSpace(r.SelectedRuntimeId) ? "true" : "false";
        _ctx.Variables[prefix + ".resolution.identity.runtimeId"] = r.SelectedRuntimeId ?? "";
        _ctx.Variables[prefix + ".resolution.identity.strength"] = ResolveResolutionIdentityStrength(r).ToString();
        _ctx.Variables[prefix + ".resolution.identity.automationId"] = r.SelectedAutomationId ?? "";
        _ctx.Variables[prefix + ".resolution.identity.ancestorPath"] = r.SelectedAncestorPath ?? "";
        _ctx.Variables[prefix + ".resolution.identity.frameworkId"] = r.SelectedFrameworkId ?? "";
        _ctx.Variables[prefix + ".resolution.identity.className"] = r.SelectedClassName ?? "";
        _ctx.Variables[prefix + ".resolution.identity.processName"] = r.SelectedProcessName ?? "";
        _ctx.Variables[prefix + ".resolution.identity.windowTitle"] = r.SelectedWindowTitle ?? "";
        _ctx.Variables[prefix + ".resolution.identity.helpTextPresent"] = r.SelectedHelpTextPresent ? "true" : "false";
        _ctx.Variables[prefix + ".resolution.identity.legacyNamePresent"] = r.SelectedLegacyNamePresent ? "true" : "false";
        if (r.ChildHwndDiagnostics.Count > 0)
            _ctx.Variables[prefix + ".resolution.childHwndDiagnostics"] = string.Join(" | ", r.ChildHwndDiagnostics);
    }

    private static IdentityStrength ResolveResolutionIdentityStrength(WebTargetResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.SelectedRuntimeId))
            return IdentityStrength.Strong;

        if (!string.IsNullOrWhiteSpace(result.SelectedName) ||
            !string.IsNullOrWhiteSpace(result.SelectedControlType) ||
            !string.IsNullOrWhiteSpace(result.SelectedClassName) ||
            !string.IsNullOrWhiteSpace(result.SelectedFrameworkId) ||
            !string.IsNullOrWhiteSpace(result.SelectedAncestorPath))
        {
            return IdentityStrength.Weak;
        }

        return IdentityStrength.None;
    }

    private void SetDesktopResolutionVars(string prefix, DesktopTargetObservationResult result)
    {
        _ctx.Variables[prefix + ".resolution.found"] = result.Found ? "true" : "false";
        _ctx.Variables[prefix + ".resolution.candidateCount"] = result.CandidateCount.ToString(CultureInfo.InvariantCulture);
        _ctx.Variables[prefix + ".resolution.selectedName"] = result.SelectedName ?? "";
        _ctx.Variables[prefix + ".resolution.selectedControlType"] = result.SelectedControlType ?? "";
        _ctx.Variables[prefix + ".resolution.selectedBoundingRect"] = result.SelectedBoundingRect ?? "";
        _ctx.Variables[prefix + ".resolution.method"] = "DesktopTargetObserve";
        _ctx.Variables[prefix + ".resolution.reason"] = result.Reason;
        _ctx.Variables[prefix + ".resolution.hasInvoke"] = result.HasInvoke ? "true" : "false";
        _ctx.Variables[prefix + ".resolution.rootHwnd"] = result.RootHwnd ?? "";
        _ctx.Variables[prefix + ".resolution.identity.runtimeIdPresent"] = !string.IsNullOrWhiteSpace(result.SelectedRuntimeId) ? "true" : "false";
        _ctx.Variables[prefix + ".resolution.identity.runtimeId"] = result.SelectedRuntimeId ?? "";
        _ctx.Variables[prefix + ".resolution.identity.strength"] = DesktopTargetObservationResultIdentityMapper.ResolveIdentityStrength(result).ToString();
        _ctx.Variables[prefix + ".resolution.identity.automationId"] = result.SelectedAutomationId ?? "";
        _ctx.Variables[prefix + ".resolution.identity.ancestorPath"] = result.SelectedAncestorPath ?? "";
        _ctx.Variables[prefix + ".resolution.identity.frameworkId"] = result.SelectedFrameworkId ?? "";
        _ctx.Variables[prefix + ".resolution.identity.className"] = result.SelectedClassName ?? "";
        _ctx.Variables[prefix + ".resolution.identity.processName"] = result.SelectedProcessName ?? "";
        _ctx.Variables[prefix + ".resolution.identity.windowTitle"] = result.SelectedWindowTitle ?? "";
        _ctx.Variables[prefix + ".resolution.identity.helpTextPresent"] = result.SelectedHelpTextPresent ? "true" : "false";
        _ctx.Variables[prefix + ".resolution.identity.legacyNamePresent"] = result.SelectedLegacyNamePresent ? "true" : "false";
    }

    private void SetSafeClickLegacyUsageVars(string prefix, bool usedElClick, bool usedUiaActionExecutor)
    {
        var usedUnsafeFallback = usedElClick || usedUiaActionExecutor;
        _ctx.Variables[prefix + ".legacy.usedElClick"] = usedElClick ? "true" : "false";
        _ctx.Variables[prefix + ".legacy.usedUiaActionExecutor"] = usedUiaActionExecutor ? "true" : "false";
        _ctx.Variables[prefix + ".legacy.usedUnsafeFallback"] = usedUnsafeFallback ? "true" : "false";
        _ctx.Variables[prefix + ".legacy.summary"] =
            $"elClick={(usedElClick ? "true" : "false")};uiaActionExecutor={(usedUiaActionExecutor ? "true" : "false")};unsafeFallback={(usedUnsafeFallback ? "true" : "false")}";

        if (_safeClickDefaultLegacyPrefixes.Contains(prefix))
        {
            if (usedElClick && _safeClickDefaultElClickCounted.Add(prefix))
                _safeClickDefaultElClickUseCount++;
            if (usedUiaActionExecutor && _safeClickDefaultUiaActionExecutorCounted.Add(prefix))
                _safeClickDefaultUiaActionExecutorUseCount++;
            if (usedUnsafeFallback && _safeClickDefaultUnsafeFallbackCounted.Add(prefix))
                _safeClickDefaultUnsafeFallbackUseCount++;
        }
    }

    private (bool UsedElClick, bool UsedUiaActionExecutor) ReadSafeClickLegacyUsage(string prefix)
    {
        var usedElClick = string.Equals(_ctx.Variables.GetValueOrDefault(prefix + ".legacy.usedElClick", "false"), "true", StringComparison.OrdinalIgnoreCase);
        var usedUiaActionExecutor = string.Equals(_ctx.Variables.GetValueOrDefault(prefix + ".legacy.usedUiaActionExecutor", "false"), "true", StringComparison.OrdinalIgnoreCase);
        return (usedElClick, usedUiaActionExecutor);
    }

    private ElementIdentity? ReadSafeClickObservedIdentity(string prefix)
    {
        var selectedName = _ctx.Variables.GetValueOrDefault(prefix + ".resolution.selectedName", "");
        var selectedControlType = _ctx.Variables.GetValueOrDefault(prefix + ".resolution.selectedControlType", "");
        var selectedBoundingRect = _ctx.Variables.GetValueOrDefault(prefix + ".resolution.selectedBoundingRect", "");
        var runtimeId = _ctx.Variables.GetValueOrDefault(prefix + ".resolution.identity.runtimeId", "");
        var automationId = _ctx.Variables.GetValueOrDefault(prefix + ".resolution.identity.automationId", "");
        var className = _ctx.Variables.GetValueOrDefault(prefix + ".resolution.identity.className", "");
        var frameworkId = _ctx.Variables.GetValueOrDefault(prefix + ".resolution.identity.frameworkId", "");
        var ancestorPath = _ctx.Variables.GetValueOrDefault(prefix + ".resolution.identity.ancestorPath", "");
        var processName = _ctx.Variables.GetValueOrDefault(prefix + ".resolution.identity.processName", "");
        var windowTitle = _ctx.Variables.GetValueOrDefault(prefix + ".resolution.identity.windowTitle", "");

        if (string.IsNullOrWhiteSpace(selectedName) &&
            string.IsNullOrWhiteSpace(selectedControlType) &&
            string.IsNullOrWhiteSpace(selectedBoundingRect) &&
            string.IsNullOrWhiteSpace(runtimeId) &&
            string.IsNullOrWhiteSpace(automationId) &&
            string.IsNullOrWhiteSpace(className) &&
            string.IsNullOrWhiteSpace(frameworkId) &&
            string.IsNullOrWhiteSpace(ancestorPath) &&
            string.IsNullOrWhiteSpace(processName) &&
            string.IsNullOrWhiteSpace(windowTitle))
        {
            return null;
        }

        return new ElementIdentity
        {
            RuntimeId = runtimeId,
            AutomationId = automationId,
            Name = selectedName,
            Role = selectedControlType,
            ControlType = selectedControlType,
            ClassName = className,
            FrameworkId = frameworkId,
            AncestorPath = ancestorPath,
            ProcessName = processName,
            WindowTitle = windowTitle,
            BoundsHint = selectedBoundingRect,
            Provenance = string.IsNullOrWhiteSpace(runtimeId) ? Provenance.Inferred : Provenance.Uia
        };
    }

    private static ElementIdentity? ReadDesktopApprovalIdentityForPlanning(ApprovalManifest? manifest)
    {
        if (manifest == null ||
            !string.Equals(manifest.IdentitySource, "uia", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return manifest.ApprovedSelector?.ExpectedIdentity;
    }

    private bool? ReadApprovalInvokeAvailability(string approvalPrefix)
    {
        var rawHasInvoke = _ctx.Variables.GetValueOrDefault(approvalPrefix + ".identity.hasInvoke", "");
        if (bool.TryParse(rawHasInvoke, out var hasInvoke))
            return hasInvoke;

        return null;
    }

    private static DesktopObservedCandidate? BuildDesktopObservedCandidate(
        AutomationElement element,
        string processName,
        string windowTitle,
        IntPtr rootHwnd)
    {
        try
        {
            var runtimeId = UiaTreeWalker.SafeRuntimeId(element);
            var automationId = UiaTreeWalker.SafeId(element);
            var name = UiaTreeWalker.SafeName(element);
            var controlType = UiaTreeWalker.SafeRole(element);
            var className = UiaTreeWalker.SafeClass(element);
            var frameworkId = UiaTreeWalker.SafeFrameworkId(element);
            var helpText = UiaTreeWalker.SafeHelpText(element);
            var legacyName = UiaTreeWalker.SafeLegacyName(element);
            var ancestorPath = BuildDesktopAncestorPath(element, 4);
            var boundsHint = BuildDesktopBoundsHint(element);

            if (string.IsNullOrWhiteSpace(runtimeId) &&
                string.IsNullOrWhiteSpace(automationId) &&
                string.IsNullOrWhiteSpace(name) &&
                string.IsNullOrWhiteSpace(controlType) &&
                string.IsNullOrWhiteSpace(className) &&
                string.IsNullOrWhiteSpace(frameworkId) &&
                string.IsNullOrWhiteSpace(ancestorPath))
            {
                return null;
            }

            var identity = new ElementIdentity
            {
                RuntimeId = runtimeId,
                AutomationId = automationId,
                Name = name,
                HelpText = helpText,
                LegacyName = legacyName,
                Role = controlType,
                ControlType = controlType,
                ClassName = className,
                FrameworkId = frameworkId,
                ProcessName = processName,
                WindowTitle = windowTitle,
                AncestorPath = ancestorPath,
                BoundsHint = boundsHint,
                Provenance = string.IsNullOrWhiteSpace(runtimeId) ? Provenance.Inferred : Provenance.Uia
            };

            return new DesktopObservedCandidate(
                identity,
                rootHwnd,
                SafeInvokeSupported(element),
                HelpTextPresent: !string.IsNullOrWhiteSpace(helpText),
                LegacyNamePresent: !string.IsNullOrWhiteSpace(legacyName));
        }
        catch
        {
            return null;
        }
    }

    private static bool SafeInvokeSupported(AutomationElement element)
    {
        try
        {
            return element.Patterns.Invoke.IsSupported;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildDesktopBoundsHint(AutomationElement element)
    {
        try
        {
            var bounds = element.BoundingRectangle;
            return $"{bounds.Left},{bounds.Top},{bounds.Width},{bounds.Height}";
        }
        catch
        {
            return "";
        }
    }

    private static string BuildDesktopAncestorPath(AutomationElement element, int maxDepth)
    {
        if (maxDepth <= 0)
            return "";

        var segments = new List<string>();
        try
        {
            var current = element.Parent;
            var depth = 0;
            while (current != null && depth < maxDepth)
            {
                var role = UiaTreeWalker.SafeRole(current);
                var name = UiaTreeWalker.SafeName(current);
                var segment = string.IsNullOrWhiteSpace(name) ? role : $"{role}:{name}";
                if (!string.IsNullOrWhiteSpace(segment))
                    segments.Add(segment);
                current = current.Parent;
                depth++;
            }
        }
        catch
        {
            return "";
        }

        segments.Reverse();
        return string.Join(" > ", segments);
    }

    private static bool SameElementIdentity(ElementIdentity left, ElementIdentity right)
    {
        if (!string.IsNullOrWhiteSpace(left.RuntimeId) && !string.IsNullOrWhiteSpace(right.RuntimeId))
            return string.Equals(left.RuntimeId, right.RuntimeId, StringComparison.Ordinal);

        return string.Equals(left.AutomationId, right.AutomationId, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.EffectiveControlType, right.EffectiveControlType, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.ClassName, right.ClassName, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.AncestorPath, right.AncestorPath, StringComparison.OrdinalIgnoreCase);
    }

    private bool? ReadSafeClickInvokeAvailability(string prefix)
    {
        var rawHasInvoke = _ctx.Variables.GetValueOrDefault(prefix + ".resolution.hasInvoke", "");
        if (bool.TryParse(rawHasInvoke, out var hasInvoke))
            return hasInvoke;

        return null;
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    // ── diagnose.msaa ─────────────────────────────────────────────────────────
    private void SetApprovalIdentityVars(string prefix, ApprovalManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.IdentitySchemaVersion))
            return;

        _ctx.Variables[prefix + ".identity.schemaVersion"] = manifest.IdentitySchemaVersion;
        _ctx.Variables[prefix + ".identity.strength"] = manifest.IdentityStrength.ToString();

        if (!string.IsNullOrWhiteSpace(manifest.ApprovedIdentityDigest))
            _ctx.Variables[prefix + ".identity.digest"] = manifest.ApprovedIdentityDigest;

        if (!string.IsNullOrWhiteSpace(manifest.IdentitySource))
            _ctx.Variables[prefix + ".identity.source"] = manifest.IdentitySource;

        if (manifest.ApprovedSelector != null)
            _ctx.Variables[prefix + ".identity.selector"] = JsonSerializer.Serialize(manifest.ApprovedSelector);

        if (!string.IsNullOrWhiteSpace(manifest.IdentityBindingHash))
            _ctx.Variables[prefix + ".identity.bindingHash"] = manifest.IdentityBindingHash;

        if (manifest.ShadowAgreesWithLegacy.HasValue)
        {
            _ctx.Variables[prefix + ".identity.shadowAgreesWithLegacy"] = manifest.ShadowAgreesWithLegacy.Value ? "true" : "false";
            _ctx.Variables[prefix + ".identity.mismatch"] = manifest.ShadowAgreesWithLegacy.Value ? "false" : "true";
        }
    }

    private void CopyApprovalObservationVars(string approvalPrefix, string fromPrefix)
    {
        var rootHwnd = _ctx.Variables.GetValueOrDefault(fromPrefix + ".identity.rootHwnd", "");
        if (!string.IsNullOrWhiteSpace(rootHwnd))
            _ctx.Variables[approvalPrefix + ".identity.rootHwnd"] = rootHwnd;

        var hasInvoke = _ctx.Variables.GetValueOrDefault(fromPrefix + ".identity.hasInvoke", "");
        if (!string.IsNullOrWhiteSpace(hasInvoke))
            _ctx.Variables[approvalPrefix + ".identity.hasInvoke"] = hasInvoke;
    }

    private ApprovedInputBinding? TryBuildManifestApprovedInputBinding(
        ApprovalManifest manifest,
        string fromPrefix,
        string actionKind)
    {
        var approvedText = FirstNonEmpty(
            _ctx.Variables.GetValueOrDefault(fromPrefix + ".approvedText", ""),
            _ctx.Variables.GetValueOrDefault(fromPrefix + ".input.approvedText", ""),
            _ctx.Variables.GetValueOrDefault(fromPrefix + ".type.approvedText", ""),
            _ctx.Variables.GetValueOrDefault(fromPrefix + ".value", ""));
        if (string.IsNullOrWhiteSpace(approvedText) ||
            string.IsNullOrWhiteSpace(manifest.IdentityBindingHash))
        {
            return null;
        }

        var digest = ComputeApprovedValueDigest(approvedText);
        return ApprovedInputBindingHashBuilder.Build(
            actionKind,
            manifest.IdentityBindingHash!,
            manifest.IdentityBindingHash!,
            digest);
    }

    private void SetApprovalInputVars(string approvalPrefix, ApprovalManifest manifest)
    {
        var binding = manifest.ApprovedInputBinding;
        if (binding == null)
            return;

        _ctx.Variables[approvalPrefix + ".input.approvedTextDigest"] = binding.ApprovedValueDigest;
        _ctx.Variables[approvalPrefix + ".input.valueDigest"] = binding.ApprovedValueDigest;
        _ctx.Variables[approvalPrefix + ".input.bindingHash"] = binding.ApprovedInputBindingHash;
        _ctx.Variables[approvalPrefix + ".input.bindingVersion"] = binding.BindingVersion;
        _ctx.Variables[approvalPrefix + ".input.digestAlgorithm"] = binding.ApprovedValueDigestAlgorithm;
        _ctx.Variables[approvalPrefix + ".input.canonicalization"] = binding.ApprovedValueCanonicalization;
        _ctx.Variables[approvalPrefix + ".input.source"] = "manifest";
    }

    private ApprovedInputBindingValidationResult ValidateApprovedInputBinding(
        ApprovalManifest manifest,
        string resolvedDigest)
    {
        return ApprovedInputBindingValidator.Validate(
            manifest.ApprovedInputBinding,
            "type",
            manifest.IdentityBindingHash ?? "",
            manifest.IdentityBindingHash ?? "",
            resolvedDigest);
    }

    private static string ToSafeTypeApprovedInputFailureReason(string reason)
    {
        return reason switch
        {
            "ApprovedInputBindingRequired" => "safe.type requires approved input binding from approval manifest",
            "ApprovedInputDigestMismatch" => "safe.type text does not match manifest-bound approved input digest",
            "ApprovedInputBindingHashMismatch" => "safe.type approved input binding hash mismatch",
            "ApprovedInputIdentityBindingMismatch" => "safe.type approved input identity binding mismatch",
            _ => $"safe.type approved input binding rejected: {reason}"
        };
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return "";
    }

    private static string ComputeApprovedValueDigest(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value ?? ""));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private ApprovedIdentityInput? TryReadApprovedIdentityInput(string fromPrefix)
    {
        var identity = ReadElementIdentity(fromPrefix);
        var source = _ctx.Variables.GetValueOrDefault(fromPrefix + ".identity.source", "");
        var parity = ReadWebSelectorParity(fromPrefix);

        if (identity == null &&
            string.IsNullOrWhiteSpace(source) &&
            parity == null)
        {
            return null;
        }

        return new ApprovedIdentityInput(identity, source, parity);
    }

    private ElementIdentity? ReadElementIdentity(string prefix)
    {
        var runtimeId = _ctx.Variables.GetValueOrDefault(prefix + ".identity.runtimeId", "");
        var automationId = _ctx.Variables.GetValueOrDefault(prefix + ".identity.automationId", "");
        var name = _ctx.Variables.GetValueOrDefault(prefix + ".identity.name", "");
        var helpText = _ctx.Variables.GetValueOrDefault(prefix + ".identity.helpText", "");
        var legacyName = _ctx.Variables.GetValueOrDefault(prefix + ".identity.legacyName", "");
        var legacyValue = _ctx.Variables.GetValueOrDefault(prefix + ".identity.legacyValue", "");
        var labeledByName = _ctx.Variables.GetValueOrDefault(prefix + ".identity.labeledByName", "");
        var role = _ctx.Variables.GetValueOrDefault(prefix + ".identity.role", "");
        var controlType = _ctx.Variables.GetValueOrDefault(prefix + ".identity.controlType", "");
        var className = _ctx.Variables.GetValueOrDefault(prefix + ".identity.className", "");
        var frameworkId = _ctx.Variables.GetValueOrDefault(prefix + ".identity.frameworkId", "");
        var processName = _ctx.Variables.GetValueOrDefault(prefix + ".identity.processName", "");
        var windowTitle = _ctx.Variables.GetValueOrDefault(prefix + ".identity.windowTitle", "");
        var ancestorPath = _ctx.Variables.GetValueOrDefault(prefix + ".identity.ancestorPath", "");
        var parentFingerprint = _ctx.Variables.GetValueOrDefault(prefix + ".identity.parentFingerprint", "");
        var boundsHint = _ctx.Variables.GetValueOrDefault(prefix + ".identity.boundsHint", "");

        int? siblingIndex = null;
        if (int.TryParse(_ctx.Variables.GetValueOrDefault(prefix + ".identity.siblingIndex", ""), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSibling))
            siblingIndex = parsedSibling;

        var provenance = Provenance.Inferred;
        if (Enum.TryParse(_ctx.Variables.GetValueOrDefault(prefix + ".identity.provenance", ""), ignoreCase: true, out Provenance parsedProvenance))
            provenance = parsedProvenance;

        if (string.IsNullOrWhiteSpace(runtimeId) &&
            string.IsNullOrWhiteSpace(automationId) &&
            string.IsNullOrWhiteSpace(name) &&
            string.IsNullOrWhiteSpace(helpText) &&
            string.IsNullOrWhiteSpace(legacyName) &&
            string.IsNullOrWhiteSpace(legacyValue) &&
            string.IsNullOrWhiteSpace(labeledByName) &&
            string.IsNullOrWhiteSpace(role) &&
            string.IsNullOrWhiteSpace(controlType) &&
            string.IsNullOrWhiteSpace(className) &&
            string.IsNullOrWhiteSpace(frameworkId) &&
            string.IsNullOrWhiteSpace(processName) &&
            string.IsNullOrWhiteSpace(windowTitle) &&
            string.IsNullOrWhiteSpace(ancestorPath) &&
            string.IsNullOrWhiteSpace(parentFingerprint) &&
            string.IsNullOrWhiteSpace(boundsHint) &&
            !siblingIndex.HasValue)
        {
            return null;
        }

        return new ElementIdentity
        {
            RuntimeId = runtimeId,
            AutomationId = automationId,
            Name = name,
            HelpText = helpText,
            LegacyName = legacyName,
            LegacyValue = legacyValue,
            LabeledByName = labeledByName,
            Role = role,
            ControlType = controlType,
            ClassName = className,
            FrameworkId = frameworkId,
            ProcessName = processName,
            WindowTitle = windowTitle,
            AncestorPath = ancestorPath,
            ParentFingerprint = parentFingerprint,
            BoundsHint = boundsHint,
            SiblingIndex = siblingIndex,
            Provenance = provenance
        };
    }

    private WebSelectorParity? ReadWebSelectorParity(string prefix)
    {
        var rawAgree = _ctx.Variables.GetValueOrDefault(prefix + ".identity.shadowAgreesWithLegacy", "");
        if (string.IsNullOrWhiteSpace(rawAgree))
            rawAgree = _ctx.Variables.GetValueOrDefault(prefix + ".resolution.shadow.agreesWithLegacy", "");

        var rawVerdict = _ctx.Variables.GetValueOrDefault(prefix + ".identity.shadowVerdict", "");
        if (string.IsNullOrWhiteSpace(rawVerdict))
            rawVerdict = _ctx.Variables.GetValueOrDefault(prefix + ".resolution.shadow.verdict", "");

        var rawSelectedName = _ctx.Variables.GetValueOrDefault(prefix + ".identity.shadowSelectedName", "");
        if (string.IsNullOrWhiteSpace(rawSelectedName))
            rawSelectedName = _ctx.Variables.GetValueOrDefault(prefix + ".resolution.shadow.selectedName", "");

        var rawReasons = _ctx.Variables.GetValueOrDefault(prefix + ".identity.shadowReasons", "");
        if (string.IsNullOrWhiteSpace(rawReasons))
            rawReasons = _ctx.Variables.GetValueOrDefault(prefix + ".resolution.shadow.reasons", "");

        var rawFound = _ctx.Variables.GetValueOrDefault(prefix + ".identity.shadowFound", "");
        if (string.IsNullOrWhiteSpace(rawFound))
            rawFound = _ctx.Variables.GetValueOrDefault(prefix + ".resolution.shadow.found", "");

        if (string.IsNullOrWhiteSpace(rawAgree) &&
            string.IsNullOrWhiteSpace(rawVerdict) &&
            string.IsNullOrWhiteSpace(rawSelectedName) &&
            string.IsNullOrWhiteSpace(rawReasons) &&
            string.IsNullOrWhiteSpace(rawFound))
        {
            return null;
        }

        var found = bool.TryParse(rawFound, out var parsedFound) && parsedFound;
        var agrees = bool.TryParse(rawAgree, out var parsedAgree) && parsedAgree;
        var reasons = string.IsNullOrWhiteSpace(rawReasons)
            ? Array.Empty<string>()
            : rawReasons.Split(" | ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new WebSelectorParity
        {
            EngineFound = found,
            EngineVerdict = string.IsNullOrWhiteSpace(rawVerdict) ? null : rawVerdict,
            EngineSelectedName = string.IsNullOrWhiteSpace(rawSelectedName) ? null : rawSelectedName,
            AgreesWithLegacy = agrees,
            Reasons = reasons
        };
    }

    private RecipeStepRunResult ExecuteDiagnoseMsaa(RecipeStepDefinition step, Stopwatch sw)
    {
        var prefix = step.SaveAs ?? "msaa";
        var targetText = step.Args?.GetValueOrDefault("targetText") ?? "";
        if (string.IsNullOrWhiteSpace(targetText))
            return Fail(step, sw, "diagnose.msaa requires 'targetText'.");

        var proc = step.Args?.GetValueOrDefault("proc") ?? "msedge";

        // Get owned session HWND
        IntPtr sessionHwnd = IntPtr.Zero;
        foreach (var key in _ctx.Variables.Keys.Where(k => k.EndsWith(".hwnd")))
        {
            var pfx = key[..^5];
            if (_ctx.Variables.GetValueOrDefault(pfx + ".owned", "false") == "true" &&
                long.TryParse(_ctx.Variables[key], out var hl))
            {
                sessionHwnd = new IntPtr(hl);
                break;
            }
        }

        if (sessionHwnd == IntPtr.Zero)
            return Fail(step, sw, "diagnose.msaa: no owned browser session active.");

        try
        {
            var reader = new MsaaAccessibleReader();
            var result = reader.Discover(sessionHwnd, targetText, proc);

            _ctx.Variables[prefix + ".found"] = result.Found ? "true" : "false";
            _ctx.Variables[prefix + ".nodeCount"] = result.NodeCount.ToString();
            _ctx.Variables[prefix + ".candidateCount"] = result.CandidateCount.ToString();
            _ctx.Variables[prefix + ".linkCount"] = result.LinkCount.ToString();
            _ctx.Variables[prefix + ".buttonCount"] = result.ButtonCount.ToString();
            _ctx.Variables[prefix + ".selectedName"] = result.SelectedName ?? "";
            _ctx.Variables[prefix + ".selectedRole"] = result.SelectedRole ?? "";
            _ctx.Variables[prefix + ".selectedDefaultAction"] = result.SelectedDefaultAction ?? "";
            _ctx.Variables[prefix + ".selectedLocation"] = result.SelectedLocation ?? "";
            _ctx.Variables[prefix + ".sourceHwnd"] = result.SourceHwnd ?? "";
            _ctx.Variables[prefix + ".reason"] = result.Reason;
            if (!string.IsNullOrWhiteSpace(result.CandidatesJson))
                _ctx.Variables[prefix + ".candidatesJson"] = result.CandidatesJson;

            // HWND summaries
            if (result.HwndSummaries.Count > 0)
            {
                var summaries = new List<string>();
                foreach (var s in result.HwndSummaries)
                {
                    var vis = s.IsVisible ? "VIS" : "HID";
                    var ena = s.IsEnabled ? "ENA" : "DIS";
                    var lvl = s.IsTopLevel ? "TOP" : "CHD";
                    summaries.Add($"{s.HwndHex} [{lvl}] [{vis}/{ena}] '{s.ClassName}' nodes={s.NodeCount} links={s.LinkCount} btns={s.ButtonCount}");
                }
                _ctx.Variables[prefix + ".hwndSummaries"] = string.Join(" | ", summaries);
            }

            sw.Stop();
            var msg = result.Found
                ? $"MSAA found '{result.SelectedName}' ({result.SelectedRole}) in {result.NodeCount} nodes"
                : $"MSAA: {result.Reason}";
            return new RecipeStepRunResult(step.Id, step.Kind, true, msg, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new RecipeStepRunResult(step.Id, step.Kind, false,
                $"diagnose.msaa: {ex.Message}", sw.ElapsedMilliseconds);
        }
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

    private sealed record DesktopObservedCandidate(
        ElementIdentity Identity,
        IntPtr RootHwnd,
        bool HasInvoke,
        bool HelpTextPresent,
        bool LegacyNamePresent);

    private sealed record SafeTypeLiveTarget(
        bool Success,
        ElementIdentity? ObservedIdentity,
        IntPtr? RootHwnd,
        string SelectedName,
        string? ProcessName,
        string? WindowTitle,
        FailureKind FailureKind,
        string BlockReason,
        string Reason)
    {
        public static SafeTypeLiveTarget Ok(
            ElementIdentity observedIdentity,
            IntPtr? rootHwnd,
            string selectedName,
            string? processName,
            string? windowTitle) =>
            new(
                true,
                observedIdentity,
                rootHwnd,
                selectedName,
                processName,
                windowTitle,
                FailureKind.Unverified,
                "",
                "");

        public static SafeTypeLiveTarget Fail(FailureKind failureKind, string blockReason, string reason) =>
            new(false, null, null, "", null, null, failureKind, blockReason, reason);
    }
}
