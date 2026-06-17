# ONNX Model Download Verification M218

## Scope

M218 attempted to execute the controlled ONNX model acquisition/verification path prepared in M215-M217. No manual download, productive OCR, SaaS OCR, real document OCR, screen OCR, sensitive OCR, shadow mode, CDP integration, or OCR-as-authority behavior was enabled.

## Initial State

- Worktree: `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Expected base: `704827c`
- Actual base: `704827c`
- Forbidden worktree `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo` was not used.

## PowerShell Detection

`pwsh` detection:

```text
where.exe pwsh
INFORMACION: no se pudo encontrar ningun archivo para los patrones dados.
```

Windows PowerShell detection:

```text
C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe
PSVersion 5.1.26100.8655
PSEdition Desktop
```

## Script Compatibility Check

Attempted:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\ocr-worker\models\onnx\verify-models.ps1
```

Result:

```text
No se puede ejecutar el script 'verify-models.ps1' porque contiene una instruccion "#requires" para Windows PowerShell 7.2.
La version de Windows PowerShell que requiere el script no coincide con Windows PowerShell 5.1.26100.8655.
```

Conclusion: Windows PowerShell 5.1 is not compatible. The scripts intentionally require PowerShell 7.2+, and M218 did not weaken that requirement.

## Model Disk State

Current model files:

| Model | State |
| --- | --- |
| `ch_PP-OCRv4_det.onnx` | missing |
| `ch_PP-OCRv4_rec.onnx` | missing |
| `ch_ppocr_mobile_v2.0_cls.onnx` | present |

Classifier verification:

- size: `585532`
- SHA-256: `E47ACEDF663230F8863FF1AB0E64DD2D82B838FCEB5957146DAB185A89D6215C`
- matches manifest: yes

Detection and recognition cannot be downloaded or verified until PowerShell 7.2+ is available.

## Git Ignore / Commit Status

`git check-ignore -v` confirms all expected ONNX paths are ignored by:

```text
tools/ocr-worker/models/onnx/.gitignore:2:*.onnx
```

No ONNX model files are tracked by git.

## Required Environment Fix

Install PowerShell 7.2+ from an official Microsoft source, then restart the terminal so `pwsh` is on PATH.

Safe options:

```powershell
winget install --id Microsoft.PowerShell --source winget
```

or download the latest stable PowerShell installer from:

```text
https://github.com/PowerShell/PowerShell/releases
```

After installation, verify:

```powershell
where.exe pwsh
pwsh -NoProfile -Command '$PSVersionTable.PSVersion'
```

Then run from the repository root:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/ocr-worker/models/onnx/download-models.ps1 -Confirm
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/ocr-worker/models/onnx/verify-models.ps1
```

## Decision

`M218 CERRADO / BLOCKED_BY_PWSH_MISSING`

The controlled script path is correct, but the current environment cannot execute it because PowerShell 7.2+ is missing. No manual download was attempted.
