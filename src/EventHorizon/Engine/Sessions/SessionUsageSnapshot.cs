namespace EventHorizon.Engine.Sessions;

public sealed class SessionUsageSnapshot
{
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long TotalTokens { get; set; }
    public decimal TotalCost { get; set; }
    public bool HasPrice { get; set; }
}
