"""Groq free-tier client — llama-3.3-70b-versatile primary."""
from __future__ import annotations

from groq import Groq, RateLimitError, APIStatusError

from ..config import GROQ_API_KEY, GROQ_MODEL
from .base import ProviderError, RateLimit


class GroqProvider:
    name = "groq"

    def __init__(self, model: str = GROQ_MODEL) -> None:
        if not GROQ_API_KEY:
            raise ProviderError("GROQ_API_KEY boş. .env dosyasını kontrol et.")
        self._client = Groq(api_key=GROQ_API_KEY)
        self._model = model

    def complete(self, prompt: str, *, json_mode: bool, temperature: float) -> str:
        kwargs: dict = {
            "model": self._model,
            "messages": [{"role": "user", "content": prompt}],
            "temperature": temperature,
            "max_tokens": 2048,
        }
        if json_mode:
            kwargs["response_format"] = {"type": "json_object"}

        try:
            resp = self._client.chat.completions.create(**kwargs)
        except RateLimitError as e:
            raise RateLimit(str(e)) from e
        except APIStatusError as e:
            if getattr(e, "status_code", None) == 429:
                raise RateLimit(str(e)) from e
            raise ProviderError(str(e)) from e
        except Exception as e:  # network vb.
            raise ProviderError(f"groq unexpected: {e}") from e

        content = resp.choices[0].message.content or ""
        if not content.strip():
            raise ProviderError("groq empty response")
        return content
