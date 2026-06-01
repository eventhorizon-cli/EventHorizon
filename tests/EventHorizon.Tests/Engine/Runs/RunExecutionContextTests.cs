using EventHorizon.Engine.Runs;

namespace EventHorizon.Tests.Engine.Runs;

/// <summary>
/// Tests for RunExecutionContext covering execution state and tool call tracking.
/// </summary>
public sealed class RunExecutionContextTests
{
    #region Initialization Tests

    [Fact]
    public void RunExecutionContext_Initializes_With_GeneratedAssistantMessageId()
    {
        // Arrange & Act
        var context = new RunExecutionContext();

        // Assert
        Assert.NotNull(context.AssistantMessageId);
        Assert.NotEmpty(context.AssistantMessageId);
        Assert.StartsWith("msg_", context.AssistantMessageId);
    }

    [Fact]
    public void RunExecutionContext_Initializes_With_GeneratedExecutionStepId()
    {
        // Arrange & Act
        var context = new RunExecutionContext();

        // Assert
        Assert.NotNull(context.ExecutionStepId);
        Assert.NotEmpty(context.ExecutionStepId);
        Assert.StartsWith("step_", context.ExecutionStepId);
    }

    [Fact]
    public void RunExecutionContext_Initializes_With_Empty_AssistantText()
    {
        // Arrange & Act
        var context = new RunExecutionContext();

        // Assert
        Assert.NotNull(context.AssistantText);
        Assert.Empty(context.AssistantText.ToString());
    }

    [Fact]
    public void RunExecutionContext_Initializes_With_Empty_ToolCalls()
    {
        // Arrange & Act
        var context = new RunExecutionContext();

        // Assert
        Assert.NotNull(context.ToolCalls);
        Assert.Empty(context.ToolCalls);
    }

    [Fact]
    public void RunExecutionContext_Initializes_AssistantMessageNotStarted()
    {
        // Arrange & Act
        var context = new RunExecutionContext();

        // Assert
        Assert.False(context.AssistantMessageStarted);
    }

    [Fact]
    public void RunExecutionContext_Generates_Unique_AssistantMessageIds()
    {
        // Arrange & Act
        var context1 = new RunExecutionContext();
        var context2 = new RunExecutionContext();

        // Assert
        Assert.NotEqual(context1.AssistantMessageId, context2.AssistantMessageId);
    }

    [Fact]
    public void RunExecutionContext_Generates_Unique_ExecutionStepIds()
    {
        // Arrange & Act
        var context1 = new RunExecutionContext();
        var context2 = new RunExecutionContext();

        // Assert
        Assert.NotEqual(context1.ExecutionStepId, context2.ExecutionStepId);
    }

    #endregion

    #region AssistantText Tests

    [Fact]
    public void AssistantText_Can_Append_Text()
    {
        // Arrange
        var context = new RunExecutionContext();
        var text = "Hello, this is a response.";

        // Act
        context.AssistantText.Append(text);

        // Assert
        Assert.Equal(text, context.AssistantText.ToString());
    }

    [Fact]
    public void AssistantText_Can_Append_Multiple_Times()
    {
        // Arrange
        var context = new RunExecutionContext();

        // Act
        context.AssistantText.Append("Part 1 ");
        context.AssistantText.Append("Part 2 ");
        context.AssistantText.Append("Part 3");

        // Assert
        Assert.Equal("Part 1 Part 2 Part 3", context.AssistantText.ToString());
    }

    [Fact]
    public void AssistantText_Handles_Empty_Append()
    {
        // Arrange
        var context = new RunExecutionContext();

        // Act
        context.AssistantText.Append("");
        context.AssistantText.Append("text");

        // Assert
        Assert.Equal("text", context.AssistantText.ToString());
    }

    [Fact]
    public void AssistantText_Handles_Long_Text()
    {
        // Arrange
        var context = new RunExecutionContext();
        var longText = new string('x', 100000);

        // Act
        context.AssistantText.Append(longText);

        // Assert
        Assert.Equal(longText, context.AssistantText.ToString());
    }

    [Fact]
    public void AssistantText_Tracks_Length()
    {
        // Arrange
        var context = new RunExecutionContext();

        // Act
        context.AssistantText.Append("Hello");
        var length = context.AssistantText.Length;

        // Assert
        Assert.Equal(5, length);
    }

    #endregion

    #region AssistantMessageStarted Tests

    [Fact]
    public void AssistantMessageStarted_Defaults_To_False()
    {
        // Arrange & Act
        var context = new RunExecutionContext();

        // Assert
        Assert.False(context.AssistantMessageStarted);
    }

    [Fact]
    public void AssistantMessageStarted_Can_Be_Set_To_True()
    {
        // Arrange
        var context = new RunExecutionContext();

        // Act
        context.AssistantMessageStarted = true;

        // Assert
        Assert.True(context.AssistantMessageStarted);
    }

    [Fact]
    public void AssistantMessageStarted_Can_Be_Set_Back_To_False()
    {
        // Arrange
        var context = new RunExecutionContext();
        context.AssistantMessageStarted = true;

        // Act
        context.AssistantMessageStarted = false;

        // Assert
        Assert.False(context.AssistantMessageStarted);
    }

    #endregion

    #region ToolCalls Dictionary Tests

    [Fact]
    public void ToolCalls_Can_Store_Tool_Call_State()
    {
        // Arrange
        var context = new RunExecutionContext();
        var toolCallState = new RunExecutionContext.ToolCallState
        {
            Id = "tool_123",
            Name = "read_file",
            Arguments = """{"path": "/tmp/file.txt"}""",
        };

        // Act
        context.ToolCalls["tool_123"] = toolCallState;

        // Assert
        Assert.Single(context.ToolCalls);
        Assert.True(context.ToolCalls.TryGetValue("tool_123", out var retrieved));
        Assert.NotNull(retrieved);
        Assert.Equal(toolCallState.Id, retrieved!.Id);
    }

    [Fact]
    public void ToolCalls_Can_Store_Multiple_Tool_Calls()
    {
        // Arrange
        var context = new RunExecutionContext();
        var toolCall1 = new RunExecutionContext.ToolCallState { Id = "t1", Name = "read_file", Arguments = null };
        var toolCall2 = new RunExecutionContext.ToolCallState { Id = "t2", Name = "write_file", Arguments = null };
        var toolCall3 = new RunExecutionContext.ToolCallState { Id = "t3", Name = "run_command", Arguments = null };

        // Act
        context.ToolCalls["t1"] = toolCall1;
        context.ToolCalls["t2"] = toolCall2;
        context.ToolCalls["t3"] = toolCall3;

        // Assert
        Assert.Equal(3, context.ToolCalls.Count);
    }

    [Fact]
    public void ToolCalls_Uses_Ordinal_StringComparison()
    {
        // Arrange
        var context = new RunExecutionContext();
        var toolCallState = new RunExecutionContext.ToolCallState { Id = "tool_test", Name = "test", Arguments = null };

        // Act
        context.ToolCalls["tool_test"] = toolCallState;

        // Assert
        Assert.True(context.ToolCalls.TryGetValue("tool_test", out var _));
        Assert.False(context.ToolCalls.TryGetValue("TOOL_TEST", out var _));
    }

    [Fact]
    public void ToolCalls_Can_Update_Existing_Tool_Call()
    {
        // Arrange
        var context = new RunExecutionContext();
        var originalState = new RunExecutionContext.ToolCallState { Id = "t1", Name = "tool1", Arguments = "arg1" };
        context.ToolCalls["t1"] = originalState;

        // Act
        var updatedState = new RunExecutionContext.ToolCallState { Id = "t1", Name = "tool1", Arguments = "arg2" };
        context.ToolCalls["t1"] = updatedState;

        // Assert
        Assert.Equal("arg2", context.ToolCalls["t1"].Arguments);
    }

    [Fact]
    public void ToolCalls_Handles_Many_Entries()
    {
        // Arrange
        var context = new RunExecutionContext();
        var count = 100;

        // Act
        for (var i = 0; i < count; i++)
        {
            var state = new RunExecutionContext.ToolCallState
            {
                Id = $"tool_{i}",
                Name = $"tool_name_{i}",
                Arguments = null,
            };
            context.ToolCalls[$"tool_{i}"] = state;
        }

        // Assert
        Assert.Equal(count, context.ToolCalls.Count);
    }

    #endregion

    #region ToolCallState Tests

    [Fact]
    public void ToolCallState_Can_Be_Created()
    {
        // Arrange & Act
        var state = new RunExecutionContext.ToolCallState
        {
            Id = "tool_123",
            Name = "read_file",
            Arguments = """{"path": "/tmp/file.txt"}""",
        };

        // Assert
        Assert.Equal("tool_123", state.Id);
        Assert.Equal("read_file", state.Name);
        Assert.Equal("""{"path": "/tmp/file.txt"}""", state.Arguments);
    }

    [Fact]
    public void ToolCallState_Initializes_StartPublished_False()
    {
        // Arrange & Act
        var state = new RunExecutionContext.ToolCallState
        {
            Id = "tool_1",
            Name = "tool",
            Arguments = null,
        };

        // Assert
        Assert.False(state.StartPublished);
    }

    [Fact]
    public void ToolCallState_Initializes_ResultPublished_False()
    {
        // Arrange & Act
        var state = new RunExecutionContext.ToolCallState
        {
            Id = "tool_1",
            Name = "tool",
            Arguments = null,
        };

        // Assert
        Assert.False(state.ResultPublished);
    }

    [Fact]
    public void ToolCallState_Can_Set_StartPublished()
    {
        // Arrange
        var state = new RunExecutionContext.ToolCallState
        {
            Id = "tool_1",
            Name = "tool",
            Arguments = null,
        };

        // Act
        state.StartPublished = true;

        // Assert
        Assert.True(state.StartPublished);
    }

    [Fact]
    public void ToolCallState_Can_Set_ResultPublished()
    {
        // Arrange
        var state = new RunExecutionContext.ToolCallState
        {
            Id = "tool_1",
            Name = "tool",
            Arguments = null,
        };

        // Act
        state.ResultPublished = true;

        // Assert
        Assert.True(state.ResultPublished);
    }

    [Fact]
    public void ToolCallState_Arguments_Can_Be_Null()
    {
        // Arrange & Act
        var state = new RunExecutionContext.ToolCallState
        {
            Id = "tool_null",
            Name = "tool",
            Arguments = null,
        };

        // Assert
        Assert.Null(state.Arguments);
    }

    [Fact]
    public void ToolCallState_Arguments_Can_Be_Updated()
    {
        // Arrange
        var state = new RunExecutionContext.ToolCallState
        {
            Id = "tool_1",
            Name = "tool",
            Arguments = "initial",
        };

        // Act
        state.Arguments = "updated";

        // Assert
        Assert.Equal("updated", state.Arguments);
    }

    [Fact]
    public void ToolCallState_Supports_Long_Arguments()
    {
        // Arrange
        var longArgs = new string('x', 100000);

        // Act
        var state = new RunExecutionContext.ToolCallState
        {
            Id = "tool_long",
            Name = "tool",
            Arguments = longArgs,
        };

        // Assert
        Assert.Equal(longArgs, state.Arguments);
    }

    #endregion

    #region Complex Workflow Tests

    [Fact]
    public void Execution_Context_Tracks_Full_Conversation_Flow()
    {
        // Arrange
        var context = new RunExecutionContext();

        // Act
        context.AssistantMessageStarted = true;
        context.AssistantText.Append("I will read the file and then execute a command.");

        var toolCall1 = new RunExecutionContext.ToolCallState
        {
            Id = "tool_1",
            Name = "read_file",
            Arguments = """{"path": "/tmp/test.txt"}""",
        };
        toolCall1.StartPublished = true;
        toolCall1.ResultPublished = true;

        var toolCall2 = new RunExecutionContext.ToolCallState
        {
            Id = "tool_2",
            Name = "run_in_terminal",
            Arguments = """{"command": "dotnet test"}""",
        };
        toolCall2.StartPublished = true;

        context.ToolCalls["tool_1"] = toolCall1;
        context.ToolCalls["tool_2"] = toolCall2;

        // Assert
        Assert.True(context.AssistantMessageStarted);
        Assert.NotEmpty(context.AssistantText.ToString());
        Assert.Equal(2, context.ToolCalls.Count);
        Assert.True(context.ToolCalls["tool_1"].ResultPublished);
        Assert.False(context.ToolCalls["tool_2"].ResultPublished);
    }

    [Fact]
    public void Execution_Context_Can_Track_Multiple_Message_Streaming()
    {
        // Arrange
        var context = new RunExecutionContext();

        // Act - Simulate streaming message content
        var chunks = new[] { "Hello, ", "I am ", "an AI ", "assistant." };
        foreach (var chunk in chunks)
        {
            context.AssistantText.Append(chunk);
        }

        // Assert
        Assert.Equal("Hello, I am an AI assistant.", context.AssistantText.ToString());
    }

    [Fact]
    public void Execution_Context_Isolates_Multiple_Instances()
    {
        // Arrange
        var context1 = new RunExecutionContext();
        var context2 = new RunExecutionContext();

        // Act
        context1.AssistantText.Append("Context 1");
        context2.AssistantText.Append("Context 2");

        context1.ToolCalls["t1"] = new RunExecutionContext.ToolCallState
        {
            Id = "t1",
            Name = "tool1",
            Arguments = null,
        };

        // Assert
        Assert.Equal("Context 1", context1.AssistantText.ToString());
        Assert.Equal("Context 2", context2.AssistantText.ToString());
        Assert.Single(context1.ToolCalls);
        Assert.Empty(context2.ToolCalls);
    }

    #endregion

    #region State Consistency Tests

    [Fact]
    public void Execution_Context_Maintains_State_Consistency()
    {
        // Arrange
        var context = new RunExecutionContext();

        // Act
        var initialMessageId = context.AssistantMessageId;
        var initialStepId = context.ExecutionStepId;

        context.AssistantMessageStarted = true;
        context.AssistantText.Append("test");

        // Assert
        Assert.Equal(initialMessageId, context.AssistantMessageId);
        Assert.Equal(initialStepId, context.ExecutionStepId);
    }

    #endregion
}
