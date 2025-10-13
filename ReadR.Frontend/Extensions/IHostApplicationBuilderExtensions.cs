using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting;

public static class IHostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddAzureOpenAi(this IHostApplicationBuilder builder)
    {
        try
        {
            // Validate configuration first
            var endpointValue = builder.Configuration["AZURE_OPENAI_ENDPOINT"];

            // i know this is dirty and gross. we'll make it better. 
            endpointValue = endpointValue.Replace("cognitiveservices", "openai");


            if (string.IsNullOrWhiteSpace(endpointValue))
            {
                // Register a null service that will cause the availability check to fail gracefully
                builder.Services.AddSingleton<IChatClient>(_ => new NullChatClient());
                return builder;
            }

            if (!Uri.TryCreate(endpointValue, UriKind.Absolute, out var endpoint))
            {
                // Register a null service for invalid URL
                builder.Services.AddSingleton<IChatClient>(_ => new NullChatClient());
                return builder;
            }

            // Get model name from configuration with fallback
            var modelName = builder.Configuration["AZURE_OPENAI_MODEL_NAME"] ?? "gpt-4o-mini";

            // Try to create the Azure OpenAI client with error handling
            try
            {
                var azureCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    // Exclude sources that might cause delays or issues in development
                    ExcludeInteractiveBrowserCredential = true,
                    ExcludeVisualStudioCodeCredential = false,
                    ExcludeVisualStudioCredential = false, // Keep for local development
                    ExcludeAzureCliCredential = false,     // Keep for local development
                    ExcludeManagedIdentityCredential = false, // Keep for production
                    // Add retry options for credential acquisition
                    Retry = { MaxRetries = 3, Delay = TimeSpan.FromSeconds(1) }
                });

                var azureOpenAIClient = new AzureOpenAIClient(endpoint, azureCredential);

                var chatClient = new ChatClientBuilder(
                    azureOpenAIClient.GetChatClient(modelName).AsIChatClient())
                    .UseFunctionInvocation()
                    .Build();

                builder.Services.AddSingleton(chatClient);
            }
            catch (Exception)
            {
                // On any error during client creation, register the null client
                builder.Services.AddSingleton<IChatClient>(_ => new NullChatClient());
            }
        }
        catch (Exception)
        {
            // Catch-all error handler to ensure the application doesn't fail to start
            builder.Services.AddSingleton<IChatClient>(_ => new NullChatClient());
        }

        return builder;
    }
}

/// <summary>
/// Null implementation of IChatClient that gracefully handles missing AI functionality
/// </summary>
public class NullChatClient : IChatClient
{
    public ChatClientMetadata Metadata { get; } = new("null-client", null, "Null Chat Client");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("AI service is not available. Please check your configuration.");
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Return empty enumerable instead of throwing to allow availability checks to work
        await Task.CompletedTask;
        yield break;
    }

    public TService? GetService<TService>(object? key = null) where TService : class
    {
        return null;
    }

    public object? GetService(Type serviceType, object? key = null)
    {
        return null;
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
