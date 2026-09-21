#!/usr/bin/env bash
set -euo pipefail

source_dir="${1:?usage: install-recovery.sh SOURCE_DIR}"
install -d -o root -g root -m 0755 /opt/snowluma
install -o root -g root -m 0755 "$source_dir/check-snowluma-health.sh" /opt/snowluma/check-snowluma-health.sh
install -o root -g root -m 0755 "$source_dir/recover-snowluma.sh" /opt/snowluma/recover-snowluma.sh
install -o root -g root -m 0755 "$source_dir/send-recovery-email.py" /opt/snowluma/send-recovery-email.py
install -o root -g root -m 0644 "$source_dir/snowluma-healthcheck.service" /etc/systemd/system/snowluma-healthcheck.service
install -o root -g root -m 0644 "$source_dir/snowluma-healthcheck.timer" /etc/systemd/system/snowluma-healthcheck.timer
install -o root -g root -m 0644 "$source_dir/snowluma-recovery.service" /etc/systemd/system/snowluma-recovery.service
install -o root -g root -m 0644 "$source_dir/snowluma-recovery.path" /etc/systemd/system/snowluma-recovery.path
systemctl daemon-reload
systemctl enable --now snowluma-healthcheck.timer snowluma-recovery.path
echo "SnowLuma recovery watchers installed."
