import { useCallback, useState } from "react";
import { createDirectory, getDirectories } from "@/api/sessionsApi";
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
  const [errorMessage, setErrorMessage] = useState<string | undefined>();
  const [isCreateFolderFormOpen, setIsCreateFolderFormOpen] = useState(false);
  const [createFolderName, setCreateFolderName] = useState("");
  const [isCreatingFolder, setIsCreatingFolder] = useState(false);

  const formatError = useCallback((error: unknown) => {
    return error instanceof Error ? error.message : "Unexpected error";
  }, []);

  const resetCreateFolderState = useCallback(() => {
    setIsCreateFolderFormOpen(false);
    setCreateFolderName("");
  }, []);

  const loadDirectories = useCallback(async (path?: string) => {
    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const listing = await getDirectories(path);
      setDirectories(listing.items);
      setCurrentPath(listing.currentPath);
      setSelectedPath(listing.currentPath);
      setPathInput(listing.currentPath);
      resetCreateFolderState();
    } catch (error) {
      setErrorMessage(formatError(error));
    } finally {
      setIsLoading(false);
    }
  }, [formatError, resetCreateFolderState]);

  const updatePathInput = useCallback((value: string) => {
    setPathInput(value);
    setSelectedPath(undefined);
    setErrorMessage(undefined);
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
    setErrorMessage(undefined);
    updatePathInput("");
    resetCreateFolderState();
  }, [resetCreateFolderState, updatePathInput]);

  const selectPath = useCallback((item: DirectoryItem) => {
    if (!item.isDirectory) {
      return;
    }

    setSelectedPath(item.path);
    setPathInput(item.path);
    setErrorMessage(undefined);
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
      setErrorMessage("Directory path is required.");
      return;
    }

    await loadDirectories(nextPath);
  }, [loadDirectories, pathInput]);

  const openCreateFolderForm = useCallback(() => {
    setErrorMessage(undefined);
    setIsCreateFolderFormOpen(true);
  }, []);

  const cancelCreateFolder = useCallback(() => {
    setErrorMessage(undefined);
    resetCreateFolderState();
  }, [resetCreateFolderState]);

  const submitCreateFolder = useCallback(async () => {
    const parentPath = currentPath ?? pathInput.trim();
    const nextFolderName = createFolderName.trim();

    if (!parentPath) {
      setErrorMessage("Select a parent directory before creating a folder.");
      return;
    }

    if (!nextFolderName) {
      setErrorMessage("Folder name is required.");
      return;
    }

    setIsCreatingFolder(true);
    setErrorMessage(undefined);

    try {
      const createdDirectory = await createDirectory({
        parentPath,
        name: nextFolderName,
      });

      await loadDirectories(parentPath);
      setSelectedPath(createdDirectory.path);
      setPathInput(createdDirectory.path);
      resetCreateFolderState();
    } catch (error) {
      setErrorMessage(formatError(error));
    } finally {
      setIsCreatingFolder(false);
    }
  }, [createFolderName, currentPath, formatError, loadDirectories, pathInput, resetCreateFolderState]);

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
    errorMessage,
    isCreateFolderFormOpen,
    createFolderName,
    isCreatingFolder,
    setPathInput: updatePathInput,
    setCreateFolderName,
    openPicker,
    closePicker,
    loadDirectories,
    selectPath,
    navigateToPath,
    submitPathInput,
    openCreateFolderForm,
    cancelCreateFolder,
    submitCreateFolder,
    confirmSelection,
  };
}
