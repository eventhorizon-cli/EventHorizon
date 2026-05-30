import { AlertTriangle, X } from "lucide-react";
import { cn } from "@/utils/cn";

type ConfirmDialogProps = {
  open: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  confirmVariant?: "default" | "danger";
  onCancel: () => void;
  onConfirm: () => void;
};

export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel = "Confirm",
  cancelLabel = "Cancel",
  confirmVariant = "default",
  onCancel,
  onConfirm,
}: ConfirmDialogProps) {
  if (!open) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onCancel} />
      <div className="relative z-10 w-full max-w-md overflow-hidden rounded-3xl border border-border bg-background shadow-xl">
        <button
          type="button"
          onClick={onCancel}
          className="absolute right-4 top-4 rounded-xl p-1.5 text-muted-foreground transition hover:bg-muted hover:text-foreground"
        >
          <X className="h-4 w-4" />
        </button>

        <div className="p-6">
          <div className="flex flex-col items-center text-center">
            <div className={cn(
              "flex h-14 w-14 items-center justify-center rounded-full",
              confirmVariant === "danger"
                ? "bg-red-500/10 text-red-500"
                : "bg-amber-500/10 text-amber-500"
            )}>
              <AlertTriangle className="h-7 w-7" />
            </div>

            <h3 className="mt-4 text-lg font-semibold">{title}</h3>
            <p className="mt-2 text-sm text-muted-foreground">{message}</p>
          </div>
        </div>

        <div className="flex items-center justify-end gap-3 border-t border-border px-6 py-4">
          <button
            type="button"
            onClick={onCancel}
            className="rounded-xl border border-border px-4 py-2 text-sm font-medium text-muted-foreground transition hover:bg-muted"
          >
            {cancelLabel}
          </button>
          <button
            type="button"
            onClick={onConfirm}
            className={cn(
              "rounded-xl px-4 py-2 text-sm font-medium transition",
              confirmVariant === "danger"
                ? "bg-red-500 text-red-500-foreground hover:bg-red-500/90"
                : "bg-primary text-primary-foreground hover:opacity-90"
            )}
          >
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}
