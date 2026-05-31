using EventHorizon.Engine.Runs;

namespace EventHorizon.Tests.Engine.Runs;

/// <summary>
/// Tests for RunStates constant values and state management constants.
/// </summary>
public sealed class RunStatesTests
{
    #region Primary Status Constants

    [Fact]
    public void RunStates_Has_Idle_Constant()
    {
        // Assert
        Assert.Equal("idle", RunStates.Idle);
    }

    [Fact]
    public void RunStates_Has_Running_Constant()
    {
        // Assert
        Assert.Equal("running", RunStates.Running);
    }

    [Fact]
    public void RunStates_Has_Completed_Constant()
    {
        // Assert
        Assert.Equal("completed", RunStates.Completed);
    }

    [Fact]
    public void RunStates_Has_Failed_Constant()
    {
        // Assert
        Assert.Equal("failed", RunStates.Failed);
    }

    [Fact]
    public void RunStates_Has_Cancelled_Constant()
    {
        // Assert
        Assert.Equal("cancelled", RunStates.Cancelled);
    }

    #endregion

    #region Detailed Status Constants

    [Fact]
    public void RunStates_Has_Planning_DetailedStatus()
    {
        // Assert
        Assert.Equal("planning", RunStates.Planning);
    }

    [Fact]
    public void RunStates_Has_Executing_DetailedStatus()
    {
        // Assert
        Assert.Equal("executing", RunStates.Executing);
    }

    [Fact]
    public void RunStates_Has_WaitingForTool_DetailedStatus()
    {
        // Assert
        Assert.Equal("waiting_for_tool", RunStates.WaitingForTool);
    }

    [Fact]
    public void RunStates_Has_ApplyingChanges_DetailedStatus()
    {
        // Assert
        Assert.Equal("applying_changes", RunStates.ApplyingChanges);
    }

    [Fact]
    public void RunStates_Has_RunningTests_DetailedStatus()
    {
        // Assert
        Assert.Equal("running_tests", RunStates.RunningTests);
    }

    #endregion

    #region State Value Validation

    [Fact]
    public void RunStates_PrimaryStates_Are_Lowercase()
    {
        // Arrange & Assert
        Assert.Equal(RunStates.Idle, RunStates.Idle.ToLowerInvariant());
        Assert.Equal(RunStates.Running, RunStates.Running.ToLowerInvariant());
        Assert.Equal(RunStates.Completed, RunStates.Completed.ToLowerInvariant());
        Assert.Equal(RunStates.Failed, RunStates.Failed.ToLowerInvariant());
        Assert.Equal(RunStates.Cancelled, RunStates.Cancelled.ToLowerInvariant());
    }

    [Fact]
    public void RunStates_DetailedStates_Use_Snake_Case()
    {
        // Arrange & Assert
        Assert.Contains("_", RunStates.WaitingForTool);
        Assert.Contains("_", RunStates.ApplyingChanges);
        Assert.Contains("_", RunStates.RunningTests);
    }

    [Fact]
    public void RunStates_All_Statuses_Are_Non_Empty()
    {
        // Arrange & Assert
        Assert.NotEmpty(RunStates.Idle);
        Assert.NotEmpty(RunStates.Running);
        Assert.NotEmpty(RunStates.Completed);
        Assert.NotEmpty(RunStates.Failed);
        Assert.NotEmpty(RunStates.Cancelled);
        Assert.NotEmpty(RunStates.Planning);
        Assert.NotEmpty(RunStates.Executing);
        Assert.NotEmpty(RunStates.WaitingForTool);
        Assert.NotEmpty(RunStates.ApplyingChanges);
        Assert.NotEmpty(RunStates.RunningTests);
    }

    #endregion

    #region State Classification

    [Fact]
    public void Primary_States_Are_Different()
    {
        // Arrange
        var states = new[]
        {
            RunStates.Idle,
            RunStates.Running,
            RunStates.Completed,
            RunStates.Failed,
            RunStates.Cancelled,
        };

        // Assert
        var distinctStates = states.Distinct().ToList();
        Assert.Equal(states.Length, distinctStates.Count);
    }

    [Fact]
    public void Detailed_States_Are_Different()
    {
        // Arrange
        var states = new[]
        {
            RunStates.Planning,
            RunStates.Executing,
            RunStates.WaitingForTool,
            RunStates.ApplyingChanges,
            RunStates.RunningTests,
        };

        // Assert
        var distinctStates = states.Distinct().ToList();
        Assert.Equal(states.Length, distinctStates.Count);
    }

    [Fact]
    public void Primary_And_Detailed_States_Are_Mutually_Exclusive()
    {
        // Arrange
        var primaryStates = new[]
        {
            RunStates.Idle,
            RunStates.Running,
            RunStates.Completed,
            RunStates.Failed,
            RunStates.Cancelled,
        };

        var detailedStates = new[]
        {
            RunStates.Planning,
            RunStates.Executing,
            RunStates.WaitingForTool,
            RunStates.ApplyingChanges,
            RunStates.RunningTests,
        };

        // Assert
        var intersection = primaryStates.Intersect(detailedStates);
        Assert.Empty(intersection);
    }

    #endregion

    #region State Matching

    [Fact]
    public void RunStates_Support_Ordinal_Comparison()
    {
        // Arrange & Act
        var state1 = RunStates.Running;
        var state2 = "running";

        // Assert
        Assert.Equal(state1, state2);
        Assert.True(string.Equals(state1, state2, StringComparison.Ordinal));
    }

    [Fact]
    public void RunStates_Comparison_Is_Case_Sensitive()
    {
        // Arrange & Act
        var state = RunStates.Running;
        var wrongCase = "RUNNING";

        // Assert
        Assert.NotEqual(state, wrongCase);
    }

    #endregion

    #region Constant Immutability

    [Fact]
    public void RunStates_Constants_Cannot_Be_Modified()
    {
        // This test documents the immutability of constants.
        // The constants are string literals and therefore immutable.

        // Arrange
        var original = RunStates.Running;

        // Act & Assert
        Assert.Equal("running", original);
        Assert.Equal("running", RunStates.Running); // Verify still the same
    }

    #endregion

    #region State Enumeration Tests

    [Fact]
    public void Primary_Status_Count()
    {
        // Arrange & Act
        var primaryStates = new[]
        {
            RunStates.Idle,
            RunStates.Running,
            RunStates.Completed,
            RunStates.Failed,
            RunStates.Cancelled,
        };

        // Assert
        Assert.Equal(5, primaryStates.Length);
    }

    [Fact]
    public void Detailed_Status_Count()
    {
        // Arrange & Act
        var detailedStates = new[]
        {
            RunStates.Planning,
            RunStates.Executing,
            RunStates.WaitingForTool,
            RunStates.ApplyingChanges,
            RunStates.RunningTests,
        };

        // Assert
        Assert.Equal(5, detailedStates.Length);
    }

    #endregion

    #region Common Usage Patterns

    [Fact]
    public void Can_Compare_Run_Status_To_Idle()
    {
        // Arrange
        var status = RunStates.Idle;

        // Act & Assert
        Assert.True(status == RunStates.Idle);
        Assert.True(string.Equals(status, RunStates.Idle, StringComparison.Ordinal));
    }

    [Fact]
    public void Can_Check_If_Status_Is_Terminal()
    {
        // Arrange
        var terminalStates = new[] { RunStates.Completed, RunStates.Failed, RunStates.Cancelled };

        // Act & Assert
        foreach (var state in terminalStates)
        {
            Assert.True(IsTerminalState(state));
        }
    }

    [Fact]
    public void Can_Check_If_Status_Is_Running()
    {
        // Arrange
        var runningStatus = RunStates.Running;
        var nonRunningStatus = RunStates.Completed;

        // Act & Assert
        Assert.True(string.Equals(runningStatus, RunStates.Running, StringComparison.Ordinal));
        Assert.False(string.Equals(nonRunningStatus, RunStates.Running, StringComparison.Ordinal));
    }

    private static bool IsTerminalState(string status)
    {
        return status == RunStates.Completed ||
               status == RunStates.Failed ||
               status == RunStates.Cancelled;
    }

    #endregion
}
