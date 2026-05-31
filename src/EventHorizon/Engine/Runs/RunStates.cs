namespace EventHorizon.Engine.Runs;

public static class RunStates
{
    public const string Idle = "idle";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";

    public const string Planning = "planning";
    public const string Executing = "executing";
    public const string WaitingForTool = "waiting_for_tool";
    public const string ApplyingChanges = "applying_changes";
    public const string RunningTests = "running_tests";
}

