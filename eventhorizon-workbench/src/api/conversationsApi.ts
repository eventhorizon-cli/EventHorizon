import { apiRequest } from "@/api/client";
import type { AgentSession, AgentSessionDetail, ConversationModelSelection, DirectoryItem, DirectoryListing } from "@/types";

type ConversationPayload = {
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

type ConversationDetailPayload = ConversationPayload & {
  messages?: AgentSessionDetail["messages"];
  sessionSkills?: AgentSessionDetail["sessionSkills"];
};

function mapConversation(payload: ConversationPayload): AgentSession {
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

export async function getConversations(): Promise<AgentSession[]> {
  const payload = await apiRequest<ConversationPayload[]>("/api/conversations");
  return payload.map(mapConversation);
}

export async function getDirectories(path?: string): Promise<DirectoryListing> {
  const params = path ? new URLSearchParams({ path }) : undefined;
  const url = path ? `/api/conversations/directories?${params}` : "/api/conversations/directories";
  return apiRequest<DirectoryListing>(url);
}

export async function createConversation(input: {
  initialMessage?: string;
  workspaceRoot?: string;
  providerName?: string;
  model?: string;
}): Promise<AgentSession> {
  return mapConversation(
    await apiRequest<ConversationPayload>("/api/conversations", {
      method: "POST",
      body: JSON.stringify(input),
    }),
  );
}

export async function getConversation(conversationId: string): Promise<AgentSessionDetail> {
  const payload = await apiRequest<ConversationDetailPayload>(`/api/conversations/${conversationId}`);
  return {
    ...mapConversation(payload),
    messages: payload.messages ?? [],
    sessionSkills: payload.sessionSkills ?? { imported: [] },
  };
}

export async function updateConversation(input: {
  conversationId: string;
  title?: string;
  providerName?: string | null;
  model?: string | null;
}): Promise<AgentSession> {
  return mapConversation(
    await apiRequest<ConversationPayload>(`/api/conversations/${input.conversationId}`, {
      method: "PATCH",
      body: JSON.stringify({
        title: input.title,
        providerName: input.providerName,
        model: input.model,
      }),
    }),
  );
}

export async function updateConversationModel(input: {
  conversationId: string;
  providerName?: string | null;
  modelId?: string | null;
}): Promise<ConversationModelSelection> {
  return apiRequest<ConversationModelSelection>(`/api/conversations/${input.conversationId}/model`, {
    method: "PUT",
    body: JSON.stringify({
      providerName: input.providerName,
      modelId: input.modelId,
    }),
  });
}

export async function deleteConversation(conversationId: string): Promise<void> {
  await apiRequest(`/api/conversations/${conversationId}`, { method: "DELETE" });
}
