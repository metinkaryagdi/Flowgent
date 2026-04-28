"""Sentetik (input, output) çifti üretici.

İki aşamalı:
1. Input metin üretimi (model -> kullanıcı talebi)
2. Output JSON üretimi (model -> ProjectDraft/EnrichResult/Plan)

Her örnek:
- schema validation
- PII scrub
- dedup (cosine > 0.90)
üçlüsünden geçer; geçemezse atılır.

Çağrı:
  python -m tools.ai-data-collector.synthetic_gen --feature scaffold-project --count 10 --smoke
  python -m tools.ai-data-collector.synthetic_gen --feature enrich-issue --count 300

Çıktı: tools/ai-data-collector/output/synthetic-<feature>.jsonl
"""
from __future__ import annotations

import argparse
import json
import random
import sys
import time
from pathlib import Path

from tqdm import tqdm

from . import config
from . import domains
from .prompts import FEATURE_MODULES
from .providers import GeminiProvider, GroqProvider, OllamaProvider, ProviderError, RateLimit
from .validation import Dedup, scrub_json_values, validate
from .validation.schema import SchemaError


def _load_existing(path: Path) -> tuple[int, list[str]]:
    """Varolan JSONL'den sayı + input metin listesi (dedup resume için)."""
    if not path.exists():
        return 0, []
    n = 0
    texts = []
    with path.open(encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            try:
                obj = json.loads(line)
            except json.JSONDecodeError:
                continue
            n += 1
            inp = obj.get("input", {})
            texts.append(_input_text(inp))
    return n, texts


def _input_text(inp) -> str:
    """Dedup için input object'ten tek string türet."""
    if isinstance(inp, dict):
        return inp.get("description") or inp.get("title") or json.dumps(inp, ensure_ascii=False)
    return str(inp)


def _parse_json(raw: str):
    raw = raw.strip()
    # Model bazen fence ekliyor; agresif temizle.
    if raw.startswith("```"):
        raw = raw.strip("`")
        if raw.lower().startswith("json"):
            raw = raw[4:]
    start = raw.find("{")
    end = raw.rfind("}")
    if start == -1 or end == -1 or end < start:
        raise ValueError("JSON sınırı bulunamadı")
    return json.loads(raw[start : end + 1])


def _generate_one(feature: str, provider_primary, provider_fallback, rng: random.Random):
    """Tek bir (input, output) çifti üret. Başarısızsa ValueError."""
    mod = FEATURE_MODULES[feature]
    tpl = rng.choice(mod.templates())
    ctx = domains.sample(rng)
    input_prompt, output_prompt = mod.build(tpl, ctx, rng)

    temperature = round(rng.uniform(0.7, 1.0), 2)

    # --- Adım 1: input metnini üret (freeform) ---
    user_text = _call_with_fallback(provider_primary, provider_fallback, input_prompt, json_mode=False, temperature=temperature)
    user_text = user_text.strip()
    if len(user_text) < 20:
        raise ValueError("input çok kısa")
    if len(user_text) > 1500:
        user_text = user_text[:1500]

    # --- Adım 2: output JSON üret ---
    # feature başına input objesinin şekli değişiyor.
    if feature == "enrich-issue":
        # üretilen 'user_text' ilk satırı issue title olarak kullan.
        title = user_text.splitlines()[0][:120]
        input_obj = {"title": title, "projectContext": f"{ctx.area} / {ctx.sub}"}
        combined = f"{output_prompt}\n\nIssue title: {title}"
    elif feature == "generate-plan":
        # İlk satır = proje adı, gerisi açıklama
        lines = [ln.strip() for ln in user_text.splitlines() if ln.strip()]
        project_name = lines[0][:80] if lines else "Proje"
        desc = "\n".join(lines[1:]) if len(lines) > 1 else user_text
        input_obj = {
            "projectId": "00000000-0000-0000-0000-000000000000",
            "projectName": project_name,
            "description": desc[:1200],
        }
        combined = f"{output_prompt}\n\nProje adı: {project_name}\nYeni talep:\n{desc}"
    else:  # scaffold-project
        input_obj = {"description": user_text[:1200], "context": {"area": ctx.area, "sub": ctx.sub, "scale": ctx.scale}}
        combined = f"{output_prompt}\n\nKullanıcı talebi:\n{user_text}"

    raw_output = _call_with_fallback(provider_primary, provider_fallback, combined, json_mode=True, temperature=0.3)
    data = _parse_json(raw_output)

    # Schema validation
    validate(feature, data)

    # PII scrub — hem input hem output
    input_obj = scrub_json_values(input_obj)
    data = scrub_json_values(data)

    return {
        "feature": feature,
        "template_id": tpl.id,
        "domain": {"area": ctx.area, "sub": ctx.sub, "scale": ctx.scale, "user": ctx.user},
        "temperature": temperature,
        "input": input_obj,
        "output": data,
    }


def _call_with_fallback(primary, fallback, prompt: str, *, json_mode: bool, temperature: float) -> str:
    if primary is not None:
        try:
            return primary.complete(prompt, json_mode=json_mode, temperature=temperature)
        except RateLimit:
            if fallback is None:
                # Groq-only mode: tek seferlik 65sn sleep + retry; hâlâ limit ise drop.
                time.sleep(65)
                return primary.complete(prompt, json_mode=json_mode, temperature=temperature)
            time.sleep(60)
        except ProviderError:
            if fallback is None:
                raise
    if fallback is None:
        raise ProviderError("no fallback available")
    return fallback.complete(prompt, json_mode=json_mode, temperature=temperature)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--feature", required=True, choices=list(config.FEATURES))
    ap.add_argument("--count", type=int, default=10)
    ap.add_argument("--smoke", action="store_true", help="sadece 1-3 örnek, bozulursa anla")
    ap.add_argument("--provider", choices=["auto", "groq", "gemini", "ollama"], default="auto")
    ap.add_argument("--seed", type=int, default=42)
    args = ap.parse_args()

    count = 3 if args.smoke else args.count
    rng = random.Random(args.seed)

    primary = None
    fallback = None
    if args.provider == "gemini":
        if not config.has_gemini():
            print("ERROR: --provider gemini ama GEMINI_API_KEY yok", file=sys.stderr)
            return 2
        primary = GeminiProvider()
        print(f"[provider] gemini -> {config.GEMINI_MODEL}")
    elif args.provider == "groq":
        if not config.has_groq():
            print("ERROR: --provider groq ama GROQ_API_KEY yok", file=sys.stderr)
            return 2
        primary = GroqProvider()
        print(f"[provider] groq -> {config.GROQ_MODEL}")
    elif args.provider == "auto":
        if config.has_gemini():
            primary = GeminiProvider()
            print(f"[provider] gemini -> {config.GEMINI_MODEL}")
        elif config.has_groq():
            primary = GroqProvider()
            print(f"[provider] groq -> {config.GROQ_MODEL}")

    if args.provider in ("auto", "ollama"):
        fallback = OllamaProvider()
        print(f"[provider] ollama fallback -> {config.OLLAMA_FALLBACK_MODEL}")

    if primary is None and fallback is None:
        print("ERROR: hiçbir provider kullanılabilir değil", file=sys.stderr)
        return 2

    out_path = config.OUTPUT_DIR / f"synthetic-{args.feature}.jsonl"
    existing_n, existing_texts = _load_existing(out_path)
    dedup = Dedup(threshold=0.90)
    dedup.load_existing(existing_texts)

    target = existing_n + count
    print(f"[target] {args.feature}: {existing_n} -> {target} örnek")

    f = out_path.open("a", encoding="utf-8")
    produced = 0
    attempts = 0
    max_attempts = count * 4
    bar = tqdm(total=count, desc=args.feature)

    try:
        while produced < count and attempts < max_attempts:
            attempts += 1
            try:
                ex = _generate_one(args.feature, primary, fallback, rng)
            except SchemaError as e:
                bar.write(f"  [drop:schema] {e}")
                continue
            except (ValueError, ProviderError) as e:
                bar.write(f"  [drop:gen] {type(e).__name__}: {str(e)[:120]}")
                continue

            key = _input_text(ex["input"])
            if not dedup.should_add(key):
                bar.write("  [drop:dup]")
                continue

            f.write(json.dumps(ex, ensure_ascii=False) + "\n")
            f.flush()
            produced += 1
            bar.update(1)
    finally:
        bar.close()
        f.close()

    print(f"[done] {produced} yeni örnek. Toplam {existing_n + produced}. attempts={attempts}")
    return 0 if produced == count else 1


if __name__ == "__main__":
    raise SystemExit(main())
