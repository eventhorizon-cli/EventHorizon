import type { AgentEvent } from "@/types";

export function subscribeRunEvents(
  runId: string,
  handlers: {
    onEvent?: (event: AgentEvent) => void;
    onError?: (error: unknown) => void;
    onOpen?: () => void;
    onClose?: () => void;
  },
) {
  const source = new EventSource(`/api/runs/${runId}/events`);
  source.onopen = () => handlers.onOpen?.();
  source.onerror = (event) => {
    handlers.onError?.(event);
  };
  source.onmessage = (message) => {
    try {
      handlers.onEvent?.(JSON.parse(message.data) as AgentEvent);
    } catch (error) {
      handlers.onError?.(error);
    }
  };

  const forwardedTypes = [
    "runStarted",
    "runFinished",
    "runError",
    "runCancelled",
    "textMessageStart",
    "textMessageContent",
    "textMessageEnd",
    "toolCallStart",
    "toolCallResult",
    "toolCallEnd",
    "toolCallFailed",
    "plan.updated",
    "reasoning.summary.updated",
    "file.created",
    "file.modified",
    "file.deleted",
    "diff.generated",
    "command.started",
    "command.output",
    "command.completed",
    "test.started",
    "test.completed",
    "error",
  ] as const;

  for (const eventType of forwardedTypes) {
    source.addEventListener(eventType, (message) => {
      try {
        handlers.onEvent?.(JSON.parse((message as MessageEvent<string>).data) as AgentEvent);
      } catch (error) {
        handlers.onError?.(error);
      }
    });
  }

  return () => {
    source.close();
    handlers.onClose?.();
  };
}

