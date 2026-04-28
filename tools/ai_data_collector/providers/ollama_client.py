"""Ollama fallback — CPU'da qwen2.5:7b yavaş ama ücretsiz."""
from __future__ import annotations

import json

import requests

from ..config import OLLAMA_BASE_URL, OLLAMA_FALLBACK_MODEL
from .base import ProviderError


class OllamaProvider:
    name = "ollama"

    def __init__(self, model: str = OLLAMA_FALLBACK_MODEL, base_url: str = OLLAMA_BASE_URL) -> None:
        self._model = model
        self._base = base_url

    def complete(self, prompt: str, *, json_mode: bool, temperature: float) -> str:
        payload = {
            "model": self._model,
            "prompt": prompt,
            "stream": False,
            "options": {"temperature": temperature, "num_predict": 2048},
        }
        if json_mode:
            payload["format"] = "json"

        try:
            r = requests.post(f"{self._base}/api/generate", json=payload, timeout=600)
        except requests.RequestException as e:
            raise ProviderError(f"ollama network: {e}") from e

        if r.status_code != 200:
            raise ProviderError(f"ollama http {r.status_code}: {r.text[:300]}")

        try:
            data = r.json()
        except json.JSONDecodeError as e:
            raise ProviderError(f"ollama non-json body: {e}") from e

        content = (data.get("response") or "").strip()
        if not content:
            raise ProviderError("ollama empty response")
        return content
