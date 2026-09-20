import { createHmac, timingSafeEqual } from "node:crypto";
import { createServer } from "node:http";
import {
  existsSync,
  mkdirSync,
  readFileSync,
  renameSync,
  writeFileSync,
} from "node:fs";
import { dirname } from "node:path";
import { QQBot } from "@tencent-connect/qqbot-nodejs";

const configPath = process.env.NOTIFIER_CONFIG ?? "/etc/qq-github-notifier/config.json";
const config = JSON.parse(readFileSync(configPath, "utf8"));

for (const key of ["appId", "appSecret", "githubWebhookSecret", "repository"]) {
  if (!config[key] || typeof config[key] !== "string") {
    throw new Error(`Missing required config value: ${key}`);
  }
}

const statePath = config.statePath ?? "/var/lib/qq-github-notifier/state.json";
const listenHost = config.listenHost ?? "127.0.0.1";
const listenPort = Number(config.listenPort ?? 8787);
const recentDeliveryLimit = 200;
const historyLimit = 500;
const maxRequestBytes = 25 * 1024 * 1024;
const onebotBaseUrl = config.onebotBaseUrl ?? "http://127.0.0.1:3001";
const onebotTokenPath = config.onebotTokenPath ?? "/etc/qq-github-notifier/onebot-token";
const onebotGroupId = Number(config.onebotGroupId ?? 0);
const reportTimeZone = config.reportTimeZone ?? "Asia/Shanghai";
const recoveryRequestPath = config.recoveryRequestPath
  ?? "/var/lib/qq-github-notifier/napcat-recovery.request";

const defaultState = {
  groupOpenId: null,
  recentDeliveries: [],
  queue: [],
  history: [],
};

let state = loadState();
let botReady = false;
let queueRunning = false;
let queueTimer = null;

const logger = {
  info: (message) => console.log(`[qq] ${redact(message)}`),
  warn: (message) => console.warn(`[qq] ${redact(message)}`),
  error: (message) => console.error(`[qq] ${redact(message)}`),
  debug: () => {},
};

const bot = new QQBot({
  appId: config.appId,
  appSecret: config.appSecret,
  baseUrl: config.apiBaseUrl ?? "https://api.bot.qq.com",
  logger,
  markdownSupport: false,
});

bot.on("ready", () => {
  botReady = true;
  console.log("QQ gateway connected; bot is online.");
});

bot.on("resumed", () => {
  botReady = true;
  console.log("QQ gateway session resumed.");
});

bot.on("error", (error) => {
  console.error(`QQ gateway error: ${redact(error?.message ?? String(error))}`);
});

bot.on("message", async (_context, message) => {
  if (message.replyTarget?.scope !== "group") {
    return;
  }

  const groupOpenId = message.replyTarget.targetId;
  if (state.groupOpenId !== groupOpenId) {
    state.groupOpenId = groupOpenId;
    saveState();
    console.log("Official bot query group updated from an @ message.");
  }

  const reply = formatTodaySummary();

  try {
    await bot.sendText(message.replyTarget, reply);
  } catch (error) {
    console.error(`Failed to reply to QQ group: ${redact(error?.message ?? String(error))}`);
  }
});

bot.on("rawEvent", (context) => {
  if (![
    "GROUP_ADD_ROBOT",
    "GROUP_MSG_RECEIVE",
  ].includes(context.eventType)) {
    return;
  }

  const groupOpenId = context.data?.group_openid;
  if (!state.groupOpenId && typeof groupOpenId === "string" && groupOpenId) {
    state.groupOpenId = groupOpenId;
    saveState();
    console.log(`Target QQ group captured from ${context.eventType}.`);
    scheduleQueue(0);
  }
});

const httpServer = createServer(async (request, response) => {
  try {
    const url = new URL(request.url ?? "/", `http://${request.headers.host ?? "localhost"}`);

    if (request.method === "GET" && url.pathname === "/health") {
      return sendJson(response, 200, {
        ok: true,
        botReady,
        groupConfigured: Boolean(state.groupOpenId),
        onebotConfigured: onebotGroupId > 0 && existsSync(onebotTokenPath),
        queuedNotifications: state.queue.length,
      });
    }

    if (request.method !== "POST" || url.pathname !== "/webhooks/github") {
      return sendJson(response, 404, { error: "not_found" });
    }

    const body = await readRequestBody(request);
    if (!verifyGitHubSignature(body, request.headers["x-hub-signature-256"])) {
      return sendJson(response, 401, { error: "invalid_signature" });
    }

    const eventName = request.headers["x-github-event"];
    if (eventName === "ping") {
      return sendJson(response, 200, { ok: true, event: "ping" });
    }
    if (eventName !== "push") {
      return sendJson(response, 202, { ok: true, ignored: "unsupported_event" });
    }

    const deliveryId = normalizedHeader(request.headers["x-github-delivery"]);
    if (!deliveryId) {
      return sendJson(response, 400, { error: "missing_delivery_id" });
    }
    if (isKnownDelivery(deliveryId)) {
      return sendJson(response, 202, { ok: true, duplicate: true });
    }

    const payload = JSON.parse(body.toString("utf8"));
    if (payload.repository?.full_name !== config.repository) {
      return sendJson(response, 403, { error: "unexpected_repository" });
    }
    if (typeof payload.ref !== "string" || !payload.ref.startsWith("refs/heads/")) {
      return sendJson(response, 202, { ok: true, ignored: "not_a_branch_push" });
    }

    const queuedItem = {
      deliveryId,
      message: formatPushMessage(payload),
      attempts: 0,
      createdAt: new Date().toISOString(),
    };
    state.queue.push(queuedItem);
    state.history.push({
      deliveryId,
      message: queuedItem.message,
      createdAt: queuedItem.createdAt,
    });
    state.history = state.history.slice(-historyLimit);
    saveState();
    scheduleQueue(0);
    return sendJson(response, 202, { ok: true, queued: true });
  } catch (error) {
    console.error(`Webhook request failed: ${redact(error?.message ?? String(error))}`);
    return sendJson(response, 400, { error: "invalid_request" });
  }
});

httpServer.listen(listenPort, listenHost, () => {
  console.log(`GitHub webhook listener ready on ${listenHost}:${listenPort}.`);
  scheduleQueue(0);
});

const abortController = new AbortController();
for (const signalName of ["SIGINT", "SIGTERM"]) {
  process.on(signalName, () => {
    if (queueTimer) {
      clearTimeout(queueTimer);
      queueTimer = null;
    }
    bot.stop();
    abortController.abort();
    httpServer.close();
  });
}

try {
  await bot.start(abortController.signal);
} catch (error) {
  console.error(`QQ bot stopped unexpectedly: ${redact(error?.message ?? String(error))}`);
  process.exitCode = 1;
}

function loadState() {
  try {
    if (!existsSync(statePath)) {
      return structuredClone(defaultState);
    }
    const loaded = JSON.parse(readFileSync(statePath, "utf8"));
    return {
      groupOpenId: typeof loaded.groupOpenId === "string" ? loaded.groupOpenId : null,
      recentDeliveries: Array.isArray(loaded.recentDeliveries) ? loaded.recentDeliveries : [],
      queue: Array.isArray(loaded.queue) ? loaded.queue : [],
      history: Array.isArray(loaded.history)
        ? loaded.history
        : (Array.isArray(loaded.queue) ? loaded.queue : []).map(({ deliveryId, message, createdAt }) => ({
            deliveryId,
            message,
            createdAt,
          })),
    };
  } catch (error) {
    console.error(`State file could not be read; starting clean: ${error.message}`);
    return structuredClone(defaultState);
  }
}

function saveState() {
  mkdirSync(dirname(statePath), { recursive: true });
  const temporaryPath = `${statePath}.tmp`;
  writeFileSync(temporaryPath, `${JSON.stringify(state, null, 2)}\n`, { mode: 0o600 });
  renameSync(temporaryPath, statePath);
}

function verifyGitHubSignature(body, signatureHeader) {
  const signature = normalizedHeader(signatureHeader);
  if (!signature.startsWith("sha256=")) {
    return false;
  }
  const expected = `sha256=${createHmac("sha256", config.githubWebhookSecret).update(body).digest("hex")}`;
  const actualBuffer = Buffer.from(signature, "utf8");
  const expectedBuffer = Buffer.from(expected, "utf8");
  return actualBuffer.length === expectedBuffer.length && timingSafeEqual(actualBuffer, expectedBuffer);
}

function normalizedHeader(value) {
  return Array.isArray(value) ? (value[0] ?? "") : (value ?? "");
}

function readRequestBody(request) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    let size = 0;
    request.on("data", (chunk) => {
      size += chunk.length;
      if (size > maxRequestBytes) {
        reject(new Error("request_too_large"));
        request.destroy();
        return;
      }
      chunks.push(chunk);
    });
    request.on("end", () => resolve(Buffer.concat(chunks)));
    request.on("error", reject);
  });
}

function sendJson(response, statusCode, body) {
  const encoded = Buffer.from(JSON.stringify(body));
  response.writeHead(statusCode, {
    "Content-Type": "application/json; charset=utf-8",
    "Content-Length": encoded.length,
    "Cache-Control": "no-store",
  });
  response.end(encoded);
}

function isKnownDelivery(deliveryId) {
  return state.recentDeliveries.includes(deliveryId)
    || state.queue.some((item) => item.deliveryId === deliveryId);
}

function scheduleQueue(delayMs) {
  if (queueTimer) {
    return;
  }
  queueTimer = setTimeout(() => {
    queueTimer = null;
    void processQueue();
  }, delayMs);
}

async function processQueue() {
  if (queueRunning || onebotGroupId <= 0 || !existsSync(onebotTokenPath) || state.queue.length === 0) {
    return;
  }

  queueRunning = true;
  try {
    while (state.queue.length > 0) {
      const item = state.queue[0];
      try {
        await sendOneBotText(item.message);
        state.queue.shift();
        state.recentDeliveries.push(item.deliveryId);
        state.recentDeliveries = state.recentDeliveries.slice(-recentDeliveryLimit);
        saveState();
        console.log(`GitHub delivery sent through NapCat: ${item.deliveryId}`);
        await sleep(3000);
      } catch (error) {
        item.attempts += 1;
        saveState();
        if (item.attempts === 3 && isRecoverableOneBotFailure(error)) {
          requestNapCatRecovery(item, error);
        }
        const delay = Math.min(300_000, 5000 * (2 ** Math.min(item.attempts - 1, 6)));
        console.error(`QQ notification attempt ${item.attempts} failed: ${redact(error?.message ?? String(error))}`);
        scheduleQueue(delay);
        break;
      }
    }
  } finally {
    queueRunning = false;
  }
}

async function sendOneBotText(message) {
  const token = readFileSync(onebotTokenPath, "utf8").trim();
  const response = await fetch(`${onebotBaseUrl}/send_group_msg`, {
    method: "POST",
    headers: {
      "Authorization": `Bearer ${token}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      group_id: onebotGroupId,
      message,
    }),
    signal: AbortSignal.timeout(15_000),
  });
  const result = await response.json();
  if (!response.ok || result.status !== "ok" || result.retcode !== 0) {
    const detail = String(result.wording || result.message || "").slice(0, 500);
    const error = new Error(
      `OneBot send failed: HTTP ${response.status}, retcode ${result.retcode ?? "unknown"}, detail ${detail}`,
    );
    error.onebotRetcode = result.retcode;
    error.onebotDetail = detail;
    throw error;
  }
}

function isRecoverableOneBotFailure(error) {
  const detail = String(error?.onebotDetail ?? error?.message ?? "");
  return error?.onebotRetcode === 200
    && /1006514|网络连接异常|EventChecker Failed|sendMsg.*Timeout/i.test(detail);
}

function requestNapCatRecovery(item, error) {
  try {
    mkdirSync(dirname(recoveryRequestPath), { recursive: true });
    const temporaryPath = `${recoveryRequestPath}.tmp`;
    writeFileSync(temporaryPath, `${JSON.stringify({
      requestedAt: new Date().toISOString(),
      deliveryId: item.deliveryId,
      attempts: item.attempts,
      reason: String(error?.onebotDetail ?? error?.message ?? "unknown").slice(0, 500),
    }, null, 2)}\n`, { mode: 0o600 });
    renameSync(temporaryPath, recoveryRequestPath);
    console.warn("NapCat recovery requested after repeated QQ transport failures.");
  } catch (requestError) {
    console.error(`Could not request NapCat recovery: ${redact(requestError?.message ?? String(requestError))}`);
  }
}

function formatTodaySummary() {
  const today = dateKey(new Date());
  const items = state.history.filter((item) => dateKey(new Date(item.createdAt)) === today);
  if (items.length === 0) {
    return "📋 2026Test 今日 Git 变动\n\n今天还没有收到任何分支推送。";
  }

  const lines = [
    "📋 2026Test 今日 Git 变动",
    "",
    `共收到 ${items.length} 次分支推送：`,
  ];
  for (const item of items) {
    const details = String(item.message)
      .split(/\r?\n/)
      .filter((line) => line && !/^https?:\/\//i.test(line));
    lines.push("", ...details.slice(1));
  }
  return truncateUtf8(lines.join("\n"), 2800);
}

function dateKey(date) {
  return new Intl.DateTimeFormat("en-CA", {
    timeZone: reportTimeZone,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(date);
}

function formatDateTime(value) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "未知";
  }
  return new Intl.DateTimeFormat("zh-CN", {
    timeZone: reportTimeZone,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false,
  }).format(date).replaceAll("/", "-");
}

function formatPushMessage(payload) {
  const branch = payload.ref.slice("refs/heads/".length);
  const actor = payload.sender?.login ?? payload.pusher?.name ?? "未知用户";
  const commits = Array.isArray(payload.commits) ? payload.commits : [];
  const action = payload.deleted
    ? "删除了分支"
    : payload.created
      ? "创建并推送了分支"
      : payload.forced
        ? "强制推送了分支"
        : "推送了分支";

  const lines = [
    "📦 2026Test Git 推送通知",
    "",
    `${actor} ${action}：${branch}`,
    `提交时间：${formatDateTime(payload.head_commit?.timestamp ?? new Date())}`,
    `提交数量：${commits.length}`,
  ];

  if (commits.length > 0) {
    lines.push("");
    for (const commit of commits.slice(-5)) {
      const subject = String(commit.message ?? "无提交说明").split(/\r?\n/, 1)[0];
      lines.push(`${String(commit.id ?? "").slice(0, 7)} ${subject}`);
    }
    if (commits.length > 5) {
      lines.push(`……另有 ${commits.length - 5} 个提交`);
    }
  }

  return truncateUtf8(lines.join("\n"), 2800);
}

function truncateUtf8(value, maxBytes) {
  const buffer = Buffer.from(value, "utf8");
  if (buffer.length <= maxBytes) {
    return value;
  }
  let end = maxBytes - Buffer.byteLength("\n……内容已截断", "utf8");
  while (end > 0 && (buffer[end] & 0b1100_0000) === 0b1000_0000) {
    end -= 1;
  }
  return `${buffer.subarray(0, end).toString("utf8")}\n……内容已截断`;
}

function redact(value) {
  let result = String(value);
  for (const secret of [config.appSecret, config.githubWebhookSecret]) {
    if (secret) {
      result = result.replaceAll(secret, "[REDACTED]");
    }
  }
  return result;
}

function sleep(milliseconds) {
  return new Promise((resolve) => setTimeout(resolve, milliseconds));
}
