#!/bin/sh
# Dumps every Flowgent Postgres database to /backups and prunes old dumps.
#
# Each service owns its own database, so each gets its own dump -- a single
# cluster-wide dump would be useless for restoring one service in isolation.
# Dumps use pg_dump's custom format (-Fc), which pg_restore can read selectively
# and which compresses by default.
#
# Intended to run inside the `backup` container (docker-compose.backup.yml), where
# every *-db host resolves on the compose network. Can also be run by hand:
#   docker compose -f docker-compose.yml -f docker-compose.backup.yml run --rm backup
set -eu

BACKUP_ROOT="${BACKUP_ROOT:-/backups}"
RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-7}"
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"

# service_name:host:database:user:password_env
DATABASES="
identity:identity-db:identitydb:identity_user:IDENTITY_DB_PASS
project:project-db:projectdb:project_user:PROJECT_DB_PASS
issue:issue-db:issuedb:issue_user:ISSUE_DB_PASS
sprint:sprint-db:sprintdb:sprint_user:SPRINT_DB_PASS
notification:notification-db:notificationdb:notification_user:NOTIFICATION_DB_PASS
storage:storage-db:storagedb:storage_user:STORAGE_DB_PASS
ai:ai-db:aidb:ai_user:AI_DB_PASS
"

failures=0
succeeded=0

for entry in $DATABASES; do
    [ -z "$entry" ] && continue

    name="$(echo "$entry" | cut -d: -f1)"
    host="$(echo "$entry" | cut -d: -f2)"
    db="$(echo "$entry" | cut -d: -f3)"
    user="$(echo "$entry" | cut -d: -f4)"
    pass_var="$(echo "$entry" | cut -d: -f5)"

    # Indirect lookup so the password never appears in the process list.
    pass="$(eval "printf '%s' \"\${$pass_var:-}\"")"
    if [ -z "$pass" ]; then
        echo "SKIP  $name -- $pass_var is not set" >&2
        failures=$((failures + 1))
        continue
    fi

    target_dir="$BACKUP_ROOT/$name"
    mkdir -p "$target_dir"
    target="$target_dir/${name}_${STAMP}.dump"

    # Write to .part first so a crashed dump is never mistaken for a good one.
    if PGPASSWORD="$pass" pg_dump \
            --host="$host" --port=5432 --username="$user" \
            --dbname="$db" --format=custom --no-owner --no-acl \
            --file="$target.part" 2>"$target.err"; then
        mv "$target.part" "$target"
        rm -f "$target.err"
        echo "OK    $name -> $target ($(du -h "$target" | cut -f1))"
        succeeded=$((succeeded + 1))
    else
        echo "FAIL  $name -- $(cat "$target.err" 2>/dev/null | tail -1)" >&2
        rm -f "$target.part"
        failures=$((failures + 1))
    fi
done

# Prune only after a successful run, so a broken backup job cannot quietly delete
# the last good copies while producing nothing to replace them.
if [ "$failures" -eq 0 ]; then
    deleted="$(find "$BACKUP_ROOT" -name '*.dump' -type f -mtime "+$RETENTION_DAYS" -print -delete | wc -l)"
    echo "Pruned $deleted dump(s) older than $RETENTION_DAYS day(s)."
else
    echo "Retention skipped: $failures database(s) failed this run." >&2
fi

echo "Backup finished at $STAMP -- $succeeded succeeded, $failures failed."
[ "$failures" -eq 0 ]
