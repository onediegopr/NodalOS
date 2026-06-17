# NODAL OS M255 - PP-OCRv5 Recognizer Pair Acquisition

## Decision

`M253+M254+M255 PARCIAL`

## Summary

The selected PP-OCRv5 English recognizer+dictionary pair was downloaded and verified through controlled scripts. The recognizer runtime smoke ran successfully out-of-process with ONNX Runtime `1.22.1`, but the model output class count was `438`, not the expected `437`.

No decode was attempted. No productive OCR or shadow mode was enabled.

## Acquisition

- Model URL: `https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.8.0/onnx/PP-OCRv5/rec/en_PP-OCRv5_rec_mobile.onnx`
- Model SHA-256: `c3461add59bb4323ecba96a492ab75e06dda42467c9e3d0c18db5d1d21924be8`
- Model size: `7872351`
- Dictionary URL: `https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.8.0/paddle/PP-OCRv5/rec/en_PP-OCRv5_rec_mobile/ppocrv5_en_dict.txt`
- Dictionary SHA-256: `e025a66d31f327ba0c232e03f407ae8d105e1e709e7ccb3f408aa778c24e70d6`
- Dictionary size: `1416`
- Dictionary tokens: `436`
- CTC blank index: `0`
- Expected classes from dictionary+blank: `437`

## Runtime Smoke

Out-of-process recognizer smoke used synthetic tensors only:

- `Zero`: run succeeded, output `fetch_name_0=[1,40,438]`.
- `Ones`: run succeeded, output `fetch_name_0=[1,40,438]`.
- `Gradient`: run succeeded, output `fetch_name_0=[1,40,438]`.
- `SyntheticTextCrop`: run succeeded, output `fetch_name_0=[1,40,438]`.
- `HighContrastManualCrop`: run succeeded, output `fetch_name_0=[1,40,438]`.

The runtime itself is not blocked, but the candidate pair is not decode-ready because output `438` is not explained by `436` dictionary tokens plus CTC blank index `0`.

## Safety

- No decode attempted.
- No OCR productivo.
- No SaaS OCR.
- No raw persistence.
- No full-screen or sensitive OCR.
- No-authority preserved.
- Candidate model and dictionary are gitignored and untracked.

## Next Gate

The next block should reconcile PP-OCRv5 class semantics (`438`) against the explicit dictionary (`436`) before any CTC decode fixture is attempted.
