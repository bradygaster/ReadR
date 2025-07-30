using Azure.Storage.Queues;
using Microsoft.Extensions.Azure;

namespace ReadR.Frontend.Services;

public interface IQueueService
{
    Task SendFeedRefreshMessageAsync(string? customMessage = null);
}

public class QueueService : IQueueService
{
    private readonly QueueServiceClient _queueServiceClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QueueService> _logger;

    public QueueService(
        QueueServiceClient queueServiceClient,
        IConfiguration configuration,
        ILogger<QueueService> logger)
    {
        _queueServiceClient = queueServiceClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendFeedRefreshMessageAsync(string? customMessage = null)
    {
        try
        {
            var queueName = _configuration["QueueSettings:FeedRefreshQueueName"] ?? "feed-refresh";
            var queueClient = _queueServiceClient.GetQueueClient(queueName);

            // Create queue if it doesn't exist
            await queueClient.CreateIfNotExistsAsync();

            // Prepare the message
            var message = customMessage ?? $"Feed refresh request at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}";

            // Send the message
            await queueClient.SendMessageAsync(message);

            _logger.LogInformation("Successfully sent feed refresh message to queue '{QueueName}': {Message}", 
                queueName, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send feed refresh message to queue: {Error}", ex.Message);
            throw;
        }
    }
}
