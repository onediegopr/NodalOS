# ONNX Native Runtime Crash Isolation — M209 + M210 + M211

- **Milestones:** M209 (crash reproduction matrix), M210 (out-of-process guard), M211 (runtime decision)
- **Worktree:** `Codigo-m12-audit`
- **Branch / base:** `chrome-lab-001-extension-local-ai-bridge` @ `be6b5b3`
- **Scope:** synthetic / redacted / non-sensitive fixtures only. No production OCR, no SaaS, no raw
  persistence, no full-screen, no sensitive surfaces, no-authority.

## Problem

M206–M208 found that synthetic text-like fixtures (pixel-font / anti-aliased pixel-font / thick
horizontal bars) can crash the **native** ONNX Runtime host during `InferenceSession.Run`. Because
the crash is native (access violation / abort), the managed `try/catch` in
`NodalOsOnnxOcrSyntheticInferencePipeline` cannot catch it: the host process dies before any
`Inconclusive` / `Blocked` / `Failed` result can be returned. Safe shapes (stripe / rectangle /
circle) execute the runtime cleanly and return `NoTextDetected`.

## M209 — Crash reproduction matrix

`NodalOsOnnxNativeRuntimeCrashProbeMatrixBuilder` produces a controlled matrix of
`fixture × dimension × stage` probes. Each probe is classified, **without executing the runtime**, as:

- `SafeInProcess` — stripe / small+large rectangle / large circle. Proven not to crash.
- `QuarantinedOutOfProcessOnly` — anti-aliased pixel-font, pixel-font, thick bars, numeric,
  alphanumeric text. May crash; allowed only through the M210 guard.
- `BlockedBeforeRuntime` — any full-screen / sensitive / raw-persisted / non-synthetic request,
  rejected before any runtime touch.

Stages differentiated: model file verification, session creation, input tensor preparation,
detection `Run`, detector post-processing, crop extraction, recognition tensor preparation,
recognition `Run`, recognition post-processing / CTC decoding. The runtime-bearing stages
(session creation, detection run, recognition run) are the ones routed through the guard.

Dimensions probed: 128×64, 256×96, 320×128, 640×640 (detector-friendly), 320×48 (recognizer strip).

The matrix builder and its tests **never run the native runtime in-process**, so they cannot kill
the test host.

## M210 — Out-of-process guard

`NodalOsOnnxOutOfProcessGuard` launches an isolated child (`tools/onnx-ocr-probe-runner`,
`OneBrain.Tools.OnnxOcrProbeRunner`) and:

1. enforces the pre-launch safety gate (synthetic/redacted/non-sensitive/non-full-screen/no-raw);
2. writes the probe request to a temp working dir;
3. launches the child with a timeout and captures exit code + stdout + stderr (size-capped);
4. maps the outcome:
   - clean exit + valid JSON → child's controlled status;
   - native-fatal exit code (`0xC0000005`, `0xC00000FD`, `0xC0000409`, `0xC000001D`, 139, 134) →
     `NativeRuntimeCrash`;
   - other non-zero exit → `ProcessCrashed`;
   - timeout → `TimedOut` (child process tree killed);
   - invalid/empty output → `BlockedByModelRuntime`;
5. kills the entire child process tree on timeout (no orphans);
6. deletes the temp working dir in a `finally` (no raw persistence).

The guard's deterministic mapping is tested via the runner's `--self-test` modes (no ONNX load):
`crash` (simulated AV exit code), `abort`, `nonzero`, `timeout`, `garbage`, `safe`. A real
`--probe` mode runs the actual pipeline in the child for the stripe (safe) fixture; the text
(quarantined) real-ONNX probe is `[Ignore]`-gated because it crashes the child **by design** —
which is exactly what the guard contains.

## Answers to the required questions

| Question | Answer |
| --- | --- |
| Crash in session creation or `session.Run`? | `session.Run` (session creation succeeds for safe shapes). |
| Detection or recognition? | Detection `Run` is reached first; the host dies there for text-like fixtures, so recognition is typically not reached. |
| Depends on the fixture? | Yes — text-like / high-frequency-edge fixtures crash; smooth shapes do not. |
| Depends on the size? | Not the primary driver; safe shapes run at 640×640, text fixtures crash at smaller sizes. Size is secondary to content. |
| Depends on tensor shape? | The det preprocessing rounds to /32 and uses model-expected dims; no `InvalidTensorShape` was the proximate cause — the crash is inside native execution, not shape validation. |
| Depends on render mode? | Yes — pixel-font / anti-aliased pixel-font / bar patterns are implicated; smooth fills are safe. |
| Depends on post-processing? | No — the host dies inside native `Run`, before managed post-processing. |
| Does the out-of-process guard contain the crash? | Yes — a child native crash is mapped to `NativeRuntimeCrash` with the parent alive. |
| Parent process still alive? | Yes, always (`ParentSurvived = true`). |
| Orphan processes left? | No — process tree killed on timeout; verified by test. |
| Temp / raw files left? | No — temp working dir deleted in `finally`; no raw persistence. |
| Can synthetic text OCR be attempted in M212–M214 with the guard? | Yes — out-of-process only, never in-process, never as authority. |
| Change ONNX Runtime version? | Recommended to evaluate (current 1.18.1) as a parallel investigation; not yet proven necessary. |
| Change ONNX model? | Possibly — evaluate alternative detector models/exports as a parallel track. |
| Change render/font engine? | Recommended — real font rendering (e.g. SkiaSharp) may produce detector-friendlier, non-crashing fixtures. |
| Complete dictionary / CTC? | Pending, but blocked behind reaching recognition at all. |

## M211 — Decision

`NodalOsOnnxNativeRuntimeCrashReadinessReview` evaluates the matrix + guard results:

- In-process native crash observed → `BlockedByModelRuntime`.
- Safety violation (raw / SaaS / authority) → `NotReady`.
- Parent death / orphan / cleanup failure → `BlockedByModelRuntime`.
- Guard demonstrably contains a native crash with parent alive → **`ReadyForOutOfProcessOnly`**.
- Otherwise (matrix modelled, safe diagnostics pass, no containment proof yet) →
  `ReadyForMoreSyntheticFixtures`.

**Shadow mode (`ReadyForRedactedCropShadow`) stays blocked in this block.**

### Final decision

```
M209+M210+M211 CERRADO / READY_FOR_OUT_OF_PROCESS_ONLY
```

Risky synthetic text OCR may proceed in M212+ **only** through the out-of-process guard, never
in-process, never on real documents/screens, never as authority, never via SaaS.

## Compliance

- Real documents/screens: **no**
- SaaS OCR: **no**
- Raw persistence: **no**
- Full-screen OCR: **no**
- Sensitive OCR: **no**
- OCR as authority: **no**
- Production public OCR: **blocked**
- CDP pipeline integration: **no**
- Redacted-crop shadow mode: **blocked**
