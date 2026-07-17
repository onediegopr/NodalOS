from pathlib import Path

path = Path("eng/ci/smoke-desktop-package.ps1")
text = path.read_text(encoding="utf-8-sig")
old = '$bundlePath = Get-ChildItem $outputRoot -Filter "*-private-beta.zip" -File | Select-Object -Single\n'
new = (
    '$bundlePaths = @(Get-ChildItem $outputRoot -Filter "*-private-beta.zip" -File)\n'
    '    if ($bundlePaths.Count -ne 1) {\n'
    '        throw "Expected exactly one private-beta bundle, found $($bundlePaths.Count)."\n'
    '    }\n'
    '    $bundlePath = $bundlePaths[0]\n'
)
if text.count(old) != 1:
    raise SystemExit("Expected the single-bundle selector exactly once.")
path.write_text(text.replace(old, new, 1), encoding="utf-8")
