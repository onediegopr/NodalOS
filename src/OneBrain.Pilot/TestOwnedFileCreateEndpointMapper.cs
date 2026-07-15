using System.Net;
using OneBrain.AgentOperations.Core.Runtime;

namespace OneBrain.Pilot;

public static class TestOwnedFileCreateEndpointMapper
{
    public const string JsonRoute = "/api/runtime/file-create-fixture";
    public const string HtmlRoute = "/runtime/file-create-fixture";

    public static IEndpointRouteBuilder MapTestOwnedFileCreateFixture(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(environment);

        endpoints.MapGet(JsonRoute, async (HttpContext context) =>
        {
            if (!SelectiveRuntimeInspectorEndpointMapper.IsRequestAllowed(
                    environment,
                    context.Connection.RemoteIpAddress))
            {
                return Results.NotFound(new { decision = "FILE_CREATE_FIXTURE_LOCAL_DEV_ONLY" });
            }

            ApplyReadOnlyHeaders(context.Response);
            var result = await new NodalOsTestOwnedFileCreateMissionScenario()
                .RunAsync(context.RequestAborted)
                .ConfigureAwait(false);
            return Results.Json(result.ToSnapshot());
        });

        endpoints.MapGet(HtmlRoute, async (HttpContext context) =>
        {
            if (!SelectiveRuntimeInspectorEndpointMapper.IsRequestAllowed(
                    environment,
                    context.Connection.RemoteIpAddress))
            {
                return Results.NotFound();
            }

            ApplyReadOnlyHeaders(context.Response);
            var result = await new NodalOsTestOwnedFileCreateMissionScenario()
                .RunAsync(context.RequestAborted)
                .ConfigureAwait(false);
            return Results.Content(Render(result), "text/html; charset=utf-8");
        });

        return endpoints;
    }

    internal static string Render(NodalOsTestOwnedFileCreateMissionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var snapshot = result.ToSnapshot();
        var timeline = string.Join(Environment.NewLine, result.Timeline.Select(item =>
            $"<li><span>{Html(item.CreatedAt.ToString("HH:mm:ss"))}</span><strong>{Html(item.Kind.ToString())}</strong><p>{Html(item.SummaryRedacted)}</p></li>"));
        var evidence = string.Join(Environment.NewLine, result.Timeline
            .SelectMany(item => item.EvidenceRefs)
            .DistinctBy(item => item.EvidenceId)
            .Select(item => $"<li><strong>{Html(item.Kind)}</strong><code>{Html(item.Hash ?? "no-hash")}</code></li>"));

        return $$$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <meta name="robots" content="noindex,nofollow">
              <title>NODAL OS Create-Only Fixture</title>
              <style>
                :root{color-scheme:dark;font-family:Inter,ui-sans-serif,system-ui,sans-serif;background:#0d1117;color:#f5f7fa}
                *{box-sizing:border-box}body{margin:0;background:#0d1117}.shell{min-height:100vh;display:grid;grid-template-columns:240px minmax(0,1fr) 300px;grid-template-rows:auto 1fr auto}.top{grid-column:1/-1;padding:18px 24px;border-bottom:1px solid #30363d;background:#161b22;display:flex;justify-content:space-between;gap:16px;align-items:center}.top h1{font-size:18px;margin:0}.badge,.chip{display:inline-flex;padding:6px 10px;border:1px solid #4f7cff;border-radius:999px;color:#b8c7ff}.chip{font-size:12px;padding:4px 8px}.side,.right{padding:20px;border-right:1px solid #30363d;background:#161b22}.right{border-right:0;border-left:1px solid #30363d}.main{padding:24px;min-width:0}.card{background:#1c2128;border:1px solid #30363d;border-radius:12px;padding:16px;margin-bottom:16px}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:12px}.metric{display:flex;justify-content:space-between;gap:16px;padding:8px 0;border-bottom:1px solid #30363d}.metric:last-child{border-bottom:0}.ok{color:#62d69f}.muted{color:#aab4c0}.timeline{list-style:none;padding:0}.timeline li{position:relative;padding:0 0 18px 24px;border-left:2px solid #30363d}.timeline li:before{content:"";position:absolute;left:-6px;top:4px;width:10px;height:10px;border-radius:50%;background:#4f7cff}.timeline span{color:#6e7681;font-size:12px}.timeline strong{display:block;margin-top:2px}.timeline p{margin:4px 0;color:#aab4c0}ul{padding-left:18px}code{display:block;overflow-wrap:anywhere;color:#aab4c0;margin-top:4px}.bottom{grid-column:1/-1;padding:12px 24px;border-top:1px solid #30363d;background:#161b22;color:#aab4c0;font-size:13px}@media(max-width:1000px){.grid{grid-template-columns:1fr}}@media(max-width:900px){.shell{grid-template-columns:1fr}.top,.bottom{grid-column:1}.side,.right{border:0;border-bottom:1px solid #30363d}.right{border-top:1px solid #30363d}}
              </style>
            </head>
            <body>
              <div class="shell" data-nodal-os="test-owned-file-create" data-local-dev-only="true" data-read-only-surface="true" data-product-authority="false">
                <header class="top">
                  <div><h1>{{{Html(result.Plan.Goal)}}}</h1><div class="muted">{{{Html(result.Workspace.DisplayNameRedacted)}}} · {{{Html(result.Mission.MissionId)}}}</div></div>
                  <span class="badge">{{{Html(snapshot.MissionStatus)}}}</span>
                </header>
                <aside class="side" data-section-id="mission">
                  <h2>Mission scope</h2>
                  <div class="metric"><span>Approval</span><strong>{{{Html(snapshot.ApprovalStatus)}}}</strong></div>
                  <div class="metric"><span>Per-step prompt</span><strong>none</strong></div>
                  <div class="metric"><span>Capability</span><strong>filesystem.write.safe</strong></div>
                  <div class="metric"><span>User workspace</span><strong>read-only</strong></div>
                </aside>
                <main class="main">
                  <section class="grid" aria-label="Create-only action state">
                    <article class="card" data-section-id="file-action"><span class="chip">create-only</span><h2>File action</h2><p class="ok">{{{Html(snapshot.FileCreateState)}}}</p><p class="muted">{{{Html(snapshot.RelativePath)}}} · {{{snapshot.BytesWritten}}} bytes</p></article>
                    <article class="card" data-section-id="verification"><span class="chip">SHA-256</span><h2>Verification</h2><p class="ok">{{{(snapshot.FileCreateVerified ? "verified" : "failed")}}}</p><code>{{{Html(snapshot.ContentSha256 ?? "no-hash")}}}</code></article>
                    <article class="card" data-section-id="cleanup"><span class="chip">test-owned root</span><h2>Cleanup</h2><p class="ok">{{{(snapshot.TestOwnedFixtureCleaned ? "removed" : "not removed")}}}</p><p class="muted">No user workspace path is exposed or touched.</p></article>
                  </section>
                  <section class="card" data-section-id="timeline"><h2>Timeline</h2><ol class="timeline">{{{timeline}}}</ol></section>
                  <section class="card" data-section-id="evidence"><h2>Evidence</h2><ul>{{{evidence}}}</ul></section>
                </main>
                <aside class="right" data-section-id="boundaries">
                  <h2>Boundaries</h2>
                  <div class="metric"><span>Registry</span><strong>{{{Html(snapshot.RegistryState)}}}</strong></div>
                  <div class="metric"><span>Overwrite</span><strong>rejected</strong></div>
                  <div class="metric"><span>Test-owned filesystem</span><strong>{{{(snapshot.TestOwnedFilesystemTouched ? "used" : "not used")}}}</strong></div>
                  <div class="metric"><span>User workspace filesystem</span><strong>{{{(snapshot.UserWorkspaceFilesystemTouched ? "used" : "not touched")}}}</strong></div>
                  <div class="metric"><span>Network</span><strong>{{{(snapshot.NetworkUsed ? "used" : "not used")}}}</strong></div>
                  <div class="metric"><span>Secrets</span><strong>excluded</strong></div>
                  <div class="metric"><span>Product authority</span><strong>not granted</strong></div>
                </aside>
                <footer class="bottom">Local/dev fixture only. Atomic create, no overwrite, SHA-256 verification, test-root cleanup, no raw paths, no scripts, forms, network, shell or product authority.</footer>
              </div>
            </body>
            </html>
            """;
    }

    private static void ApplyReadOnlyHeaders(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
        response.Headers.XContentTypeOptions = "nosniff";
    }

    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
