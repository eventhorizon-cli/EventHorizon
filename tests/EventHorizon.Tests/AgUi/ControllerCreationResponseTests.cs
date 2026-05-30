using EventHorizon.AGUI;
using EventHorizon.AGUI.Controllers;
using EventHorizon.AGUI.DTOs;
using EventHorizon.Configuration;
using EventHorizon.Context;
using EventHorizon.Conversations;
using EventHorizon.Diff;
using EventHorizon.Pricing;
using EventHorizon.Providers;
using EventHorizon.Tools;
using EventHorizon.Workspace;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.Tests.AGUI;

public sealed class ControllerCreationResponseTests
{
    [Fact]
    public async Task Conversations_CreateAsync_Returns_Created_With_Location()
    {
        using var workspace = new TemporaryWorkspace();
        var controller = new ConversationsController(
            new InMemorySessionService(workspace.Root),
            new StubConversationModelService(),
            new WorkspaceContext(workspace.Root));

        var result = await controller.CreateAsync(new CreateAGUISessionRequestDTO
        {
            InitialMessage = "hello",
            WorkspaceRoot = workspace.Root,
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result.Result);
        var payload = Assert.IsType<AGUISessionSummaryDTO>(created.Value);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal($"/api/conversations/{payload.Id}", created.Location);
    }

    [Fact]
    public async Task Runs_CreateAsync_Returns_Created_With_Location()
    {
        using var workspace = new TemporaryWorkspace();
        var runService = CreateRunService(workspace.Root);
        var controller = new RunsController(runService);

        var result = await controller.CreateAsync(new CreateAGUIRunRequestDTO
        {
            Task = "say ok",
            WorkingDirectory = workspace.Root,
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result.Result);
        var payload = Assert.IsType<AGUIRunDTO>(created.Value);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal($"/api/runs/{payload.Id}", created.Location);
    }

    private static RunService CreateRunService(string workspaceRoot)
        => new(
            new RunStore(),
            new FakeRuntime(),
            new FakeModelPriceCatalogService(),
            new FileSnapshotService(workspaceRoot),
            new FileStateTrackerAccessor(),
            new DiffService(),
            new InMemorySessionService(workspaceRoot),
            new AGUIEventMapper(),
            new AGUICodeAgentEventMapper(),
            new FakeProviderResolutionService(),
            new FakeProviderAgentFactory(),
            new FakeConversationAgentManager(),
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<RunService>());

    private sealed class InMemorySessionService(string workspaceRoot) : IAGUISessionService
    {
        public Task<IReadOnlyList<AGUISessionSummaryDTO>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<AGUISessionSummaryDTO>>([]);

        public Task<AGUISessionDetailDTO?> GetAsync(string sessionId, CancellationToken cancellationToken)
            => Task.FromResult<AGUISessionDetailDTO?>(null);

        public Task<ConversationSessionDocument?> GetDocumentAsync(string sessionId, CancellationToken cancellationToken)
            => Task.FromResult<ConversationSessionDocument?>(null);

        public Task<AGUISessionSummaryDTO> CreateAsync(CreateAGUISessionRequestDTO request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(new AGUISessionSummaryDTO(
                Guid.NewGuid().ToString("N"),
                request.InitialMessage ?? "New conversation",
                AGUIRunStates.Idle,
                now,
                now,
                null,
                null,
                null,
                null,
                null,
                0,
                false,
                request.WorkspaceRoot ?? workspaceRoot));
        }

        public Task<AGUISessionSummaryDTO?> UpdateAsync(string sessionId, UpdateAGUISessionRequestDTO request, CancellationToken cancellationToken)
            => Task.FromResult<AGUISessionSummaryDTO?>(null);

        public Task<bool> DeleteAsync(string sessionId, CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task<ConversationSessionDocument?> StartRunAsync(
            string sessionId,
            string runId,
            string task,
            string? providerName,
            string? model,
            CancellationToken cancellationToken)
            => Task.FromResult<ConversationSessionDocument?>(null);

        public Task RecordRunCompletedAsync(string sessionId, string? assistantMessage, int changedFilesCount, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task RecordRunFailedAsync(string sessionId, string error, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task RecordRunCancelledAsync(string sessionId, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task GenerateTitleIfNeededAsync(string sessionId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class StubConversationModelService : IConversationModelService
    {
        public Task<ConversationModelUpdateResult?> UpdateAsync(string conversationId, string? providerName, string? modelId, CancellationToken cancellationToken)
            => Task.FromResult<ConversationModelUpdateResult?>(null);
    }

    private sealed class FakeRuntime : IEventHorizonRuntime
    {
        public AIAgent Agent => throw new NotSupportedException();

        public string ModelName => "fake-model";

        public string Instructions => string.Empty;

        public IServiceProvider Services => new ServiceCollection().BuildServiceProvider();

        public SessionContextSnapshot ContextSnapshot => new("today", Directory.GetCurrentDirectory(), string.Empty, string.Empty, string.Empty);

        public IReadOnlyList<ToolDescriptor> ToolCatalog => [];

        public IReadOnlyList<AITool> Tools => [];

        public AgentSkillsProvider? SkillsProvider => null;

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }

    private sealed class FakeModelPriceCatalogService : IModelPriceCatalogService
    {
        public bool TryGetCatalog(out ModelPriceCatalog? catalog)
        {
            catalog = null;
            return false;
        }

        public Task RefreshIfNeededAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeProviderResolutionService : IProviderResolutionService
    {
        public IReadOnlyList<ProviderOptions> GetProviderOptions()
            => [];

        public ResolvedProviderContext? TryResolveForSession(ConversationSessionDocument? session, ChatRequestOverrides? overrides = null)
            => new(
                "openai",
                "openai-compatible",
                "gpt-5-mini",
                new ProviderOptions
                {
                    Name = "openai",
                    Type = "openai-compatible",
                    Model = "gpt-5-mini",
                    Models = ["gpt-5-mini"],
                },
                overrides ?? ChatRequestOverrides.Empty);

        public ResolvedProviderContext? TryResolveDefault()
            => TryResolveForSession(null, ChatRequestOverrides.Empty);
    }

    private sealed class FakeProviderAgentFactory : IProviderAgentFactory
    {
        public AIAgent CreateAgent(global::EventHorizon.Configuration.AppOptions options, string instructions, IReadOnlyList<AITool> tools, AgentSkillsProvider? skillsProvider, IServiceProvider services)
            => throw new NotSupportedException();
    }

    private sealed class FakeConversationAgentManager : IConversationAgentManager
    {
        public Task<ConversationAgentRuntime> GetOrCreateAsync(ConversationSessionDocument document, ChatRequestOverrides? overrides, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<ConversationAgentRuntime> RebuildAsync(ConversationSessionDocument document, ChatRequestOverrides? overrides, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task InvalidateAsync(string sessionId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task InvalidateAllAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void MarkTranscriptCount(string sessionId, int transcriptCount)
        {
        }
    }

    private sealed class TemporaryWorkspace : IDisposable
    {
        public TemporaryWorkspace()
        {
            Root = Path.Combine(Path.GetTempPath(), "eventhorizon-controller-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
