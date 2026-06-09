import { useState, useRef, useEffect } from "react";
import { formatDistanceToNow } from "date-fns";
import { ChevronDown, Edit3, Folder, MessageSquare, MoreHorizontal, PanelLeftClose, PanelLeftOpen, Plus, Settings2, Trash2 } from "lucide-react";
import { cn } from "@/utils/cn";
import { ConfirmDialog } from "@/components/dialogs/ConfirmDialog";
import type { AgentSession, AgentWorkspace } from "@/types";

type SessionsSidebarProps = {
  workspaces: AgentWorkspace[];
  sessions: AgentSession[];
  currentSessionId?: string;
  hasConfiguredProviders: boolean;
  leftPaneCollapsed: boolean;
  isCompactLayout: boolean;
  onToggleCollapsed: () => void;
  onNewChat: () => void;
  onCreateWorkspaceSession: (workspaceId: string) => void;
  onOpenSession: (sessionId: string) => void;
  onDeleteSession: (sessionId: string) => void;
  onDeleteWorkspace: (workspaceId: string) => void;
  onRenameSession: (sessionId: string, newTitle: string) => void;
};

export function SessionsSidebar({
  workspaces,
  sessions,
  currentSessionId,
  hasConfiguredProviders,
  leftPaneCollapsed,
  isCompactLayout,
  onToggleCollapsed,
  onNewChat,
  onCreateWorkspaceSession,
  onOpenSession,
  onDeleteSession,
  onDeleteWorkspace,
  onRenameSession,
}: SessionsSidebarProps) {
  const [openMenuId, setOpenMenuId] = useState<string | null>(null);
  const [renameSessionId, setRenameSessionId] = useState<string | null>(null);
  const [renameInputValue, setRenameInputValue] = useState("");
  const [deleteConfirm, setDeleteConfirm] = useState<{ sessionId: string; sessionTitle: string } | null>(null);
  const [deleteWorkspaceConfirm, setDeleteWorkspaceConfirm] = useState<{ workspaceId: string; workspaceName: string; sessionCount: number } | null>(null);
  const [expandedWorkspaceIds, setExpandedWorkspaceIds] = useState<string[]>([]);
  const menuRef = useRef<HTMLDivElement>(null);
  const workspaceGroups = groupSessionsByWorkspace(workspaces, sessions);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setOpenMenuId(null);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const handleDeleteSession = (sessionId: string, sessionTitle: string) => {
    setDeleteConfirm({ sessionId, sessionTitle });
    setOpenMenuId(null);
  };

  const handleDeleteConfirm = () => {
    if (deleteConfirm) {
      onDeleteSession(deleteConfirm.sessionId);
    }
    setDeleteConfirm(null);
  };

  const handleDeleteWorkspace = (workspaceId: string, workspaceName: string, sessionCount: number) => {
    setDeleteWorkspaceConfirm({ workspaceId, workspaceName, sessionCount });
  };

  const handleDeleteWorkspaceConfirm = () => {
    if (deleteWorkspaceConfirm) {
      onDeleteWorkspace(deleteWorkspaceConfirm.workspaceId);
    }
    setDeleteWorkspaceConfirm(null);
  };

  const handleStartRename = (sessionId: string, title: string) => {
    setRenameSessionId(sessionId);
    setRenameInputValue(title);
    setOpenMenuId(null);
  };

  const handleRenameSubmit = (sessionId: string) => {
    const trimmed = renameInputValue.trim();
    if (trimmed) {
      onRenameSession(sessionId, trimmed);
    }
    setRenameSessionId(null);
    setRenameInputValue("");
  };

  const handleRenameCancel = () => {
    setRenameSessionId(null);
    setRenameInputValue("");
  };

  const toggleWorkspace = (workspaceId: string) => {
    setExpandedWorkspaceIds((previous) => previous.includes(workspaceId)
      ? previous.filter((id) => id !== workspaceId)
      : [...previous, workspaceId]);
  };

  return (
    <>
      <aside
        className={cn(
          "hidden min-h-0 shrink-0 flex-col overflow-hidden rounded-3xl border border-border/70 bg-card/95 shadow-sm transition-[width] duration-300 ease-out md:flex",
          leftPaneCollapsed ? "w-[72px]" : "w-[280px]",
        )}
      >
      <div
        className={cn(
          "flex shrink-0 border-b border-border/70 p-3",
          leftPaneCollapsed ? "flex-col items-center gap-2" : "items-center justify-between gap-3",
        )}
      >
        {!leftPaneCollapsed ? (
          <div className="min-w-0">
            <div className="text-sm font-semibold">Sessions</div>
            <div className="text-xs text-muted-foreground">{isCompactLayout ? "Auto compact" : "History"}</div>
          </div>
        ) : null}

        <div className={cn("flex shrink-0 gap-2", leftPaneCollapsed ? "flex-col items-center" : "items-center")}>
          <button
            type="button"
            onClick={onToggleCollapsed}
            className={cn(
              "inline-flex items-center justify-center rounded-2xl bg-background/80 text-muted-foreground shadow-sm ring-1 ring-border/60 transition hover:bg-muted hover:text-foreground",
              leftPaneCollapsed ? "h-10 w-10" : "h-9 w-9",
            )}
            title={leftPaneCollapsed ? "Expand sessions" : "Collapse sessions"}
          >
            {leftPaneCollapsed ? <PanelLeftOpen className="h-4 w-4" /> : <PanelLeftClose className="h-4 w-4" />}
          </button>

          <button
            type="button"
            onClick={onNewChat}
            className={cn(
              "inline-flex items-center justify-center rounded-2xl bg-primary text-primary-foreground shadow-sm transition hover:opacity-90",
              leftPaneCollapsed ? "h-10 w-10" : "px-3 py-2 text-xs font-medium",
            )}
            title={hasConfiguredProviders ? "New Workspace" : "Configure Provider"}
          >
            {leftPaneCollapsed ? (hasConfiguredProviders ? <Plus className="h-4 w-4" /> : <Settings2 className="h-4 w-4" />) : hasConfiguredProviders ? "New Workspace" : "Setup Provider"}
          </button>
        </div>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto p-2">
        {workspaces.length === 0 ? (
          <div
            className={cn(
              "rounded-2xl border border-dashed border-border p-4 text-sm text-muted-foreground",
              leftPaneCollapsed && "p-2 text-center text-xs",
            )}
          >
            {leftPaneCollapsed ? (hasConfiguredProviders ? "Empty" : "Setup") : hasConfiguredProviders ? "No workspaces yet." : "Configure a provider to create your first workspace."}
          </div>
        ) : null}

        <div className="space-y-2">
          {workspaceGroups.map((workspace) => {
            const workspaceActive = workspace.sessions.some((session) => currentSessionId === session.id);
            const workspaceCollapsed = !expandedWorkspaceIds.includes(workspace.id);
            const workspaceInitial = workspace.name.trim()?.[0]?.toUpperCase() || "W";

            return (
              <div key={workspace.id} className={cn("rounded-2xl", !leftPaneCollapsed && "border border-border/60 bg-background/35 p-1.5")}>
                <div
                  className={cn(
                    "group flex w-full items-center rounded-xl transition-all",
                    workspaceActive ? "bg-primary/10 text-foreground" : "text-muted-foreground hover:bg-muted/70 hover:text-foreground",
                    leftPaneCollapsed ? "h-12 justify-center" : "pr-1",
                  )}
                >
                  <button
                    type="button"
                    title={workspace.root ?? workspace.name}
                    onClick={() => {
                      if (!leftPaneCollapsed) {
                        toggleWorkspace(workspace.id);
                        return;
                      }

                      if (workspace.sessions[0]) {
                        onOpenSession(workspace.sessions[0].id);
                      } else {
                        onCreateWorkspaceSession(workspace.id);
                      }
                    }}
                    className={cn(
                      "flex min-w-0 flex-1 items-center gap-2 rounded-xl text-left",
                      leftPaneCollapsed ? "h-12 justify-center" : "px-2.5 py-2",
                    )}
                  >
                    {leftPaneCollapsed ? (
                      <div
                        className={cn(
                          "flex h-8 w-8 items-center justify-center rounded-xl text-xs font-semibold",
                          workspaceActive ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground group-hover:text-foreground",
                        )}
                      >
                        {workspaceInitial}
                      </div>
                    ) : (
                      <>
                        <ChevronDown className={cn("h-4 w-4 shrink-0 transition-transform", workspaceCollapsed && "-rotate-90")} />
                        <Folder className={cn("h-4 w-4 shrink-0", workspaceActive ? "text-primary" : "text-muted-foreground")} />
                        <div className="min-w-0 flex-1">
                          <div className="truncate text-sm font-semibold">{workspace.name}</div>
                          <div className="mt-0.5 truncate text-[11px] text-muted-foreground">
                            {workspace.sessions.length} {workspace.sessions.length === 1 ? "session" : "sessions"}
                          </div>
                        </div>
                      </>
                    )}
                  </button>

                  {!leftPaneCollapsed ? (
                    <div className="flex shrink-0 items-center gap-1 opacity-0 transition group-hover:opacity-100">
                      <button
                        type="button"
                        title="New session"
                        onClick={(event) => {
                          event.stopPropagation();
                          onCreateWorkspaceSession(workspace.id);
                        }}
                        className="rounded-lg p-1.5 text-muted-foreground transition hover:bg-primary/10 hover:text-primary"
                      >
                        <Plus className="h-3.5 w-3.5" />
                      </button>
                      <button
                        type="button"
                        title="Delete workspace"
                        onClick={(event) => {
                          event.stopPropagation();
                          handleDeleteWorkspace(workspace.id, workspace.name, workspace.sessions.length);
                        }}
                        className="rounded-lg p-1.5 text-muted-foreground transition hover:bg-red-500/10 hover:text-red-600 dark:hover:text-red-400"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    </div>
                  ) : null}
                </div>

                {!leftPaneCollapsed && !workspaceCollapsed ? (
                  <div className="mt-1 space-y-1 pl-5">
                    {workspace.sessions.length === 0 ? (
                      <button
                        type="button"
                        onClick={() => onCreateWorkspaceSession(workspace.id)}
                        className="flex w-full items-center gap-2 rounded-xl border border-dashed border-border px-2.5 py-2 text-left text-xs text-muted-foreground transition hover:border-primary/50 hover:bg-primary/5 hover:text-primary"
                      >
                        <Plus className="h-3.5 w-3.5" />
                        Create first session
                      </button>
                    ) : null}
                    {workspace.sessions.map((session) => {
                      const active = currentSessionId === session.id;
                      const isMenuOpen = openMenuId === session.id;
                      const isRenaming = renameSessionId === session.id;

                      return (
                        <div key={session.id} className="relative">
                          {isMenuOpen && (
                            <div
                              ref={menuRef}
                              className="absolute right-0 top-full z-50 mt-1 w-40 rounded-xl border border-border bg-background py-1 shadow-lg"
                            >
                              <button
                                type="button"
                                onClick={() => handleStartRename(session.id, session.title)}
                                className="flex w-full items-center gap-2 px-3 py-2 text-sm text-muted-foreground transition hover:bg-muted hover:text-foreground"
                              >
                                <Edit3 className="h-4 w-4" />
                                Rename
                              </button>
                              <button
                                type="button"
                                onClick={() => handleDeleteSession(session.id, session.title)}
                                className="flex w-full items-center gap-2 px-3 py-2 text-sm text-red-600 transition hover:bg-red-500/10 dark:text-red-400"
                              >
                                <Trash2 className="h-4 w-4" />
                                Delete
                              </button>
                            </div>
                          )}

                          <button
                            type="button"
                            title={session.title}
                            onClick={() => onOpenSession(session.id)}
                            className={cn(
                              "group flex w-full items-center justify-between gap-2 rounded-xl border text-left transition-all",
                              active ? "border-primary bg-primary/10 shadow-sm" : "border-transparent hover:border-border hover:bg-muted/70",
                              "px-2.5 py-2.5",
                            )}
                          >
                            {isRenaming ? (
                              <input
                                type="text"
                                value={renameInputValue}
                                onChange={(e) => setRenameInputValue(e.target.value)}
                                onKeyDown={(e) => {
                                  if (e.key === "Enter") {
                                    handleRenameSubmit(session.id);
                                  } else if (e.key === "Escape") {
                                    handleRenameCancel();
                                  }
                                }}
                                className="flex-1 rounded-lg border border-primary bg-background px-2 py-1 text-sm outline-none focus:ring-2 focus:ring-primary"
                                autoFocus
                              />
                            ) : (
                              <div className="flex min-w-0 flex-1 items-center gap-2">
                                <MessageSquare className={cn("h-3.5 w-3.5 shrink-0", active ? "text-primary" : "text-muted-foreground")} />
                                <div className="min-w-0 flex-1">
                                  <div className="truncate text-sm font-medium">{session.title}</div>
                                  <div className="mt-1 truncate text-xs text-muted-foreground">
                                    {formatDistanceToNow(new Date(session.updatedAt), { addSuffix: true })} · {session.status}
                                  </div>
                                </div>
                              </div>
                            )}

                            {isRenaming ? (
                              <div className="flex gap-1">
                                <button
                                  type="button"
                                  onClick={() => handleRenameSubmit(session.id)}
                                  className="rounded-lg px-2 py-1 text-xs font-medium text-primary transition hover:bg-primary/10"
                                >
                                  Save
                                </button>
                                <button
                                  type="button"
                                  onClick={handleRenameCancel}
                                  className="rounded-lg px-2 py-1 text-xs text-muted-foreground transition hover:bg-muted"
                                >
                                  Cancel
                                </button>
                              </div>
                            ) : (
                              <button
                                type="button"
                                onClick={(e) => {
                                  e.stopPropagation();
                                  setOpenMenuId(isMenuOpen ? null : session.id);
                                }}
                                className="shrink-0 rounded-lg p-1.5 text-muted-foreground opacity-0 transition group-hover:opacity-100 hover:bg-muted hover:text-foreground"
                              >
                                <MoreHorizontal className="h-4 w-4" />
                              </button>
                            )}
                          </button>
                        </div>
                      );
                    })}
                  </div>
                ) : null}
              </div>
            );
          })}
        </div>
      </div>
    </aside>

    <ConfirmDialog
      open={!!deleteConfirm}
      title="Delete Conversation"
      message={deleteConfirm ? `Are you sure you want to delete the conversation "${deleteConfirm.sessionTitle}"? This action cannot be undone.` : ""}
      confirmLabel="Delete"
      cancelLabel="Cancel"
      confirmVariant="danger"
      onCancel={() => setDeleteConfirm(null)}
      onConfirm={handleDeleteConfirm}
    />
    <ConfirmDialog
      open={!!deleteWorkspaceConfirm}
      title="Delete Workspace"
      message={deleteWorkspaceConfirm ? `Are you sure you want to delete workspace "${deleteWorkspaceConfirm.workspaceName}" and its ${deleteWorkspaceConfirm.sessionCount} ${deleteWorkspaceConfirm.sessionCount === 1 ? "session" : "sessions"}? This action cannot be undone.` : ""}
      confirmLabel="Delete Workspace"
      cancelLabel="Cancel"
      confirmVariant="danger"
      onCancel={() => setDeleteWorkspaceConfirm(null)}
      onConfirm={handleDeleteWorkspaceConfirm}
    />
    </>
  );
}

type WorkspaceSessionGroup = {
  id: string;
  name: string;
  root?: string;
  updatedAt: string;
  sessions: AgentSession[];
};

function groupSessionsByWorkspace(workspaces: AgentWorkspace[], sessions: AgentSession[]): WorkspaceSessionGroup[] {
  const groups = new Map<string, WorkspaceSessionGroup>();

  for (const workspace of workspaces) {
    groups.set(workspace.id, {
      id: workspace.id,
      name: workspace.name,
      root: workspace.workspaceRoot,
      updatedAt: workspace.updatedAt,
      sessions: [],
    });
  }

  for (const session of sessions) {
    if (!session.workspaceId) {
      continue;
    }

    const workspaceId = session.workspaceId;
    const workspaceName = session.workspaceName ?? deriveWorkspaceName(session.workspaceRoot) ?? "Workspace";
    const existing = groups.get(workspaceId);

    if (existing) {
      existing.sessions.push(session);
      if (new Date(session.updatedAt) > new Date(existing.updatedAt)) {
        existing.updatedAt = session.updatedAt;
      }
      continue;
    }

    groups.set(workspaceId, {
      id: workspaceId,
      name: workspaceName,
      root: session.workspaceRoot,
      updatedAt: session.updatedAt,
      sessions: [session],
    });
  }

  return Array.from(groups.values()).sort((left, right) => new Date(right.updatedAt).getTime() - new Date(left.updatedAt).getTime());
}

function deriveWorkspaceName(workspaceRoot?: string) {
  if (!workspaceRoot) {
    return undefined;
  }

  return workspaceRoot.split(/[\\/]/).filter(Boolean).at(-1);
}
