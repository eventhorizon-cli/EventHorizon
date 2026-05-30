namespace EventHorizon.AGUI;

public sealed class ConversationModelUpdateException : InvalidOperationException
{
    public ConversationModelUpdateException(string message)
        : base(message)
    {
    }
}
