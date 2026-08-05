#!/bin/sh
# Restores one Flowgent database from a dump produced by backup.sh.
#
# Usage (inside the backup container):
#   restore.sh <service> <dump-file>
#   restore.sh identity /backups/identity/identity_20260805T120000Z.dump
#   restore.sh identity latest        # newest dump for that service
#
# From the host:
#   docker compose -f docker-compose.yml -f docker-compose.backup.yml \
#     run --rm backup restore.sh identity latest
#
# Restoring is destructive: --clean drops the objects it is about to recreate.
# Stop the owning service first, or it will keep writing into a half-restored
# schema. The script refuses to run unless RESTORE_CONFIRM=yes is set.
set -eu

BACKUP_ROOT="${BACKUP_ROOT:-/backups}"

usage() {
    echo "Usage: restore.sh <service> <dump-file|latest>" >&2
    echo "Services: identity project issue sprint notification storage ai" >&2
    exit 2
}

[ $# -eq 2 ] || usage

service="$1"
dump="$2"

case "$service" in
    identity)     host=identity-db;     db=identitydb;     user=identity_user;     pass_var=IDENTITY_DB_PASS ;;
    project)      host=project-db;      db=projectdb;      user=project_user;      pass_var=PROJECT_DB_PASS ;;
    issue)        host=issue-db;        db=issuedb;        user=issue_user;        pass_var=ISSUE_DB_PASS ;;
    sprint)       host=sprint-db;       db=sprintdb;       user=sprint_user;       pass_var=SPRINT_DB_PASS ;;
    notification) host=notification-db; db=notificationdb; user=notification_user; pass_var=NOTIFICATION_DB_PASS ;;
    storage)      host=storage-db;      db=storagedb;      user=storage_user;      pass_var=STORAGE_DB_PASS ;;
    ai)           host=ai-db;           db=aidb;           user=ai_user;           pass_var=AI_DB_PASS ;;
    *) echo "Unknown service: $service" >&2; usage ;;
esac

if [ "$dump" = "latest" ]; then
    dump="$(ls -1t "$BACKUP_ROOT/$service"/*.dump 2>/dev/null | head -1 || true)"
    [ -n "$dump" ] || { echo "No dumps found in $BACKUP_ROOT/$service" >&2; exit 1; }
    echo "Using newest dump: $dump"
fi

[ -f "$dump" ] || { echo "Dump not found: $dump" >&2; exit 1; }

pass="$(eval "printf '%s' \"\${$pass_var:-}\"")"
[ -n "$pass" ] || { echo "$pass_var is not set" >&2; exit 1; }

if [ "${RESTORE_CONFIRM:-}" != "yes" ]; then
    echo "Refusing to restore: this DROPS and recreates objects in '$db' on '$host'." >&2
    echo "Stop the $service service first, then re-run with RESTORE_CONFIRM=yes." >&2
    exit 3
fi

echo "Restoring $db on $host from $dump ..."

# --clean --if-exists so a restore onto a populated database is repeatable.
# --single-transaction so a failure leaves the database untouched rather than half-applied.
PGPASSWORD="$pass" pg_restore \
    --host="$host" --port=5432 --username="$user" \
    --dbname="$db" \
    --clean --if-exists --no-owner --no-acl \
    --single-transaction \
    "$dump"

echo "Restore of $service completed. Start the service and verify before taking traffic."
