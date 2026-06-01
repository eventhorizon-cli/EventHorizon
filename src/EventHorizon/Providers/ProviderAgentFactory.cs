using Anthropic;
using EventHorizon.Configuration;
using EventHorizon.Prompting;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace EventHorizon.Providers;

// TODO: Consider pooling agents
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
        AgentOptions agentOptions,
        ProviderOptions providerOptions,
        string instructions,
        IReadOnlyList<AITool> tools,
        AgentSkillsProvider? skillsProvider,
        IServiceProvider services)
    {
        if (string.Equals(providerOptions.Type, "anthropic", StringComparison.OrdinalIgnoreCase))
        {
            return CreateAnthropicAgent(agentOptions, providerOptions, instructions, tools);
        }

        var chatClient = _providerChatClientFactory.CreateChatClient(providerOptions);
        if (skillsProvider is null)
        {
            return chatClient.AsAIAgent(
                name: agentOptions.Name,
                description: agentOptions.Description,
                instructions: instructions,
                tools: [.. tools],
                services: services);
        }

        var chatClientAgentOptions = new ChatClientAgentOptions
        {
            Name = agentOptions.Name,
            Description = agentOptions.Description,
            ChatOptions = new ChatOptions
            {
                Instructions = instructions,
                Tools = [.. tools],
            },
            AIContextProviders = [skillsProvider],
        };

        return chatClient.AsAIAgent(chatClientAgentOptions, services: services);
    }

    private AIAgent CreateAnthropicAgent(AgentOptions agentOptions, ProviderOptions providerOptions, string instructions, IReadOnlyList<AITool> tools)
    {
        var apiKey = providerOptions.ApiKey ?? throw new InvalidOperationException("Provider.ApiKey is required for the anthropic provider.");
        var model = providerOptions.Model ?? throw new InvalidOperationException("Provider.Model is required for the anthropic provider.");
        var anthropicAgentOptions = CloneAgentOptionsWithInstructions(agentOptions, instructions);
        var anthropicInstructions = _codingInstructionsBuilder.Build(anthropicAgentOptions);

        return new AnthropicClient { ApiKey = apiKey }.AsAIAgent(
            model: model,
            name: anthropicAgentOptions.Name,
            instructions: anthropicInstructions,
            tools: [.. tools]);
    }

    private static AgentOptions CloneAgentOptionsWithInstructions(AgentOptions options, string instructions)
        => new()
        {
            Name = options.Name,
            Description = options.Description,
            EnableSkills = options.EnableSkills,
            EnableShell = options.EnableShell,
            EnableMcpTools = options.EnableMcpTools,
            AdditionalSystemPrompts = [instructions],
        };
}
