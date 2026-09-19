#!/usr/bin/env bash
set -euo pipefail

account_id="${1:?usage: set-account.sh QQ_ACCOUNT_ID}"
env_path="/opt/napcat/.env"

if grep -q '^NAPCAT_ACCOUNT=' "$env_path"; then
  sed -i "s/^NAPCAT_ACCOUNT=.*/NAPCAT_ACCOUNT=${account_id}/" "$env_path"
else
  printf 'NAPCAT_ACCOUNT=%s\n' "$account_id" >> "$env_path"
fi

docker compose \
  --project-directory /opt/napcat \
  --env-file "$env_path" \
  -f /opt/napcat/compose.yaml \
  up -d

echo "NapCat account fixed for quick login."
