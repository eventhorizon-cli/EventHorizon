import { DiffEditor } from "@monaco-editor/react";
import { Copy, FileDiff } from "lucide-react";
import { useMemo } from "react";
import { cn } from "@/utils/cn";
import type { FileDiff as FileDiffModel } from "@/types";

export type DiffViewerProps = FileDiffModel & {
  loading?: boolean;
  error?: string;
  theme: "light" | "dark";
  onBack?: () => void;
};

export function DiffViewer({
  path,
  oldPath,
  status,
  oldText,
  newText,
  language,
  binary,
  additions,
  deletions,
  loading,
  error,
  theme,
  onBack,
}: DiffViewerProps) {
  const tooLarge = useMemo(() => (oldText?.length ?? 0) + (newText?.length ?? 0) > 200_000, [newText, oldText]);
  const omittedLargeText = !binary && !loading && !error && !tooLarge && oldText == null && newText == null;

  return (
    <div className="flex h-full flex-col overflow-hidden rounded-xl border border-border bg-card shadow-panel">
      <div className="flex items-center justify-between border-b border-border px-4 py-3">
        <div className="min-w-0">
          <div className="flex items-center gap-2 text-xs uppercase tracking-wide text-muted-foreground">
            <FileDiff className="h-4 w-4" />
            <span>{status}</span>
            {typeof additions === "number" ? <span>+{additions}</span> : null}
            {typeof deletions === "number" ? <span>-{deletions}</span> : null}
          </div>
          <div className="truncate text-sm font-medium text-foreground">
            {oldPath && oldPath !== path ? `${oldPath} → ${path}` : path}
          </div>
        </div>
        <div className="flex items-center gap-2">
          {onBack ? (
            <button type="button" className="rounded-md px-3 py-1.5 text-xs text-muted-foreground hover:bg-muted" onClick={onBack}>
              Back
            </button>
          ) : null}
          <button
            type="button"
            className="rounded-md p-2 text-muted-foreground hover:bg-muted"
            onClick={() => navigator.clipboard.writeText(path)}
            title="Copy path"
          >
            <Copy className="h-4 w-4" />
          </button>
        </div>
      </div>
      <div className="flex-1 overflow-hidden">
        {loading ? <div className="p-4 text-sm text-muted-foreground">Loading diff…</div> : null}
        {error ? <div className="p-4 text-sm text-destructive">{error}</div> : null}
        {binary ? <div className="p-4 text-sm text-muted-foreground">Binary file changed.</div> : null}
        {!loading && !error && !binary && tooLarge ? (
          <div className="p-4 text-sm text-muted-foreground">This diff is too large to render inline.</div>
        ) : null}
        {omittedLargeText ? (
          <div className="p-4 text-sm text-muted-foreground">This diff was omitted by the server because it is too large to load inline.</div>
        ) : null}
        {!loading && !error && !binary && !tooLarge && !omittedLargeText ? (
          <DiffEditor
            height="100%"
            original={oldText ?? ""}
            modified={newText ?? ""}
            language={language || undefined}
            theme={theme === "dark" ? "vs-dark" : "vs"}
            options={{
              readOnly: true,
              minimap: { enabled: false },
              renderSideBySide: true,
              automaticLayout: true,
              fontSize: 13,
            }}
            className={cn("h-full")}
          />
        ) : null}
      </div>
    </div>
  );
}

