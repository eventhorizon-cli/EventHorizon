import { FileDiff, FileImage } from "lucide-react";
import type { FileChange } from "@/types";

export type ModifiedFileDiffPreviewProps = {
  file: FileChange;
  onOpenFullDiff: () => void;
};

export function ModifiedFileDiffPreview({ file, onOpenFullDiff }: ModifiedFileDiffPreviewProps) {
  return (
    <div className="mt-2 rounded-2xl border border-border/70 bg-background/70 p-3 text-sm text-muted-foreground">
      <div className="flex items-center gap-2 text-xs uppercase tracking-wide">
        {file.binary ? <FileImage className="h-4 w-4" /> : <FileDiff className="h-4 w-4" />}
        <span>{file.status}</span>
        <span>+{file.additions ?? 0}</span>
        <span>-{file.deletions ?? 0}</span>
      </div>

      <div className="mt-2">
        {file.binary
          ? "Binary files cannot be previewed inline."
          : "Open the full diff in the right pane to inspect the complete change."}
      </div>

      <button
        type="button"
        onClick={onOpenFullDiff}
        className="mt-3 rounded-xl border border-border px-3 py-1.5 text-xs font-medium text-foreground transition hover:bg-muted"
      >
        Open full diff
      </button>
    </div>
  );
}

