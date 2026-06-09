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
        IReadOnlyList<AIContextProvider> contextProviders,
        IServiceProvider services)
    {
        if (string.Equals(providerOptions.Type, ProviderTypes.Anthropic, StringComparison.OrdinalIgnoreCase))
        {
            return CreateAnthropicAgent(agentOptions, providerOptions, instructions, tools);
        }

        var chatClient = _providerChatClientFactory.CreateChatClient(providerOptions);
        var chatClientAgentOptions = new ChatClientAgentOptions
        {
            Name = agentOptions.Name,
            Description = agentOptions.Description,
            ChatOptions = new ChatOptions
            {
                Instructions = instructions,
                Tools = [.. tools],
            },
            AIContextProviders = [.. contextProviders],
        };

        return chatClient.AsAIAgent(chatClientAgentOptions, services: services);
    }

    private AIAgent CreateAnthropicAgent(AgentOptions agentOptions, ProviderOptions providerOptions, string instructions, IReadOnlyList<AITool> tools)
    {
        var apiKey = providerOptions.ApiKey ?? throw new InvalidOperationException("Provider.ApiKey is required for the anthropic provider.");
        var model = providerOptions.DefaultModel ?? throw new InvalidOperationException("Provider.DefaultModel is required for the anthropic provider.");
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
