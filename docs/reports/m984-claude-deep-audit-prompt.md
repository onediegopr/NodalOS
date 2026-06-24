# M984 — Claude Deep Audit Prompt (copy-paste)

**Mode:** audit-only / intake-only · **Base commit:** `02ceb0745e531b8e50604a8ef9a0a7b395d9697e` · **Branch:** `chrome-lab-001-extension-local-ai-bridge`

> Copy the block below to request the Claude deep audit. **PEDIR AUDITORIA CLAUDE.**

---

You are an external senior technical auditor. Project: **NODAL OS**. Audit the simulated/no-op foundation line **M933–M980** at commit `02ceb0745e531b8e50604a8ef9a0a7b395d9697e`, branch `chrome-lab-001-extension-local-ai-bridge`, worktree `C:\DESARROLLO\NodalOS\Codigo-m12-audit`.

This line is **audit-only / protocol-only / foundation/test-only / harness-prep only**. Nothing here is runtime-real. Do **not** assume or endorse: manual QA execution, host real interactive smoke, PC Commander real, productive runtime, shell, filesystem write/scan real, browser automation productive, provider/cloud, network real, process kill, credential access, capability unlock, PRODUCTIVE_ENABLED, public release, Chrome Web Store, signed public ZIP, product files modification, Bridge/CSP modification. NODRIX is FROZEN / out of scope (historical-guardrail reference only).

Scope to audit: Safe No-Op Runner; Metadata Read Runner; Local Operator Evidence Bridge; Controlled No-Op Runtime Adapter; Local Operator QA Prep; Local Host Visible No-Op Smoke Plan; Manual QA Evidence Protocol; Harness Prep; Human Evidence Capture Gate; Redaction/Leak Guard; Manual QA Trigger Gate; BrowserRuntimeSmoke external caveat handling.

Review with **high severity** for: scope drift; internal contradictions; ambiguous wording; tautological tests; weak assertions; unrealistic fake payloads; false/insufficient no-side-effect proof; metadata fixture confusable with real read; harness prep confusable with real execution; manual QA protocol confusable with manual QA passed; human evidence gate confusable with QA ready; insufficiently blocked dangerous commands; superficial redaction/leak guard; product files / Bridge/CSP drift; hidden or misrepresented BrowserRuntimeSmoke caveat; accidental release/store unlock; accidental provider/cloud unlock; accidental filesystem/browser/capability unlock; NODRIX scope drift.

Known caveat you must NOT treat as clean: `OPEN_BROWSER_RUNTIME_SMOKE_CLEANUP_EXTERNAL_QUARANTINED_VISIBLE`. Full-suite confidence is reported at 95%; do not endorse 100% while any skip/inconclusive/caveat exists.

Return a structured result:

- **Decision (one of):** `AUDIT_GO` / `AUDIT_CONDITIONAL_GO` / `AUDIT_NO_GO`.
- **Overall summary.**
- **Findings**, each classified `BLOCKER` / `HIGH` / `MEDIUM` / `LOW` / `NIT`, with: `finding_id`, `severity`, `affected_files`, `evidence`, `why_it_matters`, `exploit_or_confusion_scenario`, `recommended_remediation`, `blocks_manual_qa` (yes/no), `blocks_next_milestone` (yes/no), `blocks_runtime_real` (yes/no), `blocks_release_store` (yes/no).

Treat as **BLOCKER**: any accidental real unlock (shell/filesystem/network/provider/browser/credential/process), "manual QA ready" without evidence, PC Commander real claim, product files / Bridge/CSP drift, release/store claim, hidden caveat, or tautological tests on a safety-critical path. Do not declare manual QA ready, runtime ready, or release ready under any circumstances — those remain externally gated.

---

## What to do with the result
- **AUDIT_GO** → may continue only with the next docs/protocol block; manual QA execution stays NO-GO until operator confirmation.
- **AUDIT_CONDITIONAL_GO** → remediate all BLOCKER and HIGH-safety findings before any QA-real consideration.
- **AUDIT_NO_GO** → freeze the line until remediation.
