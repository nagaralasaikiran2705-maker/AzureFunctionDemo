using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using Microsoft.Azure.Cosmos;
using System.Text.Json;
using AzureFunctionDemo.Models;

namespace AzureFunctionDemo;

public class BlobFunctions
{
    private readonly ILogger<BlobFunctions> _logger;

    public BlobFunctions(ILogger<BlobFunctions> logger)
    {
        _logger = logger;
    }
    private Container GetContainer()
{
    string endpoint = Environment.GetEnvironmentVariable("CosmosEndpoint");
    string key = Environment.GetEnvironmentVariable("CosmosKey");
    string databaseName = Environment.GetEnvironmentVariable("DatabaseName");
    string containerName = Environment.GetEnvironmentVariable("ContainerName");

    CosmosClient client = new CosmosClient(endpoint, key);

    Database database = client.GetDatabase(databaseName);

    return database.GetContainer(containerName);
}
    [Function("ReadBlob")]
    public async Task<IActionResult> ReadBlob(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "readblob")] HttpRequest req)
    {
        _logger.LogInformation("Reading blob...");

        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        BlobServiceClient serviceClient = new BlobServiceClient(connectionString);

        BlobContainerClient container =
            serviceClient.GetBlobContainerClient("documents");

        BlobClient blob =
            container.GetBlobClient("sample.txt");

        if (!await blob.ExistsAsync())
        {
            return new NotFoundObjectResult("sample.txt not found.");
        }

        var download = await blob.DownloadContentAsync();

        string content = download.Value.Content.ToString();

        return new OkObjectResult(content);
    }

    [Function("WriteBlob")]
    public async Task<IActionResult> WriteBlob(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "writeblob")] HttpRequest req)
    {
        _logger.LogInformation("Writing blob...");

        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        BlobServiceClient serviceClient = new BlobServiceClient(connectionString);

        BlobContainerClient container =
            serviceClient.GetBlobContainerClient("documents");

        BlobClient blob =
            container.GetBlobClient("output.txt");

        string content =
            $"Hello from Azure Function!\nCreated: {DateTime.Now}";

        using MemoryStream stream =
            new MemoryStream(Encoding.UTF8.GetBytes(content));

        await blob.UploadAsync(stream, overwrite: true);

        return new OkObjectResult("output.txt uploaded successfully!");
    }

    [Function("AddEvent")]
public async Task<IActionResult> AddEvent(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addevent")] HttpRequest req)
{
string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

Event? eventData = JsonSerializer.Deserialize<Event>(requestBody);

if (eventData == null)
{
    return new BadRequestObjectResult("Invalid event data.");
}
Container container = GetContainer();

await container.CreateItemAsync(
    eventData,
    new PartitionKey(eventData.eventType));

return new OkObjectResult("Event added successfully!");
}
   [Function("GetEvents")]
    public async Task<IActionResult> GetEvents(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getevents")] HttpRequest req)
    {
        Container container = GetContainer();

        var query = new QueryDefinition("SELECT * FROM c");

        var iterator = container.GetItemQueryIterator<Event>(query);

        List<Event> events = new();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            events.AddRange(response);
        }

        return new OkObjectResult(events);
    }
}

