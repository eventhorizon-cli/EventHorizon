namespace EventHorizon.Conversations;

public sealed class ConversationUsageSnapshot
{
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long TotalTokens { get; set; }
    public decimal TotalCost { get; set; }
    public bool HasPrice { get; set; }
}