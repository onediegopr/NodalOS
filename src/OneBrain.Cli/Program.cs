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
            PrintActUsage();
            Environment.ExitCode = 1;
            return;
        }

        var kind = args[1];
        var parse = ParseTargetAndText(args.Skip(2).ToArray());

        if (!parse.Success)
        {
            Console.Error.WriteLine(parse.Error);
            PrintActUsage();
            Environment.ExitCode = 1;
            return;
        }

        var executor = new UiaActionExecutor();
        var result = executor.Execute(new ActionRequest(kind, parse.TargetSelector, parse.Text));

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
        Console.WriteLine("  snapshot");
        Console.WriteLine("      Reads the current foreground window and visible UIA elements.");
        Console.WriteLine();
        Console.WriteLine("  act type @eN [text]");
        Console.WriteLine("      Types text into a target element.");
        Console.WriteLine();
        Console.WriteLine("  act type --role Document [text]");
        Console.WriteLine("      Types text into the first element with role Document.");
        Console.WriteLine();
        Console.WriteLine("  act invoke @eN");
        Console.WriteLine("      Invokes a target element by temporary ref.");
        Console.WriteLine();
        Console.WriteLine("  act invoke --name [visible name]");
        Console.WriteLine("      Invokes by visible accessible name.");
        Console.WriteLine();
        Console.WriteLine("  act invoke --automation-id [id]");
        Console.WriteLine("      Invokes by AutomationId.");
        Console.WriteLine();
        Console.WriteLine("  act focus --role Document");
        Console.WriteLine("      Focuses first element with role Document.");
        return;
}

static (bool Success, string TargetSelector, string? Text, string Error) ParseTargetAndText(string[] args)
{
    if (args.Length == 0)
    {
        return (false, "", null, "Missing target.");
    }

    if (args[0].StartsWith("@e", StringComparison.OrdinalIgnoreCase))
    {
        var text = args.Length >= 2 ? string.Join(" ", args.Skip(1)) : null;
        return (true, args[0], text, "");
    }

    if (args.Length >= 2 && args[0].StartsWith("--", StringComparison.OrdinalIgnoreCase))
    {
        var option = args[0].Trim().ToLowerInvariant();
        var value = args[1];
        var text = args.Length >= 3 ? string.Join(" ", args.Skip(2)) : null;

        var selector = option switch
        {
            "--name" => $"name:{value}",
            "--automation-id" => $"automation-id:{value}",
            "--automationid" => $"automation-id:{value}",
            "--role" => $"role:{value}",
            "--class" => $"class:{value}",
            "--class-name" => $"class:{value}",
            _ => ""
        };

        if (string.IsNullOrWhiteSpace(selector))
        {
            return (false, "", null, $"Unknown selector option: {option}");
        }

        return (true, selector, text, "");
    }

    return (false, "", null, "Invalid target syntax.");
}

static void PrintActUsage()
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  act type @eN [text]");
    Console.Error.WriteLine("  act type --role Document [text]");
    Console.Error.WriteLine("  act invoke @eN");
    Console.Error.WriteLine("  act invoke --name [visible name]");
    Console.Error.WriteLine("  act invoke --automation-id [id]");
    Console.Error.WriteLine("  act focus --role Document");
}

static void WriteJson<T>(T value)
{
    var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
    {
        WriteIndented = true
    });

    Console.WriteLine(json);
}
