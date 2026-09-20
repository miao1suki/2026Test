#!/usr/bin/env bash
set -euo pipefail

request_path=/var/lib/qq-github-notifier/napcat-recovery.request
runtime_directory=/var/lib/napcat-recovery
last_attempt_path=$runtime_directory/last-attempt
env_path=/opt/napcat/.env
token_path=/etc/qq-github-notifier/onebot-token
cooldown_seconds=1800

notify_manual_login() {
  local reason=$1
  if [[ -x /opt/napcat/send-recovery-email.py && -s /etc/napcat-alert/email.json ]]; then
    /opt/napcat/send-recovery-email.py \
      --event recovery-failed \
      --reason "$reason" \
      || echo "NapCat recovery email could not be sent." >&2
  else
    echo "NapCat recovery email is not configured." >&2
  fi
}

exec 9>/run/lock/napcat-recovery.lock
flock -n 9 || exit 0
rm -f "$request_path"

if ! grep -Eq '^NAPCAT_QUICK_PASSWORD_MD5=[[:xdigit:]]{32}$' "$env_path"; then
  echo "NapCat recovery skipped: no root-only quick-login credential is configured." >&2
  notify_manual_login "服务器未配置可用的 NapCat 回退登录凭据。"
  exit 0
fi

install -d -o root -g root -m 0700 "$runtime_directory"
now="$(date +%s)"
last_attempt=0
if [[ -s "$last_attempt_path" ]]; then
  read -r last_attempt < "$last_attempt_path" || last_attempt=0
fi
if (( now - last_attempt < cooldown_seconds )); then
  echo "NapCat recovery skipped: the 30-minute cooldown is active."
  exit 0
fi
printf '%s\n' "$now" > "$last_attempt_path"
chmod 0600 "$last_attempt_path"

echo "Restarting NapCat after repeated QQ transport failures."
docker restart napcat >/dev/null

token="$(cat "$token_path")"
for _ in $(seq 1 60); do
  if response="$(curl -fsS --max-time 3 \
      --config <(printf 'header = "Authorization: Bearer %s"\n' "$token") \
      http://127.0.0.1:3001/get_login_info 2>/dev/null)" \
      && jq -e '.status == "ok" and .retcode == 0' >/dev/null <<<"$response"; then
    echo "NapCat login recovered; restarting the notifier to drain its queue."
    systemctl restart qq-github-notifier.service
    exit 0
  fi
  sleep 2
done

echo "NapCat did not recover automatically; WebUI verification is required." >&2
notify_manual_login "自动重启后 120 秒内仍未恢复登录，可能需要验证码或设备确认。"
exit 1
