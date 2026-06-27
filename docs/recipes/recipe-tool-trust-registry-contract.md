# Recipe Tool Trust Registry Contract

Phase: 5/9 - Tool Trust Registry + Secrets by Reference.

`RecipeToolTrustRegistry` records which tools may be referenced by recipes. It is fixture-safe and passive: it does not load providers, call APIs, start connectors, launch browsers, or run desktop automation.

## Tool Entries

`RecipeToolTrustEntry` includes:

- tool id and display name,
- provider/system family,
- category,
- supported capability refs,
- trust level,
- runtime status,
- allowed run modes,
- allowed and blocked action categories,
- permission scopes,
- required secret refs,
- approval, evidence, and redaction policy refs,
- owner/contact refs,
- version and notes refs.

## Default Safety

Default tool trust is candidate/untrusted. Browser and desktop runtime categories remain live-blocked. Connector entries are reference-only, fixture-only, or future-gated until a future policy explicitly allows more.

Unknown, disabled, deprecated, untrusted, browser live, desktop live, and live connector states block readiness.

## Safety Boundary

OpenRPA/OpenCore remains inspiration only. No dependency, code copy, XAML import, browser extension/native messaging, browser automation, desktop automation, CDP, Playwright, Selenium, Puppeteer, connector execution, scheduler, file watcher, OS hook, network/API call, vault implementation, raw screenshot/HAR/DOM/accessibility capture, or live runtime was added.
