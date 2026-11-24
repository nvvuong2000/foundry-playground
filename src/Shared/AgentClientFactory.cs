using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.AI;

namespace MasteringAgentFramework.Shared;

public static class AgentClientFactory
{
    public static IChatClient CreateChatClient()
    {
        string endpoint = NormalizeAzureOpenAIEndpoint(GetRequiredEnvironmentVariable("AZURE_OPENAI_ENDPOINT"));
        string deployment = GetRequiredEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");

        AzureOpenAIClientOptions options = new(AzureOpenAIClientOptions.ServiceVersion.V2024_10_21);

        // Dev-only shortcut: when AZURE_OPENAI_API_KEY is set, skip Entra ID (and its MFA prompt).
        // Production should keep using managed identity / AzureCliCredential.
        string? apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        AzureOpenAIClient azureClient = string.IsNullOrWhiteSpace(apiKey)
            ? new(new Uri(endpoint), new AzureCliCredential(), options)
            : new(new Uri(endpoint), new ApiKeyCredential(apiKey), options);
        return azureClient.GetChatClient(deployment).AsIChatClient();
    }

    public static AIAgent CreateFoundryProjectAgent(string instructions, string name, IList<AITool>? tools = null)
    {
        string projectEndpoint = GetRequiredEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT");
        string deploymentName = GetRequiredEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME");

        AIAgent agent = new AIProjectClient(new Uri(projectEndpoint), new AzureCliCredential())
            .AsAIAgent(model: deploymentName, instructions: instructions, name: name, tools: tools);

        return agent;
    }

    private static string NormalizeAzureOpenAIEndpoint(string endpoint)
    {
        // Portal pages show several endpoint forms; the SDK appends its own path, so longer forms can produce invalid URLs and 404s.
        string normalized = endpoint.Trim();
        string[] suffixes =
        [
            "/openai/v1/responses",
            "/openai/v1",
            "/responses",
            "/openai"
        ];

        while (true)
        {
            string trimmed = normalized.TrimEnd('/');

            if (trimmed.Length != normalized.Length)
            {
                normalized = trimmed;
                continue;
            }

            string? matchedSuffix = suffixes.FirstOrDefault(
                suffix => normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

            if (matchedSuffix is null)
            {
                return normalized;
            }

            normalized = normalized[..^matchedSuffix.Length].TrimEnd('/');
        }
    }

    private static string GetRequiredEnvironmentVariable(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing required environment variable '{name}'.");
        }

        return value;
    }
}
