import { useCallback, useState } from "react";
import { getDirectories } from "@/api/sessionsApi";
import type { DirectoryItem } from "@/types";

type UseDirectoryPickerOptions = {
  initialPath?: string;
  onConfirm: (path: string) => Promise<void> | void;
};

export function useDirectoryPicker({ initialPath, onConfirm }: UseDirectoryPickerOptions) {
  const [open, setOpen] = useState(false);
  const [directories, setDirectories] = useState<DirectoryItem[]>([]);
  const [currentPath, setCurrentPath] = useState<string | undefined>();
  const [selectedPath, setSelectedPath] = useState<string | undefined>();
  const [pathInput, setPathInput] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const loadDirectories = useCallback(async (path?: string) => {
    setIsLoading(true);

    try {
      const listing = await getDirectories(path);
      setDirectories(listing.items);
      setCurrentPath(listing.currentPath);
      setSelectedPath(listing.currentPath);
      setPathInput(listing.currentPath);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const updatePathInput = useCallback((value: string) => {
    setPathInput(value);
    setSelectedPath(undefined);
  }, []);

  const openPicker = useCallback(async (path = initialPath) => {
    setOpen(true);
    updatePathInput(path ?? "");
    await loadDirectories(path);
  }, [initialPath, loadDirectories, updatePathInput]);

  const closePicker = useCallback(() => {
    setOpen(false);
    setDirectories([]);
    setCurrentPath(undefined);
    setSelectedPath(undefined);
    updatePathInput("");
  }, [updatePathInput]);

  const selectPath = useCallback((item: DirectoryItem) => {
    if (!item.isDirectory) {
      return;
    }

    setSelectedPath(item.path);
    setPathInput(item.path);
  }, []);

  const navigateToPath = useCallback(async (item: DirectoryItem) => {
    if (!item.isDirectory) {
      return;
    }

    await loadDirectories(item.path);
  }, [loadDirectories]);

  const submitPathInput = useCallback(async () => {
    const nextPath = pathInput.trim();
    if (!nextPath) {
      return;
    }

    await loadDirectories(nextPath);
  }, [loadDirectories, pathInput]);

  const confirmSelection = useCallback(async () => {
    const nextPath = selectedPath?.trim() || pathInput.trim();
    if (!nextPath) {
      return;
    }

    await onConfirm(nextPath);
    closePicker();
  }, [closePicker, onConfirm, pathInput, selectedPath]);

  return {
    open,
    directories,
    currentPath,
    selectedPath,
    pathInput,
    isLoading,
    setPathInput: updatePathInput,
    openPicker,
    closePicker,
    loadDirectories,
    selectPath,
    navigateToPath,
    submitPathInput,
    confirmSelection,
  };
}
