#!/usr/bin/env bash
set -euo pipefail

token="$(cat /etc/qq-github-notifier/onebot-token)"
authorization="Authorization: Bearer ${token}"

curl -fsS -H "$authorization" http://127.0.0.1:3001/get_login_info \
  | jq '{status, retcode, data: {nickname: .data.nickname}}'
curl -fsS -H "$authorization" http://127.0.0.1:3001/get_group_list \
  | jq '{status, retcode, groups: [.data[] | {group_id, group_name}]}'
