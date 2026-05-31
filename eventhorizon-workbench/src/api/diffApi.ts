import { apiRequest } from "@/api/client";
import type { FileChange, FileDiff } from "@/types";

export async function getChanges(sessionId: string, runId: string): Promise<FileChange[]> {
  return apiRequest<FileChange[]>(`/api/sessions/${encodeURIComponent(sessionId)}/runs/${encodeURIComponent(runId)}/changes`);
}

export async function getFileDiff(sessionId: string, runId: string, path: string): Promise<FileDiff> {
  return apiRequest<FileDiff>(`/api/sessions/${encodeURIComponent(sessionId)}/runs/${encodeURIComponent(runId)}/diff?path=${encodeURIComponent(path)}`);
}
