import { useMemo, useState } from "react";
import { ChevronDown, ChevronRight, FileText, Files, FolderOpen } from "lucide-react";
import { ModifiedFileDiffPreview } from "@/components/chat/ModifiedFileDiffPreview";
import { cn } from "@/utils/cn";
import type { FileChange } from "@/types";

export type ModifiedFilesCardProps = {
  runId: string;
  files: FileChange[];
  defaultCollapsed?: boolean;
  onViewFiles: () => void;
  onViewDiff: (path: string) => void;
};

export function ModifiedFilesCard({
  runId,
  files,
  defaultCollapsed = true,
  onViewFiles,
  onViewDiff,
}: ModifiedFilesCardProps) {
  const [collapsed, setCollapsed] = useState(defaultCollapsed);
  const [expandedPath, setExpandedPath] = useState<string>();

  const totals = useMemo(
    () => ({
      additions: files.reduce((sum, file) => sum + (file.additions ?? 0), 0),
      deletions: files.reduce((sum, file) => sum + (file.deletions ?? 0), 0),
      modified: files.filter((file) => file.status === "modified").length,
      added: files.filter((file) => file.status === "added").length,
      deleted: files.filter((file) => file.status === "deleted").length,
      renamed: files.filter((file) => file.status === "renamed").length,
    }),
    [files],
  );

  const summary = [
    totals.modified > 0 ? `${totals.modified} modified` : undefined,
    totals.added > 0 ? `${totals.added} added` : undefined,
    totals.deleted > 0 ? `${totals.deleted} deleted` : undefined,
    totals.renamed > 0 ? `${totals.renamed} renamed` : undefined,
  ]
    .filter(Boolean)
    .join(" · ");

  if (files.length === 0) {
    return null;
  }

  return (
    <section className="w-full rounded-3xl border border-border/70 bg-card p-4 shadow-sm">
      <button
        type="button"
        onClick={() => setCollapsed((value) => !value)}
        className="flex w-full items-start justify-between gap-3 text-left"
      >
        <div className="min-w-0">
          <div className="flex items-center gap-2 text-xs uppercase tracking-wide text-muted-foreground">
            <Files className="h-4 w-4" />
            <span>Files changed</span>
            <span className="rounded-full bg-muted px-2 py-0.5 normal-case text-[11px]">{runId}</span>
          </div>
          <div className="mt-2 text-sm font-medium text-foreground">{files.length} files changed</div>
          <div className="mt-1 text-xs text-muted-foreground">
            {summary || "Changed files"} · +{totals.additions} / -{totals.deletions}
          </div>
        </div>
        {collapsed ? <ChevronRight className="mt-1 h-5 w-5 text-muted-foreground" /> : <ChevronDown className="mt-1 h-5 w-5 text-muted-foreground" />}
      </button>

      {!collapsed ? (
        <div className="mt-4 space-y-2">
          {files.map((file) => {
            const isExpanded = expandedPath === file.path;

            return (
              <div key={`${file.oldPath ?? file.path}->${file.path}`} className="rounded-2xl border border-border/70 bg-background/60 p-3">
                <div className="flex items-center gap-3">
                  <button
                    type="button"
                    onClick={() => setExpandedPath((value) => (value === file.path ? undefined : file.path))}
                    className="flex min-w-0 flex-1 items-center gap-2 text-left"
                  >
                    {isExpanded ? (
                      <ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground" />
                    ) : (
                      <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                    )}
                    <FileText className="h-4 w-4 shrink-0 text-muted-foreground" />
                    <div className="min-w-0 flex-1">
                      <div className="truncate text-sm font-medium text-foreground">
                        {file.oldPath && file.oldPath !== file.path ? `${file.oldPath} → ${file.path}` : file.path}
                      </div>
                      <div className="mt-1 text-xs text-muted-foreground">
                        {file.status} · +{file.additions ?? 0} / -{file.deletions ?? 0}
                      </div>
                    </div>
                  </button>

                  <button
                    type="button"
                    onClick={() => onViewDiff(file.path)}
                    className={cn(
                      "shrink-0 rounded-xl border border-border px-3 py-1.5 text-xs font-medium transition hover:bg-muted",
                      file.binary && "text-muted-foreground",
                    )}
                  >
                    View diff
                  </button>
                </div>

                {isExpanded ? <ModifiedFileDiffPreview file={file} onOpenFullDiff={() => onViewDiff(file.path)} /> : null}
              </div>
            );
          })}

          <div className="flex flex-wrap gap-2 pt-1">
            <button
              type="button"
              onClick={onViewFiles}
              className="inline-flex items-center gap-2 rounded-xl border border-border px-3 py-1.5 text-xs font-medium transition hover:bg-muted"
            >
              <FolderOpen className="h-4 w-4" />
              View files
            </button>
            <button
              type="button"
              onClick={() => onViewDiff(files[0].path)}
              className="inline-flex items-center gap-2 rounded-xl border border-border px-3 py-1.5 text-xs font-medium transition hover:bg-muted"
            >
              <Files className="h-4 w-4" />
              View all diffs
            </button>
          </div>
        </div>
      ) : null}
    </section>
  );
}

