# NODAL OS - M229-M231 Recognizer Runtime Compatibility Experiment

## Scope

This block isolates the PaddleOCR recognizer ONNX runtime crash. It does not enable productive OCR, shadow mode, real document OCR, real screen OCR, SaaS OCR, or OCR-as-authority.

All risky recognizer probes run through the out-of-process guard.

## Starting Point

- Base HEAD: `6066a06`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Remote: `https://github.com/onediegopr/NodalOS.git`
- Worktree: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- ONNX Runtime package: `Microsoft.ML.OnnxRuntime 1.18.1`
- Provider: `CPUExecutionProvider`

## Models

- detector: `ch_PP-OCRv4_det.onnx`
- recognizer: `ch_PP-OCRv4_rec.onnx`
- classifier: `ch_ppocr_mobile_v2.0_cls.onnx`

Recognizer verified hash:

`e8770c967605983d1570cdf5352041dfb68fa0c21664f49f47b155abd3e0e318`

Recognizer size:

`7653044`

## Recognizer Metadata

Captured before crash:

- input: `x=[-1,3,-1,-1]`
- output: `softmax_2.tmp_0=[-1,-1,97]`
- class count: `97`

## M229 Matrix

Tensor variants:

- zero
- ones
- gradient
- checker
- synthetic crop
- high-contrast manual crop
- detector-derived crop

Shapes/layouts:

- NCHW current pipeline shape `[1,3,32,320]`
- NCHW candidate PaddleOCR width `[1,3,32,640]`
- NHWC is skipped as unsupported because local metadata/pipeline expects NCHW
- invalid shape blocks before runtime

Session options:

- default
- graph optimization disabled
- graph optimization basic
- single-threaded intra/inter op
- memory pattern disabled
- CPU arena disabled
- sequential execution
- deterministic minimal options

## Evidence

M228 proved the recognizer creates a session and crashes in `session.Run` even with zero/ones/gradient tensors. M229 extends that evidence across session options and layouts.

Observed M229 results:

- zero/ones/gradient default: `NativeRuntimeCrashContained`
- synthetic/high-contrast/detector-derived crop default: `NativeRuntimeCrashContained`
- checker tensor is included in the runtime matrix
- `[1,3,32,640]` candidate width also crashes
- NHWC is skipped as `UnsupportedLayout`
- invalid shape is blocked before runtime as `InvalidTensorShape`
- no session option produced `RunSucceeded` or `OutputMetadataCaptured` after `session.Run`
- graph optimization disabled/basic and deterministic minimal return contained nonzero process exits from ONNX Runtime kernel failure
- single-threaded returns contained nonzero managed `DivideByZeroException`
- default, memory pattern disabled, CPU arena disabled, and sequential execution return contained native abort `0xC0000094`

The expected technical interpretation is:

- crash does not depend on crop content if zero/ones/gradient also crash
- dictionary mismatch is real but secondary
- recognizer output metadata can be captured before runtime crash
- postprocessing/CTC is not reached while `session.Run` crashes
- no session option avoided the recognizer runtime failure

## Dictionary / CTC Gate

- recognizer classes: `97`
- ASCII dictionary: `86`
- ASCII+blank: `87`
- decode allowed: `false`
- text invented: `false`

Dictionary completion remains deferred until recognizer runtime succeeds.

## Decision

`M229+M230+M231 CERRADO / READY_FOR_ONNX_RUNTIME_VERSION_EXPERIMENT`

Shadow mode remains blocked. Productive OCR remains blocked.
