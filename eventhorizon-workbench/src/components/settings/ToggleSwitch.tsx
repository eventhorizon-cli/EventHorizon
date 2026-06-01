import { cn } from "@/utils/cn";

type ToggleSwitchProps = {
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
  disabled?: boolean;
  className?: string;
  onLabel?: string;
  offLabel?: string;
};

export function ToggleSwitch({
  checked,
  onCheckedChange,
  disabled = false,
  className,
  onLabel = "On",
  offLabel = "Off",
}: ToggleSwitchProps) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      disabled={disabled}
      onClick={() => onCheckedChange(!checked)}
      className={cn(
        "inline-flex items-center gap-2 rounded-full border border-border bg-background px-2 py-1 text-xs font-medium text-foreground transition",
        disabled ? "cursor-not-allowed opacity-50" : "hover:bg-muted",
        className,
      )}
    >
      <span
        className={cn(
          "relative inline-flex h-5 w-9 shrink-0 items-center rounded-full transition-colors",
          checked ? "bg-primary" : "bg-muted-foreground/30",
        )}
      >
        <span
          className={cn(
            "inline-block h-4 w-4 rounded-full bg-background shadow-sm transition-transform",
            checked ? "translate-x-4" : "translate-x-0.5",
          )}
        />
      </span>
      <span className="min-w-7 text-left uppercase tracking-wide text-muted-foreground">{checked ? onLabel : offLabel}</span>
    </button>
  );
}

