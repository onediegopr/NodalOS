# OCR/Vision Evaluation M177

Total cases: 15
Passed cases: 15
No real OCR executed: true
No SaaS OCR executed: true
No-authority: true

| Fixture | Status | Reason | Provider | Passed |
| --- | --- | --- | --- | --- |
| simple-ui-crop | ProviderSelected | LocalPaddlePreferred | local-paddleocr-stub | True |
| simple-document-text | ProviderSelected | LocalPaddlePreferred | local-paddleocr-stub | True |
| low-quality-crop | AskHuman | VlmCandidateDisabled | - | True |
| blurred-crop | AskHuman | VlmCandidateDisabled | - | True |
| skewed-crop | AskHuman | AskHuman | - | True |
| table-layout | CloudDisabled | CloudCandidateDisabled | - | True |
| invoice-layout | CloudDisabled | CloudCandidateDisabled | - | True |
| receipt-layout | CloudDisabled | CloudCandidateDisabled | - | True |
| handwriting-synthetic | AskHuman | VlmCandidateDisabled | - | True |
| mixed-handwriting-synthetic | AskHuman | VlmCandidateDisabled | - | True |
| screenshot-ui-ambiguous | AskHuman | AskHuman | - | True |
| sensitive-redaction-failed | Blocked | RedactionFailedBlocked | - | True |
| full-screen-blocked | Blocked | NoProviderAllowed | - | True |
| cloud-candidate-disabled | CloudDisabled | CloudCandidateDisabled | - | True |
| budget-exceeded | BlockedByBudget | BudgetExceeded | - | True |