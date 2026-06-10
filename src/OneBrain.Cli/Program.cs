using System.Text.Json;
using OneBrain.Core.Actions;
using OneBrain.Observation;
using OneBrain.Actions.Uia;
using OneBrain.Verification.Engine;

var argsList = args.ToList();
var cmd = argsList.Count > 0 ? argsList[0].ToLowerInvariant() : "help";

if (cmd == "snapshot")
{
    string? snapshotProc = null, snapshotWin = null;
    for (int i = 1; i < argsList.Count; i++)
    {
        if (argsList[i] == "--process" && i + 1 < argsList.Count) snapshotProc = argsList[++i];
        else if (argsList[i] == "--window" && i + 1 < argsList.Count) snapshotWin = argsList[++i];
    }
    var res = new CognitiveSnapshotReader().Read(snapshotProc, snapshotWin);
    Console.WriteLine(JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = true }));
}
else if (cmd == "act" || cmd == "actv")
{
    if (argsList.Count < 3)
    {
        PrintUsage();
        return;
    }

    string kind = argsList[1];
    string? proc = null;
    string? win = null;
    string? target = null;
    List<string> text = new();

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
                "--role" => $"role:{val}",
                "--name" => $"name:{val}",
                "--automation-id" or "--automationid" => $"automation-id:{val}",
                "--class" or "--class-name" or "--classname" => $"class:{val}",
                _ => null
            };
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
        Console.WriteLine("Error: Target selector required.");
        return;
    }

    var req = new ActionRequest(kind, target, string.Join(" ", text), proc, win);

    var res = cmd == "act"
        ? (object)new UiaActionExecutor().Execute(req)
        : (object)new BasicActionVerifier().ExecuteAndVerify(req);

    Console.WriteLine(JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = true }));
}
else
{
    PrintUsage();
}

static void PrintUsage()
{
    Console.WriteLine("ONE BRAIN CLI");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  snapshot");
    Console.WriteLine("  act [kind] [target]");
    Console.WriteLine("  actv [kind] [target]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --process VALUE");
    Console.WriteLine("  --window VALUE");
    Console.WriteLine("  --role VALUE");
    Console.WriteLine("  --name VALUE");
    Console.WriteLine("  --automation-id VALUE");
    Console.WriteLine("  --class VALUE");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  actv type --process Notepad --role Document [text]");
    Console.WriteLine("  actv invoke --process Notepad --automation-id Close");
}
