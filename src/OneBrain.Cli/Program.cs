using OneBrain.Cli;
using System.Text.Json;
using OneBrain.Core.Actions;
using OneBrain.Observation;
using OneBrain.Observation.Uia;
using OneBrain.Actions.Uia;
using OneBrain.Verification.Engine;

var argsList = args.ToList();
var cmd      = argsList.Count > 0 ? argsList[0].ToLowerInvariant() : "help";

// ── snapshot ─────────────────────────────────────────────────────────────────
if (cmd == "snapshot")
{
    string? snapshotProc = null, snapshotWin = null;
    for (int i = 1; i < argsList.Count; i++)
    {
        if      (argsList[i] == "--process" && i + 1 < argsList.Count) snapshotProc = argsList[++i];
        else if (argsList[i] == "--window"  && i + 1 < argsList.Count) snapshotWin  = argsList[++i];
    }
    var res = new CognitiveSnapshotReader().Read(snapshotProc, snapshotWin);
    Console.WriteLine(JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = true }));
}

// ── act / actv ────────────────────────────────────────────────────────────────
else if (cmd == "act" || cmd == "actv")
{
    if (argsList.Count < 3)
    {
        PrintUsage();
        return;
    }

    string  kind   = argsList[1];
    string? proc   = null;
    string? win    = null;
    string? target = null;
    var     text   = new List<string>();

    for (int i = 2; i < argsList.Count; i++)
    {
        var a = argsList[i];

        if (a == "--process" && i + 1 < argsList.Count)
        {
            proc = argsList[++i];
        }
        else if (a == "--window" && i + 1 < argsList.Count)
        {
            win = argsList[++i];
        }
        else if (a.StartsWith("--") && target == null)
        {
            if (i + 1 >= argsList.Count)
            {
                Console.WriteLine("Error: missing value for selector.");
                return;
            }

            var val = argsList[++i];

            target = a.ToLowerInvariant() switch
            {
                "--role"                           => $"role:{val}",
                "--name"                           => $"name:{val}",
                "--automation-id" or "--automationid" => $"automation-id:{val}",
                "--class" or "--class-name" or "--classname" => $"class:{val}",
                _ => null
            };

            if (target == null)
            {
                Console.WriteLine($"Error: unknown selector flag '{a}'.");
                return;
            }
        }
        else if (a.StartsWith("--") && target != null)
        {
            Console.WriteLine($"Error: only one selector flag is allowed (already have '{target}').");
            return;
        }
        else if (a.StartsWith("@e", StringComparison.OrdinalIgnoreCase) && target == null)
        {
            target = a;
        }
        else
        {
            text.Add(a);
        }
    }

    if (target == null)
    {
        Console.WriteLine("Error: target selector required (--role, --name, --automation-id, --class, or @eN).");
        return;
    }

    var req = new ActionRequest(kind, target, string.Join(" ", text), proc, win);

    var res = cmd == "act"
        ? (object)new UiaActionExecutor().Execute(req)
        : (object)new BasicActionVerifier().ExecuteAndVerify(req);

    Console.WriteLine(JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = true }));
}

// ── browser open ─────────────────────────────────────────────────────────────
else if (cmd == "browser")
{
    if (argsList.Count < 3 || argsList[1].ToLowerInvariant() != "open")
    {
        Console.WriteLine("Usage: browser open [--edge|--chrome] [--force-accessibility] <url-or-file>");
        return;
    }

    string? browserName       = null;
    string? url               = null;
    bool    forceAccessibility = false;

    for (int i = 2; i < argsList.Count; i++)
    {
        var a = argsList[i];
        if      (a == "--edge")                 browserName = "edge";
        else if (a == "--chrome")               browserName = "chrome";
        else if (a == "--force-accessibility")  forceAccessibility = true;
        else if (!a.StartsWith("--"))           url = a;
    }

    if (url == null || browserName == null)
    {
        Console.WriteLine("Usage: browser open [--edge|--chrome] [--force-accessibility] <url-or-file>");
        return;
    }

    // Convert local path to file:// URI
    if (!url.StartsWith("http://",  StringComparison.OrdinalIgnoreCase) &&
        !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
        !url.StartsWith("file://",  StringComparison.OrdinalIgnoreCase))
    {
        url = new Uri(Path.GetFullPath(url)).AbsoluteUri;
    }

    var extra = forceAccessibility ? "--force-renderer-accessibility " : "";
    var browserArgs = $"{extra}\"{url}\"";

    // Candidate executable paths
    string[] candidates = browserName == "edge"
        ? [@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
           @"C:\Program Files\Microsoft\Edge\Application\msedge.exe"]
        : [@"C:\Program Files\Google\Chrome\Application\chrome.exe",
           @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"];

    var launched = false;
    foreach (var exe in candidates)
    {
        if (!File.Exists(exe)) continue;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe, browserArgs)
            {
                UseShellExecute = false
            });
            launched = true;
            break;
        }
        catch { }
    }

    if (!launched)
    {
        // Last-resort: rely on the executable being in PATH
        var fallbackExe = browserName == "edge" ? "msedge" : "chrome";
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fallbackExe, browserArgs)
            {
                UseShellExecute = false
            });
            launched = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Opened   = false,
                Browser  = browserName,
                Url      = url,
                Error    = ex.Message
            }, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }
    }

    // Brief pause so the browser has a moment to start before the caller runs snapshot.
    System.Threading.Thread.Sleep(1500);

    Console.WriteLine(JsonSerializer.Serialize(new
    {
        Opened             = true,
        Browser            = browserName,
        Url                = url,
        ForceAccessibility = forceAccessibility,
        Notes              = "Wait ~2–3s before running snapshot if page is heavy."
    }, new JsonSerializerOptions { WriteIndented = true }));
}

// ── diagnose uia ─────────────────────────────────────────────────────────────
else if (cmd == "app")
{
    if (argsList.Count < 3 || argsList[1].ToLowerInvariant() != "open")
    {
        Console.WriteLine("Usage: app open [explorer|calculator|notepad] [path/args]");
        return;
    }
    AppLauncher.Launch(argsList[2], argsList.Count > 3 ? string.Join(" ", argsList.Skip(3)) : "");
}
else if (cmd == "diagnose")
{
    if (argsList.Count < 2 || argsList[1].ToLowerInvariant() != "uia")
    {
        Console.WriteLine("Usage: diagnose uia --process VALUE [--window VALUE]");
        Console.WriteLine("                    [--contains TEXT] [--role ROLE] [--raw]");
        return;
    }

    string? diagProc = null, diagWin = null, diagContains = null, diagRole = null;
    bool    diagRaw  = false;

    for (int i = 2; i < argsList.Count; i++)
    {
        var a = argsList[i];
        if      (a == "--process"  && i + 1 < argsList.Count) diagProc     = argsList[++i];
        else if (a == "--window"   && i + 1 < argsList.Count) diagWin      = argsList[++i];
        else if (a == "--contains" && i + 1 < argsList.Count) diagContains = argsList[++i];
        else if (a == "--role"     && i + 1 < argsList.Count) diagRole     = argsList[++i];
        else if (a == "--raw")                                 diagRaw      = true;
    }

    if (diagProc == null && diagWin == null)
    {
        Console.Error.WriteLine("Error: --process or --window required for diagnose uia.");
        return;
    }

    var entries = new UiaDiagnosticReader().Read(diagProc, diagWin, diagContains, diagRole, diagRaw);
    Console.WriteLine(JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true }));
}

// ── wait ─────────────────────────────────────────────────────────────────────
else if (cmd == "wait")
{
    string? waitProc = null, waitWin = null;
    string? waitName = null, waitRole = null, waitTitleContains = null;
    int timeout  = 5000;
    int interval = 500;

    for (int i = 1; i < argsList.Count; i++)
    {
        var a = argsList[i];
        if      (a == "--process"        && i + 1 < argsList.Count) waitProc          = argsList[++i];
        else if (a == "--window"         && i + 1 < argsList.Count) waitWin           = argsList[++i];
        else if (a == "--name"           && i + 1 < argsList.Count) waitName          = argsList[++i];
        else if (a == "--role"           && i + 1 < argsList.Count) waitRole          = argsList[++i];
        else if (a == "--title-contains" && i + 1 < argsList.Count) waitTitleContains = argsList[++i];
        else if (a == "--timeout"        && i + 1 < argsList.Count) int.TryParse(argsList[++i], out timeout);
        else if (a == "--interval"       && i + 1 < argsList.Count) int.TryParse(argsList[++i], out interval);
    }

    if (waitProc == null && waitWin == null)
    {
        Console.Error.WriteLine("Error: --process or --window required for wait.");
        return;
    }
    if (waitName == null && waitRole == null && waitTitleContains == null)
    {
        Console.Error.WriteLine("Error: specify one of --name, --role, --title-contains.");
        return;
    }

    interval = Math.Max(100, interval);
    var selectorUsed = waitTitleContains != null    ? $"title-contains:{waitTitleContains}"
                     : waitName != null && waitRole != null ? $"name:{waitName}+role:{waitRole}"
                     : waitName != null              ? $"name:{waitName}"
                                                     : $"role:{waitRole}";
    var reader   = new CognitiveSnapshotReader();
    var sw       = System.Diagnostics.Stopwatch.StartNew();
    int attempts = 0;
    string lastTitle = "";

    while (sw.ElapsedMilliseconds < timeout)
    {
        attempts++;
        var snap = reader.Read(waitProc, waitWin);
        lastTitle = snap?.Window.Title ?? "";

        if (snap != null)
        {
            bool found = false;
            object? matchObj = null;

            if (waitTitleContains != null)
            {
                found = snap.Window.Title.Contains(waitTitleContains, StringComparison.OrdinalIgnoreCase);
                if (found) matchObj = new { Title = snap.Window.Title };
            }
            else if (waitName != null)
            {
                // --role can be combined with --name to skip label/Text nodes
                var el = waitRole != null
                    ? snap.Elements.FirstOrDefault(e =>
                        e.Name.Contains(waitName, StringComparison.OrdinalIgnoreCase) &&
                        e.Role.Equals(waitRole, StringComparison.OrdinalIgnoreCase))
                    : snap.Elements.FirstOrDefault(e =>
                        e.Name.Contains(waitName, StringComparison.OrdinalIgnoreCase));
                found = el != null;
                if (found) matchObj = el;
            }
            else if (waitRole != null)
            {
                var el = snap.Elements.FirstOrDefault(e =>
                    e.Role.Equals(waitRole, StringComparison.OrdinalIgnoreCase));
                found = el != null;
                if (found) matchObj = el;
            }

            if (found)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    Success         = true,
                    ElapsedMs       = sw.ElapsedMilliseconds,
                    Attempts        = attempts,
                    SelectorUsed    = selectorUsed,
                    Message         = "Found.",
                    LastWindowTitle = lastTitle,
                    MatchedElement  = matchObj
                }, new JsonSerializerOptions { WriteIndented = true }));
                return;
            }
        }

        System.Threading.Thread.Sleep(interval);
    }

    Console.WriteLine(JsonSerializer.Serialize(new
    {
        Success         = false,
        ElapsedMs       = sw.ElapsedMilliseconds,
        Attempts        = attempts,
        SelectorUsed    = selectorUsed,
        Message         = $"Timeout after {timeout}ms ({attempts} attempts).",
        LastWindowTitle = lastTitle
    }, new JsonSerializerOptions { WriteIndented = true }));
}

// ── fallback ─────────────────────────────────────────────────────────────────
else
{
    PrintUsage();
}

static void PrintUsage()
{
    Console.WriteLine("ONE BRAIN CLI");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  snapshot  [--process VALUE] [--window VALUE]");
    Console.WriteLine("  act   <kind> <selector> [text]");
    Console.WriteLine("  actv  <kind> <selector> [text]");
    Console.WriteLine("  browser open [--edge|--chrome] [--force-accessibility] <url-or-file>");
      Console.WriteLine("  app   open [explorer|calculator|notepad] [path/args]");
    Console.WriteLine("  diagnose uia --process VALUE [--window VALUE]");
    Console.WriteLine("               [--contains TEXT] [--role ROLE] [--raw]");
    Console.WriteLine("  wait  --process VALUE [--window VALUE]");
    Console.WriteLine("        (--name TEXT | --role ROLE | --title-contains TEXT)");
    Console.WriteLine("        [--timeout MS]  [--interval MS]");
    Console.WriteLine();
    Console.WriteLine("Selectors (use exactly one for act/actv):");
    Console.WriteLine("  --role VALUE          ControlType (Document, Edit, Button …)");
    Console.WriteLine("  --name VALUE          Name / HelpText / LegacyIAccessible.Name (partial match)");
    Console.WriteLine("  --automation-id VALUE AutomationId (exact match)");
    Console.WriteLine("  --class VALUE         ClassName (exact match)");
    Console.WriteLine("  @eN                   element ref from last snapshot");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  snapshot --process Notepad");
    Console.WriteLine("  actv type  --process Notepad --role Document \"Hola ONE Brain\"");
    Console.WriteLine("  actv invoke --process Notepad --automation-id Close");
    Console.WriteLine("  browser open --edge --force-accessibility tools/browser-smoke/browser-smoke.html");
      Console.WriteLine("  app open explorer [path]");
      Console.WriteLine("  app open calculator");
      Console.WriteLine("  app open notepad");
    Console.WriteLine("  snapshot  --process msedge");
    Console.WriteLine("  diagnose uia --process msedge --contains \"ONE Brain\"");
    Console.WriteLine("  diagnose uia --process msedge --role Edit");
    Console.WriteLine("  diagnose uia --process msedge --raw");
    Console.WriteLine("  wait --process msedge --name \"ONE Brain Search\" --timeout 5000");
    Console.WriteLine("  wait --process msedge --title-contains \"ONE Brain\" --timeout 5000");
    Console.WriteLine("  actv type   --process msedge --name \"ONE Brain Search\" \"hola\"");
    Console.WriteLine("  actv invoke --process msedge --name \"Run ONE Brain Search\"");
}
