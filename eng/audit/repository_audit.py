#!/usr/bin/env python3
"""Deterministic, dependency-free repository inventory for NODAL OS.

The script reports facts only. It does not mutate the repository, execute product
code, inspect secrets, or contact external services.
"""

from __future__ import annotations

import collections
import hashlib
import json
import os
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path, PurePosixPath
from typing import Any, Iterable

ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = ROOT / "artifacts" / "repository-audit"
TEXT_EXTENSIONS = {
    ".cs", ".csproj", ".props", ".targets", ".slnx", ".md", ".txt",
    ".json", ".jsonl", ".yml", ".yaml", ".xml", ".html", ".css",
    ".js", ".ts", ".tsx", ".jsx", ".ps1", ".py", ".toml", ".lock",
    ".sh", ".cmd", ".bat", ".editorconfig", ".gitignore", ".gitattributes",
}
GENERATED_PARTS = {"bin", "obj", "node_modules", "target", ".vs", ".idea"}
BINARY_SUFFIXES = {".dll", ".exe", ".pdb", ".zip", ".7z", ".tar", ".gz", ".png", ".jpg", ".jpeg", ".webp", ".pdf"}
SECRET_PATTERNS = [
    re.compile(r"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----"),
    re.compile(r"AKIA[0-9A-Z]{16}"),
    re.compile(r"gh[pousr]_[A-Za-z0-9]{30,}"),
    re.compile(r"sk-[A-Za-z0-9_-]{20,}"),
    re.compile(r"(?i)(?:api[_-]?key|secret|password|token)\s*[:=]\s*[\"'][^\"']{12,}[\"']"),
]


def run(*args: str) -> str:
    return subprocess.check_output(args, cwd=ROOT, text=True, errors="replace").strip()


def tracked_files() -> list[Path]:
    raw = subprocess.check_output(["git", "ls-files", "-z"], cwd=ROOT)
    return [ROOT / item.decode("utf-8", errors="replace") for item in raw.split(b"\0") if item]


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def is_text(path: Path) -> bool:
    if path.name in {"Dockerfile", "Makefile", "LICENSE", "NOTICE"}:
        return True
    return path.suffix.lower() in TEXT_EXTENSIONS


def read_text(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8-sig", errors="replace")
    except OSError:
        return ""


def line_count(text: str) -> int:
    return len(text.splitlines())


def top_level(path: Path) -> str:
    parts = PurePosixPath(rel(path)).parts
    return parts[0] if parts else "."


def xml_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def parse_project(path: Path) -> dict[str, Any]:
    result: dict[str, Any] = {
        "path": rel(path),
        "target_frameworks": [],
        "project_references": [],
        "packages": [],
        "parse_error": None,
    }
    try:
        root = ET.fromstring(read_text(path))
        for node in root.iter():
            name = xml_name(node.tag)
            if name in {"TargetFramework", "TargetFrameworks"} and node.text:
                result["target_frameworks"].extend(v.strip() for v in node.text.split(";") if v.strip())
            elif name == "ProjectReference" and node.attrib.get("Include"):
                target = (path.parent / node.attrib["Include"]).resolve()
                try:
                    result["project_references"].append(rel(target))
                except ValueError:
                    result["project_references"].append(node.attrib["Include"])
            elif name == "PackageReference" and node.attrib.get("Include"):
                version = node.attrib.get("Version")
                if not version:
                    version_node = next((c for c in node if xml_name(c.tag) == "Version"), None)
                    version = version_node.text.strip() if version_node is not None and version_node.text else ""
                result["packages"].append({"id": node.attrib["Include"], "version": version or ""})
        result["target_frameworks"] = sorted(set(result["target_frameworks"]))
        result["project_references"] = sorted(set(result["project_references"]))
        result["packages"] = sorted(result["packages"], key=lambda x: (x["id"].lower(), x["version"]))
    except Exception as exc:  # report, never fail the inventory
        result["parse_error"] = f"{type(exc).__name__}: {exc}"
    return result


def find_cycles(graph: dict[str, list[str]]) -> list[list[str]]:
    cycles: set[tuple[str, ...]] = set()
    visiting: set[str] = set()
    visited: set[str] = set()
    stack: list[str] = []

    def canonical(cycle: list[str]) -> tuple[str, ...]:
        body = cycle[:-1]
        rotations = [tuple(body[i:] + body[:i]) for i in range(len(body))]
        best = min(rotations)
        return best + (best[0],)

    def visit(node: str) -> None:
        if node in visiting:
            idx = stack.index(node)
            cycles.add(canonical(stack[idx:] + [node]))
            return
        if node in visited:
            return
        visiting.add(node)
        stack.append(node)
        for nxt in graph.get(node, []):
            if nxt in graph:
                visit(nxt)
        stack.pop()
        visiting.remove(node)
        visited.add(node)

    for key in sorted(graph):
        visit(key)
    return [list(value) for value in sorted(cycles)]


def duplicate_groups(files: Iterable[Path]) -> list[dict[str, Any]]:
    groups: dict[str, list[str]] = collections.defaultdict(list)
    for path in files:
        try:
            data = path.read_bytes()
        except OSError:
            continue
        if len(data) < 80:
            continue
        groups[hashlib.sha256(data).hexdigest()].append(rel(path))
    duplicates = [
        {"sha256": digest, "files": sorted(paths), "count": len(paths)}
        for digest, paths in groups.items() if len(paths) > 1
    ]
    return sorted(duplicates, key=lambda x: (-x["count"], x["files"]))


def longest_run_blocks(text: str) -> list[int]:
    lines = text.splitlines()
    sizes: list[int] = []
    idx = 0
    while idx < len(lines):
        match = re.match(r"^(\s*)run:\s*\|\s*$", lines[idx])
        if not match:
            idx += 1
            continue
        indent = len(match.group(1))
        start = idx + 1
        idx = start
        while idx < len(lines):
            current = lines[idx]
            if current.strip() and len(current) - len(current.lstrip()) <= indent:
                break
            idx += 1
        sizes.append(idx - start)
    return sizes


def workflow_metrics(path: Path, text: str) -> dict[str, Any]:
    quoted_paths = re.findall(r'^\s+-\s+["\']([^"\']+)["\']\s*$', text, re.MULTILINE)
    run_blocks = longest_run_blocks(text)
    return {
        "path": rel(path),
        "lines": line_count(text),
        "jobs": len(re.findall(r"^  [A-Za-z0-9_-]+:\s*$", text, re.MULTILINE)),
        "setup_dotnet": text.count("actions/setup-dotnet@"),
        "checkout": text.count("actions/checkout@"),
        "restore_commands": len(re.findall(r"\bdotnet restore\b", text)),
        "build_commands": len(re.findall(r"\bdotnet build\b", text)),
        "test_commands": len(re.findall(r"\bdotnet test\b", text)),
        "artifact_uploads": text.count("actions/upload-artifact@"),
        "path_filters": quoted_paths,
        "inline_run_blocks": len(run_blocks),
        "largest_inline_run_block_lines": max(run_blocks, default=0),
    }


def main() -> int:
    files = tracked_files()
    text_files = [p for p in files if is_text(p)]
    empty_files: list[str] = []
    generated_tracked: list[str] = []
    binary_tracked: list[str] = []
    secret_shape_files: list[str] = []
    todo_by_file: dict[str, int] = {}
    ext_stats: dict[str, dict[str, int]] = collections.defaultdict(lambda: {"files": 0, "bytes": 0, "lines": 0})
    top_stats: dict[str, dict[str, int]] = collections.defaultdict(lambda: {"files": 0, "bytes": 0, "lines": 0})
    file_rows: list[dict[str, Any]] = []

    cs_metrics = {
        "files": 0,
        "lines": 0,
        "types": 0,
        "records": 0,
        "interfaces": 0,
        "enums": 0,
        "route_mappings": 0,
        "test_methods": 0,
        "test_categories": collections.Counter(),
        "string_assertions": 0,
        "contains_assertions": 0,
    }
    route_inventory: list[dict[str, str]] = []
    docs_rows: list[dict[str, Any]] = []

    for path in files:
        r = rel(path)
        try:
            size = path.stat().st_size
        except OSError:
            size = 0
        if size == 0:
            empty_files.append(r)
        parts = set(PurePosixPath(r).parts)
        if parts & GENERATED_PARTS:
            generated_tracked.append(r)
        if path.suffix.lower() in BINARY_SUFFIXES:
            binary_tracked.append(r)
        text = read_text(path) if path in text_files else ""
        lines = line_count(text) if text else 0
        suffix = path.suffix.lower() or "[no-extension]"
        ext_stats[suffix]["files"] += 1
        ext_stats[suffix]["bytes"] += size
        ext_stats[suffix]["lines"] += lines
        top_stats[top_level(path)]["files"] += 1
        top_stats[top_level(path)]["bytes"] += size
        top_stats[top_level(path)]["lines"] += lines
        file_rows.append({"path": r, "bytes": size, "lines": lines, "extension": suffix})

        if text:
            todo_count = len(re.findall(r"\b(?:TODO|FIXME|HACK|XXX)\b", text, re.IGNORECASE))
            if todo_count:
                todo_by_file[r] = todo_count
            if any(pattern.search(text) for pattern in SECRET_PATTERNS):
                secret_shape_files.append(r)

        if path.suffix.lower() == ".cs":
            cs_metrics["files"] += 1
            cs_metrics["lines"] += lines
            cs_metrics["records"] += len(re.findall(r"\b(?:record|record\s+class|record\s+struct)\s+[A-Za-z_]", text))
            cs_metrics["interfaces"] += len(re.findall(r"\binterface\s+[A-Za-z_]", text))
            cs_metrics["enums"] += len(re.findall(r"\benum\s+[A-Za-z_]", text))
            cs_metrics["types"] += len(re.findall(r"\b(?:class|struct)\s+[A-Za-z_]", text))
            cs_metrics["test_methods"] += text.count("[TestMethod]")
            for category in re.findall(r'\[TestCategory\("([^"]+)"\)\]', text):
                cs_metrics["test_categories"][category] += 1
            cs_metrics["string_assertions"] += len(re.findall(r"\bStringAssert\.", text))
            cs_metrics["contains_assertions"] += len(re.findall(r"\b(?:StringAssert\.Contains|Assert\.IsTrue\([^\n]*\.Contains)", text))
            for method, route in re.findall(r'\.Map(Get|Post|Put|Delete|Patch)\(\s*"([^"]+)"', text):
                cs_metrics["route_mappings"] += 1
                route_inventory.append({"method": method.upper(), "route": route, "file": r})

        if path.suffix.lower() == ".md":
            docs_rows.append({
                "path": r,
                "lines": lines,
                "authorize_tokens": text.count("AUTHORIZE_NODAL_OS_"),
                "resulting_state_tokens": text.count("Resulting state"),
                "no_authority_tokens": len(re.findall(r"NO_(?:PRODUCTION_|PRODUCT_)?AUTHORITY", text)),
                "percentage_tokens": len(re.findall(r"`?\d{1,3}%`?", text)),
            })

    projects = [parse_project(p) for p in files if p.suffix.lower() == ".csproj"]
    graph = {p["path"]: p["project_references"] for p in projects}
    project_cycles = find_cycles(graph)
    all_packages = collections.Counter((pkg["id"], pkg["version"]) for p in projects for pkg in p["packages"])
    preview_packages = sorted(
        {f"{package} {version}" for package, version in all_packages if "preview" in version.lower() or "rc" in version.lower()}
    )
    target_frameworks = collections.Counter(tf for p in projects for tf in p["target_frameworks"])

    workflows = []
    for path in files:
        if rel(path).startswith(".github/workflows/") and path.suffix.lower() in {".yml", ".yaml"}:
            workflows.append(workflow_metrics(path, read_text(path)))

    source_lines = sum(row["lines"] for row in file_rows if row["path"].startswith("src/") and row["extension"] == ".cs")
    test_lines = sum(row["lines"] for row in file_rows if row["path"].startswith("tests/") and row["extension"] == ".cs")
    docs_lines = sum(row["lines"] for row in file_rows if row["extension"] == ".md")

    large_source = sorted(
        [row for row in file_rows if row["path"].startswith("src/") and row["extension"] == ".cs" and row["lines"] >= 500],
        key=lambda x: (-x["lines"], x["path"]),
    )
    large_tests = sorted(
        [row for row in file_rows if row["path"].startswith("tests/") and row["extension"] == ".cs" and row["lines"] >= 400],
        key=lambda x: (-x["lines"], x["path"]),
    )
    large_docs = sorted(
        [row for row in docs_rows if row["lines"] >= 500],
        key=lambda x: (-x["lines"], x["path"]),
    )
    largest_files = sorted(file_rows, key=lambda x: (-x["lines"], -x["bytes"], x["path"]))[:50]

    solution_projects: list[str] = []
    slnx = ROOT / "OneBrain.slnx"
    if slnx.exists():
        solution_projects = sorted(re.findall(r'<Project Path="([^"]+)"', read_text(slnx)))
    project_paths = sorted(p["path"] for p in projects)
    projects_outside_solution = sorted(set(project_paths) - set(solution_projects))
    missing_solution_projects = sorted(set(solution_projects) - set(project_paths))

    runtime_project = next((p for p in projects if p["path"] == "tests/OneBrain.Runtime.Tests/OneBrain.Runtime.Tests.csproj"), None)
    selective = next((w for w in workflows if w["path"].endswith("selective-runtime-integration.yml")), None)
    runtime_direct_roots = sorted({str(PurePosixPath(ref).parent).replace("\\", "/") + "/**" for ref in (runtime_project or {}).get("project_references", [])})
    workflow_filters = set((selective or {}).get("path_filters", []))
    uncovered_runtime_roots = sorted(root for root in runtime_direct_roots if root not in workflow_filters and "src/**" not in workflow_filters)

    duplicate_text = duplicate_groups(text_files)
    duplicate_csharp = [g for g in duplicate_text if all(p.endswith(".cs") for p in g["files"])]

    report: dict[str, Any] = {
        "generated_at_utc": os.environ.get("AUDIT_TIMESTAMP_UTC", "deterministic-ci-run"),
        "git": {
            "head": run("git", "rev-parse", "HEAD"),
            "branch": run("git", "rev-parse", "--abbrev-ref", "HEAD"),
            "tracked_files": len(files),
        },
        "totals": {
            "bytes": sum(row["bytes"] for row in file_rows),
            "text_lines": sum(row["lines"] for row in file_rows),
            "source_csharp_lines": source_lines,
            "test_csharp_lines": test_lines,
            "markdown_lines": docs_lines,
            "test_to_source_line_ratio": round(test_lines / source_lines, 3) if source_lines else None,
            "docs_to_source_line_ratio": round(docs_lines / source_lines, 3) if source_lines else None,
        },
        "by_extension": dict(sorted(ext_stats.items(), key=lambda item: (-item[1]["lines"], item[0]))),
        "by_top_level": dict(sorted(top_stats.items(), key=lambda item: (-item[1]["lines"], item[0]))),
        "csharp": {**cs_metrics, "test_categories": dict(cs_metrics["test_categories"].most_common())},
        "projects": {
            "count": len(projects),
            "solution_project_count": len(solution_projects),
            "target_frameworks": dict(target_frameworks),
            "preview_packages": preview_packages,
            "project_cycles": project_cycles,
            "projects_outside_solution": projects_outside_solution,
            "missing_solution_projects": missing_solution_projects,
            "items": projects,
        },
        "workflows": workflows,
        "coverage_gaps": {
            "runtime_test_direct_project_roots": runtime_direct_roots,
            "selective_runtime_uncovered_direct_roots": uncovered_runtime_roots,
        },
        "product_surface": {
            "route_count": len(route_inventory),
            "routes": sorted(route_inventory, key=lambda x: (x["route"], x["method"], x["file"])),
        },
        "documentation": {
            "markdown_file_count": len(docs_rows),
            "large_documents": large_docs,
            "authorize_token_total": sum(row["authorize_tokens"] for row in docs_rows),
            "resulting_state_token_total": sum(row["resulting_state_tokens"] for row in docs_rows),
            "no_authority_token_total": sum(row["no_authority_tokens"] for row in docs_rows),
            "percentage_token_total": sum(row["percentage_tokens"] for row in docs_rows),
        },
        "large_files": {
            "source_500_plus": large_source,
            "tests_400_plus": large_tests,
            "largest_text_files": largest_files,
        },
        "duplicates": {
            "exact_text_groups": duplicate_text[:100],
            "exact_csharp_groups": duplicate_csharp[:100],
        },
        "hygiene": {
            "empty_tracked_files": sorted(empty_files),
            "generated_paths_tracked": sorted(generated_tracked),
            "binary_files_tracked": sorted(binary_tracked),
            "todo_marker_total": sum(todo_by_file.values()),
            "todo_by_file": dict(sorted(todo_by_file.items(), key=lambda x: (-x[1], x[0]))[:100]),
            "files_matching_secret_shapes": sorted(secret_shape_files),
        },
    }

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    (OUT_DIR / "repository-audit.json").write_text(json.dumps(report, indent=2, sort_keys=True), encoding="utf-8")

    def table(rows: list[list[Any]], headers: list[str]) -> str:
        if not rows:
            return "_None._\n"
        lines = ["| " + " | ".join(headers) + " |", "| " + " | ".join("---" for _ in headers) + " |"]
        for row in rows:
            lines.append("| " + " | ".join(str(value).replace("|", "\\|") for value in row) + " |")
        return "\n".join(lines) + "\n"

    md: list[str] = [
        "# NODAL OS Repository Audit Inventory",
        "",
        f"- HEAD: `{report['git']['head']}`",
        f"- Tracked files: `{report['git']['tracked_files']}`",
        f"- Text lines: `{report['totals']['text_lines']}`",
        f"- Source C# lines: `{source_lines}`",
        f"- Test C# lines: `{test_lines}`",
        f"- Markdown lines: `{docs_lines}`",
        f"- Test/source line ratio: `{report['totals']['test_to_source_line_ratio']}`",
        f"- Docs/source line ratio: `{report['totals']['docs_to_source_line_ratio']}`",
        f"- Projects: `{len(projects)}`; solution entries: `{len(solution_projects)}`",
        f"- Test methods: `{cs_metrics['test_methods']}`",
        f"- HTTP route mappings found: `{len(route_inventory)}`",
        "",
        "## Largest source files",
        "",
        table([[row['lines'], row['path']] for row in large_source[:25]], ["Lines", "Path"]),
        "## Largest test files",
        "",
        table([[row['lines'], row['path']] for row in large_tests[:25]], ["Lines", "Path"]),
        "## Large documentation files",
        "",
        table([[row['lines'], row['authorize_tokens'], row['resulting_state_tokens'], row['path']] for row in large_docs[:30]], ["Lines", "AUTHORIZE tokens", "Resulting-state tokens", "Path"]),
        "## Workflow inventory",
        "",
        table([[w['lines'], w['jobs'], w['test_commands'], w['largest_inline_run_block_lines'], w['path']] for w in workflows], ["Lines", "Jobs", "dotnet test", "Largest inline run", "Path"]),
        "## Immediate hygiene facts",
        "",
        f"- Empty tracked files: `{len(empty_files)}`",
        f"- Generated-directory paths tracked: `{len(generated_tracked)}`",
        f"- Binary files tracked: `{len(binary_tracked)}`",
        f"- Exact duplicate text groups: `{len(duplicate_text)}`",
        f"- Exact duplicate C# groups: `{len(duplicate_csharp)}`",
        f"- TODO/FIXME/HACK markers: `{sum(todo_by_file.values())}`",
        f"- Secret-shape matches requiring manual review: `{len(secret_shape_files)}` (paths only; values are never emitted)",
        f"- Selective-runtime uncovered direct project roots: `{len(uncovered_runtime_roots)}`",
        "",
        "### Empty tracked files",
        "",
        *([f"- `{item}`" for item in sorted(empty_files)] or ["- None"]),
        "",
        "### Selective-runtime uncovered direct roots",
        "",
        *([f"- `{item}`" for item in uncovered_runtime_roots] or ["- None"]),
        "",
        "The JSON artifact contains the complete machine-readable inventory.",
    ]
    (OUT_DIR / "repository-audit.md").write_text("\n".join(md) + "\n", encoding="utf-8")

    print(json.dumps({
        "head": report["git"]["head"],
        "tracked_files": len(files),
        "source_lines": source_lines,
        "test_lines": test_lines,
        "docs_lines": docs_lines,
        "projects": len(projects),
        "test_methods": cs_metrics["test_methods"],
        "routes": len(route_inventory),
        "empty_files": len(empty_files),
        "large_source_files": len(large_source),
        "large_docs": len(large_docs),
        "uncovered_runtime_roots": uncovered_runtime_roots,
    }, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
