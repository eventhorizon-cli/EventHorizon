import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels";
import { ConversationPane } from "@/components/chat/ConversationPane";
import { ContextPanel } from "@/components/context/ContextPanel";
import { DirectoryPickerDialog } from "@/components/dialogs/DirectoryPickerDialog";
import { SessionsSidebar } from "@/components/layout/SessionsSidebar";
import { WorkbenchHeader } from "@/components/layout/WorkbenchHeader";
import { GlobalSettingsDialog } from "@/components/settings/GlobalSettingsDialog";
import { useWorkbenchApp } from "@/hooks/useWorkbenchApp";

export default function App() {
  const app = useWorkbenchApp();

  return (
    <div className="flex h-screen min-h-0 flex-col overflow-hidden bg-muted/40 p-3 text-foreground">
      <WorkbenchHeader currentRun={app.currentRun} phase={app.phase} onOpenSettings={app.openSettings} />

      <div className="flex min-h-0 flex-1 gap-3 overflow-hidden">
        <SessionsSidebar
          sessions={app.sessions}
          currentSessionId={app.currentSession?.id}
          leftPaneCollapsed={app.leftPaneCollapsed}
          isCompactLayout={app.isCompactLayout}
          onToggleCollapsed={app.toggleLeftPaneCollapsed}
          onNewChat={app.handleNewChat}
          onOpenSession={(sessionId) => void app.openSession(sessionId)}
        />

        <PanelGroup direction="horizontal" className="min-h-0 flex-1 overflow-hidden">
          <Panel defaultSize={64} minSize={45} className="min-h-0 min-w-0">
            <ConversationPane
              currentSession={app.currentSession}
              currentRun={app.currentRun}
              currentDefaultProvider={app.configuration?.currentDefaultProvider}
              availableModels={app.availableModels}
              phase={app.phase}
              logsCount={app.logs.length}
              changes={app.changes}
              composerValue={app.composerValue}
              isSubmitting={app.isSubmitting}
              isUpdatingConversation={app.isUpdatingConversation}
              onComposerChange={app.setComposerValue}
              onComposerSubmit={app.handleSubmit}
              onCancelRun={app.handleCancel}
              onSelectModel={app.handleConversationModelChange}
              onViewFiles={app.handleViewFiles}
              onViewLogs={() => app.setContextView("logs")}
              onOpenDiff={app.openDiff}
            />
          </Panel>

          <PanelResizeHandle className="flex w-3 items-center justify-center bg-transparent">
            <div className="h-12 w-1 rounded-full bg-border transition hover:bg-primary/40" />
          </PanelResizeHandle>

          <Panel defaultSize={36} minSize={26} className="min-h-0 min-w-0">
            <ContextPanel
              contextView={app.contextView}
              currentRun={app.currentRun}
              currentSession={app.currentSession}
              configuration={app.configuration}
              configurationDraft={app.configurationDraft}
              detailsMessage={app.detailsMessage}
              detailsError={app.detailsError}
              isUpdatingConversation={app.isUpdatingConversation}
              sessionTitleInput={app.sessionTitleInput}
              selectedProviderName={app.selectedProviderName}
              selectedProviderType={app.selectedProvider?.provider.type}
              availableModels={app.availableModels}
              changes={app.changes}
              selectedFile={app.selectedFile}
              currentDiff={app.currentDiff}
              logs={app.logs}
              phase={app.phase}
              resolvedTheme={app.resolvedTheme === "dark" ? "dark" : "light"}
              onContextViewChange={app.setContextView}
              onSessionTitleInputChange={app.setSessionTitleInput}
              onSaveConversationTitle={app.handleConversationTitleSave}
              onDeleteConversation={app.handleDeleteCurrentConversation}
              onChangeConversationProvider={app.handleConversationProviderChange}
              onChangeConversationModel={app.handleConversationModelChange}
              onOpenDiff={app.openDiff}
            />
          </Panel>
        </PanelGroup>
      </div>

      <DirectoryPickerDialog
        open={app.workspaceDirectoryPicker.open}
        title="Select Workspace Directory"
        confirmLabel="Create Session"
        currentPath={app.workspaceDirectoryPicker.currentPath}
        selectedPath={app.workspaceDirectoryPicker.selectedPath}
        pathInput={app.workspaceDirectoryPicker.pathInput}
        directories={app.workspaceDirectoryPicker.directories}
        isLoading={app.workspaceDirectoryPicker.isLoading}
        onCancel={app.workspaceDirectoryPicker.closePicker}
        onConfirm={() => void app.workspaceDirectoryPicker.confirmSelection()}
        onPathInputChange={app.workspaceDirectoryPicker.setPathInput}
        onPathInputSubmit={() => void app.workspaceDirectoryPicker.submitPathInput()}
        onSelectPath={app.workspaceDirectoryPicker.selectPath}
        onDoubleClickPath={(item) => void app.workspaceDirectoryPicker.navigateToPath(item)}
      />

      <GlobalSettingsDialog
        open={app.showGlobalSettingsDialog}
        configuration={app.configuration}
        configurationDraft={app.configurationDraft}
        configurationError={app.configurationError}
        globalSettingsTab={app.globalSettingsTab}
        globalSettingsMessage={app.globalSettingsMessage}
        globalSettingsError={app.globalSettingsError}
        isLoadingConfiguration={app.isLoadingConfiguration}
        isSavingConfiguration={app.isSavingConfiguration}
        isImportingSkill={app.isImportingSkill}
        skillImportPath={app.skillImportPath}
        mcpTestResults={app.mcpTestResults}
        skillDirectoryPickerOpen={app.skillDirectoryPicker.open}
        skillDirectories={app.skillDirectoryPicker.directories}
        skillCurrentPath={app.skillDirectoryPicker.currentPath}
        skillSelectedPath={app.skillDirectoryPicker.selectedPath}
        skillPathInput={app.skillDirectoryPicker.pathInput}
        isLoadingSkillDirectories={app.skillDirectoryPicker.isLoading}
        onClose={app.closeSettings}
        onTabChange={app.setGlobalSettingsTab}
        onRefreshConfiguration={app.refreshConfiguration}
        onSaveConfiguration={app.handleSaveConfiguration}
        onConfigurationDraftChange={app.setConfigurationDraft}
        onAddProvider={app.handleAddProvider}
        onRemoveProvider={app.handleRemoveProvider}
        onConfigurationFieldChange={app.handleConfigurationFieldChange}
        onProviderConfigChange={app.handleProviderConfigChange}
        onAddMcpServer={app.handleAddMcpServer}
        onRemoveMcpServer={app.handleRemoveMcpServer}
        onMcpServerChange={app.handleMcpServerChange}
        onTestMcpServer={app.handleTestMcpServer}
        onSkillImportPathChange={app.setSkillImportPath}
        onOpenSkillDirectoryPicker={() => app.skillDirectoryPicker.openPicker(app.skillImportPath.trim() || app.configurationDraft?.skills.storagePath)}
        onImportSkill={app.handleImportSkill}
        onCloseSkillDirectoryPicker={app.skillDirectoryPicker.closePicker}
        onSkillPathInputChange={app.skillDirectoryPicker.setPathInput}
        onSkillPathInputSubmit={app.skillDirectoryPicker.submitPathInput}
        onSelectSkillPath={app.skillDirectoryPicker.selectPath}
        onNavigateSkillPath={app.skillDirectoryPicker.navigateToPath}
        onConfirmSkillPath={app.skillDirectoryPicker.confirmSelection}
      />
    </div>
  );
}
