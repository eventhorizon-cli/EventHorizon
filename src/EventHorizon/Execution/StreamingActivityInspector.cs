using Microsoft.Extensions.AI;

namespace EventHorizon.Execution;

internal static class StreamingActivityInspector
{
    public static IEnumerable<QueryEvent> Inspect(Microsoft.Agents.AI.AgentResponseUpdate update)
        => InspectContents(update.Contents);

    public static IEnumerable<QueryEvent> InspectContents(IEnumerable<object> contents)
    {
        foreach (object content in contents)
        {
            if (content is UsageContent)
            {
                continue;
            }

            string typeName = content.GetType().Name;
            if (typeName.Contains("FunctionCall", StringComparison.OrdinalIgnoreCase) || typeName.Contains("ToolCall", StringComparison.OrdinalIgnoreCase))
            {
                yield return new QueryEvent(QueryEventKind.ToolCall, DescribeToolCall(content));
                continue;
            }

            if (typeName.Contains("FunctionResult", StringComparison.OrdinalIgnoreCase) || typeName.Contains("ToolResult", StringComparison.OrdinalIgnoreCase))
            {
                yield return new QueryEvent(QueryEventKind.ToolResult, DescribeToolResult(content));
            }
        }
    }

    private static string DescribeToolCall(object content)
    {
        string name = ReadString(content, "Name")
            ?? ReadString(content, "FunctionName")
            ?? ReadString(content, "CallId")
            ?? content.GetType().Name;
        string? arguments = ReadString(content, "Arguments")
            ?? ReadString(content, "Input")
            ?? ReadString(content, "ArgumentsJson");
        return string.IsNullOrWhiteSpace(arguments)
            ? name
            : $"{name} {Summarize(arguments, 88)}";
    }

    private static string DescribeToolResult(object content)
    {
        string name = ReadString(content, "Name")
            ?? ReadString(content, "FunctionName")
            ?? ReadString(content, "CallId")
            ?? content.GetType().Name;
        string? result = ReadString(content, "Result")
            ?? ReadString(content, "Text")
            ?? ReadObject(content, "Value")
            ?? ReadObject(content, "Output");
        return string.IsNullOrWhiteSpace(result)
            ? name
            : $"{name} → {Summarize(result, 88)}";
    }

    private static string? ReadString(object source, string propertyName)
    {
        object? value = source.GetType().GetProperty(propertyName)?.GetValue(source);
        return value as string;
    }

    private static string? ReadObject(object source, string propertyName)
    {
        object? value = source.GetType().GetProperty(propertyName)?.GetValue(source);
        if (value is null)
        {
            return null;
        }

        return value.ToString();
    }

    private static string Summarize(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";
}

