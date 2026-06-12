using OneBrain.Pilot;

var root = GetArg(args, "--root") ?? Directory.GetCurrentDirectory();
var dotnet = GetArg(args, "--dotnet")
    ?? Environment.GetEnvironmentVariable("ONEBRAIN_DOTNET")
    ?? PilotRecipeExecutor.DefaultDotnetPath;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(GetArg(args, "--urls") ?? "http://127.0.0.1:5084");

var app = builder.Build();
var router = new PilotIntentRouter();
var planner = new PilotPlanBuilder();
var executor = new PilotRecipeExecutor(root, dotnet);

app.MapGet("/", () => Results.Content(PilotHomePageRenderer.Render(), "text/html"));

app.MapPost("/plan", async (HttpContext context) =>
{
    var task = await ReadTaskAsync(context);
    var plan = planner.Build(router.Route(task));
    return Results.Content(PilotHomePageRenderer.Render(plan), "text/html");
});

app.MapPost("/run", async (HttpContext context) =>
{
    var task = await ReadTaskAsync(context);
    var plan = planner.Build(router.Route(task));
    var result = await executor.ExecuteAsync(plan, context.RequestAborted);
    return Results.Content(PilotHomePageRenderer.Render(plan, result), "text/html");
});

app.MapGet("/api/intent", (string? task) =>
{
    var plan = planner.Build(router.Route(task));
    return Results.Json(plan);
});

app.MapGet("/api/safety", () => Results.Json(PilotSafetySummary.ZeroReadOnly));

app.Run();

static async Task<string?> ReadTaskAsync(HttpContext context)
{
    if (!context.Request.HasFormContentType)
        return context.Request.Query["task"].FirstOrDefault();

    var form = await context.Request.ReadFormAsync();
    return form["task"].FirstOrDefault();
}

static string? GetArg(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    }

    return null;
}
