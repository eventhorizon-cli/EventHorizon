using EventHorizon.Configuration;
using EventHorizon.Conversations;
using EventHorizon.Diagnostics;
using EventHorizon.Pricing;
using EventHorizon.Terminal;
using EventHorizon.Terminal.Commands;
using EventHorizon.Terminal.Session;
using Microsoft.Agents.AI;

namespace EventHorizon.Tests.Terminal;

public sealed class TerminalCommandTests : IDisposable
{
    private readonly string _root;

    public TerminalCommandTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "eventhorizon-command-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(Path.Combine(_root, "src"));
        File.WriteAllText(Path.Combine(_root, "src", "Program.cs"), "class Program {}\n");
    }

    [Fact]
    public void Parse_Splits_Command_Name_And_Argument()
    {
        TerminalCommand command = TerminalCommand.Parse("/focus src/Program.cs");

        Assert.True(command.IsSlashCommand);
        Assert.Equal("/focus", command.Name);
        Assert.Equal("src/Program.cs", command.Argument);
    }

    [Fact]
    public async Task Focus_Command_Updates_Runtime_Focus_Path()
    {
        TerminalRuntimeContext runtime = new(
            new AppOptions { WorkspaceRoot = _root },
            new SessionUsageTracker(new ModelPriceCatalog([]), "missing"),
            new FakeTerminalSessionService(),
            new FakeErrorLogWriter());
        FocusCommandHandler handler = new();
        TerminalCommandContext context = new(runtime, TerminalCommand.Parse("/focus src/Program.cs"), CancellationToken.None);

        TerminalCommandResult result = await handler.ExecuteAsync(context);

        Assert.True(result.Handled);
        Assert.Equal("src/Program.cs", runtime.State.FocusedPath);
    }

    [Fact]
    public async Task Sidebar_Command_Switches_Sidebar_Mode()
    {
        TerminalRuntimeContext runtime = new(
            new AppOptions { WorkspaceRoot = _root },
            new SessionUsageTracker(new ModelPriceCatalog([]), "missing"),
            new FakeTerminalSessionService(),
            new FakeErrorLogWriter());
        SidebarCommandHandler handler = new();
        TerminalCommandContext context = new(runtime, TerminalCommand.Parse("/sidebar sessions"), CancellationToken.None);

        TerminalCommandResult result = await handler.ExecuteAsync(context);

        Assert.True(result.Handled);
        Assert.Equal(TerminalSidebarModeCatalog.Sessions, runtime.State.SidebarMode);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class FakeTerminalSessionService : ITerminalSessionService
    {
        public AgentSession? CurrentSession => null;

        public Task<AgentSession> EnsureSessionAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<ConversationSessionSummary>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ConversationSessionSummary>>([]);

        public Task<AgentSession> ResetAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<TerminalSessionRestoreResult> RestoreAsync(string sessionId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task SaveAsync(string sessionName, TerminalConversationState state, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class FakeErrorLogWriter : IRunErrorLogWriter
    {
        public string LogFilePath => string.Empty;

        public void Write(string category, Exception exception, IReadOnlyDictionary<string, string?>? metadata = null)
        {
        }
    }
}

