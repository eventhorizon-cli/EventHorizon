using System.Reflection;
using System.Text.Json;
using EventHorizon.Workspace;
using EventHorizon.Workspace.Skills;
using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.Tests.Workspace.Skills;

public sealed class DotNetFileRunnerSkillTests : IDisposable
{
    private readonly string _workspaceRoot = Path.Combine(Path.GetTempPath(), "eventhorizon-tests", Guid.NewGuid().ToString("N"));

    public DotNetFileRunnerSkillTests()
    {
        Directory.CreateDirectory(_workspaceRoot);
    }

    [Fact]
    public async Task RunDotNetFileAsyncDeletesFileAfterRunByDefault()
    {
        var filePath = Path.Combine(_workspaceRoot, "task.cs");
        await File.WriteAllTextAsync(filePath, """
            #:property PublishAot=false

            Console.WriteLine("ok");
            """);

        var services = new ServiceCollection()
            .AddSingleton<IWorkspaceContextAccessor>(new TestWorkspaceContextAccessor(_workspaceRoot))
            .BuildServiceProvider();

        var result = await InvokeRunDotNetFileAsync(services, "task.cs");
        using var document = JsonDocument.Parse(result);

        Assert.False(File.Exists(filePath));
        Assert.True(document.RootElement.GetProperty("cleanup").GetProperty("deleted").GetBoolean());
        Assert.Equal(0, document.RootElement.GetProperty("result").GetProperty("exitCode").GetInt32());
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspaceRoot))
        {
            Directory.Delete(_workspaceRoot, recursive: true);
        }
    }

    private static Task<string> InvokeRunDotNetFileAsync(IServiceProvider services, string filePath)
    {
        var method = typeof(DotNetFileRunnerSkill).GetMethod(
            "RunDotNetFileAsync",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        return (Task<string>)method.Invoke(
            null,
            [services, filePath, null, null, true, 180, 80_000])!;
    }

    private sealed class TestWorkspaceContextAccessor : IWorkspaceContextAccessor
    {
        public TestWorkspaceContextAccessor(string workspaceRoot)
        {
            WorkspaceContext = new WorkspaceContext(workspaceRoot);
        }

        public WorkspaceContext WorkspaceContext { get; set; }
    }
}
