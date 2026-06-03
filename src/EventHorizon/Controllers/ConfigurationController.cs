
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
        var responseProviders = MapSupportedProviders(providers);

        return Ok(new AppConfigurationResponseDTO
        {
            FilePath = _userConfigurationFileService.FilePath,
            CurrentDefaultProvider = ResolveCurrentDefaultProvider(providers.CurrentDefaultProvider, responseProviders),
            Providers = responseProviders,
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
        var responseProviders = MapSupportedProviders(savedProviders);

        return Ok(new AppConfigurationResponseDTO
        {
            FilePath = _userConfigurationFileService.FilePath,
            CurrentDefaultProvider = ResolveCurrentDefaultProvider(savedProviders.CurrentDefaultProvider, responseProviders),
            Providers = responseProviders,
            McpServers = [.. savedMcp.Servers],
            Skills = savedSkills,
        });
    }

    private static ApiProviderViewModelDTO[] MapSupportedProviders(ProvidersOptions providers)
        => providers.Providers
            .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static pair =>
            {
                var normalizedType = ProviderTypes.Normalize(pair.Value.Type);
                return new
                {
                    pair.Key,
                    Provider = pair.Value,
                    Type = normalizedType,
                    Supported = ProviderTypes.IsSupported(normalizedType),
                };
            })
            .Where(static provider => provider.Supported)
            .Select(static provider => new ApiProviderViewModelDTO
            {
                Name = provider.Key,
                Type = provider.Type,
                Model = provider.Provider.Model,
                Models = [.. provider.Provider.Models],
                Endpoint = provider.Provider.Endpoint,
                ApiKey = provider.Provider.ApiKey,
                Deployment = provider.Provider.Deployment,
                UseDefaultAzureCredential = provider.Provider.UseDefaultAzureCredential,
            })
            .ToArray();

    private static string? ResolveCurrentDefaultProvider(string? currentDefaultProvider, IReadOnlyCollection<ApiProviderViewModelDTO> providers)
        => !string.IsNullOrWhiteSpace(currentDefaultProvider) && providers.Any(provider => provider.Name == currentDefaultProvider)
            ? currentDefaultProvider
            : null;
}
