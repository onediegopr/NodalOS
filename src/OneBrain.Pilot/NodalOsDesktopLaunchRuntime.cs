using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Hosting;

namespace OneBrain.Pilot;

public static class NodalOsDesktopLaunchRuntime
{
    public const string DefaultLoopbackUrl = "http://127.0.0.1:5084";

    private const int ErrorSuccess = 0;
    private const int ErrorInsufficientBuffer = 122;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(
        ref uint packageFullNameLength,
        StringBuilder? packageFullName);

    [ModuleInitializer]
    internal static void InitializePackagedDesktop()
    {
        if (!IsPackaged())
            return;

        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        var explicitRoot = GetArgument(args, "--root");
        Environment.CurrentDirectory = ResolveProductRoot(explicitRoot);
        var urls = ResolveLoopbackUrls(GetArgument(args, "--urls"));
        if (ShouldOpenBrowser(args, packaged: true))
            StartBrowserProbe(urls);
    }

    public static bool IsPackaged()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        try
        {
            uint length = 0;
            var result = GetCurrentPackageFullName(ref length, null);
            return result is ErrorSuccess or ErrorInsufficientBuffer;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    public static string ResolveProductRoot(
        string? explicitRoot = null,
        string? localApplicationData = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitRoot))
            return EnsureDirectory(Path.GetFullPath(explicitRoot));

        localApplicationData ??= Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
            localApplicationData = Path.GetTempPath();

        return EnsureDirectory(Path.Combine(localApplicationData, "NodalOS", "ProductData"));
    }

    public static string ResolveLoopbackUrls(string? requestedUrls)
    {
        var value = string.IsNullOrWhiteSpace(requestedUrls)
            ? DefaultLoopbackUrl
            : requestedUrls.Trim();
        var urls = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (urls.Length is < 1 or > 4)
            throw new ArgumentException("One to four loopback URLs are required.", nameof(requestedUrls));

        foreach (var url in urls)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) ||
                !string.Equals(parsed.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrEmpty(parsed.UserInfo) ||
                !string.IsNullOrEmpty(parsed.Query) ||
                !string.IsNullOrEmpty(parsed.Fragment) ||
                (parsed.AbsolutePath.Length > 1 && parsed.AbsolutePath != "/"))
            {
                throw new ArgumentException("Desktop runtime URLs must be absolute loopback HTTP origins.", nameof(requestedUrls));
            }

            var loopback = string.Equals(parsed.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                IPAddress.TryParse(parsed.Host, out var address) && IPAddress.IsLoopback(address);
            if (!loopback)
                throw new ArgumentException("Desktop runtime cannot bind outside loopback.", nameof(requestedUrls));
        }

        return string.Join(';', urls);
    }

    public static bool ShouldOpenBrowser(IReadOnlyList<string> args, bool packaged)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Any(value => string.Equals(value, "--no-open-browser", StringComparison.OrdinalIgnoreCase)))
            return false;
        if (args.Any(value => string.Equals(value, "--open-browser", StringComparison.OrdinalIgnoreCase)))
            return true;
        return packaged;
    }

    public static void RegisterBrowserLaunch(
        IHostApplicationLifetime lifetime,
        string urls,
        Action<string>? opener = null)
    {
        ArgumentNullException.ThrowIfNull(lifetime);
        var target = FirstOrigin(urls);
        opener ??= OpenWithShell;

        lifetime.ApplicationStarted.Register(() =>
        {
            try
            {
                opener(target);
            }
            catch (InvalidOperationException)
            {
            }
            catch (System.ComponentModel.Win32Exception)
            {
            }
        });
    }

    private static void StartBrowserProbe(string urls)
    {
        var target = FirstOrigin(urls);
        _ = Task.Run(async () =>
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var health = new Uri(new Uri(target + "/", UriKind.Absolute), "api/mission-control");
            for (var attempt = 0; attempt < 120; attempt++)
            {
                try
                {
                    using var response = await client.GetAsync(health).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        OpenWithShell(target);
                        return;
                    }
                }
                catch (HttpRequestException)
                {
                }
                catch (TaskCanceledException)
                {
                }

                await Task.Delay(250).ConfigureAwait(false);
            }
        });
    }

    private static string FirstOrigin(string urls) => ResolveLoopbackUrls(urls)
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .First();

    private static string? GetArgument(IReadOnlyList<string> args, string name)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
                return args[index + 1];
        }
        return null;
    }

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }

    private static void OpenWithShell(string target)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = target,
            UseShellExecute = true
        });
    }
}
