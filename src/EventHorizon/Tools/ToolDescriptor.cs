using Microsoft.Extensions.AI;

namespace EventHorizon.Tools;

public sealed record ToolDescriptor(
    string Name,
    string Description,
    bool IsReadOnly,
    bool IsConcurrencySafe,
    AITool Tool);


