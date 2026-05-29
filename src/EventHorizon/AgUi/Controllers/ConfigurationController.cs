using EventHorizon.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace EventHorizon.AGUI.Controllers;

[ApiController]
[Route("api/configuration")]
public sealed class ConfigurationController : ControllerBase
{
    private readonly IAppConfigurationService _appConfigurationService;
    private readonly IUserConfigurationFileService _userConfigurationFileService;

    public ConfigurationController(IAppConfigurationService appConfigurationService, IUserConfigurationFileService userConfigurationFileService)
    {
        _appConfigurationService = appConfigurationService;
        _userConfigurationFileService = userConfigurationFileService;
    }

    [HttpGet]
    public ActionResult<AppConfigurationResponse> Get()
    {
        var options = _appConfigurationService.Get();
        return Ok(new AppConfigurationResponse
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
                    ApiKeyMasked = Mask(pair.Value.ApiKey),
                    Deployment = pair.Value.Deployment,
                    UseDefaultAzureCredential = pair.Value.UseDefaultAzureCredential,
                })
                .ToArray(),
            McpServers = [.. options.McpServers],
            Skills = options.Skills,
        });
    }

    [HttpPut]
    public async Task<ActionResult<AppConfigurationResponse>> SaveAsync(SaveAppConfigurationRequest request, CancellationToken cancellationToken)
    {
        var current = _appConfigurationService.Get();
        var next = new AppOptions
        {
            AgUi = current.AgUi,
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
        return Ok(new AppConfigurationResponse
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
                ApiKeyMasked = Mask(pair.Value.ApiKey),
                Deployment = pair.Value.Deployment,
                UseDefaultAzureCredential = pair.Value.UseDefaultAzureCredential,
            }).ToArray(),
            McpServers = [.. saved.McpServers],
            Skills = saved.Skills,
        });
    }

    private static string? Mask(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return value.Length <= 8
            ? new string('*', value.Length)
            : string.Concat(value[..2], new string('*', value.Length - 4), value[^2..]);
    }
}

