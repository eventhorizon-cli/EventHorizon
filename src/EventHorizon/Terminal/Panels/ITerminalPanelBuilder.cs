namespace EventHorizon.Terminal.Panels;

public interface ITerminalPanelBuilder
{
    string PanelId { get; }

    TerminalPanelViewModel Build(TerminalPanelBuildContext context);
}

