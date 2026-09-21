#!/usr/bin/env python3
import argparse
import json
import smtplib
import ssl
from datetime import datetime
from email.message import EmailMessage
from pathlib import Path
from zoneinfo import ZoneInfo


CONFIG_PATH = Path("/etc/napcat-alert/email.json")
STATE_PATH = Path("/var/lib/qq-github-notifier/state.json")


def load_json(path: Path) -> dict:
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def queued_count() -> int:
    try:
        queue = load_json(STATE_PATH).get("queue", [])
        return len(queue) if isinstance(queue, list) else 0
    except (OSError, ValueError):
        return -1


def build_message(config: dict, event: str, reason: str) -> EmailMessage:
    now = datetime.now(ZoneInfo("Asia/Shanghai")).strftime("%Y-%m-%d %H:%M:%S")
    count = queued_count()
    message = EmailMessage()
    message["From"] = config.get("fromAddress") or config["username"]
    message["To"] = ", ".join(config["recipients"])

    if event == "test":
        message["Subject"] = "[2026Test] QQ 机器人邮件告警配置成功"
        body = [
            "2026Test QQ 推送机器人的邮件告警已经配置成功。",
            "",
            f"时间：{now}（北京时间）",
            "自动恢复成功时不会发送邮件。",
            "只有自动恢复失败、需要人工登录时才会告警。",
        ]
    else:
        message["Subject"] = "[2026Test] SnowLuma QQ 推送机器人需要人工登录"
        body = [
            "SnowLuma 自动恢复失败，需要通过 noVNC 完成人工登录或验证码。",
            "",
            f"时间：{now}（北京时间）",
            f"原因：{reason}",
            f"待发送 Git 通知：{count if count >= 0 else '未知'} 条",
            "服务器：62.234.93.20",
            "",
            "通知队列已持久化，登录恢复后会自动补发。",
        ]

    message.set_content("\n".join(body))
    return message


def send(config: dict, message: EmailMessage) -> None:
    host = config["smtpHost"]
    port = int(config["smtpPort"])
    security = config["security"]
    context = ssl.create_default_context()

    if security == "ssl":
        with smtplib.SMTP_SSL(host, port, timeout=20, context=context) as client:
            client.login(config["username"], config["password"])
            client.send_message(message)
    elif security == "starttls":
        with smtplib.SMTP(host, port, timeout=20) as client:
            client.ehlo()
            client.starttls(context=context)
            client.ehlo()
            client.login(config["username"], config["password"])
            client.send_message(message)
    else:
        raise ValueError("security must be ssl or starttls")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--event", choices=("test", "recovery-failed"), required=True)
    parser.add_argument("--reason", default="未提供详细原因")
    parser.add_argument("--config", type=Path, default=CONFIG_PATH)
    args = parser.parse_args()

    config = load_json(args.config)
    send(config, build_message(config, args.event, args.reason))
    print("SnowLuma recovery email sent.")


if __name__ == "__main__":
    main()
