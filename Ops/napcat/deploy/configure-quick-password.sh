#!/usr/bin/env bash
set -euo pipefail

env_path=/opt/napcat/.env
compose_path=/opt/napcat/compose.yaml

IFS= read -r password_md5
if [[ ! "$password_md5" =~ ^[[:xdigit:]]{32}$ ]]; then
  echo "Expected one 32-character MD5 value on standard input." >&2
  exit 2
fi
password_md5="${password_md5,,}"

temporary_path="$(mktemp /opt/napcat/.env.XXXXXX)"
trap 'rm -f "$temporary_path"' EXIT
grep -v '^NAPCAT_QUICK_PASSWORD_MD5=' "$env_path" > "$temporary_path"
printf 'NAPCAT_QUICK_PASSWORD_MD5=%s\n' "$password_md5" >> "$temporary_path"
chown root:root "$temporary_path"
chmod 0600 "$temporary_path"
mv "$temporary_path" "$env_path"
trap - EXIT

docker compose \
  --project-directory /opt/napcat \
  --env-file "$env_path" \
  -f "$compose_path" \
  up -d

echo "Root-only NapCat fallback credential installed; container recreated."
