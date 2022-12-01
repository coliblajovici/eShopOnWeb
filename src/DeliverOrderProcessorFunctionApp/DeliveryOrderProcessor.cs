using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.Azure.Cosmos;
using Container = Microsoft.Azure.Cosmos.Container;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;

namespace OrderFunctionApp;

public static class DeliveryOrderProcessor
{
    [FunctionName("DeliveryOrderProcessor")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
         ExecutionContext context,
         ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        DeliveryOrder order = JsonSerializer.Deserialize<DeliveryOrder>(requestBody);

        log.LogInformation($"Order received {order.OrderId}");                

        var config = new ConfigurationBuilder()
           .SetBasePath(context.FunctionAppDirectory)
           .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
           .AddEnvironmentVariables()
           .Build();

        await AddOrderToContainerAsync(order, config, log);

        return new OkObjectResult($"Order received {order.OrderId}");
    }

    private static async Task AddOrderToContainerAsync(DeliveryOrder order, IConfiguration config, ILogger log)
    {
        CosmosOrder cosmosOrder = CreateCosmosOrder(order);

        CosmosClient cosmosClient = new CosmosClient(config["EndpointUri"], config["PrimaryKey"], new CosmosClientOptions() { ApplicationName = "CosmosDBStart" });

        Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync("ordersdb");

        Container container = await database.CreateContainerIfNotExistsAsync("orderscontainer", "/OrderId");
                        
        try
        {            
            ItemResponse<CosmosOrder> response = await container.ReadItemAsync<CosmosOrder>(cosmosOrder.Id, new PartitionKey(cosmosOrder.OrderId));
            log.LogInformation("Item in database with id: {0} already exists\n", response.Resource.Id);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {            
            ItemResponse<CosmosOrder> respose = await container.CreateItemAsync<CosmosOrder>(cosmosOrder, new PartitionKey(cosmosOrder.OrderId));
         
            log.LogInformation("Created item in database with id: {0} Operation consumed {1} RUs.\n", respose.Resource.Id, respose.RequestCharge);
        }
    }

    private static CosmosOrder CreateCosmosOrder(DeliveryOrder deliveryOrder)
    {
        CosmosOrder order = new CosmosOrder()
        {
            Id = Guid.NewGuid().ToString(),
            OrderId = deliveryOrder.OrderId,
            ShippingAddress = new Address(deliveryOrder.ShippingAddress.Street, deliveryOrder.ShippingAddress.City, deliveryOrder.ShippingAddress.State, deliveryOrder.ShippingAddress.Country, deliveryOrder.ShippingAddress.ZipCode),
            FinalPrice = deliveryOrder.FinalPrice
        };

        foreach (var item in deliveryOrder.OrderItemDetails)
        {
            order.OrderItemDetails.Add(
                new OrderItemDetail()
                {
                    ItemId = item.ItemId,
                    Name = item.Name,
                    Quantity = item.Quantity
                });
        };

        return order;
    }
}
