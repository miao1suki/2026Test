# SnowLuma QQ notification transport

SnowLuma is the OneBot v11 transport used by the GitHub notifier. It runs the
official Linux QQ client and SnowLuma's native hook in Docker, with all
management and bot ports bound to localhost on the server.

## Ports and persistence

- `127.0.0.1:6081` — noVNC desktop used for QR/device verification.
- `127.0.0.1:5099` — SnowLuma WebUI.
- `127.0.0.1:3001` — OneBot HTTP API consumed by the notifier (container port
  `3000`).
- `127.0.0.1:3002` — OneBot WebSocket, reserved for operator use.
- `/opt/snowluma/data` — SnowLuma and OneBot configuration.
- `/opt/snowluma/config` — QQ client configuration.
- `/opt/snowluma/qqdata` — QQ login state and cache.

Do not delete these directories during upgrades. SnowLuma's Docker image needs
`SYS_PTRACE`, `seccomp=unconfined`, a 1 GiB shared-memory mount, and a distinct
HOME/data path for each QQ account.

## First login

Use `deploy/open-webui-tunnel.ps1` to forward noVNC and WebUI through SSH. The
operator completes QR, device, or SMS verification in the noVNC desktop. Do
not expose ports 6081, 5099, 3001, or 3002 publicly.

## OneBot contract

`deploy/configure-onebot.sh` writes the existing root-only OneBot bearer token
into SnowLuma's HTTP server configuration without printing it. The notifier
continues using `/etc/qq-github-notifier/onebot-token`, so queue and webhook
state are not moved or recreated.

## Recovery

The SnowLuma health timer checks `/get_status` and `/get_login_info`. A failed
health check can request one restart subject to the existing 30-minute
cooldown. Failed automatic recovery sends one email and leaves the persistent
notifier queue intact for manual login recovery.
