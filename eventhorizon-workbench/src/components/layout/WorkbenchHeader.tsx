import { Settings2 } from "lucide-react";
import { ThemeToggle } from "@/theme/ThemeToggle";

type WorkbenchHeaderProps = {
  onOpenSettings: () => void;
};

export function WorkbenchHeader({ onOpenSettings }: WorkbenchHeaderProps) {
  return (
    <header className="shrink-0 px-1 pb-3 pt-1">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="min-w-0">
          <div className="truncate text-lg font-semibold">Event Horizon Workbench</div>
        </div>

        <div className="flex items-center gap-3">
          <ThemeToggle />
          <button
            type="button"
            onClick={onOpenSettings}
            className="inline-flex items-center gap-1 rounded-full border border-border bg-card px-3 py-1.5 text-xs text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
            title="Global settings"
          >
            <Settings2 className="h-3.5 w-3.5" />
            <span>Settings</span>
          </button>
        </div>
      </div>
    </header>
  );
}
