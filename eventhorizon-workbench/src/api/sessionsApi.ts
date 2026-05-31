import { apiRequest } from "@/api/client";
import type { AgentSession, AgentSessionDetail, DirectoryListing, SessionModelSelection } from "@/types";

type SessionPayload = {
  id: string;
  title?: string;
  name?: string;
  status: AgentSession["status"];
  createdAt: string;
  updatedAt: string;
  providerName?: string;
  providerType?: string;
  model?: string;
  lastRunId?: string;
  summary?: string;
  changedFilesCount?: number;
  isTitleGenerated?: boolean;
  workspaceRoot?: string;
};

type SessionDetailPayload = SessionPayload & {
  messages?: AgentSessionDetail["messages"];
  sessionSkills?: AgentSessionDetail["sessionSkills"];
};

function mapSession(payload: SessionPayload): AgentSession {
  return {
    id: payload.id,
    title: payload.title ?? payload.name ?? "New conversation",
    status: payload.status,
    createdAt: payload.createdAt,
    updatedAt: payload.updatedAt,
    providerName: payload.providerName,
    providerType: payload.providerType,
    model: payload.model,
    lastRunId: payload.lastRunId,
    summary: payload.summary,
    changedFilesCount: payload.changedFilesCount,
    isTitleGenerated: payload.isTitleGenerated,
    workspaceRoot: payload.workspaceRoot,
  };
}

export async function getSessions(): Promise<AgentSession[]> {
  const payload = await apiRequest<SessionPayload[]>("/api/sessions");
  return payload.map(mapSession);
}

export async function getDirectories(path?: string): Promise<DirectoryListing> {
  const params = path ? new URLSearchParams({ path }) : undefined;
  const url = path ? `/api/sessions/directories?${params}` : "/api/sessions/directories";
  return apiRequest<DirectoryListing>(url);
}

export async function createSession(input: {
  initialMessage?: string;
  workspaceRoot?: string;
  providerName?: string;
  model?: string;
}): Promise<AgentSession> {
  return mapSession(
    await apiRequest<SessionPayload>("/api/sessions", {
      method: "POST",
      body: JSON.stringify(input),
    }),
  );
}

export async function getSession(sessionId: string): Promise<AgentSessionDetail> {
  const payload = await apiRequest<SessionDetailPayload>(`/api/sessions/${sessionId}`);
  return {
    ...mapSession(payload),
    messages: payload.messages ?? [],
    sessionSkills: payload.sessionSkills ?? { imported: [] },
  };
}

export async function updateSession(input: {
  sessionId: string;
  title?: string;
  providerName?: string | null;
  model?: string | null;
}): Promise<AgentSession> {
  return mapSession(
    await apiRequest<SessionPayload>(`/api/sessions/${input.sessionId}`, {
      method: "PATCH",
      body: JSON.stringify({
        title: input.title,
        providerName: input.providerName,
        model: input.model,
      }),
    }),
  );
}

export async function updateSessionModel(input: {
  sessionId: string;
  providerName?: string | null;
  modelId?: string | null;
}): Promise<SessionModelSelection> {
  return apiRequest<SessionModelSelection>(`/api/sessions/${input.sessionId}/model`, {
    method: "PUT",
    body: JSON.stringify({
      providerName: input.providerName,
      modelId: input.modelId,
    }),
  });
}

export async function deleteSession(sessionId: string): Promise<void> {
  await apiRequest(`/api/sessions/${sessionId}`, { method: "DELETE" });
}
