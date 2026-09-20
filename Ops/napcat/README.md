# NapCat QQ notification transport

NapCat provides the OneBot 11 transport used to send GitHub push notifications
to a QQ group. It runs as an isolated Docker container on the cloud server.

Security defaults:

- WebUI `6099` and OneBot HTTP `3001` bind only to `127.0.0.1`.
- Persistent QQ and NapCat data live under `/opt/napcat/data` on the server.
- WebUI and OneBot tokens are stored outside the repository.
- The optional fallback login credential is stored only in root-readable
  `/opt/napcat/.env`; never commit it or send it through chat.
- The container has a 1 GiB memory limit and rotating logs.

The WebUI is accessed through an SSH tunnel during setup:

```text
http://127.0.0.1:6099/webui
```

NapCat is an unofficial QQ client. Use a dedicated notification account and
expect occasional QR-code reauthentication or account risk controls.

For a Tencent Cloud server that cannot reach Docker Hub directly, install
`docker-daemon.tencent-cloud.json` as `/etc/docker/daemon.json`, then restart
Docker before pulling the image.

After the notification QQ account has logged in, run
`deploy/configure-onebot.sh QQ_ACCOUNT_ID` as root. The script creates a private
OneBot token, enables the HTTP API on container port `3001`, and restarts
NapCat. The published host port remains restricted to `127.0.0.1` by Compose.

## Automatic recovery

QQNT can occasionally remain apparently online while `sendMsg` returns
`1006514` (`网络连接异常`). The notifier writes a recovery request after three
matching failures. `napcat-recovery.path` then starts a root-owned recovery
service which:

1. enforces a 30-minute restart cooldown;
2. restarts NapCat;
3. waits for OneBot login to recover; and
4. restarts the notifier so its persistent queue drains immediately.

Install the watcher with:

```bash
sudo ./deploy/install-recovery.sh ./deploy
```

Automatic login requires `NAPCAT_QUICK_PASSWORD_MD5`. Configure it from the
operator's Windows computer without printing or uploading the plaintext:

```powershell
.\deploy\set-quick-password.ps1 -SshKeyPath 'D:\path\to\key.pem'
```

The script prompts locally, hashes the password in memory, and sends only the
password-equivalent MD5 over SSH standard input. Tencent may still require a
captcha or new-device confirmation; those checks must be completed manually in
WebUI and are intentionally not bypassed.

## Failed-recovery email alert

Successful automatic recovery is silent. If the fallback credential is absent,
or NapCat still has not logged in 120 seconds after a recovery restart, the
server sends one email explaining that WebUI login is required. The existing
30-minute recovery cooldown and the notifier's persistent queue prevent alert
spam and preserve Git notifications until login returns.

The setup helper supports QQ Mail, 163 Mail, and Gmail SMTP presets. Run it on
the operator's Windows computer:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
  '.\deploy\set-email-alert.ps1' `
  -Provider QQ `
  -Recipient 'operator@example.com' `
  -SshKeyPath 'D:\path\to\key.pem'
```

The helper prompts for the sender address and its SMTP authorization code or app
password, sends a test email, and installs the configuration only if the test
succeeds. Unlike the QQ quick-login hash, SMTP credentials must remain available
to the mail client. They are stored only in root-readable
`/etc/napcat-alert/email.json` (directory mode `0700`, file mode `0600`) and must
never be committed or pasted into chat.
