using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ReadR.Serverless;

public class FeedListUpdateTrigger
{
    private readonly ILogger<FeedListUpdateTrigger> _logger;

    public FeedListUpdateTrigger(ILogger<FeedListUpdateTrigger> logger)
    {
        _logger = logger;
    }

    [Function(nameof(FeedListUpdateTrigger))]
    [QueueOutput("feed-refresh", Connection = "readrstorage")]
    public async Task<string> Run(
        [BlobTrigger("readr-feeds/{name}", Connection = "readrstorage")] Stream stream, 
        string name)
    {
        using var blobStreamReader = new StreamReader(stream);
        var content = await blobStreamReader.ReadToEndAsync();
        _logger.LogInformation("C# Blob trigger function Processed blob\n Name: {name} \n Data: {content}", name, content);

        // Create a message to send to the queue
        var queueMessage = $"{{\"blobName\": \"{name}\", \"processedAt\": \"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}\", \"contentLength\": {content.Length}}}";
        
        _logger.LogInformation("Sending message to queue: {message}", queueMessage);
        
        return queueMessage;
    }
}