#!/usr/bin/env bash
set -euo pipefail

uin="${1:?usage: configure-onebot.sh QQ_ACCOUNT_ID}"
[[ "$uin" =~ ^[0-9]+$ ]] || { echo "QQ account ID must be numeric" >&2; exit 2; }

config_dir=/opt/snowluma/data/config
token_path=/etc/qq-github-notifier/onebot-token
install -d -o 1000 -g 1000 -m 0700 "$config_dir"
[[ -s "$token_path" ]] || { echo "OneBot token file is missing." >&2; exit 1; }

tmp_global="$(mktemp "$config_dir/onebot.json.XXXXXX")"
tmp_account="$(mktemp "$config_dir/onebot_${uin}.json.XXXXXX")"
trap 'rm -f "$tmp_global" "$tmp_account"' EXIT

jq -n --rawfile token "$token_path" '
  {
      networks: {
        httpServers: [{
          name: "notifier-http",
          enabled: true,
          host: "0.0.0.0",
          port: 3000,
          path: "/",
          accessToken: ($token | sub("\\r?\\n$"; "")),
          messageFormat: "array",
          reportSelfMessage: false
        }],
        httpClients: [],
        wsServers: [],
        wsClients: []
      },
      musicSignUrl: ""
    }
' /dev/null > "$tmp_global"
cp "$tmp_global" "$tmp_account"
chown 1000:1000 "$tmp_global" "$tmp_account"
chmod 0600 "$tmp_global" "$tmp_account"
mv "$tmp_global" "$config_dir/onebot.json"
mv "$tmp_account" "$config_dir/onebot_${uin}.json"
trap - EXIT
echo "SnowLuma OneBot HTTP configuration installed for account $uin."
