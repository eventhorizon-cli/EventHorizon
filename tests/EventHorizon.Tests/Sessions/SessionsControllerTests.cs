using EventHorizon.Controllers;
using EventHorizon.DTOs;
using EventHorizon.Engine.Sessions;
using EventHorizon.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Moq;

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
        => new(
            Mock.Of<ISessionService>(),
            Mock.Of<ISessionModelService>(),
            _workspaceContextAccessor);

    public void Dispose()
    {
        _fixture.Dispose();
    }
}

