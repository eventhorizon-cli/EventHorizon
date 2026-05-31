
using EventHorizon.Configuration;
using EventHorizon.DTOs;
using EventHorizon.Providers;
using Microsoft.AspNetCore.Mvc;

namespace EventHorizon.Controllers;

[ApiController]
[Route("api/configuration")]
public sealed class ConfigurationController : ControllerBase
{
    private readonly IAppConfigurationService _appConfigurationService;
    private readonly IUserConfigurationFileService _userConfigurationFileService;
    private readonly IEventHorizonRuntime _runtime;
    private readonly ISessionAgentManager _conversationAgentManager;

    public ConfigurationController(
        IAppConfigurationService appConfigurationService,
        IUserConfigurationFileService userConfigurationFileService,
        IEventHorizonRuntime runtime,
        ISessionAgentManager conversationAgentManager)
    {
        _appConfigurationService = appConfigurationService;
        _userConfigurationFileService = userConfigurationFileService;
        _runtime = runtime;
        _conversationAgentManager = conversationAgentManager;
    }

    [HttpGet]
    public ActionResult<AppConfigurationResponseDTO> Get()
    {
        var providers = _appConfigurationService.GetProvidersOptions();
        var mcp = _appConfigurationService.GetMcpOptions();
        var skills = _appConfigurationService.GetSkillsOptions();

        return Ok(new AppConfigurationResponseDTO
        {
            FilePath = _userConfigurationFileService.FilePath,
            CurrentDefaultProvider = providers.CurrentDefaultProvider,
            Providers = providers.Providers
                .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(static pair => new ApiProviderViewModelDTO
                {
                    Name = pair.Key,
                    Type = pair.Value.Type ?? "openai",
                    Model = pair.Value.Model,
                    Models = [.. pair.Value.Models],
                    Endpoint = pair.Value.Endpoint,
                    ApiKey = pair.Value.ApiKey,
                    Deployment = pair.Value.Deployment,
                    UseDefaultAzureCredential = pair.Value.UseDefaultAzureCredential,
                })
                .ToArray(),
            McpServers = [.. mcp.Servers],
            Skills = skills,
        });
    }

    [HttpPut]
    public async Task<ActionResult<AppConfigurationResponseDTO>> SaveAsync(SaveAppConfigurationRequestDTO request, CancellationToken cancellationToken)
    {
        var providers = new ProvidersOptions
        {
            CurrentDefaultProvider = request.CurrentDefaultProvider,
            Providers = request.Providers.ToDictionary(static item => item.Name, static item => item.Provider, StringComparer.OrdinalIgnoreCase),
        };

        var mcp = new McpOptions
        {
            Servers = [.. request.McpServers],
        };

        var skills = new SkillsOptions
        {
            StoragePath = request.Skills.StoragePath,
            Imported = [.. request.Skills.Imported],
        };

        _appConfigurationService.Save(providers, mcp, skills, cancellationToken);
        _conversationAgentManager.InvalidateAll(cancellationToken);
        await _runtime.InvalidateAsync(cancellationToken).ConfigureAwait(false);

        var savedProviders = _appConfigurationService.GetProvidersOptions();
        var savedMcp = _appConfigurationService.GetMcpOptions();
        var savedSkills = _appConfigurationService.GetSkillsOptions();

        return Ok(new AppConfigurationResponseDTO
        {
            FilePath = _userConfigurationFileService.FilePath,
            CurrentDefaultProvider = savedProviders.CurrentDefaultProvider,
            Providers = savedProviders.Providers.Select(static pair => new ApiProviderViewModelDTO
            {
                Name = pair.Key,
                Type = pair.Value.Type ?? "openai",
                Model = pair.Value.Model,
                Models = [.. pair.Value.Models],
                Endpoint = pair.Value.Endpoint,
                ApiKey = pair.Value.ApiKey,
                Deployment = pair.Value.Deployment,
                UseDefaultAzureCredential = pair.Value.UseDefaultAzureCredential,
            }).ToArray(),
            McpServers = [.. savedMcp.Servers],
            Skills = savedSkills,
        });
    }
}
