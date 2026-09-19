# NapCat QQ notification transport

NapCat provides the OneBot 11 transport used to send GitHub push notifications
to a QQ group. It runs as an isolated Docker container on the cloud server.

Security defaults:

- WebUI `6099` and OneBot HTTP `3001` bind only to `127.0.0.1`.
- Persistent QQ and NapCat data live under `/opt/napcat/data` on the server.
- WebUI and OneBot tokens are stored outside the repository.
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
