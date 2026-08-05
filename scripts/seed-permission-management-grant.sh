#!/usr/bin/env bash
# Usage: scripts/seed-permission-management-grant.sh <principalId>
#
# One-time, out-of-band bootstrap of a deployment's very first `permission-management` grant
# (requirement-spec.md §6 Decision item 1: "the first permission-management grant for a
# deployment is seeded out-of-band by a one-time ops migration/seed script, avoiding an
# unbounded regress of 'who grants the first grant'"). ADM-1's own endpoint cannot be the
# mechanism that authorizes its own first caller, since issuing any grant already requires the
# caller to hold a live permission-management grant themselves.
#
# Idempotent: skips if a live (non-revoked) permission-management grant already exists for this
# principal — matches uq_admin_permission_grants_live, the same partial unique index the
# application layer relies on. Run scripts/migrate.sh first if admin_permission_grants doesn't
# exist yet.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

if [ -f .env ]; then
  set -a
  source .env
  set +a
fi

PRINCIPAL_ID="${1:-}"
if [ -z "$PRINCIPAL_ID" ]; then
  echo "Usage: $0 <principalId>" >&2
  exit 1
fi

command -v psql >/dev/null 2>&1 || { echo "psql is required but not found on PATH." >&2; exit 1; }

CONN_STRING="${ADMIN_DB_CONNECTION_STRING:-Host=localhost;Port=5432;Database=kart_admin;Username=postgres;Password=postgres}"

declare -A conn
IFS=';' read -ra PAIRS <<< "$CONN_STRING"
for pair in "${PAIRS[@]}"; do
  [ -z "$pair" ] && continue
  key="${pair%%=*}"
  val="${pair#*=}"
  conn["${key,,}"]="$val"
done

export PGHOST="${conn[host]:-localhost}"
export PGPORT="${conn[port]:-5432}"
export PGDATABASE="${conn[database]:-kart_admin}"
export PGUSER="${conn[username]:-postgres}"
export PGPASSWORD="${conn[password]:-postgres}"

EXISTING=$(psql -X -A -t -v ON_ERROR_STOP=1 -v pid="$PRINCIPAL_ID" <<'SQL' | tr -d '[:space:]'
SELECT 1 FROM admin_permission_grants WHERE principal_id = :'pid' AND category = 'permission-management' AND revoked_at IS NULL;
SQL
)

if [ "$EXISTING" = "1" ]; then
  echo "Principal $PRINCIPAL_ID already has a live permission-management grant. Skipping."
  exit 0
fi

psql -X -v ON_ERROR_STOP=1 -v pid="$PRINCIPAL_ID" <<'SQL'
INSERT INTO admin_permission_grants (grant_id, principal_id, category, granted_at, granted_by, revoked_at, version)
VALUES (gen_random_uuid(), :'pid', 'permission-management', now(), 'seed-script', NULL, 1);
SQL

echo "Seeded permission-management grant for principal $PRINCIPAL_ID."
