# NODAL OS M232-M234 - ONNX Runtime Version Experiment

## Scope

This report closes the controlled CPU-only ONNX Runtime version experiment for recognizer model `ch_PP-OCRv4_rec.onnx`.

Hard gates remained enforced:

- no productive OCR;
- no shadow mode;
- no SaaS OCR;
- no real documents or real screens;
- no raw image persistence;
- no OCR as authority;
- risky recognizer probes only out-of-process;
- ONNX models remained gitignored and untracked.

## Baseline And Candidates

- Baseline: `Microsoft.ML.OnnxRuntime 1.18.1`.
- Candidates tested: `1.22.1`, `1.23.2`, `1.25.0`.
- Provider: `CPUExecutionProvider` only.
- GPU, DirectML, external SaaS OCR, embedded runtime, Chromium fork, and productive OCR were not tested.

## Harness

The reversible harness is:

```powershell
scripts/experiments/onnx-runtime-version-experiment.ps1
```

The harness updates only the `Microsoft.ML.OnnxRuntime` package reference in:

```text
src/OneBrain.BrowserExecutor.Cdp/OneBrain.BrowserExecutor.Cdp.csproj
```

It runs restore/build, filtered runtime tests, detector-only sanity, and recognizer-only runtime probes. The `finally` block restores the baseline package version unless a selected runtime is explicitly requested.

## Recognizer Metadata

- Model: `ch_PP-OCRv4_rec.onnx`.
- Input: `x=[-1,3,-1,-1]`.
- Output: `softmax_2.tmp_0=[-1,-1,97]`.
- Dictionary gate: current ASCII dictionary remains incompatible (`97` classes vs `87` ASCII+blank).

## Results

| Runtime | Restore | Build | Detector sanity | Zero | Ones | Gradient | Crops | Output |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `1.18.1` | Succeeded | Succeeded | Succeeded | `NativeRuntimeCrashContained` | `NativeRuntimeCrashContained` | `NativeRuntimeCrashContained` | `NativeRuntimeCrashContained` | none |
| `1.22.1` | Succeeded | Succeeded | Succeeded | `RunSucceeded` | `RunSucceeded` | `RunSucceeded` | `RunSucceeded` | `[1,40,97]` |
| `1.23.2` | Succeeded | Succeeded | Succeeded | `RunSucceeded` | `RunSucceeded` | `RunSucceeded` | `RunSucceeded` | `[1,40,97]` |
| `1.25.0` | Succeeded | Succeeded | Succeeded | `RunSucceeded` | `RunSucceeded` | `RunSucceeded` | `RunSucceeded` | `[1,40,97]` |

Additional output evidence:

- Candidate recognizer width `640` produced `[1,80,97]`.
- Baseline crash remained `-1073741676 / 0xC0000094` at `RecognitionRun/session.Run`.
- Candidates returned exit code `0` for recognizer runtime probes.
- Detector sanity remained green for every tested candidate.
- Parent survived and child/temp cleanup remained contained.

## Dictionary / CTC

Recognizer runtime success does not imply text recognition success yet.

The recognizer exposes `97` output classes. The current ASCII dictionary has `86` characters and `87` including blank. Decode remains blocked by `BlockedByDictionaryClassCountMismatch`; no text was invented.

## Branch State

The branch was left at baseline package version:

```text
Microsoft.ML.OnnxRuntime 1.18.1
```

This is intentional. M232-M234 proves that runtime upgrade is the next route, but does not permanently change the runtime package in this block.

## Decision

```text
M232+M233+M234 CERRADO / READY_FOR_ONNX_RUNTIME_UPGRADE
```

Next route:

1. Upgrade `Microsoft.ML.OnnxRuntime` to at least `1.22.1` in a dedicated block.
2. Re-run guarded synthetic OCR with upgraded runtime.
3. Complete dictionary/CTC compatibility only after runtime upgrade is selected.
