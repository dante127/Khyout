#!/usr/bin/env bash
# Nightly PostgreSQL backup with 7-day retention.
# Schedule example: 0 2 * * * /opt/khyout/deploy/scripts/backup.sh
set -euo pipefail

BACKUP_DIR="${BACKUP_DIR:-/var/backups/khyout}"
CONTAINER="${CONTAINER:-khyout-postgres-1}"
RETENTION_DAYS="${RETENTION_DAYS:-7}"

mkdir -p "$BACKUP_DIR"
STAMP="$(date +%Y%m%d_%H%M%S)"

docker exec "$CONTAINER" pg_dump -U khyout khyout | gzip > "$BACKUP_DIR/khyout_${STAMP}.sql.gz"
find "$BACKUP_DIR" -name 'khyout_*.sql.gz' -mtime "+${RETENTION_DAYS}" -delete

echo "Backup written: ${BACKUP_DIR}/khyout_${STAMP}.sql.gz"
