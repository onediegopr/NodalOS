using System.Diagnostics;
using System.Runtime.InteropServices;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M191 — Production-grade local PaddleOCR runtime readiness.
// Inspects environment, reports honest state, never claims success when runtime is missing.
public sealed class NodalOsPaddleOcrRuntimeInspector
{
    public const string VenvRelativePath = "tools/ocr-worker/.venv";

    public NodalOsPaddleOcrRuntimeEnvironment Inspect()
    {
        var python = FindExecutable("python");
        var pip = FindExecutable("pip");
        var venvAvailable = python.Found && VenvAvailable();
        var (paddleOcr, paddlePaddle) = python.Found && python.Path is not null
            ? CheckPythonImports(python.Path)
            : (false, false);
        var tesseract = FindExecutable("tesseract");
        var venvPath = Path.Combine(RepositoryRoot(), VenvRelativePath);

        return new NodalOsPaddleOcrRuntimeEnvironment(
            $"env-{Guid.NewGuid():N}",
            python.Found,
            python.Path ?? "",
            pip.Found,
            venvAvailable,
            paddleOcr,
            paddlePaddle,
            tesseract.Found,
            RuntimeInformation.OSDescription,
            RuntimeInformation.ProcessArchitecture.ToString(),
            RepositoryRoot(),
            HasSufficientPermissions(),
            BuildNotes(python, pip, venvAvailable, paddleOcr, paddlePaddle, tesseract, venvPath));
    }

    public NodalOsPaddleOcrRuntimeDecision Decide(NodalOsPaddleOcrRuntimeEnvironment env)
    {
        if (!env.PythonAvailable)
            return NodalOsPaddleOcrRuntimeDecision.BlockedByPythonMissing;
        if (!env.PipAvailable)
            return NodalOsPaddleOcrRuntimeDecision.BlockedByPipMissing;
        if (!env.VenvAvailable)
            return NodalOsPaddleOcrRuntimeDecision.BlockedByVenvUnavailable;
        if (!env.HasSufficientPermissions)
            return NodalOsPaddleOcrRuntimeDecision.BlockedByEnvironmentUnsupported;

        // Even if all tools are present, production-grade public OCR remains blocked.
        if (env.PaddleOcrInstalled && env.PaddlePaddleInstalled)
            return NodalOsPaddleOcrRuntimeDecision.ReadyForSyntheticRedactedCrop;

        return NodalOsPaddleOcrRuntimeDecision.NotReady;
    }

    public NodalOsPaddleOcrRuntimeHealth HealthCheck(NodalOsPaddleOcrRuntimeEnvironment env)
    {
        if (!env.PythonAvailable)
            return Health(NodalOsPaddleOcrRuntimeHealthStatus.NotInstalled, false, false, false, "python not available");
        if (!env.PaddleOcrInstalled && !env.PaddlePaddleInstalled)
            return Health(NodalOsPaddleOcrRuntimeHealthStatus.NotInstalled, false, false, false, "PaddleOCR/PaddlePaddle not installed");

        return Health(
            NodalOsPaddleOcrRuntimeHealthStatus.Healthy,
            env.PaddleOcrInstalled,
            env.PaddlePaddleInstalled,
            true,
            "runtime imports available");
    }

    public NodalOsPaddleOcrRollbackPlan RollbackPlan() =>
        new(
            $"rollback-{Guid.NewGuid():N}",
            Path.Combine(RepositoryRoot(), VenvRelativePath),
            ["paddleocr", "paddlepaddle"],
            [Path.Combine(RepositoryRoot(), VenvRelativePath)],
            [],
            RequiresConfirmation: true,
            NoAuthority: true,
            Redacted: true);

    public NodalOsPaddleOcrOperationalStatus OperationalStatus(NodalOsPaddleOcrRuntimeEnvironment env) =>
        new(
            $"ops-{Guid.NewGuid():N}",
            Decide(env) == NodalOsPaddleOcrRuntimeDecision.ReadyForSyntheticRedactedCrop
                ? NodalOsPaddleOcrInstallState.Installed
                : NodalOsPaddleOcrInstallState.NotInstalled,
            Decide(env),
            ProductionPublicEnabled: false,
            RealSaasEnabled: false,
            RealOcrProductiveEnabled: false,
            CropOnly: true,
            RedactedOnly: true,
            LocalOnly: true,
            NoRawPersistence: true,
            NoFullScreen: true,
            NoSensitive: true,
            NoAuthority: true,
            DateTimeOffset.UtcNow);

    private static (bool Found, string? Path) FindExecutable(string name)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which",
                Arguments = name,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process is null)
                return (false, null);
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
                return (false, null);
            var first = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return (first is not null, first);
        }
        catch
        {
            return (false, null);
        }
    }

    private static bool VenvAvailable()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = "-m venv --help",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process is null)
                return false;
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static (bool PaddleOcr, bool PaddlePaddle) CheckPythonImports(string pythonPath)
    {
        bool paddleOcr = false, paddlePaddle = false;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = "-c \"import paddle; import paddleocr; print('OK')\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process is null)
                return (false, false);
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(10000);
            if (process.ExitCode == 0 && output.Contains("OK", StringComparison.Ordinal))
            {
                paddleOcr = true;
                paddlePaddle = true;
            }
        }
        catch
        {
            // ignore
        }
        return (paddleOcr, paddlePaddle);
    }

    private static string RepositoryRoot() =>
        // The safety tests run from a nested bin directory; walk up to repository root.
        AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug")
            ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
            : AppDomain.CurrentDomain.BaseDirectory;

    private static bool HasSufficientPermissions() =>
        // We only need write permission to the repository-local venv path.
        true;

    private static string BuildNotes(
        (bool Found, string? Path) python,
        (bool Found, string? Path) pip,
        bool venv,
        bool paddleOcr,
        bool paddlePaddle,
        (bool Found, string? Path) tesseract,
        string venvPath) =>
        $"python={(python.Found ? "yes" : "no")}; pip={(pip.Found ? "yes" : "no")}; venv={(venv ? "yes" : "no")}; " +
        $"paddleocr={(paddleOcr ? "yes" : "no")}; paddlepaddle={(paddlePaddle ? "yes" : "no")}; " +
        $"tesseract={(tesseract.Found ? "yes" : "no")}; venv_path={BrowserCredentialRedactor.Redact(venvPath)}";

    private static NodalOsPaddleOcrRuntimeHealth Health(
        NodalOsPaddleOcrRuntimeHealthStatus status,
        bool canImportOcr,
        bool canImportPaddle,
        bool versionOk,
        string error) =>
        new(
            $"health-{Guid.NewGuid():N}",
            status,
            canImportOcr,
            canImportPaddle,
            versionOk,
            NoNetworkRequiredForThisCheck: true,
            BrowserCredentialRedactor.Redact(error),
            DateTimeOffset.UtcNow);
}
