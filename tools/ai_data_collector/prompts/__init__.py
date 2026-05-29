from . import scaffold_project, enrich_issue, generate_plan, agent

FEATURE_MODULES = {
    "scaffold-project": scaffold_project,
    "enrich-issue": enrich_issue,
    "generate-plan": generate_plan,
    # agent: synthetic generation tool-call format'ında template tabanlı; bkz. agent_synth.py
    "agent": agent,
}
