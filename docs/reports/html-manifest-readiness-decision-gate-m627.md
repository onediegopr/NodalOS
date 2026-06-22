# M627 - HTML / Manifest Readiness Decision Gate

Decision target: `INSTALLED_EXTENSION_QA_EVIDENCE_GATE_READY`.

## Scope

M627 is audit-only. It defines when future HTML or manifest work may be considered.

Current state:

- `readyForHtmlMinimumPatch=false`
- `readyForManifestNamingCleanup=false`
- `readyForJsChanges=false`
- `readyForRuntimeChanges=false`
- `readyForProductiveConsent=false`
- `readyForProviderCloud=false`
- `readyForFilesystem=false`
- `requiresManualQaEvidence=true`
- `requiresClaudeBeforeJs=true`
- `requiresExtensionReviewBeforeManifest=true`

## Future HTML Minimum Patch Gate

Future HTML minimum patch can only become a candidate if manual evidence shows:

- `manualQaCompleted=true`
- `extensionLoaded=pass`
- `sidepanelOpened=pass`
- `consoleCriticalErrors=pass`
- `tabActiveContrast=pass`
- `stopResponsive=pass`
- `focusKeyboard=pass` or acceptable
- `runtimeLooksBlocked=pass`
- `providerLooksInactive=pass`
- `consentLooksGovernance=pass`
- `logsLookAuxiliary=pass`

Allowed future HTML scope:

- Correct mojibake in visible copy.
- Review provider display text only if separately approved.
- Adjust non-functional microcopy.
- No JS.
- No manifest.
- No permissions.
- No runtime wiring.

## Future Manifest / Naming Gate

Future manifest/naming cleanup can only become a candidate if:

- A separate milestone exists.
- Dedicated extension review exists.
- Legacy naming inventory exists.
- No permission changes are included.
- No runtime, model, or capability changes are included.
- Explicit user approval exists.
- Rollback plan exists.

## JS Decision

JS remains NO-GO.

## Safety

No CSS, HTML, JS, or manifest changes were made.

No runtime, provider/cloud integration, filesystem feature, productive consent, capability enablement, or source-of-truth promotion was introduced.
