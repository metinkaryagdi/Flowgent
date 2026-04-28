"""Gemini provider — google-genai SDK üstünden gemini-2.0-flash primary.

Free tier'da llama-3.3-70b'den çok daha cömert günlük token bütçesi sunar
(Flash: ~1500 req/gün). 429'da `RateLimit`, ağ/şema/quota hatalarında
`ProviderError` atar — synthetic_gen aynı fallback yolunu kullanır.
"""
from __future__ import annotations

from google import genai
from google.genai import errors as genai_errors
from google.genai import types as genai_types

from ..config import GEMINI_API_KEY, GEMINI_MODEL
from .base import ProviderError, RateLimit


class GeminiProvider:
    name = "gemini"

    def __init__(self, model: str = GEMINI_MODEL) -> None:
        if not GEMINI_API_KEY:
            raise ProviderError("GEMINI_API_KEY boş. .env dosyasını kontrol et.")
        self._client = genai.Client(api_key=GEMINI_API_KEY)
        self._model = model

    def complete(self, prompt: str, *, json_mode: bool, temperature: float) -> str:
        config_kwargs: dict = {
            "temperature": temperature,
            "max_output_tokens": 2048,
        }
        if json_mode:
            config_kwargs["response_mime_type"] = "application/json"

        try:
            resp = self._client.models.generate_content(
                model=self._model,
                contents=prompt,
                config=genai_types.GenerateContentConfig(**config_kwargs),
            )
        except genai_errors.ClientError as e:
            status = getattr(e, "code", None) or getattr(e, "status_code", None)
            if status == 429:
                raise RateLimit(str(e)) from e
            raise ProviderError(f"gemini client error: {e}") from e
        except genai_errors.ServerError as e:
            raise ProviderError(f"gemini server error: {e}") from e
        except Exception as e:
            raise ProviderError(f"gemini unexpected: {e}") from e

        text = (getattr(resp, "text", None) or "").strip()
        if not text:
            raise ProviderError("gemini empty response")
        return text
