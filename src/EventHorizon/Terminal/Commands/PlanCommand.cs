namespace EventHorizon.Terminal.Commands;

public sealed class PlanCommand : ITerminalCommand
{
    public string Name => "/plan";

    public string Description => "Show current plan";

    public Task ExecuteAsync(TerminalCommandContext context, CancellationToken cancellationToken)
        => context.Dialogs.ShowPlanAsync(context.State.Plan);
}

