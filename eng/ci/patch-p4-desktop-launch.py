#!/usr/bin/env python3
"""Temporary P4 helper. The one-shot workflow removes it after applying the patch."""

from pathlib import Path


def replace(path: Path, old: str, new: str, count: int = 1) -> None:
    text = path.read_text(encoding="utf-8-sig")
    actual = text.count(old)
    if actual != count:
        raise SystemExit(
            f"{path}: expected {count} occurrence(s), found {actual}: {old[:120]!r}"
        )
    path.write_text(text.replace(old, new, count), encoding="utf-8")


program = Path("src/OneBrain.Pilot/Program.cs")
replace(
    program,
    'var root = ResolveRepoRoot(GetArg(args, "--root") ?? Directory.GetCurrentDirectory());',
    '''var packaged = NodalOsDesktopLaunchRuntime.IsPackaged();
var explicitRoot = GetArg(args, "--root");
var root = packaged
    ? NodalOsDesktopLaunchRuntime.ResolveProductRoot(explicitRoot)
    : ResolveRepoRoot(explicitRoot ?? Directory.GetCurrentDirectory());''',
)
replace(
    program,
    '''var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(GetArg(args, "--urls") ?? "http://127.0.0.1:5084");''',
    '''var urls = NodalOsDesktopLaunchRuntime.ResolveLoopbackUrls(GetArg(args, "--urls"));
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(urls);''',
)
replace(
    program,
    "app.Run();",
    '''if (NodalOsDesktopLaunchRuntime.ShouldOpenBrowser(args, packaged))
    NodalOsDesktopLaunchRuntime.RegisterBrowserLaunch(app.Lifetime, urls);

app.Run();''',
)

project = Path("src/OneBrain.Pilot/OneBrain.Pilot.csproj")
replace(
    project,
    '''    <Nullable>enable</Nullable>
  </PropertyGroup>''',
    '''    <Nullable>enable</Nullable>
    <Product>NODAL OS</Product>
    <Company>NODAL OS</Company>
    <Authors>NODAL OS</Authors>
    <Description>Local-first AI Mission Control for controlled work on real projects.</Description>
    <VersionPrefix>0.1.0</VersionPrefix>
  </PropertyGroup>''',
)

workflow = Path(".github/workflows/selective-runtime-integration.yml")
replace(
    workflow,
    "|FullyQualifiedName~ByokModelConfiguration\"",
    "|FullyQualifiedName~ByokModelConfiguration|FullyQualifiedName~NodalOsDesktopLaunchRuntimeTests\"",
)
