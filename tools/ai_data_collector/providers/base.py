"""Provider contract — Groq ve Ollama aynı arayüzü paylaşır."""
from __future__ import annotations

from typing import Protocol


class ProviderError(Exception):
    pass


class RateLimit(ProviderError):
    """429 veya quota bitince atılır — caller fallback'e düşsün."""


class Provider(Protocol):
    name: str

    def complete(self, prompt: str, *, json_mode: bool, temperature: float) -> str:
        """Tek turda freeform ya da JSON string döner.

        json_mode=True → sağlayıcı destekliyorsa response_format JSON zorla.
        Hata durumunda ProviderError / RateLimit atar.
        """
        ...
