#!/usr/bin/env bash
set -euo pipefail

state_directory=/var/lib/qq-github-notifier
request_path=$state_directory/napcat-recovery.request
alerted_path=$state_directory/napcat-manual-login.alerted
token_path=/etc/qq-github-notifier/onebot-token

is_healthy() {
  local response token
  [[ -s "$token_path" ]] || return 1
  token="$(cat "$token_path")"
  response="$(curl -fsS --max-time 3 \
    --config <(printf 'header = "Authorization: Bearer %s"\n' "$token") \
    http://127.0.0.1:3001/get_status 2>/dev/null)" \
    || return 1
  jq -e \
    '.status == "ok" and .retcode == 0 and .data.online == true and .data.good == true' \
    >/dev/null <<<"$response"
}

if is_healthy; then
  rm -f "$alerted_path"
  exit 0
fi

# Confirm a transient API hiccup before requesting a container restart.
sleep 10
if is_healthy; then
  rm -f "$alerted_path"
  exit 0
fi

mkdir -p "$state_directory"
touch "$request_path"
echo "NapCat health check failed twice; automatic recovery requested." >&2
