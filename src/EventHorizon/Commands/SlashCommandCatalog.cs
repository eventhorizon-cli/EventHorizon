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

        if (!Commands.TryGetValue(input.Trim(), out var command))
        {
            Console.WriteLine("Unknown slash command. Use /help.");
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
                        Console.WriteLine($"{definition.Name.PadRight(9)} {definition.Description}");
                    }

                    return Task.FromResult(SlashCommandResult.Continue);
                }),
            ["/tools"] = new(
                new SlashCommandDefinition("/tools", "list tool registry"),
                static (runtime, _, _) =>
                {
                    foreach (var tool in runtime.ToolCatalog)
                    {
                        Console.WriteLine($"- {tool.Name}: {tool.Description}");
                    }

                    return Task.FromResult(SlashCommandResult.Continue);
                }),
            ["/context"] = new(
                new SlashCommandDefinition("/context", "show the memoized session context snapshot"),
                static (runtime, _, _) =>
                {
                    Console.WriteLine(runtime.ContextSnapshot.CurrentDate);
                    Console.WriteLine($"Workspace: {runtime.ContextSnapshot.WorkspaceRoot}");
                    Console.WriteLine();
                    Console.WriteLine("Workspace snapshot:");
                    Console.WriteLine(runtime.ContextSnapshot.WorkspaceSummary);
                    Console.WriteLine();
                    Console.WriteLine("Git snapshot:");
                    Console.WriteLine(runtime.ContextSnapshot.GitStatus);
                    return Task.FromResult(SlashCommandResult.Continue);
                }),
            ["/history"] = new(
                new SlashCommandDefinition("/history", "print the current transcript"),
                static (_, queryEngine, _) =>
                {
                    foreach (var entry in queryEngine.History)
                    {
                        Console.WriteLine($"[{entry.Role.Value}] {entry.Text}");
                        Console.WriteLine();
                    }

                    return Task.FromResult(SlashCommandResult.Continue);
                }),
            ["/reset"] = new(
                new SlashCommandDefinition("/reset", "create a fresh agent session"),
                static async (_, queryEngine, cancellationToken) =>
                {
                    await queryEngine.ResetAsync(cancellationToken).ConfigureAwait(false);
                    Console.WriteLine("Started a fresh session.");
                    return SlashCommandResult.Continue;
                }),
            ["/exit"] = new(
                new SlashCommandDefinition("/exit", "leave the console"),
                static (_, _, _) => Task.FromResult(SlashCommandResult.Exit)),
        };
    }
}

