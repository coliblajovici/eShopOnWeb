using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using Azure;
using System.IO.Pipes;

namespace OrderFunctionApp;

public static class OrderItemsReserver
{
    [FunctionName("OrderItemsReserver")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic order = JsonConvert.DeserializeObject(requestBody);

        log.LogInformation($"Order received {order}");
        string responseMessage = $"Order received {order}.";

        string name = $"order_{Random.Shared.Next()}";
        await UploadOrder(name, log);

        return new OkObjectResult(responseMessage);
    }

    private static async Task UploadOrder(string name,ILogger log)
    {        
        string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=eshopstoragemod3;AccountKey=S4DPYE1km3gzlagGquZrQwHWd90/5l9jZ6D7wRmhsJBCq/P5O/MnViG6Ip/cx1PojN6Qv7GRbPo2+AStLXt6Yg==;EndpointSuffix=core.windows.net";
        BlobServiceClient blobServiceClient = new BlobServiceClient(storageConnectionString);

        BlobContainerClient containerClient = await CreateContainerAsync(blobServiceClient, log);

        BlobClient blobClient = containerClient.GetBlobClient(name);
        // Upload the blob
        await blobClient.UploadAsync(BinaryData.FromString("hello world"), overwrite: true);

      // return blobClient.UploadAsync(order);
    }

    private static async Task<BlobContainerClient> CreateContainerAsync(BlobServiceClient blobServiceClient, ILogger log)
    {
        string containerName = "ordercontainer";

        try
        {
            // Create the container
            BlobContainerClient container = await blobServiceClient.CreateBlobContainerAsync(containerName);

            if (await container.ExistsAsync())
            {
                log.LogInformation("Created container {0}", container.Name);
                return container;
            }
        }
        catch (RequestFailedException e)
        {
            log.LogError("HTTP error code {0}: {1}",e.Status, e.ErrorCode);
            log.LogError(e.Message);
        }

        return null;
    }

    public static Task UploadFile(Stream fileStream, string name)
    {
        string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=eshopstoragemod3;AccountKey=S4DPYE1km3gzlagGquZrQwHWd90/5l9jZ6D7wRmhsJBCq/P5O/MnViG6Ip/cx1PojN6Qv7GRbPo2+AStLXt6Yg==;EndpointSuffix=core.windows.net";
        BlobServiceClient blobServiceClient = new BlobServiceClient(storageConnectionString);

        // Get the container (folder) the file will be saved in
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(storageConnectionString);

        // Get the Blob Client used to interact with (including create) the blob
        BlobClient blobClient = containerClient.GetBlobClient(name);

        // Upload the blob
        blobClient.UploadAsync(BinaryData.FromString("hello world"), overwrite: true);

        return blobClient.UploadAsync(fileStream);

    }
}
