# OCR/Vision Synthetic Worker Run M183

Run: ocr-synthetic-worker-run-m183
Total fixtures: 4
Completed fixtures: 3
Blocked fixtures: 1
No real OCR executed: true
No SaaS OCR executed: true
No external process invoked: true
No raw persistence: true
No-authority: true

| Request | Decision | Raw persisted | Real OCR | External process |
| --- | --- | --- | --- | --- |
| synthetic-worker-clean-crop | SyntheticOnly | False | False | False |
| synthetic-worker-redacted-credential-crop | SyntheticOnly | False | False | False |
| synthetic-worker-blocked-sensitive-crop | FailedHealthCheck | False | False | False |
| synthetic-worker-low-confidence-crop | AvailableForSynthetic | False | False | False |