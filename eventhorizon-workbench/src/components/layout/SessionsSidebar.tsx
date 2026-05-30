import { formatDistanceToNow } from "date-fns";
import { PanelLeftClose, PanelLeftOpen } from "lucide-react";
import { cn } from "@/utils/cn";
import type { AgentSession } from "@/types";

type SessionsSidebarProps = {
  sessions: AgentSession[];
  currentSessionId?: string;
  leftPaneCollapsed: boolean;
  isCompactLayout: boolean;
  onToggleCollapsed: () => void;
  onNewChat: () => void;
  onOpenSession: (sessionId: string) => void;
};

export function SessionsSidebar({
  sessions,
  currentSessionId,
  leftPaneCollapsed,
  isCompactLayout,
  onToggleCollapsed,
  onNewChat,
  onOpenSession,
}: SessionsSidebarProps) {
  return (
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
              leftPaneCollapsed ? "h-10 w-10 text-lg" : "px-3 py-2 text-xs font-medium",
            )}
            title="New Chat"
          >
            {leftPaneCollapsed ? "+" : "New Chat"}
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

            return (
              <button
                key={session.id}
                type="button"
                title={session.title}
                onClick={() => onOpenSession(session.id)}
                className={cn(
                  "group w-full rounded-2xl border text-left transition-all",
                  active ? "border-primary bg-primary/10 shadow-sm" : "border-transparent hover:border-border hover:bg-muted/70",
                  leftPaneCollapsed ? "flex h-12 items-center justify-center px-0 py-0" : "px-3 py-3",
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
                ) : (
                  <>
                    <div className="truncate text-sm font-medium">{session.title}</div>
                    <div className="mt-1 truncate text-xs text-muted-foreground">
                      {formatDistanceToNow(new Date(session.updatedAt), { addSuffix: true })} · {session.status}
                    </div>
                  </>
                )}
              </button>
            );
          })}
        </div>
      </div>
    </aside>
  );
}
