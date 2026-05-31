import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { subscribeRunEvents } from "@/api/workbenchClient";
import { getConfiguration, importSkillToTarget, removeGlobalSkill, removeSessionSkill, saveConfiguration, testMcp, testProvider } from "@/api/configurationApi";
import { getChanges, getFileDiff } from "@/api/diffApi";
import {
  createSession,
  deleteSession,
  getSession,
  getSessions,
  updateSession,
  updateSessionModel,
} from "@/api/sessionsApi";
import { cancelRun, createRun, getRun } from "@/api/runsApi";
import { useDirectoryPicker } from "@/hooks/useDirectoryPicker";
import { useWorkbenchStore } from "@/store/workbenchStore";
import {
  cloneConfiguration,
  createProviderDraft,
  getProvider,
  getProviderModels,
  globalSettingsTabs,
  isProviderFieldVisible,
  normalizeOptionalText,
} from "@/utils/configuration";
import { buildTemporarySessionTitle } from "@/utils/sessionTitle";
import type { AgentEvent, AgentRun, AgentSessionDetail, AppConfiguration, FileChange, McpServerConfig, ProviderEntry, ProviderType, SkillImportResult } from "@/types";

const leftPaneKey = "event-horizon-workbench-left-pane-collapsed";
const compactLayoutQuery = "(max-width: 1180px)";

function mapPhase(event: AgentEvent) {
  switch (event.type) {
    case "runStarted":
      return "understanding" as const;
    case "plan.updated":
      return "planning" as const;
    case "toolCallStart":
      return "editing" as const;
    case "command.started":
    case "test.started":
      return "validating" as const;
    case "runFinished":
      return "completed" as const;
    case "runError":
      return "failed" as const;
    case "runCancelled":
      return "cancelled" as const;
    default:
      return undefined;
  }
}

function createDraftSession(task: string): AgentSessionDetail {
  const id = `draft_${crypto.randomUUID()}`;

  return {
    id,
    title: buildTemporarySessionTitle(task),
    status: "idle",
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    isTitleGenerated: false,
    messages: [],
    sessionSkills: { imported: [] },
  };
}

function formatError(error: unknown) {
  return error instanceof Error ? error.message : "Unexpected error";
}

function mapRunStatusToPhase(status: AgentRun["status"]) {
  switch (status) {
    case "running":
      return "understanding" as const;
    case "completed":
      return "completed" as const;
    case "failed":
      return "failed" as const;
    case "cancelled":
      return "cancelled" as const;
    default:
      return "idle" as const;
  }
}

export function useWorkbenchApp() {
  const {
    sessions,
    currentSession,
    currentRun,
    phase,
    connectionStatus,
    contextView,
    selectedFile,
    changes,
    currentDiff,
    logs,
    themeMode,
    setSessions,
    setCurrentSession,
    setCurrentRun,
    setPhase,
    setConnectionStatus,
    setContextView,
    setSelectedFile,
    setChanges,
    setCurrentDiff,
    addLog,
    appendAssistantDelta,
    finishAssistantMessage,
    addUserMessage,
  } = useWorkbenchStore();

  const [composerValue, setComposerValue] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [leftPaneCollapsed, setLeftPaneCollapsed] = useState(false);
  const [isCompactLayout, setIsCompactLayout] = useState(false);
  const [configuration, setConfiguration] = useState<AppConfiguration>();
  const [configurationDraft, setConfigurationDraft] = useState<AppConfiguration>();
  const [isLoadingConfiguration, setIsLoadingConfiguration] = useState(false);
  const [isSavingConfiguration, setIsSavingConfiguration] = useState(false);
  const [configurationError, setConfigurationError] = useState<string>();
  const [detailsMessage, setDetailsMessage] = useState<string>();
  const [detailsError, setDetailsError] = useState<string>();
  const [globalSettingsTab, setGlobalSettingsTab] = useState<(typeof globalSettingsTabs)[number]>("providers");
  const [globalSettingsMessage, setGlobalSettingsMessage] = useState<string>();
  const [globalSettingsError, setGlobalSettingsError] = useState<string>();
  const [sessionSettingsMessage, setSessionSettingsMessage] = useState<string>();
  const [sessionSettingsError, setSessionSettingsError] = useState<string>();
  const [mcpTestResults, setMcpTestResults] = useState<Record<number, string>>({});
  const [providerTestResults, setProviderTestResults] = useState<Record<number, string>>({});
  const [testingProviderIndexes, setTestingProviderIndexes] = useState<Record<number, boolean>>({});
  const [skillImportPath, setSkillImportPath] = useState("");
  const [isImportingSkill, setIsImportingSkill] = useState(false);
  const [skillImportTarget, setSkillImportTarget] = useState<"global" | "session">("global");
  const [isUpdatingSession, setIsUpdatingSession] = useState(false);
  const [sessionTitleInput, setSessionTitleInput] = useState("");
  const [showGlobalSettingsDialog, setShowGlobalSettingsDialog] = useState(false);

  const eventSubscriptionRef = useRef<(() => void) | null>(null);
  const didAutoOpenInitialSessionRef = useRef(false);
  const currentSessionId = currentSession?.id;
  const selectedProviderName = currentSession?.providerName ?? configuration?.currentDefaultProvider;
  const selectedProvider = getProvider(configurationDraft ?? configuration, selectedProviderName);
  const selectedProviderDefaultModel = selectedProvider?.provider.model;
  const availableModels = getProviderModels(selectedProvider, currentSession?.model);
  const sessionModelWarning = currentSession?.model && selectedProviderName && selectedProvider
    && !selectedProvider.provider.models.includes(currentSession.model)
    && selectedProvider.provider.model !== currentSession.model
    ? `Current session model '${currentSession.model}' is no longer configured for provider '${selectedProviderName}'. Choose a new model to continue using the latest provider configuration.`
    : undefined;

  const refreshConfiguration = useCallback(async () => {
    setIsLoadingConfiguration(true);
    setConfigurationError(undefined);

    try {
      const nextConfiguration = await getConfiguration();
      setConfiguration(nextConfiguration);
      setConfigurationDraft(cloneConfiguration(nextConfiguration));
      setProviderTestResults({});
      setTestingProviderIndexes({});
    } catch (error) {
      setConfigurationError(formatError(error));
    } finally {
      setIsLoadingConfiguration(false);
    }
  }, []);

  const openSession = useCallback(async (sessionId: string) => {
    setDetailsMessage(undefined);
    setDetailsError(undefined);
    setSessionSettingsMessage(undefined);
    setSessionSettingsError(undefined);

    const detail = await getSession(sessionId);
    setCurrentSession(detail);
    setCurrentDiff(undefined);
    setSelectedFile(undefined);
    setContextView("overview");

    if (detail.lastRunId) {
      const run = await getRun(detail.id, detail.lastRunId);
      setCurrentRun(run);

      if (run.status !== "running") {
        try {
          setChanges(await getChanges(detail.id, run.id));
        } catch {
          setChanges([]);
        }
      }

      if (run.status === "running") {
        subscribeToRun(run, detail.id);
      }
    } else {
      setCurrentRun(undefined);
      setChanges([]);
    }
  }, [setChanges, setContextView, setCurrentDiff, setCurrentRun, setCurrentSession, setSelectedFile]);

  const refreshSessions = useCallback(async () => {
    const list = await getSessions();
    setSessions(list);

    if (!didAutoOpenInitialSessionRef.current) {
      didAutoOpenInitialSessionRef.current = true;

      if (!currentSessionId && list[0]) {
        await openSession(list[0].id);
      }
    }
  }, [currentSessionId, openSession, setSessions]);

  const handleConfirmCreateSession = useCallback(async (selectedPath: string) => {
    eventSubscriptionRef.current?.();
    eventSubscriptionRef.current = null;

    try {
      const created = await createSession({ workspaceRoot: selectedPath });
      const detail = await getSession(created.id);
      setCurrentSession(detail);
      await refreshSessions();
    } catch (error) {
      console.error("Failed to create session:", error);
    }

    setCurrentRun(undefined);
    setCurrentDiff(undefined);
    setSelectedFile(undefined);
    setChanges([]);
    setContextView("overview");
    setComposerValue("");
  }, [refreshSessions, setChanges, setContextView, setCurrentDiff, setCurrentRun, setCurrentSession, setSelectedFile]);

  const workspaceDirectoryPicker = useDirectoryPicker({ onConfirm: handleConfirmCreateSession });
  const skillDirectoryPicker = useDirectoryPicker({
    initialPath: skillImportPath.trim() || configurationDraft?.skills.storagePath,
    onConfirm: (path) => setSkillImportPath(path),
  });

  const resolvedTheme = useMemo(() => {
    if (themeMode === "system") {
      return document.documentElement.classList.contains("dark") ? "dark" : "light";
    }

    return themeMode;
  }, [themeMode]);

  useEffect(() => {
    const stored = localStorage.getItem(leftPaneKey);
    if (stored === "true") {
      setLeftPaneCollapsed(true);
    }
  }, []);

  useEffect(() => {
    const mediaQuery = window.matchMedia(compactLayoutQuery);

    const syncLayout = () => {
      const compact = mediaQuery.matches;
      setIsCompactLayout(compact);

      if (compact) {
        setLeftPaneCollapsed(true);
        return;
      }

      const stored = localStorage.getItem(leftPaneKey);
      setLeftPaneCollapsed(stored === "true");
    };

    syncLayout();
    mediaQuery.addEventListener("change", syncLayout);

    return () => {
      mediaQuery.removeEventListener("change", syncLayout);
    };
  }, []);

  useEffect(() => {
    void refreshSessions();
  }, [refreshSessions]);

  useEffect(() => {
    void refreshConfiguration();
  }, [refreshConfiguration]);

  useEffect(() => {
    setSessionTitleInput(currentSession?.title ?? "");
  }, [currentSession?.id, currentSession?.title]);

  useEffect(() => () => {
    eventSubscriptionRef.current?.();
  }, []);

  const toggleLeftPaneCollapsed = useCallback(() => {
    setLeftPaneCollapsed((previous) => {
      const next = !previous;
      localStorage.setItem(leftPaneKey, next ? "true" : "false");
      return next;
    });
  }, []);

  const subscribeToRun = useCallback((run: AgentRun, sessionId: string) => {
    eventSubscriptionRef.current?.();
    setConnectionStatus("connecting");

    eventSubscriptionRef.current = subscribeRunEvents(sessionId, run.id, {
      onOpen: () => setConnectionStatus("connected"),
      onClose: () => setConnectionStatus("disconnected"),
      onError: () => setConnectionStatus("reconnecting"),
      onEvent: async (event) => {
        addLog(event);

        const nextPhase = mapPhase(event);
        if (nextPhase) {
          setPhase(nextPhase);
        }

        switch (event.type) {
          case "textMessageContent":
            appendAssistantDelta(sessionId, event.delta ?? "");
            break;
          case "textMessageEnd":
            finishAssistantMessage(sessionId, event.text ?? "");
            break;
          case "runFinished": {
            const updatedRun = await getRun(sessionId, run.id);
            setCurrentRun(updatedRun);
            setPhase("completed");
            setChanges(await getChanges(sessionId, run.id));
            await refreshSessions();

            if (currentSessionId === sessionId) {
              await openSession(sessionId);
            }

            break;
          }
          case "runError":
            setPhase("failed");
            await refreshSessions();
            break;
          case "runCancelled":
            setPhase("cancelled");
            await refreshSessions();
            break;
          case "file.created":
          case "file.modified":
          case "file.deleted":
          case "file.renamed":
          case "diff.generated":
            setContextView("files");

            try {
              setChanges(await getChanges(sessionId, run.id));
            } catch {
              return;
            }
            break;
          default:
            break;
        }
      },
    });
  }, [addLog, appendAssistantDelta, currentSessionId, openSession, refreshSessions, setChanges, setConnectionStatus, setContextView, setCurrentRun, setPhase, finishAssistantMessage]);

  const handleNewChat = useCallback(() => {
    void workspaceDirectoryPicker.openPicker();
  }, [workspaceDirectoryPicker]);

  const handleSubmit = useCallback(async () => {
    const task = composerValue.trim();
    if (!task || isSubmitting) {
      return;
    }

    setIsSubmitting(true);

    try {
      let activeSession = currentSession;

      if (!activeSession) {
        activeSession = createDraftSession(task);
        setCurrentSession(activeSession);
      }

      if (activeSession.id.startsWith("draft_")) {
        const created = await createSession({
          initialMessage: task,
          providerName: currentSession?.providerName,
          model: currentSession?.model,
          workspaceRoot: currentSession?.workspaceRoot,
        });
        const detail = await getSession(created.id);
        setCurrentSession(detail);
        activeSession = detail;
        await refreshSessions();
      }

      if (!activeSession) {
        return;
      }

      addUserMessage(activeSession.id, task);

      const run = await createRun({
        sessionId: activeSession.id,
        task,
        workingDirectory: activeSession.workspaceRoot,
      });
      setCurrentRun(run);
      setChanges([]);
      setCurrentDiff(undefined);
      setSelectedFile(undefined);
      setPhase("understanding");
      setConnectionStatus("connecting");
      subscribeToRun(run, activeSession.id);
      setComposerValue("");
    } finally {
      setIsSubmitting(false);
    }
  }, [addUserMessage, composerValue, currentSession, isSubmitting, refreshSessions, setChanges, setConnectionStatus, setCurrentDiff, setCurrentRun, setCurrentSession, setPhase, setSelectedFile]);

  const handleCancel = useCallback(async () => {
    if (currentRun) {
      const sessionId = currentRun.sessionId ?? currentSessionId;
      if (!sessionId) {
        return;
      }

      setDetailsError(undefined);
      try {
        await cancelRun(sessionId, currentRun.id);
      } catch (error) {
        setDetailsError(formatError(error));
      } finally {
        const latestRun = await getRun(sessionId, currentRun.id);
        setCurrentRun(latestRun);
        setPhase(mapRunStatusToPhase(latestRun.status));
      }
    }
  }, [currentRun, currentSessionId, setCurrentRun, setPhase]);

  const openDiff = useCallback(async (change: FileChange) => {
    if (!currentRun) {
      return;
    }

    const sessionId = currentRun.sessionId ?? currentSessionId;
    if (!sessionId) {
      return;
    }

    setSelectedFile(change.path);
    setContextView("diff");
    setCurrentDiff(await getFileDiff(sessionId, currentRun.id, change.path));
  }, [currentRun, currentSessionId, setContextView, setCurrentDiff, setSelectedFile]);

  const handleViewFiles = useCallback(() => {
    setContextView("files");
  }, [setContextView]);

  const handleDeleteCurrentSession = useCallback(async () => {
    if (!currentSession || currentSession.id.startsWith("draft_")) {
      return;
    }

    if (!window.confirm(`Delete conversation "${currentSession.title}"?`)) {
      return;
    }

    try {
      await deleteSession(currentSession.id);
      setCurrentSession(undefined);
      setCurrentRun(undefined);
      setCurrentDiff(undefined);
      setSelectedFile(undefined);
      setChanges([]);
      setDetailsMessage("Conversation deleted.");
      setDetailsError(undefined);
      await refreshSessions();
    } catch (error) {
      setDetailsError(formatError(error));
    }
  }, [currentSession, refreshSessions, setChanges, setCurrentDiff, setCurrentRun, setCurrentSession, setSelectedFile]);

  const handleDeleteSession = useCallback(async (sessionId: string) => {
    try {
      await deleteSession(sessionId);
      if (currentSession?.id === sessionId) {
        setCurrentSession(undefined);
        setCurrentRun(undefined);
        setCurrentDiff(undefined);
        setSelectedFile(undefined);
        setChanges([]);
        setDetailsMessage("Conversation deleted.");
        setDetailsError(undefined);
      }
      await refreshSessions();
    } catch (error) {
      setDetailsError(formatError(error));
    }
  }, [currentSession, refreshSessions, setChanges, setCurrentDiff, setCurrentRun, setCurrentSession, setSelectedFile]);

  const handleRenameSession = useCallback(async (sessionId: string, newTitle: string) => {
    try {
      const updated = await updateSession({ sessionId, title: newTitle });
      if (currentSession?.id === sessionId) {
        setCurrentSession({ ...currentSession, ...updated });
      }
      await refreshSessions();
    } catch (error) {
      setDetailsError(formatError(error));
    }
  }, [currentSession, refreshSessions, setCurrentSession]);

  const handleSessionTitleSave = useCallback(async () => {
    const title = sessionTitleInput.trim();
    if (!currentSession || currentSession.id.startsWith("draft_") || !title) {
      return;
    }

    setIsUpdatingSession(true);
    setDetailsMessage(undefined);
    setDetailsError(undefined);

    try {
      const updated = await updateSession({ sessionId: currentSession.id, title });
      setCurrentSession({ ...currentSession, ...updated });
      setDetailsMessage("Conversation title updated.");
      await refreshSessions();
    } catch (error) {
      setDetailsError(formatError(error));
    } finally {
      setIsUpdatingSession(false);
    }
  }, [currentSession, refreshSessions, sessionTitleInput, setCurrentSession]);

  const handleSessionProviderChange = useCallback(async (providerName: string) => {
    if (!currentSession || currentSession.id.startsWith("draft_")) {
      return;
    }

    setIsUpdatingSession(true);
    setDetailsMessage(undefined);
    setDetailsError(undefined);

    try {
      const selection = await updateSessionModel({
        sessionId: currentSession.id,
        providerName: providerName || null,
        modelId: "",
      });

      setCurrentSession({
        ...currentSession,
        providerName: selection.providerName,
        providerType: selection.providerType,
        model: selection.modelId,
      });
      setDetailsMessage("Conversation provider updated.");
      await openSession(currentSession.id);
      await refreshSessions();
    } catch (error) {
      setDetailsError(formatError(error));
    } finally {
      setIsUpdatingSession(false);
    }
  }, [currentSession, openSession, refreshSessions, setCurrentSession]);

  const handleSessionModelChange = useCallback(async (modelId: string) => {
    if (!currentSession || currentSession.id.startsWith("draft_")) {
      return;
    }

    setIsUpdatingSession(true);
    setDetailsMessage(undefined);
    setDetailsError(undefined);

    try {
      const selection = await updateSessionModel({
        sessionId: currentSession.id,
        providerName: currentSession.providerName ?? null,
        modelId,
      });

      setCurrentSession({
        ...currentSession,
        providerName: selection.providerName,
        providerType: selection.providerType,
        model: selection.modelId,
      });
      setDetailsMessage("Conversation model updated.");
      await openSession(currentSession.id);
      await refreshSessions();
    } catch (error) {
      setDetailsError(formatError(error));
    } finally {
      setIsUpdatingSession(false);
    }
  }, [currentSession, openSession, refreshSessions, setCurrentSession]);

  const handleConfigurationFieldChange = useCallback((index: number, field: keyof ProviderEntry, value: string) => {
    setConfigurationDraft((previous) => {
      if (!previous) {
        return previous;
      }

      return {
        ...previous,
        providers: previous.providers.map((provider, providerIndex) => {
          if (providerIndex !== index) {
            return provider;
          }

          return field === "name" ? { ...provider, name: value } : provider;
        }),
      };
    });

    setProviderTestResults((previous) => {
      const next = { ...previous };
      delete next[index];
      return next;
    });
  }, []);

  const handleProviderConfigChange = useCallback((index: number, field: keyof ProviderEntry["provider"], value: string | boolean) => {
    setConfigurationDraft((previous) => {
      if (!previous) {
        return previous;
      }

      return {
        ...previous,
        providers: previous.providers.map((provider, providerIndex) => {
          if (providerIndex !== index) {
            return provider;
          }

          if (field === "models" && typeof value === "string") {
            return {
              ...provider,
              provider: {
                ...provider.provider,
                models: value.split("\n").map((item) => item.trim()).filter(Boolean),
              },
            };
          }

          if (field === "type" && typeof value === "string") {
            const nextType = normalizeOptionalText(value) as ProviderType | undefined;

            return {
              ...provider,
              provider: {
                ...provider.provider,
                type: nextType,
                endpoint: isProviderFieldVisible(nextType, "endpoint") ? provider.provider.endpoint : undefined,
                deployment: isProviderFieldVisible(nextType, "deployment") ? provider.provider.deployment : undefined,
                useDefaultAzureCredential: isProviderFieldVisible(nextType, "useDefaultAzureCredential")
                  ? provider.provider.useDefaultAzureCredential
                  : false,
              },
            };
          }

          return {
            ...provider,
            provider: {
              ...provider.provider,
              [field]: typeof value === "string" ? normalizeOptionalText(value) : value,
            },
          };
        }),
      };
    });

    setProviderTestResults((previous) => {
      const next = { ...previous };
      delete next[index];
      return next;
    });
  }, []);

  const handleAddProvider = useCallback(() => {
    setConfigurationDraft((previous) => previous ? { ...previous, providers: [...previous.providers, createProviderDraft()] } : previous);
  }, []);

  const handleRemoveProvider = useCallback((index: number) => {
    setConfigurationDraft((previous) => {
      if (!previous) {
        return previous;
      }

      const removed = previous.providers[index];
      return {
        ...previous,
        currentDefaultProvider: previous.currentDefaultProvider === removed?.name ? undefined : previous.currentDefaultProvider,
        providers: previous.providers.filter((_, providerIndex) => providerIndex !== index),
      };
    });

    setProviderTestResults((previous) => {
      const next = Object.entries(previous).reduce<Record<number, string>>((result, [key, value]) => {
        const currentIndex = Number(key);
        if (currentIndex < index) {
          result[currentIndex] = value;
        } else if (currentIndex > index) {
          result[currentIndex - 1] = value;
        }

        return result;
      }, {});

      return next;
    });

    setTestingProviderIndexes((previous) => {
      const next = Object.entries(previous).reduce<Record<number, boolean>>((result, [key, value]) => {
        const currentIndex = Number(key);
        if (currentIndex < index) {
          result[currentIndex] = value;
        } else if (currentIndex > index) {
          result[currentIndex - 1] = value;
        }

        return result;
      }, {});

      return next;
    });
  }, []);

  const handleTestProvider = useCallback(async (index: number) => {
    const entry = configurationDraft?.providers[index];
    if (!entry) {
      return;
    }

    if (!entry.name.trim()) {
      setProviderTestResults((previous) => ({ ...previous, [index]: "Provider name is required before testing." }));
      return;
    }

    if (!entry.provider.type) {
      setProviderTestResults((previous) => ({ ...previous, [index]: "Provider type is required before testing." }));
      return;
    }

    setTestingProviderIndexes((previous) => ({ ...previous, [index]: true }));
    setProviderTestResults((previous) => ({ ...previous, [index]: "Testing..." }));

    try {
      const result = await testProvider(entry.name.trim(), entry.provider);
      setProviderTestResults((previous) => ({
        ...previous,
        [index]: result.success
          ? `${result.message}${result.models.length ? ` Models: ${result.models.join(", ")}` : ""}`
          : result.message,
      }));
    } catch (error) {
      setProviderTestResults((previous) => ({ ...previous, [index]: formatError(error) }));
    } finally {
      setTestingProviderIndexes((previous) => ({ ...previous, [index]: false }));
    }
  }, [configurationDraft?.providers]);

  const handleMcpServerChange = useCallback((index: number, field: keyof McpServerConfig, value: string | boolean) => {
    setConfigurationDraft((previous) => {
      if (!previous) {
        return previous;
      }

      return {
        ...previous,
        mcpServers: previous.mcpServers.map((server, serverIndex) => {
          if (serverIndex !== index) {
            return server;
          }

          if (field === "arguments" && typeof value === "string") {
            return { ...server, arguments: value.split("\n").map((item) => item.trim()).filter(Boolean) };
          }

          if (field === "environmentVariables" && typeof value === "string") {
            const environmentVariables = value
              .split("\n")
              .map((line) => line.trim())
              .filter(Boolean)
              .reduce<Record<string, string>>((result, line) => {
                const separatorIndex = line.indexOf("=");
                if (separatorIndex > 0) {
                  const key = line.slice(0, separatorIndex).trim();
                  const envValue = line.slice(separatorIndex + 1).trim();
                  if (key) {
                    result[key] = envValue;
                  }
                }

                return result;
              }, {});

            return { ...server, environmentVariables };
          }

          return { ...server, [field]: typeof value === "string" ? normalizeOptionalText(value) : value };
        }),
      };
    });
  }, []);

  const handleAddMcpServer = useCallback(() => {
    setConfigurationDraft((previous) => previous
      ? {
          ...previous,
          mcpServers: [...previous.mcpServers, { name: undefined, command: undefined, arguments: [], url: undefined, environmentVariables: {}, enabled: true }],
        }
      : previous);
  }, []);

  const handleRemoveMcpServer = useCallback((index: number) => {
    setConfigurationDraft((previous) => previous
      ? { ...previous, mcpServers: previous.mcpServers.filter((_, serverIndex) => serverIndex !== index) }
      : previous);

    setMcpTestResults((previous) => {
      const next = { ...previous };
      delete next[index];
      return next;
    });
  }, []);

  const handleTestMcpServer = useCallback(async (index: number) => {
    const server = configurationDraft?.mcpServers[index];
    if (!server) {
      return;
    }

    setMcpTestResults((previous) => ({ ...previous, [index]: "Testing..." }));

    try {
      const result = await testMcp(server);
      setMcpTestResults((previous) => ({
        ...previous,
        [index]: result.success ? `${result.message}${result.tools.length ? ` Tools: ${result.tools.join(", ")}` : ""}` : result.message,
      }));
    } catch (error) {
      setMcpTestResults((previous) => ({ ...previous, [index]: formatError(error) }));
    }
  }, [configurationDraft?.mcpServers]);

  const handleImportSkill = useCallback(async () => {
    const path = skillImportPath.trim();
    if (!path) {
      return;
    }

    setIsImportingSkill(true);
    setGlobalSettingsError(undefined);
    setSessionSettingsError(undefined);

    try {
      const result: SkillImportResult = await importSkillToTarget({
        path,
        target: skillImportTarget,
        sessionId: skillImportTarget === "session" ? currentSession?.id : undefined,
      });
      if (!result.success) {
        const message = result.message || result.errors.join(", ");
        if (skillImportTarget === "session") {
          setSessionSettingsError(message);
        } else {
          setGlobalSettingsError(message);
        }
        return;
      }

      setSkillImportPath("");
      if (skillImportTarget === "session") {
        setSessionSettingsMessage(result.message || "Session skill imported.");
        if (currentSession?.id) {
          await openSession(currentSession.id);
        }
      } else {
        setGlobalSettingsMessage(result.message || "Skill imported.");
      }

      await refreshConfiguration();
    } catch (error) {
      const message = formatError(error);
      if (skillImportTarget === "session") {
        setSessionSettingsError(message);
      } else {
        setGlobalSettingsError(message);
      }
    } finally {
      setIsImportingSkill(false);
    }
  }, [currentSession?.id, openSession, refreshConfiguration, skillImportPath, skillImportTarget]);

  const handleRemoveGlobalSkill = useCallback(async (skillName: string) => {
    try {
      const result = await removeGlobalSkill(skillName);
      setGlobalSettingsMessage(result.message || "Global skill removed.");
      await refreshConfiguration();
    } catch (error) {
      setGlobalSettingsError(formatError(error));
    }
  }, [refreshConfiguration]);

  const handleRemoveSessionSkill = useCallback(async (skillName: string) => {
    if (!currentSession?.id) {
      return;
    }

    try {
      const result = await removeSessionSkill(currentSession.id, skillName);
      setSessionSettingsMessage(result.message || "Session skill removed.");
      await openSession(currentSession.id);
    } catch (error) {
      setSessionSettingsError(formatError(error));
    }
  }, [currentSession?.id, openSession]);

  const handleSaveConfiguration = useCallback(async () => {
    if (!configurationDraft) {
      return;
    }

    setIsSavingConfiguration(true);
    setConfigurationError(undefined);
    setGlobalSettingsError(undefined);

    try {
      const saved = await saveConfiguration({
        currentDefaultProvider: configurationDraft.currentDefaultProvider,
        providers: configurationDraft.providers.map((provider) => ({
          name: provider.name.trim(),
          provider: {
            ...provider.provider,
            type: provider.provider.type ?? "openai",
            model: provider.provider.model,
            endpoint: provider.provider.endpoint,
            apiKey: provider.provider.apiKey,
            deployment: provider.provider.deployment,
          },
        })),
        mcpServers: configurationDraft.mcpServers,
        skills: configurationDraft.skills,
      });

      setConfiguration(saved);
      setConfigurationDraft(cloneConfiguration(saved));
      setProviderTestResults({});
      setTestingProviderIndexes({});
      setGlobalSettingsMessage("Configuration saved.");
      if (currentSession?.id && !currentSession.id.startsWith("draft_")) {
        await openSession(currentSession.id);
      }
      await refreshSessions();
    } catch (error) {
      const message = formatError(error);
      setConfigurationError(message);
      setGlobalSettingsError(message);
    } finally {
      setIsSavingConfiguration(false);
    }
  }, [configurationDraft, currentSession?.id, openSession, refreshSessions]);

  return {
    sessions,
    currentSession,
    currentRun,
    phase,
    connectionStatus,
    contextView,
    selectedFile,
    changes,
    currentDiff,
    logs,
    composerValue,
    isSubmitting,
    leftPaneCollapsed,
    isCompactLayout,
    configuration,
    configurationDraft,
    isLoadingConfiguration,
    isSavingConfiguration,
    configurationError,
    detailsMessage,
    detailsError,
    sessionSettingsMessage,
    sessionSettingsError,
    globalSettingsTab,
    globalSettingsMessage,
    globalSettingsError,
    mcpTestResults,
    providerTestResults,
    skillImportPath,
    isImportingSkill,
    skillImportTarget,
    isUpdatingSession,
    testingProviderIndexes,
    sessionTitleInput,
    showGlobalSettingsDialog,
    selectedProviderName,
    selectedProvider,
    availableModels,
    sessionModelWarning,
    selectedProviderDefaultModel,
    resolvedTheme,
    workspaceDirectoryPicker,
    skillDirectoryPicker,
    setComposerValue,
    setContextView,
    setConfigurationDraft,
    setGlobalSettingsTab,
    setSkillImportPath,
    setSkillImportTarget,
    setSessionTitleInput,
    openSettings: () => {
      setGlobalSettingsMessage(undefined);
      setGlobalSettingsError(undefined);
      setSkillImportTarget("global");
      setShowGlobalSettingsDialog(true);
    },
    closeSettings: () => setShowGlobalSettingsDialog(false),
    toggleLeftPaneCollapsed,
    openSession,
    handleNewChat,
    handleSubmit,
    handleCancel,
    openDiff,
    handleViewFiles,
    handleDeleteCurrentSession,
    handleSessionTitleSave,
    handleDeleteSession,
    handleRenameSession,
    handleSessionProviderChange,
    handleSessionModelChange,
    handleConfigurationFieldChange,
    handleProviderConfigChange,
    handleAddProvider,
    handleRemoveProvider,
    handleTestProvider,
    handleMcpServerChange,
    handleAddMcpServer,
    handleRemoveMcpServer,
    handleTestMcpServer,
    handleImportSkill,
    handleRemoveGlobalSkill,
    handleRemoveSessionSkill,
    handleSaveConfiguration,
    refreshConfiguration,
  };
}
