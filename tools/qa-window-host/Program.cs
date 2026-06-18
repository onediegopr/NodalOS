using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Forms;

var options = ParseArgs(args);
var title = Get(options, "title", "NODAL OS OCR QA Window");
var text = Get(options, "text", "PVC WALL");
var readyFile = Get(options, "ready-file", Path.Combine(Path.GetTempPath(), $"nodal-qa-window-{Guid.NewGuid():N}.json"));
var captureFile = Get(options, "capture-file", Path.Combine(Path.GetTempPath(), $"nodal-qa-window-{Guid.NewGuid():N}.rgba"));
var width = GetInt(options, "width", 800);
var height = GetInt(options, "height", 320);
var regionX = GetInt(options, "region-x", 80);
var regionY = GetInt(options, "region-y", 64);
var regionWidth = GetInt(options, "region-width", 640);
var regionHeight = GetInt(options, "region-height", 160);
var durationMs = GetInt(options, "duration-ms", 30000);
var fontFamily = Get(options, "font-family", "Segoe UI");
var fontSize = GetFloat(options, "font-size", 76f);
var fontStyle = ParseFontStyle(Get(options, "font-style", "Bold"));
var textRenderingHint = ParseTextRenderingHint(Get(options, "text-rendering-hint", "ClearTypeGridFit"));
var baselineShiftY = GetInt(options, "baseline-shift-y", 0);
var smoothingMode = ParseSmoothingMode(Get(options, "smoothing-mode", "HighQuality"));
var interpolationMode = ParseInterpolationMode(Get(options, "interpolation-mode", "HighQualityBicubic"));
var fixtureId = Get(options, "fixture-id", text.Replace(' ', '-'));
var markerText = Get(options, "marker-text", "NODAL-QA-OCR");
var captureCoordinateMode = "window-client-bitmap-region";

Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);

var renderConfig = new QaWindowRenderConfig(fontFamily, fontSize, fontStyle, textRenderingHint, baselineShiftY, smoothingMode, interpolationMode);
using var form = new QaWindowForm(title, text, width, height, new Rectangle(regionX, regionY, regionWidth, regionHeight), renderConfig, fixtureId, markerText);
using var shutdown = new System.Windows.Forms.Timer { Interval = Math.Max(1000, durationMs) };
shutdown.Tick += (_, _) => form.Close();
shutdown.Start();

form.Shown += (_, _) =>
{
    _ = Task.Run(async () =>
    {
        await Task.Delay(750).ConfigureAwait(false);
        form.BeginInvoke(() =>
        {
            var status = "captured";
            var reason = "real QA window region captured";
            byte[]? capture = null;

            try
            {
                EnsureForegroundCaptureWindow(form);
                form.Invalidate();
                form.Update();
                Application.DoEvents();
                Thread.Sleep(150);
                capture = CaptureRegion(form, regionX, regionY, regionWidth, regionHeight);
                Directory.CreateDirectory(Path.GetDirectoryName(captureFile)!);
                File.WriteAllBytes(captureFile, capture);
            }
            catch (Exception ex)
            {
                status = "capture-failed";
                reason = ex.Message;
            }

            var clientOrigin = form.PointToScreen(Point.Empty);
            var regionOrigin = form.PointToScreen(new Point(regionX, regionY));
            var clientBounds = new Rectangle(clientOrigin.X, clientOrigin.Y, form.ClientSize.Width, form.ClientSize.Height);
            var windowBounds = form.Bounds;
            var regionBounds = new Rectangle(regionX, regionY, regionWidth, regionHeight);
            var captureFingerprint = capture is null ? null : BuildFingerprint(capture, regionWidth, regionHeight);
            var payload = new
            {
                status,
                reason,
                processId = Environment.ProcessId,
                processName = Path.GetFileNameWithoutExtension(Environment.ProcessPath) ?? "OneBrain.Tools.QaWindowHost",
                windowTitle = form.Text,
                windowHandle = form.Handle.ToInt64().ToString("X"),
                windowBounds = ToPayload(windowBounds),
                clientBounds = ToPayload(clientBounds),
                regionBounds = ToPayload(regionBounds),
                regionScreenBounds = ToPayload(new Rectangle(regionOrigin.X, regionOrigin.Y, regionWidth, regionHeight)),
                visible = form.Visible,
                windowForegroundOrActivated = NativeMethods.GetForegroundWindow() == form.Handle,
                livenessConfirmed = form.Visible && !form.IsDisposed && regionWidth > 0 && regionHeight > 0,
                captureFile,
                width = regionWidth,
                height = regionHeight,
                expectedText = text,
                fixtureId,
                markerText,
                markerVisible = true,
                deviceDpi = form.DeviceDpi,
                dpiScaleX = Math.Round(form.DeviceDpi / 96d, 4),
                dpiScaleY = Math.Round(form.DeviceDpi / 96d, 4),
                capturedRegionWidth = regionWidth,
                capturedRegionHeight = regionHeight,
                captureCoordinateMode,
                captureTechnique = "window-client-bitmap-region",
                textRendererMode = "GdiPlus.DrawString",
                fontFamily = form.EffectiveFontFamily,
                fontSize = form.RenderConfig.FontSize,
                fontStyle = form.RenderConfig.FontStyle.ToString(),
                antiAliasingMode = form.RenderConfig.TextRenderingHint.ToString(),
                smoothingMode = form.RenderConfig.SmoothingMode.ToString(),
                interpolationMode = form.RenderConfig.InterpolationMode.ToString(),
                baselineShiftY = form.RenderConfig.BaselineShiftY,
                captureFingerprint
            };

            Directory.CreateDirectory(Path.GetDirectoryName(readyFile)!);
            File.WriteAllText(readyFile, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        });
    });
};

Application.Run(form);
return 0;

static Dictionary<string, string> ParseArgs(string[] argv)
{
    var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < argv.Length; i++)
    {
        if (!argv[i].StartsWith("--", StringComparison.Ordinal)) continue;
        var key = argv[i][2..];
        if (i + 1 < argv.Length && !argv[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            map[key] = argv[i + 1];
            i++;
        }
        else
        {
            map[key] = "true";
        }
    }

    return map;
}

static string Get(IReadOnlyDictionary<string, string> options, string key, string fallback) =>
    options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

static int GetInt(IReadOnlyDictionary<string, string> options, string key, int fallback) =>
    options.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : fallback;

static float GetFloat(IReadOnlyDictionary<string, string> options, string key, float fallback) =>
    options.TryGetValue(key, out var value) && float.TryParse(value, out var parsed) ? parsed : fallback;

static FontStyle ParseFontStyle(string value)
{
    return Enum.TryParse<FontStyle>(value, ignoreCase: true, out var parsed)
        ? parsed
        : FontStyle.Bold;
}

static TextRenderingHint ParseTextRenderingHint(string value)
{
    return Enum.TryParse<TextRenderingHint>(value, ignoreCase: true, out var parsed)
        ? parsed
        : TextRenderingHint.ClearTypeGridFit;
}

static SmoothingMode ParseSmoothingMode(string value)
{
    return Enum.TryParse<SmoothingMode>(value, ignoreCase: true, out var parsed)
        ? parsed
        : SmoothingMode.HighQuality;
}

static InterpolationMode ParseInterpolationMode(string value)
{
    return Enum.TryParse<InterpolationMode>(value, ignoreCase: true, out var parsed)
        ? parsed
        : InterpolationMode.HighQualityBicubic;
}

static object ToPayload(Rectangle bounds) => new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height };

static void EnsureForegroundCaptureWindow(Form form)
{
    const int swRestore = 9;
    const uint swpShowWindow = 0x0040;
    var hwndTopmost = new IntPtr(-1);

    NativeMethods.ShowWindow(form.Handle, swRestore);
    NativeMethods.SetWindowPos(form.Handle, hwndTopmost, form.Left, form.Top, form.Width, form.Height, swpShowWindow);
    form.TopMost = true;
    form.BringToFront();
    form.Activate();
    form.Focus();
    NativeMethods.SetForegroundWindow(form.Handle);
}

static byte[] CaptureRegion(Form form, int x, int y, int width, int height)
{
    if (width <= 0 || height <= 0)
        throw new InvalidOperationException("invalid capture region dimensions");

    if (x < 0 || y < 0 || x + width > form.ClientSize.Width || y + height > form.ClientSize.Height)
        throw new InvalidOperationException("capture region is outside client bounds");

    using var clientBitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height, PixelFormat.Format32bppArgb);
    form.DrawToBitmap(clientBitmap, new Rectangle(Point.Empty, form.ClientSize));
    using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
    using (var graphics = Graphics.FromImage(bitmap))
    {
        graphics.DrawImage(
            clientBitmap,
            new Rectangle(0, 0, width, height),
            new Rectangle(x, y, width, height),
            GraphicsUnit.Pixel);
    }

    var rgba = new byte[width * height * 4];
    for (var py = 0; py < height; py++)
    {
        for (var px = 0; px < width; px++)
        {
            var color = bitmap.GetPixel(px, py);
            var index = (py * width + px) * 4;
            rgba[index + 0] = color.R;
            rgba[index + 1] = color.G;
            rgba[index + 2] = color.B;
            rgba[index + 3] = color.A;
        }
    }

    return rgba;
}

static object BuildFingerprint(byte[] rgba, int width, int height)
{
    using var sha = SHA256.Create();
    var hash = Convert.ToHexString(sha.ComputeHash(rgba));
    long red = 0;
    long green = 0;
    long blue = 0;
    var darkPixels = 0;
    var pixels = Math.Max(1, width * height);
    var stepX = Math.Max(1, width / 5);
    var stepY = Math.Max(1, height / 4);
    var samples = new List<string>();

    for (var i = 0; i < rgba.Length; i += 4)
    {
        red += rgba[i];
        green += rgba[i + 1];
        blue += rgba[i + 2];
        if ((rgba[i] + rgba[i + 1] + rgba[i + 2]) / 3d < 128d)
            darkPixels++;
    }

    for (var y = 0; y < height; y += stepY)
    {
        for (var x = 0; x < width; x += stepX)
        {
            var index = (y * width + x) * 4;
            samples.Add($"{rgba[index]:X2}{rgba[index + 1]:X2}{rgba[index + 2]:X2}");
        }
    }

    return new
    {
        Sha256 = hash,
        Width = width,
        Height = height,
        AverageRed = Math.Round(red / (double)pixels, 4),
        AverageGreen = Math.Round(green / (double)pixels, 4),
        AverageBlue = Math.Round(blue / (double)pixels, 4),
        DarkPixelRatio = Math.Round(darkPixels / (double)pixels, 6),
        SampleSignature = string.Join("-", samples)
    };
}

internal sealed record QaWindowRenderConfig(
    string RequestedFontFamily,
    float FontSize,
    FontStyle FontStyle,
    TextRenderingHint TextRenderingHint,
    int BaselineShiftY,
    SmoothingMode SmoothingMode,
    InterpolationMode InterpolationMode);

internal sealed class QaWindowForm : Form
{
    private readonly string _text;
    private readonly Rectangle _region;
    private readonly string _fixtureId;
    private readonly string _markerText;

    public QaWindowForm(string title, string text, int width, int height, Rectangle region, QaWindowRenderConfig renderConfig, string fixtureId, string markerText)
    {
        Text = title;
        _text = text;
        _region = region;
        _fixtureId = fixtureId;
        _markerText = markerText;
        RenderConfig = renderConfig;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(120, 120);
        ClientSize = new Size(width, height);
        BackColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        ShowInTaskbar = true;
        DoubleBuffered = true;
    }

    public QaWindowRenderConfig RenderConfig { get; }

    public string EffectiveFontFamily { get; private set; } = string.Empty;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.Clear(Color.White);
        e.Graphics.SmoothingMode = RenderConfig.SmoothingMode;
        e.Graphics.InterpolationMode = RenderConfig.InterpolationMode;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        e.Graphics.TextRenderingHint = RenderConfig.TextRenderingHint;

        using var markerBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
        using var markerFont = new Font("Arial", 18f, FontStyle.Bold, GraphicsUnit.Pixel);
        e.Graphics.DrawString(_markerText, markerFont, markerBrush, new PointF(18, 16));
        using var fixtureBrush = new SolidBrush(Color.FromArgb(70, 70, 70));
        using var fixtureFont = new Font("Arial", 14f, FontStyle.Regular, GraphicsUnit.Pixel);
        e.Graphics.DrawString(_fixtureId, fixtureFont, fixtureBrush, new PointF(18, 44));

        using var borderPen = new Pen(Color.FromArgb(220, 220, 220), 2);
        e.Graphics.DrawRectangle(borderPen, _region);
        using var font = new Font(RenderConfig.RequestedFontFamily, RenderConfig.FontSize, RenderConfig.FontStyle, GraphicsUnit.Pixel);
        EffectiveFontFamily = font.Name;
        using var brush = new SolidBrush(Color.Black);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.None,
            FormatFlags = StringFormatFlags.NoWrap
        };

        var drawRegion = new Rectangle(_region.X, _region.Y + RenderConfig.BaselineShiftY, _region.Width, _region.Height);
        e.Graphics.DrawString(_text, font, brush, drawRegion, format);
    }
}

internal static class NativeMethods
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
}
