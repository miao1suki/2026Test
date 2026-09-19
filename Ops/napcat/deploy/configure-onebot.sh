#!/usr/bin/env bash
set -euo pipefail

account_id="${1:?usage: configure-onebot.sh QQ_ACCOUNT_ID}"
config_path="/opt/napcat/data/config/onebot11_${account_id}.json"
token_path="/etc/qq-github-notifier/onebot-token"

if [[ ! -f "$config_path" ]]; then
  echo "NapCat account configuration not found: $config_path" >&2
  exit 1
fi

install -d -o root -g qqgitbot -m 0750 "$(dirname "$token_path")"
if [[ ! -s "$token_path" ]]; then
  umask 077
  openssl rand -hex 32 > "$token_path"
fi
chown root:qqgitbot "$token_path"
chmod 0640 "$token_path"

token="$(cat "$token_path")"
temporary_path="${config_path}.tmp"

jq --arg token "$token" '
  .network.httpServers = [{
    name: "github-notifier",
    enable: true,
    port: 3001,
    host: "0.0.0.0",
    enableCors: false,
    enableWebsocket: false,
    messagePostFormat: "array",
    token: $token,
    debug: false
  }]
' "$config_path" > "$temporary_path"

chown --reference="$config_path" "$temporary_path"
chmod --reference="$config_path" "$temporary_path"
mv "$temporary_path" "$config_path"
docker restart napcat >/dev/null

echo "NapCat OneBot HTTP server configured on host port 127.0.0.1:3001."
