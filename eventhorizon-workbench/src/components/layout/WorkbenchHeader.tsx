import { formatDistanceToNow } from "date-fns";
import { Settings2 } from "lucide-react";
import { ThemeToggle } from "@/theme/ThemeToggle";
import type { AgentPhase, AgentRun } from "@/types";

type WorkbenchHeaderProps = {
  currentRun?: AgentRun;
  phase: AgentPhase;
  onOpenSettings: () => void;
};

export function WorkbenchHeader({ currentRun, phase, onOpenSettings }: WorkbenchHeaderProps) {
  const statusLabel = currentRun?.status ?? "idle";

  return (
    <header className="shrink-0 px-1 pb-3 pt-1">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="min-w-0">
          <div className="truncate text-lg font-semibold">Event Horizon Workbench</div>
          <div className="truncate text-xs text-muted-foreground">
            {statusLabel} · {phase} ·{" "}
            {currentRun?.createdAt
              ? formatDistanceToNow(new Date(currentRun.createdAt), { addSuffix: true })
              : "Idle"}
          </div>
        </div>

        <div className="flex items-center gap-3">
          <ThemeToggle />
          <button
            type="button"
            onClick={onOpenSettings}
            className="inline-flex h-10 w-10 items-center justify-center rounded-2xl border border-border bg-card text-muted-foreground shadow-sm transition hover:bg-muted hover:text-foreground"
            title="Global settings"
          >
            <Settings2 className="h-4 w-4" />
          </button>
        </div>
      </div>
    </header>
  );
}
