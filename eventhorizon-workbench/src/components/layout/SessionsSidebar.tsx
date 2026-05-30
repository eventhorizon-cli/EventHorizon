import { useState, useRef, useEffect } from "react";
import { formatDistanceToNow } from "date-fns";
import { PanelLeftClose, PanelLeftOpen, Plus, MoreHorizontal, Trash2, Edit3 } from "lucide-react";
import { cn } from "@/utils/cn";
import { ConfirmDialog } from "@/components/dialogs/ConfirmDialog";
import type { AgentSession } from "@/types";

type SessionsSidebarProps = {
  sessions: AgentSession[];
  currentSessionId?: string;
  leftPaneCollapsed: boolean;
  isCompactLayout: boolean;
  onToggleCollapsed: () => void;
  onNewChat: () => void;
  onOpenSession: (sessionId: string) => void;
  onDeleteSession: (sessionId: string) => void;
  onRenameSession: (sessionId: string, newTitle: string) => void;
};

export function SessionsSidebar({
  sessions,
  currentSessionId,
  leftPaneCollapsed,
  isCompactLayout,
  onToggleCollapsed,
  onNewChat,
  onOpenSession,
  onDeleteSession,
  onRenameSession,
}: SessionsSidebarProps) {
  const [openMenuId, setOpenMenuId] = useState<string | null>(null);
  const [renameSessionId, setRenameSessionId] = useState<string | null>(null);
  const [renameInputValue, setRenameInputValue] = useState("");
  const [deleteConfirm, setDeleteConfirm] = useState<{ sessionId: string; sessionTitle: string } | null>(null);
  const menuRef = useRef<HTMLDivElement>(null);

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
            title="New Chat"
          >
            {leftPaneCollapsed ? <Plus className="h-4 w-4" /> : "New Session"}
          </button>
        </div>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto p-2">
        {sessions.length === 0 ? (
          <div
            className={cn(
              "rounded-2xl border border-dashed border-border p-4 text-sm text-muted-foreground",
              leftPaneCollapsed && "p-2 text-center text-xs",
            )}
          >
            {leftPaneCollapsed ? "Empty" : "No sessions yet."}
          </div>
        ) : null}

        <div className="space-y-1.5">
          {sessions.map((session) => {
            const active = currentSessionId === session.id;
            const initial = session.title?.trim()?.[0]?.toUpperCase() || "S";
            const isMenuOpen = openMenuId === session.id;
            const isRenaming = renameSessionId === session.id;

            return (
              <div key={session.id} className="relative">
                {isMenuOpen && (
                  <div
                    ref={menuRef}
                    className="absolute right-0 top-full z-50 mt-1 w-40 rounded-xl border border-border bg-background shadow-lg py-1"
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
                    "group flex w-full items-center justify-between gap-2 rounded-2xl border text-left transition-all",
                    active ? "border-primary bg-primary/10 shadow-sm" : "border-transparent hover:border-border hover:bg-muted/70",
                    leftPaneCollapsed ? "h-12 px-0 py-0 justify-center" : "px-3 py-3",
                  )}
                >
                  {leftPaneCollapsed ? (
                    <div
                      className={cn(
                        "flex h-8 w-8 items-center justify-center rounded-xl text-xs font-semibold",
                        active ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground group-hover:text-foreground",
                      )}
                    >
                      {initial}
                    </div>
                  ) : isRenaming ? (
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
                    <div className="min-w-0 flex-1">
                      <div className="truncate text-sm font-medium">{session.title}</div>
                      <div className="mt-1 truncate text-xs text-muted-foreground">
                        {formatDistanceToNow(new Date(session.updatedAt), { addSuffix: true })} · {session.status}
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
                  ) : !leftPaneCollapsed ? (
                    <button
                      type="button"
                      onClick={(e) => {
                        e.stopPropagation();
                        setOpenMenuId(isMenuOpen ? null : session.id);
                      }}
                      className="shrink-0 rounded-lg p-1.5 text-muted-foreground opacity-0 transition opacity group-hover:opacity-100 hover:bg-muted hover:text-foreground"
                    >
                      <MoreHorizontal className="h-4 w-4" />
                    </button>
                  ) : null}
                </button>
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
    </>
  );
}
