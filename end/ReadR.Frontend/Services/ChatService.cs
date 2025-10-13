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
            
            var systemPrompt = "You are a technical news summarizer. Create a VERY brief summary with:" +
                "1. A catchy title that captures the week's main theme" +
                "2. ONE paragraph (3-5 sentences max) that connects the major themes and developments" +
                "3. Pack the paragraph with inline links to the most important articles - aim for 8-15 links minimum" +
                "4. Be terse and conversational, not listy or structured" +
                "5. Focus on connecting themes rather than listing individual stories" +
                "Format: # [Week Title]\n\n[Dense paragraph with lots of [link text](url) references]";
            
            // Build the user message content from the feed entries
            var userContent = new StringBuilder();
            userContent.AppendLine("Create a super terse weekly summary from these entries. Focus on themes and pack it with links:");
            userContent.AppendLine();

            var entryCount = 0;
            var addedTitles = new HashSet<string>(); // Basic deduplication

            foreach (var entry in entries)
            {
                // Check for cancellation during processing
                cancellationToken.ThrowIfCancellationRequested();
                
                // Basic deduplication - skip very similar titles
                if (addedTitles.Any(title => 
                    string.Equals(title, entry.Title, StringComparison.OrdinalIgnoreCase) ||
                    (title.Length > 20 && entry.Title.Length > 20 && 
                     title[..Math.Min(20, title.Length)].Equals(entry.Title[..Math.Min(20, entry.Title.Length)], StringComparison.OrdinalIgnoreCase))))
                {
                    continue;
                }

                addedTitles.Add(entry.Title);
                entryCount++;
                
                userContent.AppendLine($"Title: {entry.Title}");
                userContent.AppendLine($"Source: {entry.FeedDisplayName}");
                userContent.AppendLine($"Link: {entry.Link}");
                if (!string.IsNullOrEmpty(entry.Description))
                {
                    userContent.AppendLine($"Description: {entry.Description}");
                }
                userContent.AppendLine();
            }

            ChatMessage[] messages = [
                new ChatMessage(ChatRole.System, systemPrompt),
                new ChatMessage(ChatRole.User, userContent.ToString())
            ];

            var options = new ChatOptions
            {
                MaxOutputTokens = 1024 // Reduced for concise summaries
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
}
