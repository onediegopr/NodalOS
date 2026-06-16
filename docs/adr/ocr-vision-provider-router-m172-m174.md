# ADR: OCR/Vision Provider Router M172-M174

## Decision

NODAL OS defines OCR/Vision as a layered, provider-based architecture. This milestone is model-only and fixture-first: no OCR real, no SaaS call, no API key, no Python worker, no Tesseract/PaddleOCR runtime dependency.

OCR/Vision can observe and produce redacted evidence, but it does not authorize actions. Core decides, Policy blocks, Evidence records, and Human approval is required when confidence, sensitivity or risk demands it.

## Layered Architecture

- Layer 0: DOM/CDP/UIA first.
- Layer 1: screenshot grounding without OCR: hash, diff, debug and evidence.
- Layer 2: local/open-source OCR for easy redacted crops.
- Layer 3: specialized OCR SaaS for future complex documents.
- Layer 4: VLM multimodal for future difficult handwriting/layout cases.
- Layer 5: human-in-the-loop for low confidence, sensitive or risky cases.

## Why DOM/CDP/UIA First

DOM/CDP/UIA metadata is structured, policy-checkable and easier to redact. OCR is a fallback for perception gaps, not the primary source of truth.

## Why Screenshot Grounding Before OCR

Grounding snapshots already provide page health, hashes, focused element, visible interactables and redaction status. OCR is only considered after grounding verifies safe/redacted crop metadata.

## Provider Registry

The registry models provider kind, status, capabilities, cost, performance, privacy and policy:

- local PaddleOCR stub as future main open-source worker candidate.
- local Tesseract stub as lightweight fallback.
- cloud document AI providers as future configured providers.
- OpenAI vision/VLM as future disabled-by-default candidate.
- human review as fallback.

SaaS disabled-by-default is a hard rule: SaaS providers remain disabled-by-default, require opt-in/config/API-key state and do not store real API keys.

## Local OCR Boundary

PaddleOCR/Tesseract are stubs only in this milestone. Future local OCR should run in a separate worker/process/container/CLI and exchange JSON contracts with Core. Core .NET must not be tightly coupled to heavy Python dependencies.

## Routing Policy

The router considers:

- case classification.
- complexity and quality.
- sensitivity and redaction status.
- crop vs full-screen.
- cost/budget.
- expected confidence and latency.
- provider capability and policy.

Rules:

- DOM/CDP/UIA sufficient => `NoOcrNeeded`.
- screenshot hash/diff only => `NoOcrNeeded`.
- simple redacted crop => local PaddleOCR stub preferred.
- Paddle unavailable => Tesseract stub fallback.
- complex layout/form/invoice => cloud candidate but disabled-by-default.
- handwriting/VLM cases => ask human unless a future configured provider is explicitly approved.
- sensitive surfaces => local-only or human review; cloud blocked.
- redaction failed => blocked.
- budget exceeded => blocked or human review.

## Crops Over Full-Screen

Full-screen OCR is blocked by default. Future OCR must prefer small redacted crops associated with a grounding snapshot/step.

## No-Authority Rule

OCR/Vision is no-authority by design.

OCR/Vision output cannot approve, click, submit, pay, sign, delete, bypass login/CAPTCHA/2FA or unblock sensitive surfaces. It is evidence/debug only until Core and policy decide.

## Non-Goals

- No OCR real execution.
- No SaaS OCR call.
- No API keys.
- No dependency on PaddleOCR, Tesseract or Python binaries.
- No full-screen OCR default.
- No production recorder/replay.
- No scope expansion.
