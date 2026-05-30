using EventHorizon.AGUI.DTOs;
using EventHorizon.Configuration;
using EventHorizon.EntryPoints;
using EventHorizon.Providers;
using Microsoft.AspNetCore.Mvc;

namespace EventHorizon.AGUI.Controllers;

[ApiController]
[Route("api/configuration")]
public sealed class ConfigurationController : ControllerBase
{
    private readonly IAppConfigurationService _appConfigurationService;
    private readonly IUserConfigurationFileService _userConfigurationFileService;
    private readonly IEventHorizonRuntimeInitializer _runtimeInitializer;
    private readonly IConversationAgentManager _conversationAgentManager;

    public ConfigurationController(
        IAppConfigurationService appConfigurationService,
        IUserConfigurationFileService userConfigurationFileService,
        IEventHorizonRuntimeInitializer runtimeInitializer,
        IConversationAgentManager conversationAgentManager)
    {
        _appConfigurationService = appConfigurationService;
        _userConfigurationFileService = userConfigurationFileService;
        _runtimeInitializer = runtimeInitializer;
        _conversationAgentManager = conversationAgentManager;
    }

    [HttpGet]
    public ActionResult<AppConfigurationResponseDTO> Get()
    {
        var options = _appConfigurationService.Get();
        return Ok(new AppConfigurationResponseDTO
        {
            FilePath = _userConfigurationFileService.FilePath,
            CurrentDefaultProvider = options.CurrentDefaultProvider,
            Providers = options.Providers
                .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(static pair => new ApiProviderViewModel
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
            McpServers = [.. options.McpServers],
            Skills = options.Skills,
        });
    }

    [HttpPut]
    public async Task<ActionResult<AppConfigurationResponseDTO>> SaveAsync(SaveAppConfigurationRequestDTO request, CancellationToken cancellationToken)
    {
        var current = _appConfigurationService.Get();
        var next = new AppOptions
        {
            AGUI = current.AGUI,
            Agent = current.Agent,
            Provider = current.Provider,
            CurrentDefaultProvider = request.CurrentDefaultProvider,
            Providers = request.Providers.ToDictionary(static item => item.Name, static item => item.Provider, StringComparer.OrdinalIgnoreCase),
            Pricing = current.Pricing,
            Conversation = current.Conversation,
            McpServers = [.. request.McpServers],
            Skills = request.Skills,
        };
        var saved = await _appConfigurationService.SaveAsync(next, cancellationToken).ConfigureAwait(false);
        await _conversationAgentManager.InvalidateAllAsync(cancellationToken).ConfigureAwait(false);
        await _runtimeInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        return Ok(new AppConfigurationResponseDTO
        {
            FilePath = _userConfigurationFileService.FilePath,
            CurrentDefaultProvider = saved.CurrentDefaultProvider,
            Providers = saved.Providers.Select(static pair => new ApiProviderViewModel
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
            McpServers = [.. saved.McpServers],
            Skills = saved.Skills,
        });
    }
}
