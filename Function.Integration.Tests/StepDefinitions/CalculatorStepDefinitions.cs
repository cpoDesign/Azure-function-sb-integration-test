using Reqnroll;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Corvus.Testing.AzureFunctions;
using System.Threading.Tasks;
using Reqnroll.Infrastructure;
using Azure.Messaging.ServiceBus;
using System;
using System.Text.Json;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Threading;
using Azure.Storage.Blobs;
using System.IO;
using System.Linq;

namespace Function.Integration.Tests.StepDefinitions
{
    [Binding]
    public sealed class CalculatorStepDefinitions(IReqnrollOutputHelper _reqnrollOutputHelper)
    {
        private FunctionsController controller;

        [Before]
        public async System.Threading.Tasks.Task BeforeScenarioBlock()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var _logger = loggerFactory.CreateLogger<FunctionsController>();
            _reqnrollOutputHelper.WriteLine("Starting FunctionsController...");

            // Corrected FunctionConfiguration setup  
            var functionConfig = new FunctionConfiguration();
            functionConfig.EnvironmentVariables.Add("AzureWebJobsStorage", "UseDevelopmentStorage=true");
            functionConfig.EnvironmentVariables.Add("FUNCTIONS_WORKER_RUNTIME", "dotnet-isolated");
            functionConfig.EnvironmentVariables.Add("ConnectionSetting", "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;"); // Mock or real Service Bus  
            
            // Create and start a functions instance  
            controller = new FunctionsController(_logger);
            await controller.StartFunctionsInstanceAsync(
                path: "FunctionApp1",     // Function project path  
                port: 7071,                // Port to run on  
                runtime: "net8.0",         // Runtime version  
                provider: "",
                configuration: functionConfig
            );
        }
        [Given(@"I send message to ""(.*)"" with message id ""(.*)""")]
        public async Task GiventheservicebusisrunningAsync(string topicName, string messageId)
        {
            _reqnrollOutputHelper.WriteLine("Inserting a new message...");

            string ServiceBusConnectionString = "Endpoint=sb://localhost/;SharedAccessKeyName=admin;SharedAccessKey=admin;UseDevelopmentEmulator=true;";
            

            var client = new ServiceBusClient(ServiceBusConnectionString);
            var sender = client.CreateSender(topicName);

            var msgData = new MessageSample
            {
                Id = messageId,
                Body = "This is a test message now",
            };

            var msg = new ServiceBusMessage(JsonSerializer.Serialize(msgData));
            await sender.SendMessageAsync(msg);
            _reqnrollOutputHelper.WriteLine($"Message sent to topic '{topicName}' with message id: {msg.MessageId}");
        }

        [Then(@"I will be able to to get file from storage container ""(.*)"" with name ""(.*)""")]
        public async Task GivenIWillBeAbleToGetFileFromStorageContainerWithName(string containerName, string filename)
        {
            // Use the Azure Storage Emulator connection string
            string connectionString = "UseDevelopmentStorage=true";

            // Create the BlobContainerClient
            var containerClient = new BlobContainerClient(connectionString, containerName);

            // Get a reference to the blob
            var blobClient = containerClient.GetBlobClient(filename);

            // Check if the blob exists
            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException($"Blob '{filename}' not found in container '{containerName}'.");
            }

            // Download the blob's content
            var downloadResult = await blobClient.DownloadContentAsync();
            string content = downloadResult.Value.Content.ToString();

            // Output to Reqnroll log (if available) or console
            _reqnrollOutputHelper.WriteLine("Retrieved blob content:");
            _reqnrollOutputHelper.WriteLine(content);

            // Optionally deserialize to check values
            try
            {
                var json = JsonDocument.Parse(content);
                _reqnrollOutputHelper.WriteLine($"JSON contains: {string.Join(", ", json.RootElement.EnumerateObject().Select(p => $"{p.Name}: {p.Value}"))}");
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse blob content as JSON.", ex);
            }
        }

        [Then("I want a bit")]
        public void WhenTheTwoNumbersAreAdded()
        {
            Thread.Sleep(1000); 
        }

        [After]
        public async Task AfterTestRunAsync()
        {
            await controller.TeardownFunctionsAsync();
        }
    }
    public record MessageSample
    {
        public string Id { get; set; }
        public string Body { get; set; }
    }
}