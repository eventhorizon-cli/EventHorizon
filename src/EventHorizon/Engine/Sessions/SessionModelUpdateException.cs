namespace EventHorizon.Engine.Sessions;

public sealed class SessionModelUpdateException : InvalidOperationException
{
    public SessionModelUpdateException(string message)
        : base(message)
    {
    }
}
