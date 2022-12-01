using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using System.Text.Json;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.Extensions.Configuration;

namespace OrderFunctionApp;

public static class OrderItemsReserver
{
    [FunctionName("OrderItemsReserver")]
    public static async Task Run(
        [ServiceBusTrigger("ordersqueue", Connection = "ServiceBusConnectionString")]
        string message,
        ExecutionContext context,
        ILogger log)
    {
        log.LogInformation("Azure Service Bus trigger function processed a request.");

        OrderPlacedCommand orderPlacedCommand = JsonSerializer.Deserialize<OrderPlacedCommand>(message);

        log.LogInformation($"Order received {orderPlacedCommand.OrderId}");

        var config = new ConfigurationBuilder()
           .SetBasePath(context.FunctionAppDirectory)
           .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
           .AddEnvironmentVariables()
           .Build();

        string name = $"order_{orderPlacedCommand.OrderId}";           

        await UploadOrderToBlobStorage(config, name, message);
    }

    private static async Task UploadOrderToBlobStorage(IConfiguration config, string fileName,string message)
    {                
        BlobServiceClient blobServiceClient = new BlobServiceClient(config["StorageConnectionString"]);
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(config["StorageContainerName"]);

        BlobClient blobClient = containerClient.GetBlobClient(fileName);
        await blobClient.UploadAsync(BinaryData.FromString(message), overwrite: true);      
    }
}
