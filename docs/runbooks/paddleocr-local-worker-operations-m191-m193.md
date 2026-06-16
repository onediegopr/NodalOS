# Runbook — PaddleOCR Local Worker Operations (M191-M193)

## Prerequisites

- Windows 10/11 or Linux.
- Python 3.9, 3.10, 3.11 or 3.12 (**not 3.13**).
- pip and venv modules.
- 2 GB free disk space minimum.

## Install

```powershell
.\tools\ocr-worker\setup-paddleocr.ps1
```

What it does:
- Creates `tools/ocr-worker/.venv`.
- Installs `paddlepaddle` (CPU) and `paddleocr`.
- Verifies imports.
- Logs to `tools/ocr-worker/setup-paddleocr.log`.

## Check status

```powershell
.\tools\ocr-worker\check-paddleocr.ps1
```

Outputs JSON with Python/PaddleOCR availability.

## Run worker

```powershell
$request = @{ contractVersion = "nodal-paddleocr-worker.v1"; authToken = "token"; imageBase64 = "..."; language = "en"; maxImageBytes = 2097152; allowRawPersistence = $false; allowFullScreen = $false; sensitivity = "Low" } | ConvertTo-Json -Compress
.\tools\ocr-worker\run-paddleocr-worker.ps1 -RequestJson $request
```

## Rollback

```powershell
.\tools\ocr-worker\rollback-paddleocr.ps1
```

Removes `.venv` and temp redacted crop files.

## Operational constraints

- Crop-only.
- Redacted-only.
- Local-only.
- No SaaS.
- No full-screen.
- No sensitive data.
- No raw original persistence.
- No OCR authority.
- Production public OCR remains disabled.

## Troubleshooting

| Symptom | Cause | Action |
|---------|-------|--------|
| `python not found` | Python not installed | Install Python 3.10/3.11 |
| `Python 3.13 is not supported` | Unsupported Python version | Downgrade to 3.10/3.11 |
| `PaddleOCR not available` | venv missing or install failed | Run setup script |
| `missing auth token` | Request not signed | Add authToken field |
| `raw persistence not allowed` | allowRawPersistence=true | Set false |

## Emergency stop

Run `rollback-paddleocr.ps1 -Force` to immediately remove the runtime.
