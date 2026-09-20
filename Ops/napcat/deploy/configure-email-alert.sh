#!/usr/bin/env bash
set -euo pipefail

config_directory=/etc/napcat-alert
config_path=$config_directory/email.json
temporary_path="$(mktemp /tmp/napcat-email.XXXXXX)"
trap 'rm -f "$temporary_path"' EXIT

cat > "$temporary_path"
python3 - "$temporary_path" <<'PY'
import json
import sys

path = sys.argv[1]
with open(path, "r", encoding="utf-8") as handle:
    config = json.load(handle)

required = ("smtpHost", "smtpPort", "security", "username", "password", "recipients")
for key in required:
    if not config.get(key):
        raise SystemExit(f"Missing email configuration field: {key}")
if config["security"] not in ("ssl", "starttls"):
    raise SystemExit("security must be ssl or starttls")
if not isinstance(config["recipients"], list) or not config["recipients"]:
    raise SystemExit("recipients must be a non-empty array")
PY

install -d -o root -g root -m 0700 "$config_directory"
/opt/napcat/send-recovery-email.py --event test --config "$temporary_path"
install -o root -g root -m 0600 "$temporary_path" "$config_path"
echo "Root-only NapCat email alert configuration installed."
