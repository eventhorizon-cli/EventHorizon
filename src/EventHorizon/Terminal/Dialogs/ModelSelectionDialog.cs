namespace EventHorizon.Terminal.Dialogs;

public sealed class ModelSelectionDialog : SelectionDialog<string>
{
    public ModelSelectionDialog(IReadOnlyList<string> models, Action<global::Terminal.Gui.App.IRunnable> requestStop)
        : base("Select Model", models, static model => model, requestStop)
    {
    }
}

