# PowerShell 7 ONNX Model Download M219

## Scope

M219 installed/enabled PowerShell 7, executed only the approved ONNX model scripts, and verified the PaddleOCR ONNX model files. No productive OCR, real document OCR, screen OCR, sensitive OCR, SaaS OCR, shadow mode, CDP integration, or OCR-as-authority behavior was enabled.

## Initial State

- Worktree: `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Expected base: `bef340c`
- Actual base: `bef340c`
- Forbidden worktree `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo` was not used.

## Installer / PowerShell Status

Before:

- `winget`: available at `C:\Users\diego\AppData\Local\Microsoft\WindowsApps\winget.exe`
- `pwsh`: not available
- Windows PowerShell: `5.1.26100.8655`

Install command:

```powershell
winget install --id Microsoft.PowerShell --source winget --accept-package-agreements --accept-source-agreements
```

Result:

- PowerShell package found: `Microsoft.PowerShell`
- Installed version: `7.6.2.0`
- Installer hash verified by winget
- Install completed successfully

After:

```text
C:\Users\diego\AppData\Local\Microsoft\WindowsApps\pwsh.exe
PowerShell 7.6.2
```

## Approved Scripts Executed

Download:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/ocr-worker/models/onnx/download-models.ps1 -Confirm
```

Verify:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/ocr-worker/models/onnx/verify-models.ps1
```

No manual model download was used.

## Model Verification

| Model | Status | Size | SHA-256 |
| --- | --- | ---: | --- |
| `ch_PP-OCRv4_det.onnx` | PresentAndVerified | 4,745,517 | `d2a7720d45a54257208b1e13e36a8479894cb74155a5efe29462512d42f49da9` |
| `ch_PP-OCRv4_rec.onnx` | PresentAndVerified | 7,653,044 | `e8770c967605983d1570cdf5352041dfb68fa0c21664f49f47b155abd3e0e318` |
| `ch_ppocr_mobile_v2.0_cls.onnx` | PresentAndVerified | 585,532 | `e47acedf663230f8863ff1ab0e64dd2d82b838fceb5957146dab185a89d6215c` |

The final `verify-models.ps1` run reported all configured models verified.

## Git Ignore / Commit Status

`git check-ignore -v` confirms all ONNX files are ignored by:

```text
tools/ocr-worker/models/onnx/.gitignore:2:*.onnx
```

`git ls-files tools/ocr-worker/models/onnx/*.onnx` returned no tracked model files. Models were not committed.

## Tests

Executed:

```powershell
dotnet test .\OneBrain.slnx --no-build --no-restore --filter "OnnxModelInventory|OnnxModelAvailability|OnnxModelVerification|OnnxModelReadiness"
```

Result: 23 passed, 0 failed.

Executed:

```powershell
dotnet test .\OneBrain.slnx --no-build --no-restore --filter "GuardedSyntheticTextOcr|OnnxOutOfProcessGuard|OnnxOcrProbeRunner"
```

Result: 22 passed, 1 skipped.

Full suite:

- `OneBrain.Recipes.Tests`: 635 passed.
- `OneBrain.Safety.Tests`: 1332 passed, 29 skipped, 3 failed, then test host aborted.
- Failures were browser runtime/profile/devtools cleanup related:
  - `BrowserProfileSessionManagerTests.BrowserLauncherUsesProfileSessionManagerForDefaultDisposableProfile`
  - `BrowserRuntimeSmokeTests.BrowserRuntimeSmokeRunnerExecutesAllGatesOnFixture`
  - `BrowserRuntimeSmokeTests.BrowserRuntimeSmokeCleanupLeavesNoManagedProcessPortOrProfile`
- This is unrelated to ONNX model download/verification.

## Test Hygiene Fix

M219 also isolated older M197 model-verification tests that wrote fake files to the real model directory. Those tests now use a temporary repository root so future test runs cannot delete downloaded `.onnx` files.

## Decision

`M219 CERRADO / MODELS_PRESENT_AND_VERIFIED`

All three configured PaddleOCR ONNX models are present and verified through the approved scripts under PowerShell 7. Risky OCR remains out-of-process only, and productive OCR/shadow mode remain blocked.
