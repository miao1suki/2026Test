#!/usr/bin/env bash
set -euo pipefail

group_id="${1:?usage: configure-napcat-sender.sh QQ_GROUP_ID}"
config_path="/etc/qq-github-notifier/config.json"
temporary_path="${config_path}.tmp"
token_path="/etc/qq-github-notifier/onebot-token"

if [[ ! -s "$token_path" ]]; then
  echo "OneBot token not found: $token_path" >&2
  exit 1
fi

jq --argjson group_id "$group_id" '
  .onebotBaseUrl = "http://127.0.0.1:3001"
  | .onebotTokenPath = "/etc/qq-github-notifier/onebot-token"
  | .onebotGroupId = $group_id
  | .reportTimeZone = "Asia/Shanghai"
  | .recoveryRequestPath = "/var/lib/qq-github-notifier/napcat-recovery.request"
' "$config_path" > "$temporary_path"

chown --reference="$config_path" "$temporary_path"
chmod --reference="$config_path" "$temporary_path"
mv "$temporary_path" "$config_path"
chown root:qqgitbot "$token_path"
chmod 0640 "$token_path"

systemctl restart qq-github-notifier.service
echo "Notifier configured to send proactive messages through NapCat."
