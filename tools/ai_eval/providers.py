"""Eval için model sağlayıcıları — Ollama (base ve fine-tuned) ve opsiyonel Groq baseline.

Kullanım:
  runner.py iki ayrı OllamaClient instance kullanır:
    base_client = OllamaClient(model="gemma3:4b")
    ft_client   = OllamaClient(model="bp-agent")
  Sonuçlar yan yana raporlanır.
"""
from __future__ import annotations

import json
import os
import time
import urllib.error
import urllib.request
from dataclasses import dataclass


class ProviderError(RuntimeError):
    pass


@dataclass
class OllamaClient:
    """Ollama HTTP API — /api/chat endpoint'i kullanır, JSON mode."""
    model: str
    base_url: str = ""
    timeout: int = 180

    def __post_init__(self) -> None:
        if not self.base_url:
            self.base_url = os.getenv("OLLAMA_BASE_URL", "http://localhost:11434").rstrip("/")

    def chat(self, system: str, user: str, *, temperature: float = 0.2, top_p: float = 0.9,
             max_tokens: int = 2048, json_mode: bool = True) -> tuple[str, float]:
        """Tek bir chat turu yap. Return: (raw_text, latency_ms)."""
        payload = {
            "model": self.model,
            "messages": [
                {"role": "system", "content": system},
                {"role": "user", "content": user},
            ],
            "stream": False,
            "options": {
                "temperature": temperature,
                "top_p": top_p,
                "num_predict": max_tokens,
            },
        }
        if json_mode:
            payload["format"] = "json"

        body = json.dumps(payload).encode("utf-8")
        req = urllib.request.Request(
            f"{self.base_url}/api/chat",
            data=body,
            headers={"Content-Type": "application/json"},
            method="POST",
        )

        t0 = time.perf_counter()
        try:
            with urllib.request.urlopen(req, timeout=self.timeout) as resp:
                data = json.loads(resp.read().decode("utf-8"))
        except urllib.error.HTTPError as e:
            raise ProviderError(f"ollama http {e.code}: {e.read().decode('utf-8', errors='ignore')[:200]}") from e
        except urllib.error.URLError as e:
            raise ProviderError(f"ollama bağlantı: {e.reason}") from e
        dt_ms = (time.perf_counter() - t0) * 1000

        msg = data.get("message", {})
        content = msg.get("content", "")
        if not content:
            raise ProviderError(f"ollama boş yanıt: {data}")
        return content, dt_ms

    def ping(self) -> bool:
        try:
            req = urllib.request.Request(f"{self.base_url}/api/tags", method="GET")
            with urllib.request.urlopen(req, timeout=5) as resp:
                return resp.status == 200
        except Exception:
            return False

    def has_model(self) -> bool:
        try:
            req = urllib.request.Request(f"{self.base_url}/api/tags", method="GET")
            with urllib.request.urlopen(req, timeout=5) as resp:
                data = json.loads(resp.read().decode("utf-8"))
            return any(m.get("name", "").startswith(self.model) for m in data.get("models", []))
        except Exception:
            return False
