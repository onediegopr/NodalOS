#!/usr/bin/env python3
"""Local-only OpenAI-compatible fixture for NODAL OS BYOK process smoke.

The server deliberately fails the primary model and succeeds for the configured
fallback model. It never prints Authorization values or request bodies.
"""

from __future__ import annotations

import argparse
import json
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer


class Handler(BaseHTTPRequestHandler):
    server_version = "NodalByokFixture/1.0"

    def do_GET(self) -> None:  # noqa: N802
        if self.path == "/health":
            self._json(200, {"status": "ready"})
            return
        self._json(404, {"error": "not_found"})

    def do_POST(self) -> None:  # noqa: N802
        if self.path != "/v1/chat/completions":
            self._json(404, {"error": "not_found"})
            return

        length = self.headers.get("Content-Length")
        try:
            size = int(length or "0")
        except ValueError:
            self._json(400, {"error": "invalid_length"})
            return
        if size <= 0 or size > 64 * 1024:
            self._json(413, {"error": "bounded_request_rejected"})
            return

        authorization = self.headers.get("Authorization", "")
        if authorization not in {"Bearer primary-smoke-key", "Bearer fallback-smoke-key"}:
            self._json(401, {"error": "unauthorized"})
            return

        try:
            payload = json.loads(self.rfile.read(size).decode("utf-8"))
        except (UnicodeDecodeError, json.JSONDecodeError):
            self._json(400, {"error": "invalid_json"})
            return

        model = payload.get("model")
        if model == "primary-smoke-model":
            self._json(503, {"error": "primary_temporarily_unavailable"})
            return
        if model != "fallback-smoke-model":
            self._json(404, {"error": "model_not_found"})
            return

        self._json(
            200,
            {
                "id": "fixture-response",
                "choices": [{"message": {"role": "assistant", "content": "NODAL_BYOK_SMOKE_OK"}}],
                "usage": {"prompt_tokens": 12, "completion_tokens": 4},
            },
        )

    def log_message(self, format: str, *args: object) -> None:
        # Keep diagnostics metadata-only and never echo headers or body content.
        print(f"{self.command} {self.path} {args[1] if len(args) > 1 else ''}", flush=True)

    def _json(self, status: int, payload: dict[str, object]) -> None:
        encoded = json.dumps(payload, separators=(",", ":")).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(encoded)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(encoded)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--port", type=int, required=True)
    args = parser.parse_args()
    server = ThreadingHTTPServer(("127.0.0.1", args.port), Handler)
    print(f"fixture-ready:{args.port}", flush=True)
    server.serve_forever()


if __name__ == "__main__":
    main()
