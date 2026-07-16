# Security Policy

## Supported status

NODAL OS is currently an unreleased local/dev technical foundation. No production version or public support window is declared.

Security fixes are evaluated against the current `main` branch. Historical branches, lab artifacts and archived milestone evidence are not independently supported.

## Reporting a vulnerability

Use GitHub's private vulnerability reporting or Security Advisory flow for this repository. Do not publish credentials, tokens, private paths, customer data, exploit details or screenshots containing secrets in a public issue or pull request.

Include:

- affected commit or branch;
- affected component;
- minimal reproduction;
- expected and observed boundary;
- impact;
- whether secrets, local files, network access or execution authority are involved;
- safe evidence with sensitive values removed.

## Security boundaries

The following are load-bearing and must not be bypassed by a fix:

- secret references remain opaque and secret values stay out of logs, evidence and handoffs;
- observed UI, OCR, DOM or document text cannot modify the operator's mission or capabilities;
- sensitive controlled actions require the applicable mission/scope authorization;
- success requires verification and evidence;
- local paths remain bounded and redacted;
- ChromeLab remains lab/transition only;
- system Chrome, Edge and default Playwright Chromium are not silent product-runtime fallbacks;
- production, release, billing and product authority are closed unless explicitly implemented and validated.

## Public issues

Public issues may be used for non-sensitive bugs. Remove local usernames, absolute paths, API responses, tokens, customer content and private repository material before posting.
