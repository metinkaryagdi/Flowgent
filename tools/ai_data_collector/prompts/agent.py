"""Agent fine-tune için sistem prompt'u, tool catalog'u ve intent şablonları.

Inference tarafıyla (AgentLoop.BuildPrompt) bire bir aynı sistem mesajını üretir.
AgentLoop güncellenirse buradaki SYSTEM_PROMPT da güncellenmeli — eğitim/inference
formatı eşleşmezse fine-tune transfer etmez.
"""
from __future__ import annotations

import json

# ── AiController.AgentSystemPrompt ile birebir aynı olmalı ──────────────────
_AGENT_PROMPT_BASE = (
    "Sen BitirmeProject AI agent'ısın. Kullanıcının doğal dilde yazdığı isteği "
    "araç çağrılarıyla gerçekleştirmek için tool catalog'unu kullanırsın. "
    "Her adımda ya bir tool çağırırsın ya da konuşmayı 'final' ile bitirirsin. "
    "Yalnızca geçerli JSON döndürürsün; markdown fence, açıklama, İngilizce sızıntısı yasak. "
    "Türkçe yanıt ver. Aynı tool'u gereksiz tekrar çağırma, önce mevcut durumu sorgulamak için "
    "get_active_sprint / get_project_issues kullan."
)

# ── ToolRegistry.BuildCatalog çıktısıyla birebir aynı ──────────────────────
# (Her tool ITool.Name + Description + InputSchema)
TOOL_CATALOG = [
    {
        "name": "get_active_sprint",
        "description": "Context'teki projenin aktif sprint'ini döner (yoksa null). Yeni issue'ları hangi sprint'e bağlayacağını belirlemek için çağır.",
        "input_schema": {"type": "object", "properties": {}, "additionalProperties": False},
    },
    {
        "name": "get_project_issues",
        "description": "Context'teki projenin issue listesini döner. Aynı başlıklı issue zaten var mı kontrolü veya duplikasyon önleme için kullan.",
        "input_schema": {"type": "object", "properties": {}, "additionalProperties": False},
    },
    {
        "name": "create_issue",
        "description": "Projeye yeni bir issue oluşturur. title + priority zorunlu. Mevcut organizasyonun mevcut projesine bağlanır (organizationId ve projectId context'ten gelir).",
        "input_schema": {
            "type": "object",
            "properties": {
                "title": {"type": "string", "minLength": 3, "maxLength": 120},
                "description": {"type": "string", "maxLength": 2000},
                "priority": {"type": "string", "enum": ["Low", "Medium", "High", "Critical"]},
            },
            "required": ["title", "priority"],
            "additionalProperties": False,
        },
    },
    {
        "name": "create_sprint",
        "description": "Projeye yeni bir sprint oluşturur. 'Sprint N: Tema' formatında name + tek cümle goal zorunlu.",
        "input_schema": {
            "type": "object",
            "properties": {
                "name": {"type": "string", "minLength": 3, "maxLength": 120},
                "goal": {"type": "string", "minLength": 5, "maxLength": 300},
            },
            "required": ["name", "goal"],
            "additionalProperties": False,
        },
    },
    {
        "name": "add_issue_to_sprint",
        "description": "Mevcut bir issue'yu mevcut bir sprint'e bağlar. Her ikisi de aynı projeye ait olmalı.",
        "input_schema": {
            "type": "object",
            "properties": {
                "sprintId": {"type": "string", "format": "uuid"},
                "issueId": {"type": "string", "format": "uuid"},
            },
            "required": ["sprintId", "issueId"],
            "additionalProperties": False,
        },
    },
]

TOOL_NAMES = {t["name"] for t in TOOL_CATALOG}

_FORMAT_SPEC = (
    "Yanıt formatı — yalnızca aşağıdaki iki JSON şemadan birinde yanıt ver. Markdown fence, açıklama metni yasak.\n"
    "1) Tool çağırmak için:\n"
    "   {\"tool_calls\": [{\"name\": \"<tool_name>\", \"input\": { ... }}]}\n"
    "2) Konuşmayı bitirmek için:\n"
    "   {\"final\": \"<kullanıcıya gönderilecek Türkçe mesaj>\"}"
)


# Compact JSON ayarı — C# System.Text.Json.JsonSerializer default'u ile byte-eşleşme için.
# AgentLoop inference'ı `_registry.GetCatalogJson().GetRawText()` çağırıyor ve C# default
# compact JSON üretiyor (boşluksuz). Burada training data'sının da aynı stilde olması için
# separators=(',', ':') kullanıyoruz.
_COMPACT = {"separators": (",", ":"), "ensure_ascii": False}


def system_prompt() -> str:
    """AgentLoop.BuildSystemContent ile bire bir aynı sistem mesajını döner.

    Training data ve inference arasında byte-level eşleşme:
    - Tool catalog compact JSON (boşluksuz)
    - Bölümler arası ayraç: \\n\\n (OS-bağımsız, AppendLine değil)
    """
    return (
        _AGENT_PROMPT_BASE
        + "\n\nTool catalog (kullanılabilir araçlar):\n"
        + json.dumps(TOOL_CATALOG, **_COMPACT)
        + "\n\n"
        + _FORMAT_SPEC
    )


def tool_call_response(calls: list[dict]) -> str:
    """Assistant 'tool_calls' JSON çıktısı (training assistant turn)."""
    return json.dumps({"tool_calls": calls}, **_COMPACT)


def final_response(text: str) -> str:
    """Assistant 'final' JSON çıktısı (training assistant turn)."""
    return json.dumps({"final": text}, **_COMPACT)


def tool_result_message(name: str, success: bool, data=None, error: str | None = None) -> str:
    """Tool sonucu — AgentLoop.cs ile aynı yapı.

    Training'de bu metin bir 'user' turn olarak konuşmaya eklenir (chat template'lerde
    'tool' rolü garantili değil; '[tool]' prefix'iyle user mesajı taşıma daha güvenli).
    Compact JSON ile C# tool payload formatına eşleşir.
    """
    payload = {"name": name, "success": success, "data": data, "error": error}
    return "[tool] " + json.dumps(payload, **_COMPACT)


# ── Intent template'ler ────────────────────────────────────────────────────
# Her template: (user_question, expected_tool_calls, final_after_tool)
# - user_question: kullanıcının doğal dilde sorduğu şey
# - expected_tool_calls: model ilk turn'de hangi tool'ları nasıl çağırmalı
# - final_after_tool: tool sonucundan sonra üretilecek final yanıt template'i ({...} placeholder'ları runtime'da dolar)

# Her tuple farklı varyasyonlarla zenginleştirilecek; sentezleyici bunları çoğaltır.
INTENT_TEMPLATES: list[dict] = [
    # ─── READ: tek-tool ──────────────────────────────────────────────────
    {
        "id": "ask-active-sprint",
        "variants": [
            "Aktif sprint hangisi?",
            "Şu anki sprint nedir?",
            "Hangi sprint açık?",
            "Aktif sprint'i göster",
            "Bana mevcut sprint bilgisini ver",
            "Şu an üzerinde çalıştığımız sprint hangisi?",
        ],
        "tool_calls": [{"name": "get_active_sprint", "input": {}}],
        "final_with_data": "Aktif sprint '{sprint_name}' — {start_date} ile {end_date} arası. Hedef: {sprint_goal}.",
        "final_without_data": "Şu anda projede aktif sprint bulunmuyor.",
    },
    {
        "id": "ask-issue-count",
        "variants": [
            "Bu projede kaç tane issue var?",
            "Toplam issue sayısı nedir?",
            "Kaç issue açık?",
            "Projedeki issue'ları say",
            "Şu an kaç tane görev var?",
        ],
        "tool_calls": [{"name": "get_project_issues", "input": {}}],
        "final_with_data": "Projede toplam {issue_count} issue var.",
        "final_without_data": "Projede henüz hiç issue yok.",
    },
    {
        "id": "ask-issue-list",
        "variants": [
            "Issue'ları listele",
            "Görevleri göster",
            "Tüm issue'ları getir",
            "Projedeki issue'ları sırala",
            "Hangi issue'lar var?",
        ],
        "tool_calls": [{"name": "get_project_issues", "input": {}}],
        "final_with_data": "Projede {issue_count} issue var: {issue_titles}.",
        "final_without_data": "Projede issue bulunmuyor.",
    },
    # ─── WRITE: tek-tool create_issue ────────────────────────────────────
    {
        "id": "create-issue-with-priority",
        "variants": [
            "'{title}' başlıklı {priority} priority issue oluştur",
            "{priority} öncelikli '{title}' issue'su aç",
            "'{title}' diye bir {priority} issue ekle",
            "Yeni issue: '{title}', priority {priority}",
        ],
        "tool_calls": [{"name": "create_issue", "input": {"title": "{title}", "priority": "{priority}"}}],
        "final_with_data": "✓ '{title}' issue'su {priority} öncelikle oluşturuldu.",
    },
    # ─── WRITE: create_sprint ────────────────────────────────────────────
    {
        "id": "create-sprint",
        "variants": [
            "Yeni sprint aç: '{name}', hedef: {goal}",
            "'{name}' adında bir sprint oluştur, amacı: {goal}",
            "Sprint ekle — adı '{name}', goal '{goal}'",
        ],
        "tool_calls": [{"name": "create_sprint", "input": {"name": "{name}", "goal": "{goal}"}}],
        "final_with_data": "✓ '{name}' sprint'i oluşturuldu. Hedef: {goal}.",
    },
    # ─── MULTI-TOOL: create_issue + add_issue_to_sprint ──────────────────
    {
        "id": "add-issue-to-active-sprint",
        "variants": [
            "Aktif sprint'e '{title}' başlıklı {priority} issue ekle",
            "'{title}' diye bir {priority} issue oluştur ve aktif sprint'e bağla",
            "Mevcut sprint'e '{title}' adında {priority} priority issue koy",
        ],
        # 3 adım: önce sprint'i bul, sonra issue oluştur, sonra bağla
        "multi_step": True,
        "step_1_calls": [{"name": "get_active_sprint", "input": {}}],
        "step_2_calls": [{"name": "create_issue", "input": {"title": "{title}", "priority": "{priority}"}}],
        "step_3_calls": [{"name": "add_issue_to_sprint", "input": {"sprintId": "{sprint_id}", "issueId": "{issue_id}"}}],
        "final_with_data": "✓ '{title}' issue'su {priority} öncelikle oluşturuldu ve aktif sprint '{sprint_name}'e eklendi.",
        "final_without_data": "Aktif sprint olmadığı için issue oluşturuldu ama hiçbir sprint'e bağlanmadı.",
    },
    # ─── ERROR RECOVERY ──────────────────────────────────────────────────
    {
        "id": "create-issue-missing-priority",
        "variants": [
            "{title} adlı bir issue oluştur",
            "'{title}' diye bir görev ekle",
            "Bir issue aç: {title}",
        ],
        # Priority verilmemiş → model Medium varsayar (sensible default)
        "tool_calls": [{"name": "create_issue", "input": {"title": "{title}", "priority": "Medium"}}],
        "final_with_data": "✓ '{title}' issue'su (öncelik belirtilmediği için Medium) oluşturuldu.",
    },
]
