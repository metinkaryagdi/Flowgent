"""PII scrubber — email, telefon, kurumsal tanımlayıcılar."""
from __future__ import annotations

import re

# Basit ama agresif pattern'lar; false positive kabul, false negative tercih edilmez.
_EMAIL = re.compile(r"\b[\w.+-]+@[\w-]+\.[\w.-]+\b")
_PHONE = re.compile(r"(?<!\d)(?:\+?90[\s-]?)?(?:0[\s-]?)?(?:\(?\d{3}\)?[\s-]?\d{3}[\s-]?\d{2}[\s-]?\d{2})(?!\d)")
_TCKN = re.compile(r"(?<!\d)\d{11}(?!\d)")
_CC = re.compile(r"(?<!\d)\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}(?!\d)")
_IBAN = re.compile(r"\bTR\d{2}(?:[\s-]?\d{4}){5}(?:[\s-]?\d{2})\b")
_URL = re.compile(r"https?://[^\s)]+")


def scrub(text: str) -> str:
    """String içindeki PII'leri placeholder'larla değiştir.

    Liste:
    - email → <EMAIL>
    - telefon → <PHONE>
    - TCKN (11 hane) → <TCKN>
    - kredi kartı (16 hane) → <CC>
    - IBAN → <IBAN>
    - url → <URL>
    """
    if not text:
        return text
    out = text
    out = _EMAIL.sub("<EMAIL>", out)
    out = _URL.sub("<URL>", out)
    out = _IBAN.sub("<IBAN>", out)
    out = _CC.sub("<CC>", out)
    out = _TCKN.sub("<TCKN>", out)
    out = _PHONE.sub("<PHONE>", out)
    return out


def scrub_json_values(obj):
    """Dict/list içinde recursive string scrub."""
    if isinstance(obj, str):
        return scrub(obj)
    if isinstance(obj, list):
        return [scrub_json_values(x) for x in obj]
    if isinstance(obj, dict):
        return {k: scrub_json_values(v) for k, v in obj.items()}
    return obj
