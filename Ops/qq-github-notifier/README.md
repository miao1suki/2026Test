# QQ GitHub Push Notifier

This service keeps an official QQ bot connected to the QQ WebSocket gateway and
forwards GitHub `push` webhooks for `miao1suki/2026Test` to one QQ group.

## Behavior

- The first group that mentions the bot becomes the notification target.
- Every GitHub branch push is queued and delivered in order.
- Tag pushes and non-push GitHub events are ignored.
- GitHub signatures are verified with `X-Hub-Signature-256`.
- Delivery IDs are persisted to prevent duplicate QQ notifications.
- Credentials and runtime state live outside the repository.

## Server paths

- Application: `/opt/qq-github-notifier/app`
- Private config: `/etc/qq-github-notifier/config.json`
- Runtime state: `/var/lib/qq-github-notifier/state.json`
- Public webhook: `https://meowgame.cloud/webhooks/github`

The deployment uses an isolated Node.js runtime under
`/opt/qq-github-notifier/node`, so it does not change the server's system Node.js
installation or existing applications.
