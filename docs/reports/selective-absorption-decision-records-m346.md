# Selective Absorption Decision Records M346

## Summary

This block records selective absorption decisions for BotBoard, Axiom and Robomotion without introducing runtime surface, new UI or orchestration code.

## What We Take

### From BotBoard

- agent-native workboard concept
- explicit task ownership
- blockers and progress notes
- handoff bundles
- verification before task completion

### From Axiom

- Run Report V1 direction
- Recipe Manifest / Automation JSON V1 direction
- failure taxonomy direction
- troubleshooting mapper direction
- step library and orchestration API as future roadmap

### From Robomotion

- package and skill manifest thinking
- internal skill registry direction
- worker boundary contract direction
- connector lifecycle and provenance direction

## What We Intentionally Did Not Implement

- no full workboard UI
- no orchestration API
- no scheduling
- no package registry
- no multi-worker runtime
- no cloud runtime
- no captcha solving
- no bot bypassing
- no public marketplace

## Recommended Next Milestones

- M347-M349 Mission / Task Domain Model
- M350-M352 Failure Taxonomy + Run Report V1
- M353-M355 Recipe Manifest V1

## Estimated Progress

Estimated progress for the Agent Operations / selective absorption platform layer after this block: `20%`.

This block closes architectural intent and governance boundaries, but not the domain model or execution surfaces.
