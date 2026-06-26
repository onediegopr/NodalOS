# Next Prompt: WCU UIA Read-Only Adapter Design

Mode: DESIGN-ONLY / NO IMPLEMENTATION / NO LIVE ACTIONS.

Do not execute UIA.
Do not use FlaUI to control real apps.
Do not move mouse.
Do not send keyboard.
Do not read clipboard.
Do not persist screenshots.
Do not process UAC/admin/credentials/destructive operations through any control path.

Objective: design a future read-only adapter that can map live UIA metadata into `ComputerUseSnapshot` without executing actions.

Expected output: design doc only, threat delta, test plan, and no-live gates. No code.
