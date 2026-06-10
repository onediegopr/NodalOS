using System.Text.Json;
using OneBrain.Actions.Uia;
using OneBrain.Core.Actions;
using OneBrain.Observation;

var command = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "help";

switch (command)
{
    case "snapshot":
    {
        var reader = new CognitiveSnapshotReader();
        var snapshot = reader.Read();

        if (snapshot is null)
        {
            Console.Error.WriteLine("No foreground window detected.");
            Environment.ExitCode = 1;
            return;
        }

        WriteJson(snapshot);
        return;
    }

    case "act":
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage:");
            Console.Error.WriteLine("  act type @eN \"text\"");
            Console.Error.WriteLine("  act invoke @eN");
            Console.Error.WriteLine("  act focus @eN");
            Environment.ExitCode = 1;
            return;
        }

        var kind = args[1];
        var targetRef = args[2];
        var text = args.Length >= 4 ? string.Join(" ", args.Skip(3)) : null;

        var executor = new UiaActionExecutor();
        var result = executor.Execute(new ActionRequest(kind, targetRef, text));

        WriteJson(result);

        if (!result.Success)
        {
            Environment.ExitCode = 1;
        }

        return;
    }

    case "help":
    default:
        Console.WriteLine("ONE BRAIN CLI");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  snapshot                    Reads the current foreground window and visible UIA elements.");
        Console.WriteLine("  act type @eN \"text\"          Types text into a target element.");
        Console.WriteLine("  act invoke @eN              Invokes a target element.");
        Console.WriteLine("  act focus @eN               Focuses a target element.");
        return;
}

static void WriteJson<T>(T value)
{
    var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
    {
        WriteIndented = true
    });

    Console.WriteLine(json);
}
