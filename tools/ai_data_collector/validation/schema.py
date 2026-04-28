"""Feature çıktıları için hafif schema kontrolü.

Tam JSON Schema yerine elle kontrol: 1500 örnekte 3 özelliğin şemaları iyi
biliniyor, dış kütüphaneye gerek yok. Schema kayması = örnek elenir.
"""
from __future__ import annotations

from typing import Any

PRIORITIES = {"Low", "Medium", "High", "Critical"}
FIB = {1, 2, 3, 5, 8, 13}


class SchemaError(ValueError):
    pass


def _require_str(obj: dict, key: str, min_len: int = 1, max_len: int = 10_000) -> str:
    v = obj.get(key)
    if not isinstance(v, str):
        raise SchemaError(f"{key}: string bekleniyor, {type(v).__name__} geldi")
    if not (min_len <= len(v) <= max_len):
        raise SchemaError(f"{key}: uzunluk {len(v)} sınır dışı ({min_len}-{max_len})")
    return v


def _require_int(obj: dict, key: str, allowed: set[int]) -> int:
    v = obj.get(key)
    if not isinstance(v, int) or isinstance(v, bool):
        raise SchemaError(f"{key}: int bekleniyor, {type(v).__name__} geldi")
    if v not in allowed:
        raise SchemaError(f"{key}: {v} izin verilen set dışı {allowed}")
    return v


def _check_issue(issue: Any) -> None:
    if not isinstance(issue, dict):
        raise SchemaError("issue: object bekleniyor")
    _require_str(issue, "title", 5, 120)
    _require_str(issue, "description", 10, 500)
    p = issue.get("priority")
    if p not in PRIORITIES:
        raise SchemaError(f"priority: {p} enum dışı")
    _require_int(issue, "storyPoints", FIB)


def _check_sprint(sprint: Any) -> None:
    if not isinstance(sprint, dict):
        raise SchemaError("sprint: object bekleniyor")
    _require_str(sprint, "name", 5, 120)
    _require_str(sprint, "goal", 5, 300)
    issues = sprint.get("issues")
    if not isinstance(issues, list) or not (3 <= len(issues) <= 6):
        raise SchemaError(f"issues: 3-6 eleman bekleniyor, {len(issues) if isinstance(issues, list) else 'list-değil'}")
    for i in issues:
        _check_issue(i)


def _check_sprints_list(data: dict) -> None:
    sprints = data.get("sprints")
    if not isinstance(sprints, list) or not (1 <= len(sprints) <= 4):
        raise SchemaError(f"sprints: 1-4 eleman bekleniyor")
    for s in sprints:
        _check_sprint(s)


def validate_scaffold(data: Any) -> None:
    if not isinstance(data, dict):
        raise SchemaError("root: object bekleniyor")
    project = data.get("project")
    if not isinstance(project, dict):
        raise SchemaError("project: object bekleniyor")
    _require_str(project, "name", 3, 80)
    key = _require_str(project, "key", 2, 10)
    if not key.isupper() or not key.isalnum():
        raise SchemaError(f"project.key: sadece UPPERCASE alphanumeric ({key})")
    _require_str(project, "description", 20, 600)
    _check_sprints_list(data)
    # scaffold için sprints 2-4 katı — tekrar kontrol
    n = len(data["sprints"])
    if not (2 <= n <= 4):
        raise SchemaError(f"scaffold: 2-4 sprint bekleniyor, {n}")


def validate_enrich(data: Any) -> None:
    if not isinstance(data, dict):
        raise SchemaError("root: object bekleniyor")
    _require_str(data, "description", 50, 600)
    ac = _require_str(data, "acceptanceCriteria", 20, 1200)
    if ac.count("- ") < 3:
        raise SchemaError("acceptanceCriteria: en az 3 '- ' maddesi bekleniyor")
    ec = _require_str(data, "edgeCases", 10, 800)
    if ec.count("- ") < 2:
        raise SchemaError("edgeCases: en az 2 '- ' maddesi bekleniyor")
    _require_int(data, "storyPoints", FIB)


def validate_plan(data: Any) -> None:
    if not isinstance(data, dict):
        raise SchemaError("root: object bekleniyor")
    _check_sprints_list(data)
    n = len(data["sprints"])
    if not (1 <= n <= 3):
        raise SchemaError(f"plan: 1-3 sprint bekleniyor, {n}")


_VALIDATORS = {
    "scaffold-project": validate_scaffold,
    "enrich-issue": validate_enrich,
    "generate-plan": validate_plan,
}


def validate(feature: str, data: Any) -> None:
    fn = _VALIDATORS.get(feature)
    if fn is None:
        raise SchemaError(f"bilinmeyen feature: {feature}")
    fn(data)
