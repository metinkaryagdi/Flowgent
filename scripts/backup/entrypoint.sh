#!/bin/sh
# Installs the backup scripts, then runs whatever command it was given.
#
# The scripts are bind-mounted read-only and Windows bind mounts do not carry the
# executable bit, so they are copied into PATH and chmod'ed here rather than being
# executed in place. Doing it in the entrypoint means one-off invocations
# (`run --rm backup backup.sh`) get the same setup as the scheduler.
set -eu

SRC_DIR="${BACKUP_SCRIPT_DIR:-/usr/local/bin/flowgent-backup}"

for script in backup.sh restore.sh; do
    cp "$SRC_DIR/$script" "/usr/local/bin/$script"
    chmod +x "/usr/local/bin/$script"
done

if [ $# -gt 0 ]; then
    exec "$@"
fi

# No command given: run as the scheduler.
INTERVAL="${BACKUP_INTERVAL:-86400}"
echo "Backup scheduler started; interval ${INTERVAL}s, retention ${BACKUP_RETENTION_DAYS:-7} day(s)."

while true; do
    backup.sh || echo "Backup run failed; will retry next interval." >&2
    sleep "$INTERVAL"
done
