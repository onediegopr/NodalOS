# ONNX Model Availability Reconciliation M217

## Scope

M215-M217 reconciles the real ONNX model inventory, manifest paths, acquisition scripts, and guarded retry readiness after M212-M214 found detector and recognizer models missing.

No productive OCR, SaaS OCR, real documents, full-screen OCR, sensitive OCR, external proof, or OCR-as-authority behavior was enabled.

## Manifest Entries

The manifest at `tools/ocr-worker/models/onnx/paddleocr-onnx-model-manifest.json` expects:

| Role | Model ID | Expected file | SHA-256 | Size |
| --- | --- | --- | --- | --- |
| detection | `paddleocr-det-onnx` | `tools/ocr-worker/models/onnx/ch_PP-OCRv4_det.onnx` | `d2a7720d45a54257208b1e13e36a8479894cb74155a5efe29462512d42f49da9` | 4,745,517 |
| recognition | `paddleocr-rec-onnx` | `tools/ocr-worker/models/onnx/ch_PP-OCRv4_rec.onnx` | `e8770c967605983d1570cdf5352041dfb68fa0c21664f49f47b155abd3e0e318` | 7,653,044 |
| classification | `paddleocr-cls-onnx` | `tools/ocr-worker/models/onnx/ch_ppocr_mobile_v2.0_cls.onnx` | `e47acedf663230f8863ff1ab0e64dd2d82b838fceb5957146dab185a89d6215c` | 585,532 |

These entries match the M200-M202 source decision and session smoke reports.

## Disk Inventory

Current disk state:

| Role | Expected file | Disk status |
| --- | --- | --- |
| detection | `ch_PP-OCRv4_det.onnx` | missing |
| recognition | `ch_PP-OCRv4_rec.onnx` | missing |
| classification | `ch_ppocr_mobile_v2.0_cls.onnx` | present and verified |

Classification model verification:

- actual size: 585,532 bytes
- actual SHA-256: `e47acedf663230f8863ff1ab0e64dd2d82b838fceb5957146dab185a89d6215c`
- matches manifest: yes

## Path Resolution

All manifest paths resolve under the repository root:

`C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo-m12-audit`

No path discovery mismatch was found. The reason M212-M214 saw detector/recognizer missing is direct disk absence, not manifest path mismatch.

## Git Ignore / Commit Status

`tools/ocr-worker/models/onnx/.gitignore` ignores:

- `*.onnx`
- `*.pdmodel`
- `*.pdiparams`
- `*.pdparams`

`git ls-files tools/ocr-worker/models/onnx` shows only scripts, manifest, README, conversion plan, and `.gitignore`. No ONNX model is committed.

## Acquisition Scripts

Scripts present:

- `download-models.ps1`
- `verify-models.ps1`
- `rollback-models.ps1`
- `convert-models-plan.md`

The download script reads the manifest, downloads the pinned ModelScope URLs, verifies size and SHA-256, and removes a file on mismatch. It requires explicit `-Confirm`.

The verify script checks existence, size, and SHA-256 against the manifest. It does not execute OCR.

The rollback script deletes `.onnx` files under the model directory after explicit `-Confirm`. It is intentionally broad and would remove the present classifier too.

## Script Execution Attempt

Attempted:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File tools\ocr-worker\models\onnx\verify-models.ps1
```

Result:

`pwsh` is not installed or not on PATH in this environment. Because the scripts require PowerShell 7.2, `powershell.exe` was not used as a bypass.

No model download was executed. No model files were committed.

## Commands To Reconcile Models

Run from repository root with PowerShell 7.2+:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/ocr-worker/models/onnx/download-models.ps1 -Confirm
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/ocr-worker/models/onnx/verify-models.ps1
```

After both required models verify, run the guarded synthetic text retry filters. Risky OCR must remain out-of-process only.

## Guarded Retry Status

Retry was not executed because detection and recognition models are missing and script verification could not run in this environment. The retry plan exists and requires verified detector + recognizer before any guarded synthetic OCR smoke.

## Decision

`M215+M216+M217 CERRADO / READY_FOR_MODEL_DOWNLOAD`

Rationale: manifest, scripts, paths, and hashes are consistent with M200-M202. Current detector and recognizer files are missing from disk. Acquisition is ready through controlled scripts, but this environment lacks `pwsh`, so download/verify was not executed here.
