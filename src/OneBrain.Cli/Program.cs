using System.Text.Json;
using OneBrain.Observation.Windows;

var command = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "help";

switch (command)
{
    case "snapshot":
    {
        var reader = new ForegroundWindowReader();
        var snapshot = reader.Read();

        if (snapshot is null)
        {
            Console.Error.WriteLine("No foreground window detected.");
            Environment.ExitCode = 1;
            return;
        }

        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        Console.WriteLine(json);
        return;
    }

    case "help":
    default:
        Console.WriteLine("ONE BRAIN CLI");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  snapshot    Reads the current foreground window.");
        return;
}
