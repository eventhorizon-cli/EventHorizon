using EventHorizon.Controllers;
using EventHorizon.DTOs;
using EventHorizon.Engine.Sessions;
using EventHorizon.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;

namespace EventHorizon.Tests.Sessions;

public sealed class SessionsControllerTests : IDisposable
{
    private readonly TemporaryWorkspaceFixture _fixture;
    private readonly StubWorkspaceContextAccessor _workspaceContextAccessor;

    public SessionsControllerTests()
    {
        _fixture = new TemporaryWorkspaceFixture();
        _workspaceContextAccessor = new StubWorkspaceContextAccessor(_fixture.Root);
    }

    [Fact]
    public void CreateDirectory_Creates_Subdirectory_And_Returns_Created_Result()
    {
        var controller = CreateController();
        var parentPath = _fixture.CreateSubdirectory("workspace");

        var result = controller.CreateDirectory(new CreateDirectoryRequestDTO
        {
            ParentPath = parentPath,
            Name = "feature-a",
        });

        var created = Assert.IsType<CreatedResult>(result.Result);
        var directory = Assert.IsType<DirectoryItemDTO>(created.Value);
        var createdPath = Path.Combine(parentPath, "feature-a");

        Assert.Equal(createdPath, directory.Path);
        Assert.Equal("feature-a", directory.Name);
        Assert.True(directory.IsDirectory);
        Assert.Equal(parentPath, directory.ParentPath);
        Assert.True(Directory.Exists(createdPath));
    }

    [Fact]
    public void CreateDirectory_Returns_ValidationProblem_For_Invalid_Folder_Name()
    {
        var controller = CreateController();
        var parentPath = _fixture.CreateSubdirectory("workspace");

        var result = controller.CreateDirectory(new CreateDirectoryRequestDTO
        {
            ParentPath = parentPath,
            Name = "../escape",
        });

        var objectResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, objectResult.StatusCode);
        var problem = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.Contains(nameof(CreateDirectoryRequestDTO.Name), problem.Errors.Keys);
    }

    [Fact]
    public void CreateDirectory_Returns_ValidationProblem_When_Target_Already_Exists()
    {
        var controller = CreateController();
        var parentPath = _fixture.CreateSubdirectory("workspace");
        Directory.CreateDirectory(Path.Combine(parentPath, "existing"));

        var result = controller.CreateDirectory(new CreateDirectoryRequestDTO
        {
            ParentPath = parentPath,
            Name = "existing",
        });

        var objectResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, objectResult.StatusCode);
        var problem = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.Contains(nameof(CreateDirectoryRequestDTO.Name), problem.Errors.Keys);
    }

    private SessionsController CreateController()
        => new(new StubSessionService(), new StubSessionModelService(), _workspaceContextAccessor);

    public void Dispose()
    {
        _fixture.Dispose();
    }

    private sealed class StubSessionService : ISessionService
    {
        public Task<IReadOnlyList<SessionSummaryDTO>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SessionSummaryDTO>>([]);

        public Task<SessionDetailDTO?> GetAsync(string sessionId, CancellationToken cancellationToken)
            => Task.FromResult<SessionDetailDTO?>(null);

        public Task<SessionDocument?> GetDocumentAsync(string sessionId, CancellationToken cancellationToken)
            => Task.FromResult<SessionDocument?>(null);

        public Task<SessionSummaryDTO> CreateAsync(CreateSessionRequestDTO request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<SessionSummaryDTO?> UpdateAsync(string sessionId, UpdateSessionRequestDTO request, CancellationToken cancellationToken)
            => Task.FromResult<SessionSummaryDTO?>(null);

        public Task<bool> DeleteAsync(string sessionId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task<SessionDocument?> StartRunAsync(string sessionId, string runId, string task, CancellationToken cancellationToken)
            => Task.FromResult<SessionDocument?>(null);

        public Task RecordRunCompletedAsync(string sessionId, string? assistantMessage, int changedFilesCount, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task RecordRunFailedAsync(string sessionId, string error, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task RecordRunCancelledAsync(string sessionId, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task GenerateTitleIfNeededAsync(string sessionId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class StubSessionModelService : ISessionModelService
    {
        public Task<SessionModelUpdateResult?> UpdateAsync(string sessionId, string? providerName, string? modelId, CancellationToken cancellationToken)
            => Task.FromResult<SessionModelUpdateResult?>(null);
    }
}

