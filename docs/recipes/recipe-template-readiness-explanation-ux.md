# Recipe Template Readiness Explanation UX

Product-surface phase: 2/4.

The readiness explanation layer translates composite Recipe Template readiness into operator-visible, read-only status.

## Questions Answered

- Is this template previewable?
- Is this template fixture-ready?
- Why is it blocked?
- What is missing?
- What requires human review?
- Which actions are sensitive?
- Which tools and secret refs would be needed in a future gated path?
- What evidence and validation refs are required?
- What trigger state applies?
- What locator/capture safety applies?
- What run modes are blocked?
- What is the safe next action?
- What is explicitly not included?

## Blocking Categories

The UX can classify blocked states as missing limits, validation, evidence, approval path, human path, tool trust, secret reference, raw secret marker, live-blocked tool, connector execution blocked, browser runtime blocked, desktop runtime blocked, trigger autorun blocked, recorder/playback blocked, capture blocked, locator repair apply blocked, sensitive action review required, fiscal submission blocked, payment execution blocked, marketplace mutation blocked, message delivery blocked, delete/write blocked, unknown system blocked, or unknown unsafe.

## Secret Handling

Secret requirements are shown by alias/id only. Raw values are never shown. Any raw secret marker is blocking.

## Future Enablement Notes

Future-gated notes are explicitly not enabled now. They describe what would need separate policy, trust, approval, and runtime work in a future line; they are not product authorization.
