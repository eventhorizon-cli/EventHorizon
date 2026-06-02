import { ChevronDown, ChevronLeft, ChevronRight, ChevronUp, Diff, X } from "lucide-react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { DiffViewer } from "@/components/diff/DiffViewer";
import { cn } from "@/utils/cn";
import type { FileChange, FileDiff } from "@/types";

type DiffDialogProps = {
  open: boolean;
  changes: FileChange[];
  selectedFile?: string;
  currentDiff?: FileDiff;
  loading?: boolean;
  error?: string;
  theme: "light" | "dark";
  onClose: () => void;
  onSelectChange: (change: FileChange) => Promise<void> | void;
  onOpenPrevious: () => Promise<void> | void;
  onOpenNext: () => Promise<void> | void;
};

function isEditableTarget(target: EventTarget | null) {
  return target instanceof HTMLElement
    && (target.tagName === "INPUT" || target.tagName === "TEXTAREA" || target.isContentEditable);
}

export function DiffDialog({
  open,
  changes,
  selectedFile,
  currentDiff,
  loading,
  error,
  theme,
  onClose,
  onSelectChange,
  onOpenPrevious,
  onOpenNext,
}: DiffDialogProps) {
  const dialogRef = useRef<HTMLDivElement | null>(null);
  const selectedIndex = useMemo(
    () => changes.findIndex((change) => change.path === selectedFile),
    [changes, selectedFile],
  );
  const selectedChange = selectedIndex >= 0 ? changes[selectedIndex] : changes[0];
  const activeDiff = currentDiff?.path === selectedChange?.path ? currentDiff : undefined;
  const hasPrevious = selectedIndex > 0;
  const hasNext = selectedIndex >= 0 && selectedIndex < changes.length - 1;
  const selectedButtonRef = useRef<HTMLButtonElement | null>(null);
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);
  const [isFullscreen, setIsFullscreen] = useState(false);

  const handleClose = useCallback(() => {
    if (document.fullscreenElement === dialogRef.current) {
      void document.exitFullscreen().catch(() => undefined);
    }

    onClose();
  }, [onClose]);

  const toggleFullscreen = useCallback(async () => {
    const element = dialogRef.current;
    if (!element || typeof element.requestFullscreen !== "function") {
      return;
    }

    try {
      if (document.fullscreenElement === element) {
        await document.exitFullscreen();
      } else {
        await element.requestFullscreen();
      }
    } catch {
      // Ignore fullscreen API errors to keep the dialog usable.
    }
  }, []);

  useEffect(() => {
    const syncFullscreenState = () => {
      setIsFullscreen(document.fullscreenElement === dialogRef.current);
    };

    syncFullscreenState();
    document.addEventListener("fullscreenchange", syncFullscreenState);

    return () => document.removeEventListener("fullscreenchange", syncFullscreenState);
  }, []);

  useEffect(() => {
    if (!open) {
      return undefined;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();

        if (document.fullscreenElement === dialogRef.current) {
          void document.exitFullscreen().catch(() => undefined);
          return;
        }

        handleClose();
        return;
      }

      if (isEditableTarget(event.target)) {
        return;
      }

      if (event.key === "ArrowUp" && hasPrevious) {
        event.preventDefault();
        void onOpenPrevious();
      }

      if (event.key === "ArrowDown" && hasNext) {
        event.preventDefault();
        void onOpenNext();
      }
    };

    window.addEventListener("keydown", handleKeyDown);

    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [handleClose, hasNext, hasPrevious, onOpenNext, onOpenPrevious, open]);

  useEffect(() => {
    selectedButtonRef.current?.scrollIntoView({ block: "nearest" });
  }, [selectedFile]);

  if (!open) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/55 backdrop-blur-sm" onClick={handleClose} />

      <div
        ref={dialogRef}
        className={cn(
          "relative z-10 flex overflow-hidden border border-border bg-background shadow-2xl",
          isFullscreen ? "h-full w-full rounded-none" : "h-[88vh] w-full max-w-7xl rounded-3xl",
        )}
      >
        {!isSidebarCollapsed ? (
          <aside className="flex w-full max-w-xs shrink-0 flex-col border-r border-border bg-card/70">
          <div className="border-b border-border px-5 py-4">
            <div className="flex items-center justify-between gap-3">
              <div>
                <div className="text-xs uppercase tracking-wide text-muted-foreground">Changed files</div>
                <div className="mt-1 text-lg font-semibold">{changes.length} diffs</div>
              </div>
              <div className="rounded-full bg-muted px-3 py-1 text-xs text-muted-foreground">
                {selectedIndex >= 0 ? `${selectedIndex + 1} / ${changes.length}` : `0 / ${changes.length}`}
              </div>
            </div>
            <div className="mt-3 text-xs text-muted-foreground">
              Select a file from the list, or use <kbd className="rounded bg-muted px-1.5 py-0.5 font-mono">↑</kbd> / <kbd className="rounded bg-muted px-1.5 py-0.5 font-mono">↓</kbd> to switch.
            </div>
          </div>

          <div className="min-h-0 flex-1 overflow-y-auto p-3">
            <div className="space-y-2">
              {changes.map((change) => {
                const isSelected = change.path === selectedChange?.path;

                return (
                  <button
                    key={change.path}
                    ref={isSelected ? selectedButtonRef : undefined}
                    type="button"
                    onClick={() => void onSelectChange(change)}
                    className={cn(
                      "flex w-full items-start justify-between gap-3 rounded-2xl border px-3 py-3 text-left transition",
                      isSelected
                        ? "border-primary bg-primary/10"
                        : "border-border bg-background/70 hover:bg-muted",
                    )}
                  >
                    <div className="min-w-0">
                      <div className="truncate text-sm font-medium text-foreground">{change.oldPath && change.oldPath !== change.path ? `${change.oldPath} → ${change.path}` : change.path}</div>
                      <div className="mt-1 text-xs text-muted-foreground">{change.status}</div>
                    </div>
                    <div className="shrink-0 text-right text-xs text-muted-foreground">
                      <div>+{change.additions ?? 0}</div>
                      <div>-{change.deletions ?? 0}</div>
                    </div>
                  </button>
                );
              })}
            </div>
          </div>
          </aside>
        ) : null}

        <section className="flex min-w-0 flex-1 flex-col overflow-hidden">
          <div className="flex items-center justify-between gap-4 border-b border-border px-5 py-4">
            <div className="flex min-w-0 items-start gap-3">
              <button
                type="button"
                onClick={() => setIsSidebarCollapsed((current) => !current)}
                className="mt-0.5 rounded-xl border border-border px-2 py-2 text-muted-foreground transition hover:bg-muted hover:text-foreground"
                title={isSidebarCollapsed ? "Show file list" : "Hide file list"}
              >
                {isSidebarCollapsed ? <ChevronRight className="h-4 w-4" /> : <ChevronLeft className="h-4 w-4" />}
              </button>

              <div className="min-w-0">
                <div className="flex items-center gap-2 text-xs uppercase tracking-wide text-muted-foreground">
                  <Diff className="h-4 w-4" />
                  <span>Diff viewer</span>
                  <span className="rounded-full bg-muted px-2 py-0.5 normal-case text-[11px]">
                    {selectedIndex >= 0 ? `${selectedIndex + 1} / ${changes.length}` : `0 / ${changes.length}`}
                  </span>
                </div>
                <div className="mt-1 truncate text-sm font-medium text-foreground">
                  {selectedChange
                    ? selectedChange.oldPath && selectedChange.oldPath !== selectedChange.path
                      ? `${selectedChange.oldPath} → ${selectedChange.path}`
                      : selectedChange.path
                    : "No file selected"}
                </div>
              </div>
            </div>

            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={() => void onOpenPrevious()}
                disabled={!hasPrevious}
                className="inline-flex items-center justify-center gap-2 rounded-xl border border-border px-3 py-2 text-sm font-medium transition hover:bg-muted disabled:cursor-not-allowed disabled:opacity-50"
              >
                <ChevronUp className="h-4 w-4" />
                Previous
              </button>
              <button
                type="button"
                onClick={() => void onOpenNext()}
                disabled={!hasNext}
                className="inline-flex items-center justify-center gap-2 rounded-xl border border-border px-3 py-2 text-sm font-medium transition hover:bg-muted disabled:cursor-not-allowed disabled:opacity-50"
              >
                <ChevronDown className="h-4 w-4" />
                Next
              </button>
              <button
                type="button"
                onClick={() => void toggleFullscreen()}
                className="rounded-xl border border-border px-3 py-2 text-sm font-medium text-muted-foreground transition hover:bg-muted hover:text-foreground"
                title={isFullscreen ? "Exit fullscreen" : "Enter fullscreen"}
              >
                {isFullscreen ? "Exit fullscreen" : "Fullscreen"}
              </button>
              <button
                type="button"
                onClick={handleClose}
                className="rounded-xl p-2 text-muted-foreground transition hover:bg-muted hover:text-foreground"
                title="Close diff viewer"
              >
                <X className="h-5 w-5" />
              </button>
            </div>
          </div>

          <div className="min-h-0 flex-1 p-4">
            {selectedChange ? (
              <DiffViewer
                path={selectedChange.path}
                oldPath={selectedChange.oldPath}
                status={selectedChange.status}
                additions={selectedChange.additions}
                deletions={selectedChange.deletions}
                binary={activeDiff?.binary ?? selectedChange.binary}
                oldText={activeDiff?.oldText}
                newText={activeDiff?.newText}
                language={activeDiff?.language}
                loading={loading}
                error={error}
                theme={theme}
              />
            ) : (
              <div className="flex h-full items-center justify-center rounded-2xl border border-dashed border-border text-sm text-muted-foreground">
                No file changes available.
              </div>
            )}
          </div>
        </section>
      </div>
    </div>
  );
}

