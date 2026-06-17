# Claude Audit Prompt - Detector Runtime Compatibility M225

Audit the M223-M225 detector runtime compatibility experiment.

Required checks:

- Verify detector model exists and is hash/size verified.
- Verify `.onnx` models are gitignored and not committed.
- Verify risky detector probes ran only out-of-process through the guard.
- Verify parent survival and temp cleanup.
- Audit ONNX Runtime package/version, provider, OS architecture, model opset, and input/output metadata.
- Audit session options tested: default, graph optimization disabled, graph optimization basic, single-threaded, memory pattern disabled, CPU arena disabled.
- Audit tensor variants: zero, ones, gradient, direct synthetic text, current preprocessed synthetic text, safe rectangle, safe circle.
- Audit that detector-only `session.Run` succeeded for all NCHW variants.
- Audit that NHWC was skipped because metadata expects NCHW.
- Audit that detector postprocessing was reached and succeeded.
- Audit that the full guarded OCR probe still crashes with `0xC0000094`.
- Decide whether evidence points to detector runtime/model, preprocessing, renderer, postprocessing, or downstream recognition/full-pipeline handoff.
- Verify no SaaS/no raw/no sensitive/no full-screen/no-authority.
- Verify shadow mode and productive OCR remain blocked.
- Recommend the next block: ONNX Runtime version experiment, detector model replacement, session options fix, preprocessing fix, renderer fix, postprocessing fix, or downstream recognition/full-pipeline isolation.

Primary evidence:

- Report: `docs/reports/detector-runtime-compatibility-experiment-m225.md`
- Summary: `artifacts/ocr-vision-onnx/m225/detector-runtime-compatibility-experiment-summary.json`
- ADR: `docs/adr/detector-runtime-compatibility-decision-m223-m225.md`
