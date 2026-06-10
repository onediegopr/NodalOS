using System.Text.Json;
using OneBrain.Core.Actions;
using OneBrain.Observation;
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
    Console.WriteLine("  snapshot [--process VALUE] [--window VALUE]");
    Console.WriteLine("  act   <kind> <selector> [text]");
    Console.WriteLine("  actv  <kind> <selector> [text]");
    Console.WriteLine("  browser open [--edge|--chrome] [--force-accessibility] <url-or-file>");
    Console.WriteLine();
    Console.WriteLine("Selectors (use exactly one):");
    Console.WriteLine("  --role VALUE          ControlType (Document, Edit, Button …)");
    Console.WriteLine("  --name VALUE          element Name (partial match)");
    Console.WriteLine("  --automation-id VALUE AutomationId (exact match)");
    Console.WriteLine("  --class VALUE         ClassName (exact match)");
    Console.WriteLine("  @eN                   element ref from last snapshot");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  snapshot --process Notepad");
    Console.WriteLine("  actv type  --process Notepad --role Document \"Hola ONE Brain\"");
    Console.WriteLine("  actv invoke --process Notepad --automation-id Close");
    Console.WriteLine("  browser open --edge --force-accessibility tools/browser-smoke/browser-smoke.html");
    Console.WriteLine("  snapshot --process msedge");
    Console.WriteLine("  actv type  --process msedge --name \"ONE Brain Search\" \"hola browser\"");
    Console.WriteLine("  actv invoke --process msedge --name \"Run ONE Brain Search\"");
}
