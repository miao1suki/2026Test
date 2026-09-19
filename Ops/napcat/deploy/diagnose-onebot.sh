#!/usr/bin/env bash
set -euo pipefail

group_id="${1:?usage: diagnose-onebot.sh QQ_GROUP_ID}"
account_id="${2:?usage: diagnose-onebot.sh QQ_GROUP_ID QQ_ACCOUNT_ID}"
token="$(cat /etc/qq-github-notifier/onebot-token)"
authorization="Authorization: Bearer ${token}"
content_type="Content-Type: application/json"

echo STATUS
curl -fsS -H "$authorization" http://127.0.0.1:3001/get_status | jq
echo GROUP
jq -nc --argjson group_id "$group_id" '{group_id: $group_id, no_cache: true}' \
  | curl -fsS -H "$authorization" -H "$content_type" -d @- \
      http://127.0.0.1:3001/get_group_info | jq
echo BOT_MEMBER
jq -nc --argjson group_id "$group_id" --argjson user_id "$account_id" \
  '{group_id: $group_id, user_id: $user_id, no_cache: true}' \
  | curl -fsS -H "$authorization" -H "$content_type" -d @- \
      http://127.0.0.1:3001/get_group_member_info | jq
