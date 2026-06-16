# OCR/Vision Controlled Activation M178-M180

Date: 2026-06-16

Product: NODAL OS

Decision: keep OCR/Vision model-only and fixture-first now. Define the future activation path, local worker boundary, and real activation gate without enabling real OCR, SaaS OCR, secrets, external calls, or action authority.

## Current State

- OCR/Vision provider registry exists.
- PaddleOCR and Tesseract are stubs only.
- SaaS providers are model-only stubs and disabled-by-default.
- OCR/Vision evaluation harness passed synthetic fixtures.
- No real OCR is activated.
- No SaaS OCR is activated.
- No API keys are stored.
- OCR/Vision remains no-authority.

## Why Real OCR Is Not Activated Yet

- No local worker is installed or audited.
- No PaddleOCR/Tesseract runtime is installed.
- No Python worker boundary has been implemented.
- No redacted-crop shadow run has been audited.
- No opt-in, privacy, budget, rollback, or activation audit exists for real OCR.
- SaaS OCR would require external transfer, secrets, budget, opt-in, privacy review, and audit evidence.

## Layered Architecture

1. DOM/CDP/UIA first.
2. Screenshot grounding without OCR for hash/diff/debug/evidence.
3. Local OCR for redacted crops after opt-in and audit.
4. SaaS OCR for approved documents only after explicit opt-in, secret vault, privacy, budget, and audit.
5. VLM for complex layout/handwriting/ambiguous visual cases, disabled-by-default.
6. Human-in-the-loop when confidence, sensitivity, or policy requires it.

## Activation States

- `ModelOnly`: contracts, stubs, routing and evaluation only.
- `ShadowEvaluation`: compare routing/evidence without executing real OCR.
- `LocalWorkerAvailable`: future worker is installed, disabled or available under policy.
- `LocalWorkerEnabledForSynthetic`: future worker can process synthetic fixtures only.
- `LocalWorkerEnabledForRedactedCrops`: future worker can shadow process redacted crops.
- `SaasProviderConfigured`: provider has non-secret configuration state only.
- `SaasProviderShadowOnly`: future SaaS comparison state, no authority.
- `SaasProviderEnabledForApprovedDocs`: future audited state for approved documents only.
- `BlockedByPolicy`: default current state for real OCR.
- `BlockedByPrivacy`: privacy review missing or failed.
- `BlockedByBudget`: budget missing or exceeded.
- `BlockedByMissingAudit`: audit/evaluation/rollback evidence missing.

## Local OCR Criteria

Local OCR real activation requires:

- explicit provider enablement
- explicit opt-in
- local worker installed and available
- redaction gate passed
- only small redacted crops by default
- full-screen OCR blocked unless separately approved
- sensitive surfaces blocked by default
- privacy profile accepted
- budget configured
- evaluation harness passed
- activation audit evidence present
- human escalation policy configured
- rollback/pause configured
- no-authority confirmed

PaddleOCR is the future primary local worker candidate because it is stronger for modern printed text, UI crops, bounding boxes and multilingual cases. Tesseract is the future lightweight fallback for simple printed text when PaddleOCR is unavailable or too heavy.

## SaaS OCR Criteria

SaaS OCR remains disabled-by-default. Activation would require:

- explicit provider opt-in
- secret vault integration
- budget and cost cap
- privacy and data transfer acceptance
- redaction policy passed
- no sensitive/default-full-screen processing
- audit evidence
- provider-specific rollback/pause
- human escalation

No SaaS provider can be enabled by an admin toggle alone.

## VLM Criteria

OpenAI Vision/VLM and comparable providers remain disabled-by-default. They are future candidates for complex layout, handwriting, low quality images and ambiguous UI understanding after the same opt-in, privacy, budget, redaction and audit gates.

## Worker Boundary

The future local worker must be decoupled from Core .NET:

- process, container, CLI or local loopback service
- JSON request/response contract
- input: redacted crop/document metadata only
- output: redacted text blocks, bounding boxes, confidence and warnings
- no secrets
- no full-screen default
- no persistent raw images unless separately approved
- no action authority

## Policy

- Crops over full-screen.
- Redaction required before OCR.
- Sensitive surfaces block by default.
- OCR/Vision can observe, but cannot approve, click, submit, sign, pay, delete or enter credentials.
- Core decides.
- Policy blocks.
- Evidence records.
- Human approval can unblock only approved local/safe review states.

## Audit Requirements

Before any real OCR activation, NODAL OS needs:

- activation-specific audit pack
- local worker installation and version evidence
- redaction test evidence
- synthetic-only run evidence
- crop-shadow run evidence
- privacy/budget/rollback evidence
- no-authority verification
- skipped/category and release gate consistency

## Decision

M178-M180 defines controlled activation architecture only. OCR real remains disabled. SaaS OCR remains disabled. PaddleOCR/Tesseract worker runtime is not installed. No API keys are used. NODAL OS scope does not expand.
