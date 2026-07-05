using System.Diagnostics;
using System.Text;

namespace OneBrain.Pilot;

public sealed class PilotRecipeExecutor
{
    public const string DefaultDotnetPath =
        @"C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Herramientas\dotnet-sdk-11.0.100-preview.5.26302.115-win-x64\dotnet.exe";

    private readonly string _root;
    private readonly string _dotnetPath;

    public PilotRecipeExecutor(string root, string? dotnetPath = null)
    {
        _root = Path.GetFullPath(root);
        _dotnetPath = string.IsNullOrWhiteSpace(dotnetPath) ? DefaultDotnetPath : dotnetPath;
    }

    public async Task<PilotExecutionResult> ExecuteAsync(PilotPlan plan, CancellationToken cancellationToken = default)
    {
        if (!plan.HasExecutableRecipe)
        {
            return new PilotExecutionResult(
                Plan: plan,
                Executed: false,
                Success: false,
                ExitCode: null,
                Status: plan.Intent.Reason,
                RecipePath: null,
                LatestMarkdownPath: PilotArtifactLocator.FindLatestMarkdown(_root),
                LatestHtmlPath: PilotArtifactLocator.FindLatestHtml(_root),
                ArtifactsFolder: Path.Combine(_root, "artifacts"),
                StandardOutput: "",
                StandardError: "",
                Safety: plan.Safety);
        }

        var recipe = plan.Intent.Recipe!;
        if (!PilotRecipeCatalog.IsAllowlistedPath(recipe.RecipePath))
            throw new InvalidOperationException($"Recipe is not allowlisted: {recipe.RecipePath}");

        var recipeFullPath = Path.GetFullPath(Path.Combine(_root, recipe.RecipePath));
        var rootFull = Path.GetFullPath(_root);
        if (!recipeFullPath.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Recipe path resolved outside repository root.");
        if (!File.Exists(recipeFullPath))
            throw new FileNotFoundException("Allowlisted recipe file was not found.", recipeFullPath);

        var startInfo = CreateRecipeRunStartInfo(_root, _dotnetPath, recipe.RecipePath);
        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, args) => { if (args.Data != null) stdout.AppendLine(args.Data); };
        process.ErrorDataReceived += (_, args) => { if (args.Data != null) stderr.AppendLine(args.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(cancellationToken);

        var success = process.ExitCode == 0;
        return new PilotExecutionResult(
            Plan: plan,
            Executed: true,
            Success: success,
            ExitCode: process.ExitCode,
            Status: success ? "OK" : "failed",
            RecipePath: recipe.RecipePath,
            LatestMarkdownPath: PilotArtifactLocator.FindLatestMarkdown(_root),
            LatestHtmlPath: PilotArtifactLocator.FindLatestHtml(_root),
            ArtifactsFolder: Path.Combine(_root, "artifacts"),
            StandardOutput: stdout.ToString(),
            StandardError: stderr.ToString(),
            Safety: plan.Safety);
    }

    public PilotExecutionResult BlockedByDefault(PilotPlan plan)
    {
        var gate = PilotRecipeExecutionGate.Evaluate();
        return new PilotExecutionResult(
            Plan: plan,
            Executed: false,
            Success: false,
            ExitCode: null,
            Status: gate.Status,
            RecipePath: plan.Intent.Recipe?.RecipePath,
            LatestMarkdownPath: PilotArtifactLocator.FindLatestMarkdown(_root),
            LatestHtmlPath: PilotArtifactLocator.FindLatestHtml(_root),
            ArtifactsFolder: Path.Combine(_root, "artifacts"),
            StandardOutput: "",
            StandardError: "",
            Safety: gate.Safety);
    }

    public static ProcessStartInfo CreateRecipeRunStartInfo(string root, string dotnetPath, string recipePath)
    {
        if (!PilotRecipeCatalog.IsAllowlistedPath(recipePath))
            throw new InvalidOperationException($"Recipe is not allowlisted: {recipePath}");

        var startInfo = new ProcessStartInfo(dotnetPath)
        {
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(Path.Combine("src", "OneBrain.Cli"));
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("recipe");
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add(recipePath);

        return startInfo;
    }
}
