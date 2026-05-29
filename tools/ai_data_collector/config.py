"""Env + path config for the data collector pipeline."""
from __future__ import annotations

import os
from pathlib import Path

from dotenv import load_dotenv

REPO_ROOT = Path(__file__).resolve().parents[2]
load_dotenv(REPO_ROOT / ".env")

GROQ_API_KEY = os.getenv("GROQ_API_KEY", "").strip()
GROQ_MODEL = os.getenv("GROQ_MODEL", "llama-3.3-70b-versatile")

GEMINI_API_KEY = os.getenv("GEMINI_API_KEY", "").strip()
GEMINI_MODEL = os.getenv("GEMINI_MODEL", "gemini-2.5-flash")

OLLAMA_BASE_URL = os.getenv("OLLAMA_BASE_URL", "http://localhost:11434").rstrip("/")
OLLAMA_FALLBACK_MODEL = os.getenv("OLLAMA_FALLBACK_MODEL", "qwen2.5:7b")

TOOL_DIR = Path(__file__).resolve().parent
OUTPUT_DIR = TOOL_DIR / "output"
GOLDEN_DIR = TOOL_DIR / "golden"
EVAL_DIR = REPO_ROOT / "tests" / "AiEvalDataset" / "v1"

OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
GOLDEN_DIR.mkdir(parents=True, exist_ok=True)

FEATURES = ("scaffold-project", "enrich-issue", "generate-plan", "agent")


def has_groq() -> bool:
    return bool(GROQ_API_KEY)


def has_gemini() -> bool:
    return bool(GEMINI_API_KEY)
