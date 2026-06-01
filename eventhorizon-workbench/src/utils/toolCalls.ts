import type { AgentEvent, LogItem, ToolCallDescriptor } from "@/types";

const toolCallEventTypes = new Set([
  "toolCallStart",
  "toolCallArgs",
  "toolCallResult",
  "toolCallEnd",
  "toolCallFailed",
]);

export type ToolCallTimelineItem = {
  id: string;
  name: string;
  arguments?: string;
  status: "running" | "completed" | "failed";
  result?: string;
  error?: string;
  startedAt: string;
  updatedAt: string;
};

const toolCallStatusIconMap: Record<ToolCallTimelineItem["status"], string> = {
  running: "○",
  completed: "✅",
  failed: "❌",
};

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function stringifyCompact(value: unknown) {
  try {
    return JSON.stringify(value);
  } catch {
    return String(value);
  }
}

function stringifyValue(value: unknown) {
  if (typeof value === "string") {
    return value;
  }

  if (value === null || value === undefined) {
    return undefined;
  }

  try {
    return JSON.stringify(value, null, 2);
  } catch {
    return String(value);
  }
}

function trimToUndefined(value: string | undefined) {
  const normalized = value?.trim();
  return normalized ? normalized : undefined;
}

function formatArgumentValue(value: unknown): string {
  if (typeof value === "string") {
    return JSON.stringify(value);
  }

  if (typeof value === "number" || typeof value === "boolean" || value === null) {
    return String(value);
  }

  if (Array.isArray(value) || isRecord(value)) {
    return stringifyCompact(value);
  }

  return JSON.stringify(String(value));
}

function formatToolArguments(argumentsText?: string) {
  const normalized = trimToUndefined(argumentsText);
  if (!normalized) {
    return "";
  }

  try {
    const parsed = JSON.parse(normalized) as unknown;

    if (isRecord(parsed)) {
      return Object.entries(parsed)
        .map(([key, value]) => `${key}=${formatArgumentValue(value)}`)
        .join(", ");
    }

    if (Array.isArray(parsed)) {
      return parsed.map((value, index) => `arg${index + 1}=${formatArgumentValue(value)}`).join(", ");
    }

    return `value=${formatArgumentValue(parsed)}`;
  } catch {
    return normalized;
  }
}

function readDescriptor(event: AgentEvent): Partial<ToolCallDescriptor> | undefined {
  return isRecord(event.toolCall) ? event.toolCall as Partial<ToolCallDescriptor> : undefined;
}

function mergeText(current: string | undefined, next: string | undefined) {
  const normalizedNext = trimToUndefined(next);
  if (!normalizedNext) {
    return current;
  }

  if (!current) {
    return normalizedNext;
  }

  if (normalizedNext === current || normalizedNext.startsWith(current) || normalizedNext.length >= current.length) {
    return normalizedNext;
  }

  return current;
}

export function isToolCallEvent(event: AgentEvent) {
  return toolCallEventTypes.has(event.type);
}

export function getToolCallName(event: AgentEvent) {
  return trimToUndefined(event.toolCallName) ?? trimToUndefined(readDescriptor(event)?.name) ?? "tool";
}

export function getToolCallArguments(event: AgentEvent) {
  const descriptor = readDescriptor(event);
  return trimToUndefined(
    event.type === "toolCallArgs"
      ? stringifyValue(event.text) ?? stringifyValue(descriptor?.arguments)
      : stringifyValue(descriptor?.arguments) ?? stringifyValue(event.text),
  );
}

export function getToolCallResult(event: AgentEvent) {
  const descriptor = readDescriptor(event);
  return trimToUndefined(stringifyValue(event.result) ?? stringifyValue(descriptor?.result));
}

export function getToolCallStatus(event: AgentEvent): ToolCallTimelineItem["status"] {
  const descriptorStatus = trimToUndefined(readDescriptor(event)?.status);

  if (event.type === "toolCallFailed" || descriptorStatus === "failed") {
    return "failed";
  }

  if (event.type === "toolCallResult" || event.type === "toolCallEnd" || descriptorStatus === "completed") {
    return "completed";
  }

  return "running";
}

export function getToolCallStatusIcon(status: ToolCallTimelineItem["status"]) {
  return toolCallStatusIconMap[status];
}

export function formatToolCallSignature(name: string, argumentsText?: string) {
  const normalizedName = trimToUndefined(name) ?? "tool";
  const formattedArguments = formatToolArguments(argumentsText);
  return `${normalizedName}(${formattedArguments})`;
}

export function buildToolCallTimeline(logs: LogItem[], runId?: string): ToolCallTimelineItem[] {
  if (!runId) {
    return [];
  }

  const timeline = new Map<string, ToolCallTimelineItem>();

  for (const log of logs) {
    const { event } = log;
    if (event.runId !== runId || !isToolCallEvent(event)) {
      continue;
    }

    const descriptor = readDescriptor(event);
    const id = trimToUndefined(event.toolCallId) ?? trimToUndefined(descriptor?.id) ?? log.id;
    const current = timeline.get(id);
    const entry: ToolCallTimelineItem = current ?? {
      id,
      name: getToolCallName(event),
      status: getToolCallStatus(event),
      startedAt: log.timestamp,
      updatedAt: log.timestamp,
    };

    entry.name = getToolCallName(event);
    entry.arguments = mergeText(entry.arguments, getToolCallArguments(event));
    entry.result = mergeText(entry.result, getToolCallResult(event));
    entry.error = mergeText(entry.error, trimToUndefined(event.error));
    entry.status = getToolCallStatus(event);
    entry.updatedAt = log.timestamp;

    timeline.set(id, entry);
  }

  return [...timeline.values()].sort((left, right) => left.startedAt.localeCompare(right.startedAt));
}
