# M689 Public Variant Staging Remediation

Milestone: M689

Decision: `PUBLIC_VARIANT_STAGING_REMEDIATION_READY`

## Staging Folder

Created controlled local QA staging at:

```text
C:\DESARROLLO\NodalOS\Codigo-m12-audit\artifacts\manual-qa\public-variant-staging
```

This folder is intended for Chrome `Load unpacked` manual QA only. It is not a signed package and is not a public release artifact.

## Manifest Selection

The staging folder uses `browser-extension/onebrain-chrome-lab/manifest.public.json` as `manifest.json`.

The repository internal manifest remains unchanged at `browser-extension/onebrain-chrome-lab/manifest.json`.

## Public Manifest Safety

The staging manifest:

- has name `NODAL OS Public Candidate`;
- has host permissions limited to `http://127.0.0.1/*` and `http://localhost/*`;
- does not contain `http://*/*`;
- does not contain `https://*/*`;
- does not define automatic external content scripts.

## Script

Created:

```text
tools/manual-qa/prepare-public-variant-staging.ps1
```

The script clears the staging folder, copies only extension QA files, maps the public manifest to `manifest.json`, validates JSON, blocks wildcard host permissions, and does not print or copy secret material.

## Cleanup

The staging folder can be regenerated at any time. If cleanup is required:

```powershell
Remove-Item -LiteralPath 'artifacts/manual-qa/public-variant-staging' -Recurse -Force
```
