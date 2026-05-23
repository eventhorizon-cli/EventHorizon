using EventHorizon.Execution;
using EventHorizon.Providers;

namespace EventHorizon.Commands;

public sealed record SlashCommandDefinition(string Name, string Description);

public readonly record struct SlashCommandResult(bool Handled, bool ExitRequested)
{
    public static SlashCommandResult NotHandled => new(false, false);
    public static SlashCommandResult Continue => new(true, false);
    public static SlashCommandResult Exit => new(true, true);
}

public interface ISlashCommandService
{
    IReadOnlyList<SlashCommandDefinition> GetDefinitions();

    Task<SlashCommandResult> TryExecuteAsync(
        string input,
        IEventHorizonRuntime runtime,
        QueryEngine queryEngine,
        CancellationToken cancellationToken);
}

public sealed class SlashCommandCatalog : ISlashCommandService
{
    private sealed record SlashCommand(
        SlashCommandDefinition Definition,
        Func<IEventHorizonRuntime, QueryEngine, CancellationToken, Task<SlashCommandResult>> ExecuteAsync);

    private static readonly IReadOnlyDictionary<string, SlashCommand> Commands = CreateCommands();

    public IReadOnlyList<SlashCommandDefinition> GetDefinitions()
        => Commands.Values
            .Select(static command => command.Definition)
            .OrderBy(static definition => definition.Name, StringComparer.Ordinal)
            .ToArray();

    public async Task<SlashCommandResult> TryExecuteAsync(
        string input,
        IEventHorizonRuntime runtime,
        QueryEngine queryEngine,
        CancellationToken cancellationToken)
    {
        if (!input.StartsWith("/", StringComparison.Ordinal))
        {
            return SlashCommandResult.NotHandled;
        }

        if (!Commands.TryGetValue(input.Trim(), out SlashCommand? command))
        {
            System.Console.WriteLine("Unknown slash command. Use /help.");
            return SlashCommandResult.Continue;
        }

        return await command.ExecuteAsync(runtime, queryEngine, cancellationToken).ConfigureAwait(false);
    }

    private static Dictionary<string, SlashCommand> CreateCommands()
    {
        return new Dictionary<string, SlashCommand>(StringComparer.Ordinal)
        {
            ["/help"] = new(
                new SlashCommandDefinition("/help", "show commands"),
                static (_, _, _) =>
                {
                    foreach (var definition in Commands.Values.Select(static command => command.Definition).OrderBy(static definition => definition.Name, StringComparer.Ordinal))
                    {
                        System.Console.WriteLine($"{definition.Name.PadRight(9)} {definition.Description}");
                    }

                    return Task.FromResult(SlashCommandResult.Continue);
                }),
            ["/tools"] = new(
                new SlashCommandDefinition("/tools", "list tool registry"),
                static (runtime, _, _) =>
                {
                    foreach (var tool in runtime.ToolCatalog)
                    {
                        System.Console.WriteLine($"- {tool.Name}: {tool.Description}");
                    }

                    return Task.FromResult(SlashCommandResult.Continue);
                }),
            ["/context"] = new(
                new SlashCommandDefinition("/context", "show the memoized session context snapshot"),
                static (runtime, _, _) =>
                {
                    System.Console.WriteLine(runtime.ContextSnapshot.CurrentDate);
                    System.Console.WriteLine($"Workspace: {runtime.ContextSnapshot.WorkspaceRoot}");
                    System.Console.WriteLine();
                    System.Console.WriteLine("Workspace snapshot:");
                    System.Console.WriteLine(runtime.ContextSnapshot.WorkspaceSummary);
                    System.Console.WriteLine();
                    System.Console.WriteLine("Git snapshot:");
                    System.Console.WriteLine(runtime.ContextSnapshot.GitStatus);
                    return Task.FromResult(SlashCommandResult.Continue);
                }),
            ["/history"] = new(
                new SlashCommandDefinition("/history", "print the current transcript"),
                static (_, queryEngine, _) =>
                {
                    foreach (ConversationEntry entry in queryEngine.History)
                    {
                        System.Console.WriteLine($"[{entry.Role.Value}] {entry.Text}");
                        System.Console.WriteLine();
                    }

                    return Task.FromResult(SlashCommandResult.Continue);
                }),
            ["/reset"] = new(
                new SlashCommandDefinition("/reset", "create a fresh agent session"),
                static async (_, queryEngine, cancellationToken) =>
                {
                    await queryEngine.ResetAsync(cancellationToken).ConfigureAwait(false);
                    System.Console.WriteLine("Started a fresh session.");
                    return SlashCommandResult.Continue;
                }),
            ["/exit"] = new(
                new SlashCommandDefinition("/exit", "leave the console"),
                static (_, _, _) => Task.FromResult(SlashCommandResult.Exit)),
        };
    }
}

