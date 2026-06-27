# Recipe Locator Repair Studio Contract

Phase: 7/9 - Recipe Lab + Locator Repair Studio.

Locator Repair Studio represents locator candidates, confidence, drift, repair suggestions and replay eligibility by reference only.

## Contracts

- `RecipeLocatorStudioSnapshot`
- `RecipeLocatorCandidate`
- `RecipeLocatorDriftReport`
- `RecipeLocatorRepairSuggestion`
- `RecipeLocatorRepairDecision`
- `RecipeLocatorReplayEligibility`
- `RecipeLocatorSafetyStatus`

## Strategy Order

- KnownTarget
- StableSelector
- DomOrAccessibility
- VisibleText
- SemanticTarget
- VisualAnchor
- RelativeCoordinate
- AIFallback
- HumanHandoff
- Abort

## Safety

- Locator confidence is not authorization.
- Locator repair suggestions cannot apply live.
- Locator replay is fixture/preview/manual-review only or blocked.
- BrowserLiveAction and DesktopLiveAction remain blocked.
- Broken or ambiguous locators map to human review or blocked states.
