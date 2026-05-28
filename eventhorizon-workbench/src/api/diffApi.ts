import { apiRequest } from "@/api/client";
import type { FileChange, FileDiff } from "@/types";

export async function getChanges(runId: string): Promise<FileChange[]> {
  return apiRequest<FileChange[]>(`/api/runs/${runId}/changes`);
}

export async function getFileDiff(runId: string, path: string): Promise<FileDiff> {
  return apiRequest<FileDiff>(`/api/runs/${runId}/diff?path=${encodeURIComponent(path)}`);
}

