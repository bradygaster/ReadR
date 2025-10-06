using System.Text;
using Microsoft.Extensions.AI;
using ReadR.Frontend.Models;
using Microsoft.Extensions.Logging;

namespace ReadR.Frontend.Services;

public class ChatService(IChatClient chatClient, ILogger<ChatService> logger)
{
    public async Task<string> SummarizeLastWeeksNews(IEnumerable<FeedEntry> entries, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Starting weekly news summary generation for {Count} entries", entries.Count());
            
            var systemPrompt = "You are a concise summarizer of news articles. Given a series of RSS feed entries, you read the articles in those entries and provide a general summary of the news across all the feeds over the last week." +
                "Your output should be in markdown format, and you should link to the most significant articles throughout the last week's entries in the summary.";
            
            // Build the user message content from the feed entries
            var userContent = new StringBuilder();
            userContent.AppendLine("Here are the RSS feed entries from the last week:");
            userContent.AppendLine();

            foreach (var entry in entries)
            {
                // Check for cancellation during processing
                cancellationToken.ThrowIfCancellationRequested();
                
                userContent.AppendLine($"**Title:** {entry.Title}");
                userContent.AppendLine($"**Source:** {entry.FeedDisplayName}");
                userContent.AppendLine($"**Published:** {entry.PublishDate:yyyy-MM-dd HH:mm}");
                userContent.AppendLine($"**Link:** {entry.Link}");
                if (!string.IsNullOrEmpty(entry.Author))
                {
                    userContent.AppendLine($"**Author:** {entry.Author}");
                }
                if (!string.IsNullOrEmpty(entry.Description))
                {
                    userContent.AppendLine($"**Description:** {entry.Description}");
                }
                if (entry.Categories?.Any() == true)
                {
                    userContent.AppendLine($"**Categories:** {string.Join(", ", entry.Categories)}");
                }
                userContent.AppendLine();
                userContent.AppendLine("---");
                userContent.AppendLine();
            }

            ChatMessage[] messages = [
                new ChatMessage(ChatRole.System, systemPrompt),
                new ChatMessage(ChatRole.User, userContent.ToString())
            ];

            var options = new ChatOptions
            {
                MaxOutputTokens = 4096
            };

            logger.LogInformation("Sending request to AI service for summary generation");
            
            var completionUpdates = chatClient.GetStreamingResponseAsync(messages, options, cancellationToken);
            var sb = new StringBuilder();

            await foreach (var completionUpdate in completionUpdates.WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                sb.Append(completionUpdate.Text);
            }

            var output = sb.ToString();
            
            if (string.IsNullOrWhiteSpace(output))
            {
                throw new InvalidOperationException("AI service returned empty response");
            }
            
            logger.LogInformation("Successfully generated weekly summary ({Length} characters)", output.Length);
            return output;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Weekly summary generation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate weekly summary");
            throw new InvalidOperationException("Failed to generate weekly summary. The AI service may be unavailable.", ex);
        }
    }
    
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Checking AI service availability");
            
            // Perform a minimal test to verify the service is responsive using streaming
            var testMessages = new ChatMessage[]
            {
                new(ChatRole.User, "Test")
            };
            
            var options = new ChatOptions
            {
                MaxOutputTokens = 10
            };
            
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            
            var responseStream = chatClient.GetStreamingResponseAsync(testMessages, options, combinedCts.Token);
            
            // Try to get the first response chunk to verify the service is responsive
            await using var enumerator = responseStream.GetAsyncEnumerator(combinedCts.Token);
            bool hasResponse = await enumerator.MoveNextAsync();
            
            logger.LogInformation("AI service availability check result: {Available}", hasResponse);
            
            return hasResponse;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI service availability check failed");
            return false;
        }
    }
}
