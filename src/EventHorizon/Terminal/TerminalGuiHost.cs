using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Time;

namespace EventHorizon.Terminal;

public sealed class TerminalGuiHost
{
    private IApplication? _application;

    internal static string? GetPreferredDriverName()
        => OperatingSystem.IsMacOS() ? DriverRegistry.Names.ANSI : null;

    internal static AppModel? GetPreferredAppModel()
        => OperatingSystem.IsMacOS() ? AppModel.Inline : null;

    public IApplication Application => _application ?? throw new InvalidOperationException("Terminal.Gui application has not been initialized.");

    public void Initialize()
    {
        if (_application is not null)
        {
            return;
        }

        _application = global::Terminal.Gui.App.Application.Create(new SystemTimeProvider());
        if (GetPreferredAppModel() is { } appModel)
        {
            _application.AppModel = appModel;
        }

        _application.Init(GetPreferredDriverName());
    }

    public void Run(IRunnable runnable)
        => Application.Run(runnable, static _ => true);

    public void RequestStop(IRunnable runnable)
        => Application.RequestStop(runnable);

    public void RequestStop()
        => Application.RequestStop();

    public void Invoke(Action action)
        => Application.Invoke(action);

    public void Shutdown()
    {
        _application?.Dispose();
        _application = null;
    }
}

