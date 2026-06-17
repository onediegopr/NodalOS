# NODAL OS - M226-M228 Full OCR Handoff + Recognition Runtime Isolation

## Scope

This block isolates the full OCR crash downstream of the detector. It keeps all risky OCR inside the out-of-process guard and uses only synthetic/redacted fixtures.

Production OCR, shadow mode, real documents, full-screen OCR, sensitive surfaces, SaaS OCR, and OCR-as-authority remain blocked.

## Starting Point

- Base HEAD: `7170763`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Remote observed: `https://github.com/onediegopr/NodalOS.git`
- Detector model: `ch_PP-OCRv4_det.onnx`
- Recognition model: `ch_PP-OCRv4_rec.onnx`
- Classification model: `ch_ppocr_mobile_v2.0_cls.onnx`
- Runtime: `Microsoft.ML.OnnxRuntime 1.18.1`
- Provider: `CPUExecutionProvider`

## M226 Handoff Isolation

Implemented explicit detector-to-recognizer handoff contracts and probe modes:

- `--handoff-crash-probe`
- `--handoff-crash-child`

The probe separates:

- detector session creation
- detector run
- detector output metadata
- detector postprocessing
- box validation
- crop bounds calculation
- crop extraction
- recognizer crop resize
- recognizer tensor preparation
- recognizer session creation
- recognizer `session.Run`
- recognizer output metadata
- recognition postprocessing / dictionary gate

Box variants:

- detector-produced box
- manual safe box
- degenerate box
- out-of-bounds box
- empty crop
- too-small crop

Invalid, out-of-bounds, empty, and too-small crop cases are blocked before recognizer runtime.

## M227 Recognition Runtime Probe

Implemented recognizer runtime probe modes:

- `--recognizer-runtime-probe`
- `--recognizer-runtime-child`

Recognizer tensor variants:

- zero tensor
- ones tensor
- gradient tensor
- synthetic text crop tensor
- high-contrast manual crop tensor
- detector-derived crop tensor

The recognizer probe captures:

- model path
- input metadata
- output metadata
- tensor shape
- tensor min/max/mean
- NaN/Infinity checks
- session creation
- `session.Run`
- output shape
- output class count
- crash stage / exit code if the child dies

## Dictionary / CTC

The current recognizer output class count is documented as `97`.

The current ASCII dictionary has:

- ASCII dictionary characters: `86`
- ASCII plus blank: `87`
- recognizer classes: `97`

This is incompatible. Decode must not proceed with the ASCII dictionary, and no text may be invented.

Status:

`BlockedByDictionaryClassCountMismatch`

## Observed Probe Evidence

After controlled model re-download and verification, guarded out-of-process probes produced:

- detector-produced handoff crop: child crash contained during recognizer runtime
- manual safe handoff crop: child crash contained during recognizer runtime
- degenerate box: blocked before runtime
- out-of-bounds box: blocked before runtime
- empty crop: blocked before runtime
- too-small crop: blocked before runtime
- recognizer zero tensor: session created, metadata captured, crash at `session.Run`
- recognizer ones tensor: session created, metadata captured, crash at `session.Run`
- recognizer gradient tensor: session created, metadata captured, crash at `session.Run`
- recognizer synthetic text crop tensor: session created, metadata captured, crash at `session.Run`
- recognizer high-contrast crop tensor: session created, metadata captured, crash at `session.Run`
- recognizer detector-derived crop tensor: session created, metadata captured, crash at `session.Run`

Crash exit:

`-1073741676 / 0xC0000094`

Recognizer metadata captured before crash:

- input: `x=[-1,3,-1,-1]`
- output: `softmax_2.tmp_0=[-1,-1,97]`

Parent survived and temp cleanup succeeded.

## Readiness

The detector-only runtime is no longer the proven blocker. The next technical branch depends on guarded recognizer runtime evidence:

- If recognizer `session.Run` succeeds and output class count remains `97`, the next route is dictionary completion.
- If recognizer `session.Run` crashes on zero/ones tensors, the next route is recognizer runtime experiment or recognizer model replacement.
- If box/crop validation blocks before runtime, the next route is handoff fix.

## Decision

Current runtime evidence maps all recognizer `session.Run` attempts crashing to:

`READY_FOR_RECOGNIZER_RUNTIME_EXPERIMENT`

Shadow mode remains blocked. Productive OCR remains blocked.
