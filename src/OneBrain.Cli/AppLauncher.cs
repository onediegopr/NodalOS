using System.Diagnostics;
using System.Text.Json;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace OneBrain.Cli;

public static class AppLauncher
{
    public static void Launch(string appType, string appArgs)
    {
        string? exe         = null;
        string? processName = null;
        string? notes       = null;

        try
        {
            switch (appType.ToLowerInvariant())
            {
                case "explorer":
                    if (string.IsNullOrWhiteSpace(appArgs)) { PrintError("explorer", "Path required."); return; }
                    if (!Directory.Exists(appArgs)) { PrintError("explorer", "Path not found."); return; }
                    exe = "explorer.exe"; processName = "explorer"; appArgs = $"\"{Path.GetFullPath(appArgs)}\""; notes = "Started explorer.exe";
                    break;
                case "calculator":
                    try { Process.Start(new ProcessStartInfo("calculator:") { UseShellExecute = true }); }
                    catch { Process.Start(new ProcessStartInfo("calc.exe") { UseShellExecute = true }); }
                    processName = "CalculatorApp"; notes = "Started calculator";
                    break;
                case "notepad":
                    exe = "notepad.exe"; processName = "Notepad"; notes = "Started notepad.exe";
                    break;
                default:
                    Console.WriteLine($"Error: unsupported app '{appType}'."); return;
            }
            if (exe != null) { Process.Start(new ProcessStartInfo(exe, appArgs) { UseShellExecute = true }); }
            System.Threading.Thread.Sleep(1000);
            PrintJson(new { Opened = true, App = appType, ProcessName = processName, Args = appArgs, Notes = notes });
        }
        catch (Exception ex) { PrintJson(new { Opened = false, App = appType, Error = ex.Message }); }
    }
    private static void PrintError(string app, string msg) { PrintJson(new { Opened = false, App = app, Notes = msg }); }
    private static void PrintJson<T>(T value) { Console.WriteLine(JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true })); }       
}
