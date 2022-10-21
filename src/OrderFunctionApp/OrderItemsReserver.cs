using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure;
using System.Net;
using Microsoft.Azure.Cosmos;
using Container = Microsoft.Azure.Cosmos.Container;
using System.Text.Json;

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
        CosmosOrder order = JsonSerializer.Deserialize<CosmosOrder>(requestBody);

        log.LogInformation($"Order received {order.ToString()}");
        string responseMessage = $"Order received {order.ToString()}.";

        string name = $"order_{Random.Shared.Next()}";
        order.Id = Guid.NewGuid().ToString();
        //await UploadOrder(name, log);
        await AddOrderToContainerAsync(@"https://cosmoscorina.documents.azure.com:443/", "kNtzVZFT4PohLXeHpodJOw0xIVMpy70CbHdoA0KHpPjBnTpTabTnhFhQaozwq72hmaFgkgcS1QpVO0wfGf4GAw==", order,log);

        return new OkObjectResult(responseMessage);
    }



    // <AddItemsToContainerAsync>
    /// <summary>
    /// Add Family items to the container
    /// </summary>
    private static async Task AddOrderToContainerAsync(string endpointUri, string primaryKey, CosmosOrder order,ILogger log)
    {
        CosmosClient cosmosClient = new CosmosClient(endpointUri, primaryKey, new CosmosClientOptions() { ApplicationName = "CosmosDBStart" });

        Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync("ordersdb");
        Container container = await database.CreateContainerIfNotExistsAsync("orderscontainer", "/OrderId");
                
        log.LogInformation(order.FinalPrice.ToString());
        
        try
        {
            // Read the item to see if it exists.  
            ItemResponse<CosmosOrder> response = await container.ReadItemAsync<CosmosOrder>(order.Id, new PartitionKey(order.OrderId));
            log.LogInformation("Item in database with id: {0} already exists\n", response.Resource.Id);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Create an item in the container representing the Andersen family. Note we provide the value of the partition key for this item, which is "Andersen"
            ItemResponse<CosmosOrder> respose = await container.CreateItemAsync<CosmosOrder>(order, new PartitionKey(order.OrderId));

            // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
            log.LogInformation("Created item in database with id: {0} Operation consumed {1} RUs.\n", respose.Resource.Id, respose.RequestCharge);
        }

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
}
