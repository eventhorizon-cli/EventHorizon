import { apiRequest } from "@/api/client";
import type { AgentRun } from "@/types";

export async function createRun(input: {
  sessionId?: string;
  task: string;
  workingDirectory?: string;
  options?: Record<string, unknown>;
}): Promise<AgentRun> {
  return apiRequest<AgentRun>("/api/runs", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export async function getRun(runId: string): Promise<AgentRun> {
  return apiRequest<AgentRun>(`/api/runs/${runId}`);
}

export async function cancelRun(runId: string): Promise<void> {
  await apiRequest(`/api/runs/${runId}/cancel`, {
    method: "POST",
  });
}

