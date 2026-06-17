# Detector Runtime Compatibility Experiment M223-M225

## Decision

`M223+M224+M225 CERRADO / BLOCKED_BY_MODEL_RUNTIME`

This block does not support replacing the detector model or changing ONNX Runtime for detector execution yet. Detector-only `session.Run` succeeds for all tested tensors and session options. The full guarded OCR probe still crashes downstream with `0xC0000094`, so OCR remains blocked and the next isolation step must move beyond detector runtime.

## Environment

- Worktree: `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Base commit: `ef909dc`
- Package: `Microsoft.ML.OnnxRuntime 1.18.1`
- Runtime assembly observed by child: `0.0.0.0`
- Provider: `CPUExecutionProvider`
- OS architecture: `X64`
- Detector model: `tools/ocr-worker/models/onnx/ch_PP-OCRv4_det.onnx`
- Detector hash: `d2a7720d45a54257208b1e13e36a8479894cb74155a5efe29462512d42f49da9`
- Detector size: `4745517`
- Opset: `17`
- Input metadata: `x=[-1,3,-1,-1]`
- Output metadata: `sigmoid_0.tmp_0=[-1,1,-1,-1]`
- Output shape observed: `[1,1,640,640]`

## Matrix Summary

All detector runtime probes ran out-of-process through the guard.

| Tensor | Default | Graph disabled | Graph basic | Single threaded | Memory pattern disabled | CPU arena disabled |
| --- | --- | --- | --- | --- | --- | --- |
| Zero | RunSucceeded | RunSucceeded | RunSucceeded | RunSucceeded | RunSucceeded | RunSucceeded |
| Ones | RunSucceeded | RunSucceeded | RunSucceeded | RunSucceeded | RunSucceeded | RunSucceeded |
| Gradient | RunSucceeded | RunSucceeded | RunSucceeded | RunSucceeded | RunSucceeded | RunSucceeded |
| SyntheticText direct | RunSucceeded | RunSucceeded | RunSucceeded | RunSucceeded | RunSucceeded | RunSucceeded |
| CurrentPreprocessedSyntheticText | RunSucceeded | RunSucceeded | RunSucceeded | RunSucceeded | RunSucceeded | RunSucceeded |
| SafeRectangle | RunSucceeded | RunSucceeded | RunSucceeded | RunSucceeded | RunSucceeded | RunSucceeded |
| SafeCircle | RunSucceeded | RunSucceeded | RunSucceeded | RunSucceeded | RunSucceeded | RunSucceeded |

Additional layout probe:

- NHWC: `UnsupportedLayout`, skipped before runtime because the detector manifest/metadata expects NCHW.

Additional postprocessing probe:

- `CurrentPreprocessedSyntheticText` + default session + detector postprocessing: `RunSucceeded`, postprocessor status `Success`, boxes `1`.

## Answers

- Does detector model exist and verify? Yes.
- Does session creation work? Yes, all NCHW child probes reached session creation.
- Does the crash occur in detector `session.Run`? No in detector-only reproduction.
- Does zero tensor crash? No.
- Does ones tensor crash? No.
- Does gradient tensor crash? No.
- Does synthetic text tensor crash? No.
- Does current preprocessed synthetic text tensor crash? No.
- Do safe rectangle/circle tensors crash? No.
- Does any session option avoid crash? No crash was present in detector-only mode, so no option fix is indicated.
- Does input shape/layout look correct? Yes for NCHW `[1,3,640,640]`; NHWC is unsupported/skipped.
- Does crash depend on tensor content? Not at detector-only level.
- Does crash depend on session options? Not at detector-only level.
- Was postprocessing reached? Yes in the dedicated detector postprocessing child probe.
- Did postprocessing crash? No.
- Did parent survive? Yes.
- Were child/temp resources cleaned? Yes.

## Finding

M222's full guarded OCR retry still crashes with exit `-1073741676` / `0xC0000094`, but M223's detector-only isolation does not reproduce that crash, even with current preprocessed synthetic text and detector postprocessing.

Therefore, the detector model, detector input shape, CPU provider, tested session options, and detector postprocessor are not currently proven to be the crash source. The remaining model-runtime blocker is downstream of detector isolation, most likely recognition-runtime integration or full-pipeline handoff, and must be isolated in a follow-up block before dictionary completion or shadow mode.

## Gates

- No productive OCR: passed
- No SaaS: passed
- No raw persistence: passed
- No full-screen: passed
- No sensitive: passed
- No authority: passed
- Risky probes in-process: no
- Risky detector probes out-of-process: yes
- Shadow mode: blocked
