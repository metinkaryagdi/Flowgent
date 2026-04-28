"""Eğitilmiş Unsloth LoRA adapter'ını GGUF formatına dönüştürür.

Colab'da `model.save_pretrained_gguf` zaten çalışır; bu script lokal/post-run
kullanımı için: Drive'dan çekilmiş LoRA adapter klasörü varsa llama.cpp
convert script'i üzerinden GGUF çıkarır.

Ön-koşul:
  - llama.cpp clone edilmiş veya `pip install llama-cpp-python`
  - adapter klasörü: `adapter_model.safetensors`, `adapter_config.json`,
    `tokenizer_config.json`, `special_tokens_map.json`, `tokenizer.model`

Çağrı:
  python tools/ai-finetune/scripts/export_to_gguf.py \\
      --adapter ./adapters/gemma3-4b-bp-v1 \\
      --out ./adapters/gemma3-4b-bp-v1.gguf \\
      --quant q4_k_m

Notlar:
  - Üretim GGUF'u Colab hücresi üretmeli (`model.save_pretrained_gguf`). Bu
    script gerektiğinde "lokal post-hoc" dönüşüm içindir.
  - Ollama Modelfile ADAPTER yolu GGUF'u gösterir.
"""
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
from pathlib import Path

REQUIRED_FILES = ("adapter_config.json", "adapter_model.safetensors")


def _check_adapter(path: Path) -> None:
    if not path.is_dir():
        raise FileNotFoundError(f"adapter klasörü yok: {path}")
    for f in REQUIRED_FILES:
        if not (path / f).exists():
            raise FileNotFoundError(f"adapter eksik dosya: {f} ({path})")


def _find_llama_cpp() -> Path | None:
    for cand in (
        Path.cwd() / "llama.cpp",
        Path.home() / "llama.cpp",
        Path("/opt/llama.cpp"),
    ):
        if cand.is_dir() and (cand / "convert_lora_to_gguf.py").exists():
            return cand
    return None


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--adapter", required=True, help="LoRA adapter klasörü")
    ap.add_argument("--out", required=True, help="çıkış .gguf dosyası")
    ap.add_argument("--quant", default="q4_k_m",
                    choices=["q4_k_m", "q5_k_m", "q8_0", "f16"],
                    help="GGUF quantization")
    ap.add_argument("--llama-cpp", default=None, help="llama.cpp repo yolu (auto-detect)")
    args = ap.parse_args()

    adapter = Path(args.adapter).resolve()
    out_path = Path(args.out).resolve()
    _check_adapter(adapter)
    out_path.parent.mkdir(parents=True, exist_ok=True)

    llama_cpp = Path(args.llama_cpp) if args.llama_cpp else _find_llama_cpp()
    if llama_cpp is None:
        print(
            "ERROR: llama.cpp bulunamadı. Clone et: "
            "git clone https://github.com/ggerganov/llama.cpp",
            file=sys.stderr,
        )
        return 2

    convert = llama_cpp / "convert_lora_to_gguf.py"
    if not convert.exists():
        print(f"ERROR: {convert} yok (llama.cpp güncel mi?)", file=sys.stderr)
        return 2

    cmd = [
        sys.executable, str(convert),
        "--outtype", args.quant,
        "--outfile", str(out_path),
        str(adapter),
    ]
    print(f"[run] {' '.join(cmd)}")
    result = subprocess.run(cmd, check=False)
    if result.returncode != 0:
        print(f"ERROR: dönüşüm başarısız (exit {result.returncode})", file=sys.stderr)
        return result.returncode

    size_mb = out_path.stat().st_size / 1024 / 1024
    print(f"[done] {out_path} ({size_mb:.1f} MB)")

    modelfile = out_path.parent / "Modelfile"
    if not modelfile.exists():
        modelfile.write_text(
            f"FROM gemma3:4b\n"
            f"ADAPTER {out_path.name}\n"
            "PARAMETER temperature 0.3\n"
            "PARAMETER top_p 0.9\n"
            "SYSTEM \"Sen BitirmeProject AI agent'ısın. Yalnızca geçerli JSON döndürürsün.\"\n",
            encoding="utf-8",
        )
        print(f"[scaffold] {modelfile} yazıldı (ollama create bp-agent -f Modelfile)")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
