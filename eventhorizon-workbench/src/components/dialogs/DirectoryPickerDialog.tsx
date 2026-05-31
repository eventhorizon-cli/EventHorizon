import { Check, ChevronRight, File, Folder, FolderOpen, Loader2, X } from "lucide-react";
import { cn } from "@/utils/cn";
import type { DirectoryItem } from "@/types";

type DirectoryPickerDialogProps = {
  open: boolean;
  title: string;
  confirmLabel: string;
  zIndexClassName?: string;
  currentPath?: string;
  selectedPath?: string;
  pathInput: string;
  directories: DirectoryItem[];
  isLoading: boolean;
  showCreateFolderAction?: boolean;
  onCancel: () => void;
  onConfirm: () => void;
  onPathInputChange: (value: string) => void;
  onPathInputSubmit: () => void;
  onCreateFolder?: () => void;
  onSelectPath: (item: DirectoryItem) => void;
  onDoubleClickPath: (item: DirectoryItem) => void;
};

export function DirectoryPickerDialog({
  open,
  title,
  confirmLabel,
  zIndexClassName = "z-50",
  currentPath,
  selectedPath,
  pathInput,
  directories,
  isLoading,
  showCreateFolderAction = false,
  onCancel,
  onConfirm,
  onPathInputChange,
  onPathInputSubmit,
  onCreateFolder,
  onSelectPath,
  onDoubleClickPath,
}: DirectoryPickerDialogProps) {
  if (!open) {
    return null;
  }

  return (
    <div className={`fixed inset-0 ${zIndexClassName} flex items-center justify-center p-4`}>
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onCancel} />
      <div className="relative z-10 w-full max-w-2xl max-h-[80vh] overflow-hidden rounded-3xl border border-border bg-background shadow-xl">
        <div className="flex items-center justify-between border-b border-border px-6 py-4">
          <div>
            <h2 className="text-lg font-semibold">{title}</h2>
            <div className="mt-1 flex items-center gap-4 text-xs text-muted-foreground">
              <span><kbd className="rounded bg-muted px-1.5 py-0.5 font-mono">Single-click</kbd> to select</span>
              <span><kbd className="rounded bg-muted px-1.5 py-0.5 font-mono">Double-click</kbd> folder to navigate</span>
              <span><kbd className="rounded bg-muted px-1.5 py-0.5 font-mono">..</kbd> to go up</span>
            </div>
          </div>
          <button
            type="button"
            onClick={onCancel}
            className="rounded-xl p-2 text-muted-foreground transition hover:bg-muted hover:text-foreground"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <div className="border-b border-border px-6 py-3">
          <div className="mb-3 flex items-center gap-2 text-sm text-muted-foreground">
            <ChevronRight className="h-4 w-4" />
            <span className="truncate font-mono">{currentPath || pathInput || "Loading..."}</span>
          </div>
          <div className="flex items-center gap-2">
            <input
              type="text"
              value={pathInput}
              onChange={(event) => onPathInputChange(event.target.value)}
              onKeyDown={(event) => event.key === "Enter" && onPathInputSubmit()}
              placeholder="Or paste any directory path..."
              className="flex-1 rounded-xl border border-border bg-background px-3 py-2 text-sm font-mono placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary"
            />
            <button
              type="button"
              onClick={onPathInputSubmit}
              className="rounded-xl border border-border px-3 py-2 text-sm font-medium text-muted-foreground transition hover:bg-muted"
            >
              Go
            </button>
            {showCreateFolderAction && onCreateFolder ? (
              <button
                type="button"
                onClick={onCreateFolder}
                className="rounded-xl border border-border px-3 py-2 text-sm font-medium text-muted-foreground transition hover:bg-muted"
              >
                + New Folder
              </button>
            ) : null}
          </div>
        </div>

        <div className="max-h-[50vh] overflow-y-auto p-4">
          {isLoading ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-6 w-6 animate-spin text-primary" />
            </div>
          ) : (
            <div className="space-y-1">
              {directories.map((item) => (
                <button
                  key={item.path}
                  type="button"
                  onClick={() => onSelectPath(item)}
                  onDoubleClick={() => onDoubleClickPath(item)}
                  className={cn(
                    "flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-left transition",
                    selectedPath === item.path
                      ? "bg-primary/10 text-primary"
                      : "hover:bg-muted",
                    item.isDirectory && "cursor-pointer",
                    !item.isDirectory && "cursor-default opacity-60",
                  )}
                >
                  {item.isDirectory ? (
                    <Folder className="h-4 w-4 text-amber-500" />
                  ) : (
                    <File className="h-4 w-4 text-muted-foreground" />
                  )}
                  <span className="flex-1 truncate text-sm">{item.name}</span>
                  {item.isDirectory && item.name !== ".." ? <ChevronRight className="h-4 w-4 text-muted-foreground" /> : null}
                  {selectedPath === item.path ? <Check className="h-4 w-4 text-primary" /> : null}
                </button>
              ))}
            </div>
          )}
        </div>

        <div className="flex items-center justify-end gap-3 border-t border-border px-6 py-4">
          <button
            type="button"
            onClick={onCancel}
            className="rounded-xl border border-border px-4 py-2 text-sm font-medium text-muted-foreground transition hover:bg-muted"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={onConfirm}
            disabled={!pathInput.trim()}
            className={cn(
              "inline-flex items-center gap-2 rounded-xl px-4 py-2 text-sm font-medium transition",
              pathInput.trim()
                ? "bg-primary text-primary-foreground hover:opacity-90"
                : "cursor-not-allowed bg-muted text-muted-foreground",
            )}
          >
            <FolderOpen className="h-4 w-4" />
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}
