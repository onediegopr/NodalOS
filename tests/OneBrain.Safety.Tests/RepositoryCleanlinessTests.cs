using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("RepositoryCleanliness")]
public sealed class RepositoryCleanlinessTests
{
    [TestMethod]
    public void NoUntrackedCsFilesUnderSrcOrTestsBeforeCleanClosure()
    {
        var git = FindGit();
        if (git is null)
        {
            Assert.Inconclusive("Git is not available in this environment; skipping untracked-source guard.");
            return;
        }

        var repoRoot = Execute(git, "rev-parse --show-toplevel", Environment.CurrentDirectory);
        if (string.IsNullOrWhiteSpace(repoRoot))
        {
            Assert.Inconclusive("Could not determine repository root; skipping untracked-source guard.");
            return;
        }

        var status = Execute(git, "status --porcelain", repoRoot);
        var untracked = status
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.StartsWith("?? ", StringComparison.Ordinal))
            .Select(line => line[3..].Trim().Replace('\\', '/'))
            .Where(path =>
                path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) &&
                (path.StartsWith("src/", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("tests/", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (untracked.Count > 0)
        {
            Assert.Fail($"Untracked .cs files remain under src/ or tests/: {string.Join(", ", untracked)}. Commit or isolate them before declaring clean closure.");
        }
    }

    private static string? FindGit()
    {
        foreach (var candidate in new[] { "git.exe", "git" })
        {
            try
            {
                using var process = new Process();
                process.StartInfo.FileName = candidate;
                process.StartInfo.Arguments = "--version";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                if (process.Start())
                {
                    process.WaitForExit(5000);
                    if (process.ExitCode == 0)
                        return candidate;
                }
            }
            catch
            {
                // Ignore and try next candidate.
            }
        }

        return null;
    }

    private static string Execute(string fileName, string arguments, string workingDirectory)
    {
        using var process = new Process();
        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.WorkingDirectory = workingDirectory;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(15000);
        return output.Trim();
    }
}
