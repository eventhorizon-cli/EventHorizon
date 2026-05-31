import { apiRequest } from "@/api/client";
import type { AgentRun } from "@/types";

export async function createRun(input: {
  sessionId: string;
  task: string;
  workingDirectory?: string;
  options?: Record<string, unknown>;
}): Promise<AgentRun> {
  return apiRequest<AgentRun>(`/api/sessions/${encodeURIComponent(input.sessionId)}/runs`, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export async function getRun(sessionId: string, runId: string): Promise<AgentRun> {
  return apiRequest<AgentRun>(`/api/sessions/${encodeURIComponent(sessionId)}/runs/${encodeURIComponent(runId)}`);
}

export async function cancelRun(sessionId: string, runId: string): Promise<void> {
  await apiRequest(`/api/sessions/${encodeURIComponent(sessionId)}/runs/${encodeURIComponent(runId)}/cancel`, {
    method: "POST",
  });
}
