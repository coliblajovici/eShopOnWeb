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
using Microsoft.eShopWeb.ApplicationCore.Entities;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Reflection.Metadata;

namespace OrderFunctionApp;

public static class OrderItemsReserverServiceBus
{
    [FunctionName("OrderItemsReserverServiceBus")]
    public static async Task Run(
        [ServiceBusTrigger("orderstopic", "orderreservesubscription", Connection = "ServiceBusConnectionString")]
        string message,
        ILogger log)
    {
        log.LogInformation("Azure Service Bus trigger function processed a request.");

        OrderPlacedEvent order = JsonSerializer.Deserialize<OrderPlacedEvent>(message);

        log.LogInformation($"Order received {order.OrderId}");
        
        string name = $"order_{order.OrderId}";

        await UploadOrder(name, message, log);   
    }

    private static async Task UploadOrder(string name,string message, ILogger log)
    {        
        string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=ordersstorage222;AccountKey=0PyYOnFvvvbGtrlLYvA9RC3wN5jeD6UYg9hWQ7daLhaELktSZpMWghjvzOq2Ti6/5/XR1Wk/zseP+AStk2ZiHA==;EndpointSuffix=core.windows.net";
        BlobServiceClient blobServiceClient = new BlobServiceClient(storageConnectionString);
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("orderscontainer222");

        BlobClient blobClient = containerClient.GetBlobClient(name);
        await blobClient.UploadAsync(BinaryData.FromString(message), overwrite: true);      
    }    
}
