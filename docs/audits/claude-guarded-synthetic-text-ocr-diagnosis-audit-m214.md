# Claude Audit Prompt - Guarded Synthetic Text OCR Diagnosis M214

Audit the M212-M214 implementation and evidence. Focus on whether the decision is honest and whether the guard boundary is preserved.

Files to inspect:

- `src/OneBrain.BrowserExecutor.Contracts/NodalOsGuardedSyntheticTextOcrProbeContracts.cs`
- `src/OneBrain.BrowserExecutor.Contracts/NodalOsOnnxNativeRuntimeCrashContracts.cs`
- `src/OneBrain.BrowserExecutor.Cdp/NodalOsGuardedSyntheticTextOcrProbeServices.cs`
- `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxOutOfProcessGuard.cs`
- `tools/onnx-ocr-probe-runner/Program.cs`
- `tests/OneBrain.Safety.Tests/NodalOsGuardedSyntheticTextOcrM212M214Tests.cs`
- `docs/reports/guarded-synthetic-text-ocr-diagnosis-m214.md`
- `artifacts/ocr-vision-onnx/m214/guarded-synthetic-text-ocr-diagnosis-summary.json`
- `docs/adr/guarded-synthetic-text-ocr-runtime-decision-m212-m214.md`

Audit questions:

- Did risky synthetic text probes remain out-of-process only?
- Does the parent process survive child native crash, non-zero exit, invalid JSON, and timeout?
- Are risky text probes rejected in-process?
- Are input tensor shape and tensor min/max/mean captured?
- Are NaN/Infinity blocked before ONNX Runtime?
- Is channel layout / RGB-vs-BGR status represented?
- Is model compatibility assessed honestly for RapidOCR/ModelScope PaddleOCR ONNX models?
- Is the missing detector/recognizer model status handled without inventing success?
- Is dictionary/CTC compatibility treated as unverified rather than assumed?
- Is the final decision honest?
- Should the next route be runtime version experiment, model replacement/acquisition, dictionary completion, renderer fix, or more fixtures?

Expected audit conclusion:

The correct next route should be model compatibility/model acquisition first, followed by dictionary completion, before any positive synthetic OCR recognition or shadow-mode claim.
