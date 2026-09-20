#!/usr/bin/env bash
set -euo pipefail

source_directory="${1:?usage: install-recovery.sh SOURCE_DIRECTORY}"

install -m 0755 "$source_directory/recover-napcat.sh" /opt/napcat/recover-napcat.sh
install -m 0755 "$source_directory/send-recovery-email.py" \
  /opt/napcat/send-recovery-email.py
install -m 0755 "$source_directory/configure-email-alert.sh" \
  /opt/napcat/configure-email-alert.sh
install -m 0644 "$source_directory/napcat-recovery.service" \
  /etc/systemd/system/napcat-recovery.service
install -m 0644 "$source_directory/napcat-recovery.path" \
  /etc/systemd/system/napcat-recovery.path

systemctl daemon-reload
systemctl enable --now napcat-recovery.path
echo "NapCat recovery watcher installed."
