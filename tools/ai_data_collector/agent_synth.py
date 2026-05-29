"""Agent feature için template-tabanlı sentezleyici (API gerektirmez).

Neden LLM'siz: agent training data'sı deterministik bir 'user→tool_call→tool_result→final'
örüntüsüdür. LLM'le üretmek hem yavaş, hem hatalı (bp-agent fine-tune'unun başına gelen
olay tam buydu). Bunun yerine şablon * placeholder doldurma → 100% schema-uyumlu data.

Üretim akışı:
  1. INTENT_TEMPLATES içinden bir template seç
  2. Varyant cümleyi seç + placeholder'ları (title/priority/goal/...) gerçek değerlerle doldur
  3. Tool call sequence'i + sahte tool result'ları + final yanıtı kur
  4. validate_agent ile şemaya uygun mu kontrol et
  5. JSONL'e yaz

Çağrı:
  python -m tools.ai_data_collector.agent_synth --count 500
"""
from __future__ import annotations

import argparse
import json
import random
import string
import uuid
from pathlib import Path

from . import config
from .prompts.agent import (
    INTENT_TEMPLATES,
    final_response,
    system_prompt,
    tool_call_response,
    tool_result_message,
)
from .validation.schema import SchemaError, validate_agent

# Placeholder havuzları — sentetik veri için gerçekçi Türkçe değerler
_ISSUE_TITLES = [
    "Login sayfasında 500 hatası",
    "Profil resmi yüklenmiyor",
    "Sepet toplam fiyat yanlış hesaplanıyor",
    "Şifre sıfırlama e-postası gelmiyor",
    "API rate limit eklensin",
    "Dashboard yükleme süresi 5 saniyenin üzerinde",
    "Bildirim sayısı gerçek zamanlı güncellenmiyor",
    "Search bar autocomplete çalışmıyor",
    "Mobil görünümde menü açılmıyor",
    "PDF export Türkçe karakterleri bozuyor",
    "Kullanıcı silme onay diyaloğu eksik",
    "Filter dropdown'ı kapanmıyor",
    "Tarih seçici Pazar gününü gizliyor",
    "WebSocket bağlantısı sürekli kopuyor",
    "Yetki kontrolü endpoint'te yapılmıyor",
    "DB sorgu optimizasyonu gerek",
    "Cache invalidation logic eksik",
    "Hata mesajları İngilizce dönüyor",
    "Loglama seviyesi production'da Debug",
    "Cron job sessizce fail oluyor",
    "Email template'inde unsubscribe linki yok",
    "GDPR onay formu eksik",
    "Audit log timestamp UTC değil",
    "Health check endpoint'i auth istiyor",
    "Migration rollback script'i yok",
]

_SPRINT_NAMES = [
    "Sprint 1: Temel Altyapı",
    "Sprint 2: Kullanıcı Yönetimi",
    "Sprint 3: Stabilizasyon",
    "Sprint 4: Performans İyileştirme",
    "Sprint 5: Güvenlik Sertleştirmesi",
    "Sprint 6: API Genişletme",
    "Sprint 7: Bildirim Sistemi",
    "Sprint 8: Mobil Optimizasyon",
    "Sprint 9: Raporlama",
    "Sprint 10: Tema ve UX",
]

_SPRINT_GOALS = [
    "Kritik bug'ları kapatmak ve test kapsamını artırmak",
    "Authentication akışını yenilemek",
    "Dashboard yükleme süresini yarıya indirmek",
    "Ödeme entegrasyonunu tamamlamak",
    "Bildirim altyapısını WebSocket'e geçirmek",
    "API v2'yi yayına almak",
    "Güvenlik audit önerilerini uygulamak",
    "Production'a hazır hale getirmek",
    "Mobil responsive tasarımı tamamlamak",
    "Veritabanı şemasını migrate etmek",
]

_PRIORITIES = ["Low", "Medium", "High", "Critical"]


def _rand_title(rng: random.Random) -> str:
    return rng.choice(_ISSUE_TITLES)


def _rand_priority(rng: random.Random) -> str:
    # Real-world dağılım: Medium ve High daha sık
    return rng.choices(_PRIORITIES, weights=[1, 3, 3, 1], k=1)[0]


def _rand_sprint_name(rng: random.Random) -> str:
    return rng.choice(_SPRINT_NAMES)


def _rand_sprint_goal(rng: random.Random) -> str:
    return rng.choice(_SPRINT_GOALS)


def _rand_uuid(rng: random.Random) -> str:
    # Deterministik random UUID (rng seed reproducibility için)
    hex_chars = string.hexdigits.lower()[:16]
    parts = []
    for n in (8, 4, 4, 4, 12):
        parts.append("".join(rng.choices(hex_chars, k=n)))
    return "-".join(parts)


def _rand_date_range(rng: random.Random) -> tuple[str, str]:
    """Sentetik aktif sprint tarih aralığı (2 hafta)."""
    year = 2026
    month = rng.randint(1, 11)
    start_day = rng.randint(1, 14)
    start = f"{year}-{month:02d}-{start_day:02d}"
    end_day = start_day + 13
    end = f"{year}-{month:02d}-{end_day:02d}" if end_day <= 28 else f"{year}-{month + 1:02d}-{end_day - 28:02d}"
    return start, end


# ── Generator'lar (her template_id için bir fonksiyon) ────────────────────


def _gen_ask_active_sprint(rng: random.Random, tpl: dict) -> dict:
    user_q = rng.choice(tpl["variants"])
    has_active = rng.random() < 0.8

    msgs = [{"role": "system", "content": system_prompt()}, {"role": "user", "content": user_q}]
    msgs.append({"role": "assistant", "content": tool_call_response(tpl["tool_calls"])})

    if has_active:
        sprint_name = _rand_sprint_name(rng)
        sprint_goal = _rand_sprint_goal(rng)
        sprint_id = _rand_uuid(rng)
        start, end = _rand_date_range(rng)
        tool_data = {"id": sprint_id, "name": sprint_name, "goal": sprint_goal, "startDate": start, "endDate": end}
        msgs.append({"role": "user", "content": tool_result_message("get_active_sprint", True, tool_data)})
        final_text = tpl["final_with_data"].format(
            sprint_name=sprint_name, sprint_goal=sprint_goal, start_date=start, end_date=end
        )
    else:
        msgs.append({"role": "user", "content": tool_result_message("get_active_sprint", True, None)})
        final_text = tpl["final_without_data"]

    msgs.append({"role": "assistant", "content": final_response(final_text)})
    return {"messages": msgs}


def _gen_ask_issue_count_or_list(rng: random.Random, tpl: dict) -> dict:
    user_q = rng.choice(tpl["variants"])
    n_issues = rng.choice([0, 0, 1, 3, 5, 7, 12, 25])  # 0 case dahil
    msgs = [
        {"role": "system", "content": system_prompt()},
        {"role": "user", "content": user_q},
        {"role": "assistant", "content": tool_call_response(tpl["tool_calls"])},
    ]

    if n_issues == 0:
        msgs.append({"role": "user", "content": tool_result_message("get_project_issues", True, [])})
        final_text = tpl["final_without_data"]
    else:
        issues = []
        for _ in range(n_issues):
            issues.append({"id": _rand_uuid(rng), "title": _rand_title(rng)})
        msgs.append({"role": "user", "content": tool_result_message("get_project_issues", True, issues)})
        if "{issue_titles}" in tpl["final_with_data"]:
            # max ilk 5 başlık
            sample = ", ".join(f"'{i['title']}'" for i in issues[:5])
            if n_issues > 5:
                sample += f" ve {n_issues - 5} tane daha"
            final_text = tpl["final_with_data"].format(issue_count=n_issues, issue_titles=sample)
        else:
            final_text = tpl["final_with_data"].format(issue_count=n_issues)

    msgs.append({"role": "assistant", "content": final_response(final_text)})
    return {"messages": msgs}


def _gen_create_issue(rng: random.Random, tpl: dict) -> dict:
    title = _rand_title(rng)
    priority = _rand_priority(rng)
    user_q = rng.choice(tpl["variants"]).format(title=title, priority=priority)

    calls = [{"name": "create_issue", "input": {"title": title, "priority": priority}}]
    issue_id = _rand_uuid(rng)
    tool_data = {"id": issue_id, "title": title, "priority": priority}

    msgs = [
        {"role": "system", "content": system_prompt()},
        {"role": "user", "content": user_q},
        {"role": "assistant", "content": tool_call_response(calls)},
        {"role": "user", "content": tool_result_message("create_issue", True, tool_data)},
        {"role": "assistant", "content": final_response(tpl["final_with_data"].format(title=title, priority=priority))},
    ]
    return {"messages": msgs}


def _gen_create_issue_default_priority(rng: random.Random, tpl: dict) -> dict:
    title = _rand_title(rng)
    user_q = rng.choice(tpl["variants"]).format(title=title)
    calls = [{"name": "create_issue", "input": {"title": title, "priority": "Medium"}}]
    issue_id = _rand_uuid(rng)
    tool_data = {"id": issue_id, "title": title, "priority": "Medium"}

    msgs = [
        {"role": "system", "content": system_prompt()},
        {"role": "user", "content": user_q},
        {"role": "assistant", "content": tool_call_response(calls)},
        {"role": "user", "content": tool_result_message("create_issue", True, tool_data)},
        {"role": "assistant", "content": final_response(tpl["final_with_data"].format(title=title))},
    ]
    return {"messages": msgs}


def _gen_create_sprint(rng: random.Random, tpl: dict) -> dict:
    name = _rand_sprint_name(rng)
    goal = _rand_sprint_goal(rng)
    user_q = rng.choice(tpl["variants"]).format(name=name, goal=goal)
    calls = [{"name": "create_sprint", "input": {"name": name, "goal": goal}}]
    sprint_id = _rand_uuid(rng)
    tool_data = {"id": sprint_id, "name": name, "goal": goal}

    msgs = [
        {"role": "system", "content": system_prompt()},
        {"role": "user", "content": user_q},
        {"role": "assistant", "content": tool_call_response(calls)},
        {"role": "user", "content": tool_result_message("create_sprint", True, tool_data)},
        {"role": "assistant", "content": final_response(tpl["final_with_data"].format(name=name, goal=goal))},
    ]
    return {"messages": msgs}


def _gen_add_issue_to_active_sprint(rng: random.Random, tpl: dict) -> dict:
    """3-adımlı zincir: get_active_sprint → create_issue → add_issue_to_sprint."""
    title = _rand_title(rng)
    priority = _rand_priority(rng)
    user_q = rng.choice(tpl["variants"]).format(title=title, priority=priority)
    has_active = rng.random() < 0.85

    msgs: list[dict] = [
        {"role": "system", "content": system_prompt()},
        {"role": "user", "content": user_q},
    ]

    # Step 1: get_active_sprint
    msgs.append({"role": "assistant", "content": tool_call_response([{"name": "get_active_sprint", "input": {}}])})
    if has_active:
        sprint_id = _rand_uuid(rng)
        sprint_name = _rand_sprint_name(rng)
        sprint_data = {
            "id": sprint_id,
            "name": sprint_name,
            "goal": _rand_sprint_goal(rng),
            "startDate": _rand_date_range(rng)[0],
            "endDate": _rand_date_range(rng)[1],
        }
        msgs.append({"role": "user", "content": tool_result_message("get_active_sprint", True, sprint_data)})

        # Step 2: create_issue
        issue_id = _rand_uuid(rng)
        msgs.append(
            {
                "role": "assistant",
                "content": tool_call_response(
                    [{"name": "create_issue", "input": {"title": title, "priority": priority}}]
                ),
            }
        )
        msgs.append(
            {
                "role": "user",
                "content": tool_result_message("create_issue", True, {"id": issue_id, "title": title, "priority": priority}),
            }
        )

        # Step 3: add_issue_to_sprint
        msgs.append(
            {
                "role": "assistant",
                "content": tool_call_response(
                    [{"name": "add_issue_to_sprint", "input": {"sprintId": sprint_id, "issueId": issue_id}}]
                ),
            }
        )
        msgs.append(
            {
                "role": "user",
                "content": tool_result_message(
                    "add_issue_to_sprint", True, {"sprintId": sprint_id, "issueId": issue_id, "attached": True}
                ),
            }
        )

        final_text = tpl["final_with_data"].format(title=title, priority=priority, sprint_name=sprint_name)
    else:
        msgs.append({"role": "user", "content": tool_result_message("get_active_sprint", True, None)})
        # Step 2 only: create_issue (sprint olmadığı için bağlama yok)
        issue_id = _rand_uuid(rng)
        msgs.append(
            {
                "role": "assistant",
                "content": tool_call_response(
                    [{"name": "create_issue", "input": {"title": title, "priority": priority}}]
                ),
            }
        )
        msgs.append(
            {
                "role": "user",
                "content": tool_result_message("create_issue", True, {"id": issue_id, "title": title, "priority": priority}),
            }
        )
        final_text = tpl["final_without_data"]

    msgs.append({"role": "assistant", "content": final_response(final_text)})
    return {"messages": msgs}


# ── Error recovery örnekleri ──────────────────────────────────────────────


def _gen_error_recovery(rng: random.Random) -> dict:
    """Tool fail → model kullanıcıya hatayı açıklasın, retry zorlamayalım."""
    scenarios = [
        {
            "user": "'Yeni feature' başlıklı issue oluştur ama priority belirtme — model sormalı",
            "user_short": "'Yeni feature' adında bir issue ekle",
            "calls": [{"name": "create_issue", "input": {"title": "Yeni feature", "priority": "Medium"}}],
            "tool_result": {"success": True, "data": {"id": _rand_uuid(rng), "title": "Yeni feature", "priority": "Medium"}},
            "final": "✓ 'Yeni feature' issue'su (öncelik belirtilmediği için Medium) oluşturuldu.",
        },
        {
            "user_short": "'kx' başlıklı issue aç",  # 3-char limitin altı değil ama kısa
            "calls": [{"name": "create_issue", "input": {"title": "kx", "priority": "Medium"}}],
            "tool_result": {"success": False, "error": "title min 3 karakter olmalı."},
            "final": "Başlık çok kısa (en az 3 karakter olmalı). Daha açıklayıcı bir başlık verir misin?",
        },
        {
            "user_short": "Sprint oluştur",  # eksik bilgi
            "calls": [],  # model önce sormalı
            "tool_result": None,
            "final": "Sprint oluşturmak için ad ('Sprint N: Tema' formatında) ve hedef cümlesi gerek. İkisini de söyler misin?",
        },
        {
            "user_short": "Issue ID'si abc olan kayda 'sprint-1'e ekle",
            "calls": [
                {"name": "add_issue_to_sprint", "input": {"sprintId": "abc", "issueId": "sprint-1"}}
            ],
            "tool_result": {"success": False, "error": "sprintId geçerli UUID olmalı."},
            "final": "Sprint ID ve issue ID'leri geçerli UUID olmalı. Mevcut sprint/issue listesinden seçim ister misin?",
        },
    ]
    sc = rng.choice(scenarios)
    msgs: list[dict] = [
        {"role": "system", "content": system_prompt()},
        {"role": "user", "content": sc["user_short"]},
    ]
    if sc["calls"]:
        msgs.append({"role": "assistant", "content": tool_call_response(sc["calls"])})
        if sc["tool_result"] is not None:
            tr = sc["tool_result"]
            msgs.append(
                {
                    "role": "user",
                    "content": tool_result_message(
                        sc["calls"][0]["name"], tr["success"], tr.get("data"), tr.get("error")
                    ),
                }
            )
    msgs.append({"role": "assistant", "content": final_response(sc["final"])})
    return {"messages": msgs}


# ── Template ID → generator dispatch ──────────────────────────────────────
_GENERATORS = {
    "ask-active-sprint": _gen_ask_active_sprint,
    "ask-issue-count": _gen_ask_issue_count_or_list,
    "ask-issue-list": _gen_ask_issue_count_or_list,
    "create-issue-with-priority": _gen_create_issue,
    "create-issue-missing-priority": _gen_create_issue_default_priority,
    "create-sprint": _gen_create_sprint,
    "add-issue-to-active-sprint": _gen_add_issue_to_active_sprint,
}


def _gen_one(rng: random.Random) -> dict:
    """Tek örnek üret: ya intent template ya error recovery."""
    # %15 error recovery
    if rng.random() < 0.15:
        ex = _gen_error_recovery(rng)
    else:
        tpl = rng.choice(INTENT_TEMPLATES)
        fn = _GENERATORS.get(tpl["id"])
        if fn is None:
            raise ValueError(f"generator yok: {tpl['id']}")
        ex = fn(rng, tpl)
    return ex


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--count", type=int, default=500)
    ap.add_argument("--seed", type=int, default=42)
    ap.add_argument("--out", default=str(config.OUTPUT_DIR / "synthetic-agent.jsonl"))
    args = ap.parse_args()

    rng = random.Random(args.seed)
    out_path = Path(args.out)
    out_path.parent.mkdir(parents=True, exist_ok=True)

    seen = set()  # user mesajı bazlı kaba dedup
    produced = 0
    attempts = 0
    max_attempts = args.count * 3

    with out_path.open("w", encoding="utf-8") as f:
        while produced < args.count and attempts < max_attempts:
            attempts += 1
            ex = _gen_one(rng)
            user_msg = next((m["content"] for m in ex["messages"] if m["role"] == "user"), "")
            if user_msg in seen:
                continue

            try:
                validate_agent(ex)
            except SchemaError as e:
                print(f"  [drop:schema] {e}")
                continue

            seen.add(user_msg)
            record = {"feature": "agent", "source": "synthetic", **ex}
            f.write(json.dumps(record, ensure_ascii=False) + "\n")
            produced += 1

    print(f"[agent_synth] {produced}/{args.count} uretildi (attempts={attempts}) -> {out_path}")
    return 0 if produced == args.count else 1


if __name__ == "__main__":
    raise SystemExit(main())
