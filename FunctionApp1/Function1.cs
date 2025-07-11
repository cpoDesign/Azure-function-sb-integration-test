using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp1;

public class Function1
{

    private readonly ILogger<Function1> _logger;

    private readonly BlobContainerClient _containerClient;
    public Function1(ILogger<Function1> logger)
    {
        _logger = logger;
        // Emulator connection string
        string connectionString = "UseDevelopmentStorage=true";

        // Create container client
        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient("demo");
        _containerClient.CreateIfNotExists(); // Ensure container exists
    }

    public record MessageSample
    {
        public string Id { get; set; }
        public string Body { get; set; }
    }

    [Function(nameof(Function1))]
    public async Task Run(
        [ServiceBusTrigger("topic.1", "subscription.1", Connection = "ConnectionSetting")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        var recieveMessage = JsonSerializer.Deserialize<MessageSample>(message.Body);

        try
        {
            // Convert message body to string (assumed JSON)
            string jsonString = message.Body.ToString();

            // Generate a GUID-based filename
            string fileName = $"{recieveMessage.Id}.json";

            // Get a blob client and upload
            var blobClient = _containerClient.GetBlobClient(fileName);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
            await blobClient.UploadAsync(stream);

            _logger.LogInformation("Attempting to upload to blob storage at: {Uri}", blobClient.Uri);

            _logger.LogInformation("Uploaded blob: {name}", fileName);

            // Complete the message
            await messageActions.CompleteMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing message to blob storage");
            throw; // will cause message to retry if enabled
        }
    }
}