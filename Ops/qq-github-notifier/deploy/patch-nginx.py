#!/usr/bin/env python3
from __future__ import annotations

import os
import shutil
import sys
from datetime import datetime, timezone
from pathlib import Path


if len(sys.argv) != 2:
    raise SystemExit("Usage: patch-nginx.py <nginx-site-path>")

site_path = Path(sys.argv[1])
include_line = "    include /etc/nginx/snippets/qq-github-notifier.conf;\n"
anchor = "    # Secret-token Wake-on-LAN page. The token is validated by the local service.\n"
contents = site_path.read_text(encoding="utf-8")

if include_line in contents:
    print("Nginx site already contains the notifier include.")
    raise SystemExit(0)

if contents.count(anchor) != 1:
    raise SystemExit("Expected HTTPS server anchor was not found exactly once; refusing to edit.")

timestamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
backup_path = site_path.with_name(f"{site_path.name}.before-qq-github-notifier.{timestamp}")
shutil.copy2(site_path, backup_path)

updated = contents.replace(anchor, f"{include_line}\n{anchor}", 1)
temporary_path = site_path.with_name(f".{site_path.name}.qq-github-notifier.tmp")
temporary_path.write_text(updated, encoding="utf-8")
os.replace(temporary_path, site_path)
print(f"Nginx site updated; backup: {backup_path}")
