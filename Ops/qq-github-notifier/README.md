# QQ GitHub Push Notifier

This service combines an official QQ bot with a local NapCat OneBot transport
for `miao1suki/2026Test` GitHub notifications.

## Behavior

- NapCat proactively delivers every GitHub branch push in queue order.
- Mentioning the official bot replies with a summary of today's received pushes.
- The most recent group that mentions the official bot becomes its query target.
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
