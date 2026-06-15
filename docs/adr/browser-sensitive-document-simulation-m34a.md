# ADR: Browser Sensitive Document Simulation M34A

## Status

Accepted.

## Context

M26 implemented safe download and M27 implemented safe upload. M32 defined sensitive site policy. M34A combines those capabilities in a local/sandbox sensitive document simulation using synthetic documents only.

This is not a real AFIP, banking, ERP, fiscal, financial, or government document flow. It is a safety proof that document operations remain policy-gated, approval-gated, redacted, and auditable before any real sensitive pilot is considered.

## Decision

Add a sensitive document simulation layer with:

- local synthetic fixture metadata;
- synthetic document names only: `synthetic-sensitive-simulation.pdf`, `synthetic-tax-summary.json`, and `synthetic-erp-export.csv`;
- `BrowserSensitiveDocumentSimulationRequest`, policy, result, and evidence contracts;
- `BrowserSensitiveDocumentSimulationRunner`;
- M32 policy evaluation for `DownloadDocument` and `UploadDocument`;
- single approval requirement for synthetic sensitive download/upload;
- SafeDownload and SafeUpload integration;
- hash/MIME/size/evidence validation;
- quarantine for download;
- controlled root and approval for upload;
- double approval and explicit typed confirmation modeled for future critical actions;
- document content capture blocked.

## What Is Simulated

- Sensitive document download from a local fixture.
- Sensitive document upload to a local controlled endpoint.
- Approval refs for download and upload.
- Final status verification.
- Audit summary with category, action, hash, MIME, size, policy decision, and reason refs.

## What Is Never Persisted

- Document content.
- Cookies.
- Session values.
- Request bodies.
- Response bodies.
- Sensitive header values.
- Secret values.
- Full sensitive local paths.

## Critical Action Model

In M34A:

- synthetic sensitive download requires single approval and safe download;
- synthetic sensitive upload requires single approval and safe upload;
- submit, pay, sign, delete, publish, and approve remain prohibited;
- double approval and explicit typed confirmation are modeled but not activated for irreversible execution.

## Done Criteria

The simulation can close as Done only when:

- M32 allows both document actions;
- approval refs exist;
- phase gate passed;
- safe download is verified with quarantine/hash/MIME/size;
- safe upload is verified with controlled root/approval/hash/MIME/size;
- final semantic proof exists;
- audit/evidence are redacted;
- document content capture is false.

## Future Work

Before M34B or any real sensitive document flow:

- real target ownership and compliance approval are required;
- real document data handling policy must be reviewed;
- document content minimization must be independently audited;
- irreversible submit/sign/pay/delete remain blocked until a dedicated milestone.
