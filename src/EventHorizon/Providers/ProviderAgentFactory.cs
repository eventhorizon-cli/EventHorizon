using Anthropic;
using EventHorizon.Prompting;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace EventHorizon.Providers;

public interface IProviderAgentFactory
{
    AIAgent CreateAgent(
        Configuration.AppOptions options,
        string instructions,
        IReadOnlyList<AITool> tools,
        AgentSkillsProvider? skillsProvider,
        IServiceProvider services);
}

public sealed class ProviderAgentFactory : IProviderAgentFactory
{
    private readonly IProviderChatClientFactory _providerChatClientFactory;
    private readonly ICodingInstructionsBuilder _codingInstructionsBuilder;

    public ProviderAgentFactory(IProviderChatClientFactory providerChatClientFactory, ICodingInstructionsBuilder codingInstructionsBuilder)
    {
        _providerChatClientFactory = providerChatClientFactory;
        _codingInstructionsBuilder = codingInstructionsBuilder;
    }

    public AIAgent CreateAgent(
        Configuration.AppOptions options,
        string instructions,
        IReadOnlyList<AITool> tools,
        AgentSkillsProvider? skillsProvider,
        IServiceProvider services)
    {
        if (string.Equals(options.Provider.Type, "anthropic", StringComparison.OrdinalIgnoreCase))
        {
            return CreateAnthropicAgent(options, instructions, tools);
        }

        var chatClient = _providerChatClientFactory.CreateChatClient(options.Provider);
        var agentOptions = new ChatClientAgentOptions
        {
            Name = options.Agent.Name,
            Description = options.Agent.Description,
            ChatOptions = new ChatOptions
            {
                Instructions = instructions,
                Tools = [.. tools],
            },
        };

        if (skillsProvider is not null)
        {
            agentOptions.AIContextProviders = [skillsProvider];
        }

        return chatClient.AsAIAgent(agentOptions, services: services);
    }

    private AIAgent CreateAnthropicAgent(Configuration.AppOptions options, string instructions, IReadOnlyList<AITool> tools)
    {
        var anthropicOptions = CloneOptionsWithInstructions(options, instructions);
        var apiKey = anthropicOptions.Provider.ApiKey ?? throw new InvalidOperationException("Provider.ApiKey is required for the anthropic provider.");
        var model = anthropicOptions.Provider.Model ?? throw new InvalidOperationException("Provider.Model is required for the anthropic provider.");
        var anthropicInstructions = _codingInstructionsBuilder.Build(anthropicOptions);

        return new AnthropicClient { ApiKey = apiKey }.AsAIAgent(
            model: model,
            name: anthropicOptions.Agent.Name,
            instructions: anthropicInstructions,
            tools: [.. tools]);
    }

    private static Configuration.AppOptions CloneOptionsWithInstructions(Configuration.AppOptions options, string instructions)
    {
        return new Configuration.AppOptions
        {
            Agent = new Configuration.AgentOptions
            {
                Name = options.Agent.Name,
                Description = options.Agent.Description,
                EnableSkills = options.Agent.EnableSkills,
                EnableShell = options.Agent.EnableShell,
                EnableMcpTools = options.Agent.EnableMcpTools,
                AdditionalSystemPrompts = [instructions],
            },
            Provider = options.Provider,
            CurrentProvider = options.CurrentProvider,
            Providers = options.Providers,
            Pricing = options.Pricing,
            Conversation = options.Conversation,
            McpServers = options.McpServers,
        };
    }
}
