namespace EventHorizon.Terminal.Commands;

public sealed class SidebarCommandHandler : ITerminalCommandHandler
{
    public bool CanHandle(TerminalCommand command) => command.Name == "/sidebar";

    public Task<TerminalCommandResult> ExecuteAsync(TerminalCommandContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Command.Argument))
        {
            string modes = string.Join(", ", TerminalCommandCatalog.GetSidebarModes());
            return Task.FromResult(TerminalCommandResult.Success($"Usage: /sidebar <mode> where mode is one of: {modes}."));
        }

        if (!TerminalCommandCatalog.TryNormalizeSidebarMode(context.Command.Argument, out string mode))
        {
            string modes = string.Join(", ", TerminalCommandCatalog.GetSidebarModes());
            return Task.FromResult(TerminalCommandResult.Success($"Unknown sidebar mode '{context.Command.Argument}'. Available: {modes}."));
        }

        context.Runtime.State.SetSidebarMode(mode);
        context.Runtime.State.AddActivity("sidebar", "Sidebar mode changed", mode);
        return Task.FromResult(TerminalCommandResult.Success($"Sidebar switched to {TerminalCommandCatalog.GetSidebarModeLabel(mode)}."));
    }
}