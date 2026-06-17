# Claude Audit Prompt - M252 Recognizer Dictionary Pair Replacement Selection

Audit NODAL OS M250-M252.

Verify:

- Candidate sources and provenance.
- RapidOCR/ModelScope `default_models.yaml` evidence for ONNX recognizer URLs and dictionary URLs.
- Model/dictionary explicit pairing.
- Dictionary token counts and CTC blank index.
- Hash/size pinnability.
- Why `en_PP-OCRv5_rec_mobile.onnx + ppocrv5_en_dict.txt` is selected for controlled acquisition.
- Why current PP-OCRv4 English pair remains rejected.
- Why Latin PP-OCRv5 requires manual review despite being explicit.
- That no dictionary/token/hash was invented.
- That no new model was downloaded and no decode was attempted.
- That no raw/no sensitive/no full-screen/no SaaS/no-authority were preserved.

Recommend whether M253-M255 should proceed to controlled acquisition and runtime verification of the selected PP-OCRv5 English pair.
