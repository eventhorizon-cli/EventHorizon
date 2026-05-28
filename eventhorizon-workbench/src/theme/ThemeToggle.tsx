import { Monitor, Moon, Sun } from "lucide-react";
import { useWorkbenchStore } from "@/store/workbenchStore";
import { cn } from "@/utils/cn";
import type { ThemeMode } from "@/types";

const options: { value: ThemeMode; icon: typeof Sun; label: string }[] = [
  { value: "light", icon: Sun, label: "Light" },
  { value: "dark", icon: Moon, label: "Dark" },
  { value: "system", icon: Monitor, label: "System" },
];

export function ThemeToggle() {
  const themeMode = useWorkbenchStore((state) => state.themeMode);
  const setThemeMode = useWorkbenchStore((state) => state.setThemeMode);

  return (
    <div className="inline-flex rounded-full border border-border bg-card p-1">
      {options.map(({ value, icon: Icon, label }) => (
        <button
          key={value}
          type="button"
          onClick={() => setThemeMode(value)}
          className={cn(
            "inline-flex items-center gap-1 rounded-full px-3 py-1.5 text-xs transition-colors",
            themeMode === value ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:text-foreground",
          )}
          title={label}
        >
          <Icon className="h-3.5 w-3.5" />
          <span className="hidden sm:inline">{label}</span>
        </button>
      ))}
    </div>
  );
}

