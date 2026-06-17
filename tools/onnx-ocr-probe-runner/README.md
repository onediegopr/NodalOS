# onnx-ocr-probe-runner (M210)

Isolated child runner launched by `NodalOsOnnxOutOfProcessGuard` to attempt ONNX OCR probes that
can crash the native ONNX Runtime. If the native runtime crashes, **this** process dies; the parent
guard maps the crash to a controlled result (`NativeRuntimeCrash`) and the test host survives.

## Safety invariants

- Synthetic / redacted / non-sensitive fixtures only.
- No SaaS OCR, no network, no raw persistence, no full-screen, no sensitive surfaces.
- No-authority: results are never used to approve clicks/submit/pay/sign/delete.
- Not integrated into the production CDP pipeline.

## Usage

```text
# Deterministic outcomes (no ONNX load) — used by guard unit tests:
OneBrain.Tools.OnnxOcrProbeRunner --self-test safe      # valid JSON, exit 0
OneBrain.Tools.OnnxOcrProbeRunner --self-test crash     # simulates native access violation exit code
OneBrain.Tools.OnnxOcrProbeRunner --self-test abort     # simulates native abort exit code
OneBrain.Tools.OnnxOcrProbeRunner --self-test nonzero   # exit 7
OneBrain.Tools.OnnxOcrProbeRunner --self-test timeout   # sleeps; guard must kill
OneBrain.Tools.OnnxOcrProbeRunner --self-test garbage   # invalid output, exit 0

# Real ONNX inference on a synthetic fixture (may crash for text-like fixtures):
OneBrain.Tools.OnnxOcrProbeRunner --probe --repo-root <repo> --request <probe-request.json>
```

The runner prints a single `NodalOsOnnxOutOfProcessRunnerReport` JSON object to stdout on a
controlled outcome. The guard reads stdout, exit code, and timeout to classify the result.
