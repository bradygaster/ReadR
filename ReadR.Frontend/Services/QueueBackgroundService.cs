using Azure.Storage.Queues;
using Microsoft.Extensions.Azure;

namespace ReadR.Frontend.Services;

public class QueueBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAzureClientFactory<QueueServiceClient> _queueClientFactory;
    private readonly ILogger<QueueBackgroundService> _logger;
    private readonly IConfiguration _configuration;
    private QueueClient? _queueClient;

    public QueueBackgroundService(
        IServiceProvider serviceProvider,
        IAzureClientFactory<QueueServiceClient> queueClientFactory,
        ILogger<QueueBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _queueClientFactory = queueClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Queue Background Service starting...");

        try
        {
            // Initialize the queue client
            var queueServiceClient = _queueClientFactory.CreateClient("readrstorage");
            var queueName = _configuration["QueueSettings:FeedRefreshQueueName"] ?? "feed-refresh";
            _queueClient = queueServiceClient.GetQueueClient(queueName);

            // Create the queue if it doesn't exist
            await _queueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

            _logger.LogInformation("Monitoring queue '{QueueName}' for feed refresh messages", queueName);

            // Main polling loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Poll for messages (get up to 10 messages at once)
                    var messages = await _queueClient.ReceiveMessagesAsync(
                        maxMessages: 10,
                        visibilityTimeout: TimeSpan.FromMinutes(5), // Hide message for 5 minutes while processing
                        cancellationToken: stoppingToken);

                    if (messages.Value?.Length > 0)
                    {
                        _logger.LogInformation("Received {MessageCount} message(s) from queue", messages.Value.Length);

                        foreach (var message in messages.Value)
                        {
                            try
                            {
                                _logger.LogInformation("Processing message: {MessageId}", message.MessageId);

                                // Create a new scope for scoped services
                                using var scope = _serviceProvider.CreateScope();
                                var feedCacheService = scope.ServiceProvider.GetRequiredService<IFeedCacheService>();

                                // Refresh the feed cache
                                await feedCacheService.RefreshCacheAsync();

                                _logger.LogInformation("Feed cache refreshed successfully for message: {MessageId}", message.MessageId);

                                // Delete the message from the queue after successful processing
                                await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);

                                _logger.LogInformation("Message {MessageId} processed and deleted from queue", message.MessageId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing message {MessageId}: {Error}", message.MessageId, ex.Message);
                                
                                // The message will become visible again after the visibility timeout expires
                                // Azure Storage Queues will automatically retry failed messages
                            }
                        }
                    }

                    // Wait before polling again (adjust this based on your needs)
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in queue polling loop: {Error}", ex.Message);
                    
                    // Wait before retrying after an error
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Queue Background Service: {Error}", ex.Message);
        }

        _logger.LogInformation("Queue Background Service stopped");
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Queue Background Service is stopping...");
        return base.StopAsync(cancellationToken);
    }
}
