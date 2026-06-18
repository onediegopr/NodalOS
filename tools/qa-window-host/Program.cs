using System.Drawing;
using System.Drawing.Imaging;
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

Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);

using var form = new QaWindowForm(title, text, width, height, new Rectangle(regionX, regionY, regionWidth, regionHeight));
using var shutdown = new System.Windows.Forms.Timer { Interval = Math.Max(1000, durationMs) };
shutdown.Tick += (_, _) => form.Close();
shutdown.Start();

form.Shown += (_, _) =>
{
    _ = Task.Run(async () =>
    {
        await Task.Delay(500).ConfigureAwait(false);
        form.BeginInvoke(() =>
        {
            var status = "captured";
            var reason = "real QA window region captured";
            try
            {
                var capture = CaptureRegion(form, regionX, regionY, regionWidth, regionHeight);
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
            var payload = new
            {
                status,
                reason,
                processId = Environment.ProcessId,
                processName = Path.GetFileNameWithoutExtension(Environment.ProcessPath) ?? "OneBrain.Tools.QaWindowHost",
                windowTitle = form.Text,
                windowHandle = form.Handle.ToInt64().ToString("X"),
                windowBounds = new { x = clientOrigin.X, y = clientOrigin.Y, width = form.ClientSize.Width, height = form.ClientSize.Height },
                regionBounds = new { x = regionX, y = regionY, width = regionWidth, height = regionHeight },
                regionScreenBounds = new { x = regionOrigin.X, y = regionOrigin.Y, width = regionWidth, height = regionHeight },
                visible = form.Visible,
                livenessConfirmed = form.Visible && !form.IsDisposed && regionWidth > 0 && regionHeight > 0,
                captureFile,
                width = regionWidth,
                height = regionHeight,
                expectedText = text
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

static byte[] CaptureRegion(Form form, int x, int y, int width, int height)
{
    if (width <= 0 || height <= 0)
        throw new InvalidOperationException("invalid capture region dimensions");

    var screenPoint = form.PointToScreen(new Point(x, y));
    using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
    using (var graphics = Graphics.FromImage(bitmap))
    {
        graphics.CopyFromScreen(screenPoint, Point.Empty, new Size(width, height), CopyPixelOperation.SourceCopy);
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

internal sealed class QaWindowForm : Form
{
    private readonly string _text;
    private readonly Rectangle _region;

    public QaWindowForm(string title, string text, int width, int height, Rectangle region)
    {
        Text = title;
        _text = text;
        _region = region;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(120, 120);
        ClientSize = new Size(width, height);
        BackColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        ShowInTaskbar = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.Clear(Color.White);
        using var borderPen = new Pen(Color.FromArgb(220, 220, 220), 2);
        e.Graphics.DrawRectangle(borderPen, _region);
        using var font = new Font("Segoe UI", 76, FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(Color.Black);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.None
        };
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        e.Graphics.DrawString(_text, font, brush, _region, format);
    }
}
