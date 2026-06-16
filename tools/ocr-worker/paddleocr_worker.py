#!/usr/bin/env python3
"""
NODAL OS — PaddleOCR local worker entrypoint.
Production-grade, crop-only, redacted-only, local-only.
No secrets. No SaaS. No raw persistence. No authority.
Contract version: nodal-paddleocr-worker.v1
"""
import argparse
import base64
import json
import os
import sys
import tempfile
import traceback
from pathlib import Path

CONTRACT_VERSION = "nodal-paddleocr-worker.v1"


def redact_log(value: str) -> str:
    """Keep logs redacted; never echo raw request payloads."""
    return f"[REDACTED:{len(value)} chars]"


def ensure_paddleocr():
    try:
        from paddleocr import PaddleOCR
        return PaddleOCR
    except Exception as exc:
        raise RuntimeError(f"PaddleOCR not available: {exc}") from exc


def run_ocr(request: dict) -> dict:
    if request.get("contractVersion") != CONTRACT_VERSION:
        return error_response("contract version mismatch", "version_mismatch")

    if not request.get("authToken"):
        return error_response("missing auth token", "auth_missing")

    if request.get("allowRawPersistence") is True:
        return error_response("raw persistence not allowed", "raw_persistence")

    if request.get("allowFullScreen") is True:
        return error_response("full-screen OCR not allowed", "full_screen")

    if request.get("sensitivity", "").lower() in ("sensitive", "credentials", "personaldat"):
        return error_response("sensitive content blocked", "sensitive")

    image_b64 = request.get("imageBase64", "")
    if not image_b64:
        return error_response("missing image", "missing_image")

    image_bytes = base64.b64decode(image_b64)
    if len(image_bytes) > request.get("maxImageBytes", 2 * 1024 * 1024):
        return error_response("image too large", "oversize")

    PaddleOCR = ensure_paddleocr()
    ocr = PaddleOCR(
        use_angle_cls=True,
        lang=request.get("language", "en"),
        show_log=False,
        use_gpu=False,
    )

    # Write only the redacted crop to a controlled temp file; delete immediately.
    tmp_dir = Path(__file__).parent / "tmp"
    tmp_dir.mkdir(exist_ok=True)
    tmp_path = tmp_dir / f"redacted-crop-{os.urandom(4).hex()}.png"
    try:
        tmp_path.write_bytes(image_bytes)
        result = ocr.ocr(str(tmp_path), cls=True)
        lines = []
        if result and result[0]:
            for line in result[0]:
                if line:
                    bbox, (text, confidence) = line
                    lines.append({
                        "text": text,
                        "confidence": float(confidence),
                        "bbox": bbox,
                    })
        return ok_response(lines)
    finally:
        try:
            tmp_path.unlink(missing_ok=True)
        except Exception:
            pass


def ok_response(lines: list) -> dict:
    return {
        "status": "ok",
        "contractVersion": CONTRACT_VERSION,
        "lines": lines,
        "lineCount": len(lines),
        "noAuthority": True,
        "redacted": True,
        "rawPersisted": False,
        "callsRealOcr": True,
        "callsSaas": False,
    }


def error_response(reason: str, code: str) -> dict:
    return {
        "status": "error",
        "contractVersion": CONTRACT_VERSION,
        "errorCode": code,
        "errorReason": reason,
        "noAuthority": True,
        "redacted": True,
        "rawPersisted": False,
        "callsRealOcr": False,
        "callsSaas": False,
    }


def main():
    parser = argparse.ArgumentParser(description="NODAL OS PaddleOCR local worker")
    parser.add_argument("--request-base64", required=True, help="Base64-encoded JSON request")
    args = parser.parse_args()

    try:
        request_json = base64.b64decode(args.request_base64).decode("utf-8")
        request = json.loads(request_json)
        response = run_ocr(request)
    except json.JSONDecodeError as exc:
        response = error_response(f"invalid json: {exc}", "invalid_json")
    except Exception as exc:
        response = error_response(f"worker error: {exc}", "worker_error")

    print(json.dumps(response, ensure_ascii=False))


if __name__ == "__main__":
    main()
