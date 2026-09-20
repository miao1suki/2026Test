#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 2 ]]; then
  echo "Usage: install.sh <source-directory> <credentials-json>" >&2
  exit 2
fi

source_directory=$1
credentials_json=$2
service_root=/opt/qq-github-notifier
application_directory=$service_root/app
runtime_directory=$service_root/runtimes
config_directory=/etc/qq-github-notifier
config_path=$config_directory/config.json
state_directory=/var/lib/qq-github-notifier

if [[ ! -f "$source_directory/app.mjs" || ! -f "$source_directory/package.json" ]]; then
  echo "Deployment source is incomplete." >&2
  exit 2
fi
if [[ ! -f "$credentials_json" ]]; then
  echo "Credentials file does not exist." >&2
  exit 2
fi

if ! id qqgitbot >/dev/null 2>&1; then
  useradd --system --home-dir "$state_directory" --shell /usr/sbin/nologin qqgitbot
fi

install -d -m 0755 "$service_root" "$application_directory" "$runtime_directory"
install -d -m 0750 -o root -g qqgitbot "$config_directory"
install -d -m 0750 -o qqgitbot -g qqgitbot "$state_directory"

node_archive=$(curl -fsSL https://nodejs.org/dist/latest-v22.x/SHASUMS256.txt \
  | awk '$2 ~ /^node-v22\..*-linux-x64\.tar\.xz$/ {print $2; exit}')
if [[ -z "$node_archive" ]]; then
  echo "Could not resolve the latest Node.js 22 runtime." >&2
  exit 1
fi

node_version=${node_archive%-linux-x64.tar.xz}
node_install_directory=$runtime_directory/$node_version
if [[ ! -x "$node_install_directory/bin/node" ]]; then
  archive_path=$(mktemp "/tmp/${node_archive}.XXXXXX")
  extraction_directory=$(mktemp -d "/tmp/${node_version}.XXXXXX")
  curl -fsSL "https://nodejs.org/dist/latest-v22.x/$node_archive" -o "$archive_path"
  expected_hash=$(curl -fsSL https://nodejs.org/dist/latest-v22.x/SHASUMS256.txt \
    | awk -v file="$node_archive" '$2 == file {print $1; exit}')
  actual_hash=$(sha256sum "$archive_path" | awk '{print $1}')
  if [[ -z "$expected_hash" || "$actual_hash" != "$expected_hash" ]]; then
    echo "Node.js archive checksum verification failed." >&2
    exit 1
  fi
  tar -xJf "$archive_path" --strip-components=1 -C "$extraction_directory"
  mv "$extraction_directory" "$node_install_directory"
  chmod -R a+rX "$node_install_directory"
  rm -f "$archive_path"
fi

chmod -R a+rX "$node_install_directory"

ln -sfn "$node_install_directory" "$service_root/node.next"
mv -Tf "$service_root/node.next" "$service_root/node"

install -m 0644 "$source_directory/app.mjs" "$application_directory/app.mjs"
install -m 0644 "$source_directory/package.json" "$application_directory/package.json"

PATH="$service_root/node/bin:$PATH" npm install \
  --prefix "$application_directory" \
  --omit=dev \
  --ignore-scripts \
  --no-audit \
  --no-fund

generated_config=$(mktemp /tmp/qq-github-notifier-config.XXXXXX)
python3 - "$credentials_json" "$config_path" "$generated_config" <<'PY'
import json
import os
import secrets
import sys

credentials_path, existing_path, output_path = sys.argv[1:]
with open(credentials_path, "r", encoding="utf-8") as handle:
    credentials = json.load(handle)

app_id = str(credentials.get("appId", "")).strip()
app_secret = str(credentials.get("appSecret", "")).strip()
if not app_id or not app_secret:
    raise SystemExit("Credentials JSON must contain appId and appSecret.")

existing = {}
if os.path.exists(existing_path):
    with open(existing_path, "r", encoding="utf-8") as handle:
        existing = json.load(handle)

config = {
    "appId": app_id,
    "appSecret": app_secret,
    "githubWebhookSecret": existing.get("githubWebhookSecret") or secrets.token_hex(32),
    "repository": "miao1suki/2026Test",
    "apiBaseUrl": "https://api.bot.qq.com",
    "listenHost": "127.0.0.1",
    "listenPort": 8787,
    "statePath": "/var/lib/qq-github-notifier/state.json",
    "onebotBaseUrl": existing.get("onebotBaseUrl", "http://127.0.0.1:3001"),
    "onebotTokenPath": existing.get("onebotTokenPath", "/etc/qq-github-notifier/onebot-token"),
    "onebotGroupId": existing.get("onebotGroupId", 0),
    "reportTimeZone": existing.get("reportTimeZone", "Asia/Shanghai"),
    "recoveryRequestPath": existing.get(
        "recoveryRequestPath",
        "/var/lib/qq-github-notifier/napcat-recovery.request",
    ),
}
with open(output_path, "w", encoding="utf-8") as handle:
    json.dump(config, handle, ensure_ascii=False, indent=2)
    handle.write("\n")
PY

install -m 0640 -o root -g qqgitbot "$generated_config" "$config_path"
rm -f "$generated_config"

install -m 0644 "$source_directory/deploy/qq-github-notifier.service" \
  /etc/systemd/system/qq-github-notifier.service
systemctl daemon-reload
systemctl enable --now qq-github-notifier.service

echo "qq-github-notifier installation completed."
