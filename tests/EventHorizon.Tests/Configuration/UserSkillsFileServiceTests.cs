using EventHorizon.Configuration;
using EventHorizon.Tests.Fixtures;

namespace EventHorizon.Tests.Configuration;

public sealed class UserSkillsFileServiceTests : IDisposable
{
    private readonly TemporaryWorkspaceFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    private sealed class StubPathEnvironment : IPathEnvironment
    {
        private readonly string _homeDirectory;

        public StubPathEnvironment(string homeDirectory)
        {
            _homeDirectory = homeDirectory;
        }

        public string CurrentDirectory => Directory.GetCurrentDirectory();
        public string HomeDirectory => _homeDirectory;
    }

    [Fact]
    public void FilePath_Returns_Expected_Location()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);

        // Act
        var service = new UserSkillsFileService(pathEnvironment);

        // Assert
        var expected = Path.Combine(_fixture.Root, ".eventhorizon", "skills.json");
        Assert.Equal(expected, service.FilePath);
    }

    [Fact]
    public void EnsureExists_Creates_Directory_And_Empty_File()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserSkillsFileService(pathEnvironment);

        // Act
        service.EnsureExists();

        // Assert
        Assert.True(File.Exists(service.FilePath));
        var content = File.ReadAllText(service.FilePath);
        Assert.Equal("{}\n", content);
    }

    [Fact]
    public void EnsureExists_Preserves_Existing_File()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserSkillsFileService(pathEnvironment);
        service.EnsureExists();
        var originalContent = File.ReadAllText(service.FilePath);

        // Act
        service.EnsureExists();

        // Assert
        var currentContent = File.ReadAllText(service.FilePath);
        Assert.Equal(originalContent, currentContent);
    }

    [Fact]
    public void Save_Persists_Skills_Options()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserSkillsFileService(pathEnvironment);
        var skillsPath = Path.Combine(_fixture.Root, "skills");
        Directory.CreateDirectory(skillsPath);

        var skillsOptions = new SkillsOptions
        {
            StoragePath = skillsPath,
            Imported =
            [
                new ImportedSkillOptions
                {
                    Name = "test-skill",
                    Path = Path.Combine(skillsPath, "test-skill"),
                    Description = "A test skill",
                    ImportedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        // Act
        service.Save(skillsOptions);

        // Assert
        Assert.True(File.Exists(service.FilePath));
        var content = File.ReadAllText(service.FilePath);
        Assert.Contains("Skills", content);
        Assert.Contains("StoragePath", content);
        Assert.Contains("Imported", content);
        Assert.Contains("test-skill", content);
    }

    [Fact]
    public void Save_Handles_Empty_Imported_Skills()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserSkillsFileService(pathEnvironment);
        var skillsOptions = new SkillsOptions
        {
            StoragePath = Path.Combine(_fixture.Root, "skills"),
            Imported = []
        };

        // Act
        service.Save(skillsOptions);

        // Assert
        Assert.True(File.Exists(service.FilePath));
        var content = File.ReadAllText(service.FilePath);
        Assert.Contains("Skills", content);
    }

    [Fact]
    public void Save_Includes_All_Skill_Properties()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserSkillsFileService(pathEnvironment);
        var skillsPath = Path.Combine(_fixture.Root, "skills");
        var skillPath = Path.Combine(skillsPath, "advanced-skill");
        Directory.CreateDirectory(skillPath);

        var importedAt = DateTimeOffset.UtcNow;
        var skillsOptions = new SkillsOptions
        {
            StoragePath = skillsPath,
            Imported =
            [
                new ImportedSkillOptions
                {
                    Name = "advanced-skill",
                    Path = skillPath,
                    Description = "An advanced skill with features",
                    ImportedAt = importedAt
                }
            ]
        };

        // Act
        service.Save(skillsOptions);

        // Assert
        var content = File.ReadAllText(service.FilePath);
        Assert.Contains("advanced-skill", content);
        Assert.Contains("An advanced skill with features", content);
    }

    [Fact]
    public void Save_Handles_Multiple_Skills()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserSkillsFileService(pathEnvironment);
        var skillsPath = Path.Combine(_fixture.Root, "skills");
        Directory.CreateDirectory(skillsPath);

        var skillsOptions = new SkillsOptions
        {
            StoragePath = skillsPath,
            Imported =
            [
                new ImportedSkillOptions
                {
                    Name = "skill1",
                    Path = Path.Combine(skillsPath, "skill1"),
                    Description = "First skill",
                    ImportedAt = DateTimeOffset.UtcNow
                },
                new ImportedSkillOptions
                {
                    Name = "skill2",
                    Path = Path.Combine(skillsPath, "skill2"),
                    Description = "Second skill",
                    ImportedAt = DateTimeOffset.UtcNow
                },
                new ImportedSkillOptions
                {
                    Name = "skill3",
                    Path = Path.Combine(skillsPath, "skill3"),
                    Description = "Third skill",
                    ImportedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        // Act
        service.Save(skillsOptions);

        // Assert
        var content = File.ReadAllText(service.FilePath);
        Assert.Contains("skill1", content);
        Assert.Contains("skill2", content);
        Assert.Contains("skill3", content);
    }

    [Fact]
    public void GetDefaultFilePath_Returns_Correct_Path()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);

        // Act
        var path = UserSkillsFileService.GetDefaultFilePath(pathEnvironment);

        // Assert
        Assert.Contains(".eventhorizon", path);
        Assert.Contains("skills.json", path);
        Assert.StartsWith(_fixture.Root, path);
    }

    [Fact]
    public void Save_Multiple_Times_Latest_Wins()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserSkillsFileService(pathEnvironment);
        var skillsPath = Path.Combine(_fixture.Root, "skills");
        Directory.CreateDirectory(skillsPath);

        // Act
        service.Save(new SkillsOptions
        {
            StoragePath = skillsPath,
            Imported =
            [
                new ImportedSkillOptions
                {
                    Name = "old-skill",
                    Path = Path.Combine(skillsPath, "old-skill"),
                    Description = "Old",
                    ImportedAt = DateTimeOffset.UtcNow
                }
            ]
        });

        service.Save(new SkillsOptions
        {
            StoragePath = skillsPath,
            Imported =
            [
                new ImportedSkillOptions
                {
                    Name = "new-skill",
                    Path = Path.Combine(skillsPath, "new-skill"),
                    Description = "New",
                    ImportedAt = DateTimeOffset.UtcNow
                }
            ]
        });

        // Assert
        var content = File.ReadAllText(service.FilePath);
        Assert.Contains("new-skill", content);
        Assert.DoesNotContain("old-skill", content);
    }

    [Fact]
    public void Save_Preserves_Skill_Order()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserSkillsFileService(pathEnvironment);
        var skillsPath = Path.Combine(_fixture.Root, "skills");
        Directory.CreateDirectory(skillsPath);

        var skillsOptions = new SkillsOptions
        {
            StoragePath = skillsPath,
            Imported =
            [
                new ImportedSkillOptions
                {
                    Name = "alpha",
                    Path = Path.Combine(skillsPath, "alpha"),
                    Description = "First",
                    ImportedAt = DateTimeOffset.UtcNow
                },
                new ImportedSkillOptions
                {
                    Name = "beta",
                    Path = Path.Combine(skillsPath, "beta"),
                    Description = "Second",
                    ImportedAt = DateTimeOffset.UtcNow
                },
                new ImportedSkillOptions
                {
                    Name = "gamma",
                    Path = Path.Combine(skillsPath, "gamma"),
                    Description = "Third",
                    ImportedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        // Act
        service.Save(skillsOptions);

        // Assert
        var content = File.ReadAllText(service.FilePath);
        var alphaIndex = content.IndexOf("alpha");
        var betaIndex = content.IndexOf("beta");
        var gammaIndex = content.IndexOf("gamma");

        Assert.True(alphaIndex < betaIndex && betaIndex < gammaIndex);
    }
}
